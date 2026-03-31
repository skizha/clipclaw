# Research: Clipboard Panel UX Improvements

**Branch**: `002-clip-panel-ux` | **Date**: 2026-03-30
**Input**: Feature spec + existing codebase analysis

---

## Decision 1: Runtime Theme Switching (MaterialDesignThemes.Wpf)

**Decision**: Use `PaletteHelper.SetTheme()` from the MaterialDesignThemes.Wpf
package to switch between Light and Dark bases at runtime.

**Rationale**: The existing project already depends on MaterialDesignThemes.Wpf.
The package exposes a supported runtime API:

```csharp
var helper = new PaletteHelper();
ITheme theme = helper.GetTheme();
theme.SetBaseTheme(isDark ? Theme.Dark : Theme.Light);
helper.SetTheme(theme);
```

This updates the live `Application.Current.Resources` merge, meaning all open
windows pick up the change immediately — satisfying FR-014 (no restart required).

**Alternatives considered**:
- Swapping merged ResourceDictionaries manually at runtime → works but brittle and
  requires tracking dictionary references.
- Restart-to-apply approach → rejected (FR-014 explicitly requires live switching).

---

## Decision 2: SQLite Additive Migration

**Decision**: Extend the existing schema using `ALTER TABLE … ADD COLUMN` guards.

For `ClipItems`:
```sql
ALTER TABLE ClipItems ADD COLUMN ShortName TEXT NULL;
```

For `AppSettings`:
```sql
ALTER TABLE AppSettings ADD COLUMN Theme TEXT NOT NULL DEFAULT 'Dark';
```

Guards (try/catch or PRAGMA `table_info`) prevent errors if the columns already
exist (e.g., running `InitialiseAsync` on an updated database).

**Rationale**: Additive columns with defaults are fully backward-compatible with
existing data — satisfies constitution Principle VII (Non-Regression Guarantee).
No data is lost or altered during migration.

**Alternatives considered**:
- Schema versioning table + explicit migration scripts → over-engineering for two
  simple column additions on a single-user local database.
- Drop-and-recreate table → destructive; loses all user clip history. Rejected.

---

## Decision 3: Keyboard Triggers for Edit and Delete

**Decision**:
- **Edit item**: `F2` key (standard Windows edit/rename shortcut — File Explorer,
  VS, Word).
- **Delete item**: `Delete` key (already wired in code-behind) + add confirmation
  dialog before executing the DeleteCommand.
- **Confirmation dialog**: Standard `MessageBox.Show(…, MessageBoxButton.YesNo)`
  — keyboard-navigable (Y/N/Enter/Escape), no custom dialog needed.

**Rationale**: F2 is the platform-standard "enter rename mode" shortcut. Users
already know it from Windows. Delete is already wired; adding the confirmation
guard is a single insertion in the code-behind handler. MessageBox is modal,
keyboard-navigable, and requires zero new XAML.

**Alternatives considered**:
- Inline edit in the list row (contenteditable cell) → complex selection state
  management; conflicts with arrow-key navigation while typing.
- Dedicated Edit button in each row → adds UI noise; mouse-biased design.
- Custom confirmation dialog XAML → unnecessary complexity; MessageBox already
  satisfies the spec's requirements.

---

## Decision 4: Alternating Row Colors

**Decision**: Set `AlternationCount="2"` on each section `ListBox` and bind
`ItemsControl.AlternationIndex` in the `ClipRow` ListBoxItem style to apply
distinct even/odd row backgrounds.

**Rationale**: WPF `AlternationCount` is the idiomatic way to achieve zebra
striping. The codebase already uses `AlternationIndex` via `IndexToBadgeConverter`
for the shortcut badge (indices 0–4), so the infrastructure is already understood.
Extending the item style to also switch background color is a minimal, additive
change.

**Alternatives considered**:
- Border separator between rows → simpler but less visually clear on dense lists.
- Custom `ItemContainerStyleSelector` → more code for the same result; rejected
  in favor of the built-in `AlternationCount` mechanism.

---

## Decision 5: Copy Count Display

**Decision**: Display the existing `PasteCount` field in every panel row (not just
the Frequent section). Add a new `CopyCountDisplayConverter` that:
- Returns the count as a string for values 0–999
- Returns `"999+"` for values ≥ 1000

Show the count as a small badge/label on the right side of each row (consistent
with the existing shortcut badge position in the Recent section).

**Rationale**: `PasteCount` already exists in the model and database. The feature
only requires surfacing it in all rows and adding the overflow cap. No schema
change needed for this story.

**Alternatives considered**:
- Adding a separate `CopyCount` field distinct from `PasteCount` → duplicate data
  for no benefit; `PasteCount` already tracks in-app copies.

---

## Decision 6: Short Name Display in Panel Row

**Decision**: Each panel row displays:
- **Primary line**: `ShortName` if set; otherwise `DisplayText` (existing 80-char
  truncation).
- **Secondary line** (shown when ShortName is set): `DisplayText` in smaller,
  muted text for context.

Row height increases from 44px to ~56px only for items that have a short name
(to accommodate the second line). Items without a short name remain 44px.

**Rationale**: Keeps the panel compact for unlabelled items while giving labelled
items enough space to show both the name and a text preview.

**Alternatives considered**:
- Fixed row height for all items (always show two lines) → wastes space for the
  majority of items that have no short name.
- Tooltip-only for full text → inaccessible (requires hover, breaks keyboard flow).

---

## Decision 7: Auto-Focus on Panel Open

**Decision**: When the clipboard panel opens, the `ListBox` selection (or focus)
defaults to the first item in the list. Subsequent ↓ presses move to item 2, 3,
etc. If the list is empty, no item is selected.

**Rationale**: The spec requires that pressing ↓ immediately moves to the first
item (US1-AS4). Auto-selecting item 1 on open means the user can press ↓ once
and Enter to copy item 2, or simply press Enter to copy item 1 — the most
common case. The existing `SelectFirst()` method in `PanelViewModel` is already
implemented; it just needs to be called on open.

**Alternatives considered**:
- No initial selection (user must press ↓ first) → requires one extra keypress
  to copy the first (most recent) item; inconvenient.
