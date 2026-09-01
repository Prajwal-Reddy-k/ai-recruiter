using AIRecruiter.Application.DTOs.Analytics;

namespace AIRecruiter.Application.DTOs.Reports;

public record ReportFilterRequest(DateTime? FromUtc, DateTime? ToUtc);

public record RecruiterReportDto(
    int JobsCreated,
    int JobsPublished,
    int JobsClosed,
    int TotalApplications,
    IReadOnlyList<NamedCountDto> ApplicationsByJob,
    IReadOnlyList<NamedCountDto> ApplicationsByCity,
    IReadOnlyList<NamedCountDto> ApplicationsByState,
    IReadOnlyList<NamedCountDto> StatusFunnel,
    int InterviewsScheduledCount,
    int InterviewsCompletedCount,
    double? InterviewConversionRatePercent,
    double? AverageDaysToInterview,
    IReadOnlyList<NamedCountDto> TopCandidateSkills,
    int OffersMade,
    int Hires);
