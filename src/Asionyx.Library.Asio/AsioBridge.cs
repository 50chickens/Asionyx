using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.Asio;

namespace AsioBridge
{
    public class AsioBridgeManager : IDisposable
    {
        private readonly List<IDisposable> _drivers = new();

        /// <summary>
        /// Opens an ASIO device by partial (case-insensitive) name match and returns device plus channel info.
        /// </summary>
        public (AsioOut device, int inputChannels, int outputChannels) OpenDeviceByPartialNameWithChannels(string partialName)
        {
            var driverName = AsioOut.GetDriverNames()
                .FirstOrDefault(n => n.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0);
            if (driverName == null)
                throw new InvalidOperationException($"ASIO device containing '{partialName}' not found.");
            var driver = new AsioOut(driverName);
            _drivers.Add(driver);

            int inputChannels = driver.DriverInputChannelCount;
            int outputChannels = driver.DriverOutputChannelCount;

            return (driver, inputChannels, outputChannels);
        }

        /// <summary>
        /// Gets the dBFS (decibels relative to full scale) level for each channel of the input device over a duration.
        /// This method is now safe to call repeatedly: it uses a temporary AsioOut instance for metering.
        /// </summary>
        public double[] GetInputChannelDbfsLevels(AsioOut inputDevice, int durationMs = 1000, int sampleRate = 48000)
        {
            // Use a temporary instance for metering to avoid re-initializing the main device
            string driverName = inputDevice.DriverName;
            using var tempDevice = new AsioOut(driverName);
            int inputChannels = tempDevice.DriverInputChannelCount;
            float[][] channelSamples = new float[inputChannels][];
            for (int c = 0; c < inputChannels; c++)
                channelSamples[c] = new float[0];

            bool done = false;
            int totalSamples = 0;
            tempDevice.InitRecordAndPlayback(null, inputChannels, sampleRate);

            tempDevice.AudioAvailable += (s, e) =>
            {
                float[] samples = new float[e.SamplesPerBuffer * inputChannels];
                e.GetAsInterleavedSamples(samples);

                for (int c = 0; c < inputChannels; c++)
                {
                    var ch = new float[e.SamplesPerBuffer];
                    for (int i = 0; i < e.SamplesPerBuffer; i++)
                        ch[i] = samples[i * inputChannels + c];
                    channelSamples[c] = channelSamples[c].Concat(ch).ToArray();
                }
                totalSamples += e.SamplesPerBuffer;
                if (totalSamples >= (sampleRate * durationMs / 1000))
                    done = true;
            };

            tempDevice.Play();

            var start = DateTime.UtcNow;
            while (!done && (DateTime.UtcNow - start).TotalMilliseconds < durationMs + 500)
            {
                System.Threading.Thread.Sleep(50);
            }

            tempDevice.Stop();

            double[] dbfs = new double[inputChannels];
            for (int c = 0; c < inputChannels; c++)
            {
                float max = channelSamples[c].Length > 0 ? channelSamples[c].Max(Math.Abs) : 0f;
                dbfs[c] = max > 0 ? 20 * Math.Log10(max) : double.NegativeInfinity;
            }
            return dbfs;
        }

        /// <summary>
        /// Bridges input device channels 1/2 to output device channels 3/4 using a buffer.
        /// </summary>
        public void BridgeInputToOutput(
            AsioOut inputDevice, AsioOut outputDevice,
            Action onAudioDetected)
        {
            int outputChannels = 4;
            int sampleRate = 48000;
            var waveFormat = new WaveFormat(sampleRate, 32, outputChannels);
            var buffer = new BufferedWaveProvider(waveFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(2),
                DiscardOnBufferOverflow = true
            };

            inputDevice.InitRecordAndPlayback(null, 2, sampleRate);
            outputDevice.Init(buffer);

            inputDevice.AudioAvailable += (s, e) =>
            {
                // Get input (channels 1/2)
                float[] samples = new float[e.SamplesPerBuffer * 2];
                e.GetAsInterleavedSamples(samples);

                // Prepare output buffer for output device (channels 3/4)
                float[] outSamples = new float[e.SamplesPerBuffer * outputChannels];
                for (int i = 0; i < e.SamplesPerBuffer; i++)
                {
                    outSamples[i * outputChannels + 2] = samples[i * 2];
                    outSamples[i * outputChannels + 3] = samples[i * 2 + 1];
                }

                // Convert float[] to byte[]
                byte[] byteBuffer = new byte[outSamples.Length * 4];
                Buffer.BlockCopy(outSamples, 0, byteBuffer, 0, byteBuffer.Length);

                buffer.AddSamples(byteBuffer, 0, byteBuffer.Length);

                // Signal audio detected if any non-zero sample
                if (onAudioDetected != null && samples.Any(smp => smp != 0))
                    onAudioDetected();
            };

            inputDevice.Play();
            outputDevice.Play();
        }

        public void Dispose()
        {
            foreach (var driver in _drivers)
            {
                driver.Dispose();
            }
            _drivers.Clear();
        }
    }
}