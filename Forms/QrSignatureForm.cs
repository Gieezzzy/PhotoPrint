using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using PhotoPrintApp.Services;
using QRCoder;

namespace PhotoPrintApp.Forms;

public sealed class QrSignatureForm : Form
{
    private readonly MobileSignatureServer _server;
    private readonly Action<Bitmap> _onSignatureReceived;

    private ComboBox cmbIpAddress = null!;
    private PictureBox picQr = null!;
    private TextBox txtUrl = null!;
    private Button btnCopyUrl = null!;
    private LinkLabel lnkTestBrowser = null!;
    private Label lblWaitStatus = null!;
    private CheckBox chkAutoClose = null!;
    private Button btnClose = null!;
    private Bitmap? _qrBitmap;

    public QrSignatureForm(Action<Bitmap> onSignatureReceived)
    {
        _onSignatureReceived = onSignatureReceived;
        _server = new MobileSignatureServer();

        InitializeUi();
        StartServerAndGenerateQr();
    }

    private void InitializeUi()
    {
        Text = "Sign via Phone (QR Code)";
        Size = new Size(440, 585);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        var lblTitle = new Label
        {
            Text = "Scan this QR code with your phone's camera",
            Location = new Point(20, 14),
            Size = new Size(385, 24),
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblSub = new Label
        {
            Text = "Make sure your phone is on the same Wi-Fi / local network as this PC.",
            Location = new Point(20, 38),
            Size = new Size(385, 20),
            ForeColor = Color.DimGray,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblIp = new Label
        {
            Text = "Network IP:",
            Location = new Point(24, 68),
            AutoSize = true
        };

        cmbIpAddress = new ComboBox
        {
            Location = new Point(105, 64),
            Width = 300,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbIpAddress.SelectedIndexChanged += (s, e) => UpdateQrCode();

        picQr = new PictureBox
        {
            Location = new Point(62, 100),
            Size = new Size(300, 300),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };

        txtUrl = new TextBox
        {
            Location = new Point(24, 412),
            Width = 295,
            ReadOnly = true,
            BackColor = Color.FromArgb(245, 246, 248)
        };

        btnCopyUrl = new Button
        {
            Text = "Copy",
            Location = new Point(325, 411),
            Size = new Size(80, 24)
        };
        btnCopyUrl.Click += (s, e) =>
        {
            if (!string.IsNullOrEmpty(txtUrl.Text))
            {
                Clipboard.SetText(txtUrl.Text);
                lblWaitStatus.Text = "URL copied to clipboard! Waiting for signature...";
            }
        };

        lnkTestBrowser = new LinkLabel
        {
            Text = "Open in local browser (test)",
            Location = new Point(24, 442),
            AutoSize = true
        };
        lnkTestBrowser.LinkClicked += (s, e) =>
        {
            if (!string.IsNullOrEmpty(txtUrl.Text))
            {
                Process.Start(new ProcessStartInfo(txtUrl.Text) { UseShellExecute = true });
            }
        };

        lblWaitStatus = new Label
        {
            Text = "Waiting for phone signature...",
            Location = new Point(24, 470),
            Size = new Size(381, 30),
            BackColor = Color.FromArgb(232, 240, 254),
            ForeColor = Color.FromArgb(25, 103, 210),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        chkAutoClose = new CheckBox
        {
            Text = "Auto-close when signature is received",
            Location = new Point(24, 510),
            AutoSize = true,
            Checked = true
        };

        btnClose = new Button
        {
            Text = "Close",
            Location = new Point(320, 506),
            Size = new Size(85, 28),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange(new Control[]
        {
            lblTitle, lblSub, lblIp, cmbIpAddress, picQr,
            txtUrl, btnCopyUrl, lnkTestBrowser,
            lblWaitStatus, chkAutoClose, btnClose
        });
    }

    private void StartServerAndGenerateQr()
    {
        _server.SignatureReceived += bmp =>
        {
            if (IsDisposed)
            {
                bmp.Dispose();
                return;
            }

            BeginInvoke(() =>
            {
                _onSignatureReceived(bmp);
                lblWaitStatus.Text = "Signature received and applied!";
                lblWaitStatus.BackColor = Color.FromArgb(230, 244, 234);
                lblWaitStatus.ForeColor = Color.FromArgb(19, 115, 51);

                if (chkAutoClose.Checked)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            });
        };

        _server.Start(5088);

        var ips = MobileSignatureServer.GetLocalIPv4Addresses();
        cmbIpAddress.Items.Clear();
        foreach (var ip in ips)
            cmbIpAddress.Items.Add(ip);

        if (cmbIpAddress.Items.Count > 0)
            cmbIpAddress.SelectedIndex = 0;
    }

    private void UpdateQrCode()
    {
        string ip = cmbIpAddress.SelectedItem?.ToString() ?? "127.0.0.1";
        string url = _server.BuildUrl(ip);
        txtUrl.Text = url;

        using var qrGen = new QRCodeGenerator();
        using var qrData = qrGen.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new QRCode(qrData);

        _qrBitmap?.Dispose();
        _qrBitmap = qrCode.GetGraphic(10, Color.Black, Color.White, true);
        picQr.Image = _qrBitmap;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _server.Dispose();
        _qrBitmap?.Dispose();
        base.OnFormClosed(e);
    }
}
