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

public record JobReportDto(
    int Id,
    int JobPostingId,
    string JobTitle,
    string ReportedByName,
    string Reason,
    string Status,
    string? ResolutionNote,
    DateTime CreatedAt);

public record ModerateJobRequest(ModerationStatus ModerationStatus);
public record ResolveReportRequest(ReportStatus Status, string? ResolutionNote);
public record ReportJobRequest(string Reason);
