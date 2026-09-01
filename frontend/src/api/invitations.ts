import apiClient from "./client";
import type { Invitation } from "../types";

export async function inviteCandidate(jobPostingId: number, candidateProfileId: number, message?: string): Promise<Invitation> {
  const { data } = await apiClient.post<Invitation>("/invitations", { jobPostingId, candidateProfileId, message });
  return data;
}

export async function getSentInvitations(jobPostingId?: number): Promise<Invitation[]> {
  const { data } = await apiClient.get<Invitation[]>("/invitations/sent", {
    params: jobPostingId ? { jobPostingId } : undefined,
  });
  return data;
}

export async function getMyInvitations(): Promise<Invitation[]> {
  const { data } = await apiClient.get<Invitation[]>("/invitations/mine");
  return data;
}

export async function markInvitationViewed(id: number): Promise<Invitation> {
  const { data } = await apiClient.post<Invitation>(`/invitations/${id}/view`);
  return data;
}

export async function acceptInvitation(id: number): Promise<Invitation> {
  const { data } = await apiClient.post<Invitation>(`/invitations/${id}/accept`);
  return data;
}

export async function declineInvitation(id: number): Promise<Invitation> {
  const { data } = await apiClient.post<Invitation>(`/invitations/${id}/decline`);
  return data;
}

export async function dismissInvitation(id: number): Promise<void> {
  await apiClient.post(`/invitations/${id}/dismiss`);
}
