namespace AIRecruiter.Domain.Enums;

/// <summary>Only the base lifecycle is persisted. Interviewing/Hired/NotSelected are derived
/// at read time from the linked JobApplication's status, never separately tracked here — a
/// second source of truth for the same fact would drift.</summary>
public enum ReferralStatus
{
    Invited = 1,
    Registered = 2,
    Applied = 3
}
