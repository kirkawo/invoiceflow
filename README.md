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
| **UnitTests** | `InvoiceFlow.UnitTests` | xUnit tests |

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
docker compose up -d
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

A GitHub Actions workflow runs on every push and pull request to `main`:

- **Trigger**: `push` / `pull_request` on `main`
- **Steps**:
  1. Restore NuGet packages
  2. Build solution in **Release** configuration
  3. Run all tests (unit + integration) — integration tests use in-memory repositories, no external Postgres required
  4. Upload test results as artifacts on failure
- **Docker smoke check**: On push to `main`, the Docker image for the Web app is built to verify the Dockerfile stays valid.
- **Secrets required**: None for CI. Tests run in `Testing` environment with safe defaults.

## Out of scope for MVP

- AI/ML features
- Cryptocurrency/blockchain
- Entity Framework Core / Identity
- MediatR / FluentValidation
- Third-party payment gateways
