using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.UnitTests.Services;

public class RefreshTokenServiceTests
{
    private static AppDbContext CreateDbSharing(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(name).Options;
        return new AppDbContext(options);
    }

    private static async Task<User> SeedUserAsync(AppDbContext db)
    {
        var user = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task IssueAsync_StoresOnlyHashNeverRawToken()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDbSharing(dbName);
        var user = await SeedUserAsync(db);
        var sut = TestServiceFactory.CreateRefreshTokens(db);

        var rawToken = await sut.IssueAsync(user, "1.2.3.4");

        var stored = Assert.Single(db.RefreshTokens);
        Assert.NotEqual(rawToken, stored.TokenHash);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task RedeemAsync_ValidToken_RotatesAndReturnsNewPair()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDbSharing(dbName);
        var user = await SeedUserAsync(db);
        var sut = TestServiceFactory.CreateRefreshTokens(db);
        var rawToken = await sut.IssueAsync(user, null);

        var result = await sut.RedeemAsync(rawToken, null);

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.NotEqual(rawToken, result.RefreshToken);
        Assert.Equal(2, db.RefreshTokens.Count());
        var original = db.RefreshTokens.OrderBy(t => t.Id).First();
        Assert.NotNull(original.RevokedAtUtc);
        Assert.NotNull(original.ReplacedByTokenId);
    }

    [Fact]
    public async Task RedeemAsync_ExpiredToken_ThrowsUnauthorized()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDbSharing(dbName);
        var user = await SeedUserAsync(db);
        var sut = TestServiceFactory.CreateRefreshTokens(db);
        var rawToken = await sut.IssueAsync(user, null);
        db.RefreshTokens.Single().ExpiresAtUtc = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.RedeemAsync(rawToken, null));
    }

    [Fact]
    public async Task RedeemAsync_RevokedToken_ThrowsUnauthorized()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDbSharing(dbName);
        var user = await SeedUserAsync(db);
        var sut = TestServiceFactory.CreateRefreshTokens(db);
        var rawToken = await sut.IssueAsync(user, null);
        await sut.RevokeAsync(rawToken);

        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.RedeemAsync(rawToken, null));
    }

    [Fact]
    public async Task RedeemAsync_ReuseOfRotatedToken_RevokesWholeChain()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDbSharing(dbName);
        var user = await SeedUserAsync(db);
        var sut = TestServiceFactory.CreateRefreshTokens(db);
        var rawToken = await sut.IssueAsync(user, null);

        var firstRotation = await sut.RedeemAsync(rawToken, null);

        // Reusing the original (now-rotated) token is theft detection.
        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.RedeemAsync(rawToken, null));

        // The child issued by the legitimate first rotation must now also be revoked.
        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.RedeemAsync(firstRotation.RefreshToken, null));

        Assert.All(db.RefreshTokens, t => Assert.NotNull(t.RevokedAtUtc));
    }

    [Fact]
    public async Task RevokeAllForUserAsync_RevokesEveryActiveToken()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDbSharing(dbName);
        var user = await SeedUserAsync(db);
        var sut = TestServiceFactory.CreateRefreshTokens(db);
        var token1 = await sut.IssueAsync(user, null);
        var token2 = await sut.IssueAsync(user, null);

        await sut.RevokeAllForUserAsync(user.Id);

        Assert.All(db.RefreshTokens, t => Assert.NotNull(t.RevokedAtUtc));
        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.RedeemAsync(token1, null));
        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.RedeemAsync(token2, null));
    }

    [Fact]
    public async Task RedeemAsync_ConcurrentRedemptionOfSameToken_OnlyOneSucceeds()
    {
        var dbName = Guid.NewGuid().ToString();
        using var seedDb = CreateDbSharing(dbName);
        var user = await SeedUserAsync(seedDb);
        var issuer = TestServiceFactory.CreateRefreshTokens(seedDb);
        var rawToken = await issuer.IssueAsync(user, null);

        using var dbA = CreateDbSharing(dbName);
        using var dbB = CreateDbSharing(dbName);
        var sutA = TestServiceFactory.CreateRefreshTokens(dbA);
        var sutB = TestServiceFactory.CreateRefreshTokens(dbB);

        // Both contexts load the same not-yet-revoked token before either commits, then race
        // to redeem it — simulating two concurrent requests presenting the same refresh token.
        await dbA.RefreshTokens.LoadAsync();
        await dbB.RefreshTokens.LoadAsync();

        var taskA = SafeRedeemAsync(sutA, rawToken);
        var taskB = SafeRedeemAsync(sutB, rawToken);
        var results = await Task.WhenAll(taskA, taskB);

        Assert.Single(results, r => r);

        using var verifyDb = CreateDbSharing(dbName);
        var activeCount = verifyDb.RefreshTokens.Count(t => t.RevokedAtUtc == null);
        Assert.Equal(1, activeCount); // exactly one valid child token survives the race
    }

    private static async Task<bool> SafeRedeemAsync(AIRecruiter.Application.Interfaces.IRefreshTokenService sut, string rawToken)
    {
        try
        {
            await sut.RedeemAsync(rawToken, null);
            return true;
        }
        catch (UnauthorizedException)
        {
            return false;
        }
    }
}
