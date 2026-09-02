using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

public class Referral : BaseEntity
{
    public int ReferrerUserId { get; set; }
    public User ReferrerUser { get; set; } = null!;

    public int JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = null!;

    public string ReferredName { get; set; } = string.Empty;

    /// <summary>Stored lowercase-trimmed, same normalization convention as User.Email.</summary>
    public string ReferredEmail { get; set; } = string.Empty;

    public string? ReferredPhone { get; set; }
    public string? RelevantSkillsCsv { get; set; }
    public string? Note { get; set; }

    /// <summary>SHA256 hex hash of the raw token — the raw token is returned to the
    /// referrer exactly once and never persisted, matching AuthService's reset-token
    /// convention.</summary>
    public string TokenHash { get; set; } = string.Empty;
    public DateTime TokenExpiresAtUtc { get; set; }

    public ReferralStatus Status { get; set; } = ReferralStatus.Invited;

    public int? RegisteredUserId { get; set; }
    public User? RegisteredUser { get; set; }
    public DateTime? RegisteredAtUtc { get; set; }

    public int? JobApplicationId { get; set; }
    public JobApplication? JobApplication { get; set; }
    public DateTime? AppliedAtUtc { get; set; }
}
