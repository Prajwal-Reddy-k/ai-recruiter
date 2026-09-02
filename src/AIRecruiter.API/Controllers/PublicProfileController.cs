using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.PublicProfile;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
public class PublicProfileController : ControllerBase
{
    private readonly IPublicProfileService _publicProfile;

    public PublicProfileController(IPublicProfileService publicProfile)
    {
        _publicProfile = publicProfile;
    }

    [HttpGet("api/candidates/me/public-profile-preview")]
    [Authorize(Roles = "Candidate")]
    public async Task<ActionResult<PublicProfilePreviewDto>> GetMyPreview(CancellationToken ct) =>
        Ok(await _publicProfile.GetMyPreviewAsync(User.GetUserId(), ct));

    [HttpGet("api/talent/{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicCandidateProfileDto>> GetBySlug(string slug, CancellationToken ct) =>
        Ok(await _publicProfile.GetBySlugAsync(slug, ct));
}
