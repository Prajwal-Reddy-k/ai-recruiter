using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

public class Interview : BaseEntity
{
    public int JobApplicationId { get; set; }
    public JobApplication JobApplication { get; set; } = null!;

    public InterviewStatus Status { get; set; } = InterviewStatus.Proposed;

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public string? DeclineNote { get; set; }

    public ICollection<InterviewSlot> Slots { get; set; } = new List<InterviewSlot>();
}
