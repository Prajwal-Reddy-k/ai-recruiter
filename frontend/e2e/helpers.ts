import { type APIRequestContext, type Page, expect } from "@playwright/test";

export const BACKEND_URL = "http://localhost:5199";

/** Resets the dedicated E2E database back to its freshly-seeded state. Only reachable when the
 * backend is running with ASPNETCORE_ENVIRONMENT=Testing (see TestController.ResetSeed) — the
 * playwright.config.ts webServer entry always starts it that way. */
export async function resetSeed(request: APIRequestContext): Promise<void> {
  const response = await request.post(`${BACKEND_URL}/api/test/reset-seed`);
  expect(response.ok(), `reset-seed failed: ${response.status()}`).toBeTruthy();
}

export const SEEDED_RECRUITER = { email: "recruiter1@demo.airecruiter.dev", password: "Demo@123" };
export const SEEDED_CANDIDATE = { email: "candidate1@demo.airecruiter.dev", password: "Demo@123" };

export async function login(page: Page, email: string, password: string): Promise<void> {
  await page.goto("/login");
  await page.locator("#login-email").fill(email);
  await page.locator("#login-password").fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page).toHaveURL(/\/(candidate|recruiter)\/dashboard/);
}

export async function logout(page: Page): Promise<void> {
  await page.locator("button.user-menu-trigger").click();
  await page.getByRole("menuitem", { name: "Logout" }).click();
  await expect(page).toHaveURL(/\/login/);
}

/** A unique job title per test run, so specs never collide with each other or with seeded data
 * when running against a database that already has seeded jobs of the same generic titles. */
export function uniqueJobTitle(prefix: string): string {
  return `${prefix} ${Date.now()}`;
}
