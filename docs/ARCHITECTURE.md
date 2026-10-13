# AlQalam Backend — Architecture Decisions

This document records the initial agreed architecture. Update it when a decision intentionally changes.

## 1. Shape of the system
Use a modular monolith. There is one deployable ASP.NET Core Host and three business modules: Users, Memorization, and Dataset. A module has two production projects only:

- `<Module>.Core`
- `<Module>.Infrastructure`

The solution also has `Host` and a deliberately small `Shared` project. Test projects do not count against the two-production-project convention.

## 2. Endpoint ownership
All Minimal API endpoint mappings live in `Host/Endpoints/`, organized into files/folders by module. `Host/Program.cs` composes route mappings and dependency injection. Infrastructure must not map HTTP endpoints or reference Host.

Endpoint code maps HTTP contracts to module use cases and maps results to HTTP outcomes. Business logic lives in Core/application handlers, not in endpoint lambdas.

## 3. Project dependency direction
- `Shared`: no dependency on business modules.
- `Module.Core` → `Shared` only (plus carefully justified non-infrastructure libraries if actually needed).
- `Module.Infrastructure` → its own `Module.Core` and `Shared`, plus persistence/external integration packages.
- `Host` → all module Core and Infrastructure projects, plus `Shared`.
- No business module references another business module's `Core` or `Infrastructure`. If cross-module communication is needed later, introduce a dedicated `<Module>.Contracts` project containing public contracts/DTOs/events only.
- No Infrastructure project references Host.

Prefer explicit interfaces/contracts for cross-module coordination. Avoid a global hidden event-handler web.

## 4. Data and database
One PostgreSQL database to start, one `DbContext` per module, one schema per module:

| Context | Schema | Ownership |
|---|---|---|
| `UsersDbContext` | `users` | identity/profile data |
| `MemorizationDbContext` | `memorization` | sessions, attempts, progress-owned records |
| `DatasetDbContext` | `dataset` | consent, sample metadata, ingestion state |

Each context and its migrations live in that module's Infrastructure project. Configure an explicit default schema and an independent migration-history table in that schema. A context must not map another module's entities.

Prefer module-owned records referencing another module's ID over cross-module EF navigation properties. Cross-module database constraints/transactions may be used only when the benefit is explicit and the coupling is documented. Keep most transactions inside one module.

Do not infer that millions of registered users automatically require multiple databases. Optimize based on actual workload and measurements. Preserving module ownership makes later extraction easier, but does not make it automatic.

## 5. Application patterns & CQRS
- **Use Case Handlers over MediatR**: Do not use MediatR. Register handlers and interfaces directly into ASP.NET Core Dependency Injection for compile-time safety and zero reflection overhead.
- **Commands (Writes)**:
  - Command records and their handler implementations live in `Module.Core`.
  - **Rich Domain Model**: Domain entities must be encapsulated with private/protected setters and explicit domain methods to protect domain invariants. Do not create anemic `{ get; set; }` property bags.
  - **Aggregate-Specific Repositories**: To abstract persistence without referencing EF Core in `Core`, define focused, aggregate-specific repository interfaces in `Module.Core` (e.g., `ISessionRepository`, `ISampleRepository`, or `IIdentityService` for Identity-managed accounts). Do not use generic `IRepository<T>` wrappers.
  - Repository implementations live in `Module.Infrastructure` backed by the module's `DbContext`.
- **Queries (Reads)**:
  - The Query record, return DTOs, and `IQueryHandler<TQuery, TResult>` interface live in `Module.Core`.
  - **Query Handler implementations live in `Module.Infrastructure`**. Because queries are pure database projections with zero domain logic, placing implementations in Infrastructure allows direct use of EF Core (`.AsNoTracking()`, `.Select()` straight to DTOs) for maximum performance without forcing `Module.Core` to reference EF Core. Do not introduce intermediate pass-through query services.
- **Operational Failures**: Use a small `Result<T>` / `Error` model for expected business failures; let unexpected faults bubble up to centralized exception handling and ProblemDetails.
- **Cross-Module Communication**:
  - Modules do not reference one another.
  - Prefer ID-referencing (sharing only entity IDs, such as `UserId` `Guid`).
  - For unavoidable runtime queries, expose minimal contract interfaces in `Shared` (or `<Module>.Contracts`).
  - **Integration Events**: Keep deferred until an actual cross-module event is required. When introduced, evaluate non-blocking background dispatch (`System.Threading.Channels`) or the Transactional Outbox pattern so subscriber failures never fail or roll back the publishing module.
- **Module Ownership of Social Auth**:
  - External authentication (Google and Apple) belongs directly in the `Users` module, mapped via ASP.NET Core Identity's `user_logins` table. Neither separate modules nor duplicate identity stores should be created.

## 6. API conventions
- Version API routes with `/api/v1/...`.
- Use Minimal API route groups and OpenAPI metadata.
- Use centralized ProblemDetails/error mapping and validation.
- Require authentication and enforce ownership for per-user resources.
- Keep health endpoints separate from business routes.

## 7. Local and production hosting
- Docker Compose provides local PostgreSQL and MinIO, with named volumes and health checks.
- Run the API natively during daily development by default.
- Configure MinIO's internal service endpoint separately from the public/browser-reachable endpoint used in signed URLs. URLs returned to a browser must not contain a Docker-only service hostname.
- One multi-stage Dockerfile builds/runs the Host for production.
- Production migrations run as a controlled deployment operation, not opportunistically during every instance startup.
- Never commit real secrets. Local-only credentials must be clearly marked as development-only and configurable.

## 8. Offline synchronization rules
Practice writes must be retry-safe. Use a stable client-generated operation/session ID and a uniqueness constraint scoped to the authenticated user. A duplicate request must not create a second session. Treat completed attempts as historical evidence where practical; do not silently overwrite progress with a last-write-wins update.

Store client event time separately from server receipt time. Record relevant model/corpus/normalization versions. Use optimistic concurrency where multiple clients can update shared mutable summaries. Paginate histories and index actual query paths.

## 9. Dataset ingestion lifecycle
Treat pre-signed URLs as bearer capabilities. A baseline lifecycle is:
1. Verify caller and explicit active opt-in/consent.
2. Create a server-owned sample ID and unique object key.
3. Issue a short-lived, narrowly scoped upload permission.
4. Client uploads directly to private object storage.
5. Completion endpoint verifies object existence and available integrity/size metadata.
6. A sample is not training-eligible until validation passes.

Uploads, completion requests, and background validation must be idempotent. Do not trust client-provided object keys, owner IDs, MIME types, checksums, or reported upload success. Document any size restriction that the selected S3-compatible signed-upload mechanism cannot enforce before receipt.

Capture only the sample metadata needed for debugging/retraining. Document consent version, access, retention, deletion, and what happens to already-exported training datasets after consent withdrawal/account deletion.

## 10. Model manifest
The initial manifest endpoint can be config/file-backed in Host; no fourth production module yet. Keep model artifacts immutable and version-addressed. Manifest entries should include model version, target platform/channel, artifact location, SHA-256, byte size, minimum compatible client version, preprocessing/decoder/vocabulary compatibility versions, and corpus/normalization compatibility versions where applicable. Add a signature if the chosen distribution trust model requires it.

Clients should download to a temporary location, verify integrity, install atomically, and retain a last-known-good model. Never store `.onnx` binaries in Git.

## 11. Test strategy
- Unit-test core behavior independently of PostgreSQL and HTTP.
- Use PostgreSQL-backed tests for migrations, schema separation, unique constraints, transactions, and query behavior.
- Test authorization/ownership, idempotent retries, concurrent updates, refresh-token rotation/revocation, object-upload lifecycle, and model-manifest validation.
- Do not use EF InMemory to claim relational correctness.

## 12. Not yet decided (do not invent silently)
- Exact mastery-score formula and streak boundary/time-zone semantics.
- Exact handwriting sample formats and retention duration.
- Dataset consent wording/legal policy and whether raw strokes, rendered images, or both are retained.
- Social login (Google / Apple) token exchange and account-linking policy (post-MVP; first-party ASP.NET Core Identity with password auth in MVP).
- Model artifact hosting provider, release approval workflow, and rollout percentages.

Choose conservative defaults only where implementation needs them, document the choice, and keep the product-policy questions explicit.
