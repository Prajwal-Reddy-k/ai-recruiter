import { test, expect } from "@playwright/test";
import { login, logout, resetSeed, SEEDED_CANDIDATE, SEEDED_RECRUITER, uniqueJobTitle } from "../helpers";

test.describe("apply/send offer/accept offer -> Hired", () => {
  test.beforeEach(async ({ request }) => {
    await resetSeed(request);
  });

  test("candidate accepting an offer moves the application to Hired", async ({ page }) => {
    const jobTitle = uniqueJobTitle("E2E Offer Role");

    await login(page, SEEDED_RECRUITER.email, SEEDED_RECRUITER.password);
    await page.goto("/post-job");
    await page.locator("#job-title").fill(jobTitle);
    await page.locator("#job-description").fill("An end-to-end test job posting for the offer flow.");
    await page.getByLabel("Remote — India").check();
    await page.getByRole("button", { name: "Publish" }).click();
    await expect(page).toHaveURL(/\/jobs\/\d+$/);
    const jobId = page.url().match(/\/jobs\/(\d+)$/)![1];
    await logout(page);

    await login(page, SEEDED_CANDIDATE.email, SEEDED_CANDIDATE.password);
    await page.goto(`/jobs/${jobId}`);
    await page.getByRole("button", { name: "Apply to this job" }).click();
    await page.getByRole("button", { name: "Submit application" }).click();
    await expect(page.getByText(/applied|application submitted/i).first()).toBeVisible({ timeout: 10_000 });
    await logout(page);

    // Recruiter opens the applicant's application detail page and sends an offer.
    await login(page, SEEDED_RECRUITER.email, SEEDED_RECRUITER.password);
    await page.goto(`/jobs/${jobId}/applicants`);
    const applicationHref = await page.locator('a[href^="/applications/"]').first().getAttribute("href");
    await page.goto(applicationHref!);

    await page.getByRole("button", { name: "Create offer" }).click();
    await page.locator("#offer-salary").fill("1500000");
    const joining = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10);
    const expiry = new Date(Date.now() + 14 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10);
    await page.locator("#offer-joining").fill(joining);
    await page.locator("#offer-expiry").fill(expiry);
    await page.getByLabel(/remote/i).first().check().catch(() => {});
    await page.getByRole("button", { name: "Save draft" }).click();
    await expect(page.getByRole("button", { name: "Send offer" })).toBeVisible({ timeout: 10_000 });
    await page.getByRole("button", { name: "Send offer" }).click();
    await expect(page.getByRole("button", { name: "Withdraw offer" })).toBeVisible({ timeout: 10_000 });
    await logout(page);

    // Candidate accepts the offer.
    await login(page, SEEDED_CANDIDATE.email, SEEDED_CANDIDATE.password);
    await page.goto(applicationHref!);
    await page.getByRole("button", { name: "Accept offer" }).click();
    await page.getByRole("button", { name: "Accept offer", exact: true }).last().click();

    await expect(page.getByText("Hired", { exact: true }).first()).toBeVisible({ timeout: 10_000 });
  });
});
