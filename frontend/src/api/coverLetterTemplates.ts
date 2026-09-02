import apiClient from "./client";
import type { CoverLetterTemplate, UpsertCoverLetterTemplateRequest } from "../types";

const BASE = "/cover-letter-templates";

export async function getMyCoverLetterTemplates(): Promise<CoverLetterTemplate[]> {
  const { data } = await apiClient.get<CoverLetterTemplate[]>(BASE);
  return data;
}

export async function createCoverLetterTemplate(payload: UpsertCoverLetterTemplateRequest): Promise<CoverLetterTemplate> {
  const { data } = await apiClient.post<CoverLetterTemplate>(BASE, payload);
  return data;
}

export async function updateCoverLetterTemplate(id: number, payload: UpsertCoverLetterTemplateRequest): Promise<CoverLetterTemplate> {
  const { data } = await apiClient.put<CoverLetterTemplate>(`${BASE}/${id}`, payload);
  return data;
}

export async function deleteCoverLetterTemplate(id: number): Promise<void> {
  await apiClient.delete(`${BASE}/${id}`);
}
