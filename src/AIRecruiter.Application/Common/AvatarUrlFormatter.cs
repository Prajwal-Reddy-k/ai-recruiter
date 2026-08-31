namespace AIRecruiter.Application.Common;

public static class AvatarUrlFormatter
{
    /// <summary>Path (relative to the API root, no leading "/api") for the public
    /// avatar-serving endpoint, or null if the candidate has no avatar uploaded. The
    /// frontend prefixes this with its configured API base URL to build an &lt;img src&gt;.</summary>
    public static string? Format(int candidateProfileId, string? avatarStorageKey) =>
        string.IsNullOrEmpty(avatarStorageKey) ? null : $"/candidates/{candidateProfileId}/avatar";
}
