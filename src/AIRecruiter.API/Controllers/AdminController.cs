using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Admin;
using AIRecruiter.Application.DTOs.Companies;
using AIRecruiter.Application.DTOs.Feedback;
using AIRecruiter.Application.DTOs.Reviews;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IFeedbackService _feedback;
    private readonly IAuditLogService _auditLog;

    public AdminController(IAdminService adminService, IFeedbackService feedback, IAuditLogService auditLog)
    {
        _adminService = adminService;
        _feedback = feedback;
        _auditLog = auditLog;
    }

    [HttpGet("feedback")]
    public async Task<IActionResult> GetFeedback(CancellationToken ct) => Ok(await _feedback.GetAllAsync(ct));

    [HttpPost("feedback/{id:int}/status")]
    public async Task<IActionResult> SetFeedbackStatus(int id, SetFeedbackStatusRequest request, CancellationToken ct)
    {
        await _feedback.SetStatusAsync(User.GetUserId(), id, request, ct);
        return NoContent();
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken ct) => Ok(await _adminService.GetUsersAsync(ct));

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies(CancellationToken ct) => Ok(await _adminService.GetCompaniesAsync(ct));

    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs(CancellationToken ct) => Ok(await _adminService.GetJobsAsync(ct));

    [HttpGet("reports")]
    public async Task<IActionResult> GetReports(CancellationToken ct) => Ok(await _adminService.GetReportsAsync(ct));

    [HttpPost("jobs/{id:int}/moderate")]
    public async Task<IActionResult> ModerateJob(int id, ModerateJobRequest request, CancellationToken ct)
    {
        var job = await _adminService.ModerateJobAsync(User.GetUserId(), id, request, ct);
        return Ok(job);
    }

    [HttpPost("reports/{id:int}/status")]
    public async Task<IActionResult> SetReportStatus(int id, SetReportStatusRequest request, CancellationToken ct)
    {
        await _adminService.SetReportStatusAsync(User.GetUserId(), id, request, ct);
        return NoContent();
    }

    [HttpPost("reports/{id:int}/note")]
    public async Task<IActionResult> AddReportNote(int id, AddReportNoteRequest request, CancellationToken ct)
    {
        await _adminService.AddReportNoteAsync(User.GetUserId(), id, request, ct);
        return NoContent();
    }

    [HttpPost("users/{id:int}/suspend")]
    public async Task<IActionResult> SuspendUser(int id, CancellationToken ct)
    {
        await _adminService.SuspendUserAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    [HttpPost("users/{id:int}/reactivate")]
    public async Task<IActionResult> ReactivateUser(int id, CancellationToken ct)
    {
        await _adminService.ReactivateUserAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog(CancellationToken ct) => Ok(await _auditLog.GetAllAsync(ct));

    [HttpGet("companies/pending-verification")]
    public async Task<IActionResult> GetPendingCompanyVerifications(CancellationToken ct) => Ok(await _adminService.GetPendingCompanyVerificationsAsync(ct));

    [HttpPost("companies/{id:int}/verification")]
    public async Task<IActionResult> SetCompanyVerificationStatus(int id, SetCompanyVerificationStatusRequest request, CancellationToken ct)
    {
        await _adminService.SetCompanyVerificationStatusAsync(User.GetUserId(), id, request, ct);
        return NoContent();
    }

    [HttpGet("reviews/pending")]
    public async Task<IActionResult> GetPendingReviews(CancellationToken ct) => Ok(await _adminService.GetPendingReviewsAsync(ct));

    [HttpPost("reviews/{id:int}/status")]
    public async Task<IActionResult> SetReviewStatus(int id, SetReviewStatusRequest request, CancellationToken ct)
    {
        await _adminService.SetReviewStatusAsync(User.GetUserId(), id, request, ct);
        return NoContent();
    }

    [HttpGet("users/pending-deletion")]
    public async Task<IActionResult> GetPendingDeletionRequests(CancellationToken ct) => Ok(await _adminService.GetPendingDeletionRequestsAsync(ct));

    [HttpPost("users/{id:int}/cancel-deletion")]
    public async Task<IActionResult> AdminCancelDeletionRequest(int id, CancellationToken ct)
    {
        await _adminService.AdminCancelDeletionRequestAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
