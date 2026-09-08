import { defineConfig, devices } from "@playwright/test";

const BACKEND_PORT = 5199;
const FRONTEND_PORT = 4173;
const BACKEND_URL = `http://localhost:${BACKEND_PORT}`;
const FRONTEND_URL = `http://localhost:${FRONTEND_PORT}`;

// CI (and anyone who already has both servers running locally) can skip Playwright's own
// server management by setting E2E_SKIP_WEBSERVER=1 and starting them separately — this is
// what the GitHub Actions workflow does, since it needs the backend up before `dotnet test`
// runs too, not just before Playwright starts.
const skipWebServer = process.env.E2E_SKIP_WEBSERVER === "1";

export default defineConfig({
  testDir: "./e2e/tests",
  fullyParallel: false, // specs share one seeded database via the reset-seed endpoint
  workers: 1, // must run strictly sequentially — see above
  retries: process.env.CI ? 1 : 0,
  reporter: [["html", { open: "never" }]],
  use: {
    baseURL: FRONTEND_URL,
    trace: "retain-on-failure",
  },
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"] } },
  ],
  webServer: skipWebServer
    ? undefined
    : [
        {
          // --no-launch-profile is required — otherwise `dotnet run` applies launchSettings.json's
          // default "http" profile, whose own ASPNETCORE_ENVIRONMENT=Development overrides the
          // env var passed below, and the app never actually starts in the Testing environment.
          command: `dotnet run --project ../src/AIRecruiter.API --no-launch-profile --urls ${BACKEND_URL}`,
          url: `${BACKEND_URL}/health`,
          reuseExistingServer: !process.env.CI,
          timeout: 120_000,
          env: { ASPNETCORE_ENVIRONMENT: "Testing" },
        },
        {
          // `npm run dev` (not build+preview) — Vite only substitutes import.meta.env.VITE_*
          // at build time, so a prebuilt `preview` bundle would ignore the env var below and
          // keep pointing at whatever VITE_API_BASE_URL was set when it was last built. The
          // dev server picks up env vars fresh on every start.
          command: `npm run dev -- --port ${FRONTEND_PORT} --strictPort`,
          url: FRONTEND_URL,
          reuseExistingServer: !process.env.CI,
          timeout: 60_000,
          env: { VITE_API_BASE_URL: `${BACKEND_URL}/api` },
        },
      ],
});

export const testConfig = { BACKEND_URL, FRONTEND_URL };
