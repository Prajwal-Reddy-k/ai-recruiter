using AIRecruiter.Application.DTOs.Users;

namespace AIRecruiter.Application.Interfaces;

public interface INotificationPreferenceService
{
    Task<NotificationPreferenceDto> GetMyPreferencesAsync(int userId, CancellationToken ct = default);
    Task<NotificationPreferenceDto> UpdateMyPreferencesAsync(int userId, UpdateNotificationPreferenceRequest request, CancellationToken ct = default);
}
