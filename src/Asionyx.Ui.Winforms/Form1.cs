using System.Text.Json;
using NAudio.Wave;
using AsioRouter.Spectrum;
using AsioAudioEngine;

namespace AsioAudioRouter;

public partial class Form1 : Form
{
    private AudioRouter? router;
    private System.Windows.Forms.Timer? levelMeterTimer;

    // UI level values
    private float inputLeftLevel = 0;
    private float inputRightLevel = 0;
    private float outputLeftLevel = 0;
    private float outputRightLevel = 0;

    private int selectedInputChannelCount = 2;
    private int selectedOutputChannelCount = 2;
    private int selectedMP3ChannelCount = 2;
    private int inputStartChannel = 0;
    private int outputStartChannel = 0;
    private int mp3StartChannel = 0;
    private bool includeASIO4ALL = false;

    // MP3 player variables
    private AudioFileReader? mp3Reader;
    private AsioOut? asioMP3;
    private bool isMp3Playing = false;
    private float mp3Volume = 1.0f;

    // Test tone generator variables
    private AsioOut? asioTestTone;
    private SignalGenerator? testToneGenerator;
    private bool isTestTonePlaying = false;

    // Settings
    private AppSettings settings = new AppSettings();

    // Diagnostic playback fields
    private AsioOut? asioDiagOut;
    private AudioFileReader? diagReader;
    private bool isDiagPlaying = false;
    private Button? buttonPlayDiagnostic;

    // --- Monitor input state (logic only) ---
    private IAsioOutWrapper? asioMonitorWrapper;
    private int selectedMonitorChannelCount = 2;
    private int monitorStartChannel = 0;
    private float monitorVolume = 1.0f;
    private bool isMonitorMuted = false;
    private float monitorLeftLevel = 0f;
    private float monitorRightLevel = 0f;
    private bool isMonitorRunning = false;
    private bool monitorHasLoggedSample = false;

    // Spectrum analyzer
    private SpectrumAnalyzer? spectrumAnalyzer;
    // UI-controlled spectrum settings (defaults: 80Hz - 10kHz, floor -60dB)
    private int spectrumFreqMinHz = 80;
    private int spectrumFreqMaxHz = 10000;
    private int spectrumDbFloor = -60;

    // ASIO factory used for device enumeration and creating wrappers. Can be TestAsioFactory in test mode.
    private IAsioFactory? asioFactory;

    public Form1()
    {
        InitializeComponent();

        levelMeterTimer = new System.Windows.Forms.Timer();
        levelMeterTimer.Interval = 50; // 20 Hz
        levelMeterTimer.Tick += LevelMeterTimer_Tick;

        // Choose ASIO factory: test mode if env var or command line flag present
        bool testMode = false;
        try {
            var args = Environment.GetCommandLineArgs();
            if (args.Any(a => string.Equals(a, "--asio-test", StringComparison.OrdinalIgnoreCase))) testMode = true;
            if (Environment.GetEnvironmentVariable("ASIO_TEST_MODE") == "1") testMode = true;
        } catch { }
        asioFactory = testMode ? new TestAsioFactory(forceFake: true) as IAsioFactory : new AsioFactory();

        router = new AudioRouter(asioFactory!);
        router.StatusLogged += Router_StatusLogged;
        router.AudioLogged += Router_AudioLogged;
        router.LevelsUpdated += Router_LevelsUpdated;
        router.RawInputAvailable += Router_RawInputAvailable;

        LogStatus($"Diagnostics {(router.EnableDiagnostics ? "enabled" : "disabled")} (writing to status log)");

        try
        {
            buttonPlayDiagnostic = new Button()
            {
                Text = "Play Diagnostic",
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            buttonPlayDiagnostic.Click += (s, e) =>
            {
                if (!isDiagPlaying) PlayDiagnosticCapture(); else StopDiagnosticCapture();
            };

            int margin = 8;
            int x = Math.Max(8, this.ClientSize.Width - 120 - margin);
            buttonPlayDiagnostic.Location = new System.Drawing.Point(x, margin + 40);
            this.Controls.Add(buttonPlayDiagnostic);
        }
        catch { }

        // spectrum analyzer
        spectrumAnalyzer = new SpectrumAnalyzer(1024);
    }

    private void Router_LevelsUpdated(object? sender, AudioLevelsEventArgs e)
    {
        // update UI levels from engine event (thread marshaling later in UpdateLevelMeters)
        inputLeftLevel = Math.Max(inputLeftLevel * 0.7f, e.Left);
        inputRightLevel = Math.Max(inputRightLevel * 0.7f, e.Right);

        // mirror to output approximate values
        outputLeftLevel = inputLeftLevel;
        outputRightLevel = inputRightLevel;
    }

    private void Router_StatusLogged(object? sender, string e)
    {
        LogStatus(e);
    }

    private void Router_AudioLogged(object? sender, string e)
    {
        // audio category logs go to status area too
        LogStatus($"[AUDIO] {e}");
    }

    private void Form1_Load(object sender, EventArgs e)
    {
        try
        {
            LoadSettings();

            comboBoxInputDevice.SelectedIndexChanged += ComboBoxInputDevice_SelectedIndexChanged;
            comboBoxOutputDevice.SelectedIndexChanged += ComboBoxOutputDevice_SelectedIndexChanged;
            comboBoxMP3Device.SelectedIndexChanged += ComboBoxMP3Device_SelectedIndexChanged;

            // Refresh device lists on dropdown so hot-plugged devices are visible when user opens the selector
            comboBoxInputDevice.DropDown += (s, ev) => ReEnumerateDevices();
            comboBoxOutputDevice.DropDown += (s, ev) => ReEnumerateDevices();
            comboBoxMP3Device.DropDown += (s, ev) => ReEnumerateDevices();

            // Enumerate devices on startup
            ReEnumerateDevices();

            // Populate channel selectors for current selections
            if (comboBoxInputDevice.SelectedItem != null) UpdateChannelSelector(comboBoxInputDevice, comboBoxInputChannels);
            if (comboBoxOutputDevice.SelectedItem != null) UpdateChannelSelector(comboBoxOutputDevice, comboBoxOutputChannels);
            if (comboBoxMP3Device.SelectedItem != null) UpdateChannelSelector(comboBoxMP3Device, comboBoxMP3Channels);

            ApplySettings();

            // Restore generic UI state after controls populated
            LoadUIState();

            LogStatus("Application started. Select input and output devices.");
        }
        catch (Exception ex)
        {
            LogStatus($"Error loading ASIO drivers: {ex.Message}");
            MessageBox.Show($"Error loading ASIO drivers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ReEnumerateDevices()
    {
        // Preserve selections when possible
        string? curInput = comboBoxInputDevice.SelectedItem?.ToString();
        string? curOutput = comboBoxOutputDevice.SelectedItem?.ToString();
        string? curMP3 = comboBoxMP3Device.SelectedItem?.ToString();
        string? curMonitor = comboBoxMonitorDevice?.SelectedItem?.ToString();

        comboBoxInputDevice.Items.Clear();
        comboBoxOutputDevice.Items.Clear();
        comboBoxMP3Device.Items.Clear();
        try { comboBoxMonitorDevice?.Items.Clear(); } catch { }

        string[] driverNames = asioFactory != null ? asioFactory.GetDriverNames() : Array.Empty<string>();
        foreach (string driverName in driverNames)
        {
            if (!includeASIO4ALL && driverName.Contains("ASIO4ALL", StringComparison.OrdinalIgnoreCase)) continue;
            comboBoxInputDevice.Items.Add(driverName);
            comboBoxOutputDevice.Items.Add(driverName);
            comboBoxMP3Device.Items.Add(driverName);
            try { comboBoxMonitorDevice?.Items.Add(driverName); } catch { }
        }

        if (!string.IsNullOrEmpty(curInput) && comboBoxInputDevice.Items.Contains(curInput)) comboBoxInputDevice.SelectedItem = curInput;
        else if (comboBoxInputDevice.Items.Count > 0) comboBoxInputDevice.SelectedIndex = 0;

        if (!string.IsNullOrEmpty(curOutput) && comboBoxOutputDevice.Items.Contains(curOutput)) comboBoxOutputDevice.SelectedItem = curOutput;
        else if (comboBoxOutputDevice.Items.Count > 0) comboBoxOutputDevice.SelectedIndex = Math.Min(1, comboBoxOutputDevice.Items.Count - 1);

        if (!string.IsNullOrEmpty(curMP3) && comboBoxMP3Device.Items.Contains(curMP3)) comboBoxMP3Device.SelectedItem = curMP3;
        else if (comboBoxMP3Device.Items.Count > 0) comboBoxMP3Device.SelectedIndex = 0;

        try
        {
            if (!string.IsNullOrEmpty(curMonitor) && comboBoxMonitorDevice != null && comboBoxMonitorDevice.Items.Contains(curMonitor)) comboBoxMonitorDevice.SelectedItem = curMonitor;
            else if (comboBoxMonitorDevice != null && comboBoxMonitorDevice.Items.Count > 0) comboBoxMonitorDevice.SelectedIndex = 0;
        }
        catch { }

        LogStatus($"Found {driverNames.Length} ASIO driver(s).");
    }

    private void LoadSettings()
    {
        try
        {
            settings = AppSettings.Load();
            LogStatus("Settings loaded successfully.");
        }
        catch (Exception ex)
        {
            LogStatus($"Failed to load settings, using defaults: {ex.Message}");
            settings = new AppSettings();
        }
    }

    private void ApplySettings()
    {
        try
        {
            checkBoxIncludeASIO4ALL.Checked = settings.IncludeASIO4ALL;
            includeASIO4ALL = settings.IncludeASIO4ALL;

            trackBarInputVolume.Value = settings.InputVolume;
            trackBarOutputVolume.Value = settings.OutputVolume;
            trackBarMP3Volume.Value = settings.MP3Volume;
            mp3Volume = settings.MP3Volume / 100.0f;

            labelInputVolume.Text = $"Input Volume: {settings.InputVolume}%";
            labelOutputVolume.Text = $"Output Volume: {settings.OutputVolume}%";
            labelMP3Volume.Text = $"Volume: {settings.MP3Volume}%";

            if (!string.IsNullOrEmpty(settings.InputDevice) && comboBoxInputDevice.Items.Contains(settings.InputDevice))
                comboBoxInputDevice.SelectedItem = settings.InputDevice;
            if (!string.IsNullOrEmpty(settings.OutputDevice) && comboBoxOutputDevice.Items.Contains(settings.OutputDevice))
                comboBoxOutputDevice.SelectedItem = settings.OutputDevice;
            if (!string.IsNullOrEmpty(settings.MP3Device) && comboBoxMP3Device.Items.Contains(settings.MP3Device))
                comboBoxMP3Device.SelectedItem = settings.MP3Device;

            // Apply monitor settings: initialize monitor volume control if present
            try { if (trackBarMonitorVolume != null) monitorVolume = trackBarMonitorVolume.Value / 100.0f; } catch { }
        }
        catch (Exception ex)
        {
            LogStatus($"Error applying settings: {ex.Message}");
        }
    }

    private void ComboBoxInputDevice_SelectedIndexChanged(object? sender, EventArgs e) => UpdateChannelSelector(comboBoxInputDevice, comboBoxInputChannels);
    private void ComboBoxOutputDevice_SelectedIndexChanged(object? sender, EventArgs e) => UpdateChannelSelector(comboBoxOutputDevice, comboBoxOutputChannels);
    private void ComboBoxMP3Device_SelectedIndexChanged(object? sender, EventArgs e) => UpdateChannelSelector(comboBoxMP3Device, comboBoxMP3Channels);

    private void UpdateChannelSelector(ComboBox deviceCombo, ComboBox channelCombo)
    {
        channelCombo.Items.Clear();
        if (deviceCombo.SelectedItem == null) return;
        string driverName = deviceCombo.SelectedItem.ToString() ?? "";

        try
        {
            // use factory to create a temporary wrapper to query channel counts if possible
            int channelCount = 2;
            try
            {
                using var tmp = asioFactory?.Create(driverName);
                if (tmp != null)
                {
                    channelCount = tmp.DriverInputChannelCount;
                    if (deviceCombo == comboBoxOutputDevice || deviceCombo == comboBoxMP3Device) channelCount = tmp.DriverOutputChannelCount;
                }
            }
            catch { }

            // Build channel options: single channels and adjacent stereo pairs (no 'Mono' or global 'Stereo' headers)
            for (int i = 1; i <= channelCount; i++) channelCombo.Items.Add($"Channel {i}");

            if (channelCount >= 2)
            {
                for (int i = 1; i < channelCount; i++) channelCombo.Items.Add($"Channels {i}/{i + 1}");
            }

            if (channelCombo.Items.Count > 0)
            {
                // default to first stereo pair if present, else first channel
                int firstStereoIndex = -1;
                for (int i = 0; i < channelCombo.Items.Count; i++) if (channelCombo.Items[i].ToString()?.StartsWith("Channels ") == true) { firstStereoIndex = i; break; }
                channelCombo.SelectedIndex = firstStereoIndex >= 0 ? firstStereoIndex : 0;
            }

            LogStatus($"Device '{driverName}' has {channelCount} channels available.");
        }
        catch (Exception ex)
        {
            LogStatus($"Error querying device '{driverName}': {ex.Message}");
            channelCombo.Items.Add("Channels 1/2");
            if (channelCombo.Items.Count > 0) channelCombo.SelectedIndex = 0;
        }
    }

    private void SaveSettings()
    {
        try
        {
            settings.IncludeASIO4ALL = includeASIO4ALL;
            settings.InputDevice = comboBoxInputDevice.SelectedItem?.ToString() ?? "";
            settings.OutputDevice = comboBoxOutputDevice.SelectedItem?.ToString() ?? "";
            settings.MP3Device = comboBoxMP3Device.SelectedItem?.ToString() ?? "";
            settings.InputChannels = selectedInputChannelCount;
            settings.OutputChannels = selectedOutputChannelCount;
            settings.MP3Channels = selectedMP3ChannelCount;
            settings.InputVolume = trackBarInputVolume.Value;
            settings.OutputVolume = trackBarOutputVolume.Value;
            settings.MP3Volume = trackBarMP3Volume.Value;

            // Note: monitor UI state is not persisted in AppSettings (not present in AppSettings model)
            LogStatus("Settings saved successfully.");
        }
        catch (Exception ex)
        {
            LogStatus($"Error saving settings: {ex.Message}");
        }
    }

    private void SaveUIState()
    {
        try
        {
            settings.UIState.Clear();

            foreach (var c in GetAllControls(this))
            {
                if (string.IsNullOrEmpty(c.Name)) continue;

                var state = new Dictionary<string, object?>();
                state["Type"] = c.GetType().Name;

                // Generic properties
                state["Visible"] = c.Visible;
                state["Enabled"] = c.Enabled;
                state["LocationX"] = c.Location.X;
                state["LocationY"] = c.Location.Y;
                state["Width"] = c.Size.Width;
                state["Height"] = c.Size.Height;

                // Type-specific properties (safe casts)
                switch (c)
                {
                    case ComboBox cb:
                        state["SelectedIndex"] = cb.SelectedIndex;
                        state["Text"] = cb.SelectedItem?.ToString() ?? cb.Text;
                        break;
                    case CheckBox chk:
                        state["Checked"] = chk.Checked;
                        break;
                    case TrackBar tb:
                        state["Value"] = tb.Value;
                        break;
                    case TextBox txt:
                        state["Text"] = txt.Text;
                        break;
                    case NumericUpDown num:
                        state["Value"] = num.Value;
                        break;
                    case RadioButton rb:
                        state["Checked"] = rb.Checked;
                        break;
                    case Label lbl:
                        state["Text"] = lbl.Text;
                        break;
                    case Button btn:
                        state["Text"] = btn.Text;
                        break;
                    case ProgressBar pb:
                        // don't persist runtime meter values
                        break;
                    default:
                        // attempt common properties via reflection if needed later
                        break;
                }

                settings.UIState[c.Name] = JsonSerializer.Serialize(state);
            }
        }
        catch (Exception ex)
        {
            LogStatus($"Error saving UI state: {ex.Message}");
        }
    }

    private void LoadUIState()
    {
        try
        {
            if (settings.UIState == null) return;

            foreach (var kv in settings.UIState)
            {
                string controlName = kv.Key;
                string json = kv.Value;
                Control? c = FindControlByName(this, controlName);
                if (c == null) continue;

                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("Visible", out var vVis) && vVis.ValueKind == JsonValueKind.True || vVis.ValueKind == JsonValueKind.False)
                        c.Visible = vVis.GetBoolean();
                    if (root.TryGetProperty("Enabled", out var vEn) && vEn.ValueKind == JsonValueKind.True || vEn.ValueKind == JsonValueKind.False)
                        c.Enabled = vEn.GetBoolean();

                    if (root.TryGetProperty("LocationX", out var lx) && lx.ValueKind == JsonValueKind.Number && root.TryGetProperty("LocationY", out var ly) && ly.ValueKind == JsonValueKind.Number)
                    {
                        int x = lx.GetInt32();
                        int y = ly.GetInt32();
                        c.Location = new System.Drawing.Point(x, y);
                    }

                    if (root.TryGetProperty("Width", out var w) && w.ValueKind == JsonValueKind.Number && root.TryGetProperty("Height", out var h) && h.ValueKind == JsonValueKind.Number)
                    {
                        int width = w.GetInt32();
                        int height = h.GetInt32();
                        c.Size = new System.Drawing.Size(width, height);
                    }

                    // Type-specific restoration
                    switch (c)
                    {
                        case ComboBox cb:
                            if (root.TryGetProperty("SelectedIndex", out var si) && si.ValueKind == JsonValueKind.Number)
                            {
                                int idx = si.GetInt32();
                                if (idx >= 0 && idx < cb.Items.Count) cb.SelectedIndex = idx;
                            }
                            else if (root.TryGetProperty("Text", out var st) && st.ValueKind == JsonValueKind.String)
                            {
                                string? selText = st.GetString();
                                if (!string.IsNullOrEmpty(selText))
                                {
                                    for (int i = 0; i < cb.Items.Count; i++) if (cb.Items[i]?.ToString() == selText) { cb.SelectedIndex = i; break; }
                                }
                            }
                            break;
                        case CheckBox chk:
                            if (root.TryGetProperty("Checked", out var chkVal) && (chkVal.ValueKind == JsonValueKind.True || chkVal.ValueKind == JsonValueKind.False))
                                chk.Checked = chkVal.GetBoolean();
                            break;
                        case TrackBar tb:
                            if (root.TryGetProperty("Value", out var tbVal) && tbVal.ValueKind == JsonValueKind.Number)
                            {
                                int v = tbVal.GetInt32(); if (v >= tb.Minimum && v <= tb.Maximum) tb.Value = v;
                            }
                            break;
                        case TextBox txt:
                            if (root.TryGetProperty("Text", out var t) && t.ValueKind == JsonValueKind.String) txt.Text = t.GetString() ?? "";
                            break;
                        case NumericUpDown num:
                            if (root.TryGetProperty("Value", out var nVal) && nVal.ValueKind == JsonValueKind.Number)
                            {
                                decimal d = nVal.GetDecimal(); if (d >= num.Minimum && d <= num.Maximum) num.Value = d;
                            }
                            break;
                        case RadioButton rb:
                            if (root.TryGetProperty("Checked", out var rVal) && (rVal.ValueKind == JsonValueKind.True || rVal.ValueKind == JsonValueKind.False))
                                rb.Checked = rVal.GetBoolean();
                            break;
                        case Label lbl:
                            if (root.TryGetProperty("Text", out var lt) && lt.ValueKind == JsonValueKind.String) lbl.Text = lt.GetString() ?? lbl.Text;
                            break;
                        case Button btn:
                            if (root.TryGetProperty("Text", out var bt) && bt.ValueKind == JsonValueKind.String) btn.Text = bt.GetString() ?? btn.Text;
                            break;
                    }
                }
                catch { /* ignore malformed entries */ }
            }
        }
        catch (Exception ex)
        {
            LogStatus($"Error loading UI state: {ex.Message}");
        }
    }

    private IEnumerable<Control> GetAllControls(Control parent)
    {
        foreach (Control c in parent.Controls)
        {
            yield return c;
            foreach (var child in GetAllControls(c)) yield return child;
        }
    }

    private Control? FindControlByName(Control parent, string name)
    {
        foreach (Control c in parent.Controls)
        {
            if (c.Name == name) return c;
            var found = FindControlByName(c, name);
            if (found != null) return found;
        }
        return null;
    }

    private void LoadMP3Devices()
    {
        comboBoxMP3Device.Items.Clear();
        string[] driverNames = asioFactory != null ? asioFactory.GetDriverNames() : Array.Empty<string>();
        foreach (string driverName in driverNames)
        {
            if (!includeASIO4ALL && driverName.Contains("ASIO4ALL", StringComparison.OrdinalIgnoreCase)) continue;
            comboBoxMP3Device.Items.Add(driverName);
        }
        if (comboBoxMP3Device.Items.Count > 0) comboBoxMP3Device.SelectedIndex = 0;
    }

    private void LoadAsioDrivers()
    {
        comboBoxInputDevice.Items.Clear();
        comboBoxOutputDevice.Items.Clear();

        string[] driverNames = asioFactory != null ? asioFactory.GetDriverNames() : Array.Empty<string>();
        if (driverNames.Length == 0)
        {
            LogStatus("No ASIO drivers found on this system.");
            MessageBox.Show("No ASIO drivers found. Please install ASIO drivers for your audio devices.", "No ASIO Drivers", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        foreach (string driverName in driverNames)
        {
            if (!includeASIO4ALL && driverName.Contains("ASIO4ALL", StringComparison.OrdinalIgnoreCase)) continue;
            comboBoxInputDevice.Items.Add(driverName);
            comboBoxOutputDevice.Items.Add(driverName);
        }

        if (comboBoxInputDevice.Items.Count > 0) comboBoxInputDevice.SelectedIndex = 0;
        if (comboBoxOutputDevice.Items.Count > 0) comboBoxOutputDevice.SelectedIndex = Math.Min(1, comboBoxOutputDevice.Items.Count - 1);

        LogStatus($"Found {driverNames.Length} ASIO driver(s).");
    }

    private void ButtonStart_Click(object sender, EventArgs e)
    {
        if (comboBoxInputDevice.SelectedItem == null || comboBoxOutputDevice.SelectedItem == null)
        {
            MessageBox.Show("Please select both input and output devices.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try { StartAudioRouting(); }
        catch (Exception ex)
        {
            LogStatus($"Error starting audio routing: {ex.Message}");
            MessageBox.Show($"Error starting audio routing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            StopAudioRouting();
        }
    }

    private void StartAudioRouting()
    {
        // ensure other playback sources are stopped before starting routing
        try { StopMP3(); } catch { }
        try { StopTestTone(); } catch { }
        try { StopDiagnosticCapture(); } catch { }
        try { StopMonitor(); } catch { }

        string inputDriverName = comboBoxInputDevice.SelectedItem?.ToString() ?? "";
        string outputDriverName = comboBoxOutputDevice.SelectedItem?.ToString() ?? "";

        ParseChannelSelection(comboBoxInputChannels.SelectedItem?.ToString() ?? "", out selectedInputChannelCount, out inputStartChannel);
        ParseChannelSelection(comboBoxOutputChannels.SelectedItem?.ToString() ?? "", out selectedOutputChannelCount, out outputStartChannel);

        if (router == null) router = new AudioRouter();
        router.EnableDiagnostics = true;
        router.InputVolume = trackBarInputVolume.Value / 100.0f;
        router.OutputVolume = trackBarOutputVolume.Value / 100.0f;
        router.IsInputMuted = false;
        router.IsOutputMuted = false;

        router.StartRouting(inputDriverName, outputDriverName, selectedInputChannelCount, selectedOutputChannelCount, inputStartChannel, outputStartChannel);

        buttonStart.Enabled = false;
        buttonStop.Enabled = true;
        comboBoxInputDevice.Enabled = false;
        comboBoxOutputDevice.Enabled = false;
        comboBoxInputChannels.Enabled = false;
        comboBoxOutputChannels.Enabled = false;

        levelMeterTimer?.Start();

        LogStatus($"Audio routing started: {inputDriverName} -> {outputDriverName}");
    }

    private void ButtonStop_Click(object sender, EventArgs e) => StopAudioRouting();

    private void StopAudioRouting()
    {
        // Stop level meter updates first
        levelMeterTimer?.Stop();

        // Stop any playback sources that might still be running
        try
        {
            StopMP3();
        }
        catch (Exception ex)
        {
            LogStatus($"Error stopping MP3 during StopAudioRouting: {ex.Message}");
        }

        try
        {
            StopTestTone();
        }
        catch (Exception ex)
        {
            LogStatus($"Error stopping test tone during StopAudioRouting: {ex.Message}");
        }

        try
        {
            StopDiagnosticCapture();
        }
        catch (Exception ex)
        {
            LogStatus($"Error stopping diagnostic during StopAudioRouting: {ex.Message}");
        }

        // Stop and dispose the router to ensure all AsioOut instances are released
        try
        {
            if (router != null)
            {
                try { router.StopRouting(); } catch (Exception ex) { LogStatus($"Error stopping audio routing: {ex.Message}"); }
                try { router.Dispose(); } catch (Exception ex) { LogStatus($"Error disposing router: {ex.Message}"); }
                router = null;
            }
        }
        catch (Exception ex)
        {
            LogStatus($"Error during router cleanup: {ex.Message}");
        }

        // Reset UI and meters
        try
        {
            inputLeftLevel = inputRightLevel = outputLeftLevel = outputRightLevel = 0f;
            UpdateLevelMeters();
        }
        catch { }

        buttonStart.Enabled = true;
        buttonStop.Enabled = false;
        comboBoxInputDevice.Enabled = true;
        comboBoxOutputDevice.Enabled = true;
        comboBoxInputChannels.Enabled = true;
        comboBoxOutputChannels.Enabled = true;

        LogStatus("Audio routing stopped.");
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        StopAudioRouting();
        StopMP3();
        StopTestTone();
        StopDiagnosticCapture();
        // ensure monitor is stopped on exit
        try { StopMonitor(); } catch { }
        SaveSettings();
    }

    private void LogStatus(string message)
    {
        if (InvokeRequired) { Invoke(new Action<string>(LogStatus), message); return; }
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        textBoxStatus.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
    }

    private void LevelMeterTimer_Tick(object? sender, EventArgs e)
    {
        UpdateLevelMeters();
        // draw spectrum at same timer rate
        DrawSpectrum();
    }

    private void UpdateLevelMeters()
    {
        if (InvokeRequired) { Invoke(new Action(UpdateLevelMeters)); return; }

        int inputLeftPercent = (int)(inputLeftLevel * 100);
        int inputRightPercent = (int)(inputRightLevel * 100);
        int outputLeftPercent = (int)(outputLeftLevel * 100);
        int outputRightPercent = (int)(outputRightLevel * 100);
        int monitorLeftPercent = (int)(monitorLeftLevel * 100);
        int monitorRightPercent = (int)(monitorRightLevel * 100);

        try { progressBarInputLeft.Value = Math.Min(100, Math.Max(0, inputLeftPercent)); } catch { }
        try { progressBarInputRight.Value = Math.Min(100, Math.Max(0, inputRightPercent)); } catch { }
        try { progressBarOutputLeft.Value = Math.Min(100, Math.Max(0, outputLeftPercent)); } catch { }
        try { progressBarOutputRight.Value = Math.Min(100, Math.Max(0, outputRightPercent)); } catch { }

        try { if (progressBarMonitorLeft != null) progressBarMonitorLeft.Value = Math.Min(100, Math.Max(0, monitorLeftPercent)); } catch { }
        try { if (progressBarMonitorRight != null) progressBarMonitorRight.Value = Math.Min(100, Math.Max(0, monitorRightPercent)); } catch { }

        try { labelInputLeft.Text = $"L: {ConvertToDb(inputLeftLevel):F1} dB"; } catch { }
        try { labelInputRight.Text = $"R: {ConvertToDb(inputRightLevel):F1} dB"; } catch { }
        try { labelOutputLeft.Text = $"L: {ConvertToDb(outputLeftLevel):F1} dB"; } catch { }
        try { labelOutputRight.Text = $"R: {ConvertToDb(outputRightLevel):F1} dB"; } catch { }
        try { if (labelMonitorLeft != null) labelMonitorLeft.Text = $"L: {ConvertToDb(monitorLeftLevel):F1} dB"; } catch { }
        try { if (labelMonitorRight != null) labelMonitorRight.Text = $"R: {ConvertToDb(monitorRightLevel):F1} dB"; } catch { }

        try { progressBarInputLeft.ForeColor = inputLeftPercent > 0 ? Color.Lime : Color.Black; } catch { }
        try { progressBarInputRight.ForeColor = inputRightPercent > 0 ? Color.Lime : Color.Black; } catch { }
        try { progressBarOutputLeft.ForeColor = outputLeftPercent > 0 ? Color.Lime : Color.Black; } catch { }
        try { progressBarOutputRight.ForeColor = outputRightPercent > 0 ? Color.Lime : Color.Black; } catch { }

        inputLeftLevel *= 0.95f;
        inputRightLevel *= 0.95f;
        outputLeftLevel *= 0.95f;
        outputRightLevel *= 0.95f;
        monitorLeftLevel *= 0.95f;
        monitorRightLevel *= 0.95f;
    }

    private float ConvertToDb(float level)
    {
        if (level <= 0.0001f) return -60.0f;
        return 20.0f * (float)Math.Log10(level);
    }

    private void ButtonMuteInput_Click(object? sender, EventArgs e)
    {
        if (router != null) { router.IsInputMuted = !router.IsInputMuted; buttonMuteInput.Text = router.IsInputMuted ? "Unmute Input" : "Mute Input"; }
    }

    private void ButtonMuteOutput_Click(object? sender, EventArgs e)
    {
        if (router != null) { router.IsOutputMuted = !router.IsOutputMuted; buttonMuteOutput.Text = router.IsOutputMuted ? "Unmute Output" : "Mute Output"; }
    }

    private void TrackBarInputVolume_Scroll(object? sender, EventArgs e)
    {
        if (router != null) router.InputVolume = trackBarInputVolume.Value / 100.0f;
        labelInputVolume.Text = $"Input Volume: {trackBarInputVolume.Value}%";
        SaveSettings();
    }

    private void TrackBarOutputVolume_Scroll(object? sender, EventArgs e)
    {
        if (router != null) router.OutputVolume = trackBarOutputVolume.Value / 100.0f;
        labelOutputVolume.Text = $"Output Volume: {trackBarOutputVolume.Value}%";
        SaveSettings();
    }

    private void TrackBarMP3Volume_Scroll(object? sender, EventArgs e)
    {
        mp3Volume = trackBarMP3Volume.Value / 100.0f;
        if (mp3Reader != null) mp3Reader.Volume = mp3Volume;
        labelMP3Volume.Text = $"Volume: {trackBarMP3Volume.Value}%";
        SaveSettings();
    }

    private void CheckBoxIncludeASIO4ALL_CheckedChanged(object? sender, EventArgs e)
    {
        includeASIO4ALL = checkBoxIncludeASIO4ALL.Checked;
        LogStatus($"ASIO4ALL devices {(includeASIO4ALL ? "included" : "excluded")}");

        ReEnumerateDevices();
        LoadMP3Devices();
        SaveSettings();
    }

    private void ButtonBrowseMP3_Click(object? sender, EventArgs e)
    {
        using OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Audio Files|*.mp3;*.wav;*.m4a;*.aac|All Files|*.*", Title = "Select Audio File" };
        if (openFileDialog.ShowDialog() == DialogResult.OK) { textBoxMP3File.Text = openFileDialog.FileName; LogStatus($"Selected audio file: {Path.GetFileName(openFileDialog.FileName)}"); }
    }

    private void ButtonPlayMP3_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(textBoxMP3File.Text)) { MessageBox.Show("Please select an audio file first.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (comboBoxMP3Device.SelectedItem == null) { MessageBox.Show("Please select an ASIO device for MP3 playback.", "No Device Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        try { PlayMP3(); } catch (Exception ex) { LogStatus($"Error playing MP3: {ex.Message}"); MessageBox.Show($"Error playing MP3: {ex.Message}", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error); StopMP3(); }
    }

    private void PlayMP3()
    {
        string mp3DeviceName = comboBoxMP3Device.SelectedItem?.ToString() ?? "";
        ParseChannelSelection(comboBoxMP3Channels.SelectedItem?.ToString() ?? "", out selectedMP3ChannelCount, out mp3StartChannel);
        LogStatus($"Initializing MP3 playback on {mp3DeviceName} (Channels: {selectedMP3ChannelCount}, Start: {mp3StartChannel})");

        mp3Reader = new AudioFileReader(textBoxMP3File.Text) { Volume = mp3Volume };
        int totalChannels = GetDeviceOutputChannelCount(mp3DeviceName);

        IWaveProvider outputProvider;
        if (mp3StartChannel > 0 && totalChannels > selectedMP3ChannelCount)
        {
            outputProvider = new ChannelRoutingStream(mp3Reader, mp3StartChannel, totalChannels);
            LogStatus($"Routing MP3 to physical channels {mp3StartChannel + 1}-{mp3StartChannel + selectedMP3ChannelCount}");
        }
        else { outputProvider = mp3Reader; LogStatus($"Routing MP3 to physical channels 1-{selectedMP3ChannelCount}"); }

        asioMP3 = new AsioOut(mp3DeviceName);
        asioMP3.Init(outputProvider);
        asioMP3.Play();

        isMp3Playing = true; buttonPlayMP3.Enabled = false; buttonStopMP3.Enabled = true; buttonBrowseMP3.Enabled = false; comboBoxMP3Device.Enabled = false; comboBoxMP3Channels.Enabled = false; LogStatus($"MP3 playback started: {Path.GetFileName(textBoxMP3File.Text)}");
    }

    private void ButtonStopMP3_Click(object? sender, EventArgs e) => StopMP3();

    private void StopMP3()
    {
        isMp3Playing = false;
        try { if (asioMP3 != null) { asioMP3.Stop(); asioMP3.Dispose(); asioMP3 = null; } if (mp3Reader != null) { mp3Reader.Dispose(); mp3Reader = null; } } catch (Exception ex) { LogStatus($"Error stopping MP3: {ex.Message}"); }
        buttonPlayMP3.Enabled = true; buttonStopMP3.Enabled = false; buttonBrowseMP3.Enabled = true; comboBoxMP3Device.Enabled = true; comboBoxMP3Channels.Enabled = true; LogStatus("MP3 playback stopped.");
    }

    private void ButtonPlayTestTone_Click(object? sender, EventArgs e)
    {
        if (comboBoxMP3Device.SelectedItem == null) { MessageBox.Show("Please select an ASIO device for test tone playback.", "No Device Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        try { PlayTestTone(); } catch (Exception ex) { LogStatus($"Error playing test tone: {ex.Message}"); MessageBox.Show($"Error playing test tone: {ex.Message}", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error); StopTestTone(); }
    }

    private void PlayTestTone()
    {
        string deviceName = comboBoxMP3Device.SelectedItem?.ToString() ?? "";
        if (!int.TryParse(textBoxTestFrequency.Text, out int frequency)) { frequency = 440; textBoxTestFrequency.Text = "440"; }
        if (frequency < 20) frequency = 20; if (frequency > 20000) frequency = 20000;
        ParseChannelSelection(comboBoxMP3Channels.SelectedItem?.ToString() ?? "", out selectedMP3ChannelCount, out mp3StartChannel);

        testToneGenerator = new SignalGenerator(48000, selectedMP3ChannelCount) { Gain = 0.5, Frequency = frequency };
        int totalChannels = GetDeviceOutputChannelCount(deviceName);

        IWaveProvider outputProvider;
        if (mp3StartChannel > 0 && totalChannels > selectedMP3ChannelCount)
        {
            outputProvider = new ChannelRoutingStream(testToneGenerator, mp3StartChannel, totalChannels);
            LogStatus($"Routing test tone to physical channels {mp3StartChannel + 1}-{mp3StartChannel + selectedMP3ChannelCount}");
        }
        else { outputProvider = testToneGenerator; LogStatus($"Routing test tone to physical channels 1-{selectedMP3ChannelCount}"); }

        asioTestTone = new AsioOut(deviceName);
        asioTestTone.Init(outputProvider);
        asioTestTone.Play();

        isTestTonePlaying = true; buttonPlayTestTone.Enabled = false; buttonStopTestTone.Enabled = true; buttonPlayMP3.Enabled = false; textBoxTestFrequency.Enabled = false; comboBoxMP3Device.Enabled = false; comboBoxMP3Channels.Enabled = false;
        LogStatus($"Test tone playing: {frequency} Hz");
    }

    private void ButtonStopTestTone_Click(object? sender, EventArgs e) => StopTestTone();

    private void StopTestTone()
    {
        isTestTonePlaying = false;
        try { if (asioTestTone != null) { asioTestTone.Stop(); asioTestTone.Dispose(); asioTestTone = null; } testToneGenerator = null; } catch (Exception ex) { LogStatus($"Error stopping test tone: {ex.Message}"); }
        buttonPlayTestTone.Enabled = true; buttonStopTestTone.Enabled = false; buttonPlayMP3.Enabled = true; textBoxTestFrequency.Enabled = true; comboBoxMP3Device.Enabled = true; comboBoxMP3Channels.Enabled = true; LogStatus("Test tone stopped.");
    }

    private void PlayDiagnosticCapture()
    {
        try
        {
            string file = Path.Combine(Path.GetTempPath(), "asio_routed_capture.wav");
            if (!File.Exists(file)) { LogStatus($"Diagnostic file not found: {file}"); return; }
            if (comboBoxOutputDevice.SelectedItem == null) { LogStatus("Select an output ASIO device first."); return; }

            string outputDevice = comboBoxOutputDevice.SelectedItem.ToString() ?? "";
            diagReader = new AudioFileReader(file);
            asioDiagOut = new AsioOut(outputDevice);
            asioDiagOut.Init(diagReader);
            asioDiagOut.Play();
            isDiagPlaying = true; buttonPlayDiagnostic!.Text = "Stop Diagnostic"; LogStatus($"Diagnostic playback started on {outputDevice}");
        }
        catch (Exception ex) { LogStatus($"Failed to play diagnostic file: {ex.Message}"); StopDiagnosticCapture(); }
    }

    private void StopDiagnosticCapture()
    {
        try
        {
            if (asioDiagOut != null) { asioDiagOut.Stop(); asioDiagOut.Dispose(); asioDiagOut = null; }
            if (diagReader != null) { diagReader.Dispose(); diagReader = null; }
            isDiagPlaying = false; if (buttonPlayDiagnostic != null) buttonPlayDiagnostic.Text = "Play Diagnostic"; LogStatus("Diagnostic playback stopped.");
        }
        catch (Exception ex) { LogStatus($"Error stopping diagnostic playback: {ex.Message}"); }
    }

    private int GetDeviceOutputChannelCount(string deviceName)
    {
        try { using var asio = new AsioOut(deviceName); return asio.DriverOutputChannelCount; }
        catch { return 8; }
    }

    private void ParseChannelSelection(string selection, out int channelCount, out int startChannel)
    {
        channelCount = 2; startChannel = 0; if (string.IsNullOrEmpty(selection)) return; selection = selection.Trim();
        // Removed Mono/Stereo special cases � channel lists no longer include those options.
        // Removed 4 Channel / 5.1 / 7.1 checks as they are not used.
        if (selection.StartsWith("Channel ")) { var parts = selection.Split(' '); if (parts.Length >= 2 && int.TryParse(parts[1], out int ch)) { channelCount = 1; startChannel = ch - 1; } return; }
        if (selection.StartsWith("Channels "))
        {
            var channelPart = selection.Substring("Channels ".Length);
            if (channelPart.Contains('/')) { var parts = channelPart.Split('/'); if (parts.Length == 2 && int.TryParse(parts[0], out int ch1)) { channelCount = 2; startChannel = ch1 - 1; } }
            else if (channelPart.Contains('-')) { var parts = channelPart.Split('-'); if (parts.Length == 2 && int.TryParse(parts[0], out int ch1) && int.TryParse(parts[1], out int ch2)) { channelCount = ch2 - ch1 + 1; startChannel = ch1 - 1; } }
        }
    }

    private void ExitToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void StartMonitor()
    {
        try
        {
            if (comboBoxMonitorDevice == null || comboBoxMonitorDevice.SelectedItem == null) return;
            string device = comboBoxMonitorDevice.SelectedItem.ToString() ?? "";

            ParseChannelSelection(comboBoxMonitorChannels?.SelectedItem?.ToString() ?? "", out selectedMonitorChannelCount, out monitorStartChannel);
            monitorVolume = trackBarMonitorVolume?.Value / 100.0f ?? 1.0f;

            // ensure any previous monitor is stopped first
            try { StopMonitor(); } catch { }

            asioMonitorWrapper = asioFactory?.Create(device);
            if (asioMonitorWrapper != null)
            {
                // reset logged-sample flag so tests/logging can detect new audio
                monitorHasLoggedSample = false;

                asioMonitorWrapper.AudioAvailable += (s, ev) => Monitor_AudioAvailable(s, ev);
                int driverInputCh = asioMonitorWrapper.DriverInputChannelCount;
                asioMonitorWrapper.InitRecordAndPlayback(null, driverInputCh, 48000);
                asioMonitorWrapper.Play();
                isMonitorRunning = true;

                // update UI state
                try { buttonStartMonitor.Enabled = false; } catch { }
                try { buttonStopMonitor.Enabled = true; } catch { }
                try { comboBoxMonitorDevice.Enabled = false; } catch { }
                try { comboBoxMonitorChannels.Enabled = false; } catch { }

                // ensure level meters are updated
                levelMeterTimer?.Start();
                LogStatus($"Monitor started on {device} (driverInputs={driverInputCh}, selectedChannels={selectedMonitorChannelCount}, start {monitorStartChannel})");
                LogStatus("Monitor UI: start disabled, stop enabled");
            }
        }
        catch (Exception ex)
        {
            LogStatus($"Failed to start monitor: {ex.Message}");
            StopMonitor();
        }
    }

    private void StopMonitor()
    {
        try
        {
            if (asioMonitorWrapper != null)
            {
                try { asioMonitorWrapper.Stop(); } catch { }
                try { asioMonitorWrapper.Dispose(); } catch { }
                asioMonitorWrapper = null;
            }
        }
        catch (Exception ex)
        {
            LogStatus($"Error stopping monitor: {ex.Message}");
        }

        isMonitorRunning = false;
        monitorLeftLevel = monitorRightLevel = 0f;
        try { if (progressBarMonitorLeft != null) progressBarMonitorLeft.Value = 0; } catch { }
        try { if (progressBarMonitorRight != null) progressBarMonitorRight.Value = 0; } catch { }
        try { if (labelMonitorLeft != null) labelMonitorLeft.Text = "L: -60.0 dB"; } catch { }
        try { if (labelMonitorRight != null) labelMonitorRight.Text = "R: -60.0 dB"; } catch { }

        // update UI state
        try { buttonStartMonitor.Enabled = true; } catch { }
        try { buttonStopMonitor.Enabled = false; } catch { }
        try { comboBoxMonitorDevice.Enabled = true; } catch { }
        try { comboBoxMonitorChannels.Enabled = true; } catch { }

        LogStatus("Monitor UI: start enabled, stop disabled");
    }

    private void RestartMonitor()
    {
        bool wasRunning = isMonitorRunning;
        try { StopMonitor(); } catch { }
        if (wasRunning) StartMonitor();
    }

    private void Monitor_AudioAvailable(object? sender, AudioAvailableEventArgs e)
    {
        if (!isMonitorRunning) return;
        try
        {
            int inputDriverChannels = e.InputBuffersLength;
            int samplesPerBuffer = e.SamplesPerBuffer;

            float[] samples = new float[samplesPerBuffer * Math.Max(1, inputDriverChannels)];
            e.GetAsInterleavedSamples(samples);

            float leftSum = 0f, rightSum = 0f; int leftCount = 0, rightCount = 0;

            for (int i = 0; i < samplesPerBuffer; i++)
            {
                if (monitorStartChannel >= 0 && monitorStartChannel < inputDriverChannels)
                {
                    float v = samples[i * inputDriverChannels + monitorStartChannel];
                    leftSum += v * v; leftCount++;
                }
                if (selectedMonitorChannelCount >= 2 && (monitorStartChannel + 1) < inputDriverChannels)
                {
                    float v = samples[i * inputDriverChannels + monitorStartChannel + 1];
                    rightSum += v * v; rightCount++;
                }
            }

            float leftRms = leftCount > 0 ? (float)Math.Sqrt(leftSum / leftCount) : 0f;
            float rightRms = rightCount > 0 ? (float)Math.Sqrt(rightSum / rightCount) : 0f;

            monitorLeftLevel = isMonitorMuted ? 0f : leftRms * monitorVolume;
            monitorRightLevel = isMonitorMuted ? 0f : rightRms * monitorVolume;

            // Log a single message when we first detect audio on the monitor path (helps UI tests)
            if (!monitorHasLoggedSample && (monitorLeftLevel > 0.0001f || monitorRightLevel > 0.0001f))
            {
                monitorHasLoggedSample = true;
                LogStatus("[AUDIO] Monitor sample detected");
            }

            // feed spectrum analyzer with interleaved samples
            spectrumAnalyzer?.FeedInterleaved(samples, inputDriverChannels, monitorStartChannel);
            try { pictureBoxSpectrum.Invalidate(); } catch { }
        }
        catch (Exception ex)
        {
            LogStatus($"Monitor audio error: {ex.Message}");
        }
    }

    // Designer event handlers for monitor controls
    private void ComboBoxMonitorDevice_DropDown(object? sender, EventArgs e) => ReEnumerateDevices();

    private void ComboBoxMonitorDevice_SelectedIndexChanged(object? sender, EventArgs e)
    {
        try { if (comboBoxMonitorDevice?.SelectedItem != null && comboBoxMonitorChannels != null) UpdateChannelSelector(comboBoxMonitorDevice, comboBoxMonitorChannels); } catch { }
        if (isMonitorRunning) RestartMonitor();
    }

    private void ComboBoxMonitorChannels_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (isMonitorRunning) RestartMonitor();
    }

    private void TrackBarMonitorVolume_Scroll(object? sender, EventArgs e)
    {
        try { monitorVolume = trackBarMonitorVolume.Value / 100.0f; SaveSettings(); } catch { }
    }

    private void ButtonStartMonitor_Click(object? sender, EventArgs e)
    {
        StartMonitor();
        SaveSettings();
    }

    private void ButtonStopMonitor_Click(object? sender, EventArgs e)
    {
        StopMonitor();
        SaveSettings();
    }

    private void ButtonMuteMonitor_Click(object? sender, EventArgs e)
    {
        isMonitorMuted = !isMonitorMuted;
        try { buttonMuteMonitor.Text = isMonitorMuted ? "Unmute" : "Mute"; } catch { }
        SaveSettings();
    }

    private void Router_RawInputAvailable(object? sender, AudioAvailableEventArgs e)
    {
        // Forward raw interleaved samples to spectrum analyzer (non-blocking)
        try
        {
            int channels = Math.Max(1, e.InputBuffersLength);
            spectrumAnalyzer?.FeedInterleaved(e.Samples, channels, 0);
        }
        catch { }
    }

    // UI handlers for spectrum controls
    private void TrackBarFreqMin_Scroll(object? sender, EventArgs e)
    {
        try
        {
            spectrumFreqMinHz = Math.Clamp(trackBarFreqMin.Value, 20, 20000);
            if (spectrumFreqMinHz >= spectrumFreqMaxHz) spectrumFreqMinHz = Math.Max(20, spectrumFreqMaxHz - 1);
            labelFreqMin.Text = $"Min: {spectrumFreqMinHz} Hz";
            pictureBoxSpectrum.Invalidate();
        }
        catch { }
    }

    private void TrackBarFreqMax_Scroll(object? sender, EventArgs e)
    {
        try
        {
            spectrumFreqMaxHz = Math.Clamp(trackBarFreqMax.Value, 20, 24000);
            if (spectrumFreqMaxHz <= spectrumFreqMinHz) spectrumFreqMaxHz = Math.Min(24000, spectrumFreqMinHz + 1);
            labelFreqMax.Text = $"Max: {spectrumFreqMaxHz} Hz";
            pictureBoxSpectrum.Invalidate();
        }
        catch { }
    }

    private void TrackBarDbMin_Scroll(object? sender, EventArgs e)
    {
        try
        {
            spectrumDbFloor = Math.Clamp(trackBarDbMin.Value, -120, 0);
            labelDbMin.Text = $"Floor: {spectrumDbFloor} dB";
            pictureBoxSpectrum.Invalidate();
        }
        catch { }
    }

    private void DrawSpectrum()
    {
        if (InvokeRequired) { BeginInvoke(new Action(DrawSpectrum)); return; }
        try
        {
            float[] mags = spectrumAnalyzer?.GetLatestMagnitudes() ?? Array.Empty<float>();
            using var bmp = new Bitmap(Math.Max(1, pictureBoxSpectrum.Width), Math.Max(1, pictureBoxSpectrum.Height));
            using var g = Graphics.FromImage(bmp);
            g.Clear(pictureBoxSpectrum.BackColor);
            if (mags.Length == 0) { pictureBoxSpectrum.Image?.Dispose(); pictureBoxSpectrum.Image = (Bitmap)bmp.Clone(); return; }

            int w = bmp.Width; int h = bmp.Height; int bins = mags.Length;
            // assume Nyquist at 24kHz (sample rate 48k)
            float nyquist = 24000f;
            float binHz = nyquist / bins;

            int startBin = Math.Clamp((int)(spectrumFreqMinHz / binHz), 0, bins - 1);
            int endBin = Math.Clamp((int)(spectrumFreqMaxHz / binHz), 0, bins - 1);
            if (endBin <= startBin) { endBin = Math.Min(bins - 1, startBin + 1); }

            float maxMag = 1e-9f;
            for (int i = startBin; i <= endBin; i++) if (mags[i] > maxMag) maxMag = mags[i];

            using var pen = new Pen(Color.Lime, 1);
            for (int x = 0; x < w; x++)
            {
                // map x to bin in selected range
                int bin = startBin + (int)((x / (float)w) * (endBin - startBin + 1));
                bin = Math.Clamp(bin, startBin, endBin);
                float mag = mags[bin];
                // convert to dB, clamp to user floor..0 and scale
                float db = 20f * (float)Math.Log10(Math.Max(1e-9f, mag));
                db = Math.Min(0f, Math.Max(spectrumDbFloor, db));
                float v = (db - spectrumDbFloor) / (0f - spectrumDbFloor);
                int y = h - (int)(v * h);
                g.DrawLine(pen, x, h, x, y);
            }

            pictureBoxSpectrum.Image?.Dispose();
            pictureBoxSpectrum.Image = (Bitmap)bmp.Clone();
        }
        catch { }
    }

    private void PictureBoxSpectrum_Paint(object? sender, PaintEventArgs e)
    {
        try
        {
            var g = e.Graphics;
            g.Clear(pictureBoxSpectrum.BackColor);

            var rect = pictureBoxSpectrum.ClientRectangle;
            // leave margins for labels
            int left = rect.Left + 40;
            int right = rect.Right - 10;
            int top = rect.Top + 10;
            int bottom = rect.Bottom - 30;

            // draw axes lines
            using (var axisPen = new Pen(Color.LightGray, 1))
            using (var font = new Font("Segoe UI", 8))
            using (var brush = new SolidBrush(Color.White))
            {
                // Y axis (dB)
                g.DrawLine(axisPen, left, top, left, bottom);
                // X axis (frequency)
                g.DrawLine(axisPen, left, bottom, right, bottom);

                // vertical dB ticks and labels: from floor to 0 using 10dB steps
                int dbTop = spectrumDbFloor; int dbBottom = 0; int dbStep = 10;
                int dbRange = dbBottom - dbTop;
                if (dbRange <= 0) dbRange = 60;
                for (int db = dbTop; db <= dbBottom; db += dbStep)
                {
                    float y = top + (bottom - top) * (1f - (db - dbTop) / (float)dbRange);
                    g.DrawLine(axisPen, left - 4, y, left, y);
                    g.DrawString($"{db} dB", font, brush, 4, y - 8);
                }

                // horizontal frequency ticks and labels using selected range
                int width = right - left;
                float nyquist = 24000f;
                float binHz = Math.Max(1f, nyquist / Math.Max(1, (spectrumAnalyzer?.GetLatestMagnitudes()?.Length ?? 1)));
                float rangeHz = Math.Max(1f, spectrumFreqMaxHz - spectrumFreqMinHz);

                // choose ticks: 0.25,0.5,1,2,5,10 steps of decades within range
                int[] freqs = new int[] { 80, 250, 500, 1000, 2000, 5000, 10000 };
                foreach (var f in freqs)
                {
                    if (f < spectrumFreqMinHz || f > spectrumFreqMaxHz) continue;
                    float x = left + ((f - spectrumFreqMinHz) / (float)rangeHz) * width;
                    g.DrawLine(axisPen, x, bottom, x, bottom + 4);
                    g.DrawString(f >= 1000 ? $"{f / 1000}k" : f.ToString(), font, brush, x - 10, bottom + 6);
                }

                // draw spectrum if available
                var mags = spectrumAnalyzer?.GetLatestMagnitudes();
                if (mags != null && mags.Length > 1)
                {
                    int bins = mags.Length;
                    int startBin = Math.Clamp((int)(spectrumFreqMinHz / (nyquist / bins)), 0, bins - 1);
                    int endBin = Math.Clamp((int)(spectrumFreqMaxHz / (nyquist / bins)), 0, bins - 1);
                    if (endBin <= startBin) endBin = Math.Min(bins - 1, startBin + 1);

                    PointF[] points = new PointF[endBin - startBin + 1];
                    for (int i = startBin; i <= endBin; i++)
                    {
                        float mag = mags[i];
                        float db = 20f * (float)Math.Log10(Math.Max(1e-9, mag));
                        db = Math.Min(0f, Math.Max(spectrumDbFloor, db));
                        float x = left + ((i - startBin) / (float)(endBin - startBin + 1)) * width;
                        float y = top + (bottom - top) * (1f - (db - spectrumDbFloor) / (0f - spectrumDbFloor));
                        points[i - startBin] = new PointF(x, y);
                    }
                    if (points.Length > 1) g.DrawLines(Pens.Lime, points);
                }
            }
        }
        catch { }
    }
}
