using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

public class OfferStatusHistory : BaseEntity
{
    public int OfferId { get; set; }
    public Offer Offer { get; set; } = null!;

    public OfferStatus? FromStatus { get; set; }
    public OfferStatus ToStatus { get; set; }

    public int ChangedByUserId { get; set; }
    public User ChangedByUser { get; set; } = null!;

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}
