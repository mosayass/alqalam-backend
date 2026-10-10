# AlQalam Backend — Implementation Plan

Work in these phases sequentially. Do not implement every phase in one giant change. Finish each phase with a reviewable commit and a concise report.

## Phase 1 — Repository bootstrap and scaffold
- Verify the current workspace is safe to initialize and the supplied GitHub remote is the intended empty repository.
- Initialize Git if needed; use branch `main`; add `origin` as `https://github.com/mosayass/alqalam-backend.git` only if it is not already configured correctly.
- Create `AGENTS.md`, this docs set, solution, Host, Shared, and Core/Infrastructure projects for Users, Memorization, and Dataset.
- Ensure dependency direction follows `AGENTS.md`, projects build, API starts, and a simple liveness health endpoint works.
- Add `.gitignore`, `.editorconfig`, SDK/package centralization if appropriate. Do not create README.md.
- Commit the bootstrap and push to `origin/main` only when safe. Never force-push.

## Phase 2 — Foundation and local infrastructure
- Establish stable .NET 10 package versions and central package management if beneficial.
- Add small shared Result/Error primitives and centralized API error mapping.
- Register one DbContext per module and configure `users`, `memorization`, and `dataset` schemas and separate migration history tables.
- Add local Docker Compose for PostgreSQL and MinIO, with named volumes and health checks; add `.env.example` while ignoring actual `.env`.
- Add separate internal/public MinIO endpoint configuration so signed URLs work from browser/host environments.
- Add structured configuration options, liveness/readiness health checks, a production multi-stage Host Dockerfile, CI build/test workflow, and test project(s).
- Verify context creation and migration commands for each context. Avoid empty migrations with no useful schema change; real module migrations should be added with the first entities.

## Phase 3 — Users and Memorization MVP
- Implement first-party auth with ASP.NET Core Identity or a reviewed provider; do not custom-build password cryptography. If using JWT, implement short-lived access tokens and hashed refresh-token storage with rotation/revocation and tests.
- Implement a minimal authenticated profile endpoint.
- Implement the first retry-safe practice-session write and history/progress reads.
- Derive ownership from authenticated claims, not request-body UserId.
- Use stable client operation IDs and database uniqueness to make retries idempotent.
- Store client event time separately from server receipt time and record model/corpus/normalization versions.
- Keep session history distinct from derived progress summaries. Do not invent an opaque mastery score or streak boundary policy; document unresolved requirements.

## Phase 4 — Dataset ingestion and model manifest
- Implement explicit sample-collection opt-in/consent state and a private S3-compatible upload lifecycle.
- Add sample request and upload-completion endpoints, short-lived unique-key signed uploads, object verification, validation statuses, idempotency, and bounded retry handling.
- Ensure MinIO signed URLs returned to clients use a client-reachable hostname.
- Add a configuration/file-backed read-only model manifest endpoint, immutable artifact references, hashes, sizes, compatibility fields, and client/platform/channel selection as required by the manifest contract.
- Add tests for authorization, replay/retries, missing/corrupt/oversized object behavior, and incompatible/invalid manifests.
- Document retention, deletion, operational assumptions, and current limitations.

## Exit criteria for every phase
- Build and tests pass, or each blocker is clearly identified and justified.
- No real secret is committed.
- `AGENTS.md` and architecture docs remain aligned with implementation.
- A commit exists locally; push it to the configured remote if authentication/network/permissions allow.
- Report exact commands and outcomes. Never claim a command or push succeeded if it was not actually executed successfully.
