import apiClient from "./client";
import type { CandidateDashboard, RecruiterAnalytics, RecruiterDashboard } from "../types";

export async function getCandidateDashboard(): Promise<CandidateDashboard> {
  const { data } = await apiClient.get<CandidateDashboard>("/dashboard/candidate");
  return data;
}

export async function getRecruiterDashboard(): Promise<RecruiterDashboard> {
  const { data } = await apiClient.get<RecruiterDashboard>("/dashboard/recruiter");
  return data;
}

export async function getRecruiterAnalytics(): Promise<RecruiterAnalytics> {
  const { data } = await apiClient.get<RecruiterAnalytics>("/dashboard/recruiter/analytics");
  return data;
}
