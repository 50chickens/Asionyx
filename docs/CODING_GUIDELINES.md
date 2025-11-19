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
# Asionyx Coding Guidelines (C# / .NET)

This document supplements `.editorconfig` with team-level conventions and rationale. It is intentionally pragmatic and focuses on things we care about for this project (services running on Linux).

## Goals

- Predictable, readable C# code.
- Consistent formatting across editors and CI.
- Favor small, testable services with clear DI boundaries.
- Keep container images small and reproducible.
- Add unit tests for high-priority modules.

## Formatting / tooling

- Use `.editorconfig` rules. Run `dotnet format` during development and in CI (`--verify-no-changes` in checks).
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
- `POST /systemd` — manage systemd services (body contains `{ action: "start"|"stop"|"status", name: "..." }`) — keep this behind appropriate auth in production

## Logging and Errors

- Use structured logging (`ILogger<T>`) and include context (request id, service name).
- Do not swallow exceptions silently. Log at appropriate levels and return a meaningful HTTP status.

## Dependency injection

- Register interfaces with small lifetime surface. Use `Singleton` for long-lived services (like system configurator), `Scoped` for request-scoped, `Transient` for lightweight ephemeral services.

## System-level operations

- System operations (service management, package install, file writes) should be behind `ISystemConfigurator` so they can be replaced by emulators in tests (the `SystemD` emulator project is an example).
- Avoid running operations as root in tests; use emulators and mocks where possible.

## Docker

- Use official `mcr.microsoft.com/dotnet` images for build and runtime to improve reproducibility.
- Expose only necessary ports and set `ASPNETCORE_URLS` to listen on the proper port (e.g., `http://+:5000`).
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

## Recommended Programming Techniques (practical, actionable)

These are practical techniques to improve robustness, readability, and testability across the repository. Apply them incrementally where they provide clear value.

- Use POCOs/DTOs instead of raw `string[]` or loosely-structured arrays.
  - Example: replace `StartUnit(string[] parts)` with `record StartUnitCmd(string UnitName);` and parse arguments into typed commands.

- Prefer strongly-typed configuration and options objects for paths and behavior.
  - Example: `class EmulatorOptions { public string UnitsDir { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "units"); }` and bind with `IOptions<EmulatorOptions>`.

- Convert entry points to async: `static async Task<int> Main(string[] args)` and `await` long-running operations instead of using `.GetAwaiter().GetResult()`.

- Encapsulate file and runtime state behind repositories/services.
  - Example: `IUnitRepository` (manages unit files) and `IServiceManager` (starts/stops services) so process logic and file IO can be mocked in tests.

- Return rich `CommandResult` objects instead of raw ints; include `ExitCode`, `StdOut`, `StdErr`.

- Use dependency injection, avoid global mutable state, and prefer small single-purpose classes.

- Secure process invocation:
  - Use `ProcessStartInfo` with explicit `FileName` and `ArgumentList` or `ArgumentEscaping`, set `RedirectStandardOutput/Error = true`, and pass a timeout/CancellationToken.

- Atomic file writes and safe PID handling:
  - Write to a temporary file then rename/move into place. When storing PIDs, additionally record command-line and start time to validate the PID on stop.

- Input validation and sanitization:
  - Strictly validate service names and file paths (e.g., `^[A-Za-z0-9._-]{1,64}$`) and sanitize values used in file paths.

- Use `System.CommandLine` or a small parser for CLI handling and auto-generated help text.

- Structured output for machine consumption:
  - Add a `--json` flag that returns a compact JSON object for tooling and controller consumption instead of relying on free-form stdout parsing.

- Prefer typed unit-file representations rather than `Dictionary<string, Dictionary<string,string>>`.
  - Example: `class UnitFile { public ServiceSection Service { get; set; } }` with a `ServiceSection` POCO.

- Make operations cancellable via `CancellationToken` and enforce sensible timeouts for long-running tasks.

- Add interfaces for side-effecting operations so tests can replace them with in-memory emulators.

- Capture and propagate errors as structured data; map to HTTP status codes at the API boundary, not in low-level libraries.

### String Processing & Text Parsing

Text parsing is a common source of brittle code, bugs, and performance issues. Apply these guidelines when working with strings and textual input:

- Prefer structured parsers over ad-hoc splitting:
  - Avoid brittle patterns like `var parts = s.Split(':'); var x = parts[1];` which can throw or silently mis-interpret malformed input.
  - Use `TryParse`, JSON parsers (`System.Text.Json`), or CSV libraries (`CsvHelper`) when the input has a standard format.

- Use typed results and POCOs instead of raw arrays:
  - Parse text into a `record` or class (e.g., `record KeyValue(string Key, string Value)`) so callers expect typed data instead of positional indexes.

- For simple splits prefer `IndexOf`/`AsSpan()` to avoid allocations and clearer checks:
  - Example: `var i = s.IndexOf(':'); if (i > -1) { var key = s.AsSpan(0,i).ToString(); var val = s.AsSpan(i+1).ToString(); }` — this avoids creating intermediate arrays and makes bounds explicit.

- Use `ReadOnlySpan<char>` / `Span<char>` for high-performance, allocation-free parsing in hot paths:
  - APIs like `Utf8Parser`, `MemoryExtensions` and `ReadOnlySpan<char>` let you parse numbers and slices without allocations.

- Prefer `TryParse` over `Parse` and avoid exceptions for control flow:
  - `int.TryParse`, `DateTime.TryParseExact`, etc. return a bool and avoid throwing on invalid input.

- Regex usage:
  - Use `Regex` with named capture groups for complex extractions rather than multiple `Split` + index steps.
  - Cache/compile reusable regexes as `static readonly Regex` with `RegexOptions.Compiled | RegexOptions.CultureInvariant` when the regex is used frequently. Be cautious: `Compiled` has JIT/startup cost and may not be beneficial for rarely-used patterns.
  - Always set a match timeout for user-controlled input: `new Regex(pattern, options, TimeSpan.FromMilliseconds(500))` to mitigate ReDoS risks.

- Avoid hand-rolled parsers for known formats — prefer libraries:
  - For CSV/TSV use a proven parser (`CsvHelper`) to handle quoting/escaping reliably.
  - For JSON use `System.Text.Json` or `Newtonsoft.Json` depending on requirements.

- Concatenation and building strings:
  - Use `StringBuilder` for incremental concatenation in loops. For single-shot known-length concatenation prefer `string.Create` or interpolated strings.

- Culture and formatting:
  - When parsing or formatting numbers/dates for machine protocols, use `CultureInfo.InvariantCulture` to avoid locale surprises.
  - When accepting user-facing input, explicitly document the expected culture or accept multiple formats and normalize.

- Defensive checks and input validation:
  - Always validate input length and shape before indexing into arrays returned by `Split`.
  - Prefer `Span`-based scanning or `IndexOf` checks to be explicit about boundaries.

- Performance vs readability:
  - Use `Span`/`Utf8Parser` for performance-critical code, but keep readability for non-hot paths. Measure before optimizing.

- Examples of migration (quick patterns):
  - From brittle split:
    - Bad: `var parts = s.Split(','); var a = parts[0]; var b = parts[1];`
    - Better: `if (TryParsePair(s, out var a, out var b)) { ... }` where `TryParsePair` uses `IndexOf`/`AsSpan` or `Regex`.

  - From repeated allocation to span:
    - Bad: `var token = s.Substring(start, len);`
    - Better (allocation-avoid): `var tokenSpan = s.AsSpan(start, len);` and use `tokenSpan` with `Utf8Parser` or copy to a buffer only when needed.

These rules reduce bugs, improve performance in hot paths, and make parsing logic more testable. When in doubt, prefer clarity first; optimize with `Span`/`Utf8Parser` only after profiling.

## Conventions summary

- Naming: `PascalCase` for public types and methods; `camelCase` for method parameters and local variables.
- Async methods should have `Async` suffix.
- Prefer nullable annotations (`Nullable enable`) and modern C# features.
- Avoid global state; prefer DI for dependencies.

## CI & Hygiene

- CI should include formatting (`dotnet format`), static analysis, build-and-test, and secret scanning.
- Do not commit `bin/` and `obj/` artifacts. Add them to `.gitignore` and remove from repository history if present.

## Practical Developer Commands

- Format: `dotnet format`
- Restore: `dotnet restore` or `dotnet restore Asionyx.sln`
- Build: `dotnet build -c Release`
- Test: `dotnet test -c Release`

---

If you want, I can now implement a small refactor example (non-invasive) in the SystemD emulator to demonstrate using a POCO for commands and an `IUnitRepository` interface — or I can just run a review pass for other files to identify the best first refactors.