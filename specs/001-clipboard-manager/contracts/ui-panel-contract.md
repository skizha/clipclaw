# UI Panel Contract: Clipboard Manager

**Branch**: `001-clipboard-manager` | **Date**: 2026-03-29

Defines the structure, sections, states, and layout constraints of every visible
surface in Clipclaw. This is a design-level contract, not an implementation spec.

---

## Clipboard Panel

The primary user surface. A floating, borderless window that appears near the
system tray (or screen centre as fallback) when the global shortcut is pressed.

### Layout Structure

```
┌─────────────────────────────────┐
│  🔍  Search clips...            │  ← Search field (always at top)
├─────────────────────────────────┤
│  📌 PINNED                      │  ← Section header (hidden if no pinned items)
│  ┌──────────────────────────┐   │
│  │ ★ Item text preview…   1 │   │  ← Pinned clip row (shortcut badge on right)
│  └──────────────────────────┘   │
├─────────────────────────────────┤
│  ⭐ FREQUENTLY USED             │  ← Section header (hidden if < 1 frequent item)
│  ┌──────────────────────────┐   │
│  │   Item text preview…    5 │  │  ← Frequent clip row (paste count badge)
│  └──────────────────────────┘   │
├─────────────────────────────────┤
│  🕐 RECENT                      │  ← Section header (always shown)
│  ┌──────────────────────────┐   │
│  │   Item text preview… ⊞1  │   │  ← Recent row (shortcut label for top 5)
│  │   Item text preview…     │   │
│  │   Item text preview…     │   │
│  └──────────────────────────┘   │
│                                 │
│  [scrollable area]              │
└─────────────────────────────────┘
```

### Panel Dimensions

| Property         | Value                              |
|------------------|------------------------------------|
| Width            | 360 px (fixed)                     |
| Max height       | 560 px; scrolls internally if list overflows |
| Position         | Near tray icon; stays within screen bounds |
| Border           | None (floating card with drop shadow) |
| Corner radius    | 8 px                               |

### Clip Row Layout

Each row in any section displays:

```
┌──────────────────────────────────────────┐
│ [icon]  Display text (max 80 chars, …)  [badge] │
└──────────────────────────────────────────┘
```

- **Icon**: Pin icon (★) for pinned items; empty space otherwise.
- **Display text**: First 80 characters of `Text`; truncated with `…` if longer.
  Full text shown in a tooltip on hover.
- **Badge** (right-aligned): Shortcut label (`⊞ 1` … `⊞ 5`) for the top 5 recent
  items; paste count number for frequently-used items; empty for all others.
- **Selected state**: Row background highlighted (Material Design secondary colour),
  full-width highlight.
- **Hover state**: Subtle background tint on mouse-over.

---

## Settings Window

A standard, resizable dialog window (not floating) with tabbed sections.

### Tabs

| Tab Label    | Contents                                                        |
|--------------|-----------------------------------------------------------------|
| General      | Max history size (slider + text), launch on startup (toggle), persist history on exit (toggle) |
| Shortcuts    | Table of all ShortcutBindings; inline key recorder to change bindings; conflict warning inline |
| About        | App version, licence, link to project repo                      |

### Shortcut Recorder Behaviour

1. User clicks the key field for a binding.
2. Field enters "recording" state (shows "Press keys…").
3. Next key combination pressed is captured.
4. App validates against reserved list and existing bindings.
5. If conflict: red inline message, field reverts, no save.
6. If valid: binding saves immediately (no explicit Save button needed).

---

## Tray Icon

| State          | Icon appearance                                     |
|----------------|-----------------------------------------------------|
| Running        | Clipclaw icon (solid)                               |
| Panel open     | Clipclaw icon (highlighted / accent colour)         |
| Error/warning  | Clipclaw icon with small badge (⚠)                  |

### Tray Context Menu

```
Open Clipclaw          (shows panel, same as global shortcut)
Settings…
─────────────────
Exit
```

---

## Visual Design Constraints (FR-014)

- **Colour scheme**: Material Design 3 baseline; dark theme by default; light theme
  available via Settings.
- **Typography**: System UI font (`Segoe UI Variable` on Windows 11,
  `Segoe UI` on Windows 10). Minimum body size 13 sp.
- **Contrast**: All text must meet WCAG AA (4.5:1 ratio for normal text).
- **Transitions**: Panel open/close uses a 150 ms fade + subtle vertical slide.
  Arrow-key navigation scrolls instantly (no animation between rows).
- **Spacing**: 8 px base grid; 12 px row padding; 16 px section padding.
