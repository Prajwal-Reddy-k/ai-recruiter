using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Application.DTOs.Team;

public record TeamMemberDto(
    int RecruiterProfileId,
    int UserId,
    string FullName,
    string Email,
    string? Designation,
    string CompanyRole,
    DateTime JoinedAt);

public record AddTeamMemberRequest(string Email, CompanyRole Role);

public record UpdateTeamMemberRoleRequest(CompanyRole Role);

public record JobAssignmentDto(int JobPostingId, int RecruiterProfileId, string RecruiterName, string CompanyRole, DateTime AssignedAt);

public record InterviewAssignmentDto(int InterviewId, int RecruiterProfileId, string RecruiterName, string CompanyRole, DateTime AssignedAt);
