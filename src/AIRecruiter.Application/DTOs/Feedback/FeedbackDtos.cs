using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Application.DTOs.Feedback;

public record SubmitFeedbackRequest(string Name, string Email, FeedbackCategory Category, string Message);

public record FeedbackDto(
    int Id,
    string Name,
    string Email,
    string Category,
    string Message,
    string Status,
    string? SubmittedByName,
    DateTime CreatedAt);

public record SetFeedbackStatusRequest(FeedbackStatus Status);
