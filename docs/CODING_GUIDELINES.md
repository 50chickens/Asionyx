# Asionyx Coding Guidelines (C# / .NET)

This document supplements `.editorconfig` with team-level conventions and rationale. It is intentionally pragmatic and focuses on things we care about for this project (services running on Linux).

## Goals

- Predictable, readable C# code.
- Consistent formatting across editors and CI.
- Favor small, testable services with clear DI boundaries.
- Keep container images small and reproducible.

## Formatting / tooling

- Use `.editorconfig` rules. Run `dotnet format` during development and in CI.
- Use Roslyn analyzers and StyleCop (configured in `Directory.Build.props`) for consistent code quality.

## Project structure

- Keep service projects small and focused. `Asionyx.Services.Deployment` should only host API/controller logic and delegate system operations to `ISystemConfigurator` implementations in `Asionyx.Library.Core` or `Asionyx.Library.Shared`.
- Tests go in `*.Tests` projects alongside the code they exercise.

## API Controllers

- Keep controllers thin: validate input, map to domain/service calls, return appropriate status codes.
- Use explicit model DTOs for requests/responses. Avoid returning internal domain objects from controllers.
- Endpoints should use RESTful verbs where possible.

Recommended endpoints (examples for `Asionyx.Services.Deployment`):
- `GET /info` — service metadata (version, environment)
- `GET /status` — simple health/status check (200 OK if the service is healthy)
- `POST /systemd` — manage systemd services (body contains { action: "start"|"stop"|"status", name: "..." }) — keep this behind appropriate auth in production

## Logging and Errors

- Use structured logging (ILogger) and include context (request id, service name).
- Do not swallow exceptions silently. Log at appropriate levels and return a meaningful HTTP status.

## Dependency injection

- Register interfaces with small lifetime surface. Use `Singleton` for long-lived services (like system configurator), `Scoped` for request-scoped, `Transient` for lightweight ephemeral services.

## System-level operations

- System operations (service management, package install, file writes) should be behind `ISystemConfigurator` so they can be replaced by emulators in tests (the `SystemD` emulator project is an example).
- Avoid running operations as root in tests; use emulators and mocks where possible.

## Docker

- Use official `mcr.microsoft.com/dotnet` images for build and runtime to improve reproducibility.
- Expose only necessary ports and set `ASPNETCORE_URLS` to listen on the proper port (`http://+:5000`).
- Normalize line endings for shell scripts to `LF` and mark them executable.

## Tests

- Unit tests should be fast and independent.
- Integration tests that require Docker should be marked explicit and run by orchestrator/CI in a Docker-enabled environment.

## Security

- Don’t commit secrets. Use environment variables or other secret stores for credentials.

## Useful commands

- Format: `dotnet format`
- Restore: `dotnet restore` or `dotnet restore Asionyx.sln`
- Build: `dotnet build -c Release`
- Test: `dotnet test -c Release`

## Conventions summary

- Naming: `PascalCase` for public types and methods; `camelCase` for method parameters and local variables.
- Async methods should have `Async` suffix.
- Avoid global state; prefer DI for dependencies.



# Asionyx Coding Guidelines (C# / .NET)

This document supplements .editorconfig with team-level conventions and rationale. It is intentionally pragmatic and focuses on things we care about for this project (services running on Linux).

Goals
- Predictable, readable C# code.
- Consistent formatting across editors and CI.
- Favor small, testable services with clear DI boundaries.
- Keep container images small and reproducible.

Formatting / tooling
- Use .editorconfig rules. Run `dotnet format` during development and in CI.
- Use Roslyn analyzers and StyleCop (configured in Directory.Build.props) for consistent code quality.

Project structure
- Keep service projects small and focused. `Solution1.Services.Deployment` should only host API/controller logic and delegate system operations to `ISystemConfigurator` implementations in `Solution1.Library.Core` or `Solution1.Library.Shared`.
- Tests go in `*.Tests` projects alongside the code they exercise.

API Controllers
- Keep controllers thin: validate input, map to domain/service calls, return appropriate status codes.
- Use explicit model DTOs for requests/responses. Avoid returning internal domain objects from controllers.
- Endpoints should use RESTful verbs where possible.

Recommended endpoints (examples for `Solution1.Services.Deployment`):
- GET /info - service metadata (version, environment)
- GET /status - simple health/status check (200 OK if the service is healthy)
- POST /system - manage systemd services (body contains { action: "start"|"stop"|"status", name: "..." }) - keep this behind appropriate auth in production

Logging and Errors
- Use structured logging (ILogger) and include context (request id, service name).
- Do not swallow exceptions silently. Log at appropriate levels and return a meaningful HTTP status.

Dependency Injection
- Register interfaces with a small lifetime surface. Use `Singleton` for long-lived services (like system configurator), `Scoped` for request-scoped, `Transient` for lightweight ephemeral services.

System-level operations
- System operations (service management, package install, file writes) should be behind `ISystemConfigurator` so they can be replaced by emulators in tests (the `SystemD` emulator project is an example).
- Avoid running operations as root in tests; use emulators and mocks where possible.

Docker
- Use official `mcr.microsoft.com/dotnet` images for build and runtime to improve reproducibility.
- Expose only necessary ports and set `ASPNETCORE_URLS` to listen on the proper port (e.g., `http://*:5000`).
- Normalize line endings for shell scripts to LF and mark them executable.

Tests
- Unit tests should be fast and independent.
- Integration tests that require Docker should be marked explicit and run by orchestrator/CI in a Docker-enabled environment.

Security
- Don't commit secrets. Use environment variables or other secret stores for credentials.

Useful commands
- Format: `dotnet format`
- Restore: `dotnet restore` or `dotnet restore Solution1.sln`
- Build: `dotnet build -c Release`
- Test: `dotnet test -c Release`

# Project1 Coding Practices

This document collects a comprehensive, pragmatic list of coding practices for .NET services and libraries used by Project1. Keep entries concise and actionable. Where possible, prefer conventions that are verifiable by CI tooling and unit tests.

## Project Structure & Modularity
- Keep projects small and focused. Separate runtime services, libraries, and tests into individual projects (e.g., `Project1.Library.Core`, `Project1.Services.Deployment`, `*.Tests`).
- Use clear project responsibilities: controllers should be thin, services contain business logic, libraries provide reusable abstractions.

## Documentation & Contributor Guidance
- Provide a `CONTRIBUTING.md` and readable `docs/CODING_GUIDELINES.md` describing conventions and rationale.
- Keep an up-to-date `README.md` with build and run steps.

## Formatting & Static Analysis
- Commit an `.editorconfig` and run `dotnet format` locally and in CI (`--verify-no-changes` in checks).
- Enable Roslyn analyzers and StyleCop rules in `Directory.Build.props` and treat warnings as build warnings (or errors in CI).

## Continuous Integration (CI)
- Build, test, and run code-quality checks on push and PRs. Include a dedicated code-quality workflow that runs `dotnet format`, analyzers, and tests.
- Fail the CI on formatting or analyzer rule violations unless explicitly suppressed with documented rationale.

## Testing
- Keep unit tests fast and deterministic. Use `*.Tests` projects near the code they exercise.
- Integration tests that require Docker or external services should be marked and run by orchestrator/CI in Docker-enabled runners.
- Supply `PublicProgram` or test hooks to allow `WebApplicationFactory` usage for server tests.

## Dependency Injection (DI)
- Use DI for all runtime dependencies. Register small-surface interfaces and avoid `new` in controllers/services for production dependencies.
- Use `Singleton` for long-lived services, `Scoped` for per-request services, and `Transient` for lightweight ephemeral services.

## Abstractions for System Operations
- Wrap system-level operations (file writes, process invocations, systemctl, package installs) behind interfaces (e.g., `ISystemConfigurator`) so they can be replaced by emulators or mocks in tests.

## Logging & Observability
- Use structured logging (`ILogger<T>`) everywhere. Include contextual fields (request id, correlation id, service name).
- Avoid `Console.WriteLine` in library code. Use console output only for thin CLI apps or during early startup while logging isn't configured.

## Error Handling & Exceptions
- Do not swallow exceptions silently. Log full exceptions at appropriate levels and return meaningful errors.
- Prefer typed exceptions for predictable error handling or return result objects with error details for library APIs.

## Async & Concurrency
- Use `async`/`await` consistently and suffix async methods with `Async`.
- Avoid blocking on async tasks (e.g., `GetAwaiter().GetResult()` or `.Result`). Initialize async resources in startup/host or expose async initialization APIs.
- Protect lazy async initialization with `SemaphoreSlim` or an `AsyncLazy` pattern to prevent races.

## Input Validation & Sanitization
- Validate all external inputs (HTTP body, CLI args) with strict rules. For service names, use a safe regex (e.g., `^[A-Za-z0-9._-]{1,64}$`).
- Sanitize values used in file paths or templates to prevent path traversal and template injection.

## Process & Command Execution
- Use `ProcessStartInfo`/`ProcessStartAsync` where available to avoid shell-escaping issues. Always validate arguments and add timeouts/cancellation for process execution.
- Capture stdout/stderr and log appropriately. Fail fast if `Process.Start()` fails and surface error details.

## Network IO & Timeouts
- Add cancellation tokens and timeouts for network reads/writes (TCP, HTTP). Avoid `Task.Delay` hacks for synchronization.
- Use framed protocols (newline-terminated messages or length-prefix) to avoid `ReadToEndAsync` deadlocks.

## Configuration Management
- Centralize configuration with `appsettings.json` and environment variable overrides. Bind strongly-typed settings with `IOptions<T>` or `IConfiguration.Get<T>()`.
- Keep templates in separate files (e.g., `templates/*.template`) instead of inline huge JSON strings for maintainability.

## Secrets & Data Protection
- Never commit secrets. Prefer environment variables or secret stores.
- When persisting secrets, encrypt them using `IDataProtector` or a secret manager and enforce file permissions (`chmod 600`).

## File & Binary Hygiene
- Do not commit `bin/` and `obj/` artifacts. Add them to `.gitignore` and remove from repository history if present.
- Keep generated files separate from source and clearly documented.

## Naming & Language Conventions
- `PascalCase` for public types and methods; `camelCase` for parameters and locals.
- Async methods should have `Async` suffix. Prefer nullable annotations (`Nullable enable`) and modern C# features.

## Performance & Resilience
- Add appropriate timeouts, backoff strategies for retries, and circuit-breakers for external dependencies.
- Prefer correctness over premature optimization; measure with benchmarks when needed.

## Security: File Permissions & Ownership
- When creating files under system locations (e.g., `/etc`), ensure proper ownership and restrictive permissions are applied after writing.

## CI Enforced Checks
- CI should include: formatting (`dotnet format`), static analysis, build-and-test, and optionally secret-scanning tools.

## Practical Developer Commands
- Format: `dotnet format`
- Restore: `dotnet restore` or `dotnet restore src/Project1.sln`
- Build: `dotnet build -c Release`
- Test: `dotnet test -c Release`

## Conventions summary
- Naming: `PascalCase` for public types and methods; `camelCase` for parameters and locals.
- Async methods should have `Async` suffix. Prefer nullable annotations (`Nullable enable`) and modern C# features.
- Avoid global state; prefer DI for dependencies.

- Practical Developer Commands
  - Format: `dotnet format`
  - Restore: `dotnet restore` or `dotnet restore src/Project1.sln`
  - Build: `dotnet build -c Release`
  - Test: `dotnet test -c Release`

  - Do not commit `bin/` and `obj/` artifacts. Add them to `.gitignore` and remove from repository history if present.
- Keep generated files separate from source and clearly documented.

- CI should include: formatting (`dotnet format`), static analysis, build-and-test, and optionally secret-scanning tools.