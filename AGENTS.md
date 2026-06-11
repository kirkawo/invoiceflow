# Invoiceflow — Agent guide

## Project

Backend-first billing assistant for freelancers (invoices, reminders, payment tracking).

## Stack clues

- `.gitignore` is the standard **Visual Studio** template → project is **.NET / C#**.
- No `package.json`, `Cargo.toml`, `pyproject.toml`, etc. — not Node, Rust, or Python.
- No `.sln` or `.csproj` yet; repo is at initial commit with no code.

## Branches

- `main` / `develop` — standard Git Flow branching. Feature branches off `develop`.

## Conventions (inferred from .gitignore)

```
*.env           # no secrets in repo
node_modules/   # if any JS tooling is added later
```

## When scaffolding

- Use standard .NET project layout: `src/` for projects, `tests/` for test projects.
- Use file-scoped namespaces, implicit usings, and nullable reference types (current .NET conventions).
- Run `dotnet restore` after adding packages, `dotnet build` before committing.
