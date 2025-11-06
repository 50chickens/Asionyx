using NAudio.Wave;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Buffers;
using System.Threading.Tasks;
using Asionyx.Audio.Core;

namespace AsioAudioEngine;

public class AudioRouter : IDisposable
{
    private IAsioOutWrapper? asioInOut; // single instance when drivers are same
    private IAsioOutWrapper? asioInput; // separate instances when different
    private IAsioOutWrapper? asioOutput;
    private BufferedWaveProvider? waveBuffer;
    private BackgroundDiagnosticWriter? diagnosticWriter;
    private bool isRunning = false;
    private bool isInputRunning = false;
    private bool isOutputRunning = false;
    private int stopRequested = 0;

    // keep delegate references so we can unsubscribe reliably
    private EventHandler<AudioAvailableEventArgs>? asioInOutHandler;
    private EventHandler<AudioAvailableEventArgs>? asioInputHandler;

    public event EventHandler<string>? StatusLogged;
    // New audio-specific log category
    public event EventHandler<string>? AudioLogged;
    public event EventHandler<AudioLevelsEventArgs>? LevelsUpdated;
    // Expose raw interleaved input samples for UI-spectrum analysis (invoked on audio thread)
    public event EventHandler<AudioAvailableEventArgs>? RawInputAvailable;

    public bool EnableDiagnostics { get; set; } = true;
    public float InputVolume { get; set; } = 1.0f;
    public float OutputVolume { get; set; } = 1.0f;
    public bool IsInputMuted { get; set; } = false;
    public bool IsOutputMuted { get; set; } = false;

    // diagnostic flag
    private bool hasLoggedAudioParams = false;

    // audio log throttling (ticks)
    private readonly long audioLogIntervalTicks = TimeSpan.FromMilliseconds(100).Ticks;
    private long lastAudioLogTicks = 0;

    public void Log(string message) => StatusLogged?.Invoke(this, message);

    // Audio-specific logging helper - throttled to at most one message per 100ms
    public void LogAudio(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        // Always allow immediate important messages (errors, diagnostic capture notices)
        if (message.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0 || message.Contains("Diagnostic capture"))
        {
            AudioLogged?.Invoke(this, message);
            return;
        }

        long now = DateTime.UtcNow.Ticks;
        long last = Interlocked.Read(ref lastAudioLogTicks);
        if (last == 0 || now - last >= audioLogIntervalTicks)
        {
            Interlocked.Exchange(ref lastAudioLogTicks, now);
            AudioLogged?.Invoke(this, message);
        }
        // otherwise drop the message to avoid hammering the UI
    }

    private readonly IAsioFactory asioFactory;

    public AudioRouter() : this(new AsioFactory()) { }

    public AudioRouter(IAsioFactory asioFactory)
    {
        this.asioFactory = asioFactory;
        // route Diagnostics logger to our LogAudio so timestamps are included in UI logs
        Diagnostics.Logger = (m) => LogAudio(m);
    }

    public void StartRouting(string inputDriver, string outputDriver, int selectedInputChannels, int selectedOutputChannels, int inputStartChannel, int outputStartChannel)
    {
        StopRouting();

        if (string.Equals(inputDriver, outputDriver, StringComparison.Ordinal))
        {
            asioInOut = asioFactory.Create(inputDriver);

            int inCh = asioInOut.DriverInputChannelCount;
            int outCh = asioInOut.DriverOutputChannelCount;
            LogAudio($"Starting single-driver routing. inCh={inCh}, outCh={outCh}");

            int channelCount = outCh;
            WaveFormat wf = new WaveFormat(48000, 16, channelCount);
            waveBuffer = new BufferedWaveProvider(wf) { BufferDuration = TimeSpan.FromSeconds(2), DiscardOnBufferOverflow = true };

            if (EnableDiagnostics)
            {
                string file = Path.Combine(Path.GetTempPath(), "asio_routed_capture.wav");
                diagnosticWriter?.Dispose();
                diagnosticWriter = new BackgroundDiagnosticWriter(file, wf, LogAudio);
                LogAudio($"Diagnostic capture: {file}");
            }

            // store handler so we can unsubscribe on Stop
            asioInOutHandler = (s, e) => HandleAudioAvailable(e, selectedInputChannels, selectedOutputChannels, inputStartChannel, outputStartChannel);
            asioInOut.AudioAvailable += asioInOutHandler;

            // Use input channel count for InitRecordAndPlayback so recording is configured correctly
            LogAudio($"InitRecordAndPlayback called with inputChannels={inCh}, playbackChannels={channelCount}, sampleRate=48000");
            asioInOut.InitRecordAndPlayback(waveBuffer, inCh, 48000);

            // set running flags before starting playback so callbacks see the correct state
            isRunning = true;
            isInputRunning = true;
            isOutputRunning = true;
            asioInOut.Play();
            return;
        }

        asioInput = asioFactory.Create(inputDriver);
        asioOutput = asioFactory.Create(outputDriver);

        int inputDriverCh = asioInput.DriverInputChannelCount;
        int outDriverCh2 = asioOutput.DriverOutputChannelCount;
        LogAudio($"Starting separate drivers. inCh={inputDriverCh}, outCh={outDriverCh2}");

        int channelCountDefault = outDriverCh2;
        WaveFormat waveFormatDefault = new WaveFormat(48000, 16, channelCountDefault);
        waveBuffer = new BufferedWaveProvider(waveFormatDefault) { BufferDuration = TimeSpan.FromSeconds(2), DiscardOnBufferOverflow = true };

        if (EnableDiagnostics)
        {
            string file = Path.Combine(Path.GetTempPath(), "asio_routed_capture.wav");
            diagnosticWriter?.Dispose();
            diagnosticWriter = new BackgroundDiagnosticWriter(file, waveFormatDefault, LogAudio);
            LogAudio($"Diagnostic capture: {file}");
        }

        // store handler so we can unsubscribe on Stop
        asioInputHandler = (s, e) => HandleAudioAvailable(e, selectedInputChannels, selectedOutputChannels, inputStartChannel, outputStartChannel);
        asioInput.AudioAvailable += asioInputHandler;
        // use driver input channel count when initializing recording so ASIO provides correct buffers
        asioInput.InitRecordAndPlayback(null, inputDriverCh, 48000);

        asioOutput.Init(waveBuffer);

        // set running flags before starting playback so callbacks see the correct state
        isRunning = true;
        isInputRunning = true;
        isOutputRunning = true;

        asioInput.Play();
        asioOutput.Play();
    }

    private void HandleAudioAvailable(AudioAvailableEventArgs e, int selectedInputChannels, int selectedOutputChannels, int inputStartChannel, int outputStartChannel)
    {
        // Capture local references to avoid race conditions if StopRouting/StopInput/StopOutput nulled or disposed fields concurrently
        var bufferProvider = waveBuffer;
        if (bufferProvider == null || !(isRunning || isInputRunning)) return;

        byte[]? buffer = null;
        try
        {
            int inputDriverChannels = e.InputBuffersLength;
            int outDriverChannels = bufferProvider.WaveFormat?.Channels ?? selectedOutputChannels;
            int samplesPerBuffer = e.SamplesPerBuffer;

            // one-time diagnostic logging to help troubleshoot missing audio
            if (!hasLoggedAudioParams)
            {
                hasLoggedAudioParams = true;
                LogAudio($"AudioAvailable: inputDriverChannels={inputDriverChannels}, outDriverChannels={outDriverChannels}, samplesPerBuffer={samplesPerBuffer}");
                LogAudio($"Mapping: selectedInputChannels={selectedInputChannels}, selectedOutputChannels={selectedOutputChannels}, inputStart={inputStartChannel}, outputStart={outputStartChannel}");
            }

            float[] samples = new float[samplesPerBuffer * Math.Max(1, inputDriverChannels)];
            e.GetAsInterleavedSamples(samples);

            // Notify listeners with raw interleaved samples for spectrum analysis (non-blocking expected)
            try { RawInputAvailable?.Invoke(this, new AudioAvailableEventArgs(samples, inputDriverChannels, samplesPerBuffer)); } catch { }

            // prepare array to capture output physical channel activity while we build the buffer
            bool[] outActive = new bool[outDriverChannels];

            // Per-path diagnostic: check per-input-channel and per-output-physical-channel activity
            if (EnableDiagnostics)
            {
                // determine which input channels have any non-zero sample
                float[] maxAbsInput = new float[Math.Max(1, inputDriverChannels)];
                for (int i = 0; i < samplesPerBuffer; i++)
                {
                    for (int ch = 0; ch < inputDriverChannels; ch++)
                    {
                        int idx = i * inputDriverChannels + ch;
                        if (idx >= 0 && idx < samples.Length)
                        {
                            float a = Math.Abs(samples[idx]);
                            if (a > maxAbsInput[ch]) maxAbsInput[ch] = a;
                        }
                    }
                }

                // prepare array to capture output physical channel activity while we build the buffer
                // (already created above) -- we'll mark this during processing

                // Log initial summary (before mapping) for visibility
                var inputActiveList = new List<int>();
                for (int ch = 0; ch < inputDriverChannels; ch++) if (maxAbsInput[ch] > 1e-6f) inputActiveList.Add(ch + 1);
                LogAudio($"Path=(isRunning?\"FullDuplex\":isInputRunning?\"InputOnly\":isOutputRunning?\"OutputOnly\":\"Unknown\") - inputDriverChannels={inputDriverChannels}, outDriverChannels={outDriverChannels}, samplesPerBuffer={samplesPerBuffer}, activeInputChannels={ (inputActiveList.Count>0? string.Join(",", inputActiveList) : "none") }");
            }

            int bytesNeeded = samplesPerBuffer * 2 * outDriverChannels;
            buffer = ArrayPool<byte>.Shared.Rent(bytesNeeded);
            int offset = 0;

            float leftSum = 0f, rightSum = 0f;
            int leftCount = 0, rightCount = 0;

            // determine which physical channels correspond to the left/right meters
            int leftPhys = -1, rightPhys = -1;
            if (selectedOutputChannels >= 1) leftPhys = outputStartChannel;
            if (selectedOutputChannels >= 2) rightPhys = outputStartChannel + 1;

            for (int i = 0; i < samplesPerBuffer; i++)
            {
                for (int physCh = 0; physCh < outDriverChannels; physCh++)
                {
                    float sampleValue = 0f;

                    if (physCh >= outputStartChannel && physCh < outputStartChannel + selectedOutputChannels)
                    {
                        int relativeOutIndex = physCh - outputStartChannel;
                        if (relativeOutIndex < selectedInputChannels)
                        {
                            int inputIndex = inputStartChannel + relativeOutIndex;
                            if (inputIndex >= 0 && inputIndex < inputDriverChannels)
                            {
                                sampleValue = samples[i * inputDriverChannels + inputIndex];
                            }
                        }
                        else if (selectedInputChannels == 1 && selectedOutputChannels >= 2)
                        {
                            int inputIndex = inputStartChannel;
                            if (inputIndex >= 0 && inputIndex < inputDriverChannels)
                            {
                                sampleValue = samples[i * inputDriverChannels + inputIndex];
                            }
                        }
                    }

                    if (IsInputMuted) sampleValue = 0f; else sampleValue *= InputVolume;

                    // accumulate levels based on the physical channels chosen for left/right
                    if (physCh == leftPhys)
                    {
                        leftSum += sampleValue * sampleValue; leftCount++;
                    }
                    if (physCh == rightPhys)
                    {
                        rightSum += sampleValue * sampleValue; rightCount++;
                    }

                    int intSample = (int)(sampleValue * short.MaxValue);
                    if (intSample > short.MaxValue) intSample = short.MaxValue;
                    if (intSample < short.MinValue) intSample = short.MinValue;
                    short sampleShort = (short)intSample;

                    buffer[offset++] = (byte)(sampleShort & 0xFF);
                    buffer[offset++] = (byte)((sampleShort >> 8) & 0xFF);

                    // mark output physical channel active if we produced non-zero content
                    try { if (Math.Abs(sampleValue) > 1e-6f) outActive[physCh] = true; } catch { }
                }
            }

            int usedBytes = offset;

            if (IsOutputMuted || OutputVolume != 1.0f)
            {
                for (int i = 0; i < usedBytes; i += 2)
                {
                    if (i + 1 >= usedBytes) break;
                    short s = (short)(buffer[i] | (buffer[i + 1] << 8));
                    if (IsOutputMuted) s = 0; else if (OutputVolume != 1.0f)
                    {
                        int scaled = (int)(s * OutputVolume);
                        if (scaled > short.MaxValue) scaled = short.MaxValue;
                        if (scaled < short.MinValue) scaled = short.MinValue;
                        s = (short)scaled;
                    }
                    buffer[i] = (byte)(s & 0xFF);
                    buffer[i + 1] = (byte)((s >> 8) & 0xFF);
                }
            }

            // use captured provider reference
            bufferProvider.AddSamples(buffer, 0, usedBytes);

            // enqueue diagnostics to background writer
            if (EnableDiagnostics)
            {
                diagnosticWriter?.Enqueue(buffer, 0, usedBytes);
            }

            // After building buffer, if diagnostics are enabled log which physical output channels received non-zero samples
            if (EnableDiagnostics)
            {
                try
                {
                    var outActiveList = new List<int>();
                    for (int ch = 0; ch < outDriverChannels; ch++) if (outActive[ch]) outActiveList.Add(ch + 1);
                    LogAudio($"Output active physical channels: {(outActiveList.Count>0? string.Join(",", outActiveList) : "none")}");
                }
                catch { }
            }

            if (leftCount > 0)
            {
                float rms = (float)Math.Sqrt(leftSum / leftCount);
                float leftLevel = rms;
                float rightLevel = rightCount > 0 ? (float)Math.Sqrt(rightSum / rightCount) : 0f;
                try { Diagnostics.Log($"HandleAudioAvailable: levels computed L={leftLevel:F6} R={rightLevel:F6}"); } catch { }
                LevelsUpdated?.Invoke(this, new AudioLevelsEventArgs(leftLevel, rightLevel));
            }
            else
            {
                // ensure meters reset when no data
                try { Diagnostics.Log("HandleAudioAvailable: no samples, reporting zeros"); } catch { }
                LevelsUpdated?.Invoke(this, new AudioLevelsEventArgs(0f, 0f));
            }
        }
        catch (Exception ex)
        {
            LogAudio($"Audio handling error: {ex.Message}");
        }
        finally
        {
            if (buffer != null) try { ArrayPool<byte>.Shared.Return(buffer, true); } catch { }
        }
    }

    public void StopRouting()
    {
        // unset running flags first to help callbacks decide early
        isRunning = false;
        isInputRunning = false;
        isOutputRunning = false;

        // ensure we only start background stop once
        if (Interlocked.Exchange(ref stopRequested, 1) == 1) return;

        // Capture instances and handlers synchronously and clear fields to avoid race with StartRouting
        var capturedAsioInOut = Interlocked.Exchange(ref asioInOut, null);
        var capturedAsioInOutHandler = Interlocked.Exchange(ref asioInOutHandler, null);
        var capturedAsioInput = Interlocked.Exchange(ref asioInput, null);
        var capturedAsioInputHandler = Interlocked.Exchange(ref asioInputHandler, null);
        var capturedAsioOutput = Interlocked.Exchange(ref asioOutput, null);
        var capturedDiagnosticWriter = Interlocked.Exchange(ref diagnosticWriter, null);
        waveBuffer = null;

        // Run the heavy stop work off the calling thread to avoid UI hangs
        Task.Run(() =>
        {
            try
            {
                if (capturedAsioInOut != null)
                {
                    Diagnostics.Log("StopRouting (bg): removing asioInOut handler");
                    try { if (capturedAsioInOutHandler != null) capturedAsioInOut.AudioAvailable -= capturedAsioInOutHandler; } catch { }
                    Diagnostics.Log("StopRouting (bg): calling asioInOut.Stop()");
                    try { capturedAsioInOut.Stop(); Diagnostics.Log("StopRouting (bg): asioInOut.Stop() returned"); } catch (Exception ex) { Diagnostics.Log($"StopRouting (bg): asioInOut.Stop() error: {ex.Message}"); }
                    Diagnostics.Log("StopRouting (bg): disposing asioInOut");
                    try { capturedAsioInOut.Dispose(); Diagnostics.Log("StopRouting (bg): asioInOut.Dispose() returned"); } catch (Exception ex) { Diagnostics.Log($"StopRouting (bg): asioInOut.Dispose() error: {ex.Message}"); }
                }

                if (capturedAsioInput != null)
                {
                    Diagnostics.Log("StopRouting (bg): removing asioInput handler");
                    try { if (capturedAsioInputHandler != null) capturedAsioInput.AudioAvailable -= capturedAsioInputHandler; } catch { }
                    Diagnostics.Log("StopRouting (bg): calling asioInput.Stop()");
                    try { capturedAsioInput.Stop(); Diagnostics.Log("StopRouting (bg): asioInput.Stop() returned"); } catch (Exception ex) { Diagnostics.Log($"StopRouting (bg): asioInput.Stop() error: {ex.Message}"); }
                    Diagnostics.Log("StopRouting (bg): disposing asioInput");
                    try { capturedAsioInput.Dispose(); Diagnostics.Log("StopRouting (bg): asioInput.Dispose() returned"); } catch (Exception ex) { Diagnostics.Log($"StopRouting (bg): asioInput.Dispose() error: {ex.Message}"); }
                }

                if (capturedAsioOutput != null)
                {
                    Diagnostics.Log("StopRouting (bg): calling asioOutput.Stop()");
                    try { capturedAsioOutput.Stop(); Diagnostics.Log("StopRouting (bg): asioOutput.Stop() returned"); } catch (Exception ex) { Diagnostics.Log($"StopRouting (bg): asioOutput.Stop() error: {ex.Message}"); }
                    Diagnostics.Log("StopRouting (bg): disposing asioOutput");
                    try { capturedAsioOutput.Dispose(); Diagnostics.Log("StopRouting (bg): asioOutput.Dispose() returned"); } catch (Exception ex) { Diagnostics.Log($"StopRouting (bg): asioOutput.Dispose() error: {ex.Message}"); }
                }

                if (capturedDiagnosticWriter != null)
                {
                    Diagnostics.Log("StopRouting (bg): disposing diagnosticWriter (captured)");
                    try { capturedDiagnosticWriter.Dispose(); Diagnostics.Log("StopRouting (bg): diagnosticWriter disposed"); } catch (Exception ex) { Diagnostics.Log($"StopRouting (bg): diagnosticWriter.Dispose error: {ex.Message}"); }
                }

                Diagnostics.Log("StopRouting (bg): completed");
            }
            catch (Exception ex)
            {
                Diagnostics.Log($"StopRouting (bg) error: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref stopRequested, 0);
            }
        });
    }

    // Start capturing from an input ASIO driver only (fills internal buffer)
    public void StartInputOnly(string inputDriver, int selectedInputChannels, int inputStartChannel)
    {
        StopInput();

        asioInput = asioFactory.Create(inputDriver);

        int inCh = asioInput.DriverInputChannelCount;

        // ensure waveBuffer exists with at least input channels
        int channels = Math.Max(1, selectedInputChannels);
        waveBuffer = new BufferedWaveProvider(new WaveFormat(48000, 16, channels)) { BufferDuration = TimeSpan.FromSeconds(2), DiscardOnBufferOverflow = true };

        if (EnableDiagnostics)
        {
            string file = Path.Combine(Path.GetTempPath(), "asio_routed_capture.wav");
            diagnosticWriter?.Dispose();
            diagnosticWriter = new BackgroundDiagnosticWriter(file, waveBuffer.WaveFormat, LogAudio);
            LogAudio($"Diagnostic capture: {file}");
        }

        asioInputHandler = (s, e) => HandleAudioAvailable(e, selectedInputChannels, selectedInputChannels, inputStartChannel, 0);
        asioInput.AudioAvailable += asioInputHandler;
        asioInput.InitRecordAndPlayback(null, inCh, 48000);
        // set input running flag before play so callbacks run
        isInputRunning = true;
        asioInput.Play();
    }

    // Start playback to an output ASIO driver only (plays from internal buffer)
    public void StartOutputOnly(string outputDriver, int selectedOutputChannels, int outputStartChannel)
    {
        StopOutput();

        // ensure we have a buffer with required channels
        if (waveBuffer == null || waveBuffer.WaveFormat.Channels != selectedOutputChannels)
        {
            waveBuffer = new BufferedWaveProvider(new WaveFormat(48000, 16, selectedOutputChannels)) { BufferDuration = TimeSpan.FromSeconds(2), DiscardOnBufferOverflow = true };
        }

        asioOutput = asioFactory.Create(outputDriver);
        asioOutput.Init(waveBuffer);
        // set running flags before starting playback so callbacks see the correct state
        isRunning = true;
        isOutputRunning = true;
        asioOutput.Play();
    }

    public void StopInput()
    {
        try
        {
            if (asioInput != null)
            {
                if (asioInputHandler != null) asioInput.AudioAvailable -= asioInputHandler;
                try { asioInput.Stop(); } catch { }
                try { asioInput.Dispose(); } catch { }
                asioInput = null;
                asioInputHandler = null;
            }
        }
        catch (Exception ex)
        {
            LogAudio($"Stop input error: {ex.Message}");
        }
        isInputRunning = false;
    }

    public void StopOutput()
    {
        try
        {
            if (asioOutput != null)
            {
                try { asioOutput.Stop(); } catch { }
                try { asioOutput.Dispose(); } catch { }
                asioOutput = null;
            }
        }
        catch (Exception ex)
        {
            LogAudio($"Stop output error: {ex.Message}");
        }
        isOutputRunning = false;
    }

    public void Dispose()
    {
        StopRouting();
    }
}

// Background diagnostic writer that writes byte buffers to disk on a dedicated thread
internal class BackgroundDiagnosticWriter : IDisposable
{
    private readonly BlockingCollection<PooledBuffer> queue;
    private readonly Thread? writerThread;
    private WaveFileWriter? writer;
    private readonly Action<string>? logger;
    private long dropCounter = 0;
    private readonly CancellationTokenSource writeCts;

    public BackgroundDiagnosticWriter(string filePath, WaveFormat format, Action<string>? logger = null)
    {
        this.logger = logger;
        queue = new BlockingCollection<PooledBuffer>(boundedCapacity: 128);
        writeCts = new CancellationTokenSource();

        try
        {
            writer = new WaveFileWriter(filePath, format);
        }
        catch
        {
            writer = null;
            logger?.Invoke($"Diagnostic writer could not be created: {filePath}");
        }

        writerThread = new Thread(WriterLoop) { IsBackground = true, Name = "BackgroundDiagnosticWriter" };
        writerThread.Start();
    }

    private void WriterLoop()
    {
        var token = writeCts.Token;
        try
        {
            // Use TryTake with timeout so we can respond to cancellation promptly
            while (!queue.IsCompleted && !token.IsCancellationRequested)
            {
                try
                {
                    if (queue.TryTake(out var item, 200, token))
                    {
                        try
                        {
                            writer?.Write(item.Buffer, 0, item.Length);
                        }
                        catch { }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(item.Buffer, true);
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { /* ignore other transient errors */ }
            }

            // If cancellation was not requested, flush remaining items normally
            if (!token.IsCancellationRequested)
            {
                try
                {
                    foreach (var item in queue.GetConsumingEnumerable())
                    {
                        try { writer?.Write(item.Buffer, 0, item.Length); } catch { }
                        finally { ArrayPool<byte>.Shared.Return(item.Buffer, true); }
                    }
                }
                catch { }
            }

            try { writer?.Flush(); } catch { }
        }
        catch (Exception ex)
        {
            try { logger?.Invoke($"BackgroundDiagnosticWriter.WriterLoop error: {ex.Message}"); } catch { }
        }
    }

    // enqueue a copy of the buffer segment for background writing
    public void Enqueue(byte[] buffer, int offset, int count)
    {
        if (buffer == null || count <= 0 || offset < 0 || offset + count > buffer.Length) return;

        // If writer is shutting down, skip
        if (writeCts.IsCancellationRequested) return;

        // quick-bail if queue is full to avoid allocation on realtime thread
        try
        {
            if (queue.BoundedCapacity > 0 && queue.Count >= queue.BoundedCapacity)
            {
                var dropped = Interlocked.Increment(ref dropCounter);
                if ((dropped & 0xFF) == 0) logger?.Invoke($"Diagnostic queue full - dropped {dropped} buffers");
                return;
            }
        }
        catch { }

        var copy = ArrayPool<byte>.Shared.Rent(count);
        Buffer.BlockCopy(buffer, offset, copy, 0, count);
        var pooled = new PooledBuffer(copy, count);
        if (!queue.TryAdd(pooled))
        {
            ArrayPool<byte>.Shared.Return(copy, true);
            var dropped = Interlocked.Increment(ref dropCounter);
            if ((dropped & 0xFF) == 0) logger?.Invoke($"Diagnostic queue full - dropped {dropped} buffers");
        }
    }

    public void Dispose()
    {
        try { logger?.Invoke("BackgroundDiagnosticWriter.Dispose: start"); } catch { }

        try
        {
            // Signal completion and request cancellation
            queue.CompleteAdding();
            writeCts.Cancel();

            // wait for thread to finish within timeout
            bool joined = false;
            try { joined = writerThread?.Join(500) ?? true; } catch { }

            if (!joined)
            {
                try { logger?.Invoke("BackgroundDiagnosticWriter.Dispose: writer thread did not stop within 500ms, abandoning pending writes"); } catch { }

                // abandon any pending buffers to avoid blocking shutdown
                try
                {
                    while (queue.TryTake(out var it)) ArrayPool<byte>.Shared.Return(it.Buffer, true);
                }
                catch { }

                // try to dispose writer (may fail if thread still using it)
                try { writer?.Dispose(); } catch (Exception ex) { try { logger?.Invoke($"BackgroundDiagnosticWriter.Dispose: writer.Dispose error after abort: {ex.Message}"); } catch { } }
            }
            else
            {
                try { logger?.Invoke("BackgroundDiagnosticWriter.Dispose: writer thread stopped"); } catch { }
                try { writer?.Dispose(); } catch (Exception ex) { try { logger?.Invoke($"BackgroundDiagnosticWriter.Dispose: writer.Dispose error: {ex.Message}"); } catch { } }
            }
        }
        catch (Exception ex)
        {
            try { logger?.Invoke($"BackgroundDiagnosticWriter.Dispose: error {ex.Message}"); } catch { }
        }
        finally
        {
            try { writeCts.Dispose(); } catch { }
        }
    }

    public struct PooledBuffer
    {
        public byte[] Buffer;
        public int Length;
        public PooledBuffer(byte[] buffer, int length) { Buffer = buffer; Length = length; }
    }
}

public class AudioLevelsEventArgs : EventArgs
{
    public float Left { get; }
    public float Right { get; }
    public AudioLevelsEventArgs(float left, float right) { Left = left; Right = right; }
}
