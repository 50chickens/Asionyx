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

- The orchestrator (`orchestrate.ps1`) publishes projects outside the image; the Dockerfile copies published output into a minimal runtime image. (IMPLEMENTED)
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
    - The orchestrator (`orchestrate.ps1`) will generate an API key for integration runs and inject it into the container (`-e API_KEY=...`) and into the test process so integration tests authenticate correctly. (IMPLEMENTED)
    - Note: encryption-at-rest uses the ASP.NET Core Data Protection stack; the key-ring location and protection lifetime are the platform defaults. For production you should configure key persistence and rotation per your security policies.

  - Integrates with a systemd-style lifecycle so it can be started/stopped under systemd. (IMPLEMENTED)
  - The project calls `.UseSystemd()` and the `Microsoft.Extensions.Hosting.Systemd` package is referenced in the project file so real systemd integration is available when running on Linux hosts under systemd. (IMPLEMENTED)
  - When running in the provided Docker integration image we emulate systemd using the `Asionyx.Services.Deployment.SystemD` emulator. (IMPLEMENTED)
  - On startup the service attempts to invoke the systemd emulator CLI to start `Asionyx.Services.HelloWorld`. (IMPLEMENTED)

  - Diagnostics: a lightweight in-process diagnostics writer that persists structured JSON files for post-mortem inspection (e.g. `/var/asionyx/diagnostics/<name>.json`). This will be provided as an `IAppDiagnostics` and `FileDiagnostics` implementation that uses `Newtonsoft.Json` and atomic file writes. (PENDING)

- Asionyx.Library.Shared
  - Shared helper code. (PRESENT)

- Asionyx.Library.Core
  - Core interfaces (e.g. `ISystemConfigurator`). (PRESENT)

 - Asionyx.Services.Deployment.Docker (PRESENT)
  - Dockerfile that publishes `Asionyx.Services.Deployment`, `Asionyx.Services.Deployment.SystemD`, and `Asionyx.Services.HelloWorld` into a runtime image and runs both systemd emulator and deployment service as separate processes. (IMPLEMENTED)

- Asionyx.Services.Deployment.SystemD
  - A small systemd-like emulator that can run as a daemon in the container and has the same command line options as the real systemctl command line tool. instead of using systemctl status, systemctl start/stop, systemctl reload-daemon  etc the command is Asionyx.Services.Deployment.SystemD start/stop/daemon-reload. (IMPLEMENTED)
  - it also has the same configuration options as the real systemd. (IMPLEMENTED)
  - Asionyx.Services.Deployment.Tests
  -  - add a unit test for the middleware that is used to authenticate the api key. (IMPLEMENTED)
  - Asionyx.Services.Deployment.Tests
  -  - Integration test(s) that start the docker container and run tests against the service. there should be no tests that are ignored or skipped. all api endpoints have corresponding integration tests that perform the operations inside the application container. (IMPLEMENTED)
  -  - The integration test has been implemented to use the Docker CLI to start/stop the container and poll the `/info` endpoint (avoids the previous Testcontainers package dependency). This requires Docker to be available on the test runner. (IMPLEMENTED)
  -  - the integration test suite expects the orchestrator to inject `API_KEY` into the container and into the test process; the tests then send `X-API-KEY` on protected endpoints. Running the tests directly (without setting `API_KEY` in the test environment and passing it into the container) will cause authentication failures. (IMPLEMENTED)

 - Asionyx.Services.Deployment.Client
  - Console client to call the deployment service. (PRESENT)
  - it can call all of the endpoints that are hosted by the Asionyx.Services.Deployment service and with an --api-key (IMPLEMENTED)

- Asionyx.Services.Deployment.Client.Tests
  - Integration tests for the client which connect to the container and call endpoints (present). (PRESENT)
  - ensure all integration tests are using the testcontainers based tests. 
CI
 - GitHub Actions workflow to build and run tests (unit/explicit skipped). (ADDED: `.github/workflows/ci.yml`)

- Other implementation notes / infra changes made
- `orchestrate.ps1` was updated to fail-fast on `dotnet restore` / `dotnet build` failures so Docker image build is skipped if earlier steps fail. (IMPLEMENTED)
- Docker image build updated to use official Microsoft images (`mcr.microsoft.com/dotnet/sdk:9.0` and `mcr.microsoft.com/dotnet/aspnet:9.0`) to avoid apt dependency issues. (IMPLEMENTED)
- The image sets `ASPNETCORE_URLS=http://+:5000` so the deployment service listens on port 5000 inside the container. (IMPLEMENTED)
- `Asionyx.Services.Deployment.Docker/entrypoint.sh` line endings are normalized inside the image to avoid shebang CRLF errors on Linux. (IMPLEMENTED)
  - the CI progress should: (PENDING)

- Build policy: the container image must NOT build the solution inside the image. The repository `orchestrate.ps1` (or CI job) must perform a full `dotnet restore` / `dotnet build` / `dotnet publish` outside the image and the Dockerfile should copy only the published output into a minimal runtime image. (PENDING)

Notes and next steps
- Integration tests that rely on Docker/Testcontainers will only run in environments that have Docker and can restore the Testcontainers package. The tests are marked `Explicit` and will not run in default CI.
- To get full systemd integration (so the host reacts to systemctl start/stop), add `Microsoft.Extensions.Hosting.Systemd` to `Asionyx.Services.Deployment` and remove the shim added in `Program.cs`.
- If you want the orchestrator to run integration tests automatically, re-enable them and ensure the CI runner has Docker access and can restore Testcontainers.

todo -

add: services.AddControllers().AddNewtonsoftJson(...) in Program.cs. (IMPLEMENTED)
fix warnings about the Microsoft.CodeAnalysis.NetAnalyzers package version mismatch (pre-existing). These are non-blocking and can be resolved by updating/removing that package reference. (PENDING)
wire MVC-formatting globally to Newtonsoft for consistent controller serialization:
Add AddControllers().AddNewtonsoftJson(...) and verify controllers behavior. (IMPLEMENTED)
sweep the repo for any remaining JSON serializer usages and prefer `Newtonsoft.Json` for controller and diagnostics serialization. (IMPLEMENTED)
run the full orchestrator script (./orchestrate.ps1) to exercise the full E2E build/publish/dockering/orchestration flow (this will rebuild images and run tests). (PENDING)



```
testing requirements and workflow:

Asionyx.Services.Deployment.Docker - this creates a docker image (i'll call it the intergration test container) that only contains -
    - the published output of Asionyx.Services.Deployment. this is the docker container entrypoint. 
    - the published output of Asionyx.Services.Deployment.SystemD. it should be installed into the container, but should not be started as a seperate background process to Asionyx.Services.Deployment. it should be called by command line only from Asionyx.Services.Deployment to a) create a unit file for Asionyx.Services.HelloWorld. and b) configure the Asionyx.Services.HelloWorld service and start it. so that we can integration test adding/removing/starting/stopping systemd services.
    - the published output Asionyx.Services.HelloWorld. 

The integration test sequence should be:
    - the orchestration script runs.
    - it publishes the contents of Asionyx.Services.Deployment, Asionyx.Services.Deployment.SystemD and Asionyx.Services.Deployment.Client to a publish folder.
    - the integration test docker image is built. the contents of the 3 published folders only is copied into the container. 
    - the integration tests run. 
    - the integration test container is started using testcontainers.
    - Asionyx.Services.Deployment adds a new service called Asionyx.Services.HelloWorld using the Asionyx.Services.Deployment.SystemD CLI tool - it creates Asionyx.Services.Helloworld by creating a new systemd unit file and then calling Asionyx.Services.Deployment.SystemD to start it. 
    the Asionyx.Services.Hello work exposes an API endpoint - /info that we use to confirm the container has started. 
    the container is considered ready when we can call the /info endpoint.
    during the integration tests we should also call the /info endpoint of the deployment service named Asionyx.Services.Deployment to confirm it is also running. but this is not a requirement for the container ready section in the testcontainers startup.
    finally we remove the Asionyx.Services.HelloWorld service using Asionyx.Services.Deployment.SystemD CLI tool.
    the integration tests confirm that the service has been removed.
The orchestration script should clean up all published folders and docker images after the tests are complete.