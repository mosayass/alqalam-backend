# AlQalam Backend — Product Requirements & User Stories

This document defines the functional requirements and user stories for the AlQalam backend, derived from the inspected Figma design system (`JHytc94KpPeVuluGD1iYvq`) and established architectural constraints.

---

## Architecture & Boundary Principles

1. **On-Device Inference**: The AI handwriting recognition (HTR), Quran text retrieval, diacritics normalization, and diff/alignment run **100% on-device in the client application**. The backend does not run OCR or perform recitation verification.
2. **Backend Responsibilities**:
   - Identity & Account security.
   - Profile settings and user metadata.
   - Offline-resilient sync for practice sessions, attempt logs, and progress metrics.
   - Optional, consent-governed handwriting sample upload (via pre-signed S3 URLs) for offline model retraining.
   - Immutable client model manifest distribution.
   - Future (Post-MVP): Challenges, leaderboards, and Quran study circles ("الحلقات").

---

## Epic 1: Identity, Accounts & Profile Management

### Story 1.1: New User Registration
- **User Goal**: As a Quran learner, I want to create an account with my full name, email, password, and my current memorization level so that my practice and progress are securely tracked across devices.
- **Related Figma Screen**: `إنشاء حساب جديد - Al-Qalam Sign Up` (`id: 1:477`).
- **Priority**: **MVP**
- **Acceptance Criteria**:
  - `POST /api/v1/users/register` accepts `FullName`, `Email`, `Password`, and optional `InitialLevel` (e.g. `Beginner` / جزء عمّ, `Intermediate` / ٥ أجزاء, `Advanced` / خاتم أو قريب).
  - Validates full name presence and email format (`rfc5322`).
  - Enforces password complexity (minimum 8 characters, requiring mixed character classes) using standard ASP.NET Core Identity password validation.
  - Rejects duplicate emails with a generic 409 Conflict ProblemDetails.
  - Returns `201 Created` with the registered user profile (excluding password/security hashes).
  - *Social Login (Google / Apple)*: Figma shows Google/Apple buttons. For MVP, identity endpoints support first-party email/password; OAuth/OIDC external token exchange is planned as a fast-follow.

### Story 1.2: User Authentication & Token Refresh
- **User Goal**: As a returning learner, I want to sign in securely with my email/username and password and maintain a persistent, secure session.
- **Related Figma Screen**: `تسجيل الدخول - Al-Qalam Login` (`id: 1:3`).
- **Priority**: **MVP**
- **Acceptance Criteria**:
  - `POST /api/v1/users/login` accepts email/identifier and password.
  - Verifies credentials against ASP.NET Core Identity password hash.
  - Returns a short-lived JWT access token (e.g. 15 minutes) and a secure, cryptographically random refresh token.
  - Plaintext refresh tokens are **never** stored in the database; only SHA-256 hashed representations are persisted.
  - `POST /api/v1/users/refresh` rotates the refresh token upon valid exchange (old token is revoked, new token issued). Detecting reuse of a revoked token invalidates the entire refresh-token family.
  - `POST /api/v1/users/logout` revokes active refresh tokens for the calling session.
  - Returns generic 401 Unauthorized for invalid credentials to prevent account enumeration.

### Story 1.3: User Profile & Learning Metrics Query
- **User Goal**: As an authenticated learner, I want to view my profile details, current memorization track, and aggregated metrics so I can monitor my dedication and progress.
- **Related Figma Screen**: `الملف الشخصي - Al-Qalam Profile` (`id: 1:1003`).
- **Priority**: **MVP**
- **Acceptance Criteria**:
  - `GET /api/v1/users/me` derives the `UserId` strictly from authenticated claims (never from client parameters).
  - Returns:
    - User identity: `FullName`, `Email`, `TrackName` (e.g. "حافظ متعلم • مسار سورتي الكهف والبقرة").
    - Transparent counts: Consecutive streak days (`StreakDays`), cumulative inking duration in minutes (`InkingMinutes`), total mastered Ayahs (`MasteredAyahsCount`), and current active target (`CurrentTarget`: Surah, Page, Ayah).
  - Returns 401 Unauthorized if the access token is invalid or expired.

### Story 1.4: User Preferences & Sync Settings
- **User Goal**: As a learner, I want my application preferences (language, daily targets, cloud sync preferences) saved and restored across sessions.
- **Related Figma Screen**: `الإعدادات والتفضيلات - Al-Qalam Settings` (`id: 1:633`).
- **Priority**: **MVP**
- **Acceptance Criteria**:
  - `GET /api/v1/users/preferences` and `PUT /api/v1/users/preferences`.
  - Supports fields visible in Figma:
    - `Language` (`ar` default, `en`).
    - `DailyGoalVersesCount` (e.g., 5 verses/day).
    - `CloudSyncEnabled` (boolean toggle: "المزامنة الفورية مع السحابة").
    - `DefaultReciterId` (e.g., Mishary Rashid Alafasy).
  - Client-only UI preferences (canvas pen pressure, palm rejection, blue-light filter, font scale) are primarily cached on-device, but synced to user preferences if cloud sync is enabled.

---

## Epic 2: Memorization & Practice Synchronization

### Story 2.1: Idempotent Practice Session Ingestion
- **User Goal**: As a learner practicing handwriting offline, I want my completed practice sessions and verse attempts to synchronize reliably to the server when connectivity returns, without creating duplicate entries upon network retry.
- **Related Figma Screens**: `Dashboard` (`id: 1:263`), `Handwriting Canvas` (`id: 1:83`), `Profile` (`id: 1:1003`).
- **Priority**: **MVP**
- **Acceptance Criteria**:
  - `POST /api/v1/memorization/sessions` accepts practice session data:
    - `ClientSessionId` (client-generated UUID for idempotency).
    - `ClientStartedAt` and `ClientCompletedAt` (UTC timestamps stored separately from `ServerReceivedAt`).
    - `SurahNumber`, `AyahStart`, `AyahEnd`, `PageNumber`.
    - `Attempts`: list of per-Ayah attempt evidence containing `AyahNumber`, `ClientAccuracyScore` (percentage reported by on-device alignment), `ErrorCount`, `DurationSeconds`, `ModelVersion`.
  - **Idempotency Guarantee**: Submitting the same `ClientSessionId` from the same authenticated user returns `200 OK` (or `201 Created` on first receipt) and the existing persisted record without creating duplicate database rows (enforced by a database uniqueness constraint on `(UserId, ClientSessionId)`).
  - Server records client-reported outcomes; the backend does not recalculate or alter the client's ML alignment score.

### Story 2.2: Practice History & Session Logs
- **User Goal**: As a learner, I want to review my past practice history and corrected attempts so I can identify recurring mistakes.
- **Related Figma Screen**: `الملف الشخصي - Al-Qalam Profile` (`id: 1:1003` - "سجل التسميع والمحاولات").
- **Priority**: **MVP**
- **Acceptance Criteria**:
  - `GET /api/v1/memorization/sessions` supports keyset or offset-based pagination (`page`, `pageSize`, `surah`).
  - Returns historical session summaries ordered by date descending.
  - Queries are indexed by `(UserId, CreatedAt)`.

### Story 2.3: Memorization Progress & Slate Summary
- **User Goal**: As a learner opening the application, I want to see my current memorization dashboard with active targets, recent attempts, and streak status.
- **Related Figma Screen**: `Dashboard - Kinesthetic Quran Memorization` (`id: 1:263`).
- **Priority**: **MVP**
- **Acceptance Criteria**:
  - `GET /api/v1/memorization/progress` returns:
    - Current slate target: Surah, Ayah number, Juz, Page.
    - Last recorded attempt: Surah, Ayah, accuracy, timestamp.
    - Transparent aggregated counts: Total Ayahs attempted, count of Ayahs with 100% accuracy, total practice sessions.
  - Does not invent an opaque proprietary mastery formula; reports clear, observable counts and rates.

---

## Epic 3: Offline Training Dataset Ingestion (Consent-Based)

### Story 3.1: Consent & Privacy Management
- **User Goal**: As a privacy-conscious learner, I want explicit control over whether my difficult handwriting strokes are contributed to offline model retraining datasets.
- **Related Figma Screen**: `الإعدادات والتفضيلات - Al-Qalam Settings` (`id: 1:633` - "الخصوصية وتشفير الكتابة: البيانات مشفرة طرفياً ولا تُشارك إطلاقاً").
- **Priority**: **MVP**
- **Acceptance Criteria**:
  - `POST /api/v1/dataset/consent` allows users to opt in or opt out.
  - Captures `ConsentGiven` (boolean), `ConsentVersion`, and `Timestamp`.
  - Default state is strictly **opted-out** (no samples accepted without active consent).

### Story 3.2: Secure Pre-Signed Sample Upload Initiation
- **User Goal**: As a client device with active user consent, I want to request a secure, temporary upload URL to upload an unrecognized handwriting sample directly to private storage.
- **Related Figma Flow**: Client HTR engine fallback / low-confidence sample collection.
- **Priority**: **MVP**
- **Acceptance Criteria**:
  - `POST /api/v1/dataset/samples/upload-request` requires authentication and active opt-in consent.
  - Validates sample metadata: `SurahNumber`, `AyahNumber`, `ClientModelVersion`, `ReportedAccuracy`.
  - Generates a server-owned `SampleId` and random, non-guessable S3 object key.
  - Generates a short-lived (e.g. 10-minute) pre-signed PUT URL using the configured **PublicEndpoint** (ensuring reachability from tablets and browsers).
  - Returns `SampleId`, `UploadUrl`, and `ExpiresAt`.

### Story 3.3: Upload Confirmation & Metadata Validation
- **User Goal**: As a client that finished uploading a handwriting sample, I want to notify the server so it verifies the object exists and marks it for offline pipeline ingestion.
- **Priority**: **MVP**
- **Acceptance Criteria**:
  - `POST /api/v1/dataset/samples/{sampleId}/complete` verifies ownership.
  - Backend verifies object existence in private S3 bucket and inspects byte size and content headers.
  - Marks sample status as `Received` / `Validated`. If missing or corrupt, marks as `Failed`.
  - Operation is idempotent (re-sending complete for an already validated sample returns success).

---

## Epic 4: Client Model Distribution

### Story 4.1: Fetch Latest Model Manifest
- **User Goal**: As a client app launching or checking for updates, I want to retrieve the latest compatible ONNX Runtime handwriting model manifest so I can run recognition locally on-device.
- **Related Figma Screen**: Model settings / client versioning ("تطبيق القلم • الإصدار ٢.٤").
- **Priority**: **MVP**
- **Acceptance Criteria**:
  - `GET /api/v1/models/manifest` accepts optional query parameters: `platform` (`ios`, `android`, `web`, `windows`), `channel` (`stable`, `beta`).
  - Returns versioned manifest:
    - `ModelVersion` (e.g. `2.4.0`).
    - `DownloadUrl` (direct CDN / object storage URL, never streamed through the backend API).
    - `Sha256Checksum` for integrity verification.
    - `ByteSize`.
    - `MinClientVersion`.
  - Manifest is backed by strongly typed configuration / versioned manifest document (no database overhead for MVP).

---

## Epic 5: Gamification, Challenges & Leaderboards (Post-MVP)

### Story 5.1: Weekly Honor Podium & Leaderboard
- **User Goal**: As a dedicated student of the Quran, I want to see weekly rankings and top reciters so I am motivated to maintain my daily practice.
- **Related Figma Screens**: `الإنجاز والتنافس - النمط الليلي الزمردي` (`id: 1:1175`) and `النمط الفاتح` (`id: 1:1661`).
- **Priority**: **Post-MVP**
- **Requirements (Design Grounded)**:
  - Weekly Honor Platform ("منصة شرف الأسبوع الحالية").
  - Top 3 podium (🥇 Golden Pen of Mastery / "قلم الإتقان الذهبي", 🥈 Silver, 🥉 Bronze).
  - Leaderboard rows displaying user name, weekly points ("٢,٠٥٠ ن"), mastered surahs ("عمّ وتبارك"), and accuracy rate ("دقة ٩٨.٢٪").
  - Tier leagues: Gold Tier ("الفئة الذهبية") to Platinum Tier ("الارتقاء للفئة البلاتينية").

### Story 5.2: Memorization Challenges & Circles (الحلقات)
- **User Goal**: As a learner, I want to participate in weekly challenges ("سباق أهل القرآن الكبرى") and study circles.
- **Related Figma Screen**: `الإنجاز والتنافس` tabs: "الحلقات", "أوسمة الإتقان", "تحديات الحفظ", "المتصدرون".
- **Priority**: **Post-MVP**
- **Requirements**:
  - Seasonal events (e.g. "موسم الإتقان الربيعي • الأسبوع ٣").
  - Circles / Halawat group progress.
  - Badges of mastery ("أوسمة الإتقان").

---

## Open Product Decisions Requiring Stakeholder Resolution

1. **Streak Boundary Semantics**:
   - *Question*: How is a daily streak day bounded? Is it based on client local time (e.g. UTC offset reported by device) or Islamic prayer times (Fajr-to-Fajr)?
   - *Current Default*: Client local midnight UTC offset until an Islamic prayer time schedule is specified.
2. **Offline Retraining Sample Format**:
   - *Question*: Does the client upload raw vector stroke points (JSON / Protobuf), rendered raster tiles (PNG), or both?
   - *Current Default*: Accept binary payload under configured size cap (e.g. 5 MB) with MIME type validation.
3. **External Social Authentication (Google / Apple)**:
   - *Question*: When will OAuth client IDs and token validation be scheduled?
   - *Current Default*: First-party email/password in MVP; Google/Apple federated token validation in Phase 3.5.
4. **Leaderboard Scoring & Tier Formula**:
   - *Question*: Exact point accrual algorithm for leaderboard placement (points per verse, accuracy weighting, streak multiplier). Post-MVP.
