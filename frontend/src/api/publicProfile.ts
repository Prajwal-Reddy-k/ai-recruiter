import apiClient from "./client";
import type { PublicCandidateProfile, PublicProfilePreview } from "../types";

export async function getMyPublicProfilePreview(): Promise<PublicProfilePreview> {
  const { data } = await apiClient.get<PublicProfilePreview>("/candidates/me/public-profile-preview");
  return data;
}

export async function getPublicProfileBySlug(slug: string): Promise<PublicCandidateProfile> {
  const { data } = await apiClient.get<PublicCandidateProfile>(`/talent/${encodeURIComponent(slug)}`);
  return data;
}
