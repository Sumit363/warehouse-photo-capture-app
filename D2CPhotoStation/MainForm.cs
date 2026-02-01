using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace D2CPhotoStation
{
    public sealed class MainForm : Form
    {
        // =========================
        // UI
        // =========================
        private PictureBox picLive;
        private PictureBox picFront;
        private PictureBox picBack;

        private Button btnTakePicture;
        private Button btnSave;
        private Button btnResetFront;
        private Button btnResetBack;
        private Button btnSettings;
        private Button btnExit;

        private TextBox txtCommand;
        private Label lblCommandHint;

        // Header background image reference (dispose if loaded from file)
        private Image _headerImage;

        // =========================
        // Camera
        // =========================
        private FilterInfoCollection _videoDevices;
        private VideoCaptureDevice _videoSource;

        // Latest frame buffer
        private Bitmap _latestFrame;
        private readonly object _frameLock = new object();

        // Accessibility: increase button font size by ~+6
        private readonly Font _buttonFont = new Font("Segoe UI", 14f, FontStyle.Regular);

        public MainForm()
        {
            Text = "D2C Photo Capture V1.0";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;

            InitializeUi();

            Load += (s, e) => StartCamera();
            FormClosing += (s, e) => SafeShutdown();

            // Scanner-friendly focus
            Shown += (s, e) => txtCommand?.Focus();
            Click += (s, e) => txtCommand?.Focus();
        }

        // =========================
        // UI BUILD
        // =========================
        private void InitializeUi()
        {
            // ---------- HEADER ----------
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110
            };
            LoadHeaderImageIntoPanel(headerPanel);

            // ---------- SETTINGS BAR ----------
            var settingsBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(12, 10, 12, 10)
            };

            btnSettings = CreateBigButton("Settings", 160, 46);
            btnSettings.Click += (s, e) => OpenSettings();
            settingsBar.Controls.Add(btnSettings);

            // ---------- MAIN LAYOUT ----------
            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(12)
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

            // ---------- LIVE CAMERA ----------
            var liveGroup = new GroupBox { Text = "Live Camera", Dock = DockStyle.Fill, Padding = new Padding(10) };
            picLive = new PictureBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom };
            liveGroup.Controls.Add(picLive);

            // ---------- CAPTURED PHOTOS ----------
            var capGroup = new GroupBox { Text = "Captured Photos", Dock = DockStyle.Fill, Padding = new Padding(10) };

            var capLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };
            capLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            capLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            capLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 85));
            capLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 15));

            picFront = new PictureBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom };
            picBack = new PictureBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom };

            capLayout.Controls.Add(BuildImagePanel("Front", picFront), 0, 0);
            capLayout.Controls.Add(BuildImagePanel("Back", picBack), 1, 0);

            btnResetFront = CreateBigButton("Reset Front", 0, 0);
            btnResetBack = CreateBigButton("Reset Back", 0, 0);
            btnResetFront.Dock = DockStyle.Fill;
            btnResetBack.Dock = DockStyle.Fill;

            btnResetFront.Click += (s, e) => ClearPictureBox(picFront);
            btnResetBack.Click += (s, e) => ClearPictureBox(picBack);

            capLayout.Controls.Add(btnResetFront, 0, 1);
            capLayout.Controls.Add(btnResetBack, 1, 1);

            capGroup.Controls.Add(capLayout);

            // ---------- CONTROLS (buttons top, command under) ----------
            var controlGroup = new GroupBox { Text = "Controls", Dock = DockStyle.Fill, Padding = new Padding(12) };

            var controlsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3
            };
            controlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            controlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            controlsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 0, 0, 8),
                AutoSize = true
            };

            btnTakePicture = CreateBigButton("Take Picture", 190, 48);
            btnSave = CreateBigButton("Save", 150, 48);
            btnExit = CreateBigButton("Exit", 150, 48);

            btnTakePicture.Click += (s, e) => TakePicture();
            btnSave.Click += (s, e) => SaveImages();
            btnExit.Click += (s, e) => Close();

            buttonsPanel.Controls.AddRange(new Control[] { btnTakePicture, btnSave, btnExit });

            lblCommandHint = new Label
            {
                Text = "Scan/Manual Command (Press Enter): TakePicture | Save | Exit | ResetFront | ResetBack",
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                Dock = DockStyle.Fill,
                Padding = new Padding(2, 4, 2, 2)
            };

            txtCommand = new TextBox
            {
                Font = new Font("Segoe UI", 14f, FontStyle.Regular),
                Dock = DockStyle.Top,
                Height = 36
            };
            txtCommand.KeyDown += TxtCommand_KeyDown;

            controlsLayout.Controls.Add(buttonsPanel, 0, 0);
            controlsLayout.Controls.Add(lblCommandHint, 0, 1);
            controlsLayout.Controls.Add(txtCommand, 0, 2);

            controlGroup.Controls.Add(controlsLayout);

            // ---------- PLACE ----------
            main.Controls.Add(liveGroup, 0, 0);
            main.SetRowSpan(liveGroup, 2);
            main.Controls.Add(capGroup, 1, 0);
            main.Controls.Add(controlGroup, 1, 1);

            Controls.Add(main);
            Controls.Add(settingsBar);
            Controls.Add(headerPanel);
        }

        private Button CreateBigButton(string text, int width, int height)
        {
            var b = new Button
            {
                Text = text,
                Font = _buttonFont,
                AutoSize = false
            };

            if (width > 0) b.Width = width;
            if (height > 0) b.Height = height;

            return b;
        }

        private Control BuildImagePanel(string title, PictureBox pb)
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var lbl = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // keep label above the picturebox
            pb.Dock = DockStyle.Fill;

            panel.Controls.Add(pb);
            panel.Controls.Add(lbl);

            return panel;
        }

        // =========================
        // Header (stretched background)
        // =========================
        private void LoadHeaderImageIntoPanel(Panel headerPanel)
        {
            headerPanel.BackgroundImageLayout = ImageLayout.Stretch;

            // Dispose previously loaded file-based header image
            if (_headerImage != null)
            {
                try { _headerImage.Dispose(); } catch { }
                _headerImage = null;
            }

            // Preferred: Resources
            try
            {
                _headerImage = Properties.Resources.ctdi_Header_Logo;
                headerPanel.BackgroundImage = _headerImage;
                return;
            }
            catch
            {
                // ignore and try fallback
            }

            // Fallback: EXE directory file
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string fallbackPath = Path.Combine(exeDir, "ctdi_Header_Logo.png");

                if (File.Exists(fallbackPath))
                {
                    using (var fs = new FileStream(fallbackPath, FileMode.Open, FileAccess.Read))
                    using (var temp = Image.FromStream(fs))
                    {
                        _headerImage = new Bitmap(temp);
                    }
                    headerPanel.BackgroundImage = _headerImage;
                }
            }
            catch
            {
                // keep blank if cannot load
            }
        }

        // =========================
        // Command box
        // =========================
        private void TxtCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;

            e.SuppressKeyPress = true; // no ding
            ProcessCommand(txtCommand.Text);
            txtCommand.Clear();
            txtCommand.Focus();
        }

        private void ProcessCommand(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return;

            string cmd = raw.Trim();
            string normalized = cmd.Replace(" ", "").ToLowerInvariant();

            switch (normalized)
            {
                case "takepicture":
                case "capture":
                case "snap":
                    TakePicture();
                    break;

                case "save":
                    SaveImages();
                    break;

                case "exit":
                case "quit":
                    Close();
                    break;

                case "resetfront":
                case "clearfront":
                    ClearPictureBox(picFront);
                    break;

                case "resetback":
                case "clearback":
                    ClearPictureBox(picBack);
                    break;

                case "settings":
                    OpenSettings();
                    break;

                default:
                    MessageBox.Show($"Unknown command: {cmd}", "Command", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
            }
        }

        // =========================
        // Camera
        // =========================
        private void StartCamera()
        {
            try
            {
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (_videoDevices.Count == 0)
                {
                    MessageBox.Show("No camera devices found.", "Camera", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string monikerToUse = null;

                if (!string.IsNullOrWhiteSpace(AppState.SelectedCameraMoniker))
                {
                    foreach (FilterInfo fi in _videoDevices)
                    {
                        if (string.Equals(fi.MonikerString, AppState.SelectedCameraMoniker, StringComparison.OrdinalIgnoreCase))
                        {
                            monikerToUse = fi.MonikerString;
                            break;
                        }
                    }
                }

                if (monikerToUse == null)
                {
                    monikerToUse = _videoDevices[0].MonikerString;
                    AppState.SelectedCameraMoniker = monikerToUse;
                }

                StopCamera();

                _videoSource = new VideoCaptureDevice(monikerToUse);
                _videoSource.NewFrame += VideoSource_NewFrame;
                _videoSource.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to start camera:\n" + ex.Message, "Camera", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopCamera()
        {
            try
            {
                if (_videoSource != null)
                {
                    _videoSource.NewFrame -= VideoSource_NewFrame;

                    if (_videoSource.IsRunning)
                    {
                        _videoSource.SignalToStop();
                        _videoSource.WaitForStop();
                    }

                    _videoSource = null;
                }
            }
            catch
            {
                // swallow exceptions during shutdown
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap frameClone;
            try
            {
                frameClone = (Bitmap)eventArgs.Frame.Clone();
            }
            catch
            {
                return;
            }

            // store latest frame safely
            lock (_frameLock)
            {
                _latestFrame?.Dispose();
                _latestFrame = (Bitmap)frameClone.Clone();
            }

            // update UI safely
            if (!picLive.IsHandleCreated)
            {
                frameClone.Dispose();
                return;
            }

            try
            {
                picLive.BeginInvoke(new Action(() =>
                {
                    var old = picLive.Image;
                    picLive.Image = frameClone;
                    old?.Dispose();
                }));
            }
            catch
            {
                frameClone.Dispose();
            }
        }

        private Bitmap Snapshot()
        {
            lock (_frameLock)
            {
                if (_latestFrame == null) return null;
                return (Bitmap)_latestFrame.Clone();
            }
        }

        // =========================
        // Capture / Save workflow
        // =========================
        private void TakePicture()
        {
            var snap = Snapshot();
            if (snap == null)
            {
                MessageBox.Show("No camera frame available yet. Please wait a moment.", "Capture", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (picFront.Image == null)
            {
                SetPictureBoxImage(picFront, snap);
                return;
            }

            if (picBack.Image == null)
            {
                SetPictureBoxImage(picBack, snap);
                return;
            }

            var choice = MessageBox.Show(
                "Both Front and Back are already captured.\n\nYes = overwrite Front\nNo = overwrite Back\nCancel = do nothing",
                "Overwrite?",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (choice == DialogResult.Yes)
                SetPictureBoxImage(picFront, snap);
            else if (choice == DialogResult.No)
                SetPictureBoxImage(picBack, snap);
            else
                snap.Dispose();
        }

        private void SaveImages()
        {
            if (picFront.Image == null || picBack.Image == null)
            {
                MessageBox.Show("Please capture BOTH Front and Back images before saving.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dlg = new InputDialog("Folder Name", "Enter folder name for this device:"))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                string folderName = (dlg.Value ?? "").Trim();
                if (string.IsNullOrWhiteSpace(folderName))
                {
                    MessageBox.Show("Folder name cannot be empty.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string basePath = AppState.BaseSavePath ?? @"C:\Users\sumit\OneDrive\Desktop\D2C\";
                if (!basePath.EndsWith(@"\")) basePath += @"\";

                string deviceFolder = Path.Combine(basePath, folderName);
                Directory.CreateDirectory(deviceFolder);

                string frontPath = Path.Combine(deviceFolder, "Front.jpg");
                string backPath = Path.Combine(deviceFolder, "Back.jpg");

                try
                {
                    SaveJpeg(picFront.Image, frontPath, 92L);
                    SaveJpeg(picBack.Image, backPath, 92L);

                    MessageBox.Show($"Saved:\n{frontPath}\n{backPath}", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    ClearPictureBox(picFront);
                    ClearPictureBox(picBack);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Save failed:\n" + ex.Message, "Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OpenSettings()
        {
            using (var pwd = new PasswordDialog())
            {
                if (pwd.ShowDialog(this) != DialogResult.OK) return;

                if (!string.Equals(pwd.Username, AppState.SettingsUsername, StringComparison.OrdinalIgnoreCase) ||
                    pwd.Password != AppState.SettingsPassword)
                {
                    MessageBox.Show("Invalid credentials.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            using (var s = new SettingsForm())
            {
                if (s.ShowDialog(this) == DialogResult.OK)
                {
                    bool cameraChanged = !string.Equals(s.SelectedCameraMoniker, AppState.SelectedCameraMoniker, StringComparison.OrdinalIgnoreCase);

                    AppState.BaseSavePath = s.BaseSavePath;
                    AppState.SelectedCameraMoniker = s.SelectedCameraMoniker;

                    if (cameraChanged)
                        StartCamera();
                }
            }
        }

        private void SaveJpeg(Image image, string path, long quality)
        {
            var jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            if (jpgEncoder == null)
            {
                image.Save(path, ImageFormat.Jpeg);
                return;
            }

            using (var encParams = new EncoderParameters(1))
            {
                encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                image.Save(path, jpgEncoder, encParams);
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var c in codecs)
            {
                if (c.FormatID == format.Guid) return c;
            }
            return null;
        }

        private void SetPictureBoxImage(PictureBox pb, Bitmap newImage)
        {
            var old = pb.Image;
            pb.Image = newImage;
            old?.Dispose();
        }

        private void ClearPictureBox(PictureBox pb)
        {
            var old = pb.Image;
            pb.Image = null;
            old?.Dispose();
        }

        // =========================
        // Shutdown
        // =========================
        private void SafeShutdown()
        {
            StopCamera();

            lock (_frameLock)
            {
                _latestFrame?.Dispose();
                _latestFrame = null;
            }

            if (picLive.Image != null) { picLive.Image.Dispose(); picLive.Image = null; }
            if (picFront.Image != null) { picFront.Image.Dispose(); picFront.Image = null; }
            if (picBack.Image != null) { picBack.Image.Dispose(); picBack.Image = null; }

            if (_headerImage != null)
            {
                try { _headerImage.Dispose(); } catch { }
                _headerImage = null;
            }
        }
    }
}
