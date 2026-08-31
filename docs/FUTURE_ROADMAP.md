# Future Roadmap

## Table of Contents
- [Currently Implemented](#currently-implemented)
- [Partially Implemented](#partially-implemented)
- [Next Practical Features](#next-practical-features)
- [Production-Scale Improvements](#production-scale-improvements)
- [Optional Future AI Features](#optional-future-ai-features)
- [Deployment / DevOps Improvements](#deployment--devops-improvements)

## Currently Implemented

See [PROJECT_OVERVIEW.md § Feature Status](PROJECT_OVERVIEW.md#feature-status) for the full, verified list. In summary: auth for 3 roles, India-only structured job locations, job search/filtering, the full job lifecycle (draft/publish/close/reopen/archive/duplicate/edit), company onboarding & public profiles, applications with a full status history, explainable local resume matching, an accessible Kanban applicant board, interview scheduling with `.ics` export, saved jobs, job alerts (with live matches), company-scoped recruiter candidate search with CSV export, privacy-conscious view tracking, recruiter analytics, in-app notifications, admin moderation + reporting, and an audit trail.

## Partially Implemented

| Item | What exists | What's missing |
|---|---|---|
| Location autocomplete (Nominatim) | A working, rate-limited, cached backend endpoint (`GET /api/locations/search`) and a `LocationAutocomplete.tsx` component | Not wired into any current form — superseded by the India-only structured selector. Either remove it as dead code, or repurpose it for a future "outside India" mode. |
| "Recently viewed jobs" (candidate dashboard) | A UI section exists | Explicitly backed by sample data — there's no actual per-candidate view-history table; would need a new `JobView(CandidateProfileId?, JobPostingId, ViewedAt)`-style table (distinct from the anonymous view-count mechanism) |
| External job search (Adzuna) | Fully functional when credentials are configured, gracefully disabled otherwise | Still a read-only, apply-elsewhere feature by design — not a gap, but worth noting it's optional/config-gated rather than "always on" |

## Next Practical Features

Ordered roughly by value-to-effort ratio for a project already at this maturity level.

### 1. Password reset & email verification
**Why it matters**: Table-stakes for any real account system; currently missing entirely.
**Approach**: A time-limited, single-use token stored on the `User` (or a new `PasswordResetToken` table), an endpoint to request one and one to redeem it. Email *delivery* would need a provider — see [Deployment/DevOps](#deployment--devops-improvements) for how to keep this zero-cost during development (e.g. a local SMTP catcher like MailHog/Mailpit) versus production.
**Dependencies/risks**: Needs a delivery mechanism (see below) or, at minimum, a dev-only "show the reset link in the response" mode for local testing.
**Zero-cost feasible?**: Yes for local/dev via a fake SMTP catcher; a real free-tier email provider (e.g. a low-volume free tier) would be needed for a live demo deployment.

### 2. Refresh tokens
**Why it matters**: The current JWT simply expires and forces re-login; a refresh-token flow is standard for a smoother session experience.
**Approach**: Issue a longer-lived, single-use refresh token alongside the access token; store its hash server-side (a new table) with rotation on use.
**Dependencies/risks**: Needs careful invalidation/rotation logic to avoid becoming a security regression.
**Zero-cost feasible?**: Yes — purely a backend/database feature.

### 3. Automated end-to-end (browser) tests
**Why it matters**: Current test coverage is unit/service-level only (xUnit + EF Core InMemory); nothing exercises the real HTTP pipeline or the frontend.
**Approach**: Add Playwright (or similar) tests covering the core journeys already documented in [SETUP_AND_DEMO.md § Manual Test Checklist](SETUP_AND_DEMO.md#manual-test-checklist).
**Dependencies/risks**: Needs a seeded, resettable test database and a running instance of both frontend and backend in CI.
**Zero-cost feasible?**: Yes — Playwright is free and open source; CI minutes on GitHub Actions' free tier are sufficient for a project this size.

### 4. Bulk/paginated candidate search & job search
**Why it matters**: Current search endpoints return the full filtered result set with no pagination — fine at demo data volume, not fine at real scale.
**Approach**: Add `page`/`pageSize` query params and a total-count response wrapper, mirroring the pattern already used by the Adzuna external-search DTO (`ExternalJobSearchResult`).
**Dependencies/risks**: Frontend pages consuming these lists need pagination UI; low risk, mostly additive.
**Zero-cost feasible?**: Yes.

### 5. Structured alert-notification digest
**Why it matters**: Job-alert matches are currently only visible when the candidate opens the dashboard or alerts page — there's no proactive nudge.
**Approach**: Extend the existing `Notification` entity/service to also fire an in-app notification when a new job matches an active alert (computed on a schedule or on job publish), still with **no email/SMS delivery** to stay zero-cost.
**Dependencies/risks**: Needs a trigger point — either a lightweight scheduled check (see background jobs below) or hooking into `JobPostingService`'s publish path to re-check active alerts immediately.
**Zero-cost feasible?**: Yes — stays entirely in-app.

## Production-Scale Improvements

See also [SYSTEM_DESIGN.md § Production Scalability Discussion](SYSTEM_DESIGN.md#production-scalability-discussion) for the full table.

| Improvement | Why | Approach | Zero-cost? |
|---|---|---|---|
| Distributed cache (Redis) | The Adzuna response cache and view-dedup cache are process-local; won't work correctly behind a load balancer with multiple instances | Swap `IMemoryCache`/the in-memory dictionary for a Redis-backed implementation behind the same interfaces | Free tier available (e.g. a small managed Redis instance) for light production use; genuinely free for local dev via Docker |
| Object storage | Local disk resume storage doesn't work across multiple stateless instances | Add an S3/Azure Blob/GCS-backed `IResumeStorage` implementation | Free tiers exist for light use; not free at meaningful production scale |
| Database indexing pass | No performance-tuned indexes exist beyond the two correctness-critical unique indexes | Add composite/covering indexes once real query patterns and data volume are known (e.g. `JobApplications(JobPostingId, Status)`) | Yes — purely a migration |
| Rate limiting | No inbound rate limiting exists | ASP.NET Core's built-in rate-limiting middleware, especially on `/api/auth/*` and CSV export | Yes — built into the framework |
| Observability | `ILogger` only, no metrics/tracing | Structured logging (Serilog) + OpenTelemetry/Application Insights | Free/open-source options exist (Serilog, self-hosted OpenTelemetry collector); hosted APM products often aren't free at scale |
| Background job runner | Nothing currently runs outside a request | Introduce Hangfire (free, open-source) or a simple `IHostedService` for anything that shouldn't block a request | Yes, with the open-source option |

## Optional Future AI Features

These are explicitly **not implemented** and are listed here as ideas only — none should be assumed to exist:

- **LLM-assisted job-description writing help** for recruiters (e.g. suggest missing sections) — would require either a local model or a paid API call; if pursued while staying zero-cost, a small local/open-weight model would need to be self-hosted, which has real infrastructure cost even if the model itself is free.
- **Semantic (embedding-based) skill matching** instead of substring/TF-IDF matching, to catch synonyms (e.g. "JS" vs "JavaScript"). Could be built with a free local embedding model, but adds meaningful complexity and a new dependency; the current approach is deliberately simple and fully explainable, which is a real trade-off worth preserving unless embeddings are added *in addition to*, not instead of, the existing breakdown.
- **Auto-generated interview questions** from a job description + candidate profile — same cost/complexity trade-off as above; would need a clear disclaimer if ever added, consistent with the project's existing "decision support only" stance on the resume-matching score.

Any of these should preserve the project's current, deliberate stance: matching and scoring stay **explainable and decision-support-only**, never an automated accept/reject mechanism.

## Deployment / DevOps Improvements

| Improvement | Why | Approach | Zero-cost? |
|---|---|---|---|
| Containerization | No `Dockerfile`/`docker-compose.yml` exists in the repo today | Add a `Dockerfile` per project (API, and a static build of the frontend served via nginx or similar) plus a `docker-compose.yml` that also runs SQL Server for local integration testing | Yes — Docker itself is free; the SQL Server Docker image has a free developer-use license |
| CI pipeline | None configured | GitHub Actions workflow: restore → build → `dotnet test` → `npm run build`/`npm run lint` on every PR | Yes — GitHub Actions has a generous free tier for public/small private repos |
| CD pipeline | None configured | Build+push a container image on merge to main; deploy to a free/low-cost target (e.g. a free-tier PaaS, or a self-managed VM) | Depends on hosting target — several free tiers exist for small projects, but real production traffic will eventually require a paid tier |
| Environment-specific config validation | Configuration is read but never validated at startup (e.g. a missing `Jwt:Secret` would only fail at first token generation) | Add startup-time validation (`IValidateOptions<T>`) for required configuration sections | Yes |
