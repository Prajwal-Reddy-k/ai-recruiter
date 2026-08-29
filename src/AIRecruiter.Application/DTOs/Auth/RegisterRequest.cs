using System.ComponentModel.DataAnnotations;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Application.DTOs.Auth;

public record RegisterRequest(
    [Required, StringLength(200, MinimumLength = 2)]
    string FullName,
    [Required, EmailAddress, StringLength(256)]
    string Email,
    [Required, MinLength(6), StringLength(200)]
    string Password,
    [Required, EnumDataType(typeof(UserRole))]
    UserRole Role);
