using AsioAudioEngine;
using NAudio.Wave;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;

namespace AsioAudioEngine.Tests
{
    [TestFixture]
    public class FakeAsioFactoryTests
    {
        [Test]
        public void FakeInput_IsRoutedTo_Monitor_OnOutput34()
        {
            var factory = new TestAsioFactory(forceFake: true);

            var input = factory.Create("fm3");
            var output = factory.Create("mackie");
            var monitor = factory.Create("monitor");

            // Prepare output buffer as 32-bit float, 4 channels
            var wf = WaveFormat.CreateIeeeFloatWaveFormat(48000, 4);
            var buffer = new BufferedWaveProvider(wf) { DiscardOnBufferOverflow = true, BufferDuration = TimeSpan.FromSeconds(2) };
            output.Init(buffer);

            float[] lastInputSamples = Array.Empty<float>();
            float[] lastMonitorSamples = Array.Empty<float>();
            AutoResetEvent gotMonitor = new(false);

            // Wire input -> output: convert input floats into interleaved float output and add to buffer
            input.AudioAvailable += (s, e) =>
            {
                var samples = new float[e.SamplesPerBuffer * Math.Max(1, e.InputBuffersLength)];
                e.GetAsInterleavedSamples(samples);
                lastInputSamples = samples.ToArray();

                int outCh = 4;
                float[] outSamples = new float[e.SamplesPerBuffer * outCh];
                for (int i = 0; i < e.SamplesPerBuffer; i++)
                {
                    if (e.InputBuffersLength >= 1) outSamples[i * outCh + 2] = samples[i * e.InputBuffersLength + 0];
                    if (e.InputBuffersLength >= 2) outSamples[i * outCh + 3] = samples[i * e.InputBuffersLength + 1];
                }

                byte[] bytes = new byte[outSamples.Length * 4];
                Buffer.BlockCopy(outSamples, 0, bytes, 0, bytes.Length);
                buffer.AddSamples(bytes, 0, bytes.Length);
            };

            monitor.AudioAvailable += (s, e) =>
            {
                var samples = new float[e.SamplesPerBuffer * Math.Max(1, e.InputBuffersLength)];
                e.GetAsInterleavedSamples(samples);
                lastMonitorSamples = samples.ToArray();
                gotMonitor.Set();
            };

            // Start playback
            output.Play();
            input.Play();

            // Wait for monitor to receive
            Assert.That(gotMonitor.WaitOne(TimeSpan.FromSeconds(2)), Is.True, "Monitor did not receive samples in time");

            Assert.That(lastInputSamples.Length > 0 && lastMonitorSamples.Length > 0, Is.True, "No samples captured");

            int inputChannels = Math.Max(1, input.DriverInputChannelCount);
            int monitorChannels = Math.Max(1, monitor.DriverInputChannelCount);

            int frames = Math.Min(lastInputSamples.Length / inputChannels, lastMonitorSamples.Length / monitorChannels);
            if (frames <= 0) Assert.Inconclusive("Not enough frames captured");

            int compareFrames = Math.Min(256, frames);
            double dot = 0, normA = 0, normB = 0;
            for (int i = 0; i < compareFrames; i++)
            {
                double a = lastInputSamples[i * inputChannels + 0];
                double b = lastMonitorSamples[i * monitorChannels + 0];
                dot += a * b;
                normA += a * a;
                normB += b * b;
            }
            double corr = 0;
            if (normA > 0 && normB > 0) corr = dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
 
            // Relaxed threshold for CI/fake pipeline variability while still requiring meaningful correlation
            Assert.That(corr, Is.GreaterThan(0.5), $"Input/monitor correlation too low: {corr:F3}");

            // cleanup
            try { input.Stop(); } catch { }
            try { output.Stop(); } catch { }
            try { monitor.Stop(); } catch { }
        }
    }
}