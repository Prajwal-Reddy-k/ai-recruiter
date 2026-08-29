using AIRecruiter.Application.DTOs.Auth;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Options;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;
using Microsoft.Extensions.Options;

namespace AIRecruiter.UnitTests.Services;

public class AuthServiceTests
{
    private static AuthService CreateSut(AppDbContext db)
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Secret = "unit-test-secret-key-at-least-32-characters-long",
            Issuer = "AIRecruiter.Tests",
            Audience = "AIRecruiter.Tests",
            ExpiryMinutes = 60,
        });
        var tokenService = new TokenService(jwtOptions);
        return new AuthService(db, tokenService, TestServiceFactory.CreateAuditLog(db));
    }

    [Fact]
    public async Task RegisterAsync_NormalizesEmailCasingAndWhitespace()
    {
        using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);

        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "  Casey@Example.COM  ", "Passw0rd!", UserRole.Candidate));

        var stored = Assert.Single(db.Users);
        Assert.Equal("casey@example.com", stored.Email);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmailDifferentCasing_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));

        await Assert.ThrowsAsync<ConflictException>(
            () => sut.RegisterAsync(new RegisterRequest("Casey Duplicate", "CASEY@EXAMPLE.COM", "Passw0rd!", UserRole.Candidate)));
    }

    [Fact]
    public async Task RegisterAsync_AdminRole_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.RegisterAsync(new RegisterRequest("Wannabe Admin", "admin@example.com", "Passw0rd!", UserRole.Admin)));
    }

    [Fact]
    public async Task RegisterAsync_NeverStoresPlainTextPassword()
    {
        using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);

        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));

        var stored = Assert.Single(db.Users);
        Assert.NotEqual("Passw0rd!", stored.PasswordHash);
        Assert.StartsWith("$2", stored.PasswordHash); // BCrypt hash prefix
    }

    [Fact]
    public async Task LoginAsync_CaseInsensitiveEmail_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));

        var result = await sut.LoginAsync(new LoginRequest("CASEY@example.com", "Passw0rd!"));

        Assert.Equal("casey@example.com", result.Email);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsGenericUnauthorized()
    {
        using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => sut.LoginAsync(new LoginRequest("nobody@example.com", "whatever")));

        Assert.Equal("Invalid email or password.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsSameGenericMessageAsUnknownEmail()
    {
        using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => sut.LoginAsync(new LoginRequest("casey@example.com", "WrongPassword!")));

        Assert.Equal("Invalid email or password.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_DeactivatedAccount_ThrowsSameGenericMessage()
    {
        using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));

        var user = Assert.Single(db.Users);
        user.IsActive = false;
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => sut.LoginAsync(new LoginRequest("casey@example.com", "Passw0rd!")));

        Assert.Equal("Invalid email or password.", ex.Message);
    }
}
