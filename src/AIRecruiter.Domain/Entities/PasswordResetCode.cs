using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

/// <summary>A single forgot-password attempt: a hashed 6-digit code, and — once that code is
/// verified — a hashed, short-lived reset token that alone authorizes the password change.
/// Never stores the raw code or raw token, only their hashes.</summary>
public class PasswordResetCode : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; }
    public int FailedAttempts { get; set; }

    public string? ResetTokenHash { get; set; }
    public DateTime? ResetTokenExpiresAtUtc { get; set; }
    public bool ResetTokenUsed { get; set; }
}
