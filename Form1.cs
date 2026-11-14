using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Windows.Forms;
using PhotoPrintApp.Forms;
using PhotoPrintApp.Models;
using PhotoPrintApp.Services;

namespace PhotoPrintApp;

public partial class Form1 : Form
{
    private Image? _uploadedImage;
    private string _uploadedFileName = "";
    private Image? _signatureImage;
    private LayoutResult? _currentResult;
    private readonly PrintJobSettings _settings = new();

    private System.Windows.Forms.Timer? _previewDebounceTimer;
    private bool _isInitializing = true;
    private bool _zoomFitMode = true;
    private float _zoomFactor = 1.0f;

    public Form1()
    {
        InitializeComponent();
        InitializePresetsAndState();
    }

    private void InitializePresetsAndState()
    {
        _isInitializing = true;

        cmbPhotoFormat.Items.Clear();
        foreach (var fmt in PrintPresets.PhotoFormats)
            cmbPhotoFormat.Items.Add(fmt);
        cmbPhotoFormat.SelectedIndex = 3; // 2x2

        cmbPaperFormat.Items.Clear();
        foreach (var paper in PrintPresets.PaperFormats)
            cmbPaperFormat.Items.Add(paper);
        cmbPaperFormat.SelectedIndex = 0; // A4

        _previewDebounceTimer = new System.Windows.Forms.Timer { Interval = 100 };
        _previewDebounceTimer.Tick += (s, e) =>
        {
            _previewDebounceTimer.Stop();
            RenderPreviewNow();
        };

        _isInitializing = false;
        SyncSettingsFromUi();
        UpdateTelemetryWithoutImage();
    }

    private void BtnUploadImage_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.webp",
            Title = "Select Photo to Print"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            using var stream = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read);
            using var temp = Image.FromStream(stream);
            _uploadedImage?.Dispose();
            _uploadedImage = new Bitmap(temp);
            _uploadedFileName = Path.GetFileName(dlg.FileName);

            picPreview.Image = _uploadedImage;
            UpdateSourceImageInfo();
            SetOutputActionsEnabled(true);
            RenderPreviewNow();
            UpdateStatus($"Loaded \"{_uploadedFileName}\" ({_uploadedImage.Width}×{_uploadedImage.Height} px).");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not load image: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnUploadSignature_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff",
            Title = "Select Signature Image"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            using var stream = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read);
            using var temp = Image.FromStream(stream);
            _signatureImage?.Dispose();
            _signatureImage = new Bitmap(temp);
            picSignature.Image = _signatureImage;

            TriggerPreviewUpdate();
            UpdateStatus("Signature overlay loaded.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not load signature: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnClearSignature_Click(object? sender, EventArgs e)
    {
        _signatureImage?.Dispose();
        _signatureImage = null;
        picSignature.Image = null;
        TriggerPreviewUpdate();
        UpdateStatus("Signature cleared.");
    }

    private void BtnQrSignature_Click(object? sender, EventArgs e)
    {
        using var qrDlg = new QrSignatureForm(bmp =>
        {
            _signatureImage?.Dispose();
            _signatureImage = bmp;
            picSignature.Image = _signatureImage;
            TriggerPreviewUpdate();
            UpdateStatus("Phone signature received and applied!");
        });
        qrDlg.ShowDialog(this);
    }

    private void BtnFont_Click(object? sender, EventArgs e)
    {
        fontDialog.Font = _settings.NameFont;
        if (fontDialog.ShowDialog() == DialogResult.OK)
        {
            _settings.NameFont.Dispose();
            _settings.NameFont = (Font)fontDialog.Font.Clone();
            lblFont.Text = $"Font: {_settings.NameFont.FontFamily.Name}, {_settings.NameFont.Size:0.#}pt ({_settings.NameFont.Style})";
            TriggerPreviewUpdate();
        }
    }

    private void BtnNameColor_Click(object? sender, EventArgs e)
    {
        colorDialog.Color = _settings.NameColor;
        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            _settings.NameColor = colorDialog.Color;
            btnNameColor.BackColor = colorDialog.Color;
            btnNameColor.ForeColor = GetContrastForeColor(colorDialog.Color);
            TriggerPreviewUpdate();
        }
    }

    private void BtnNameBgColor_Click(object? sender, EventArgs e)
    {
        colorDialog.Color = Color.FromArgb(255, _settings.NameBackgroundColor);
        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            _settings.NameBackgroundColor = Color.FromArgb(210, colorDialog.Color);
            btnNameBgColor.BackColor = colorDialog.Color;
            btnNameBgColor.ForeColor = GetContrastForeColor(colorDialog.Color);
            TriggerPreviewUpdate();
        }
    }

    private void BtnFitBgColor_Click(object? sender, EventArgs e)
    {
        colorDialog.Color = _settings.FitBackgroundColor;
        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            _settings.FitBackgroundColor = colorDialog.Color;
            btnFitBgColor.BackColor = colorDialog.Color;
            btnFitBgColor.ForeColor = GetContrastForeColor(colorDialog.Color);
            TriggerPreviewUpdate();
        }
    }

    private void BtnBorderColor_Click(object? sender, EventArgs e)
    {
        colorDialog.Color = _settings.PhotoBorderColor;
        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            _settings.PhotoBorderColor = colorDialog.Color;
            btnBorderColor.BackColor = colorDialog.Color;
            btnBorderColor.ForeColor = GetContrastForeColor(colorDialog.Color);
            TriggerPreviewUpdate();
        }
    }

    private void BtnCenterCrop_Click(object? sender, EventArgs e)
    {
        trkCropX.Value = 0;
        trkCropY.Value = 0;
    }

    private void BtnResetAdjust_Click(object? sender, EventArgs e)
    {
        trkBrightness.Value = 0;
        trkContrast.Value = 0;
        chkGrayscale.Checked = false;
    }

    private void ChkMixSizes_CheckedChanged(object? sender, EventArgs e)
    {
        bool mix = chkMixSizes.Checked;
        pnlMixOptions.Visible = mix;
        pnlSingleCopies.Visible = !mix;
        OnSettingChanged(sender, e);
    }

    private void BtnFillPage_Click(object? sender, EventArgs e)
    {
        SyncSettingsFromUi();
        var photoSize = PrintPresets.GetPhotoSizeInches(_settings);
        int maxCap = Math.Max(1, PhotoRenderer.CalculateMaxCapacity(_settings, photoSize));
        numCopies.Value = Math.Min(numCopies.Maximum, maxCap);
    }

    private void BtnResetAll_Click(object? sender, EventArgs e)
    {
        _isInitializing = true;

        cmbPhotoFormat.SelectedIndex = 3; // 2x2
        cmbFitMode.SelectedIndex = 0;
        _settings.FitBackgroundColor = Color.White;
        btnFitBgColor.BackColor = Color.White;
        btnFitBgColor.ForeColor = Color.Black;
        trkCropX.Value = 0;
        trkCropY.Value = 0;
        _settings.RotationDegrees = 0;
        _settings.FlipHorizontal = false;
        trkBrightness.Value = 0;
        trkContrast.Value = 0;
        chkGrayscale.Checked = false;
        chkPhotoBorder.Checked = false;
        numBorderMm.Value = 0.5M;
        _settings.PhotoBorderColor = Color.Black;
        btnBorderColor.BackColor = Color.Black;
        btnBorderColor.ForeColor = Color.White;

        cmbSigPos.SelectedIndex = 2;
        trkSigScale.Value = 20;
        trkSigOpacity.Value = 100;
        chkSigRemoveWhite.Checked = true;
        chkSigAutoTrim.Checked = true;

        txtName.Text = "";
        trkNameScale.Value = 100;
        cmbNamePos.SelectedIndex = 0;
        chkNameBg.Checked = true;
        _settings.NameBackgroundColor = Color.White;
        btnNameBgColor.BackColor = Color.White;
        btnNameBgColor.ForeColor = Color.Black;
        chkCombineSigName.Checked = true;
        cmbCombinedPos.SelectedIndex = 0;
        chkFullWidthStrip.Checked = false;
        trkOverlayBgOpacity.Value = 85;
        numOverlayPaddingMm.Value = 1.0M;
        numOverlayEdgeMarginMm.Value = 1.0M;
        numSigNameGapMm.Value = 0.5M;

        cmbPaperFormat.SelectedIndex = 0; // A4
        cmbOrientation.SelectedIndex = 0; // Portrait
        numPageMarginMm.Value = 8M;
        numPhotoSpacingMm.Value = 4M;
        cmbCutGuides.SelectedIndex = 2; // Corner Crop Marks
        chkCenterOnPage.Checked = true;
        chkMixSizes.Checked = false;
        pnlMixOptions.Visible = false;
        pnlSingleCopies.Visible = true;
        numCopies.Value = 4;
        cmbDpi.SelectedIndex = 1; // 300 DPI
        chkShowGuides.Checked = true;

        _isInitializing = false;
        SyncSettingsFromUi();
        TriggerPreviewUpdate();
        UpdateStatus("All print settings reset to defaults.");
    }

    private void OnSettingChanged(object? sender, EventArgs e)
    {
        if (_isInitializing) return;

        SyncSettingsFromUi();
        UpdateSourceImageInfo();
        UpdateTelemetryWithoutImage();
        TriggerPreviewUpdate();
    }

    private void SyncSettingsFromUi()
    {
        if (cmbPhotoFormat.SelectedItem is PhotoFormatPreset pFmt)
            _settings.SelectedPhotoFormat = pFmt.Name;

        pnlCustomSize.Visible = _settings.SelectedPhotoFormat == "Custom";
        _settings.CustomWidth = (float)numCustomW.Value;
        _settings.CustomHeight = (float)numCustomH.Value;
        _settings.CustomUnit = cmbCustomUnit.SelectedIndex == 1 ? MeasurementUnit.Millimeters : MeasurementUnit.Inches;

        _settings.FitMode = cmbFitMode.SelectedIndex switch
        {
            1 => FitMode.FitInside,
            2 => FitMode.Stretch,
            _ => FitMode.CropToFill
        };
        bool enableCropSliders = _settings.FitMode == FitMode.CropToFill;
        trkCropX.Enabled = enableCropSliders;
        trkCropY.Enabled = enableCropSliders;
        btnCenterCrop.Enabled = enableCropSliders;

        _settings.CropOffsetX = trkCropX.Value / 100f;
        _settings.CropOffsetY = trkCropY.Value / 100f;
        lblCropXVal.Text = $"{trkCropX.Value}%";
        lblCropYVal.Text = $"{trkCropY.Value}%";

        _settings.Brightness = trkBrightness.Value;
        _settings.Contrast = trkContrast.Value;
        lblBrightnessVal.Text = trkBrightness.Value.ToString();
        lblContrastVal.Text = trkContrast.Value.ToString();
        _settings.Grayscale = chkGrayscale.Checked;

        _settings.EnablePhotoBorder = chkPhotoBorder.Checked;
        _settings.PhotoBorderMm = (float)numBorderMm.Value;

        // Overlays
        _settings.SignaturePosition = cmbSigPos.SelectedIndex switch
        {
            1 => OverlayPosition.BottomLeft,
            2 => OverlayPosition.BottomRight,
            3 => OverlayPosition.TopCenter,
            _ => OverlayPosition.BottomCenter
        };
        _settings.SignatureScalePercent = trkSigScale.Value;
        lblSigScaleVal.Text = $"{trkSigScale.Value}%";
        _settings.SignatureOpacityPercent = trkSigOpacity.Value;
        lblSigOpacityVal.Text = $"{trkSigOpacity.Value}%";
        _settings.SignatureRemoveWhiteBg = chkSigRemoveWhite.Checked;
        _settings.SignatureAutoTrim = chkSigAutoTrim.Checked;

        _settings.NameText = txtName.Text;
        _settings.NameScalePercent = trkNameScale.Value;
        lblNameScaleVal.Text = $"{trkNameScale.Value}%";
        _settings.NamePosition = cmbNamePos.SelectedIndex switch
        {
            1 => OverlayPosition.BottomLeft,
            2 => OverlayPosition.BottomRight,
            3 => OverlayPosition.TopCenter,
            _ => OverlayPosition.BottomCenter
        };
        _settings.NameShowBackground = chkNameBg.Checked;

        _settings.CombineSignatureAndName = chkCombineSigName.Checked;
        _settings.CombinedOverlayPosition = cmbCombinedPos.SelectedIndex switch
        {
            1 => OverlayPosition.BottomLeft,
            2 => OverlayPosition.BottomRight,
            3 => OverlayPosition.TopCenter,
            _ => OverlayPosition.BottomCenter
        };
        _settings.CombinedFullWidthStrip = chkFullWidthStrip.Checked;
        _settings.OverlayBgOpacityPercent = trkOverlayBgOpacity.Value;
        lblOverlayBgOpacityVal.Text = $"{trkOverlayBgOpacity.Value}%";
        _settings.OverlayPaddingMm = (float)numOverlayPaddingMm.Value;
        _settings.OverlayEdgeMarginMm = (float)numOverlayEdgeMarginMm.Value;
        _settings.SignatureNameGapMm = (float)numSigNameGapMm.Value;

        // When combined into 1 entity, Block Position controls both
        cmbSigPos.Enabled = !_settings.CombineSignatureAndName;
        cmbNamePos.Enabled = !_settings.CombineSignatureAndName;
        cmbCombinedPos.Enabled = _settings.CombineSignatureAndName;
        chkFullWidthStrip.Enabled = _settings.CombineSignatureAndName;

        // Sheet & Layout
        if (cmbPaperFormat.SelectedItem is PaperFormatPreset paper)
            _settings.SelectedPaperFormat = paper.Name;
        _settings.Orientation = cmbOrientation.SelectedIndex == 1 ? SheetOrientation.Landscape : SheetOrientation.Portrait;
        _settings.PageMarginMm = (float)numPageMarginMm.Value;
        _settings.PhotoSpacingMm = (float)numPhotoSpacingMm.Value;
        _settings.CutGuideStyle = cmbCutGuides.SelectedIndex switch
        {
            0 => CutGuideStyle.None,
            1 => CutGuideStyle.DashedBorder,
            _ => CutGuideStyle.CornerCropMarks
        };
        _settings.CenterOnPage = chkCenterOnPage.Checked;

        _settings.MixSizes = chkMixSizes.Checked;
        _settings.Copies = (int)numCopies.Value;
        _settings.MixCounts["Passport (35x45mm)"] = (int)numMixPassport.Value;
        _settings.MixCounts["1x1"] = (int)numMix1x1.Value;
        _settings.MixCounts["2x2"] = (int)numMix2x2.Value;
        _settings.MixCounts["2R (Wallet)"] = (int)numMix2R.Value;

        int mixTotal = _settings.MixCounts.Values.Sum();
        lblMixTotal.Text = $"Total requested: {mixTotal}";

        _settings.Dpi = cmbDpi.SelectedIndex switch
        {
            0 => 150,
            2 => 600,
            _ => 300
        };
        _settings.ShowPreviewMarginGuides = chkShowGuides.Checked;
    }

    private void TriggerPreviewUpdate()
    {
        if (_uploadedImage == null || !chkAutoPreview.Checked) return;
        _previewDebounceTimer?.Stop();
        _previewDebounceTimer?.Start();
    }

    private void RenderPreviewNow()
    {
        if (_uploadedImage == null) return;

        try
        {
            var sw = Stopwatch.StartNew();
            var result = PhotoRenderer.RenderSheet(_uploadedImage, _signatureImage, _settings, isPreview: true);
            sw.Stop();

            _currentResult?.SheetBitmap.Dispose();
            _currentResult = result;
            picSheetPreview.Image = result.SheetBitmap;
            UpdatePreviewDisplaySize();

            UpdateTelemetryWithResult(result, sw.ElapsedMilliseconds);
            SetOutputActionsEnabled(true);
        }
        catch (Exception ex)
        {
            UpdateStatus($"Render error: {ex.Message}");
        }
    }

    private void UpdateSourceImageInfo()
    {
        if (_uploadedImage == null)
        {
            lblImageInfo.Text = "No photo loaded.\nSupported: JPG, PNG, BMP, TIFF";
            lblDpiBadge.Text = "DPI Quality: --";
            lblDpiBadge.BackColor = Color.FromArgb(230, 232, 236);
            lblDpiBadge.ForeColor = Color.Black;
            lblSourceMeta.Text = "No image loaded";
            return;
        }

        var photoInches = PrintPresets.GetPhotoSizeInches(_settings);
        float effDpi = PhotoRenderer.CalculateEffectiveDpi(_uploadedImage.Size, photoInches);
        float aspect = (float)_uploadedImage.Width / _uploadedImage.Height;

        lblImageInfo.Text =
            $"File: {_uploadedFileName}\n" +
            $"Size: {_uploadedImage.Width} × {_uploadedImage.Height} px\n" +
            $"Aspect: {aspect:0.00}:1\n" +
            $"Target: {photoInches.Width:0.00} × {photoInches.Height:0.00} in";

        lblSourceMeta.Text = $"{_uploadedFileName} ({_uploadedImage.Width}×{_uploadedImage.Height})";

        if (effDpi >= 250)
        {
            lblDpiBadge.Text = $"Quality: {effDpi:0} DPI (Excellent)";
            lblDpiBadge.BackColor = Color.FromArgb(230, 244, 234);
            lblDpiBadge.ForeColor = Color.FromArgb(19, 115, 51);
        }
        else if (effDpi >= 150)
        {
            lblDpiBadge.Text = $"Quality: {effDpi:0} DPI (Acceptable)";
            lblDpiBadge.BackColor = Color.FromArgb(254, 247, 224);
            lblDpiBadge.ForeColor = Color.FromArgb(176, 96, 0);
        }
        else
        {
            lblDpiBadge.Text = $"Quality: {effDpi:0} DPI (Low Res!)";
            lblDpiBadge.BackColor = Color.FromArgb(252, 232, 230);
            lblDpiBadge.ForeColor = Color.FromArgb(197, 34, 31);
        }
    }

    private void UpdateTelemetryWithoutImage()
    {
        var sheetIn = PrintPresets.GetPaperSizeInches(_settings);
        var photoIn = PrintPresets.GetPhotoSizeInches(_settings);
        int maxCap = PhotoRenderer.CalculateMaxCapacity(_settings, photoIn);

        lblTelSheet.Text = $"Sheet: {_settings.SelectedPaperFormat} {_settings.Orientation} ({sheetIn.Width * 25.4f:0}×{sheetIn.Height * 25.4f:0} mm)";
        lblTelFormat.Text = $"Photo: {_settings.SelectedPhotoFormat} ({photoIn.Width:0.00}×{photoIn.Height:0.00} in)";
        lblMaxCapacity.Text = $"Max capacity on sheet: {maxCap} photos";
    }

    private void UpdateTelemetryWithResult(LayoutResult result, long elapsedMs)
    {
        UpdateTelemetryWithoutImage();

        int placed = result.PlacedItems.Count;
        int req = result.RequestedCount;
        int overflow = result.OverflowCount;

        if (overflow > 0)
        {
            lblTelPlaced.Text = $"Placed: {placed} / {req} ({overflow} overflow!)";
            lblTelPlaced.BackColor = Color.FromArgb(252, 232, 230);
            lblTelPlaced.ForeColor = Color.FromArgb(197, 34, 31);
        }
        else
        {
            lblTelPlaced.Text = $"Placed: {placed} / {req} (Max: {result.MaxCapacityForPrimary})";
            lblTelPlaced.BackColor = Color.FromArgb(230, 244, 234);
            lblTelPlaced.ForeColor = Color.FromArgb(19, 115, 51);
        }

        lblTelQuality.Text = $"Source: {result.EffectiveDpi:0} DPI | Render: {_settings.Dpi} DPI";
        lblRenderTime.Text = $"Rendered in {elapsedMs} ms";

        lblPrintSummary.Text =
            $"Sheet Format: {_settings.SelectedPaperFormat} {_settings.Orientation} ({result.SheetSizeInches.Width:0.00}×{result.SheetSizeInches.Height:0.00} in)\n" +
            $"Sheet Output: {result.SheetBitmap.Width} × {result.SheetBitmap.Height} px @ {_settings.Dpi} DPI\n" +
            $"Photo Format: {(_settings.MixSizes ? "Mixed Sizes" : _settings.SelectedPhotoFormat)}\n" +
            $"Photos Placed: {placed} of {req} requested" + (overflow > 0 ? $" ({overflow} did not fit!)" : "") + "\n" +
            $"Source Print Resolution: {result.EffectiveDpi:0} DPI\n" +
            $"Cut Guides: {_settings.CutGuideStyle} | Margin: {_settings.PageMarginMm:0.#} mm | Gap: {_settings.PhotoSpacingMm:0.#} mm";

        UpdateStatus(overflow > 0
            ? $"Warning: {overflow} photo(s) exceeded sheet capacity. Increase paper size or reduce copies/margins."
            : $"Preview updated: {placed} photo(s) ready on {_settings.SelectedPaperFormat} {_settings.Orientation}.");
    }

    private void SetOutputActionsEnabled(bool enabled)
    {
        tsbExport.Enabled = enabled;
        tsbPrintPreview.Enabled = enabled;
        tsbPrint.Enabled = enabled;
        btnTabPrintPreview.Enabled = enabled;
        btnTabExport.Enabled = enabled;
        btnTabPrint.Enabled = enabled;
    }

    private void SetZoomFit()
    {
        _zoomFitMode = true;
        lblZoomVal.Text = "Fit";
        UpdatePreviewDisplaySize();
    }

    private void SetZoomPercent(float factor)
    {
        _zoomFitMode = false;
        _zoomFactor = Math.Clamp(factor, 0.15f, 3.0f);
        lblZoomVal.Text = $"{_zoomFactor * 100:0}%";
        UpdatePreviewDisplaySize();
    }

    private void AdjustZoom(float delta)
    {
        if (_zoomFitMode)
        {
            _zoomFactor = 0.5f;
        }
        SetZoomPercent(_zoomFactor + delta);
    }

    private void UpdatePreviewDisplaySize()
    {
        if (_currentResult == null || pnlCanvas.ClientSize.Width <= 0 || pnlCanvas.ClientSize.Height <= 0)
            return;

        var bmp = _currentResult.SheetBitmap;
        int targetW, targetH;

        if (_zoomFitMode)
        {
            int pad = 24;
            int availW = Math.Max(50, pnlCanvas.ClientSize.Width - pad);
            int availH = Math.Max(50, pnlCanvas.ClientSize.Height - pad);
            float scale = Math.Min((float)availW / bmp.Width, (float)availH / bmp.Height);
            targetW = Math.Max(20, (int)(bmp.Width * scale));
            targetH = Math.Max(20, (int)(bmp.Height * scale));
        }
        else
        {
            // Scale relative to 96 DPI screen display vs render DPI
            float screenScale = (96f / _settings.Dpi) * _zoomFactor;
            targetW = Math.Max(20, (int)(bmp.Width * screenScale));
            targetH = Math.Max(20, (int)(bmp.Height * screenScale));
        }

        picSheetPreview.Size = new Size(targetW, targetH);
        int x = Math.Max(12, (pnlCanvas.ClientSize.Width - targetW) / 2);
        int y = Math.Max(12, (pnlCanvas.ClientSize.Height - targetH) / 2);
        picSheetPreview.Location = new Point(x, y);
    }

    private PrintDocument CreateConfiguredPrintDocument()
    {
        var printDoc = new PrintDocument();
        var sheetIn = PrintPresets.GetPaperSizeInches(_settings);
        int wHundredths = (int)Math.Round(sheetIn.Width * 100f);
        int hHundredths = (int)Math.Round(sheetIn.Height * 100f);

        // PaperSize expects portrait dimensions when Landscape = true, or custom size
        int rawW = _settings.Orientation == SheetOrientation.Landscape ? hHundredths : wHundredths;
        int rawH = _settings.Orientation == SheetOrientation.Landscape ? wHundredths : hHundredths;

        printDoc.DefaultPageSettings.PaperSize = new PaperSize(_settings.SelectedPaperFormat, rawW, rawH);
        printDoc.DefaultPageSettings.Landscape = _settings.Orientation == SheetOrientation.Landscape;
        printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        printDoc.PrinterSettings.Copies = (short)numPrinterCopies.Value;

        Bitmap? printSheet = null;
        printDoc.BeginPrint += (s, e) =>
        {
            if (_uploadedImage != null)
            {
                var res = PhotoRenderer.RenderSheet(_uploadedImage, _signatureImage, _settings, isPreview: false);
                printSheet = res.SheetBitmap;
            }
        };

        printDoc.PrintPage += (s, pe) =>
        {
            if (pe.Graphics == null || printSheet == null) return;
            pe.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            pe.Graphics.DrawImage(printSheet, pe.PageBounds);
            pe.HasMorePages = false;
        };

        printDoc.EndPrint += (s, e) =>
        {
            printSheet?.Dispose();
            printSheet = null;
        };

        return printDoc;
    }

    private void BtnPrintPreview_Click(object? sender, EventArgs e)
    {
        if (_uploadedImage == null) return;
        using var printDoc = CreateConfiguredPrintDocument();
        using var previewDlg = new PrintPreviewDialog
        {
            Document = printDoc,
            Width = 900,
            Height = 700,
            StartPosition = FormStartPosition.CenterParent
        };
        previewDlg.ShowDialog(this);
    }

    private void BtnPrint_Click(object? sender, EventArgs e)
    {
        if (_uploadedImage == null)
        {
            MessageBox.Show("Please upload an image first.", "Missing Image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var printDoc = CreateConfiguredPrintDocument();
        using var printDialog = new PrintDialog
        {
            Document = printDoc,
            UseEXDialog = true
        };

        if (printDialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            Cursor = Cursors.WaitCursor;
            printDoc.Print();
            UpdateStatus("Print job sent to printer successfully.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Print error: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        if (_uploadedImage == null) return;

        using var sfd = new SaveFileDialog
        {
            Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp",
            FileName = $"PhotoSheet_{_settings.SelectedPaperFormat}_{_settings.SelectedPhotoFormat}_{_settings.Dpi}dpi",
            Title = "Save Print Sheet"
        };

        if (sfd.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            Cursor = Cursors.WaitCursor;
            var res = PhotoRenderer.RenderSheet(_uploadedImage, _signatureImage, _settings, isPreview: false);
            using (res.SheetBitmap)
            {
                var ext = Path.GetExtension(sfd.FileName).ToLowerInvariant();
                var format = ext switch
                {
                    ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                    ".bmp" => ImageFormat.Bmp,
                    _ => ImageFormat.Png
                };
                res.SheetBitmap.Save(sfd.FileName, format);
            }
            UpdateStatus($"Exported high-res sheet to {Path.GetFileName(sfd.FileName)}.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export error: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void UpdateStatus(string msg)
    {
        lblStatus.Text = msg;
    }

    private static Color GetContrastForeColor(Color bg)
    {
        double luminance = (0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B) / 255.0;
        return luminance > 0.55 ? Color.Black : Color.White;
    }
}
