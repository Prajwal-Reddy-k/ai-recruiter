import apiClient from "./client";
import type { IndiaLocationCatalog, LocationSuggestion } from "../types";

export async function searchLocations(query: string): Promise<LocationSuggestion[]> {
  const { data } = await apiClient.get<LocationSuggestion[]>("/locations/search", {
    params: { q: query },
  });
  return data;
}

let cachedCatalog: IndiaLocationCatalog | null = null;

export async function getIndiaLocationCatalog(): Promise<IndiaLocationCatalog> {
  if (cachedCatalog) return cachedCatalog;
  const { data } = await apiClient.get<IndiaLocationCatalog>("/locations/india");
  cachedCatalog = data;
  return data;
}
