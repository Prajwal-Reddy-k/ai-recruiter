import apiClient from "./client";
import type { FollowedCompany } from "../types";

export async function followCompany(companyId: number): Promise<void> {
  await apiClient.post(`/companies/${companyId}/follow`);
}

export async function unfollowCompany(companyId: number): Promise<void> {
  await apiClient.delete(`/companies/${companyId}/follow`);
}

export async function getFollowedCompanies(): Promise<FollowedCompany[]> {
  const { data } = await apiClient.get<FollowedCompany[]>("/companies/followed");
  return data;
}

export async function updateFollowNotifyPreference(companyId: number, notifyOnNewJob: boolean): Promise<void> {
  await apiClient.patch(`/companies/${companyId}/follow/notify`, { notifyOnNewJob });
}
