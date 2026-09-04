using AIRecruiter.Application.DTOs.Users;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class UserProfileServiceTests
{
    private static UserProfileService CreateSut(AppDbContext db) => new(db, TestServiceFactory.CreateAuditLog(db));

    private static async Task<User> SeedAsync(AppDbContext db)
    {
        var user = new User { FullName = "Neymar", Email = "messi10@example.com", Role = UserRole.Recruiter, PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task UpdateMyDetailsAsync_ValidName_UpdatesFullName()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedAsync(db);
        var sut = CreateSut(db);

        var result = await sut.UpdateMyDetailsAsync(user.Id, new UpdateUserDetailsRequest("Messi", null));

        Assert.Equal("Messi", result.FullName);
        Assert.Equal("Messi", db.Users.First(u => u.Id == user.Id).FullName);
    }

    [Fact]
    public async Task UpdateMyDetailsAsync_NormalizesPhoneWithCountryCode()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedAsync(db);
        var sut = CreateSut(db);

        var result = await sut.UpdateMyDetailsAsync(user.Id, new UpdateUserDetailsRequest("Messi", "+91 98765 43210"));

        Assert.Equal("9876543210", result.PhoneNumber);
    }

    [Fact]
    public async Task UpdateMyDetailsAsync_InvalidPhone_ThrowsValidationWithFieldError()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedAsync(db);
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.UpdateMyDetailsAsync(user.Id, new UpdateUserDetailsRequest("Messi", "12345")));

        Assert.NotNull(ex.FieldErrors);
        Assert.True(ex.FieldErrors!.ContainsKey("phoneNumber"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("Name123")]
    public async Task UpdateMyDetailsAsync_InvalidFullName_ThrowsValidationWithFieldError(string fullName)
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedAsync(db);
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.UpdateMyDetailsAsync(user.Id, new UpdateUserDetailsRequest(fullName, null)));

        Assert.NotNull(ex.FieldErrors);
        Assert.True(ex.FieldErrors!.ContainsKey("fullName"));
    }

    [Fact]
    public async Task UpdateMyDetailsAsync_ClearingPhone_SetsPhoneToNull()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedAsync(db);
        user.PhoneNumber = "9876543210";
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var result = await sut.UpdateMyDetailsAsync(user.Id, new UpdateUserDetailsRequest("Messi", null));

        Assert.Null(result.PhoneNumber);
    }

    [Fact]
    public async Task GetMyDetailsAsync_UnknownUser_ThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetMyDetailsAsync(9999));
    }

    private static async Task<User> SeedWithPasswordAsync(AppDbContext db, string password)
    {
        var user = new User { FullName = "Neymar", Email = "messi10@example.com", Role = UserRole.Recruiter, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password) };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task ChangePasswordAsync_CorrectCurrentPassword_UpdatesHashAndRotatesStamp()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedWithPasswordAsync(db, "OldPassw0rd!");
        var originalStamp = user.SecurityStamp;
        var sut = CreateSut(db);

        await sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("OldPassw0rd!", "NewPassw0rd!", "NewPassw0rd!"));

        var reloaded = db.Users.First(u => u.Id == user.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPassw0rd!", reloaded.PasswordHash));
        Assert.NotEqual(originalStamp, reloaded.SecurityStamp);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedWithPasswordAsync(db, "OldPassw0rd!");
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("WrongPassword!", "NewPassw0rd!", "NewPassw0rd!")));

        Assert.True(ex.FieldErrors!.ContainsKey("currentPassword"));
    }

    [Fact]
    public async Task ChangePasswordAsync_MismatchedConfirm_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedWithPasswordAsync(db, "OldPassw0rd!");
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("OldPassw0rd!", "NewPassw0rd!", "Different1!")));

        Assert.True(ex.FieldErrors!.ContainsKey("confirmPassword"));
    }

    [Fact]
    public async Task ChangePasswordAsync_TooShort_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedWithPasswordAsync(db, "OldPassw0rd!");
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("OldPassw0rd!", "Sh0rt!", "Sh0rt!")));

        Assert.True(ex.FieldErrors!.ContainsKey("newPassword"));
    }

    [Fact]
    public async Task ChangePasswordAsync_SameAsCurrentPassword_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedWithPasswordAsync(db, "OldPassw0rd!");
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("OldPassw0rd!", "OldPassw0rd!", "OldPassw0rd!")));

        Assert.True(ex.FieldErrors!.ContainsKey("newPassword"));
    }

    [Fact]
    public async Task RequestAccountDeletionAsync_CorrectPassword_StartsGracePeriodWithoutDeactivating()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedWithPasswordAsync(db, "MyPassw0rd!");
        var originalStamp = user.SecurityStamp;
        var sut = CreateSut(db);

        var status = await sut.RequestAccountDeletionAsync(user.Id, new RequestAccountDeletionRequest("MyPassw0rd!"));

        var reloaded = db.Users.First(u => u.Id == user.Id);
        Assert.True(reloaded.IsActive);
        Assert.NotNull(reloaded.DeletionRequestedAt);
        Assert.Equal(originalStamp, reloaded.SecurityStamp);
        Assert.True(status.IsPending);
        Assert.Equal(14, status.DaysRemaining);
    }

    [Fact]
    public async Task RequestAccountDeletionAsync_WrongPassword_ThrowsValidationAndDoesNotSetDeletionFlag()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedWithPasswordAsync(db, "MyPassw0rd!");
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.RequestAccountDeletionAsync(user.Id, new RequestAccountDeletionRequest("WrongPassword!")));

        var reloaded = db.Users.First(u => u.Id == user.Id);
        Assert.True(reloaded.IsActive);
        Assert.Null(reloaded.DeletionRequestedAt);
    }

    [Fact]
    public async Task CancelAccountDeletionAsync_ClearsThePendingFlag()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedWithPasswordAsync(db, "MyPassw0rd!");
        var sut = CreateSut(db);
        await sut.RequestAccountDeletionAsync(user.Id, new RequestAccountDeletionRequest("MyPassw0rd!"));

        var status = await sut.CancelAccountDeletionAsync(user.Id);

        Assert.False(status.IsPending);
        Assert.Null(db.Users.First(u => u.Id == user.Id).DeletionRequestedAt);
    }
}
