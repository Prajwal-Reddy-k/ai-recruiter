import apiClient from "./client";
import type { CareerGoal, CareerGoalsSummary, UpsertCareerGoalRequest } from "../types";

const BASE = "/career-goals";

export async function getMyCareerGoals(status?: string): Promise<CareerGoalsSummary> {
  const { data } = await apiClient.get<CareerGoalsSummary>(BASE, { params: status ? { status } : undefined });
  return data;
}

export async function createCareerGoal(payload: UpsertCareerGoalRequest): Promise<CareerGoal> {
  const { data } = await apiClient.post<CareerGoal>(BASE, payload);
  return data;
}

export async function updateCareerGoal(id: number, payload: UpsertCareerGoalRequest): Promise<CareerGoal> {
  const { data } = await apiClient.put<CareerGoal>(`${BASE}/${id}`, payload);
  return data;
}

export async function deleteCareerGoal(id: number): Promise<void> {
  await apiClient.delete(`${BASE}/${id}`);
}
