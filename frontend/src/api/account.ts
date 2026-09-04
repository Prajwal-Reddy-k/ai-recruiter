import apiClient from "./client";
import type {
  AccountDeletionStatus,
  AccountExport,
  ChangePasswordRequest,
  NotificationPreferences,
  PrivacySummary,
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

export async function requestAccountDeletion(payload: RequestAccountDeletionRequest): Promise<AccountDeletionStatus> {
  const { data } = await apiClient.post<AccountDeletionStatus>("/account/request-deletion", payload);
  return data;
}

export async function cancelAccountDeletion(): Promise<AccountDeletionStatus> {
  const { data } = await apiClient.post<AccountDeletionStatus>("/account/cancel-deletion");
  return data;
}

export async function getPrivacySummary(): Promise<PrivacySummary> {
  const { data } = await apiClient.get<PrivacySummary>("/account/privacy");
  return data;
}

export async function getAccountDataExportJson(): Promise<AccountExport> {
  const { data } = await apiClient.get<AccountExport>("/account/export", { params: { format: "json" } });
  return data;
}

export async function downloadAccountDataExportCsv(): Promise<Blob> {
  const { data } = await apiClient.get("/account/export", { params: { format: "csv" }, responseType: "blob" });
  return data;
}
