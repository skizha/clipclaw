# Feature Specification: Clipboard Panel UX Improvements

**Feature Branch**: `002-clip-panel-ux`
**Created**: 2026-03-30
**Status**: Draft
**Input**: User description — keyboard navigation, copy counter, item editing/naming,
deletion with confirmation, visual row differentiation, light theme

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Keyboard Navigation & Enter-to-Copy (Priority: P1)

A user opens the clipboard panel and uses the up/down arrow keys to browse stored
clips. When they press Enter on the highlighted item, that item is copied to the
system clipboard and pasted into the active window (consistent with existing
behaviour).

**Why this priority**: This is a direct extension of the app's core promise —
keyboard-only access. Without it, selecting an item from the panel requires a mouse
click, breaking the keyboard-first principle.

**Independent Test**: Open the panel, press ↓ twice, press Enter — confirm the
third item is now on the clipboard.

**Acceptance Scenarios**:

1. **Given** the panel is open with at least two items, **When** the user presses
   ↓, **Then** the second item becomes highlighted with a visible focus indicator.
2. **Given** an item is highlighted, **When** the user presses Enter, **Then**
   that item's full text is written to the system clipboard and the panel closes.
3. **Given** the panel is open and focus is on the first item, **When** the user
   presses ↑, **Then** focus stays on the first item (no wrap-around).
4. **Given** no item is highlighted (panel just opened), **When** the user presses
   ↓, **Then** the first item receives focus.
5. **Given** the panel is open with an empty list, **When** the user presses ↓ or
   Enter, **Then** nothing happens and an empty-state message is visible.

---

### User Story 2 — Copy Counter per Item (Priority: P2)

Each stored clip item tracks how many times it has been copied via the app. The
count is visible in the panel row and persists across restarts.

**Why this priority**: Helps users identify their most-used clips, enabling them
to prioritise and clean up their history.

**Independent Test**: Copy the same item three times via the app; confirm the
counter beside that item reads "3" (or equivalent).

**Acceptance Scenarios**:

1. **Given** a clip item with no prior copies, **When** the user copies it via any
   in-app mechanism (keyboard shortcut, Enter-to-copy), **Then** the copy count
   increments to 1 and is visible in the row.
2. **Given** a clip item has a non-zero count, **When** the panel is closed and
   reopened, **Then** the persisted count is still shown correctly.
3. **Given** a new clip item is captured from the system clipboard, **Then** its
   copy count starts at 0.
4. **Given** a count reaches a large number (e.g., 1000), **Then** it displays as
   a capped label (e.g., "999+") to avoid layout overflow.

---

### User Story 3 — Item Editing & Short Name (Priority: P2)

A user can assign an optional short human-readable name (label) to any stored clip
and can edit the full text of the clip. The short name appears as the primary
identifier in the panel row.

**Why this priority**: Long items (URLs, code snippets, tokens) are unrecognisable
from their first characters alone. A short name restores instant identification.

**Independent Test**: Assign the name "My API Key" to a clip containing a long
token; reopen the panel and confirm "My API Key" is the displayed row heading.

**Acceptance Scenarios**:

1. **Given** a clip item, **When** the user opens its edit view and enters a short
   name, **Then** the panel row displays the short name as the primary identifier.
2. **Given** a clip with a short name, **When** the user edits the full text and
   saves, **Then** the updated text is used on the next copy action.
3. **Given** a clip has no short name, **Then** the panel row shows a truncated
   preview of the full text as a fallback (existing behaviour).
4. **Given** the user is in the edit view, **When** they press Escape, **Then**
   all changes are discarded and the panel returns to the list.

---

### User Story 4 — Item Deletion with Confirmation (Priority: P2)

A user can delete any stored clip item. The app requires explicit confirmation
before the item is permanently removed.

**Why this priority**: Accidental deletion of a curated item is irreversible; a
confirmation step prevents user frustration.

**Independent Test**: Trigger deletion on any item, dismiss confirmation — item
remains. Trigger again and confirm — item is gone.

**Acceptance Scenarios**:

1. **Given** a clip item, **When** the user triggers the delete action (keyboard
   shortcut or button), **Then** a confirmation prompt appears: "Delete this item?"
   with Yes and No options.
2. **Given** the confirmation is shown, **When** the user selects No or presses
   Escape, **Then** the item is NOT deleted and the list is unchanged.
3. **Given** the confirmation is shown, **When** the user selects Yes or presses
   Enter, **Then** the item is permanently removed from the list and storage.
4. **Given** the deleted item was focused, **When** deletion completes, **Then**
   focus moves to the nearest remaining item (or empty-state if none remain).

---

### User Story 5 — Visual Row Differentiation (Priority: P3)

Adjacent clip rows in the panel are visually distinguishable — either by alternating
background shading or by a clear separator line between rows.

**Why this priority**: Scanning identical rows causes visual fatigue; row
differentiation is low-effort, high-benefit polish.

**Independent Test**: Open the panel with five or more items; confirm rows are
visually distinct without relying on text content differences.

**Acceptance Scenarios**:

1. **Given** the panel is open with multiple items, **Then** adjacent rows have
   visually different backgrounds or a visible separator between them.
2. **Given** an item is highlighted (keyboard focus), **Then** its row uses a
   distinct selection colour that overrides the alternating background.
3. **Given** the panel is displayed in either the dark or light theme, **Then**
   row differentiation remains visible in both.

---

### User Story 6 — Light Theme (Priority: P3)

The application offers a Light colour theme as an alternative to the existing Dark
theme. Users choose their preferred theme in Settings, and the choice persists.

**Why this priority**: Some users prefer light themes; this broadens the app's
comfort for everyday use in varying lighting environments.

**Independent Test**: Switch to Light theme in Settings, reopen the panel — confirm
all text is dark on a light background with no illegible combinations.

**Acceptance Scenarios**:

1. **Given** the user opens Settings, **Then** a Theme option offers "Light" and
   "Dark" choices.
2. **Given** the user selects Light theme, **Then** the clipboard panel, settings
   window, and any dialogs adopt light-background styling immediately (no restart
   required).
3. **Given** light theme is active, **Then** all text, icons, and interactive
   elements meet minimum readable contrast (no washed-out or invisible elements).
4. **Given** the user switches from Light back to Dark, **Then** the dark styling
   is restored immediately.
5. **Given** the app is restarted after a theme change, **Then** the previously
   selected theme is restored.

---

### Edge Cases

- **Empty list + keyboard**: Arrow and Enter keys are no-ops; an empty-state
  message is shown.
- **Short name overflow**: A name exceeding the row display width is truncated with
  an ellipsis.
- **Last item deleted**: After deletion the empty-state message is shown.
- **Duplicate text items**: Two items with identical text are treated as independent
  items — each has its own ID, count, and short name.
- **Copy count display cap**: Counts above 999 display as "999+" to prevent layout
  distortion.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The clipboard panel MUST support ↑/↓ arrow-key navigation to move
  keyboard focus between items.
- **FR-002**: Pressing Enter on a focused item MUST copy its full text to the
  system clipboard and close the panel.
- **FR-003**: Each clip item MUST maintain a persistent copy count that increments
  on every in-app copy action for that item.
- **FR-004**: The copy count for each item MUST be visible in its panel row.
- **FR-005**: Users MUST be able to assign an optional short name (label) to any
  clip item.
- **FR-006**: Users MUST be able to edit the full text content of any stored clip
  item.
- **FR-007**: When a clip item has a short name, the panel row MUST display the
  short name as its primary identifier.
- **FR-008**: Users MUST be able to delete any clip item via a keyboard-accessible
  action.
- **FR-009**: The system MUST display a confirmation prompt before permanently
  deleting a clip item; deletion MUST NOT proceed without explicit user confirmation.
- **FR-010**: Adjacent rows in the clipboard panel MUST be visually distinguishable
  (alternating shading or visible separators).
- **FR-011**: The application MUST provide a Light colour theme alongside the
  existing Dark theme.
- **FR-012**: Theme selection MUST be persisted across application restarts.
- **FR-013**: Theme selection MUST be configurable from the Settings window.
- **FR-014**: Theme changes MUST apply to all application windows without requiring
  a restart.

### Key Entities

- **ClipItem** (existing — extend): Text content, timestamp, usage tracking. Add:
  `ShortName` (optional string, max 60 chars) and `CopyCount` (integer, default 0).
- **AppSettings** (existing — extend): Add `Theme` preference (Dark | Light,
  default Dark).

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can navigate to any item in a 10-item list and copy it using
  only the keyboard in 5 or fewer keystrokes from panel open.
- **SC-002**: Copy counts for all items persist correctly across application
  restarts in 100% of cases.
- **SC-003**: A user can assign a short name, edit clip text, and delete a clip
  entirely via keyboard — no mouse required for any of these actions.
- **SC-004**: Deletion confirmation prevents accidental removal: No item is deleted
  without an explicit "Yes" response to the confirmation prompt.
- **SC-005**: Row differentiation is visible in both themes at 100% DPI: no two
  adjacent rows appear identical.
- **SC-006**: Light theme applies across all windows with no text or icon rendered
  illegible against its background.

---

## Assumptions

- Arrow-key focus stops at the top and bottom of the list; it does NOT wrap around.
- The deletion confirmation dialog is modal — it blocks list interaction until
  resolved.
- Short names are free-form text (max ~60 chars); uniqueness is not required.
- Editing a clip item does not reset its `CopyCount` or `ShortName`.
- The light theme applies to all application windows: panel, settings, dialogs.
- Copy count increments only for copies initiated through the app, not for
  clipboard changes captured passively from external applications.
- The existing Dark theme remains the default; users opt into Light explicitly.
- The edit view for a clip item is accessed via a dedicated keyboard shortcut or
  an Edit button visible in the panel row (exact trigger defined at planning stage).
- The delete action for a clip item is accessible via a dedicated keyboard shortcut
  or a Delete button visible in the panel row (exact trigger defined at planning stage).
