namespace AIRecruiter.Application.DTOs.Auth;

public record AuthResponse(
    int UserId,
    string FullName,
    string Email,
    string Role,
    string Token,
    DateTime ExpiresAt,
    string? AvatarUrl = null,
    string RefreshToken = "");

public record RefreshTokenRequest(string RefreshToken);

public record RefreshTokenResponse(string Token, DateTime ExpiresAt, string RefreshToken);
