using AIRecruiter.Application.DTOs.Auth;

namespace AIRecruiter.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress = null, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress = null, CancellationToken ct = default);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, string ipAddress, CancellationToken ct = default);
    Task<VerifyResetCodeResponse> VerifyResetCodeAsync(VerifyResetCodeRequest request, string ipAddress, CancellationToken ct = default);
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task<RefreshTokenResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress = null, CancellationToken ct = default);
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default);
}
