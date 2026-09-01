import apiClient from "./client";
import type { RecruiterReport } from "../types";

export interface ReportDateRange {
  fromUtc?: string;
  toUtc?: string;
}

export async function getRecruiterReport(range: ReportDateRange): Promise<RecruiterReport> {
  const { data } = await apiClient.get<RecruiterReport>("/recruiters/reports", { params: range });
  return data;
}

async function exportCsv(path: string, range: ReportDateRange): Promise<Blob> {
  const { data } = await apiClient.get(`/recruiters/reports/${path}`, { params: range, responseType: "blob" });
  return data;
}

export const exportJobsCsv = (range: ReportDateRange) => exportCsv("export/jobs", range);
export const exportApplicantsCsv = (range: ReportDateRange) => exportCsv("export/applicants", range);
export const exportInterviewsCsv = (range: ReportDateRange) => exportCsv("export/interviews", range);
export const exportFunnelCsv = (range: ReportDateRange) => exportCsv("export/funnel", range);
