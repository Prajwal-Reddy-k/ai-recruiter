using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

/// <summary>A company-wide resource, not a personal one — any recruiter/owner at the
/// company can manage any pool, mirroring JobAssignment's company-shared model.</summary>
public class TalentPool : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public ICollection<TalentPoolCandidate> Candidates { get; set; } = new List<TalentPoolCandidate>();
}
