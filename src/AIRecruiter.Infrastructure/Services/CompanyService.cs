using AIRecruiter.Application.DTOs.Companies;
using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class CompanyService : ICompanyService
{
    private readonly AppDbContext _db;
    private readonly IJobPostingService _jobPostingService;

    public CompanyService(AppDbContext db, IJobPostingService jobPostingService)
    {
        _db = db;
        _jobPostingService = jobPostingService;
    }

    public async Task<CompanyProfileDto> GetPublicProfileAsync(int companyId, CancellationToken ct = default)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, ct)
            ?? throw new NotFoundException("Company not found.");

        IReadOnlyList<JobPostingDto> openJobs = await _jobPostingService.GetByCompanyAsync(companyId, ct);

        return new CompanyProfileDto(
            company.Id,
            company.Name,
            company.Website,
            company.Industry,
            company.Description,
            company.LogoUrl,
            company.City,
            company.State,
            company.Size,
            company.Benefits,
            company.CultureHighlights,
            company.LinkedInUrl,
            company.TwitterUrl,
            openJobs);
    }
}
