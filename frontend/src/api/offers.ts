import apiClient from "./client";
import type { Offer, OfferDetail, UpsertOfferRequest, RespondToOfferRequest } from "../types";

export async function createOfferDraft(jobApplicationId: number, request: UpsertOfferRequest): Promise<Offer> {
  const { data } = await apiClient.post<Offer>(`/applications/${jobApplicationId}/offers`, request);
  return data;
}

export async function getOffersForApplication(jobApplicationId: number): Promise<Offer[]> {
  const { data } = await apiClient.get<Offer[]>(`/applications/${jobApplicationId}/offers`);
  return data;
}

export async function updateOfferDraft(offerId: number, request: UpsertOfferRequest): Promise<Offer> {
  const { data } = await apiClient.put<Offer>(`/offers/${offerId}`, request);
  return data;
}

export async function sendOffer(offerId: number): Promise<Offer> {
  const { data } = await apiClient.post<Offer>(`/offers/${offerId}/send`, {});
  return data;
}

export async function withdrawOffer(offerId: number): Promise<Offer> {
  const { data } = await apiClient.post<Offer>(`/offers/${offerId}/withdraw`, {});
  return data;
}

export async function getOfferDetail(offerId: number): Promise<OfferDetail> {
  const { data } = await apiClient.get<OfferDetail>(`/offers/${offerId}`);
  return data;
}

export async function getMyOffers(): Promise<Offer[]> {
  const { data } = await apiClient.get<Offer[]>("/offers/my");
  return data;
}

export async function respondToOffer(offerId: number, request: RespondToOfferRequest): Promise<Offer> {
  const { data } = await apiClient.post<Offer>(`/offers/${offerId}/respond`, request);
  return data;
}
