using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Application.DTOs.Interviews;

public record ScheduleInterviewRequest(
    DateTime StartUtc,
    DateTime EndUtc,
    InterviewType Type,
    string? Location,
    string? RecruiterNote);

public record RescheduleInterviewRequest(
    DateTime StartUtc,
    DateTime EndUtc,
    InterviewType? Type,
    string? Location,
    string? RecruiterNote);

public record RespondInterviewRequest(string? ResponseNote);

public record InterviewDto(
    int Id,
    int JobApplicationId,
    int JobId,
    string JobTitle,
    int CompanyId,
    string CompanyName,
    int CandidateProfileId,
    string CandidateFullName,
    DateTime ScheduledStartUtc,
    DateTime ScheduledEndUtc,
    string Type,
    string? Location,
    string? RecruiterNote,
    string? CandidateResponseNote,
    string Status,
    bool CanManage,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record UpcomingInterviewDto(
    int InterviewId,
    int JobApplicationId,
    string JobTitle,
    string CompanyName,
    string CandidateFullName,
    DateTime StartUtc,
    DateTime EndUtc);
