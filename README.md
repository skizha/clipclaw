# Clipclaw

A keyboard-first Windows clipboard manager. Clipclaw sits in the system tray, silently captures everything you copy, and lets you recall any past clip with a single hotkey — no mouse required.

## Features

- **Instant recall** — open the clip panel from anywhere with `Ctrl+Shift+C` (configurable)
- **Paste by shortcut** — assign up to five clips to `Ctrl+Shift+1` … `Ctrl+Shift+5` per item in **Edit** (slots are not tied to list position)
- **Full keyboard navigation** — arrow keys, Page Up/Down, Home/End, Enter to paste
- **Pin items** — keep important clips at the top permanently (`Ctrl+P` or right-click → Pin / Unpin)
- **Edit clips** — short label, full text, and optional shortcut slot (`F2`, double-click a row, or right-click → **Edit**)
- **Add clip** — create a clip manually with the **+** button or `Ctrl+N` when the panel is open
- **Delete clips** — remove any item with confirmation (`Delete`)
- **Usage tracking** — USES column counts how many times each clip has been pasted
- **Sections** — Pinned, Frequent, and Recent, each with its own accent colour on the section header and on the selected row
- **Search** — type instantly to filter; first Escape clears search, second closes panel
- **Alternating rows** — subtle row shading for easy scanning
- **Light and Dark themes** — switchable from Settings → General
- **Persist history** — clipboard survives restarts (optional, SQLite-backed)
- **Configurable shortcuts** — remap global hotkeys from Settings → Shortcuts
- **Auto-start** — optionally launch with Windows

## Keyboard Reference

### Panel navigation

| Key | Action |
|-----|--------|
| `Ctrl+Shift+C` | Open / close clip panel (default; configurable) |
| `↑` / `↓` | Move selection up / down |
| `Page Up` / `Page Down` | Jump 5 rows |
| `Home` / `End` | First / last item |
| `Enter` | Paste selected item |
| `Escape` | Clear search (first press) / close panel (second press) |
| Any letter/digit | Focus search box and begin filtering |

### Item actions (panel open)

| Key / gesture | Action |
|---------------|--------|
| `Ctrl+Shift+1` – `5` | Paste the clip assigned to that slot in **Edit** (works globally when configured) |
| `F2` | Edit selected item (short name, text, shortcut slot) |
| Double-click row | Same as `F2` |
| `Ctrl+N` | Add a new clip (same as the **+** button) |
| `Delete` | Delete selected item (with confirmation) |
| `Ctrl+P` | Pin / unpin selected item |
| Right-click | Context menu: Pin / Unpin, **Edit**, Delete |
| `Apps` or `Shift+F10` | Open context menu for selected item |

## Getting Started

### Requirements

- Windows 10 or 11
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (Desktop)

### Build from source

```bash
git clone https://github.com/skizha/clipclaw
cd clipclaw

# Build
dotnet build src/Clipclaw/Clipclaw.csproj

# Run
dotnet run --project src/Clipclaw/Clipclaw.csproj

# Test
dotnet test tests/Clipclaw.Tests/Clipclaw.Tests.csproj
```

### First run

1. Clipclaw starts in the system tray (bottom-right taskbar area).
2. Copy anything to the clipboard — Clipclaw captures it automatically.
3. Press `Ctrl+Shift+C` to open the panel.
4. Navigate with arrow keys, press `Enter` to paste into the active window.

## Settings

Access Settings by right-clicking the tray icon → **Settings**.

| Setting | Default | Description |
|---------|---------|---------------|
| Max history size | 50 | Maximum number of clips to keep |
| Launch on startup | On | Start with Windows |
| Keep history across restarts | On | Persist clips to SQLite on exit |
| Theme | Dark | Dark or Light colour theme |

Shortcuts can be remapped on the **Shortcuts** tab. Click any shortcut field and press the new key combination.

## Project Structure

```
src/Clipclaw/
├── Models/            # Data classes (ClipItem, AppSettings, ShortcutBinding)
├── Services/          # Business logic with interfaces (Clipboard, Hotkey, Persistence)
├── ViewModels/        # Presentation logic (MVVM, INotifyPropertyChanged, commands)
├── Views/             # XAML windows (ClipboardPanel, SettingsWindow, EditClipDialog)
├── Infrastructure/    # P/Invoke, RelayCommand, HotkeyConstants, ThemeService, Converters
└── Assets/Icons/      # Tray icon

tests/Clipclaw.Tests/  # xUnit project
specs/                 # Design docs (spec, plan, research, data-model, contracts)
```

## Tech Stack

| Component | Technology |
|-----------|------------|
| Language | C# 12 / .NET 8 LTS |
| UI | WPF (Windows Presentation Foundation) |
| UI theme | MaterialDesignInXamlToolkit (Material Design 3) |
| Tray icon | Hardcodet.Wpf.TaskbarNotification |
| Global hotkeys | NHotkey.Wpf |
| Persistence | SQLite via Microsoft.Data.Sqlite |
| DI container | Microsoft.Extensions.DependencyInjection |
| Architecture | Layered MVVM |
| Tests | xUnit |

## How It Works

- **Clipboard capture** — registers `AddClipboardFormatListener` (Win32) on the hidden main window; receives `WM_CLIPBOARDUPDATE` on every copy.
- **Global hotkeys** — NHotkey registers system-wide key combinations; hotfires `WM_HOTKEY` messages handled in the main window procedure.
- **Paste simulation** — sets the target text onto the clipboard then sends `Ctrl+V` via `SendInput` (Win32) to the previously active window.
- **Theme switching** — MaterialDesign's `PaletteHelper` + `BundledTheme`; custom `DynamicResource` brushes in `App.xaml` updated at runtime by `ThemeService`.
- **Persistence** — single SQLite file at `%APPDATA%\Clipclaw\clipclaw.db`; history pruned to `MaxHistorySize` on each write.

## License

MIT
