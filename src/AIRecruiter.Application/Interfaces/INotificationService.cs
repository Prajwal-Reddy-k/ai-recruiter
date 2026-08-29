using AIRecruiter.Application.DTOs.Notifications;

namespace AIRecruiter.Application.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetMyNotificationsAsync(int userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default);
    Task MarkAsReadAsync(int userId, int notificationId, CancellationToken ct = default);
    Task NotifyAsync(int userId, string type, string message, string? relatedEntityType = null, int? relatedEntityId = null, CancellationToken ct = default);
}
