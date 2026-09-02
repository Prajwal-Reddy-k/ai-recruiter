using System.Security.Cryptography;
using System.Text;
using AIRecruiter.Application.Common;
using AIRecruiter.Application.DTOs.Auth;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Email;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIRecruiter.Infrastructure.Services;

public class AuthService : IAuthService
{
    private const int CodeExpiryMinutes = 10;
    private const int ResetTokenExpiryMinutes = 10;
    private const int MaxCodeAttempts = 5;
    private const int ResendCooldownSeconds = 60;
    private const int MaxCodesPerDay = 5;

    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IAuditLogService _auditLog;
    private readonly IEmailSender _emailSender;
    private readonly IIpRateLimiter _rateLimiter;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext db,
        ITokenService tokenService,
        IAuditLogService auditLog,
        IEmailSender emailSender,
        IIpRateLimiter rateLimiter,
        ILogger<AuthService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _auditLog = auditLog;
        _emailSender = emailSender;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (request.Role == UserRole.Admin)
        {
            throw new ValidationException("Only Candidate or Recruiter roles can self-register.");
        }

        if (!CandidateProfileValidator.IsValidFullName(request.FullName))
        {
            throw new ValidationException(
                "Full name is required and must be 2-100 characters using letters, spaces, and reasonable punctuation only.",
                new Dictionary<string, string> { ["fullName"] = "Enter a valid name (2-100 characters, letters and spaces only)." });
        }

        var normalizedEmail = NormalizeEmail(request.Email);

        var emailExists = await _db.Users.AnyAsync(u => u.Email == normalizedEmail, ct);
        if (emailExists)
        {
            throw new ConflictException("EMAIL_TAKEN", "A user with this email already exists.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role
        };

        if (request.Role == UserRole.Candidate)
        {
            user.CandidateProfile = new CandidateProfile();
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(request.ReferralToken))
        {
            // Invalid/expired/already-used tokens are silently ignored — registration must
            // always succeed on its own merits regardless of referral-link validity.
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.ReferralToken)));
            var referral = await _db.Referrals.FirstOrDefaultAsync(r =>
                r.TokenHash == tokenHash && r.Status == ReferralStatus.Invited &&
                r.TokenExpiresAtUtc > DateTime.UtcNow && r.RegisteredUserId == null, ct);
            if (referral is not null)
            {
                referral.RegisteredUserId = user.Id;
                referral.RegisteredAtUtc = DateTime.UtcNow;
                referral.Status = ReferralStatus.Registered;
                await _db.SaveChangesAsync(ct);
            }
        }

        await _auditLog.LogAsync(user.Id, user.Role.ToString(), "UserRegistered", "User", user.Id, new { user.Role }, ct);

        var (token, expiresAt) = _tokenService.GenerateToken(user);
        return new AuthResponse(user.Id, user.FullName, user.Email, user.Role.ToString(), token, expiresAt);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        // Deliberately the same exception/message for "no such user", "wrong password", and
        // "deactivated account" — none of these should let a caller distinguish whether a
        // given email is registered.
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash) || !user.IsActive)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var (token, expiresAt) = _tokenService.GenerateToken(user);
        var avatarUrl = await GetAvatarUrlAsync(user, ct);
        return new AuthResponse(user.Id, user.FullName, user.Email, user.Role.ToString(), token, expiresAt, avatarUrl);
    }

    private async Task<string?> GetAvatarUrlAsync(User user, CancellationToken ct)
    {
        if (user.Role != UserRole.Candidate) return null;

        var profile = await _db.CandidateProfiles
            .Where(c => c.UserId == user.Id)
            .Select(c => new { c.Id, c.AvatarStorageKey })
            .FirstOrDefaultAsync(ct);

        return profile is null ? null : AvatarUrlFormatter.Format(profile.Id, profile.AvatarStorageKey);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, string ipAddress, CancellationToken ct = default)
    {
        const string genericMessage = "If an account exists for this email, a verification code has been sent.";

        // IP-level abuse guard — independent of whether the email exists, so this can't be
        // used to fingerprint valid accounts either.
        if (!_rateLimiter.IsAllowed($"forgot-password:ip:{ipAddress}", maxRequests: 10, TimeSpan.FromHours(1)))
        {
            throw new RateLimitedException("Too many password reset requests from this location. Please try again later.", retryAfterSeconds: 3600);
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is null)
        {
            // Never reveal whether the email exists — return the generic message without
            // creating a code or sending anything.
            return new ForgotPasswordResponse(genericMessage);
        }

        var since = DateTime.UtcNow.AddSeconds(-ResendCooldownSeconds);
        var recentCode = await _db.PasswordResetCodes
            .Where(c => c.UserId == user.Id)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (recentCode is not null && recentCode.CreatedAt > since)
        {
            // Resend cooldown — stay silent rather than telling the caller "please wait",
            // which would itself reveal that this email is registered.
            return new ForgotPasswordResponse(genericMessage);
        }

        var sinceToday = DateTime.UtcNow.AddDays(-1);
        var codesToday = await _db.PasswordResetCodes.CountAsync(c => c.UserId == user.Id && c.CreatedAt > sinceToday, ct);
        if (codesToday >= MaxCodesPerDay)
        {
            return new ForgotPasswordResponse(genericMessage);
        }

        // Invalidate every previous unused code for this user before issuing a new one.
        var previousCodes = await _db.PasswordResetCodes.Where(c => c.UserId == user.Id && !c.IsUsed).ToListAsync(ct);
        foreach (var previous in previousCodes)
        {
            previous.IsUsed = true;
        }

        var rawCode = GenerateSixDigitCode();
        var codeRow = new PasswordResetCode
        {
            UserId = user.Id,
            CodeHash = BCrypt.Net.BCrypt.HashPassword(rawCode),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(CodeExpiryMinutes),
        };
        _db.PasswordResetCodes.Add(codeRow);
        await _db.SaveChangesAsync(ct);

        var (subject, html, text) = PasswordResetEmailTemplate.Build(rawCode, CodeExpiryMinutes);
        try
        {
            await _emailSender.SendAsync(user.Email, subject, html, text, ct);
        }
        catch (Exception ex)
        {
            // Sending failures never surface to the caller — the response contract must not
            // change based on whether email delivery succeeded, or it would reveal account
            // existence via timing/error-shape differences.
            _logger.LogError(ex, "Failed to send password reset email (recipient/content suppressed).");
        }

        await _auditLog.LogAsync(user.Id, user.Role.ToString(), "PasswordResetRequested", "User", user.Id, null, ct);

        return new ForgotPasswordResponse(genericMessage);
    }

    public async Task<VerifyResetCodeResponse> VerifyResetCodeAsync(VerifyResetCodeRequest request, string ipAddress, CancellationToken ct = default)
    {
        const string genericError = "Invalid or expired code.";

        if (!_rateLimiter.IsAllowed($"verify-reset-code:ip:{ipAddress}", maxRequests: 20, TimeSpan.FromHours(1)))
        {
            throw new RateLimitedException("Too many attempts. Please try again later.", retryAfterSeconds: 3600);
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        if (user is null)
        {
            throw new ValidationException(genericError);
        }

        if (!_rateLimiter.IsAllowed($"verify-reset-code:email:{normalizedEmail}", maxRequests: 20, TimeSpan.FromHours(1)))
        {
            throw new RateLimitedException("Too many attempts. Please try again later.", retryAfterSeconds: 3600);
        }

        var codeRow = await _db.PasswordResetCodes
            .Where(c => c.UserId == user.Id && !c.IsUsed)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (codeRow is null || codeRow.ExpiresAtUtc < DateTime.UtcNow || codeRow.FailedAttempts >= MaxCodeAttempts)
        {
            await _auditLog.LogAsync(user.Id, user.Role.ToString(), "PasswordResetVerificationFailed", "User", user.Id, new { Reason = "NoValidCode" }, ct);
            throw new ValidationException(genericError);
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Code, codeRow.CodeHash))
        {
            codeRow.FailedAttempts++;
            await _db.SaveChangesAsync(ct);
            await _auditLog.LogAsync(user.Id, user.Role.ToString(), "PasswordResetVerificationFailed", "User", user.Id, new { Reason = "WrongCode", codeRow.FailedAttempts }, ct);
            throw new ValidationException(genericError);
        }

        // The code is consumed the moment it's verified — it can never be used again, even
        // to mint a second reset token.
        codeRow.IsUsed = true;

        var rawToken = GenerateResetToken();
        codeRow.ResetTokenHash = HashToken(rawToken);
        codeRow.ResetTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(ResetTokenExpiryMinutes);
        codeRow.ResetTokenUsed = false;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(user.Id, user.Role.ToString(), "PasswordResetVerificationSucceeded", "User", user.Id, null, ct);

        return new VerifyResetCodeResponse(rawToken, codeRow.ResetTokenExpiresAtUtc.Value);
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        const string genericError = "This reset link has expired or is invalid. Please request a new code.";

        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ValidationException("Passwords do not match.");
        }

        var tokenHash = HashToken(request.ResetToken);
        var codeRow = await _db.PasswordResetCodes
            .Include(c => c.User)
            .FirstOrDefaultAsync(c =>
                c.ResetTokenHash == tokenHash &&
                !c.ResetTokenUsed &&
                c.ResetTokenExpiresAtUtc != null &&
                c.ResetTokenExpiresAtUtc > DateTime.UtcNow, ct);

        if (codeRow is null)
        {
            throw new ValidationException(genericError);
        }

        var user = codeRow.User;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        // Rotating the security stamp invalidates every JWT issued before this moment —
        // see User.SecurityStamp and the JWT validation event in Program.cs.
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        codeRow.ResetTokenUsed = true;

        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(user.Id, user.Role.ToString(), "PasswordResetCompleted", "User", user.Id, null, ct);

        return new ResetPasswordResponse("Password updated successfully. Please sign in with your new password.");
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string GenerateSixDigitCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string GenerateResetToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
