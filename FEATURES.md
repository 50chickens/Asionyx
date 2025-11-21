# Project features

This file lists the features the solution is expected to provide and notes their current implementation status.

for a feature to be considered implemented it needs to have -

- an api endpoint that performs the operation.
- an integration test.
- the Asionyx.Services.Deployment.Client has a corresponding option to call the api endpoint on the Asionyx.Services.Deployment service.

## Asionyx.Services.Deployment (PRESENT)

- net9.0 Kestrel web service hosting API endpoints for local machine configuration. (IMPLEMENTED)
  - it has following endpoints (IMPLEMENTED)
    - `/info` — returns application version based on assembly version. (IMPLEMENTED)
    - `/status` — returns 200 OK when the service has appropriate permissions. (IMPLEMENTED)
    - `/systemd` — manage systemd-style services: add/remove/start/stop/status. (IMPLEMENTED)
    - `/packages` — manage apt packages (install/remove/list). (IMPLEMENTED)
    - `/filesystem/files` — manage files (upload/download/write/read/delete). (IMPLEMENTED)
    - `/package` — accepts a `.nupkg`, extracts to uploads dir and returns `manifest.json`. (IMPLEMENTED)

### Testing workflow / requirements

- Integration tests drive the emulator via the deployment service (`/systemd`). The container-ready signal is that the `Asionyx.Services.HelloWorld` `/info` endpoint is reachable after being started via the emulator. (REQUIREMENT)
- Integration tests must not rely on any environment variables inside the container except `API_KEY` for authentication. (REQUIREMENT)
- The systemd emulator treats managed apps as .NET apps and launches published DLLs with `dotnet <dll>` or single-file executables as appropriate. (REQUIREMENT)
- Managed services are launched as background processes so the deployment container continues running. (REQUIREMENT)

### Security / API key

- All endpoints except `/info` are protected by `X-API-KEY`. (IMPLEMENTED)
- `IApiKeyService` centralises key lifecycle; service prefers `API_KEY` env var, otherwise reads `/etc/asionyx_api_key`, or generates and persists an encrypted key using ASP.NET Data Protection. (IMPLEMENTED)

## Packaging / Runtime

- The orchestrator (`build-test-and-deploy.ps1`) publishes projects outside the image; the Dockerfile copies published output into a minimal runtime image. (IMPLEMENTED)
- Build policy: the container image must NOT build the solution inside the image; publishing is done outside the image. (IMPLEMENTED)

## Diagnostics

- In-process diagnostics writer (`IAppDiagnostics`/`FileDiagnostics`) persists structured JSON for post-mortem inspection using `Newtonsoft.Json` and atomic writes. (IMPLEMENTED)

## CI / infra notes

- A GitHub Actions CI workflow exists to build and run tests (`.github/workflows/ci.yml`). The CI progress items are tracked separately. (ADDED)
- Fix warnings about `Microsoft.CodeAnalysis.NetAnalyzers` package mismatch have been addressed in the repo. (IMPLEMENTED)

## Development / next steps

- Run the full orchestrator script to exercise the full E2E flow (publish → build image → run container → run tests). (IMPLEMENTED — orchestrator run completed locally in workspace)
- If you want CI to run the orchestrator automatically, ensure the CI runner has Docker and permission to run the published workflow (tracked as a follow-up).

---

If you'd like, I can now:
- run the orchestrator again to re-validate, or
- push these documentation updates to a branch and open a PR, or
- update the CI workflow to run the orchestrator (requires adjusting CI permissions).
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

  This file lists the features the solution is expected to provide and notes their current implementation status.

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
      - `POST /package` — accepts a `.nupkg`, unpacks to uploads, returns `manifest.json`. (IMPLEMENTED)

  - Asionyx.Services.HelloWorld — small sample service used by integration tests (exposes `/info`). (PRESENT)
  - Asionyx.Services.Deployment.SystemD — systemd-like emulator used by the deployment service to manage HelloWorld. (PRESENT)
  - Asionyx.Library.Core / Asionyx.Library.Shared — core interfaces and shared helpers used across projects. (PRESENT)
  - Asionyx.Services.Deployment.Client — console client that can call the deployment endpoints (PRESENT)

  ---

  ## Testing / integration rules

  - Integration tests live in `Asionyx.Services.Deployment.IntegrationTests` and use a Testcontainers-based harness to start/stop the integration image and exercise endpoints. Tests are marked with `Category("Integration")`.
  - The harness forwards container stdout/stderr to the NUnit output to aid debugging.
  - Readiness: tests wait for the deployment image to become reachable at `/info`. The harness enforces a 60s readiness timeout for this endpoint.
  - API key rules:
    - Integration tests are responsible for injecting `X-API-KEY` into the container at start time; the orchestrator does NOT inject API keys.
    - Tests must not rely on any container environment variables except for `API_KEY` used for authentication.

  ---

  ## Orchestrator behavior

  - `build-test-and-deploy.ps1` publishes projects to `src/publish`, then builds the Docker image (`asionyx/deployment:local`).
  - Important: the orchestrator builds the image only — it does NOT start containers nor inject API keys into test processes.
  - After integration tests run, the orchestrator always removes the built image and cleans up published folders.

  ---

  ## Diagnostics & instrumentation

  - The service provides `IAppDiagnostics` / `FileDiagnostics` which persist structured JSON diagnostics for post-mortem analysis.
  - Controllers and diagnostics serialization use `Newtonsoft.Json` for consistent JSON formatting across endpoints and diagnostics files.

  ---

  ## CI / notes

  - Integration tests require a Docker-capable runner and Testcontainers support. In CI, ensure Docker is available and Testcontainers packages can be restored.
  - If CI should run the orchestrator, grant the runner the necessary Docker permissions and adjust the workflow accordingly.

  ---

  ## Next steps (suggested)

  - Keep `Asionyx.Services.Deployment.IntegrationTests` as the single source of truth for integration scenarios that require Docker.
  - Optionally update CI to run the orchestrator on Docker-enabled runners.
  - If you want, I can:
    - update `src/FEATURES.md` to match this consolidated content as well, or
    - open a PR with this change and a short changelog entry.
