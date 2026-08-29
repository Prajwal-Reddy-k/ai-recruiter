using AIRecruiter.Application.Interfaces;
using AIRecruiter.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AIRecruiter.Infrastructure.Storage;

/// <summary>
/// Default, zero-cost resume storage: files live on local disk outside any web-servable
/// root, named by a generated GUID so the original filename never becomes a path.
/// </summary>
public class LocalResumeStorage : IResumeStorage
{
    private readonly string _rootPath;

    public LocalResumeStorage(IHostEnvironment env, IOptions<ResumeStorageOptions> options)
    {
        _rootPath = Path.IsPathRooted(options.Value.LocalRootPath)
            ? options.Value.LocalRootPath
            : Path.Combine(env.ContentRootPath, options.Value.LocalRootPath);

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<ResumeStorageResult> SaveAsync(int candidateProfileId, Stream content, string originalFileName, string contentType, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(originalFileName);
        var storageKey = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_rootPath, storageKey);

        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream, ct);
        }

        var size = new FileInfo(fullPath).Length;
        return new ResumeStorageResult(storageKey, size);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = ResolveSafePath(storageKey);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = ResolveSafePath(storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    private string ResolveSafePath(string storageKey)
    {
        // storageKey is always a bare generated filename (no directory separators) —
        // reject anything else to prevent path traversal.
        if (storageKey.Contains('/') || storageKey.Contains('\\') || storageKey.Contains(".."))
        {
            throw new ArgumentException("Invalid storage key.", nameof(storageKey));
        }

        return Path.Combine(_rootPath, storageKey);
    }
}
