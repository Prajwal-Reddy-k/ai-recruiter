using System.Text.RegularExpressions;
using AIRecruiter.Application.DTOs.Activity;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Reuses the existing AuditLogEntry (actor-side) and Notification (recipient-side)
/// tables — no new tracking mechanism. AuditLogEntry only ever records the actor, never a
/// recipient, so "things that happened to me" (a message received, an interview proposed, an
/// offer sent) would be invisible from AuditLogEntry alone; Notification already covers that
/// half reliably via the app's existing dual-write convention at every relevant call site.</summary>
public class ActivityTimelineService : IActivityTimelineService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;

    public ActivityTimelineService(AppDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<ActivityTimelineEntryDto>> GetMyTimelineAsync(
        int userId, string? typeFilter, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var actions = await _auditLog.GetForUserAsync(userId, typeFilter, from, to, ct);

        var notificationQuery = _db.Notifications.Where(n => n.UserId == userId);
        if (!string.IsNullOrWhiteSpace(typeFilter))
        {
            notificationQuery = notificationQuery.Where(n => n.Type == typeFilter);
        }
        if (from.HasValue)
        {
            notificationQuery = notificationQuery.Where(n => n.CreatedAt >= from.Value);
        }
        if (to.HasValue)
        {
            notificationQuery = notificationQuery.Where(n => n.CreatedAt <= to.Value);
        }
        var notifications = await notificationQuery.OrderByDescending(n => n.CreatedAt).Take(300).ToListAsync(ct);

        var entries = new List<ActivityTimelineEntryDto>();
        entries.AddRange(actions.Select(a => new ActivityTimelineEntryDto(
            a.ActionType, FormatActionMessage(a.ActionType), a.TimestampUtc, a.EntityType, a.EntityId, "Action")));
        entries.AddRange(notifications.Select(n => new ActivityTimelineEntryDto(
            n.Type, n.Message, n.CreatedAt, n.RelatedEntityType, n.RelatedEntityId, "Notification")));

        return entries.OrderByDescending(e => e.TimestampUtc).ToList();
    }

    /// <summary>"JobCreated" -> "Job Created" — a readable fallback label since AuditLogEntry
    /// doesn't carry a pre-built human message the way Notification does.</summary>
    private static string FormatActionMessage(string actionType) =>
        Regex.Replace(actionType, "([a-z])([A-Z])", "$1 $2");
}
