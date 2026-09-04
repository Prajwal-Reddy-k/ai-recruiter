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
    private readonly IAccountDataExportService _dataExport;
    private readonly IIpRateLimiter _rateLimiter;

    public AccountController(IUserProfileService userProfile, INotificationPreferenceService notificationPreferences, IAccountDataExportService dataExport, IIpRateLimiter rateLimiter)
    {
        _userProfile = userProfile;
        _notificationPreferences = notificationPreferences;
        _dataExport = dataExport;
        _rateLimiter = rateLimiter;
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
    public async Task<ActionResult<AccountDeletionStatusDto>> RequestDeletion(RequestAccountDeletionRequest request, CancellationToken ct) =>
        Ok(await _userProfile.RequestAccountDeletionAsync(User.GetUserId(), request, ct));

    [HttpPost("cancel-deletion")]
    public async Task<ActionResult<AccountDeletionStatusDto>> CancelDeletion(CancellationToken ct) =>
        Ok(await _userProfile.CancelAccountDeletionAsync(User.GetUserId(), ct));

    [HttpGet("privacy")]
    public async Task<ActionResult<PrivacySummaryDto>> GetPrivacy(CancellationToken ct) =>
        Ok(await _userProfile.GetPrivacySummaryAsync(User.GetUserId(), ct));

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string format = "json", CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (!_rateLimiter.IsAllowed($"AccountExport:{userId}", 3, TimeSpan.FromHours(1)))
        {
            return StatusCode(429, new { message = "You've requested too many exports recently — please try again later." });
        }

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = await _dataExport.GetMyDataExportAsCsvAsync(userId, ct);
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "my-account-data.csv");
        }

        var export = await _dataExport.GetMyDataExportAsync(userId, ct);
        return Ok(export);
    }
}
