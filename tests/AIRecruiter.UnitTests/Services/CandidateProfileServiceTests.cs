using System.Text;
using AIRecruiter.Application.DTOs.Candidates;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Options;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;
using Microsoft.Extensions.Options;
using Moq;

namespace AIRecruiter.UnitTests.Services;

/// <summary>Echoes the input back unchanged, tagged as a processed JPEG — avoids depending on
/// ImageSharp's real decode/encode behavior (a well-tested third-party library) in these tests,
/// which only need to verify the service wires validation/storage/ownership correctly.</summary>
public class FakeAvatarImageProcessor : IAvatarImageProcessor
{
    public Task<AvatarProcessResult> ProcessAsync(Stream content, CancellationToken ct = default)
    {
        var output = new MemoryStream();
        content.CopyTo(output);
        output.Position = 0;
        return Task.FromResult(new AvatarProcessResult(output, "image/jpeg", ".jpg"));
    }
}

/// <summary>Minimal in-memory <see cref="IResumeStorage"/> fake so tests can assert on what was
/// actually persisted/deleted without touching disk.</summary>
public class FakeFileStorage : IResumeStorage
{
    private readonly Dictionary<string, byte[]> _files = new();
    public List<string> DeletedKeys { get; } = new();

    public Task<ResumeStorageResult> SaveAsync(int candidateProfileId, Stream content, string originalFileName, string contentType, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        content.CopyTo(ms);
        var bytes = ms.ToArray();
        var key = $"{Guid.NewGuid():N}{Path.GetExtension(originalFileName)}";
        _files[key] = bytes;
        return Task.FromResult(new ResumeStorageResult(key, bytes.Length));
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        if (!_files.TryGetValue(storageKey, out var bytes))
        {
            throw new FileNotFoundException();
        }
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        DeletedKeys.Add(storageKey);
        _files.Remove(storageKey);
        return Task.CompletedTask;
    }
}

public class CandidateProfileServiceTests
{
    private static CandidateProfileService CreateSut(AppDbContext db, FakeFileStorage storage)
    {
        var extractorFactory = new Mock<IResumeTextExtractorFactory>();
        return new CandidateProfileService(
            db,
            storage,
            extractorFactory.Object,
            new ResumeFileValidator(),
            Options.Create(new ResumeStorageOptions()),
            TestServiceFactory.CreateLocationValidator(),
            new CandidateProfileValidator(),
            new ImageFileValidator(),
            new FakeAvatarImageProcessor(),
            Options.Create(new AvatarOptions()));
    }

    private static async Task<User> SeedCandidateAsync(AppDbContext db, string email)
    {
        var user = new User { FullName = "Test Candidate", Email = email, Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static readonly byte[] JpegHeader = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };

    private static MemoryStream FakeJpegStream() => new(JpegHeader.Concat(Encoding.UTF8.GetBytes("fake jpeg body")).ToArray());

    [Fact]
    public async Task UploadAvatarAsync_ValidJpeg_StoresAndReturnsAvatarUrl()
    {
        var db = TestDbContextFactory.Create();
        var storage = new FakeFileStorage();
        var sut = CreateSut(db, storage);
        var user = await SeedCandidateAsync(db, "candidate1@example.com");

        using var stream = FakeJpegStream();
        var dto = await sut.UploadAvatarAsync(user.Id, stream, "photo.jpg", "image/jpeg", stream.Length);

        Assert.NotNull(dto.AvatarUrl);
        Assert.Contains($"/candidates/{dto.Id}/avatar", dto.AvatarUrl);
    }

    [Fact]
    public async Task UploadAvatarAsync_DisallowedExtension_ThrowsValidationException()
    {
        var db = TestDbContextFactory.Create();
        var storage = new FakeFileStorage();
        var sut = CreateSut(db, storage);
        var user = await SeedCandidateAsync(db, "candidate2@example.com");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not an image"));

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.UploadAvatarAsync(user.Id, stream, "malware.exe", "application/octet-stream", stream.Length));
    }

    [Fact]
    public async Task UploadAvatarAsync_OversizedFile_ThrowsValidationException()
    {
        var db = TestDbContextFactory.Create();
        var storage = new FakeFileStorage();
        var sut = CreateSut(db, storage);
        var user = await SeedCandidateAsync(db, "candidate3@example.com");

        using var stream = FakeJpegStream();
        const long oversized = 6 * 1024 * 1024;

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.UploadAvatarAsync(user.Id, stream, "photo.jpg", "image/jpeg", oversized));
    }

    [Fact]
    public async Task UploadAvatarAsync_MismatchedSignature_ThrowsValidationException()
    {
        var db = TestDbContextFactory.Create();
        var storage = new FakeFileStorage();
        var sut = CreateSut(db, storage);
        var user = await SeedCandidateAsync(db, "candidate4@example.com");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("this is not really a jpeg"));

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.UploadAvatarAsync(user.Id, stream, "photo.jpg", "image/jpeg", stream.Length));
    }

    [Fact]
    public async Task UploadAvatarAsync_ReplacingExistingAvatar_DeletesPreviousFile()
    {
        var db = TestDbContextFactory.Create();
        var storage = new FakeFileStorage();
        var sut = CreateSut(db, storage);
        var user = await SeedCandidateAsync(db, "candidate5@example.com");

        using var first = FakeJpegStream();
        var firstDto = await sut.UploadAvatarAsync(user.Id, first, "photo1.jpg", "image/jpeg", first.Length);

        using var second = FakeJpegStream();
        await sut.UploadAvatarAsync(user.Id, second, "photo2.jpg", "image/jpeg", second.Length);

        Assert.Single(storage.DeletedKeys);
        Assert.NotEmpty(firstDto.AvatarUrl!);
    }

    [Fact]
    public async Task UploadAvatarAsync_NeverAffectsAnotherUsersProfile()
    {
        var db = TestDbContextFactory.Create();
        var storage = new FakeFileStorage();
        var sut = CreateSut(db, storage);
        var userA = await SeedCandidateAsync(db, "usera@example.com");
        var userB = await SeedCandidateAsync(db, "userb@example.com");

        using var streamA = FakeJpegStream();
        var dtoA = await sut.UploadAvatarAsync(userA.Id, streamA, "photo.jpg", "image/jpeg", streamA.Length);

        var profileB = await sut.GetMyProfileAsync(userB.Id);

        Assert.Null(profileB.AvatarUrl);
        Assert.NotEqual(dtoA.Id, profileB.Id);
    }

    [Fact]
    public async Task RemoveAvatarAsync_ClearsAvatarAndDeletesFile()
    {
        var db = TestDbContextFactory.Create();
        var storage = new FakeFileStorage();
        var sut = CreateSut(db, storage);
        var user = await SeedCandidateAsync(db, "candidate6@example.com");

        using var stream = FakeJpegStream();
        await sut.UploadAvatarAsync(user.Id, stream, "photo.jpg", "image/jpeg", stream.Length);

        var afterRemove = await sut.RemoveAvatarAsync(user.Id);

        Assert.Null(afterRemove.AvatarUrl);
        Assert.Single(storage.DeletedKeys);
    }

    [Fact]
    public async Task OpenAvatarAsync_NoAvatarUploaded_ThrowsNotFoundException()
    {
        var db = TestDbContextFactory.Create();
        var storage = new FakeFileStorage();
        var sut = CreateSut(db, storage);
        var user = await SeedCandidateAsync(db, "candidate7@example.com");
        var profile = await sut.GetMyProfileAsync(user.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.OpenAvatarAsync(profile.Id));
    }

    [Fact]
    public async Task OpenAvatarAsync_UnknownProfileId_ThrowsNotFoundException()
    {
        var db = TestDbContextFactory.Create();
        var storage = new FakeFileStorage();
        var sut = CreateSut(db, storage);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.OpenAvatarAsync(999));
    }

    [Fact]
    public async Task OpenAvatarAsync_WithAvatar_ReturnsContentForAnyCaller()
    {
        var db = TestDbContextFactory.Create();
        var storage = new FakeFileStorage();
        var sut = CreateSut(db, storage);
        var user = await SeedCandidateAsync(db, "candidate8@example.com");

        using var stream = FakeJpegStream();
        var dto = await sut.UploadAvatarAsync(user.Id, stream, "photo.jpg", "image/jpeg", stream.Length);

        var (content, contentType) = await sut.OpenAvatarAsync(dto.Id);

        Assert.Equal("image/jpeg", contentType);
        Assert.True(content.Length > 0);
    }

    [Fact]
    public async Task UpsertMyProfileAsync_InvalidHeadline_ThrowsValidationExceptionWithFieldErrors()
    {
        var db = TestDbContextFactory.Create();
        var storage = new FakeFileStorage();
        var sut = CreateSut(db, storage);
        var user = await SeedCandidateAsync(db, "candidate9@example.com");

        var request = new UpsertCandidateProfileRequest(
            Headline: "Hi", Summary: null, Education: null, GraduationYear: null,
            ExperienceSummary: null, TotalExperienceYears: null,
            City: "Bengaluru", State: "Karnataka", Locality: null,
            CurrentSalary: null, ExpectedSalary: null, SkillsCsv: "C#",
            Phone: null, LinkedInUrl: null, GithubUrl: null, PortfolioUrl: null,
            AvailabilityStatus: AvailabilityStatus.OpenToOpportunities,
            PreferredJobTypesCsv: null, PreferredLocationsCsv: null, RemotePreference: null,
            ExpectedSalaryMin: null, ExpectedSalaryMax: null, NoticePeriodDays: null,
            PreferredRolesCsv: null, ProfileVisibility: ProfileVisibility.VisibleAfterApplying);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => sut.UpsertMyProfileAsync(user.Id, request));
        Assert.NotNull(ex.FieldErrors);
        Assert.True(ex.FieldErrors!.ContainsKey("headline"));
    }

    [Fact]
    public async Task UpsertMyProfileAsync_ValidRequest_NormalizesPhoneAndDedupesSkills()
    {
        var db = TestDbContextFactory.Create();
        var storage = new FakeFileStorage();
        var sut = CreateSut(db, storage);
        var user = await SeedCandidateAsync(db, "candidate10@example.com");

        var request = new UpsertCandidateProfileRequest(
            Headline: "Backend Engineer", Summary: null, Education: null, GraduationYear: null,
            ExperienceSummary: null, TotalExperienceYears: 5,
            City: "Bengaluru", State: "Karnataka", Locality: null,
            CurrentSalary: null, ExpectedSalary: null, SkillsCsv: "C#, c#, SQL Server",
            Phone: "+91 98765 43210", LinkedInUrl: null, GithubUrl: null, PortfolioUrl: null,
            AvailabilityStatus: AvailabilityStatus.OpenToOpportunities,
            PreferredJobTypesCsv: null, PreferredLocationsCsv: null, RemotePreference: null,
            ExpectedSalaryMin: null, ExpectedSalaryMax: null, NoticePeriodDays: null,
            PreferredRolesCsv: null, ProfileVisibility: ProfileVisibility.VisibleAfterApplying);

        // Duplicate skills are a validation error, not silently accepted — fix the request first.
        var deduped = request with { SkillsCsv = "C#, SQL Server" };
        var dto = await sut.UpsertMyProfileAsync(user.Id, deduped);

        Assert.Equal("9876543210", dto.Phone);
        Assert.Equal("C#, SQL Server", dto.SkillsCsv);
    }

    private static UpsertCandidateProfileRequest PublicShareableRequest() => new(
        Headline: "Backend Engineer", Summary: null, Education: null, GraduationYear: null,
        ExperienceSummary: null, TotalExperienceYears: null,
        City: null, State: null, Locality: null,
        CurrentSalary: null, ExpectedSalary: null, SkillsCsv: "C#",
        Phone: null, LinkedInUrl: null, GithubUrl: null, PortfolioUrl: null,
        AvailabilityStatus: AvailabilityStatus.OpenToOpportunities,
        PreferredJobTypesCsv: null, PreferredLocationsCsv: null, RemotePreference: null,
        ExpectedSalaryMin: null, ExpectedSalaryMax: null, NoticePeriodDays: null,
        PreferredRolesCsv: null, ProfileVisibility: ProfileVisibility.PublicShareable);

    [Fact]
    public async Task UpsertMyProfile_EnablingPublicShareableFirstTime_GeneratesSlug()
    {
        var db = TestDbContextFactory.Create();
        var storage = new FakeFileStorage();
        var sut = CreateSut(db, storage);
        var user = await SeedCandidateAsync(db, "public1@example.com");

        var dto = await sut.UpsertMyProfileAsync(user.Id, PublicShareableRequest());

        var profile = db.CandidateProfiles.Single(c => c.UserId == user.Id);
        Assert.NotNull(profile.PublicProfileSlug);
        Assert.StartsWith("test-candidate-", profile.PublicProfileSlug);
    }

    [Fact]
    public async Task UpsertMyProfile_TogglingVisibilityOffThenBackToPublicShareable_KeepsSameSlug()
    {
        var db = TestDbContextFactory.Create();
        var storage = new FakeFileStorage();
        var sut = CreateSut(db, storage);
        var user = await SeedCandidateAsync(db, "public2@example.com");

        await sut.UpsertMyProfileAsync(user.Id, PublicShareableRequest());
        var firstSlug = db.CandidateProfiles.Single(c => c.UserId == user.Id).PublicProfileSlug;

        await sut.UpsertMyProfileAsync(user.Id, PublicShareableRequest() with { ProfileVisibility = ProfileVisibility.Private });
        await sut.UpsertMyProfileAsync(user.Id, PublicShareableRequest());
        var secondSlug = db.CandidateProfiles.Single(c => c.UserId == user.Id).PublicProfileSlug;

        Assert.Equal(firstSlug, secondSlug);
    }

    [Fact]
    public async Task UpsertMyProfile_TwoDifferentCandidatesEnablingPublicShareable_GetDifferentSlugs()
    {
        var db = TestDbContextFactory.Create();
        var storage = new FakeFileStorage();
        var sut = CreateSut(db, storage);
        var userA = await SeedCandidateAsync(db, "public3a@example.com");
        var userB = await SeedCandidateAsync(db, "public3b@example.com");

        await sut.UpsertMyProfileAsync(userA.Id, PublicShareableRequest());
        await sut.UpsertMyProfileAsync(userB.Id, PublicShareableRequest());

        var slugA = db.CandidateProfiles.Single(c => c.UserId == userA.Id).PublicProfileSlug;
        var slugB = db.CandidateProfiles.Single(c => c.UserId == userB.Id).PublicProfileSlug;

        Assert.NotEqual(slugA, slugB);
    }
}
