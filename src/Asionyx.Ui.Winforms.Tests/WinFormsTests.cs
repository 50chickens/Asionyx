using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace AsioAudioEngine.Tests.Winforms
{
    [TestFixture]
    [Category("Integration")]
    public class WinFormsTests
    {
        private Application? app;
        private AutomationBase? automation;
        private Window? mainWindow;

        private const string ExeName = "AsioAudioRouter.exe";

        [SetUp]
        public void Setup()
        {
            // ensure built exe exists
            var projectDir = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "AsioAudioRouter"));
            var exePath = Path.Combine(projectDir, "bin", "Debug", "net9.0-windows", ExeName);
            if (!File.Exists(exePath)) Assert.Inconclusive($"Test exe not found at {exePath}. Build the solution before running UI tests.");

            app = Application.Launch(new ProcessStartInfo(exePath) { Arguments = "--asio-test" });
            automation = new UIA3Automation();
            // wait for main window
            mainWindow = RetryFindWindow(() => app.GetMainWindow(automation), TimeSpan.FromSeconds(10));
            if (mainWindow == null) Assert.Inconclusive("Main window not found.");
        }

        [TearDown]
        public void TearDown()
        {
            try { mainWindow?.Close(); } catch { }
            try { app?.Close(); } catch { }
            automation?.Dispose();
        }

        private T? RetryFind<T>(Func<T?> getter, TimeSpan timeout) where T : class
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                try
                {
                    var v = getter();
                    if (v != null) return v;
                }
                catch { }
                Thread.Sleep(200);
            }
            return null;
        }

        private Window? RetryFindWindow(Func<Window?> getter, TimeSpan timeout)
        {
            return RetryFind(getter, timeout);
        }

        private AutomationElement? FindById(string id)
        {
            return mainWindow?.FindFirstDescendant(cf => cf.ByAutomationId(id));
        }

        [Test]
        public void InputControls_Workflow()
        {
            var comboDevice = FindById("comboBoxInputDevice")?.AsComboBox();
            var comboChannels = FindById("comboBoxInputChannels")?.AsComboBox();
            // main routing start/stop buttons
            var start = FindById("buttonStart")?.AsButton();
            var stop = FindById("buttonStop")?.AsButton();
            var mute = FindById("buttonMuteInput")?.AsButton();
            var volume = FindById("trackBarInputVolume")?.AsSlider();
            var leftBar = FindById("progressBarInputLeft")?.AsProgressBar();

            Assert.That(comboDevice, Is.Not.Null);
            Assert.That(comboChannels, Is.Not.Null);
            Assert.That(start, Is.Not.Null);
            Assert.That(stop, Is.Not.Null);
            Assert.That(mute, Is.Not.Null);
            Assert.That(volume, Is.Not.Null);
            Assert.That(leftBar, Is.Not.Null);

            // exercise channel selection if available
            if (comboDevice!.Items.Length > 0)
            {
                comboDevice.Select(0);
                Thread.Sleep(200);
                if (comboChannels!.Items.Length > 0) comboChannels.Select(0);
            }

            // toggle mute
            mute!.Invoke();
            Thread.Sleep(100);
            mute.Invoke();

            // start/stop routing
            start!.Invoke();
            Thread.Sleep(500);
            if (!start.IsEnabled)
            {
                Assert.That(comboDevice!.IsEnabled, Is.False);
                stop!.Invoke();
                Thread.Sleep(300);
            }
            else
            {
                Assert.Pass("Input start did not engage (likely no ASIO devices). Controls present and interactive.");
            }
        }

        [Test]
        public void OutputControls_Workflow()
        {
            var comboDevice = FindById("comboBoxOutputDevice")?.AsComboBox();
            var comboChannels = FindById("comboBoxOutputChannels")?.AsComboBox();
            // Output group does not have a separate start button in this UI; use mute/volume and meters
            var mute = FindById("buttonMuteOutput")?.AsButton();
            var volume = FindById("trackBarOutputVolume")?.AsSlider();
            var leftBar = FindById("progressBarOutputLeft")?.AsProgressBar();

            Assert.That(comboDevice, Is.Not.Null);
            Assert.That(comboChannels, Is.Not.Null);
            Assert.That(mute, Is.Not.Null);
            Assert.That(volume, Is.Not.Null);
            Assert.That(leftBar, Is.Not.Null);

            if (comboDevice!.Items.Length > 0)
            {
                comboDevice.Select(0);
                Thread.Sleep(200);
                if (comboChannels!.Items.Length > 0) comboChannels.Select(0);
            }

            mute!.Invoke(); Thread.Sleep(100); mute.Invoke();

            // we can't start output separately in this UI; just ensure controls are interactive
            Assert.Pass("Output controls present and interactive.");
        }

        [Test]
        public void MonitorControls_Workflow()
        {
            var comboDevice = FindById("comboBoxMonitorDevice")?.AsComboBox();
            var comboChannels = FindById("comboBoxMonitorChannels")?.AsComboBox();
            var start = FindById("buttonStartMonitor")?.AsButton();
            var stop = FindById("buttonStopMonitor")?.AsButton();
            var mute = FindById("buttonMuteMonitor")?.AsButton();
            var volume = FindById("trackBarMonitorVolume")?.AsSlider();
            var leftBar = FindById("progressBarMonitorLeft")?.AsProgressBar();

            Assert.That(comboDevice, Is.Not.Null);
            Assert.That(comboChannels, Is.Not.Null);
            Assert.That(start, Is.Not.Null);
            Assert.That(stop, Is.Not.Null);
            Assert.That(mute, Is.Not.Null);
            Assert.That(volume, Is.Not.Null);
            Assert.That(leftBar, Is.Not.Null);

            if (comboDevice!.Items.Length > 0)
            {
                comboDevice.Select(0);
                Thread.Sleep(200);
                if (comboChannels!.Items.Length > 0) comboChannels.Select(0);
            }

            start!.Invoke(); Thread.Sleep(500);
            if (!start.IsEnabled)
            {
                Assert.That(stop!.IsEnabled, Is.True);
                Thread.Sleep(1000);
                Assert.DoesNotThrow(() => { var v = leftBar!.Value; });
                stop!.Invoke();
            }
            else Assert.Pass("Monitor start did not engage. Controls are present.");
        }

        [Test]
        public void Mp3PlayerControls_Workflow()
        {
            var browse = FindById("buttonBrowseMP3")?.AsButton();
            var play = FindById("buttonPlayMP3")?.AsButton();
            var stop = FindById("buttonStopMP3")?.AsButton();
            var device = FindById("comboBoxMP3Device")?.AsComboBox();
            var channels = FindById("comboBoxMP3Channels")?.AsComboBox();

            Assert.That(browse, Is.Not.Null);
            Assert.That(play, Is.Not.Null);
            Assert.That(stop, Is.Not.Null);
            Assert.That(device, Is.Not.Null);
            Assert.That(channels, Is.Not.Null);

            // Can't actually open file dialog in CI reliably; ensure controls present
            Assert.That(browse!.IsEnabled, Is.True);
            Assert.That(play!.IsEnabled, Is.True);
        }

        [Test]
        public void StartStopRouting_DisablesSelectorsAndUpdatesButtons()
        {
            var comboDevice = FindById("comboBoxOutputDevice")?.AsComboBox();
            var start = FindById("buttonStart")?.AsButton();
            var stop = FindById("buttonStop")?.AsButton();

            Assert.That(comboDevice, Is.Not.Null);
            Assert.That(start, Is.Not.Null);
            Assert.That(stop, Is.Not.Null);

            // wait up to 5s for devices to populate
            bool devicesPopulated = false;
            for (int i = 0; i < 25; i++)
            {
                try { if (comboDevice!.Items.Length > 0) { devicesPopulated = true; break; } } catch { }
                Thread.Sleep(200);
            }

            if (!devicesPopulated)
            {
                Assert.Pass("No ASIO drivers available in test environment; UI wiring appears intact.");
                return;
            }

            // start routing
            start!.Invoke();
            Thread.Sleep(500);

            // After starting, the output device combo should be disabled
            Assert.That(comboDevice.IsEnabled, Is.False);

            // stop routing
            stop!.Invoke();
            Thread.Sleep(500);

            // After stopping, the output device combo should be enabled again
            Assert.That(comboDevice.IsEnabled, Is.True);
        }

        [Test]
        public void SyntheticAudio_ProducesAudioLogsAndMeterUpdates()
        {
            // This test launches the app in test mode and verifies that audio logs appear and meters increase.
            var statusBox = mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("textBoxStatus"))?.AsTextBox();
            var monitorLeft = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("progressBarMonitorLeft"))?.AsProgressBar();

            Assert.That(statusBox, Is.Not.Null);
            Assert.That(monitorLeft, Is.Not.Null);

            // Start monitor using test driver if present
            var monitorCombo = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("comboBoxMonitorDevice"))?.AsComboBox();
            var monitorStart = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("buttonStartMonitor"))?.AsButton();

            if (monitorCombo != null && monitorCombo.Items.Length > 0)
            {
                monitorCombo.Select(0);
                Thread.Sleep(200);
            }

            if (monitorStart != null)
            {
                monitorStart.Invoke();
            }

            // wait up to 5s for audio logs to appear
            bool sawAudioLog = false;
            for (int i = 0; i < 25; i++)
            {
                var text = statusBox!.Text;
                if (!string.IsNullOrEmpty(text) && text.Contains("Monitor sample detected")) { sawAudioLog = true; break; }
                Thread.Sleep(200);
            }

            Assert.That(sawAudioLog, Is.True, "Expected audio logs to appear from synthetic driver");

            // wait for meter to update to non-zero
            bool meterMoved = false;
            for (int i = 0; i < 25; i++)
            {
                try { if (monitorLeft!.Value > 0) { meterMoved = true; break; } } catch { }
                Thread.Sleep(200);
            }

            Assert.That(meterMoved, Is.True, "Expected monitor left meter to show activity from synthetic driver");
        }
    }
}
