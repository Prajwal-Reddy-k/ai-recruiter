using System.Text;
using AIRecruiter.Infrastructure.Options;
using AIRecruiter.Infrastructure.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;

namespace AIRecruiter.UnitTests.Storage;

public class LocalResumeStorageTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly LocalResumeStorage _sut;

    public LocalResumeStorageTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "airecruiter-tests-" + Guid.NewGuid());

        var env = new Mock<IHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(_tempRoot);

        var options = Options.Create(new ResumeStorageOptions { LocalRootPath = "resumes" });

        _sut = new LocalResumeStorage(env.Object, options);
    }

    [Fact]
    public async Task SaveAsync_ThenOpenReadAsync_RoundTripsContent()
    {
        var content = Encoding.UTF8.GetBytes("fake pdf content");
        using var stream = new MemoryStream(content);

        var result = await _sut.SaveAsync(1, stream, "resume.pdf", "application/pdf");

        await using var readStream = await _sut.OpenReadAsync(result.StorageKey);
        using var reader = new StreamReader(readStream);
        var text = await reader.ReadToEndAsync();

        Assert.Equal("fake pdf content", text);
        Assert.Equal(content.Length, result.SizeBytes);
    }

    [Fact]
    public async Task SaveAsync_GeneratesKeyDifferentFromOriginalFileName()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        var result = await _sut.SaveAsync(1, stream, "my-secret-resume.pdf", "application/pdf");

        Assert.DoesNotContain("my-secret-resume", result.StorageKey);
        Assert.EndsWith(".pdf", result.StorageKey);
    }

    [Fact]
    public async Task DeleteAsync_RemovesFile()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        var result = await _sut.SaveAsync(1, stream, "resume.pdf", "application/pdf");

        await _sut.DeleteAsync(result.StorageKey);

        await Assert.ThrowsAsync<FileNotFoundException>(() => _sut.OpenReadAsync(result.StorageKey));
    }

    [Fact]
    public async Task OpenReadAsync_RejectsPathTraversalAttempt()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.OpenReadAsync("../../secrets.txt"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
