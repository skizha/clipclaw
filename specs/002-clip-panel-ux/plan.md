# Implementation Plan: Clipboard Panel UX Improvements

**Branch**: `002-clip-panel-ux` | **Date**: 2026-03-30 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/002-clip-panel-ux/spec.md`

---

## Summary

Six UX improvements to the Clipclaw clipboard panel:

1. **Keyboard Navigation + Enter-to-Copy** (P1) — panel opens with first item
   pre-selected; ↑/↓ navigate the list; Enter copies the focused item and closes
   the panel.
2. **Copy Counter** (P2) — display the existing `PasteCount` in every panel row
   with a "999+" cap.
3. **Item Editing + Short Name** (P2) — F2 opens an edit dialog; users can set a
   short label and edit full text; label shown as primary row identifier.
4. **Delete with Confirmation** (P2) — Delete key triggers a confirmation dialog;
   no item is removed without explicit Yes.
5. **Alternating Row Colors** (P3) — `AlternationCount="2"` on list boxes; even/odd
   rows get distinct backgrounds; visible in both themes.
6. **Light Theme** (P3) — `PaletteHelper.SetTheme()` switches MaterialDesign base
   at runtime; Theme preference stored in `AppSettings`; configurable in Settings.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8 LTS
**Primary Dependencies**: WPF, MaterialDesignThemes.Wpf (Material Design 3),
  NHotkey.Wpf, Microsoft.Data.Sqlite, Microsoft.Extensions.DependencyInjection
**Storage**: SQLite via `Microsoft.Data.Sqlite`; database at
  `%LOCALAPPDATA%\Clipclaw\clipclaw.db`
**Testing**: xUnit (scaffolded; tests optional per constitution)
**Target Platform**: Windows 10/11 desktop
**Project Type**: WPF desktop application (background tray + popup panel)
**Performance Goals**: Panel open-to-interactive < 200ms; theme switch < 100ms
**Constraints**: No clipboard interference; negligible idle CPU/memory; additive
  schema migrations only
**Scale/Scope**: Single-user local app; up to 500 clipboard items in history

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec-First | ✅ PASS | `spec.md` exists, no unresolved markers |
| II. User-Story Orientation | ✅ PASS | 6 stories (P1–P3), all traceable |
| III. Keyboard-First | ✅ PASS | F2=edit, Delete=delete, ↑↓=nav, Enter=copy; all keyboard-reachable |
| IV. Readable Code | ✅ PASS | New constants for F2/action names; no magic literals planned |
| V. Simplicity & YAGNI | ✅ PASS | Extending existing model; MessageBox for confirmation; no new abstractions |
| VI. Windows-Native Reliability | ✅ PASS | Schema changes additive; no clipboard interference |
| VII. Non-Regression Guarantee | ✅ PASS | Existing PasteCount, hotkeys, and panel behaviour unchanged |

**No violations → Complexity Tracking table not required.**

---

## Project Structure

### Documentation (this feature)

```text
specs/002-clip-panel-ux/
├── plan.md              # This file
├── research.md          # Phase 0 — design decisions
├── data-model.md        # Phase 1 — schema + entity changes
├── quickstart.md        # Phase 1 — manual test guide
├── contracts/
│   └── keyboard-contracts.md  # Phase 1 — keyboard shortcut contracts
└── tasks.md             # Phase 2 output (/speckit.tasks — not yet created)
```

### Source Code (existing structure, files to change)

```text
src/Clipclaw/
├── Models/
│   ├── ClipItem.cs           # + ShortName field + DisplayLabel/HasShortName props
│   └── AppSettings.cs        # + Theme field (ClipTheme enum)
│
├── Services/
│   └── SqlitePersistenceService.cs
│       # + ALTER TABLE migrations in InitialiseAsync
│       # + ShortName in Upsert/Select for ClipItems
│       # + Theme in Save/Get for AppSettings
│
├── ViewModels/
│   ├── PanelViewModel.cs
│   │   # + ensure SelectFirst() called on load
│   │   # + DeleteCommand now triggers confirmation (or moves confirm to code-behind)
│   └── SettingsViewModel.cs
│       # + Theme property bound to new ComboBox
│       # + call PaletteHelper.SetTheme() on save
│
├── Views/
│   ├── ClipboardPanel.xaml
│   │   # + AlternationCount="2" on ListBoxes
│   │   # + AlternationIndex-based background in ClipRow style
│   │   # + ShortName primary line + DisplayText secondary line in row template
│   │   # + CopyCount badge visible in all rows (not just Frequent)
│   ├── ClipboardPanel.xaml.cs
│   │   # + F2 handler → open EditClipDialog
│   │   # + Delete handler → MessageBox confirmation before DeleteCommand
│   │   # + auto-select first item on open (call SelectFirst)
│   ├── EditClipDialog.xaml        # NEW — edit dialog (ShortName + Text fields)
│   ├── EditClipDialog.xaml.cs     # NEW — code-behind (Escape/Enter/Save)
│   └── SettingsWindow.xaml
│       # + Theme ComboBox in General tab
│
└── Infrastructure/
    ├── HotkeyConstants.cs     # + EditItem action name constant
    ├── Converters.cs          # + CopyCountDisplayConverter (999+ cap)
    └── AppResources.xaml      # + register CopyCountDisplayConverter
```

**Structure Decision**: Single WPF project with no structural changes to the
layered MVVM layout. All new files are within existing layer directories.

---

## Complexity Tracking

> No constitution violations — table not applicable.
