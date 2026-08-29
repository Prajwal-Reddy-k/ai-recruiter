import apiClient from "./client";
import { applicationStatusToNumber, type ApplicationStatusValue } from "./applications";
import type { CandidateSearchDetail, CandidateSearchResult, CandidateSortOption } from "../types";

export interface CandidateSearchFilters {
  skills?: string;
  city?: string;
  state?: string;
  minExperienceYears?: number;
  maxExperienceYears?: number;
  education?: string;
  status?: ApplicationStatusValue;
  minMatchScore?: number;
  maxMatchScore?: number;
  sort?: CandidateSortOption;
}

const SORT_TO_NUMBER: Record<CandidateSortOption, number> = {
  NewestApplication: 1,
  HighestMatchScore: 2,
  ExperienceDesc: 3,
  NameAlphabetical: 4,
};

function toParams(filters: CandidateSearchFilters) {
  return {
    skills: filters.skills || undefined,
    city: filters.city || undefined,
    state: filters.state || undefined,
    minExperienceYears: filters.minExperienceYears,
    maxExperienceYears: filters.maxExperienceYears,
    education: filters.education || undefined,
    status: filters.status ? applicationStatusToNumber[filters.status] : undefined,
    minMatchScore: filters.minMatchScore,
    maxMatchScore: filters.maxMatchScore,
    sort: filters.sort ? SORT_TO_NUMBER[filters.sort] : undefined,
  };
}

export async function searchCandidates(filters: CandidateSearchFilters): Promise<CandidateSearchResult[]> {
  const { data } = await apiClient.get<CandidateSearchResult[]>("/recruiters/candidates", { params: toParams(filters) });
  return data;
}

export async function getCandidateDetail(candidateProfileId: number): Promise<CandidateSearchDetail> {
  const { data } = await apiClient.get<CandidateSearchDetail>(`/recruiters/candidates/${candidateProfileId}`);
  return data;
}

export async function exportCandidatesCsv(filters: CandidateSearchFilters): Promise<Blob> {
  const { data } = await apiClient.get("/recruiters/candidates/export", {
    params: toParams(filters),
    responseType: "blob",
  });
  return data;
}

export async function downloadCandidateResume(applicationId: number): Promise<Blob> {
  const { data } = await apiClient.get(`/recruiters/candidates/applications/${applicationId}/resume`, { responseType: "blob" });
  return data;
}
