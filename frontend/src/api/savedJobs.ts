import apiClient from "./client";
import type { JobPosting, JobAlert, SavedJobEntry, UpsertJobAlertRequest } from "../types";
import type { JobTypeValue } from "./jobs";
import { jobTypeToNumber } from "./jobs";

export async function getSavedJobs(): Promise<SavedJobEntry[]> {
  const { data } = await apiClient.get<SavedJobEntry[]>("/candidates/me/saved-jobs");
  return data;
}

export async function saveJob(jobId: number): Promise<void> {
  await apiClient.post(`/candidates/me/saved-jobs/${jobId}`);
}

export async function unsaveJob(jobId: number): Promise<void> {
  await apiClient.delete(`/candidates/me/saved-jobs/${jobId}`);
}

export async function getMyAlerts(): Promise<JobAlert[]> {
  const { data } = await apiClient.get<JobAlert[]>("/candidates/me/alerts");
  return data;
}

export async function getAlertMatches(): Promise<JobPosting[]> {
  const { data } = await apiClient.get<JobPosting[]>("/candidates/me/alerts/matches");
  return data;
}

function toWirePayload(payload: UpsertJobAlertRequest) {
  return {
    ...payload,
    jobType: payload.jobType ? jobTypeToNumber[payload.jobType as JobTypeValue] : undefined,
  };
}

export async function createAlert(payload: UpsertJobAlertRequest): Promise<JobAlert> {
  const { data } = await apiClient.post<JobAlert>("/candidates/me/alerts", toWirePayload(payload));
  return data;
}

export async function updateAlert(alertId: number, payload: UpsertJobAlertRequest): Promise<JobAlert> {
  const { data } = await apiClient.put<JobAlert>(`/candidates/me/alerts/${alertId}`, toWirePayload(payload));
  return data;
}

export async function setAlertActive(alertId: number, isActive: boolean): Promise<JobAlert> {
  const { data } = await apiClient.patch<JobAlert>(`/candidates/me/alerts/${alertId}/active`, { isActive });
  return data;
}

export async function deleteAlert(alertId: number): Promise<void> {
  await apiClient.delete(`/candidates/me/alerts/${alertId}`);
}

export async function duplicateAlert(alertId: number): Promise<JobAlert> {
  const { data } = await apiClient.post<JobAlert>(`/candidates/me/alerts/${alertId}/duplicate`);
  return data;
}

export async function setDefaultAlert(alertId: number): Promise<JobAlert> {
  const { data } = await apiClient.patch<JobAlert>(`/candidates/me/alerts/${alertId}/default`);
  return data;
}
