using AIRecruiter.Application.DTOs.JobTemplates;
using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Mapping;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Job templates are visible to every recruiter at the creating company (a shared
/// team resource, like a job posting itself), but only the creator or a company Owner can
/// edit/delete/duplicate one — the same owning-recruiter-or-Owner pattern used for jobs.
/// "Create a Draft job from a template" mirrors JobPostingService.CreateAsync/DuplicateAsync's
/// field-copy logic exactly.</summary>
public class JobTemplateService : IJobTemplateService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;

    public JobTemplateService(AppDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<JobTemplateDto>> GetMyTemplatesAsync(int recruiterUserId, string? search, CancellationToken ct = default)
    {
        var companyId = await GetCallerCompanyIdAsync(recruiterUserId, ct);

        var query = _db.JobTemplates
            .Include(t => t.RecruiterProfile).ThenInclude(r => r.User)
            .Where(t => t.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => t.Title.Contains(search) || (t.Department != null && t.Department.Contains(search)));
        }

        var templates = await query.OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt).ToListAsync(ct);
        return templates.Select(t => ToDto(t, recruiterUserId)).ToList();
    }

    public async Task<JobTemplateDto> GetByIdAsync(int recruiterUserId, int templateId, CancellationToken ct = default)
    {
        var template = await LoadForCompanyAsync(recruiterUserId, templateId, ct);
        return ToDto(template, recruiterUserId);
    }

    public async Task<JobTemplateDto> CreateAsync(int recruiterUserId, UpsertJobTemplateRequest request, CancellationToken ct = default)
    {
        var recruiterProfile = await GetCallerProfileAsync(recruiterUserId, ct);
        ValidateRequest(request);

        var template = new JobTemplate
        {
            CompanyId = recruiterProfile.CompanyId,
            RecruiterProfileId = recruiterProfile.Id,
        };
        ApplyRequest(template, request);

        _db.JobTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        template.RecruiterProfile = recruiterProfile;

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "JobTemplateCreated", "JobTemplate", template.Id, new { template.Title }, ct);

        return ToDto(template, recruiterUserId);
    }

    public async Task<JobTemplateDto> CreateFromJobAsync(int recruiterUserId, int jobId, string? title, CancellationToken ct = default)
    {
        var recruiterProfile = await GetCallerProfileAsync(recruiterUserId, ct);

        var job = await _db.JobPostings
            .FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new NotFoundException("Job posting not found.");

        // Any recruiter at the job's own company can templatize it — a job posting is
        // already a company-wide-visible resource, matching CandidateSearchService's
        // company-wide read convention.
        if (job.CompanyId != recruiterProfile.CompanyId)
        {
            throw new ForbiddenException("You do not have access to this job posting.");
        }

        var template = new JobTemplate
        {
            CompanyId = recruiterProfile.CompanyId,
            RecruiterProfileId = recruiterProfile.Id,
            Title = string.IsNullOrWhiteSpace(title) ? job.Title : title,
            Description = job.Description,
            RequiredSkillsCsv = job.RequiredSkillsCsv,
            MinExperienceYears = job.MinExperienceYears,
            MaxExperienceYears = job.MaxExperienceYears,
            EmploymentType = job.JobType,
            SalaryVisible = job.MinSalary.HasValue || job.MaxSalary.HasValue,
            MinSalary = job.MinSalary,
            MaxSalary = job.MaxSalary,
            DefaultCity = job.City,
            DefaultState = job.State,
            DefaultLocality = job.Locality,
            DefaultIsRemote = job.IsRemote,
        };

        _db.JobTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        template.RecruiterProfile = recruiterProfile;

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "JobTemplateCreatedFromJob", "JobTemplate", template.Id, new { SourceJobId = job.Id, template.Title }, ct);

        return ToDto(template, recruiterUserId);
    }

    public async Task<JobTemplateDto> UpdateAsync(int recruiterUserId, int templateId, UpsertJobTemplateRequest request, CancellationToken ct = default)
    {
        var template = await LoadForCompanyAsync(recruiterUserId, templateId, ct);
        await EnsureCanManageAsync(recruiterUserId, template, ct);
        ValidateRequest(request);

        ApplyRequest(template, request);
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "JobTemplateUpdated", "JobTemplate", template.Id, new { template.Title }, ct);

        return ToDto(template, recruiterUserId);
    }

    public async Task DeleteAsync(int recruiterUserId, int templateId, CancellationToken ct = default)
    {
        var template = await LoadForCompanyAsync(recruiterUserId, templateId, ct);
        await EnsureCanManageAsync(recruiterUserId, template, ct);

        _db.JobTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "JobTemplateDeleted", "JobTemplate", templateId, new { template.Title }, ct);
    }

    public async Task<JobTemplateDto> DuplicateAsync(int recruiterUserId, int templateId, CancellationToken ct = default)
    {
        var source = await LoadForCompanyAsync(recruiterUserId, templateId, ct);
        var recruiterProfile = await GetCallerProfileAsync(recruiterUserId, ct);

        var copy = new JobTemplate
        {
            CompanyId = source.CompanyId,
            RecruiterProfileId = recruiterProfile.Id,
            Title = $"{source.Title} (Copy)",
            Department = source.Department,
            Description = source.Description,
            Responsibilities = source.Responsibilities,
            RequiredSkillsCsv = source.RequiredSkillsCsv,
            PreferredSkillsCsv = source.PreferredSkillsCsv,
            MinExperienceYears = source.MinExperienceYears,
            MaxExperienceYears = source.MaxExperienceYears,
            EmploymentType = source.EmploymentType,
            SalaryVisible = source.SalaryVisible,
            MinSalary = source.MinSalary,
            MaxSalary = source.MaxSalary,
            DefaultCity = source.DefaultCity,
            DefaultState = source.DefaultState,
            DefaultLocality = source.DefaultLocality,
            DefaultIsRemote = source.DefaultIsRemote,
        };

        _db.JobTemplates.Add(copy);
        await _db.SaveChangesAsync(ct);
        copy.RecruiterProfile = recruiterProfile;

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "JobTemplateDuplicated", "JobTemplate", copy.Id, new { SourceTemplateId = source.Id, copy.Title }, ct);

        return ToDto(copy, recruiterUserId);
    }

    public async Task<JobPostingDto> CreateDraftJobFromTemplateAsync(int recruiterUserId, int templateId, CancellationToken ct = default)
    {
        var template = await LoadForCompanyAsync(recruiterUserId, templateId, ct);
        var recruiterProfile = await GetCallerProfileAsync(recruiterUserId, ct);

        var job = new JobPosting
        {
            Title = template.Title,
            Description = template.Description,
            RequiredSkillsCsv = template.RequiredSkillsCsv,
            MinExperienceYears = template.MinExperienceYears,
            MaxExperienceYears = template.MaxExperienceYears,
            MinSalary = template.SalaryVisible ? template.MinSalary : null,
            MaxSalary = template.SalaryVisible ? template.MaxSalary : null,
            City = template.DefaultIsRemote ? null : template.DefaultCity,
            State = template.DefaultIsRemote ? null : template.DefaultState,
            Locality = template.DefaultIsRemote ? null : template.DefaultLocality,
            IsRemote = template.DefaultIsRemote,
            JobType = template.EmploymentType,
            Status = JobStatus.Draft,
            PublishedAt = null,
            ModerationStatus = ModerationStatus.Approved,
            CompanyId = recruiterProfile.CompanyId,
            RecruiterProfileId = recruiterProfile.Id,
        };

        _db.JobPostings.Add(job);
        await _db.SaveChangesAsync(ct);
        job.Company = await _db.Companies.FirstAsync(c => c.Id == recruiterProfile.CompanyId, ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "JobTemplateUsedForJob", "JobPosting", job.Id, new { TemplateId = template.Id, job.Title }, ct);

        return JobPostingMapper.ToDto(job);
    }

    private async Task<RecruiterProfile> GetCallerProfileAsync(int recruiterUserId, CancellationToken ct)
    {
        return await _db.RecruiterProfiles.FirstOrDefaultAsync(r => r.UserId == recruiterUserId, ct)
            ?? throw new ConflictException("NOT_ONBOARDED", "Complete company onboarding first.");
    }

    private async Task<int> GetCallerCompanyIdAsync(int recruiterUserId, CancellationToken ct)
    {
        var companyId = await _db.RecruiterProfiles
            .Where(r => r.UserId == recruiterUserId)
            .Select(r => (int?)r.CompanyId)
            .FirstOrDefaultAsync(ct);

        return companyId ?? throw new ConflictException("NOT_ONBOARDED", "Complete company onboarding first.");
    }

    private async Task<JobTemplate> LoadForCompanyAsync(int recruiterUserId, int templateId, CancellationToken ct)
    {
        var companyId = await GetCallerCompanyIdAsync(recruiterUserId, ct);
        var template = await _db.JobTemplates
            .Include(t => t.RecruiterProfile).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(t => t.Id == templateId, ct)
            ?? throw new NotFoundException("Job template not found.");

        if (template.CompanyId != companyId)
        {
            throw new ForbiddenException("You do not have access to this job template.");
        }

        return template;
    }

    private async Task EnsureCanManageAsync(int recruiterUserId, JobTemplate template, CancellationToken ct)
    {
        if (!await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(_db, recruiterUserId, template.RecruiterProfile.UserId, template.CompanyId, ct))
        {
            throw new ForbiddenException("Only the creator or a company owner can manage this template.");
        }
    }

    private static void ValidateRequest(UpsertJobTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 150)
        {
            throw new ValidationException("Title is required and must be 150 characters or fewer.",
                new Dictionary<string, string> { ["title"] = "Title is required and must be 150 characters or fewer." });
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ValidationException("Description is required.",
                new Dictionary<string, string> { ["description"] = "Description is required." });
        }
    }

    private static void ApplyRequest(JobTemplate template, UpsertJobTemplateRequest request)
    {
        template.Title = request.Title.Trim();
        template.Department = request.Department?.Trim();
        template.Description = request.Description;
        template.Responsibilities = request.Responsibilities;
        template.RequiredSkillsCsv = request.RequiredSkillsCsv;
        template.PreferredSkillsCsv = request.PreferredSkillsCsv;
        template.MinExperienceYears = request.MinExperienceYears;
        template.MaxExperienceYears = request.MaxExperienceYears;
        template.EmploymentType = request.EmploymentType;
        template.SalaryVisible = request.SalaryVisible;
        template.MinSalary = request.MinSalary;
        template.MaxSalary = request.MaxSalary;
        template.DefaultCity = request.DefaultIsRemote ? null : request.DefaultCity;
        template.DefaultState = request.DefaultIsRemote ? null : request.DefaultState;
        template.DefaultLocality = request.DefaultIsRemote ? null : request.DefaultLocality;
        template.DefaultIsRemote = request.DefaultIsRemote;
    }

    private static JobTemplateDto ToDto(JobTemplate t, int callerUserId) => new(
        t.Id,
        t.Title,
        t.Department,
        t.Description,
        t.Responsibilities,
        t.RequiredSkillsCsv,
        t.PreferredSkillsCsv,
        t.MinExperienceYears,
        t.MaxExperienceYears,
        t.EmploymentType.ToString(),
        t.SalaryVisible,
        t.MinSalary,
        t.MaxSalary,
        t.DefaultCity,
        t.DefaultState,
        t.DefaultLocality,
        t.DefaultIsRemote,
        t.RecruiterProfile.User.FullName,
        t.RecruiterProfile.UserId == callerUserId,
        t.CreatedAt,
        t.UpdatedAt);
}
