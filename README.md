# Financial Tracker - Lab 3 Microservices

Проєкт переведено на 3 окремі сервіси:

- `src/FinancialTracker.Auth.API`
- `src/FinancialTracker.Accounts.API`
- `src/FinancialTracker.Transactions.API`

`FinancialTracker.API` (старий композиційний host) повністю прибраний.

## Що реалізовано по ЛР №3

- декомпозиція на 3 сервіси;
- `Database per Service` (окремі `Auth/Accounts/Transactions` DB connection strings);
- синхронний REST виклик `Transactions -> Accounts`;
- resilience для міжсервісного клієнта: timeout + retry + circuit breaker;
- передача `X-Correlation-ID` між сервісами;
- контрольована відповідь `503`, якщо `Accounts` тимчасово недоступний.

## Потрібні змінні середовища

```powershell
$env:JWT_SECRET="YourSuperSecretKeyForJwtTokensMustBeAtLeast32CharactersLong!"
$env:JWT_ISSUER="FinancialTracker"
$env:JWT_AUDIENCE="FinancialTracker"
$env:JWT_EXPIRATION_HOURS="24"

$env:AUTH_DB_CONNECTION_STRING="Data Source=AuthDb;Mode=Memory;Cache=Shared"
$env:ACCOUNTS_DB_CONNECTION_STRING="Data Source=AccountsDb;Mode=Memory;Cache=Shared"
$env:TRANSACTIONS_DB_CONNECTION_STRING="Data Source=TransactionsDb;Mode=Memory;Cache=Shared"

$env:AccountsService__BaseUrl="http://localhost:5002"
```

## Запуск (3 окремі термінали)

```powershell
dotnet run --project src/FinancialTracker.Auth.API/FinancialTracker.Auth.API.csproj
dotnet run --project src/FinancialTracker.Accounts.API/FinancialTracker.Accounts.API.csproj
dotnet run --project src/FinancialTracker.Transactions.API/FinancialTracker.Transactions.API.csproj
```

Swagger:

- Auth: `http://localhost:5001/swagger`
- Accounts: `http://localhost:5002/swagger`
- Transactions: `http://localhost:5003/swagger`

## Демонстрація відмовостійкості

1. Запустити всі 3 сервіси.
2. Отримати JWT через Auth.
3. Створити рахунок в Accounts.
4. Виконати income/expense у Transactions (happy path).
5. Зупинити Accounts.
6. Повторити виклик у Transactions.
7. Очікувано:
   - retries/timeouts у логах Transactions;
   - спрацювання circuit breaker;
   - відповідь `503 Accounts service temporarily unavailable`;
   - `X-Correlation-ID` у заголовках і логах.

## Перевірка збірки

```powershell
dotnet build FinancialTracker.sln
```
# Financial Tracker - Lab 3 (Microservices)

This repository is now configured for **Lab #3** requirements:
- decomposition into independent services;
- synchronous REST interaction between services;
- resilience patterns (timeout, retry, circuit breaker);
- correlation ID propagation for distributed tracing;
- failure scenario demonstration.

## Services

- `FinancialTracker.Auth.API` - registration/login and JWT issuing.
- `FinancialTracker.Accounts.API` - accounts and balances.
- `FinancialTracker.Transactions.API` - income/expense operations.

Each service has its own DbContext and its own connection string:
- `AUTH_DB_CONNECTION_STRING`
- `ACCOUNTS_DB_CONNECTION_STRING`
- `TRANSACTIONS_DB_CONNECTION_STRING`

## Important architecture change

The old host-style composition project `FinancialTracker.API` was removed from the solution startup path.
`Transactions` no longer calls `Accounts` in-process via DI contracts; now it calls `Accounts` over HTTP:

- `POST /api/v1/internal/accounts/{accountId}/credit`
- `POST /api/v1/internal/accounts/{accountId}/debit`

## Resilience in Transactions -> Accounts call

`Transactions` uses typed `HttpClient` with:
- timeout;
- retry with backoff;
- circuit breaker.

Config section (in `src/FinancialTracker.Transactions.API/appsettings.json`):

```json
"AccountsService": {
  "BaseUrl": "http://localhost:5002",
  "TimeoutSeconds": 2,
  "RetryCount": 3,
  "RetryBaseDelayMs": 250,
  "CircuitBreakerFailureThreshold": 3,
  "CircuitBreakerBreakSeconds": 20
}
```

You can override by env var:
- `AccountsService__BaseUrl=http://localhost:5002`

## Correlation ID tracing

All three services use `X-Correlation-ID` middleware:
- reads incoming header or generates new ID;
- returns it in response header;
- adds correlation scope for logs.

During `Transactions -> Accounts` REST call, `X-Correlation-ID` is forwarded automatically.

## Prerequisites

- .NET 8 SDK

## Environment variables

Use values from `.env.example`:

```powershell
$env:JWT_SECRET="YourSuperSecretKeyForJwtTokensMustBeAtLeast32CharactersLong!"
$env:JWT_ISSUER="FinancialTracker"
$env:JWT_AUDIENCE="FinancialTracker"
$env:JWT_EXPIRATION_HOURS="24"
$env:AUTH_DB_CONNECTION_STRING="Data Source=AuthDb;Mode=Memory;Cache=Shared"
$env:ACCOUNTS_DB_CONNECTION_STRING="Data Source=AccountsDb;Mode=Memory;Cache=Shared"
$env:TRANSACTIONS_DB_CONNECTION_STRING="Data Source=TransactionsDb;Mode=Memory;Cache=Shared"
$env:AccountsService__BaseUrl="http://localhost:5002"
```

## Run services (3 terminals)

From repository root:

```powershell
dotnet run --project src/FinancialTracker.Auth.API/FinancialTracker.Auth.API.csproj
dotnet run --project src/FinancialTracker.Accounts.API/FinancialTracker.Accounts.API.csproj
dotnet run --project src/FinancialTracker.Transactions.API/FinancialTracker.Transactions.API.csproj
```

Default URLs:
- Auth: `http://localhost:5001/swagger`
- Accounts: `http://localhost:5002/swagger`
- Transactions: `http://localhost:5003/swagger`

## Reliability test scenario (Lab requirement)

1. Start all three services.
2. Register/login via Auth and get JWT.
3. Create an account in Accounts.
4. Execute income/expense from Transactions (happy path).
5. Stop Accounts service.
6. Call Transactions expense/income endpoint again.
7. Observe behavior:
   - retries/timeouts in Transactions logs;
   - circuit breaker eventually opens;
   - Transactions returns controlled error (`503 Accounts service temporarily unavailable`);
   - same `X-Correlation-ID` appears in request chain logs while Accounts is reachable.

## Build

```powershell
dotnet build FinancialTracker.sln
```

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
AUTH_DB_CONNECTION_STRING=Data Source=AuthDb;Mode=Memory;Cache=Shared
ACCOUNTS_DB_CONNECTION_STRING=Data Source=AccountsDb;Mode=Memory;Cache=Shared
TRANSACTIONS_DB_CONNECTION_STRING=Data Source=TransactionsDb;Mode=Memory;Cache=Shared
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
$env:AUTH_DB_CONNECTION_STRING="Data Source=AuthDb;Mode=Memory;Cache=Shared"
$env:ACCOUNTS_DB_CONNECTION_STRING="Data Source=AccountsDb;Mode=Memory;Cache=Shared"
$env:TRANSACTIONS_DB_CONNECTION_STRING="Data Source=TransactionsDb;Mode=Memory;Cache=Shared"
```

**Linux/Mac:**
```bash
export JWT_SECRET="YourSuperSecretKeyForJwtTokensMustBeAtLeast32CharactersLong!"
export JWT_ISSUER="FinancialTracker"
export JWT_AUDIENCE="FinancialTracker"
export JWT_EXPIRATION_HOURS="24"
export AUTH_DB_CONNECTION_STRING="Data Source=AuthDb;Mode=Memory;Cache=Shared"
export ACCOUNTS_DB_CONNECTION_STRING="Data Source=AccountsDb;Mode=Memory;Cache=Shared"
export TRANSACTIONS_DB_CONNECTION_STRING="Data Source=TransactionsDb;Mode=Memory;Cache=Shared"
```

See `.env.example` for reference.

**Note:** The database is SQLite in-memory — tables are created automatically on startup, and data resets when the application restarts.

## Run

```bash
cd "d:\Financial tracker"
dotnet run --project src/FinancialTracker.API/FinancialTracker.API.csproj
```

Open https://localhost:7001/swagger (Swagger UI) or https://localhost:7001/swagger/v1/swagger.json (OpenAPI JSON).

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
- Interactive API documentation at `/swagger`
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
