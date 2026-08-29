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
