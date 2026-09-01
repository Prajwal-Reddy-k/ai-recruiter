using AIRecruiter.Application.DTOs.Team;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Company-level hiring-team management — every action here requires the caller to
/// be a company Owner, enforced by EnsureCallerIsOwnerAsync at the top of every method.
/// "Manually created user association" per the spec: adding a member looks up an already
/// -registered Recruiter by email and attaches them to the company — no invite token/link,
/// no email dependency.</summary>
public class TeamService : ITeamService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;

    public TeamService(AppDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<TeamMemberDto>> GetMyCompanyTeamAsync(int ownerUserId, CancellationToken ct = default)
    {
        var owner = await EnsureCallerIsOwnerAsync(ownerUserId, ct);

        var members = await _db.RecruiterProfiles
            .Include(r => r.User)
            .Where(r => r.CompanyId == owner.CompanyId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);

        return members.Select(ToDto).ToList();
    }

    public async Task<TeamMemberDto> AddMemberAsync(int ownerUserId, AddTeamMemberRequest request, CancellationToken ct = default)
    {
        var owner = await EnsureCallerIsOwnerAsync(ownerUserId, ct);

        var normalizedEmail = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.RecruiterProfile)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is null || user.Role != UserRole.Recruiter)
        {
            throw new ValidationException(
                "No registered recruiter account was found for that email. Ask them to register as a Recruiter first, then add them here.",
                new Dictionary<string, string> { ["email"] = "No registered recruiter account found for this email." });
        }

        RecruiterProfile profile;
        if (user.RecruiterProfile is not null)
        {
            if (user.RecruiterProfile.CompanyId != owner.CompanyId)
            {
                throw new ConflictException("ALREADY_AT_ANOTHER_COMPANY", "This recruiter already belongs to a different company.");
            }
            profile = user.RecruiterProfile;
            profile.CompanyRole = request.Role;
            profile.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            profile = new RecruiterProfile
            {
                UserId = user.Id,
                CompanyId = owner.CompanyId,
                CompanyRole = request.Role,
            };
            _db.RecruiterProfiles.Add(profile);
        }

        await _db.SaveChangesAsync(ct);
        profile.User = user;

        await _auditLog.LogAsync(ownerUserId, "Recruiter", "TeamMemberAdded", "RecruiterProfile", profile.Id, new { user.Email, Role = request.Role.ToString() }, ct);

        return ToDto(profile);
    }

    public async Task<TeamMemberDto> UpdateRoleAsync(int ownerUserId, int recruiterProfileId, UpdateTeamMemberRoleRequest request, CancellationToken ct = default)
    {
        var owner = await EnsureCallerIsOwnerAsync(ownerUserId, ct);
        var member = await LoadCompanyMemberAsync(owner.CompanyId, recruiterProfileId, ct);

        if (member.CompanyRole == CompanyRole.Owner && request.Role != CompanyRole.Owner)
        {
            await EnsureNotLastOwnerAsync(owner.CompanyId, recruiterProfileId, ct);
        }

        member.CompanyRole = request.Role;
        member.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(ownerUserId, "Recruiter", "TeamMemberRoleChanged", "RecruiterProfile", member.Id, new { NewRole = request.Role.ToString() }, ct);

        return ToDto(member);
    }

    public async Task RemoveMemberAsync(int ownerUserId, int recruiterProfileId, CancellationToken ct = default)
    {
        var owner = await EnsureCallerIsOwnerAsync(ownerUserId, ct);
        var member = await LoadCompanyMemberAsync(owner.CompanyId, recruiterProfileId, ct);

        if (member.UserId == ownerUserId)
        {
            throw new ValidationException("You cannot remove yourself from the team.");
        }

        if (member.CompanyRole == CompanyRole.Owner)
        {
            await EnsureNotLastOwnerAsync(owner.CompanyId, recruiterProfileId, ct);
        }

        // No job posting can end up without an owning recruiter — a removed member's jobs
        // stay theirs on record (RecruiterProfile isn't deleted from history, only their
        // team membership), but future assignments referencing them are cleared.
        var jobAssignments = _db.JobAssignments.Where(a => a.RecruiterProfileId == recruiterProfileId);
        var interviewAssignments = _db.InterviewAssignments.Where(a => a.RecruiterProfileId == recruiterProfileId);
        _db.JobAssignments.RemoveRange(jobAssignments);
        _db.InterviewAssignments.RemoveRange(interviewAssignments);

        _db.RecruiterProfiles.Remove(member);
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(ownerUserId, "Recruiter", "TeamMemberRemoved", "RecruiterProfile", recruiterProfileId, null, ct);
    }

    public async Task<IReadOnlyList<JobAssignmentDto>> GetJobAssignmentsAsync(int ownerUserId, int jobId, CancellationToken ct = default)
    {
        var owner = await EnsureCallerIsOwnerAsync(ownerUserId, ct);
        await EnsureJobInCompanyAsync(owner.CompanyId, jobId, ct);

        var assignments = await _db.JobAssignments
            .Include(a => a.RecruiterProfile).ThenInclude(r => r.User)
            .Where(a => a.JobPostingId == jobId)
            .ToListAsync(ct);

        return assignments.Select(a => new JobAssignmentDto(a.JobPostingId, a.RecruiterProfileId, a.RecruiterProfile.User.FullName, a.RecruiterProfile.CompanyRole.ToString(), a.CreatedAt)).ToList();
    }

    public async Task AssignToJobAsync(int ownerUserId, int jobId, int recruiterProfileId, CancellationToken ct = default)
    {
        var owner = await EnsureCallerIsOwnerAsync(ownerUserId, ct);
        await EnsureJobInCompanyAsync(owner.CompanyId, jobId, ct);
        await LoadCompanyMemberAsync(owner.CompanyId, recruiterProfileId, ct);

        var exists = await _db.JobAssignments.AnyAsync(a => a.JobPostingId == jobId && a.RecruiterProfileId == recruiterProfileId, ct);
        if (exists) return;

        _db.JobAssignments.Add(new JobAssignment { JobPostingId = jobId, RecruiterProfileId = recruiterProfileId, AssignedByUserId = ownerUserId });
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(ownerUserId, "Recruiter", "JobAssigned", "JobPosting", jobId, new { RecruiterProfileId = recruiterProfileId }, ct);
    }

    public async Task UnassignFromJobAsync(int ownerUserId, int jobId, int recruiterProfileId, CancellationToken ct = default)
    {
        var owner = await EnsureCallerIsOwnerAsync(ownerUserId, ct);
        await EnsureJobInCompanyAsync(owner.CompanyId, jobId, ct);

        var assignment = await _db.JobAssignments.FirstOrDefaultAsync(a => a.JobPostingId == jobId && a.RecruiterProfileId == recruiterProfileId, ct);
        if (assignment is null) return;

        _db.JobAssignments.Remove(assignment);
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(ownerUserId, "Recruiter", "JobUnassigned", "JobPosting", jobId, new { RecruiterProfileId = recruiterProfileId }, ct);
    }

    public async Task<IReadOnlyList<InterviewAssignmentDto>> GetInterviewAssignmentsAsync(int ownerUserId, int interviewId, CancellationToken ct = default)
    {
        var owner = await EnsureCallerIsOwnerAsync(ownerUserId, ct);
        await EnsureInterviewInCompanyAsync(owner.CompanyId, interviewId, ct);

        var assignments = await _db.InterviewAssignments
            .Include(a => a.RecruiterProfile).ThenInclude(r => r.User)
            .Where(a => a.InterviewId == interviewId)
            .ToListAsync(ct);

        return assignments.Select(a => new InterviewAssignmentDto(a.InterviewId, a.RecruiterProfileId, a.RecruiterProfile.User.FullName, a.RecruiterProfile.CompanyRole.ToString(), a.CreatedAt)).ToList();
    }

    public async Task AssignToInterviewAsync(int ownerUserId, int interviewId, int recruiterProfileId, CancellationToken ct = default)
    {
        var owner = await EnsureCallerIsOwnerAsync(ownerUserId, ct);
        await EnsureInterviewInCompanyAsync(owner.CompanyId, interviewId, ct);
        await LoadCompanyMemberAsync(owner.CompanyId, recruiterProfileId, ct);

        var exists = await _db.InterviewAssignments.AnyAsync(a => a.InterviewId == interviewId && a.RecruiterProfileId == recruiterProfileId, ct);
        if (exists) return;

        _db.InterviewAssignments.Add(new InterviewAssignment { InterviewId = interviewId, RecruiterProfileId = recruiterProfileId, AssignedByUserId = ownerUserId });
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(ownerUserId, "Recruiter", "InterviewAssigned", "Interview", interviewId, new { RecruiterProfileId = recruiterProfileId }, ct);
    }

    public async Task UnassignFromInterviewAsync(int ownerUserId, int interviewId, int recruiterProfileId, CancellationToken ct = default)
    {
        var owner = await EnsureCallerIsOwnerAsync(ownerUserId, ct);
        await EnsureInterviewInCompanyAsync(owner.CompanyId, interviewId, ct);

        var assignment = await _db.InterviewAssignments.FirstOrDefaultAsync(a => a.InterviewId == interviewId && a.RecruiterProfileId == recruiterProfileId, ct);
        if (assignment is null) return;

        _db.InterviewAssignments.Remove(assignment);
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(ownerUserId, "Recruiter", "InterviewUnassigned", "Interview", interviewId, new { RecruiterProfileId = recruiterProfileId }, ct);
    }

    private async Task<RecruiterProfile> EnsureCallerIsOwnerAsync(int ownerUserId, CancellationToken ct)
    {
        var profile = await _db.RecruiterProfiles.FirstOrDefaultAsync(r => r.UserId == ownerUserId, ct)
            ?? throw new ConflictException("NOT_ONBOARDED", "Complete company onboarding first.");

        if (profile.CompanyRole != CompanyRole.Owner)
        {
            throw new ForbiddenException("Only a company owner can manage the hiring team.");
        }

        return profile;
    }

    private async Task<RecruiterProfile> LoadCompanyMemberAsync(int companyId, int recruiterProfileId, CancellationToken ct)
    {
        var member = await _db.RecruiterProfiles.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == recruiterProfileId, ct)
            ?? throw new NotFoundException("Team member not found.");

        if (member.CompanyId != companyId)
        {
            throw new ForbiddenException("This person is not on your team.");
        }

        return member;
    }

    private async Task EnsureNotLastOwnerAsync(int companyId, int excludeRecruiterProfileId, CancellationToken ct)
    {
        var otherOwners = await _db.RecruiterProfiles
            .CountAsync(r => r.CompanyId == companyId && r.CompanyRole == CompanyRole.Owner && r.Id != excludeRecruiterProfileId, ct);

        if (otherOwners == 0)
        {
            throw new ConflictException("LAST_OWNER", "A company must always have at least one Owner.");
        }
    }

    private async Task EnsureJobInCompanyAsync(int companyId, int jobId, CancellationToken ct)
    {
        var exists = await _db.JobPostings.AnyAsync(j => j.Id == jobId && j.CompanyId == companyId, ct);
        if (!exists)
        {
            throw new NotFoundException("Job posting not found.");
        }
    }

    private async Task EnsureInterviewInCompanyAsync(int companyId, int interviewId, CancellationToken ct)
    {
        var exists = await _db.Interviews.AnyAsync(i => i.Id == interviewId && i.JobApplication.JobPosting.CompanyId == companyId, ct);
        if (!exists)
        {
            throw new NotFoundException("Interview not found.");
        }
    }

    private static TeamMemberDto ToDto(RecruiterProfile r) => new(
        r.Id, r.UserId, r.User.FullName, r.User.Email, r.Designation, r.CompanyRole.ToString(), r.CreatedAt);
}
