# SnapForge

**SnapForge** - A lightweight, open-source screenshot & annotation tool for Windows, featuring a unique hybrid TUI + GUI architecture.

| TUI Dashboard (Panel Mode) | TUI Terminal Mode | Annotation Editor |
|---|---|---|
| Tokyo Night themed dashboard with log, status, and commands | Full-screen terminal with bash-like command input | WinForms-based screenshot editor with drawing tools |

## Features

- **Screen Capture** - Drag-to-select region capture with full-screen overlay
- **Annotation Tools** - Arrow, rectangle, ellipse, line, freehand, text, and mosaic/blur
- **TUI Dashboard** - Terminal.Gui-based control panel with Tokyo Night color theme
- **Terminal Mode** - Switch to a raw bash/cmd-like terminal interface
- **Global Hotkey** - Configurable system-wide hotkey (default: PrintScreen)
- **System Tray** - Runs in background with tray icon and context menu
- **Auto-start** - Optional Windows startup registration
- **JSON Config** - All settings stored in a human-readable config file
- **Clipboard + File** - Copy to clipboard and/or save to disk

## Quick Start

### Requirements

- Windows 10 1809+ / Windows 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build & Run

```bash
cd src
dotnet build
dotnet run
```

### Release Build

```bash
cd src
dotnet publish -c Release -r win-x64 --self-contained
```

## Usage

### TUI Panel Mode

Launch SnapForge to open the TUI dashboard. Use the bottom command input or buttons:

| Key | Action |
|-----|--------|
| F1 | Take screenshot |
| F2 | Show help |
| F3 | Open settings dialog |
| F4 | About |
| F5 | Exit |

Type commands directly in the command input box. Any keypress auto-focuses the input.

### TUI Terminal Mode

Type `mode` to switch between Panel and Terminal mode. Terminal mode provides a raw, frameless bash-like interface.

### Commands

```
capture          Take a screenshot
mode             Switch between panel/terminal mode
config           Show current configuration
set <key> <val>  Set a config value
get <key>        Get a config value
hotkey           Open hotkey recorder dialog
autostart on|off Enable/disable Windows auto-start
about            Show about info
clear            Clear log
exit             Exit application
help             Show available commands
```

### Config Keys for `set` / `get`

| Key | Aliases | Description |
|-----|---------|-------------|
| `hotkey` | - | Global hotkey combo (e.g. `Ctrl+Shift+X`) |
| `savedirectory` | `dir` | Screenshot save directory |
| `imageformat` | `format` | Image format: `png`, `jpg`, `bmp` |
| `defaultcolor` | `color` | Default annotation color (hex, e.g. `#FF0000`) |
| `defaultpenwidth` | `penwidth` | Annotation pen width in pixels |
| `defaultfontsize` | `fontsize` | Text annotation font size |
| `mosaicblocksize` | `mosaic` | Mosaic pixelation block size |

### Annotation Editor

After capturing a screenshot region, the annotation editor opens with these tools:

| Tool | Shortcut | Description |
|------|----------|-------------|
| Arrow | A | Draw arrows |
| Rectangle | R | Draw rectangles |
| Ellipse | E | Draw ellipses |
| Line | L | Draw straight lines |
| Freehand | F | Freehand drawing |
| Text | T | Add text annotations |
| Mosaic | M | Pixelate/blur regions |

**Editor actions:**
- **Ctrl+Z** - Undo last annotation
- **Ctrl+S** - Save to file
- **Ctrl+C** - Copy to clipboard
- **Esc** - Cancel / close editor
- **Mouse wheel** - Adjust pen width

### System Tray

Right-click the tray icon for quick actions:
- Screenshot capture
- Open TUI panel
- Settings
- Exit

Left-click the tray icon to open the TUI panel.

## Configuration

Config file location: `%APPDATA%\SnapForge\config.json`

```json
{
  "SaveDirectory": "C:\\Users\\YourName\\Pictures",
  "ImageFormat": "png",
  "JpegQuality": 90,
  "AutoStart": false,
  "MinimizeToTray": true,
  "PlaySound": false,
  "CopyToClipboard": true,
  "Hotkey": "PrintScreen",
  "MosaicBlockSize": 12,
  "DefaultPenWidth": 2,
  "DefaultColor": "#FF0000",
  "DefaultFontSize": 16,
  "DefaultFontFamily": "Microsoft YaHei"
}
```

### Config Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `SaveDirectory` | string | `~/Pictures` | Directory to save screenshots |
| `ImageFormat` | string | `"png"` | Output format: `png`, `jpg`, `bmp` |
| `JpegQuality` | int | `90` | JPEG compression quality (1-100) |
| `AutoStart` | bool | `false` | Start with Windows |
| `MinimizeToTray` | bool | `true` | Minimize to system tray |
| `PlaySound` | bool | `false` | Play sound on capture |
| `CopyToClipboard` | bool | `true` | Copy screenshot to clipboard |
| `Hotkey` | string | `"PrintScreen"` | Global hotkey combination |
| `MosaicBlockSize` | int | `12` | Mosaic pixelation block size |
| `DefaultPenWidth` | int | `2` | Default drawing pen width |
| `DefaultColor` | string | `"#FF0000"` | Default annotation color (hex) |
| `DefaultFontSize` | int | `16` | Text tool font size |
| `DefaultFontFamily` | string | `"Microsoft YaHei"` | Text tool font family |

### Hotkey Format

Hotkey strings use `+` to combine modifiers and a key:

```
PrintScreen           Single key
Ctrl+Shift+X          Modifier combo
Alt+F9                Alt + function key
Ctrl+Alt+S            Multiple modifiers
```

Supported modifiers: `Ctrl`, `Shift`, `Alt`, `Win`

## Tech Stack

- **.NET 8.0** - Runtime
- **Terminal.Gui 2.x** - TUI framework (Tokyo Night TrueColor theme)
- **WinForms** - GUI annotation editor & hotkey recorder
- **System.Drawing** - Image processing and rendering

## Architecture

SnapForge uses a hybrid architecture with two UI layers running on separate threads:

```
Program.cs (main loop)
  |
  +-- TrayIcon (WinForms STA thread)
  |     +-- Global Hotkey listener
  |     +-- Context menu
  |
  +-- TuiMainWindow (Console thread)
  |     +-- Panel Mode (dashboard)
  |     +-- Terminal Mode (raw CLI)
  |     +-- Returns TuiAction enum
  |
  +-- ScreenshotOverlay (WinForms)
  |     +-- Full-screen transparent overlay
  |     +-- Drag-to-select region
  |
  +-- AnnotationEditor (WinForms)
        +-- Drawing tools
        +-- Save / copy to clipboard
```

The TUI never nests `Application.Init()`/`Run()` calls. Instead, `TuiMainWindow.Run()` returns a `TuiAction` enum, and the main loop handles the action after the TUI exits.

## Project Structure

```
ScreenshotPlugin/
+-- src/
|   +-- Program.cs                 # Entry point & main loop
|   +-- App/
|   |   +-- Config.cs              # JSON config management
|   |   +-- TrayIcon.cs            # System tray & global hotkey
|   |   +-- GlobalHotkey.cs        # Win32 hotkey registration
|   |   +-- AutoStartManager.cs    # Windows auto-start registry
|   +-- TUI/
|   |   +-- TuiMainWindow.cs       # TUI dashboard & terminal
|   +-- GUI/
|   |   +-- ScreenshotOverlay.cs   # Screen capture overlay
|   |   +-- AnnotationEditor.cs    # Annotation editor window
|   |   +-- Tools/
|   |       +-- AnnotationTool.cs  # Tool definitions
|   |       +-- AnnotationRenderer.cs # Drawing renderer
|   +-- ScreenshotPlugin.csproj
+-- LICENSE                        # Apache-2.0
+-- README.md
```

## License

Licensed under the [Apache License 2.0](LICENSE).

Copyright 2026 lhx077

## Author

**lhx077** - [GitHub](https://github.com/lhx077)

---

<p align="center">
  <b>SnapForge</b> - Capture * Annotate * Share
</p>
