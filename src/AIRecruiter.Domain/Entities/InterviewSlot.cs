using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

public class InterviewSlot : BaseEntity
{
    public int InterviewId { get; set; }
    public Interview Interview { get; set; } = null!;

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public bool IsSelected { get; set; }
}
