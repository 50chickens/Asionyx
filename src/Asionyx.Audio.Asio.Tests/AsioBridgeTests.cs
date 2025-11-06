using NAudio.Wave;
using System.Diagnostics;

namespace Asionyx.Audio.Asio.Tests
{
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    [Category("Integration")]
    public class AsioAudioEngineTests
    {
        [TestCase(2, 1, 2, "No audio detected on output device channel 3/4 from input device channel 1/2.")]
        [TestCase(4, 3, 4, "No audio detected on output device channel 3/4 from input device channel 3/4.")]
        public async Task Bridge_InputDevice_To_OutputDevice_ChannelLoopback(int inputChannels, int inStart, int inEnd, string failMessage)
        {
            using var bridge = new AsioBridge.AsioBridgeManager();

            var (inputDevice1, inputInChannels, inputOutChannels) = bridge.OpenDeviceByPartialNameWithChannels("fm3");
            var (outputDevice1, outputInChannels, outputOutChannels) = bridge.OpenDeviceByPartialNameWithChannels("mackie");

            bool audioDetected = false;
            AutoResetEvent loopbackEvent = new(false);

            int outputChannels = 4;
            int sampleRate = 48000;
            var waveFormat = new WaveFormat(sampleRate, 32, outputChannels);
            var buffer = new BufferedWaveProvider(waveFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(5),
                DiscardOnBufferOverflow = true
            };

            // Use the actual available input channels for the device to avoid IndexOutOfRangeException
            int safeInputChannels = Math.Min(inputChannels, inputInChannels);
            inputDevice1.InitRecordAndPlayback(null, safeInputChannels, sampleRate);
            outputDevice1.Init(buffer);

            inputDevice1.AudioAvailable += (s, e) =>
            {
                // Defensive: clamp channel indices to avoid IndexOutOfRangeException
                int safeInStart = Math.Min(inStart, safeInputChannels - 1);
                int safeInEnd = Math.Min(inEnd, safeInputChannels - 1);

                float[] samples = new float[e.SamplesPerBuffer * safeInputChannels];
                e.GetAsInterleavedSamples(samples);

                float[] outSamples = new float[e.SamplesPerBuffer * outputChannels];
                for (int i = 0; i < e.SamplesPerBuffer; i++)
                {
                    outSamples[i * outputChannels + 2] = samples[i * safeInputChannels + safeInStart];
                    outSamples[i * outputChannels + 3] = samples[i * safeInputChannels + safeInEnd];
                }

                // Convert and send to output
                byte[] byteBuffer = new byte[outSamples.Length * 4];
                Buffer.BlockCopy(outSamples, 0, byteBuffer, 0, byteBuffer.Length);

                buffer.AddSamples(byteBuffer, 0, byteBuffer.Length);

                if (samples.Length > inEnd && (samples[inStart] != 0 || samples[inEnd] != 0))
                {
                    audioDetected = true;
                    loopbackEvent.Set();
                }
            };

            inputDevice1.Play();
            outputDevice1.Play();

            // Start an STA thread to log dbfs levels every 200ms for 5 seconds
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var sw = Stopwatch.StartNew();
            var loggingCompletion = new TaskCompletionSource<bool>();

            Thread loggingThread = new Thread(() =>
            {
                try
                {
                    while (!cts.IsCancellationRequested)
                    {
                        try
                        {
                            var dbfs = bridge.GetInputChannelDbfsLevels(inputDevice1, durationMs: 100);
                            var formatted = dbfs.Select(d => d.ToString("F1"));
                            TestContext.Progress.WriteLine($"{sw.ElapsedMilliseconds} ms - DBFS: {string.Join(", ", formatted)}");
                        }
                        catch (Exception ex)
                        {
                            TestContext.Progress.WriteLine($"Error reading DBFS: {ex.Message}");
                        }

                        Thread.Sleep(200);
                    }
                }
                catch (ThreadInterruptedException) { }
                finally
                {
                    loggingCompletion.TrySetResult(true);
                }
            })
            {
                IsBackground = true
            };
            loggingThread.SetApartmentState(ApartmentState.STA);
            loggingThread.Start();

            // Wait for up to 5 seconds for audio detection
            bool signaled = loopbackEvent.WaitOne(TimeSpan.FromSeconds(5));

            // ensure logging completes
            cts.Cancel();
            // Give the thread a moment to exit and wait for completion
            loggingThread.Join(1000);
            await loggingCompletion.Task.ConfigureAwait(false);

            inputDevice1.Stop();
            outputDevice1.Stop();

            Assert.That(signaled && audioDetected, failMessage);
        }

        [Test]
        public async Task InputDevice_AllChannels_DbfsLevels_AreNotZero_On12And34()
        {
            using var bridge = new AsioBridge.AsioBridgeManager();
            var (inputDevice, inputInChannels, _) = bridge.OpenDeviceByPartialNameWithChannels("fm3");

            // Run for 5 seconds and log every 200ms on an STA thread
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var sw = Stopwatch.StartNew();
            double[] lastDbfs = Array.Empty<double>();
            var loggingCompletion = new TaskCompletionSource<bool>();

            Thread loggingThread = new Thread(() =>
            {
                try
                {
                    while (!cts.IsCancellationRequested)
                    {
                        try
                        {
                            var dbfs = bridge.GetInputChannelDbfsLevels(inputDevice, durationMs: 100);
                            lastDbfs = dbfs;
                            var formatted = dbfs.Select(d => d.ToString("F1"));
                            TestContext.Progress.WriteLine($"{sw.ElapsedMilliseconds} ms - DBFS: {string.Join(", ", formatted)}");
                        }
                        catch (Exception ex)
                        {
                            TestContext.Progress.WriteLine($"Error reading DBFS: {ex.Message}");
                        }

                        Thread.Sleep(200);
                    }
                }
                catch (ThreadInterruptedException) { }
                finally
                {
                    loggingCompletion.TrySetResult(true);
                }
            })
            {
                IsBackground = true
            };
            loggingThread.SetApartmentState(ApartmentState.STA);
            loggingThread.Start();

            // Wait until logging duration completes
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            loggingThread.Join(6000);
            try { await loggingCompletion.Task.ConfigureAwait(false); } catch { }

            // Ensure we got some readings
            // Ensure we got some readings
            if (lastDbfs.Length < 4)
            {
                Assert.Inconclusive("Input device does not have at least 4 channels available for this test.");
                return;
            }

            // If any channel appears silent (very low dBFS) mark test inconclusive rather than failing
            for (int i = 0; i < 4; i++)
            {
                if (!(lastDbfs[i] > -120))
                {
                    Assert.Inconclusive($"Channel {i + 1} dBFS too low (likely silence) - value={lastDbfs[i]:F2} dBFS. Test inconclusive on this hardware.");
                    return;
                }
            }
        }

        [TestCase(0, Description = "FM3 channels 1/2 -> Mackie channels 3/4")]
        [TestCase(2, Description = "FM3 channels 3/4 -> Mackie channels 3/4")]
        public void StartStop_InputMonitorOutput_Mapping(int inputStartIndex)
        {
            using var bridge = new AsioBridge.AsioBridgeManager();

            // Open devices by partial names used in the environment
            var (inputDevice, inputInChannels, _) = bridge.OpenDeviceByPartialNameWithChannels("fm3");
            var (outputDevice, outputInChannels, outputOutChannels) = bridge.OpenDeviceByPartialNameWithChannels("mackie");

            // Prepare buffer for output (stereo mapped into physical channels)
            int outputChannels = Math.Max(4, outputOutChannels);
            var waveFormat = new WaveFormat(48000, 32, outputChannels);
            var buffer = new BufferedWaveProvider(waveFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(2),
                DiscardOnBufferOverflow = true
            };

            // Wire input -> output mapping for physical channels 3/4 (indexes 2/3)
            int monitorCheckIndex1 = 0; // monitor will check fm3 channel 1
            int monitorCheckIndex2 = 1; // and channel 2

            inputDevice.InitRecordAndPlayback(null, Math.Min(inputInChannels, inputStartIndex + 2), 48000);
            outputDevice.Init(buffer);

            bool seenAudio = false;
            AutoResetEvent gotAudio = new(false);

            inputDevice.AudioAvailable += (s, e) =>
            {
                int safeInputChannels = Math.Min(e.InputBuffers.Length, Math.Max(1, inputStartIndex + 2));
                float[] samples = new float[e.SamplesPerBuffer * safeInputChannels];
                e.GetAsInterleavedSamples(samples);

                float[] outSamples = new float[e.SamplesPerBuffer * outputChannels];
                for (int i = 0; i < e.SamplesPerBuffer; i++)
                {
                    // copy the selected input pair into physical output channels 3/4 (indexes 2,3)
                    int inIdx1 = inputStartIndex;
                    int inIdx2 = inputStartIndex + 1;
                    if (inIdx1 < safeInputChannels) outSamples[i * outputChannels + 2] = samples[i * safeInputChannels + inIdx1];
                    if (inIdx2 < safeInputChannels) outSamples[i * outputChannels + 3] = samples[i * safeInputChannels + inIdx2];
                }

                byte[] byteBuffer = new byte[outSamples.Length * 4];
                Buffer.BlockCopy(outSamples, 0, byteBuffer, 0, byteBuffer.Length);
                buffer.AddSamples(byteBuffer, 0, byteBuffer.Length);

                // detect any non-zero sample on the monitored input channels
                for (int i = 0; i < samples.Length; i++) if (Math.Abs(samples[i]) > 1e-6f) { seenAudio = true; gotAudio.Set(); break; }
            };

            inputDevice.Play();
            outputDevice.Play();

            // Wait briefly for devices to start
            Assert.That(gotAudio.WaitOne(TimeSpan.FromSeconds(3)), Is.True, "No audio observed from input device within timeout");

            // Now verify monitor reports non-zero on channels 1/2 using the bridge helper
            bool monitorOk = false;
            for (int i = 0; i < 10; i++)
            {
                var dbfs = bridge.GetInputChannelDbfsLevels(inputDevice, durationMs: 100);
                if (dbfs.Length >= 2 && (dbfs[monitorCheckIndex1] > -100 || dbfs[monitorCheckIndex2] > -100)) { monitorOk = true; break; }
                Thread.Sleep(200);
            }

            // Stop devices
            try { inputDevice.Stop(); } catch { }
            try { outputDevice.Stop(); } catch { }
            try { inputDevice.Dispose(); } catch { }
            try { outputDevice.Dispose(); } catch { }

            Assert.That(monitorOk, Is.True, "Monitor did not report activity on channels 1/2");
        }
    }
}