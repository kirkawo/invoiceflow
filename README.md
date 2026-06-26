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
