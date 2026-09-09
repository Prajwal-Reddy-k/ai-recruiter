namespace AIRecruiter.Infrastructure.Options;

/// <summary>Same IsConfigured-gated optional-provider pattern as CloudinaryOptions/SmtpOptions
/// — absent (the default) means every distributed-cache-eligible feature keeps using its
/// existing in-memory implementation, unchanged.</summary>
public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);
}
