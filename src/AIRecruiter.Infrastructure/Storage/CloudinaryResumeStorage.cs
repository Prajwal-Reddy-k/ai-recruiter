using AIRecruiter.Application.Interfaces;
using AIRecruiter.Infrastructure.Options;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace AIRecruiter.Infrastructure.Storage;

/// <summary>
/// Optional resume storage backed by Cloudinary. Only registered when
/// Cloudinary:CloudName/ApiKey/ApiSecret are all present in server-side configuration.
/// Files are uploaded as authenticated/private raw resources; every read goes through a
/// short-lived signed URL fetched server-side and streamed back — the browser never sees
/// Cloudinary URLs or credentials.
/// </summary>
public class CloudinaryResumeStorage : IResumeStorage
{
    private readonly Cloudinary _cloudinary;
    private readonly HttpClient _httpClient;

    public CloudinaryResumeStorage(IOptions<CloudinaryOptions> options, IHttpClientFactory httpClientFactory)
    {
        var settings = options.Value;
        var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
        _cloudinary = new Cloudinary(account);
        _httpClient = httpClientFactory.CreateClient("CloudinaryDownload");
    }

    public async Task<ResumeStorageResult> SaveAsync(int candidateProfileId, Stream content, string originalFileName, string contentType, CancellationToken ct = default)
    {
        var publicId = $"resumes/{candidateProfileId}/{Guid.NewGuid():N}";

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(originalFileName, content),
            PublicId = publicId,
            Type = "authenticated",
            Overwrite = false,
        };

        var result = await _cloudinary.UploadAsync(uploadParams, "raw", ct);

        if (result.Error is not null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");
        }

        return new ResumeStorageResult(publicId, result.Bytes);
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var url = _cloudinary.Api.UrlImgUp
            .ResourceType("raw")
            .Secure(true)
            .Signed(true)
            .Type("authenticated")
            .BuildUrl(storageKey);

        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(ct);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var deleteParams = new DeletionParams(storageKey) { ResourceType = ResourceType.Raw, Type = "authenticated" };
        return _cloudinary.DestroyAsync(deleteParams);
    }
}
