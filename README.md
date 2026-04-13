# Financial Tracker — Labs 3 & 4

Мікросервісна система з подієво-орієнтованою взаємодією та Transactional Outbox.

## Сервіси

| Сервіс | Порт | Опис |
|--------|------|------|
| `FinancialTracker.Auth.API` | 5001 | Реєстрація, логін, JWT |
| `FinancialTracker.Accounts.API` | 5002 | Рахунки та баланси + Kafka Consumer |
| `FinancialTracker.Transactions.API` | 5003 | Доходи/витрати + Outbox Relay → Kafka |

## Архітектура (ЛР №4)

```
┌────────────────────────┐         ┌─────────────────────────┐
│  Transactions.API      │         │  Accounts.API           │
│                        │         │                         │
│  POST /transactions/*  │  GET    │  GET /accounts/{id}     │
│        │               │ ───────►│  (validation only)     │
│        ▼               │         │         ▲               │
│        │               │         │         │ Kafka only   │
│  ┌───────────────┐     │         │  ┌──────────────────┐   │
│  │ TransactionDB │     │         │  │  AccountsDB      │   │
│  │  Transactions │     │         │  │  FinanceAccounts  │   │
│  │  OutboxMessages│    │         │  └──────────────────┘   │
│  └───────┬───────┘     │         │         ▲               │
│          │             │         │         │               │
│  ┌───────▼───────┐     │         │  ┌──────┴──────────┐    │
│  │ OutboxRelay   │     │         │  │ KafkaConsumer   │    │
│  │ (Background)  │     │         │  │ (Background)    │    │
│  └───────┬───────┘     │         │  └──────▲──────────┘    │
│          │             │         │         │               │
└──────────┼─────────────┘         └─────────┼───────────────┘
           │                                 │
           │      ┌──────────────┐           │
           └─────►│    Kafka     ├───────────┘
                  │  (topic:     │
                  │  transaction │
                  │  -events)    │
                  └──────────────┘
```

### Transactional Outbox Pattern

1. **Атомарний запис**: при створенні транзакції (income/expense) — `Transaction` та `OutboxMessage` зберігаються в одній БД-транзакції.
2. **Outbox Relay** (фоновий процес): кожні 5 секунд зчитує необроблені записи з таблиці `OutboxMessages` та надсилає їх у Kafka топік `transaction-events`.
3. **Kafka Consumer** (в Accounts.API): підписується на `transaction-events`, **єдине місце** де змінюється баланс (credit/debit). Запис у таблицю `ProcessedLedgerTransactions` забезпечує **ідемпотентність** (повторна доставка з Kafka не подвоює проведення).
4. **Перед збереженням транзакції** Transactions.API робить лише **HTTP GET** балансу в Accounts (перевірка існування рахунку та для expense — достатності коштів). Синхронних POST credit/debit з Transactions більше немає.

### Формат JSON-події

```json
{
  "transactionId": "550e8400-e29b-41d4-a716-446655440000",
  "accountId": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
  "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "transactionType": "Income",
  "amount": 500.00,
  "category": "Salary",
  "note": "Monthly salary",
  "occurredAtUtc": "2026-04-13T12:00:00Z"
}
```

## Що реалізовано

### ЛР №3
- Декомпозиція на 3 сервіси
- `Database per Service` (окремі Auth/Accounts/Transactions DB)
- Синхронний REST **лише для читання** (`GET` балансу) `Transactions → Accounts`; зміни балансу — через Kafka
- Resilience: timeout + retry + circuit breaker
- Передача `X-Correlation-ID` між сервісами

### ЛР №4
- Apache Kafka (KRaft mode) через Docker Compose
- Transactional Outbox: таблиця `OutboxMessages` у Transactions DB
- Outbox Relay: `BackgroundService` — polling кожні 5 сек, публікація в Kafka
- Kafka Consumer: `BackgroundService` в Accounts.API — обробка `TransactionCreated` подій
- JSON-формат повідомлень із чіткою структурою
- Kafka UI доступний на `http://localhost:8080`

## Передумови

- .NET 8 SDK
- Docker та Docker Compose (для Kafka)

## Змінні середовища

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

## Запуск

### 1. Запустити Kafka

```powershell
docker-compose up -d
```

Перевірити: `http://localhost:8080` (Kafka UI).

### 2. Запустити сервіси (3 окремі термінали)

```powershell
dotnet run --project src/FinancialTracker.Auth.API/FinancialTracker.Auth.API.csproj
dotnet run --project src/FinancialTracker.Accounts.API/FinancialTracker.Accounts.API.csproj
dotnet run --project src/FinancialTracker.Transactions.API/FinancialTracker.Transactions.API.csproj
```

Swagger:
- Auth: `http://localhost:5001/swagger`
- Accounts: `http://localhost:5002/swagger`
- Transactions: `http://localhost:5003/swagger`

## Тестування надійності (ЛР №4)

### Happy Path

1. Запустити Kafka (`docker-compose up -d`) і всі 3 сервіси.
2. Зареєструватися та отримати JWT через Auth.
3. Створити рахунок в Accounts.
4. Виконати income у Transactions.
5. Перевірити логи:
   - **Transactions**: `Published outbox message ... to Kafka topic 'transaction-events'`
   - **Accounts**: `Processing TransactionCreated event ... Successfully processed balance update`
6. Перевірити баланс рахунку — оновлений через подію.

### Reliability Test (Вимкнення брокера)

1. **Зупинити Kafka**:
   ```powershell
   docker-compose stop kafka
   ```

2. **Виконати бізнес-операцію** (POST income/expense в Transactions).
   - Транзакція та подія зберігаються в БД (outbox).
   - У логах Transactions: `Failed to publish outbox message ... — will retry on next poll`

3. **Увімкнути Kafka**:
   ```powershell
   docker-compose start kafka
   ```

4. **Перевірити автоматичну доставку**:
   - Outbox Relay автоматично підхоплює необроблені повідомлення.
   - У логах Transactions: `Published outbox message ... to Kafka`
   - У логах Accounts: `Processing TransactionCreated event ... Successfully processed balance update`
   - Баланс рахунку оновлюється коректно.

### Демонстрація відмовостійкості (ЛР №3)

1. Запустити всі 3 сервіси.
2. Отримати JWT через Auth.
3. Створити рахунок в Accounts.
4. Виконати income/expense у Transactions (happy path).
5. Зупинити Accounts.
6. Повторити виклик у Transactions.
7. Очікувано:
   - retries/timeouts у логах Transactions
   - спрацювання circuit breaker
   - відповідь `503 Accounts service temporarily unavailable`
   - `X-Correlation-ID` у заголовках і логах

## Структура проєкту

```
FinancialTracker.sln
docker-compose.yml                           ← Kafka (KRaft) + Kafka UI
src/
  FinancialTracker.Auth.Domain/
  FinancialTracker.Auth.Application/
  FinancialTracker.Auth.Infrastructure/
  FinancialTracker.Auth.API/

  FinancialTracker.Accounts.Domain/
  FinancialTracker.Accounts.Application/
  FinancialTracker.Accounts.Infrastructure/
  FinancialTracker.Accounts.API/
    Kafka/
      TransactionEventConsumer.cs             ← Kafka Consumer (BackgroundService)

  FinancialTracker.Transactions.Domain/
  FinancialTracker.Transactions.Application/
  FinancialTracker.Transactions.Infrastructure/
    Persistence/
      OutboxMessage.cs                        ← Outbox entity
      OutboxMessageEntityConfiguration.cs     ← EF configuration
      TransactionsDbContext.cs                ← includes OutboxMessages DbSet
    Repositories/
      TransactionRepository.cs               ← atomic save: Transaction + OutboxMessage
  FinancialTracker.Transactions.API/
    Outbox/
      OutboxRelayService.cs                   ← Outbox Relay (BackgroundService)
```

## Dashboard (Avalonia UI)

Десктопний додаток для управління всією системою через GUI:

```powershell
dotnet run --project src/FinancialTracker.Dashboard/FinancialTracker.Dashboard.csproj
```

**Можливості Dashboard:**
- **Infrastructure** — Start/Stop Kafka та кожного мікросервісу кнопками
- **API Demo** — Register, Login, Create Account, Income/Expense через UI-форми
- **Reliability Test** — покрокова демонстрація Outbox Pattern (Stop Kafka → Create Transaction → Start Kafka → Check Balance)
- **Logs** — real-time логи кожного сервісу

## Збірка

```powershell
dotnet build FinancialTracker.sln
```
