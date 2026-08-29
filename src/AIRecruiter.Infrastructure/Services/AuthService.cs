using AIRecruiter.Application.DTOs.Auth;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IAuditLogService _auditLog;

    public AuthService(AppDbContext db, ITokenService tokenService, IAuditLogService auditLog)
    {
        _db = db;
        _tokenService = tokenService;
        _auditLog = auditLog;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (request.Role == UserRole.Admin)
        {
            throw new ValidationException("Only Candidate or Recruiter roles can self-register.");
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
        return new AuthResponse(user.Id, user.FullName, user.Email, user.Role.ToString(), token, expiresAt);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
