# Data Model: Clipboard Manager

**Branch**: `001-clipboard-manager` | **Date**: 2026-03-29
**Source**: `spec.md` entities + research decisions

---

## Entities

### ClipItem

Represents one captured piece of text. The central entity of the app.

| Field         | Type       | Constraints                                  | Notes                                          |
|---------------|------------|----------------------------------------------|------------------------------------------------|
| `Id`          | integer    | Primary key, auto-increment                  |                                                |
| `Text`        | string     | NOT NULL, UNIQUE, max 100 000 chars          | The captured clipboard text                    |
| `CopiedAt`    | datetime   | NOT NULL                                     | UTC timestamp of the most recent copy event    |
| `LastPastedAt`| datetime   | nullable                                     | UTC timestamp of the most recent paste via app |
| `PasteCount`  | integer    | NOT NULL, default 0, ≥ 0                    | Incremented each time user pastes via Clipclaw |
| `IsPinned`    | boolean    | NOT NULL, default false                      | Pinned items survive history truncation        |
| `DisplayOrder`| integer    | NOT NULL, default 0                          | Explicit ordering for pinned items             |

**Business rules**:
- A ClipItem is uniquely identified by its `Text` value. Copying the same text again
  updates `CopiedAt` and moves the item to the top of the history; it does NOT create
  a second entry (FR-012).
- When `PasteCount ≥ 5`, the item qualifies for the "Frequently Used" section (FR-005,
  SC-004).
- Items with `IsPinned = true` are never removed by the history-size limit (FR-006).
- Non-pinned items beyond the configured `MaxHistorySize` are deleted oldest-first by
  `CopiedAt` (FR-010).

**State transitions**:

```
[Copied]
   │  text does not exist → INSERT new ClipItem (PasteCount=0, IsPinned=false)
   │  text already exists → UPDATE CopiedAt, keep PasteCount and IsPinned
   ▼
[In History]
   │  user pastes via app → PasteCount++, LastPastedAt = now
   │  user pins           → IsPinned = true
   │  user unpins         → IsPinned = false
   │  user deletes        → DELETE row
   │  history limit hit   → DELETE oldest non-pinned (if IsPinned = false)
   ▼
[Removed]
```

---

### AppSettings

Singleton row — one record in the database holds all user preferences.

| Field              | Type    | Default  | Constraints            | Notes                                         |
|--------------------|---------|----------|------------------------|-----------------------------------------------|
| `Id`               | integer | 1        | Primary key (always 1) |                                               |
| `MaxHistorySize`   | integer | 50       | 10 – 500 inclusive     | FR-010                                        |
| `LaunchOnStartup`  | boolean | true     |                        | FR-009                                        |
| `PersistHistory`   | boolean | true     |                        | If false, history is cleared on app exit      |
| `PanelShortcut`    | string  | "Win+Shift+V" | NOT NULL          | Serialised key combination for the show/hide panel hotkey (FR-004, FR-008) |

**Business rules**:
- `MaxHistorySize` change triggers an immediate trim of non-pinned history if current
  count exceeds the new limit.
- `PanelShortcut` must not match any reserved Windows system shortcut; the app
  validates on save and rejects conflicts (FR-008).

---

### ShortcutBinding

Defines the mapping from a named action to a key combination. Stored as multiple
rows, one per action.

| Field          | Type   | Constraints               | Notes                                           |
|----------------|--------|---------------------------|-------------------------------------------------|
| `Id`           | integer| Primary key               |                                                 |
| `ActionName`   | string | NOT NULL, UNIQUE          | e.g., `"ShowPanel"`, `"PasteItem_1"` … `"PasteItem_9"` |
| `Modifiers`    | string | NOT NULL                  | Serialised modifier flags, e.g., `"Win+Shift"` |
| `Key`          | string | NOT NULL                  | Key name, e.g., `"V"`, `"1"`, `"2"`            |
| `IsEnabled`    | boolean| NOT NULL, default true    | Allows user to disable individual shortcuts     |

**Business rules**:
- `ActionName` values are defined in code as named constants (no magic strings in
  business logic — Readable Code principle).
- Before saving a binding, the app checks all other enabled bindings for key
  combination conflicts (FR-008).
- Default bindings seeded on first run:

  | ActionName      | Default Binding  |
  |-----------------|------------------|
  | `ShowPanel`     | Win + Shift + V  |
  | `PasteItem_1`   | Win + Shift + 1  |
  | `PasteItem_2`   | Win + Shift + 2  |
  | `PasteItem_3`   | Win + Shift + 3  |
  | `PasteItem_4`   | Win + Shift + 4  |
  | `PasteItem_5`   | Win + Shift + 5  |

---

## Persistence Schema (SQLite)

```sql
CREATE TABLE IF NOT EXISTS ClipItems (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Text         TEXT    NOT NULL UNIQUE,
    CopiedAt     TEXT    NOT NULL,   -- ISO 8601 UTC
    LastPastedAt TEXT,               -- ISO 8601 UTC, nullable
    PasteCount   INTEGER NOT NULL DEFAULT 0,
    IsPinned     INTEGER NOT NULL DEFAULT 0,  -- 0 = false, 1 = true
    DisplayOrder INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS AppSettings (
    Id              INTEGER PRIMARY KEY DEFAULT 1,
    MaxHistorySize  INTEGER NOT NULL DEFAULT 50,
    LaunchOnStartup INTEGER NOT NULL DEFAULT 1,
    PersistHistory  INTEGER NOT NULL DEFAULT 1,
    PanelShortcut   TEXT    NOT NULL DEFAULT 'Win+Shift+V'
);

CREATE TABLE IF NOT EXISTS ShortcutBindings (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    ActionName  TEXT    NOT NULL UNIQUE,
    Modifiers   TEXT    NOT NULL,
    Key         TEXT    NOT NULL,
    IsEnabled   INTEGER NOT NULL DEFAULT 1
);

-- Indexes for common query patterns
CREATE INDEX IF NOT EXISTS idx_clipitems_copiedat
    ON ClipItems (CopiedAt DESC);

CREATE INDEX IF NOT EXISTS idx_clipitems_pastecount
    ON ClipItems (PasteCount DESC);
```

---

## In-Memory View Models

These are not persisted; they are derived from the database for the UI layer.

### PanelItem (ViewModel projection of ClipItem)

Used to populate the clipboard panel list. Adds display-time derived fields.

| Field            | Derived From                         | Notes                              |
|------------------|--------------------------------------|------------------------------------|
| `DisplayText`    | `ClipItem.Text` (truncated to 80ch)  | Shown in panel row                 |
| `FullText`       | `ClipItem.Text`                      | Used on paste                      |
| `IsFrequent`     | `PasteCount >= 5`                    | Drives "Frequently Used" section   |
| `IsPinned`       | `ClipItem.IsPinned`                  | Drives pinned section              |
| `ShortcutLabel`  | Derived from `ShortcutBindings`      | e.g., "⊞ Shift 1" shown in panel  |

### PanelSection

The panel groups PanelItems into sections in this fixed order:

1. **Pinned** — `IsPinned = true`, ordered by `DisplayOrder`
2. **Frequently Used** — `IsPinned = false AND IsFrequent = true`, ordered by
   `PasteCount DESC`
3. **Recent** — all remaining items, ordered by `CopiedAt DESC`
