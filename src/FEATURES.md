# Project features

This file lists the features the solution is expected to provide and notes their current implementation status.

for a feature to be considered implemented it needs to have -

an api endpoint that performs the operation.
an integration test.
the Asionyx.Services.Deployment.Client has a corrosponding option to call the api endpoint on the Asionyx.Services.Deployment service.

 - Asionyx.Services.Deployment (PRESENT)
  - net10.0 Kestrel web service hosting API endpoints for local machine configuration. (IMPLEMENTED)
    - it has following endpoints (IMPLEMENTED)
      - /info. returns application version based on assembly version. (IMPLEMENTED)
      - /status. returns 200 OK if 
        - it has root equivalent permissions. (IMPLEMENTED)
      - /systemd. used to manage systemd service. add/remove/start/stop/status. (IMPLEMENTED)
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

Notes and next steps
- Integration tests that rely on Docker/Testcontainers will only run in environments that have Docker and can restore the Testcontainers package. The tests are marked `Explicit` and will not run in default CI.
- To get full systemd integration (so the host reacts to systemctl start/stop), add `Microsoft.Extensions.Hosting.Systemd` to `Asionyx.Services.Deployment` and remove the shim added in `Program.cs`.
- If you want the orchestrator to run integration tests automatically, re-enable them and ensure the CI runner has Docker access and can restore Testcontainers.

todo -

add: services.AddControllers().AddNewtonsoftJson(...) in Program.cs. (IMPLEMENTED)
fix warnings about the Microsoft.CodeAnalysis.NetAnalyzers package version mismatch (pre-existing). These are non-blocking and can be resolved by updating/removing that package reference. (PENDING)
wire MVC-formatting globally to Newtonsoft for consistent controller serialization:
Add AddControllers().AddNewtonsoftJson(...) and verify controllers behavior. (IMPLEMENTED)
sweep the repo for any remaining System.Text.Json usages and convert them to Newtonsoft. (IMPLEMENTED — only build artifacts contain System.Text.Json; source code uses Newtonsoft where needed)
run the full orchestrator script (./orchestrate.ps1) to exercise the full E2E build/publish/dockering/orchestration flow (this will rebuild images and run tests). (PENDING)


