using System.Text.RegularExpressions;
using AIRecruiter.Application.DTOs.Users;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class UserProfileService : IUserProfileService
{
    private static readonly Regex IndianMobileRegex = new("^[6-9][0-9]{9}$", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;

    public UserProfileService(AppDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<UserDetailsDto> GetMyDetailsAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User not found.");
        return ToDto(user);
    }

    public async Task<UserDetailsDto> UpdateMyDetailsAsync(int userId, UpdateUserDetailsRequest request, CancellationToken ct = default)
    {
        if (!CandidateProfileValidator.IsValidFullName(request.FullName))
        {
            throw new ValidationException(
                "Enter a valid name (2-100 characters, letters and spaces only).",
                new Dictionary<string, string> { ["fullName"] = "Enter a valid name (2-100 characters, letters and spaces only)." });
        }

        string? normalizedPhone = null;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            normalizedPhone = NormalizePhone(request.PhoneNumber);
            if (normalizedPhone is null)
            {
                throw new ValidationException(
                    "Enter a valid 10-digit Indian mobile number.",
                    new Dictionary<string, string> { ["phoneNumber"] = "Enter a valid 10-digit Indian mobile number." });
            }
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User not found.");

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = normalizedPhone;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(userId, user.Role.ToString(), "UserDetailsUpdated", "User", user.Id, null, ct);

        return ToDto(user);
    }

    private static UserDetailsDto ToDto(User user) => new(user.Id, user.FullName, user.Email, user.PhoneNumber, user.Role.ToString());

    /// <summary>Strips a leading country code / trunk prefix and formatting characters,
    /// leaving a bare 10-digit number for storage — mirrors CandidateProfileValidator's
    /// normalization so the same phone number always ends up stored the same way.</summary>
    private static string? NormalizePhone(string phone)
    {
        var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

        var bare = digitsOnly.Length switch
        {
            10 => digitsOnly,
            11 when digitsOnly.StartsWith('0') => digitsOnly[1..],
            12 when digitsOnly.StartsWith("91") => digitsOnly[2..],
            13 when digitsOnly.StartsWith("091") => digitsOnly[3..],
            _ => null,
        };

        return bare is not null && IndianMobileRegex.IsMatch(bare) ? bare : null;
    }
}
