# AI Recruiter

An India-focused recruitment platform — candidates search and apply to jobs with an explainable, locally-computed resume match score; recruiters run a complete job lifecycle (draft → publish → close → archive), manage applicants on a Kanban board, schedule interviews, and search candidates across their whole company with CSV export. Built end-to-end with a .NET 10 / ASP.NET Core Clean Architecture backend and a React 19 + TypeScript frontend, and designed to run entirely on free, self-hosted, or local tooling — no paid API is required for any core feature.

📖 **Full documentation package**: see [`docs/`](docs/) — start with [`docs/PROJECT_OVERVIEW.md`](docs/PROJECT_OVERVIEW.md) for the complete feature list, user journeys, and tech stack, or jump straight to a topic below.

## Screenshots

_No screenshots are checked into this repository yet._ To add some: run the app locally (see [Quick Start](#quick-start)), capture the job search page, a candidate dashboard, the recruiter Manage Jobs page, and the Kanban applicant board, save them under `docs/screenshots/`, and reference them here as `![Job search](docs/screenshots/job-search.png)`.

## Key Stack & Features

| | |
|---|---|
| Backend | ASP.NET Core Web API on .NET 10, Clean Architecture (Domain → Application → Infrastructure → API), EF Core 10 + SQL Server/LocalDB |
| Frontend | React 19 + TypeScript, Vite, react-router-dom, axios |
| Auth | JWT Bearer tokens, BCrypt password hashing, role-based (Candidate / Recruiter / Admin) + ownership authorization |
| Core features | India-only structured job locations · job draft/publish/close/archive lifecycle · applications with full status history · explainable local resume matching · Kanban applicant board · interview scheduling with `.ics` export · saved jobs & job alerts · company-scoped candidate search + CSV export · privacy-conscious job-view tracking · analytics · audit trail · secure forgot/reset password with email verification codes · candidate profile photo upload with server-side resize/validation · footer with deep-linked popular job-role searches · reusable job templates · recruiter–candidate in-app messaging · interview feedback scorecards · hiring-team roles & permissions · recruiter reports with CSV export · admin moderation & user suspension · candidate availability/preferences & profile-visibility controls · recruiter candidate-invitation workflow · job application deadlines & auto-expiry · installable Progressive Web App |
| Cost | Zero — every optional third-party integration (Cloudinary, Adzuna, SMTP) is behind an interface and only activates when credentials are configured; falls back gracefully otherwise |
| Tests | 272 passing (xUnit + EF Core InMemory + Moq) |

Full, verified feature-by-feature status table: [`docs/PROJECT_OVERVIEW.md`](docs/PROJECT_OVERVIEW.md#feature-status).

## Structure

```
AI Recruiter/
├── src/
│   ├── AIRecruiter.API             # ASP.NET Core Web API, controllers, auth, middleware
│   ├── AIRecruiter.Application     # DTOs, service interfaces, matching engine, validation
│   ├── AIRecruiter.Domain          # Entities, enums
│   └── AIRecruiter.Infrastructure  # EF Core DbContext, migrations, service implementations
├── tests/
│   └── AIRecruiter.UnitTests       # xUnit tests
├── frontend/                       # React + TypeScript (Vite)
├── docs/                           # Full documentation package (see below)
└── database/                       # Reserved for raw SQL scripts / seed data
```

## Quick Start

**Prerequisites**: .NET 10 SDK, Node.js 20+, SQL Server or LocalDB. See [`docs/SETUP_AND_DEMO.md`](docs/SETUP_AND_DEMO.md) for exact verified versions.

```bash
# Backend
cd src/AIRecruiter.API
dotnet restore
dotnet ef database update --project ../AIRecruiter.Infrastructure --startup-project .
dotnet run --launch-profile http

# Frontend (in a second terminal)
cd frontend
npm install
npm run dev
```

- API: `http://localhost:5087` (Swagger at `/swagger` in Development)
- Frontend: `http://localhost:5173` (or the next free port)

**Demo login** (seeded automatically in Development): `recruiter1@demo.airecruiter.dev` or `candidate1@demo.airecruiter.dev`, password `Demo@123`. Full seeded-account table and CORS/HTTPS troubleshooting: [`docs/SETUP_AND_DEMO.md`](docs/SETUP_AND_DEMO.md).

## Password Reset & Email Setup

Forgot/reset password (`/forgot-password` → `/verify-reset-code` → `/reset-password`) sends a 6-digit verification code by email. **No SMTP account is required for local development** — if `Smtp:*` isn't configured, a Development-only sender logs the code to the console instead of sending real mail:

```bash
# In dotnet run's console output after requesting a reset code:
info: AIRecruiter.Infrastructure.Email.DevEmailSender[0]
      [DEV EMAIL — not actually sent, SMTP is not configured]
      To: someone@example.com
      ...
      Your verification code is: 123456
```

To send real email, configure any SMTP account (Gmail with an app password, Outlook, a self-hosted relay — no paid API) via `dotnet user-secrets` (never commit real values):

```bash
cd src/AIRecruiter.API
dotnet user-secrets set "Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Smtp:Port" "587"
dotnet user-secrets set "Smtp:Username" "<your-smtp-username>"
dotnet user-secrets set "Smtp:Password" "<your-smtp-app-password>"
dotnet user-secrets set "Smtp:SenderEmail" "<your-sender-address>"
dotnet user-secrets set "Smtp:SenderName" "AI Recruiter"
dotnet user-secrets set "Smtp:EnableSsl" "true"
```

Security behavior in one paragraph: codes are 6-digit, cryptographically random, stored only as a BCrypt hash, expire in 10 minutes, and allow at most 5 wrong attempts before lockout; the forgot-password endpoint always returns the same generic message regardless of whether the email exists; a verified code exchanges for a separate, short-lived reset token (never the login JWT) that alone can change the password; and a successful reset rotates the account's security stamp, which **immediately invalidates every previously-issued JWT** for that account (checked on every authenticated request). Full detail: [`docs/SECURITY_AND_AUTH.md`](docs/SECURITY_AND_AUTH.md#password-reset-flow).

## Interview Scheduling

Recruiters propose a single interview time for a shortlisted applicant; the candidate accepts or declines. No paid calendar API is used — calendar invites are generated locally as `.ics` files (RFC 5545).

- **Recruiter** — from an applicant's row on `/jobs/:id/applicants` (or `/applications/:id`), click **Schedule interview** and set date/time, duration, type (Online / Phone / In Person), a meeting link or venue, and an optional note. Only the recruiter who owns the job posting can schedule, reschedule, cancel, or mark the interview completed. `/recruiter/interviews` lists every interview across the recruiter's company with status filters (Proposed / Scheduled / Completed / Cancelled / Declined).
- **Candidate** — `/interviews` lists the candidate's own upcoming and past interviews, each showing the job title, company, date/time in **India Standard Time (IST)**, interview type, meeting link/venue, and the recruiter's note. The candidate can **Accept** (moves the interview to `Scheduled`) or **Decline** (with an optional response note).
- **Rescheduling** — a recruiter changing the time/type/location moves the interview back to `Proposed`, so the candidate must reconfirm.
- **Calendar download** — once `Scheduled`, either side can download an `.ics` file (`GET /api/interviews/{id}/calendar.ics`) containing the correct UTC start/end time, the job + company as the event title, the venue/link, and a short description; opens directly in Google Calendar, Outlook, or Apple Calendar.
- **Validation** — the API rejects interviews scheduled in the past, end times before start times, and duplicate/overlapping interviews for the same candidate or recruiter, all with friendly, field-level error messages (never a generic 500).
- **Authorization** — every check is server-side and derived from the JWT (never a client-supplied user ID): a recruiter can only manage interviews for their own company's job postings, and a candidate can only view/respond to their own interviews.

**Manual test steps**:
1. Log in as a recruiter (`recruiter1@demo.airecruiter.dev` / `Demo@123`), open a job's applicants, and click **Schedule interview** on a shortlisted candidate — fill in a future date/time and submit.
2. Log in as that candidate (`candidate1@demo.airecruiter.dev` / `Demo@123`), open `/interviews`, confirm the invite appears with the correct IST time, and click **Accept**.
3. Back as the recruiter, open `/recruiter/interviews`, filter by **Scheduled**, and download the `.ics` file — open it and confirm the time/title/link are correct.
4. As the recruiter, **Reschedule** the interview to a new time and confirm its status reverts to **Proposed** until the candidate reconfirms.
5. Try scheduling a time in the past, or an end time before the start time, and confirm the API returns a friendly validation error instead of a 500.
6. As the recruiter, **Cancel** or mark an accepted interview **Completed**, and confirm the status updates on both dashboards' "Upcoming interviews" widgets.

## Candidate Profile: Photo Upload & Validation

Candidates can upload a profile photo and get thorough validation across the whole profile form, both in the browser and enforced again on the server.

- **Photo upload** (`/profile` → Profile photo card) — accepts JPG, JPEG, PNG, and WEBP up to 5 MB; the extension, declared MIME type, *and* the file's actual byte signature are all checked server-side (so a renamed `.exe` claiming to be a `.jpg` is rejected) before anything is resized. The server then constrains it to a 512×512 box (via [ImageSharp](https://github.com/SixLabors/ImageSharp), preserving aspect ratio) and re-encodes it as JPEG, so stored/served avatars are always a bounded, predictable size regardless of what was uploaded. Storage reuses the same local-disk/Cloudinary-swappable abstraction already used for resumes (`IResumeStorage`) — no new provider was introduced.
- **Serving** — avatars are served from `GET /api/candidates/{id}/avatar`, deliberately public/anonymous (profile photos aren't sensitive the way a resume is, and recruiters/other pages need to display them without juggling auth headers on an `<img>` tag). Removing a photo reverts to a generated initials avatar everywhere it's shown — profile page, navbar, and candidate dashboard, all updating live without a page reload.
- **Field validation** — headline, bio, education, graduation year, experience years, skills (at least one, deduplicated, length-capped), phone (normalized to a bare 10-digit Indian mobile number), and LinkedIn/GitHub/portfolio URLs (must be `https://`, and LinkedIn/GitHub URLs must actually point at `linkedin.com`/`github.com`, not just contain those characters somewhere) are all validated. Every rule is enforced server-side regardless of what the client sends — the API returns a `fieldErrors` map so the exact same inline, per-field error UI (a standing convention across this app's forms) works for server-side rejections too, not just client-side ones.

**Manual test steps**:
1. Log in as a candidate (`candidate1@demo.airecruiter.dev` / `Demo@123`), open `/profile`, and upload a JPG/PNG/WEBP under 5 MB — confirm the preview updates and the navbar avatar changes immediately.
2. Try uploading a renamed non-image file (e.g. a `.txt` renamed to `.jpg`) — confirm a clear inline error, not a crash.
3. Click **Remove** — confirm it reverts to the initials avatar everywhere.
4. Clear the Headline field and click elsewhere (blur) — confirm "Headline is required." appears inline immediately, without submitting the form.
5. Enter a duplicate skill (e.g. `C#, c#`), an invalid phone number, or a GitHub URL that isn't actually a `github.com` link, and submit — confirm each produces its own inline field error and the rest of your entered values are preserved (nothing is cleared on a failed save).
6. Fix the errors and save — confirm the success toast and that the values persist after a page reload.

## Recruiter Collaboration: Templates, Messaging, Scorecards, Hiring Team & Reports

Five features that turn a single-recruiter tool into a small hiring team's workspace — all built on a new, purely **additive** `CompanyRole` permission model (`Owner / Recruiter / HiringManager / Interviewer`) that changes nothing about how the app already worked: every existing recruiter account was backfilled as an `Owner` of their own company, so today's behavior is unchanged unless you deliberately add teammates.

- **Job Templates** (`/recruiter/templates`) — save a reusable starting point (title, department, description, responsibilities, required/preferred skills, experience range, employment type, salary visibility, default India location) from scratch or from an existing job. Templates are visible to your whole company; only the creator or a company Owner can edit/delete one. "Use Template" on the job-templates list (or the picker at the top of **Post a Job**) creates a new **Draft** job pre-filled from it, ready to review before publishing.
- **Messaging** (`/recruiter/messages`, `/messages` for candidates, and a panel on every application detail page) — a conversation is tied to one specific job application; a recruiter can only message candidates who applied to their company's jobs, and vice versa. Messages are non-empty, capped at 2000 characters, rate-limited (20/5 min per sender), and always rendered as plain text on the frontend — never through `dangerouslySetInnerHTML` — so there's no HTML/script injection surface to sanitize against. New messages trigger an in-app notification and an unread badge in the navbar.
- **Interview Feedback Scorecards** — from a **Completed** interview on `/recruiter/interviews`, the owning recruiter or an assigned Interviewer can score Technical/Communication/Problem-solving/Culture-fit (1–5), pick an overall recommendation (Strong Yes → Strong No), and add strengths/concerns/private notes. Save as a draft or submit; editing an already-submitted scorecard writes a snapshot of the previous values to an edit-history table first. An aggregated summary (averages + every individual scorecard) shows on the application detail page to the owning recruiter, a company Owner, or an assigned Hiring Manager/Interviewer — **never to the candidate**.
- **Hiring Team** (`/recruiter/team`, Owner-only) — add a colleague by email (they must already be a registered Recruiter) and assign them a role: **Owner** manages the whole company including jobs/applicants they didn't create; **Recruiter** keeps today's exact behavior (manages what they created); **Hiring Manager**/**Interviewer** are scoped to jobs/interviews explicitly assigned to them via the same page. No email or invite-link infrastructure was added — this is a deliberate zero-cost choice (see below). A non-Owner who navigates to the page directly sees a clear "you don't have permission" state instead of an error.
- **Reports** (`/recruiter/reports`) — company-wide, date-range-filterable metrics (jobs created/published/closed, applications by job/city/state, the full status funnel, interview conversion rate, average days-to-interview, top candidate skills, offers/hires), rendered as the same dependency-free bar-chart + data-table pairing used on the existing Analytics page. Four CSV exports (jobs, applicants, interview schedule, funnel summary) are scoped strictly to the caller's own company and never include resume file contents, passwords, tokens, or another company's data.

**Why no invite-link/email flow for the hiring team**: adding a teammate is a "manually created user association" (the colleague must already have a Recruiter account) rather than a token/email invite — this needs zero new infrastructure and avoids depending on email delivery working, consistent with this project's zero-paid-API constraint.

**Migration**: `AddHiringTeamTemplatesMessagingScorecardsReporting` — adds `RecruiterProfiles.CompanyRole` (every existing recruiter is backfilled as `Owner`) plus six new tables: `JobTemplates`, `Messages`, `InterviewFeedbacks`, `InterviewFeedbackEditHistories`, `JobAssignments`, `InterviewAssignments`. Reports needed no new tables — it's pure read-aggregation over existing data.

**Manual test steps**:
1. Log in as a company Owner (`recruiter1@demo.airecruiter.dev` / `Demo@123`), open `/recruiter/templates`, and confirm the seeded "Backend Engineer" template — click **Use Template**, confirm the new job's Draft form is pre-filled, then edit and publish it.
2. On `/recruiter/messages`, open the seeded Arjun Mehta conversation, send a reply, and confirm it appears immediately; log in as `candidate1@demo.airecruiter.dev` and confirm the same message shows on `/messages` and the application detail page.
3. Log in as `interviewer1@demo.airecruiter.dev` / `Demo@123` (an Interviewer at Solstice Retail Group, assigned to one specific interview), open `/recruiter/interviews`, click **Feedback** on the completed UI/UX Designer interview, and confirm the seeded scorecard loads — edit and resubmit it, then confirm an edit-history row was written.
4. As `recruiter3@demo.airecruiter.dev` (Solstice's Owner), open `/recruiter/team` and confirm both team members are listed with their roles; log in as the Interviewer and confirm `/recruiter/team` shows the permission-denied state instead of the team list.
5. As any Owner, open `/recruiter/reports`, apply a date range, and confirm the metrics update; download each of the four CSV exports and confirm they only contain that recruiter's own company's data.

## Platform Quality: Moderation, Preferences, Invitations, Job Expiry & PWA

Five more features that harden the platform for real usage — all built with the same zero-paid-API constraint as everything above.

- **Admin Moderation & Reporting** (`/admin`, Admin-only) — any authenticated user can report a job, a company, a message, or another user for a specific reason (Spam / Fraudulent Job / Inappropriate Content / Fake Company / Harassment / Other) with optional free-text details, via a single generic `POST /api/reports` endpoint (`ModerationReportsController`, deliberately distinct from the pre-existing recruiter-analytics `ReportsController` at `/recruiter/reports`). Reports live in one polymorphic `Report` table (`EntityType` + `EntityId`, the same FK-less convention the audit log already used) so one review queue covers all four entity types. An Admin can move a report through Open → Under Review → Resolved/Dismissed, attach an internal moderation note at any time, hide/unhide/remove a job (hidden/removed jobs disappear from search, direct-URL viewing by non-owners, and can no longer receive applications — both gaps were fixed as part of this work), and **suspend** or reactivate a user. Suspending a user sets `IsActive = false` **and** rotates their `SecurityStamp`, which the JWT-validation pipeline already checks on every request — so a suspended user is logged out of every active session immediately, not just blocked from a future login. Every moderation action is audit-logged.
- **Candidate Availability & Preferences** (`/profile` → "Availability & preferences" section) — candidates set an availability status (Actively Looking / Open to Opportunities / Not Looking), preferred job types/locations/roles, remote preference, expected salary range, and notice period. A new **profile visibility** control (Visible to Recruiters / Visible only after applying / Private) governs exactly who can see the profile: `VisibleAfterApplying` is the default and preserves today's exact pre-existing behavior (a recruiter sees a candidate only once they've applied to that recruiter's company); `VisibleToRecruiters` additionally exposes the candidate to a new company-agnostic discovery search any recruiter can run; `Private` hides the candidate from discovery entirely (existing applications are unaffected — recruiters you've actually applied to can still see your application). Preferences also feed a small scoring bonus in the candidate dashboard's job recommendations (skill overlap remains the primary signal).
- **Recruiter Candidate Invitations** (Candidate Search → "Discover candidates" tab, and the candidate dashboard) — a recruiter can invite any candidate visible to them (via discovery or an existing application at their company — never a `Private` candidate) to apply to one specific open job of theirs, with an optional message (2000-char cap, plain text only). Duplicate active invitations for the same candidate+job are blocked; invitations expire automatically after 14 days. Candidates see invitations on their dashboard and can Accept (jumps to the job), Decline, or Dismiss; recruiters see Sent/Viewed/Accepted/Declined/Expired status on their dashboard.
- **Job Expiry & Reminders** (Manage Jobs → "Set/Extend deadline") — a published job can have an application deadline. Applying to an expired job is rejected server-side (`JOB_EXPIRED`), same as applying to a hidden/removed job (`JOB_CLOSED`). A new background service (`JobLifecycleSweepService` — the first scheduled task in this codebase, a plain `PeriodicTimer` inside a `BackgroundService`, no external scheduler dependency) runs every 15 minutes, auto-closes any `Open` job past its deadline, and notifies the owning recruiter in-app (no email/SMS). Manage Jobs shows an "Expiring soon" / "Expired" / "No applications received" badge per job, computed client-side from data already on the page; recruiters can extend the deadline at any time via `PATCH /api/jobs/{id}/deadline` (owning-recruiter-or-company-Owner only).
- **Progressive Web App** — the frontend is installable and has a safe offline fallback. `frontend/public/manifest.json` + a hand-written `frontend/public/sw.js` (no `vite-plugin-pwa` dependency) cache **only** the static app shell (built JS/CSS, the manifest, icons) cache-first; every request under `/api/` is explicitly excluded from the service worker's fetch handler and always goes straight to the network, so JWTs, resumes, messages, and any other private API response can never end up in Cache Storage. A navigation request that fails while offline falls back to `frontend/public/offline.html`. The navbar shows an **Install** button when the browser fires `beforeinstallprompt`.

**Migration**: `AddModerationSuspensionPreferencesInvitationsJobExpiry` — generalizes the old job-only `JobReports` table into a polymorphic `Reports` table, adds 8 preference/visibility columns to `CandidateProfiles`, adds `JobPostings.ApplicationDeadlineUtc`, and adds a new `Invitations` table.

**Testing the PWA locally**:
1. `npm run build && npm run preview` in `frontend/` (service workers are skipped in `npm run dev`'s non-HTTPS localhost in some browsers, but Chrome allows `http://localhost`; `preview` is the more reliable check).
2. Open Chrome DevTools → **Application** tab → **Manifest**: confirm name, icons, and theme color load without errors. → **Service Workers**: confirm `sw.js` shows as "activated and running".
3. Check the **Install** icon in Chrome's address bar (or the navbar's Install button after `beforeinstallprompt` fires) and install the app.
4. DevTools → **Application** → **Cache Storage**: confirm only static asset URLs appear — never any `/api/...` URL.
5. DevTools → **Network** tab → toggle **Offline**, then reload: confirm the offline fallback page appears instead of a browser error page.

**Manual test steps**:
1. Log in as Admin (`admin@demo.airecruiter.dev` / `Demo@123`), open `/admin` → Reports, and confirm reports across all four entity types (Job/Company/Message/User) are seeded — move one through Under Review → Resolved, and add a note to another.
2. On the Users tab, **Suspend** a non-admin user (not `candidate8@demo.airecruiter.dev`, which is already seeded suspended) — confirm they can no longer log in, and if they were already logged in elsewhere, their next request is rejected. **Reactivate** them and confirm login works again.
3. Log in as `candidate1@demo.airecruiter.dev` (seeded `VisibleToRecruiters`), open `/profile`, and confirm the "Availability & preferences" section shows their seeded preferences; change **Profile visibility** to `Private` and save.
4. Log in as any recruiter, open Candidate Search → **Discover candidates**, and confirm the now-`Private` candidate no longer appears while `candidate4@demo.airecruiter.dev` (seeded `VisibleToRecruiters`) still does — click **Invite to Apply**, pick a job, and send an invitation.
5. Log in as `candidate4@demo.airecruiter.dev` and confirm the invitation appears on the dashboard under "Invitations to apply" — **Accept** it and confirm it navigates to the job.
6. Log in as `recruiter1@demo.airecruiter.dev`, open `/jobs/mine`, and confirm the seeded "Senior Backend Engineer" job (deadline in 2 days) shows an **Expiring soon** badge — click **Extend deadline** and push it out a month.
7. Set a job's deadline to a date in the past via the API or database, wait up to 15 minutes (or restart the API to trigger the sweep's on-startup run), and confirm the job auto-closes and the recruiter gets an in-app notification.

## Documentation

| Document | Contents |
|---|---|
| [`docs/PROJECT_OVERVIEW.md`](docs/PROJECT_OVERVIEW.md) | Elevator pitch, problem statement, user journeys, full feature-status table, tech stack, résumé bullets |
| [`docs/SYSTEM_DESIGN.md`](docs/SYSTEM_DESIGN.md) | Architecture diagrams, request/auth/application/job/resume flows, CORS, production scalability |
| [`docs/API_REFERENCE.md`](docs/API_REFERENCE.md) | Every endpoint — method, auth, role, ownership rule, request/response examples, curl examples |
| [`docs/DATABASE_DESIGN.md`](docs/DATABASE_DESIGN.md) | ER diagram, table descriptions, keys/constraints, data lifecycle, seed-data strategy |
| [`docs/SECURITY_AND_AUTH.md`](docs/SECURITY_AND_AUTH.md) | Auth implementation, ownership checks, CORS, file-upload/view-tracking privacy, known limitations |
| [`docs/SETUP_AND_DEMO.md`](docs/SETUP_AND_DEMO.md) | Full local setup, environment variables, seed credentials, build/test commands, manual test checklist |
| [`docs/INTERVIEW_GUIDE.md`](docs/INTERVIEW_GUIDE.md) | Elevator pitches, demo script, common interview Q&A, STAR stories, résumé/LinkedIn copy |
| [`docs/FUTURE_ROADMAP.md`](docs/FUTURE_ROADMAP.md) | What's implemented vs. planned, next features, production/deployment improvements |

## License / Status

Portfolio project — not affiliated with any real recruitment service. See [`docs/PROJECT_OVERVIEW.md § Current Limitations`](docs/PROJECT_OVERVIEW.md#current-limitations) for an honest list of what's out of scope today.
