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

If you want, I can add examples and automated checks (pre-commit hooks, CI jobs) to enforce these rules.