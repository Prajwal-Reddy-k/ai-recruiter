using AIRecruiter.Application.DTOs.Messaging;

namespace AIRecruiter.Application.Interfaces;

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(int userId, string role, int applicationId, string ipAddress, SendMessageRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<MessageDto>> GetThreadAsync(int userId, string role, int applicationId, CancellationToken ct = default);
    Task<IReadOnlyList<ConversationSummaryDto>> GetMyInboxAsync(int userId, string role, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(int userId, string role, CancellationToken ct = default);
    Task MarkThreadReadAsync(int userId, string role, int applicationId, CancellationToken ct = default);
}
