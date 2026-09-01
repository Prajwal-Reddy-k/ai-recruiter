using AIRecruiter.Application.DTOs.Team;

namespace AIRecruiter.Application.Interfaces;

public interface ITeamService
{
    Task<IReadOnlyList<TeamMemberDto>> GetMyCompanyTeamAsync(int ownerUserId, CancellationToken ct = default);
    Task<TeamMemberDto> AddMemberAsync(int ownerUserId, AddTeamMemberRequest request, CancellationToken ct = default);
    Task<TeamMemberDto> UpdateRoleAsync(int ownerUserId, int recruiterProfileId, UpdateTeamMemberRoleRequest request, CancellationToken ct = default);
    Task RemoveMemberAsync(int ownerUserId, int recruiterProfileId, CancellationToken ct = default);

    Task<IReadOnlyList<JobAssignmentDto>> GetJobAssignmentsAsync(int ownerUserId, int jobId, CancellationToken ct = default);
    Task AssignToJobAsync(int ownerUserId, int jobId, int recruiterProfileId, CancellationToken ct = default);
    Task UnassignFromJobAsync(int ownerUserId, int jobId, int recruiterProfileId, CancellationToken ct = default);

    Task<IReadOnlyList<InterviewAssignmentDto>> GetInterviewAssignmentsAsync(int ownerUserId, int interviewId, CancellationToken ct = default);
    Task AssignToInterviewAsync(int ownerUserId, int interviewId, int recruiterProfileId, CancellationToken ct = default);
    Task UnassignFromInterviewAsync(int ownerUserId, int interviewId, int recruiterProfileId, CancellationToken ct = default);
}
