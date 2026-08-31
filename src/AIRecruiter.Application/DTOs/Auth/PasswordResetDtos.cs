using System.ComponentModel.DataAnnotations;

namespace AIRecruiter.Application.DTOs.Auth;

public record ForgotPasswordRequest(
    [Required, EmailAddress, StringLength(256)]
    string Email);

public record ForgotPasswordResponse(string Message);

public record VerifyResetCodeRequest(
    [Required, EmailAddress, StringLength(256)]
    string Email,
    [Required, StringLength(6, MinimumLength = 6)]
    string Code);

public record VerifyResetCodeResponse(string ResetToken, DateTime ExpiresAtUtc);

public record ResetPasswordRequest(
    [Required]
    string ResetToken,
    [Required, MinLength(6), StringLength(200)]
    string NewPassword,
    [Required]
    string ConfirmPassword);

public record ResetPasswordResponse(string Message);
