# Keyboard & Command Contracts: Clipboard Panel UX Improvements

**Branch**: `002-clip-panel-ux` | **Date**: 2026-03-30

This document defines the authoritative keyboard shortcut contracts for all new
interactions introduced in this feature. All shortcuts MUST be registered as named
constants in `Infrastructure/HotkeyConstants.cs` before use in code.

---

## Panel Keyboard Contracts

These keys are active while the clipboard panel (`ClipboardPanel`) is open.

| Key       | Action                                         | Scope          |
|-----------|------------------------------------------------|----------------|
| ↓         | Move focus to next item in list                | Panel open     |
| ↑         | Move focus to previous item in list            | Panel open     |
| Enter     | Copy focused item to clipboard and close panel | Item focused   |
| F2        | Open edit dialog for focused item              | Item focused   |
| Delete    | Prompt confirmation → delete focused item      | Item focused   |
| Escape    | Close panel (existing behaviour, unchanged)    | Panel open     |
| Ctrl+P    | Toggle pin on focused item (existing)          | Item focused   |

**Notes**:
- Enter behaviour for copy-and-close is the same as the existing click behaviour.
- ↑ at the first item: focus stays on first item (no wrap-around).
- ↓ with no selection: focuses the first item.
- F2 and Delete require an item to be focused; both are no-ops on an empty list.
- The Delete key handler MUST call the confirmation dialog before executing
  `DeleteClipItemAsync`; the existing direct-delete behaviour is superseded.

---

## Edit Dialog Keyboard Contracts

These keys are active while the `EditClipDialog` is open.

| Key      | Action                                         |
|----------|------------------------------------------------|
| Enter    | Save changes and close dialog                  |
| Escape   | Discard changes and close dialog               |
| Tab      | Move focus between ShortName and Text fields   |

**Notes**:
- Enter saves only if focus is NOT in the multiline Text field (where Enter adds
  a newline). A dedicated Save button or Ctrl+Enter triggers save from Text field.
- Escape always discards all changes regardless of which field is focused.

---

## Settings Window Keyboard Contracts

| Key / Control | Action                              |
|---------------|-------------------------------------|
| Theme ComboBox | Selects "Dark" or "Light" theme   |
| Save Settings  | Applies theme immediately (live)   |

No new global hotkeys are introduced by this feature.

---

## Confirmation Dialog Contracts

The delete confirmation uses the standard system `MessageBox` which is
keyboard-navigable natively:

| Key   | Action        |
|-------|---------------|
| Y     | Confirm Yes   |
| N     | Confirm No    |
| Enter | Confirm Yes (focused button) |
| Escape| Dismiss (No)  |

---

## Unchanged Existing Shortcuts

The following existing shortcuts are PRESERVED without modification:

| Shortcut      | Action                          |
|---------------|---------------------------------|
| Ctrl+Shift+V  | Open/close clipboard panel      |
| Ctrl+Shift+1–5| Paste item 1–5 directly         |
| Ctrl+P        | Toggle pin on selected item     |
| Page Up/Down  | Jump by page in list            |
| Home / End    | Jump to first/last item         |
