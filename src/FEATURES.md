# Features

This file lists application features grouped by category.

## Audio
- Low-latency ASIO input/output device abstraction and wrappers — see [`src/Asionyx.Library.Asio/AsioWrapper.cs`](src/Asionyx.Library.Asio/AsioWrapper.cs:1)
- Realtime-safe capture/playback with buffered providers and thread-safe buffering
- Test/fake ASIO devices for UI test mode and CI — see [`src/Asionyx.Library.Asio/TestAsioFactory.cs`](src/Asionyx.Library.Asio/TestAsioFactory.cs:1)

## Routing & Channel Management
- Channel routing and mapping between physical device channels and logical streams — see [`src/Asionyx.Library.Asio/AudioRouter.cs`](src/Asionyx.Library.Asio/AudioRouter.cs:1)
- Per-channel stream wrapper / ChannelRoutingStream for processing and mapping

## Signal Generation & Analysis
- Built-in signal generator / test tones for diagnostics and testing — see [`src/Asionyx.Library.Asio/SignalGenerator.cs`](src/Asionyx.Library.Asio/SignalGenerator.cs:1)
- Spectrum analysis utilities for frequency-domain inspection — see [`src/Asionyx.Library.Spectrum/SpectrumAnalyzer.cs`](src/Asionyx.Library.Spectrum/SpectrumAnalyzer.cs:1)

## UI
- WinForms control panel for device selection, routing, monitoring and test mode — see [`src/Asionyx.Ui.Winforms/Form1.cs`](src/Asionyx.Ui.Winforms/Form1.cs:1)
- Real-time level meters, raw audio preview and test-tone controls
- Test-mode UI that injects fake devices for deterministic testing

## Logging
- Structured status and audio logging events emitted by core components
- Background diagnostics writer to offload disk I/O from realtime threads — see [`src/Asionyx.Library.Asio/Diagnostics.cs`](src/Asionyx.Library.Asio/Diagnostics.cs:1)

## Diagnostics & Monitoring
- LevelsUpdated and RawInputAvailable events for live monitoring
- Diagnostic capture writer that runs off the realtime thread
- Unit tests covering ASIO behavior and channel counts

## Service / Control Plane
- Kestrel .NET control-plane service with appsettings and Swagger for REST control — see [`src/Asionyx.Service/Program.cs`](src/Asionyx.Service/Program.cs:1)
- Cross-platform device management with DI for OS-specific implementations

## Dependency Injection & Composition
- Autofac module / library for wiring services and platform-specific implementations — see [`src/Asionyx.Library.Autofac/Asionyx.Library.Autofac.csproj`](src/Asionyx.Library.Autofac/Asionyx.Library.Autofac.csproj:1)

## Testing & CI
- Multiple unit/integration test projects (core, ASIO, spectrum) with fake-device tests for deterministic CI — see [`src/Asionyx.Library.Asio.Tests/`](src/Asionyx.Library.Asio.Tests/:1)
- Build orchestration and test runner script — see [`src/build.ps1`](src/build.ps1:1)
- GitHub Actions workflow for build+test — see [`.github/workflows/build-test.yml`](.github/workflows/build-test.yml:1)

## Configuration & Utilities
- appsettings.json configuration and environment-aware launch settings — see [`src/Asionyx.Service/appsettings.json`](src/Asionyx.Service/appsettings.json:1)
- Helpers for buffered audio, routing helpers and cross-project abstractions

## Notes
- Some types are intentionally split across projects to enable smaller runtime surfaces (core vs asio). Consolidation may reduce duplication.