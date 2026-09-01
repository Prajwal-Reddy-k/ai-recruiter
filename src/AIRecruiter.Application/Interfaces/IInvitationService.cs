using AIRecruiter.Application.DTOs.Invitations;

namespace AIRecruiter.Application.Interfaces;

public interface IInvitationService
{
    Task<InvitationDto> InviteAsync(int recruiterUserId, string ipAddress, InviteCandidateRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<InvitationDto>> GetMyInvitationsAsync(int candidateUserId, CancellationToken ct = default);
    Task<IReadOnlyList<InvitationDto>> GetSentInvitationsAsync(int recruiterUserId, int? jobPostingId, CancellationToken ct = default);
    Task<InvitationDto> MarkViewedAsync(int candidateUserId, int invitationId, CancellationToken ct = default);
    Task<InvitationDto> RespondAsync(int candidateUserId, int invitationId, bool accept, CancellationToken ct = default);
    Task DismissAsync(int candidateUserId, int invitationId, CancellationToken ct = default);
}
