import apiClient from "./client";
import type {
  Resume, ReorderRequest,
  UpsertResumeSummaryRequest, UpsertWorkExperienceRequest, UpsertEducationEntryRequest,
  UpsertCertificationRequest, UpsertProjectRequest,
} from "../types";

const BASE = "/candidates/me/resume-builder";

export async function getMyResume(): Promise<Resume> {
  const { data } = await apiClient.get<Resume>(BASE);
  return data;
}

export async function upsertResumeSummary(payload: UpsertResumeSummaryRequest): Promise<Resume> {
  const { data } = await apiClient.put<Resume>(`${BASE}/summary`, payload);
  return data;
}

export async function addExperience(payload: UpsertWorkExperienceRequest): Promise<Resume> {
  const { data } = await apiClient.post<Resume>(`${BASE}/experience`, payload);
  return data;
}
export async function updateExperience(id: number, payload: UpsertWorkExperienceRequest): Promise<Resume> {
  const { data } = await apiClient.put<Resume>(`${BASE}/experience/${id}`, payload);
  return data;
}
export async function deleteExperience(id: number): Promise<Resume> {
  const { data } = await apiClient.delete<Resume>(`${BASE}/experience/${id}`);
  return data;
}
export async function reorderExperience(payload: ReorderRequest): Promise<Resume> {
  const { data } = await apiClient.post<Resume>(`${BASE}/experience/reorder`, payload);
  return data;
}

export async function addEducation(payload: UpsertEducationEntryRequest): Promise<Resume> {
  const { data } = await apiClient.post<Resume>(`${BASE}/education`, payload);
  return data;
}
export async function updateEducation(id: number, payload: UpsertEducationEntryRequest): Promise<Resume> {
  const { data } = await apiClient.put<Resume>(`${BASE}/education/${id}`, payload);
  return data;
}
export async function deleteEducation(id: number): Promise<Resume> {
  const { data } = await apiClient.delete<Resume>(`${BASE}/education/${id}`);
  return data;
}
export async function reorderEducation(payload: ReorderRequest): Promise<Resume> {
  const { data } = await apiClient.post<Resume>(`${BASE}/education/reorder`, payload);
  return data;
}

export async function addCertification(payload: UpsertCertificationRequest): Promise<Resume> {
  const { data } = await apiClient.post<Resume>(`${BASE}/certifications`, payload);
  return data;
}
export async function updateCertification(id: number, payload: UpsertCertificationRequest): Promise<Resume> {
  const { data } = await apiClient.put<Resume>(`${BASE}/certifications/${id}`, payload);
  return data;
}
export async function deleteCertification(id: number): Promise<Resume> {
  const { data } = await apiClient.delete<Resume>(`${BASE}/certifications/${id}`);
  return data;
}
export async function reorderCertification(payload: ReorderRequest): Promise<Resume> {
  const { data } = await apiClient.post<Resume>(`${BASE}/certifications/reorder`, payload);
  return data;
}

export async function addProject(payload: UpsertProjectRequest): Promise<Resume> {
  const { data } = await apiClient.post<Resume>(`${BASE}/projects`, payload);
  return data;
}
export async function updateProject(id: number, payload: UpsertProjectRequest): Promise<Resume> {
  const { data } = await apiClient.put<Resume>(`${BASE}/projects/${id}`, payload);
  return data;
}
export async function deleteProject(id: number): Promise<Resume> {
  const { data } = await apiClient.delete<Resume>(`${BASE}/projects/${id}`);
  return data;
}
export async function reorderProject(payload: ReorderRequest): Promise<Resume> {
  const { data } = await apiClient.post<Resume>(`${BASE}/projects/reorder`, payload);
  return data;
}
