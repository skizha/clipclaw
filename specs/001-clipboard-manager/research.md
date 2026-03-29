# Research: Clipboard Manager

**Branch**: `001-clipboard-manager` | **Date**: 2026-03-29
**Purpose**: Resolve all technical unknowns before design begins.

---

## 1. UI Framework

**Decision**: WPF (.NET 8 LTS)

WPF is the most mature choice for a small, single-developer Windows tray utility.
It has comprehensive documentation, 15+ years of community resources, and
established patterns for everything Clipclaw needs: custom-styled panels, system
tray integration, keyboard navigation, and the Win32 P/Invoke interop required for
clipboard monitoring and global hotkeys.

**Alternatives considered**:
- **WinUI 3 (Windows App SDK)** — the "future" of Windows UI, but a smaller
  ecosystem, more API churn, and steeper setup for a small utility. Not justified.
- **Windows Forms** — legacy; insufficient styling capabilities for a modern UI.

---

## 2. Clipboard Monitoring

**Decision**: Win32 `AddClipboardFormatListener` via P/Invoke

The officially recommended approach for Windows Vista and later. Each listener is
independent — unlike the legacy `SetClipboardViewer` chain, a failing listener
cannot break others. The hidden WPF `MainWindow` provides the required window
handle; `HwndSource` routes the `WM_CLIPBOARDUPDATE` (0x031D) message to managed
code with no polling overhead.

Cleanup uses `RemoveClipboardFormatListener` on app shutdown, satisfying the
Windows-Native Reliability principle.

**Alternatives considered**:
- **SetClipboardViewer** — deprecated linked-list approach; fragile, not recommended.
- **IDataObject polling** — background thread, continuous CPU use, battery drain.
  Prohibited by the Windows-Native Reliability principle.

---

## 3. Global Hotkeys

**Decision**: [NHotkey](https://github.com/grokys/NHotkey) NuGet package

NHotkey wraps `RegisterHotKey` / `UnregisterHotKey` cleanly, integrates with
WPF's message pump, exposes an event-driven API, and handles duplicate-registration
errors gracefully. Its conflict-detection support directly satisfies FR-008.

**Alternatives considered**:
- **Raw `RegisterHotKey` P/Invoke** — possible, but requires manual message-pump
  wiring via `HwndSource`, custom conflict detection, and significant boilerplate.
  YAGNI: NHotkey delivers all of this in one package.
- **GlobalHotKey NuGet** — lighter but no built-in conflict detection.

---

## 4. System Tray Integration

**Decision**: [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon) NuGet package

A WPF-native tray icon library with no WinForms dependency. The `TaskbarIcon`
control is declared in XAML and supports full data binding to ViewModel commands,
making it MVVM-compliant and consistent with the rest of the codebase.

**Alternatives considered**:
- **`System.Windows.Forms.NotifyIcon`** — pulls in a WinForms assembly, requires
  event handlers rather than commands, less XAML-friendly.
- **Windows App SDK tray** — WinUI 3 only; not applicable here.

---

## 5. Local Persistence

**Decision**: SQLite via `Microsoft.Data.Sqlite` NuGet package

SQLite is the right fit for up to 500 clip items with indexable queries (sort by
paste count, filter by text, fetch pinned items). It stores a single `.db` file
locally, supports async operations, and never leaves the device — satisfying the
spec assumption of no cloud sync. Schema evolution (adding columns) is
straightforward via `ALTER TABLE`.

Settings (hotkey bindings, max history size, preferences) are stored in a
`Settings` table in the same database for a single-file deployment.

**Alternatives considered**:
- **LiteDB** — document database, heavier for simple tabular data. No advantage
  over SQLite for this schema.
- **JSON file** — fine for prototyping but causes full-file rewrite on every copy
  event and lacks indexed queries. Rejected for production.

---

## 6. Modern UI Theming

**Decision**: [MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)

Material Design 3 is widely recognised as a contemporary, clean visual language.
The toolkit ships pre-styled controls (ListBox, TextBox, Button, ScrollViewer),
1000+ icons, and built-in light/dark theme switching — all compatible with WPF
data binding. This directly satisfies FR-014 and SC-008 with minimal custom XAML.

**Alternatives considered**:
- **ModernWpf** — lighter, mimics Fluent Design, fewer components. Good if
  "Windows-native" look is preferred over Material. Valid backup option.
- **MahApps.Metro** — Metro-inspired (Windows 8 era), showing its age in 2025.
- **Hand-rolled styles** — maximum control but prohibitively time-consuming for a
  solo developer; violates the YAGNI principle.

---

## 7. Architecture Pattern

**Decision**: Layered MVVM — `Models` / `Services` / `ViewModels` / `Views` / `Infrastructure`

MVVM is the industry-standard pattern for WPF. It separates domain data (Models),
platform operations (Services), presentation logic (ViewModels), and UI markup
(Views). Interface-first services (e.g., `IClipboardService`) enable unit testing
of ViewModels without the real Win32 APIs. `Infrastructure` isolates all P/Invoke
declarations and low-level interop into a single named location, satisfying the
Readable Code principle (no magic scattered across files).

Dependency injection via `Microsoft.Extensions.DependencyInjection` wires services
as singletons in `App.xaml.cs`.

**Alternatives considered**:
- **Flat structure** — unmaintainable beyond ~20 files.
- **Feature-based folders** — appropriate for multi-feature apps; overkill here.
- **MVVM framework (Prism / MvvmLight)** — adds dependencies and learning curve
  for boilerplate that is trivial to write by hand.

---

## Summary

| Concern             | Decision                          | Package / API                            |
|---------------------|-----------------------------------|------------------------------------------|
| UI framework        | WPF .NET 8                        | Built-in SDK                             |
| Clipboard monitor   | AddClipboardFormatListener        | Win32 P/Invoke (no NuGet)                |
| Global hotkeys      | NHotkey                           | `NHotkey.Wpf`                            |
| System tray         | Hardcodet.NotifyIcon.Wpf          | `Hardcodet.Wpf.TaskbarNotification`      |
| Persistence         | SQLite                            | `Microsoft.Data.Sqlite`                  |
| Theming             | Material Design in XAML Toolkit   | `MaterialDesignThemes`                   |
| Architecture        | Layered MVVM                      | `Microsoft.Extensions.DependencyInjection` |
| Testing             | xUnit (optional per spec)         | `xunit`, `xunit.runner.visualstudio`     |
