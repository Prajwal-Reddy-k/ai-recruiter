using System.Text;
using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Reports;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/recruiters/reports")]
[Authorize(Roles = "Recruiter")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    [HttpGet]
    public async Task<ActionResult<RecruiterReportDto>> GetReport([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
    {
        return Ok(await _reports.GetReportAsync(User.GetUserId(), new ReportFilterRequest(fromUtc, toUtc), ct));
    }

    [HttpGet("export/jobs")]
    public async Task<IActionResult> ExportJobs([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
    {
        var csv = await _reports.ExportJobsCsvAsync(User.GetUserId(), new ReportFilterRequest(fromUtc, toUtc), ct);
        return CsvFile(csv, "jobs");
    }

    [HttpGet("export/applicants")]
    public async Task<IActionResult> ExportApplicants([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
    {
        var csv = await _reports.ExportApplicantsCsvAsync(User.GetUserId(), new ReportFilterRequest(fromUtc, toUtc), ct);
        return CsvFile(csv, "applicants");
    }

    [HttpGet("export/interviews")]
    public async Task<IActionResult> ExportInterviews([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
    {
        var csv = await _reports.ExportInterviewScheduleCsvAsync(User.GetUserId(), new ReportFilterRequest(fromUtc, toUtc), ct);
        return CsvFile(csv, "interview-schedule");
    }

    [HttpGet("export/funnel")]
    public async Task<IActionResult> ExportFunnel([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
    {
        var csv = await _reports.ExportFunnelSummaryCsvAsync(User.GetUserId(), new ReportFilterRequest(fromUtc, toUtc), ct);
        return CsvFile(csv, "hiring-funnel");
    }

    private IActionResult CsvFile(string csv, string name)
    {
        var bytes = Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"{name}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }
}
