# Features

- Audio
  - Multi-channel ASIO host (real + fake drivers)
  - Per-physical-channel routing, mixing and buffering
  - Channel mapping / expansion stream
  - Signal generator (sine IWaveProvider)
  - Buffered playback/record pipelines, float ↔ PCM interleaving

- ASIO integration & abstraction
  - IAsioFactory / IAsioOutWrapper interfaces for DI and testability
  - TestAsioFactory and fake wrappers for CI / non-Windows

- Routing, processing & analysis
  - AudioRouter: compose inputs, remap channels, drive playback and compute levels
  - Spectrum analysis (FFT) utilities
  - Per-channel level / meter computation and events

- Diagnostics & logging
  - Central Diagnostics logger and audio-throttled logging
  - Background WAV capture / diagnostic writer for traces

- UI
  - WinForms device enumeration, selection and control UI
  - Events for status, audio logs and level updates to drive UI

- Service / Control plane
  - Kestrel-based control-plane service with DI and Swagger-ready config
  - Cross-platform DI to select real vs test ASIO factories

- Testing & CI
  - Unit and integration tests across projects; fake ASIO drivers enable CI
  - PowerShell build script and GitHub Actions workflow

- Build & configuration
  - Multi-project solution layout and appsettings.json for services

- Extensibility & interfaces
  - Clear interface boundaries enabling adapters, DI and platform-specific implementations

References
- Audio core: [`src/Asionyx.Audio.Core/AudioRouter.cs`](src/Asionyx.Audio.Core/AudioRouter.cs:1)
- ASIO wrapper: [`src/Asionyx.Audio.Asio/AsioWrapper.cs`](src/Asionyx.Audio.Asio/AsioWrapper.cs:1)
- Test factory: [`src/Asionyx.Audio.Core/TestAsioFactory.cs`](src/Asionyx.Audio.Core/TestAsioFactory.cs:1)
- Spectrum: [`src/Asionyx.Library.Spectrum/SpectrumAnalyzer.cs`](src/Asionyx.Library.Spectrum/SpectrumAnalyzer.cs:1)
- Build script: [`src/build.ps1`](src/build.ps1:1)