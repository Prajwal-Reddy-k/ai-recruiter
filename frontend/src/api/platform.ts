import apiClient from "./client";
import type { PlatformStats } from "../types";

export async function getPlatformStats(): Promise<PlatformStats> {
  const { data } = await apiClient.get<PlatformStats>("/platform/stats");
  return data;
}
