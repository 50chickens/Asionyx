using NAudio.Wave;
using NAudio.Wave.Asio;
using System;
using Asionyx.Library.Core;

namespace Asionyx.Audio.Asio;

internal class AsioOutWrapper : IAsioOutWrapper
{
    private AsioOut _asio;
    public event EventHandler<AudioAvailableEventArgs>? AudioAvailable;

    public AsioOutWrapper(string driverName, int sampleRate = 48000)
    {
        _asio = new AsioOut(driverName);
        _asio.AudioAvailable += (s, e) =>
        {
            float[] samples = new float[e.SamplesPerBuffer * Math.Max(1, e.InputBuffers.Length)];
            e.GetAsInterleavedSamples(samples);
            AudioAvailable?.Invoke(this, new AudioAvailableEventArgs(samples, e.InputBuffers.Length, e.SamplesPerBuffer));
        };
    }

    public int DriverInputChannelCount => _asio.DriverInputChannelCount;
    public int DriverOutputChannelCount => _asio.DriverOutputChannelCount;

    public void InitRecordAndPlayback(IWaveProvider? playbackProvider, int inputChannels, int sampleRate) => _asio.InitRecordAndPlayback(playbackProvider, inputChannels, sampleRate);
    public void Init(IWaveProvider playbackProvider) => _asio.Init(playbackProvider);
    public void Play() => _asio.Play();
    public void Stop()
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _asio.Stop();
            sw.Stop();
            Diagnostics.Log($"AsioOutWrapper.Stop: took {sw.ElapsedMilliseconds} ms");
        }
        catch (Exception ex)
        {
            Diagnostics.Log($"AsioOutWrapper.Stop: error {ex.Message}");
        }
    }
    public void Dispose()
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _asio.Dispose();
            sw.Stop();
            Diagnostics.Log($"AsioOutWrapper.Dispose: took {sw.ElapsedMilliseconds} ms");
        }
        catch (Exception ex)
        {
            Diagnostics.Log($"AsioOutWrapper.Dispose: error {ex.Message}");
        }
    }
}

public class AsioFactory : IAsioFactory
{
    public string[] GetDriverNames() => AsioOut.GetDriverNames();
    public IAsioOutWrapper Create(string driverName, int sampleRate = 48000) => new AsioOutWrapper(driverName, sampleRate);
}