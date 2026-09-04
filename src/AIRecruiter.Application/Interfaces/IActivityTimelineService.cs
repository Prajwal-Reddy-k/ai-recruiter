using AIRecruiter.Application.DTOs.Activity;

namespace AIRecruiter.Application.Interfaces;

public interface IActivityTimelineService
{
    /// <summary>Strictly scoped to the caller's own id — merges AuditLogEntry rows where the
    /// caller is the actor with Notification rows where the caller is the recipient. Never
    /// the broader team-wide scope AuditLogService.GetForCompanyAsync uses elsewhere.</summary>
    Task<IReadOnlyList<ActivityTimelineEntryDto>> GetMyTimelineAsync(
        int userId, string? typeFilter, DateTime? from, DateTime? to, CancellationToken ct = default);
}
