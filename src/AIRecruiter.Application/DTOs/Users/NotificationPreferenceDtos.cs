namespace AIRecruiter.Application.DTOs.Users;

public record NotificationPreferenceDto(bool MessagesEnabled, bool ApplicationsEnabled, bool InterviewsEnabled, bool InvitationsEnabled);

public record UpdateNotificationPreferenceRequest(bool MessagesEnabled, bool ApplicationsEnabled, bool InterviewsEnabled, bool InvitationsEnabled);
