using System.Security.Cryptography;
using System.Text;

namespace AIRecruiter.Application.Validation;

/// <summary>Pure slug-building helper — the actual DB uniqueness-retry loop lives in
/// CandidateProfileService (needs DB access this class deliberately doesn't have).</summary>
public static class PublicProfileSlugGenerator
{
    /// <summary>Builds one candidate slug, e.g. "priya-sharma-a3f9c1e2". Call again with a
    /// fresh suffix on a uniqueness collision.</summary>
    public static string Generate(string fullName)
    {
        var baseSlug = Slugify(fullName);
        var suffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
        return string.IsNullOrEmpty(baseSlug) ? suffix : $"{baseSlug}-{suffix}";
    }

    private static string Slugify(string value)
    {
        var sb = new StringBuilder();
        var lastWasHyphen = false;
        foreach (var c in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && sb.Length > 0)
            {
                sb.Append('-');
                lastWasHyphen = true;
            }
        }

        var result = sb.ToString().Trim('-');
        return result.Length > 60 ? result[..60].Trim('-') : result;
    }
}
