# Quickstart & Manual Test Guide: Clipboard Panel UX Improvements

**Branch**: `002-clip-panel-ux` | **Date**: 2026-03-30

Use this guide to verify each user story independently after implementation.

---

## Build & Run

```bash
dotnet build src/Clipclaw/Clipclaw.csproj
dotnet run --project src/Clipclaw/Clipclaw.csproj
```

The app starts minimised to the system tray. Look for the Clipclaw icon in the
notification area (bottom-right).

---

## US1 — Keyboard Navigation & Enter-to-Copy

**Prerequisites**: At least 3 items in clipboard history. Copy 3 different text
strings to the Windows clipboard before starting.

1. Press `Ctrl+Shift+V` to open the panel.
2. Verify the first item is already highlighted (focus indicator visible).
3. Press ↓ once — second item should be highlighted.
4. Press ↓ again — third item highlighted.
5. Press ↑ — second item highlighted again.
6. Press ↑ until at the first item; press ↑ again — focus stays on first item.
7. Navigate to the second item, press Enter.
8. Open a text editor (Notepad etc.) and verify the second item's text was pasted.
9. Open the panel again (empty list scenario): if you have a way to clear items,
   verify pressing ↓ and Enter does nothing and "No clipboard items yet." is shown.

**Pass criteria**: Items navigate with ↑/↓; Enter copies + closes panel; no
wrap-around past first/last item.

---

## US2 — Copy Counter per Item

**Prerequisites**: At least 1 item in history.

1. Open the panel (`Ctrl+Shift+V`) — each row should show a copy count
   (e.g., "0" or "×0").
2. Select item 1 and press Enter to copy it. Reopen the panel — item 1 count
   should now be "1".
3. Repeat step 2 twice more — count should reach "3".
4. Close and restart the application. Reopen panel — count should still be "3".

**Pass criteria**: Count increments on each in-app copy; persists across restarts.

---

## US3 — Item Editing & Short Name

**Prerequisites**: At least 1 item with a long or unrecognisable text.

1. Open the panel; navigate to any item.
2. Press `F2` — an edit dialog should open with two fields: Short Name and Text.
3. Enter `"My Test Label"` in the Short Name field.
4. Click Save (or press Enter while Short Name is focused).
5. Verify the panel row now shows `"My Test Label"` as the primary heading.
6. Navigate to an item with no short name — verify it still shows the truncated
   text preview.
7. Press F2 again on the labelled item; change the Text content to a new value.
8. Save and press Enter to copy — verify the new text was pasted (not the old).
9. Press F2, make changes, then press Escape — verify changes were NOT saved.

**Pass criteria**: Short name displays as primary heading; Escape discards; full
text editable; no-short-name fallback works.

---

## US4 — Item Deletion with Confirmation

**Prerequisites**: At least 2 items in history.

1. Open the panel; navigate to any item.
2. Press `Delete` — a confirmation dialog "Delete this item?" with Yes/No appears.
3. Press `Escape` (or click No) — dialog closes, item is still in the list.
4. Press `Delete` again — dialog appears again.
5. Press `Enter` (or click Yes) — item is removed from the list.
6. Verify the focus moved to the nearest remaining item.
7. Delete all remaining items one by one — verify the empty-state message appears
   after the last one.

**Pass criteria**: No item deleted without confirmation; cancel preserves item;
confirm removes item; empty state shown when list is empty.

---

## US5 — Visual Row Differentiation

**Prerequisites**: At least 5 items in history.

1. Open the panel.
2. Look at the list without moving the mouse or keyboard.
3. Verify that adjacent rows have visibly different background shades (alternating).
4. Use ↑/↓ to move focus — verify the selected row has a distinct highlight colour
   that overrides the alternating background.
5. Switch to Light theme (see US6 below) and reopen the panel — verify alternating
   rows are still visible in the light theme.

**Pass criteria**: Even/odd rows visually distinct; selected row clearly different
from alternating rows; works in both themes.

---

## US6 — Light Theme

**Prerequisites**: App running in default Dark theme.

1. Right-click the tray icon → Open Settings (or press `Ctrl+Shift+V` then
   navigate to settings).
2. Locate the Theme option in the General tab.
3. Change from "Dark" to "Light" and click Save Settings.
4. Verify the Settings window, clipboard panel, and any dialogs now use a
   light-background style.
5. Open the clipboard panel (`Ctrl+Shift+V`) — verify dark text on light background.
6. Change back to "Dark" — verify dark theme is restored immediately.
7. Close and restart the app — verify the previously selected theme is restored.

**Pass criteria**: Theme switches live (no restart); all windows update; theme
persists across restarts; no illegible contrast combinations in either theme.
