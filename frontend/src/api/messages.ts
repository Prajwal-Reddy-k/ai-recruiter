import apiClient from "./client";
import type { ConversationSummary, Message } from "../types";

export async function getInbox(): Promise<ConversationSummary[]> {
  const { data } = await apiClient.get<ConversationSummary[]>("/messages/inbox");
  return data;
}

export async function getUnreadMessageCount(): Promise<number> {
  const { data } = await apiClient.get<number>("/messages/unread-count");
  return data;
}

export async function getMessageThread(applicationId: number): Promise<Message[]> {
  const { data } = await apiClient.get<Message[]>(`/messages/applications/${applicationId}`);
  return data;
}

export async function sendMessage(applicationId: number, body: string): Promise<Message> {
  const { data } = await apiClient.post<Message>(`/messages/applications/${applicationId}`, { body });
  return data;
}

export async function markThreadRead(applicationId: number): Promise<void> {
  await apiClient.post(`/messages/applications/${applicationId}/read`);
}
