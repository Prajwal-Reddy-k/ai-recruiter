# Security and Authentication

## Table of Contents
- [Registration Validation](#registration-validation)
- [Password Hashing and Login Verification](#password-hashing-and-login-verification)
- [JWT Generation, Claims, and Validation](#jwt-generation-claims-and-validation)
- [Password Reset Flow](#password-reset-flow)
- [Frontend Token Handling](#frontend-token-handling)
- [Role-Based Authorization](#role-based-authorization)
- [Ownership Validation](#ownership-validation)
- [CORS Configuration](#cors-configuration)
- [Input Validation and Error Handling](#input-validation-and-error-handling)
- [Resume / File Upload Controls](#resume--file-upload-controls)
- [View-Tracking Privacy Design](#view-tracking-privacy-design)
- [Secret and Configuration Handling](#secret-and-configuration-handling)
- [CSV Export Privacy Protections](#csv-export-privacy-protections)
- [Audit Logging](#audit-logging)
- [Known Limitations and Production Hardening](#known-limitations-and-production-hardening)

## Registration Validation

`RegisterRequest` (`src/AIRecruiter.Application/DTOs/Auth/RegisterRequest.cs`) uses standard data-annotation validation, enforced automatically by ASP.NET Core model binding before the controller action even runs:

- `FullName`: required, 2–200 characters.
- `Email`: required, must be a valid email format, max 256 characters.
- `Password`: required, minimum 6 characters, max 200.
- `Role`: required, must be a valid `UserRole` value.

On top of attribute validation, `AuthService.RegisterAsync` enforces two business rules in code:
- **Registering as `Admin` is rejected** with a `ValidationException` — Admin accounts can only be created by the seed data, never through the public registration endpoint.
- **Email uniqueness is checked case-insensitively** — the email is trimmed and lowercased before comparison and storage, so `Jordan@Example.com` and `jordan@example.com` are treated as the same account (`ConflictException` on collision).

## Password Hashing and Login Verification

- Passwords are hashed with **BCrypt** (`BCrypt.Net-Next`) — a salted, adaptive hash designed to be slow to brute-force. The plaintext password is never stored or logged anywhere.
- `AuthService.LoginAsync` looks the user up by normalized email, then calls `BCrypt.Verify(password, user.PasswordHash)`.
- **Every failure path returns the identical generic message**: `"Invalid email or password."` — this applies to an unknown email, a wrong password, *and* a deactivated (`IsActive=false`) account. A caller can never distinguish which case occurred from the response alone, which prevents user enumeration via the login endpoint.

## JWT Generation, Claims, and Validation

`TokenService.GenerateToken` (`src/AIRecruiter.Infrastructure/Services/TokenService.cs`) issues an HMAC-SHA256-signed JWT containing:

| Claim | Value |
|---|---|
| `sub` | the user's numeric id |
| `email` | the user's email |
| `name` (`ClaimTypes.Name`) | the user's full name |
| `role` (`ClaimTypes.Role`) | the user's role as a string (`"Candidate"`/`"Recruiter"`/`"Admin"`) |
| `securityStamp` | a per-user GUID, rotated on password reset — see [Password Reset Flow](#password-reset-flow) below |

Expiration is configurable (`Jwt:ExpiryMinutes`, defaults to 120 minutes in `appsettings.json`). Validation (`Program.cs`) checks issuer, audience, lifetime, and the signing key — all four (`ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey`) are explicitly enabled, none left at framework defaults. A custom `OnTokenValidated` event additionally re-checks the `securityStamp` claim against the current database value on every request.

**On the server side, the JWT is the only source of identity.** `ClaimsPrincipalExtensions.GetUserId()`/`GetRole()` read directly from `HttpContext.User` claims — no controller or service ever accepts a client-supplied user id or company id as a trusted input; every "whose data is this" decision is derived from the token.

## Password Reset Flow

Three endpoints implement a three-step forgot-password experience — `POST /api/auth/forgot-password`, `POST /api/auth/verify-reset-code`, `POST /api/auth/reset-password` (full request/response detail: [API_REFERENCE.md](API_REFERENCE.md)).

**Step 1 — request a code.** The email is normalized and looked up, but the response is **always** the identical generic message (`"If an account exists for this email, a verification code has been sent."`) whether or not the account exists — an unknown email creates no database row and sends no email, so the endpoint cannot be used to enumerate registered accounts. When the email *is* known, a cryptographically random 6-digit code (`RandomNumberGenerator`, not `System.Random`) is generated, hashed with BCrypt (the same algorithm used for passwords — a 6-digit space is small enough that an adaptive hash matters here, unlike the high-entropy reset token below), and stored with a 10-minute UTC expiry. Every previous unused code for that user is invalidated first, so only the newest code is ever valid. A resend cooldown (60 seconds) and a daily cap (5 codes/24h) are both enforced silently — neither produces a different response, for the same enumeration-safety reason.

**Step 2 — verify the code.** The submitted code is compared against the stored hash. Up to 5 wrong attempts are allowed per code (`FailedAttempts`, incremented on every miss); the 6th attempt — even with the *correct* code — is rejected, because the code row becomes permanently unusable once its failure count reaches the limit. Every failure path (unknown email, no code on file, expired code, locked-out code, wrong code) returns the identical generic message `"Invalid or expired code."`, again to avoid distinguishing account existence from a simple typo. On success, the code is immediately marked used — it can never be verified a second time, even to obtain a second reset token — and a separate, high-entropy (32 random bytes), SHA-256-hashed reset token is issued with its own 10-minute expiry. **This token is not a JWT and cannot be used to authenticate as the user for anything other than the next step.**

**Step 3 — reset the password.** The reset token is hashed and looked up; it must be unexpired and not already used to reset a password. `NewPassword` and `ConfirmPassword` must match, and the same minimum-length rule used at registration applies. On success: the password is re-hashed with BCrypt, the reset token is marked used (permanently — see below), and **`User.SecurityStamp` is rotated to a fresh GUID**. Because every JWT embeds the security stamp that was current *at the moment it was issued*, and the custom `OnTokenValidated` handler in `Program.cs` compares that embedded value against the current database value on every authenticated request, rotating the stamp immediately invalidates every JWT issued before the reset — the closest equivalent to session revocation this stateless-JWT design supports, without needing a token blocklist or distributed cache.

**Anti-abuse**: both `forgot-password` and `verify-reset-code` are additionally rate-limited by IP address (`InMemoryIpRateLimiter`, the same in-memory sliding-window pattern used elsewhere in this codebase — see [SYSTEM_DESIGN.md](SYSTEM_DESIGN.md)) — 10 forgot-password requests/hour and 20 verify attempts/hour per IP, returning `429 RATE_LIMITED` with a `Retry-After` header when exceeded. Rate limiting is intentionally IP-scoped rather than email-scoped for this layer, so it can't itself be used to infer which emails are registered.

**Audit trail**: `PasswordResetRequested`, `PasswordResetVerificationSucceeded`, `PasswordResetVerificationFailed` (with only a non-sensitive reason code as metadata, e.g. `"WrongCode"`), and `PasswordResetCompleted` are all logged via `IAuditLogService` — never the raw code, the raw token, or the new password.

## Frontend Token Handling

- On successful login/register, the frontend stores the JWT and a small `{userId, fullName, email, role}` object in `localStorage` (`AuthContext.setSession`).
- A shared axios instance (`api/client.ts`) has a request interceptor that reads `localStorage.getItem("token")` and sets `Authorization: Bearer <token>` on every outgoing request — individual API modules never handle the header themselves.
- `logout()` clears both `localStorage` entries and the in-memory auth state.
- **Note**: storing a JWT in `localStorage` (rather than an `HttpOnly` cookie) means it is readable by any JavaScript running on the page, which is the standard trade-off of this pattern — see [Known Limitations](#known-limitations-and-production-hardening).

## Role-Based Authorization

Every controller action that should be role-restricted carries `[Authorize(Roles = "...")]` (e.g. `[Authorize(Roles = "Recruiter")]`, `[Authorize(Roles = "Admin")]`). `AdminController` applies the attribute at the **controller** level so every action requires Admin without needing to repeat it per method. ASP.NET Core's authorization middleware rejects the request with `401` (no/invalid token) or `403` (valid token, wrong role) before the controller method body executes — the method itself never has to re-check the role.

On the frontend, `<ProtectedRoute allowedRoles={[...]}>` mirrors this: it redirects to `/login` if unauthenticated, or to the caller's own dashboard if authenticated but the role doesn't match — this is a UX convenience, **not** a security boundary; the actual enforcement is always server-side.

## Ownership Validation

Role checks alone aren't sufficient — a Recruiter must only manage *their own* jobs/applicants, not every recruiter's. Every service that returns or mutates a specific resource re-derives the caller's identity from the JWT and compares it against the loaded entity, for example (`JobPostingService.UpdateStatusAsync`):

```csharp
if (job.RecruiterProfile.UserId != recruiterUserId)
{
    throw new ForbiddenException("You do not have access to this job posting.");
}
```

This pattern is repeated consistently across `JobPostingService`, `JobApplicationService`, `InterviewService`, `SavedJobService`, `JobAlertService`, and `CandidateProfileService`. List-returning methods (e.g. `GetMyJobsAsync`) apply the same check as a `Where` filter instead of a post-fetch comparison, so a recruiter's job list query is scoped at the database level, not filtered in memory after the fact.

**`CandidateSearchService` is a deliberate, documented exception**: it scopes to the caller's entire **company** (`RecruiterProfile.CompanyId`, itself derived server-side) rather than the caller's own postings only — this lets colleagues at the same company discover each other's applicants, which is the intended behavior for a company-wide candidate search feature. Per-application *write* actions (status changes) still separately enforce the stricter per-recruiter ownership check on the underlying `/api/applications/{id}/status` endpoint; the candidate-detail view exposes a server-computed `canManage` flag so the UI only offers a status-change control when the caller is actually allowed to use it.

## CORS Configuration

See [SYSTEM_DESIGN.md § CORS and Local HTTP/HTTPS Configuration](SYSTEM_DESIGN.md#cors-and-local-httphttps-configuration) for the full explanation. In short:

- CORS middleware runs **before** `UseHttpsRedirection()` so preflight `OPTIONS` requests are answered directly and never redirected (browsers refuse to follow redirects for preflights).
- In Development, any `http://localhost:<port>` origin is allowed (accommodates Vite's automatic port-hopping). In any other environment, an explicit allow-list (`Cors:AllowedOrigins`) is required — empty/deny by default, so a production deployment must be configured before it will accept any browser traffic.
- HTTPS redirection itself is skipped entirely in Development (the API runs plain HTTP locally on purpose) and only enabled outside Development.

## Input Validation and Error Handling

- DTO-level validation uses standard `System.ComponentModel.DataAnnotations` attributes (`[Required]`, `[EmailAddress]`, `[StringLength]`, `[EnumDataType]`) enforced automatically by model binding.
- Business-rule validation (India location pairs, resume file signatures, job-status transitions, duplicate-prevention) lives in dedicated, framework-free validator classes (`IndiaLocationValidator`, `ResumeFileValidator`) or directly in services, always throwing a typed `AppException` subclass.
- **`ExceptionHandlingMiddleware` is the single place** that converts any thrown exception into an HTTP response — a known `AppException` maps to its declared status code and a `ProblemDetails` body with a stable `errorCode`; any *unexpected* exception is logged server-side with full detail but returns only a generic `500 INTERNAL_ERROR` to the client, so internal error details (stack traces, exception messages, connection strings) are never leaked to a caller.

## Resume / File Upload Controls

`ResumeFileValidator` (framework-free, fully unit-tested) runs before anything is persisted:

1. **Extension check** — only `.pdf` and `.docx` are accepted.
2. **Declared MIME type check** — must be exactly `application/pdf` or `application/vnd.openxmlformats-officedocument.wordprocessingml.document`.
3. **File-signature (magic bytes) check** — the actual file content must start with the PDF signature (`%PDF-`) or the ZIP signature (`PK\x03\x04`, since DOCX is a ZIP container) — this catches a file renamed to `.pdf` that isn't actually a PDF.
4. **Size limit** — configurable, defaults to 5 MB (`ResumeStorage:MaxSizeBytes`); the controller additionally caps the raw multipart request at 6 MB (`[RequestSizeLimit]`).

Text is extracted from the validated file **before** it's persisted (a file that fails parsing never becomes the candidate's stored resume). Storage uses generated, non-guessable keys — `IResumeStorage` (local disk by default, or Cloudinary if configured) — outside any web-servable directory. **Every download goes through an authorized backend endpoint** (`GET /api/candidates/me/resume/download`, `GET /api/applications/{id}/resume`, `GET /api/recruiters/candidates/applications/{id}/resume`) that re-checks ownership before streaming the file; there is no direct, guessable, or public file URL anywhere in the system.

## View-Tracking Privacy Design

Job-view counting (`InMemoryViewDeduplicationService`, see [SYSTEM_DESIGN.md](SYSTEM_DESIGN.md)) was designed to avoid persisting any personal data:

- The visitor identity used for de-duplication is **never the raw IP address or user-agent string**. For an authenticated viewer it's `"u:{userId}"`; for an anonymous viewer it's a SHA-256 hash of `IP + User-Agent`, computed once in the controller and never logged or stored in that raw form.
- The de-duplication window (30 minutes) is held in a `ConcurrentDictionary` **in process memory only** — there is no database table, so nothing survives an application restart and nothing is queryable as a per-visitor history.
- The owning recruiter's own visits to their own job listing are explicitly excluded from the count (comparing the JWT-derived caller id against the job's owner), so recruiters previewing their own postings can't accidentally inflate their own analytics.

## Secret and Configuration Handling

- `appsettings.json` / `appsettings.Development.json` (both committed to the repository) contain **only placeholder values** — an explicit placeholder string for the JWT secret (`"REPLACE_WITH_A_LONG_RANDOM_SECRET_KEY_AT_LEAST_32_CHARS"`), and empty strings for every optional integration's credentials (`Cloudinary:*`, `Adzuna:*`). No real secret is ever committed.
- Local development secrets are meant to be supplied via **.NET user secrets** (`dotnet user-secrets set "Jwt:Secret" "..."`), which are stored outside the repository on the developer's machine.
- In any real deployment, the equivalent environment variables (`Jwt__Secret`, `Cloudinary__ApiKey`, etc. — double underscore is the .NET convention for nested configuration keys) should be set via the hosting platform's secret manager, never committed.
- The frontend **never holds or sends any third-party API key** — every external integration (resume storage, external job search, location autocomplete) is proxied through the backend; the browser only ever talks to the AI Recruiter API itself.
- `.gitignore` excludes `bin/`, `obj/`, `frontend/node_modules/`, and the local resume storage folder (`src/AIRecruiter.API/App_Data/`).

## CSV Export Privacy Protections

The recruiter candidate-export endpoint (`GET /api/recruiters/candidates/export`) is scoped by the same company-ownership rule as candidate search (never a client-supplied company id), and the CSV column set is a fixed allow-list: `Name, Headline, Skills, City, State, ExperienceYears, Education, ApplicationStatus, AppliedDate, MatchScore, JobTitle`. It **never** includes resume file content, password hashes, tokens, or any candidate's data outside the caller's own company. The CSV is hand-built with proper RFC-4180-style quoting/escaping (fields containing a comma, quote, or newline are quoted and internal quotes doubled) rather than relying on an unvetted external library. Every export call is written to the audit log with only a row count as metadata — never the exported data itself.

## Audit Logging

`IAuditLogService.LogAsync(actorUserId, actorRole, actionType, entityType, entityId, metadata)` is called from every state-changing service (registration, company create/update, job create/edit/status-change/duplicate, applications and status changes, interview proposals/responses, candidate-search exports, admin moderation actions). The interface's own doc comment states the rule enforced by convention at every call site: **metadata must always be a small, explicit anonymous object — never a raw entity, and never a password, password hash, JWT, token, or resume content.** For example, a job status change logs `new { job.Title, Status = job.Status.ToString() }`, not the `JobPosting` entity itself. Recruiters can see an activity feed scoped to their own company; Admins can see the full platform-wide log.

## Known Limitations and Production Hardening

| Limitation | Production recommendation |
|---|---|
| JWT stored in `localStorage`, readable by any script on the page (XSS exposure) | Consider an `HttpOnly`, `Secure`, `SameSite` cookie for the token in a production deployment, with CSRF protection added accordingly |
| No refresh-token flow — the JWT simply expires after `Jwt:ExpiryMinutes` and the user must log in again | Add a refresh-token endpoint if a longer session without re-authentication is required |
| Password reset is implemented (see [Password Reset Flow](#password-reset-flow)); **email address verification at registration is not** — a user can register with an email they don't control | Add an email-verification step, reusing the same `IEmailSender`/code infrastructure |
| No rate limiting on `/api/auth/login` or `/api/auth/register` (only the password-reset endpoints have rate limiting) | Extend `IIpRateLimiter` (or add ASP.NET Core's built-in rate-limiting middleware) to login/register as well, to slow down credential-stuffing/enumeration attempts |
| No account lockout after repeated failed logins | Add a lockout policy (e.g. exponential backoff or a temporary lock) tied to `IsActive` or a new failure-count field |
| `InMemoryIpRateLimiter` is process-local, like the view-dedup cache | Won't correctly rate-limit across multiple load-balanced instances — move to a Redis-backed implementation behind the same `IIpRateLimiter` interface before scaling horizontally |
| SMTP send failures for password-reset emails are only logged, never retried | Acceptable for a zero-budget project (the response contract must not change based on delivery success anyway — see above); a production system might add a retry queue |
| Audit log and view-dedup cache have no automated retention/rotation | Add a retention policy once real data-volume and compliance requirements are known |
| CORS allow-list for non-Development environments must be configured before deployment (empty by default) | Set `Cors:AllowedOrigins` explicitly for the real frontend origin(s) |
| No centralized secrets manager wired up | Use the hosting platform's secret manager (Azure Key Vault, AWS Secrets Manager, etc.) instead of environment variables alone for anything beyond local development |
