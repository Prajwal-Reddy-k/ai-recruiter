# Interview Guide

## Table of Contents
- [30-Second Elevator Pitch](#30-second-elevator-pitch)
- [60-Second Explanation](#60-second-explanation)
- [2–3 Minute Technical Explanation](#23-minute-technical-explanation)
- [Live Demo Walkthrough Script](#live-demo-walkthrough-script)
- [Common Interview Questions & Answers](#common-interview-questions--answers)
- [STAR-Format Stories](#star-format-stories)
- [Résumé Bullets](#résumé-bullets)
- [GitHub Project Description](#github-project-description)
- [LinkedIn Project Description](#linkedin-project-description)
- [Glossary](#glossary)

## 30-Second Elevator Pitch

"AI Recruiter is a full-stack recruitment platform I built with a .NET Clean Architecture backend and a React/TypeScript frontend. Candidates search and apply to jobs with a transparent, locally-computed resume match score — no external AI API, fully explainable. Recruiters run a real job lifecycle — draft, publish, close, archive — manage applicants on a Kanban board, schedule interviews, and search candidates across their whole company with CSV export. It's built to run entirely on free tooling, which forced me to think carefully about what actually needs a paid service and what doesn't."

## 60-Second Explanation

"It's an India-focused job platform with three roles — Candidate, Recruiter, and a seeded Admin. Candidates build a profile, upload a resume, and apply to jobs; at apply time, a deterministic scoring engine computes a match score from weighted skill coverage, TF-IDF text similarity, experience, and education — and shows the candidate exactly *why* they got that score, which was a deliberate choice over a black-box AI call. Recruiters onboard a company, then post jobs through a real lifecycle — save as a draft, publish when ready (which re-validates the location), close, reopen, or archive — and I enforce that state machine on the backend, not just in the UI. They manage applicants on an accessible Kanban board, propose interview time slots the candidate can accept or decline, and search every candidate who's applied to *any* job at their company, not just their own postings, with filters and a CSV export. Everything's built on Clean Architecture — Domain, Application, Infrastructure, API — with JWT auth, and every optional third-party integration, like Cloudinary for resume storage or Adzuna for external job listings, is behind an interface and only activates if credentials are configured, so the whole thing runs for free by default."

## 2–3 Minute Technical Explanation

"Architecturally, it's four .NET projects following Clean Architecture: `Domain` has just entities and enums, no dependencies. `Application` has the DTOs, service interfaces, and the framework-free business logic — things like the India-location validator and the resume-matching engine live here specifically so I could unit test them without spinning up a database. `Infrastructure` has the EF Core `DbContext`, the migrations, and every concrete service implementation. `API` is thin controllers that pull the caller's identity off the JWT and call an injected interface — they never talk to EF Core directly.

For auth, I hash passwords with BCrypt, issue a JWT with the user id, email, name, and role as claims, and validate issuer/audience/lifetime/signing key on every request. Role checks happen via `[Authorize(Roles=...)]` at the controller level, but that's not enough on its own — a Recruiter shouldn't be able to touch another recruiter's job just because they're both Recruiters. So every service that mutates a specific resource re-derives the caller's id from the JWT and compares it against the entity it loaded — for example, checking `job.RecruiterProfile.UserId == callerId` before allowing a status change. I made one deliberate exception to that pattern: candidate search is scoped to the caller's whole *company*, not just their own job postings, because that's genuinely what a company-wide candidate search should do — but I still gate the actual write actions, like changing an application's status, at the stricter per-recruiter level, and the frontend gets a server-computed `canManage` flag so it knows which controls to show.

The job lifecycle is the piece I'm most proud of getting right — it's a `Draft → Open → Closed ⇄ Open → Archived` state machine with an explicit allowed-transitions table on the backend, so even if someone bypasses the UI and hits the API directly, they can't skip straight from Draft to Archived-then-back-to-Open. Publishing re-validates the India location even if it was left incomplete while in Draft.

On the data side, there are 14 entities and 5 EF Core migrations. I use unique indexes — not just application-level checks — to prevent duplicate applications and duplicate saved jobs, because a service-layer check alone has a race condition under concurrent requests; the database is the actual source of truth for uniqueness.

For the 'AI' in the resume matching, I was intentional about calling it 'AI-assisted / explainable matching' rather than implying a black-box model — it's a weighted, deterministic algorithm (skill coverage, TF-IDF/cosine similarity, experience, education) that returns a full breakdown, and it's explicitly decision support — it never auto-rejects or auto-ranks candidates anywhere in the UI, a human always makes the call."

## Live Demo Walkthrough Script

1. **Home / job search** — open `/jobs`, show the state/city/remote/job-type/experience/skill filters, point out the India-only structured location on each card.
2. **Candidate registration/login/dashboard** — register a candidate, note the redirect to `/login` (not auto-logged-in) with the email prefilled, log in, tour the dashboard: profile completion, recommended jobs, saved jobs, alert matches, upcoming interviews.
3. **Saving/applying/tracking jobs** — save a job (bookmark icon), open a job detail page, apply with a cover note, show the match-score breakdown on the resulting application, then show `/applications` and the status timeline.
4. **Recruiter registration/login/dashboard** — register (or log in as `recruiter1@demo.airecruiter.dev`), tour the recruiter dashboard: active jobs, total applicants, job performance table, upcoming interviews, recent activity feed.
5. **Company onboarding** — show the onboarding form, India location selector, and explain that posting a job is blocked until this is complete (`409 NOT_ONBOARDED`).
6. **Job draft/publish/close flow** — post a job, save as Draft with an incomplete location, then edit it, add a location, and Publish; show it now appears in public search; then on Manage Jobs, Close it (confirmation dialog), Reopen it, then Duplicate it.
7. **Applicant pipeline** — open the Kanban board for a job with applicants, move a card via the "Move to…" control, open Candidate Search, filter across the whole company, open a candidate's detail drawer, export CSV and open the file to show the safe column set.
8. **Interview scheduling** — schedule a time with an applicant, switch to the candidate account, accept it, download the `.ics` file.
9. **Analytics** — open `/recruiter/analytics`, walk through the hiring funnel, applications-by-city, top skills, and views-vs-applications table.
10. **Swagger / API** — open `http://localhost:5087/swagger`, authorize with a token from `/api/auth/login`, run a `GET /api/jobs` call live.
11. **Database** — open the DB in SSMS/Azure Data Studio (or reference [DATABASE_DESIGN.md](DATABASE_DESIGN.md)'s ER diagram), show `JobApplications`' unique index and the `ApplicationStatusHistories` table as the audit trail for one application.

## Common Interview Questions & Answers

**Why this tech stack?**
".NET/ASP.NET Core because it's a strongly-typed, well-structured environment for enforcing Clean Architecture boundaries with real compile-time guarantees, and EF Core's migrations give me a reliable, versioned schema history. React + TypeScript on the frontend because I wanted end-to-end type safety on the API contract, and Vite for a fast local dev loop. I deliberately avoided a heavier state-management library — React Context plus `useState`/`useEffect` was genuinely sufficient for this app's scope, and I'd rather show I know when *not* to reach for extra complexity."

**Why Clean Architecture?**
"It keeps business rules (ownership checks, the job-status state machine, resume validation) testable and framework-agnostic — the Application layer doesn't know EF Core or ASP.NET Core exist. It also makes the 'optional integration' pattern clean: swapping Cloudinary for local disk storage, or Adzuna for a disabled stub, is a one-line change in the DI registration, because callers only ever depend on the interface."

**How does JWT authentication work?**
"BCrypt-hash the password at registration; at login, verify against the hash, and if it matches, issue an HMAC-SHA256-signed JWT with the user id, email, name, and role as claims, expiring after a configurable window. The frontend stores it and an axios interceptor attaches it as a Bearer token on every request. The backend validates issuer, audience, lifetime, and signature on every call — nothing about identity is ever trusted from a request body or query string."

**How do CORS and frontend/backend communication work?**
"They're different origins locally, so cross-origin writes trigger a preflight `OPTIONS` request. The key detail is that CORS middleware has to run *before* HTTPS redirection in the pipeline — otherwise the API would 307-redirect the preflight itself, and browsers refuse to follow a redirect for a preflight, which breaks every POST/PUT/PATCH/DELETE. I actually had to fix that ordering bug during development, which is part of why I documented it explicitly."

**How are roles and ownership enforced?**
"Two layers: `[Authorize(Roles=...)]` on the controller action for the role gate, and then an explicit ownership comparison inside the service — load the entity, compare its owning user/recruiter id against the JWT-derived caller id, throw a `ForbiddenException` on mismatch. Role checks alone aren't enough; without the ownership check, any recruiter could edit any other recruiter's job."

**How does the data model work?**
"Fourteen entities under Clean Architecture's Domain layer — Users branch into CandidateProfile or RecruiterProfile 1:1, RecruiterProfiles belong to a Company, Companies own JobPostings, JobPostings receive JobApplications, and JobApplications have a full ApplicationStatusHistory rather than just a current-status field, so the timeline is always reconstructible. Two unique indexes — on JobApplications(JobPostingId, CandidateProfileId) and SavedJobs(CandidateProfileId, JobPostingId) — are the actual source of truth for duplicate-prevention, not just an application-level check."

**How do job drafts and publishing work?**
"A job can be created as a Draft, which skips location validation since it might be intentionally incomplete. Publishing — either at creation or later via a status transition — re-validates the India location and sets a `PublishedAt` timestamp. Status changes go through an explicit allowed-transitions table on the backend, so you can't jump straight from Draft to Archived-then-reopen; Archived is a genuine terminal state."

**How do application statuses work?**
"Nine states — Applied through Hired/Rejected/Withdrawn, plus Screening/Shortlisted/InterviewScheduled/InterviewCompleted/Offer in between. Every transition appends a row to `ApplicationStatusHistory` with who changed it, when, and an optional note visible to the candidate — status is never just overwritten in place."

**How do you prevent duplicate saves/applications?**
"Two layers again: the service pre-checks before inserting, but the actual guarantee is a unique database index on the (candidate, job) pair, which also catches the race condition where two near-simultaneous requests both pass the pre-check. I specifically wrapped the insert in a try/catch for the resulting `DbUpdateException` and translate it into the same friendly conflict error the pre-check would have thrown."

**How does India-only location validation work?**
"There's a checked-in JSON dataset of India's states/UTs and their cities, embedded as a resource and loaded once into an in-memory catalog. Every write path that touches a location — job posting, candidate profile, company onboarding — calls the same validator against that catalog, so the UI's dropdown can't be bypassed by hitting the API directly with an invalid city/state pair."

**How is resume matching designed to be explainable and fair?**
"It's a deterministic, rule-based algorithm, not a model — weighted skill coverage (55%), TF-IDF/cosine text similarity against the job description (30%), experience match (10%), and education match (5%), all computed locally from resume text I extract myself with PdfPig/OpenXml. It returns matched skills, missing skills, and a full explanation string breaking down each component's score. It's explicitly labeled decision support — it's never used to auto-rank or auto-reject anywhere in the UI."

**How would you scale/deploy this?**
"The two things that are currently process-local — the Adzuna response cache and the view-count de-duplication dictionary — would need to move to Redis before running more than one API instance. Resume storage is already behind an interface, so moving to a real object store like S3 is a one-line DI change. I'd add structured logging, a rate limiter (especially on auth and the CSV export), containerize both projects, and set up CI to build/test on every PR before deploying."

**What challenges did you solve?**
"The CORS/HTTPS-redirect preflight ordering bug was a real one I hit and fixed. Designing the job-status state machine to be enforced server-side rather than just hidden in the UI took a deliberate pass — I built an explicit allowed-transitions table rather than a scattered set of if-statements. And scoping candidate search to the whole company (not just one recruiter's jobs) while keeping the stricter per-recruiter check on the actual write path required a `canManage` flag computed server-side rather than assumed on the frontend."

**What would you improve next?**
"Email/SMS delivery for notifications and alerts — deliberately out of scope for a zero-budget build, but the notification and alert data models are already structured to support it. A refresh-token flow and password reset. Automated end-to-end browser tests — right now testing is unit/service-level only. And moving the in-memory caches to Redis if I ever needed to run more than one instance."

## STAR-Format Stories

**Situation**: The initial CORS setup worked for GET requests but every POST/PUT from the frontend failed with a cryptic "Redirect is not allowed for a preflight request" error.
**Task**: Diagnose why only cross-origin *write* requests were failing.
**Action**: Traced it to middleware ordering in `Program.cs` — `UseHttpsRedirection()` was running before `UseCors()`, so the API's own redirect middleware intercepted the browser's preflight `OPTIONS` request before CORS could answer it, and browsers refuse to follow a redirect for a preflight.
**Result**: Reordered the middleware pipeline (CORS before HTTPS redirection) and documented the reasoning directly in a code comment so it can't silently regress; also disabled HTTPS redirection entirely in Development, since the dev server intentionally runs plain HTTP.

**Situation**: A generic "job status" endpoint would let any target status be set from any current status, with no real lifecycle enforcement.
**Task**: Give recruiters a real Draft/Published/Closed/Archived workflow that can't be bypassed by calling the API directly.
**Action**: Designed an explicit allowed-transitions table server-side (`Draft→{Open, Archived}`, `Open→{Closed, Archived}`, `Closed→{Open, Archived}`, `Archived→{}`), added a re-validation of the India location specifically at publish time (since a Draft can be saved incomplete), and built a "Manage Jobs" UI with tabs, per-job application/view counts, and confirmation dialogs for the two consequential actions (Close, Archive).
**Result**: A job can never skip an invalid transition even via a raw API call, and archiving is genuinely terminal — verified with dedicated unit tests for both the valid and invalid transition paths.

**Situation**: A recruiter-facing candidate search needed to span every job at a company, not just one recruiter's own postings, but every other ownership check in the codebase was scoped per-recruiter.
**Task**: Support company-wide search without weakening the stricter per-recruiter checks that already protect application status changes.
**Action**: Derived the company id server-side from the caller's own `RecruiterProfile` (never accepted from the client), scoped search/detail/export to that company, and added a server-computed `canManage` boolean to each result so the frontend only exposes status-change controls where the caller is actually authorized — the underlying status-change endpoint's stricter check was left untouched.
**Result**: Recruiters can discover a colleague's applicants, but can't silently gain write access to applications they don't own — verified with unit tests asserting a recruiter from a different company sees zero results and a `403`/`404` on cross-company detail/resume access.

## Résumé Bullets

- Built a full-stack recruitment platform (ASP.NET Core 10 + React 19/TypeScript) using Clean Architecture, with JWT authentication, role-based authorization, and ownership checks enforced on every protected endpoint.
- Designed a job-posting lifecycle state machine (Draft/Published/Closed/Archived) with server-side transition validation, edit/duplicate actions, and a recruiter management UI with tabbed filtering and per-job analytics.
- Implemented a deterministic, explainable resume-matching engine (weighted skill coverage, TF-IDF/cosine similarity, experience/education signals) running entirely locally, with a documented scoring breakdown surfaced to both parties.
- Built a company-scoped recruiter candidate-search feature with filter/sort, a candidate detail view, and an audited CSV export excluding all sensitive fields.
- Designed a privacy-conscious job-view tracking system using hashed, in-memory, time-windowed visitor de-duplication.
- Wrote 90+ unit/service tests (xUnit, EF Core InMemory, Moq) covering ownership, duplicate-prevention, state-machine transitions, and cross-tenant isolation.

## GitHub Project Description

> India-focused recruitment platform (ASP.NET Core 10 + React 19/TypeScript, Clean Architecture) with candidate/recruiter/admin roles, a full job lifecycle (draft → publish → close → archive), explainable local resume matching, an accessible Kanban applicant pipeline, interview scheduling with `.ics` export, company-scoped candidate search with CSV export, and privacy-conscious view tracking — built to run entirely on free/local tooling.

## LinkedIn Project Description

> I built **AI Recruiter**, a full-stack hiring platform, to explore what a genuinely complete recruitment workflow looks like end-to-end — not just a job list with an apply button.
>
> On the backend (ASP.NET Core 10, EF Core, Clean Architecture), recruiters run jobs through a real lifecycle — draft, publish, close, reopen, archive — enforced by an explicit state machine, manage applicants on an accessible Kanban board, schedule interviews, and search candidates across their whole company with a CSV export. Candidates get a resume-match score I made deliberately transparent: it's a deterministic, weighted algorithm (skill coverage + text similarity + experience + education), not a black-box AI call, and it always shows its work.
>
> On the frontend (React 19 + TypeScript), everything is typed end-to-end against the API contract, with role-gated routing and a small hand-built component library.
>
> I was intentional about keeping it zero-cost: every optional integration (resume cloud storage, external job search) sits behind an interface and only activates if credentials are configured — the app runs completely free by default, including the "AI" matching, which is entirely local.

## Glossary

| Term | Meaning in this project |
|---|---|
| **Clean Architecture** | The layering (Domain → Application → Infrastructure → API) used to keep business rules independent of frameworks |
| **Ownership check** | A service-level comparison between the JWT-derived caller id and an entity's owning user/recruiter id, distinct from a role check |
| **State machine (job lifecycle)** | The `JobPostingService.AllowedTransitions` table restricting which `JobStatus` changes are permitted |
| **Explainable matching** | The deterministic, weighted resume-scoring algorithm and its human-readable breakdown — as opposed to an opaque ML/LLM score |
| **DTO** | Data Transfer Object — the shape returned by/sent to the API, distinct from the internal EF Core entity |
| **`AppException`** | The typed exception hierarchy (`NotFoundException`, `ForbiddenException`, `ConflictException`, `ValidationException`, `UnauthorizedException`) every service throws instead of raw exceptions |
| **View de-duplication** | The in-memory, hashed-visitor-key mechanism that stops a page refresh from inflating a job's view count |
| **`canManage`** | A server-computed flag on candidate-search results indicating whether the caller may change that specific application's status |
| **India location catalog** | The checked-in `indian-locations.json` dataset and the `IndianLocationCatalog`/`IndiaLocationValidator` that validate every location field against it |
