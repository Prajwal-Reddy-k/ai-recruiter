using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Rotated whenever the user's password is reset via the forgot-password flow.
    /// Embedded as a JWT claim and re-checked against this value on every request, so a
    /// password reset invalidates any JWT issued before it — the closest thing to session
    /// revocation this stateless-JWT design supports without a token blocklist.</summary>
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Set when the user requests account deletion; cleared if they cancel during
    /// the grace period. JobLifecycleSweepService deactivates the account once this is more
    /// than the grace period in the past — see UserProfileService.RequestAccountDeletionAsync.</summary>
    public DateTime? DeletionRequestedAt { get; set; }

    public CandidateProfile? CandidateProfile { get; set; }
    public RecruiterProfile? RecruiterProfile { get; set; }
    public ICollection<PasswordResetCode> PasswordResetCodes { get; set; } = new List<PasswordResetCode>();
}
