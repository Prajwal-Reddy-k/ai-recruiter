using AIRecruiter.Application.Common;
using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Infrastructure.Mapping;

public static class JobPostingMapper
{
    public static JobPostingDto ToDto(JobPosting j, bool includePreferredAnswers = false) => new(
        j.Id,
        j.Title,
        j.Description,
        j.RequiredSkillsCsv,
        j.MinExperienceYears,
        j.MaxExperienceYears,
        j.MinSalary,
        j.MaxSalary,
        j.City,
        j.State,
        j.Locality,
        j.IsRemote,
        IndiaLocationFormatter.Format(j.City, j.State, j.IsRemote),
        j.JobType.ToString(),
        j.Status.ToString(),
        j.CompanyId,
        j.Company.Name,
        j.Company.LogoUrl,
        j.CreatedAt,
        j.ViewCount,
        j.PublishedAt,
        j.ApplicationDeadlineUtc,
        j.ShareCount,
        j.Company.VerificationStatus == CompanyVerificationStatus.Verified,
        j.ScreeningQuestions
            .OrderBy(q => q.DisplayOrder)
            .Select(q => new ScreeningQuestionDto(
                q.Id, q.QuestionText, q.QuestionType.ToString(), q.IsRequired, q.HelpText,
                q.Options.OrderBy(o => o.DisplayOrder).Select(o => new ScreeningQuestionOptionDto(o.Id, o.OptionText, o.DisplayOrder)).ToList(),
                q.DisplayOrder,
                includePreferredAnswers ? q.PreferredAnswer : null))
            .ToList());
}
