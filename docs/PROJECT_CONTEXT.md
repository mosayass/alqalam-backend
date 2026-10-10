# AlQalam Backend — Project Context

## Product goal
AlQalam is a cross-platform, tablet-first and web application for Quran memorization and recitation verification through handwritten stylus input.

## Client-side flow
1. A learner writes one or more Quranic verses with a stylus on a digital canvas.
2. The client captures stroke vectors/touch data and preprocesses the writing.
3. An on-device deep-learning handwriting-recognition engine transcribes the writing into Arabic characters. The planned model is developed with PyTorch and exported/quantized for ONNX Runtime INT8 across supported native/mobile/web targets.
4. A local normalization and inverted-index retrieval engine matches the transcription against the canonical Quran corpus of 6,236 Ayahs.
5. A local sequence-alignment/diff engine (e.g. Needleman–Wunsch or Levenshtein-based alignment) compares the attempt with canonical text and highlights correct, missing, and erroneous words/characters.

Recognition, Quran lookup, and detailed alignment should remain on-device where practical. The backend should not be placed in the critical path of checking each verse.

## Backend responsibilities
- User identity, authentication/session management, profiles, and account settings.
- Synchronization of practice sessions and user progress across devices, including offline/retry scenarios.
- Memorization history, per-Ayah attempt evidence, and transparent progress/streak metrics.
- Optional, consent-based collection of low-confidence or otherwise useful handwriting samples for offline training/retraining.
- Issuing narrowly scoped pre-signed object-storage URLs and recording/validating sample metadata.
- Serving small model-release manifests and compatible download metadata. Large model files should be stored and delivered through object storage/CDN rather than proxied through API requests when possible.

## Main modules
### Users
Authentication, refresh/revocation, profile, account settings, and the server-side identity contract.

### Memorization
Practice session ingestion, idempotent synchronization, attempt history, progress summaries, and streak/ayah metrics. Preserve enough model/corpus/normalization version metadata to interpret an attempt later. Do not invent an unspecified mastery-scoring algorithm.

### Dataset
Explicit opt-in/consent state, sample upload authorization, sample metadata, object existence and integrity checks, ingestion status, retention/deletion behavior, and eligibility for a future offline training pipeline. Training itself is out of scope for the web backend.

### Model manifest (initially small)
For the first implementation, do not create a fourth production module just for a read-only manifest. Expose a thin Host endpoint backed by strongly typed configuration or a small versioned manifest file. Keep the model artifacts immutable in object storage/CDN. Document extraction into its own module only if release workflows become sufficiently complex (staged rollouts, admin approval, per-device targeting, etc.).

## Expected development environment
- Backend: .NET 10 / ASP.NET Core Minimal APIs.
- Data access: EF Core with PostgreSQL through the Npgsql provider.
- Local dependencies: PostgreSQL and S3-compatible storage such as MinIO via Docker Compose.
- Normal daily debugging: run the Host natively using the .NET CLI.
- Production packaging: one multi-stage Dockerfile for the Host; no per-module containers.
- Tests: unit tests plus PostgreSQL-backed integration tests for behavior that depends on relational constraints or migrations.

## Explicit non-goals for the initial backend
- Server-side handwriting recognition or ONNX inference as part of normal recitation verification.
- A microservice per module.
- RabbitMQ/Kafka or a general-purpose distributed event bus.
- Event sourcing, a generic repository framework, a dedicated model registry platform, or premature database partitioning.
- Collecting handwriting samples by default without explicit consent.
