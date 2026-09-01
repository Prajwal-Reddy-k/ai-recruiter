import apiClient from "./client";
import type { JobPosting, JobTemplate, UpsertJobTemplateRequest } from "../types";

export async function getMyJobTemplates(search?: string): Promise<JobTemplate[]> {
  const { data } = await apiClient.get<JobTemplate[]>("/recruiters/job-templates", {
    params: search ? { search } : undefined,
  });
  return data;
}

export async function getJobTemplateById(templateId: number): Promise<JobTemplate> {
  const { data } = await apiClient.get<JobTemplate>(`/recruiters/job-templates/${templateId}`);
  return data;
}

export async function createJobTemplate(payload: UpsertJobTemplateRequest): Promise<JobTemplate> {
  const { data } = await apiClient.post<JobTemplate>("/recruiters/job-templates", payload);
  return data;
}

export async function createJobTemplateFromJob(jobId: number, title?: string): Promise<JobTemplate> {
  const { data } = await apiClient.post<JobTemplate>(`/recruiters/job-templates/from-job/${jobId}`, null, {
    params: title ? { title } : undefined,
  });
  return data;
}

export async function updateJobTemplate(templateId: number, payload: UpsertJobTemplateRequest): Promise<JobTemplate> {
  const { data } = await apiClient.put<JobTemplate>(`/recruiters/job-templates/${templateId}`, payload);
  return data;
}

export async function deleteJobTemplate(templateId: number): Promise<void> {
  await apiClient.delete(`/recruiters/job-templates/${templateId}`);
}

export async function duplicateJobTemplate(templateId: number): Promise<JobTemplate> {
  const { data } = await apiClient.post<JobTemplate>(`/recruiters/job-templates/${templateId}/duplicate`);
  return data;
}

export async function createJobFromTemplate(templateId: number): Promise<JobPosting> {
  const { data } = await apiClient.post<JobPosting>(`/recruiters/job-templates/${templateId}/create-job`);
  return data;
}
