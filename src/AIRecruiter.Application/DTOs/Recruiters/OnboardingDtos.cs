namespace AIRecruiter.Application.DTOs.Recruiters;

public record OnboardingStatusDto(
    bool IsOnboarded,
    int? CompanyId,
    string? CompanyName,
    string? Designation,
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
    string? TwitterUrl);

public record UpsertRecruiterOnboardingRequest(
    string CompanyName,
    string? Website,
    string? Industry,
    string? Description,
    string? Designation,
    string? LogoUrl,
    string? City,
    string? State,
    string? Size,
    string? Benefits,
    string? CultureHighlights,
    string? LinkedInUrl,
    string? TwitterUrl);
