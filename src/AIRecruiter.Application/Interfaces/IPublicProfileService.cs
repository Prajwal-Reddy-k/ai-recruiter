using AIRecruiter.Application.DTOs.PublicProfile;

namespace AIRecruiter.Application.Interfaces;

public interface IPublicProfileService
{
    Task<PublicProfilePreviewDto> GetMyPreviewAsync(int userId, CancellationToken ct = default);

    /// <summary>Anonymous lookup — throws NotFoundException both when the slug doesn't exist
    /// and when the profile's current visibility isn't PublicShareable, so a disabled link
    /// can't be distinguished from a link that never existed.</summary>
    Task<PublicCandidateProfileDto> GetBySlugAsync(string slug, CancellationToken ct = default);
}
