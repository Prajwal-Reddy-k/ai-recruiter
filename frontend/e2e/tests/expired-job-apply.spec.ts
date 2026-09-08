import { test, expect } from "@playwright/test";
import { BACKEND_URL, login, resetSeed, SEEDED_CANDIDATE, SEEDED_RECRUITER, uniqueJobTitle } from "../helpers";

test.describe("negative path: applying to a closed job", () => {
  test.beforeEach(async ({ request }) => {
    await resetSeed(request);
  });

  test("candidate applying to a closed job sees a clear inline error, not a crash", async ({ page, request }) => {
    // Set up the closed job directly via the API (faster/more reliable than driving the
    // recruiter UI for state that isn't the point of this negative-path test).
    const recruiterLogin = await request.post(`${BACKEND_URL}/api/auth/login`, {
      data: { email: SEEDED_RECRUITER.email, password: SEEDED_RECRUITER.password },
    });
    expect(recruiterLogin.ok()).toBeTruthy();
    const { token } = await recruiterLogin.json();
    const authHeaders = { Authorization: `Bearer ${token}` };

    const createJob = await request.post(`${BACKEND_URL}/api/jobs`, {
      headers: authHeaders,
      data: {
        title: uniqueJobTitle("E2E Closed Role"),
        description: "A job that will be closed before a candidate tries to apply.",
        requiredSkillsCsv: null,
        minExperienceYears: null,
        maxExperienceYears: null,
        minSalary: null,
        maxSalary: null,
        city: null,
        state: null,
        locality: null,
        isRemote: true,
        jobType: 1,
        saveAsDraft: false,
      },
    });
    expect(createJob.ok()).toBeTruthy();
    const job = await createJob.json();

    const closeJob = await request.patch(`${BACKEND_URL}/api/jobs/${job.id}/status`, {
      headers: authHeaders,
      data: { status: 3 }, // JobStatus.Closed
    });
    expect(closeJob.ok()).toBeTruthy();

    // A direct API attempt to apply must be a clean 409, never a 500.
    const candidateLogin = await request.post(`${BACKEND_URL}/api/auth/login`, {
      data: { email: SEEDED_CANDIDATE.email, password: SEEDED_CANDIDATE.password },
    });
    const { token: candidateToken } = await candidateLogin.json();
    const apiApplyAttempt = await request.post(`${BACKEND_URL}/api/jobs/${job.id}/apply`, {
      headers: { Authorization: `Bearer ${candidateToken}` },
      data: {},
    });
    expect(apiApplyAttempt.status()).toBe(409);

    // And the candidate UI shows a clear inline error rather than a blank/crashed page.
    await login(page, SEEDED_CANDIDATE.email, SEEDED_CANDIDATE.password);
    await page.goto(`/jobs/${job.id}`);
    await expect(page.getByRole("heading", { name: job.title })).toBeVisible();
    await page.getByRole("button", { name: "Apply to this job" }).click();
    await page.getByRole("button", { name: "Submit application" }).click();

    await expect(page.locator(".error").first()).toBeVisible({ timeout: 10_000 });
  });
});
