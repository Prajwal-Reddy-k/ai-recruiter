using AIRecruiter.Application.DTOs.Admin;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Report submission for any of the four reportable entity types. Reports are
/// stored polymorphically (EntityType + EntityId, no hard FK — see Report.cs) so this one
/// service covers jobs, companies, messages, and users without four near-duplicate
/// services.</summary>
public class ModerationService : IModerationService
{
    private const int MaxDetailsLength = 1000;

    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;

    public ModerationService(AppDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task SubmitReportAsync(int reportedByUserId, SubmitReportRequest request, CancellationToken ct = default)
    {
        if (request.Details is not null && request.Details.Length > MaxDetailsLength)
        {
            throw new ValidationException($"Details must be {MaxDetailsLength} characters or fewer.",
                new Dictionary<string, string> { ["details"] = $"Details must be {MaxDetailsLength} characters or fewer." });
        }

        var exists = await EntityExistsAsync(request.EntityType, request.EntityId, ct);
        if (!exists)
        {
            throw new NotFoundException("The reported item could not be found.");
        }

        var report = new Report
        {
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            ReportedByUserId = reportedByUserId,
            Reason = request.Reason,
            Details = string.IsNullOrWhiteSpace(request.Details) ? null : request.Details.Trim(),
        };
        _db.Reports.Add(report);

        // A flagged review is pulled from public view immediately, pending Admin review —
        // same "hide first, review later" precedent as a moderated-out job posting.
        if (request.EntityType == ReportedEntityType.Review)
        {
            var review = await _db.CompanyReviews.FirstOrDefaultAsync(r => r.Id == request.EntityId, ct);
            if (review is not null && review.Status == ReviewStatus.Published)
            {
                review.Status = ReviewStatus.Flagged;
                review.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(reportedByUserId, null, "ReportSubmitted", request.EntityType.ToString(), request.EntityId,
            new { Reason = request.Reason.ToString() }, ct);
    }

    private Task<bool> EntityExistsAsync(ReportedEntityType type, int entityId, CancellationToken ct) => type switch
    {
        ReportedEntityType.Job => _db.JobPostings.AnyAsync(j => j.Id == entityId, ct),
        ReportedEntityType.Company => _db.Companies.AnyAsync(c => c.Id == entityId, ct),
        ReportedEntityType.Message => _db.Messages.AnyAsync(m => m.Id == entityId, ct),
        ReportedEntityType.User => _db.Users.AnyAsync(u => u.Id == entityId, ct),
        ReportedEntityType.Review => _db.CompanyReviews.AnyAsync(r => r.Id == entityId, ct),
        _ => Task.FromResult(false),
    };
}
