# Asionyx Coding Guidelines (C# / .NET)

<!--
  Merged coding-guidelines derived from project review notes and Solution1 comparisons.
  This file is intentionally pragmatic and actionable; keep it short and put long-form rationale
  # Asionyx Coding Guidelines (C# / .NET)

  This document is the concise, canonical set of coding guidelines for Asionyx services.

  ## Goals
  - Predictable, readable C# code.
  - Consistent formatting and analyzer checks in CI.
  # Asionyx Coding Guidelines (C# / .NET)

  This file is the concise, canonical set of coding guidelines for Asionyx services. It focuses on conventions we enforce via CI and the most important developer practices for maintainable, testable .NET services.

  ## Goals
  - Predictable, readable C# code.
  - Consistent formatting and analyzer checks in CI.
  - Small, testable services with clear DI boundaries.

  ## Key Practices
  - Use explicit DTOs for controller requests/responses; avoid returning anonymous or internal domain types.
  - Encapsulate side effects (file IO, process invocation, system ops) behind interfaces so tests can replace them.
  - Prefer typed configuration via `IOptions<T>` and validate options at startup.
  - Avoid blocking on async (no `.GetAwaiter().GetResult()` in startup/auth).
  - Use structured logging (`ILogger<T>`) and include a correlation/request id in logs.

  ## Tooling & CI
  - Include an `.editorconfig` and run `dotnet format` locally and in CI (`--verify-no-changes`).
  - Enable Roslyn analyzers in `Directory.Build.props` and address warnings promptly.
  - CI should run: `dotnet format --verify-no-changes`, `dotnet restore`, `dotnet build`, and `dotnet test` on PRs and pushes.

  ## API Controllers
  - Keep controllers thin: validate input, map to domain/service calls, return appropriate status codes.
  - Use explicit model DTOs for requests/responses and map domain types inside services.

  ## Error Handling & Security
  - Do not return stack traces or exception details to API clients; use sanitized `ErrorDto` responses and log full diagnostics separately.
  - Never commit secrets. Use environment variables or secret stores.

  ## System Operations & Safety
  - Run system-level operations behind interfaces (e.g. `IProcessRunner`, `IUnitRepository`) so they can be emulator-backed in tests.
  - Make long-running operations cancellable via `CancellationToken` and enforce sensible timeouts.

  ## Tests
  - Unit tests should be fast and independent.
  - Integration tests that require Docker should be explicitly marked and run by CI jobs that have Docker available.

  ## Useful Commands
  - Format: `dotnet format`
  - Restore: `dotnet restore Asionyx.sln`
  - Build: `dotnet build -c Release`
  - Test: `dotnet test -c Release`

  ## Where to find more
  - Prioritized engineering work and project-specific feature notes live in `FEATURES.md` at the repository root.

  ---

  If you want, I can:
  - Add the prioritized action items to `FEATURES.md` as tracked tasks.
  - Implement a small, non-invasive refactor example in the SystemD emulator to demonstrate `IUnitRepository` and typed command objects.
