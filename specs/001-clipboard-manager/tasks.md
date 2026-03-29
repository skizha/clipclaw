---
description: "Task list for Clipboard Manager implementation"
---

# Tasks: Clipboard Manager

**Input**: Design documents from `specs/001-clipboard-manager/`
**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ contracts/ ✅

**Tests**: Not explicitly requested in spec. Test project is scaffolded (T004) but
test task phases are omitted per spec. Add tests when explicitly requested.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- All file paths are relative to the repository root

---

## Phase 1: Setup

**Purpose**: Create the project, install packages, scaffold folders.

- [ ] T001 Create .NET 8 WPF application project at `src/Clipclaw/Clipclaw.csproj` with `<OutputType>WinExe</OutputType>`, `<TargetFramework>net8.0-windows</TargetFramework>`, `<UseWPF>true</UseWPF>`
- [ ] T002 [P] Add NuGet packages to `src/Clipclaw/Clipclaw.csproj`: `MaterialDesignThemes`, `Hardcodet.Wpf.TaskbarNotification`, `NHotkey.Wpf`, `Microsoft.Data.Sqlite`, `Microsoft.Extensions.DependencyInjection`
- [ ] T003 [P] Create folder structure inside `src/Clipclaw/`: `Models/`, `Services/`, `ViewModels/`, `Views/`, `Infrastructure/`, `Assets/Icons/`
- [ ] T004 [P] Scaffold xUnit test project at `tests/Clipclaw.Tests/Clipclaw.Tests.csproj` (empty, no tests yet — placeholder for future use)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared infrastructure that every user story depends on. No user story
work begins until this phase is complete.

**⚠️ CRITICAL**: All Phase 3/4/5 tasks depend on this phase completing first.

- [ ] T005 Create `src/Clipclaw/Infrastructure/HotkeyConstants.cs` — named string constants for every action name (`ShowPanel`, `PasteItem_1` … `PasteItem_5`), Win32 message constants (`WM_CLIPBOARDUPDATE = 0x031D`, `WM_HOTKEY = 0x0312`), and the reserved Windows shortcut list (Win+C, Win+V, Ctrl+C, Ctrl+V, Ctrl+X, Ctrl+Z, Alt+F4, etc.) — no magic values anywhere else in the codebase
- [ ] T006 [P] Create `src/Clipclaw/Infrastructure/RelayCommand.cs` — `ICommand` implementation with `Execute(object?)` and `CanExecute(object?)`, supporting both `Action` and `Action<T>` constructors
- [ ] T007 [P] Create `src/Clipclaw/ViewModels/ViewModelBase.cs` — implements `INotifyPropertyChanged`; provides `SetProperty<T>` helper that sets the backing field and raises `PropertyChanged` only when the value changes
- [ ] T008 Create `src/Clipclaw/Infrastructure/WindowsClipboardInterop.cs` — all Win32 P/Invoke declarations: `AddClipboardFormatListener(IntPtr hwnd)`, `RemoveClipboardFormatListener(IntPtr hwnd)`, `RegisterHotKey`, `UnregisterHotKey`; each import attributed with `[DllImport("user32.dll", SetLastError = true)]`
- [ ] T009 [P] Create `src/Clipclaw/Models/ClipItem.cs` — fields: `Id` (int), `Text` (string), `CopiedAt` (DateTime), `LastPastedAt` (DateTime?), `PasteCount` (int), `IsPinned` (bool), `DisplayOrder` (int); no constructor logic, plain POCO
- [ ] T010 [P] Create `src/Clipclaw/Models/AppSettings.cs` — fields: `MaxHistorySize` (int, default 50), `LaunchOnStartup` (bool, default true), `PersistHistory` (bool, default true), `PanelShortcut` (string, default `"Win+Shift+V"`)
- [ ] T011 [P] Create `src/Clipclaw/Models/ShortcutBinding.cs` — fields: `Id` (int), `ActionName` (string), `Modifiers` (string), `Key` (string), `IsEnabled` (bool); `ActionName` values MUST only be assigned from `HotkeyConstants` constants
- [ ] T012 Create `src/Clipclaw/Services/IPersistenceService.cs` — interface declaring: `InitialiseAsync()`, `GetAllClipItemsAsync()`, `UpsertClipItemAsync(ClipItem)`, `DeleteClipItemAsync(int id)`, `ClearNonPinnedAsync()`, `GetSettingsAsync()`, `SaveSettingsAsync(AppSettings)`, `GetShortcutBindingsAsync()`, `SaveShortcutBindingAsync(ShortcutBinding)`, `IncrementPasteCountAsync(int id)`
- [ ] T013 Create `src/Clipclaw/Services/SqlitePersistenceService.cs` — implements `IPersistenceService`; database path: `Path.Combine(Environment.GetFolderPath(SpecialFolder.LocalApplicationData), "Clipclaw", "clipclaw.db")`; `InitialiseAsync` creates the `ClipItems`, `AppSettings`, and `ShortcutBindings` tables (schema from `data-model.md`) and seeds default `ShortcutBindings` and a single `AppSettings` row if absent; all queries use parameterised commands (no string interpolation)
- [ ] T014 Configure `src/Clipclaw/App.xaml.cs` — register all services as singletons in `Microsoft.Extensions.DependencyInjection`; call `persistence.InitialiseAsync()` on startup; store `ServiceProvider` as a static property for ViewModel access; catch and log unhandled exceptions with a user-visible error message before exit
- [ ] T015 Configure `src/Clipclaw/App.xaml` — set `ShutdownMode="OnExplicitShutdown"` so the app does not exit when windows are closed; apply Material Design 3 dark theme resource dictionaries from `MaterialDesignThemes`

**Checkpoint**: `dotnet build src/Clipclaw/Clipclaw.csproj` succeeds. App runs and starts without UI. Database file created at `%LOCALAPPDATA%\Clipclaw\clipclaw.db`.

---

## Phase 3: User Story 1 — Capture and Retrieve Clipboard Items (Priority: P1) 🎯 MVP

**Goal**: Every copied text item is silently stored. Any of the top 5 stored items
can be pasted into any app by pressing `Win + Shift + 1` through `Win + Shift + 5`
— without opening any panel.

**Independent Test**: Open Notepad. Copy five different words. Switch to another
app. Press `Win + Shift + 3`. Press `Ctrl + V`. The third-most-recently-copied
word should appear.

### Implementation for User Story 1

- [ ] T016 Create `src/Clipclaw/Infrastructure/WindowMessageHandler.cs` — wraps `HwndSource`; hooks into the WPF `MainWindow` HWND after the window is loaded; routes `WM_CLIPBOARDUPDATE` messages to a registered `Action` callback; exposes `Attach(Window)` and `Detach()` methods
- [ ] T017 [P] Create `src/Clipclaw/Services/IClipboardService.cs` — interface declaring: `StartMonitoring()`, `StopMonitoring()`, `GetHistory()` (returns ordered list), `GetItem(int index)`, `SetActiveClipboard(int index)` (writes item to Windows clipboard and increments use count)
- [ ] T018 Create `src/Clipclaw/Services/ClipboardService.cs` — implements `IClipboardService`; calls `WindowMessageHandler.Attach` on start; on `WM_CLIPBOARDUPDATE` reads `Clipboard.GetText()`, skips if empty or same as most-recent item (dedup rule FR-012), calls `IPersistenceService.UpsertClipItemAsync`; enforces `MaxHistorySize` by deleting oldest non-pinned items after every insert; `SetActiveClipboard` writes to `System.Windows.Clipboard`, then calls `IPersistenceService.IncrementPasteCountAsync`
- [ ] T019 [P] Create `src/Clipclaw/Services/IHotkeyService.cs` — interface declaring: `RegisterAsync(string actionName, string modifiers, string key)`, `UnregisterAll()`, `OnHotkeyPressed` event (passes `actionName`)
- [ ] T020 Create `src/Clipclaw/Services/HotkeyService.cs` — implements `IHotkeyService` using `NHotkey.Wpf.HotkeyManager`; reads bindings from `IPersistenceService`; validates each binding against `HotkeyConstants.ReservedShortcuts` before registering; raises `OnHotkeyPressed` event with the matching `ActionName`; `UnregisterAll` removes every binding cleanly on shutdown
- [ ] T021 Wire clipboard monitoring and hotkey registration in `src/Clipclaw/App.xaml.cs` — call `ClipboardService.StartMonitoring()` after `MainWindow` is shown (HWND must exist); register default paste-item hotkeys (`PasteItem_1` … `PasteItem_5`); subscribe to `HotkeyService.OnHotkeyPressed` and dispatch to `ClipboardService.SetActiveClipboard(index)`

**Checkpoint**: US1 is fully functional and testable independently. Copy 5 items, press `Win+Shift+1` through `Win+Shift+5`, paste in any app — correct items appear. No panel needed.

---

## Phase 4: User Story 2 — Tray Residence and Quick-Access Panel (Priority: P2)

**Goal**: App lives silently in the tray. `Win + Shift + V` opens a keyboard-navigable
panel showing recent clips. Arrow keys scroll. Enter pastes and closes. Escape
dismisses. The panel looks modern.

**Independent Test**: App is running (no taskbar entry). Press `Win + Shift + V`.
Panel appears within 300 ms. Press `↓` three times; `Enter`. The fourth item is
pasted into the previously focused app. Press the shortcut again; press `Escape`;
panel closes. Right-click tray icon; click Exit; no process remains.

### Implementation for User Story 2

- [ ] T022 Set up tray icon in `src/Clipclaw/App.xaml` using `<tb:TaskbarIcon>` from Hardcodet — icon source `Assets/Icons/tray.ico`, tooltip `"Clipclaw"`, context menu with items: **Open Clipclaw** (bound to `ShowPanelCommand`), **Settings…** (bound to `ShowSettingsCommand`), separator, **Exit** (bound to `ExitCommand`)
- [ ] T023 Create `Assets/Icons/tray.ico` — a simple 16×16 and 32×32 multi-resolution ICO file (placeholder; final art can be replaced later)
- [ ] T024 Create `src/Clipclaw/ViewModels/PanelViewModel.cs` (extends `ViewModelBase`) — properties: `ObservableCollection<ClipItem> PinnedItems`, `ObservableCollection<ClipItem> FrequentItems`, `ObservableCollection<ClipItem> RecentItems`, `ClipItem? SelectedItem`, `string SearchText`; commands: `PasteSelectedCommand`, `CloseCommand`; `LoadItemsAsync()` fetches all items from `IPersistenceService` and splits into the three collections per `data-model.md` section rules; `SearchText` setter refilters all three collections in real time
- [ ] T025 Create `src/Clipclaw/Views/ClipboardPanel.xaml` — floating `Window` with `WindowStyle="None"`, `AllowsTransparency="True"`, `ShowInTaskbar="False"`, width 360 px, max-height 560 px; layout: search `TextBox` at top (Material Design outlined style), then a scrollable `StackPanel` with three labeled sections (Pinned / Frequently Used / Recent) each backed by a `ListBox`; section header hidden when its collection is empty; each row shows truncated text (max 80 chars) and a right-aligned shortcut badge for items 1–5; Material Design card shadow and 8 px corner radius
- [ ] T026 Create `src/Clipclaw/Views/ClipboardPanel.xaml.cs` — `PreviewKeyDown` handler implementing the full keyboard contract from `contracts/keyboard-contract.md`: `Down`/`Up` move selection one item (wrapping across section boundaries), `PageDown`/`PageUp` jump one visible page, `Home`/`End` jump to first/last item across all sections, `Enter` calls `PasteSelectedCommand` and closes panel, `Escape` (when search empty) closes panel, `Escape` (when search has text) clears search, any printable character redirects focus to search `TextBox`; after every navigation call `BringIntoView()` on the selected item
- [ ] T027 Implement panel show/hide logic in `src/Clipclaw/App.xaml.cs` — `ShowPanel()`: call `PanelViewModel.LoadItemsAsync()`, position window near tray icon (bottom-right, inside screen bounds), set `Topmost = true`, show and `Activate()`; capture previously focused window HWND before showing; `HidePanel()`: hide window, restore focus to previously active window using `SetForegroundWindow` P/Invoke (declared in `WindowsClipboardInterop.cs`)
- [ ] T028 Apply panel open/close animation in `src/Clipclaw/Views/ClipboardPanel.xaml` — 150 ms `DoubleAnimation` on `Opacity` (0 → 1 open, 1 → 0 close) combined with a subtle `TranslateTransform` vertical slide (8 px) using `Storyboard`; navigation between rows MUST be instant (no per-row animation)
- [ ] T029 Wire `ShowPanel` hotkey and tray commands in `src/Clipclaw/App.xaml.cs` — subscribe to `HotkeyService.OnHotkeyPressed` for `HotkeyConstants.ShowPanel` action; bind tray `ShowPanelCommand` and `ExitCommand` to `RelayCommand` instances that call `ShowPanel()` / `Application.Current.Shutdown()`
- [ ] T030 Handle panel deactivation (click-outside-to-close) in `src/Clipclaw/Views/ClipboardPanel.xaml.cs` — subscribe to `Deactivated` event; call `HidePanel()` unless focus moved to `SettingsWindow`

**Checkpoint**: US1 + US2 both independently functional. Panel opens in < 300 ms, keyboard navigation works end-to-end per `contracts/keyboard-contract.md`, design matches `contracts/ui-panel-contract.md`.

---

## Phase 5: User Story 3 — Usage Tracking and Smart Recommendations (Priority: P3)

**Goal**: Items pasted 5+ times appear in "Frequently Used". Items can be pinned
and survive restarts. Real-time search filters all sections. Settings window
allows hotkey reconfiguration and preference changes.

**Independent Test**: Copy one item and paste it 10 times via the app (Enter in
panel). Copy a second item and paste it twice. Open panel — verify first item
appears in "Frequently Used" above second item. Pin the first item. Restart app.
Open panel — pinned item is still present in "Pinned" section.

### Implementation for User Story 3

- [ ] T031 Create `src/Clipclaw/Services/UsageTrackingService.cs` — called by `ClipboardService.SetActiveClipboard` after every paste; increments `PasteCount` and updates `LastPastedAt` via `IPersistenceService.IncrementPasteCountAsync`; raises a `UsageUpdated` event that `PanelViewModel` subscribes to for live section refresh
- [ ] T032 Update `src/Clipclaw/ViewModels/PanelViewModel.cs` to populate `FrequentItems` — items with `PasteCount >= 5` not already pinned, sorted by `PasteCount DESC`; subscribe to `UsageTrackingService.UsageUpdated` and refresh sections without closing the panel
- [ ] T033 [P] Implement pin/unpin in `src/Clipclaw/ViewModels/PanelViewModel.cs` — `PinCommand` and `UnpinCommand` toggle `ClipItem.IsPinned`, call `IPersistenceService.UpsertClipItemAsync`, then call `LoadItemsAsync()` to refresh all three sections
- [ ] T034 [P] Add `Ctrl+P` (pin/unpin) and `Delete` (delete item) keyboard handlers in `src/Clipclaw/Views/ClipboardPanel.xaml.cs` — wire to `PinCommand` and a new `DeleteSelectedCommand` on `PanelViewModel`
- [ ] T035 [P] Implement context menu on clip rows in `src/Clipclaw/Views/ClipboardPanel.xaml` — `ContextMenu` with items: **Pin** / **Unpin** (toggle based on state), **Delete**; triggered by `Application` key or `Shift + F10` per `contracts/keyboard-contract.md`
- [ ] T036 Implement `DeleteSelectedCommand` in `src/Clipclaw/ViewModels/PanelViewModel.cs` — calls `IPersistenceService.DeleteClipItemAsync` and removes item from the appropriate observable collection; if the deleted item was selected, move selection to the next item
- [ ] T037 Implement real-time search in `src/Clipclaw/ViewModels/PanelViewModel.cs` — `SearchText` property setter applies a case-insensitive `Contains` filter across all three source collections and repopulates `PinnedItems`, `FrequentItems`, `RecentItems`; when `SearchText` is empty all items are shown unfiltered
- [ ] T038 Create `src/Clipclaw/ViewModels/SettingsViewModel.cs` (extends `ViewModelBase`) — exposes `AppSettings`, `ObservableCollection<ShortcutBinding> Bindings`; `SaveSettingsCommand` validates `MaxHistorySize` range (10–500), calls `IPersistenceService.SaveSettingsAsync`; `RecordShortcutCommand(actionName)` enters recording mode; validates captured key combo against `HotkeyConstants.ReservedShortcuts` and existing bindings; shows `ConflictMessage` string property on collision; on valid combo saves via `IPersistenceService.SaveShortcutBindingAsync` then calls `HotkeyService.RegisterAsync`
- [ ] T039 Create `src/Clipclaw/Views/SettingsWindow.xaml` — standard `Window` (resizable, shows in taskbar), Material Design styling; three `TabItem` tabs: **General** (max-history `Slider` + numeric display, launch-on-startup `ToggleButton`, persist-history `ToggleButton`), **Shortcuts** (DataGrid listing all `ShortcutBinding` rows with inline key-recorder cells and red conflict warning text), **About** (app name, version, repo link)
- [ ] T040 Create `src/Clipclaw/Views/SettingsWindow.xaml.cs` — key recorder: on cell entry into recording mode, capture next `PreviewKeyDown` event, display the combo as human-readable text (e.g., `"Win + Shift + V"`), call `SettingsViewModel.RecordShortcutCommand`; show inline red `TextBlock` with `ConflictMessage` if conflict detected; do not save on conflict
- [ ] T041 Wire `ShowSettingsCommand` in `src/Clipclaw/App.xaml.cs` — `RelayCommand` that opens `SettingsWindow` as a non-modal window (user can still interact with tray); only one instance at a time

**Checkpoint**: All three user stories independently functional. Usage count drives section placement. Pinned items survive restart. Search filters live across all sections.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Production-readiness, Windows citizenship, spec acceptance criteria.

- [ ] T042 [P] Handle Windows session events in `src/Clipclaw/App.xaml.cs` — subscribe to `SystemEvents.SessionSwitch`; call `HotkeyService.UnregisterAll()` on `SessionLock` / `ConsoleDisconnect`; re-register on `SessionUnlock` / `ConsoleConnect`; close panel if open on any session event
- [ ] T043 [P] Implement launch-on-startup in `src/Clipclaw/Services/SqlitePersistenceService.cs` (or a dedicated `StartupService`) — when `LaunchOnStartup = true`, write app exe path to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Clipclaw`; when false, delete the registry key; call on every `SaveSettingsAsync` where `LaunchOnStartup` changes
- [ ] T044 [P] Implement history persistence toggle in `src/Clipclaw/App.xaml.cs` — in the `Exit` handler, if `AppSettings.PersistHistory = false` call `IPersistenceService.ClearNonPinnedAsync()` before shutdown
- [ ] T045 [P] Add `RemoveClipboardFormatListener` and `HotkeyService.UnregisterAll()` calls to the app `Exit` handler in `src/Clipclaw/App.xaml.cs` — ensures clean Windows resource release (Windows-Native Reliability principle)
- [ ] T046 [P] Readability audit across all source files — verify every function is ≤ 40 lines; every Win32 constant is in `HotkeyConstants.cs`; every `ActionName` string comes from `HotkeyConstants`; no abbreviations in identifiers except `id`, `ui`, `url`; all comments explain *why* not *what*
- [ ] T047 Run `quickstart.md` validation — follow every step in `specs/001-clipboard-manager/quickstart.md`; exercise all six acceptance scenarios from `spec.md`; confirm SC-001 through SC-008 are met; fix any failures before considering the feature done

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — **blocks all user stories**
- **US1 (Phase 3)**: Depends on Phase 2 — no dependency on US2/US3
- **US2 (Phase 4)**: Depends on Phase 2 — no dependency on US1 (panel uses `IPersistenceService` directly)
- **US3 (Phase 5)**: Depends on Phase 2, US1 (paste counting), and US2 (panel sections)
- **Polish (Phase 6)**: Depends on all user story phases

### User Story Dependencies

- **US1 (P1)**: Foundation complete → clipboard monitor + hotkey paste → done independently
- **US2 (P2)**: Foundation complete → tray + panel + keyboard nav → done independently
- **US3 (P3)**: US1 (paste count exists) + US2 (panel sections exist) → usage tracking + pin + search + settings

### Within Each Phase

- Tasks without [P] must complete before the next sequential task
- Tasks marked [P] within the same phase can be parallelised
- Models before services; services before ViewModels; ViewModels before Views

---

## Parallel Examples

### Phase 2 Foundational — parallelisable group

```
Parallel group A (T006, T007, T009, T010, T011):
  T006: RelayCommand.cs
  T007: ViewModelBase.cs
  T009: ClipItem.cs
  T010: AppSettings.cs
  T011: ShortcutBinding.cs
```

### Phase 3 US1 — parallelisable group

```
Parallel group B (T017, T019):
  T017: IClipboardService.cs
  T019: IHotkeyService.cs
```

### Phase 5 US3 — parallelisable group

```
Parallel group C (T033, T034, T035):
  T033: PinCommand / UnpinCommand in PanelViewModel
  T034: Ctrl+P / Delete keyboard handlers in ClipboardPanel.xaml.cs
  T035: Row context menu in ClipboardPanel.xaml
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: US1 (clipboard capture + hotkey paste — **no panel needed**)
4. **STOP and VALIDATE**: Copy 5 items, paste any via `Win+Shift+1`–`5`
5. Demo / validate SC-001, SC-002 are met

### Incremental Delivery

1. Foundation → US1 → **Validate**: Silent clipboard manager working
2. Add US2 (tray + panel) → **Validate**: Panel opens, keyboard nav works, looks modern
3. Add US3 (usage + pin + search + settings) → **Validate**: Full feature complete

---

## Notes

- `[P]` = different files, no incomplete dependencies — safe to parallelise
- `[US1]`/`[US2]`/`[US3]` labels trace every task to its user story
- Each user story phase ends with an explicit checkpoint that tests the story independently
- Win32 P/Invoke MUST all live in `Infrastructure/WindowsClipboardInterop.cs` — nothing else imports user32.dll directly
- `HotkeyConstants.cs` is the single source of truth for all string constants — never use string literals for action names outside this file
- Commit after each completed task or logical group; include task ID in commit message (e.g., `feat: T018 implement clipboard monitoring service`)
