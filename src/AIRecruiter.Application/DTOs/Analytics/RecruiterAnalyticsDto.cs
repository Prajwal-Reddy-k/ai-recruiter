namespace AIRecruiter.Application.DTOs.Analytics;

public record NamedCountDto(string Name, int Count);

public record JobViewsVsApplicationsDto(int JobId, string Title, int ViewCount, int ApplicationCount);

public record RecruiterAnalyticsDto(
    int ActiveJobs,
    int TotalApplications,
    int ShortlistedCandidates,
    int InterviewsScheduled,
    int OffersMade,
    IReadOnlyList<NamedCountDto> ApplicationsPerJob,
    IReadOnlyList<NamedCountDto> HiringFunnel,
    IReadOnlyList<NamedCountDto> ApplicationsByCity,
    IReadOnlyList<NamedCountDto> TopCandidateSkills,
    IReadOnlyList<JobViewsVsApplicationsDto> ViewsVsApplications);
