using AIRecruiter.Application.DTOs.Companies;

namespace AIRecruiter.Application.Interfaces;

public interface ICompanyService
{
    Task<CompanyProfileDto> GetPublicProfileAsync(int companyId, CancellationToken ct = default);
}
