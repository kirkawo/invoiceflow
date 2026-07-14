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

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
