import apiClient from "./client";
import type {
  Interview,
  InterviewTypeValue,
  RescheduleInterviewRequest,
  RespondInterviewRequest,
  ScheduleInterviewRequest,
  UpcomingInterview,
} from "../types";

const interviewTypeToNumber: Record<InterviewTypeValue, number> = {
  Online: 1,
  Phone: 2,
  InPerson: 3,
};

function toWireType(payload: { type?: InterviewTypeValue }) {
  return payload.type ? interviewTypeToNumber[payload.type] : undefined;
}

export async function scheduleInterview(applicationId: number, payload: ScheduleInterviewRequest): Promise<Interview> {
  const { data } = await apiClient.post<Interview>(`/applications/${applicationId}/interviews`, {
    ...payload,
    type: interviewTypeToNumber[payload.type],
  });
  return data;
}

export async function rescheduleInterview(interviewId: number, payload: RescheduleInterviewRequest): Promise<Interview> {
  const { data } = await apiClient.put<Interview>(`/interviews/${interviewId}/reschedule`, {
    ...payload,
    type: toWireType(payload),
  });
  return data;
}

export async function cancelInterview(interviewId: number): Promise<Interview> {
  const { data } = await apiClient.post<Interview>(`/interviews/${interviewId}/cancel`);
  return data;
}

export async function completeInterview(interviewId: number): Promise<Interview> {
  const { data } = await apiClient.post<Interview>(`/interviews/${interviewId}/complete`);
  return data;
}

export async function acceptInterview(interviewId: number, payload: RespondInterviewRequest = {}): Promise<Interview> {
  const { data } = await apiClient.post<Interview>(`/interviews/${interviewId}/accept`, payload);
  return data;
}

export async function declineInterview(interviewId: number, payload: RespondInterviewRequest = {}): Promise<Interview> {
  const { data } = await apiClient.post<Interview>(`/interviews/${interviewId}/decline`, payload);
  return data;
}

export async function getInterviewsForApplication(applicationId: number): Promise<Interview[]> {
  const { data } = await apiClient.get<Interview[]>(`/applications/${applicationId}/interviews`);
  return data;
}

export async function getMyInterviews(status?: string): Promise<Interview[]> {
  const { data } = await apiClient.get<Interview[]>("/interviews/mine", { params: status ? { status } : undefined });
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
