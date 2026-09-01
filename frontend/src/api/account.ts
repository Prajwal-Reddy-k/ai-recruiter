import apiClient from "./client";
import type {
  ChangePasswordRequest,
  NotificationPreferences,
  RequestAccountDeletionRequest,
  UpdateUserDetailsRequest,
  UserDetails,
} from "../types";

export async function getMyDetails(): Promise<UserDetails> {
  const { data } = await apiClient.get<UserDetails>("/account/details");
  return data;
}

export async function updateMyDetails(payload: UpdateUserDetailsRequest): Promise<UserDetails> {
  const { data } = await apiClient.put<UserDetails>("/account/details", payload);
  return data;
}

export async function changePassword(payload: ChangePasswordRequest): Promise<void> {
  await apiClient.put("/account/password", payload);
}

export async function getNotificationPreferences(): Promise<NotificationPreferences> {
  const { data } = await apiClient.get<NotificationPreferences>("/account/notification-preferences");
  return data;
}

export async function updateNotificationPreferences(payload: NotificationPreferences): Promise<NotificationPreferences> {
  const { data } = await apiClient.put<NotificationPreferences>("/account/notification-preferences", payload);
  return data;
}

export async function requestAccountDeletion(payload: RequestAccountDeletionRequest): Promise<void> {
  await apiClient.post("/account/request-deletion", payload);
}
