using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

/// <summary>One-to-one with User, created lazily on first access (get-or-create) rather than
/// backfilled for every existing user. Covers the notification "type" categories that
/// NotificationService.NotifyAsync groups its existing call sites into — a "System" bucket
/// (e.g. JobExpired) is intentionally NOT toggleable here, so account-level notices can never
/// be silenced.</summary>
public class NotificationPreference : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public bool MessagesEnabled { get; set; } = true;
    public bool ApplicationsEnabled { get; set; } = true;
    public bool InterviewsEnabled { get; set; } = true;
    public bool InvitationsEnabled { get; set; } = true;
}
