# AlQalam Backend — Sequential Agent Prompts

Run these prompts **one at a time, in order, in the coding agent connected to the intended local workspace**. Review the diff and commit after each phase. Prompt 1 sets repository instructions that the agent should follow for later prompts.

---

## Prompt 1 — Bootstrap the empty repository, write project instructions, scaffold the solution, and connect Git

You are starting the AlQalam backend from an empty GitHub repository. The local workspace is expected to already contain the supplied starter-pack files: `AGENTS.md`, `AGENT_PROMPTS.md`, and `docs/PROJECT_CONTEXT.md`, `docs/ARCHITECTURE.md`, and `docs/IMPLEMENTATION_PLAN.md`. These files are intentional and must be preserved. Read them before changing anything; update them in place if needed rather than blindly overwriting them. The GitHub remote is empty, but the local workspace is not literally empty because it contains these instructions. Do not assume there is a README, existing solution, or existing application code.

### Repository and Git requirements — do these first
The target remote is exactly:
`https://github.com/mosayass/alqalam-backend.git`

1. Inspect the current working directory, files, `git status`, branch, and `git remote -v` before making changes.
2. If the current directory is already a Git repository linked to this target, use it. If it is not a Git repository and is clearly the intended empty project workspace, initialize Git in the current directory with `main` as the initial branch.
3. Add the target as `origin` if no remote exists. If `origin` points somewhere else, or this workspace contains an unrelated existing repository/files, **do not overwrite, reset, delete, or silently replace anything**. Stop before risky Git actions, explain the conflict, and preserve the workspace.
4. Check whether the target remote already has branch history before the initial push. The owner says it is a fresh/empty repository. If it is genuinely empty, make the initial commit and push `main` using `git push -u origin main`. If it has commits, do not force-push; preserve remote history and explain the safe next step.
5. If credentials/network prevent pushing, still make the local commit if safe, report the exact failure, and do not claim the push succeeded. Never run `git push --force`.

### Product context
AlQalam is a tablet-first and web Quran memorization/recitation-verification application. The client captures stylus handwriting and performs handwriting recognition using a PyTorch-trained ONNX Runtime INT8 model, local Quran retrieval against a canonical corpus of 6,236 Ayahs, normalization, and word/character alignment/diff. The backend is supporting infrastructure, not the normal OCR/alignment engine. It handles users/authentication, offline practice-session/progress sync, optional consent-based collection of difficult handwriting samples via pre-signed object-storage uploads, and small model manifests/metadata.

### Architecture decisions — treat these as constraints
- .NET 10 / ASP.NET Core Minimal APIs, C#; use stable compatible package versions.
- One modular-monolith Host, one Shared project, and three business modules: Users, Memorization, Dataset.
- Each business module gets exactly two production projects: `<Module>.Core` and `<Module>.Infrastructure`.
- **Every API endpoint mapping belongs in Host under `Host/Endpoints/`, grouped by module. Do not put endpoints in Infrastructure and do not create separate API projects per module.**
- Core owns domain/application logic and module contracts; it must not reference EF Core, Npgsql, S3 SDKs, ASP.NET HTTP types, Host, or another business module.
- Infrastructure owns EF Core, context/entity configuration, migrations, external integrations and Core abstraction implementations. It references only its own Core plus Shared and infrastructure packages.
- Host composes all modules and owns HTTP translation, routing, middleware, OpenAPI, global auth setup, and endpoint mapping.
- One PostgreSQL database initially, one EF Core DbContext per module, with separate schemas `users`, `memorization`, and `dataset`. Each context owns its migrations and uses a separate migration-history table in its own schema.
- Use Result/Error primitives only for expected failures; centralize unexpected exception handling. Use CQRS/handlers selectively. Do not automatically add MediatR without checking its current licensing and justifying it.
- No microservices, RabbitMQ/Kafka, event sourcing, generic repository framework, or outbox by default.
- Docker Compose will support local PostgreSQL and MinIO. Daily API development should run natively via the .NET CLI. Production has one multi-stage Host Dockerfile.
- Do not commit secrets or `.onnx` files. Do not create `README.md`; the owner explicitly said the GitHub repository starts empty, and setup notes should go in `docs/`.

### Work to perform in this phase
1. Create `AGENTS.md` at repository root with persistent coding-agent instructions containing the above rules, testing expectations, Git safety, dependency boundaries and a requirement to read the docs before changes.
2. Create:
   - `docs/PROJECT_CONTEXT.md`
   - `docs/ARCHITECTURE.md`
   - `docs/IMPLEMENTATION_PLAN.md`
   Populate them with the product and architecture details above, including explicit not-yet-decided product policies (mastery formula, streak semantics, exact dataset retention/consent policy, and eventual model rollout workflow). Do not invent those policies.
3. Create a solution and minimal buildable projects in a clear layout like:
   - `src/Host/AlQalam.Host.csproj` (ASP.NET Core Web project)
   - `src/Shared/AlQalam.Shared.csproj`
   - `src/Modules/Users/AlQalam.Users.Core.csproj`
   - `src/Modules/Users/AlQalam.Users.Infrastructure.csproj`
   - equivalent Core and Infrastructure projects for Memorization and Dataset
4. Apply project references according to the dependency rules. Host may reference each module's Core and Infrastructure; each Infrastructure references only its own Core and Shared; each Core references Shared; no module references another module.
5. Add sensible `.gitignore`, `.editorconfig`, nullable reference types, and central package version management if it keeps dependencies consistent. Add a stable .NET 10 `global.json` only if it will not make the repo unusable on other stable .NET 10 SDK patch versions.
6. Add only a tiny liveness endpoint such as `/health/live`, basic OpenAPI setup for development, and minimal DI extension-method placeholders so the solution builds. Do not begin auth, database entities, dataset upload, or session features yet; those are later phases.
7. Run `dotnet --info`, `dotnet restore`, `dotnet build`, and launch/smoke-test the Host if the environment allows. Use a currently installed stable .NET 10 SDK, not a preview SDK. If the SDK is missing, do not silently retarget; report the blocker.
8. Review the dependency graph and write the final file tree in your report. Commit with a message like `chore: bootstrap backend architecture`, then push to `origin/main` only if the repository/credentials checks above make it safe.

Keep this phase focused on repository setup, documented decisions, and a buildable skeleton. Do not implement future features early.

---

## Prompt 2 — Build the shared foundation and local development infrastructure

Read `AGENTS.md`, `docs/PROJECT_CONTEXT.md`, `docs/ARCHITECTURE.md`, and `docs/IMPLEMENTATION_PLAN.md` before changing code. Inspect the current Git state first. Work only on the foundation phase; do not implement full authentication, memorization features, dataset uploads, or a model release workflow yet.

Requirements:
1. Confirm .NET 10 and EF Core 10 are used with a stable Npgsql EF Core provider compatible with the selected EF Core version. Verify the current stable compatible versions using official docs/NuGet metadata. Prefer central package management if not already configured. Do not choose preview packages.
2. Add a small Shared `Result<T>`/`Error` representation if it is not already present. Keep it minimal and tested. Add centralized API error handling using ProblemDetails; expected application failures should map consistently, while unexpected exceptions are logged and returned without leaking internals.
3. Add `UsersDbContext`, `MemorizationDbContext`, and `DatasetDbContext` in their respective Infrastructure projects. Explicitly configure schemas `users`, `memorization`, and `dataset`. Configure a distinct migration-history table within each schema. Do not put entity types for multiple business modules in one context. Add correct registrations to each module's `Add...` extension method and compose them in Host.
4. Configure a shared PostgreSQL connection string through options/configuration. Do not put real credentials in source control. The app must start safely when optional external services are unavailable, but readiness should report database status accurately.
5. Add local `compose.yaml`/`docker-compose.yml` containing PostgreSQL and MinIO with pinned stable image tags, health checks, named volumes, and explicit development-only credentials supplied via environment variables or ignored `.env`. Commit only `.env.example` placeholders. Document startup/stop/reset commands in `docs/DEVELOPMENT.md` (do not create README.md).
6. Make MinIO configuration distinguish the endpoint used by the server/container to contact storage from the externally reachable endpoint embedded in browser/client-facing pre-signed URLs. This is essential because a Docker-only hostname is not reachable from a browser. Do not yet implement the upload workflow, but establish the right config shape.
7. Add liveness and readiness health checks; readiness must accurately reflect required dependencies and should not be the same as liveness. Avoid exposing sensitive health detail publicly.
8. Add a single multi-stage production `Dockerfile` for Host. Add a CI workflow that restores, builds, and tests on a stable .NET 10 SDK.
9. Add a test project or projects with unit-test support. Prefer PostgreSQL-backed tests (e.g. a disposable/test database or Testcontainers, after checking compatibility) for schema/migration/relational behavior. Do not use EF InMemory as evidence that PostgreSQL migrations or constraints work. Avoid creating empty migration files with no schema changes; prove each context can be created and document/run the proper future migration commands.
10. Keep production migrations as a controlled deployment task, not an automatic concurrent action by every Host instance.

Before finishing, run restore/build/test and the feasible compose/health checks. Fix issues rather than only listing them. Review package licenses and document any meaningful dependency choices. Update docs, commit with a focused message such as `chore: add persistence and local infrastructure`, and push without force if safe. Report exact command outcomes and any checks you could not run.

---

## Prompt 3 — Implement the Users and Memorization MVP vertical slices

Read all repository instructions and architecture documents first. Inspect Git status and the current code. Implement only the initial Users and Memorization behavior. Do not start dataset uploads or model-manifest release features yet.

### Users / identity
1. Implement a minimal first-party authentication path. Prefer ASP.NET Core Identity for password hashing/user lifecycle unless a reviewed identity provider is already configured. Do not write custom password hashing or token cryptography.
2. Keep Identity/EF implementation details in `Users.Infrastructure`. Do not expose `IdentityUser`/EF entities through Host contracts or Users.Core.
3. If using JWT access tokens, use configuration-bound signing/validation settings, short-lived access tokens, hashed refresh-token storage, refresh-token rotation, revocation, and tests. Store no plaintext refresh tokens. Make production startup fail clearly if required signing configuration is absent; do not include a production fallback secret.
4. Add only the necessary endpoints, for example register, login, refresh, logout/revoke, and authenticated `me/profile`. Use `/api/v1/users/...`. Add appropriate validation, rate limiting, generic login failure messages, ownership enforcement, and ProblemDetails mapping. Do not invent email verification/password reset flows unless they are needed and documented as out of scope.
5. Keep HTTP endpoints in `Host/Endpoints/Users/`; have them call application services or handlers, not `UserManager`/EF directly from endpoint lambdas.

### Memorization
6. Add the minimum domain/persistence model needed for idempotent practice-session ingestion and history. Store per-session client event time separately from server receipt time, authenticated user ownership, stable client-generated session/operation ID, and model/corpus/normalization version metadata.
7. A duplicate retry from the same authenticated user must not create a second session. Enforce this with a database uniqueness constraint and return a stable, documented outcome. Never accept the owner UserId from request data as authority.
8. Support a paginated session-history query and a useful initial progress summary. Preserve attempt/session history as historical evidence; treat derived progress summaries separately if needed. Avoid unbounded queries and add indexes aligned with actual access patterns.
9. If storing per-Ayah attempt summaries, use transparent counts/evidence. Do not invent the product's mastery-score formula, error taxonomy, or streak boundary/time-zone semantics; record unresolved policy questions in `docs/ARCHITECTURE.md` and choose only the minimal structure required to proceed.
10. Do not claim server-side Quran verification: correctness/alignment is client-computed in this product. Validate structural inputs (such as surah/ayah number ranges and payload bounds) without pretending to reproduce client ML logic.

### Quality bar
- Implement Core behavior and tests separately from EF/HTTP.
- Add PostgreSQL-backed integration tests for persistence constraints and idempotency where feasible, plus endpoint tests for auth/ownership/status mapping.
- Use cancellation tokens and async I/O.
- Update local configuration examples only with placeholders; never commit secrets.
- Run restore/build/test, check migrations, and perform an API smoke test if possible. Fix failures.
- Update `docs/ARCHITECTURE.md` and `docs/DEVELOPMENT.md` if behavior or commands changed. Commit with a focused message such as `feat: add authentication and memorization sync`; push without force if safe.
- Finish with a concise report of endpoints added, data model decisions, tests run, and open product questions. Do not proceed to Prompt 4 until this phase is reviewed.

---

## Prompt 4 — Implement consent-based dataset ingestion and the model manifest

Read `AGENTS.md` and all relevant docs first. Inspect Git status and the working implementation. Keep this phase focused on Dataset ingestion and the initial read-only model manifest. Do not add a fourth production module unless a concrete requirement forces it and you explain why.

### Dataset ingestion
1. Add an explicit opt-in/consent state with a version identifier and timestamps. Do not accept or store training samples by default, and do not invent legal consent wording. Put the consent contract/policy still needing owner/legal input in docs.
2. Implement authenticated endpoints under `/api/v1/dataset/...` to opt in/out as appropriate, request a sample upload, and confirm completion. Derive UserId from claims.
3. For each requested sample, create a server-generated sample ID and unique object key; verify active consent and eligibility; store metadata and lifecycle status in `DatasetDbContext`; then issue a short-lived pre-signed upload URL for a private S3-compatible bucket.
4. Use a maintained S3-compatible SDK, verify MinIO compatibility, and configure service/internal and browser/client-visible endpoints separately. Signed URLs returned to clients must use a hostname the client can actually reach. Avoid a proxy-through-API upload by default.
5. Treat pre-signed URLs as bearer capabilities. Never accept arbitrary client object keys. Bind permissions to one object. Require authenticated ownership for completion. Make request/completion endpoints idempotent.
6. On completion, verify the object actually exists and inspect actual stored size and available checksum/content metadata. Do not trust a client success message, file extension, MIME type, reported size, or checksum without verifying it. A sample is not training-eligible until validation succeeds.
7. Implement a small persisted-state validation/retry worker only if needed for reliable processing; do not add a broker. Fail safely and make transitions explicit. Add limits and retention configuration. If the selected presigned upload style cannot enforce a size limit before the object arrives, document the trade-off and implement compensating controls or select an S3-compatible constrained-upload strategy.
8. Capture only useful metadata, such as model/corpus/normalization versions, sample format, timestamps, integrity metadata and consent version. Document retention, deletion/account deletion behavior, and what happens to samples already exported into training datasets. Never put samples or secrets in Git.

### Model manifest
9. Keep the initial manifest in Host configuration or a small versioned manifest file; do not add a fourth module or a database-backed admin platform for this first iteration. Serve it from a thin Host Minimal API endpoint, e.g. `/api/v1/models/manifest`, with explicit selection/validation for supported platform and release channel where needed.
10. Manifest entries should include immutable model version/artifact location, SHA-256, size in bytes, minimum compatible client version, preprocessing/decoder/vocabulary compatibility versions, and corpus/normalization versions where applicable. Do not commit `.onnx` files. Keep artifact transfer out of the API request path.
11. Validate manifests at startup or in tests so missing hashes, invalid URLs, duplicate versions, or incompatible required fields cannot silently ship. Document signature verification if the release threat model requires a signed manifest. Do not claim client rollback behavior is implemented unless there is actual client code in this repository; document it as the client contract.

### Quality bar
- Test consent enforcement, auth/ownership, duplicate requests, missing objects, invalid/oversized objects, storage errors/retries, state transitions, and malformed/incompatible manifests.
- Verify the local MinIO round-trip from the perspective of the actual caller (including browser-reachable presigned URL, not only the API container).
- Run restore/build/test and feasible Docker integration tests; fix issues.
- Update docs with local setup, configuration variable names, lifecycle states, retention/consent assumptions, and any limits not yet resolved.
- Commit with a focused message such as `feat: add dataset ingestion and model manifest`; push without force if safe.
- Report exactly what is implemented and tested, and clearly list what remains a client responsibility or needs a product/legal decision.
