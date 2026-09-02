import apiClient from "./client";
import type {
  TalentPool, TalentPoolCandidate, AddCandidateToPoolRequest, UpdatePoolCandidateNotesRequest,
} from "../types";

export async function getMyTalentPools(): Promise<TalentPool[]> {
  const { data } = await apiClient.get<TalentPool[]>("/talent-pools");
  return data;
}

export async function createTalentPool(name: string): Promise<TalentPool> {
  const { data } = await apiClient.post<TalentPool>("/talent-pools", { name });
  return data;
}

export async function renameTalentPool(id: number, name: string): Promise<TalentPool> {
  const { data } = await apiClient.put<TalentPool>(`/talent-pools/${id}`, { name });
  return data;
}

export async function deleteTalentPool(id: number): Promise<void> {
  await apiClient.delete(`/talent-pools/${id}`);
}

export async function getTalentPoolCandidates(poolId: number): Promise<TalentPoolCandidate[]> {
  const { data } = await apiClient.get<TalentPoolCandidate[]>(`/talent-pools/${poolId}/candidates`);
  return data;
}

export async function addCandidateToPool(poolId: number, request: AddCandidateToPoolRequest): Promise<void> {
  await apiClient.post(`/talent-pools/${poolId}/candidates`, request);
}

export async function removeCandidateFromPool(poolId: number, candidateProfileId: number): Promise<void> {
  await apiClient.delete(`/talent-pools/${poolId}/candidates/${candidateProfileId}`);
}

export async function updatePoolCandidateNotes(poolId: number, candidateProfileId: number, request: UpdatePoolCandidateNotesRequest): Promise<void> {
  await apiClient.patch(`/talent-pools/${poolId}/candidates/${candidateProfileId}`, request);
}
