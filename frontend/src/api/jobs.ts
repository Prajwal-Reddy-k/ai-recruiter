import apiClient from "./client";
import type { CreateJobPostingRequest, JobPosting, PagedResult, RecruiterJobSummary, UpdateJobPostingRequest } from "../types";
import { type ReportReasonValue, reportReasonToNumber } from "./moderationReports";

export type JobTypeValue = "FullTime" | "PartTime" | "Contract" | "Internship" | "Freelance";

export const jobTypeToNumber: Record<JobTypeValue, number> = {
  FullTime: 1,
  PartTime: 2,
  Contract: 3,
  Internship: 4,
  Freelance: 5,
};

export async function getOpenJobsPage(search?: string, page = 1, pageSize = 20): Promise<PagedResult<JobPosting>> {
  const { data } = await apiClient.get<PagedResult<JobPosting>>("/jobs", {
    params: { search: search || undefined, page, pageSize },
  });
  return data;
}

/** Convenience wrapper for callers that just want a flat list (landing page highlights,
 * similar-jobs matching) rather than paging UI — fetches a single, larger page. */
export async function getOpenJobs(search?: string, pageSize = 50): Promise<JobPosting[]> {
  const result = await getOpenJobsPage(search, 1, pageSize);
  return result.items;
}

export async function getJobById(id: number): Promise<JobPosting> {
  const { data } = await apiClient.get<JobPosting>(`/jobs/${id}`);
  return data;
}

export async function createJob(payload: CreateJobPostingRequest): Promise<JobPosting> {
  const { data } = await apiClient.post<JobPosting>("/jobs", {
    ...payload,
    jobType: jobTypeToNumber[payload.jobType as JobTypeValue],
  });
  return data;
}

export async function updateJob(jobId: number, payload: UpdateJobPostingRequest): Promise<JobPosting> {
  const { data } = await apiClient.put<JobPosting>(`/jobs/${jobId}`, {
    ...payload,
    jobType: jobTypeToNumber[payload.jobType as JobTypeValue],
  });
  return data;
}

export async function getMyJobs(): Promise<RecruiterJobSummary[]> {
  const { data } = await apiClient.get<RecruiterJobSummary[]>("/jobs/mine");
  return data;
}

export type JobStatusValue = "Draft" | "Open" | "Closed" | "Archived";

const jobStatusToNumber: Record<JobStatusValue, number> = {
  Draft: 1,
  Open: 2,
  Closed: 3,
  Archived: 4,
};

export async function updateJobStatus(jobId: number, status: JobStatusValue): Promise<JobPosting> {
  const { data } = await apiClient.patch<JobPosting>(`/jobs/${jobId}/status`, {
    status: jobStatusToNumber[status],
  });
  return data;
}

export const publishJob = (jobId: number) => updateJobStatus(jobId, "Open");
export const closeJob = (jobId: number) => updateJobStatus(jobId, "Closed");
export const reopenJob = (jobId: number) => updateJobStatus(jobId, "Open");
export const archiveJob = (jobId: number) => updateJobStatus(jobId, "Archived");

export async function duplicateJob(jobId: number): Promise<JobPosting> {
  const { data } = await apiClient.post<JobPosting>(`/jobs/${jobId}/duplicate`);
  return data;
}

export async function reportJob(jobId: number, reason: ReportReasonValue, details?: string): Promise<void> {
  await apiClient.post(`/jobs/${jobId}/report`, { reason: reportReasonToNumber[reason], details });
}

export async function extendJobDeadline(jobId: number, applicationDeadlineUtc: string | null): Promise<JobPosting> {
  const { data } = await apiClient.patch<JobPosting>(`/jobs/${jobId}/deadline`, { applicationDeadlineUtc });
  return data;
}

export async function recordJobShare(jobId: number): Promise<void> {
  await apiClient.post(`/jobs/${jobId}/share`);
}

export async function recordJobView(jobId: number): Promise<void> {
  await apiClient.post(`/jobs/${jobId}/view`);
}
