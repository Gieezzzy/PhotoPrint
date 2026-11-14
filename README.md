# Photo Print Studio

A Windows desktop app (C# / .NET 6 WinForms) that prepares photos for printing on A4 paper.

## Features

- Upload a photo (JPG, PNG, BMP, TIFF) with a DPI quality indicator
- Choose a print format: Passport, 1x1, 2x2, A5, A4 or custom size
- Framing and crop tools: fit mode, horizontal/vertical crop, rotate, flip
- Color and tone adjustments: brightness, contrast, grayscale
- Optional photo border/frame
- Add a signature (including capture from a phone via QR code) and a name overlay
- Arrange up to 10 copies on one A4 sheet (mixed Passport / 1x1 / 2x2), with margins between photos
- Preview the A4 layout, then print using your printer settings

## Requirements

- Windows 10/11
- .NET 6 SDK (only to build from source)

## Build and run

```bash
dotnet run --project PhotoPrintApp.csproj
```

## Publish a standalone exe

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

The result is `publish/PhotoPrintApp.exe`, which runs without installing .NET.

## Usage

1. Browse and load a photo.
2. Pick the print format and adjust framing/tone.
3. Add a signature and name if needed.
4. Choose the layout and number of copies (max 10).
5. Click Process to preview the A4 sheet, then Print.
