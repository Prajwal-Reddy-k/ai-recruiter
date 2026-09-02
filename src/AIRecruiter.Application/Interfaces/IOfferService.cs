using AIRecruiter.Application.DTOs.Offers;

namespace AIRecruiter.Application.Interfaces;

public interface IOfferService
{
    Task<OfferDto> CreateDraftAsync(int recruiterUserId, int jobApplicationId, CreateOfferRequest request, CancellationToken ct = default);
    Task<OfferDto> UpdateDraftAsync(int recruiterUserId, int offerId, UpdateOfferRequest request, CancellationToken ct = default);
    Task<OfferDto> SendAsync(int recruiterUserId, int offerId, CancellationToken ct = default);
    Task<OfferDto> WithdrawAsync(int recruiterUserId, int offerId, CancellationToken ct = default);
    Task<OfferDetailDto> GetDetailAsync(int userId, string role, int offerId, CancellationToken ct = default);
    Task<IReadOnlyList<OfferDto>> GetForApplicationAsync(int userId, string role, int jobApplicationId, CancellationToken ct = default);
    Task<IReadOnlyList<OfferDto>> GetMyOffersAsync(int candidateUserId, CancellationToken ct = default);
    Task<OfferDto> RespondAsync(int candidateUserId, int offerId, RespondToOfferRequest request, CancellationToken ct = default);
}
