using AIRecruiter.Application.DTOs.CareerGoals;
using AIRecruiter.Domain.Entities;

namespace AIRecruiter.Application.Validation;

public record CareerGoalSuggestionInput(
    bool HasResumeFile,
    bool TargetSkillMissingFromProfile,
    bool HasRecentRelevantAssessment,
    bool HasAppliedToJobsRecently,
    bool HasPendingInterviewInvites);

/// <summary>Pure, deterministic per-goal suggestions — no external AI, just a small ordered
/// checklist derived from signals the caller already has on hand. Mirrors
/// ProfileStrengthCalculator's exact pattern so both surfaces stay consistent.</summary>
public static class CareerGoalSuggestionEngine
{
    public static IReadOnlyList<CareerGoalSuggestionDto> Suggest(CareerGoal goal, CareerGoalSuggestionInput input)
    {
        var suggestions = new List<CareerGoalSuggestionDto>();

        if (!input.HasResumeFile)
        {
            suggestions.Add(new CareerGoalSuggestionDto("Complete your resume", "A complete resume makes every other step toward this goal more effective.", "/resume-builder"));
        }

        if (!string.IsNullOrWhiteSpace(goal.TargetSkill) && input.TargetSkillMissingFromProfile)
        {
            suggestions.Add(new CareerGoalSuggestionDto($"Add \"{goal.TargetSkill}\" to your skills", "Add this skill to your profile once you've gained some experience with it, so it starts showing up in job matches.", "/profile"));
        }

        if (!input.HasRecentRelevantAssessment)
        {
            suggestions.Add(new CareerGoalSuggestionDto("Take a skill assessment", "A completed assessment is a concrete, verifiable step toward this goal.", "/assessments"));
        }

        if (!input.HasAppliedToJobsRecently)
        {
            suggestions.Add(new CareerGoalSuggestionDto("Apply to relevant jobs", "Applying to roles that match this goal keeps your progress moving.", "/jobs"));
        }

        if (input.HasPendingInterviewInvites)
        {
            suggestions.Add(new CareerGoalSuggestionDto("Respond to your interview invite", "A pending interview invite is a direct opportunity toward this goal.", "/interviews"));
        }

        return suggestions.Take(4).ToList();
    }
}
