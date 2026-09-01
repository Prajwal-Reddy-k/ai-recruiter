using AIRecruiter.Application.DTOs.Platform;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Real, cheap counts for the public landing page — never fabricated demo numbers.</summary>
public class PlatformStatsService : IPlatformStatsService
{
    private readonly AppDbContext _db;

    public PlatformStatsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PlatformStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var openJobCount = await _db.JobPostings.CountAsync(j => j.Status == JobStatus.Open && j.ModerationStatus == ModerationStatus.Approved, ct);
        var candidateCount = await _db.Users.CountAsync(u => u.Role == UserRole.Candidate && u.IsActive, ct);
        var companyCount = await _db.Companies.CountAsync(ct);

        return new PlatformStatsDto(openJobCount, candidateCount, companyCount);
    }
}
