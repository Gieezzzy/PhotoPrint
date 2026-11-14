using System.Drawing;

namespace PhotoPrintApp.Models;

public enum FitMode
{
    CropToFill,
    FitInside,
    Stretch
}

public enum OverlayPosition
{
    BottomCenter,
    BottomLeft,
    BottomRight,
    TopCenter
}

public enum CutGuideStyle
{
    None,
    DashedBorder,
    CornerCropMarks
}

public enum SheetOrientation
{
    Portrait,
    Landscape
}

public enum MeasurementUnit
{
    Inches,
    Millimeters
}

public record PhotoFormatPreset(string Name, float WidthInches, float HeightInches, string Description)
{
    public override string ToString() => $"{Name} ({Description})";
}

public record PaperFormatPreset(string Name, float WidthInches, float HeightInches, string Description)
{
    public override string ToString() => $"{Name} ({Description})";
}

public class LayoutEntry
{
    public string FormatName { get; }
    public float WidthInches { get; }
    public float HeightInches { get; }

    public LayoutEntry(string formatName, float widthInches, float heightInches)
    {
        FormatName = formatName;
        WidthInches = widthInches;
        HeightInches = heightInches;
    }
}

public class PlacedItem
{
    public LayoutEntry Entry { get; }
    public RectangleF BoundsPx { get; }

    public PlacedItem(LayoutEntry entry, RectangleF boundsPx)
    {
        Entry = entry;
        BoundsPx = boundsPx;
    }
}

public class LayoutResult
{
    public Bitmap SheetBitmap { get; }
    public List<PlacedItem> PlacedItems { get; }
    public int RequestedCount { get; }
    public int OverflowCount => Math.Max(0, RequestedCount - PlacedItems.Count);
    public int MaxCapacityForPrimary { get; }
    public float EffectiveDpi { get; }
    public SizeF SheetSizeInches { get; }

    public LayoutResult(
        Bitmap sheetBitmap,
        List<PlacedItem> placedItems,
        int requestedCount,
        int maxCapacityForPrimary,
        float effectiveDpi,
        SizeF sheetSizeInches)
    {
        SheetBitmap = sheetBitmap;
        PlacedItems = placedItems;
        RequestedCount = requestedCount;
        MaxCapacityForPrimary = maxCapacityForPrimary;
        EffectiveDpi = effectiveDpi;
        SheetSizeInches = sheetSizeInches;
    }
}

public class PrintJobSettings
{
    // Photo Format
    public string SelectedPhotoFormat { get; set; } = "2x2";
    public float CustomWidth { get; set; } = 2.0f;
    public float CustomHeight { get; set; } = 2.0f;
    public MeasurementUnit CustomUnit { get; set; } = MeasurementUnit.Inches;

    // Framing & Adjustments
    public FitMode FitMode { get; set; } = FitMode.CropToFill;
    public Color FitBackgroundColor { get; set; } = Color.White;
    public float CropOffsetX { get; set; } = 0f; // -1.0 to +1.0
    public float CropOffsetY { get; set; } = 0f; // -1.0 to +1.0
    public int RotationDegrees { get; set; } = 0; // 0, 90, 180, 270
    public bool FlipHorizontal { get; set; } = false;
    public int Brightness { get; set; } = 0; // -100 to +100
    public int Contrast { get; set; } = 0;   // -100 to +100
    public bool Grayscale { get; set; } = false;

    // Photo Border
    public bool EnablePhotoBorder { get; set; } = false;
    public float PhotoBorderMm { get; set; } = 0.5f;
    public Color PhotoBorderColor { get; set; } = Color.Black;

    // Signature Overlay
    public OverlayPosition SignaturePosition { get; set; } = OverlayPosition.BottomRight;
    public int SignatureScalePercent { get; set; } = 20; // 5 to 60
    public int SignatureOpacityPercent { get; set; } = 100; // 10 to 100
    public bool SignatureRemoveWhiteBg { get; set; } = true;
    public bool SignatureAutoTrim { get; set; } = true;

    // Name Overlay
    public string NameText { get; set; } = "";
    public Font NameFont { get; set; } = new("Arial", 8, FontStyle.Bold);
    public int NameScalePercent { get; set; } = 100; // 40 to 200
    public Color NameColor { get; set; } = Color.Black;
    public OverlayPosition NamePosition { get; set; } = OverlayPosition.BottomCenter;
    public bool NameShowBackground { get; set; } = true;
    public Color NameBackgroundColor { get; set; } = Color.White;

    // Combined Signature + Name Entity & Spacing
    public bool CombineSignatureAndName { get; set; } = true;
    public OverlayPosition CombinedOverlayPosition { get; set; } = OverlayPosition.BottomCenter;
    public bool CombinedFullWidthStrip { get; set; } = false;
    public int OverlayBgOpacityPercent { get; set; } = 85;
    public float OverlayPaddingMm { get; set; } = 1.0f;
    public float OverlayEdgeMarginMm { get; set; } = 1.0f;
    public float SignatureNameGapMm { get; set; } = 0.5f;

    // Sheet & Layout
    public string SelectedPaperFormat { get; set; } = "A4";
    public SheetOrientation Orientation { get; set; } = SheetOrientation.Portrait;
    public float PageMarginMm { get; set; } = 8f;
    public float PhotoSpacingMm { get; set; } = 4f;
    public CutGuideStyle CutGuideStyle { get; set; } = CutGuideStyle.CornerCropMarks;
    public bool CenterOnPage { get; set; } = true;
    public int Dpi { get; set; } = 300;

    // Copies / Mix
    public int Copies { get; set; } = 4;
    public bool MixSizes { get; set; } = false;
    public Dictionary<string, int> MixCounts { get; } = new()
    {
        { "Passport (35x45mm)", 0 },
        { "1x1", 0 },
        { "2x2", 0 },
        { "2R (Wallet)", 0 }
    };

    // Preview-only visual aids
    public bool ShowPreviewMarginGuides { get; set; } = true;
}

public static class PrintPresets
{
    public static readonly List<PhotoFormatPreset> PhotoFormats = new()
    {
        new("Passport (35x45mm)", 1.38f, 1.77f, "35×45 mm"),
        new("Passport (US 2x2)", 2.0f, 2.0f, "2×2 in"),
        new("1x1", 1.0f, 1.0f, "1×1 in / ID"),
        new("2x2", 2.0f, 2.0f, "2×2 in / ID"),
        new("2R (Wallet)", 2.5f, 3.5f, "2.5×3.5 in"),
        new("3R", 3.5f, 5.0f, "3.5×5 in"),
        new("4R", 4.0f, 6.0f, "4×6 in"),
        new("5R", 5.0f, 7.0f, "5×7 in"),
        new("A5", 5.83f, 8.27f, "148×210 mm"),
        new("A4", 8.27f, 11.69f, "210×297 mm"),
        new("Custom", 2.0f, 2.0f, "Custom Size")
    };

    public static readonly List<PaperFormatPreset> PaperFormats = new()
    {
        new("A4", 8.27f, 11.69f, "210×297 mm"),
        new("Letter", 8.5f, 11.0f, "8.5×11 in"),
        new("Legal", 8.5f, 14.0f, "8.5×14 in"),
        new("A5", 5.83f, 8.27f, "148×210 mm"),
        new("4R", 4.0f, 6.0f, "4×6 in"),
        new("5R", 5.0f, 7.0f, "5×7 in")
    };

    public static SizeF GetPhotoSizeInches(PrintJobSettings settings)
    {
        if (settings.SelectedPhotoFormat == "Custom")
        {
            float factor = settings.CustomUnit == MeasurementUnit.Millimeters ? 1f / 25.4f : 1f;
            return new SizeF(
                Math.Max(0.2f, settings.CustomWidth * factor),
                Math.Max(0.2f, settings.CustomHeight * factor));
        }

        var preset = PhotoFormats.FirstOrDefault(p => p.Name == settings.SelectedPhotoFormat)
                     ?? PhotoFormats[3]; // 2x2 fallback
        return new SizeF(preset.WidthInches, preset.HeightInches);
    }

    public static SizeF GetPaperSizeInches(PrintJobSettings settings)
    {
        var preset = PaperFormats.FirstOrDefault(p => p.Name == settings.SelectedPaperFormat)
                     ?? PaperFormats[0]; // A4 fallback

        return settings.Orientation == SheetOrientation.Landscape
            ? new SizeF(preset.HeightInches, preset.WidthInches)
            : new SizeF(preset.WidthInches, preset.HeightInches);
    }
}
