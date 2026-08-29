namespace AIRecruiter.Application.Interfaces;

public record ResumeStorageResult(string StorageKey, long SizeBytes);

/// <summary>
/// Provider-agnostic resume file storage. The default implementation is local-disk based
/// and requires no external service or paid account. Files are never served directly —
/// downloads always go through an authorized backend endpoint.
/// </summary>
public interface IResumeStorage
{
    Task<ResumeStorageResult> SaveAsync(int candidateProfileId, Stream content, string originalFileName, string contentType, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
