using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Application.DTOs.Admin;

public record AdminUserDto(int Id, string FullName, string Email, string Role, bool IsActive, DateTime CreatedAt);

public record AdminCompanyDto(int Id, string Name, string? Industry, int JobCount, int RecruiterCount, DateTime CreatedAt);

public record AdminJobDto(
    int Id,
    string Title,
    string CompanyName,
    string Status,
    string ModerationStatus,
    int ApplicationCount,
    DateTime CreatedAt);

public record ReportDto(
    int Id,
    string EntityType,
    int EntityId,
    string? EntityLabel,
    string ReportedByName,
    string Reason,
    string? Details,
    string Status,
    string? ModerationNote,
    string? ReviewedByName,
    DateTime? ReviewedAt,
    DateTime CreatedAt);

public record ModerateJobRequest(ModerationStatus ModerationStatus);
public record SetReportStatusRequest(ReportStatus Status, string? Note);
public record AddReportNoteRequest(string Note);
public record SubmitReportRequest(ReportedEntityType EntityType, int EntityId, ReportReason Reason, string? Details);

/// <summary>Convenience shape for the existing job-scoped report endpoint
/// (POST /api/jobs/{id}/report) — EntityType/EntityId are implied by the route.</summary>
public record ReportJobRequest(ReportReason Reason, string? Details);
