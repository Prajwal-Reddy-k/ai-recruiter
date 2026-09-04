using AIRecruiter.Application.DTOs.Companies;

namespace AIRecruiter.Application.Interfaces;

public interface ICompanyVerificationService
{
    Task<CompanyVerificationStatusDto> SubmitAsync(int recruiterUserId, SubmitCompanyVerificationRequest request, CancellationToken ct = default);
    Task<CompanyVerificationStatusDto> GetMyStatusAsync(int recruiterUserId, CancellationToken ct = default);
}
