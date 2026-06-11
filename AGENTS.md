# InvoiceFlow — Agent guide

## Stack

- **.NET 8**, ASP.NET Core Web API + Blazor Web App, xUnit
- Layered: `Domain` → `Application` → (`Infrastructure` / `Pdf`) → (`Api` / `Web`)
- Branches: `main` / `develop` (feature branches off `develop`)
- `*.env` excluded; no secrets in repo

## Commands

```powershell
dotnet build InvoiceFlow.sln          # full solution build
dotnet test InvoiceFlow.sln           # run all tests
dotnet add reference <path>.csproj    # add project reference
```

## Conventions

- File-scoped namespaces, implicit usings, nullable enabled (set per project)
- No Class1.cs / demo boilerplate — keep `Program.cs` minimal
- New features go in the appropriate layer; Domain has no external deps
- Run `dotnet build` before committing

## Out of scope for MVP

No AI, crypto, Docker, EF Core, Identity, MediatR, FluentValidation, or payment gateways.
