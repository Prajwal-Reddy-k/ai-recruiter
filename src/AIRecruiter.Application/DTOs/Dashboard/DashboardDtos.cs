using AIRecruiter.Application.DTOs.Applications;
using AIRecruiter.Application.DTOs.Assessments;
using AIRecruiter.Application.DTOs.CareerGoals;
using AIRecruiter.Application.DTOs.Interviews;
using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.DTOs.Recruiters;

namespace AIRecruiter.Application.DTOs.Dashboard;

public record ApplicationStatusSummaryDto(int Applied, int UnderReview, int Shortlisted, int Rejected);

public record NextBestActionDto(string Label, string Description, string LinkPath);

public record CandidateDashboardDto(
    int ProfileCompletionPercent,
    ApplicationStatusSummaryDto ApplicationSummary,
    IReadOnlyList<JobApplicationDto> RecentApplications,
    IReadOnlyList<JobPostingDto> RecommendedJobs,
    IReadOnlyList<string> SkillSuggestions,
    IReadOnlyList<JobPostingDto> RecentlyViewedJobs,
    IReadOnlyList<JobPostingDto> SavedJobs,
    int ActiveAlertCount,
    IReadOnlyList<JobPostingDto> AlertMatches,
    IReadOnlyList<UpcomingInterviewDto> UpcomingInterviews,
    IReadOnlyList<NextBestActionDto> NextBestActions,
    IReadOnlyList<AssessmentAttemptHistoryItemDto> RecentAssessmentResults,
    CareerGoalsSummaryDto CareerGoalsSummary);

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
