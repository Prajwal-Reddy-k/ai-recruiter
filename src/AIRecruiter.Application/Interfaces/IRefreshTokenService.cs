using AIRecruiter.Application.DTOs.Auth;
using AIRecruiter.Domain.Entities;

namespace AIRecruiter.Application.Interfaces;

public interface IRefreshTokenService
{
    /// <summary>Issues a new refresh token for a freshly-authenticated user (login/register).
    /// Returns the raw token — only its hash is ever persisted.</summary>
    Task<string> IssueAsync(User user, string? ip, CancellationToken ct = default);

    /// <summary>Atomically rotates a valid refresh token: revokes it, issues a new access token
    /// + child refresh token. Throws UnauthorizedException if the token is unknown, expired, or
    /// already revoked — reuse of an already-rotated token revokes the entire chain (theft
    /// detection) before throwing.</summary>
    Task<RefreshTokenResponse> RedeemAsync(string rawToken, string? ip, CancellationToken ct = default);

    /// <summary>Revokes a single token (logout). No-ops if the token is unknown/already revoked.</summary>
    Task RevokeAsync(string rawToken, CancellationToken ct = default);

    /// <summary>Revokes every active refresh token for a user — called alongside SecurityStamp
    /// rotation on password reset, password change, and admin suspension.</summary>
    Task RevokeAllForUserAsync(int userId, CancellationToken ct = default);
}
