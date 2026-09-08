using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

/// <summary>Long-lived, single-use refresh token — same CSPRNG-generate/hash-at-rest pattern as
/// PasswordResetCode's reset token (only the SHA-256 hash of the raw value is ever stored).
/// Rotation forms a chain via ReplacedByTokenId; reuse of an already-rotated token is theft
/// detection and revokes the whole chain (see RefreshTokenService.RedeemAsync).</summary>
public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Also configured as an EF Core concurrency token (see AppDbContext) — a race
    /// between two redemption attempts of the same token means both read RevokedAtUtc as null,
    /// but only the first SaveChangesAsync that sets it wins; the loser hits
    /// DbUpdateConcurrencyException, which RefreshTokenService.RedeemAsync treats as reuse of an
    /// already-rotated token (theft detection) rather than issuing a second valid child.</summary>
    public DateTime? RevokedAtUtc { get; set; }
    public int? ReplacedByTokenId { get; set; }
    public string? CreatedByIp { get; set; }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
}
