using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using PhotoPrintApp.Models;

namespace PhotoPrintApp.Services;

public static class PhotoRenderer
{
    public static int CalculateMaxCapacity(PrintJobSettings settings, SizeF photoSizeInches)
    {
        var sheetInches = PrintPresets.GetPaperSizeInches(settings);
        float marginIn = settings.PageMarginMm / 25.4f;
        float spacingIn = settings.PhotoSpacingMm / 25.4f;

        float usableW = Math.Max(0.1f, sheetInches.Width - 2 * marginIn);
        float usableH = Math.Max(0.1f, sheetInches.Height - 2 * marginIn);

        float itemW = Math.Min(photoSizeInches.Width, usableW);
        float itemH = Math.Min(photoSizeInches.Height, usableH);

        int cols = Math.Max(1, (int)Math.Floor((usableW + spacingIn + 0.001f) / (itemW + spacingIn)));
        int rows = Math.Max(1, (int)Math.Floor((usableH + spacingIn + 0.001f) / (itemH + spacingIn)));

        // Verify at least 1 fits without excessive clipping
        if (itemW <= 0 || itemH <= 0) return 0;
        return cols * rows;
    }

    public static List<LayoutEntry> BuildLayoutEntries(PrintJobSettings settings)
    {
        var entries = new List<LayoutEntry>();

        if (settings.MixSizes)
        {
            foreach (var kvp in settings.MixCounts)
            {
                if (kvp.Value <= 0) continue;
                var preset = PrintPresets.PhotoFormats.FirstOrDefault(p => p.Name == kvp.Key);
                if (preset == null) continue;

                for (int i = 0; i < kvp.Value; i++)
                {
                    entries.Add(new LayoutEntry(preset.Name, preset.WidthInches, preset.HeightInches));
                }
            }
        }
        else
        {
            var size = PrintPresets.GetPhotoSizeInches(settings);
            for (int i = 0; i < settings.Copies; i++)
            {
                entries.Add(new LayoutEntry(settings.SelectedPhotoFormat, size.Width, size.Height));
            }
        }

        return entries;
    }

    public static LayoutResult RenderSheet(
        Image sourceImage,
        Image? signatureImage,
        PrintJobSettings settings,
        bool isPreview)
    {
        int dpi = settings.Dpi;
        var sheetInches = PrintPresets.GetPaperSizeInches(settings);
        int sheetW = Math.Max(100, (int)Math.Round(sheetInches.Width * dpi));
        int sheetH = Math.Max(100, (int)Math.Round(sheetInches.Height * dpi));

        float spacingPx = settings.PhotoSpacingMm / 25.4f * dpi;
        float pageMarginPx = settings.PageMarginMm / 25.4f * dpi;

        float usableW = Math.Max(10f, sheetW - 2 * pageMarginPx);
        float usableH = Math.Max(10f, sheetH - 2 * pageMarginPx);

        var entries = BuildLayoutEntries(settings);
        int requestedCount = entries.Count;

        // Sort largest area first for better packing in mixed mode
        entries.Sort((a, b) => (b.WidthInches * b.HeightInches).CompareTo(a.WidthInches * a.HeightInches));

        // Prepare transformed source image once
        using var preparedSource = PrepareSourceImage(sourceImage, settings);
        using var preparedSignature = signatureImage != null ? PrepareSignatureImage(signatureImage, settings) : null;

        // Calculate effective DPI for the primary photo format
        var primarySize = PrintPresets.GetPhotoSizeInches(settings);
        float effectiveDpi = CalculateEffectiveDpi(preparedSource.Size, primarySize);
        int maxCapacity = CalculateMaxCapacity(settings, primarySize);

        // Compute placement rects
        var placements = new List<PlacedItem>();
        float curX = pageMarginPx;
        float curY = pageMarginPx;
        float rowMaxH = 0;

        foreach (var entry in entries)
        {
            float imgW = entry.WidthInches * dpi;
            float imgH = entry.HeightInches * dpi;

            if (imgW > usableW || imgH > usableH)
            {
                float scale = Math.Min(usableW / imgW, usableH / imgH);
                imgW *= scale;
                imgH *= scale;
            }

            // Wrap to next row if needed (with tiny floating point tolerance)
            if (curX > pageMarginPx + 0.5f && curX + imgW > pageMarginPx + usableW + 0.5f)
            {
                curX = pageMarginPx;
                curY += rowMaxH + spacingPx;
                rowMaxH = 0;
            }

            if (curY + imgH > pageMarginPx + usableH + 0.5f)
                break;

            placements.Add(new PlacedItem(entry, new RectangleF(curX, curY, imgW, imgH)));

            curX += imgW + spacingPx;
            if (imgH > rowMaxH) rowMaxH = imgH;
        }

        // Center the entire bounding box of placed items within the usable area if enabled
        if (settings.CenterOnPage && placements.Count > 0)
        {
            float minX = placements.Min(p => p.BoundsPx.Left);
            float minY = placements.Min(p => p.BoundsPx.Top);
            float maxX = placements.Max(p => p.BoundsPx.Right);
            float maxY = placements.Max(p => p.BoundsPx.Bottom);

            float contentW = maxX - minX;
            float contentH = maxY - minY;

            float offsetX = pageMarginPx + (usableW - contentW) / 2f - minX;
            float offsetY = pageMarginPx + (usableH - contentH) / 2f - minY;

            placements = placements
                .Select(p => new PlacedItem(
                    p.Entry,
                    new RectangleF(p.BoundsPx.X + offsetX, p.BoundsPx.Y + offsetY, p.BoundsPx.Width, p.BoundsPx.Height)))
                .ToList();
        }

        var sheet = new Bitmap(sheetW, sheetH);
        sheet.SetResolution(dpi, dpi);

        using var g = Graphics.FromImage(sheet);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.White);

        // Cache composite per format key so repeated copies render instantaneously
        var compositeCache = new Dictionary<string, Bitmap>();
        try
        {
            foreach (var item in placements)
            {
                string key = $"{item.Entry.FormatName}_{Math.Round(item.BoundsPx.Width)}_{Math.Round(item.BoundsPx.Height)}";
                if (!compositeCache.TryGetValue(key, out var composite))
                {
                    composite = CreateComposite(
                        preparedSource,
                        preparedSignature,
                        (int)Math.Max(1, Math.Round(item.BoundsPx.Width)),
                        (int)Math.Max(1, Math.Round(item.BoundsPx.Height)),
                        settings);
                    compositeCache[key] = composite;
                }

                g.DrawImage(composite, item.BoundsPx);
                DrawCutGuides(g, item.BoundsPx, settings.CutGuideStyle, dpi);
            }
        }
        finally
        {
            foreach (var bmp in compositeCache.Values)
                bmp.Dispose();
        }

        if (isPreview)
        {
            if (settings.ShowPreviewMarginGuides)
            {
                using var marginPen = new Pen(Color.FromArgb(110, 0, 120, 215), Math.Max(1f, dpi / 150f))
                {
                    DashStyle = DashStyle.Dash
                };
                g.DrawRectangle(marginPen, pageMarginPx, pageMarginPx, usableW, usableH);
            }

            using var borderPen = new Pen(Color.FromArgb(180, 180, 180), Math.Max(1f, dpi / 150f));
            g.DrawRectangle(borderPen, 0, 0, sheetW - 1, sheetH - 1);
        }

        return new LayoutResult(sheet, placements, requestedCount, maxCapacity, effectiveDpi, sheetInches);
    }

    public static float CalculateEffectiveDpi(Size sourceSize, SizeF photoSizeInches)
    {
        if (photoSizeInches.Width <= 0 || photoSizeInches.Height <= 0) return 0;
        float dpiW = sourceSize.Width / photoSizeInches.Width;
        float dpiH = sourceSize.Height / photoSizeInches.Height;
        return Math.Min(dpiW, dpiH);
    }

    private static Bitmap PrepareSourceImage(Image source, PrintJobSettings settings)
    {
        var bmp = new Bitmap(source.Width, source.Height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            bool hasColorAdjust = settings.Brightness != 0 || settings.Contrast != 0 || settings.Grayscale;
            if (hasColorAdjust)
            {
                using var attrs = new ImageAttributes();
                attrs.SetColorMatrix(BuildColorMatrix(settings.Brightness, settings.Contrast, settings.Grayscale));
                g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height),
                    0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attrs);
            }
            else
            {
                g.DrawImage(source, 0, 0, source.Width, source.Height);
            }
        }

        var rotateFlip = GetRotateFlipType(settings.RotationDegrees, settings.FlipHorizontal);
        if (rotateFlip != RotateFlipType.RotateNoneFlipNone)
        {
            bmp.RotateFlip(rotateFlip);
        }

        return bmp;
    }

    private static RotateFlipType GetRotateFlipType(int rotationDegrees, bool flipH)
    {
        int norm = ((rotationDegrees % 360) + 360) % 360;
        return (norm, flipH) switch
        {
            (0, false) => RotateFlipType.RotateNoneFlipNone,
            (90, false) => RotateFlipType.Rotate90FlipNone,
            (180, false) => RotateFlipType.Rotate180FlipNone,
            (270, false) => RotateFlipType.Rotate270FlipNone,
            (0, true) => RotateFlipType.RotateNoneFlipX,
            (90, true) => RotateFlipType.Rotate90FlipX,
            (180, true) => RotateFlipType.Rotate180FlipX,
            (270, true) => RotateFlipType.Rotate270FlipX,
            _ => RotateFlipType.RotateNoneFlipNone
        };
    }

    private static ColorMatrix BuildColorMatrix(int brightness, int contrast, bool grayscale)
    {
        float b = brightness / 100f * 0.5f; // -0.5 to +0.5
        float c = 1f + (contrast / 100f);   // 0.0 to 2.0
        float t = 0.5f * (1f - c) + b;

        float[][] matrix;
        if (grayscale)
        {
            const float rw = 0.299f, gw = 0.587f, bw = 0.114f;
            matrix = new[]
            {
                new[] { rw * c, rw * c, rw * c, 0f, 0f },
                new[] { gw * c, gw * c, gw * c, 0f, 0f },
                new[] { bw * c, bw * c, bw * c, 0f, 0f },
                new[] { 0f,     0f,     0f,     1f, 0f },
                new[] { t,      t,      t,      0f, 1f }
            };
        }
        else
        {
            matrix = new[]
            {
                new[] { c,  0f, 0f, 0f, 0f },
                new[] { 0f, c,  0f, 0f, 0f },
                new[] { 0f, 0f, c,  0f, 0f },
                new[] { 0f, 0f, 0f, 1f, 0f },
                new[] { t,  t,  t,  0f, 1f }
            };
        }

        return new ColorMatrix(matrix);
    }

    private static Bitmap PrepareSignatureImage(Image sig, PrintJobSettings settings)
    {
        var bmp = new Bitmap(sig.Width, sig.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.DrawImage(sig, 0, 0, sig.Width, sig.Height);
        }

        if (settings.SignatureRemoveWhiteBg)
        {
            // Convert near-white pixels to transparent for clean overlay
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color p = bmp.GetPixel(x, y);
                    if (p.A > 0 && p.R > 225 && p.G > 225 && p.B > 225)
                    {
                        bmp.SetPixel(x, y, Color.FromArgb(0, p.R, p.G, p.B));
                    }
                }
            }
        }

        if (settings.SignatureAutoTrim)
        {
            var trimmed = TrimSignatureWhitespace(bmp);
            if (trimmed != null)
            {
                bmp.Dispose();
                return trimmed;
            }
        }

        return bmp;
    }

    private static Bitmap? TrimSignatureWhitespace(Bitmap bmp)
    {
        int minX = bmp.Width, minY = bmp.Height, maxX = -1, maxY = -1;
        for (int y = 0; y < bmp.Height; y++)
        {
            for (int x = 0; x < bmp.Width; x++)
            {
                Color p = bmp.GetPixel(x, y);
                bool isInk = p.A > 20 && !(p.R > 240 && p.G > 240 && p.B > 240);
                if (isInk)
                {
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (maxX < minX || maxY < minY) return null;

        int pad = 2;
        int cropX = Math.Max(0, minX - pad);
        int cropY = Math.Max(0, minY - pad);
        int cropW = Math.Min(bmp.Width - cropX, (maxX - minX + 1) + pad * 2);
        int cropH = Math.Min(bmp.Height - cropY, (maxY - minY + 1) + pad * 2);

        if (cropW <= 0 || cropH <= 0) return null;
        return bmp.Clone(new Rectangle(cropX, cropY, cropW, cropH), bmp.PixelFormat);
    }

    private static Bitmap CreateComposite(
        Image source,
        Image? signature,
        int w,
        int h,
        PrintJobSettings settings)
    {
        var bmp = new Bitmap(w, h);
        bmp.SetResolution(settings.Dpi, settings.Dpi);

        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        g.Clear(settings.FitBackgroundColor);

        var destRect = new Rectangle(0, 0, w, h);
        switch (settings.FitMode)
        {
            case FitMode.CropToFill:
                DrawImageCropped(g, source, destRect, settings.CropOffsetX, settings.CropOffsetY);
                break;
            case FitMode.FitInside:
                DrawImageFitInside(g, source, destRect);
                break;
            case FitMode.Stretch:
                g.DrawImage(source, destRect);
                break;
        }

        bool hasName = !string.IsNullOrWhiteSpace(settings.NameText);
        if (settings.CombineSignatureAndName && (signature != null || hasName))
        {
            DrawCombinedSignatureAndName(g, signature, w, h, settings);
        }
        else
        {
            // Independent Signature overlay
            if (signature != null)
            {
                float scale = settings.SignatureScalePercent / 100f;
                int sigMaxW = Math.Max(10, (int)(w * scale));
                int sigMaxH = Math.Max(10, (int)(h * (scale * 0.5f)));
                var sigSize = ScaleToFit(signature.Size, new Size(sigMaxW, sigMaxH));
                int edgePad = Math.Max(0, (int)Math.Round(settings.OverlayEdgeMarginMm / 25.4f * settings.Dpi));
                float boxPad = Math.Max(0f, settings.OverlayPaddingMm / 25.4f * settings.Dpi);

                var sigPos = GetOverlayPoint(settings.SignaturePosition, w, h, sigSize.Width, sigSize.Height, edgePad);
                if (settings.NameShowBackground)
                {
                    int bgAlpha = Math.Clamp((int)Math.Round(settings.OverlayBgOpacityPercent / 100f * 255f), 0, 255);
                    using var bgBrush = new SolidBrush(Color.FromArgb(bgAlpha, settings.NameBackgroundColor));
                    g.FillRectangle(bgBrush, sigPos.X - boxPad, sigPos.Y - boxPad, sigSize.Width + boxPad * 2, sigSize.Height + boxPad * 2);
                }

                DrawSignatureImage(g, signature, new Rectangle(sigPos.X, sigPos.Y, sigSize.Width, sigSize.Height), settings.SignatureOpacityPercent);
            }

            // Independent Name overlay
            if (hasName)
            {
                int edgePad = Math.Max(0, (int)Math.Round(settings.OverlayEdgeMarginMm / 25.4f * settings.Dpi));
                float boxPad = Math.Max(0f, settings.OverlayPaddingMm / 25.4f * settings.Dpi);
                float maxTextW = Math.Max(20f, w - (edgePad + boxPad) * 2f);

                using var scaledFont = CreateFittedNameFont(g, settings, maxTextW);
                var textSize = g.MeasureString(settings.NameText, scaledFont, int.MaxValue, StringFormat.GenericTypographic);

                var pt = GetOverlayPoint(settings.NamePosition, w, h, (int)Math.Ceiling(textSize.Width), (int)Math.Ceiling(textSize.Height), edgePad);

                if (settings.NameShowBackground)
                {
                    int bgAlpha = Math.Clamp((int)Math.Round(settings.OverlayBgOpacityPercent / 100f * 255f), 0, 255);
                    using var bgBrush = new SolidBrush(Color.FromArgb(bgAlpha, settings.NameBackgroundColor));
                    g.FillRectangle(bgBrush, pt.X - boxPad, pt.Y - boxPad, textSize.Width + boxPad * 2, textSize.Height + boxPad * 2);
                }

                using var textBrush = new SolidBrush(settings.NameColor);
                g.DrawString(settings.NameText, scaledFont, textBrush, pt.X, pt.Y, StringFormat.GenericTypographic);
            }
        }

        // Optional Photo Border
        if (settings.EnablePhotoBorder && settings.PhotoBorderMm > 0)
        {
            float borderPx = Math.Max(1f, settings.PhotoBorderMm / 25.4f * settings.Dpi);
            using var pen = new Pen(settings.PhotoBorderColor, borderPx);
            g.DrawRectangle(pen, borderPx / 2f, borderPx / 2f, w - borderPx, h - borderPx);
        }

        return bmp;
    }

    private static void DrawCombinedSignatureAndName(
        Graphics g,
        Image? signature,
        int w,
        int h,
        PrintJobSettings settings)
    {
        bool hasName = !string.IsNullOrWhiteSpace(settings.NameText);
        float boxPad = Math.Max(0f, settings.OverlayPaddingMm / 25.4f * settings.Dpi);
        int edgePad = settings.CombinedFullWidthStrip
            ? 0
            : Math.Max(0, (int)Math.Round(settings.OverlayEdgeMarginMm / 25.4f * settings.Dpi));

        Size sigSize = Size.Empty;
        if (signature != null)
        {
            float scale = settings.SignatureScalePercent / 100f;
            int sigMaxW = Math.Max(8, (int)(w * scale));
            int sigMaxH = Math.Max(8, (int)(h * (scale * 0.5f)));
            sigSize = ScaleToFit(signature.Size, new Size(sigMaxW, sigMaxH));
        }

        Font? scaledFont = null;
        SizeF textSize = SizeF.Empty;
        if (hasName)
        {
            float maxTextW = Math.Max(20f, w - (boxPad + edgePad) * 2f);
            scaledFont = CreateFittedNameFont(g, settings, maxTextW);
            textSize = g.MeasureString(settings.NameText, scaledFont, int.MaxValue, StringFormat.GenericTypographic);
        }

        try
        {
            float gap = (signature != null && hasName)
                ? Math.Max(0f, settings.SignatureNameGapMm / 25.4f * settings.Dpi)
                : 0f;
            float contentW = Math.Max(sigSize.Width, textSize.Width);
            float contentH = sigSize.Height + gap + textSize.Height;

            int boxW = (int)Math.Ceiling(contentW + boxPad * 2);
            int boxH = (int)Math.Ceiling(contentH + boxPad * 2);

            Point boxPt = GetOverlayPoint(settings.CombinedOverlayPosition, w, h, boxW, boxH, edgePad);

            RectangleF bgRect = settings.CombinedFullWidthStrip
                ? new RectangleF(0, boxPt.Y, w, boxH)
                : new RectangleF(boxPt.X, boxPt.Y, boxW, boxH);

            if (settings.NameShowBackground)
            {
                int bgAlpha = Math.Clamp((int)Math.Round(settings.OverlayBgOpacityPercent / 100f * 255f), 0, 255);
                using var bgBrush = new SolidBrush(Color.FromArgb(bgAlpha, settings.NameBackgroundColor));
                g.FillRectangle(bgBrush, bgRect);
            }

            float centerX = bgRect.X + bgRect.Width / 2f;
            float curY = bgRect.Y + boxPad;

            // 1. Signature on top
            if (signature != null)
            {
                int sigX = (int)Math.Round(centerX - sigSize.Width / 2f);
                int sigY = (int)Math.Round(curY);
                DrawSignatureImage(g, signature, new Rectangle(sigX, sigY, sigSize.Width, sigSize.Height), settings.SignatureOpacityPercent);
                curY += sigSize.Height + gap;
            }

            // 2. Name directly below signature
            if (hasName && scaledFont != null)
            {
                float textX = centerX - textSize.Width / 2f;
                using var textBrush = new SolidBrush(settings.NameColor);
                g.DrawString(settings.NameText, scaledFont, textBrush, textX, curY, StringFormat.GenericTypographic);
            }
        }
        finally
        {
            scaledFont?.Dispose();
        }
    }

    private static Font CreateFittedNameFont(Graphics g, PrintJobSettings settings, float maxTextWidthPx)
    {
        float targetPt = Math.Max(3f, settings.NameFont.Size * (settings.NameScalePercent / 100f));
        var font = new Font(settings.NameFont.FontFamily, targetPt, settings.NameFont.Style, GraphicsUnit.Point);
        var measured = g.MeasureString(settings.NameText, font, int.MaxValue, StringFormat.GenericTypographic);

        // Auto-shrink proportionally if text exceeds photo width
        if (measured.Width > maxTextWidthPx && measured.Width > 0)
        {
            float shrinkRatio = maxTextWidthPx / measured.Width;
            font.Dispose();
            float fittedPt = Math.Max(3f, targetPt * shrinkRatio);
            font = new Font(settings.NameFont.FontFamily, fittedPt, settings.NameFont.Style, GraphicsUnit.Point);
        }

        return font;
    }

    private static void DrawSignatureImage(Graphics g, Image signature, Rectangle destRect, int opacityPercent)
    {
        using var attrs = new ImageAttributes();
        float alpha = Math.Clamp(opacityPercent / 100f, 0.05f, 1f);
        var cm = new ColorMatrix { Matrix33 = alpha };
        attrs.SetColorMatrix(cm);

        g.DrawImage(signature, destRect, 0, 0, signature.Width, signature.Height, GraphicsUnit.Pixel, attrs);
    }

    private static Point GetOverlayPoint(OverlayPosition pos, int containerW, int containerH, int itemW, int itemH, int pad)
    {
        return pos switch
        {
            OverlayPosition.BottomLeft => new Point(pad, containerH - itemH - pad),
            OverlayPosition.BottomRight => new Point(containerW - itemW - pad, containerH - itemH - pad),
            OverlayPosition.TopCenter => new Point((containerW - itemW) / 2, pad),
            _ => new Point((containerW - itemW) / 2, containerH - itemH - pad) // BottomCenter
        };
    }

    private static void DrawImageCropped(Graphics g, Image src, Rectangle dest, float offsetX, float offsetY)
    {
        float srcAspect = (float)src.Width / src.Height;
        float destAspect = (float)dest.Width / dest.Height;

        RectangleF srcRect;
        if (srcAspect > destAspect)
        {
            float cropW = src.Height * destAspect;
            float maxShift = (src.Width - cropW) / 2f;
            float x = maxShift + Math.Clamp(offsetX, -1f, 1f) * maxShift;
            srcRect = new RectangleF(x, 0, cropW, src.Height);
        }
        else
        {
            float cropH = src.Width / destAspect;
            float maxShift = (src.Height - cropH) / 2f;
            float y = maxShift + Math.Clamp(offsetY, -1f, 1f) * maxShift;
            srcRect = new RectangleF(0, y, src.Width, cropH);
        }

        g.DrawImage(src, dest, srcRect, GraphicsUnit.Pixel);
    }

    private static void DrawImageFitInside(Graphics g, Image src, Rectangle dest)
    {
        var fitted = ScaleToFit(src.Size, dest.Size);
        int x = dest.X + (dest.Width - fitted.Width) / 2;
        int y = dest.Y + (dest.Height - fitted.Height) / 2;
        g.DrawImage(src, new Rectangle(x, y, fitted.Width, fitted.Height));
    }

    private static Size ScaleToFit(Size original, Size maxSize)
    {
        if (original.Width <= 0 || original.Height <= 0) return maxSize;
        float ratio = Math.Min((float)maxSize.Width / original.Width, (float)maxSize.Height / original.Height);
        return new Size(Math.Max(1, (int)(original.Width * ratio)), Math.Max(1, (int)(original.Height * ratio)));
    }

    private static void DrawCutGuides(Graphics g, RectangleF rect, CutGuideStyle style, int dpi)
    {
        if (style == CutGuideStyle.None) return;

        float lineW = Math.Max(1f, dpi / 300f);
        using var pen = new Pen(Color.FromArgb(170, 90, 90, 90), lineW);

        if (style == CutGuideStyle.DashedBorder)
        {
            pen.DashStyle = DashStyle.Dash;
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }
        else if (style == CutGuideStyle.CornerCropMarks)
        {
            float len = Math.Max(8f, dpi * 0.08f);
            float x1 = rect.Left, y1 = rect.Top, x2 = rect.Right, y2 = rect.Bottom;

            // Top-Left
            g.DrawLine(pen, x1 - len, y1, x1, y1);
            g.DrawLine(pen, x1, y1 - len, x1, y1);
            // Top-Right
            g.DrawLine(pen, x2, y1, x2 + len, y1);
            g.DrawLine(pen, x2, y1 - len, x2, y1);
            // Bottom-Left
            g.DrawLine(pen, x1 - len, y2, x1, y2);
            g.DrawLine(pen, x1, y2, x1, y2 + len);
            // Bottom-Right
            g.DrawLine(pen, x2, y2, x2 + len, y2);
            g.DrawLine(pen, x2, y2, x2, y2 + len);
        }
    }
}
