namespace AIRecruiter.Application.DTOs.Interviews;

public record InterviewSlotDto(int Id, DateTime StartUtc, DateTime EndUtc, bool IsSelected);

public record InterviewDto(
    int Id,
    int JobApplicationId,
    string JobTitle,
    string CompanyName,
    string CandidateFullName,
    string Status,
    string? DeclineNote,
    DateTime CreatedAt,
    IReadOnlyList<InterviewSlotDto> Slots);

public record UpcomingInterviewDto(
    int InterviewId,
    int JobApplicationId,
    string JobTitle,
    string CompanyName,
    string CandidateFullName,
    DateTime StartUtc,
    DateTime EndUtc);

public record ProposeInterviewSlotRequest(DateTime StartUtc, DateTime EndUtc);
public record ProposeInterviewRequest(IReadOnlyList<ProposeInterviewSlotRequest> Slots);
public record RespondInterviewRequest(int? AcceptedSlotId, string? DeclineNote);
