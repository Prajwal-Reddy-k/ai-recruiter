import apiClient from "./client";
import type { ActivityTimelineEntry } from "../types";

export interface ActivityTimelineFilter {
  type?: string;
  from?: string;
  to?: string;
}

export async function getMyActivityTimeline(filter?: ActivityTimelineFilter): Promise<ActivityTimelineEntry[]> {
  const { data } = await apiClient.get<ActivityTimelineEntry[]>("/activity/timeline", {
    params: filter,
  });
  return data;
}
