using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Feedback;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

/// <summary>The public Help &amp; Support contact form — deliberately no [Authorize], since
/// guests can submit feedback too. Admin review lives on AdminController, matching the
/// existing Report-tab convention.</summary>
[ApiController]
[Route("api/feedback")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedback;

    public FeedbackController(IFeedbackService feedback)
    {
        _feedback = feedback;
    }

    [HttpPost]
    public async Task<IActionResult> Submit(SubmitFeedbackRequest request, CancellationToken ct)
    {
        int? userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        await _feedback.SubmitFeedbackAsync(userId, GetClientIp(), request, ct);
        return NoContent();
    }

    private string GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
