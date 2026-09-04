using AIRecruiter.Domain.Entities;

namespace AIRecruiter.Application.Validation;

/// <summary>Single source of truth for "does this job match this saved search" — used both
/// by JobAlertService (matching many jobs against one alert, for the candidate's own results)
/// and by JobPostingService's publish-notify hook (matching one newly published job against
/// many alerts). Kept pure/static so both directions stay in sync automatically.</summary>
public static class JobAlertMatcher
{
    public static bool Matches(JobAlert alert, JobPosting job)
    {
        if (!string.IsNullOrWhiteSpace(alert.City) && !string.Equals(alert.City, job.City, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (!string.IsNullOrWhiteSpace(alert.State) && !string.Equals(alert.State, job.State, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (alert.IsRemote.HasValue && job.IsRemote != alert.IsRemote.Value)
        {
            return false;
        }
        if (alert.JobType.HasValue && job.JobType != alert.JobType.Value)
        {
            return false;
        }
        if (alert.MinExperienceYears.HasValue && job.MinExperienceYears.HasValue && job.MinExperienceYears > alert.MinExperienceYears.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(alert.SkillsCsv))
        {
            var skills = alert.SkillsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (job.RequiredSkillsCsv is null || !skills.Any(s => job.RequiredSkillsCsv.Contains(s, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(alert.Keyword))
        {
            var haystack = $"{job.Title} {job.Description}";
            if (!haystack.Contains(alert.Keyword, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Salary range overlap — only enforced when both the alert and the job disclose a range.
        if ((alert.MinSalary.HasValue || alert.MaxSalary.HasValue) && (job.MinSalary.HasValue || job.MaxSalary.HasValue))
        {
            var alertMin = alert.MinSalary ?? decimal.MinValue;
            var alertMax = alert.MaxSalary ?? decimal.MaxValue;
            var jobMin = job.MinSalary ?? decimal.MinValue;
            var jobMax = job.MaxSalary ?? decimal.MaxValue;
            if (jobMax < alertMin || jobMin > alertMax)
            {
                return false;
            }
        }

        return true;
    }
}
