import apiClient from "./client";

export type ReportedEntityTypeValue = "Job" | "Company" | "Message" | "User";
export const reportedEntityTypeToNumber: Record<ReportedEntityTypeValue, number> = {
  Job: 1,
  Company: 2,
  Message: 3,
  User: 4,
};

export type ReportReasonValue = "Spam" | "FraudulentJob" | "InappropriateContent" | "FakeCompany" | "Harassment" | "Other";
export const reportReasonToNumber: Record<ReportReasonValue, number> = {
  Spam: 1,
  FraudulentJob: 2,
  InappropriateContent: 3,
  FakeCompany: 4,
  Harassment: 5,
  Other: 6,
};

export const REPORT_REASON_LABELS: { value: ReportReasonValue; label: string }[] = [
  { value: "Spam", label: "Spam" },
  { value: "FraudulentJob", label: "Fraudulent Job" },
  { value: "InappropriateContent", label: "Inappropriate Content" },
  { value: "FakeCompany", label: "Fake Company" },
  { value: "Harassment", label: "Harassment" },
  { value: "Other", label: "Other" },
];

/// Submits a report against any of the four reportable entity types. Distinct from
/// api/reports.ts, which handles recruiter analytics reports (a pre-existing, unrelated
/// feature) — kept in a separate module to avoid naming confusion.
export async function submitReport(
  entityType: ReportedEntityTypeValue,
  entityId: number,
  reason: ReportReasonValue,
  details?: string
): Promise<void> {
  await apiClient.post("/reports", {
    entityType: reportedEntityTypeToNumber[entityType],
    entityId,
    reason: reportReasonToNumber[reason],
    details,
  });
}
