using System.Drawing;
using System.Windows.Forms;

namespace PhotoPrintApp;

partial class Form1
{
    private System.ComponentModel.IContainer components = null!;

    // Top ToolStrip
    private ToolStrip toolStripMain = null!;
    private ToolStripButton tsbUpload = null!;
    private ToolStripButton tsbQrSign = null!;
    private ToolStripButton tsbExport = null!;
    private ToolStripButton tsbPrintPreview = null!;
    private ToolStripButton tsbPrint = null!;
    private ToolStripButton tsbReset = null!;

    // Main Layout
    private SplitContainer splitMain = null!;
    private TabControl tabSettings = null!;
    private TabPage tabPhoto = null!;
    private TabPage tabOverlays = null!;
    private TabPage tabLayout = null!;
    private TabPage tabPrint = null!;

    // Tab 1: Photo & Crop
    private Panel pnlPhotoScroll = null!;
    private GroupBox grpUpload = null!;
    private Button btnUploadImage = null!;
    private PictureBox picPreview = null!;
    private Label lblImageInfo = null!;
    private Label lblDpiBadge = null!;

    private GroupBox grpFormat = null!;
    private ComboBox cmbPhotoFormat = null!;
    private Panel pnlCustomSize = null!;
    private NumericUpDown numCustomW = null!;
    private NumericUpDown numCustomH = null!;
    private ComboBox cmbCustomUnit = null!;

    private GroupBox grpFraming = null!;
    private ComboBox cmbFitMode = null!;
    private Button btnFitBgColor = null!;
    private TrackBar trkCropX = null!;
    private TrackBar trkCropY = null!;
    private Label lblCropXVal = null!;
    private Label lblCropYVal = null!;
    private Button btnCenterCrop = null!;
    private Button btnRotLeft = null!;
    private Button btnRotRight = null!;
    private Button btnFlipH = null!;

    private GroupBox grpAdjust = null!;
    private TrackBar trkBrightness = null!;
    private TrackBar trkContrast = null!;
    private Label lblBrightnessVal = null!;
    private Label lblContrastVal = null!;
    private CheckBox chkGrayscale = null!;
    private Button btnResetAdjust = null!;

    private GroupBox grpBorder = null!;
    private CheckBox chkPhotoBorder = null!;
    private NumericUpDown numBorderMm = null!;
    private Button btnBorderColor = null!;

    // Tab 2: Overlays
    private Panel pnlOverlaysScroll = null!;
    private GroupBox grpSignature = null!;
    private Button btnUploadSignature = null!;
    private Button btnQrSignature = null!;
    private Button btnClearSignature = null!;
    private PictureBox picSignature = null!;
    private ComboBox cmbSigPos = null!;
    private TrackBar trkSigScale = null!;
    private Label lblSigScaleVal = null!;
    private TrackBar trkSigOpacity = null!;
    private Label lblSigOpacityVal = null!;
    private CheckBox chkSigRemoveWhite = null!;
    private CheckBox chkSigAutoTrim = null!;

    private GroupBox grpName = null!;
    private TextBox txtName = null!;
    private Button btnFont = null!;
    private Label lblFont = null!;
    private Button btnNameColor = null!;
    private TrackBar trkNameScale = null!;
    private Label lblNameScaleVal = null!;
    private ComboBox cmbNamePos = null!;
    private CheckBox chkNameBg = null!;
    private Button btnNameBgColor = null!;

    private GroupBox grpCombinedOverlay = null!;
    private CheckBox chkCombineSigName = null!;
    private ComboBox cmbCombinedPos = null!;
    private CheckBox chkFullWidthStrip = null!;
    private TrackBar trkOverlayBgOpacity = null!;
    private Label lblOverlayBgOpacityVal = null!;
    private NumericUpDown numOverlayPaddingMm = null!;
    private NumericUpDown numOverlayEdgeMarginMm = null!;
    private NumericUpDown numSigNameGapMm = null!;

    // Tab 3: Sheet & Layout
    private Panel pnlLayoutScroll = null!;
    private GroupBox grpPaper = null!;
    private ComboBox cmbPaperFormat = null!;
    private ComboBox cmbOrientation = null!;
    private NumericUpDown numPageMarginMm = null!;
    private NumericUpDown numPhotoSpacingMm = null!;
    private ComboBox cmbCutGuides = null!;
    private CheckBox chkCenterOnPage = null!;

    private GroupBox grpCopies = null!;
    private CheckBox chkMixSizes = null!;
    private Panel pnlSingleCopies = null!;
    private NumericUpDown numCopies = null!;
    private Button btnFillPage = null!;
    private Label lblMaxCapacity = null!;

    private Panel pnlMixOptions = null!;
    private NumericUpDown numMixPassport = null!;
    private NumericUpDown numMix1x1 = null!;
    private NumericUpDown numMix2x2 = null!;
    private NumericUpDown numMix2R = null!;
    private Label lblMixTotal = null!;

    // Tab 4: Print & Export
    private Panel pnlPrintScroll = null!;
    private GroupBox grpPrintSummary = null!;
    private Label lblPrintSummary = null!;
    private GroupBox grpPrintOptions = null!;
    private ComboBox cmbDpi = null!;
    private NumericUpDown numPrinterCopies = null!;
    private Button btnTabPrintPreview = null!;
    private Button btnTabPrint = null!;
    private Button btnTabExport = null!;

    // Right Panel: Enhanced Preview
    private Panel pnlRight = null!;
    private FlowLayoutPanel pnlTelemetry = null!;
    private Label lblTelSheet = null!;
    private Label lblTelFormat = null!;
    private Label lblTelPlaced = null!;
    private Label lblTelQuality = null!;

    private Panel pnlPreviewToolbar = null!;
    private CheckBox chkShowGuides = null!;
    private CheckBox chkAutoPreview = null!;
    private Button btnManualProcess = null!;
    private Button btnZoomOut = null!;
    private Button btnZoomIn = null!;
    private Button btnZoomFit = null!;
    private Button btnZoom100 = null!;
    private Label lblZoomVal = null!;

    private Panel pnlCanvas = null!;
    private PictureBox picSheetPreview = null!;

    // StatusStrip
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel lblStatus = null!;
    private ToolStripStatusLabel lblSourceMeta = null!;
    private ToolStripStatusLabel lblRenderTime = null!;

    // Dialogs
    private FontDialog fontDialog = null!;
    private ColorDialog colorDialog = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        _uploadedImage?.Dispose();
        _signatureImage?.Dispose();
        _currentResult?.SheetBitmap.Dispose();
        _settings.NameFont.Dispose();
        _previewDebounceTimer?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        fontDialog = new FontDialog();
        colorDialog = new ColorDialog();

        SuspendLayout();

        // Form properties
        Text = "Photo Print Studio";
        Size = new Size(1240, 820);
        MinimumSize = new Size(980, 680);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(243, 244, 246);

        // Top ToolStrip
        toolStripMain = new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            BackColor = Color.White,
            Padding = new Padding(6, 4, 6, 4),
            RenderMode = ToolStripRenderMode.System
        };

        tsbUpload = new ToolStripButton("Upload Photo") { Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 102, 204) };
        tsbUpload.Click += BtnUploadImage_Click;

        tsbQrSign = new ToolStripButton("Sign via Phone (QR)") { Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(103, 58, 183) };
        tsbQrSign.Click += BtnQrSignature_Click;

        tsbExport = new ToolStripButton("Save Sheet Image...") { Enabled = false };
        tsbExport.Click += BtnExport_Click;

        tsbPrintPreview = new ToolStripButton("Print Preview...") { Enabled = false };
        tsbPrintPreview.Click += BtnPrintPreview_Click;

        tsbPrint = new ToolStripButton("Print Now...") { Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(16, 124, 16), Enabled = false };
        tsbPrint.Click += BtnPrint_Click;

        tsbReset = new ToolStripButton("Reset All Settings");
        tsbReset.Click += BtnResetAll_Click;

        toolStripMain.Items.AddRange(new ToolStripItem[]
        {
            tsbUpload,
            tsbQrSign,
            new ToolStripSeparator(),
            tsbExport,
            tsbPrintPreview,
            tsbPrint,
            new ToolStripSeparator(),
            tsbReset
        });

        // StatusStrip
        statusStrip = new StatusStrip { BackColor = Color.White };
        lblStatus = new ToolStripStatusLabel("Ready. Upload a photo to begin.") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        lblSourceMeta = new ToolStripStatusLabel("No image loaded") { BorderSides = ToolStripStatusLabelBorderSides.Left };
        lblRenderTime = new ToolStripStatusLabel("") { BorderSides = ToolStripStatusLabelBorderSides.Left };
        statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus, lblSourceMeta, lblRenderTime });

        // SplitContainer
        splitMain = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 395,
            FixedPanel = FixedPanel.Panel1,
            SplitterWidth = 5,
            BackColor = Color.FromArgb(220, 223, 228)
        };

        // Left TabControl
        tabSettings = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(10, 6)
        };

        tabPhoto = new TabPage("Photo & Crop") { BackColor = Color.FromArgb(248, 249, 251) };
        tabOverlays = new TabPage("Overlays") { BackColor = Color.FromArgb(248, 249, 251) };
        tabLayout = new TabPage("Sheet & Layout") { BackColor = Color.FromArgb(248, 249, 251) };
        tabPrint = new TabPage("Print & Export") { BackColor = Color.FromArgb(248, 249, 251) };

        BuildPhotoTab();
        BuildOverlaysTab();
        BuildLayoutTab();
        BuildPrintTab();

        tabSettings.TabPages.AddRange(new[] { tabPhoto, tabOverlays, tabLayout, tabPrint });
        splitMain.Panel1.Controls.Add(tabSettings);

        // Right Panel (Preview & Telemetry)
        BuildRightPreviewPanel();
        splitMain.Panel2.Controls.Add(pnlRight);

        Controls.Add(splitMain);
        Controls.Add(toolStripMain);
        Controls.Add(statusStrip);

        ResumeLayout(false);
        PerformLayout();
    }

    private void BuildPhotoTab()
    {
        pnlPhotoScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(8) };

        // 1. Upload & Info
        grpUpload = new GroupBox { Text = "Source Photo", Location = new Point(8, 8), Size = new Size(352, 185) };
        btnUploadImage = new Button
        {
            Text = "Browse Photo...",
            Location = new Point(10, 22),
            Size = new Size(130, 30),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnUploadImage.Click += BtnUploadImage_Click;

        picPreview = new PictureBox
        {
            Location = new Point(10, 58),
            Size = new Size(130, 115),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };

        lblImageInfo = new Label
        {
            Location = new Point(150, 24),
            Size = new Size(192, 95),
            Text = "No photo loaded.\nSupported: JPG, PNG, BMP, TIFF",
            ForeColor = Color.FromArgb(60, 60, 60)
        };

        lblDpiBadge = new Label
        {
            Location = new Point(150, 125),
            Size = new Size(192, 46),
            Text = "DPI Quality: --",
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(230, 232, 236),
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
        };

        grpUpload.Controls.AddRange(new Control[] { btnUploadImage, picPreview, lblImageInfo, lblDpiBadge });

        // 2. Photo Format
        grpFormat = new GroupBox { Text = "Photo Print Format", Location = new Point(8, 200), Size = new Size(352, 108) };
        var lblFmt = new Label { Text = "Format:", Location = new Point(10, 26), AutoSize = true };
        cmbPhotoFormat = new ComboBox
        {
            Location = new Point(75, 22),
            Width = 265,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbPhotoFormat.SelectedIndexChanged += OnSettingChanged;

        pnlCustomSize = new Panel { Location = new Point(10, 54), Size = new Size(332, 44), Visible = false };
        var lblW = new Label { Text = "W:", Location = new Point(0, 10), AutoSize = true };
        numCustomW = new NumericUpDown { Location = new Point(24, 7), Width = 68, DecimalPlaces = 2, Minimum = 0.2M, Maximum = 500M, Value = 2.0M, Increment = 0.1M };
        numCustomW.ValueChanged += OnSettingChanged;
        var lblH = new Label { Text = "H:", Location = new Point(102, 10), AutoSize = true };
        numCustomH = new NumericUpDown { Location = new Point(124, 7), Width = 68, DecimalPlaces = 2, Minimum = 0.2M, Maximum = 500M, Value = 2.0M, Increment = 0.1M };
        numCustomH.ValueChanged += OnSettingChanged;
        cmbCustomUnit = new ComboBox { Location = new Point(202, 7), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbCustomUnit.Items.AddRange(new object[] { "Inches (in)", "Millimeters (mm)" });
        cmbCustomUnit.SelectedIndex = 0;
        cmbCustomUnit.SelectedIndexChanged += OnSettingChanged;
        pnlCustomSize.Controls.AddRange(new Control[] { lblW, numCustomW, lblH, numCustomH, cmbCustomUnit });

        grpFormat.Controls.AddRange(new Control[] { lblFmt, cmbPhotoFormat, pnlCustomSize });

        // 3. Framing & Transform
        grpFraming = new GroupBox { Text = "Framing, Crop & Transform", Location = new Point(8, 315), Size = new Size(352, 205) };
        var lblFit = new Label { Text = "Fit Mode:", Location = new Point(10, 26), AutoSize = true };
        cmbFitMode = new ComboBox { Location = new Point(75, 22), Width = 165, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbFitMode.Items.AddRange(new object[] { "Crop to Fill", "Fit Inside", "Stretch" });
        cmbFitMode.SelectedIndex = 0;
        cmbFitMode.SelectedIndexChanged += OnSettingChanged;

        btnFitBgColor = new Button { Text = "BG Color", Location = new Point(248, 21), Size = new Size(92, 25), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnFitBgColor.Click += BtnFitBgColor_Click;

        var lblCX = new Label { Text = "Crop H:", Location = new Point(10, 62), AutoSize = true };
        trkCropX = new TrackBar { Location = new Point(68, 54), Width = 215, Minimum = -100, Maximum = 100, Value = 0, TickFrequency = 25 };
        trkCropX.ValueChanged += OnSettingChanged;
        lblCropXVal = new Label { Text = "0%", Location = new Point(288, 62), AutoSize = true };

        var lblCY = new Label { Text = "Crop V:", Location = new Point(10, 102), AutoSize = true };
        trkCropY = new TrackBar { Location = new Point(68, 94), Width = 215, Minimum = -100, Maximum = 100, Value = 0, TickFrequency = 25 };
        trkCropY.ValueChanged += OnSettingChanged;
        lblCropYVal = new Label { Text = "0%", Location = new Point(288, 102), AutoSize = true };

        btnCenterCrop = new Button { Text = "Center Crop", Location = new Point(10, 145), Size = new Size(82, 28) };
        btnCenterCrop.Click += BtnCenterCrop_Click;

        btnRotLeft = new Button { Text = "Rot -90°", Location = new Point(98, 145), Size = new Size(74, 28) };
        btnRotLeft.Click += (s, e) => { _settings.RotationDegrees = (_settings.RotationDegrees + 270) % 360; TriggerPreviewUpdate(); };

        btnRotRight = new Button { Text = "Rot +90°", Location = new Point(178, 145), Size = new Size(74, 28) };
        btnRotRight.Click += (s, e) => { _settings.RotationDegrees = (_settings.RotationDegrees + 90) % 360; TriggerPreviewUpdate(); };

        btnFlipH = new Button { Text = "Flip H", Location = new Point(258, 145), Size = new Size(82, 28) };
        btnFlipH.Click += (s, e) => { _settings.FlipHorizontal = !_settings.FlipHorizontal; TriggerPreviewUpdate(); };

        grpFraming.Controls.AddRange(new Control[]
        {
            lblFit, cmbFitMode, btnFitBgColor,
            lblCX, trkCropX, lblCropXVal,
            lblCY, trkCropY, lblCropYVal,
            btnCenterCrop, btnRotLeft, btnRotRight, btnFlipH
        });

        // 4. Image Adjustments
        grpAdjust = new GroupBox { Text = "Color & Tone Adjustments", Location = new Point(8, 527), Size = new Size(352, 145) };
        var lblBr = new Label { Text = "Bright:", Location = new Point(10, 28), AutoSize = true };
        trkBrightness = new TrackBar { Location = new Point(68, 20), Width = 215, Minimum = -100, Maximum = 100, Value = 0, TickFrequency = 25 };
        trkBrightness.ValueChanged += OnSettingChanged;
        lblBrightnessVal = new Label { Text = "0", Location = new Point(288, 28), AutoSize = true };

        var lblCo = new Label { Text = "Contrast:", Location = new Point(10, 68), AutoSize = true };
        trkContrast = new TrackBar { Location = new Point(68, 60), Width = 215, Minimum = -100, Maximum = 100, Value = 0, TickFrequency = 25 };
        trkContrast.ValueChanged += OnSettingChanged;
        lblContrastVal = new Label { Text = "0", Location = new Point(288, 68), AutoSize = true };

        chkGrayscale = new CheckBox { Text = "Black && White (Grayscale)", Location = new Point(12, 110), AutoSize = true };
        chkGrayscale.CheckedChanged += OnSettingChanged;

        btnResetAdjust = new Button { Text = "Reset Tone", Location = new Point(250, 106), Size = new Size(90, 26) };
        btnResetAdjust.Click += BtnResetAdjust_Click;

        grpAdjust.Controls.AddRange(new Control[]
        {
            lblBr, trkBrightness, lblBrightnessVal,
            lblCo, trkContrast, lblContrastVal,
            chkGrayscale, btnResetAdjust
        });

        // 5. Photo Border
        grpBorder = new GroupBox { Text = "Photo Frame / Border", Location = new Point(8, 678), Size = new Size(352, 62) };
        chkPhotoBorder = new CheckBox { Text = "Add Border", Location = new Point(12, 24), AutoSize = true };
        chkPhotoBorder.CheckedChanged += OnSettingChanged;

        var lblBThick = new Label { Text = "mm:", Location = new Point(112, 26), AutoSize = true };
        numBorderMm = new NumericUpDown { Location = new Point(144, 22), Width = 60, DecimalPlaces = 1, Minimum = 0.2M, Maximum = 10M, Value = 0.5M, Increment = 0.2M };
        numBorderMm.ValueChanged += OnSettingChanged;

        btnBorderColor = new Button { Text = "Border Color", Location = new Point(218, 21), Size = new Size(122, 26), BackColor = Color.Black, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnBorderColor.Click += BtnBorderColor_Click;

        grpBorder.Controls.AddRange(new Control[] { chkPhotoBorder, lblBThick, numBorderMm, btnBorderColor });

        pnlPhotoScroll.Controls.AddRange(new Control[] { grpUpload, grpFormat, grpFraming, grpAdjust, grpBorder });
        tabPhoto.Controls.Add(pnlPhotoScroll);
    }

    private void BuildOverlaysTab()
    {
        pnlOverlaysScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(8) };

        // Signature Group
        grpSignature = new GroupBox { Text = "Signature Overlay (Optional)", Location = new Point(8, 8), Size = new Size(352, 315) };
        btnUploadSignature = new Button { Text = "Browse File...", Location = new Point(10, 22), Size = new Size(105, 28) };
        btnUploadSignature.Click += BtnUploadSignature_Click;

        btnClearSignature = new Button { Text = "Clear", Location = new Point(121, 22), Size = new Size(105, 28) };
        btnClearSignature.Click += BtnClearSignature_Click;

        btnQrSignature = new Button
        {
            Text = "Sign via Phone (QR)...",
            Location = new Point(10, 56),
            Size = new Size(216, 30),
            BackColor = Color.FromArgb(103, 58, 183),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        btnQrSignature.Click += BtnQrSignature_Click;

        picSignature = new PictureBox
        {
            Location = new Point(236, 22),
            Size = new Size(104, 64),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };

        var lblSPos = new Label { Text = "Position:", Location = new Point(10, 98), AutoSize = true };
        cmbSigPos = new ComboBox { Location = new Point(75, 94), Width = 265, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbSigPos.Items.AddRange(new object[] { "Bottom Center", "Bottom Left", "Bottom Right", "Top Center" });
        cmbSigPos.SelectedIndex = 2;
        cmbSigPos.SelectedIndexChanged += OnSettingChanged;

        var lblSScale = new Label { Text = "Size %:", Location = new Point(10, 136), AutoSize = true };
        trkSigScale = new TrackBar { Location = new Point(70, 128), Width = 215, Minimum = 5, Maximum = 60, Value = 20, TickFrequency = 5 };
        trkSigScale.ValueChanged += OnSettingChanged;
        lblSigScaleVal = new Label { Text = "20%", Location = new Point(290, 136), AutoSize = true };

        var lblSOp = new Label { Text = "Opacity:", Location = new Point(10, 180), AutoSize = true };
        trkSigOpacity = new TrackBar { Location = new Point(70, 172), Width = 215, Minimum = 10, Maximum = 100, Value = 100, TickFrequency = 15 };
        trkSigOpacity.ValueChanged += OnSettingChanged;
        lblSigOpacityVal = new Label { Text = "100%", Location = new Point(290, 180), AutoSize = true };

        chkSigRemoveWhite = new CheckBox
        {
            Text = "Make white background transparent",
            Location = new Point(12, 224),
            AutoSize = true,
            Checked = true
        };
        chkSigRemoveWhite.CheckedChanged += OnSettingChanged;

        chkSigAutoTrim = new CheckBox
        {
            Text = "Auto-trim empty space around signature",
            Location = new Point(12, 250),
            AutoSize = true,
            Checked = true
        };
        chkSigAutoTrim.CheckedChanged += OnSettingChanged;

        grpSignature.Controls.AddRange(new Control[]
        {
            btnUploadSignature, btnClearSignature, btnQrSignature, picSignature,
            lblSPos, cmbSigPos,
            lblSScale, trkSigScale, lblSigScaleVal,
            lblSOp, trkSigOpacity, lblSigOpacityVal,
            chkSigRemoveWhite, chkSigAutoTrim
        });

        // Name Group
        grpName = new GroupBox { Text = "Name / Label Overlay", Location = new Point(8, 330), Size = new Size(352, 252) };
        txtName = new TextBox { Location = new Point(10, 24), Width = 330, PlaceholderText = "Enter name or label to overlay..." };
        txtName.TextChanged += OnSettingChanged;

        btnFont = new Button { Text = "Choose Font...", Location = new Point(10, 56), Size = new Size(120, 28) };
        btnFont.Click += BtnFont_Click;

        btnNameColor = new Button { Text = "Text Color", Location = new Point(138, 56), Size = new Size(96, 28), BackColor = Color.Black, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnNameColor.Click += BtnNameColor_Click;

        lblFont = new Label { Text = "Font: Arial, 8pt (Bold)", Location = new Point(10, 90), Size = new Size(330, 18), ForeColor = Color.DimGray };

        var lblNScale = new Label { Text = "Text Scale:", Location = new Point(10, 118), AutoSize = true };
        trkNameScale = new TrackBar { Location = new Point(82, 110), Width = 200, Minimum = 40, Maximum = 200, Value = 100, TickFrequency = 20 };
        trkNameScale.ValueChanged += OnSettingChanged;
        lblNameScaleVal = new Label { Text = "100%", Location = new Point(288, 118), AutoSize = true };

        var lblNPos = new Label { Text = "Position:", Location = new Point(10, 158), AutoSize = true };
        cmbNamePos = new ComboBox { Location = new Point(75, 154), Width = 265, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbNamePos.Items.AddRange(new object[] { "Bottom Center", "Bottom Left", "Bottom Right", "Top Center" });
        cmbNamePos.SelectedIndex = 0;
        cmbNamePos.SelectedIndexChanged += OnSettingChanged;

        chkNameBg = new CheckBox { Text = "Show background box", Location = new Point(12, 192), AutoSize = true, Checked = true };
        chkNameBg.CheckedChanged += OnSettingChanged;

        btnNameBgColor = new Button { Text = "Box Color", Location = new Point(244, 188), Size = new Size(96, 26), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnNameBgColor.Click += BtnNameBgColor_Click;

        grpName.Controls.AddRange(new Control[]
        {
            txtName, btnFont, btnNameColor, lblFont,
            lblNScale, trkNameScale, lblNameScaleVal,
            lblNPos, cmbNamePos,
            chkNameBg, btnNameBgColor
        });

        pnlOverlaysScroll.Controls.AddRange(new Control[] { grpSignature, grpName });

        // Combined Signature & Name Block Group
        grpCombinedOverlay = new GroupBox { Text = "Combined Block, Padding & Margins", Location = new Point(8, 590), Size = new Size(352, 232) };
        chkCombineSigName = new CheckBox
        {
            Text = "Combine Signature above Name (1 entity)",
            Location = new Point(12, 22),
            AutoSize = true,
            Checked = true
        };
        chkCombineSigName.CheckedChanged += OnSettingChanged;

        var lblCPos = new Label { Text = "Block Pos:", Location = new Point(12, 52), AutoSize = true };
        cmbCombinedPos = new ComboBox { Location = new Point(85, 48), Width = 255, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbCombinedPos.Items.AddRange(new object[] { "Bottom Center", "Bottom Left", "Bottom Right", "Top Center" });
        cmbCombinedPos.SelectedIndex = 0;
        cmbCombinedPos.SelectedIndexChanged += OnSettingChanged;

        chkFullWidthStrip = new CheckBox
        {
            Text = "Full-width background strip across photo",
            Location = new Point(12, 80),
            AutoSize = true,
            Checked = false
        };
        chkFullWidthStrip.CheckedChanged += OnSettingChanged;

        var lblBgOp = new Label { Text = "BG Opacity:", Location = new Point(12, 114), AutoSize = true };
        trkOverlayBgOpacity = new TrackBar { Location = new Point(92, 106), Width = 195, Minimum = 0, Maximum = 100, Value = 85, TickFrequency = 20 };
        trkOverlayBgOpacity.ValueChanged += OnSettingChanged;
        lblOverlayBgOpacityVal = new Label { Text = "85%", Location = new Point(292, 114), AutoSize = true };

        var lblOPad = new Label { Text = "Box Pad (mm):", Location = new Point(12, 152), AutoSize = true };
        numOverlayPaddingMm = new NumericUpDown { Location = new Point(108, 149), Width = 58, DecimalPlaces = 1, Minimum = 0M, Maximum = 10M, Value = 1.0M, Increment = 0.5M };
        numOverlayPaddingMm.ValueChanged += OnSettingChanged;

        var lblEMarg = new Label { Text = "Edge (mm):", Location = new Point(182, 152), AutoSize = true };
        numOverlayEdgeMarginMm = new NumericUpDown { Location = new Point(260, 149), Width = 62, DecimalPlaces = 1, Minimum = 0M, Maximum = 15M, Value = 1.0M, Increment = 0.5M };
        numOverlayEdgeMarginMm.ValueChanged += OnSettingChanged;

        var lblSNGap = new Label { Text = "Sig-Name Gap (mm):", Location = new Point(12, 184), AutoSize = true };
        numSigNameGapMm = new NumericUpDown { Location = new Point(145, 181), Width = 62, DecimalPlaces = 1, Minimum = 0M, Maximum = 10M, Value = 0.5M, Increment = 0.5M };
        numSigNameGapMm.ValueChanged += OnSettingChanged;

        grpCombinedOverlay.Controls.AddRange(new Control[]
        {
            chkCombineSigName,
            lblCPos, cmbCombinedPos,
            chkFullWidthStrip,
            lblBgOp, trkOverlayBgOpacity, lblOverlayBgOpacityVal,
            lblOPad, numOverlayPaddingMm,
            lblEMarg, numOverlayEdgeMarginMm,
            lblSNGap, numSigNameGapMm
        });

        pnlOverlaysScroll.Controls.Add(grpCombinedOverlay);
        tabOverlays.Controls.Add(pnlOverlaysScroll);
    }

    private void BuildLayoutTab()
    {
        pnlLayoutScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(8) };

        // Paper & Margins
        grpPaper = new GroupBox { Text = "Paper Sheet & Margins", Location = new Point(8, 8), Size = new Size(352, 205) };
        var lblPaper = new Label { Text = "Paper Size:", Location = new Point(10, 28), AutoSize = true };
        cmbPaperFormat = new ComboBox { Location = new Point(100, 24), Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbPaperFormat.SelectedIndexChanged += OnSettingChanged;

        var lblOrient = new Label { Text = "Orientation:", Location = new Point(10, 60), AutoSize = true };
        cmbOrientation = new ComboBox { Location = new Point(100, 56), Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbOrientation.Items.AddRange(new object[] { "Portrait", "Landscape" });
        cmbOrientation.SelectedIndex = 0;
        cmbOrientation.SelectedIndexChanged += OnSettingChanged;

        var lblPM = new Label { Text = "Page Margin (mm):", Location = new Point(10, 94), AutoSize = true };
        numPageMarginMm = new NumericUpDown { Location = new Point(145, 90), Width = 68, DecimalPlaces = 1, Minimum = 0M, Maximum = 50M, Value = 8M, Increment = 1M };
        numPageMarginMm.ValueChanged += OnSettingChanged;

        var lblPS = new Label { Text = "Photo Gap (mm):", Location = new Point(10, 124), AutoSize = true };
        numPhotoSpacingMm = new NumericUpDown { Location = new Point(145, 120), Width = 68, DecimalPlaces = 1, Minimum = 0M, Maximum = 40M, Value = 4M, Increment = 0.5M };
        numPhotoSpacingMm.ValueChanged += OnSettingChanged;

        var lblCut = new Label { Text = "Cut Guides:", Location = new Point(10, 154), AutoSize = true };
        cmbCutGuides = new ComboBox { Location = new Point(100, 150), Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbCutGuides.Items.AddRange(new object[] { "None", "Dashed Border", "Corner Crop Marks" });
        cmbCutGuides.SelectedIndex = 2;
        cmbCutGuides.SelectedIndexChanged += OnSettingChanged;

        chkCenterOnPage = new CheckBox { Text = "Center layout grid on sheet", Location = new Point(12, 180), AutoSize = true, Checked = true };
        chkCenterOnPage.CheckedChanged += OnSettingChanged;

        grpPaper.Controls.AddRange(new Control[]
        {
            lblPaper, cmbPaperFormat,
            lblOrient, cmbOrientation,
            lblPM, numPageMarginMm,
            lblPS, numPhotoSpacingMm,
            lblCut, cmbCutGuides,
            chkCenterOnPage
        });

        // Copies & Mix Mode
        grpCopies = new GroupBox { Text = "Copies & Mixed Layout", Location = new Point(8, 220), Size = new Size(352, 235) };
        chkMixSizes = new CheckBox { Text = "Mix multiple photo sizes on one sheet", Location = new Point(10, 24), AutoSize = true };
        chkMixSizes.CheckedChanged += ChkMixSizes_CheckedChanged;

        pnlSingleCopies = new Panel { Location = new Point(10, 52), Size = new Size(332, 85) };
        var lblC = new Label { Text = "Copies:", Location = new Point(0, 8), AutoSize = true };
        numCopies = new NumericUpDown { Location = new Point(62, 5), Width = 72, Minimum = 1, Maximum = 100, Value = 4 };
        numCopies.ValueChanged += OnSettingChanged;

        btnFillPage = new Button { Text = "Fill Page (Max)", Location = new Point(146, 4), Size = new Size(125, 26), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnFillPage.Click += BtnFillPage_Click;

        lblMaxCapacity = new Label { Text = "Max capacity on sheet: --", Location = new Point(0, 42), Size = new Size(325, 24), ForeColor = Color.DimGray };
        pnlSingleCopies.Controls.AddRange(new Control[] { lblC, numCopies, btnFillPage, lblMaxCapacity });

        pnlMixOptions = new Panel { Location = new Point(10, 52), Size = new Size(332, 170), Visible = false };
        var lblMP = new Label { Text = "Passport (35×45mm):", Location = new Point(0, 8), AutoSize = true };
        numMixPassport = new NumericUpDown { Location = new Point(160, 5), Width = 68, Minimum = 0, Maximum = 50 };
        numMixPassport.ValueChanged += OnSettingChanged;

        var lblM1 = new Label { Text = "1×1 inch:", Location = new Point(0, 38), AutoSize = true };
        numMix1x1 = new NumericUpDown { Location = new Point(160, 35), Width = 68, Minimum = 0, Maximum = 50 };
        numMix1x1.ValueChanged += OnSettingChanged;

        var lblM2 = new Label { Text = "2×2 inch:", Location = new Point(0, 68), AutoSize = true };
        numMix2x2 = new NumericUpDown { Location = new Point(160, 65), Width = 68, Minimum = 0, Maximum = 50 };
        numMix2x2.ValueChanged += OnSettingChanged;

        var lblMR = new Label { Text = "2R (Wallet 2.5×3.5):", Location = new Point(0, 98), AutoSize = true };
        numMix2R = new NumericUpDown { Location = new Point(160, 95), Width = 68, Minimum = 0, Maximum = 20 };
        numMix2R.ValueChanged += OnSettingChanged;

        lblMixTotal = new Label { Text = "Total requested: 0", Location = new Point(0, 134), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.DarkGreen };
        pnlMixOptions.Controls.AddRange(new Control[] { lblMP, numMixPassport, lblM1, numMix1x1, lblM2, numMix2x2, lblMR, numMix2R, lblMixTotal });

        grpCopies.Controls.AddRange(new Control[] { chkMixSizes, pnlSingleCopies, pnlMixOptions });

        pnlLayoutScroll.Controls.AddRange(new Control[] { grpPaper, grpCopies });
        tabLayout.Controls.Add(pnlLayoutScroll);
    }

    private void BuildPrintTab()
    {
        pnlPrintScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(8) };

        grpPrintSummary = new GroupBox { Text = "Live Sheet & Print Summary", Location = new Point(8, 8), Size = new Size(352, 200) };
        lblPrintSummary = new Label
        {
            Location = new Point(12, 24),
            Size = new Size(328, 166),
            Text = "Upload a photo to see full print telemetry.",
            ForeColor = Color.FromArgb(40, 40, 40)
        };
        grpPrintSummary.Controls.Add(lblPrintSummary);

        grpPrintOptions = new GroupBox { Text = "Output & Print Settings", Location = new Point(8, 216), Size = new Size(352, 235) };
        var lblDpi = new Label { Text = "Render DPI:", Location = new Point(12, 28), AutoSize = true };
        cmbDpi = new ComboBox { Location = new Point(120, 24), Width = 216, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbDpi.Items.AddRange(new object[] { "150 DPI (Fast Draft)", "300 DPI (Standard Print)", "600 DPI (Ultra High)" });
        cmbDpi.SelectedIndex = 1;
        cmbDpi.SelectedIndexChanged += OnSettingChanged;

        var lblPCopies = new Label { Text = "Printer Copies:", Location = new Point(12, 62), AutoSize = true };
        numPrinterCopies = new NumericUpDown { Location = new Point(120, 58), Width = 72, Minimum = 1, Maximum = 99, Value = 1 };

        btnTabPrintPreview = new Button { Text = "Open Print Preview...", Location = new Point(12, 102), Size = new Size(324, 34), Enabled = false };
        btnTabPrintPreview.Click += BtnPrintPreview_Click;

        btnTabExport = new Button { Text = "Save Sheet as PNG / JPEG...", Location = new Point(12, 144), Size = new Size(324, 34), Enabled = false };
        btnTabExport.Click += BtnExport_Click;

        btnTabPrint = new Button
        {
            Text = "Print Sheet Now",
            Location = new Point(12, 186),
            Size = new Size(324, 38),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            BackColor = Color.FromArgb(16, 124, 16),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        btnTabPrint.Click += BtnPrint_Click;

        grpPrintOptions.Controls.AddRange(new Control[]
        {
            lblDpi, cmbDpi,
            lblPCopies, numPrinterCopies,
            btnTabPrintPreview, btnTabExport, btnTabPrint
        });

        pnlPrintScroll.Controls.AddRange(new Control[] { grpPrintSummary, grpPrintOptions });
        tabPrint.Controls.Add(pnlPrintScroll);
    }

    private void BuildRightPreviewPanel()
    {
        pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };

        // Telemetry Header Bar
        pnlTelemetry = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.White,
            Padding = new Padding(8, 6, 8, 4),
            WrapContents = false,
            AutoScroll = true
        };

        lblTelSheet = CreateTelemetryPill("Sheet: A4 Portrait", Color.FromArgb(232, 240, 254), Color.FromArgb(25, 103, 210));
        lblTelFormat = CreateTelemetryPill("Format: 2x2", Color.FromArgb(241, 243, 244), Color.FromArgb(60, 64, 67));
        lblTelPlaced = CreateTelemetryPill("Placed: --", Color.FromArgb(230, 244, 234), Color.FromArgb(19, 115, 51));
        lblTelQuality = CreateTelemetryPill("DPI: --", Color.FromArgb(241, 243, 244), Color.FromArgb(60, 64, 67));

        pnlTelemetry.Controls.AddRange(new Control[] { lblTelSheet, lblTelFormat, lblTelPlaced, lblTelQuality });

        // Preview Controls Toolbar
        pnlPreviewToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 38,
            BackColor = Color.FromArgb(245, 246, 248),
            Padding = new Padding(8, 4, 8, 4)
        };

        chkShowGuides = new CheckBox { Text = "Show Margin Guides", Location = new Point(10, 9), AutoSize = true, Checked = true };
        chkShowGuides.CheckedChanged += OnSettingChanged;

        chkAutoPreview = new CheckBox { Text = "Live Preview", Location = new Point(155, 9), AutoSize = true, Checked = true };
        chkAutoPreview.CheckedChanged += (s, e) => btnManualProcess.Visible = !chkAutoPreview.Checked;

        btnManualProcess = new Button
        {
            Text = "Refresh Preview",
            Location = new Point(260, 5),
            Size = new Size(115, 28),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Visible = false
        };
        btnManualProcess.Click += (s, e) => RenderPreviewNow();

        btnZoomOut = new Button { Text = "-", Location = new Point(390, 6), Size = new Size(28, 26) };
        btnZoomOut.Click += (s, e) => AdjustZoom(-0.15f);

        lblZoomVal = new Label { Text = "Fit", Location = new Point(422, 10), Size = new Size(46, 18), TextAlign = ContentAlignment.MiddleCenter };

        btnZoomIn = new Button { Text = "+", Location = new Point(472, 6), Size = new Size(28, 26) };
        btnZoomIn.Click += (s, e) => AdjustZoom(0.15f);

        btnZoomFit = new Button { Text = "Fit", Location = new Point(508, 6), Size = new Size(44, 26) };
        btnZoomFit.Click += (s, e) => SetZoomFit();

        btnZoom100 = new Button { Text = "100%", Location = new Point(558, 6), Size = new Size(50, 26) };
        btnZoom100.Click += (s, e) => SetZoomPercent(1.0f);

        pnlPreviewToolbar.Controls.AddRange(new Control[]
        {
            chkShowGuides, chkAutoPreview, btnManualProcess,
            btnZoomOut, lblZoomVal, btnZoomIn, btnZoomFit, btnZoom100
        });

        // Canvas
        pnlCanvas = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(43, 48, 58),
            AutoScroll = true
        };
        pnlCanvas.Resize += (s, e) => UpdatePreviewDisplaySize();

        picSheetPreview = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        pnlCanvas.Controls.Add(picSheetPreview);

        pnlRight.Controls.Add(pnlCanvas);
        pnlRight.Controls.Add(pnlPreviewToolbar);
        pnlRight.Controls.Add(pnlTelemetry);
    }

    private static Label CreateTelemetryPill(string text, Color back, Color fore)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            BackColor = back,
            ForeColor = fore,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(0, 0, 8, 0)
        };
    }
}
