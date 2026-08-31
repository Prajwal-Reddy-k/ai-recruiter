namespace AIRecruiter.Application.Interfaces;

public record ResumeStorageResult(string StorageKey, long SizeBytes);

/// <summary>
/// Provider-agnostic file storage for a candidate's uploaded files — resumes and profile
/// photos alike, keyed by candidate profile id and an opaque storage key. The default
/// implementation is local-disk based and requires no external service or paid account.
/// Files are never served directly — downloads always go through an authorized (or, for
/// avatars, a deliberately public) backend endpoint.
/// </summary>
public interface IResumeStorage
{
    Task<ResumeStorageResult> SaveAsync(int candidateProfileId, Stream content, string originalFileName, string contentType, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
