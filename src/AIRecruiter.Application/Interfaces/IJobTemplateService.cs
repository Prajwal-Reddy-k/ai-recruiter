using AIRecruiter.Application.DTOs.JobTemplates;
using AIRecruiter.Application.DTOs.Jobs;

namespace AIRecruiter.Application.Interfaces;

public interface IJobTemplateService
{
    Task<IReadOnlyList<JobTemplateDto>> GetMyTemplatesAsync(int recruiterUserId, string? search, CancellationToken ct = default);
    Task<JobTemplateDto> GetByIdAsync(int recruiterUserId, int templateId, CancellationToken ct = default);
    Task<JobTemplateDto> CreateAsync(int recruiterUserId, UpsertJobTemplateRequest request, CancellationToken ct = default);
    Task<JobTemplateDto> CreateFromJobAsync(int recruiterUserId, int jobId, string? title, CancellationToken ct = default);
    Task<JobTemplateDto> UpdateAsync(int recruiterUserId, int templateId, UpsertJobTemplateRequest request, CancellationToken ct = default);
    Task DeleteAsync(int recruiterUserId, int templateId, CancellationToken ct = default);
    Task<JobTemplateDto> DuplicateAsync(int recruiterUserId, int templateId, CancellationToken ct = default);
    Task<JobPostingDto> CreateDraftJobFromTemplateAsync(int recruiterUserId, int templateId, CancellationToken ct = default);
}
