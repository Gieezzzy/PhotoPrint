# Photo Print Studio (`PhotoPrintApp`)

A Windows desktop application built with **.NET 6 Windows Forms** for preparing, customizing, and printing ID, passport, and standard photo sheets with live preview, image adjustments, name/signature overlays, and **QR-code mobile phone signing**.

---

## Features

### 1. Photo Formats & Custom Sizing
- **Built-in Presets**: `Passport (35×45 mm)`, `Passport (US 2×2 in)`, `1×1 in`, `2×2 in`, `2R (Wallet 2.5×3.5 in)`, `3R (3.5×5 in)`, `4R (4×6 in)`, `5R (5×7 in)`, `A5`, and `A4`.
- **Custom Format**: Define any custom width and height in **Inches (`in`)** or **Millimeters (`mm`)**.

### 2. Framing, Adjustments & Borders
- **Fit Modes**: `Crop to Fill`, `Fit Inside` (with selectable background color), and `Stretch`.
- **Crop Framing**: Horizontal and vertical crop offset sliders (`-100%` to `+100%`) plus `Center Crop` reset.
- **Transforms**: Rotate `-90°`, Rotate `+90°`, and Flip Horizontal.
- **Tone Adjustments**: Brightness, Contrast, and Black & White (Grayscale) toggle.
- **Photo Border**: Optional frame around each photo with configurable thickness (`mm`) and color.

### 3. Signature & Name Overlays
- **Combined 1-Entity Block**: Places the **Signature directly above the Name** inside a shared, customizable background box or full-width footer/header strip.
- **Fine Spacing Controls**: Adjustable **Box Padding (`mm`)**, **Edge Margin (`mm`)**, **Signature-Name Gap (`mm`)**, and **Background Opacity (`0%`–`100%`)**.
- **Signature Tools**:
  - Load signature from file (`PNG`, `JPG`, `BMP`, etc.) or capture live from a smartphone.
  - **Auto-Trim Whitespace**: Automatically crops empty margins around signature strokes.
  - **Make White Transparent**: Removes light backgrounds for clean overlays.
- **Name Tools**: Custom font picker, text color, text scale (`40%`–`200%`), and automatic width fitting.

### 4. Sign via Phone (QR Code + Local Server)
- Click **Sign via Phone (QR)...** to start an embedded local HTTP signature server (`TcpListener`) and display a scannable QR code.
- Scan with any phone on the same Wi-Fi/LAN, draw your signature on the touch-friendly HTML5 canvas, and tap **Submit Signature** — it appears immediately in the app.

### 5. Sheet Layout, Live Preview & Printing
- **Paper Formats**: `A4`, `Letter`, `Legal`, `A5`, `4R`, and `5R` in **Portrait** or **Landscape**.
- **Layout Controls**: Page margins (`mm`), photo spacing (`mm`), center-on-page toggle, **Fill Page (Max)** button, and **Mix Sizes** mode.
- **Cut Guides**: `None`, `Dashed Border`, or `Corner Crop Marks`.
- **Live Preview & Telemetry**: Real-time debounced preview with zoom controls (`Fit`, `100%`, `+`/`-`), margin guide toggle, placement/overflow counters, and source DPI quality badge.
- **Print & Export**: Print directly, open Print Preview, or export the full print sheet as high-resolution `PNG`, `JPEG`, or `BMP` at `150`, `300`, or `600 DPI`.

---

## Tech Stack

- **Framework**: `.NET 6.0` (`net6.0-windows`, Windows Forms)
- **Graphics & Printing**: `System.Drawing.Common` (`6.0.0`)
- **QR Code Generation**: `QRCoder` (`1.8.1`)

---

## Getting Started

### Prerequisites
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) (or newer) on Windows

### Build & Run
```powershell
git clone https://github.com/Gieezzzy/PhotoPrint.git
cd PhotoPrint
dotnet restore
dotnet build
dotnet run
```

---

## Project Structure

```text
PhotoPrintApp/
├── Assets/
│   ├── app.ico                  # Multi-resolution application icon
│   └── logo.png                 # Application toolbar logo
├── Forms/
│   └── QrSignatureForm.cs       # QR code modal & network IP selector
├── Models/
│   └── PrintModels.cs           # Presets, layout entries, and PrintJobSettings
├── Services/
│   ├── MobileSignatureServer.cs # Embedded local HTTP server & mobile canvas pad
│   └── PhotoRenderer.cs         # Image processing, overlay compositing & sheet layout
├── Form1.cs                     # Main application logic & print/export handlers
├── Form1.Designer.cs            # Tabbed UI & live preview layout
├── Program.cs                   # Application entry point
└── PhotoPrintApp.csproj         # Project configuration & dependencies
```

---

## Notes on Phone QR Signature
- Ensure your PC and smartphone are connected to the **same Wi-Fi / local network**.
- If prompted by **Windows Defender Firewall** the first time you open **Sign via Phone (QR)...**, allow access on Private networks so your phone can reach the local port.
