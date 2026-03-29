# Developer Quickstart: Clipboard Manager

**Branch**: `001-clipboard-manager`

---

## Prerequisites

| Requirement        | Version          | Where to get it                                   |
|--------------------|------------------|---------------------------------------------------|
| .NET SDK           | 8.0 (LTS)        | https://dotnet.microsoft.com/download             |
| Windows            | 10 (1903+) or 11 |                                                   |
| Visual Studio      | 2022 (17.8+)     | Community edition is sufficient                   |
| Git                | Any recent       |                                                   |

No admin rights required to build or run.

---

## Clone and Build

```bash
git clone <repo-url>
cd clipclaw

# Restore NuGet packages and build
dotnet build src/Clipclaw/Clipclaw.csproj
```

Expected output: `Build succeeded. 0 Error(s)`.

---

## Run

```bash
dotnet run --project src/Clipclaw/Clipclaw.csproj
```

The app starts minimised to the system tray. Look for the Clipclaw icon in the
notification area (bottom-right corner of the taskbar).

- **Show panel**: `Win + Shift + V`
- **Dismiss panel**: `Escape`
- **Exit**: Right-click tray icon → Exit

---

## Database Location

The SQLite database is created on first run at:

```
%LOCALAPPDATA%\Clipclaw\clipclaw.db
```

To reset to a clean state, stop the app and delete this file.

---

## Running Tests (if tests exist)

```bash
dotnet test tests/Clipclaw.Tests/Clipclaw.Tests.csproj
```

---

## Project Layout

```
src/
└── Clipclaw/                   # WPF application (.csproj)
    ├── App.xaml                # Application entry point, DI setup
    ├── Models/                 # Plain data classes (no dependencies)
    ├── Services/               # Business logic; interface-first
    ├── ViewModels/             # Presentation logic; INotifyPropertyChanged
    ├── Views/                  # XAML windows and panels
    ├── Infrastructure/         # P/Invoke, relay commands, constants
    └── Assets/                 # Icons, images

specs/
└── 001-clipboard-manager/      # Design docs for this feature
    ├── spec.md
    ├── plan.md
    ├── research.md
    ├── data-model.md
    ├── quickstart.md           # This file
    └── contracts/
        ├── keyboard-contract.md
        └── ui-panel-contract.md

tests/
└── Clipclaw.Tests/             # xUnit test project
```

---

## Key Concepts

| Concept                   | Where to look                           |
|---------------------------|-----------------------------------------|
| Clipboard monitoring      | `Services/ClipboardService.cs`          |
| Global hotkey registration| `Services/HotkeyService.cs`             |
| Tray icon setup           | `App.xaml` + `Infrastructure/`         |
| Panel UI + keyboard nav   | `Views/ClipboardPanel.xaml`             |
| Panel logic               | `ViewModels/PanelViewModel.cs`          |
| SQLite queries            | `Services/SqlitePersistenceService.cs`  |
| All Win32 P/Invoke        | `Infrastructure/WindowsClipboardInterop.cs` |
| All named constants       | `Infrastructure/HotkeyConstants.cs`     |

---

## Common Issues

| Symptom                              | Likely cause                           | Fix                                        |
|--------------------------------------|----------------------------------------|--------------------------------------------|
| Panel shortcut doesn't work          | Another app registered `Win+Shift+V`   | Change binding in Settings → Shortcuts     |
| App won't start (DLL not found)      | .NET 8 runtime not installed           | Install .NET 8 Desktop Runtime             |
| Database locked error on startup     | Previous instance still running        | Check system tray; kill orphaned process   |
| Clipboard not capturing              | App lacks window message pump focus    | Check that `AddClipboardFormatListener` is called after `MainWindow` is shown |
