# API Reference

All routes are relative to the API base URL (`http://localhost:5087/api` in local development — see [SETUP_AND_DEMO.md](SETUP_AND_DEMO.md)). Enum-typed fields in this document are shown by name; the actual JSON wire values are their **integer** backing values (see each enum's table) unless otherwise noted.

## Table of Contents
- [Conventions](#conventions)
- [Swagger / OpenAPI](#swagger--openapi)
- [Authentication](#authentication)
- [Candidate Profile](#candidate-profile)
- [Saved Jobs & Job Alerts](#saved-jobs--job-alerts)
- [Recruiter / Company Onboarding](#recruiter--company-onboarding)
- [Jobs & Job Lifecycle](#jobs--job-lifecycle)
- [Applications & Application Status](#applications--application-status)
- [Applicant Management (Recruiter Candidate Search)](#applicant-management-recruiter-candidate-search)
- [Interview Scheduling](#interview-scheduling)
- [Notifications](#notifications)
- [Dashboard & Analytics](#dashboard--analytics)
- [Companies (Public)](#companies-public)
- [Locations](#locations)
- [Admin / Moderation](#admin--moderation)
- [External Jobs (Optional Integration)](#external-jobs-optional-integration)
- [Enum Reference](#enum-reference)
- [Common Error Shape](#common-error-shape)

## Conventions

- **Auth**: "Yes" means the endpoint requires `Authorize: Bearer <jwt>`. "No" means it is publicly accessible (still works with a token if one is sent, but doesn't require it).
- **Role**: the exact string required in the JWT's role claim, or "Any authenticated" / "Public".
- **Ownership**: how the endpoint verifies the caller can act on the target resource, beyond the role check.
- All examples use placeholder values only (`<jwt-token>`, `demo@example.com`, etc.) — never real credentials.

## Swagger / OpenAPI

Swagger is enabled **only in the Development environment** (`Program.cs`: `if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }`). With the API running locally:

- Swagger UI: `http://localhost:5087/swagger`
- OpenAPI JSON: `http://localhost:5087/swagger/v1/swagger.json`

Swagger is configured with a Bearer security scheme — click **Authorize**, paste `Bearer <your-jwt>` (obtained from `POST /api/auth/login`), and every subsequent "Try it out" call in the UI carries the token automatically.

---

## Authentication

### `POST /api/auth/register`
Register a new Candidate or Recruiter account. Registering as `Admin` is rejected — Admin accounts are seed-only.

| | |
|---|---|
| Auth | No |
| Role | Public |
| Ownership | N/A |

**Request body**
```json
{
  "fullName": "Jordan Candidate",
  "email": "jordan@example.com",
  "password": "Passw0rd!",
  "role": 1
}
```
`role`: `1` = Candidate, `2` = Recruiter, `3` = Admin (rejected here).

**Success response** `200 OK`
```json
{
  "userId": 42,
  "fullName": "Jordan Candidate",
  "email": "jordan@example.com",
  "role": "Candidate",
  "token": "<jwt-token>",
  "expiresAt": "2026-08-30T10:00:00Z"
}
```
Note: the frontend does **not** auto-login after registration — it shows a success message and redirects to `/login` with the email prefilled.

**Errors**: `400` if `role` is `Admin` or field validation fails; `409` (`ConflictException`) if the (normalized, lowercased) email already exists.

```bash
curl -X POST http://localhost:5087/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Jordan Candidate","email":"jordan@example.com","password":"Passw0rd!","role":1}'
```

### `POST /api/auth/login`

| | |
|---|---|
| Auth | No |
| Role | Public |
| Ownership | N/A |

**Request body**
```json
{ "email": "jordan@example.com", "password": "Passw0rd!" }
```

**Success response** `200 OK` — same `AuthResponse` shape as register.

**Errors**: `401` for unknown email, wrong password, **or** a deactivated account — all three return the identical generic message `"Invalid email or password."` so a caller can never distinguish which case occurred.

```bash
curl -X POST http://localhost:5087/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"jordan@example.com","password":"Passw0rd!"}'
```

### `POST /api/auth/forgot-password`
Step 1 of password reset. Full behavior, anti-abuse, and security rationale: [SECURITY_AND_AUTH.md § Password Reset Flow](SECURITY_AND_AUTH.md#password-reset-flow).

| Auth | Role |
|---|---|
| No | Public |

**Request body**: `{ "email": "jordan@example.com" }`

**Success response** `200 OK` (always this shape, whether or not the email is registered):
```json
{ "message": "If an account exists for this email, a verification code has been sent." }
```

**Errors**: `429 RATE_LIMITED` (with `Retry-After` header) if too many requests have come from this IP recently.

```bash
curl -X POST http://localhost:5087/api/auth/forgot-password \
  -H "Content-Type: application/json" -d '{"email":"jordan@example.com"}'
```

### `POST /api/auth/verify-reset-code`
Step 2. Exchanges a verification code for a short-lived reset token.

| Auth | Role |
|---|---|
| No | Public |

**Request body**: `{ "email": "jordan@example.com", "code": "123456" }`

**Success response** `200 OK`:
```json
{ "resetToken": "<opaque-base64-token>", "expiresAtUtc": "2026-08-29T15:10:00Z" }
```

**Errors**: `400 VALIDATION_ERROR` with the message `"Invalid or expired code."` for every failure case (unknown email, no code on file, expired code, already-used code, 5+ failed attempts, or a simply wrong code) — deliberately identical across all of them. `429 RATE_LIMITED` if too many attempts have come from this IP.

```bash
curl -X POST http://localhost:5087/api/auth/verify-reset-code \
  -H "Content-Type: application/json" -d '{"email":"jordan@example.com","code":"123456"}'
```

### `POST /api/auth/reset-password`
Step 3. Sets a new password using the reset token from step 2 — never the verification code directly.

| Auth | Role |
|---|---|
| No (but requires a valid, unexpired reset token in the body) | Public |

**Request body**
```json
{ "resetToken": "<token from verify-reset-code>", "newPassword": "NewPassw0rd!", "confirmPassword": "NewPassw0rd!" }
```

**Success response** `200 OK`:
```json
{ "message": "Password updated successfully. Please sign in with your new password." }
```

**Errors**: `400 VALIDATION_ERROR` — `"Passwords do not match."` if `newPassword` != `confirmPassword`; `"This reset link has expired or is invalid. Please request a new code."` for an unknown, expired, or already-used token.

**Side effect**: rotates the user's security stamp, which invalidates every JWT issued before this call (see [SECURITY_AND_AUTH.md](SECURITY_AND_AUTH.md#jwt-generation-claims-and-validation)).

```bash
curl -X POST http://localhost:5087/api/auth/reset-password \
  -H "Content-Type: application/json" \
  -d '{"resetToken":"<token>","newPassword":"NewPassw0rd!","confirmPassword":"NewPassw0rd!"}'
```

---

## Candidate Profile

Base route: `/api/candidates`. All actions require `Authorize(Roles = "Candidate")`.

### `GET /api/candidates/me`
Returns the caller's own candidate profile (auto-created empty on first access).

| Auth | Role | Ownership |
|---|---|---|
| Yes | Candidate | Implicit — always "me" (`User.GetUserId()`), never a path parameter |

**Success response** `200 OK`
```json
{
  "id": 7, "fullName": "Jordan Candidate", "headline": "Backend Engineer",
  "summary": null, "education": null, "experienceSummary": null,
  "totalExperienceYears": 4, "city": "Bengaluru", "state": "Karnataka", "locality": null,
  "displayLocation": "Bengaluru, Karnataka, India",
  "currentSalary": null, "expectedSalary": null, "skillsCsv": "C#, SQL Server",
  "resumeOriginalFileName": null, "resumeSizeBytes": null, "resumeUploadedAt": null
}
```

### `PUT /api/candidates/me`
Upserts the profile. Location fields are validated against the India dataset unless both `city` and `state` are blank (treated as "not set yet").

**Request body**
```json
{
  "headline": "Backend Engineer", "summary": "5 years in .NET", "education": "B.Tech CS",
  "experienceSummary": "...", "totalExperienceYears": 4,
  "city": "Bengaluru", "state": "Karnataka", "locality": null,
  "currentSalary": 1200000, "expectedSalary": 1600000, "skillsCsv": "C#, SQL Server"
}
```
**Errors**: `400` if `city`/`state` don't form a valid India pair.

### `POST /api/candidates/me/resume`
Multipart upload (`file` field), max 6 MB at the request level (5 MB enforced by `ResumeFileValidator`).

**Errors**: `400` if the extension/declared MIME type/file signature aren't PDF or DOCX, or the file can't be parsed.

```bash
curl -X POST http://localhost:5087/api/candidates/me/resume \
  -H "Authorization: Bearer <jwt-token>" \
  -F "file=@resume.pdf"
```

### `GET /api/candidates/me/resume/download`
Streams the caller's own resume file. `404` if none uploaded.

---

## Saved Jobs & Job Alerts

Base route: `/api/candidates`, `Authorize(Roles = "Candidate")`.

### `GET /api/candidates/me/saved-jobs`
Returns every saved job **with the save date**: `[{ "job": { ...JobPostingDto }, "savedAt": "2026-08-29T04:20:00Z" }]`.

### `POST /api/candidates/me/saved-jobs/{jobId}`
Idempotent — saving an already-saved job is a no-op (`204 No Content`), enforced both in the service and by a unique DB index on `(CandidateProfileId, JobPostingId)`.

### `DELETE /api/candidates/me/saved-jobs/{jobId}`
`204 No Content` whether or not it was saved.

### `GET /api/candidates/me/alerts`
Each alert includes a live-computed `matchingJobCount` and up to 3 sample `matchingJobs` (empty/zero when the alert is paused).

**Success response** `200 OK`
```json
[{
  "id": 3, "skillsCsv": "React", "state": null, "city": null, "isRemote": null,
  "jobType": null, "minExperienceYears": null, "isActive": true,
  "matchingJobCount": 2, "matchingJobs": [ { "...": "JobPostingDto" } ],
  "createdAt": "2026-08-29T09:00:00Z"
}]
```

### `GET /api/candidates/me/alerts/matches`
Up to 10 open jobs matching **any** of the caller's active alerts (used on the candidate dashboard).

### `POST /api/candidates/me/alerts`
**Request body**
```json
{ "skillsCsv": "React", "state": null, "city": null, "isRemote": null, "jobType": null, "minExperienceYears": null, "isActive": true }
```
`jobType` (if set): `1`=FullTime, `2`=PartTime, `3`=Contract, `4`=Internship, `5`=Freelance.

### `PUT /api/candidates/me/alerts/{alertId}`
Same body shape as create — full replace, ownership-checked (`403` if the alert belongs to another candidate).

### `PATCH /api/candidates/me/alerts/{alertId}/active`
**Request body**: `{ "isActive": false }` — pauses an alert without deleting it.

### `DELETE /api/candidates/me/alerts/{alertId}`
`204 No Content`. `404` if unknown, `403` if not the owner.

---

## Recruiter / Company Onboarding

Base route: `/api/recruiters`, `Authorize(Roles = "Recruiter")`.

### `GET /api/recruiters/me/onboarding-status`
```json
{
  "isOnboarded": true, "companyId": 1, "companyName": "Nimbus Cloud Systems",
  "designation": "Talent Lead", "website": "https://...", "industry": "Cloud Software",
  "description": "...", "logoUrl": null, "city": "Bengaluru", "state": "Karnataka",
  "size": "201-500", "benefits": "...", "cultureHighlights": "...",
  "linkedInUrl": null, "twitterUrl": null
}
```
If not yet onboarded, every field except `isOnboarded: false` is `null`.

### `POST /api/recruiters/me/onboarding`
Upserts the recruiter's company. First call creates the `Company` + `RecruiterProfile`; subsequent calls update in place. Required before posting any job — `JobPostingService.CreateAsync` throws `409 NOT_ONBOARDED` otherwise.

```bash
curl -X POST http://localhost:5087/api/recruiters/me/onboarding \
  -H "Authorization: Bearer <jwt-token>" -H "Content-Type: application/json" \
  -d '{"companyName":"Acme Corp","website":"https://acme.example","industry":"Software","description":"...","designation":"Recruiter","city":"Bengaluru","state":"Karnataka"}'
```

### `GET /api/recruiters/me/activity`
Returns the audit-log entries for every recruiter at the caller's own company (recent activity feed). Empty array if not onboarded.

---

## Jobs & Job Lifecycle

Base route: `/api/jobs`.

### `GET /api/jobs`
Public job search — only `Status=Open` **and** `ModerationStatus=Approved` jobs are returned.

| Auth | Role |
|---|---|
| No | Public |

**Query params**: `search` (optional, matches title/skills/city/state substring).

```bash
curl "http://localhost:5087/api/jobs?search=backend"
```

### `GET /api/jobs/mine`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Recruiter | Scoped to `RecruiterProfile.UserId == caller` |

Returns **every** status (Draft/Open/Closed/Archived), each wrapped with an application count:
```json
[{ "job": { "...": "JobPostingDto (includes viewCount, publishedAt)" }, "applicationCount": 3 }]
```

### `GET /api/jobs/{id}`

| Auth | Role |
|---|---|
| No | Public |

Increments the job's view count if `Status == Open`, the caller isn't the owning recruiter, and the visitor hasn't been seen for this job in the last 30 minutes (see [SECURITY_AND_AUTH.md](SECURITY_AND_AUTH.md)). `404` if the job doesn't exist.

### `POST /api/jobs`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Recruiter | Job is created under the caller's own `RecruiterProfile`/`CompanyId` |

**Request body**
```json
{
  "title": "Senior Backend Engineer", "description": "...", "requiredSkillsCsv": "C#, SQL Server",
  "minExperienceYears": 3, "maxExperienceYears": 6, "minSalary": 1500000, "maxSalary": 2200000,
  "city": "Bengaluru", "state": "Karnataka", "locality": null, "isRemote": false,
  "jobType": 1, "saveAsDraft": false
}
```
`saveAsDraft: true` → `Status=Draft`, location validation is skipped (a draft can be incomplete). `saveAsDraft: false` (default) → `Status=Open`, `PublishedAt` set to now, full India-location validation is enforced.

**Errors**: `409 NOT_ONBOARDED` if the recruiter hasn't completed onboarding; `400` if publishing without a valid India location.

### `PUT /api/jobs/{id}`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Recruiter | `job.RecruiterProfile.UserId == caller` |

Edits an existing job's content (same body shape as create, minus `saveAsDraft`). `409 JOB_ARCHIVED` if the job is Archived (terminal — can no longer be edited).

### `PATCH /api/jobs/{id}/status`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Recruiter | `job.RecruiterProfile.UserId == caller` |

**Request body**: `{ "status": 2 }` (`1`=Draft, `2`=Open, `3`=Closed, `4`=Archived). Used for publish (`Draft→Open`), close (`Open→Closed`), reopen (`Closed→Open`), and archive (`→Archived`).

**Errors**: `409 INVALID_TRANSITION` if the transition isn't in the allowed table (e.g. `Archived→Open`); `400` if publishing without a complete India location.

```bash
curl -X PATCH http://localhost:5087/api/jobs/5/status \
  -H "Authorization: Bearer <jwt-token>" -H "Content-Type: application/json" \
  -d '{"status":3}'
```

### `POST /api/jobs/{id}/duplicate`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Recruiter | `job.RecruiterProfile.UserId == caller` |

Creates a new `Draft` copy of the job (title suffixed `" (Copy)"`, view/application counts reset). Returns the new `JobPostingDto` with `201 Created`.

### `POST /api/jobs/{id}/report`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Any authenticated | N/A — anyone can report any job |

**Request body**: `{ "reason": "Looks like a scam listing" }`. `204 No Content`. Feeds the Admin Reports tab.

---

## Applications & Application Status

Routes are split across `/api/jobs/{jobId}/apply`, `/api/applications/*`.

### `POST /api/jobs/{jobId}/apply`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Candidate | N/A (any candidate may apply once) |

**Request body**: `{ "coverNote": "Excited to apply!" }` (optional, may be `null`/omitted).

**Errors**: `409 JOB_CLOSED` if the job isn't `Open` (covers Draft, Closed, and Archived); `409 ALREADY_APPLIED` if the candidate already applied to this job (checked both pre-insert and via a unique `(JobPostingId, CandidateProfileId)` index as a race-condition backstop).

```bash
curl -X POST http://localhost:5087/api/jobs/5/apply \
  -H "Authorization: Bearer <jwt-token>" -H "Content-Type: application/json" \
  -d '{"coverNote":"Excited to apply!"}'
```

### `GET /api/applications/me`

| Auth | Role |
|---|---|
| Yes | Candidate |

Returns all of the caller's own applications.

### `GET /api/applications/{id}`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Candidate or Recruiter | Owning candidate, or the recruiter who owns the job |

Returns the full detail DTO including match score breakdown and the complete status-change history.

### `GET /api/applications/{id}/resume`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Candidate or Recruiter | Same as above |

Streams the applicant's resume file.

### `GET /api/jobs/{jobId}/applications`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Recruiter | `job.RecruiterProfile.UserId == caller` |

All applications for one job (used by the flat list and Kanban board views).

### `PATCH /api/applications/{id}/status`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Recruiter | `application.JobPosting.RecruiterProfile.UserId == caller` |

**Request body**: `{ "status": 3, "note": "Great interview, moving forward" }` (`note` optional, shown to the candidate).

Status values: `1`=Applied, `2`=Screening, `3`=Shortlisted, `4`=InterviewScheduled, `5`=InterviewCompleted, `6`=Offer, `7`=Hired, `8`=Rejected, `9`=Withdrawn.

Every change appends an `ApplicationStatusHistory` row and notifies the candidate.

### `POST /api/applications/{id}/withdraw`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Candidate | `application.CandidateProfile.UserId == caller` |

Sets status to `Withdrawn`, notifies the owning recruiter.

---

## Applicant Management (Recruiter Candidate Search)

Base route: `/api/recruiters/candidates`, `Authorize(Roles = "Recruiter")`. Scoped to **every job owned by the caller's company** (derived server-side from the caller's own `RecruiterProfile.CompanyId` — never a client-supplied value).

### `GET /api/recruiters/candidates`
**Query params**: `skills`, `city`, `state`, `minExperienceYears`, `maxExperienceYears`, `education`, `status` (ApplicationStatus int), `minMatchScore`, `maxMatchScore`, `sort` (`1`=NewestApplication, `2`=HighestMatchScore, `3`=ExperienceDesc, `4`=NameAlphabetical).

**Success response** — one row per application:
```json
[{
  "applicationId": 14, "candidateProfileId": 6, "fullName": "Divya Krishnan",
  "headline": "Full Stack Developer", "skillsCsv": "C#, React, SQL Server",
  "city": "Chennai", "state": "Tamil Nadu", "totalExperienceYears": 4,
  "education": "B.E. Computer Science, Anna University", "applicationStatus": "Withdrawn",
  "jobId": 1, "jobTitle": "Senior Backend Engineer", "appliedAt": "2026-08-29T04:24:56Z",
  "matchScore": null, "canManage": true
}]
```
`canManage` is server-computed (`job.RecruiterProfile.UserId == caller`) — the frontend uses it to decide whether to show status-change controls for an application owned by a colleague at the same company.

```bash
curl "http://localhost:5087/api/recruiters/candidates?skills=React&sort=2" \
  -H "Authorization: Bearer <jwt-token>"
```

### `GET /api/recruiters/candidates/{candidateProfileId}`
Candidate profile summary + every application to the caller's company + per-application interview status. `404` if the candidate never applied to the caller's company (indistinguishable from "candidate doesn't exist" by design — no cross-company enumeration).

### `GET /api/recruiters/candidates/export`
Same query params as search. Returns `text/csv` (`Content-Disposition: attachment; filename=candidates-<timestamp>.csv`), columns: `Name, Headline, Skills, City, State, ExperienceYears, Education, ApplicationStatus, AppliedDate, MatchScore, JobTitle`. Never includes resume content, password data, or tokens. Every export call is written to the audit log.

```bash
curl "http://localhost:5087/api/recruiters/candidates/export" \
  -H "Authorization: Bearer <jwt-token>" -o candidates.csv
```

### `GET /api/recruiters/candidates/applications/{applicationId}/resume`
Resume download scoped to the **caller's company** (`application.JobPosting.CompanyId == callerCompanyId`), distinct from the per-recruiter-only `/api/applications/{id}/resume` endpoint above — this one intentionally allows any recruiter at the company to view a colleague's applicant's resume.

---

## Interview Scheduling

An interview proposes a single start/end time (not a list of candidate-selectable slots). Recruiters propose, reschedule, cancel, or complete; candidates accept or decline.

### `POST /api/applications/{applicationId}/interviews`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Recruiter | `application.JobPosting.RecruiterProfile.UserId == caller` |

**Request body**
```json
{ "startUtc": "2026-09-05T09:30:00Z", "endUtc": "2026-09-05T10:00:00Z", "type": 1, "location": "https://meet.example.com/...", "recruiterNote": "Bring a laptop" }
```
`type`: `1`=Online, `2`=Phone, `3`=InPerson. **Errors**: `400` if the start time is in the past, the end isn't after the start, or the candidate/recruiter already has an overlapping interview.

### `GET /api/applications/{applicationId}/interviews`

| Auth | Ownership |
|---|---|
| Yes | Owning candidate or owning recruiter |

### `PUT /api/interviews/{interviewId}/reschedule`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Recruiter | Owning recruiter |

Same body shape as propose (all fields optional except a valid start/end pair). Resets `Status` to `Proposed` — the candidate must reconfirm.

### `POST /api/interviews/{interviewId}/cancel`
### `POST /api/interviews/{interviewId}/complete`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Recruiter | Owning recruiter |

`complete` returns `409 INVALID_INTERVIEW_STATE` unless the interview is currently `Scheduled`.

### `POST /api/interviews/{interviewId}/accept`
### `POST /api/interviews/{interviewId}/decline`

| Auth | Role | Ownership |
|---|---|---|
| Yes | Candidate | `interview.JobApplication.CandidateProfile.UserId == caller` |

**Request body**: `{ "responseNote": "Looking forward to it" }` (optional). Accept sets `Status=Scheduled`; decline sets `Status=Declined`.

### `GET /api/interviews/mine?status={status}`

| Auth | Role |
|---|---|
| Yes | Candidate or Recruiter |

Candidates see their own interviews; recruiters see every interview across their **company's** job postings (not just their own). Optional `status` query filters to one of `Proposed`/`Scheduled`/`Completed`/`Cancelled`/`Declined`.

### `GET /api/interviews/upcoming`

| Auth | Role |
|---|---|
| Yes | Candidate or Recruiter |

Returns each caller's own upcoming (future, `Scheduled`) interviews, sorted by start time — used by the dashboard widgets.

### `GET /api/interviews/{interviewId}/calendar.ics`
Streams a hand-built `.ics` file (`text/calendar`) for the interview, authorized to its candidate or owning recruiter only.

---

## Notifications

Base route: `/api/notifications`, `Authorize` (any authenticated role).

| Endpoint | Purpose |
|---|---|
| `GET /api/notifications` | All of the caller's notifications |
| `GET /api/notifications/unread-count` | `{ "count": 3 }` |
| `POST /api/notifications/{id}/read` | Marks one notification read, `204 No Content` |

---

## Dashboard & Analytics

Base route: `/api/dashboard`, `Authorize`.

### `GET /api/dashboard/candidate`

| Role |
|---|
| Candidate |

Profile completion %, application-status summary, recent applications, recommended jobs (skill-matched), skill-gap suggestions, saved jobs, active alert count, live alert matches, and upcoming interviews — all computed from real data except `recentlyViewedJobs`, which is explicitly flagged `isSampleData: true` (no per-candidate view-history feature exists).

### `GET /api/dashboard/recruiter`

| Role |
|---|
| Recruiter |

Onboarding status, active job count, total applicants, recent applications, per-job performance (status/applicant count/view count), and upcoming interviews.

### `GET /api/dashboard/recruiter/analytics`

| Role |
|---|
| Recruiter |

```json
{
  "activeJobs": 2, "totalApplications": 5, "shortlistedCandidates": 1,
  "interviewsScheduled": 0, "offersMade": 0,
  "applicationsPerJob": [{ "name": "Senior Backend Engineer", "count": 2 }],
  "hiringFunnel": [{ "name": "Applied", "count": 2 }, "..."],
  "applicationsByCity": [{ "name": "Bengaluru", "count": 3 }],
  "topCandidateSkills": [{ "name": "C#", "count": 3 }],
  "viewsVsApplications": [{ "jobId": 1, "title": "...", "viewCount": 12, "applicationCount": 2 }]
}
```
All scoped to jobs owned by the caller (`RecruiterProfile.UserId == caller`).

---

## Companies (Public)

### `GET /api/companies/{id}`

| Auth | Role |
|---|---|
| No | Public |

Public company profile + its currently open, approved jobs. Used for the public company page.

---

## Locations

### `GET /api/locations/india`

| Auth | Role |
|---|---|
| No | Public |

Returns the full India location catalog (18 states/UTs with their cities) — fetched once by the frontend and cached client-side.

### `GET /api/locations/search`

| Auth | Role |
|---|---|
| No | Public |

**Query params**: `q` (free-text place query). Proxies OpenStreetMap Nominatim, rate-limited to ≤1 req/sec and cached 1 hour server-side. **Implemented but not currently used by any form** — superseded by the India-only structured selector; kept for reference/demonstration.

---

## Admin / Moderation

Base route: `/api/admin`, `Authorize(Roles = "Admin")` on the whole controller.

| Endpoint | Purpose |
|---|---|
| `GET /api/admin/users` | All users (id, name, email, role, active flag, created date) |
| `GET /api/admin/companies` | All companies with job/recruiter counts |
| `GET /api/admin/jobs` | All jobs with status, moderation status, application count |
| `GET /api/admin/reports` | All job reports |
| `POST /api/admin/jobs/{id}/moderate` | Body: `{ "moderationStatus": 2 }` (`1`=Approved, `2`=Hidden, `3`=Removed) |
| `POST /api/admin/reports/{id}/resolve` | Body: `{ "status": 2, "resolutionNote": "..." }` (`1`=Pending, `2`=Reviewed, `3`=Dismissed) |
| `GET /api/admin/audit-log` | Platform-wide audit log, most recent 500 entries |

Only `Hidden`/`Removed` jobs are excluded from `ModerationStatus == Approved`-filtered public search — moderation is independent of a job's `JobStatus` lifecycle state.

---

## External Jobs (Optional Integration)

Base route: `/api/external-jobs`, `Authorize` (any authenticated role). **Only meaningfully functional when `Adzuna:AppId`/`Adzuna:AppKey` are configured** — otherwise `DisabledExternalJobSearchService` backs these endpoints.

### `GET /api/external-jobs/availability`
`{ "isAvailable": false }` when Adzuna isn't configured — the frontend uses this to hide the nav link and page entirely.

### `GET /api/external-jobs/search`
**Query params**: `keywords`, `location`, `page`, `pageSize`. Results cached 24h server-side; every listing is labeled "External listing — sourced from Adzuna", links out to the original posting, and **cannot be applied to inside this app**.

**Errors**: `503 ExternalServiceUnavailableException` (with `Retry-After` header when Adzuna returns a 429) if the upstream call fails.

---

## Enum Reference

| Enum | Values (int = name) |
|---|---|
| `UserRole` | 1=Candidate, 2=Recruiter, 3=Admin |
| `JobStatus` | 1=Draft, 2=Open, 3=Closed, 4=Archived |
| `JobType` | 1=FullTime, 2=PartTime, 3=Contract, 4=Internship, 5=Freelance |
| `ModerationStatus` | 1=Approved, 2=Hidden, 3=Removed |
| `ApplicationStatus` | 1=Applied, 2=Screening, 3=Shortlisted, 4=InterviewScheduled, 5=InterviewCompleted, 6=Offer, 7=Hired, 8=Rejected, 9=Withdrawn |
| `InterviewStatus` | 1=Proposed, 2=Scheduled, 3=Completed, 4=Cancelled, 5=Declined |
| `InterviewType` | 1=Online, 2=Phone, 3=InPerson |
| `ReportStatus` | 1=Pending, 2=Reviewed, 3=Dismissed |

## Common Error Shape

Every non-2xx response (thrown from a service as an `AppException`) has this shape (`application/problem+json`):

```json
{
  "status": 409,
  "title": "JOB_CLOSED",
  "detail": "This job is no longer accepting applications.",
  "errorCode": "JOB_CLOSED"
}
```

| Status | Meaning | Example `errorCode` values |
|---|---|---|
| 400 | Validation failure | `VALIDATION_ERROR` |
| 401 | Not authenticated / bad credentials | `UNAUTHORIZED` |
| 403 | Authenticated but not authorized for this resource | `FORBIDDEN` |
| 404 | Resource not found | `NOT_FOUND` |
| 409 | Conflict with current state | `ALREADY_APPLIED`, `JOB_CLOSED`, `NOT_ONBOARDED`, `INVALID_TRANSITION`, `JOB_ARCHIVED` |
| 429 | Too many requests (rate limited) | `RATE_LIMITED` |
| 503 | Optional external integration unavailable | `SERVICE_UNAVAILABLE` |
| 500 | Unhandled exception (logged server-side, generic message returned) | `INTERNAL_ERROR` |
