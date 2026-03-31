# Data Model: Clipboard Panel UX Improvements

**Branch**: `002-clip-panel-ux` | **Date**: 2026-03-30

---

## Entity Changes

### ClipItem (extend existing)

**New field**: `ShortName`

| Field         | Type    | Nullable | Default | Constraint        |
|---------------|---------|----------|---------|-------------------|
| ShortName     | string  | yes      | null    | max 60 characters |

**All existing fields unchanged:**
`Id`, `Text`, `CopiedAt`, `LastPastedAt`, `PasteCount`, `IsPinned`, `DisplayOrder`

**New computed property**:
```
DisplayLabel → ShortName if non-null/non-empty, otherwise DisplayText (truncated)
HasShortName → ShortName != null && ShortName.Length > 0
```

No new persistence method needed — existing `UpsertClipItemAsync()` handles
INSERT OR REPLACE; the ShortName column will be included in the upsert.

---

### AppSettings (extend existing)

**New field**: `Theme`

| Field  | Type   | Nullable | Default | Values        |
|--------|--------|----------|---------|---------------|
| Theme  | string | no       | "Dark"  | "Dark"/"Light"|

**All existing fields unchanged:**
`Id`, `MaxHistorySize`, `LaunchOnStartup`, `PersistHistory`, `PanelShortcut`

Stored as a string in SQLite; parsed to a `ClipTheme` enum in the C# model.

---

## New Enum

### ClipTheme

```
Dark  — existing dark theme (default)
Light — new light theme
```

Used in `AppSettings.Theme` to drive `PaletteHelper.SetTheme()` on app startup
and on settings save.

---

## SQLite Schema Migrations

Both migrations are additive (no existing data altered or removed).
Run during `InitialiseAsync()` using try/catch to guard idempotency:

```sql
-- Migration: add ShortName to ClipItems
ALTER TABLE ClipItems ADD COLUMN ShortName TEXT NULL;

-- Migration: add Theme to AppSettings
ALTER TABLE AppSettings ADD COLUMN Theme TEXT NOT NULL DEFAULT 'Dark';
```

After migration, the `AppSettings` row (Id=1) will have `Theme = 'Dark'`
for all existing installations, preserving the current user experience.

---

## Persistence Service Changes

### IPersistenceService (interface — no signature changes)

The existing `UpsertClipItemAsync(ClipItem)` and `SaveSettingsAsync(AppSettings)`
signatures are unchanged. Both methods will transparently persist the new fields
because they write all model fields via parameterized SQL.

### SqlitePersistenceService (implementation changes)

1. `InitialiseAsync()` — add the two `ALTER TABLE` migration statements
   (guarded with try/catch on `SqliteException`).
2. `UpsertClipItemAsync()` — include `ShortName` in the INSERT OR REPLACE
   parameter list.
3. `GetAllClipItemsAsync()` — map `ShortName` column in the SELECT reader.
4. `SaveSettingsAsync()` — include `Theme` in the UPDATE parameter.
5. `GetSettingsAsync()` — map `Theme` column in the SELECT reader;
   parse string to `ClipTheme` enum (default to `Dark` on unrecognised value).

---

## State Transitions

### Item Edit Flow

```
List view (item focused)
  → F2 pressed
  → EditClipDialog opens (modal)
      fields: ShortName (TextBox), Text (TextBox multiline)
      actions: Save (Enter), Cancel (Escape)
  → On Save: UpsertClipItemAsync(updated item)
  → Return to list view, same item focused
```

### Item Delete Flow

```
List view (item focused)
  → Delete key pressed
  → MessageBox "Delete this item?" [Yes] [No]
      → No / Escape: dismiss, return to list, item unchanged
      → Yes / Enter: DeleteClipItemAsync(item.Id)
                     focus moves to nearest remaining item
                     (or empty-state message if list is now empty)
```

### Theme Change Flow

```
Settings window open
  → User changes Theme ComboBox selection
  → Click Save Settings
  → SaveSettingsAsync(settings)
  → PaletteHelper.SetTheme(…) called immediately
  → All open windows update live (no restart)
  → Theme persisted; restored on next app start
```
