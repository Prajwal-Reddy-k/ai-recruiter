using AIRecruiter.Application.DTOs.Applications;
using AIRecruiter.Application.DTOs.Interviews;
using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.DTOs.Recruiters;

namespace AIRecruiter.Application.DTOs.Dashboard;

public record ApplicationStatusSummaryDto(int Applied, int UnderReview, int Shortlisted, int Rejected);

/// <summary>Wraps a list with an explicit flag so the UI can label sections that have no
/// real backing feature yet (recently-viewed jobs — there is no view-tracking in the
/// domain) as sample data rather than presenting it as real.</summary>
public record DemoJobsSectionDto(IReadOnlyList<JobPostingDto> Items, bool IsSampleData);

public record CandidateDashboardDto(
    int ProfileCompletionPercent,
    ApplicationStatusSummaryDto ApplicationSummary,
    IReadOnlyList<JobApplicationDto> RecentApplications,
    IReadOnlyList<JobPostingDto> RecommendedJobs,
    IReadOnlyList<string> SkillSuggestions,
    DemoJobsSectionDto RecentlyViewedJobs,
    IReadOnlyList<JobPostingDto> SavedJobs,
    int ActiveAlertCount,
    IReadOnlyList<JobPostingDto> AlertMatches,
    IReadOnlyList<UpcomingInterviewDto> UpcomingInterviews);

public record JobPerformanceDto(
    int JobId,
    string Title,
    string Status,
    int ApplicantCount,
    int ViewCount,
    DateTime CreatedAt);

public record RecruiterDashboardDto(
    OnboardingStatusDto OnboardingStatus,
    int ActiveJobPostingCount,
    int TotalApplicantCount,
    IReadOnlyList<JobApplicationDto> RecentApplications,
    IReadOnlyList<JobPerformanceDto> JobPerformance,
    IReadOnlyList<UpcomingInterviewDto> UpcomingInterviews);
