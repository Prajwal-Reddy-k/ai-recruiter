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
| Core features | India-only structured job locations · job draft/publish/close/archive lifecycle · applications with full status history · explainable local resume matching · Kanban applicant board · interview scheduling with `.ics` export · saved jobs & job alerts · company-scoped candidate search + CSV export · privacy-conscious job-view tracking · analytics · admin moderation · audit trail · secure forgot/reset password with email verification codes |
| Cost | Zero — every optional third-party integration (Cloudinary, Adzuna, SMTP) is behind an interface and only activates when credentials are configured; falls back gracefully otherwise |
| Tests | 141 passing (xUnit + EF Core InMemory + Moq) |

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
