using AIRecruiter.Application.DTOs.TalentPools;

namespace AIRecruiter.Application.Interfaces;

/// <summary>Deliberately has no method taking a candidate's own user id — candidates must
/// never see which talent pools they belong to.</summary>
public interface ITalentPoolService
{
    Task<TalentPoolDto> CreatePoolAsync(int recruiterUserId, CreateTalentPoolRequest request, CancellationToken ct = default);
    Task<TalentPoolDto> RenamePoolAsync(int recruiterUserId, int poolId, RenameTalentPoolRequest request, CancellationToken ct = default);
    Task DeletePoolAsync(int recruiterUserId, int poolId, CancellationToken ct = default);
    Task<IReadOnlyList<TalentPoolDto>> GetPoolsAsync(int recruiterUserId, CancellationToken ct = default);
    Task<IReadOnlyList<TalentPoolCandidateDto>> GetPoolCandidatesAsync(int recruiterUserId, int poolId, CancellationToken ct = default);
    Task AddCandidateAsync(int recruiterUserId, int poolId, AddCandidateToPoolRequest request, CancellationToken ct = default);
    Task RemoveCandidateAsync(int recruiterUserId, int poolId, int candidateProfileId, CancellationToken ct = default);
    Task UpdateCandidateNotesAsync(int recruiterUserId, int poolId, int candidateProfileId, UpdatePoolCandidateNotesRequest request, CancellationToken ct = default);
}
