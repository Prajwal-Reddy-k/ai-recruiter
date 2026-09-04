namespace AIRecruiter.Application.DTOs.Activity;

/// <summary>Source is "Action" (from AuditLogEntry — something the caller did) or
/// "Notification" (something that happened to the caller — a message received, an
/// interview proposed, an offer sent). See ActivityTimelineService for the merge.</summary>
public record ActivityTimelineEntryDto(
    string Type,
    string Message,
    DateTime TimestampUtc,
    string? RelatedEntityType,
    int? RelatedEntityId,
    string Source);
