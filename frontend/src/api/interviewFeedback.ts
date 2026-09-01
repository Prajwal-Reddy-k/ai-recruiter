import apiClient from "./client";
import type { InterviewFeedback, InterviewFeedbackSummary, InterviewRecommendationValue, UpsertInterviewFeedbackRequest } from "../types";

export const recommendationToNumber: Record<InterviewRecommendationValue, number> = {
  StrongNo: 1,
  No: 2,
  Neutral: 3,
  Yes: 4,
  StrongYes: 5,
};

export async function getMyFeedback(interviewId: number): Promise<InterviewFeedback | null> {
  const { data } = await apiClient.get<InterviewFeedback | null>(`/interviews/${interviewId}/feedback/mine`);
  return data;
}

export async function getFeedbackSummary(interviewId: number): Promise<InterviewFeedbackSummary> {
  const { data } = await apiClient.get<InterviewFeedbackSummary>(`/interviews/${interviewId}/feedback/summary`);
  return data;
}

export async function saveFeedbackDraft(interviewId: number, payload: UpsertInterviewFeedbackRequest): Promise<InterviewFeedback> {
  const { data } = await apiClient.put<InterviewFeedback>(`/interviews/${interviewId}/feedback/draft`, payload);
  return data;
}

export async function submitFeedback(interviewId: number, payload: UpsertInterviewFeedbackRequest): Promise<InterviewFeedback> {
  const { data } = await apiClient.post<InterviewFeedback>(`/interviews/${interviewId}/feedback/submit`, payload);
  return data;
}
