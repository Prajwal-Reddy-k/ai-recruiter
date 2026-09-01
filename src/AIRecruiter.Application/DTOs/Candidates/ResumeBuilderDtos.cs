namespace AIRecruiter.Application.DTOs.Candidates;

public record WorkExperienceDto(int Id, string Title, string Company, string? Location, DateTime StartDate, DateTime? EndDate, string? Description, int DisplayOrder);
public record UpsertWorkExperienceRequest(string Title, string Company, string? Location, DateTime StartDate, DateTime? EndDate, string? Description);

public record EducationEntryDto(int Id, string Institution, string Degree, string? FieldOfStudy, DateTime? StartDate, DateTime? EndDate, string? GradeOrGpa, string? Description, int DisplayOrder);
public record UpsertEducationEntryRequest(string Institution, string Degree, string? FieldOfStudy, DateTime? StartDate, DateTime? EndDate, string? GradeOrGpa, string? Description);

public record CertificationDto(int Id, string Name, string? IssuingOrganization, DateTime? IssueDate, DateTime? ExpiryDate, string? CredentialUrl, int DisplayOrder);
public record UpsertCertificationRequest(string Name, string? IssuingOrganization, DateTime? IssueDate, DateTime? ExpiryDate, string? CredentialUrl);

public record ProjectDto(int Id, string Title, string? Description, string? ProjectUrl, string? TechnologiesCsv, int DisplayOrder);
public record UpsertProjectRequest(string Title, string? Description, string? ProjectUrl, string? TechnologiesCsv);

public record ReorderRequest(IReadOnlyList<int> OrderedIds);

public record UpsertResumeSummaryRequest(string? Summary, string? SkillsCsv, string? LinkedInUrl, string? GithubUrl, string? PortfolioUrl, string? AchievementsText);

public record ResumeDto(
    string FullName,
    string? Headline,
    string? Summary,
    string? SkillsCsv,
    string? LinkedInUrl,
    string? GithubUrl,
    string? PortfolioUrl,
    string? AchievementsText,
    IReadOnlyList<WorkExperienceDto> WorkExperiences,
    IReadOnlyList<EducationEntryDto> Educations,
    IReadOnlyList<CertificationDto> Certifications,
    IReadOnlyList<ProjectDto> Projects,
    ProfileStrengthResult Strength);
