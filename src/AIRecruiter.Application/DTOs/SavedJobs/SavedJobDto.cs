using AIRecruiter.Application.DTOs.Jobs;

namespace AIRecruiter.Application.DTOs.SavedJobs;

public record SavedJobDto(JobPostingDto Job, DateTime SavedAt);
