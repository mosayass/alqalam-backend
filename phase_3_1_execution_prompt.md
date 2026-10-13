# Execution Prompt — Phase 3.1: Users Auth Slice (Register, Confirm Email, Login)

You are the **implementer** for this task. Architecture decisions below were agreed with the project owner and the architect; treat them as constraints. If something is technically impossible or unsafe, **stop and report the conflict** with the smallest proposed change — do not silently redesign.

## 0. Read first (mandatory)
1. `AGENTS.md`
2. `docs/PROJECT_CONTEXT.md`
3. `docs/ARCHITECTURE.md` (note §5 rich domain model, §6 login decision)
4. `docs/IMPLEMENTATION_PLAN.md` (Phase 3.1)
5. `docs/USER_STORIES.md` (Stories 1.1, 1.2)
6. Existing code: `src/Shared/Results/*`, `src/Host/Errors/*`, `src/Host/Program.cs`, `src/Modules/Users/**`, `tests/AlQalam.Tests/**`, `Directory.Packages.props`.

## 1. Scope
**In scope:** register, confirm email, login (access + refresh token issuance).
**Out of scope (do NOT implement):** refresh endpoint, logout, `/me`, preferences, resend-confirmation, password reset, social login, roles, Memorization/Dataset changes.

## 2. Agreed architecture decisions

### 2.1 Account / Profile split (key decision)
The "user" is split into two responsibilities sharing the **same Guid**:

| Concept | Project | Table | Contents |
|---|---|---|---|
| `AppUser : IdentityUser<Guid>` | `Users.Infrastructure` | `users.users` | Credentials/security **only**: email, password hash (nullable for future social login), email confirmed, security stamp, lockout. **No business fields.** |
| `UserProfile` (rich entity) | `Users.Core` | `users.user_profiles` | Learner data: `UserId` (PK = account Id), `FullName`, `MemorizationLevel?`, `CreatedAtUtc`. Private setters, static factory `Create(...)` enforcing invariants (FullName required, trimmed, max length). |

- `Users.Core` must **never** reference EF Core, Npgsql, ASP.NET Core Identity types (`IdentityUser`, `UserManager`, `IdentityResult`), or ASP.NET HTTP types.
- Infrastructure only **translates** (calls `UserManager`, maps `IdentityResult` → Core `Error`). No business decisions in Infrastructure.
- No EF navigation between `AppUser` and `UserProfile`; a relational FK `user_profiles.user_id → users.id` (same module/schema) is fine.

### 2.2 Core contents (`Users.Core`)
- `MemorizationLevel` enum: `Beginner`, `Intermediate`, `Advanced`.
- `UserProfile` rich entity (above).
- `RefreshToken` rich entity, private setters:
  - Fields: `Id`, `UserId`, `TokenHash` (SHA-256, never plaintext), `FamilyId` (populate now; rotation logic is Phase 3.2), `DeviceId?`, `CreatedAtUtc`, `ExpiresAtUtc`, `RevokedAtUtc?`.
  - Methods: static `Create(...)`, `Revoke(utcNow)` (idempotent), `IsActive(utcNow)`.
- Abstractions (focused, no generic repository):
  - `IIdentityService` — create account, generate/verify email-confirmation token, check credentials. Accepts/returns primitives, Core DTOs, `Result`/`Result<T>` only.
  - `IUserProfileRepository`, `IRefreshTokenRepository`.
  - `ITokenService` — issue access JWT and generate refresh-token plaintext + hash (implementation in Infrastructure). Keep issuance separate from credential checking (`IssueTokens(userId, deviceId)`) so future social login reuses it.
  - `IConfirmationEmailSender` — sends the confirmation link/token.
- Commands + handlers (direct DI, **no MediatR**): `RegisterUserCommand`/handler, `ConfirmEmailCommand`/handler, `LoginCommand`/handler.
- DTOs: `UserSummaryDto` (Id, FullName, Email, MemorizationLevel, EmailConfirmed), `AuthTokensDto` (AccessToken, AccessTokenExpiresAtUtc, RefreshToken, RefreshTokenExpiresAtUtc).
- Use `TimeProvider` for time; pass `CancellationToken` everywhere.

### 2.3 Infrastructure contents (`Users.Infrastructure`)
- `AppUser`; `UsersDbContext` changed to inherit **`IdentityUserContext<AppUser, Guid>`** (no role tables). Keep default schema `users` and the existing migration-history-table setup.
- Entity configurations for `UserProfile`, `RefreshToken` (unique index on `TokenHash`, index on `UserId`), Identity tables; follow the naming conventions already used in the repo.
- `AddIdentityCore<AppUser>()` (no cookie auth), `RequireConfirmedEmail = true`, unique email, `UserName = Email`, password policy per Story 1.1 (min 8, mixed classes), lockout enabled for failed attempts.
- Implementations of all Core abstractions. Dev `IConfirmationEmailSender` implementation **logs** the confirmation link (no SMTP provider decided yet — record as open question in docs).
- Initial EF migration for the `users` schema.

### 2.4 Flows
- **Register** → `POST /api/v1/users/register` → `201 Created` with `UserSummaryDto`.
  - Validate input; `UserProfile.Create(...)`; create account via `IIdentityService`.
  - **Account + profile must be persisted atomically** (single transaction; no orphan rows on failure). Prove with an integration test.
  - After commit, generate confirmation token and send via `IConfirmationEmailSender`. Token encoded Base64Url.
  - Duplicate email → `409 Conflict` (per Story 1.1). Identity validation failures → `400` ProblemDetails.
- **Confirm email** → `POST /api/v1/users/confirm-email` body `{ userId, token }` → `204`. Invalid/expired token or unknown user → same generic `400`. Already confirmed → `204` (idempotent).
- **Login** → `POST /api/v1/users/login` body `{ email, password, deviceId? }` → `200` with `AuthTokensDto`.
  - Unknown email, wrong password, locked out, **unconfirmed email** → identical generic `401` ProblemDetails (ARCHITECTURE.md §6). Increment failed-access count on wrong password; reset on success.
  - On success: issue JWT (~15 min), create refresh token (random ≥32 bytes, Base64Url plaintext returned once, SHA-256 hash stored, ~30 days), new `FamilyId`.

### 2.5 Host
- Endpoints in `Host/Endpoints/Users/`, route group `/api/v1/users`, thin handlers, map `Result` via existing `ResultExtensions`, OpenAPI metadata.
- JWT bearer authentication configured in Host. Strongly typed `JwtOptions` (Issuer, Audience, SigningKey, AccessTokenMinutes, RefreshTokenDays) bound from config, **validated on startup**; no fallback/default signing key in source; add placeholders to `.env.example` only.
- Built-in ASP.NET Core rate limiting on register, confirm-email, login.

### 2.6 Packages
Add via `Directory.Packages.props` (central management), stable versions compatible with .NET 10 / existing 10.0.x packages — verify on official sources:
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (Users.Infrastructure)
- `Microsoft.AspNetCore.Authentication.JwtBearer` (Host)
- JWT creation library (e.g. `Microsoft.IdentityModel.JsonWebTokens`) in Users.Infrastructure
No MediatR, no AutoMapper, no generic repository.

## 3. Tests (follow existing test setup; real PostgreSQL for relational behavior, no EF InMemory)
- Unit: `UserProfile.Create` invariants; `RefreshToken` `Revoke`/`IsActive`; handlers with fakes.
- Integration: register success (201, both rows exist); duplicate email (409); weak password (400); atomicity (failure → no orphan profile); confirm valid/invalid/idempotent; login before confirmation → 401; wrong password → 401 identical body to unknown email; login success returns tokens and stores only the hash; lockout after repeated failures; schema/table placement in `users`.

## 4. Documentation updates
- `ARCHITECTURE.md`: record the Account/Profile split (§2.1 above) and that `AppUser` is an Infrastructure persistence model containing no business fields.
- `IMPLEMENTATION_PLAN.md` Phase 3.1: update entity list (`UserProfile` in Core; `AppUser` credentials-only; `RefreshToken` with `FamilyId`), mark items done as completed.
- `ARCHITECTURE.md` §12: add open questions — email provider; resend-confirmation flow; confirmation-token lifetime; 409-on-duplicate-email vs. enumeration trade-off.
- `DEVELOPMENT.md`: new config keys and how to read the logged confirmation link locally.

## 5. Finish
- Run `dotnet restore`, `dotnet build`, `dotnet test`; all must pass with zero errors. Report exact commands and results honestly.
- Commit locally with a clear message. Push only if safe; never force-push.
- **Stop after Phase 3.1.** Report: changed files, decisions made, deviations (if any) with reasons, open questions. Do not start Phase 3.2.
