import apiClient from "./client";
import type { AuditLogEntry, OnboardingStatus, UpsertOnboardingRequest } from "../types";

export async function getOnboardingStatus(): Promise<OnboardingStatus> {
  const { data } = await apiClient.get<OnboardingStatus>("/recruiters/me/onboarding-status");
  return data;
}

export async function upsertOnboarding(payload: UpsertOnboardingRequest): Promise<OnboardingStatus> {
  const { data } = await apiClient.post<OnboardingStatus>("/recruiters/me/onboarding", payload);
  return data;
}

export async function getMyActivity(): Promise<AuditLogEntry[]> {
  const { data } = await apiClient.get<AuditLogEntry[]>("/recruiters/me/activity");
  return data;
}
