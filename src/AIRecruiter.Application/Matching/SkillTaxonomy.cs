using System.Text.RegularExpressions;

namespace AIRecruiter.Application.Matching;

/// <summary>
/// Curated list of common technology/recruiting skills used for deterministic,
/// explainable keyword-based skill extraction (no external AI service).
/// </summary>
public static class SkillTaxonomy
{
    public static readonly IReadOnlyList<string> Skills = new[]
    {
        // Languages
        "C#", "Java", "Python", "JavaScript", "TypeScript", "Go", "Rust", "Ruby", "PHP", "Kotlin",
        "Swift", "C++", "C", "Scala", "R", "SQL", "Bash", "PowerShell",
        // Web / Frontend
        "React", "Angular", "Vue", "Vue.js", "Next.js", "Redux", "HTML", "CSS", "Sass", "Tailwind CSS",
        "Node.js", "Express", "REST", "GraphQL", "Webpack", "Vite",
        // Backend / .NET
        "ASP.NET Core", ".NET", "Entity Framework", "Entity Framework Core", "Spring Boot", "Django",
        "Flask", "FastAPI", "Laravel", "Ruby on Rails",
        // Data / Databases
        "SQL Server", "PostgreSQL", "MySQL", "MongoDB", "Redis", "SQLite", "Elasticsearch",
        "Data Analysis", "Machine Learning", "Deep Learning", "TensorFlow", "PyTorch", "Pandas",
        "NumPy", "Power BI", "Tableau", "ETL",
        // Cloud / DevOps
        "AWS", "Azure", "Google Cloud", "GCP", "Docker", "Kubernetes", "CI/CD", "Jenkins", "GitHub Actions",
        "Terraform", "Ansible", "Linux", "Git", "DevOps", "Microservices",
        // Mobile
        "Android", "iOS", "React Native", "Flutter", "Xamarin",
        // QA / Testing
        "Unit Testing", "xUnit", "NUnit", "Jest", "Selenium", "Cypress", "Test Automation",
        // Practices / Soft skills
        "Agile", "Scrum", "Kanban", "Project Management", "Communication", "Leadership",
        "Problem Solving", "Team Collaboration", "Stakeholder Management",
        // Recruiting-adjacent domains
        "Recruiting", "Talent Acquisition", "HR", "Sourcing", "ATS", "Onboarding",
        // Design
        "UI/UX", "Figma", "Adobe XD", "Photoshop",
        // Security
        "OAuth", "JWT", "OWASP", "Penetration Testing", "Cybersecurity",
    };

    private static readonly Lazy<HashSet<string>> NormalizedSkillLookup = new(() =>
        new HashSet<string>(Skills.Select(Normalize), StringComparer.Ordinal));

    /// <summary>Collapses to lowercase, alphanumeric-only tokens separated by single spaces.</summary>
    public static string Normalize(string text) =>
        Regex.Replace(text.ToLowerInvariant(), "[^a-z0-9]+", " ").Trim();

    /// <summary>Extracts the set of taxonomy skills mentioned in free text (multi-word aware, punctuation-insensitive).</summary>
    public static HashSet<string> ExtractFromText(string text)
    {
        var normalizedText = " " + Normalize(text) + " ";
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var skill in Skills)
        {
            var normalizedSkill = Normalize(skill);
            if (normalizedSkill.Length == 0) continue;
            if (normalizedText.Contains(" " + normalizedSkill + " ", StringComparison.Ordinal))
            {
                found.Add(skill);
            }
        }

        return found;
    }

    /// <summary>Checks whether an arbitrary skill string (not necessarily in the taxonomy) appears in text.</summary>
    public static bool ContainsSkill(string normalizedText, string skill)
    {
        var normalizedSkill = Normalize(skill);
        if (normalizedSkill.Length == 0) return false;
        return (" " + normalizedText + " ").Contains(" " + normalizedSkill + " ", StringComparison.Ordinal);
    }

    /// <summary>Parses a comma-separated skills string into a distinct, trimmed, order-preserving list.</summary>
    public static List<string> ParseCsv(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return new List<string>();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var raw in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (seen.Add(raw))
            {
                result.Add(raw);
            }
        }

        return result;
    }
}
