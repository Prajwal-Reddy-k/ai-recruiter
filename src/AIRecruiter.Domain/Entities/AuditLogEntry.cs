using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

public class AuditLogEntry : BaseEntity
{
    public int? ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public string? ActorRole { get; set; }

    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string? MetadataJson { get; set; }
}
