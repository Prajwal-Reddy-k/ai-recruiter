using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

/// <summary>A report against any reportable entity (job posting, company, message, or user),
/// keyed polymorphically by EntityType+EntityId — deliberately no hard FK to any specific
/// table (the same "polymorphic, no FK" convention AuditLogEntry already uses for EntityId),
/// since a single report table would otherwise need four nullable FK columns.</summary>
public class Report : BaseEntity
{
    public ReportedEntityType EntityType { get; set; }
    public int EntityId { get; set; }

    public int ReportedByUserId { get; set; }
    public User ReportedByUser { get; set; } = null!;

    public ReportReason Reason { get; set; }
    public string? Details { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Open;

    public int? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ModerationNote { get; set; }
}
