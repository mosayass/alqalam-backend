# AGENTS.md — AlQalam Backend

## Read this first
Before changing code, read:
1. `docs/PROJECT_CONTEXT.md`
2. `docs/ARCHITECTURE.md`
3. `docs/IMPLEMENTATION_PLAN.md`

These files record the product context and decisions already agreed with the project owner. Treat them as constraints, not suggestions. If new information makes a decision unsafe or technically impossible, explain the conflict and propose the smallest change instead of silently changing architecture.

## Strict Scope & Direct Answering Rule (CRITICAL)
- **Answer strictly what is asked**: Do not jump ahead, do not prepare unrequested code, components, or steps that have not been explicitly asked for and approved.
- **No unsolicited lengthy previews**: Avoid long outputs that run through unapproved future implementations.
- **Strict single-step focus**: Focus 100% on the single immediate question or task. Wait for explicit approval before proceeding to any next step.

## Product summary
AlQalam is a tablet-first and web Quran memorization and recitation-verification application. The client captures stylus handwriting and performs handwriting recognition, Quran-ayah retrieval, normalization, and word/character alignment on-device. The backend supports identity, progress synchronization, dataset collection, and model manifest delivery; it is not the normal inference engine.

## Required architecture
- Target **.NET 10 / C#**. Use stable, mutually compatible package versions. Verify framework/provider compatibility using official documentation when setting up or upgrading packages; do not use preview packages without explicit approval.
- Use a **modular monolith** with one ASP.NET Core HTTP host.
- Business modules: `Users`, `Memorization`, and `Dataset`.
- Each business module has exactly two production projects initially: `<Module>.Core` and `<Module>.Infrastructure`.
- `Host` owns all Minimal API endpoint mappings, HTTP request/response translation, routing, OpenAPI, middleware, authentication/authorization setup, and application composition. **Do not put HTTP endpoint mappings in Infrastructure. Do not create a separate API project per module.** Group endpoint files by module under `Host/Endpoints/`.
- `Module.Core` owns module domain concepts, application use cases/handlers, module DTOs/contracts, and module-specific abstractions. It must not reference EF Core, Npgsql, AWS/MinIO SDKs, ASP.NET Core HTTP types, or another business module.
- `Module.Infrastructure` owns EF Core persistence, migrations, integrations, and implementations of abstractions from its own Core project. It must not reference Host or another module.
- `Shared` must stay small and contain only genuinely cross-cutting primitives such as a minimal Result/Error model. Do not move domain-specific concepts into Shared.
- `Host` composes all modules and may reference their Core and Infrastructure projects. Business modules must not reference one another. Prefer explicit cross-module contracts for unavoidable interactions.
- Use CQRS/handlers where they clarify use cases, not ceremonially for every trivial query. Do not add MediatR automatically: if proposing it, check its current licensing/terms and justify the value. Direct application-service/handler invocation is acceptable.
- Use `Result<T>` (or an equally small documented Result model) for expected operational failures. Do not disguise unexpected faults as normal results; use centralized exception handling and sanitized ProblemDetails responses.

## Persistence rules
- Use one PostgreSQL database initially, with a dedicated EF Core `DbContext` per module and a separate schema per module: `users`, `memorization`, and `dataset`.
- Each context, its entity configurations, and its migrations belong to the corresponding Infrastructure project. Give each context its own migration history table in its own schema.
- Set the default schema explicitly in each context. Keep module table ownership strict.
- Avoid cross-module EF navigation properties and direct writes to another module's tables. Store foreign module IDs where needed. Avoid cross-module database joins unless there is a clearly documented reason.
- Use async database I/O and pass cancellation tokens through application and infrastructure methods.
- Do not use EF Core InMemory as proof that PostgreSQL constraints, schema configuration, transactions, or migrations work. Use real PostgreSQL for integration tests where feasible; document any environment limitation.
- Run production migrations as a controlled deployment step, not automatically and concurrently from every API instance.
- Do not add table partitioning, caching infrastructure, read replicas, extra databases, or microservices without measured need.

## API and security rules
- Use ASP.NET Core Minimal APIs, grouped and organized by module in `Host/Endpoints/`.
- Prefer a consistent `/api/v1/...` route prefix for public API routes, with health endpoints separately named.
- Keep endpoint handlers thin: bind and validate HTTP inputs, call application use cases, map outcomes to HTTP status codes/ProblemDetails. Do not put business logic or EF queries directly in endpoint lambdas.
- Enforce resource ownership server-side. Never trust a client-provided `UserId` to establish ownership.
- Do not invent bespoke password hashing or token cryptography. For first-party email/password authentication, prefer ASP.NET Core Identity or a reviewed identity provider. If implementing JWT and refresh tokens, use short-lived access tokens, hashed refresh-token storage, rotation/revocation, secure signing configuration, and tests. Never store plaintext refresh tokens.
- No secrets in source control. Use environment/configuration binding and `.env.example` placeholders for local setup; ignore real `.env` files. Do not add fallback production JWT secrets or credentials.
- Apply appropriate rate limiting and authorization to authentication and dataset-upload operations. Keep CORS restrictive and configuration-driven.
- Treat pre-signed object-storage URLs as bearer capabilities: short expiry, server-generated unique keys, private storage, ownership checks, upload completion verification, size/checksum/format validation, consent/retention handling, and idempotent operations.
- The server records client-reported recognition/alignment outcomes; do not claim that it independently verified Quranic correctness unless server-side verification is explicitly implemented.
- Model artifacts must be immutable/versioned. Manifest metadata should include integrity and compatibility information; never overwrite a versioned model object in place. Do not commit `.onnx` artifacts to Git.

## Delivery and engineering standards
- Keep changes scoped to the current phase; do not implement future phases early.
- Before choosing a library, check that it is supported, compatible with .NET 10, maintained enough for the use case, and appropriately licensed. Prefer official docs and stable packages.
- Use nullable reference types, clear names, focused methods, explicit configuration, and dependency injection. Avoid generic repositories or custom frameworks that merely wrap EF Core without adding a meaningful boundary.
- Add tests for changed behavior, especially authorization, idempotency, concurrency, state transitions, validation, and failures. Tests should assert observable behavior.
- Update architecture/development docs when making an intentional decision or discovering a limitation. Record unresolved product questions rather than inventing business rules.
- Do not create `README.md` unless the owner asks for one. Keep setup notes in `docs/`.
- Never use `git push --force`, destructive resets, or history rewriting. Do not discard unrelated files or silently replace an existing Git remote.
- Run and report `dotnet restore`, `dotnet build`, `dotnet test`, and feasible integration checks only when application or test code actually changed. If changes are strictly documentation/markdown, skip build and test. State honestly what was run (or skipped and why). Summarize changed files, decisions, and outcomes.
