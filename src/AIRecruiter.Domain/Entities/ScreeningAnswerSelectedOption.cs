using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

public class ScreeningAnswerSelectedOption : BaseEntity
{
    public int ScreeningAnswerId { get; set; }
    public ScreeningAnswer ScreeningAnswer { get; set; } = null!;

    public int ScreeningQuestionOptionId { get; set; }
    public ScreeningQuestionOption ScreeningQuestionOption { get; set; } = null!;
}
