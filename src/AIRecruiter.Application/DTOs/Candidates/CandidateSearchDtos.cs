using AIRecruiter.Application.DTOs.Assessments;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Application.DTOs.Candidates;

public enum CandidateSortOption
{
    NewestApplication = 1,
    HighestMatchScore = 2,
    ExperienceDesc = 3,
    NameAlphabetical = 4,
}

public record CandidateSearchQuery(
    string? Skills = null,
    string? City = null,
    string? State = null,
    int? MinExperienceYears = null,
    int? MaxExperienceYears = null,
    string? Education = null,
    ApplicationStatus? Status = null,
    int? MinMatchScore = null,
    int? MaxMatchScore = null,
    CandidateSortOption Sort = CandidateSortOption.NewestApplication,
    int Page = 1,
    int PageSize = 20);

public record CandidateSearchResultDto(
    int ApplicationId,
    int CandidateProfileId,
    string FullName,
    string? Headline,
    string? SkillsCsv,
    string? City,
    string? State,
    int? TotalExperienceYears,
    string? Education,
    string ApplicationStatus,
    int JobId,
    string JobTitle,
    DateTime AppliedAt,
    int? MatchScore,
    bool CanManage,
    string? AvatarUrl = null);

public record InterviewSummaryDto(int InterviewId, string Status, DateTime? NextSlotUtc);

public record CandidateApplicationSummaryDto(
    int ApplicationId,
    int JobId,
    string JobTitle,
    string Status,
    DateTime AppliedAt,
    int? MatchScore,
    bool HasResumeOnFile,
    bool CanManage,
    IReadOnlyList<InterviewSummaryDto> Interviews);

public record CandidateSearchDetailDto(
    int CandidateProfileId,
    string FullName,
    string? Headline,
    string? Summary,
    string? Education,
    string? ExperienceSummary,
    int? TotalExperienceYears,
    string DisplayLocation,
    string? SkillsCsv,
    IReadOnlyList<CandidateApplicationSummaryDto> Applications,
    string? AvatarUrl = null,
    IReadOnlyList<WorkExperienceDto>? WorkExperiences = null,
    IReadOnlyList<EducationEntryDto>? Educations = null,
    IReadOnlyList<CertificationDto>? Certifications = null,
    IReadOnlyList<ProjectDto>? Projects = null,
    string? AchievementsText = null,
    IReadOnlyList<RecruiterVisibleBadgeDto>? AssessmentBadges = null);

public record DiscoverCandidatesQuery(
    string? Skills = null,
    string? City = null,
    string? State = null,
    int? MinExperienceYears = null,
    AvailabilityStatus? AvailabilityStatus = null);

/// <summary>A candidate discoverable ahead of applying anywhere — only ever populated from
/// profiles with ProfileVisibility == VisibleToRecruiters. Deliberately excludes contact
/// details (phone, links) and resume access; those still require an actual application.</summary>
public record DiscoverableCandidateDto(
    int CandidateProfileId,
    string FullName,
    string? Headline,
    string? SkillsCsv,
    string DisplayLocation,
    int? TotalExperienceYears,
    string AvailabilityStatus,
    bool? RemotePreference,
    string? PreferredJobTypesCsv,
    string? AvatarUrl = null);
