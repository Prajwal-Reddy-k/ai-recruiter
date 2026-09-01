using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

/// <summary>A single message in the conversation attached to one job application — the
/// application itself is the conversation key (no separate Conversation table), matching how
/// Interview avoids denormalizing foreign keys already reachable through a relationship.
/// Body is always rendered as plain text on the frontend (React's default escaping) — never
/// via dangerouslySetInnerHTML — so no HTML sanitization step is needed here.</summary>
public class Message : BaseEntity
{
    public int JobApplicationId { get; set; }
    public JobApplication JobApplication { get; set; } = null!;

    public int SenderUserId { get; set; }
    public User SenderUser { get; set; } = null!;
    public string SenderRole { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
