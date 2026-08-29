using AIRecruiter.Application.DTOs.Jobs;

namespace AIRecruiter.Application.DTOs.Companies;

public record CompanyProfileDto(
    int Id,
    string Name,
    string? Website,
    string? Industry,
    string? Description,
    string? LogoUrl,
    string? City,
    string? State,
    string? Size,
    string? Benefits,
    string? CultureHighlights,
    string? LinkedInUrl,
    string? TwitterUrl,
    IReadOnlyList<JobPostingDto> OpenJobs);
