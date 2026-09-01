using AIRecruiter.Application.DTOs.Admin;

namespace AIRecruiter.Application.Interfaces;

/// <summary>Report submission — available to any authenticated user, distinct from
/// IAdminService (which is the Admin-only review/action side of moderation).</summary>
public interface IModerationService
{
    Task SubmitReportAsync(int reportedByUserId, SubmitReportRequest request, CancellationToken ct = default);
}
