using AIRecruiter.Application.DTOs.Users;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly AppDbContext _db;

    public NotificationPreferenceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<NotificationPreferenceDto> GetMyPreferencesAsync(int userId, CancellationToken ct = default)
    {
        var prefs = await GetOrCreateAsync(_db, userId, ct);
        return ToDto(prefs);
    }

    public async Task<NotificationPreferenceDto> UpdateMyPreferencesAsync(int userId, UpdateNotificationPreferenceRequest request, CancellationToken ct = default)
    {
        var prefs = await GetOrCreateAsync(_db, userId, ct);
        prefs.MessagesEnabled = request.MessagesEnabled;
        prefs.ApplicationsEnabled = request.ApplicationsEnabled;
        prefs.InterviewsEnabled = request.InterviewsEnabled;
        prefs.InvitationsEnabled = request.InvitationsEnabled;
        await _db.SaveChangesAsync(ct);
        return ToDto(prefs);
    }

    /// <summary>Lazily creates a default (all-enabled) preference row on first access —
    /// avoids backfilling one for every pre-existing user. Internal so NotificationService
    /// can reuse the exact same get-or-create + defaults without a second implementation.</summary>
    internal static async Task<NotificationPreference> GetOrCreateAsync(AppDbContext db, int userId, CancellationToken ct)
    {
        var prefs = await db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (prefs is not null) return prefs;

        prefs = new NotificationPreference { UserId = userId };
        db.NotificationPreferences.Add(prefs);
        await db.SaveChangesAsync(ct);
        return prefs;
    }

    private static NotificationPreferenceDto ToDto(NotificationPreference p) =>
        new(p.MessagesEnabled, p.ApplicationsEnabled, p.InterviewsEnabled, p.InvitationsEnabled);
}
