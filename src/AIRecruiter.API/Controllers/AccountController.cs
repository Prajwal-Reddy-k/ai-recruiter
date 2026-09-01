using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Users;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

/// <summary>Self-service identity/account settings for any authenticated role — distinct from
/// RecruitersController (company onboarding) and CandidatesController (candidate-specific
/// profile fields).</summary>
[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IUserProfileService _userProfile;
    private readonly INotificationPreferenceService _notificationPreferences;

    public AccountController(IUserProfileService userProfile, INotificationPreferenceService notificationPreferences)
    {
        _userProfile = userProfile;
        _notificationPreferences = notificationPreferences;
    }

    [HttpGet("details")]
    public async Task<ActionResult<UserDetailsDto>> GetDetails(CancellationToken ct) =>
        Ok(await _userProfile.GetMyDetailsAsync(User.GetUserId(), ct));

    [HttpPut("details")]
    public async Task<ActionResult<UserDetailsDto>> UpdateDetails(UpdateUserDetailsRequest request, CancellationToken ct) =>
        Ok(await _userProfile.UpdateMyDetailsAsync(User.GetUserId(), request, ct));

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await _userProfile.ChangePasswordAsync(User.GetUserId(), request, ct);
        return NoContent();
    }

    [HttpGet("notification-preferences")]
    public async Task<ActionResult<NotificationPreferenceDto>> GetNotificationPreferences(CancellationToken ct) =>
        Ok(await _notificationPreferences.GetMyPreferencesAsync(User.GetUserId(), ct));

    [HttpPut("notification-preferences")]
    public async Task<ActionResult<NotificationPreferenceDto>> UpdateNotificationPreferences(UpdateNotificationPreferenceRequest request, CancellationToken ct) =>
        Ok(await _notificationPreferences.UpdateMyPreferencesAsync(User.GetUserId(), request, ct));

    [HttpPost("request-deletion")]
    public async Task<IActionResult> RequestDeletion(RequestAccountDeletionRequest request, CancellationToken ct)
    {
        await _userProfile.RequestAccountDeletionAsync(User.GetUserId(), request, ct);
        return NoContent();
    }
}
