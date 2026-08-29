import apiClient from "./client";
import type { ExternalJobSearchResult } from "../types";

export async function getExternalJobsAvailability(): Promise<boolean> {
  const { data } = await apiClient.get<{ available: boolean }>("/external-jobs/availability");
  return data.available;
}

export async function searchExternalJobs(
  keywords: string,
  location: string,
  page = 1
): Promise<ExternalJobSearchResult> {
  const { data } = await apiClient.get<ExternalJobSearchResult>("/external-jobs/search", {
    params: { keywords, location, page },
  });
  return data;
}
