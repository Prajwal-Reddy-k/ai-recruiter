namespace AIRecruiter.Infrastructure.Options;

public class ResumeStorageOptions
{
    public const string SectionName = "ResumeStorage";

    public string LocalRootPath { get; set; } = "App_Data/Resumes";
    public long MaxSizeBytes { get; set; } = 5 * 1024 * 1024;
}

public class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";

    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(CloudName)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(ApiSecret);
}
