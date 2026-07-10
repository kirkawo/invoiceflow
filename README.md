# InvoiceFlow

Backend-first billing assistant for freelancers. Handles invoices, reminders, payment tracking, and PDF generation — so freelancers can focus on work, not admin.

## MVP features

- Owner authentication
- Client management
- Invoice creation and management
- PDF generation
- Public invoice sharing link
- Email delivery
- Payment status tracking
- Overdue payment reminders

## Architecture (layered)

| Layer | Project | Responsibility |
|---|---|---|
| **Domain** | `InvoiceFlow.Domain` | Core entities, enums, value objects |
| **Application** | `InvoiceFlow.Application` | Use cases, DTOs, interfaces |
| **Infrastructure** | `InvoiceFlow.Infrastructure` | Data access, email, external services |
| **Pdf** | `InvoiceFlow.Pdf` | PDF generation engine |
| **Api** | `InvoiceFlow.Api` | REST API (ASP.NET Core) |
| **Web** | `InvoiceFlow.Web` | Blazor web app |
| **UnitTests** | `InvoiceFlow.UnitTests` | xUnit unit tests |
| **ApiTests**  | `InvoiceFlow.ApiTests`  | xUnit integration tests |

## Current structure

```
InvoiceFlow.sln
src/
  InvoiceFlow.Api
  InvoiceFlow.Web
  InvoiceFlow.Application
  InvoiceFlow.Domain
  InvoiceFlow.Infrastructure
  InvoiceFlow.Pdf
tests/
  InvoiceFlow.UnitTests
docs/
```

## Local development

### Prerequisites

- .NET 8 SDK
- Docker (for PostgreSQL)

### Start the database

```sh
docker compose -f docker-compose.dev.yml up -d
```

PostgreSQL starts on host port **5433** (mapped to container port 5432). If another local project already uses port 5432, InvoiceFlow's DB will not conflict.

### Run the app

```sh
dotnet run --project src/InvoiceFlow.Api    # API on http://localhost:5232
dotnet run --project src/InvoiceFlow.Web    # Web on http://localhost:5217
```

### Connection notes

| Context        | Host                    | Port |
|----------------|-------------------------|------|
| Local dev (host→container) | `localhost`     | 5433 |
| Container→container        | `db` (service)  | 5432 |

The base `appsettings.json` connection strings use `localhost:5433` for host-side development. Production/staging override via `Database:ConnectionString` env var or `appsettings.Production.json`.

## CI

A GitHub Actions workflow runs on every push and pull request to `main` and `develop`:

- **Trigger**: `push` / `pull_request` on `main`, `develop`
- **Steps**:
  1. Restore NuGet packages
  2. Build solution in **Release** configuration
  3. Run all tests (unit + integration) — integration tests use in-memory repositories, no external Postgres required
  4. Upload test results as artifacts on failure
- **Docker smoke check**: On push to `main`, Docker images for the Api and Web apps are built to verify both Dockerfiles stay valid.
- **Secrets required**: None for CI. Tests run in `Testing` environment with safe defaults.

## Production deployment (Docker Compose)

The production stack runs three services: `db` (PostgreSQL 16 Alpine), `api` (the REST API), and `web` (the Blazor app).

```sh
# 1. Copy and fill in environment variables
cp .env.example .env
# Edit .env with your production values

# 2. Start the stack
docker compose -f docker-compose.prod.yml up -d

# 3. Check logs
docker compose -f docker-compose.prod.yml logs -f
```

### Health checks

Both the Api and Web services expose `GET /health` (returns `{"status":"healthy"}`) for container health checks and monitoring. The stack's startup order is `db → api → web` — Web waits for both db and api to be healthy before starting.

### Migration strategy

- The **Api** service runs EF Core migrations automatically on startup (both Development and Production).
- The **Web** service does **not** run migrations — it relies on the Api having applied them.
- Seed data (default workspace + admin user) only runs in `Development` environment.

### Environment variables

| Variable | Purpose |
|---|---|
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | PostgreSQL credentials |
| `API_BASE_URL` | Public-facing URL for the Api service |
| `WEB_BASE_URL` | Public-facing URL for the Web service |
| `APP_PUBLIC_BASE_URL` | Used for public invoice share links |
| `EMAIL_*` | SMTP configuration for email delivery |

### Ports (host → container)

| Service | Host port (default) | Container port |
|---|---|---|
| db     | — (internal only)   | 5432 |
| api    | 5000                | 8080 |
| web    | 5001                | 8080 |

### Data Protection

Authentication cookies and antiforgery tokens are encrypted using ASP.NET Core Data Protection. Keys are persisted on a Docker named volume (`dataprotection_keys`) shared between Api and Web, mounted at `/app/DataProtection-Keys` in each container. This keeps token decryption stable across restarts.

When switching between stacks or clearing volumes locally, delete browser cookies for `localhost:5000` and `localhost:5001` to avoid stale-key errors.

## Out of scope for MVP

- AI/ML features
- Cryptocurrency/blockchain
- Entity Framework Core / Identity
- MediatR / FluentValidation
- Third-party payment gateways
