import apiClient from "./client";
import type { AdminCompany, AdminJob, AdminUser, AuditLogEntry, Report } from "../types";

export type ModerationStatusValue = "Approved" | "Hidden" | "Removed";
const moderationStatusToNumber: Record<ModerationStatusValue, number> = {
  Approved: 1,
  Hidden: 2,
  Removed: 3,
};

export type ReportStatusValue = "Open" | "UnderReview" | "Resolved" | "Dismissed";
const reportStatusToNumber: Record<ReportStatusValue, number> = {
  Open: 1,
  UnderReview: 2,
  Resolved: 3,
  Dismissed: 4,
};

export async function getAdminUsers(): Promise<AdminUser[]> {
  const { data } = await apiClient.get<AdminUser[]>("/admin/users");
  return data;
}

export async function getAdminCompanies(): Promise<AdminCompany[]> {
  const { data } = await apiClient.get<AdminCompany[]>("/admin/companies");
  return data;
}

export async function getAdminJobs(): Promise<AdminJob[]> {
  const { data } = await apiClient.get<AdminJob[]>("/admin/jobs");
  return data;
}

export async function getAdminReports(): Promise<Report[]> {
  const { data } = await apiClient.get<Report[]>("/admin/reports");
  return data;
}

export async function moderateJob(jobId: number, status: ModerationStatusValue): Promise<AdminJob> {
  const { data } = await apiClient.post<AdminJob>(`/admin/jobs/${jobId}/moderate`, {
    moderationStatus: moderationStatusToNumber[status],
  });
  return data;
}

export async function setReportStatus(reportId: number, status: ReportStatusValue, note?: string): Promise<void> {
  await apiClient.post(`/admin/reports/${reportId}/status`, {
    status: reportStatusToNumber[status],
    note,
  });
}

export async function addReportNote(reportId: number, note: string): Promise<void> {
  await apiClient.post(`/admin/reports/${reportId}/note`, { note });
}

export async function suspendUser(userId: number): Promise<void> {
  await apiClient.post(`/admin/users/${userId}/suspend`);
}

export async function reactivateUser(userId: number): Promise<void> {
  await apiClient.post(`/admin/users/${userId}/reactivate`);
}

export async function getAuditLog(): Promise<AuditLogEntry[]> {
  const { data } = await apiClient.get<AuditLogEntry[]>("/admin/audit-log");
  return data;
}
