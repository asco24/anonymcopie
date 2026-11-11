# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**AnonymCopie** is a Windows desktop app (WinForms, .NET 8, `net8.0-windows`) for numbering exam copies ("compostage des copies d'examen"). It prints one blank A4 sheet per number. Each sheet carries the same formatted number twice: once in the header and once in the body, for example `L1SN-001`. The UI and all user-facing text are in French.

Development happens on Linux, but the app only runs on Windows. The `.csproj` sets `EnableWindowsTargeting=true` so it can be built here. The UI can't be launched or seen on Linux, so layout and printing changes have to be checked by the user on Windows.

## Commands

The project folder and namespace are still called `ComptCopie`. Only `AssemblyName` and `Product` were renamed to `AnonymCopie`.

```bash
# Build (compile check)
dotnet build ComptCopie/ComptCopie.csproj

# Release: self-contained single-file exe (~160 MB) → publish/AnonymCopie.exe
dotnet publish ComptCopie/ComptCopie.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

There are no tests and no linter.

**Distribution.** The user runs the exe from an internal SMB share. Copy each release there and check the copy with `cmp`. The share path is kept out of the repo, in `CLAUDE.local.md` (gitignored). Never commit it.
- If the exe is running on a Windows PC, the file on the share is locked and the copy can fail or leave it unreadable. Ask the user to close the app first.
- An old `ComptCopie.exe` (pre-rename) may still be in that folder.

## Architecture

- **`Program.cs`** runs two message loops one after the other: first `SplashForm` (an animated fade-in with a progress bar, which closes itself), then `MainForm`.
- **`MainForm`** builds the whole UI in code. There is no designer file. It uses a 3-column `TableLayoutPanel` (label | input | side buttons), and the form is `AutoSize` with `GrowAndShrink`. Two things follow from this:
  - Any label whose text changes width will resize the whole window. That's why `_endValue` and `_lastLabel` are switched to fixed width (`AutoSize = false`) and sized in the `Shown` event from `root.GetColumnWidths()`.
  - Control sizes that depend on DPI scaling, such as the ＋/－ buttons matching the prefix combo's height, must be set in `Shown`/`SizeChanged`, not in the constructor. The real heights are only known after Windows has applied its scaling.
- **`NumberPrinter`** wraps a `PrintDocument`.
  - `Configure(prefix, digits, start, quantity, printer, alignment)` is called before each preview or print. `StringAlignment` Near/Center/Far are the "à gauche / centrée / à droite" print buttons, and they apply to both the header and the body.
  - The `Printed` event fires only when `e.PrintAction == PrintToPrinter`. A preview never moves the counters forward.
- **`AppSettings`** is JSON stored in `%AppData%\AnonymCopie\settings.json`.
  - On first run it copies `%AppData%\ComptCopie\settings.json` from the pre-rename version, if that file exists.
  - It keeps one counter per prefix (`LastByPrefix`). After a print, the counter only moves forward, never back (`Math.Max`), so reprinting an old range doesn't reset it.
  - `PrefixList` is the user-managed list of prefixes, added and removed with the ＋/－ buttons through `PrefixDialog`.
  - `AppSettings.Format(prefix, n, digits)` is the single place where numbers are formatted (`PREFIX-000n`).
- **`AppAssets`** loads `Assets/logo.png` and `Assets/logo.ico`, which are embedded resources with the logical names `logo.png` and `logo.ico`. `logo.ico` is also the exe's `ApplicationIcon`. The logo is an original design (a sheet with a red "N°" stamp on a green background). The UI colors are green `(0,150,40)` and red `(200,20,30)`.

## Behavior the user explicitly asked for

- After printing, the printed range stays on screen. The "Nouvelle impression" button, and the app at startup, set the start to the last printed number for the selected prefix + 1 and reset the quantity to 1.
- The input is a start number plus a quantity, not a start and an end.
- The preview button uses the alignment from the last print button clicked.
- The header and body numbers use the same font size (16 pt bold Arial).
