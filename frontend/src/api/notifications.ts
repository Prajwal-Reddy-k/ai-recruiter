import apiClient from "./client";
import type { AppNotification } from "../types";

export async function getMyNotifications(): Promise<AppNotification[]> {
  const { data } = await apiClient.get<AppNotification[]>("/notifications");
  return data;
}

export async function getUnreadCount(): Promise<number> {
  const { data } = await apiClient.get<{ count: number }>("/notifications/unread-count");
  return data.count;
}

export async function markNotificationRead(id: number): Promise<void> {
  await apiClient.post(`/notifications/${id}/read`);
}
