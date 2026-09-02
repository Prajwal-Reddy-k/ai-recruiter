using AIRecruiter.Application.DTOs.Assessments;
using AIRecruiter.Application.DTOs.Candidates;

namespace AIRecruiter.Application.DTOs.PublicProfile;

/// <summary>The public, unauthenticated shape of a candidate's portfolio — deliberately a
/// narrow, curated subset. Never includes phone, resume access, salary fields, notice
/// period, applications, messages, or account settings.</summary>
public record PublicCandidateProfileDto(
    string FullName,
    string? Headline,
    string? AvatarUrl,
    string? SkillsCsv,
    string? Summary,
    string? ExperienceSummary,
    int? TotalExperienceYears,
    string? Education,
    IReadOnlyList<ProjectDto> Projects,
    string? LinkedInUrl,
    string? GithubUrl,
    string? PortfolioUrl,
    IReadOnlyList<RecruiterVisibleBadgeDto> AssessmentBadges);

public record PublicProfilePreviewDto(bool IsCurrentlyPublic, string? PublicUrl, PublicCandidateProfileDto Preview);
