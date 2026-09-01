using AIRecruiter.Application.DTOs.Candidates;

namespace AIRecruiter.Application.Validation;

public record ProfileStrengthInput(
    bool HasHeadline,
    bool HasSummary,
    bool HasSkills,
    bool HasAvatar,
    bool HasResumeFile,
    bool HasWorkExperience,
    bool HasEducation,
    bool HasLinks,
    bool HasPreferences);

/// <summary>Pure, deterministic profile-strength scoring — no external AI, just a weighted
/// checklist. Shared by DashboardService (the dashboard's completion card) and
/// ResumeBuilderService (the resume builder's strength meter) so both surfaces report the
/// exact same number and the same "how to improve" guidance.</summary>
public static class ProfileStrengthCalculator
{
    public static ProfileStrengthResult Calculate(ProfileStrengthInput input)
    {
        var checks = new (bool Filled, string Label, string Tip, string LinkPath)[]
        {
            (input.HasHeadline, "Add a headline", "A short, specific headline (e.g. \"Senior Backend Engineer\") helps recruiters understand your role at a glance.", "/profile"),
            (input.HasSummary, "Write a professional summary", "2-3 sentences summarizing your experience and strengths make your profile far more compelling.", "/resume-builder"),
            (input.HasSkills, "List at least 3 skills", "Skills are the main signal used to match you to relevant jobs.", "/profile"),
            (input.HasAvatar, "Add a profile photo", "Profiles with a photo feel more complete and trustworthy to recruiters.", "/profile"),
            (input.HasResumeFile, "Upload your resume", "Recruiters and the match-scoring engine both need an uploaded resume file.", "/profile"),
            (input.HasWorkExperience, "Add work experience", "At least one work experience entry gives recruiters a sense of your career history.", "/resume-builder"),
            (input.HasEducation, "Add education", "At least one education entry rounds out your background.", "/resume-builder"),
            (input.HasLinks, "Add a LinkedIn, GitHub, or portfolio link", "Links let recruiters see more of your work beyond the resume.", "/profile"),
            (input.HasPreferences, "Set your job preferences", "Availability, preferred locations, and job types improve your job recommendations.", "/profile"),
        };

        var filled = checks.Count(c => c.Filled);
        var score = (int)Math.Round(filled / (double)checks.Length * 100);

        var missing = checks
            .Where(c => !c.Filled)
            .Select(c => new MissingProfileItemDto(c.Label, c.Tip, c.LinkPath))
            .ToList();

        return new ProfileStrengthResult(score, missing);
    }
}
