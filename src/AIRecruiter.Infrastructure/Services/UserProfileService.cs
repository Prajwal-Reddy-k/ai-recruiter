using System.Text.RegularExpressions;
using AIRecruiter.Application.DTOs.Users;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class UserProfileService : IUserProfileService
{
    private static readonly Regex IndianMobileRegex = new("^[6-9][0-9]{9}$", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;

    public UserProfileService(AppDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<UserDetailsDto> GetMyDetailsAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User not found.");
        return ToDto(user);
    }

    public async Task<UserDetailsDto> UpdateMyDetailsAsync(int userId, UpdateUserDetailsRequest request, CancellationToken ct = default)
    {
        if (!CandidateProfileValidator.IsValidFullName(request.FullName))
        {
            throw new ValidationException(
                "Enter a valid name (2-100 characters, letters and spaces only).",
                new Dictionary<string, string> { ["fullName"] = "Enter a valid name (2-100 characters, letters and spaces only)." });
        }

        string? normalizedPhone = null;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            normalizedPhone = NormalizePhone(request.PhoneNumber);
            if (normalizedPhone is null)
            {
                throw new ValidationException(
                    "Enter a valid 10-digit Indian mobile number.",
                    new Dictionary<string, string> { ["phoneNumber"] = "Enter a valid 10-digit Indian mobile number." });
            }
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User not found.");

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = normalizedPhone;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(userId, user.Role.ToString(), "UserDetailsUpdated", "User", user.Id, null, ct);

        return ToDto(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ValidationException(
                "Passwords do not match.",
                new Dictionary<string, string> { ["confirmPassword"] = "Passwords do not match." });
        }

        if (request.NewPassword.Length < 8)
        {
            throw new ValidationException(
                "New password must be at least 8 characters.",
                new Dictionary<string, string> { ["newPassword"] = "New password must be at least 8 characters." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ValidationException(
                "Current password is incorrect.",
                new Dictionary<string, string> { ["currentPassword"] = "Current password is incorrect." });
        }

        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
        {
            throw new ValidationException(
                "New password must be different from your current password.",
                new Dictionary<string, string> { ["newPassword"] = "New password must be different from your current password." });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        // Rotating the security stamp invalidates every JWT issued before this moment — same
        // mechanism as AuthService.ResetPasswordAsync — so other active sessions are signed out.
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(userId, user.Role.ToString(), "PasswordChanged", "User", user.Id, null, ct);
    }

    /// <summary>The grace period before JobLifecycleSweepService actually deactivates the
    /// account — matches the existing 14-day invitation-expiry precedent elsewhere.</summary>
    private static readonly TimeSpan DeletionGracePeriod = TimeSpan.FromDays(14);

    public async Task<AccountDeletionStatusDto> RequestAccountDeletionAsync(int userId, RequestAccountDeletionRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new ValidationException(
                "Incorrect password.",
                new Dictionary<string, string> { ["password"] = "Incorrect password." });
        }

        // Deliberately non-destructive and NOT immediate: starts a grace period rather than
        // deactivating right away, so the account stays active and usable, and the request can
        // be cancelled — JobLifecycleSweepService performs the actual deactivation (the same
        // IsActive=false + SecurityStamp-rotation mechanism AdminService.SuspendUserAsync uses)
        // once the grace period has elapsed and the request hasn't been cancelled.
        user.DeletionRequestedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(userId, user.Role.ToString(), "AccountDeletionRequested", "User", user.Id, null, ct);

        return ToDeletionStatusDto(user);
    }

    public async Task<AccountDeletionStatusDto> CancelAccountDeletionAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User not found.");

        user.DeletionRequestedAt = null;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(userId, user.Role.ToString(), "AccountDeletionCancelled", "User", user.Id, null, ct);

        return ToDeletionStatusDto(user);
    }

    public async Task<PrivacySummaryDto> GetPrivacySummaryAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User not found.");

        var candidateProfile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        var preferences = await _db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);

        return new PrivacySummaryDto(
            candidateProfile?.ProfileVisibility.ToString(),
            preferences?.MessagesEnabled ?? true,
            preferences?.ApplicationsEnabled ?? true,
            preferences?.InterviewsEnabled ?? true,
            preferences?.InvitationsEnabled ?? true,
            ToDeletionStatusDto(user));
    }

    private static AccountDeletionStatusDto ToDeletionStatusDto(User user)
    {
        if (user.DeletionRequestedAt is not { } requestedAt)
        {
            return new AccountDeletionStatusDto(false, null, null, null);
        }

        var scheduledAt = requestedAt + DeletionGracePeriod;
        var daysRemaining = Math.Max(0, (int)Math.Ceiling((scheduledAt - DateTime.UtcNow).TotalDays));
        return new AccountDeletionStatusDto(true, requestedAt, scheduledAt, daysRemaining);
    }

    private static UserDetailsDto ToDto(User user) => new(user.Id, user.FullName, user.Email, user.PhoneNumber, user.Role.ToString());

    /// <summary>Strips a leading country code / trunk prefix and formatting characters,
    /// leaving a bare 10-digit number for storage — mirrors CandidateProfileValidator's
    /// normalization so the same phone number always ends up stored the same way.</summary>
    private static string? NormalizePhone(string phone)
    {
        var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

        var bare = digitsOnly.Length switch
        {
            10 => digitsOnly,
            11 when digitsOnly.StartsWith('0') => digitsOnly[1..],
            12 when digitsOnly.StartsWith("91") => digitsOnly[2..],
            13 when digitsOnly.StartsWith("091") => digitsOnly[3..],
            _ => null,
        };

        return bare is not null && IndianMobileRegex.IsMatch(bare) ? bare : null;
    }
}
