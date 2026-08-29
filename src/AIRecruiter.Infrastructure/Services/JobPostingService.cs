using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Mapping;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class JobPostingService : IJobPostingService
{
    private readonly AppDbContext _db;
    private readonly IndiaLocationValidator _locationValidator;
    private readonly IAuditLogService _auditLog;
    private readonly IViewDeduplicationService _viewDedup;

    /// <summary>Allowed job-status transitions, keyed by current status. Anything not listed
    /// here (including any transition out of Archived, which is terminal) is rejected.</summary>
    private static readonly Dictionary<JobStatus, JobStatus[]> AllowedTransitions = new()
    {
        [JobStatus.Draft] = new[] { JobStatus.Open, JobStatus.Archived },
        [JobStatus.Open] = new[] { JobStatus.Closed, JobStatus.Archived },
        [JobStatus.Closed] = new[] { JobStatus.Open, JobStatus.Archived },
        [JobStatus.Archived] = Array.Empty<JobStatus>(),
    };

    public JobPostingService(AppDbContext db, IndiaLocationValidator locationValidator, IAuditLogService auditLog, IViewDeduplicationService viewDedup)
    {
        _db = db;
        _locationValidator = locationValidator;
        _auditLog = auditLog;
        _viewDedup = viewDedup;
    }

    public async Task<IReadOnlyList<JobPostingDto>> GetOpenJobsAsync(string? search, CancellationToken ct = default)
    {
        var query = _db.JobPostings
            .Include(j => j.Company)
            .Where(j => j.Status == JobStatus.Open && j.ModerationStatus == ModerationStatus.Approved)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(j =>
                j.Title.Contains(search) ||
                (j.RequiredSkillsCsv != null && j.RequiredSkillsCsv.Contains(search)) ||
                (j.City != null && j.City.Contains(search)) ||
                (j.State != null && j.State.Contains(search)));
        }

        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => ToDto(j))
            .ToListAsync(ct);

        return jobs;
    }

    public async Task<JobPostingDto?> GetByIdAsync(int id, string? viewerKey, int? viewerUserId, CancellationToken ct = default)
    {
        var job = await _db.JobPostings
            .Include(j => j.Company)
            .Include(j => j.RecruiterProfile)
            .FirstOrDefaultAsync(j => j.Id == id, ct);

        if (job is null)
        {
            return null;
        }

        // Only count views on published jobs, never count the owning recruiter previewing
        // their own listing, and de-duplicate repeat hits from the same visitor within a
        // short window so a page refresh can't inflate the count.
        var isOwner = viewerUserId.HasValue && job.RecruiterProfile.UserId == viewerUserId.Value;
        if (job.Status == JobStatus.Open && !isOwner && (viewerKey is null || _viewDedup.ShouldCountView(viewerKey, job.Id)))
        {
            job.ViewCount++;
            await _db.SaveChangesAsync(ct);
        }

        return ToDto(job);
    }

    public async Task<JobPostingDto> CreateAsync(int recruiterUserId, CreateJobPostingRequest request, CancellationToken ct = default)
    {
        var recruiterProfile = await _db.RecruiterProfiles
            .Include(r => r.Company)
            .FirstOrDefaultAsync(r => r.UserId == recruiterUserId, ct);

        if (recruiterProfile is null)
        {
            throw new ConflictException("NOT_ONBOARDED", "Complete company onboarding before posting a job.");
        }

        // Drafts can be saved incomplete — only a publish-time (or already-published) job
        // enforces full location validation.
        if (!request.SaveAsDraft)
        {
            var (isValid, error) = _locationValidator.Validate(request.State, request.City, request.IsRemote);
            if (!isValid)
            {
                throw new ValidationException(error!);
            }
        }

        var status = request.SaveAsDraft ? JobStatus.Draft : JobStatus.Open;

        var job = new JobPosting
        {
            Title = request.Title,
            Description = request.Description,
            RequiredSkillsCsv = request.RequiredSkillsCsv,
            MinExperienceYears = request.MinExperienceYears,
            MaxExperienceYears = request.MaxExperienceYears,
            MinSalary = request.MinSalary,
            MaxSalary = request.MaxSalary,
            City = request.IsRemote ? null : request.City,
            State = request.IsRemote ? null : request.State,
            Locality = request.IsRemote ? null : request.Locality,
            IsRemote = request.IsRemote,
            JobType = request.JobType,
            Status = status,
            PublishedAt = status == JobStatus.Open ? DateTime.UtcNow : null,
            ModerationStatus = ModerationStatus.Approved,
            CompanyId = recruiterProfile.CompanyId,
            RecruiterProfileId = recruiterProfile.Id
        };

        _db.JobPostings.Add(job);
        await _db.SaveChangesAsync(ct);

        job.Company = recruiterProfile.Company;

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", status == JobStatus.Draft ? "JobDraftSaved" : "JobCreated", "JobPosting", job.Id, new { job.Title }, ct);

        return ToDto(job);
    }

    public async Task<JobPostingDto> UpdateAsync(int recruiterUserId, int jobId, UpdateJobPostingRequest request, CancellationToken ct = default)
    {
        var job = await _db.JobPostings
            .Include(j => j.Company)
            .Include(j => j.RecruiterProfile)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new NotFoundException("Job posting not found.");

        if (job.RecruiterProfile.UserId != recruiterUserId)
        {
            throw new ForbiddenException("You do not have access to this job posting.");
        }

        if (job.Status == JobStatus.Archived)
        {
            throw new ConflictException("JOB_ARCHIVED", "An archived job can no longer be edited.");
        }

        // A published (or previously-published) job must keep satisfying full validation;
        // a draft can still be saved with an incomplete location.
        if (job.Status != JobStatus.Draft)
        {
            var (isValid, error) = _locationValidator.Validate(request.State, request.City, request.IsRemote);
            if (!isValid)
            {
                throw new ValidationException(error!);
            }
        }

        job.Title = request.Title;
        job.Description = request.Description;
        job.RequiredSkillsCsv = request.RequiredSkillsCsv;
        job.MinExperienceYears = request.MinExperienceYears;
        job.MaxExperienceYears = request.MaxExperienceYears;
        job.MinSalary = request.MinSalary;
        job.MaxSalary = request.MaxSalary;
        job.City = request.IsRemote ? null : request.City;
        job.State = request.IsRemote ? null : request.State;
        job.Locality = request.IsRemote ? null : request.Locality;
        job.IsRemote = request.IsRemote;
        job.JobType = request.JobType;
        job.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "JobUpdated", "JobPosting", job.Id, new { job.Title }, ct);

        return ToDto(job);
    }

    public async Task<IReadOnlyList<RecruiterJobSummaryDto>> GetMyJobsAsync(int recruiterUserId, CancellationToken ct = default)
    {
        var jobs = await _db.JobPostings
            .Include(j => j.Company)
            .Include(j => j.RecruiterProfile)
            .Where(j => j.RecruiterProfile.UserId == recruiterUserId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

        var jobIds = jobs.Select(j => j.Id).ToList();
        var applicationCounts = await _db.JobApplications
            .Where(a => jobIds.Contains(a.JobPostingId))
            .GroupBy(a => a.JobPostingId)
            .Select(g => new { JobPostingId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.JobPostingId, x => x.Count, ct);

        return jobs
            .Select(j => new RecruiterJobSummaryDto(ToDto(j), applicationCounts.GetValueOrDefault(j.Id, 0)))
            .ToList();
    }

    public async Task<IReadOnlyList<JobPostingDto>> GetByCompanyAsync(int companyId, CancellationToken ct = default)
    {
        var jobs = await _db.JobPostings
            .Include(j => j.Company)
            .Where(j => j.CompanyId == companyId && j.Status == JobStatus.Open && j.ModerationStatus == ModerationStatus.Approved)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => ToDto(j))
            .ToListAsync(ct);

        return jobs;
    }

    public async Task<JobPostingDto> UpdateStatusAsync(int recruiterUserId, int jobId, UpdateJobStatusRequest request, CancellationToken ct = default)
    {
        var job = await _db.JobPostings
            .Include(j => j.Company)
            .Include(j => j.RecruiterProfile)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new NotFoundException("Job posting not found.");

        if (job.RecruiterProfile.UserId != recruiterUserId)
        {
            throw new ForbiddenException("You do not have access to this job posting.");
        }

        if (job.Status == request.Status)
        {
            return ToDto(job);
        }

        var allowedTargets = AllowedTransitions.GetValueOrDefault(job.Status, Array.Empty<JobStatus>());
        if (!allowedTargets.Contains(request.Status))
        {
            throw new ConflictException("INVALID_TRANSITION", $"A job cannot move from {job.Status} to {request.Status}.");
        }

        if (request.Status == JobStatus.Open)
        {
            // Publishing (from Draft or reopening from Closed) must satisfy full validation.
            var (isValid, error) = _locationValidator.Validate(job.State, job.City, job.IsRemote);
            if (!isValid)
            {
                throw new ValidationException($"Complete the job's location before publishing it. {error}");
            }
            job.PublishedAt ??= DateTime.UtcNow;
        }

        job.Status = request.Status;
        job.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "JobStatusChanged", "JobPosting", job.Id, new { job.Title, Status = job.Status.ToString() }, ct);

        return ToDto(job);
    }

    public async Task<JobPostingDto> DuplicateAsync(int recruiterUserId, int jobId, CancellationToken ct = default)
    {
        var source = await _db.JobPostings
            .Include(j => j.Company)
            .Include(j => j.RecruiterProfile)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new NotFoundException("Job posting not found.");

        if (source.RecruiterProfile.UserId != recruiterUserId)
        {
            throw new ForbiddenException("You do not have access to this job posting.");
        }

        var copy = new JobPosting
        {
            Title = $"{source.Title} (Copy)",
            Description = source.Description,
            RequiredSkillsCsv = source.RequiredSkillsCsv,
            MinExperienceYears = source.MinExperienceYears,
            MaxExperienceYears = source.MaxExperienceYears,
            MinSalary = source.MinSalary,
            MaxSalary = source.MaxSalary,
            City = source.City,
            State = source.State,
            Locality = source.Locality,
            IsRemote = source.IsRemote,
            JobType = source.JobType,
            Status = JobStatus.Draft,
            PublishedAt = null,
            ModerationStatus = ModerationStatus.Approved,
            CompanyId = source.CompanyId,
            RecruiterProfileId = source.RecruiterProfileId,
        };

        _db.JobPostings.Add(copy);
        await _db.SaveChangesAsync(ct);
        copy.Company = source.Company;

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "JobDuplicated", "JobPosting", copy.Id, new { SourceJobId = source.Id, copy.Title }, ct);

        return ToDto(copy);
    }

    public async Task ReportAsync(int userId, int jobId, string reason, CancellationToken ct = default)
    {
        var jobExists = await _db.JobPostings.AnyAsync(j => j.Id == jobId, ct);
        if (!jobExists)
        {
            throw new NotFoundException("Job posting not found.");
        }

        _db.JobReports.Add(new JobReport
        {
            JobPostingId = jobId,
            ReportedByUserId = userId,
            Reason = reason,
        });
        await _db.SaveChangesAsync(ct);
    }

    private static JobPostingDto ToDto(JobPosting j) => JobPostingMapper.ToDto(j);
}
