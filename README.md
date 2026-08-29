# AI Recruiter

An India-focused recruitment platform with a .NET Web API backend, React + TypeScript frontend, and SQL Server database. Built as a zero-budget portfolio project — the core app runs entirely locally on free/open-source tooling and checked-in data; a few enhancements are optional and credential-gated, and every optional external integration is server-side only, behind an interface, and fails gracefully.

## Structure

```
AI Recruiter/
├── src/
│   ├── AIRecruiter.API             # ASP.NET Core Web API, controllers, auth, middleware
│   ├── AIRecruiter.Application     # DTOs, service interfaces, matching engine, validation
│   ├── AIRecruiter.Domain          # Entities, enums
│   └── AIRecruiter.Infrastructure  # EF Core DbContext, migrations, service implementations
├── tests/
│   └── AIRecruiter.UnitTests       # xUnit tests (matching, storage, auth, HTTP adapters)
├── frontend/                       # React + TypeScript (Vite)
├── database/                       # Reserved for raw SQL scripts / seed data
└── AIRecruiter.sln
```

## Backend

Requires .NET 10 SDK and SQL Server (LocalDB works for dev — ships with Visual Studio / SQL Server Express).

```bash
cd src/AIRecruiter.API
dotnet restore
dotnet ef database update --project ../AIRecruiter.Infrastructure --startup-project .
dotnet run
```

API runs at `http://localhost:5087` (Swagger UI at `/swagger` in Development). Use the **"http" launch profile** (`src/AIRecruiter.API/Properties/launchSettings.json`) for local development — the API deliberately does **not** force an HTTPS redirect in Development (see "CORS & HTTPS in development" below), so the plain-HTTP profile is the one to run against.

Run the test suite:

```bash
dotnet test
```

### CORS & HTTPS in development

The frontend (`http://localhost:5173`/`5174`, or whatever port Vite lands on) and the API (`http://localhost:5087`) are different origins, so the browser sends a CORS preflight (`OPTIONS`) before every `POST`/`PUT`/`PATCH`/`DELETE`. Two things have to both be true for that to work:

1. CORS middleware must run **before** `UseHttpsRedirection()` in `Program.cs` — ASP.NET Core's CORS middleware answers preflight requests directly and never passes them further down the pipeline, so as long as it runs first, a preflight can never be redirected. (Browsers refuse to follow redirects for preflight requests — if `UseHttpsRedirection()` runs first and issues a 307, every cross-origin write request breaks with "Redirect is not allowed for a preflight request.")
2. `UseHttpsRedirection()` is only registered outside `Development` — the SPA dev server talks to the API over plain HTTP, and forcing HTTPS here would just reintroduce the same class of problem (plus require trusting a local dev cert for no real benefit).

In Development, CORS allows any `http://localhost:<port>` origin (covers Vite's automatic port-hopping when 5173 is taken). In other environments, it reads an explicit allow-list from `Cors:AllowedOrigins` in configuration (empty/deny by default — set this before deploying anywhere).

### Configuration & secrets

Base settings live in `appsettings.json` / `appsettings.Development.json` — connection string, JWT signing key, and **empty placeholders** for every optional integration. Never put real secrets in these files (they're committed). For local development, use .NET user secrets instead:

```bash
cd src/AIRecruiter.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "a-long-random-string-at-least-32-characters"
dotnet user-secrets set "Cloudinary:CloudName" "your-cloud-name"
dotnet user-secrets set "Cloudinary:ApiKey" "your-api-key"
dotnet user-secrets set "Cloudinary:ApiSecret" "your-api-secret"
dotnet user-secrets set "Adzuna:AppId" "your-app-id"
dotnet user-secrets set "Adzuna:AppKey" "your-app-key"
dotnet user-secrets set "Nominatim:ContactEmail" "you@example.com"
```

In production, set the equivalent environment variables (`Jwt__Secret`, `Cloudinary__ApiKey`, etc. — double underscore is the .NET convention for nested config keys).

## Frontend

Requires Node.js.

```bash
cd frontend
cp .env.example .env.local   # optional — .env.development already ships with the right value
npm install
npm run dev
```

Runs at `http://localhost:5173` (or the next free port — the backend's dev CORS policy allows any `localhost` origin, see above). `VITE_API_BASE_URL` (in `.env.development`, or your own `.env.local`) must point at whatever port the API's **"http"** launch profile is actually listening on — `http://localhost:5087/api` by default; see `.env.example` for the documented value.

The frontend never holds or sends any third-party API key — every external integration (resume storage, external job search, location autocomplete) is proxied through the backend.

## Features

### India-only locations
Every location in the app — job postings, candidate profiles, companies — is modeled as structured `State` / `City` / `Locality?` / `IsRemote`, `Country` fixed to `"India"`. There is no external geocoding call involved: `src/AIRecruiter.Infrastructure/Data/indian-locations.json` is a checked-in dataset (18 states/UTs, covering Bengaluru, Hyderabad, Pune, Mumbai, Delhi NCR, Chennai, Ahmedabad, Kochi, Kolkata, Jaipur, Indore, Coimbatore and more), embedded as a resource and loaded once by `IndianLocationCatalog`. `GET /api/locations/india` serves the catalog to the frontend's state → city selector (`IndiaLocationSelector`). A server-side `IndiaLocationValidator` rejects any city/state pair not in the catalog on every write path (job posting, candidate profile, company onboarding) — the UI can't be bypassed via a raw API call. Job search supports state, city, remote, job type, experience, skills, and date-posted facets, all computed client-side over real seeded data.

### Application workflow, notifications, and interview scheduling
- Application statuses: Applied → Screening → Shortlisted → Interview Scheduled → Interview Completed → Offer → Hired, plus Rejected/Withdrawn at any point. Every transition is recorded in `ApplicationStatusHistory` (who, when, optional candidate-visible note) and shown as a timeline on the application detail page. Recruiters can only update applications to jobs they own.
- In-app notifications (bell icon with unread badge) fire on application submission and every status change — no email involved.
- Interview scheduling: a recruiter proposes one or more time slots; the candidate accepts one or declines with an optional note. Times are stored in UTC and always displayed in IST (`toIST` helper, fixed +5:30 offset). A scheduled interview can be downloaded as a `.ics` file, hand-built server-side (`IcsCalendarBuilder`) — no calendar API or paid service involved.
- A Kanban applicant board (`/jobs/:id/board`) gives recruiters a New → Screening → Shortlisted → Interview → Offer → Rejected pipeline per job. Each card has an accessible "Move to…" dropdown as the primary control; native HTML5 drag-and-drop is layered on as a mouse-only convenience, never the only way to move a card.

### Saved jobs and job alerts (candidate)
- Candidates save/unsave any open job from a job card or the job-detail page; the DB has a unique `(CandidateProfileId, JobPostingId)` index and the service layer is idempotent on top of it, so a duplicate save is a no-op rather than an error. A dedicated **Saved Jobs** page (`/saved-jobs`) lists every saved job with the date it was saved and a one-click remove action, with an empty state pointing back to the job search.
- Job alerts (`/alerts`) match on skill/keyword, India city/state, remote preference, job type, and minimum experience. Alerts can be created, **edited**, **activated/deactivated** (paused alerts stop counting toward matches without being deleted), and deleted — all ownership-checked server-side. Matches are computed live on every read (no background job, no email/SMS — everything stays in-app); the candidate dashboard surfaces a "New matches for your alerts" section pulling from every active alert.

### Recruiter job lifecycle and drafts
- Jobs move through `Draft → Open (Published) → Closed ⇄ Open → Archived`, enforced server-side by an explicit transition table (`JobPostingService.AllowedTransitions`) — e.g. an archived job can never be reopened, and publishing (Draft→Open or Closed→Open) always re-validates the India location fields even if they were left incomplete while in Draft. Only `Open` jobs are ever returned by public job search or candidate recommendations; `Closed`/`Archived`/`Draft` jobs reject new applications with a `JOB_CLOSED` conflict.
- Recruiters can save an incomplete job as a draft (skips location validation until publish time), edit any non-archived job's content, and duplicate any job into a fresh draft copy (new title suffixed "(Copy)", zeroed view/application counts).
- **Manage Jobs** (`/jobs/mine`) replaces the old flat job list with tabs for All/Draft/Published/Closed/Archived (with live counts), per-job application count, view count, conversion rate, created/published dates, and a status badge. Closing or archiving a job asks for confirmation first since both are consequential (archiving is effectively permanent — a job can never leave that state).

### Recruiter candidate search and CSV export
- `/recruiter/candidates` searches every candidate who has applied to **any job owned by the recruiter's company** (not just the caller's own postings, so colleagues can find each other's applicants) — company id is always derived server-side from the caller's own `RecruiterProfile`, never accepted from the client. Filters: skills, India city/state, experience range, education (substring), application status, and match-score range; sort by newest application, highest match score, most experience, or name.
- Clicking a result opens a candidate detail view: profile summary, skills, every application to the recruiter's company (with per-application interview status where scheduled), a resume-download link gated to applications the caller can actually manage, and an inline application-status control.
- **CSV export** streams only non-sensitive, already-authorized fields (name, headline, skills, city/state, experience, education, status, applied date, match score) for the current filter set, hand-rolled with proper RFC-4180 quoting (no external CSV library needed) — resume content, password hashes, and tokens are never included, and the export is scoped to the caller's own company exactly like search. Every export is written to the audit log (`CandidatesExported`, with just a row count as metadata).

### Job view tracking and recruiter insights
- Every `GET /api/jobs/{id}` on a **published** job counts a view, unless the viewer is the job's own owning recruiter (previewing your own listing doesn't inflate its stats). Duplicate views from the same visitor within a 30-minute window don't count twice — visitors are identified by `"u:{userId}"` when authenticated, or a SHA-256 hash of IP + User-Agent when anonymous (the raw IP/user-agent is never stored, only kept in an in-process dictionary for the current process's lifetime — no database table, no cross-restart persistence, nothing resembling a personal-data record).
- `/recruiter/analytics` and Manage Jobs both surface views, applications, and view→application conversion rate per job, with an explicit "No data yet" state for jobs that haven't been viewed — using the same dependency-free `.bar-chart`/table pattern as the rest of the app, no charting library.

### Company profiles and moderation
- Every company has a public profile page (`/companies/:id`) with logo (or an initials placeholder when none is set), industry, size, location, description, benefits, culture highlights, and social links, plus its open roles. Recruiters manage their own company's profile from onboarding; the same fields power both.
- An `Admin` role (seeded, not self-registerable) can view/manage users, companies, and jobs, and moderate jobs (Approved/Hidden/Removed) at `/admin`. Any signed-in user can report a job from its detail page; admins resolve reports as Reviewed or Dismissed.
- An append-only audit trail (`AuditLogEntry`) logs registration, company create/update, job create/status-change/duplicate, applications and status updates, interview actions, candidate-search exports, and moderation actions — actor, action, entity, timestamp, and a small non-sensitive metadata blob only. **Passwords, password hashes, JWTs, tokens, and resume content are never logged.** Recruiters see an activity feed scoped to their own company on their dashboard; admins see the full log under the Audit Log tab.

### Core (always available, zero cost)
- Registration/login (Candidate, Recruiter, or seeded Admin) with JWT auth, BCrypt password hashing. Registration never auto-logs the user in — it shows a success message and sends them to Login with their email prefilled (never the password); login looks the user up by normalized (trimmed, lowercased) email, and every failure path — unknown email, wrong password, or a deactivated account — returns the identical generic "Invalid email or password" so a caller can never tell which case occurred.
- Role-based dashboards at `/candidate/dashboard`, `/recruiter/dashboard`, and `/admin` — login redirects each role to its own landing page, a generic `/dashboard` redirects by JWT-derived role, and visiting the wrong role's pages bounces back to your own. The candidate dashboard shows profile completion, application-status summary, recommended jobs (scored against your skills), skill gap suggestions, saved jobs, active alert count, and upcoming interviews — all real data; "recently viewed" jobs has no tracking feature behind it, so that one section is explicitly labeled **Sample data**. The recruiter dashboard shows onboarding status, active job count, total applicants, a per-job performance table, upcoming interviews, and a recent-activity feed — all real, backed by a genuine page-view counter incremented on each job-detail fetch.
- Recruiter & company onboarding — required before a recruiter can post a job; the UI blocks posting and links to onboarding until it's complete
- Job posting: create, browse, search, view detail, and "my jobs" for recruiters, all India-location-aware (see above)
- Candidate profile: headline, summary, education, experience summary, skills, India location
- Resume upload (PDF/DOCX, 5 MB max) — validated by extension, declared MIME type, and file signature; stored outside the web root under generated filenames; downloads only via authorized backend endpoints, never a direct file URL
- Apply to a job (once per job, enforced by a unique index), application tracking dashboard, and a recruiter-side applicant list/board — every access path checks role + ownership
- **Explainable, fully local resume matching**: a deterministic, rule-based scoring engine (weighted skill coverage + TF-IDF/cosine text similarity + experience/education signals) computed at apply time from locally-extracted resume text (PdfPig for PDF, Open XML SDK for DOCX). No external AI API, no network call, no cost. Visible only to the candidate who owns the application and the recruiter who owns the job. Labeled in the UI as "AI-assisted / explainable matching" with a persistent disclaimer that it's decision support only — never an automated hiring/rejection decision, and candidates are never ranked or sorted by score anywhere in the UI.

### Optional (disabled by default, enable via server-side config only)
- **Resume storage on Cloudinary** instead of local disk — set `Cloudinary:CloudName/ApiKey/ApiSecret`; falls back to local disk storage automatically when unset. Uses authenticated/private delivery with signed URLs generated server-side; the browser never sees Cloudinary URLs or credentials.
- **External job search (Adzuna)** — set `Adzuna:AppId/AppKey`. Results are cached 24h, rate-limit (429/Retry-After) and timeout handled gracefully, single attempt only (no polling). Every result is labeled "External listing — sourced from Adzuna", links out to the original posting, and cannot be applied to inside this app. The nav link and search page only appear once `/api/external-jobs/availability` reports the feature is configured.
- **Location autocomplete (OpenStreetMap Nominatim)** — no API key needed, just a contact email for the identifying `User-Agent` (`Nominatim:ContactEmail`), per Nominatim's usage policy. All calls are server-proxied, rate-limited to at most 1 request/second process-wide, cached for 1 hour, and only fire after 3+ typed characters with a 400ms debounce. If Nominatim is unreachable, the field silently falls back to plain-text entry — nothing breaks. OpenStreetMap attribution is shown next to the field. **Not used by any form since the India-only location model was introduced** — every location field now uses the checked-in India dataset instead (see above). The endpoint and `LocationAutocomplete` component are left in place as harmless, unused code rather than removed.

## Privacy & security notes

- Passwords are hashed with BCrypt; JWTs carry the user id and role and are the only source of identity used server-side (the frontend never sends a user id — every endpoint derives "who is calling" from the token).
- Resumes are stored with generated (non-guessable) filenames outside any web-servable directory; every download is authorized per-request (candidate owner, or a recruiter whose job the candidate applied to).
- The matching engine only ever processes resume text already uploaded by the candidate for their own applications — nothing is sent to a third party.
- `.gitignore` excludes `bin/`, `obj/`, `frontend/node_modules`, and the local resume storage folder (`src/AIRecruiter.API/App_Data/`). No `.env` files or secrets are part of this repo — see the user-secrets instructions above.

## Database migrations

Five migrations exist: `InitialCreate` (core schema), `AddOnboardingProfilesApplicationsResumeMatching` (candidate profile enrichment, resume metadata, application matching fields), `AddJobPostingViewCount` (recruiter dashboard's real view counts), `AddIndiaPlatformUpgrade` (India-only City/State/Locality/JobType/ModerationStatus on jobs and candidates, expanded Company fields, application status history, notifications, interviews + slots, saved jobs, job alerts, job reports, and the audit log), and `AddJobLifecycleAlertsAndCandidateSearch` (`JobPostings.PublishedAt`, `JobAlerts.IsActive` — the candidate-search and CSV-export feature needed no schema changes since it only reads existing `JobApplication`/`CandidateProfile` data, and view-count de-duplication is deliberately in-memory rather than a new table). Apply with:

```bash
dotnet ef database update --project src/AIRecruiter.Infrastructure --startup-project src/AIRecruiter.API
```

If you already had a database from before this upgrade, its seed data predates the new India schema and the seeder's idempotency check will skip reseeding. Drop and recreate it once:

```bash
dotnet ef database drop --project src/AIRecruiter.Infrastructure --startup-project src/AIRecruiter.API --force
dotnet ef database update --project src/AIRecruiter.Infrastructure --startup-project src/AIRecruiter.API
```

## Demo data & login credentials

On every startup **in Development only**, the API seeds a small, fixed set of demo accounts, companies, jobs, applications, interviews, saved jobs/alerts, and a job report if they aren't already present (it checks for one known seed user and skips entirely if found — safe to restart the API repeatedly, nothing is duplicated). This never runs outside `Development` (`DataSeeder.SeedAsync` is only called when `app.Environment.IsDevelopment()`), so it's a no-op in any real deployment.

All seed accounts use the password **`Demo@123`** — obviously not a production password, and every seed email uses the clearly-fake `@demo.airecruiter.dev` domain so they're easy to spot and never collide with a real signup:

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
| Candidate | `candidate8@demo.airecruiter.dev` | Jaipur — deliberately sparse profile, shows the profile-completion card working on a low score |

Known limitation: seeded candidates have no resume file on disk (that would require real files through `IResumeStorage`), so their seeded applications have no match score. Upload a real resume through the UI on any account to see the actual local matching engine run.
