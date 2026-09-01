using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Application.DTOs.InterviewFeedback;

public record InterviewFeedbackDto(
    int Id,
    int InterviewId,
    int RecruiterProfileId,
    string AuthorName,
    int TechnicalScore,
    int CommunicationScore,
    int ProblemSolvingScore,
    int CultureFitScore,
    string Recommendation,
    string? Strengths,
    string? Concerns,
    string? PrivateNotes,
    bool IsDraft,
    DateTime? SubmittedAtUtc,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool CanEdit,
    bool IsMine);

public record UpsertInterviewFeedbackRequest(
    int TechnicalScore,
    int CommunicationScore,
    int ProblemSolvingScore,
    int CultureFitScore,
    InterviewRecommendation Recommendation,
    string? Strengths,
    string? Concerns,
    string? PrivateNotes);

public record InterviewFeedbackSummaryDto(
    int InterviewId,
    IReadOnlyList<InterviewFeedbackDto> Scorecards,
    double? AverageTechnicalScore,
    double? AverageCommunicationScore,
    double? AverageProblemSolvingScore,
    double? AverageCultureFitScore);
