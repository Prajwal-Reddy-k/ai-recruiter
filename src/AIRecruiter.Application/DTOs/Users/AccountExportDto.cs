namespace AIRecruiter.Application.DTOs.Users;

/// <summary>Every field here is explicitly selected — nothing is ever passed through from an
/// entity wholesale — specifically so PasswordHash/SecurityStamp/reset-code fields/internal
/// storage keys can never accidentally end up in an export by inheriting a new entity field.</summary>
public record AccountExportAccountDto(int UserId, string FullName, string Email, string? PhoneNumber, string Role, DateTime CreatedAt);

public record AccountExportCandidateProfileDto(
    string? Headline, string? Summary, string? Education, int? GraduationYear, string? ExperienceSummary, int? TotalExperienceYears,
    string? City, string? State, string? Locality, string? SkillsCsv, string? LinkedInUrl, string? GithubUrl, string? PortfolioUrl,
    string? ResumeOriginalFileName, string AvailabilityStatus, string? PreferredJobTypesCsv, string? PreferredLocationsCsv,
    bool? RemotePreference, decimal? ExpectedSalaryMin, decimal? ExpectedSalaryMax, int? NoticePeriodDays, string ProfileVisibility);

public record AccountExportRecruiterProfileDto(string CompanyName, string? Designation, string CompanyRole);

public record AccountExportApplicationDto(string JobTitle, string CompanyName, string Status, DateTime CreatedAt);
public record AccountExportSavedJobDto(string JobTitle, string CompanyName, DateTime SavedAtUtc);
public record AccountExportSavedSearchDto(string? Name, string? Keyword, string? City, string? State, bool IsActive, DateTime CreatedAt);
public record AccountExportFollowedCompanyDto(string CompanyName, DateTime FollowedAtUtc);
public record AccountExportReviewDto(string CompanyName, int OverallRating, string Title, string Status, DateTime CreatedAt);

public record AccountExportDto(
    DateTime ExportedAtUtc,
    AccountExportAccountDto Account,
    AccountExportCandidateProfileDto? CandidateProfile,
    AccountExportRecruiterProfileDto? RecruiterProfile,
    IReadOnlyList<AccountExportApplicationDto> Applications,
    IReadOnlyList<AccountExportSavedJobDto> SavedJobs,
    IReadOnlyList<AccountExportSavedSearchDto> SavedSearches,
    IReadOnlyList<AccountExportFollowedCompanyDto> FollowedCompanies,
    IReadOnlyList<AccountExportReviewDto> CompanyReviews);
