---
description: "Task list for Clipboard Panel UX Improvements"
---

# Tasks: Clipboard Panel UX Improvements

**Input**: Design documents from `specs/002-clip-panel-ux/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅

**Tests**: Not requested — no test tasks generated.

**Organization**: Tasks are grouped by user story to enable independent
implementation and testing of each story.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no shared state dependencies)
- **[Story]**: Which user story this task belongs to (US1–US6)
- Paths relative to `src/Clipclaw/` unless otherwise noted

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add new constants and converters used across multiple stories.
No user stories can surface cleanly without these in place.

- [X] T001 Add `EditItem` action name constant to `Infrastructure/HotkeyConstants.cs`
- [X] T002 [P] Add `CopyCountDisplayConverter` (returns "999+" for values ≥ 1000) to `Infrastructure/Converters.cs`
- [X] T003 [P] Register `CopyCountDisplayConverter` in `Infrastructure/AppResources.xaml`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Schema migrations and model extensions that every user story depends
on. No story phase can safely begin until the database schema and C# models are
updated.

**⚠️ CRITICAL**: All story phases depend on these tasks completing first.

- [X] T004 Add `ShortName` property (nullable string, max 60 chars) to `Models/ClipItem.cs`; add `DisplayLabel` computed property (returns ShortName if set, otherwise DisplayText) and `HasShortName` bool property
- [X] T005 [P] Add `ClipTheme` enum (`Dark`, `Light`) to `Models/AppSettings.cs` and add `Theme` property (type `ClipTheme`, default `Dark`)
- [X] T006 Add `ALTER TABLE ClipItems ADD COLUMN ShortName TEXT NULL` migration (guarded with try/catch on SqliteException) to `InitialiseAsync()` in `Services/SqlitePersistenceService.cs`
- [X] T007 [P] Add `ALTER TABLE AppSettings ADD COLUMN Theme TEXT NOT NULL DEFAULT 'Dark'` migration to `InitialiseAsync()` in `Services/SqlitePersistenceService.cs`
- [X] T008 Update `UpsertClipItemAsync()` in `Services/SqlitePersistenceService.cs` to include `ShortName` in the INSERT OR REPLACE parameter list
- [X] T009 [P] Update `GetAllClipItemsAsync()` in `Services/SqlitePersistenceService.cs` to map the `ShortName` column from the SELECT reader
- [X] T010 Update `SaveSettingsAsync()` in `Services/SqlitePersistenceService.cs` to include `Theme` in the UPDATE parameter
- [X] T011 [P] Update `GetSettingsAsync()` in `Services/SqlitePersistenceService.cs` to map and parse the `Theme` column (default to `Dark` on unrecognised value)

**Checkpoint**: Build passes (`dotnet build src/Clipclaw/Clipclaw.csproj`) — all
stories can now begin in parallel.

---

## Phase 3: User Story 1 — Keyboard Navigation & Enter-to-Copy (Priority: P1) 🎯 MVP

**Goal**: Panel opens with first item pre-selected; ↑/↓ navigate the list; Enter
copies the focused item to clipboard and closes the panel.

**Independent Test**: Open panel → press ↓ once → press Enter → verify second
item's text is on the clipboard.

### Implementation for User Story 1

- [X] T012 [US1] In `ViewModels/PanelViewModel.cs`, call `SelectFirst()` at the end of `LoadItemsAsync()` so the first item is pre-selected when the panel opens
- [X] T013 [US1] In `Views/ClipboardPanel.xaml.cs`, update the Enter key handler so it calls `PasteSelectedCommand` and closes the panel when an item is focused (confirm existing wiring handles this; fix if not)
- [X] T014 [US1] In `Views/ClipboardPanel.xaml.cs`, confirm the ↑ key handler stops at the first item (does NOT wrap to last); confirm ↓ with no selection focuses the first item; fix either if broken

**Checkpoint**: US1 is independently testable — keyboard-only clip selection
and copy works end-to-end.

---

## Phase 4: User Story 2 — Copy Counter per Item (Priority: P2)

**Goal**: Every panel row displays the `PasteCount` for that clip item. Counts
≥ 1000 display as "999+".

**Independent Test**: Copy an item 3× via the app; reopen panel; confirm count
badge reads "3". Restart app; confirm count persists.

### Implementation for User Story 2

- [X] T015 [US2] In `Views/ClipboardPanel.xaml`, add the copy count badge (bound to `PasteCount` via `CopyCountDisplayConverter`) to the Pinned section's item template — display on right side of row, styled as a small muted label
- [X] T016 [P] [US2] In `Views/ClipboardPanel.xaml`, add the same copy count badge to the Frequent section's item template (replacing or augmenting any existing PasteCount display)
- [X] T017 [P] [US2] In `Views/ClipboardPanel.xaml`, add the same copy count badge to the Recent section's item template

**Checkpoint**: US2 independently testable — all rows show count badge; count
persists across restarts (backed by existing `IncrementPasteCountAsync`).

---

## Phase 5: User Story 3 — Item Editing & Short Name (Priority: P2)

**Goal**: F2 on a focused item opens an edit dialog; user can set/change the
short name and edit the full text. Short name displays as primary row heading.

**Independent Test**: Press F2 on any item → enter short name "My Label" → Save
→ confirm row heading is "My Label". Press F2 → change text → Escape → confirm
original text unchanged.

### Implementation for User Story 3

- [X] T018 [US3] Create `Views/EditClipDialog.xaml` — modal dialog window with: ShortName TextBox (single line, max 60 chars), Text TextBox (multiline, scrollable), Save button, Cancel button; follows existing dark/light theme styling
- [X] T019 [US3] Create `Views/EditClipDialog.xaml.cs` — code-behind: Escape → discard + close; Enter (when ShortName is focused) → save + close; Ctrl+Enter (when Text is focused) → save + close; expose `ResultItem` property (null on cancel, updated ClipItem on save)
- [X] T020 [US3] In `Views/ClipboardPanel.xaml.cs`, add F2 key handler: get focused `ClipItem`, open `EditClipDialog` as modal, on non-null result call `UpsertClipItemAsync(result)` and reload panel items
- [X] T021 [US3] In `Views/ClipboardPanel.xaml`, update the Pinned section item template: show `DisplayLabel` as bold primary text; when `HasShortName` is true, show `DisplayText` as a secondary smaller muted line beneath
- [X] T022 [P] [US3] In `Views/ClipboardPanel.xaml`, apply the same two-line row template (primary `DisplayLabel` + optional secondary `DisplayText`) to the Frequent section item template
- [X] T023 [P] [US3] In `Views/ClipboardPanel.xaml`, apply the same two-line row template to the Recent section item template

**Checkpoint**: US3 independently testable — F2 opens dialog; short name
persists and shows as primary; Escape discards; full text edit works.

---

## Phase 6: User Story 4 — Item Deletion with Confirmation (Priority: P2)

**Goal**: Delete key on a focused item shows "Delete this item?" MessageBox;
item is removed only on Yes confirmation; focus moves to nearest remaining item.

**Independent Test**: Focus item → Delete → click No → item present. Delete
again → click Yes → item gone, focus on neighbour.

### Implementation for User Story 4

- [X] T024 [US4] In `Views/ClipboardPanel.xaml.cs`, update the Delete key handler: before executing `DeleteCommand`, call `MessageBox.Show("Delete this item?", "Clipclaw", MessageBoxButton.YesNo, MessageBoxImage.Question)` and only proceed on `MessageBoxResult.Yes`
- [X] T025 [US4] In `ViewModels/PanelViewModel.cs`, after a successful delete, call `SelectFirst()` (or select the item at the same index if one exists) so focus is restored to a valid item

**Checkpoint**: US4 independently testable — no item is deleted without
confirmation; cancel preserves item; focus is restored after delete.

---

## Phase 7: User Story 5 — Visual Row Differentiation (Priority: P3)

**Goal**: Adjacent rows in the panel have visibly different backgrounds
(alternating even/odd shading) in both dark and light themes.

**Independent Test**: Open panel with 5+ items; without hovering or selecting,
confirm rows alternate visually.

### Implementation for User Story 5

- [X] T026 [US5] In `Views/ClipboardPanel.xaml`, set `AlternationCount="2"` on the Pinned `ListBox` and update the `ClipRow` `ListBoxItem` style to bind `Background` to `ItemsControl.AlternationIndex` — even rows use base row colour, odd rows use a slightly lighter/darker variant (theme-aware: use `MaterialDesign` surface colour tokens)
- [X] T027 [P] [US5] Apply the same `AlternationCount="2"` and updated `ClipRow` style to the Frequent `ListBox` in `Views/ClipboardPanel.xaml`
- [X] T028 [P] [US5] Apply the same `AlternationCount="2"` and updated `ClipRow` style to the Recent `ListBox` in `Views/ClipboardPanel.xaml`

**Checkpoint**: US5 independently testable — open panel in dark theme, confirm
row alternation; switch to light theme (after US6), confirm still visible.

---

## Phase 8: User Story 6 — Light Theme (Priority: P3)

**Goal**: Settings window exposes Light/Dark theme choice; selection applies
live to all windows via `PaletteHelper.SetTheme()`; preference persists.

**Independent Test**: Open Settings → select Light → Save → all windows switch
to light styling immediately. Restart app → light theme still active.

### Implementation for User Story 6

- [X] T029 [US6] In `ViewModels/SettingsViewModel.cs`, add `SelectedTheme` property (type `ClipTheme`), load it from `AppSettings.Theme` in the existing load logic, and call `ThemeService.Apply()` after `SaveSettingsAsync()`
- [X] T030 [US6] In `Views/SettingsWindow.xaml`, add a Theme `ComboBox` to the General tab, bound to `SelectedTheme` on `SettingsViewModel`, with items "Dark" and "Light"; replace all hard-coded dark colors with DynamicResource references
- [X] T031 [US6] In `App.xaml.cs`, after loading settings on startup, call `ThemeService.Apply()` to restore the persisted theme before the main window is shown

**Checkpoint**: US6 independently testable — theme switches live; persists
across restarts; all windows update.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Verify non-regression, ensure keyboard contracts are consistent,
and run the quickstart validation guide.

- [ ] T032 [P] Verify the empty-state message (`"No clipboard items yet."`) is shown and ↑/↓/Enter are no-ops when the list is empty — fix if broken (`Views/ClipboardPanel.xaml` / `Views/ClipboardPanel.xaml.cs`)
- [ ] T033 [P] Verify short name display truncates with ellipsis when the name exceeds the row width (`Views/ClipboardPanel.xaml` — ensure `TextTrimming="CharacterEllipsis"` on the primary text element)
- [ ] T034 [P] Verify copy count cap: if `PasteCount >= 1000`, badge shows "999+" (`Infrastructure/Converters.cs` — confirm `CopyCountDisplayConverter` logic)
- [ ] T035 Run through `specs/002-clip-panel-ux/quickstart.md` for all 6 user stories and confirm each pass criterion is met
- [ ] T036 [P] Confirm existing shortcuts (Ctrl+Shift+V, Ctrl+Shift+1–5, Ctrl+P) still work as before (non-regression per constitution Principle VII)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — can start immediately after
- **US2 (Phase 4)**: Depends on Phase 2 — parallel with US1 if separate developer
- **US3 (Phase 5)**: Depends on Phase 2 — parallel with US1/US2 if separate developer
- **US4 (Phase 6)**: Depends on Phase 2 — can run in parallel with US1/US2/US3
- **US5 (Phase 7)**: Depends on Phase 2 — parallel possible; visually confirms after US6
- **US6 (Phase 8)**: Depends on Phase 2 — independent; required for US5 full verification
- **Polish (Phase 9)**: Depends on all user story phases completing

### User Story Dependencies

- **US1 (P1)**: Independent — no story dependencies
- **US2 (P2)**: Independent — no story dependencies
- **US3 (P2)**: Independent — no story dependencies
- **US4 (P2)**: Independent — no story dependencies
- **US5 (P3)**: Independent — full visual verification benefits from US6 being done
- **US6 (P3)**: Independent — no story dependencies

### Within Each User Story

- Models/infrastructure (Phase 2) before all story phases
- Dialog creation (T018, T019) before F2 handler (T020)
- ViewModel changes (T012, T025, T029) before View changes that bind to them
- Commit after each phase checkpoint

### Parallel Opportunities

- T002, T003, T005, T007, T009, T011 can all run in parallel with their siblings
- Once Phase 2 completes: US1–US6 phases can all be started in parallel
- Within US3: T021, T022, T023 can run in parallel (different template blocks)
- Within US5: T026, T027, T028 can run in parallel
- Polish tasks T032–T034, T036 can run in parallel

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1: Setup (T001–T003)
2. Complete Phase 2: Foundational (T004–T011) — required for schema/model stability
3. Complete Phase 3: US1 (T012–T014)
4. **STOP and VALIDATE**: Arrow keys work; Enter copies + closes; first item pre-selected
5. Ship/demo US1

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. US1 → keyboard navigation works (P1 MVP)
3. US2 → copy counter visible in all rows
4. US3 → short names and text editing
5. US4 → safe deletion with confirmation
6. US5 → alternating rows
7. US6 → light theme
8. Polish → non-regression verified

### Parallel Strategy (two developers)

- After Phase 2 completes:
  - **Dev A**: US1 (T012–T014) → US3 (T018–T023) → US5 (T026–T028)
  - **Dev B**: US2 (T015–T017) → US4 (T024–T025) → US6 (T029–T031)
  - Both: Polish (T032–T036) together

---

## Notes

- [P] tasks = different files or isolated logic blocks, no conflict risk
- [Story] label maps each task to its user story for traceability
- `dotnet build src/Clipclaw/Clipclaw.csproj` should pass cleanly after every phase
- All new string literals (action names, dialog text) belong in constants — not
  inline in code
- Commit message format: `feat(002): T0XX description` per convention
