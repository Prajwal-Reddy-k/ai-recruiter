namespace AIRecruiter.Infrastructure.Options;

public class AdzunaOptions
{
    public const string SectionName = "Adzuna";

    public string AppId { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
    public string Country { get; set; } = "gb";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(AppKey);
}

public class NominatimOptions
{
    public const string SectionName = "Nominatim";

    public string ContactEmail { get; set; } = "example@example.com";
}
