import { test, expect } from "@playwright/test";
import { login, logout, resetSeed, SEEDED_CANDIDATE, SEEDED_RECRUITER, uniqueJobTitle } from "../helpers";

test.describe("register/post job/apply/schedule/accept/download .ics", () => {
  test.beforeEach(async ({ request }) => {
    await resetSeed(request);
  });

  test("candidate accepts a proposed interview and downloads the .ics file", async ({ page }) => {
    const jobTitle = uniqueJobTitle("E2E Backend Engineer");

    // Recruiter posts a job.
    await login(page, SEEDED_RECRUITER.email, SEEDED_RECRUITER.password);
    await page.goto("/post-job");
    await page.locator("#job-title").fill(jobTitle);
    await page.locator("#job-description").fill("An end-to-end test job posting.");
    await page.getByLabel("Remote — India").check();
    await page.getByRole("button", { name: "Publish" }).click();
    await expect(page).toHaveURL(/\/jobs\/\d+$/);
    const jobId = page.url().match(/\/jobs\/(\d+)$/)![1];
    await logout(page);

    // Candidate applies.
    await login(page, SEEDED_CANDIDATE.email, SEEDED_CANDIDATE.password);
    await page.goto(`/jobs/${jobId}`);
    await page.getByRole("button", { name: "Apply to this job" }).click();
    await page.getByRole("button", { name: "Submit application" }).click();
    await expect(page.getByText(/applied|application submitted/i).first()).toBeVisible({ timeout: 10_000 });
    await logout(page);

    // Recruiter schedules an interview.
    await login(page, SEEDED_RECRUITER.email, SEEDED_RECRUITER.password);
    await page.goto(`/jobs/${jobId}/applicants`);
    await page.getByRole("button", { name: "Schedule interview" }).first().click();
    const future = new Date(Date.now() + 3 * 24 * 60 * 60 * 1000);
    const localValue = `${future.getFullYear()}-${String(future.getMonth() + 1).padStart(2, "0")}-${String(future.getDate()).padStart(2, "0")}T10:00`;
    await page.locator("#interview-datetime").fill(localValue);
    await page.getByRole("button", { name: "Send invitation" }).click();
    await expect(page.getByRole("button", { name: "Send invitation" })).not.toBeVisible({ timeout: 10_000 });
    await logout(page);

    // Candidate accepts and downloads the .ics file.
    await login(page, SEEDED_CANDIDATE.email, SEEDED_CANDIDATE.password);
    await page.goto("/interviews");
    await page.getByRole("button", { name: "Accept" }).first().click();
    await expect(page.getByRole("button", { name: /Add to calendar/ })).toBeVisible({ timeout: 10_000 });

    const [download] = await Promise.all([
      page.waitForEvent("download"),
      page.getByRole("button", { name: /Add to calendar/ }).click(),
    ]);
    expect(download.suggestedFilename()).toMatch(/\.ics$/);
  });
});
