import apiClient from "./client";
import type { CreateReferralRequest, CreateReferralResponse, Referral, ReferralTokenPreview } from "../types";

export async function createReferral(request: CreateReferralRequest): Promise<CreateReferralResponse> {
  const { data } = await apiClient.post<CreateReferralResponse>("/referrals", request);
  return data;
}

export async function getMyReferrals(): Promise<Referral[]> {
  const { data } = await apiClient.get<Referral[]>("/referrals/my");
  return data;
}

export async function getCompanyReferrals(): Promise<Referral[]> {
  const { data } = await apiClient.get<Referral[]>("/referrals/company");
  return data;
}

export async function resolveReferralToken(token: string): Promise<ReferralTokenPreview> {
  const { data } = await apiClient.get<ReferralTokenPreview>(`/referrals/token/${encodeURIComponent(token)}`);
  return data;
}
