# Project Overview

## Table of Contents
- [Elevator Pitch](#elevator-pitch)
- [Problem Statement](#problem-statement)
- [Target Users and Roles](#target-users-and-roles)
- [Core User Journeys](#core-user-journeys)
- [Feature Status](#feature-status)
- [Technology Stack](#technology-stack)
- [Architecture Summary](#architecture-summary)
- [India-Only Job Locations](#india-only-job-locations)
- [Free / Local-First Design Choices](#free--local-first-design-choices)
- [Current Limitations](#current-limitations)
- [Résumé Bullet Points](#résumé-bullet-points)
- [Documentation Accuracy Checklist](#documentation-accuracy-checklist)

## Elevator Pitch

**AI Recruiter** is a full-stack, India-focused recruitment platform — candidates search and apply to jobs with an explainable, locally-computed resume match score; recruiters manage a complete job lifecycle (draft → publish → close → archive), review applicants on a Kanban board, schedule interviews, and search across every candidate who has applied to their company. It is built end-to-end with a .NET 10 / ASP.NET Core Clean Architecture backend and a React 19 + TypeScript frontend, and runs entirely on free, self-hosted, or local tooling — no paid APIs are required for any core feature.

## Problem Statement

Job boards typically solve one half of the hiring problem well (candidate search *or* applicant tracking) and treat the other as an afterthought. This project was built to demonstrate a realistic, end-to-end hiring workflow in one codebase: structured job postings with a real lifecycle (not just "open/closed"), a transparent (not black-box) resume-matching score recruiters and candidates can both inspect, and recruiter tooling — a Kanban applicant pipeline, cross-job candidate search, CSV export, interview scheduling, and analytics — that goes beyond a simple "list of applicants" table. It is scoped to Indian locations specifically to keep the location model structured and validated end-to-end (state → city → locality) rather than a free-text field, which is a common weak point in portfolio job-board projects.

## Target Users and Roles

Three roles exist in the codebase (`UserRole` enum: `Candidate = 1`, `Recruiter = 2`, `Admin = 3`), each with role-gated backend endpoints (`[Authorize(Roles = "...")]`) and role-gated frontend routes (`<ProtectedRoute allowedRoles={[...]}>`):

| Role | Can do |
|---|---|
| **Candidate** | Register/login, build a profile, upload a resume, search/save/apply to jobs, create job alerts, track application status, respond to interview proposals |
| **Recruiter** | Register/login, onboard a company, create/edit/publish/close/archive/duplicate jobs, manage applicants (list + Kanban board), propose interviews, search candidates across their company's jobs, export candidates to CSV, view analytics |
| **Admin** | Seeded only (self-registration as Admin is blocked server-side) — view/manage users, companies, and jobs; moderate reported jobs (Approve/Hide/Remove); resolve reports; view the platform-wide audit log |

## Core User Journeys

### Candidate
`Register` → `Login` → `Candidate Dashboard` (profile completion, recommended jobs, saved jobs, alert matches, upcoming interviews) → `Job Search` (`/jobs`, filterable by state/city/remote/job type/experience/skills/date posted) → `Save` and/or `Apply` to a job → `My Applications` (status timeline) → respond to an `Interview` proposal if one is scheduled.

### Recruiter
`Register` → `Login` → `Company Onboarding` (required before posting a job) → `Post a Job` (save as **Draft** or **Publish** immediately) → `Manage Jobs` (tabs for Draft/Published/Closed/Archived, with per-job application/view counts and a conversion rate) → `Applicant List` or `Kanban Board` per job → change application status / propose an interview → `Candidate Search` across the whole company's applicants → `Analytics` for a company-wide performance summary.

### Admin
`Login` (seeded account only) → `Admin` page tabs: Users, Companies, Jobs (moderate: Approve/Hide/Remove), Reports (resolve a candidate/recruiter's job report), Audit Log (platform-wide action history).

## Feature Status

| Feature | Description | Role(s) | Status | Frontend / API area |
|---|---|---|---|---|
| Registration & login | Email/password auth, BCrypt hashing, JWT issuance | All | **Implemented** | `RegisterPage.tsx`, `LoginPage.tsx` / `AuthController` |
| Forgot / reset password | Email verification code → short-lived reset token → new password, with rate limiting, code hashing, and JWT session invalidation on reset | All | **Implemented** | `ForgotPasswordPage.tsx`, `VerifyResetCodePage.tsx`, `ResetPasswordPage.tsx` / `AuthController`, `AuthService` |
| Role-based dashboards | Separate dashboards per role with real aggregated data | Candidate, Recruiter | **Implemented** | `CandidateDashboardPage.tsx`, `RecruiterDashboardPage.tsx` / `DashboardController` |
| India-only job locations | Structured State/City/Locality, validated server-side against a checked-in dataset | All | **Implemented** | `IndiaLocationSelector.tsx` / `LocationsController`, `IndiaLocationValidator` |
| Job search & filtering | State/city/remote/job type/experience/skills/date-posted, client-side filtering | Candidate | **Implemented** | `JobsPage.tsx`, `utils/jobFilters.ts` |
| Job posting & lifecycle | Draft → Publish → Close ⇄ Reopen → Archive, edit, duplicate; state-machine enforced server-side | Recruiter | **Implemented** | `PostJobPage.tsx`, `ManageJobsPage.tsx` / `JobsController`, `JobPostingService` |
| Company onboarding & public profile | Company details required before posting; public company page | Recruiter, public | **Implemented** | `OnboardingPage.tsx`, `CompanyProfilePage.tsx` / `RecruitersController`, `CompaniesController` |
| Apply to a job | Duplicate-proof (unique index), blocked on non-Open jobs | Candidate | **Implemented** | `JobDetailPage.tsx` / `ApplicationsController` |
| Explainable resume matching | Deterministic weighted scoring (skills/similarity/experience/education), computed at apply time from locally-extracted resume text | Candidate, Recruiter | **Implemented** | `ApplicationDetailPage.tsx` / `ResumeMatchingService` |
| Application status tracking | 9-state status with a full change history and candidate-visible notes | Candidate, Recruiter | **Implemented** | `ApplicationDetailPage.tsx` / `JobApplicationService` |
| Recruiter applicant management | Flat list + accessible Kanban board (drag-and-drop as an enhancement, not required) | Recruiter | **Implemented** | `RecruiterApplicantsPage.tsx`, `KanbanBoardPage.tsx` |
| Interview scheduling | Recruiter proposes a single time (UTC), candidate accepts/declines, reschedule/cancel/complete, `.ics` calendar download, IST display | Candidate, Recruiter | **Implemented** | `CandidateInterviewsPage.tsx`, `RecruiterInterviewsPage.tsx` / `InterviewsController`, `InterviewService` |
| Resume upload & storage | PDF/DOCX, extension + MIME + file-signature validation, local disk by default | Candidate | **Implemented** | `CandidateProfilePage.tsx` / `CandidateProfileService`, `LocalResumeStorage` |
| Resume storage on Cloudinary | Same upload flow, swapped storage backend when credentials are configured | Candidate | **Implemented (optional, config-gated)** | `CloudinaryResumeStorage` |
| Saved jobs | Save/unsave, dedicated page with saved date | Candidate | **Implemented** | `SavedJobsPage.tsx` / `SavedJobService` |
| Job alerts | Create/edit/activate/deactivate/delete; live-computed matches shown on the dashboard | Candidate | **Implemented** | `JobAlertsPage.tsx` / `JobAlertService` |
| Recruiter candidate search & CSV export | Cross-job, company-scoped search with filters/sort, candidate detail view, CSV export, audit-logged | Recruiter | **Implemented** | `CandidateSearchPage.tsx` / `RecruiterCandidatesController`, `CandidateSearchService` |
| Job view tracking | View counted per published job, de-duplicated per visitor for 30 minutes, in-memory only | Recruiter, public | **Implemented** | `ManageJobsPage.tsx`, `RecruiterAnalyticsPage.tsx` / `InMemoryViewDeduplicationService` |
| Recruiter analytics | Hiring funnel, applications by city, top candidate skills, views vs. applications | Recruiter | **Implemented** | `RecruiterAnalyticsPage.tsx` / `AnalyticsService` |
| Notifications | In-app bell with unread count, created on application/status/interview events | Candidate, Recruiter | **Implemented** | `NavBar.tsx` / `NotificationsController`, `NotificationService` |
| Admin moderation | User/company/job listing, job moderation (Approve/Hide/Remove), report resolution | Admin | **Implemented** | `AdminPage.tsx` / `AdminController` |
| Job reporting | Any signed-in user can report a job; feeds the admin Reports tab | Candidate, Recruiter | **Implemented** | `JobDetailPage.tsx` / `JobsController.Report` |
| Audit trail | Append-only log of registration, job/company/application/interview/export/moderation actions | Admin (view), all (as actor) | **Implemented** | `AdminPage.tsx` (Audit Log tab), `RecruiterDashboardPage.tsx` (company activity) / `AuditLogService` |
| External job search (Adzuna) | Read-only external listings, cannot apply in-app | Candidate | **Implemented (optional, config-gated)** | `ExternalJobsPage.tsx` / `ExternalJobsController` |
| Location autocomplete (Nominatim) | Free-text place lookup via OpenStreetMap | — | **Implemented but unused** — superseded by the India-only structured location model; the component and endpoint remain in the codebase but no form calls them | `LocationAutocomplete.tsx` (dead code) |
| Email/SMS delivery for alerts or notifications | — | — | **Planned / future scope** — deliberately out of scope for a zero-budget project; see [FUTURE_ROADMAP.md](FUTURE_ROADMAP.md) | — |
| Real-time (WebSocket) notifications | — | — | **Planned / future scope** — notifications are currently poll/fetch-on-demand, not pushed | — |

## Technology Stack

| Layer | Technology |
|---|---|
| Backend framework | ASP.NET Core Web API on **.NET 10** |
| Backend architecture | Clean Architecture — `Domain` → `Application` → `Infrastructure` → `API` |
| ORM / database | Entity Framework Core 10 (`Microsoft.EntityFrameworkCore.SqlServer`) against SQL Server / LocalDB |
| Auth | JWT Bearer tokens (`Microsoft.AspNetCore.Authentication.JwtBearer`), `System.IdentityModel.Tokens.Jwt`, BCrypt.Net-Next for password hashing |
| API docs | Swashbuckle (Swagger UI, Development only) |
| Resume parsing | `UglyToad.PdfPig` (PDF), `DocumentFormat.OpenXml` (DOCX) |
| Optional resume storage | `CloudinaryDotNet` (used only when credentials are configured; local disk otherwise) |
| Email (password reset) | Built-in `System.Net.Mail.SmtpClient` — no third-party package/service; works with any SMTP account when configured, falls back to a Development-only console logger or a Production-safe no-op otherwise |
| Frontend framework | React 19 + TypeScript, built with Vite 8 |
| Frontend routing | `react-router-dom` v7 |
| Frontend HTTP client | `axios` |
| Frontend icons | `lucide-react` |
| Frontend linting | `oxlint` |
| Testing | xUnit + EF Core InMemory provider + Moq (91 tests as of this writing) |

## Architecture Summary

The backend follows **Clean Architecture** across four projects:

- **`AIRecruiter.Domain`** — entities and enums only, no dependencies on anything else.
- **`AIRecruiter.Application`** — DTOs, service interfaces, pure validation/matching logic (`IndiaLocationValidator`, `ResumeMatchingService`, `ResumeFileValidator`); depends only on `Domain`.
- **`AIRecruiter.Infrastructure`** — EF Core `AppDbContext`, migrations, and every service implementation (auth, jobs, applications, interviews, candidate search, notifications, audit log, etc.); depends on `Application` and `Domain`.
- **`AIRecruiter.API`** — ASP.NET Core controllers, JWT/CORS/Swagger configuration, the global exception-handling middleware; depends on all three inner layers and wires everything together via dependency injection.

See [SYSTEM_DESIGN.md](SYSTEM_DESIGN.md) for diagrams and a full request-flow walkthrough.

## India-Only Job Locations

Every location field in the app (job postings, candidate profiles, companies) is a structured `City` / `State` / `Locality?` / `IsRemote` set, with `Country` implicitly `"India"` everywhere — there is no free-text location field and no external geocoding call in the active code path. A checked-in JSON dataset (`src/AIRecruiter.Infrastructure/Data/indian-locations.json`, 18 states/UTs and their cities) is embedded as a resource, loaded once by `IndianLocationCatalog`, and served to the frontend's cascading state → city selector via `GET /api/locations/india`. A server-side `IndiaLocationValidator` rejects any city/state combination not present in that dataset on every write path, so the UI cannot be bypassed by a raw API call.

## Free / Local-First Design Choices

This project was deliberately built to run at zero cost:

- **No paid APIs are required for any core feature.** The two optional integrations (Cloudinary for resume storage, Adzuna for external job search) are behind interfaces and only activate when credentials are present in configuration; the app falls back to local disk storage / a "disabled" external-search response otherwise.
- **Resume matching is fully local and deterministic** — a rule-based weighted scoring algorithm, not a call to an LLM or hosted AI service.
- **Interview `.ics` calendar files are hand-built** server-side (RFC 5545 minimal `VEVENT`) rather than using a paid calendar API.
- **Job-view de-duplication is in-memory** (a process-lifetime dictionary), not a new database table or a Redis dependency, keeping the feature genuinely free and simple to reason about.
- **Notifications and job-alert "matches" are computed on read**, not via a background job/queue or an email/SMS provider — everything stays inside the app.

## Current Limitations

- Single backend instance only — the in-memory view-deduplication cache and `IMemoryCache`-based Adzuna cache are process-local and won't work correctly if load-balanced across multiple instances without a shared cache.
- No automated end-to-end (browser) test suite — testing is unit/service-level only (xUnit + EF Core InMemory).
- No CI/CD pipeline is configured in this repository.
- No email/SMS delivery — all notifications and alerts are in-app only, by design.
- The "recently viewed jobs" section on the candidate dashboard is explicitly labeled sample data — there is no view-history tracking per candidate (only aggregate per-job view counts).
- `LocationAutocomplete.tsx` and the Nominatim search endpoint exist in the codebase but are not wired into any current form (superseded by the India-only selector) — dead code, not a bug, but worth knowing about if reading the source.
- Password reset exists (email verification code → short-lived reset token → new password); registration-time **email address verification does not** — a user can register with an email they don't control.

## Résumé Bullet Points

*(Based only on what is implemented and verified in this codebase.)*

- Built a full-stack recruitment platform (ASP.NET Core 10 + React 19/TypeScript) using Clean Architecture, with JWT authentication, role-based authorization, and ownership checks enforced on every protected endpoint.
- Designed and implemented a job-posting lifecycle state machine (Draft/Published/Closed/Archived) with server-side transition validation, edit/duplicate actions, and a recruiter-facing management UI with tabbed filtering and per-job analytics.
- Implemented a deterministic, explainable resume-matching engine (weighted skill coverage, TF-IDF/cosine text similarity, experience/education signals) running entirely locally — no external AI service — with a documented scoring breakdown surfaced to both candidate and recruiter.
- Built a company-scoped recruiter candidate-search feature spanning every job at a company (not just a single recruiter's own postings), with filter/sort, a candidate detail view, and a hand-rolled CSV export that is audited and excludes all sensitive fields.
- Designed a privacy-conscious job-view tracking system using hashed, in-memory, time-windowed visitor de-duplication — no personal data persisted, no cross-restart tracking.
- Modeled and validated India-specific structured job locations (state → city → locality) against a checked-in reference dataset, enforced identically on every create/update path server-side.
- Wrote 90+ unit/service tests (xUnit, EF Core InMemory, Moq) covering ownership checks, duplicate-prevention, state-machine transitions, and cross-tenant data isolation.

## Documentation Accuracy Checklist

The following source areas were directly inspected while writing this documentation package (not inferred from prior conversations, README history, or design intent):

- [x] All 12 controllers in `src/AIRecruiter.API/Controllers/` — every `[Http*]` route, `[Authorize]` attribute, and role requirement read directly from source.
- [x] All 14 entities in `src/AIRecruiter.Domain/Entities/` and all 7 enums in `src/AIRecruiter.Domain/Enums/`.
- [x] `AppDbContext.cs` — relationships, delete behaviors, unique indexes.
- [x] All 5 EF Core migrations under `src/AIRecruiter.Infrastructure/Persistence/Migrations/`.
- [x] All service interfaces (`Application/Interfaces/`) and their implementations (`Infrastructure/Services/`).
- [x] `Program.cs` — JWT configuration, CORS policy, Swagger registration, middleware order.
- [x] `ExceptionHandlingMiddleware.cs` and the `AppException` hierarchy.
- [x] Frontend `App.tsx` (every route), `AuthContext.tsx`, `ProtectedRoute.tsx`, and all files under `frontend/src/api/`.
- [x] `DataSeeder.cs` — exact seeded accounts, companies, and the demo password.
- [x] All test files under `tests/AIRecruiter.UnitTests/` — confirmed 91 passing tests via `dotnet test`.
- [x] `frontend/package.json` and backend `.csproj` files for exact dependency versions.
- [x] `indian-locations.json` — confirmed 18 states/UTs.
- [x] `appsettings.json` / `appsettings.Development.json` / `.env.development` / `.env.example` — confirmed no real secrets are committed, only placeholders.
