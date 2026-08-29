import apiClient from "./client";
import type { Interview, ProposeInterviewRequest, RespondInterviewRequest, UpcomingInterview } from "../types";

export async function proposeInterview(applicationId: number, payload: ProposeInterviewRequest): Promise<Interview> {
  const { data } = await apiClient.post<Interview>(`/applications/${applicationId}/interviews`, payload);
  return data;
}

export async function getInterviewsForApplication(applicationId: number): Promise<Interview[]> {
  const { data } = await apiClient.get<Interview[]>(`/applications/${applicationId}/interviews`);
  return data;
}

export async function respondToInterview(interviewId: number, payload: RespondInterviewRequest): Promise<Interview> {
  const { data } = await apiClient.post<Interview>(`/interviews/${interviewId}/respond`, payload);
  return data;
}

export async function getUpcomingInterviews(): Promise<UpcomingInterview[]> {
  const { data } = await apiClient.get<UpcomingInterview[]>("/interviews/upcoming");
  return data;
}

export async function downloadInterviewIcs(interviewId: number): Promise<Blob> {
  const { data } = await apiClient.get(`/interviews/${interviewId}/calendar.ics`, { responseType: "blob" });
  return data;
}
