import apiClient from "./client";
import type { CompanyReviewsSummary, ReviewEligibility, SubmitCompanyReviewRequest } from "../types";

export async function getCompanyReviews(companyId: number, relationshipType?: string): Promise<CompanyReviewsSummary> {
  const { data } = await apiClient.get<CompanyReviewsSummary>(`/companies/${companyId}/reviews`, {
    params: relationshipType ? { relationshipType } : undefined,
  });
  return data;
}

export async function getReviewEligibility(companyId: number): Promise<ReviewEligibility> {
  const { data } = await apiClient.get<ReviewEligibility>(`/companies/${companyId}/reviews/eligibility`);
  return data;
}

export async function submitCompanyReview(companyId: number, payload: SubmitCompanyReviewRequest): Promise<void> {
  await apiClient.post(`/companies/${companyId}/reviews`, payload);
}

export async function respondToReview(reviewId: number, response: string): Promise<void> {
  await apiClient.post(`/reviews/${reviewId}/respond`, { response });
}
