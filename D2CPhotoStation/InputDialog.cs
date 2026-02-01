using System;
using System.Drawing;
using System.Windows.Forms;

namespace D2CPhotoStation
{
    public sealed class InputDialog : Form
    {
        private readonly Label _lblPrompt;
        private readonly TextBox _txtValue;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;

        public string Value => _txtValue.Text;

        public InputDialog(string title, string prompt)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(420, 150);

            _lblPrompt = new Label
            {
                AutoSize = false,
                Text = prompt,
                Location = new Point(12, 12),
                Size = new Size(396, 40)
            };

            _txtValue = new TextBox
            {
                Location = new Point(12, 58),
                Size = new Size(396, 23)
            };

            _btnOk = new Button
            {
                Text = "OK",
                Location = new Point(252, 100),
                Size = new Size(75, 27),
                DialogResult = DialogResult.OK
            };

            _btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(333, 100),
                Size = new Size(75, 27),
                DialogResult = DialogResult.Cancel
            };

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            Controls.AddRange(new Control[] { _lblPrompt, _txtValue, _btnOk, _btnCancel });
        }
    }
}
