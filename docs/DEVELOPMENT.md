# AlQalam Backend — Local Development & Operations Guide

This guide documents local development setup, infrastructure services, configuration, and EF Core migration commands.

---

## 1. Prerequisites

- **.NET 10 SDK** (verified with installed SDK `10.0.200-preview.0.26103.119` and runtime `10.0.2`)
- **Docker & Docker Compose** (for local PostgreSQL and MinIO)
- **`dotnet-ef` global tool** (optional, for managing migrations):
  ```powershell
  dotnet tool install --global dotnet-ef
  ```

---

## 2. Local Infrastructure (Docker Compose)

Local infrastructure is defined in [`compose.yaml`](file:///c:/Users/mosa/OneDrive/Desktop/alqalam/compose.yaml) and includes:
- **PostgreSQL 17** (`postgres:17.4-alpine`) on port `5432` with health checks and named volume `alqalam_postgres_data`.
- **MinIO S3-compatible Object Storage** (`minio/minio:RELEASE.2025-02-28T09-55-16Z`) on port `9000` (API) and `9001` (Console) with named volume `alqalam_minio_data`.

### Common Commands

- **Start all dependencies in the background**:
  ```powershell
  docker compose up -d
  ```

- **Inspect service health and container status**:
  ```powershell
  docker compose ps
  ```

- **View real-time service logs**:
  ```powershell
  docker compose logs -f
  ```

- **Stop all services (preserving data volumes)**:
  ```powershell
  docker compose down
  ```

- **Reset all services (wiping volumes for clean state)**:
  ```powershell
  docker compose down -v
  ```

---

## 3. Configuration & Environment Variables

### `.env` File
Copy [`.env.example`](file:///c:/Users/mosa/OneDrive/Desktop/alqalam/.env.example) to `.env` if you wish to override default credentials locally:
```powershell
Copy-Item .env.example .env
```
> [!NOTE]
> `.env` is ignored by Git and must never contain production secrets.

### Connection Strings & Storage Options
Configuration is defined in [`src/Host/appsettings.json`](file:///c:/Users/mosa/OneDrive/Desktop/alqalam/src/Host/appsettings.json):
```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5432;Database=alqalam;Username=alqalam_dev;Password=alqalam_dev_password"
  },
  "Storage": {
    "InternalEndpoint": "http://localhost:9000",
    "PublicEndpoint": "http://localhost:9000",
    "Region": "us-east-1",
    "BucketName": "alqalam-dataset",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin"
  },
  "Jwt": {
    "Issuer": "AlQalam",
    "Audience": "AlQalam.Clients",
    "SigningKey": "",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 30
  }
}
```

### Critical Note on MinIO Endpoints
- **`InternalEndpoint`**: The endpoint used by the API server or container to connect to MinIO (e.g. `http://minio:9000` inside Docker or `http://localhost:9000` during local debugging).
- **`PublicEndpoint`**: The endpoint embedded in client/browser-facing pre-signed URLs (e.g. `http://localhost:9000` or `https://s3.alqalam.app`). A Docker-internal hostname such as `http://minio:9000` is **not reachable** from a client device or web browser.

### JWT Authentication Configuration
- **`SigningKey`**: Required HMAC-SHA256 signing secret key (minimum 32 characters / 256 bits).
- For local native execution, set `Jwt__SigningKey` via environment variable or in local `.env`:
  ```powershell
  $env:Jwt__SigningKey = "your_dev_secret_key_at_least_32_characters_long!"
  ```
- Startup validation immediately fails if `SigningKey` is missing or shorter than 32 characters.

### Local Development Email Confirmation
In development, no SMTP server is configured yet. When a user registers via `POST /api/v1/users/register`, the development `LoggingConfirmationEmailSender` logs the confirmation token to the application console:
```text
info: AlQalam.Users.Infrastructure.Services.LoggingConfirmationEmailSender[0]
      [DEV EMAIL] Confirmation link for user@example.com (UserId: 7b81...): Token: CfDJ8...
```
To confirm the email locally, copy the `userId` and `token` from the console output and send a `POST` request to `/api/v1/users/confirm-email`:
```json
{
  "userId": "7b81...",
  "token": "CfDJ8..."
}
```

---

## 4. Running the API Locally

Run the Host natively using the .NET CLI:
```powershell
dotnet run --project src/Host/AlQalam.Host.csproj --launch-profile http
```

### Health Endpoints
- **Liveness Probe**: `GET http://localhost:5000/health/live`
  - Returns `200 OK` `{ "status": "live" }` whenever the host process is running.
- **Readiness Probe**: `GET http://localhost:5000/health/ready`
  - Verifies connectivity to the database.
  - Returns `200 OK` `{ "status": "ready" }` when dependencies are healthy.
  - Returns `503 Service Unavailable` `{ "status": "unhealthy" }` when dependencies are down, without leaking sensitive internals or credentials.

### OpenAPI & Interactive API Documentation (Scalar)
- When running in Development mode:
  - **OpenAPI Schema**: `GET http://localhost:5000/openapi/v1.json`
  - **Scalar Interactive UI**: `GET http://localhost:5000/scalar/v1` (modern interactive API documentation replacing Swagger UI)

---

## 5. Running Tests

Run the complete test suite:
```powershell
dotnet test AlQalam.slnx
```

Tests include:
- Result/Error primitives and failure type assertions.
- ProblemDetails HTTP status mapping and error sanitization.
- DbContext schema isolation (`users`, `memorization`, `dataset`).
- StorageOptions configuration binding.
- Health endpoint liveness and readiness probes.

---

## 6. Entity Framework Core & Database Migrations

### Architecture Rules
- Each module has its own `DbContext` and its own schema:
  - `UsersDbContext` → Schema `users`, history table `users.__EFMigrationsHistory`
  - `MemorizationDbContext` → Schema `memorization`, history table `memorization.__EFMigrationsHistory`
  - `DatasetDbContext` → Schema `dataset`, history table `dataset.__EFMigrationsHistory`
- Avoid cross-module foreign key navigation properties or cross-module database joins.
- **Production Migrations**: Run migrations as a controlled deployment step (e.g. via `dotnet ef database update` or generated idempotent SQL scripts). **Do not** run concurrent `Database.Migrate()` on every API instance startup.

### Generating Migrations (Phase 3+)
When entities are added in future phases, generate migrations per context:

```powershell
# Users
dotnet ef migrations add InitialUsers \
  --project src/Modules/Users/Infrastructure/AlQalam.Users.Infrastructure.csproj \
  --startup-project src/Host/AlQalam.Host.csproj \
  --context UsersDbContext

# Memorization
dotnet ef migrations add InitialMemorization \
  --project src/Modules/Memorization/Infrastructure/AlQalam.Memorization.Infrastructure.csproj \
  --startup-project src/Host/AlQalam.Host.csproj \
  --context MemorizationDbContext

# Dataset
dotnet ef migrations add InitialDataset \
  --project src/Modules/Dataset/Infrastructure/AlQalam.Dataset.Infrastructure.csproj \
  --startup-project src/Host/AlQalam.Host.csproj \
  --context DatasetDbContext
```

### Applying Migrations Locally
```powershell
dotnet ef database update --project src/Modules/Users/Infrastructure/AlQalam.Users.Infrastructure.csproj --startup-project src/Host/AlQalam.Host.csproj --context UsersDbContext
dotnet ef database update --project src/Modules/Memorization/Infrastructure/AlQalam.Memorization.Infrastructure.csproj --startup-project src/Host/AlQalam.Host.csproj --context MemorizationDbContext
dotnet ef database update --project src/Modules/Dataset/Infrastructure/AlQalam.Dataset.Infrastructure.csproj --startup-project src/Host/AlQalam.Host.csproj --context DatasetDbContext
```

### Generating Production SQL Scripts
```powershell
dotnet ef migrations script --idempotent \
  --project src/Modules/Users/Infrastructure/AlQalam.Users.Infrastructure.csproj \
  --startup-project src/Host/AlQalam.Host.csproj \
  --context UsersDbContext \
  --output migrations_users.sql
```

---

## 7. Dependency Licenses & Compatibility

All dependencies are officially maintained, compatible with .NET 10, and use standard permissive open-source licenses:
- `Microsoft.EntityFrameworkCore` 10.0.12 (MIT)
- `Microsoft.EntityFrameworkCore.Relational` 10.0.12 (MIT)
- `Microsoft.EntityFrameworkCore.Design` 10.0.12 (MIT)
- `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 (PostgreSQL License)
- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 10.0.12 (MIT)
- `Microsoft.AspNetCore.OpenApi` 10.0.12 (MIT)
- `xUnit` 2.9.3 (Apache 2.0)
- `Microsoft.NET.Test.Sdk` 18.10.1 (Microsoft Pre-Release/Standard Software License)
- `coverlet.collector` 6.0.4 (MIT)
