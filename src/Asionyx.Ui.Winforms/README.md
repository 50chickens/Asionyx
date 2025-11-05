# ASIO Audio Router

A .NET Windows Forms application that allows you to route audio from one ASIO device to another in real-time.

## Features

- **Device Selection**: Select input and output ASIO devices from dropdown menus
- **Real-time Audio Routing**: Route audio from input device to output device with minimal latency
- **Status Monitoring**: Real-time status log showing device initialization, connection status, and any errors
- **Simple Controls**: Start and Stop buttons to control audio routing
- **Error Handling**: Comprehensive error handling with user-friendly messages

## Requirements

- Windows operating system
- .NET 9.0 or later
- ASIO-compatible audio devices with drivers installed

## Building the Application

```bash
dotnet build AsioAudioRouter.csproj
```

## Running the Application

```bash
dotnet run --project AsioAudioRouter.csproj
```

Or run the compiled executable:
```bash
.\bin\Debug\net9.0-windows\AsioAudioRouter.exe
```

## Usage

1. Launch the application
2. Select your input ASIO device from the "Input Device" dropdown
3. Select your output ASIO device from the "Output Device" dropdown
4. Click "Start" to begin routing audio
5. Monitor the status log for connection information
6. Click "Stop" to stop audio routing

## Technical Details

- Built with .NET 9.0 Windows Forms
- Uses NAudio.Asio library for ASIO audio handling
- Sample rate: 48kHz
- Bit depth: 16-bit
- Channels: Stereo (2 channels)
- Buffer duration: 2 seconds with overflow discard

## Notes

- Both input and output devices must support ASIO
- Ensure your ASIO drivers are properly installed and configured
- The application will automatically detect all available ASIO devices
- Audio routing happens in real-time with minimal latency
- The application properly releases all resources when stopped or closed

## Troubleshooting

- **No ASIO drivers found**: Install ASIO drivers for your audio devices
- **Device initialization errors**: Check that your audio devices are properly connected and not in use by other applications
- **Audio quality issues**: Try adjusting your ASIO driver settings (buffer size, sample rate) in your device's control panel