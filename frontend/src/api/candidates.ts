import apiClient from "./client";
import type { CandidateProfile, UpsertCandidateProfileRequest } from "../types";

export async function getMyCandidateProfile(): Promise<CandidateProfile> {
  const { data } = await apiClient.get<CandidateProfile>("/candidates/me");
  return data;
}

export async function upsertMyCandidateProfile(payload: UpsertCandidateProfileRequest): Promise<CandidateProfile> {
  const { data } = await apiClient.put<CandidateProfile>("/candidates/me", payload);
  return data;
}

export async function uploadResume(
  file: File,
  onProgress?: (percent: number) => void
): Promise<CandidateProfile> {
  const formData = new FormData();
  formData.append("file", file);

  const { data } = await apiClient.post<CandidateProfile>("/candidates/me/resume", formData, {
    headers: { "Content-Type": "multipart/form-data" },
    onUploadProgress: (event) => {
      if (onProgress && event.total) {
        onProgress(Math.round((event.loaded / event.total) * 100));
      }
    },
  });
  return data;
}

export async function downloadMyResume(): Promise<Blob> {
  const { data } = await apiClient.get("/candidates/me/resume/download", { responseType: "blob" });
  return data;
}

export async function uploadAvatar(
  file: File,
  onProgress?: (percent: number) => void
): Promise<CandidateProfile> {
  const formData = new FormData();
  formData.append("file", file);

  const { data } = await apiClient.post<CandidateProfile>("/candidates/me/avatar", formData, {
    headers: { "Content-Type": "multipart/form-data" },
    onUploadProgress: (event) => {
      if (onProgress && event.total) {
        onProgress(Math.round((event.loaded / event.total) * 100));
      }
    },
  });
  return data;
}

export async function removeAvatar(): Promise<CandidateProfile> {
  const { data } = await apiClient.delete<CandidateProfile>("/candidates/me/avatar");
  return data;
}
