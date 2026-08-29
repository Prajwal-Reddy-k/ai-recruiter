import apiClient from "./client";
import type { CompanyProfile } from "../types";

export async function getCompanyProfile(id: number): Promise<CompanyProfile> {
  const { data } = await apiClient.get<CompanyProfile>(`/companies/${id}`);
  return data;
}
