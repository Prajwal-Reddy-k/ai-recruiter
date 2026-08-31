import apiClient from "./client";
import type { AuthResponse, UserRole } from "../types";

export interface RegisterPayload {
  fullName: string;
  email: string;
  password: string;
  role: UserRole;
}

export interface LoginPayload {
  email: string;
  password: string;
}

const roleToNumber: Record<UserRole, number> = {
  Candidate: 1,
  Recruiter: 2,
  Admin: 3,
};

export async function register(payload: RegisterPayload): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>("/auth/register", {
    ...payload,
    role: roleToNumber[payload.role],
  });
  return data;
}

export async function login(payload: LoginPayload): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>("/auth/login", payload);
  return data;
}

export interface ForgotPasswordResponse {
  message: string;
}

export interface VerifyResetCodeResponse {
  resetToken: string;
  expiresAtUtc: string;
}

export interface ResetPasswordResponse {
  message: string;
}

export async function forgotPassword(email: string): Promise<ForgotPasswordResponse> {
  const { data } = await apiClient.post<ForgotPasswordResponse>("/auth/forgot-password", { email });
  return data;
}

export async function verifyResetCode(email: string, code: string): Promise<VerifyResetCodeResponse> {
  const { data } = await apiClient.post<VerifyResetCodeResponse>("/auth/verify-reset-code", { email, code });
  return data;
}

export async function resetPassword(resetToken: string, newPassword: string, confirmPassword: string): Promise<ResetPasswordResponse> {
  const { data } = await apiClient.post<ResetPasswordResponse>("/auth/reset-password", {
    resetToken,
    newPassword,
    confirmPassword,
  });
  return data;
}
