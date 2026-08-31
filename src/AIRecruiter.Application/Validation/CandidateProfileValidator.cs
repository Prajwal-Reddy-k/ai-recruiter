using System.Text.RegularExpressions;
using AIRecruiter.Application.DTOs.Candidates;

namespace AIRecruiter.Application.Validation;

public record CandidateProfileValidationResult(bool IsValid, IReadOnlyDictionary<string, string> Errors, string? NormalizedPhone);

/// <summary>
/// Server-side validation for the candidate profile form — the backstop that still applies
/// even if a caller bypasses the frontend's own checks. Pure logic, no IO.
/// </summary>
public class CandidateProfileValidator
{
    private const int MaxSkills = 30;
    private const int MaxSkillLength = 50;
    private const decimal MaxSalary = 100_000_000m;

    private static readonly Regex IndianMobileRegex = new("^[6-9][0-9]{9}$", RegexOptions.Compiled);
    private static readonly Regex NamePunctuationRegex = new(@"^[\p{L}\s.'\-]+$", RegexOptions.Compiled);

    public CandidateProfileValidationResult Validate(UpsertCandidateProfileRequest request)
    {
        var errors = new Dictionary<string, string>();
        var currentYear = DateTime.UtcNow.Year;

        if (string.IsNullOrWhiteSpace(request.Headline))
        {
            errors["headline"] = "Headline is required.";
        }
        else if (request.Headline.Trim().Length < 5 || request.Headline.Trim().Length > 150)
        {
            errors["headline"] = "Headline must be between 5 and 150 characters.";
        }

        if (!string.IsNullOrEmpty(request.Summary) && request.Summary.Length > 2000)
        {
            errors["summary"] = "Bio must be 2000 characters or fewer.";
        }

        if (!string.IsNullOrEmpty(request.Education) && request.Education.Length > 300)
        {
            errors["education"] = "Education must be 300 characters or fewer.";
        }

        if (request.GraduationYear is not null)
        {
            if (request.GraduationYear < 1950 || request.GraduationYear > currentYear + 1)
            {
                errors["graduationYear"] = $"Graduation year must be between 1950 and {currentYear + 1}.";
            }
            else if (string.IsNullOrWhiteSpace(request.Education))
            {
                errors["education"] = "Enter your degree/institution along with a graduation year.";
            }
        }

        if (!string.IsNullOrEmpty(request.ExperienceSummary) && request.ExperienceSummary.Length > 2000)
        {
            errors["experienceSummary"] = "Experience summary must be 2000 characters or fewer.";
        }

        if (request.TotalExperienceYears is not null && (request.TotalExperienceYears < 0 || request.TotalExperienceYears > 60))
        {
            errors["totalExperienceYears"] = "Enter a whole number of years between 0 and 60.";
        }

        ValidateSkills(request.SkillsCsv, errors);

        if (request.CurrentSalary is not null && (request.CurrentSalary < 0 || request.CurrentSalary > MaxSalary))
        {
            errors["currentSalary"] = "Current salary must be a realistic non-negative amount.";
        }

        if (request.ExpectedSalary is not null && (request.ExpectedSalary < 0 || request.ExpectedSalary > MaxSalary))
        {
            errors["expectedSalary"] = "Expected salary must be a realistic non-negative amount.";
        }

        string? normalizedPhone = null;
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            normalizedPhone = NormalizePhone(request.Phone);
            if (normalizedPhone is null || !IndianMobileRegex.IsMatch(normalizedPhone))
            {
                errors["phone"] = "Enter a valid 10-digit Indian mobile number.";
            }
        }

        ValidateUrl(request.LinkedInUrl, "linkedInUrl", "LinkedIn", "linkedin.com", errors);
        ValidateUrl(request.GithubUrl, "githubUrl", "GitHub", "github.com", errors);
        ValidateUrl(request.PortfolioUrl, "portfolioUrl", "portfolio/website", null, errors);

        return new CandidateProfileValidationResult(errors.Count == 0, errors, normalizedPhone);
    }

    public static bool IsValidFullName(string? fullName) =>
        !string.IsNullOrWhiteSpace(fullName)
        && fullName.Trim().Length is >= 2 and <= 100
        && NamePunctuationRegex.IsMatch(fullName.Trim());

    private static void ValidateSkills(string? skillsCsv, Dictionary<string, string> errors)
    {
        var raw = (skillsCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        if (raw.Count == 0)
        {
            errors["skills"] = "Add at least one skill.";
            return;
        }

        if (raw.Count > MaxSkills)
        {
            errors["skills"] = $"You can list at most {MaxSkills} skills.";
            return;
        }

        if (raw.Any(s => s.Length > MaxSkillLength))
        {
            errors["skills"] = $"Each skill must be {MaxSkillLength} characters or fewer.";
            return;
        }

        var distinctCount = raw.Select(s => s.ToLowerInvariant()).Distinct().Count();
        if (distinctCount != raw.Count)
        {
            errors["skills"] = "Remove duplicate skills.";
        }
    }

    /// <summary>Strips a leading country code / trunk prefix and formatting characters,
    /// leaving a bare 10-digit number for storage — never stores the raw, possibly
    /// inconsistently-formatted input.</summary>
    private static string? NormalizePhone(string phone)
    {
        var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

        if (digitsOnly.Length == 10) return digitsOnly;
        if (digitsOnly.Length == 11 && digitsOnly.StartsWith('0')) return digitsOnly[1..];
        if (digitsOnly.Length == 12 && digitsOnly.StartsWith("91")) return digitsOnly[2..];
        if (digitsOnly.Length == 13 && digitsOnly.StartsWith("091")) return digitsOnly[3..];

        return null;
    }

    /// <summary>True if <paramref name="host"/> is exactly <paramref name="domain"/> or a
    /// subdomain of it — a plain substring check would wrongly accept a host like
    /// "notgithub.com" or "github.com.attacker.net" as matching "github.com".</summary>
    private static bool IsHostOrSubdomain(string host, string domain) =>
        host.Equals(domain, StringComparison.OrdinalIgnoreCase)
        || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);

    private static void ValidateUrl(string? url, string field, string label, string? requiredDomainSubstring, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors[field] = $"Enter a valid {label} URL (starting with https://).";
            return;
        }

        if (requiredDomainSubstring is not null && !IsHostOrSubdomain(uri.Host, requiredDomainSubstring))
        {
            errors[field] = $"Enter a valid {label} URL (should link to {requiredDomainSubstring}).";
        }
    }
}
