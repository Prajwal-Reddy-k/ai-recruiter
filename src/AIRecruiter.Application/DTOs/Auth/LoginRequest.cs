using System.ComponentModel.DataAnnotations;

namespace AIRecruiter.Application.DTOs.Auth;

public record LoginRequest(
    [Required, EmailAddress, StringLength(256)]
    string Email,
    [Required]
    string Password);
