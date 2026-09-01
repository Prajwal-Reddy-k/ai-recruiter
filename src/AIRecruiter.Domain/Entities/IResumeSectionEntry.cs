namespace AIRecruiter.Domain.Entities;

/// <summary>Shared shape of the 4 resume-builder repeatable section entities
/// (CandidateWorkExperience/CandidateEducation/CandidateCertification/CandidateProject) —
/// lets ResumeBuilderService's ownership-check and reorder logic stay generic instead of
/// being duplicated four times over.</summary>
public interface IResumeSectionEntry
{
    int Id { get; }
    int CandidateProfileId { get; set; }
    int DisplayOrder { get; set; }
}
