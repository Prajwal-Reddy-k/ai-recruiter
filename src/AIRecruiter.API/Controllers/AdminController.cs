using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Admin;
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
    private readonly IAuditLogService _auditLog;

    public AdminController(IAdminService adminService, IAuditLogService auditLog)
    {
        _adminService = adminService;
        _auditLog = auditLog;
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

    [HttpPost("reports/{id:int}/resolve")]
    public async Task<IActionResult> ResolveReport(int id, ResolveReportRequest request, CancellationToken ct)
    {
        await _adminService.ResolveReportAsync(User.GetUserId(), id, request, ct);
        return NoContent();
    }

    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog(CancellationToken ct) => Ok(await _auditLog.GetAllAsync(ct));
}
