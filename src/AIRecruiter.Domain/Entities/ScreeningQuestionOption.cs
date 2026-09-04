using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

public class ScreeningQuestionOption : BaseEntity
{
    public int JobScreeningQuestionId { get; set; }
    public JobScreeningQuestion JobScreeningQuestion { get; set; } = null!;

    public string OptionText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
