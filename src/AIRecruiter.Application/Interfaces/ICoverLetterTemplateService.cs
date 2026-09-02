using AIRecruiter.Application.DTOs.CoverLetters;

namespace AIRecruiter.Application.Interfaces;

public interface ICoverLetterTemplateService
{
    Task<IReadOnlyList<CoverLetterTemplateDto>> GetMyTemplatesAsync(int userId, CancellationToken ct = default);
    Task<CoverLetterTemplateDto> CreateAsync(int userId, UpsertCoverLetterTemplateRequest request, CancellationToken ct = default);
    Task<CoverLetterTemplateDto> UpdateAsync(int userId, int templateId, UpsertCoverLetterTemplateRequest request, CancellationToken ct = default);
    Task DeleteAsync(int userId, int templateId, CancellationToken ct = default);
}
