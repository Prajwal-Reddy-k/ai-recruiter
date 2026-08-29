using AIRecruiter.Application.DTOs.Notifications;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetMyNotificationsAsync(int userId, CancellationToken ct = default)
    {
        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto(n.Id, n.Type, n.Message, n.RelatedEntityType, n.RelatedEntityId, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);

        return notifications;
    }

    public Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task MarkAsReadAsync(int userId, int notificationId, CancellationToken ct = default)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId, ct)
            ?? throw new NotFoundException("Notification not found.");

        if (notification.UserId != userId)
        {
            throw new ForbiddenException("You do not have access to this notification.");
        }

        notification.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task NotifyAsync(int userId, string type, string message, string? relatedEntityType = null, int? relatedEntityId = null, CancellationToken ct = default)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Message = message,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
        });
        await _db.SaveChangesAsync(ct);
    }
}
