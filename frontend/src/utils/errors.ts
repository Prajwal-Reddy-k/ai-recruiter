import { isAxiosError } from "axios";
import type { ApiProblem } from "../types";

export function getErrorMessage(err: unknown, fallback = "Something went wrong. Please try again."): string {
  if (isAxiosError<ApiProblem>(err)) {
    return err.response?.data?.detail ?? err.response?.data?.title ?? fallback;
  }
  return fallback;
}

export function getErrorCode(err: unknown): string | undefined {
  if (isAxiosError<ApiProblem>(err)) {
    return err.response?.data?.errorCode;
  }
  return undefined;
}
