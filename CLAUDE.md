# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BilibiliDownloader is a WPF desktop application (C#/.NET 10.0) for downloading Bilibili videos. It uses Python backends (`Search.py`, `download.py`) compiled to standalone executables via PyInstaller and invoked as child processes.

## Build Commands

```bash
# Build
dotnet build BilibiliDownloader/BilibiliDownloader.csproj

# Run
dotnet run --project BilibiliDownloader/BilibiliDownloader.csproj

# Publish release
dotnet publish BilibiliDownloader/BilibiliDownloader.csproj -c Release

# Compile Python backends (requires PyInstaller)
pyinstaller --onefile BilibiliDownloader/Search.py
pyinstaller --onefile BilibiliDownloader/download.py
```

Solution file: `BilibiliDownloader.slnx` (modern slnx format). No test project exists.

## Architecture

### C# / WPF Layer

- **App.xaml** -- Defines all global control styles (`PrimaryButtonStyle`, `ModernTextBoxStyle`, `ModernComboBoxStyle`, etc.) using Bilibili's pink accent (`#FB7299`). Styles use `DynamicResource` for theme-switchable colors and `StaticResource` for shared primary colors.
- **App.xaml.cs** -- `App.SetTheme(bool)` swaps the first `MergedDictionary` between `Themes/LightTheme.xaml` and `Themes/DarkTheme.xaml` at runtime.
- **Themes/LightTheme.xaml, DarkTheme.xaml** -- Define the same set of resource keys (colors + brushes) with different values. Primary colors (`#FB7299`) are identical in both; semantic colors (background, card, border, text, title bar) differ.
- **MainWindow.xaml / .cs** -- Chrome-less window (`WindowStyle=None` + `WindowChrome`) with custom title bar. All UI logic is code-behind (no MVVM/ViewModels).
- **AppConfig.cs** -- Persists `AppSettings` (default save path, dark mode flag) to `%LOCALAPPDATA%\BilibiliDownloader\settings.json` via `System.Text.Json`.
- **StreamInfo.cs** -- `StreamInfo` (audio), `VideoStreamInfo` (video, adds Width/Height), `SearchResponse` (wraps lists + title + error). `DisplayText` property drives ComboBox display.

### Python Backends

Both scripts communicate with the C# app via stdout JSON.

- **Search.py** -- `Search.exe <url> <cookie>`. Fetches the Bilibili page HTML, extracts `window.__playinfo__` JSON, and outputs a `SearchResponse`-shaped JSON with sorted video/audio streams. Uses `requests`.
- **download.py** -- `download.exe <url> <title> <video_url> <audio_url> <output_dir> <mode>`. Downloads streams via `requests` with Bilibili-specific headers. Mode is `only_audio`, `only_video`, or `merge` (uses `ffmpeg`). Launched in a visible `cmd.exe` window so the user sees progress.

### Workflow

1. User enters URL, selects download mode (audio-only / video-only / both)
2. **Query** -> `Search.exe` returns JSON via stdout -> parsed into `SearchResponse`
3. Quality ComboBoxes populated: single dropdown for audio-only/video-only; two dropdowns (video + audio) for merge mode
4. **Download** -> `download.exe` launched via `cmd.exe /c` in a new window

### Download mode invocations

```
only_audio:  download.exe <url> <title> ""          <audio_url> <output_dir> only_audio
only_video:  download.exe <url> <title> <video_url> ""          <output_dir> only_video
merge:       download.exe <url> <title> <video_url> <audio_url> <output_dir> merge
```

### Runtime dependencies

`Search.exe`, `download.exe`, and `ffmpeg.exe` (merge mode only) must be in the output directory (`bin\Debug\net10.0-windows\`).

## Key Details

- Target framework: `net10.0-windows`, WPF enabled, nullable enabled, implicit usings
- No NuGet dependencies beyond the SDK
- UI language is Chinese
- Custom window chrome with drag-to-move, maximize/minimize/close, theme toggle
- Download button stays disabled until a quality selection is made
- Theme preference persists across sessions
