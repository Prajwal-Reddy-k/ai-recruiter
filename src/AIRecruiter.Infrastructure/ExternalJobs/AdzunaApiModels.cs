using System.Text.Json.Serialization;

namespace AIRecruiter.Infrastructure.ExternalJobs;

public class AdzunaSearchResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("results")]
    public List<AdzunaResult> Results { get; set; } = new();
}

public class AdzunaResult
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("company")]
    public AdzunaCompany? Company { get; set; }

    [JsonPropertyName("location")]
    public AdzunaLocation? Location { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("redirect_url")]
    public string? RedirectUrl { get; set; }

    [JsonPropertyName("salary_min")]
    public decimal? SalaryMin { get; set; }

    [JsonPropertyName("salary_max")]
    public decimal? SalaryMax { get; set; }

    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }
}

public class AdzunaCompany
{
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}

public class AdzunaLocation
{
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}
