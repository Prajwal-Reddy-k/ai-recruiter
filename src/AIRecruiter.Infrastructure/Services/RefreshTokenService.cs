using System.Security.Cryptography;
using System.Text;
using AIRecruiter.Application.DTOs.Auth;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(30);

    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IAuditLogService _auditLog;

    public RefreshTokenService(AppDbContext db, ITokenService tokenService, IAuditLogService auditLog)
    {
        _db = db;
        _tokenService = tokenService;
        _auditLog = auditLog;
    }

    public async Task<string> IssueAsync(User user, string? ip, CancellationToken ct = default)
    {
        var rawToken = GenerateRawToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAtUtc = DateTime.UtcNow.Add(TokenLifetime),
            CreatedByIp = ip,
        });
        await _db.SaveChangesAsync(ct);
        return rawToken;
    }

    public async Task<RefreshTokenResponse> RedeemAsync(string rawToken, string? ip, CancellationToken ct = default)
    {
        const string genericError = "This session is no longer valid — please sign in again.";

        var tokenHash = HashToken(rawToken);
        var existing = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (existing is null)
        {
            throw new UnauthorizedException(genericError);
        }

        if (existing.RevokedAtUtc is not null)
        {
            // Theft detection: this token was already redeemed once (or explicitly revoked) —
            // presenting it again means either a race or a stolen/replayed token. Either way,
            // the whole chain for this user is revoked so no descendant token remains valid.
            await RevokeAllForUserAsync(existing.UserId, ct);
            await _auditLog.LogAsync(existing.UserId, existing.User.Role.ToString(), "RefreshTokenReuseDetected", "User", existing.UserId, null, ct);
            throw new UnauthorizedException(genericError);
        }

        if (existing.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new UnauthorizedException(genericError);
        }

        var user = existing.User;

        // Revoke the presented token FIRST, alone, before creating anything new — a concurrent
        // redemption attempt racing on this same SaveChangesAsync call is the only place the
        // RevokedAtUtc concurrency token can catch the loser, and it must lose before it ever
        // creates a child token (never issuing two valid children for one presented token).
        existing.RevokedAtUtc = DateTime.UtcNow;
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent redemption of the same token won the race — treat this attempt as
            // reuse-of-an-already-rotated-token (theft detection), never issue a second child.
            await RevokeAllForUserAsync(existing.UserId, ct);
            throw new UnauthorizedException(genericError);
        }

        var newRawToken = GenerateRawToken();
        var child = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(newRawToken),
            ExpiresAtUtc = DateTime.UtcNow.Add(TokenLifetime),
            CreatedByIp = ip,
        };
        _db.RefreshTokens.Add(child);
        await _db.SaveChangesAsync(ct);

        existing.ReplacedByTokenId = child.Id;
        await _db.SaveChangesAsync(ct);

        var (accessToken, expiresAt) = _tokenService.GenerateToken(user);
        await _auditLog.LogAsync(user.Id, user.Role.ToString(), "TokenRefreshed", "User", user.Id, null, ct);
        return new RefreshTokenResponse(accessToken, expiresAt, newRawToken);
    }

    public async Task RevokeAsync(string rawToken, CancellationToken ct = default)
    {
        var tokenHash = HashToken(rawToken);
        var existing = await _db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
        if (existing is null || existing.RevokedAtUtc is not null) return;

        existing.RevokedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _auditLog.LogAsync(existing.UserId, existing.User.Role.ToString(), "LogoutSucceeded", "User", existing.UserId, null, ct);
    }

    public async Task RevokeAllForUserAsync(int userId, CancellationToken ct = default)
    {
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(ct);

        if (active.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var token in active)
        {
            token.RevokedAtUtc = now;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // One of these rows (typically the token whose failed redemption led here) was
            // already revoked by a concurrent winner in the meantime — that's the desired end
            // state either way, so this is not an error worth surfacing.
        }
    }

    private static string GenerateRawToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
