using NAudio.Wave;
 
namespace Asionyx.Library.Core;
 
/// <summary>
/// Wrapper that routes audio to specific ASIO channels
/// </summary>
public class ChannelRoutingStream : IWaveProvider
{
    private readonly IWaveProvider sourceProvider;
    private readonly int startChannel;
    private readonly int totalOutputChannels;

    public ChannelRoutingStream(IWaveProvider sourceProvider, int startChannel, int totalOutputChannels)
    {
        this.sourceProvider = sourceProvider;
        this.startChannel = startChannel;
        this.totalOutputChannels = totalOutputChannels;
        
        // Create output format with all channels
        WaveFormat = new WaveFormat(
            sourceProvider.WaveFormat.SampleRate,
            sourceProvider.WaveFormat.BitsPerSample,
            totalOutputChannels
        );
    }

    public WaveFormat WaveFormat { get; }

    public int Read(byte[] buffer, int offset, int count)
    {
        // Calculate how many samples we need
        int bytesPerSample = WaveFormat.BitsPerSample / 8;
        int sourceChannels = sourceProvider.WaveFormat.Channels;
        int outputChannels = WaveFormat.Channels;
        
        // Clear the output buffer
        Array.Clear(buffer, offset, count);
        
        // Calculate how much source data we need
        int framesNeeded = count / (bytesPerSample * outputChannels);
        int sourceBytesNeeded = framesNeeded * bytesPerSample * sourceChannels;
        byte[] sourceBuffer = new byte[sourceBytesNeeded];
        
        // Read from source
        int sourceBytesRead = sourceProvider.Read(sourceBuffer, 0, sourceBytesNeeded);
        int framesRead = sourceBytesRead / (bytesPerSample * sourceChannels);
        
        // Route source channels to output channels starting at startChannel
        int sourceIndex = 0;
        int destIndex = offset;
        
        for (int frame = 0; frame < framesRead; frame++)
        {
            // Skip to start channel
            destIndex = offset + (frame * outputChannels * bytesPerSample) + (startChannel * bytesPerSample);
            
            // Copy source channels
            for (int ch = 0; ch < sourceChannels && (startChannel + ch) < outputChannels; ch++)
            {
                for (int b = 0; b < bytesPerSample; b++)
                {
                    buffer[destIndex++] = sourceBuffer[sourceIndex++];
                }
            }
            
            // If source has fewer channels than we're trying to write, skip the remaining source bytes
            if (sourceChannels + startChannel > outputChannels)
            {
                sourceIndex += (sourceChannels - (outputChannels - startChannel)) * bytesPerSample;
            }
        }
        
        return framesRead * outputChannels * bytesPerSample;
    }
}