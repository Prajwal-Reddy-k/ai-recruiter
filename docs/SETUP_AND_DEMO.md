# Setup and Demo Guide

## Table of Contents
- [Prerequisites](#prerequisites)
- [Backend Setup](#backend-setup)
- [Database / Migration Setup](#database--migration-setup)
- [Frontend Setup](#frontend-setup)
- [Environment Variables](#environment-variables)
- [Local URLs](#local-urls)
- [CORS Troubleshooting](#cors-troubleshooting)
- [Seed Data & Demo Credentials](#seed-data--demo-credentials)
- [Backend Build & Test Commands](#backend-build--test-commands)
- [Frontend Install / Build / Lint Commands](#frontend-install--build--lint-commands)
- [Common Errors & Troubleshooting](#common-errors--troubleshooting)
- [Manual Test Checklist](#manual-test-checklist)
- [Production-Readiness Checklist](#production-readiness-checklist)

## Prerequisites

Verified working versions (this exact environment):

| Tool | Verified version | Notes |
|---|---|---|
| .NET SDK | 10.0.303 | `dotnet --version` |
| Node.js | 24.18.0 | `node --version` |
| npm | 12.0.1 | `npm --version` |
| SQL Server | LocalDB (ships with Visual Studio / SQL Server Express) | A full SQL Server instance also works — just change the connection string |
| `dotnet-ef` CLI | any version compatible with EF Core 10 | `dotnet tool install --global dotnet-ef` if not already installed |

## Backend Setup

```bash
cd src/AIRecruiter.API
dotnet restore
```

## Database / Migration Setup

Apply all 5 migrations to a fresh LocalDB database:

```bash
dotnet ef database update --project src/AIRecruiter.Infrastructure --startup-project src/AIRecruiter.API
```

If you already have a database from before the most recent migrations (`AddIndiaPlatformUpgrade` / `AddJobLifecycleAlertsAndCandidateSearch`), its seed data predates the current schema and the idempotent seeder will skip reseeding. Drop and recreate once:

```bash
dotnet ef database drop --project src/AIRecruiter.Infrastructure --startup-project src/AIRecruiter.API --force
dotnet ef database update --project src/AIRecruiter.Infrastructure --startup-project src/AIRecruiter.API
```

Then run the API — see below.

## Frontend Setup

```bash
cd frontend
npm install
npm run dev
```

## Environment Variables

**Backend** — never commit real values here; use `dotnet user-secrets` locally (see [SECURITY_AND_AUTH.md](SECURITY_AND_AUTH.md#secret-and-configuration-handling)):

```bash
cd src/AIRecruiter.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "<a-long-random-string-at-least-32-characters>"
# Optional integrations — only needed if you want to enable them:
dotnet user-secrets set "Cloudinary:CloudName" "<your-cloud-name>"
dotnet user-secrets set "Cloudinary:ApiKey" "<your-api-key>"
dotnet user-secrets set "Cloudinary:ApiSecret" "<your-api-secret>"
dotnet user-secrets set "Adzuna:AppId" "<your-app-id>"
dotnet user-secrets set "Adzuna:AppKey" "<your-app-key>"
dotnet user-secrets set "Nominatim:ContactEmail" "<you@example.com>"
# SMTP (forgot/reset password email) — any provider, no paid API. Omit entirely for local
# dev; a Development-only sender will log the verification code to the console instead.
dotnet user-secrets set "Smtp:Host" "<smtp.yourprovider.com>"
dotnet user-secrets set "Smtp:Port" "587"
dotnet user-secrets set "Smtp:Username" "<your-smtp-username>"
dotnet user-secrets set "Smtp:Password" "<your-smtp-password-or-app-password>"
dotnet user-secrets set "Smtp:SenderEmail" "<your-sender-address>"
dotnet user-secrets set "Smtp:SenderName" "AI Recruiter"
dotnet user-secrets set "Smtp:EnableSsl" "true"
```

### Local email testing without SMTP

If `Smtp:Host`/`Username`/`Password`/`SenderEmail` are not all set, the API automatically falls back to a Development-only email sender (`DevEmailSender`) that **logs the full email content — including the raw 6-digit code — to the console** instead of sending anything. This lets you exercise the entire forgot-password flow locally with zero setup. Look for a log block starting with `[DEV EMAIL — not actually sent, SMTP is not configured]` after calling `POST /api/auth/forgot-password`. This fallback is gated to `ASPNETCORE_ENVIRONMENT=Development` only — in any other environment, an unconfigured SMTP account instead uses a silent `NullEmailSender` that sends nothing and only logs a server-side warning, so a misconfigured production deployment fails safely (no email sent) rather than leaking codes to a log anyone might read.

**Frontend** (`frontend/.env.development` already ships with the correct local value; only override if your API runs on a different port):

```bash
VITE_API_BASE_URL=http://localhost:5087/api
```

## Local URLs

| Service | URL |
|---|---|
| API (http launch profile — use this one) | `http://localhost:5087` |
| Swagger UI (Development only) | `http://localhost:5087/swagger` |
| Frontend (Vite dev server) | `http://localhost:5173` (or the next free port if 5173 is taken — Vite auto-hops, and the backend's dev CORS policy allows any `localhost` origin) |

Run the API with the **"http"** launch profile specifically:
```bash
dotnet run --launch-profile http --project src/AIRecruiter.API
```
The API deliberately does not force an HTTPS redirect in Development (see below), so the plain-HTTP profile is the one to run against for local frontend/backend integration.

## CORS Troubleshooting

The frontend and API are different origins in local development, so the browser sends a CORS preflight (`OPTIONS`) before every `POST`/`PUT`/`PATCH`/`DELETE`. If cross-origin writes fail with something like *"Redirect is not allowed for a preflight request"*:

- Confirm you're running the API's **"http"** launch profile, not "https" — the https profile forces `UseHttpsRedirection()`, and browsers refuse to follow a redirect for a preflight request.
- Confirm `VITE_API_BASE_URL` points at `http://localhost:5087/api` (not `https://`).
- If you changed `Program.cs`, remember CORS middleware **must** run before `UseHttpsRedirection()` — see [SYSTEM_DESIGN.md](SYSTEM_DESIGN.md#cors-and-local-httphttps-configuration) for why the ordering matters.
- In Development, any `http://localhost:<port>` origin is allowed automatically — if your frontend runs somewhere other than `localhost` (e.g. a LAN IP), it will be rejected; use `localhost` for local development.

## Seed Data & Demo Credentials

On every startup **in Development only**, the API seeds a fixed set of demo accounts, companies, jobs, applications, interviews, saved jobs/alerts, and a job report if they aren't already present (checks for one known seed user and skips entirely if found — safe to restart repeatedly). All seeded accounts use the password **`Demo@123`**; every seeded email uses the domain `@demo.airecruiter.dev` so it's obviously not a production account.

| Role | Email | Notes |
|---|---|---|
| Admin | `admin@demo.airecruiter.dev` | Seeded directly — self-registration as Admin is blocked |
| Recruiter | `recruiter1@demo.airecruiter.dev` | Nimbus Cloud Systems, Bengaluru — already onboarded, has posted jobs |
| Recruiter | `recruiter2@demo.airecruiter.dev` | BluePeak Analytics, Hyderabad |
| Recruiter | `recruiter3@demo.airecruiter.dev` | Solstice Retail Group, Pune |
| Recruiter | `recruiter4@demo.airecruiter.dev` | Vertex FinTech Solutions, Mumbai |
| Recruiter | `recruiter5@demo.airecruiter.dev` | Northgate Systems, Chennai |
| Recruiter | `recruiter6@demo.airecruiter.dev` | Coral Health Informatics, Kochi — has a job hidden by moderation and a pending report, for the admin demo |
| Candidate | `candidate1@demo.airecruiter.dev` | Bengaluru — fully-filled profile, a few applications |
| Candidate | `candidate2@demo.airecruiter.dev` | Hyderabad — fully-filled profile |
| Candidate | `candidate3@demo.airecruiter.dev` | Pune — fully-filled profile, includes a Hired application |
| Candidate | `candidate4@demo.airecruiter.dev` | Mumbai — fully-filled profile |
| Candidate | `candidate5@demo.airecruiter.dev` | Chennai — fully-filled profile |
| Candidate | `candidate6@demo.airecruiter.dev` | Kolkata — fully-filled profile, has a Withdrawn application |
| Candidate | `candidate7@demo.airecruiter.dev` | Ahmedabad — fully-filled profile |
| Candidate | `candidate8@demo.airecruiter.dev` | Jaipur — deliberately sparse profile, shows the profile-completion card at a low score |

**Known limitation**: seeded candidates have no resume file on disk (that would require real uploaded files), so their seeded applications have no match score. Upload a real resume through the UI on any candidate account to see the actual local matching engine compute one.

## Backend Build & Test Commands

```bash
# Build the whole backend
dotnet build src/AIRecruiter.API/AIRecruiter.API.csproj

# Run the full unit test suite
dotnet test tests/AIRecruiter.UnitTests/AIRecruiter.UnitTests.csproj
```

At the time of writing, `dotnet test` reports **141 passing tests, 0 failed** — covering ownership checks, duplicate-prevention, job-status transitions, application-status restrictions, resume validation, resume-matching scoring, the full forgot/verify/reset password flow (rate limiting, code hashing/expiry/lockout, token reuse prevention, session invalidation), and interview scheduling (ownership, invalid/past times, overlap prevention, accept/decline/reschedule/cancel/complete transitions, `.ics` generation), using xUnit + the EF Core InMemory provider + Moq.

## Frontend Install / Build / Lint Commands

```bash
cd frontend
npm install        # install dependencies
npm run dev         # start the Vite dev server
npm run build        # tsc -b && vite build — type-checks then produces a production build
npm run lint         # oxlint
npm run preview       # preview the production build locally
```

## Common Errors & Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `Cross-Origin Request Blocked` / preflight redirect error | Running the API's "https" profile, or `VITE_API_BASE_URL` pointing at `https://` | Use the "http" launch profile and `http://localhost:5087/api` |
| `409 NOT_ONBOARDED` when posting a job | Recruiter hasn't completed company onboarding | Submit `POST /api/recruiters/me/onboarding` (or use the Company Setup page) first |
| `400` on job publish with a location error | Draft was saved without a valid India state/city, then published without adding one | Edit the job, set a valid state + city (from the dropdown, not free text), then publish |
| `401 Invalid email or password` on a known-good demo account | Database wasn't seeded (production build, or seeding skipped because a stale idempotency-check user already exists from an older schema) | Confirm you're running in Development, or drop/recreate the database per [Database / Migration Setup](#database--migration-setup) |
| Resume upload rejected as invalid file | File isn't actually a PDF/DOCX, or was renamed to have a `.pdf`/`.docx` extension without matching content | Upload a genuine PDF or DOCX under 5 MB |
| Swagger UI 404s | Running outside the Development environment | Swagger is only registered when `ASPNETCORE_ENVIRONMENT=Development` (the default for the "http" launch profile) |
| `dotnet ef` command not found | EF Core CLI tool not installed | `dotnet tool install --global dotnet-ef` |

## Manual Test Checklist

### Forgot / reset password
- [ ] From `/login`, click "Forgot password?" and submit a registered email — confirm you land on the verification-code screen.
- [ ] Submit an email that is *not* registered — confirm the exact same UI progression and no error revealing the email doesn't exist.
- [ ] Read the 6-digit code from the console log (`DevEmailSender`) if SMTP isn't configured, or from the real inbox if it is.
- [ ] Enter the code using keyboard only (type digits, confirm auto-advance; try Backspace and arrow keys; try pasting a 6-digit string).
- [ ] Confirm "Resend code" is disabled with a visible countdown for 60 seconds, then becomes clickable.
- [ ] Enter an intentionally wrong code 5 times — confirm the 6th attempt (even with the correct code) is rejected as locked out.
- [ ] Enter the correct code — confirm redirect to the new-password screen with live password-strength hints.
- [ ] Submit mismatched password/confirm-password — confirm a clear inline error, no request sent.
- [ ] Submit a valid new password — confirm redirect to `/login` with "Password updated successfully. Please sign in with your new password."
- [ ] Log in with the **old** password — confirm it now fails.
- [ ] Log in with the **new** password — confirm it succeeds.
- [ ] (Optional, via API) Capture a JWT before resetting the password, reset it, then call any authenticated endpoint with the old token — confirm `401`.
- [ ] Confirm `localStorage` never contains the verification code or either password at any point in the flow (only `token`/`user` after a real login).

### Candidate workflow
- [ ] Register a new Candidate account, confirm redirect to `/login` (not auto-logged-in) with the email prefilled.
- [ ] Log in, land on the Candidate Dashboard.
- [ ] Fill out the candidate profile (India location via the state → city selector).
- [ ] Upload a resume (PDF or DOCX).
- [ ] Browse `/jobs`, filter by state/city/remote/job type/experience/skills.
- [ ] Save a job (bookmark icon), confirm it appears on `/saved-jobs` with a saved date.
- [ ] Apply to a job with a cover note.
- [ ] Confirm the match score, matched/missing skills, and scoring explanation appear on the application detail page.
- [ ] Create a job alert, confirm live matching-job results appear.
- [ ] Attempt to apply to the same job twice — confirm a clear "already applied" error.
- [ ] Withdraw an application, confirm status updates to `Withdrawn`.

### Recruiter workflow
- [ ] Register a new Recruiter account.
- [ ] Complete company onboarding (India location required).
- [ ] Post a job as a **Draft** with an incomplete location — confirm it saves.
- [ ] Edit the draft, add a valid India location, then **Publish** it — confirm it now appears in public job search.
- [ ] Confirm a candidate can now find and apply to it.
- [ ] Open **Manage Jobs**, confirm tabs (Draft/Published/Closed/Archived) and per-job application/view counts.
- [ ] Close the job — confirm the confirmation dialog appears, and that a candidate can no longer apply to it after confirming.
- [ ] Reopen it, then Archive it — confirm Archive requires confirmation and the job can no longer be edited or reopened.
- [ ] Duplicate a job — confirm a new Draft copy is created.
- [ ] Open the applicant Kanban board, move a card between stages using the "Move to…" control.
- [ ] Propose an interview slot; as the candidate, accept it; confirm the `.ics` file downloads.
- [ ] Open **Candidate Search**, filter by skill/city/experience, open a candidate's detail view, export the filtered results as CSV and confirm the downloaded file has no resume content or sensitive fields.
- [ ] Open **Analytics**, confirm views/applications/conversion rate render (or a "No data yet" state for a job with zero views).

### Admin workflow (seeded account only)
- [ ] Log in as `admin@demo.airecruiter.dev`.
- [ ] View Users/Companies/Jobs/Reports tabs.
- [ ] Moderate the seeded hidden job (Approve/Hide/Remove).
- [ ] Resolve the seeded pending report.
- [ ] View the platform-wide Audit Log.

## Production-Readiness Checklist

This project is built for local/portfolio use. Before any real production deployment:

- [ ] Replace `Jwt:Secret` with a securely generated, environment-specific secret (never the placeholder value).
- [ ] Set `Cors:AllowedOrigins` explicitly to the real frontend origin(s) — the non-Development policy is deny-by-default.
- [ ] Enable HTTPS redirection (already gated to non-Development) and terminate TLS properly at the hosting layer.
- [ ] Move resume storage to a real object store (S3/Azure Blob/GCS) behind the existing `IResumeStorage` interface if not already using Cloudinary.
- [ ] Replace the in-memory view-dedup cache and Adzuna `IMemoryCache` with a shared cache (Redis) if deploying more than one API instance.
- [ ] Add structured logging/observability (see [SYSTEM_DESIGN.md § Production Scalability Discussion](SYSTEM_DESIGN.md#production-scalability-discussion)).
- [ ] Add rate limiting, especially on `/api/auth/*` and the CSV export endpoint.
- [ ] Set up CI (build + test on every PR) — none is configured in this repository yet.
- [ ] Review [SECURITY_AND_AUTH.md § Known Limitations](SECURITY_AND_AUTH.md#known-limitations-and-production-hardening) in full.
