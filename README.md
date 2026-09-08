# AI Recruiter

[![CI](https://github.com/OWNER/REPO/actions/workflows/ci.yml/badge.svg)](https://github.com/OWNER/REPO/actions/workflows/ci.yml)

> Replace `OWNER/REPO` above with this repository's actual GitHub path once pushed — the badge and link only resolve after that.

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
| Core features | India-only structured job locations · job draft/publish/close/archive lifecycle · applications with full status history, filters/sort, and a visual timeline · explainable local resume matching · Kanban applicant board · interview scheduling with `.ics` export · saved jobs & job alerts · company-scoped candidate search + CSV export · privacy-conscious job-view tracking · analytics · audit trail · secure forgot/reset password with email verification codes · candidate profile photo upload with in-browser crop/zoom/rotate and server-side resize/validation · structured resume builder (experience/education/certifications/projects) with a profile-strength score and client-side PDF export · reusable cover-letter templates merged into a job-specific letter at apply time · locally seeded, timed skill assessments with opt-in recruiter-visible badges · an optional public shareable candidate portfolio page · a career-goals tracker with deterministic progress suggestions · digital offer management with accept/decline and a client-side offer-summary PDF · private, company-scoped recruiter talent pools with notes/tags · a no-email employee-referral workflow with shareable tokenized links · candidate dashboard with next-best-actions · footer with deep-linked popular job-role searches · reusable job templates · recruiter–candidate in-app messaging · interview feedback scorecards · hiring-team roles & permissions · recruiter reports with CSV export · admin moderation & user suspension · candidate availability/preferences & profile-visibility controls · recruiter candidate-invitation workflow · job application deadlines & auto-expiry · installable Progressive Web App · public landing page with live platform stats · job discovery with compare/sort/recent-searches/mobile filter drawer · in-app Help & Support with a feedback inbox · self-service account settings (password change, notification preferences, account deletion) · light/dark theme · company verification with a "Platform Verified" badge · a recruiter-only deterministic job quality score · follow-companies with new-job notifications · job sharing with aggregate share counts · a personalized activity timeline · moderated, anonymous company reviews & ratings · locally computed salary insights · advanced saved searches with default-search and match notifications · a privacy center with JSON/CSV account-data export and grace-period account deletion · customizable per-job application screening questions with recruiter-only preferred answers and applicant filtering · rotating single-use refresh tokens with theft-detection chain revocation · server-side pagination for job search and candidate search · framework-level rate limiting on auth/export/general API traffic · real per-candidate "recently viewed jobs" history · a Playwright end-to-end test suite with a GitHub Actions CI pipeline |
| Cost | Zero — every optional third-party integration (Cloudinary, Adzuna, SMTP) is behind an interface and only activates when credentials are configured; falls back gracefully otherwise. Image cropping (`react-easy-crop`), PDF export (`jsPDF`, used for both resumes and offer summaries), skill assessments, career-goal suggestions, and offer/referral tokens all run entirely client-side or on deterministic local/cryptographic logic — no paid image, assessment, document, e-signature, or AI API is used anywhere |
| Tests | 548 passing (xUnit + EF Core InMemory + Moq, including WebApplicationFactory-based rate-limiter integration tests) + a 3-journey Playwright E2E suite |

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

## Candidate Enhancements: Avatar Crop, Resume Builder, Dashboard Actions & Application Tracker

Four candidate-facing additions, all built with the same zero-paid-API constraint — no image-editing API, no resume-builder SaaS, no AI API, no subscription of any kind.

- **Avatar crop & adjustment** (`/profile` → Profile photo card) — selecting a file now opens a crop modal (built on [`react-easy-crop`](https://github.com/ValentinH/react-easy-crop), MIT-licensed) instead of uploading immediately: drag to reposition, a zoom slider, 90° rotate steps, a **Reset** action, and a live circular preview. **Save Photo** crops the image client-side on an HTML `<canvas>` (a hand-written ~50-line utility, no extra library) and uploads only the final square JPEG; **Cancel** discards the selection and revokes the temporary object URL. The server keeps its existing defense-in-depth: `ImageFileValidator`'s signature/type/size checks run unchanged, and `ImageSharpAvatarProcessor` now also center-crops to a square before resizing, so a stored avatar is always square even if a client bypasses the crop UI entirely. The avatar (or an initials fallback) now renders consistently in the navbar, profile page, candidate dashboard, and every recruiter-facing view that shows a candidate — Applicants list, Kanban board, and Candidate Search (result rows + detail modal).
- **Resume Builder & profile strength** (`/resume-builder`, linked from `/profile` and the dashboard) — structured, repeatable sections for work experience, education, certifications, and projects (add/edit/reorder/remove, each with its own date/URL/length validation enforced both client- and server-side), plus a summary/links panel reusing the profile's existing headline/summary/LinkedIn/GitHub/portfolio/skills fields and a new free-text achievements field. A profile-strength meter (0–100%, via a shared `ProfileStrengthCalculator`) lists exactly which of 9 weighted checks are still missing with a plain-language tip for each — no AI involved, just static rules. A **Preview** toggle renders a clean read-only resume layout, and **Download PDF** generates a polished PDF entirely in the browser via [`jsPDF`](https://github.com/parallax/jsPDF) (MIT-licensed) — no backend PDF library, no external rendering service. The generated resume's structured sections are private by default and only become visible to a recruiter through the same existing profile-visibility gate that already governs the rest of the profile (`VisibleAfterApplying` by default — a recruiter sees them once the candidate has applied to that recruiter's company — or `VisibleToRecruiters` for pre-application discovery); this is kept fully separate from the original uploaded resume *file*, which candidates can still upload/download independently.
- **Dashboard "Next best actions"** — the candidate dashboard now opens with up to 4 prioritized suggestions (Complete your profile, Upload/update your resume, Apply to recommended jobs, Respond to an interview invite, Review a recruiter invitation), each linking straight to the relevant page. They're derived from data the dashboard already computes (profile strength, resume-on-file, recommended jobs, pending interviews/invitations) — no new queries beyond one cheap invitation count.
- **Application tracker enhancements** (`/applications`, `/applications/:id`) — withdrawal is now blocked **server-side** once an application reaches a final status (`Hired`/`Rejected`/`Withdrawn`), not just hidden client-side, and shows a clear message if a race occurs; the withdraw confirmation uses the shared `ConfirmDialog` component. The list page gained status/company/location filters, an application-date range, and a sort control (latest activity / application date / interview date), plus a friendlier empty state when filters match nothing. The detail page replaces the old plain status list with a visual vertical `ApplicationTimeline`, and now surfaces a plain-language **Next action** (e.g. "Interview scheduled — check your email") alongside the existing interview cards, messages, and recruiter notes.

**Migration**: `AddCandidateResumeBuilder` — four new purely-additive tables (`CandidateWorkExperiences`, `CandidateEducations`, `CandidateCertifications`, `CandidateProjects`, each cascade-deleted with the profile) and one nullable `CandidateProfiles.AchievementsText` column. No existing table or column is modified.

**Manual test steps**:
1. Log in as a candidate (`candidate1@demo.airecruiter.dev` / `Demo@123`), open `/profile`, select a photo, and confirm the crop modal opens — drag, zoom, rotate, then **Reset** and confirm it returns to the original framing; **Save Photo** and confirm the cropped circular avatar appears immediately in the navbar and profile page.
2. As a recruiter (`recruiter1@demo.airecruiter.dev` / `Demo@123`), open that candidate's application on the Applicants list, the Kanban board, and Candidate Search, and confirm the same avatar (not an initials fallback) appears in all three.
3. As the candidate, open `/resume-builder`, add a work-experience entry and an education entry, reorder one of them, and confirm the strength meter and "how to improve" list update; toggle **Preview**, then **Download PDF** and open the file to confirm it's a clean, correctly-populated resume.
4. On the candidate dashboard, confirm **Next best actions** lists relevant, clickable suggestions; complete one (e.g. upload a resume) and reload to confirm it drops off the list.
5. On `/applications`, apply a status filter, a date range, and each sort option; then attempt to withdraw a `Hired` or `Rejected` seeded application and confirm it's blocked (button hidden client-side, and rejected server-side if attempted directly) — then withdraw an early-status application via the `ConfirmDialog` and confirm the visual timeline and next-action text update on its detail page.

## Candidate Enhancements Batch 2: Cover Letters, Skill Assessments, Public Portfolio, Career Goals

Four more candidate-facing features, all built with the same zero-paid-API constraint as every feature above — no paid AI, assessment platform, document service, or subscription anywhere.

- **Cover-Letter Builder** (`/cover-letter-templates`, and a step inside **Apply to this job**) — candidates create reusable templates (title, introduction, skills/experience highlights, project achievements, closing message; title is unique per candidate, case-insensitive, with a friendly conflict error). When applying to a job, a candidate optionally picks a template — a purely client-side utility (`frontend/src/utils/coverLetterMerge.ts`) merges it with safe, already-known profile fields (name, headline, skills) and the job/company title into an editable draft, which stays editable before submitting. The final letter reuses the existing `JobApplication.CoverNote` field end-to-end (now length-capped at 4,000 characters server-side) — recruiter read access was already correctly gated to applicants at their own company via the existing `EnsureCanViewApplicationAsync` check, so no new access-control code was needed.
- **Skill Assessments** (`/assessments`) — locally seeded, fictional multiple-choice question banks (~15 questions each) across 8 categories: Java, C#/.NET, React, JavaScript, SQL, Python, Communication, Aptitude. Starting an attempt draws 15 random questions (a `RandomNumberGenerator`-backed Fisher-Yates shuffle, matching this codebase's existing cryptographic-RNG convention) with a 20-minute time limit and a live progress indicator; answers save as you go so a refresh doesn't lose progress. Only one attempt can be active at a time (any category), and retaking the same category has a 24-hour cooldown after a completed attempt — both enforced server-side, not just in the UI. After submitting, candidates see their score and a full per-question review (correct answer + explanation), and can opt a completed attempt's badge in or out of recruiter visibility at any time — badges default to **private** and only ever appear to recruiters (on the Candidate Search detail view) or on the public portfolio profile when explicitly opted in. This is explicitly not an official certification and has no external proctoring.
- **Public Portfolio Profile** (`/profile` → Profile visibility, and `/talent/:slug`) — a fourth `ProfileVisibility` option, **Public shareable**, generates a stable, random slug (`{name}-{random}`, via `RandomNumberGenerator`, never regenerated once set) and exposes an anonymous, unauthenticated `/talent/:slug` page. That page shows only a curated safe subset — name, headline, avatar, skills, summary/experience/education text, projects, portfolio/LinkedIn/GitHub links, and opted-in assessment badges — and **never** email, phone, resume download, applications, messages, or account settings. Disabling public sharing takes effect immediately (the endpoint 404s the same way for a disabled link as for one that never existed, so a stale link can't be distinguished from a wrong one). A preview panel on the profile page (fed by the same safe-field DTO) lets a candidate see exactly what's public before/without sharing it, plus a one-click copy-link action.
- **Career Goals & Progress Tracker** (`/career-goals`, and a card on the dashboard) — candidates set goals with any combination of a target role/skill/company type, a preferred India location (validated the same way as the rest of the app), a target date, a progress percentage, and notes; goals move through In Progress / Completed / Paused, with a small celebration toast on completion. Each goal gets a short list of deterministic, rule-based suggestions (complete your resume, add a missing target skill, take a relevant assessment, apply to jobs, respond to a pending interview invite) computed by a new `CareerGoalSuggestionEngine` — pure, no external AI, mirroring the same static-checklist pattern the dashboard's profile-strength calculator already uses.

**Migration**: `AddCoverLettersAssessmentsPublicProfileCareerGoals` — five new tables (`CoverLetterTemplates`, `SkillAssessmentQuestions`, `SkillAssessmentAttempts`, `SkillAssessmentAnswers`, `CareerGoals`) plus one new nullable `CandidateProfiles.PublicProfileSlug` column with a filtered unique index. Purely additive — no existing table or column is modified.

**Manual test steps**:
1. Log in as a candidate (`candidate1@demo.airecruiter.dev` / `Demo@123`), open `/cover-letter-templates`, confirm the two seeded templates, then open any job and click **Apply to this job** — pick a template, confirm the letter auto-fills and is editable, and submit; as `recruiter1@demo.airecruiter.dev`, open that application and confirm the cover letter is visible.
2. As the candidate, open `/assessments`, start a Java attempt, confirm the 20-minute countdown and progress indicator, answer a few questions, and submit — confirm the score and per-question review; try starting a second attempt immediately and confirm it's blocked (one active attempt at a time), then submit/finish and try retaking the same category to confirm the 24-hour cooldown message.
3. Toggle a completed attempt's recruiter-visibility checkbox on, then as a recruiter, open that candidate in Candidate Search and confirm the badge appears — toggle it off and confirm the badge disappears.
4. As the candidate, open `/profile`, set **Profile visibility** to **Public shareable**, copy the generated link, and open it in an incognito/private window — confirm only the safe fields show (no email/phone/resume/applications) and that the seeded `candidate1` demo profile at `/talent/arjun-mehta-demo1` behaves the same way; switch visibility back to Private and confirm the link now 404s.
5. Open `/career-goals`, confirm the seeded goals across all three statuses, create a new goal with just a target skill, confirm at least one suggestion appears, then mark it Completed and confirm the completion toast — check the candidate dashboard shows the same goals and recent assessment results.

## Digital Offers, Talent Pools & Employee Referrals

Three more recruitment-workflow features, all built with the same zero-paid-API constraint as every feature above — no paid offer/e-signature service, no external assessment or CRM platform, no automated email sending anywhere in this batch.

- **Digital Offer Management** — from a candidate's application detail page, a recruiter at the owning company (any teammate, not just the original poster — the same company-wide access rule used everywhere else) can create an offer covering salary (with a Monthly/Annual toggle), joining date, work location (an India city/state pair or Remote — India), employment type, probation details, benefits, an expiry date, and a personal message. Offers move through **Draft → Sent → Viewed → Accepted/Declined/Withdrawn/Expired**, with every transition recorded in a full status-history timeline (mirroring the existing application-status-history pattern) and a notification sent to the other side at each step. A candidate can only see and respond to their own offers, and can never accept an offer that's expired, withdrawn, or already decided — enforced server-side, not just hidden in the UI. Accepting automatically moves the linked application to **Hired**; declining moves it to **Rejected** — no manual follow-up status change needed. Either side can download a clean **offer-summary PDF**, generated entirely client-side (the same `jsPDF` approach already used for resumes) and clearly labeled as a portfolio/demo document — not a legally binding offer or e-signature.
- **Recruiter Talent Pools** (`/recruiter/talent-pools`) — recruiters create named, company-wide pools (e.g. "Frontend Talent", "Immediate Joiners") and save candidates into one or more of them directly from Candidate Search or an applicant list, with private per-candidate notes and tags visible only to your own team. A pool card shows the candidate's photo, headline, skills, experience, India location, and — when they've applied to your company — their latest application status and match score. Saving a candidate re-enforces the exact same privacy rule the existing invitation feature already uses: a `Private` candidate who has never applied to your company can't be added to a pool, so pools can never become a backdoor around a candidate's visibility settings. Inviting a pooled candidate to a job reuses the existing invitation endpoint outright — no separate invite logic. **Candidates never see which pools they're in** — there is no candidate-facing endpoint or UI for this feature at all. Pools, and everything in them, are strictly isolated per company.
- **Employee Referral Workflow** (`/referrals` for your own referrals, "Refer a friend" on any open job, `/recruiter/referrals` for a company's incoming referrals) — any authenticated user (candidate or recruiter) can refer someone for an open, approved job by entering their name, email, optional phone, relevant skills, and a note. This generates a unique, cryptographically random shareable link (`/register?ref=<token>` — the same CSPRNG-plus-SHA256-hash-at-rest convention already used for password-reset tokens; only the hash is ever stored) with **no email sent automatically** — just a copy-link button. When the referred person registers through that link, their account is linked to the referral (**Invited → Registered**); if they then apply to the referred job, the referral advances automatically (**→ Applied**), and from there its displayed status (**Interviewing / Hired / Not Selected**) is derived live from that same application's real status — never tracked as a second, driftable copy of the truth. A referrer sees only their own referrals, and only privacy-safe fields (name, email, status) — never the referred person's resume or full profile. Recruiters see referrals only for their own company's jobs. Duplicate referrals (same email + job) are blocked, and referral creation is rate-limited the same way invitations already are.

**Migration**: `AddOffersTalentPoolsReferrals` — five new tables (`Offers`, `OfferStatusHistories`, `TalentPools`, `TalentPoolCandidates`, `Referrals`), purely additive — no existing table or column is modified.

**Manual test steps**:
1. Log in as `recruiter1@demo.airecruiter.dev` / `Demo@123`, open the seeded Arjun Mehta application for Senior Backend Engineer, and confirm a **Draft** offer already exists — edit it, then **Send** it; log in as `candidate1@demo.airecruiter.dev` and confirm the offer appears on that application with **Accept**/**Decline** available, then **Accept** it (with a note) and confirm the application status becomes **Hired** and the offer's status timeline shows both transitions.
2. As `recruiter4@demo.airecruiter.dev`, open the seeded Fatima Sheikh application for Backend Engineer (Payments) — confirm a **Sent** offer is already there, and as `candidate4@demo.airecruiter.dev`, confirm it can be downloaded as a PDF.
3. As `recruiter1@demo.airecruiter.dev`, open `/recruiter/talent-pools` and confirm the seeded "Backend Shortlist" pool with two candidates and their notes/tags; open Candidate Search, pick a `VisibleToRecruiters` candidate, and confirm "Save to pool" works — then try saving a `Private` candidate who hasn't applied to your company and confirm it's blocked.
4. As any logged-in user, open any open job's detail page, click **Refer a friend**, fill in a friend's details, and copy the generated link; open it in an incognito window, confirm the "You were referred to..." banner appears, register a new account, then log in as that account and apply to the referred job.
5. Back as the original referrer, open `/referrals` and confirm the referral's status progressed from Invited → Registered → Applied; as the owning recruiter, open `/recruiter/referrals` and confirm the same referral appears there.

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

## Feature & UI Batch: Landing Page, Job Discovery, Help & Support, Account Settings, Theme

Five more additions focused on first-impression polish and self-service account management — same zero-paid-API constraint as everything above.

- **Landing Page** (`/`, unauthenticated visitors only — logged-in users are redirected straight to their dashboard) — a hero with an inline job search (title/skill + location, with a small keyboard-navigable suggestions dropdown), **Find Jobs**/**Post a Job** primary actions, popular-role quick links (shared with the footer's list so both stay in sync), real platform stats (open job count, candidate count, company count — genuine `COUNT()` queries via a new public `GET /api/platform/stats`, never fabricated numbers), up to 6 featured open jobs, Candidate/Recruiter feature highlights, a 3-step "How it works," and a CTA band before the footer.
- **Job Discovery** (`/jobs`) — quick search suggestions (a small static role/skill list feeding the same suggestions dropdown as the landing page); recent searches for candidates (stored in `localStorage`, keyed per-candidate, capped at 5 — a client-side convenience, not a new backend feature); removable filter chips for every active filter plus "Clear all filters"; two new sort options (**Relevance** — a substring-weighted score against your search query — and **Experience: Low to High**, alongside the existing Newest/Salary-High); a mobile filters **Drawer** (slide-over panel) replacing the old inline-toggle sidebar; a **Compare** feature — select up to 3 jobs via a checkbox on each card, then compare location/experience/skills/job type/salary/posted-date in a table; and an improved empty state suggesting popular roles. All of this stays client-side, matching the existing architecture (`GET /api/jobs` already returns the full open-jobs list once; filtering/sorting/search all happen in the browser).
- **Help & Support** (`/help`, public) — FAQ accordions (native `<details>`, zero JS, fully accessible) grouped under Registration/Login, Job Search, Applications, Recruiter Job Posting, Interviews, and Password Reset; a feedback/contact form (name, email, category, message) backed by a new `Feedback` entity, rate-limited at 5 submissions/hour/IP via the existing `IIpRateLimiter`, open to guests and logged-in users alike (captures `SubmittedByUserId` when authenticated). Admins review submissions on a new **Feedback** tab in `/admin`, moving each through New → In Progress → Resolved — same pattern as the existing Reports tab. Uses the footer's real fictional contact details, never fabricates new ones.
- **Account Settings** (`/settings`, any authenticated role) — profile summary; the "Your details" (name/phone) form, now shared between here and the recruiter's Company Setup page and available to Candidates too (previously Recruiter-only); **change password while logged in** (separate from the forgot-password OTP flow — verifies your current password, requires 8+ characters, and rotates your security stamp so other signed-in sessions are logged out); **notification preferences** — 4 real, enforced toggles (Messages/Applications/Interviews/Invitations) backed by a new `NotificationPreference` table that `NotificationService.NotifyAsync` checks before writing a notification row — account-critical notices (e.g. a job you posted expiring) are never gated; a Privacy section (candidates only) linking to the existing profile-visibility control rather than duplicating it; and a **Danger Zone** — "Request account deletion" requires re-entering your password, immediately deactivates and logs you out everywhere via the same non-destructive mechanism as admin suspension (no data is erased; an Admin can reactivate).
- **Light/dark theme** — a `ThemeContext` persists your choice in `localStorage` and otherwise follows your OS preference; toggle from the navbar. Only the neutral surface/text CSS variables are redefined for dark mode — brand colors (green primary, orange accent, navy nav bar) are untouched.
- **New reusable UI components** — `PageHeader`, `ErrorState`, `ConfirmDialog`, `SearchBar` (with the suggestions dropdown), `FilterChip`, `Drawer`, `SectionCard`, `Switch`, plus `ListSkeleton`/`TableSkeleton` alongside the existing `JobCardSkeleton`, and a new `info` toast kind. Retrofitted onto the new pages plus a representative pass over `AdminPage`, `ManageJobsPage`, `CandidateProfilePage`, `CandidateSearchPage`, and the recruiter dashboard.

**Migration**: `AddFeedbackAndNotificationPreferences` — adds the `Feedbacks` and `NotificationPreferences` tables (both purely additive, no changes to existing tables).

**Manual test steps**:
1. Log out and visit `/` — confirm the landing page renders with real stats, then search "React" from the hero and confirm it lands on `/jobs` with the query applied.
2. On `/jobs`, apply a few filters, confirm each shows as a removable chip and "Clear all filters" resets everything; switch sort to **Relevance** while searching a role name and confirm the closest title-matches float up; select 3 jobs via their **Compare** checkboxes and open the comparison table; resize to a narrow viewport and confirm the mobile **Filters** button opens a slide-over drawer with the same controls.
3. Visit `/help` as a guest, expand a couple of FAQ items, and submit the feedback form — then log in as `admin@demo.airecruiter.dev` / `Demo@123`, open `/admin` → Feedback, and move it to Resolved.
4. Log in as any user, open `/settings`, update your full name, change your password (confirm you can log back in with the new one), toggle off "Messages" notifications and confirm a new message notification no longer appears while an application-status notification still does, then open (but don't confirm) the account-deletion dialog to see the confirmation flow.
5. Toggle the theme button in the navbar and confirm it persists across a page reload.

## Company Verification, Job Quality Score, Follow Companies, Job Sharing & Activity Timeline

Five more portfolio features — same zero-paid-API, zero-external-verification constraint as everything above. All reuse existing conventions rather than inventing new ones: the admin-review pattern from Reports, the idempotent-save pattern from Saved Jobs, and the existing `AuditLogEntry`/`Notification` tables instead of a new activity-tracking table.

- **Company Verification Workflow** (`/onboarding` for recruiters, a new "Company Verification" tab on `/admin`) — a company's Owner submits business email, website, India HQ location, description, and an optional supporting-document reference (a free-text reference like a CIN/registration number, not a file upload). An Admin reviews each submission and moves it through **Pending → Verified / Rejected / Needs More Information**, with an internal note and a notification sent to the company's Owner either way. Once **Verified**, a **"Platform Verified"** badge (explicitly labeled as a platform review, never a government or legal verification) appears next to the company name on its public profile page and on every job card/detail page for that company. Recruiters keep full, unrestricted use of the platform while verification is Pending or unsubmitted — nothing is gated behind it.
- **Job Quality Score** (visible only to the owning recruiter, on `/jobs/mine` and after saving on `/post-job` — never on any public or candidate-facing job view) — a deterministic 0–100 score computed from 8 weighted checks (clear title, complete description, at least 3 required skills, an experience range, a confirmed India location or Remote, salary info, a complete company profile, an application deadline), with specific missing-item suggestions (e.g. *"Add at least three required skills."*). No external or paid AI is involved — it's a plain weighted checklist, the same pattern as the existing candidate Profile Strength score. The score is computed live on every read and never blocks publishing; only the fields that were already required to publish still gate it.
- **Follow Companies & Job Alerts** (a Follow button on every company profile and job detail page, a "Followed companies" card on the Candidate Dashboard) — a candidate can follow/unfollow a public company and toggle a per-company "notify me of new jobs" preference. When a followed company publishes a new job (a genuine first publish from Draft, not a Closed→Open reopen), every follower with notifications on gets an in-app notification. Recruiters can never see who follows their company — there is no endpoint or UI anywhere that exposes a follower list or count to them.
- **Job Sharing & Referral-Ready Links** (a Share button on every job card and job detail page) — Copy Link, WhatsApp, LinkedIn, and Email share options, plus the native Web Share sheet on supported mobile browsers. Each shared link carries a client-only, never-persisted token for a recruiter's own external analytics — the backend never sees or stores it. A separate, genuine aggregate `ShareCount` per job increments once per distinct visitor (reusing the exact same view-deduplication mechanism as the existing job view counter, with a distinct key prefix so the two counts never collide) and shows up next to each job on Manage Jobs and in the Views/Applications/Shares table on Recruiter Analytics.
- **Personalized Activity Timeline** (`/activity`, for both Candidates and Recruiters) — a single, filterable timeline (by activity type and date range) of everything that happened on your account: things you did (audit-logged actions like publishing a job or submitting an assessment) merged with things that happened to you (notifications like a message, an interview update, or an offer). Per the "reuse, don't duplicate" constraint, this is a **read-time merge of the existing `AuditLogEntry` and `Notification` tables** — no new tracking table was added. Two small gaps in audit coverage were closed as part of this (Offers and Skill Assessments previously had zero audit-log entries, only notifications), so the "things I did" side is now complete for both. Every query is strictly scoped to the caller's own id — nobody can see another user's timeline.

**Migration**: `AddCompanyVerificationFollowsAndShareCount` — adds a `CompanyFollows` table, 7 verification-related columns on `Companies`, and a `ShareCount` column on `JobPostings`. Purely additive — no existing table or column is modified or removed.

**Manual test steps**:
1. Log in as `recruiter1@demo.airecruiter.dev` / `Demo@123` (Nimbus Cloud Systems, seeded **Verified**) and confirm the "Platform Verified" badge appears on the company's public profile and its job cards; log in as `recruiter2@demo.airecruiter.dev` (BluePeak Analytics, seeded **Pending**) and confirm `/onboarding` shows the Pending status with no badge yet.
2. Log in as Admin (`admin@demo.airecruiter.dev` / `Demo@123`), open `/admin` → **Company Verification**, and confirm BluePeak's submission is listed with its business email, website, and document reference — **Approve** it, then log back in as `recruiter2` and confirm both the status and the badge updated, and a notification arrived.
3. As `recruiter1@demo.airecruiter.dev`, open `/jobs/mine` and confirm each job shows a quality-score badge — click one to expand its specific missing-item suggestions, then edit a sparse job on `/post-job` to add a few required skills and confirm the score increases after saving.
4. Log in as `candidate1@demo.airecruiter.dev` / `Demo@123` and confirm "Nimbus Cloud Systems" already appears under "Followed companies" on the dashboard (seeded); follow a second company from its profile page, then toggle its notify preference off and back on.
5. As `recruiter1@demo.airecruiter.dev`, publish a new Draft job for Nimbus and confirm `candidate1` (a follower with notifications on) receives an in-app notification; reopen a previously **Closed** job and confirm no new notification is sent.
6. On any job card or detail page, click **Share**, confirm **Copy Link**/WhatsApp/LinkedIn/Email options appear, copy the link, and confirm the job's share count increases on `/jobs/mine` and on `/recruiter/analytics`.
7. Open `/activity` as both a candidate and a recruiter — confirm each sees only their own actions and notifications merged into one timeline, filter by an activity type and a date range, and confirm an Offer or Skill Assessment event appears as an "action" (not just a notification) for whoever performed it.

## Company Reviews, Salary Insights, Advanced Saved Searches & Privacy Center

Four candidate-facing trust/insight features — same zero-budget, zero-external-verification constraint as everything above (no paid salary/review-platform APIs, no hosted AI). Every feature reuses an existing mechanism rather than duplicating it: reviews reuse the existing polymorphic report/moderation pattern, saved searches extend the existing Job Alert feature in place, salary insights aggregate data that already exists (disclosed job salaries + accepted offers), and account deletion completes an existing but incomplete workflow.

- **Company Reviews & Ratings** (a "Reviews" section on every company's public profile page) — a candidate who has applied to a company can submit a review (overall/culture/interview/work-life-balance/career-growth ratings, title, pros, cons, optional advice to management, and a self-declared relationship — Applicant/Interviewed/Received an Offer/Hired — cross-checked against their real application-status history so it can't be misrepresented). Reviews are **anonymous to the company** — no recruiter-facing or public endpoint ever exposes the reviewer's identity, only an Admin's moderation queue does. New reviews start **Pending** and only appear publicly once an Admin moves them to **Published**; an Admin can also **Reject** or a review can be **Flagged** through the existing generic report/flag flow (`ReportedEntityType.Review`), which pulls it from public view immediately pending review. A company's recruiters can post one public response per review, without ever learning who wrote it. Submission is rate-limited (5/day per user + per IP) and one review per candidate per company is enforced at the database level.
- **Salary Insights** (`/salary-insights`, public) — locally computed salary ranges grouped by role, a deterministic experience band (Junior/Mid/Senior/Lead from years of experience), and India location/remote, pooling two real data sources: published jobs with a disclosed salary range and **accepted** offers (annualized, anonymized — never an individual candidate's specific offer). A bucket with fewer than 5 data points never shows a number — it shows "Not enough data yet," so a rare role/location combination can never be reverse-engineered to one person's salary. When a recruiter posts or edits a job with a salary notably outside the locally observed range, they see a one-line advisory on Manage Jobs (never a validation error — publishing is never blocked on it).
- **Advanced Saved Searches** (`/alerts`, evolving the existing Job Alerts feature rather than duplicating it) — a saved search now has a name, keyword, skills, job type, experience, India state/city, remote preference, salary range, and a sort preference; duplicate/edit/activate-deactivate/delete, with exact-duplicate configurations rejected. One saved search can be marked as your **default dashboard search**, shown in place of "all active alerts" on the Candidate Dashboard. Publishing a genuinely new job (not a reopen) notifies every candidate whose active saved search matches it — one notification per candidate even if several of their searches match.
- **Privacy Center & Account-Data Export** (`/privacy`, linked from Settings) — every authenticated user can see their profile-visibility setting (candidates) and notification preferences at a glance, download a copy of their own account data as **JSON** (full nested export — profile, applications, saved jobs, saved searches, followed companies, reviews written) or **CSV** (a flat summary), and manage account deletion. Requesting deletion now starts a genuine **14-day grace period** — the account stays fully active and usable, and can be self-service cancelled at any time — rather than deactivating immediately as in the prior version; the existing background sweep (`JobLifecycleSweepService`, the app's one and only scheduled task) performs the actual deactivation once the grace period elapses uncancelled, using the same mechanism as an Admin suspension. Every export and deletion action is rate-limited and audit-logged, and an Admin can view pending deletion requests and cancel one on a user's behalf.

**Migration**: `AddCompanyReviewsSavedSearchUpgradeAndAccountDeletionGracePeriod` — adds a `CompanyReviews` table, 5 columns on `JobAlerts` (name/keyword/salary range/sort/default flag), and a `DeletionRequestedAt` column on `Users`. Purely additive — no existing table or column is modified or removed.

**Manual test steps**:
1. Log in as `candidate1@demo.airecruiter.dev` / `Demo@123` and open the Nimbus Cloud Systems public profile — confirm the seeded Published review with a recruiter response appears with no reviewer name anywhere; log in as Admin (`admin@demo.airecruiter.dev` / `Demo@123`), open `/admin` → **Reviews**, and confirm the seeded Pending and Flagged reviews are listed with the reviewer's real name — approve or reject one and confirm it updates.
2. As `candidate1`, open a company you've applied to and submit a new review — confirm it doesn't appear publicly until an Admin approves it; try selecting "Hired" as your relationship without ever having been hired there and confirm it's rejected with a clear validation message.
3. Visit `/salary-insights`, search "Cloud Platform Engineer" in Bengaluru and confirm real min/median/max numbers (5 seeded data points); search "Underwater Robotics Engineer" and confirm it shows "Not enough data yet" instead of a number.
4. As `recruiter1@demo.airecruiter.dev`, edit a job's salary to something far below the local range and confirm an advisory (non-blocking) appears on Manage Jobs.
5. Open `/alerts` as `candidate1` and confirm the two seeded saved searches appear, one marked **Default**; try creating an exact duplicate of one and confirm it's rejected; duplicate one, then set the duplicate as the new default and confirm the Candidate Dashboard's alert-matches section reflects it.
6. As `recruiter1`, publish a new job matching a saved search's keyword and confirm the search's owner gets an in-app notification.
7. Open `/privacy` as any user, download both a JSON and a CSV export, and confirm neither contains a password, security token, or another user's data. Request account deletion from `/settings`, confirm your account stays active with a visible countdown, then cancel it and confirm the countdown disappears. Log in as Admin and confirm `candidate9@demo.airecruiter.dev` (seeded with a pending deletion request) appears under Users → Pending account deletions.

## Customizable Job-Application Screening Questions

Recruiters can attach custom qualifying questions to a job posting — candidates answer them as part of applying, and recruiters review and filter applicants by their answers. Zero-budget, fully local: no external form-builder or survey service, just a new set of database tables and deterministic validation.

- **Question types**: Short Text, Long Text, Yes/No, Single Choice, Multiple Choice, Number, and URL. For each question a recruiter sets the question text, type, required/optional, optional help text, choice options (for Single/Multiple Choice), display order, and an optional **preferred answer** — a private reference note visible only to the recruiter's own company, never to candidates or the public.
- **Post Job / Edit Job flow** — a new "Application Questions" section lets a recruiter add, edit, reorder (up/down), duplicate, and delete questions (up to 10 per job) entirely client-side before saving, plus a "Preview as candidate" mode showing exactly what applicants will see. Adding a question to an already-Published job is always allowed. Once a question has at least one submitted answer, its **type and options become locked** and it can no longer be deleted — attempting either is rejected with a clear error — so editing a live job can never corrupt a prior applicant's answers; its text, help text, required flag, and display order remain freely editable at any time.
- **Apply flow** — the job's screening questions appear as part of the application form, with the correct input for each type and inline required-field validation before submission; answers are preserved in the form if submission fails, so a candidate never has to redo their work. Candidates can view their own submitted answers on their application detail page — the recruiter's preferred answer is never included in that response at all, not just hidden in the UI.
- **Applicant management** — a recruiter's application detail page shows a structured "Screening answers" section (with the preferred answer shown alongside, clearly labeled as private). The applicant list supports filtering by a Yes/No answer, a selected option, a numeric range, or whether all required questions were answered, and shows a compact "X/Y required answered" summary per applicant. Screening answers are never used to auto-reject an applicant — moderation/filtering is advisory only, the recruiter still makes every status decision.
- **Authorization**: recruiters can manage questions and view answers only for their own company's jobs (reusing the existing company-ownership check everywhere); candidates can only submit answers for their own application; a cross-company recruiter is blocked from both the applicant list and any individual application, verified end-to-end.

**Migration**: `AddJobScreeningQuestions` — four new tables (`JobScreeningQuestions`, `ScreeningQuestionOptions`, `ScreeningAnswers`, `ScreeningAnswerSelectedOptions`), purely additive.

**Manual test steps**:
1. Log in as `recruiter1@demo.airecruiter.dev` / `Demo@123`, open "Senior Backend Engineer" for editing, and confirm the seeded "Application Questions" section shows a Number, a Yes/No, and a Multiple Choice question, each with a preferred answer — try "Preview as candidate" to see the candidate-facing rendering.
2. Add a new Single Choice question with two options, reorder it above an existing one, duplicate it, then delete the duplicate — save and confirm the change persists.
3. Try deleting the seeded Yes/No question (it already has an answer from candidate1's seeded application) and confirm it's blocked with a clear message; confirm deleting a brand-new, never-answered question works fine.
4. Log in as `candidate2@demo.airecruiter.dev` / `Demo@123`, open "Senior Backend Engineer", and apply — confirm the screening questions appear, required-field validation blocks submission until answered, and submitting succeeds.
5. On your own application detail page, confirm your answers show but no preferred answer appears anywhere in the page or network response.
6. Log back in as `recruiter1@demo.airecruiter.dev`, open the applicant list for the job, and confirm the preferred answers are visible on the application detail page; filter the applicant list by the Yes/No question and by "all required answered."
7. Log in as `recruiter2@demo.airecruiter.dev` (a different company) and confirm both the applicant list and the individual application detail page for this job are inaccessible (403).

## Refresh Tokens for Session Continuity

Login/register now issue a long-lived, single-use **refresh token** alongside the existing short-lived JWT access token, so a session survives past the access token's 120-minute expiry without forcing a re-login.

- **Storage**: only a SHA-256 hash of the refresh token is ever persisted (`RefreshToken.TokenHash`) — the same CSPRNG-generate/hash-at-rest pattern the password-reset flow already used. The raw token is returned to the client exactly once, at issuance.
- **Rotation**: `POST /api/auth/refresh` atomically revokes the presented token and issues a new access token + a new child refresh token in one call. A concurrency token on `RevokedAtUtc` makes a race between two simultaneous redemptions of the same token resolve to exactly one winner — the loser is treated as reuse (see below), never as a second valid child.
- **Theft detection**: presenting an already-rotated (or already-revoked) refresh token immediately revokes every token in that chain for the user, and the request is rejected — the assumption is that reuse means the token was copied/stolen.
- **Revoke-all on security events**: password reset, password change, and admin suspension each revoke every one of a user's refresh tokens, exactly where they already rotate `User.SecurityStamp` — so a compromised or ended session is fully cut off, not just its access token.
- **Frontend**: the refresh token is stored the same way the access token is (`localStorage`); a 401 caused by access-token expiry triggers a silent background refresh-and-retry-once, and concurrent requests that all 401 at once share a single in-flight refresh call rather than each racing their own. Logout revokes the current refresh token server-side (best-effort) before clearing local storage.
- **No JWT validation was weakened** — signature, expiry, and the existing security-stamp check in `Program.cs` are unchanged; refresh tokens are a second, independent credential.

**Migration**: `AddRefreshTokens` — one new table, purely additive.

**Manual test steps**:
1. Log in as any seeded user and confirm the network response includes a `refreshToken` alongside `token`.
2. Call `POST /api/auth/refresh` with that refresh token — confirm you get back a new access token and a *different* refresh token, and that redeeming the original token again now fails with 401.
3. Change your password (Account Settings) and confirm any refresh token issued before the change now fails to redeem.
4. In the browser, manually expire/corrupt the stored access token and make any authenticated request — confirm it's silently retried after a refresh rather than redirecting to `/login`.

## Pagination for Job Search and Candidate Search

`GET /api/jobs` and `GET /api/recruiters/candidates` now accept `page`/`pageSize` query parameters and return a `{ items, totalCount, page, pageSize }` wrapper (mirroring the existing Adzuna external-job-search pagination shape) instead of the full result set.

- Defaults to page 1 / 20 items; `pageSize` is clamped to a maximum of 50 server-side regardless of what's requested.
- Sorting/filtering still composes correctly with paging — filters narrow the query (and, for candidate search, the skills filter still applies in-memory) before `Skip`/`Take` is applied, so page boundaries are stable.
- **`/jobs`** now fetches pages incrementally — "Load more roles" requests the next server page rather than slicing an already-fully-loaded array — while every existing client-side filter chip, sort option, and search-suggestion still works against the accumulated set of loaded jobs. The **Compare** feature (up to 3 jobs) now keeps working correctly across pages, since a job stays in the accumulated list once loaded rather than disappearing when the next page replaces it.
- **Candidate Search** gained Previous/Next paging controls with a page indicator; company-scoping and profile-visibility rules are enforced exactly as before, unchanged by paging.
- **CSV export is unaffected** — both export endpoints ignore paging entirely and always export the full filtered result set.

**No migration required** — purely additive DTO/query changes.

**Manual test steps**:
1. On `/jobs`, confirm the initial page loads a bounded set of jobs and "Load more roles" appears if more exist; click it and confirm new jobs are appended, not replacing the current ones.
2. Select a job on the first page for **Compare**, load a second page, and confirm the first job is still shown as selected and still appears in the Compare modal.
3. As a recruiter, open Candidate Search, run a search that matches more than 20 candidates, and page through with Previous/Next — confirm the count and pages line up and no candidate appears twice.
4. Export CSV from Candidate Search after paging to page 2 — confirm the downloaded file contains the full filtered set, not just the current page.

## Rate Limiting via ASP.NET Core Built-in Middleware

Framework-level rate limiting (`Microsoft.AspNetCore.RateLimiting`) now sits in the pipeline alongside the existing `IIpRateLimiter` business-rule limiters (forgot-password, referrals, feedback, company reviews, etc.), which are unchanged and still enforce their own finer-grained, longer-window limits.

- **Policies** (all partitioned by client IP, for both anonymous and authenticated routes — configurable in `appsettings.json` under `RateLimiting`): `auth` (strict — `AuthController` as a whole: login, register, forgot/verify/reset-password, refresh, logout), `export` (CSV/report export endpoints), and `general` (a global fallback for everything else, automatically overridden by the two policies above wherever they apply — nothing is ever double-limited).
- **429 responses** always use the same `application/problem+json` shape the rest of the app already uses for errors, with a `Retry-After` header and an `errorCode` of `RATE_LIMITED` — including reconciling the one endpoint (`AccountController.Export`'s existing per-user `IIpRateLimiter` check) that previously returned an ad-hoc response body.
- Limits are configuration-driven (`RateLimiting:Auth`/`General`/`Export`, each a `PermitLimit`/`WindowSeconds` pair), following the same options-binding convention as `Smtp:*`.

**No migration required.**

**Manual test steps**:
1. Send 11+ rapid `POST /api/auth/login` requests from the same machine — confirm the 11th+ returns `429` with a `Retry-After` header and a `RATE_LIMITED` error body, while the first 10 behave normally (401 for bad credentials).
2. Confirm normal browsing (`GET /api/jobs`, a few requests) is unaffected by the general policy.
3. Confirm the feedback/referral endpoints' existing `IIpRateLimiter` limits still work exactly as before (their own tests are unchanged and still pass).
4. As a recruiter, call a CSV export endpoint (e.g. `/api/recruiters/reports/export/jobs`) more than the configured export limit within a minute — confirm a 429 rather than the export endpoint just running slowly or erroring.

## Recently-Viewed Jobs Backed by Real Data

The candidate dashboard's "Recently viewed" section is now backed by a real `JobView` table (`CandidateProfileId`, `JobPostingId`, `ViewedAt`) — previously it showed a couple of open jobs as clearly-labeled sample data, since no view-tracking existed.

- Visiting a job detail page while logged in as a candidate records (or updates) a view row for that candidate+job — a repeat visit bumps `ViewedAt` rather than creating a duplicate (enforced by a unique index).
- The dashboard shows the candidate's own most-recently-viewed 10 distinct jobs, most-recent first. A job that's since closed/been archived still appears (history is history) but is marked "No longer open" rather than silently disappearing.
- This is entirely separate from the existing anonymous, in-memory, de-duplicated `JobPosting.ViewCount` used for recruiter-facing analytics — that mechanism never stores a candidate id and is untouched by this feature; the two do not interact.
- Every query is scoped to the calling candidate's own resolved profile id — there is no code path that can return another candidate's view history. Recruiters, admins, and anonymous visitors never create `JobView` rows (the recording endpoint is `[Authorize(Roles = "Candidate")]`).

**Migration**: `AddJobViews` — one new table, purely additive.

**Manual test steps**:
1. Log in as a candidate, open a job's detail page, then visit the candidate dashboard and confirm it appears under "Recently viewed" (no "Sample data" label anymore).
2. Revisit the same job and confirm the dashboard still shows only one entry for it (moved to the top, not duplicated).
3. As a recruiter, close that job, then revisit the candidate dashboard and confirm the job still appears but now shows "No longer open".
4. Confirm recruiters and logged-out visitors browsing jobs never affect a candidate's "Recently viewed" list.

## Automated Playwright E2E Tests + CI Pipeline

A Playwright suite (`frontend/e2e/`) now exercises three high-value journeys against the real running app (real backend + real frontend, no mocked network calls), plus a GitHub Actions workflow (`.github/workflows/ci.yml`) that runs the full xUnit suite and this E2E suite on every pull request using a Dockerized SQL Server — no paid CI or database service.

- **Journeys covered**: (1) recruiter posts a job → candidate applies → recruiter schedules an interview → candidate accepts → downloads the `.ics` file; (2) candidate applies → recruiter sends an offer → candidate accepts it → application status becomes **Hired**; (3) a negative path — applying to a closed job is rejected with a clear inline error and a `409`, never a `500`.
- **Deterministic state**: a test-only `POST /api/test/reset-seed` endpoint drops and reseeds a dedicated database between specs — it's strictly gated behind `ASPNETCORE_ENVIRONMENT=Testing` (`TestController.ResetSeed`) and returns `404` in every other environment, so it is never reachable in Development or Production. Specs run sequentially (`workers: 1` in `playwright.config.ts`) since they share this one seeded database.
- **Running locally**:
  ```bash
  cd frontend
  npm install
  npx playwright install --with-deps chromium
  npx playwright test          # starts the backend (Testing env) and frontend dev server itself
  npx playwright show-report   # view the HTML report after a run
  ```
  This requires a local SQL Server/LocalDB instance reachable from `src/AIRecruiter.API/appsettings.Testing.json`'s connection string — point it at a dedicated database (never your dev database; a Testing-environment reset drops it).
- **CI**: every pull request runs `dotnet test` against a `mcr.microsoft.com/mssql/server` service container, then builds the frontend, starts both servers, and runs the Playwright suite — see the CI badge at the top of this file. Playwright's HTML report and the backend log are uploaded as workflow artifacts on every run (pass or fail) for debugging.

**No migration required** beyond the ones already listed above (the E2E database is just a normal Testing-environment database, reset and reseeded by `DataSeeder.ResetForTestingAsync`).

**Manual test steps**:
1. Run `npx playwright test` locally per the instructions above and confirm all 3 specs pass.
2. Open `npx playwright show-report` and confirm the HTML report renders with all 3 journeys green.
3. Push a branch and open a pull request — confirm the `CI` workflow runs and both the unit-test and Playwright jobs succeed (or, if a spec is intentionally broken, that the workflow fails clearly with a downloadable Playwright report artifact).

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
