# Implementation Plan: Clipboard Manager

**Branch**: `001-clipboard-manager` | **Date**: 2026-03-29 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-clipboard-manager/spec.md`

## Summary

Build Clipclaw: a Windows system-tray clipboard manager that captures every text
copy event, stores up to 500 items in a local SQLite database, and lets the user
instantly paste any stored item using configurable global keyboard shortcuts. A
Material Design panel (summoned by `Win + Shift + V`) provides keyboard-navigable
history with pinned items and frequently-used recommendations.

**Stack**: C# 12 / .NET 8 WPF · SQLite · MaterialDesignInXamlToolkit ·
Hardcodet.NotifyIcon.Wpf · NHotkey · Win32 P/Invoke

## Technical Context

**Language/Version**: C# 12 / .NET 8 LTS
**Primary Dependencies**:
- `MaterialDesignThemes` — modern UI components
- `Hardcodet.Wpf.TaskbarNotification` — WPF-native tray icon
- `NHotkey.Wpf` — global hotkey registration
- `Microsoft.Data.Sqlite` — local clipboard history storage
- `Microsoft.Extensions.DependencyInjection` — DI container

**Storage**: SQLite (single `clipclaw.db` file at `%LOCALAPPDATA%\Clipclaw\`)
**Testing**: xUnit (optional — not requested in spec; project scaffolded for future use)
**Target Platform**: Windows 10 (1903+) and Windows 11 — no admin elevation required
**Project Type**: Desktop application (WPF, single executable)
**Performance Goals**:
- Panel open in < 300 ms (SC-003)
- Clipboard capture with zero perceptible delay (SC-002)
- Idle CPU: < 0.1%, idle memory: < 50 MB
**Constraints**: No cloud/network; all data local; no admin rights; no WinForms dependency
**Scale/Scope**: 1 user, up to 500 clip items, 14 configurable shortcut bindings

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle                    | Status | Notes |
|------------------------------|--------|-------|
| I.  Spec-First Development   | ✅ Pass | `spec.md` complete, no unresolved markers |
| II. User-Story Orientation   | ✅ Pass | All tasks will map to US1 (P1), US2 (P2), or US3 (P3) |
| III. TDD                     | ✅ Pass | Tests not requested in spec; project scaffolded for future |
| IV. Keyboard-First           | ✅ Pass | FR-002, FR-013: all operations keyboard-accessible; shortcut constants centralised in `HotkeyConstants.cs` |
| V.  Readable Code            | ✅ Pass | Interface-first services, named constants, MVVM pattern, no magic strings/keys |
| VI. Simplicity & YAGNI       | ✅ Pass | Single project, 5 NuGet packages, no premature abstractions |
| VII. Windows-Native Reliability | ✅ Pass | `AddClipboardFormatListener` (non-destructive), explicit Win32 error handling, session events handled in `App.xaml.cs` |

**Post-design re-check**: All gates still pass. No complexity violations; no Complexity
Tracking table entries required.

## Project Structure

### Documentation (this feature)

```text
specs/001-clipboard-manager/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research findings
├── data-model.md        # Entity definitions and SQLite schema
├── quickstart.md        # Developer setup guide
├── contracts/
│   ├── keyboard-contract.md   # All keyboard shortcuts and panel focus rules
│   └── ui-panel-contract.md   # Panel layout, sections, visual constraints
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
src/
└── Clipclaw/
    ├── Clipclaw.csproj
    ├── App.xaml                   # DI setup, tray init, session event handlers
    ├── App.xaml.cs
    │
    ├── Models/
    │   ├── ClipItem.cs            # Clipboard entry with metadata
    │   ├── ShortcutBinding.cs     # Key combination → action mapping
    │   └── AppSettings.cs         # User preferences
    │
    ├── Services/
    │   ├── IClipboardService.cs
    │   ├── ClipboardService.cs    # Win32 clipboard monitoring + read/write
    │   ├── IHotkeyService.cs
    │   ├── HotkeyService.cs       # NHotkey registration + conflict detection
    │   ├── IPersistenceService.cs
    │   ├── SqlitePersistenceService.cs  # All database operations
    │   └── UsageTrackingService.cs     # PasteCount + LastPastedAt updates
    │
    ├── ViewModels/
    │   ├── ViewModelBase.cs       # INotifyPropertyChanged + RelayCommand helpers
    │   ├── PanelViewModel.cs      # History list, search, sections, keyboard nav
    │   └── SettingsViewModel.cs   # Settings form + shortcut recorder
    │
    ├── Views/
    │   ├── ClipboardPanel.xaml    # Floating panel (search + 3 sections)
    │   ├── ClipboardPanel.xaml.cs # KeyDown handler for navigation
    │   ├── SettingsWindow.xaml    # Tabbed settings dialog
    │   └── SettingsWindow.xaml.cs
    │
    ├── Infrastructure/
    │   ├── RelayCommand.cs        # ICommand implementation
    │   ├── WindowsClipboardInterop.cs  # All Win32 P/Invoke declarations
    │   ├── WindowMessageHandler.cs     # HwndSource + WM_CLIPBOARDUPDATE routing
    │   └── HotkeyConstants.cs     # All action names and reserved key list
    │
    └── Assets/
        └── Icons/
            └── tray.ico

tests/
└── Clipclaw.Tests/
    ├── Clipclaw.Tests.csproj
    └── unit/                      # Scaffolded; populated when tests are requested
```

**Structure Decision**: Single WPF project. All clipboard, hotkey, tray, and
persistence concerns live within `src/Clipclaw/`. A separate `tests/` project
is scaffolded but empty until tests are explicitly requested. No multi-project
solution complexity needed for a single-window utility.

## Complexity Tracking

> No constitution violations — this table is intentionally empty.
