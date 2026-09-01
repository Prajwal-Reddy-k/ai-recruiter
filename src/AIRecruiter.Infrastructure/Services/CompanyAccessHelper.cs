using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Shared "does the caller own this, or are they the company Owner" check, reused
/// everywhere the existing owning-recruiter-only authorization (RecruiterProfile.UserId ==
/// callerId) needs the additive "or a same-company Owner" extension — this is the one place
/// that extension is implemented, so every call site behaves identically.</summary>
public static class CompanyAccessHelper
{
    public static async Task<bool> IsOwningRecruiterOrCompanyOwnerAsync(
        AppDbContext db, int callerUserId, int owningRecruiterUserId, int companyId, CancellationToken ct)
    {
        if (owningRecruiterUserId == callerUserId)
        {
            return true;
        }

        var callerRole = await db.RecruiterProfiles
            .Where(r => r.UserId == callerUserId && r.CompanyId == companyId)
            .Select(r => (CompanyRole?)r.CompanyRole)
            .FirstOrDefaultAsync(ct);

        return callerRole == CompanyRole.Owner;
    }
}
