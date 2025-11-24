# Project features

This file is the canonical list of project features, current status, and prioritized action items.

Summary
- The repository provides a small deployment API (`Asionyx.Services.Deployment`) plus a SystemD emulator (`Asionyx.Services.Deployment.SystemD`) and a sample `HelloWorld` service. Integration tests use a shared test harness to start a Docker image built by the orchestrator and validate endpoints (notably readiness via `/info`).

Core guarantees / rules
- Integration tests must not rely on container env vars except `API_KEY` (tests are responsible for injecting the API key).
- The orchestrator publishes projects and builds the Docker image (it does not start containers or inject API keys).
- Controllers return typed DTOs and non-sensitive errors (see `docs/CODING_GUIDELINES.md`).

Prioritized Actionable Plan

1. [Critical] Replace anonymous response objects with explicit DTOs (Controllers: Info, Files, Package, Packages, Systemd)
2. [Critical] Remove hard-coded file paths and magic strings (introduce `DeploymentOptions` / `EmulatorOptions` and bind via `IOptions<T>`)
3. [High] Make process invocation safe and cancellable (`IProcessRunner`, argument lists, timeouts, CancellationToken)
4. [High] Encapsulate side effects behind interfaces (`IFileSystem`, `IProcessRunner`, `IArchiveExtractor`, `IUploadStore`)
5. [High] Avoid synchronous blocking in startup and auth (remove `.GetAwaiter().GetResult()` usages)
6. [High] Strengthen API key handling and auth separation (`IApiKeyService`, constant-time comparison, header constant)
7. [Medium] Improve error handling and avoid leaking stack traces (centralized error middleware and `ErrorDto`)
8. [Medium] Centralize logging via `ILogger<T>`/`ILog<T>` and correlation propagation (`X-Correlation-ID` middleware)
9. [Medium] Add focused unit and integration tests for edge cases (file-permission, timeouts, malformed inputs)
10. [Low] Clean docs and remove duplicate/legacy content (consolidate `docs/CODING_GUIDELINES.md` and this file)
11. [Low] CI: enforce `dotnet format --verify-no-changes` and Roslyn analyzers as blocking checks
12. [Low] Use typed options via `IOptions<T>` for paths, upload limits, and timeouts

If you want, I can:
- Open small PRs for each change above (recommended: one PR per prioritized item)
- Add the CI workflow that enforces formatting and analyzers (blocked until you confirm)
- Continue implementing items 10..12 now (docs, CI, typed options)
```markdown
# Project features

This file lists the features the solution is expected to provide and notes their current implementation status.

for a feature to be considered implemented it needs to have -

an api endpoint that performs the operation.
an integration test.
the Asionyx.Services.Deployment.Client has a corrosponding option to call the api endpoint on the Asionyx.Services.Deployment service.

 - Asionyx.Services.Deployment (PRESENT)
  - net9.0 Kestrel web service hosting API endpoints for local machine configuration. (IMPLEMENTED)
    - it has following endpoints (IMPLEMENTED)
      - /info. returns application version based on assembly version. (IMPLEMENTED)
      - /status. returns 200 OK if 
        - it has root equivalent permissions. (IMPLEMENTED)
      - /systemd. used to manage systemd service. add/remove/start/stop/status. (IMPLEMENTED)

    - **Testing workflow requirement:** The deployment service must start and expose its API; the `/Systemd` endpoint of the deployment service will execute the `Asionyx.Services.Deployment.SystemD` CLI to manage the `Asionyx.Services.HelloWorld` project. Integration tests should drive the emulator via the deployment service (add, start, status, stop, remove). The container-ready signal for Testcontainers is that the tests can successfully connect to the `/info` endpoint of the `Asionyx.Services.HelloWorld` service after it has been started through the `/Systemd` endpoint. (REQUIREMENT)
      - **Integration test rules:**
        - Do not use environment variables inside integration tests running in the Docker container, except to pass the `API_KEY` for authenticating to the deployment service. Tests must not rely on any other env vars being set inside the container. (REQUIREMENT)
        - The systemd emulator will assume any application it manages is a .NET application. Unit/start logic should favour launching published .NET DLLs with `dotnet <dll>` or launched processes produced by `dotnet publish` as appropriate. (REQUIREMENT)
        - The `Asionyx.Services.HelloWorld` service must be started as a background process by the emulator (so the deployment container can continue running while HelloWorld runs). Use a robust process start (e.g., starting a detached/background process or `StartProcess`-style approach) rather than relying on TCP or socket forwarding. (REQUIREMENT)
        - Remove all TCP usage from the `SystemdController` and from any systemd CLI tools. The `/Systemd` endpoint must invoke the CLI locally (via process start) and return the result; the emulator CLI must be purely command-line and file-driven (no TCP socket listeners). (REQUIREMENT)
      - /packages. used to manage apt-get packages. add/remove/list. (IMPLEMENTED — runs apt-get inside container)
      - /filesystem/files. used to manage files. upload/download/add/edit/remove. (IMPLEMENTED — read/write/delete/list)
      - /package. endpoint that accepts a .nupkg file. this nupkg file should be uploaded to a temporary directory (not /tmp) and then unzipped. (IMPLEMENTED — extracts to /var/asionyx_uploads and returns manifest.json)

  - when nuget packages are unzipped looks for a file called manifest.json. (IMPLEMENTED)
  - each of the api endpoints except `/info` are protected by an API key header `X-API-KEY`. (IMPLEMENTED)
    - API key storage and lifecycle are now handled by a dedicated `IApiKeyService` which centralises the logic. (IMPLEMENTED)
    - The service prefers the environment variable `API_KEY` (highest precedence). If absent it will attempt to read and decrypt the API key from `/etc/asionyx_api_key`. (IMPLEMENTED)
    - If no key is present the service generates a random key and persists an encrypted copy to `/etc/asionyx_api_key` using ASP.NET Core Data Protection (encryption-at-rest). (IMPLEMENTED)
    - The orchestrator (`build-test-and-deploy.ps1`) publishes projects and builds the Docker image but does NOT start containers or inject `API_KEY` into the test process. Integration tests and the test harness are responsible for starting test containers and injecting `API_KEY` into the container at start time. (IMPLEMENTED)
    - Note: encryption-at-rest uses the ASP.NET Core Data Protection stack; the key-ring location and protection lifetime are the platform defaults. For production you should configure key persistence and rotation per your security policies.

  - Integrates with a systemd-style lifecycle so it can be started/stopped under systemd. (IMPLEMENTED)
  - The project calls `.UseSystemd()` and the `Microsoft.Extensions.Hosting.Systemd` package is referenced in the project file so real systemd integration is available when running on Linux hosts under systemd. (IMPLEMENTED)
  - When running in the provided Docker integration image we emulate systemd using the `Asionyx.Services.Deployment.SystemD` emulator. (IMPLEMENTED)
  - On startup the service attempts to invoke the systemd emulator CLI to start `Asionyx.Services.HelloWorld`. (IMPLEMENTED)

  - Diagnostics: a lightweight in-process diagnostics writer that persists structured JSON files for post-mortem inspection (e.g. `/var/asionyx/diagnostics/<name>.json`). This is provided as `IAppDiagnostics` and `FileDiagnostics` using `Newtonsoft.Json` and atomic file writes. (IMPLEMENTED)

- Asionyx.Library.Shared
  - Shared helper code. (PRESENT)

- Asionyx.Library.Core
  # Project features

  This file lists the features the solution is expected to provide and notes their current implementation status. It consolidates the documented expectations for the deployment service, integration test harness, packaging/orchestration, and CI considerations.

  For a feature to be considered implemented it should have:

  - an API endpoint that performs the operation;
  - an integration test that exercises the endpoint; and
  - a corresponding client option in `Asionyx.Services.Deployment.Client` where applicable.

  ---

  ## Implemented features (summary)

  - Asionyx.Services.Deployment (net9.0 Kestrel web service)
    # Project features

    This file lists the features the solution is expected to provide and notes their current implementation status. It mirrors the repository root `FEATURES.md` and documents expectations for the deployment service, integration test harness, packaging/orchestration, and CI considerations.

    For a feature to be considered implemented it should have:

    - an API endpoint that performs the operation;
    - an integration test that exercises the endpoint; and
    - a corresponding client option in `Asionyx.Services.Deployment.Client` where applicable.

    ---

    ## Implemented features (summary)

    - Asionyx.Services.Deployment (net9.0 Kestrel web service)
      - Endpoints implemented and covered by integration tests:
        - `GET /info` — returns application identity/version. (IMPLEMENTED)
        - `GET /status` — returns service status. (IMPLEMENTED)
        - `POST /systemd` — manage services (add/start/stop/remove/status). (IMPLEMENTED)
        - `POST|GET /packages` — install/list/remove apt packages. (IMPLEMENTED)
        - `POST|GET /filesystem/files` — write/read/delete files. (IMPLEMENTED)
        - `POST /package` — accepts a `.nupkg`, unpacks to uploads (uploads directory), returns `manifest.json`. (IMPLEMENTED)

    - Asionyx.Services.HelloWorld — sample service used by integration tests (exposes `/info`). (PRESENT)
    - Asionyx.Services.Deployment.SystemD — systemd-like emulator used by the deployment service to manage HelloWorld. (PRESENT)
    - Asionyx.Library.Core / Asionyx.Library.Shared — core interfaces and shared helpers used across projects. (PRESENT)
    - Asionyx.Services.Deployment.Client — console client that can call the deployment endpoints (PRESENT)

    ---

    ## Testing / integration rules and harness

    - Integration tests live in `Asionyx.Services.Deployment.IntegrationTests`. They use a Testcontainers-based harness to start/stop the image and exercise endpoints. Integration tests are marked `Category("Integration")`.
    - The harness responsibilities and behavior:
      - Starts the local image (built by the orchestrator) using Testcontainers and forwards container stdout/stderr into the NUnit console output for diagnostics.
      - Waits for the container to be reachable at `/info` (the HelloWorld service) and enforces a 60s readiness timeout; if `/info` is not reachable within 60s the test fails.
      - Provides shared helpers and fixtures (e.g., `IntegrationTestSetup`) that expose a shared `HttpClient`, `TestHostPort`, `ExecInContainerAsync`, `ReadFileFromContainerAsync`, and an `ExecResult` structure (`ExitCode`, `Stdout`, `Stderr`) for container exec calls.
      - Manages API-key injection into the container at start time; integration tests are responsible for providing `X-API-KEY` where required.
      - Removes reliance on `TEST_*` environment variables — tests must not rely on any container env vars except `API_KEY` for authentication.
      - Integration tests were reorganized to share a single test-wide container lifecycle and a shared `HttpClient` across endpoint-focused test classes (e.g., Info/Status/Systemd/Packages/Filesystem/PackageUpload tests).

    ---

    ## Orchestrator / Packaging / Runtime

    - `build-test-and-deploy.ps1` (orchestrator) publishes the required projects to `src/publish` (e.g., `deployment`, `systemd`, `helloworld`) and then builds the Docker image (`asionyx/deployment:local`) from the published output.
    - Important: the orchestrator builds the image only — it does NOT start containers nor inject API keys or other test environment settings into test processes. Container lifecycle and API key injection are handled by the integration test harness.
    - After integration tests complete the orchestrator ensures cleanup: it removes the built Docker image (if present) and deletes the publish folder.
    - Build policy: the image must not perform `dotnet restore`/`dotnet build` inside the image; all publishing is done outside and the Dockerfile copies published output into the runtime image.

    ---

    ## Systemd emulator and managed apps

    - The systemd emulator (`Asionyx.Services.Deployment.SystemD`) behaves as a CLI-driven emulator rather than a full systemd runtime. The deployment service invokes it to create unit files and to start/stop managed apps (HelloWorld).
    - Managed apps are launched as background/detached processes so the deployment container remains running while HelloWorld runs. The integration tests confirm HelloWorld `/info` as the readiness signal.

    ---

    ## Diagnostics & instrumentation

    - The service provides `IAppDiagnostics` / `FileDiagnostics` which persist structured JSON diagnostics for post-mortem analysis.
    - Controllers and diagnostics serialization use `Newtonsoft.Json` consistently across endpoints and diagnostics outputs.

    ---

    ## CI / notes

    - Integration tests require a Docker-capable runner and Testcontainers support; CI workflows must run integration tests only on runners with Docker access.
    - If CI should run the orchestrator end-to-end, ensure the runner has Docker permissions and can restore the Testcontainers package.

    ---

    ## Additional implementation notes

    - The integration test harness exposes improved diagnostics and structured results (e.g., `ExecResult`).
    - Hard-coded or ad-hoc `TEST_*` environment variables were removed from test code; tests use the harness fixtures/applied headers instead.
    - The image uses official Microsoft base images (`mcr.microsoft.com/dotnet/aspnet:9.0`) and sets `ASPNETCORE_URLS=http://+:5000` so services listen on port 5000 inside the container.

    ---

    ## Next steps / suggestions

    - Keep `Asionyx.Services.Deployment.IntegrationTests` as the authoritative source for integration scenarios that require Docker.
    - Consider updating the GitHub Actions workflow to run integration tests only on Docker-enabled runners, or gate them behind an explicit matrix run.
    - I can synchronize the repo root `FEATURES.md` with this file, open a PR for these documentation changes, or run a focused test or the orchestrator if you want validation.

    ## Improvements (Planned / In-Progress)

    - **Correlation ID middleware:** add a middleware that generates or forwards a `X-Correlation-ID` header for every request and ensures it is included in logs and forwarded to downstream calls.
    - **Health endpoint:** provide `GET /healthz` returning simple JSON `{ "status": "ok" }` and use it for lightweight health checks (distinct from `/info`).
    - **API-key enforcement integration test:** add an integration test that verifies endpoints (e.g., POST to `/packages` or POST to `/systemd`) return `401 Unauthorized` when `X-API-KEY` is missing and succeed when the correct key is provided by the test harness.
    - **CI gating for integration tests:** ensure integration tests only run on Docker-capable runners or behind a matrix flag; consider artifact upload on failure for diagnostics.
    - **Observability improvements:** ensure container stdout/stderr are forwarded to test logs (already implemented) and wire correlation-id into diagnostics outputs.

    These items are being implemented incrementally. Code changes for correlation-id middleware, `GET /healthz`, and the API-key enforcement integration test are in progress; one documentation patch was previously attempted and re-applied here.

  ## Code Review Action Items (prioritized)

  The following actionable plan is derived from the recent code review and the merged `docs/CODING_GUIDELINES.md`. Each item should be implemented with tests and a small migration PR.

  1. [Critical] Replace anonymous response objects with explicit DTOs
    - Affected controllers: `InfoController`, `FilesController`, `PackageController`, `PackagesController`, `SystemdController`.
    - Benefit: improves contracts, typing, serialization, discoverability and backward-compatibility.

  2. [Critical] Remove hard-coded file paths and magic strings
    - Examples: `/usr/local/bin/Asionyx.Services.Deployment.SystemD`, `/var/asionyx_uploads`, `/etc/asionyx_api_key`.
    - Action: introduce `DeploymentOptions` / `EmulatorOptions` and bind via `IOptions<T>`.

  3. [High] Make process invocation safe and cancellable
    - Implement `IProcessRunner` abstraction: use `ProcessStartInfo.ArgumentList`, async reads, timeouts and `CancellationToken` support.
    - Replace blocking `WaitForExit` and synchronous `ReadToEnd`.

  4. [High] Encapsulate side effects behind interfaces
    - Introduce `IFileSystem`, `IProcessRunner`, `IArchiveExtractor`, `IUploadStore` and inject into controllers/services.
    - Use in-memory/fake implementations in tests.

  5. [High] Avoid synchronous blocking in startup and auth
    - Remove `.GetAwaiter().GetResult()` usages in `Program.cs` and authentication handlers; prefer async initialization or graceful startup semantics.

  6. [High] Strengthen API key handling and auth separation
    - Centralize key retrieval/validation in `IApiKeyService`; avoid duplicate env/config reads in auth handler.
    - Use a constant for header name and constant-time comparison for secret validation.

  7. [Medium] Improve error handling and avoid leaking stack traces
    - Return typed error DTOs and sanitized messages to clients; log full exceptions at appropriate levels.
    - Standardize an error mapping strategy (exceptions -> HTTP status + error code).

  8. [Medium] Centralize logging via `ILogger<T>` and correlation propagation
    - Ensure all components use `ILogger<T>` and include `X-Correlation-ID` consistently in logs and diagnostics.

  9. [Medium] Add focused unit and integration tests for edge cases
    - Controller contract tests, malformed input, file-permission errors, process timeouts, and auth enforcement tests.

  10. [Low] Clean docs and remove duplicate/legacy content
     - Consolidate `docs/CODING_GUIDELINES.md` and sync these action items into `FEATURES.md`.

  11. [Low] CI: enforce `dotnet format --verify-no-changes` and Roslyn analyzers as blocking checks
     - Prevent style/regression churn and catch analyzer issues early.

  12. [Low] Use typed options via `IOptions<T>` for paths, upload limits, and timeouts
     - Bind a small `DeploymentOptions`/`EmulatorOptions` for uploads dir, exec path, diagnostics dir, and timeouts.

  Quick next steps (first 3 priorities):
  - Introduce DTOs for controllers and update integration/unit tests to assert contracts.
  - Extract configuration options for paths into typed options and read from `IConfiguration`/`IOptions<T>`.
  - Implement `IProcessRunner`, replace direct `Process` usage, add timeouts and cancellation handling.

  Files to update (suggested):
  - `docs/CODING_GUIDELINES.md`
  - `FEATURES.md`
  - Controllers and services listed above

  Each change should include tests and a small migration PR. Avoid crude in-place behavior changes without tests.
