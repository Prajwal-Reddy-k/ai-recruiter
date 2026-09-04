import apiClient from "./client";
import type { ApplicantScreeningFilter, JobApplication, JobApplicationDetail, SubmitScreeningAnswerRequest } from "../types";

export async function applyToJob(jobId: number, coverNote?: string, answers?: SubmitScreeningAnswerRequest[]): Promise<JobApplication> {
  const { data } = await apiClient.post<JobApplication>(`/jobs/${jobId}/apply`, { coverNote, answers });
  return data;
}

export async function getMyApplications(): Promise<JobApplication[]> {
  const { data } = await apiClient.get<JobApplication[]>("/applications/me");
  return data;
}

export async function getApplicationDetail(id: number): Promise<JobApplicationDetail> {
  const { data } = await apiClient.get<JobApplicationDetail>(`/applications/${id}`);
  return data;
}

export async function getApplicationsForJob(jobId: number, filter?: ApplicantScreeningFilter): Promise<JobApplication[]> {
  const { data } = await apiClient.get<JobApplication[]>(`/jobs/${jobId}/applications`, { params: filter });
  return data;
}

export async function downloadApplicationResume(applicationId: number): Promise<Blob> {
  const { data } = await apiClient.get(`/applications/${applicationId}/resume`, { responseType: "blob" });
  return data;
}

export type ApplicationStatusValue =
  | "Applied"
  | "Screening"
  | "Shortlisted"
  | "InterviewScheduled"
  | "InterviewCompleted"
  | "Offer"
  | "Hired"
  | "Rejected"
  | "Withdrawn";

export const applicationStatusToNumber: Record<ApplicationStatusValue, number> = {
  Applied: 1,
  Screening: 2,
  Shortlisted: 3,
  InterviewScheduled: 4,
  InterviewCompleted: 5,
  Offer: 6,
  Hired: 7,
  Rejected: 8,
  Withdrawn: 9,
};

export async function updateApplicationStatus(
  applicationId: number,
  status: ApplicationStatusValue,
  note?: string
): Promise<JobApplication> {
  const { data } = await apiClient.patch<JobApplication>(`/applications/${applicationId}/status`, {
    status: applicationStatusToNumber[status],
    note,
  });
  return data;
}

export async function withdrawApplication(applicationId: number): Promise<JobApplication> {
  const { data } = await apiClient.post<JobApplication>(`/applications/${applicationId}/withdraw`);
  return data;
}
