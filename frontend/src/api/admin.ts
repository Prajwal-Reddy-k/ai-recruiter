import apiClient from "./client";
import type { AdminCompany, AdminJob, AdminUser, AuditLogEntry, JobReport } from "../types";

export type ModerationStatusValue = "Approved" | "Hidden" | "Removed";
const moderationStatusToNumber: Record<ModerationStatusValue, number> = {
  Approved: 1,
  Hidden: 2,
  Removed: 3,
};

export type ReportStatusValue = "Pending" | "Reviewed" | "Dismissed";
const reportStatusToNumber: Record<ReportStatusValue, number> = {
  Pending: 1,
  Reviewed: 2,
  Dismissed: 3,
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

export async function getAdminReports(): Promise<JobReport[]> {
  const { data } = await apiClient.get<JobReport[]>("/admin/reports");
  return data;
}

export async function moderateJob(jobId: number, status: ModerationStatusValue): Promise<AdminJob> {
  const { data } = await apiClient.post<AdminJob>(`/admin/jobs/${jobId}/moderate`, {
    moderationStatus: moderationStatusToNumber[status],
  });
  return data;
}

export async function resolveReport(reportId: number, status: ReportStatusValue, resolutionNote?: string): Promise<void> {
  await apiClient.post(`/admin/reports/${reportId}/resolve`, {
    status: reportStatusToNumber[status],
    resolutionNote,
  });
}

export async function getAuditLog(): Promise<AuditLogEntry[]> {
  const { data } = await apiClient.get<AuditLogEntry[]>("/admin/audit-log");
  return data;
}
