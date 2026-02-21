# Financial Tracker — Modular Monolith

A financial tracker built as a **modular monolith** with clear bounded contexts, layered architecture, and readiness to split into microservices.

## Architecture

### Bounded contexts (3 modules)

1. **Auth** — Identity: registration, login, JWT issuance. All access to other services is through this auth flow.
2. **Accounts** — Finance accounts: create account, list accounts, get balance.
3. **Transactions** — Incomes and expenses: add income, add expense (updates account balance via application contract only).

### Structure per module (layered)

- **API / Controller** — HTTP endpoints, validation, auth.
- **Application / Service** — Use cases, DTOs, FluentValidation, coordination.
- **Domain** — Entities and domain logic (no EF, no framework).
- **Infrastructure** — EF Core, MySQL, repositories, JWT/BCrypt.

### Conventions

- **Domain** does not reference DB or frameworks.
- **DTOs** are split into Request and Response; responses expose only needed fields.
- **No cross-module infrastructure** — Transactions do not touch Accounts repositories; they use `IAccountBalanceUpdater` from Accounts.Application.
- **Auth gateway** — Login returns `name` and JWT; all other endpoints use `Authorization: Bearer <token>`.

## Tech stack

- .NET 8
- **SQLite in-memory** (H2 equivalent for .NET) — no database setup required
- Entity Framework Core 8
- FluentValidation with automatic validation
- JWT Bearer authentication
- BCrypt for password hashing
- API versioning (path-based: `/api/v1/...`)
- Centralized exception handling with unified error format
- Health check endpoints (`/health`, `/status`)
- Swagger/OpenAPI with JWT authentication support

## Prerequisites

- .NET 8 SDK
- **No database setup required** — uses SQLite in-memory (data resets on restart)

## Configuration

**All configuration is via environment variables** (no hardcoded values). Set the following:

### Required Environment Variables

```bash
JWT_SECRET=YourSuperSecretKeyForJwtTokensMustBeAtLeast32CharactersLong!
JWT_ISSUER=FinancialTracker
JWT_AUDIENCE=FinancialTracker
JWT_EXPIRATION_HOURS=24
```

### Optional Environment Variables

```bash
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=https://localhost:7001;http://localhost:5001
```

**Windows (PowerShell):**
```powershell
$env:JWT_SECRET="YourSuperSecretKeyForJwtTokensMustBeAtLeast32CharactersLong!"
$env:JWT_ISSUER="FinancialTracker"
$env:JWT_AUDIENCE="FinancialTracker"
$env:JWT_EXPIRATION_HOURS="24"
```

**Linux/Mac:**
```bash
export JWT_SECRET="YourSuperSecretKeyForJwtTokensMustBeAtLeast32CharactersLong!"
export JWT_ISSUER="FinancialTracker"
export JWT_AUDIENCE="FinancialTracker"
export JWT_EXPIRATION_HOURS="24"
```

See `.env.example` for reference.

**Note:** The database is SQLite in-memory — tables are created automatically on startup, and data resets when the application restarts.

## Run

```bash
cd "d:\Finance tracker"
dotnet run --project src/FinancialTracker.API/FinancialTracker.API.csproj
```

Open https://localhost:7001 (Swagger UI) or https://localhost:7001/swagger/v1/swagger.json (OpenAPI JSON).

## API Features

### 1. **API Versioning**
All endpoints are versioned via path: `/api/v1/...`
- Default version: v1.0
- Version specified in URL path
- Future versions can be added as `/api/v2/...`

### 2. **Centralized Exception Handling**
All errors return a unified format:
```json
{
  "statusCode": 400,
  "errorCode": "VALIDATION_ERROR",
  "message": "One or more validation errors occurred",
  "timestamp": "2026-02-21T10:00:00Z",
  "path": "/api/v1/auth/register",
  "method": "POST",
  "errors": [
    {
      "propertyName": "Email",
      "errorMessage": "Email is required",
      "attemptedValue": null
    }
  ]
}
```

### 3. **Automatic Input Validation**
- FluentValidation validates all DTOs before reaching business logic
- Validation errors automatically return 400 with detailed error messages
- No manual validation checks needed in controllers

### 4. **Health Checks**
- **GET /health** — Standard health check (all checks)
- **GET /health/ready** — Readiness: only checks tagged `ready` (DBs, JWT config); use for load balancers
- **GET /health/live** — Liveness: no dependencies; use for process-alive probes
- **GET /status** — Detailed JSON with status, total duration, and per-check results (name, status, description, duration)

All `IHealthCheck` implementations in the API assembly are **registered automatically**. To add a new check: create a class implementing `IHealthCheck` in the `HealthChecks` folder (optionally with `[HealthCheckRegistration(Name = "...", Tags = new[] { "ready" })]`). No changes to `Program.cs` are required.

### 5. **Swagger Documentation**
- Interactive API documentation at root (`/`)
- JWT authentication support — click "Authorize" and enter `Bearer {token}`
- All endpoints documented with request/response schemas

## REST API Endpoints

All calls to Accounts and Transactions require a JWT obtained from Auth.

### Auth (no JWT required)

- **POST /api/v1/auth/register**  
  Body: `{ "email", "password", "name" }`  
  Response: `{ "userId", "email", "name" }`

- **POST /api/v1/auth/login**  
  Body: `{ "email", "password" }`  
  Response: `{ "name", "token" }` — use `token` as Bearer for other APIs.

### Accounts (Bearer token required)

- **POST /api/v1/accounts**  
  Body: `{ "name", "currency" }` (e.g. `"USD"`)  
  Response: `{ "accountId", "name", "currency" }`

- **GET /api/v1/accounts**  
  Response: list of `{ "accountId", "name", "balance", "currency" }`

- **GET /api/v1/accounts/{accountId}**  
  Response: `{ "accountId", "name", "balance", "currency" }`

### Transactions (Bearer token required)

- **POST /api/v1/transactions/income**  
  Body: `{ "accountId", "amount", "category?", "note?" }`  
  Response: `{ "transactionId", "accountId", "type", "amount", "category", "occurredAtUtc" }`

- **POST /api/v1/transactions/expense**  
  Body: `{ "accountId", "amount", "category?", "note?" }`  
  Response: same shape. Returns 404 if account not found or insufficient balance.

## Solution layout

```
FinancialTracker.sln
src/
  FinancialTracker.API/                    # Host: auth, modules, JWT, Swagger
  FinancialTracker.Auth.Domain/
  FinancialTracker.Auth.Application/
  FinancialTracker.Auth.Infrastructure/
  FinancialTracker.Auth.API/
  FinancialTracker.Accounts.Domain/
  FinancialTracker.Accounts.Application/
  FinancialTracker.Accounts.Infrastructure/
  FinancialTracker.Accounts.API/
  FinancialTracker.Transactions.Domain/
  FinancialTracker.Transactions.Application/
  FinancialTracker.Transactions.Infrastructure/
  FinancialTracker.Transactions.API/
```

The original single-project app (`Finance tracker.sln` / `Finance tracker.csproj`) is unchanged; use `FinancialTracker.sln` for the modular monolith.
