using System.Security.Cryptography;
using AIRecruiter.Application.DTOs.Assessments;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Locally seeded, deterministic skill assessments — not an official certification,
/// no external proctoring. Anti-cheat is limited to what's reasonable for a portfolio
/// project: random question order per attempt, and only one active attempt at a time.</summary>
public class SkillAssessmentService : ISkillAssessmentService
{
    private const int QuestionsPerAttempt = 15;
    private const int TimeLimitMinutes = 20;
    private const int CooldownHours = 24;

    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;

    public SkillAssessmentService(AppDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<AssessmentCategorySummaryDto>> GetCategoriesAsync(int userId, CancellationToken ct = default)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId, ct);

        var bankSizes = await _db.SkillAssessmentQuestions
            .GroupBy(q => q.Category)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, ct);

        var summaries = new List<AssessmentCategorySummaryDto>();

        if (profile is null)
        {
            foreach (var category in Enum.GetValues<AssessmentCategory>())
            {
                summaries.Add(new AssessmentCategorySummaryDto(category.ToString(), bankSizes.GetValueOrDefault(category), null, null, true, null, false));
            }
            return summaries;
        }

        await SweepExpiredAttemptsAsync(profile.Id, ct);

        var attempts = await _db.SkillAssessmentAttempts.Where(a => a.CandidateProfileId == profile.Id).ToListAsync(ct);
        var hasActiveAttempt = attempts.Any(a => a.Status == AssessmentAttemptStatus.InProgress);

        foreach (var category in Enum.GetValues<AssessmentCategory>())
        {
            var completedInCategory = attempts.Where(a => a.Category == category && a.Status == AssessmentAttemptStatus.Completed).ToList();
            var best = completedInCategory.Count > 0 ? completedInCategory.Max(a => a.PercentageScore) : null;
            var last = completedInCategory.Count > 0 ? completedInCategory.Max(a => a.SubmittedAt) : null;

            DateTime? cooldownEndsAt = null;
            var mostRecent = completedInCategory.OrderByDescending(a => a.SubmittedAt).FirstOrDefault();
            if (mostRecent?.SubmittedAt is { } submittedAt)
            {
                var end = submittedAt.AddHours(CooldownHours);
                if (end > DateTime.UtcNow) cooldownEndsAt = end;
            }

            var canAttemptNow = !hasActiveAttempt && cooldownEndsAt is null && bankSizes.GetValueOrDefault(category) > 0;

            summaries.Add(new AssessmentCategorySummaryDto(
                category.ToString(), bankSizes.GetValueOrDefault(category), best.HasValue ? (int)Math.Round(best.Value) : null, last, canAttemptNow, cooldownEndsAt, hasActiveAttempt));
        }

        return summaries;
    }

    public async Task<AssessmentAttemptInProgressDto> StartAttemptAsync(int userId, StartAssessmentAttemptRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<AssessmentCategory>(request.Category, ignoreCase: true, out var category))
        {
            throw new ValidationException("Please fix the highlighted fields.", new Dictionary<string, string> { ["category"] = "Unknown assessment category." });
        }

        var profile = await GetOrCreateProfileAsync(userId, ct);
        await SweepExpiredAttemptsAsync(profile.Id, ct);

        var hasActiveAttempt = await _db.SkillAssessmentAttempts.AnyAsync(
            a => a.CandidateProfileId == profile.Id && a.Status == AssessmentAttemptStatus.InProgress, ct);
        if (hasActiveAttempt)
        {
            throw new ConflictException("ATTEMPT_ALREADY_ACTIVE", "You already have an assessment in progress. Finish or let it expire before starting another.");
        }

        var since = DateTime.UtcNow.AddHours(-CooldownHours);
        var recentCompleted = await _db.SkillAssessmentAttempts
            .Where(a => a.CandidateProfileId == profile.Id && a.Category == category && a.Status == AssessmentAttemptStatus.Completed)
            .OrderByDescending(a => a.SubmittedAt)
            .FirstOrDefaultAsync(ct);
        if (recentCompleted?.SubmittedAt > since)
        {
            throw new ConflictException("COOLDOWN_ACTIVE", $"You can retake this assessment after {recentCompleted.SubmittedAt.Value.AddHours(CooldownHours):u}.");
        }

        var pool = await _db.SkillAssessmentQuestions.Where(q => q.Category == category).Select(q => q.Id).ToListAsync(ct);
        if (pool.Count == 0)
        {
            throw new NotFoundException("No questions are available for this category yet.");
        }

        var shuffled = ShuffleSecure(pool);
        var chosenIds = shuffled.Take(Math.Min(QuestionsPerAttempt, shuffled.Count)).ToList();

        var now = DateTime.UtcNow;
        var attempt = new SkillAssessmentAttempt
        {
            CandidateProfileId = profile.Id,
            Category = category,
            Status = AssessmentAttemptStatus.InProgress,
            StartedAt = now,
            ExpiresAt = now.AddMinutes(TimeLimitMinutes),
        };
        _db.SkillAssessmentAttempts.Add(attempt);
        await _db.SaveChangesAsync(ct);

        for (var i = 0; i < chosenIds.Count; i++)
        {
            _db.SkillAssessmentAnswers.Add(new SkillAssessmentAnswer
            {
                SkillAssessmentAttemptId = attempt.Id,
                SkillAssessmentQuestionId = chosenIds[i],
                DisplayOrder = i,
            });
        }
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(userId, "Candidate", "AssessmentAttemptStarted", "SkillAssessmentAttempt", attempt.Id, new { Category = category.ToString() }, ct);

        return await GetActiveAttemptAsync(userId, attempt.Id, ct);
    }

    public async Task<AssessmentAttemptInProgressDto> GetActiveAttemptAsync(int userId, int attemptId, CancellationToken ct = default)
    {
        var attempt = await GetOwnedAttemptAsync(userId, attemptId, ct);
        if (attempt.Status != AssessmentAttemptStatus.InProgress)
        {
            throw new ConflictException("ATTEMPT_NOT_ACTIVE", "This attempt is no longer active.");
        }

        var answers = await _db.SkillAssessmentAnswers
            .Where(a => a.SkillAssessmentAttemptId == attemptId)
            .Include(a => a.Question)
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync(ct);

        var questions = answers.Select(a => new AssessmentQuestionForAttemptDto(
            a.Id, a.DisplayOrder, a.Question.QuestionText, a.Question.OptionA, a.Question.OptionB, a.Question.OptionC, a.Question.OptionD)).ToList();

        var selected = answers.Where(a => a.SelectedOptionIndex.HasValue).ToDictionary(a => a.Id, a => a.SelectedOptionIndex!.Value);

        return new AssessmentAttemptInProgressDto(attempt.Id, attempt.Category.ToString(), attempt.StartedAt, attempt.ExpiresAt, questions, selected);
    }

    public async Task AnswerQuestionAsync(int userId, int attemptId, SubmitAssessmentAnswerRequest request, CancellationToken ct = default)
    {
        var attempt = await GetOwnedAttemptAsync(userId, attemptId, ct);
        if (attempt.Status != AssessmentAttemptStatus.InProgress)
        {
            throw new ConflictException("ATTEMPT_NOT_ACTIVE", "This attempt is no longer active.");
        }
        if (DateTime.UtcNow > attempt.ExpiresAt)
        {
            throw new ConflictException("ATTEMPT_EXPIRED", "Time's up for this attempt — submit to see your score.");
        }
        if (request.SelectedOptionIndex is < 0 or > 3)
        {
            throw new ValidationException("Please fix the highlighted fields.", new Dictionary<string, string> { ["selectedOptionIndex"] = "Choose one of the four options." });
        }

        var answer = await _db.SkillAssessmentAnswers.FirstOrDefaultAsync(a => a.Id == request.AnswerId && a.SkillAssessmentAttemptId == attemptId, ct)
            ?? throw new NotFoundException("Question not found in this attempt.");

        answer.SelectedOptionIndex = request.SelectedOptionIndex;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AssessmentAttemptResultDto> SubmitAttemptAsync(int userId, int attemptId, CancellationToken ct = default)
    {
        var attempt = await GetOwnedAttemptAsync(userId, attemptId, ct);
        if (attempt.Status != AssessmentAttemptStatus.InProgress)
        {
            throw new ConflictException("ATTEMPT_NOT_ACTIVE", "This attempt has already been submitted or is no longer active.");
        }

        var answers = await _db.SkillAssessmentAnswers
            .Where(a => a.SkillAssessmentAttemptId == attemptId)
            .Include(a => a.Question)
            .ToListAsync(ct);

        var correct = answers.Count(a => a.SelectedOptionIndex == a.Question.CorrectOptionIndex);
        attempt.ScoreCorrectCount = correct;
        attempt.TotalQuestionCount = answers.Count;
        attempt.PercentageScore = answers.Count > 0 ? Math.Round(correct / (decimal)answers.Count * 100, 1) : 0;
        attempt.Status = AssessmentAttemptStatus.Completed;
        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.IsVisibleToRecruiters = false;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(userId, "Candidate", "AssessmentAttemptSubmitted", "SkillAssessmentAttempt", attempt.Id, new { Category = attempt.Category.ToString(), attempt.PercentageScore }, ct);

        return ToResultDto(attempt, answers);
    }

    public async Task<IReadOnlyList<AssessmentAttemptHistoryItemDto>> GetMyHistoryAsync(int userId, CancellationToken ct = default)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (profile is null) return Array.Empty<AssessmentAttemptHistoryItemDto>();

        return await _db.SkillAssessmentAttempts
            .Where(a => a.CandidateProfileId == profile.Id && a.Status == AssessmentAttemptStatus.Completed)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new AssessmentAttemptHistoryItemDto(
                a.Id, a.Category.ToString(), a.ScoreCorrectCount!.Value, a.TotalQuestionCount!.Value, a.PercentageScore!.Value, a.SubmittedAt!.Value, a.IsVisibleToRecruiters))
            .ToListAsync(ct);
    }

    public async Task<AssessmentAttemptResultDto> GetAttemptReviewAsync(int userId, int attemptId, CancellationToken ct = default)
    {
        var attempt = await GetOwnedAttemptAsync(userId, attemptId, ct);
        if (attempt.Status != AssessmentAttemptStatus.Completed)
        {
            throw new ConflictException("ATTEMPT_NOT_COMPLETED", "This attempt hasn't been submitted yet.");
        }

        var answers = await _db.SkillAssessmentAnswers
            .Where(a => a.SkillAssessmentAttemptId == attemptId)
            .Include(a => a.Question)
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync(ct);

        return ToResultDto(attempt, answers);
    }

    public async Task SetAttemptVisibilityAsync(int userId, int attemptId, SetAttemptVisibilityRequest request, CancellationToken ct = default)
    {
        var attempt = await GetOwnedAttemptAsync(userId, attemptId, ct);
        if (attempt.Status != AssessmentAttemptStatus.Completed)
        {
            throw new ConflictException("ATTEMPT_NOT_COMPLETED", "Only a completed attempt's visibility can be changed.");
        }

        attempt.IsVisibleToRecruiters = request.IsVisibleToRecruiters;
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

    /// <summary>Loads an attempt and verifies it belongs to the caller's own candidate
    /// profile — never trusts the route id alone.</summary>
    private async Task<SkillAssessmentAttempt> GetOwnedAttemptAsync(int userId, int attemptId, CancellationToken ct)
    {
        var attempt = await _db.SkillAssessmentAttempts.FirstOrDefaultAsync(a => a.Id == attemptId, ct)
            ?? throw new NotFoundException("Attempt not found.");

        var ownerUserId = await _db.CandidateProfiles.Where(c => c.Id == attempt.CandidateProfileId).Select(c => c.UserId).FirstOrDefaultAsync(ct);
        if (ownerUserId != userId)
        {
            throw new ForbiddenException("You do not have access to this attempt.");
        }

        return attempt;
    }

    /// <summary>Marks any InProgress attempt past its time limit as Abandoned, so it stops
    /// blocking the one-active-attempt-at-a-time guard. Done lazily here rather than via a
    /// background sweep — cheap and sufficient for a portfolio project.</summary>
    private async Task SweepExpiredAttemptsAsync(int candidateProfileId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var stale = await _db.SkillAssessmentAttempts
            .Where(a => a.CandidateProfileId == candidateProfileId && a.Status == AssessmentAttemptStatus.InProgress && a.ExpiresAt < now)
            .ToListAsync(ct);

        if (stale.Count == 0) return;

        foreach (var attempt in stale)
        {
            attempt.Status = AssessmentAttemptStatus.Abandoned;
        }
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Fisher-Yates shuffle backed by RandomNumberGenerator (never System.Random),
    /// matching this codebase's convention for anything identifier/selection-related.</summary>
    private static List<int> ShuffleSecure(List<int> source)
    {
        var list = new List<int>(source);
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    private static AssessmentAttemptResultDto ToResultDto(SkillAssessmentAttempt attempt, List<SkillAssessmentAnswer> answers)
    {
        var review = answers
            .OrderBy(a => a.DisplayOrder)
            .Select(a => new AssessmentReviewQuestionDto(
                a.Question.QuestionText, a.Question.OptionA, a.Question.OptionB, a.Question.OptionC, a.Question.OptionD,
                a.Question.CorrectOptionIndex, a.SelectedOptionIndex, a.Question.Explanation))
            .ToList();

        return new AssessmentAttemptResultDto(
            attempt.Id, attempt.Category.ToString(), attempt.ScoreCorrectCount!.Value, attempt.TotalQuestionCount!.Value,
            attempt.PercentageScore!.Value, attempt.SubmittedAt!.Value, attempt.IsVisibleToRecruiters, review);
    }
}
