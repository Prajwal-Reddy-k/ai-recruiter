import apiClient from "./client";
import type { FeedbackSubmission, SubmitFeedbackRequest } from "../types";

export type FeedbackCategoryValue = "General" | "Bug" | "JobPosting" | "Account" | "Other";
const feedbackCategoryToNumber: Record<FeedbackCategoryValue, number> = {
  General: 1,
  Bug: 2,
  JobPosting: 3,
  Account: 4,
  Other: 5,
};

export const FEEDBACK_CATEGORY_OPTIONS: { value: FeedbackCategoryValue; label: string }[] = [
  { value: "General", label: "General feedback" },
  { value: "Bug", label: "Report a bug" },
  { value: "JobPosting", label: "Job posting issue" },
  { value: "Account", label: "Account issue" },
  { value: "Other", label: "Other" },
];

export async function submitFeedback(payload: SubmitFeedbackRequest): Promise<void> {
  await apiClient.post("/feedback", {
    ...payload,
    category: feedbackCategoryToNumber[payload.category as FeedbackCategoryValue],
  });
}

export type FeedbackStatusValue = "New" | "InProgress" | "Resolved";
const feedbackStatusToNumber: Record<FeedbackStatusValue, number> = {
  New: 1,
  InProgress: 2,
  Resolved: 3,
};

export async function getAdminFeedback(): Promise<FeedbackSubmission[]> {
  const { data } = await apiClient.get<FeedbackSubmission[]>("/admin/feedback");
  return data;
}

export async function setFeedbackStatus(id: number, status: FeedbackStatusValue): Promise<void> {
  await apiClient.post(`/admin/feedback/${id}/status`, { status: feedbackStatusToNumber[status] });
}
