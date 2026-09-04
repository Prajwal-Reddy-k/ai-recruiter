using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

/// <summary>A candidate's answer to one screening question on one application. TextValue
/// covers ShortText/LongText/Url, and "Yes"/"No" for YesNo. NumberValue is populated
/// separately (not just parsed from TextValue) so numeric range filtering can query it
/// directly. SelectedOptions covers SingleChoice/MultipleChoice.</summary>
public class ScreeningAnswer : BaseEntity
{
    public int JobApplicationId { get; set; }
    public JobApplication JobApplication { get; set; } = null!;

    public int JobScreeningQuestionId { get; set; }
    public JobScreeningQuestion JobScreeningQuestion { get; set; } = null!;

    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }

    public ICollection<ScreeningAnswerSelectedOption> SelectedOptions { get; set; } = new List<ScreeningAnswerSelectedOption>();
}
