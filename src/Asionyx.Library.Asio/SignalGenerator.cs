using NAudio.Wave;

namespace AsioAudioRouter;

public class SignalGenerator : IWaveProvider
{
    private readonly WaveFormat waveFormat;
    private double frequency;
    private double gain;
    private int sampleIndex;

    public SignalGenerator(int sampleRate, int channels)
    {
        // Use standard 16-bit PCM format for better ASIO compatibility
        waveFormat = new WaveFormat(sampleRate, 16, channels);
        frequency = 440.0;
        gain = 0.5; // Increased to 50% for better audibility
        sampleIndex = 0;
    }

    public double Frequency
    {
        get => frequency;
        set => frequency = value;
    }

    public double Gain
    {
        get => gain;
        set => gain = value;
    }

    public WaveFormat WaveFormat => waveFormat;

    public int Read(byte[] buffer, int offset, int count)
    {
        int samplesRequired = count / 2; // 2 bytes per 16-bit sample
        int channels = waveFormat.Channels;
        int samplesPerChannel = samplesRequired / channels;
        
        int bufferIndex = offset;
        
        for (int i = 0; i < samplesPerChannel; i++)
        {
            // Generate sine wave sample
            double time = sampleIndex / (double)waveFormat.SampleRate;
            double sampleValue = gain * Math.Sin(2 * Math.PI * frequency * time);
            
            // Convert to 16-bit integer
            short sample = (short)(sampleValue * short.MaxValue);
            
            // Write to all channels
            for (int ch = 0; ch < channels; ch++)
            {
                buffer[bufferIndex++] = (byte)(sample & 0xFF);
                buffer[bufferIndex++] = (byte)((sample >> 8) & 0xFF);
            }
            
            sampleIndex++;
        }
        
        return count;
    }
}