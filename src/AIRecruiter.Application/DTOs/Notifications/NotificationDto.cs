namespace AIRecruiter.Application.DTOs.Notifications;

public record NotificationDto(
    int Id,
    string Type,
    string Message,
    string? RelatedEntityType,
    int? RelatedEntityId,
    bool IsRead,
    DateTime CreatedAt);
