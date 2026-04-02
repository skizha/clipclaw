<!--
  SYNC IMPACT REPORT
  ==================
  Version change: 1.1.0 → 1.2.0
  Modified principles:
    - I.   Spec-First Development          — unchanged
    - II.  User-Story Orientation          — unchanged
    - III. Keyboard-First Interaction      — unchanged (NON-NEGOTIABLE)
    - IV.  Readable Code                   — unchanged (NON-NEGOTIABLE)
    - V.   Simplicity & YAGNI              — unchanged
    - VI.  Windows-Native Reliability      — unchanged
    - VII. Non-Regression Guarantee        — NEW (added 2026-03-30)
  Added sections:
    - Principle VII: Non-Regression Guarantee
    - Quality Gate 7: Non-regression gate (manual smoke or automated test pass)
  Removed sections: none
  Templates reviewed:
    - .specify/templates/plan-template.md   ✅ aligned (Constitution Check section present)
    - .specify/templates/spec-template.md   ✅ aligned
    - .specify/templates/tasks-template.md  ✅ aligned (Polish phase covers regression validation)
  Deferred TODOs: none
-->

# Clipclaw Constitution

## Product Identity

Clipclaw is a **Windows clipboard manager** that lets users accumulate multiple
clipboard items during a session and retrieve any of them instantly via keyboard
shortcuts — without lifting their hands from the keyboard.

Every decision in this project MUST serve that core promise: fast, keyboard-driven
access to a personal clipboard history.

## Core Principles

### I. Spec-First Development

Every feature MUST begin with a written specification (`spec.md`) before any
implementation work starts. The specification is the source of truth for scope,
acceptance criteria, and user intent.

- Feature branches MUST NOT contain implementation commits until a spec exists
  (trivial one-line fixes are the only permitted exception).
- Specifications MUST be technology-agnostic: they describe **what** and **why**,
  never **how**.
- Unclear requirements MUST be resolved via `/speckit.clarify` before planning
  begins.

**Rationale**: Scope ambiguity discovered late is the primary source of rework.
Writing the spec first surfaces it at the cheapest possible moment.

### II. User-Story Orientation

All work MUST trace back to a user story with an assigned priority (P1, P2, …).
Work without a traceable user story MUST NOT be merged.

- Each user story MUST be independently testable and deliverable as an MVP increment.
- Priority order (P1 → P2 → P3 …) MUST drive implementation sequencing unless
  explicitly overridden with written justification.
- Cross-cutting concerns belong in a dedicated "Polish" phase, not inside individual
  story phases.

**Rationale**: Story-oriented work prevents gold-plating and ensures every shipped
increment delivers visible user value.

### III. Keyboard-First Interaction (NON-NEGOTIABLE)

Every user-facing operation in Clipclaw MUST be reachable by keyboard alone.
Mouse interaction is permitted as a convenience layer, never a requirement.

- Each stored clip item MUST be accessible via a distinct, documented keyboard
  shortcut or an indexed navigation pattern (e.g., `Win + V + [1–9]`).
- Shortcut assignments MUST NOT conflict with common Windows system shortcuts
  (`Win + C`, `Ctrl + C`, `Ctrl + V`, `Alt + F4`, etc.).
- New features that require pointer input with no keyboard equivalent MUST be
  rejected or redesigned before merging.
- Shortcut bindings MUST be listed in a single, authoritative location in the
  codebase; scattered magic-key literals are PROHIBITED.

**Rationale**: The app's core value proposition is zero-friction retrieval. A user
reaching for the mouse has already lost time.

### IV. Readable Code (NON-NEGOTIABLE)

Code MUST be written to be read by a human first and executed by a machine second.

- Every non-trivial function or block MUST have a name that states its intent
  without requiring a comment to decode it.
- Magic numbers and magic strings MUST be replaced with named constants.
- Comments MUST explain **why**, not **what** — the code already states what it
  does.
- Functions MUST do one thing. Functions longer than ~40 lines are a signal to
  decompose.
- Abbreviations in names are PROHIBITED unless they are universally understood
  domain terms (e.g., `id`, `url`, `ui`).

**Rationale**: Clipclaw is intended to remain maintainable over time. Unreadable
code is a liability that compounds with every new contributor.

### V. Simplicity & YAGNI

The simplest solution that satisfies the current user story MUST be preferred.

- Abstractions for one-time operations MUST NOT be introduced.
- Speculative future requirements MUST NOT drive present design.
- Complexity beyond what the spec requires MUST be justified in the Complexity
  Tracking table (see plan template).
- Dependencies MUST be added only when they provide clear, immediate value.

**Rationale**: Premature abstraction inflates maintenance cost. Three similar
lines of code are better than a premature abstraction.

### VI. Windows-Native Reliability

Clipclaw runs as a background process on Windows. It MUST behave as a
well-mannered Windows citizen.

- The app MUST NOT degrade system clipboard behaviour for other applications.
  Intercepting clipboard events MUST be non-destructive.
- Resource use (CPU, memory) MUST be negligible during idle monitoring.
- The app MUST handle Windows session events gracefully: lock, sleep, user
  switch, and logoff.
- Tray icon or system-level entry points MUST follow Windows UI conventions.
- All Windows API calls MUST have their failure paths handled explicitly; no
  silent `HRESULT` ignores.

**Rationale**: A clipboard manager that interferes with normal copy/paste or
drains battery will be uninstalled immediately regardless of its feature set.

### VII. Non-Regression Guarantee

New features MUST NOT break or degrade any previously shipped functionality.

- Every feature spec MUST include an explicit "Existing Behaviour Unaffected"
  section that lists clipboard capture, paste, shortcut retrieval, tray icon, and
  any other existing user-facing behaviours that the change touches — with a
  statement confirming each is unaffected or explicitly updated.
- Before merging a feature branch, a smoke pass over ALL prior acceptance
  scenarios (from their respective `spec.md` files) MUST be completed. Failures
  block the merge.
- Refactors that touch shared infrastructure (hotkey registration, SQLite schema,
  clipboard listener, tray icon) MUST be accompanied by at minimum a manual
  regression checklist entry in the PR description.
- SQLite schema changes MUST be additive (new columns/tables with defaults) or
  accompanied by a migration; destructive schema changes that break existing data
  are PROHIBITED without an explicit migration plan.

**Rationale**: Clipclaw is installed as a persistent background tool. A regression
in core copy/paste behaviour, even from an unrelated new feature, erodes user
trust instantly and permanently.

## Development Workflow

- **Branch naming**: `{number}-{short-name}` (sequential), auto-assigned by
  the feature creation script.
- **Feature lifecycle**: Specify → Clarify (if needed) → Plan → Tasks →
  Implement → Review → Merge.
- **Merge policy**: All merges MUST target `main`. Direct pushes to `main` are
  PROHIBITED.
- **Commit discipline**: Commit after each completed task or logical group;
  commit messages MUST reference the task ID (e.g., `T014`).
- **Agent tooling**: The `/speckit.*` command suite is the REQUIRED workflow for
  all feature work. Manual bypasses MUST be documented.

## Quality Gates

The following gates MUST pass before a feature branch is merged:

1. **Spec gate**: `spec.md` exists and has no unresolved `[NEEDS CLARIFICATION]`
   markers.
2. **Plan gate**: `plan.md` exists; Constitution Check passes or violations are
   justified in the Complexity Tracking table.
3. **Keyboard gate**: Every new user-facing operation has a documented keyboard
   shortcut and does not conflict with Windows system shortcuts.
4. **Readability gate**: No magic numbers/strings; all names are self-describing;
   no function exceeds ~40 lines without justification.
5. **Test gate** *(when tests requested)*: All tests pass; no tests are skipped
   without documented reason.
6. **Review gate**: At least one peer review (or self-review with checklist) is
   completed.
7. **Non-regression gate**: Smoke pass over all prior acceptance scenarios passes;
   the spec's "Existing Behaviour Unaffected" section is present and complete.

Complexity exceptions MUST be recorded in the plan's Complexity Tracking table
and referenced in the PR description.

## Governance

- This constitution supersedes all other development practices and ad-hoc
  conventions. Conflicts MUST be resolved in favour of the constitution.
- **Amendment procedure**: Amend via `/speckit.constitution`; every change MUST
  increment the version and update `Last Amended`. Material principle changes
  require a brief rationale comment in the Sync Impact Report.
- **Versioning policy**:
  - MAJOR — principle removed, redefined, or governance made incompatible with
    prior process.
  - MINOR — new principle or section added, or existing guidance materially
    expanded.
  - PATCH — clarifications, wording fixes, non-semantic refinements.
- **Compliance review**: Revisit this constitution at the start of each major
  project phase or when a new contributor joins.
- **Runtime guidance**: Consult `.specify/memory/` for up-to-date agent context
  and active feature plans.

**Version**: 1.2.0 | **Ratified**: 2026-03-29 | **Last Amended**: 2026-03-30
