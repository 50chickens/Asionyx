# Asionyx - ASIO Audio Router Development Guide

## Project Overview

Asionyx is a Windows-based audio routing application that enables real-time routing of ASIO audio between devices with low latency. The solution consists of multiple .NET 9.0 projects organized into libraries, services, and a WinForms UI.

**Key Components:**
- `Asionyx.Ui.Winforms`: Primary WinForms application for audio routing with device selection, channel mapping, volume controls, and audio meters
- `Asionyx.Library.Asio`: Core audio routing engine (`AudioRouter` class) that bridges ASIO input/output devices using NAudio
- `Asionyx.Audio.Core`: Shared interfaces (`IAsioFactory`, `IAsioOutWrapper`) for ASIO device abstraction and testability
- `Asionyx.Library.Spectrum`: Real-time spectrum analyzer for audio visualization
- `Asionyx.Service`: Windows background service (currently minimal implementation)

## Architecture Patterns

### ASIO Device Abstraction
The codebase uses an interface-based wrapper pattern to enable testing without physical ASIO hardware:

- `IAsioOutWrapper` wraps NAudio's `AsioOut` class and exposes `AudioAvailable` events with normalized `AudioAvailableEventArgs`
- `IAsioFactory` creates ASIO device instances - production uses `AsioFactory`, tests use `TestAsioFactory`
- `TestAsioFactory` provides fake devices (`FakeInputWrapper` generates sine waves, `FakeAsioOutWrapper` simulates output, `FakeMonitorWrapper` simulates monitoring)

**Testing ASIO code:** Always inject `IAsioFactory` and use `TestAsioFactory` in tests. Physical device names like "fm3" and "mackie" are referenced in integration tests but mocked via fakes.

### Audio Pipeline Flow
```
ASIO Input Device → AudioRouter.HandleAudioAvailable() → BufferedWaveProvider → ASIO Output Device
                                    ↓
                            RawInputAvailable event → SpectrumAnalyzer (UI)
                            LevelsUpdated event → Audio Meters (UI)
```

`AudioRouter` manages:
- Single-driver mode (same device for input/output) or separate input/output drivers
- Channel mapping (e.g., route input channels 1-2 to output channels 3-4)
- Volume control and muting via `InputVolume`, `OutputVolume`, `IsInputMuted`, `IsOutputMuted` properties
- Real-time level metering via `LevelsUpdated` event
- Background diagnostic WAV capture via `BackgroundDiagnosticWriter` when `EnableDiagnostics=true`

### Namespace Inconsistencies
The codebase has **mixed namespaces** due to iterative development:
- Modern code uses `Asionyx.*` namespaces (e.g., `Asionyx.Library.Asio`, `Asionyx.Audio.Core`)
- Older code uses `AsioAudioRouter`, `AsioAudioEngine`, `AsioBridge` namespaces
- `IAsio.cs` contains type aliases for backward compatibility: `using IAsioOutWrapper = Asionyx.Audio.Core.IAsioOutWrapper;`

**When adding new code:** Use `Asionyx.*` namespaces consistently. Avoid introducing more legacy namespace variations.

## Build & Test Workflow

### Standard Build Commands
```powershell
# From workspace root or src/ directory
.\src\build.ps1

# Manual steps (equivalent to build.ps1)
cd src
dotnet clean .\Asionyx.sln
dotnet build .\Asionyx.sln
dotnet test --no-restore .\Asionyx.sln
```

The `build.ps1` script auto-navigates to `src/` if not already there and fails fast on errors (`$LASTEXITCODE`).

### Test Framework
- All test projects use **NUnit** with `[Test]` and `[TestFixture]` attributes
- Integration tests marked with `[Category("Integration")]` and `[Apartment(System.Threading.ApartmentState.STA)]` for COM interop
- `Asionyx.Ui.Winforms.Tests` uses FlaUI for UI automation testing (targets .NET Framework 4.8)

### Running Specific Tests
```powershell
# Run only unit tests (exclude integration tests that require hardware)
dotnet test --filter "Category!=Integration"

# Run specific test project
dotnet test .\Asionyx.Library.Asio.Tests\Asionyx.Library.Asio.Tests.csproj
```

## Key Development Conventions

### Settings Persistence
- `AppSettings` class uses JSON serialization to `%APPDATA%\AsioAudioRouter\settings.json`
- Settings are loaded on startup and saved on changes
- If load fails, defaults are silently used (no crash)
- Generic `UIState` dictionary allows arbitrary control state persistence

### Audio Thread Safety
- Audio callbacks (`HandleAudioAvailable`) run on ASIO driver threads - **keep processing minimal**
- Use `ArrayPool<byte>.Shared` for temporary audio buffers to reduce GC pressure
- Events like `RawInputAvailable` and `LevelsUpdated` are raised from audio threads - subscribers must marshal to UI thread
- `StopRouting()` captures device references synchronously, then runs `Stop()`/`Dispose()` in background `Task.Run()` to avoid UI hangs

### Logging Pattern
- `AudioRouter` exposes two event streams: `StatusLogged` (general) and `AudioLogged` (audio-specific with throttling)
- `AudioLogged` throttles messages to max 100ms interval to prevent UI flooding (errors/diagnostics bypass throttle)
- Internal `Diagnostics.Log()` helper adds timestamps: `[HH:mm:ss.fff] message`
- Diagnostic WAV captures write to `%TEMP%\asio_routed_capture.wav`

### Channel Mapping Logic
The UI allows flexible mono/stereo channel selection (e.g., channels 1, 2, or 1-2, 3-4 for stereo). `AudioRouter.HandleAudioAvailable()` maps:
- Physical input channel index = `inputStartChannel + relativeChannelIndex`
- Physical output channel index = `outputStartChannel + relativeChannelIndex`
- Mono-to-stereo duplication: if `selectedInputChannels=1` and `selectedOutputChannels≥2`, duplicate mono to both L/R

## Common Pitfalls

1. **IndexOutOfRangeException in audio callbacks:** Always validate `inputDriverChannels` matches actual device capabilities before indexing `samples[]` array
2. **UI freezes on Stop:** Never call ASIO `Stop()`/`Dispose()` on UI thread - use `Task.Run()` pattern from `AudioRouter.StopRouting()`
3. **Missing TestAsioFactory in tests:** Integration tests will fail without physical ASIO devices unless `TestAsioFactory` is injected with `forceFake=true`
4. **Namespace confusion:** When referencing `IAsioFactory` or `IAsioOutWrapper`, always use `Asionyx.Audio.Core` namespace, not legacy aliases

## Diagnostic Troubleshooting

Enable diagnostic capture:
```csharp
audioRouter.EnableDiagnostics = true;
audioRouter.AudioLogged += (s, msg) => Console.WriteLine(msg);
```

This logs:
- Audio buffer parameters (input/output channel counts, samples per buffer)
- Active input/output physical channels with non-zero samples
- Diagnostic WAV file path (check audio content with Audacity/similar tool)

## GitHub Actions CI/CD

The repository uses `.github/workflows/build-test.yml` to run `build.ps1` on Windows with .NET 9.0.x on push to `main` or manual trigger.
