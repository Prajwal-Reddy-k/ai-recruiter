using AIRecruiter.Application.DTOs.Matching;
using AIRecruiter.Application.Interfaces;

namespace AIRecruiter.Application.Matching;

/// <summary>
/// Rule-based, deterministic matching: weighted skill coverage, TF-IDF/cosine text
/// similarity, and experience/education signals. Runs entirely locally, no network calls.
/// Weights: skills 55, text similarity 30, experience 10, education 5.
/// </summary>
public class ResumeMatchingService : IResumeMatchingService
{
    private const double SkillWeight = 55;
    private const double SimilarityWeight = 30;
    private const double ExperienceWeight = 10;
    private const double EducationWeight = 5;

    private static readonly string[] StopWords =
    {
        "the", "a", "an", "and", "or", "of", "to", "in", "for", "with", "on", "at", "is", "are",
        "as", "by", "be", "this", "that", "will", "we", "you", "our", "your", "it", "from"
    };

    private static readonly string[] DegreeKeywords = { "bachelor", "master", "phd", "doctorate", "diploma", "associate", "degree" };

    public ResumeMatchResult CalculateMatch(string resumeText, JobMatchInput job, CandidateMatchInput candidate)
    {
        resumeText ??= string.Empty;

        var (skillScore, matchedSkills, missingSkills) = ScoreSkills(resumeText, job);
        var (similarityScore, cosine) = ScoreTextSimilarity(resumeText, job);
        var (experienceScore, experienceNote) = ScoreExperience(job, candidate);
        var (educationScore, educationNote) = ScoreEducation(job, candidate);

        var total = skillScore + similarityScore + experienceScore + educationScore;
        var overallScore = (int)Math.Round(Math.Clamp(total, 0, 100));

        var suggestions = BuildSuggestions(missingSkills, cosine, experienceNote);

        var explanation =
            $"Skill match: {matchedSkills.Count}/{Math.Max(matchedSkills.Count + missingSkills.Count, 0)} required skills found " +
            $"({skillScore:F0} of {SkillWeight:F0} pts). " +
            $"Text similarity to job description: {cosine:P0} ({similarityScore:F0} of {SimilarityWeight:F0} pts). " +
            $"Experience: {experienceNote} ({experienceScore:F0} of {ExperienceWeight:F0} pts). " +
            $"Education: {educationNote} ({educationScore:F0} of {EducationWeight:F0} pts). " +
            $"Total score: {overallScore}/100.";

        return new ResumeMatchResult(overallScore, matchedSkills, missingSkills, suggestions, explanation);
    }

    private static (double Score, List<string> Matched, List<string> Missing) ScoreSkills(string resumeText, JobMatchInput job)
    {
        var jobSkills = SkillTaxonomy.ParseCsv(job.RequiredSkillsCsv);
        if (jobSkills.Count == 0)
        {
            jobSkills = SkillTaxonomy.ExtractFromText(job.Description).ToList();
        }

        // Deduplicate case-insensitively while preserving first-seen casing.
        var dedupedJobSkills = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in jobSkills)
        {
            if (seen.Add(skill)) dedupedJobSkills.Add(skill);
        }

        var normalizedResume = SkillTaxonomy.Normalize(resumeText);

        var matched = new List<string>();
        var missing = new List<string>();
        foreach (var skill in dedupedJobSkills)
        {
            if (SkillTaxonomy.ContainsSkill(normalizedResume, skill))
            {
                matched.Add(skill);
            }
            else
            {
                missing.Add(skill);
            }
        }

        if (dedupedJobSkills.Count == 0)
        {
            // No skills declared on the job — neutral full credit, nothing to penalize against.
            return (SkillWeight, matched, missing);
        }

        var coverage = (double)matched.Count / dedupedJobSkills.Count;
        return (coverage * SkillWeight, matched, missing);
    }

    private static (double Score, double Cosine) ScoreTextSimilarity(string resumeText, JobMatchInput job)
    {
        var jobText = string.Join(' ', job.Title, job.Description, job.RequiredSkillsCsv ?? string.Empty);

        var resumeTerms = Tokenize(resumeText);
        var jobTerms = Tokenize(jobText);

        if (resumeTerms.Count == 0 || jobTerms.Count == 0)
        {
            return (0, 0);
        }

        var vocabulary = new HashSet<string>(resumeTerms.Keys);
        vocabulary.UnionWith(jobTerms.Keys);

        double dot = 0, resumeNorm = 0, jobNorm = 0;
        foreach (var term in vocabulary)
        {
            var docFreq = (resumeTerms.ContainsKey(term) ? 1 : 0) + (jobTerms.ContainsKey(term) ? 1 : 0);
            var idf = Math.Log(2.0 / docFreq) + 1.0;

            var resumeTf = resumeTerms.GetValueOrDefault(term, 0) / (double)resumeTerms.Values.Sum();
            var jobTf = jobTerms.GetValueOrDefault(term, 0) / (double)jobTerms.Values.Sum();

            var resumeWeight = resumeTf * idf;
            var jobWeight = jobTf * idf;

            dot += resumeWeight * jobWeight;
            resumeNorm += resumeWeight * resumeWeight;
            jobNorm += jobWeight * jobWeight;
        }

        if (resumeNorm == 0 || jobNorm == 0)
        {
            return (0, 0);
        }

        var cosine = dot / (Math.Sqrt(resumeNorm) * Math.Sqrt(jobNorm));
        cosine = Math.Clamp(cosine, 0, 1);

        return (cosine * SimilarityWeight, cosine);
    }

    private static Dictionary<string, int> Tokenize(string text)
    {
        var normalized = SkillTaxonomy.Normalize(text);
        var terms = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var token in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length < 2 || StopWords.Contains(token)) continue;
            terms[token] = terms.GetValueOrDefault(token, 0) + 1;
        }

        return terms;
    }

    private static (double Score, string Note) ScoreExperience(JobMatchInput job, CandidateMatchInput candidate)
    {
        if (job.MinExperienceYears is not { } minYears || minYears <= 0)
        {
            return (ExperienceWeight, "not required by this role");
        }

        if (candidate.TotalExperienceYears is not { } candidateYears)
        {
            return (0, $"requires {minYears}+ years, none listed on profile");
        }

        if (candidateYears >= minYears)
        {
            return (ExperienceWeight, $"{candidateYears} years meets the {minYears}-year requirement");
        }

        var ratio = Math.Clamp((double)candidateYears / minYears, 0, 1);
        return (ratio * ExperienceWeight, $"{candidateYears} of {minYears} required years");
    }

    private static (double Score, string Note) ScoreEducation(JobMatchInput job, CandidateMatchInput candidate)
    {
        var jobText = SkillTaxonomy.Normalize(job.Title + " " + job.Description);
        var requiresDegree = DegreeKeywords.Any(k => (" " + jobText + " ").Contains(" " + k + " ", StringComparison.Ordinal));

        if (!requiresDegree)
        {
            return (EducationWeight, "not specified as a requirement");
        }

        if (string.IsNullOrWhiteSpace(candidate.Education))
        {
            return (0, "role references a degree requirement, none listed on profile");
        }

        var candidateEducation = SkillTaxonomy.Normalize(candidate.Education);
        var matches = DegreeKeywords.Any(k => (" " + candidateEducation + " ").Contains(" " + k + " ", StringComparison.Ordinal));

        return matches
            ? (EducationWeight, "candidate education matches a referenced requirement")
            : (0, "candidate education does not clearly match the referenced requirement");
    }

    private static List<string> BuildSuggestions(List<string> missingSkills, double cosine, string experienceNote)
    {
        var suggestions = new List<string>();

        foreach (var skill in missingSkills.Take(5))
        {
            suggestions.Add($"Consider highlighting or gaining experience with: {skill}.");
        }

        if (cosine < 0.3)
        {
            suggestions.Add("Tailor your resume language to more closely match the job description's terminology.");
        }

        if (experienceNote.Contains("of", StringComparison.OrdinalIgnoreCase) && experienceNote.Contains("required years", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("Highlight any additional relevant experience that may not be fully reflected in your years-of-experience total.");
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add("Your resume aligns well with this role's stated requirements.");
        }

        return suggestions;
    }
}
