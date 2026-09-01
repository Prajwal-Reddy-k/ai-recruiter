using AIRecruiter.Application.DTOs.Feedback;

namespace AIRecruiter.Application.Interfaces;

public interface IFeedbackService
{
    Task SubmitFeedbackAsync(int? submittedByUserId, string ipAddress, SubmitFeedbackRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<FeedbackDto>> GetAllAsync(CancellationToken ct = default);
    Task SetStatusAsync(int adminUserId, int feedbackId, SetFeedbackStatusRequest request, CancellationToken ct = default);
}
