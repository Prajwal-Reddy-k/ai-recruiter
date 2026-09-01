namespace AIRecruiter.Application.DTOs.Messaging;

public record MessageDto(
    int Id,
    int JobApplicationId,
    int SenderUserId,
    string SenderRole,
    string SenderName,
    string Body,
    DateTime CreatedAt,
    bool IsRead);

public record SendMessageRequest(string Body);

public record ConversationSummaryDto(
    int JobApplicationId,
    int JobId,
    string JobTitle,
    string CompanyName,
    string CounterpartName,
    string? LastMessageBody,
    DateTime? LastMessageAt,
    int UnreadCount);
