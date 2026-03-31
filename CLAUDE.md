# Clipclaw Development Guidelines

Auto-generated from feature plan `001-clipboard-manager`. Last updated: 2026-03-30

## Active Technologies
- C# 12 / .NET 8 LTS + WPF, MaterialDesignThemes.Wpf (Material Design 3), (002-clip-panel-ux)
- SQLite via `Microsoft.Data.Sqlite`; database at (002-clip-panel-ux)

- **Language**: C# 12 / .NET 8 LTS
- **UI Framework**: WPF (Windows Presentation Foundation)
- **UI Theme**: MaterialDesignInXamlToolkit (Material Design 3)
- **Tray Icon**: Hardcodet.Wpf.TaskbarNotification
- **Global Hotkeys**: NHotkey.Wpf
- **Persistence**: SQLite via Microsoft.Data.Sqlite
- **DI Container**: Microsoft.Extensions.DependencyInjection
- **Architecture**: Layered MVVM
- **Testing**: xUnit (scaffolded, optional)

## Project Structure

```text
src/Clipclaw/          # WPF application project
├── Models/            # Plain data classes (ClipItem, AppSettings, ShortcutBinding)
├── Services/          # Business logic with interfaces (Clipboard, Hotkey, Persistence)
├── ViewModels/        # Presentation logic (INotifyPropertyChanged, commands)
├── Views/             # XAML windows (ClipboardPanel, SettingsWindow)
├── Infrastructure/    # P/Invoke, RelayCommand, HotkeyConstants
└── Assets/Icons/      # tray.ico and other images

tests/Clipclaw.Tests/  # xUnit project (scaffolded)
specs/                 # Design docs (spec, plan, research, data-model, contracts)
```

## Commands

```bash
# Build
dotnet build src/Clipclaw/Clipclaw.csproj

# Run
dotnet run --project src/Clipclaw/Clipclaw.csproj

# Test
dotnet test tests/Clipclaw.Tests/Clipclaw.Tests.csproj
```

## Code Style

- **Naming**: PascalCase for types/methods, camelCase for locals/fields
- **Constants**: All hotkey action names and Win32 message constants live in
  `Infrastructure/HotkeyConstants.cs` — no magic strings or numbers elsewhere
- **Interfaces**: Every service MUST have an `I`-prefixed interface in the same folder
- **Function size**: Functions > ~40 lines are a signal to decompose (Readable Code principle)
- **Comments**: Explain *why*, not *what* — the code states what it does
- **No magic**: Every key combination, window message ID, and SQLite table name
  MUST be a named constant

## Recent Changes
- 002-clip-panel-ux: Added C# 12 / .NET 8 LTS + WPF, MaterialDesignThemes.Wpf (Material Design 3),

- `001-clipboard-manager`: Established full stack (WPF + SQLite + NHotkey +
  Hardcodet tray + MaterialDesign), MVVM architecture, clipboard capture via
  `AddClipboardFormatListener`, keyboard-navigable panel, usage tracking.

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
