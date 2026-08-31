namespace AIRecruiter.Application.Interfaces;

public record AvatarProcessResult(Stream Content, string ContentType, string Extension);

/// <summary>
/// Resizes/re-encodes an uploaded profile-photo image server-side so stored avatars are
/// bounded in dimensions and file size regardless of what was uploaded.
/// </summary>
public interface IAvatarImageProcessor
{
    Task<AvatarProcessResult> ProcessAsync(Stream content, CancellationToken ct = default);
}
