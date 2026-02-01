using AForge.Video.DirectShow;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace D2CPhotoStation
{
    public sealed class SettingsForm : Form
    {
        private readonly ComboBox _cmbCameras;
        private readonly TextBox _txtBasePath;
        private FilterInfoCollection _devices;

        public string SelectedCameraMoniker { get; private set; }
        public string BaseSavePath { get; private set; }

        public SettingsForm()
        {
            Text = "Settings";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(560, 210);

            var lblCam = new Label { Text = "Camera:", Location = new Point(12, 18), Size = new Size(100, 20) };
            _cmbCameras = new ComboBox
            {
                Location = new Point(120, 15),
                Size = new Size(420, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblPath = new Label { Text = "Destination base folder:", Location = new Point(12, 60), Size = new Size(160, 20) };
            _txtBasePath = new TextBox { Location = new Point(180, 57), Size = new Size(280, 23) };
            var btnBrowse = new Button { Text = "Browse...", Location = new Point(470, 56), Size = new Size(70, 25) };

            var hint = new Label
            {
                Text = "Saved device folders will be created under the base folder:\n<Base>\\<FolderName>\\Front.jpg and Back.jpg",
                Location = new Point(12, 92),
                Size = new Size(528, 42)
            };

            var btnOk = new Button { Text = "OK", Location = new Point(384, 160), Size = new Size(75, 27) };
            var btnCancel = new Button { Text = "Cancel", Location = new Point(465, 160), Size = new Size(75, 27), DialogResult = DialogResult.Cancel };

            btnBrowse.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Select base folder where device folders will be created.";
                    if (Directory.Exists(_txtBasePath.Text)) fbd.SelectedPath = _txtBasePath.Text;

                    if (fbd.ShowDialog(this) == DialogResult.OK)
                        _txtBasePath.Text = fbd.SelectedPath;
                }
            };

            btnOk.Click += (s, e) =>
            {
                if (_cmbCameras.SelectedItem == null)
                {
                    MessageBox.Show("Please select a camera.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var basePath = (_txtBasePath.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(basePath))
                {
                    MessageBox.Show("Please select a destination base folder.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!basePath.EndsWith(@"\")) basePath += @"\";

                var cam = (CameraItem)_cmbCameras.SelectedItem;
                SelectedCameraMoniker = cam.Moniker;
                BaseSavePath = basePath;

                DialogResult = DialogResult.OK;
            };

            Controls.AddRange(new Control[] { lblCam, _cmbCameras, lblPath, _txtBasePath, btnBrowse, hint, btnOk, btnCancel });

            Load += SettingsForm_Load;
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            _txtBasePath.Text = AppState.BaseSavePath ?? "";

            _devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            _cmbCameras.Items.Clear();

            int selectedIndex = -1;
            for (int i = 0; i < _devices.Count; i++)
            {
                var d = _devices[i];
                _cmbCameras.Items.Add(new CameraItem(d.Name, d.MonikerString));

                if (!string.IsNullOrWhiteSpace(AppState.SelectedCameraMoniker) &&
                    string.Equals(d.MonikerString, AppState.SelectedCameraMoniker, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                }
            }

            if (_cmbCameras.Items.Count > 0)
                _cmbCameras.SelectedIndex = (selectedIndex >= 0) ? selectedIndex : 0;
        }

        private sealed class CameraItem
        {
            public string Name { get; }
            public string Moniker { get; }

            public CameraItem(string name, string moniker)
            {
                Name = name;
                Moniker = moniker;
            }

            public override string ToString() => Name;
        }
    }
}
