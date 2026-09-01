using AIRecruiter.Application.Common;
using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Domain.Entities;

namespace AIRecruiter.Infrastructure.Mapping;

public static class JobPostingMapper
{
    public static JobPostingDto ToDto(JobPosting j) => new(
        j.Id,
        j.Title,
        j.Description,
        j.RequiredSkillsCsv,
        j.MinExperienceYears,
        j.MaxExperienceYears,
        j.MinSalary,
        j.MaxSalary,
        j.City,
        j.State,
        j.Locality,
        j.IsRemote,
        IndiaLocationFormatter.Format(j.City, j.State, j.IsRemote),
        j.JobType.ToString(),
        j.Status.ToString(),
        j.CompanyId,
        j.Company.Name,
        j.Company.LogoUrl,
        j.CreatedAt,
        j.ViewCount,
        j.PublishedAt,
        j.ApplicationDeadlineUtc);
}
