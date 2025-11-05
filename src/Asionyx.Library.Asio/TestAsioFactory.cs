using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using NAudio.Wave;
using Asionyx.Audio.Core;

namespace AsioAudioEngine;

// A fake ASIO output wrapper that reads from a provided IWaveProvider (typically BufferedWaveProvider)
// and exposes raw float samples via an internal SamplesAvailable event so a fake monitor can subscribe.
internal class FakeAsioOutWrapper : IAsioOutWrapper
{
    private IWaveProvider? playbackProvider;
    private Thread? readThread;
    private bool running;
    private int readIntervalMs = 20; // read ~50Hz
    public event EventHandler<AudioAvailableEventArgs>? AudioAvailable; // not used for output

    public event EventHandler<float[]?>? SamplesAvailable;

    public FakeAsioOutWrapper(int inputChannels = 0, int outputChannels = 4, int sampleRate = 48000)
    {
        DriverInputChannelCount = inputChannels;
        DriverOutputChannelCount = outputChannels;
        SampleRate = sampleRate;
    }

    public int DriverInputChannelCount { get; }
    public int DriverOutputChannelCount { get; }
    public int SampleRate { get; }

    public void InitRecordAndPlayback(IWaveProvider? playbackProvider, int inputChannels, int sampleRate) { /* no-op for fake output */ }
    public void Init(IWaveProvider playbackProvider)
    {
        this.playbackProvider = playbackProvider;
    }

    public void Play()
    {
        if (playbackProvider == null) return;
        running = true;
        readThread = new Thread(ReadLoop) { IsBackground = true };
        readThread.Start();
    }

    private void ReadLoop()
    {
        // We'll read small chunks and convert to floats and raise SamplesAvailable
        int bytesPerSample = (playbackProvider?.WaveFormat.BitsPerSample ?? 16) / 8;
        int channels = playbackProvider?.WaveFormat.Channels ?? DriverOutputChannelCount;
        int samplesPerRead = 256;
        byte[] buffer = new byte[samplesPerRead * channels * bytesPerSample];
        try
        {
            while (running)
            {
                int read = playbackProvider!.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    int frames = read / (bytesPerSample * channels);
                    float[] floats = new float[frames * channels];
                    if (bytesPerSample == 2)
                    {
                        for (int i = 0; i < frames * channels; i++)
                        {
                            int idx = i * 2;
                            if (idx + 1 >= read) break;
                            short s = (short)(buffer[idx] | (buffer[idx + 1] << 8));
                            floats[i] = s / (float)short.MaxValue;
                        }
                    }
                    else if (bytesPerSample == 4)
                    {
                        // assume float
                        Buffer.BlockCopy(buffer, 0, floats, 0, frames * channels * 4);
                    }

                    SamplesAvailable?.Invoke(this, floats);
                }
                Thread.Sleep(readIntervalMs);
            }
        }
        catch { }
    }

    public void Stop()
    {
        running = false;
        try { readThread?.Join(500); } catch { }
        readThread = null;
    }

    public void Dispose() { Stop(); }
}

// Fake monitor wrapper: subscribes to a FakeAsioOutWrapper.SamplesAvailable and produces AudioAvailable events
internal class FakeMonitorWrapper : IAsioOutWrapper
{
    private readonly FakeAsioOutWrapper source;
    private int samplesPerBuffer = 256;
    public event EventHandler<AudioAvailableEventArgs>? AudioAvailable;

    public FakeMonitorWrapper(FakeAsioOutWrapper source, int inputChannels, int outputChannels)
    {
        this.source = source;
        // Monitor exposes two input channels (left/right) mapped from source physical channels 3/4
        DriverInputChannelCount = 2;
        DriverOutputChannelCount = 0;
        source.SamplesAvailable += Source_SamplesAvailable;
    }

    private void Source_SamplesAvailable(object? sender, float[]? floats)
    {
        if (floats == null || floats.Length == 0) return;
        // Source floats are interleaved with sourceChannels equal to the FakeAsioOutWrapper.DriverOutputChannelCount
        int sourceChannels = source.DriverOutputChannelCount;
        if (sourceChannels <= 3)
        {
            // not enough channels on source, fall back to passing through as-is (attempt)
            int frames = floats.Length / Math.Max(1, sourceChannels);
            AudioAvailable?.Invoke(this, new AudioAvailableEventArgs(floats, Math.Max(1, sourceChannels), frames));
            return;
        }

        int framesCount = floats.Length / sourceChannels;
        // build 2-channel buffer from source channels 3 and 4 (indexes 2 and 3)
        float[] outFloats = new float[framesCount * 2];
        for (int f = 0; f < framesCount; f++)
        {
            int srcBase = f * sourceChannels;
            float ch3 = srcBase + 2 < floats.Length ? floats[srcBase + 2] : 0f;
            float ch4 = srcBase + 3 < floats.Length ? floats[srcBase + 3] : 0f;
            outFloats[f * 2] = ch3;
            outFloats[f * 2 + 1] = ch4;
        }

        AudioAvailable?.Invoke(this, new AudioAvailableEventArgs(outFloats, 2, framesCount));
    }

    public int DriverInputChannelCount { get; }
    public int DriverOutputChannelCount { get; }

    public void InitRecordAndPlayback(IWaveProvider? playbackProvider, int inputChannels, int sampleRate) { }
    public void Init(IWaveProvider playbackProvider) { }
    public void Play() { }
    public void Stop() { }
    public void Dispose() { }
}

// Fake input that generates a sine wave and raises AudioAvailable
internal class FakeInputWrapper : IAsioOutWrapper
{
    private Thread? t;
    private bool running;
    private double phase;
    public event EventHandler<AudioAvailableEventArgs>? AudioAvailable;

    public FakeInputWrapper(int inputChannels = 4, int outputChannels = 0, int sampleRate = 48000)
    {
        DriverInputChannelCount = inputChannels;
        DriverOutputChannelCount = outputChannels;
        SampleRate = sampleRate;
    }

    public int DriverInputChannelCount { get; }
    public int DriverOutputChannelCount { get; }
    public int SampleRate { get; }

    public void InitRecordAndPlayback(IWaveProvider? playbackProvider, int inputChannels, int sampleRate) { }
    public void Init(IWaveProvider playbackProvider) { }

    public void Play()
    {
        running = true;
        t = new Thread(Loop) { IsBackground = true };
        t.Start();
    }

    private void Loop()
    {
        int frames = 256;
        int channels = Math.Max(1, DriverInputChannelCount);
        while (running)
        {
            float[] samples = new float[frames * channels];
            for (int i = 0; i < frames; i++)
            {
                float v = (float)(Amplitude * Math.Sin(2.0 * Math.PI * 440.0 * phase / SampleRate));
                phase += 1.0;
                for (int ch = 0; ch < channels; ch++) samples[i * channels + ch] = v;
            }
            AudioAvailable?.Invoke(this, new AudioAvailableEventArgs(samples, channels, frames));
            Thread.Sleep(20);
        }
    }

    public void Stop() { running = false; try { t?.Join(200); } catch { } }
    public void Dispose() { Stop(); }

    // amplitude 0.1259 gives -18 dBFS RMS when channels = 2
    // amplitude = rms * sqrt(2)
    // using Amplitude ~0.178 to achieve ~-18 dBFS per channel
    public float Amplitude { get; set; } = (float)(0.125892541 / Math.Sqrt(0.5));
}

public class TestAsioFactory : IAsioFactory
{
    private readonly string[] realDrivers;
    private readonly ConcurrentDictionary<string, object> created = new();
    private readonly bool forceFake;

    public TestAsioFactory(bool forceFake = false)
    {
        this.forceFake = forceFake;
        try { realDrivers = AsioOut.GetDriverNames(); } catch { realDrivers = Array.Empty<string>(); }
    }

    public string[] GetDriverNames()
    {
        // If forcing fakes, advertise the fake names
        if (forceFake) return new[] { "fm3", "mackie", "monitor" };
        return realDrivers.Length > 0 ? realDrivers : new[] { "fm3", "mackie" };
    }

    public IAsioOutWrapper Create(string driverName, int sampleRate = 48000)
    {
        // If not forcing fakes and a real driver exists containing the name, return real wrapper
        if (!forceFake && realDrivers.Any(d => d.IndexOf(driverName, StringComparison.OrdinalIgnoreCase) >= 0))
        {
            return new AsioOutWrapper(driverName, sampleRate);
        }

        // Force fake behavior: create or return fakes for fm3 (input), mackie (output) and monitors
        string key = driverName.ToLowerInvariant();

        if (created.TryGetValue(key, out var existing)) return (IAsioOutWrapper)existing;

        if (key.Contains("mackie"))
        {
            var fake = new FakeAsioOutWrapper(0, 4, sampleRate);
            created[key] = fake;
            return fake;
        }

        if (key.Contains("fm3"))
        {
            // amplitude chosen so RMS ~ 0.1259 (-18 dBFS): amplitude = rms * sqrt(2)
            var fakeIn = new FakeInputWrapper(4, 0, sampleRate) { Amplitude = (float)(0.125892541 / Math.Sqrt(0.5)) };
            created[key] = fakeIn;
            return fakeIn;
        }

        if (key.Contains("monitor"))
        {
            // link to existing fake mackie if present, else create mackie first
            if (!created.TryGetValue("mackie", out var obj) || !(obj is FakeAsioOutWrapper mackieFake))
            {
                mackieFake = new FakeAsioOutWrapper(0, 4, sampleRate);
                created["mackie"] = mackieFake;
            }
            var mon = new FakeMonitorWrapper(mackieFake, mackieFake.DriverInputChannelCount, mackieFake.DriverOutputChannelCount);
            created[key] = mon;
            return mon;
        }

        // default: return a fake input
        var fallback = new FakeInputWrapper(2, 0, sampleRate) { Amplitude = (float)(0.125892541 / Math.Sqrt(0.5)) };
        created[key] = fallback;
        return fallback;
    }
}
