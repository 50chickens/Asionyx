namespace Asionyx.Ui.Winforms;
 
partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        menuStrip = new MenuStrip();
        fileToolStripMenuItem = new ToolStripMenuItem();
        exitToolStripMenuItem = new ToolStripMenuItem();
        checkBoxIncludeASIO4ALL = new CheckBox();
        comboBoxInputDevice = new ComboBox();
        labelInputChannels = new Label();
        comboBoxInputChannels = new ComboBox();
        comboBoxOutputDevice = new ComboBox();
        comboBoxOutputChannels = new ComboBox();
        buttonStart = new Button();
        buttonStop = new Button();
        buttonMuteInput = new Button();
        buttonMuteOutput = new Button();
        trackBarInputVolume = new TrackBar();
        trackBarOutputVolume = new TrackBar();
        groupBoxMP3 = new GroupBox();
        buttonStopTestTone = new Button();
        buttonPlayTestTone = new Button();
        labelHz = new Label();
        textBoxTestFrequency = new TextBox();
        labelTestTone = new Label();
        trackBarMP3Volume = new TrackBar();
        labelMP3Volume = new Label();
        buttonStopMP3 = new Button();
        buttonPlayMP3 = new Button();
        comboBoxMP3Channels = new ComboBox();
        labelMP3Channels = new Label();
        comboBoxMP3Device = new ComboBox();
        labelMP3Device = new Label();
        labelMP3File = new Label();
        textBoxMP3File = new TextBox();
        buttonBrowseMP3 = new Button();
        labelStatus = new Label();
        textBoxStatus = new TextBox();
        progressBarInputLeft = new ProgressBar();
        progressBarInputRight = new ProgressBar();
        labelInputLeft = new Label();
        labelInputRight = new Label();
        progressBarOutputLeft = new ProgressBar();
        progressBarOutputRight = new ProgressBar();
        labelOutputLeft = new Label();
        labelOutputRight = new Label();
        groupBoxMonitor = new GroupBox();
        labelMonitorVolume = new Label();
        labelMonitorChannels = new Label();
        comboBoxMonitorDevice = new ComboBox();
        comboBoxMonitorChannels = new ComboBox();
        trackBarMonitorVolume = new TrackBar();
        buttonStartMonitor = new Button();
        buttonStopMonitor = new Button();
        buttonMuteMonitor = new Button();
        progressBarMonitorLeft = new ProgressBar();
        labelMonitorLeft = new Label();
        progressBarMonitorRight = new ProgressBar();
        labelMonitorRight = new Label();
        groupBoxInput = new GroupBox();
        pictureBoxSpectrum = new PictureBox();
        labelInputVolume = new Label();
        labelInputLevel = new Label();
        groupBoxOutput = new GroupBox();
        labelOutputChannels = new Label();
        labelOutputVolume = new Label();
        labelOutputLevel = new Label();
        groupBox1 = new GroupBox();
        menuStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)trackBarInputVolume).BeginInit();
        ((System.ComponentModel.ISupportInitialize)trackBarOutputVolume).BeginInit();
        groupBoxMP3.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)trackBarMP3Volume).BeginInit();
        groupBoxMonitor.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)trackBarMonitorVolume).BeginInit();
        groupBoxInput.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pictureBoxSpectrum).BeginInit();
        groupBoxOutput.SuspendLayout();
        groupBox1.SuspendLayout();
        SuspendLayout();
        // 
        // menuStrip
        // 
        menuStrip.ImageScalingSize = new Size(20, 20);
        menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
        menuStrip.Location = new Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.Padding = new Padding(5, 2, 0, 2);
        menuStrip.Size = new Size(882, 24);
        menuStrip.TabIndex = 18;
        menuStrip.Text = "menuStrip1";
        // 
        // fileToolStripMenuItem
        // 
        fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { exitToolStripMenuItem });
        fileToolStripMenuItem.Name = "fileToolStripMenuItem";
        fileToolStripMenuItem.Size = new Size(37, 20);
        fileToolStripMenuItem.Text = "&File";
        // 
        // exitToolStripMenuItem
        // 
        exitToolStripMenuItem.Name = "exitToolStripMenuItem";
        exitToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
        exitToolStripMenuItem.Size = new Size(134, 22);
        exitToolStripMenuItem.Text = "E&xit";
        exitToolStripMenuItem.Click += ExitToolStripMenuItem_Click;
        // 
        // checkBoxIncludeASIO4ALL
        // 
        checkBoxIncludeASIO4ALL.AutoSize = true;
        checkBoxIncludeASIO4ALL.Location = new Point(578, 56);
        checkBoxIncludeASIO4ALL.Margin = new Padding(3, 2, 3, 2);
        checkBoxIncludeASIO4ALL.Name = "checkBoxIncludeASIO4ALL";
        checkBoxIncludeASIO4ALL.Size = new Size(120, 19);
        checkBoxIncludeASIO4ALL.TabIndex = 30;
        checkBoxIncludeASIO4ALL.Text = "Include ASIO4ALL";
        checkBoxIncludeASIO4ALL.UseVisualStyleBackColor = true;
        checkBoxIncludeASIO4ALL.CheckedChanged += CheckBoxIncludeASIO4ALL_CheckedChanged;
        // 
        // comboBoxInputDevice
        // 
        comboBoxInputDevice.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBoxInputDevice.FormattingEnabled = true;
        comboBoxInputDevice.Location = new Point(12, 22);
        comboBoxInputDevice.Margin = new Padding(3, 2, 3, 2);
        comboBoxInputDevice.Name = "comboBoxInputDevice";
        comboBoxInputDevice.Size = new Size(241, 23);
        comboBoxInputDevice.TabIndex = 1;
        // 
        // labelInputChannels
        // 
        labelInputChannels.AutoSize = true;
        labelInputChannels.Location = new Point(259, 25);
        labelInputChannels.Name = "labelInputChannels";
        labelInputChannels.Size = new Size(59, 15);
        labelInputChannels.TabIndex = 19;
        labelInputChannels.Text = "Channels:";
        // 
        // comboBoxInputChannels
        // 
        comboBoxInputChannels.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBoxInputChannels.FormattingEnabled = true;
        comboBoxInputChannels.Location = new Point(330, 21);
        comboBoxInputChannels.Margin = new Padding(3, 2, 3, 2);
        comboBoxInputChannels.Name = "comboBoxInputChannels";
        comboBoxInputChannels.Size = new Size(100, 23);
        comboBoxInputChannels.TabIndex = 20;
        // 
        // comboBoxOutputDevice
        // 
        comboBoxOutputDevice.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBoxOutputDevice.FormattingEnabled = true;
        comboBoxOutputDevice.Location = new Point(12, 22);
        comboBoxOutputDevice.Margin = new Padding(3, 2, 3, 2);
        comboBoxOutputDevice.Name = "comboBoxOutputDevice";
        comboBoxOutputDevice.Size = new Size(241, 23);
        comboBoxOutputDevice.TabIndex = 3;
        // 
        // comboBoxOutputChannels
        // 
        comboBoxOutputChannels.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBoxOutputChannels.FormattingEnabled = true;
        comboBoxOutputChannels.Location = new Point(330, 21);
        comboBoxOutputChannels.Margin = new Padding(3, 2, 3, 2);
        comboBoxOutputChannels.Name = "comboBoxOutputChannels";
        comboBoxOutputChannels.Size = new Size(100, 23);
        comboBoxOutputChannels.TabIndex = 22;
        // 
        // buttonStart
        // 
        buttonStart.Location = new Point(13, 125);
        buttonStart.Margin = new Padding(3, 2, 3, 2);
        buttonStart.Name = "buttonStart";
        buttonStart.Size = new Size(88, 26);
        buttonStart.TabIndex = 4;
        buttonStart.Text = "Start";
        buttonStart.UseVisualStyleBackColor = true;
        buttonStart.Click += ButtonStart_Click;
        // 
        // buttonStop
        // 
        buttonStop.Enabled = false;
        buttonStop.Location = new Point(167, 125);
        buttonStop.Margin = new Padding(3, 2, 3, 2);
        buttonStop.Name = "buttonStop";
        buttonStop.Size = new Size(88, 26);
        buttonStop.TabIndex = 5;
        buttonStop.Text = "Stop";
        buttonStop.UseVisualStyleBackColor = true;
        buttonStop.Click += ButtonStop_Click;
        // 
        // buttonMuteInput
        // 
        buttonMuteInput.Location = new Point(330, 61);
        buttonMuteInput.Margin = new Padding(3, 2, 3, 2);
        buttonMuteInput.Name = "buttonMuteInput";
        buttonMuteInput.Size = new Size(100, 47);
        buttonMuteInput.TabIndex = 23;
        buttonMuteInput.Text = "Mute";
        buttonMuteInput.UseVisualStyleBackColor = true;
        buttonMuteInput.Click += ButtonMuteInput_Click;
        // 
        // buttonMuteOutput
        // 
        buttonMuteOutput.Location = new Point(330, 61);
        buttonMuteOutput.Margin = new Padding(3, 2, 3, 2);
        buttonMuteOutput.Name = "buttonMuteOutput";
        buttonMuteOutput.Size = new Size(100, 47);
        buttonMuteOutput.TabIndex = 24;
        buttonMuteOutput.Text = "Mute";
        buttonMuteOutput.UseVisualStyleBackColor = true;
        buttonMuteOutput.Click += ButtonMuteOutput_Click;
        // 
        // trackBarInputVolume
        // 
        trackBarInputVolume.Location = new Point(439, 21);
        trackBarInputVolume.Margin = new Padding(3, 2, 3, 2);
        trackBarInputVolume.Maximum = 100;
        trackBarInputVolume.Name = "trackBarInputVolume";
        trackBarInputVolume.Orientation = Orientation.Vertical;
        trackBarInputVolume.Size = new Size(45, 130);
        trackBarInputVolume.TabIndex = 26;
        trackBarInputVolume.TickFrequency = 10;
        trackBarInputVolume.Value = 100;
        trackBarInputVolume.Scroll += TrackBarInputVolume_Scroll;
        // 
        // trackBarOutputVolume
        // 
        trackBarOutputVolume.Location = new Point(439, 21);
        trackBarOutputVolume.Margin = new Padding(3, 2, 3, 2);
        trackBarOutputVolume.Maximum = 100;
        trackBarOutputVolume.Name = "trackBarOutputVolume";
        trackBarOutputVolume.Orientation = Orientation.Vertical;
        trackBarOutputVolume.Size = new Size(45, 130);
        trackBarOutputVolume.TabIndex = 28;
        trackBarOutputVolume.TickFrequency = 10;
        trackBarOutputVolume.Value = 100;
        trackBarOutputVolume.Scroll += TrackBarOutputVolume_Scroll;
        // 
        // groupBoxMP3
        // 
        groupBoxMP3.Controls.Add(buttonStopTestTone);
        groupBoxMP3.Controls.Add(buttonPlayTestTone);
        groupBoxMP3.Controls.Add(labelHz);
        groupBoxMP3.Controls.Add(textBoxTestFrequency);
        groupBoxMP3.Controls.Add(labelTestTone);
        groupBoxMP3.Controls.Add(trackBarMP3Volume);
        groupBoxMP3.Controls.Add(labelMP3Volume);
        groupBoxMP3.Controls.Add(buttonStopMP3);
        groupBoxMP3.Controls.Add(buttonPlayMP3);
        groupBoxMP3.Controls.Add(comboBoxMP3Channels);
        groupBoxMP3.Controls.Add(labelMP3Channels);
        groupBoxMP3.Controls.Add(comboBoxMP3Device);
        groupBoxMP3.Controls.Add(labelMP3Device);
        groupBoxMP3.Controls.Add(labelMP3File);
        groupBoxMP3.Controls.Add(textBoxMP3File);
        groupBoxMP3.Controls.Add(buttonBrowseMP3);
        groupBoxMP3.Location = new Point(22, 570);
        groupBoxMP3.Margin = new Padding(3, 2, 3, 2);
        groupBoxMP3.Name = "groupBoxMP3";
        groupBoxMP3.Padding = new Padding(3, 2, 3, 2);
        groupBoxMP3.Size = new Size(490, 172);
        groupBoxMP3.TabIndex = 29;
        groupBoxMP3.TabStop = false;
        groupBoxMP3.Text = "MP3 Player & Test Tone";
        // 
        // buttonStopTestTone
        // 
        buttonStopTestTone.Enabled = false;
        buttonStopTestTone.Location = new Point(280, 105);
        buttonStopTestTone.Margin = new Padding(3, 2, 3, 2);
        buttonStopTestTone.Name = "buttonStopTestTone";
        buttonStopTestTone.Size = new Size(70, 22);
        buttonStopTestTone.TabIndex = 15;
        buttonStopTestTone.Text = "Stop";
        buttonStopTestTone.UseVisualStyleBackColor = true;
        buttonStopTestTone.Click += ButtonStopTestTone_Click;
        // 
        // buttonPlayTestTone
        // 
        buttonPlayTestTone.Location = new Point(201, 105);
        buttonPlayTestTone.Margin = new Padding(3, 2, 3, 2);
        buttonPlayTestTone.Name = "buttonPlayTestTone";
        buttonPlayTestTone.Size = new Size(70, 22);
        buttonPlayTestTone.TabIndex = 14;
        buttonPlayTestTone.Text = "Play";
        buttonPlayTestTone.UseVisualStyleBackColor = true;
        buttonPlayTestTone.Click += ButtonPlayTestTone_Click;
        // 
        // labelHz
        // 
        labelHz.AutoSize = true;
        labelHz.Location = new Point(153, 109);
        labelHz.Name = "labelHz";
        labelHz.Size = new Size(21, 15);
        labelHz.TabIndex = 13;
        labelHz.Text = "Hz";
        // 
        // textBoxTestFrequency
        // 
        textBoxTestFrequency.Location = new Point(79, 106);
        textBoxTestFrequency.Margin = new Padding(3, 2, 3, 2);
        textBoxTestFrequency.Name = "textBoxTestFrequency";
        textBoxTestFrequency.Size = new Size(70, 23);
        textBoxTestFrequency.TabIndex = 12;
        textBoxTestFrequency.Text = "440";
        // 
        // labelTestTone
        // 
        labelTestTone.AutoSize = true;
        labelTestTone.Location = new Point(13, 109);
        labelTestTone.Name = "labelTestTone";
        labelTestTone.Size = new Size(60, 15);
        labelTestTone.TabIndex = 11;
        labelTestTone.Text = "Test Tone:";
        // 
        // trackBarMP3Volume
        // 
        trackBarMP3Volume.Location = new Point(482, 19);
        trackBarMP3Volume.Margin = new Padding(3, 2, 3, 2);
        trackBarMP3Volume.Maximum = 100;
        trackBarMP3Volume.Name = "trackBarMP3Volume";
        trackBarMP3Volume.Orientation = Orientation.Vertical;
        trackBarMP3Volume.Size = new Size(45, 130);
        trackBarMP3Volume.TabIndex = 8;
        trackBarMP3Volume.TickFrequency = 10;
        trackBarMP3Volume.Value = 100;
        trackBarMP3Volume.Scroll += TrackBarMP3Volume_Scroll;
        // 
        // labelMP3Volume
        // 
        labelMP3Volume.AutoSize = true;
        labelMP3Volume.Location = new Point(512, 20);
        labelMP3Volume.Name = "labelMP3Volume";
        labelMP3Volume.Size = new Size(50, 15);
        labelMP3Volume.TabIndex = 7;
        labelMP3Volume.Text = "Volume:";
        // 
        // buttonStopMP3
        // 
        buttonStopMP3.Enabled = false;
        buttonStopMP3.Location = new Point(280, 80);
        buttonStopMP3.Margin = new Padding(3, 2, 3, 2);
        buttonStopMP3.Name = "buttonStopMP3";
        buttonStopMP3.Size = new Size(70, 22);
        buttonStopMP3.TabIndex = 6;
        buttonStopMP3.Text = "Stop";
        buttonStopMP3.UseVisualStyleBackColor = true;
        buttonStopMP3.Click += ButtonStopMP3_Click;
        // 
        // buttonPlayMP3
        // 
        buttonPlayMP3.Location = new Point(201, 80);
        buttonPlayMP3.Margin = new Padding(3, 2, 3, 2);
        buttonPlayMP3.Name = "buttonPlayMP3";
        buttonPlayMP3.Size = new Size(70, 22);
        buttonPlayMP3.TabIndex = 5;
        buttonPlayMP3.Text = "Play";
        buttonPlayMP3.UseVisualStyleBackColor = true;
        buttonPlayMP3.Click += ButtonPlayMP3_Click;
        // 
        // comboBoxMP3Channels
        // 
        comboBoxMP3Channels.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBoxMP3Channels.FormattingEnabled = true;
        comboBoxMP3Channels.Location = new Point(79, 80);
        comboBoxMP3Channels.Margin = new Padding(3, 2, 3, 2);
        comboBoxMP3Channels.Name = "comboBoxMP3Channels";
        comboBoxMP3Channels.Size = new Size(106, 23);
        comboBoxMP3Channels.TabIndex = 10;
        // 
        // labelMP3Channels
        // 
        labelMP3Channels.AutoSize = true;
        labelMP3Channels.Location = new Point(13, 82);
        labelMP3Channels.Name = "labelMP3Channels";
        labelMP3Channels.Size = new Size(59, 15);
        labelMP3Channels.TabIndex = 9;
        labelMP3Channels.Text = "Channels:";
        // 
        // comboBoxMP3Device
        // 
        comboBoxMP3Device.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBoxMP3Device.FormattingEnabled = true;
        comboBoxMP3Device.Location = new Point(79, 50);
        comboBoxMP3Device.Margin = new Padding(3, 2, 3, 2);
        comboBoxMP3Device.Name = "comboBoxMP3Device";
        comboBoxMP3Device.Size = new Size(219, 23);
        comboBoxMP3Device.TabIndex = 4;
        // 
        // labelMP3Device
        // 
        labelMP3Device.AutoSize = true;
        labelMP3Device.Location = new Point(13, 52);
        labelMP3Device.Name = "labelMP3Device";
        labelMP3Device.Size = new Size(45, 15);
        labelMP3Device.TabIndex = 3;
        labelMP3Device.Text = "Device:";
        // 
        // labelMP3File
        // 
        labelMP3File.AutoSize = true;
        labelMP3File.Location = new Point(13, 22);
        labelMP3File.Name = "labelMP3File";
        labelMP3File.Size = new Size(28, 15);
        labelMP3File.TabIndex = 2;
        labelMP3File.Text = "File:";
        // 
        // textBoxMP3File
        // 
        textBoxMP3File.Location = new Point(79, 20);
        textBoxMP3File.Margin = new Padding(3, 2, 3, 2);
        textBoxMP3File.Name = "textBoxMP3File";
        textBoxMP3File.ReadOnly = true;
        textBoxMP3File.Size = new Size(271, 23);
        textBoxMP3File.TabIndex = 1;
        // 
        // buttonBrowseMP3
        // 
        buttonBrowseMP3.Location = new Point(377, 19);
        buttonBrowseMP3.Margin = new Padding(3, 2, 3, 2);
        buttonBrowseMP3.Name = "buttonBrowseMP3";
        buttonBrowseMP3.Size = new Size(88, 22);
        buttonBrowseMP3.TabIndex = 0;
        buttonBrowseMP3.Text = "Browse...";
        buttonBrowseMP3.UseVisualStyleBackColor = true;
        buttonBrowseMP3.Click += ButtonBrowseMP3_Click;
        // 
        // labelStatus
        // 
        labelStatus.AutoSize = true;
        labelStatus.Location = new Point(16, 620);
        labelStatus.Name = "labelStatus";
        labelStatus.Size = new Size(42, 15);
        labelStatus.TabIndex = 6;
        labelStatus.Text = "Status:";
        // 
        // textBoxStatus
        // 
        textBoxStatus.BackColor = SystemColors.ActiveCaption;
        textBoxStatus.Location = new Point(22, 746);
        textBoxStatus.Margin = new Padding(3, 2, 3, 2);
        textBoxStatus.Multiline = true;
        textBoxStatus.Name = "textBoxStatus";
        textBoxStatus.ReadOnly = true;
        textBoxStatus.ScrollBars = ScrollBars.Vertical;
        textBoxStatus.Size = new Size(490, 89);
        textBoxStatus.TabIndex = 7;
        // 
        // progressBarInputLeft
        // 
        progressBarInputLeft.Location = new Point(13, 66);
        progressBarInputLeft.Margin = new Padding(3, 2, 3, 2);
        progressBarInputLeft.Name = "progressBarInputLeft";
        progressBarInputLeft.Size = new Size(240, 20);
        progressBarInputLeft.TabIndex = 9;
        // 
        // progressBarInputRight
        // 
        progressBarInputRight.Location = new Point(13, 91);
        progressBarInputRight.Margin = new Padding(3, 2, 3, 2);
        progressBarInputRight.Name = "progressBarInputRight";
        progressBarInputRight.Size = new Size(240, 20);
        progressBarInputRight.TabIndex = 10;
        // 
        // labelInputLeft
        // 
        labelInputLeft.AutoSize = true;
        labelInputLeft.Location = new Point(259, 69);
        labelInputLeft.Name = "labelInputLeft";
        labelInputLeft.Size = new Size(62, 15);
        labelInputLeft.TabIndex = 11;
        labelInputLeft.Text = "L: -60.0 dB";
        // 
        // labelInputRight
        // 
        labelInputRight.AutoSize = true;
        labelInputRight.Location = new Point(259, 93);
        labelInputRight.Name = "labelInputRight";
        labelInputRight.Size = new Size(63, 15);
        labelInputRight.TabIndex = 12;
        labelInputRight.Text = "R: -60.0 dB";
        // 
        // progressBarOutputLeft
        // 
        progressBarOutputLeft.Location = new Point(13, 66);
        progressBarOutputLeft.Margin = new Padding(3, 2, 3, 2);
        progressBarOutputLeft.Name = "progressBarOutputLeft";
        progressBarOutputLeft.Size = new Size(240, 20);
        progressBarOutputLeft.TabIndex = 14;
        // 
        // progressBarOutputRight
        // 
        progressBarOutputRight.Location = new Point(13, 91);
        progressBarOutputRight.Margin = new Padding(3, 2, 3, 2);
        progressBarOutputRight.Name = "progressBarOutputRight";
        progressBarOutputRight.Size = new Size(240, 20);
        progressBarOutputRight.TabIndex = 15;
        // 
        // labelOutputLeft
        // 
        labelOutputLeft.AutoSize = true;
        labelOutputLeft.Location = new Point(259, 69);
        labelOutputLeft.Name = "labelOutputLeft";
        labelOutputLeft.Size = new Size(62, 15);
        labelOutputLeft.TabIndex = 16;
        labelOutputLeft.Text = "L: -60.0 dB";
        // 
        // labelOutputRight
        // 
        labelOutputRight.AutoSize = true;
        labelOutputRight.Location = new Point(259, 93);
        labelOutputRight.Name = "labelOutputRight";
        labelOutputRight.Size = new Size(63, 15);
        labelOutputRight.TabIndex = 17;
        labelOutputRight.Text = "R: -60.0 dB";
        // 
        // groupBoxMonitor
        // 
        groupBoxMonitor.Controls.Add(labelMonitorVolume);
        groupBoxMonitor.Controls.Add(labelMonitorChannels);
        groupBoxMonitor.Controls.Add(comboBoxMonitorDevice);
        groupBoxMonitor.Controls.Add(comboBoxMonitorChannels);
        groupBoxMonitor.Controls.Add(trackBarMonitorVolume);
        groupBoxMonitor.Controls.Add(buttonStartMonitor);
        groupBoxMonitor.Controls.Add(buttonStopMonitor);
        groupBoxMonitor.Controls.Add(buttonMuteMonitor);
        groupBoxMonitor.Controls.Add(progressBarMonitorLeft);
        groupBoxMonitor.Controls.Add(labelMonitorLeft);
        groupBoxMonitor.Controls.Add(progressBarMonitorRight);
        groupBoxMonitor.Controls.Add(labelMonitorRight);
        groupBoxMonitor.Location = new Point(22, 393);
        groupBoxMonitor.Name = "groupBoxMonitor";
        groupBoxMonitor.Size = new Size(490, 172);
        groupBoxMonitor.TabIndex = 31;
        groupBoxMonitor.TabStop = false;
        groupBoxMonitor.Text = "Monitor Input";
        // 
        // labelMonitorVolume
        // 
        labelMonitorVolume.AutoSize = true;
        labelMonitorVolume.Location = new Point(330, 125);
        labelMonitorVolume.Name = "labelMonitorVolume";
        labelMonitorVolume.Size = new Size(81, 15);
        labelMonitorVolume.TabIndex = 27;
        labelMonitorVolume.Text = "Input Volume:";
        // 
        // labelMonitorChannels
        // 
        labelMonitorChannels.AutoSize = true;
        labelMonitorChannels.Location = new Point(259, 25);
        labelMonitorChannels.Name = "labelMonitorChannels";
        labelMonitorChannels.Size = new Size(59, 15);
        labelMonitorChannels.TabIndex = 29;
        labelMonitorChannels.Text = "Channels:";
        // 
        // comboBoxMonitorDevice
        // 
        comboBoxMonitorDevice.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBoxMonitorDevice.FormattingEnabled = true;
        comboBoxMonitorDevice.Location = new Point(12, 22);
        comboBoxMonitorDevice.Margin = new Padding(3, 2, 3, 2);
        comboBoxMonitorDevice.Name = "comboBoxMonitorDevice";
        comboBoxMonitorDevice.Size = new Size(241, 23);
        comboBoxMonitorDevice.TabIndex = 32;
        comboBoxMonitorDevice.DropDown += ComboBoxMonitorDevice_DropDown;
        comboBoxMonitorDevice.SelectedIndexChanged += ComboBoxMonitorDevice_SelectedIndexChanged;
        // 
        // comboBoxMonitorChannels
        // 
        comboBoxMonitorChannels.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBoxMonitorChannels.FormattingEnabled = true;
        comboBoxMonitorChannels.Location = new Point(330, 21);
        comboBoxMonitorChannels.Margin = new Padding(3, 2, 3, 2);
        comboBoxMonitorChannels.Name = "comboBoxMonitorChannels";
        comboBoxMonitorChannels.Size = new Size(100, 23);
        comboBoxMonitorChannels.TabIndex = 33;
        comboBoxMonitorChannels.SelectedIndexChanged += ComboBoxMonitorChannels_SelectedIndexChanged;
        // 
        // trackBarMonitorVolume
        // 
        trackBarMonitorVolume.Location = new Point(439, 21);
        trackBarMonitorVolume.Margin = new Padding(3, 2, 3, 2);
        trackBarMonitorVolume.Maximum = 100;
        trackBarMonitorVolume.Name = "trackBarMonitorVolume";
        trackBarMonitorVolume.Orientation = Orientation.Vertical;
        trackBarMonitorVolume.Size = new Size(45, 130);
        trackBarMonitorVolume.TabIndex = 34;
        trackBarMonitorVolume.TickFrequency = 10;
        trackBarMonitorVolume.Value = 100;
        trackBarMonitorVolume.Scroll += TrackBarMonitorVolume_Scroll;
        // 
        // buttonStartMonitor
        // 
        buttonStartMonitor.Location = new Point(13, 125);
        buttonStartMonitor.Margin = new Padding(3, 2, 3, 2);
        buttonStartMonitor.Name = "buttonStartMonitor";
        buttonStartMonitor.Size = new Size(86, 26);
        buttonStartMonitor.TabIndex = 36;
        buttonStartMonitor.Text = "Start";
        buttonStartMonitor.UseVisualStyleBackColor = true;
        buttonStartMonitor.Click += ButtonStartMonitor_Click;
        // 
        // buttonStopMonitor
        // 
        buttonStopMonitor.Enabled = false;
        buttonStopMonitor.Location = new Point(167, 125);
        buttonStopMonitor.Margin = new Padding(3, 2, 3, 2);
        buttonStopMonitor.Name = "buttonStopMonitor";
        buttonStopMonitor.Size = new Size(86, 26);
        buttonStopMonitor.TabIndex = 37;
        buttonStopMonitor.Text = "Stop";
        buttonStopMonitor.UseVisualStyleBackColor = true;
        buttonStopMonitor.Click += ButtonStopMonitor_Click;
        // 
        // buttonMuteMonitor
        // 
        buttonMuteMonitor.Location = new Point(330, 61);
        buttonMuteMonitor.Margin = new Padding(3, 2, 3, 2);
        buttonMuteMonitor.Name = "buttonMuteMonitor";
        buttonMuteMonitor.Size = new Size(100, 47);
        buttonMuteMonitor.TabIndex = 35;
        buttonMuteMonitor.Text = "Mute";
        buttonMuteMonitor.UseVisualStyleBackColor = true;
        buttonMuteMonitor.Click += ButtonMuteMonitor_Click;
        // 
        // progressBarMonitorLeft
        // 
        progressBarMonitorLeft.Location = new Point(13, 66);
        progressBarMonitorLeft.Margin = new Padding(3, 2, 3, 2);
        progressBarMonitorLeft.Name = "progressBarMonitorLeft";
        progressBarMonitorLeft.Size = new Size(240, 20);
        progressBarMonitorLeft.TabIndex = 38;
        // 
        // labelMonitorLeft
        // 
        labelMonitorLeft.AutoSize = true;
        labelMonitorLeft.Location = new Point(259, 69);
        labelMonitorLeft.Name = "labelMonitorLeft";
        labelMonitorLeft.Size = new Size(62, 15);
        labelMonitorLeft.TabIndex = 39;
        labelMonitorLeft.Text = "L: -60.0 dB";
        // 
        // progressBarMonitorRight
        // 
        progressBarMonitorRight.Location = new Point(13, 91);
        progressBarMonitorRight.Margin = new Padding(3, 2, 3, 2);
        progressBarMonitorRight.Name = "progressBarMonitorRight";
        progressBarMonitorRight.Size = new Size(240, 20);
        progressBarMonitorRight.TabIndex = 40;
        // 
        // labelMonitorRight
        // 
        labelMonitorRight.AutoSize = true;
        labelMonitorRight.Location = new Point(259, 93);
        labelMonitorRight.Name = "labelMonitorRight";
        labelMonitorRight.Size = new Size(63, 15);
        labelMonitorRight.TabIndex = 41;
        labelMonitorRight.Text = "R: -60.0 dB";
        // 
        // groupBoxInput
        // 
        groupBoxInput.Controls.Add(comboBoxInputDevice);
        groupBoxInput.Controls.Add(labelInputChannels);
        groupBoxInput.Controls.Add(comboBoxInputChannels);
        groupBoxInput.Controls.Add(labelInputVolume);
        groupBoxInput.Controls.Add(trackBarInputVolume);
        groupBoxInput.Controls.Add(buttonStart);
        groupBoxInput.Controls.Add(buttonStop);
        groupBoxInput.Controls.Add(buttonMuteInput);
        groupBoxInput.Controls.Add(labelInputLevel);
        groupBoxInput.Controls.Add(progressBarInputLeft);
        groupBoxInput.Controls.Add(labelInputLeft);
        groupBoxInput.Controls.Add(progressBarInputRight);
        groupBoxInput.Controls.Add(labelInputRight);
        groupBoxInput.Location = new Point(22, 27);
        groupBoxInput.Name = "groupBoxInput";
        groupBoxInput.Size = new Size(490, 172);
        groupBoxInput.TabIndex = 30;
        groupBoxInput.TabStop = false;
        groupBoxInput.Text = "Input";
        // 
        // pictureBoxSpectrum
        // 
        pictureBoxSpectrum.BackColor = SystemColors.ControlDark;
        pictureBoxSpectrum.Location = new Point(39, 62);
        pictureBoxSpectrum.Name = "pictureBoxSpectrum";
        pictureBoxSpectrum.Size = new Size(275, 205);
        pictureBoxSpectrum.TabIndex = 50;
        pictureBoxSpectrum.TabStop = false;
        pictureBoxSpectrum.Paint += PictureBoxSpectrum_Paint;

        // spectrum sliders and labels (freq min, freq max, dB floor)
        trackBarFreqMin = new TrackBar();
        trackBarFreqMin.Orientation = Orientation.Horizontal;
        trackBarFreqMin.Minimum = 20;
        trackBarFreqMin.Maximum = 20000;
        trackBarFreqMin.TickFrequency = 10;
        trackBarFreqMin.Value = 80;
        trackBarFreqMin.SmallChange = 1;
        trackBarFreqMin.LargeChange = 10;
        trackBarFreqMin.Location = new Point(39, 270);
        trackBarFreqMin.Size = new Size(130, 45);
        trackBarFreqMin.Scroll += TrackBarFreqMin_Scroll;

        trackBarFreqMax = new TrackBar();
        trackBarFreqMax.Orientation = Orientation.Horizontal;
        trackBarFreqMax.Minimum = 20;
        trackBarFreqMax.Maximum = 24000;
        trackBarFreqMax.TickFrequency = 1000;
        trackBarFreqMax.Value = 10000;
        trackBarFreqMax.SmallChange = 10;
        trackBarFreqMax.LargeChange = 100;
        trackBarFreqMax.Location = new Point(184, 270);
        trackBarFreqMax.Size = new Size(130, 45);
        trackBarFreqMax.Scroll += TrackBarFreqMax_Scroll;

        trackBarDbMin = new TrackBar();
        trackBarDbMin.Orientation = Orientation.Vertical;
        trackBarDbMin.Minimum = -120;
        trackBarDbMin.Maximum = 0;
        trackBarDbMin.TickFrequency = 10;
        trackBarDbMin.Value = -60;
        trackBarDbMin.SmallChange = 1;
        trackBarDbMin.LargeChange = 10;
        trackBarDbMin.Location = new Point(330, 62);
        trackBarDbMin.Size = new Size(45, 200);
        trackBarDbMin.Scroll += TrackBarDbMin_Scroll;

        labelFreqMin = new Label();
        labelFreqMin.AutoSize = true;
        labelFreqMin.Location = new Point(39, 246);
        labelFreqMin.Name = "labelFreqMin";
        labelFreqMin.Size = new Size(80, 15);
        labelFreqMin.TabIndex = 51;
        labelFreqMin.Text = "Min: 80 Hz";

        labelFreqMax = new Label();
        labelFreqMax.AutoSize = true;
        labelFreqMax.Location = new Point(184, 246);
        labelFreqMax.Name = "labelFreqMax";
        labelFreqMax.Size = new Size(80, 15);
        labelFreqMax.TabIndex = 52;
        labelFreqMax.Text = "Max: 10000 Hz";

        labelDbMin = new Label();
        labelDbMin.AutoSize = true;
        labelDbMin.Location = new Point(330, 268);
        labelDbMin.Name = "labelDbMin";
        labelDbMin.Size = new Size(60, 15);
        labelDbMin.TabIndex = 53;
        labelDbMin.Text = "Floor: -60 dB";
        // 
        // labelInputVolume
        // 
        labelInputVolume.AutoSize = true;
        labelInputVolume.Location = new Point(330, 125);
        labelInputVolume.Name = "labelInputVolume";
        labelInputVolume.Size = new Size(81, 15);
        labelInputVolume.TabIndex = 25;
        labelInputVolume.Text = "Input Volume:";
        // 
        // labelInputLevel
        // 
        labelInputLevel.AutoSize = true;
        labelInputLevel.Location = new Point(13, 47);
        labelInputLevel.Name = "labelInputLevel";
        labelInputLevel.Size = new Size(73, 15);
        labelInputLevel.TabIndex = 8;
        labelInputLevel.Text = "Input Levels:";
        // 
        // groupBoxOutput
        // 
        groupBoxOutput.Controls.Add(comboBoxOutputDevice);
        groupBoxOutput.Controls.Add(labelOutputChannels);
        groupBoxOutput.Controls.Add(comboBoxOutputChannels);
        groupBoxOutput.Controls.Add(labelOutputVolume);
        groupBoxOutput.Controls.Add(trackBarOutputVolume);
        groupBoxOutput.Controls.Add(buttonMuteOutput);
        groupBoxOutput.Controls.Add(labelOutputLevel);
        groupBoxOutput.Controls.Add(progressBarOutputLeft);
        groupBoxOutput.Controls.Add(labelOutputLeft);
        groupBoxOutput.Controls.Add(progressBarOutputRight);
        groupBoxOutput.Controls.Add(labelOutputRight);
        groupBoxOutput.Location = new Point(22, 205);
        groupBoxOutput.Name = "groupBoxOutput";
        groupBoxOutput.Size = new Size(490, 172);
        groupBoxOutput.TabIndex = 31;
        groupBoxOutput.TabStop = false;
        groupBoxOutput.Text = "Output";
        // 
        // labelOutputChannels
        // 
        labelOutputChannels.AutoSize = true;
        labelOutputChannels.Location = new Point(259, 25);
        labelOutputChannels.Name = "labelOutputChannels";
        labelOutputChannels.Size = new Size(59, 15);
        labelOutputChannels.TabIndex = 21;
        labelOutputChannels.Text = "Channels:";
        // 
        // labelOutputVolume
        // 
        labelOutputVolume.AutoSize = true;
        labelOutputVolume.Location = new Point(330, 125);
        labelOutputVolume.Name = "labelOutputVolume";
        labelOutputVolume.Size = new Size(91, 15);
        labelOutputVolume.TabIndex = 27;
        labelOutputVolume.Text = "Output Volume:";
        // 
        // labelOutputLevel
        // 
        labelOutputLevel.AutoSize = true;
        labelOutputLevel.Location = new Point(13, 47);
        labelOutputLevel.Name = "labelOutputLevel";
        labelOutputLevel.Size = new Size(83, 15);
        labelOutputLevel.TabIndex = 13;
        labelOutputLevel.Text = "Output Levels:";
        // 
        // groupBox1
        // 
        groupBox1.Controls.Add(pictureBoxSpectrum);
        groupBox1.Controls.Add(trackBarFreqMin);
        groupBox1.Controls.Add(trackBarFreqMax);
        groupBox1.Controls.Add(trackBarDbMin);
        groupBox1.Controls.Add(labelFreqMin);
        groupBox1.Controls.Add(labelFreqMax);
        groupBox1.Controls.Add(labelDbMin);
        groupBox1.Location = new Point(524, 212);
        groupBox1.Name = "groupBox1";
        groupBox1.Size = new Size(346, 289);
        groupBox1.TabIndex = 32;
        groupBox1.TabStop = false;
        groupBox1.Text = "groupBox1";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(882, 851);
        Controls.Add(groupBox1);
        Controls.Add(checkBoxIncludeASIO4ALL);
        Controls.Add(groupBoxMP3);
        Controls.Add(groupBoxInput);
        Controls.Add(groupBoxOutput);
        Controls.Add(textBoxStatus);
        Controls.Add(labelStatus);
        Controls.Add(groupBoxMonitor);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
        Margin = new Padding(3, 2, 3, 2);
        Name = "Form1";
        Text = "ASIO Audio Router";
        FormClosing += Form1_FormClosing;
        Load += Form1_Load;
        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)trackBarInputVolume).EndInit();
        ((System.ComponentModel.ISupportInitialize)trackBarOutputVolume).EndInit();
        groupBoxMP3.ResumeLayout(false);
        groupBoxMP3.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)trackBarMP3Volume).EndInit();
        groupBoxMonitor.ResumeLayout(false);
        groupBoxMonitor.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)trackBarMonitorVolume).EndInit();
        groupBoxInput.ResumeLayout(false);
        groupBoxInput.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pictureBoxSpectrum).EndInit();
        groupBoxOutput.ResumeLayout(false);
        groupBoxOutput.PerformLayout();
        groupBox1.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private MenuStrip menuStrip;
    private ToolStripMenuItem fileToolStripMenuItem;
    private ToolStripMenuItem exitToolStripMenuItem;
    private CheckBox checkBoxIncludeASIO4ALL;
    private ComboBox comboBoxInputDevice;
    private Label labelInputChannels;
    private ComboBox comboBoxInputChannels;
    private ComboBox comboBoxOutputDevice;
    private ComboBox comboBoxOutputChannels;
    private Button buttonStart;
    private Button buttonStop;
    private Button buttonMuteInput;
    private Button buttonMuteOutput;
    private TrackBar trackBarInputVolume;
    private TrackBar trackBarOutputVolume;
    private GroupBox groupBoxMP3;
    private Button buttonBrowseMP3;
    private TextBox textBoxMP3File;
    private Label labelMP3File;
    private Label labelMP3Device;
    private ComboBox comboBoxMP3Device;
    private Label labelMP3Channels;
    private ComboBox comboBoxMP3Channels;
    private Button buttonPlayMP3;
    private Button buttonStopMP3;
    private Label labelMP3Volume;
    private TrackBar trackBarMP3Volume;
    private Label labelTestTone;
    private TextBox textBoxTestFrequency;
    private Label labelHz;
    private Button buttonPlayTestTone;
    private Button buttonStopTestTone;
    private Label labelStatus;
    private TextBox textBoxStatus;
    private ProgressBar progressBarInputLeft;
    private ProgressBar progressBarInputRight;
    private Label labelInputLeft;
    private Label labelInputRight;
    private ProgressBar progressBarOutputLeft;
    private ProgressBar progressBarOutputRight;
    private Label labelOutputLeft;
    private Label labelOutputRight;
    private GroupBox groupBoxMonitor;
    private ComboBox comboBoxMonitorDevice;
    private ComboBox comboBoxMonitorChannels;
    private TrackBar trackBarMonitorVolume;
    private Button buttonStartMonitor;
    private Button buttonStopMonitor;
    private Button buttonMuteMonitor;
    private ProgressBar progressBarMonitorLeft;
    private ProgressBar progressBarMonitorRight;
    private Label labelMonitorLeft;
    private Label labelMonitorRight;
    private GroupBox groupBoxInput;
    private GroupBox groupBoxOutput;
    private Label labelInputVolume;
    private Label labelInputLevel;
    private Label labelOutputChannels;
    private Label labelOutputVolume;
    private Label labelOutputLevel;
    private Label labelMonitorChannels;
    private Label labelMonitorVolume;
    // spectrum display
    private PictureBox pictureBoxSpectrum;
    private TrackBar trackBarFreqMin;
    private TrackBar trackBarFreqMax;
    private TrackBar trackBarDbMin;
    private Label labelFreqMin;
    private Label labelFreqMax;
    private Label labelDbMin;
    private GroupBox groupBox1;
}
