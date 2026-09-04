import apiClient from "./client";
import type { CompanyVerificationStatusDto, SubmitCompanyVerificationRequest } from "../types";

export async function submitCompanyVerification(payload: SubmitCompanyVerificationRequest): Promise<CompanyVerificationStatusDto> {
  const { data } = await apiClient.post<CompanyVerificationStatusDto>("/companies/verification", payload);
  return data;
}

export async function getMyCompanyVerificationStatus(): Promise<CompanyVerificationStatusDto> {
  const { data } = await apiClient.get<CompanyVerificationStatusDto>("/companies/verification");
  return data;
}
