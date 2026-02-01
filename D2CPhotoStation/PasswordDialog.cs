using System;
using System.Drawing;
using System.Windows.Forms;

namespace D2CPhotoStation
{
    public sealed class PasswordDialog : Form
    {
        private readonly TextBox _txtUsername;
        private readonly TextBox _txtPassword;

        public string Username => (_txtUsername.Text ?? "").Trim();
        public string Password => _txtPassword.Text ?? "";

        public PasswordDialog()
        {
            Text = "Settings Login";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(360, 170);

            var lblU = new Label { Text = "Username:", Location = new Point(12, 18), Size = new Size(90, 20) };
            var lblP = new Label { Text = "Password:", Location = new Point(12, 58), Size = new Size(90, 20) };

            _txtUsername = new TextBox { Location = new Point(110, 15), Size = new Size(230, 23) };
            _txtPassword = new TextBox { Location = new Point(110, 55), Size = new Size(230, 23), UseSystemPasswordChar = true };

            var btnOk = new Button { Text = "OK", Location = new Point(184, 110), Size = new Size(75, 27), DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Cancel", Location = new Point(265, 110), Size = new Size(75, 27), DialogResult = DialogResult.Cancel };

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[] { lblU, lblP, _txtUsername, _txtPassword, btnOk, btnCancel });
        }
    }
}
