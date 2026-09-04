import apiClient from "./client";
import type { SalaryInsight } from "../types";

export interface SalaryInsightsFilter {
  role?: string;
  city?: string;
  state?: string;
  isRemote?: boolean;
  experienceBand?: string;
}

export async function getSalaryInsights(filter?: SalaryInsightsFilter): Promise<SalaryInsight[]> {
  const { data } = await apiClient.get<SalaryInsight[]>("/salary-insights", { params: filter });
  return data;
}
