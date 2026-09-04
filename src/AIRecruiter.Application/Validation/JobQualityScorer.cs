using AIRecruiter.Application.DTOs.Jobs;

namespace AIRecruiter.Application.Validation;

public record JobQualityInput(
    bool HasClearTitle,
    bool HasCompleteDescription,
    bool HasEnoughRequiredSkills,
    bool HasExperienceRange,
    bool HasLocationOrRemote,
    bool HasSalaryInfo,
    bool HasCompleteCompanyProfile,
    bool HasApplicationDeadline);

/// <summary>Pure, deterministic job-post quality scoring — no external AI, just a weighted
/// checklist. Mirrors ProfileStrengthCalculator's exact pattern so both surfaces stay
/// consistent. Computed on read, never persisted, and only ever attached to recruiter-only
/// response DTOs — never the public job DTO.</summary>
public static class JobQualityScorer
{
    public static JobQualityScoreDto Calculate(JobQualityInput input)
    {
        var checks = new (bool Filled, string Label, string Tip)[]
        {
            (input.HasClearTitle, "Write a clear job title", "A specific title (e.g. \"Senior Backend Engineer\") attracts more relevant applicants than a vague one."),
            (input.HasCompleteDescription, "Add a complete description", "Describe the role, responsibilities, and what a typical day looks like — aim for at least a few solid paragraphs."),
            (input.HasEnoughRequiredSkills, "Add at least three required skills", "Skills are the main signal used to match candidates to this job."),
            (input.HasExperienceRange, "Specify an experience range", "Candidates use this to quickly judge if they're a fit."),
            (input.HasLocationOrRemote, "Confirm the job location or mark it Remote", "Candidates filter and search heavily by India location."),
            (input.HasSalaryInfo, "Add salary information", "Listings with salary ranges typically get more qualified applicants — this is optional but recommended."),
            (input.HasCompleteCompanyProfile, "Complete your company profile", "A fuller company profile (description, website, industry) builds candidate trust in this listing."),
            (input.HasApplicationDeadline, "Add an application deadline", "A deadline creates urgency and helps you plan your hiring timeline."),
        };

        var filled = checks.Count(c => c.Filled);
        var score = (int)Math.Round(filled / (double)checks.Length * 100);

        var suggestions = checks
            .Where(c => !c.Filled)
            .Select(c => new JobQualitySuggestionDto(c.Label, c.Tip))
            .ToList();

        return new JobQualityScoreDto(score, suggestions);
    }
}
