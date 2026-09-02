import apiClient from "./client";
import type {
  AssessmentAttemptInProgress, AssessmentAttemptHistoryItem, AssessmentAttemptResult, AssessmentCategorySummary,
} from "../types";

const BASE = "/assessments";

export async function getAssessmentCategories(): Promise<AssessmentCategorySummary[]> {
  const { data } = await apiClient.get<AssessmentCategorySummary[]>(`${BASE}/categories`);
  return data;
}

export async function startAssessmentAttempt(category: string): Promise<AssessmentAttemptInProgress> {
  const { data } = await apiClient.post<AssessmentAttemptInProgress>(`${BASE}/attempts`, { category });
  return data;
}

export async function getActiveAssessmentAttempt(attemptId: number): Promise<AssessmentAttemptInProgress> {
  const { data } = await apiClient.get<AssessmentAttemptInProgress>(`${BASE}/attempts/${attemptId}`);
  return data;
}

export async function answerAssessmentQuestion(attemptId: number, answerId: number, selectedOptionIndex: number): Promise<void> {
  await apiClient.post(`${BASE}/attempts/${attemptId}/answers`, { answerId, selectedOptionIndex });
}

export async function submitAssessmentAttempt(attemptId: number): Promise<AssessmentAttemptResult> {
  const { data } = await apiClient.post<AssessmentAttemptResult>(`${BASE}/attempts/${attemptId}/submit`, {});
  return data;
}

export async function getAssessmentAttemptReview(attemptId: number): Promise<AssessmentAttemptResult> {
  const { data } = await apiClient.get<AssessmentAttemptResult>(`${BASE}/attempts/${attemptId}/review`);
  return data;
}

export async function setAssessmentAttemptVisibility(attemptId: number, isVisibleToRecruiters: boolean): Promise<void> {
  await apiClient.patch(`${BASE}/attempts/${attemptId}/visibility`, { isVisibleToRecruiters });
}

export async function getAssessmentHistory(): Promise<AssessmentAttemptHistoryItem[]> {
  const { data } = await apiClient.get<AssessmentAttemptHistoryItem[]>(`${BASE}/history`);
  return data;
}
