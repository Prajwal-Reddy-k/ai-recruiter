namespace AIRecruiter.Application.DTOs.Audit;

public record AuditLogEntryDto(
    int Id,
    int? ActorUserId,
    string? ActorName,
    string? ActorRole,
    string ActionType,
    string EntityType,
    int? EntityId,
    DateTime TimestampUtc,
    string? MetadataJson);
