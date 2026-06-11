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

## Out of scope for MVP

- AI/ML features
- Cryptocurrency/blockchain
- Docker/containerization
- Entity Framework Core / Identity
- MediatR / FluentValidation
- Third-party payment gateways
