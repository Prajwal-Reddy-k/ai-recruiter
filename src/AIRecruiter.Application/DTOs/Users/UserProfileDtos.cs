namespace AIRecruiter.Application.DTOs.Users;

public record UserDetailsDto(int UserId, string FullName, string Email, string? PhoneNumber, string Role);

public record UpdateUserDetailsRequest(string FullName, string? PhoneNumber);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

public record RequestAccountDeletionRequest(string Password);

public record AccountDeletionStatusDto(bool IsPending, DateTime? RequestedAtUtc, DateTime? ScheduledDeactivationAtUtc, int? DaysRemaining);

public record PrivacySummaryDto(string? ProfileVisibility, bool MessagesEnabled, bool ApplicationsEnabled, bool InterviewsEnabled, bool InvitationsEnabled, AccountDeletionStatusDto Deletion);

