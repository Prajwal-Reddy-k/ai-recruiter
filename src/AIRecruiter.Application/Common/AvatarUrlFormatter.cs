namespace AIRecruiter.Application.Common;

public static class AvatarUrlFormatter
{
    /// <summary>Path (relative to the API root, no leading "/api") for the public
    /// avatar-serving endpoint, or null if the candidate has no avatar uploaded. The
    /// frontend prefixes this with its configured API base URL to build an &lt;img src&gt;.
    /// A "v" query param carrying the storage key is appended so the URL itself changes
    /// whenever the photo is replaced — otherwise the endpoint's Cache-Control: max-age=300
    /// header (and the browser's own cache) would keep serving the previous image under the
    /// same unchanged URL.</summary>
    public static string? Format(int candidateProfileId, string? avatarStorageKey) =>
        string.IsNullOrEmpty(avatarStorageKey)
            ? null
            : $"/candidates/{candidateProfileId}/avatar?v={Uri.EscapeDataString(avatarStorageKey)}";
}
