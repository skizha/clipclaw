# Keyboard Contract: Clipboard Manager

**Branch**: `001-clipboard-manager` | **Date**: 2026-03-29

This document defines every keyboard interaction the app exposes. It is the
authoritative source for all shortcut assignments. No key literal may appear in
application code without a corresponding entry here (Readable Code principle:
no magic key constants).

---

## Global Shortcuts (Active Everywhere)

These shortcuts work regardless of which application has focus.

| Action              | Default Binding   | Configurable? | Spec Ref        |
|---------------------|-------------------|---------------|-----------------|
| Show / hide panel   | Win + Shift + V   | Yes (FR-008)  | FR-004, SC-003  |
| Paste item 1        | Win + Shift + 1   | Yes (FR-008)  | FR-002          |
| Paste item 2        | Win + Shift + 2   | Yes (FR-008)  | FR-002          |
| Paste item 3        | Win + Shift + 3   | Yes (FR-008)  | FR-002          |
| Paste item 4        | Win + Shift + 4   | Yes (FR-008)  | FR-002          |
| Paste item 5        | Win + Shift + 5   | Yes (FR-008)  | FR-002          |

**Conflict rules**:
- The following key combinations are RESERVED by Windows and MUST NOT be assigned:
  `Win + C`, `Win + V`, `Win + X`, `Win + D`, `Win + E`, `Win + L`, `Win + R`,
  `Ctrl + C`, `Ctrl + V`, `Ctrl + X`, `Ctrl + Z`, `Alt + F4`, `Ctrl + Alt + Del`.
- The app validates bindings against this list before saving and shows an inline
  error if a conflict is detected (FR-008).

---

## Panel Shortcuts (Active When Panel Is Open)

These shortcuts apply only while the clipboard panel has focus.

### Navigation

| Action                              | Key                    | Spec Ref         |
|-------------------------------------|------------------------|------------------|
| Move selection down one item        | Down arrow             | FR-013, SC-007   |
| Move selection up one item          | Up arrow               | FR-013, SC-007   |
| Jump down one visible page          | Page Down              | FR-013           |
| Jump up one visible page            | Page Up                | FR-013           |
| Jump to first item                  | Home                   | FR-013           |
| Jump to last item                   | End                    | FR-013           |
| Paste selected item and close panel | Enter                  | FR-013, FR-002   |
| Close panel, return focus           | Escape                 | US2 scenario 3   |

### Item Actions (on highlighted item)

| Action              | Key              | Notes                                          |
|---------------------|------------------|------------------------------------------------|
| Open context menu   | Application key (≡) or Shift + F10 | Exposes pin/delete actions |
| Pin / unpin item    | Ctrl + P         | Toggles IsPinned on highlighted item (FR-006)  |
| Delete item         | Delete           | Removes highlighted item from history (FR-007) |

### Search

| Action                       | Key           | Notes                                         |
|------------------------------|---------------|-----------------------------------------------|
| Focus search field           | Any printable char (when list has focus) | Typing auto-redirects to search   |
| Clear search and return list | Escape (when search has text)            | First Escape clears search; second Escape closes panel |

---

## Panel Behaviour Contract

### Focus Flow on Open

1. Panel opens with focus on the **most recent item** in the list.
2. If a search term was active from a previous open, it is cleared.
3. The first `Tab` press moves focus to the search field.
4. `Shift + Tab` from the list moves focus to the search field.

### Paste and Dismiss

Selecting an item (Enter or click) MUST:
1. Write the item's full text to the Windows clipboard.
2. Increment the item's `PasteCount`.
3. Update `LastPastedAt`.
4. Close the panel.
5. Return focus to the application that was active before the panel opened.

Step 5 is critical: the user must be able to immediately press `Ctrl + V` in their
target app without clicking.

### Scroll Behaviour

- The selected item MUST always be visible (scroll-into-view on every navigation).
- Scrolling MUST feel instant — no animation delay on arrow key presses.
- Smooth scrolling animation is acceptable for Page Down / Page Up only.
