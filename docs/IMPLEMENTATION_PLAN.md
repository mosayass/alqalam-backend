# AlQalam Backend — Implementation Plan

Work in these phases sequentially. Do not implement every phase in one giant change. Finish each phase with a reviewable commit and a concise report.

---

## Phase 1 — Repository bootstrap and scaffold [COMPLETED]
- [x] Initialized Git repository, established branch `main`, connected `origin` (`https://github.com/mosayass/alqalam-backend.git`).
- [x] Created `AGENTS.md`, architecture docs, solution, Host, Shared, and Core/Infrastructure projects for Users, Memorization, and Dataset.
- [x] Standardized on `AlQalam.slnx`, verified dependency boundaries, verified build, and confirmed `/health/live` probe works.
- [x] Added `.gitignore`, `.editorconfig`, and central package management (`Directory.Packages.props`).
- [x] Committed (`d7d2bc0`) and pushed to `origin/main`.

---

## Phase 2 — Foundation and local infrastructure [COMPLETED]
- [x] Verified .NET 10 and EF Core 10 with Npgsql provider (`10.0.3`) and official stable packages in CPM.
- [x] Added `Result<T>` and `Error` primitives to `AlQalam.Shared` and centralized ProblemDetails error handling in `AlQalam.Host`.
- [x] Added `UsersDbContext`, `MemorizationDbContext`, and `DatasetDbContext` in their respective Infrastructure projects with isolated schemas (`users`, `memorization`, `dataset`) and dedicated migration history tables.
- [x] Added local `compose.yaml` (PostgreSQL 17 + MinIO) with health checks, named volumes, and `.env.example`.
- [x] Added strongly typed `StorageOptions` in Host distinguishing server-to-storage internal endpoint from client-visible public endpoint for pre-signed URLs.
- [x] Added separate `/health/live` and `/health/ready` probes with sanitized dependency reporting.
- [x] Added multi-stage production `Dockerfile` and GitHub Actions CI workflow (`.github/workflows/ci.yml`).
- [x] Added test suite (`tests/AlQalam.Tests`) with 27 unit and integration tests; verified context schemas and health probes.
- [x] Created `docs/DEVELOPMENT.md` with operational and migration guides (no README created).
- [x] Committed (`6385935`, `0f11daa`) and pushed to `origin/main`.

---

## Requirements & Design Alignment [CURRENT]
- [x] Inspected Figma design system (`JHytc94KpPeVuluGD1iYvq`) via Figma API using personal access token.
- [x] Extracted user flows and forms across Login, Sign Up, Settings, Profile, Dashboard, Canvas, and Competition screens.
- [x] Established on-device vs. backend boundaries: the AI recognition model runs 100% on the frontend client app; backend provides identity, settings, practice session sync, and model manifest distribution.
- [x] Created `docs/USER_STORIES.md` mapping Figma screens to testable acceptance criteria across 5 epics.
- [x] Isolated Post-MVP features (challenges, weekly honor podium, leaderboards, and study circles) from the MVP scope.

---

## Phase 3 — Users and Memorization MVP [NEXT RECOMMENDED STEP]

### Phase 3.1 — Users Core Auth Slice: Register & Login Only [CURRENT FOCUS]
- Implement minimal entities/enums needed strictly for Register & Login:
  - `MemorizationLevel` domain enum (`Beginner`, `Intermediate`, `Advanced`) in `Users.Core`.
  - `AppUser` (`users.users`) extending `IdentityUser<Guid>` in `Users.Infrastructure` (nullable `PasswordHash` for future social login compatibility).
  - `RefreshToken` (`users.refresh_tokens`) rich entity with private setters and domain methods (`Revoke`, `Create`).
- Commands & Handlers (in `Users.Core`):
  - `RegisterUserCommand` & `RegisterUserHandler` (validates, delegates to `IIdentityService`, returns `Result<UserSummaryDto>`).
  - `LoginCommand` & `LoginHandler` (verifies credentials, issues JWT access token and SHA-256 hashed refresh token).
- Infrastructure & Persistence (in `Users.Infrastructure`):
  - `IIdentityService` implementation wrapping `UserManager<AppUser>`.
  - `UsersDbContext` entity configurations in `users` schema.
  - Initial EF Core migration for `users` schema (`__UsersMigrationsHistory`).
- Endpoints (in `Host/Endpoints/Users/`):
  - `POST /api/v1/users/register`
  - `POST /api/v1/users/login`
- Integration and unit tests for registration validation, duplicate email handling, password verification, and JWT/refresh token generation.

### Phase 3.2 — Users Session, Profile & Preferences Slice
- `POST /api/v1/users/refresh` (family-based refresh token rotation and reuse detection).
- `POST /api/v1/users/logout` (session revocation).
- `GET /api/v1/users/me` (profile query implemented in Infrastructure via direct EF projection).
- `UserPreferences` entity, `GET` and `PUT /api/v1/users/preferences`.

### Phase 3.3 — Memorization Practice Session Ingestion Slice
- Idempotent session ingestion (`POST /api/v1/memorization/sessions`).
- Enforce idempotency via database uniqueness on `(UserId, ClientSessionId)`.
- Rich domain entity `PracticeSession` and `SessionAttempt` with private setters.
- Store client event times (`ClientStartedAt`, `ClientCompletedAt`) separately from `ServerReceivedAt`.

### Phase 3.4 — Memorization Queries Slice
- `GET /api/v1/memorization/sessions` (paginated history query implemented directly in Infrastructure).
- `GET /api/v1/memorization/progress` (progress summary query).

### ⚠️ Reminders & Watch-outs for Phase 3:
- **Mobile-friendly Token Lifetimes & Scoping:** Use short access tokens (~10–15 mins) and long sliding refresh tokens (~30–90 days) stored SHA-256 hashed and device-scoped so mobile users don't face frequent re-logins after offline periods.
- **Decouple Issuance from Credentials:** Keep credential checking separate from token generation (`IssueTokens(user, device)`) so future Google/Apple flows plug seamlessly into the exact same issuance pipeline.
- **Schema Forward-Compatibility:** Ensure the user identity schema allows nullable password hashes to accommodate future external/social logins.
- **Undecided Mastery/Streak Formulas:** If mastery score or streak boundary/time-zone semantics remain undecided in `ARCHITECTURE.md`, return raw counts and baseline activity first; do not invent proprietary formulas.

---

## Phase 4 — Dataset ingestion and model manifest
- Implement explicit sample-collection opt-in/consent state and private S3-compatible upload lifecycle.
- Add sample request and upload-completion endpoints, short-lived unique-key signed uploads, object verification, validation statuses, idempotency, and bounded retry handling.
- Ensure MinIO signed URLs returned to clients use client-reachable hostname (`PublicEndpoint`).
- Add a configuration/file-backed read-only model manifest endpoint (`GET /api/v1/models/manifest`), immutable artifact references, hashes, sizes, and compatibility fields.
- Add tests for consent enforcement, authorization, duplicate requests, missing/corrupt objects, and manifest contracts.
- Document retention, deletion, operational assumptions, and current limitations.

---

## Phase 5 — Challenges, Leaderboards & Social Auth [POST-MVP]
- Grounded in Figma screens `الإنجاز والتنافس` (`id: 1:1175` / `1:1661`).
- Weekly honor podium ("منصة شرف الأسبوع الحالية"), user tiers (Gold / Platinum), and points leaderboard.
- Quran study circles ("الحلقات") and seasonal memorization competitions.

### ⚠️ Reminders & Decisions for Social Auth (Google / Apple Sign-in):
- **Token Exchange Pattern:** Flutter app acquires provider ID token from native SDKs and submits to `POST /api/v1/auth/external/{provider}`. Backend validates signature, issuer, and audience against platform-specific configuration before issuing standard app tokens.
- **Account Linking Policy:** Do not auto-link accounts purely on matching email strings (prevents account takeover attacks). Require explicit re-authentication or verified-email confirmation.
- **Apple Quirks:** Account for Apple Private Relay emails (`@privaterelay.appleid.com`) and the fact that user profile display names are only sent on the initial authorization.

---

## Exit criteria for every phase
- Build and tests pass with zero errors (run only when code actually changed; skipped for docs-only updates).
- No real secret is committed.
- Documentation and architecture docs remain aligned with implementation.
- Work is committed locally and pushed to the configured remote when safe.
- Report exact commands and outcomes. Never claim a command or push succeeded if it was not executed successfully.
