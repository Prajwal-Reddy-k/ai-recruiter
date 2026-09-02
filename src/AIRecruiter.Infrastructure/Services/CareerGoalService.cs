using AIRecruiter.Application.DTOs.CareerGoals;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class CareerGoalService : ICareerGoalService
{
    private const int MaxShortText = 150;
    private const int MaxNotes = 1000;
    private static readonly TimeSpan RecentAssessmentWindow = TimeSpan.FromDays(90);
    private static readonly TimeSpan RecentApplicationWindow = TimeSpan.FromDays(30);

    private readonly AppDbContext _db;
    private readonly IndiaLocationValidator _locationValidator;

    public CareerGoalService(AppDbContext db, IndiaLocationValidator locationValidator)
    {
        _db = db;
        _locationValidator = locationValidator;
    }

    public async Task<CareerGoalsSummaryDto> GetMyGoalsAsync(int userId, string? statusFilter, CancellationToken ct = default)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (profile is null) return new CareerGoalsSummaryDto(Array.Empty<CareerGoalDto>());

        var query = _db.CareerGoals.Where(g => g.CandidateProfileId == profile.Id);
        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<CareerGoalStatus>(statusFilter, ignoreCase: true, out var status))
        {
            query = query.Where(g => g.Status == status);
        }

        var goals = await query.OrderByDescending(g => g.CreatedAt).ToListAsync(ct);
        var signals = await GatherSignalsAsync(profile, ct);

        return new CareerGoalsSummaryDto(goals.Select(g => ToDto(g, signals)).ToList());
    }

    public async Task<CareerGoalDto> CreateAsync(int userId, UpsertCareerGoalRequest request, CancellationToken ct = default)
    {
        var status = Validate(request);
        var profile = await GetOrCreateProfileAsync(userId, ct);

        var goal = new CareerGoal
        {
            CandidateProfileId = profile.Id,
            TargetRole = request.TargetRole?.Trim(),
            TargetSkill = request.TargetSkill?.Trim(),
            TargetCompanyType = request.TargetCompanyType?.Trim(),
            PreferredState = request.PreferredState?.Trim(),
            PreferredCity = request.PreferredCity?.Trim(),
            IsLocationRemote = request.IsLocationRemote,
            TargetCompletionDate = request.TargetCompletionDate,
            ProgressPercent = request.ProgressPercent,
            Status = status,
            Notes = request.Notes?.Trim(),
        };
        _db.CareerGoals.Add(goal);
        await _db.SaveChangesAsync(ct);

        var signals = await GatherSignalsAsync(profile, ct);
        return ToDto(goal, signals);
    }

    public async Task<CareerGoalDto> UpdateAsync(int userId, int goalId, UpsertCareerGoalRequest request, CancellationToken ct = default)
    {
        var status = Validate(request);
        var goal = await GetOwnedAsync(userId, goalId, ct);

        goal.TargetRole = request.TargetRole?.Trim();
        goal.TargetSkill = request.TargetSkill?.Trim();
        goal.TargetCompanyType = request.TargetCompanyType?.Trim();
        goal.PreferredState = request.PreferredState?.Trim();
        goal.PreferredCity = request.PreferredCity?.Trim();
        goal.IsLocationRemote = request.IsLocationRemote;
        goal.TargetCompletionDate = request.TargetCompletionDate;
        goal.ProgressPercent = request.ProgressPercent;
        goal.Status = status;
        goal.Notes = request.Notes?.Trim();
        goal.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var profile = await _db.CandidateProfiles.FirstAsync(c => c.Id == goal.CandidateProfileId, ct);
        var signals = await GatherSignalsAsync(profile, ct);
        return ToDto(goal, signals);
    }

    public async Task DeleteAsync(int userId, int goalId, CancellationToken ct = default)
    {
        var goal = await GetOwnedAsync(userId, goalId, ct);
        _db.CareerGoals.Remove(goal);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<CandidateProfile> GetOrCreateProfileAsync(int userId, CancellationToken ct)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (profile is not null) return profile;

        profile = new CandidateProfile { UserId = userId };
        _db.CandidateProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);
        return profile;
    }

    /// <summary>Loads a goal and verifies it belongs to the caller's own candidate profile —
    /// never trusts the route id alone.</summary>
    private async Task<CareerGoal> GetOwnedAsync(int userId, int goalId, CancellationToken ct)
    {
        var goal = await _db.CareerGoals.FirstOrDefaultAsync(g => g.Id == goalId, ct)
            ?? throw new NotFoundException("Goal not found.");

        var ownerUserId = await _db.CandidateProfiles.Where(c => c.Id == goal.CandidateProfileId).Select(c => c.UserId).FirstOrDefaultAsync(ct);
        if (ownerUserId != userId)
        {
            throw new ForbiddenException("You do not have access to this goal.");
        }

        return goal;
    }

    private CareerGoalStatus Validate(UpsertCareerGoalRequest r)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(r.TargetRole) && string.IsNullOrWhiteSpace(r.TargetSkill) && string.IsNullOrWhiteSpace(r.TargetCompanyType))
        {
            errors["targetRole"] = "Set at least one of a target role, target skill, or target company type.";
        }
        if (r.TargetRole is { Length: > MaxShortText }) errors["targetRole"] = $"Target role must be {MaxShortText} characters or fewer.";
        if (r.TargetSkill is { Length: > MaxShortText }) errors["targetSkill"] = $"Target skill must be {MaxShortText} characters or fewer.";
        if (r.TargetCompanyType is { Length: > MaxShortText }) errors["targetCompanyType"] = $"Target company type must be {MaxShortText} characters or fewer.";
        if (r.Notes is { Length: > MaxNotes }) errors["notes"] = $"Notes must be {MaxNotes} characters or fewer.";
        if (r.ProgressPercent < 0 || r.ProgressPercent > 100) errors["progressPercent"] = "Progress must be between 0 and 100.";

        if (!Enum.TryParse<CareerGoalStatus>(r.Status, ignoreCase: true, out var status))
        {
            errors["status"] = "Status must be InProgress, Completed, or Paused.";
            status = CareerGoalStatus.InProgress;
        }

        var (locationValid, locationError) = _locationValidator.Validate(r.PreferredState, r.PreferredCity, isRemote: true);
        if (!locationValid)
        {
            errors["preferredCity"] = locationError!;
        }

        if (errors.Count > 0) throw new ValidationException("Please fix the highlighted fields.", errors);
        return status;
    }

    private record SignalContext(bool HasResumeFile, HashSet<string> Skills, HashSet<AssessmentCategory> RecentAssessmentCategories, bool HasAppliedRecently, bool HasPendingInterviewInvites);

    private async Task<SignalContext> GatherSignalsAsync(CandidateProfile profile, CancellationToken ct)
    {
        var skills = (profile.SkillsCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToLowerInvariant())
            .ToHashSet();

        var since = DateTime.UtcNow - RecentAssessmentWindow;
        var recentCategories = await _db.SkillAssessmentAttempts
            .Where(a => a.CandidateProfileId == profile.Id && a.Status == AssessmentAttemptStatus.Completed && a.SubmittedAt > since)
            .Select(a => a.Category)
            .ToListAsync(ct);

        var applicationSince = DateTime.UtcNow - RecentApplicationWindow;
        var hasAppliedRecently = await _db.JobApplications.AnyAsync(a => a.CandidateProfileId == profile.Id && a.CreatedAt > applicationSince, ct);

        var hasPendingInterviews = await _db.Interviews.AnyAsync(
            i => i.JobApplication.CandidateProfileId == profile.Id && i.Status == InterviewStatus.Proposed, ct);

        return new SignalContext(
            !string.IsNullOrEmpty(profile.ResumeStorageKey),
            skills,
            recentCategories.ToHashSet(),
            hasAppliedRecently,
            hasPendingInterviews);
    }

    private static CareerGoalDto ToDto(CareerGoal g, SignalContext signals)
    {
        var suggestionInput = new CareerGoalSuggestionInput(
            signals.HasResumeFile,
            !string.IsNullOrWhiteSpace(g.TargetSkill) && !signals.Skills.Contains(g.TargetSkill.ToLowerInvariant()),
            signals.RecentAssessmentCategories.Count > 0,
            signals.HasAppliedRecently,
            signals.HasPendingInterviewInvites);

        var suggestions = g.Status == CareerGoalStatus.Completed
            ? Array.Empty<CareerGoalSuggestionDto>()
            : CareerGoalSuggestionEngine.Suggest(g, suggestionInput);

        return new CareerGoalDto(
            g.Id, g.TargetRole, g.TargetSkill, g.TargetCompanyType, g.PreferredState, g.PreferredCity, g.IsLocationRemote,
            g.TargetCompletionDate, g.ProgressPercent, g.Status.ToString(), g.Notes, g.CreatedAt, g.UpdatedAt, suggestions);
    }
}
