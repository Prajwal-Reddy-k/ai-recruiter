using AIRecruiter.Application.Common;
using AIRecruiter.Application.DTOs.Applications;
using AIRecruiter.Application.DTOs.Matching;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class JobApplicationService : IJobApplicationService
{
    /// <summary>Statuses a candidate can no longer withdraw from — mirrors the frontend's
    /// existing TERMINAL_STATUSES set, but enforced here too since a UI-only check is not a
    /// real authorization boundary.</summary>
    private static readonly ApplicationStatus[] FinalStatuses =
        { ApplicationStatus.Hired, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn };

    private const int MaxCoverNoteLength = 4000;

    private readonly AppDbContext _db;
    private readonly IResumeMatchingService _matchingService;
    private readonly CandidateProfileService _candidateProfileService;
    private readonly INotificationService _notifications;
    private readonly IAuditLogService _auditLog;

    public JobApplicationService(
        AppDbContext db,
        IResumeMatchingService matchingService,
        CandidateProfileService candidateProfileService,
        INotificationService notifications,
        IAuditLogService auditLog)
    {
        _db = db;
        _matchingService = matchingService;
        _candidateProfileService = candidateProfileService;
        _notifications = notifications;
        _auditLog = auditLog;
    }

    public async Task<JobApplicationDto> ApplyAsync(int candidateUserId, int jobPostingId, string? coverNote, IReadOnlyList<SubmitScreeningAnswerRequest>? answers = null, CancellationToken ct = default)
    {
        if (coverNote is { Length: > MaxCoverNoteLength })
        {
            throw new ValidationException("Please fix the highlighted fields.", new Dictionary<string, string>
            {
                ["coverNote"] = $"Cover letter must be {MaxCoverNoteLength} characters or fewer.",
            });
        }

        var candidateProfile = await _db.CandidateProfiles.Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == candidateUserId, ct)
            ?? throw new NotFoundException("Complete your candidate profile before applying.");

        var job = await _db.JobPostings.Include(j => j.Company).Include(j => j.RecruiterProfile)
            .Include(j => j.ScreeningQuestions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(j => j.Id == jobPostingId, ct)
            ?? throw new NotFoundException("Job posting not found.");

        if (job.Status != JobStatus.Open)
        {
            throw new ConflictException("JOB_CLOSED", "This job is no longer accepting applications.");
        }

        if (job.ModerationStatus != ModerationStatus.Approved)
        {
            throw new ConflictException("JOB_CLOSED", "This job is no longer accepting applications.");
        }

        if (job.ApplicationDeadlineUtc.HasValue && job.ApplicationDeadlineUtc.Value < DateTime.UtcNow)
        {
            throw new ConflictException("JOB_EXPIRED", "The application deadline for this job has passed.");
        }

        var alreadyApplied = await _db.JobApplications.AnyAsync(
            a => a.JobPostingId == jobPostingId && a.CandidateProfileId == candidateProfile.Id, ct);
        if (alreadyApplied)
        {
            throw new ConflictException("ALREADY_APPLIED", "You have already applied to this job.");
        }

        var screeningAnswers = ValidateAndBuildScreeningAnswers(job.ScreeningQuestions, answers);

        var application = new JobApplication
        {
            JobPostingId = job.Id,
            CandidateProfileId = candidateProfile.Id,
            CoverNote = coverNote?.Trim(),
            Status = ApplicationStatus.Applied,
        };

        if (!string.IsNullOrWhiteSpace(candidateProfile.ResumeExtractedText))
        {
            var jobInput = new JobMatchInput(job.Title, job.Description, job.RequiredSkillsCsv, job.MinExperienceYears);
            var candidateInput = new CandidateMatchInput(candidateProfile.TotalExperienceYears, candidateProfile.Education);

            var match = _matchingService.CalculateMatch(candidateProfile.ResumeExtractedText, jobInput, candidateInput);

            application.MatchScore = match.OverallScore;
            application.MatchedSkillsCsv = string.Join(", ", match.MatchedSkills);
            application.MissingSkillsCsv = string.Join(", ", match.MissingSkills);
            application.SuggestedImprovements = string.Join(" | ", match.SuggestedImprovements);
            application.ScoringExplanation = match.Explanation;
        }
        else
        {
            application.ScoringExplanation = "No resume was on file at the time of application, so a match score could not be calculated.";
        }

        foreach (var answer in screeningAnswers)
        {
            application.ScreeningAnswers.Add(answer);
        }

        _db.JobApplications.Add(application);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique index (JobPostingId, CandidateProfileId) catches a race between the
            // pre-check above and this insert.
            var stillDuplicate = await _db.JobApplications.AnyAsync(
                a => a.JobPostingId == jobPostingId && a.CandidateProfileId == candidateProfile.Id && a.Id != application.Id, ct);
            if (stillDuplicate)
            {
                throw new ConflictException("ALREADY_APPLIED", "You have already applied to this job.");
            }
            throw;
        }

        _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            JobApplicationId = application.Id,
            FromStatus = null,
            ToStatus = ApplicationStatus.Applied,
            ChangedByUserId = candidateUserId,
        });
        await _db.SaveChangesAsync(ct);

        var referral = await _db.Referrals.FirstOrDefaultAsync(
            r => r.RegisteredUserId == candidateUserId && r.JobPostingId == jobPostingId && r.Status == ReferralStatus.Registered, ct);
        if (referral is not null)
        {
            referral.JobApplicationId = application.Id;
            referral.AppliedAtUtc = DateTime.UtcNow;
            referral.Status = ReferralStatus.Applied;
            await _db.SaveChangesAsync(ct);
            await _notifications.NotifyAsync(
                referral.ReferrerUserId, "ReferralApplied",
                $"{candidateProfile.User.FullName} (your referral) applied to {job.Title}.", "Referral", referral.Id, ct);
        }

        await _notifications.NotifyAsync(
            job.RecruiterProfile.UserId,
            "ApplicationReceived",
            $"{candidateProfile.User.FullName} applied to {job.Title}.",
            "JobApplication", application.Id, ct);

        await _auditLog.LogAsync(candidateUserId, "Candidate", "ApplicationSubmitted", "JobApplication", application.Id, new { job.Title }, ct);

        return ToDto(application, job.Title, job.Company.Name);
    }

    public async Task<IReadOnlyList<JobApplicationDto>> GetMyApplicationsAsync(int candidateUserId, CancellationToken ct = default)
    {
        var applications = await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(a => a.CandidateProfile)
            .Where(a => a.CandidateProfile.UserId == candidateUserId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        var applicationIds = applications.Select(a => a.Id).ToList();
        var activeInterviewStatuses = new[] { InterviewStatus.Proposed, InterviewStatus.Scheduled };
        var nextInterviewByApp = await _db.Interviews
            .Where(i => applicationIds.Contains(i.JobApplicationId) && activeInterviewStatuses.Contains(i.Status))
            .GroupBy(i => i.JobApplicationId)
            .Select(g => new { JobApplicationId = g.Key, NextStart = g.Min(i => i.ScheduledStartUtc) })
            .ToDictionaryAsync(x => x.JobApplicationId, x => (DateTime?)x.NextStart, ct);

        return applications
            .Select(a => ToDto(
                a, a.JobPosting.Title, a.JobPosting.Company.Name,
                jobLocation: IndiaLocationFormatter.Format(a.JobPosting.City, a.JobPosting.State, a.JobPosting.IsRemote),
                nextInterviewAtUtc: nextInterviewByApp.GetValueOrDefault(a.Id)))
            .ToList();
    }

    public async Task<JobApplicationDetailDto> GetApplicationDetailAsync(int userId, string role, int applicationId, CancellationToken ct = default)
    {
        var application = await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .Include(a => a.CandidateProfile).ThenInclude(c => c.User)
            .Include(a => a.StatusHistory).ThenInclude(h => h.ChangedByUser)
            .Include(a => a.ScreeningAnswers).ThenInclude(sa => sa.JobScreeningQuestion).ThenInclude(q => q.Options)
            .Include(a => a.ScreeningAnswers).ThenInclude(sa => sa.SelectedOptions).ThenInclude(so => so.ScreeningQuestionOption)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new NotFoundException("Application not found.");

        // A recruiter from another company throws here before ever reaching ToDetailDto, so
        // the includePreferredAnswers flag below is only ever true for the owning company.
        await EnsureCanViewApplicationAsync(application, userId, role, ct);

        return ToDetailDto(application, includePreferredAnswers: role == "Recruiter");
    }

    public async Task<IReadOnlyList<JobApplicationDto>> GetApplicationsForJobAsync(int recruiterUserId, int jobPostingId, ApplicantScreeningFilterQuery? filter = null, CancellationToken ct = default)
    {
        var job = await _db.JobPostings.Include(j => j.RecruiterProfile).Include(j => j.Company)
            .Include(j => j.ScreeningQuestions)
            .FirstOrDefaultAsync(j => j.Id == jobPostingId, ct)
            ?? throw new NotFoundException("Job posting not found.");

        if (!await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(_db, recruiterUserId, job.RecruiterProfile.UserId, job.CompanyId, ct))
        {
            throw new ForbiddenException("You do not have access to this job's applicants.");
        }

        var query = _db.JobApplications
            .Include(a => a.CandidateProfile).ThenInclude(c => c.User)
            .Include(a => a.ScreeningAnswers).ThenInclude(sa => sa.SelectedOptions)
            .Where(a => a.JobPostingId == jobPostingId);

        if (filter is not null)
        {
            if (filter.QuestionId.HasValue && filter.YesNo is not null)
            {
                query = query.Where(a => a.ScreeningAnswers.Any(sa => sa.JobScreeningQuestionId == filter.QuestionId && sa.TextValue == filter.YesNo));
            }
            if (filter.OptionId.HasValue)
            {
                query = query.Where(a => a.ScreeningAnswers.Any(sa => sa.SelectedOptions.Any(o => o.ScreeningQuestionOptionId == filter.OptionId)));
            }
            if (filter.QuestionId.HasValue && (filter.MinNumber.HasValue || filter.MaxNumber.HasValue))
            {
                query = query.Where(a => a.ScreeningAnswers.Any(sa =>
                    sa.JobScreeningQuestionId == filter.QuestionId &&
                    sa.NumberValue != null &&
                    (!filter.MinNumber.HasValue || sa.NumberValue >= filter.MinNumber) &&
                    (!filter.MaxNumber.HasValue || sa.NumberValue <= filter.MaxNumber)));
            }
        }

        var applications = await query.OrderByDescending(a => a.CreatedAt).ToListAsync(ct);

        var requiredQuestionIds = job.ScreeningQuestions.Where(q => q.IsRequired).Select(q => q.Id).ToHashSet();

        if (filter?.RequiredAnsweredOnly.HasValue == true)
        {
            applications = applications.Where(a =>
            {
                var answeredIds = a.ScreeningAnswers.Select(sa => sa.JobScreeningQuestionId).ToHashSet();
                var allAnswered = requiredQuestionIds.All(answeredIds.Contains);
                return filter.RequiredAnsweredOnly.Value == allAnswered;
            }).ToList();
        }

        return applications.Select(a =>
        {
            var answeredRequiredCount = requiredQuestionIds.Count(qid => a.ScreeningAnswers.Any(sa => sa.JobScreeningQuestionId == qid));
            return ToDto(a, job.Title, job.Company.Name, a.CandidateProfile.User.FullName,
                requiredQuestionsAnsweredCount: answeredRequiredCount, requiredQuestionsTotalCount: requiredQuestionIds.Count);
        }).ToList();
    }

    public async Task<(Stream Content, string FileName, string ContentType)> DownloadApplicantResumeAsync(int userId, string role, int applicationId, CancellationToken ct = default)
    {
        var application = await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .Include(a => a.CandidateProfile).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new NotFoundException("Application not found.");

        await EnsureCanViewApplicationAsync(application, userId, role, ct);

        return await _candidateProfileService.OpenResumeAsync(application.CandidateProfile, ct);
    }

    public async Task<JobApplicationDto> UpdateStatusAsync(int recruiterUserId, int applicationId, ApplicationStatus newStatus, string? note = null, CancellationToken ct = default)
    {
        var application = await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .Include(a => a.CandidateProfile).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new NotFoundException("Application not found.");

        if (!await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(_db, recruiterUserId, application.JobPosting.RecruiterProfile.UserId, application.JobPosting.CompanyId, ct))
        {
            throw new ForbiddenException("You do not have access to this application.");
        }

        var previousStatus = application.Status;
        application.Status = newStatus;
        application.UpdatedAt = DateTime.UtcNow;

        _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            JobApplicationId = application.Id,
            FromStatus = previousStatus,
            ToStatus = newStatus,
            ChangedByUserId = recruiterUserId,
            Note = note,
        });

        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            application.CandidateProfile.UserId,
            "ApplicationStatusChanged",
            $"Your application for {application.JobPosting.Title} is now: {newStatus}.",
            "JobApplication", application.Id, ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "ApplicationStatusChanged", "JobApplication", application.Id, new { From = previousStatus.ToString(), To = newStatus.ToString() }, ct);

        return ToDto(application, application.JobPosting.Title, application.JobPosting.Company.Name, application.CandidateProfile.User.FullName);
    }

    public async Task<JobApplicationDto> WithdrawAsync(int candidateUserId, int applicationId, CancellationToken ct = default)
    {
        var application = await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .Include(a => a.CandidateProfile)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new NotFoundException("Application not found.");

        if (application.CandidateProfile.UserId != candidateUserId)
        {
            throw new ForbiddenException("You do not have access to this application.");
        }

        if (FinalStatuses.Contains(application.Status))
        {
            throw new ConflictException("APPLICATION_FINAL", $"This application is already {application.Status} and can no longer be withdrawn.");
        }

        var previousStatus = application.Status;
        application.Status = ApplicationStatus.Withdrawn;
        application.UpdatedAt = DateTime.UtcNow;

        _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            JobApplicationId = application.Id,
            FromStatus = previousStatus,
            ToStatus = ApplicationStatus.Withdrawn,
            ChangedByUserId = candidateUserId,
        });

        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            application.JobPosting.RecruiterProfile.UserId,
            "ApplicationWithdrawn",
            $"An applicant withdrew their application for {application.JobPosting.Title}.",
            "JobApplication", application.Id, ct);

        return ToDto(application, application.JobPosting.Title, application.JobPosting.Company.Name);
    }

    /// <summary>Validates the candidate's submitted screening answers against the job's
    /// current question set and returns the ScreeningAnswer entities ready to attach to the
    /// new JobApplication. All failures are collected into one ValidationException, matching
    /// the existing ["coverNote"] field-error convention above.</summary>
    private static List<ScreeningAnswer> ValidateAndBuildScreeningAnswers(ICollection<JobScreeningQuestion> questions, IReadOnlyList<SubmitScreeningAnswerRequest>? answers)
    {
        var errors = new Dictionary<string, string>();
        var answersByQuestionId = new Dictionary<int, SubmitScreeningAnswerRequest>();
        foreach (var a in answers ?? Array.Empty<SubmitScreeningAnswerRequest>())
        {
            answersByQuestionId[a.QuestionId] = a;
        }

        var results = new List<ScreeningAnswer>();

        foreach (var question in questions)
        {
            var hasAnswer = answersByQuestionId.TryGetValue(question.Id, out var answer);
            var fieldKey = $"question_{question.Id}";

            if (!hasAnswer || answer is null)
            {
                if (question.IsRequired)
                {
                    errors[fieldKey] = "This question is required.";
                }
                continue;
            }

            switch (question.QuestionType)
            {
                case ScreeningQuestionType.ShortText:
                case ScreeningQuestionType.LongText:
                    if (string.IsNullOrWhiteSpace(answer.TextValue))
                    {
                        if (question.IsRequired) errors[fieldKey] = "This question is required.";
                        continue;
                    }
                    results.Add(new ScreeningAnswer { JobScreeningQuestionId = question.Id, TextValue = answer.TextValue.Trim() });
                    break;

                case ScreeningQuestionType.YesNo:
                    if (answer.TextValue is not ("Yes" or "No"))
                    {
                        if (question.IsRequired || answer.TextValue is not null) errors[fieldKey] = "Answer must be Yes or No.";
                        continue;
                    }
                    results.Add(new ScreeningAnswer { JobScreeningQuestionId = question.Id, TextValue = answer.TextValue });
                    break;

                case ScreeningQuestionType.Number:
                    if (string.IsNullOrWhiteSpace(answer.TextValue) && !answer.NumberValue.HasValue)
                    {
                        if (question.IsRequired) errors[fieldKey] = "This question is required.";
                        continue;
                    }
                    var numberValue = answer.NumberValue ?? (decimal.TryParse(answer.TextValue, out var parsed) ? parsed : (decimal?)null);
                    if (!numberValue.HasValue)
                    {
                        errors[fieldKey] = "Enter a valid number.";
                        continue;
                    }
                    results.Add(new ScreeningAnswer { JobScreeningQuestionId = question.Id, NumberValue = numberValue.Value, TextValue = numberValue.Value.ToString() });
                    break;

                case ScreeningQuestionType.Url:
                    if (string.IsNullOrWhiteSpace(answer.TextValue))
                    {
                        if (question.IsRequired) errors[fieldKey] = "This question is required.";
                        continue;
                    }
                    if (!Uri.TryCreate(answer.TextValue.Trim(), UriKind.Absolute, out _))
                    {
                        errors[fieldKey] = "Enter a valid URL (including https://).";
                        continue;
                    }
                    results.Add(new ScreeningAnswer { JobScreeningQuestionId = question.Id, TextValue = answer.TextValue.Trim() });
                    break;

                case ScreeningQuestionType.SingleChoice:
                case ScreeningQuestionType.MultipleChoice:
                    var selectedIds = (answer.SelectedOptionIds ?? Array.Empty<int>()).Distinct().ToList();
                    if (selectedIds.Count == 0)
                    {
                        if (question.IsRequired) errors[fieldKey] = "This question is required.";
                        continue;
                    }
                    if (question.QuestionType == ScreeningQuestionType.SingleChoice && selectedIds.Count > 1)
                    {
                        errors[fieldKey] = "Choose only one option.";
                        continue;
                    }
                    var validOptionIds = question.Options.Select(o => o.Id).ToHashSet();
                    if (!selectedIds.All(validOptionIds.Contains))
                    {
                        errors[fieldKey] = "One or more selected options are not valid for this question.";
                        continue;
                    }
                    results.Add(new ScreeningAnswer
                    {
                        JobScreeningQuestionId = question.Id,
                        SelectedOptions = selectedIds.Select(id => new ScreeningAnswerSelectedOption { ScreeningQuestionOptionId = id }).ToList(),
                    });
                    break;
            }
        }

        // Any submitted answer that doesn't correspond to a question on this job is rejected
        // outright — it can't be silently ignored, since that would hide a client-side bug.
        var validQuestionIds = questions.Select(q => q.Id).ToHashSet();
        if (answers is not null && answers.Any(a => !validQuestionIds.Contains(a.QuestionId)))
        {
            errors["answers"] = "One or more answers reference a question that doesn't belong to this job.";
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Please fix the highlighted fields.", errors);
        }

        return results;
    }

    private async Task EnsureCanViewApplicationAsync(JobApplication application, int userId, string role, CancellationToken ct)
    {
        var isOwningCandidate = role == "Candidate" && application.CandidateProfile.UserId == userId;
        var isOwningRecruiter = role == "Recruiter" && await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(
            _db, userId, application.JobPosting.RecruiterProfile.UserId, application.JobPosting.CompanyId, ct);

        if (!isOwningCandidate && !isOwningRecruiter)
        {
            throw new ForbiddenException("You do not have access to this application.");
        }
    }

    private static JobApplicationDto ToDto(
        JobApplication a, string jobTitle, string companyName, string? candidateFullName = null,
        string? jobLocation = null, DateTime? nextInterviewAtUtc = null,
        int requiredQuestionsAnsweredCount = 0, int requiredQuestionsTotalCount = 0) => new(
        a.Id,
        a.JobPostingId,
        jobTitle,
        companyName,
        candidateFullName,
        a.CandidateProfile?.Headline,
        a.CandidateProfile?.SkillsCsv,
        a.Status.ToString(),
        a.CreatedAt,
        a.UpdatedAt,
        a.MatchScore.HasValue ? (int)a.MatchScore.Value : null,
        jobLocation,
        nextInterviewAtUtc,
        a.CandidateProfile is null ? null : AvatarUrlFormatter.Format(a.CandidateProfile.Id, a.CandidateProfile.AvatarStorageKey),
        a.CandidateProfileId,
        requiredQuestionsAnsweredCount,
        requiredQuestionsTotalCount);

    private static JobApplicationDetailDto ToDetailDto(JobApplication a, bool includePreferredAnswers = false) => new(
        a.Id,
        a.JobPostingId,
        a.JobPosting.Title,
        a.JobPosting.Company.Name,
        a.CandidateProfileId,
        a.CandidateProfile.User.FullName,
        a.Status.ToString(),
        a.CoverNote,
        a.CreatedAt,
        a.UpdatedAt,
        a.MatchScore.HasValue ? (int)a.MatchScore.Value : null,
        SplitCsv(a.MatchedSkillsCsv),
        SplitCsv(a.MissingSkillsCsv),
        SplitList(a.SuggestedImprovements),
        a.ScoringExplanation,
        a.StatusHistory
            .OrderBy(h => h.ChangedAt)
            .Select(h => new StatusHistoryEntryDto(
                h.FromStatus?.ToString(),
                h.ToStatus.ToString(),
                h.ChangedByUser?.FullName ?? "System",
                h.ChangedAt,
                h.Note))
            .ToList(),
        ComputeNextAction(a.Status),
        a.ScreeningAnswers
            .OrderBy(sa => sa.JobScreeningQuestion.DisplayOrder)
            .Select(sa => new ScreeningAnswerDto(
                sa.JobScreeningQuestionId,
                sa.JobScreeningQuestion.QuestionText,
                sa.JobScreeningQuestion.QuestionType.ToString(),
                sa.JobScreeningQuestion.IsRequired,
                sa.TextValue,
                sa.NumberValue,
                sa.SelectedOptions.Select(so => so.ScreeningQuestionOption.OptionText).ToList(),
                includePreferredAnswers ? sa.JobScreeningQuestion.PreferredAnswer : null))
            .ToList());

    /// <summary>Plain-language "what happens next" guidance per status — static, no AI.</summary>
    private static string ComputeNextAction(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Applied => "Awaiting recruiter review.",
        ApplicationStatus.Screening => "Your application is being screened.",
        ApplicationStatus.Shortlisted => "You've been shortlisted — an interview may be scheduled soon.",
        ApplicationStatus.InterviewScheduled => "Interview scheduled — check your email/notifications for details.",
        ApplicationStatus.InterviewCompleted => "Interview completed — awaiting a decision.",
        ApplicationStatus.Offer => "Offer extended — respond soon.",
        ApplicationStatus.Hired => "Congratulations — you were hired for this role.",
        ApplicationStatus.Rejected => "This application was not successful.",
        ApplicationStatus.Withdrawn => "You withdrew this application.",
        _ => "No action needed right now.",
    };

    private static IReadOnlyList<string> SplitCsv(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? Array.Empty<string>()
            : csv.Split(", ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IReadOnlyList<string> SplitList(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(" | ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
