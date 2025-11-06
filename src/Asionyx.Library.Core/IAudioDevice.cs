using NAudio.Wave;

namespace Asionyx.Library.Core;

public class AudioAvailableEventArgs : EventArgs
{
    public float[] Samples { get; }
    public int InputBuffersLength { get; }
    public int SamplesPerBuffer { get; }

    public AudioAvailableEventArgs(float[] samples, int inputBuffersLength, int samplesPerBuffer)
    {
        Samples = samples;
        InputBuffersLength = inputBuffersLength;
        SamplesPerBuffer = samplesPerBuffer;
    }

    public void GetAsInterleavedSamples(float[] destination)
    {
        if (destination == null) return;
        Array.Copy(Samples, destination, Math.Min(destination.Length, Samples.Length));
    }
}

public interface IAsioOutWrapper : IDisposable
{
    event EventHandler<AudioAvailableEventArgs>? AudioAvailable;
    int DriverInputChannelCount { get; }
    int DriverOutputChannelCount { get; }
    void InitRecordAndPlayback(IWaveProvider? playbackProvider, int inputChannels, int sampleRate);
    void Init(IWaveProvider playbackProvider);
    void Play();
    void Stop();
}

public interface IAsioFactory
{
    string[] GetDriverNames();
    IAsioOutWrapper Create(string driverName, int sampleRate = 48000);
}