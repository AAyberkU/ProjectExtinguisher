This file is the entry point and operational guide for any AI agent joining the project. It defines the documentation order, project workflow, and update rules for keeping the living docs aligned with the Unity project.

## Documentation Structure

Project documentation is stored in the `docs/` directory, with this repository-wide guide at the project root. Read the files in this order before changing project files or documentation:

1. `AGENTS.md`: Operational rules and sub-agent expectations.
2. `docs/design.md`: Game design, mechanics, scene architecture, and technical decisions.
3. `docs/progress.md`: Current project state, completed work, immediate next steps, and session notes.
4. `docs/bugs.md`: Confirmed bugs, open technical risks, and fixed issues.

## Agent Roles & Workflow

- `developer`: Implements, refactors, and investigates technical logic.
- `tester`: Reproduces bugs, verifies fixes, and reports regressions or risks.
- `designer`: Reviews gameplay feel, UX/UI, visual direction, and audio/polish choices.
- `docs-maintainer`: Updates `.md` files after significant implementation, design, or production-state changes.

## Standard Execution Loop

1. Analysis: Read the `docs/` files and inspect the relevant Unity assets, scenes, scripts, and settings before making changes.
2. Implementation: Make the smallest correct project change when the user asks for code, scene, asset, or design work.
3. Manual Handoff: For gameplay-affecting changes, stop after implementation and let the user manually test before broad QA or follow-up polish.
4. Quality Assurance: When requested or appropriate, run targeted checks, Unity tests, console checks, and/or a tester pass.
5. Documentation: Update `docs/design.md`, `docs/progress.md`, and `docs/bugs.md` to reflect the actual state of the project.
6. Finalization: Commit or push only when the user explicitly requests it. Do not push automatically after documentation-only or implementation work.

Documentation-only sync tasks skip implementation and manual gameplay handoff unless the sync uncovers a blocking bug that requires a separate fix.

## Working Rules

- Small Steps: Prefer incremental, functional changes over large rewrites.
- User-Centric: Ask only when a decision is ambiguous or conflicts with a manual Unity scene setup.
- Inspector-Driven: Prefer serialized fields, prefabs, and scene references over hardcoded lookups for tunable gameplay data.
- Tunable Scripts: Expose gameplay-feel values in the Inspector so jam iteration does not require code edits.
- Debug Logging: Gameplay scripts should keep practical debug logs behind an Inspector toggle.
- Operational Memory: New sessions should summarize the current status from `docs/progress.md` before doing substantial work.
- Commit Messages: When commits are requested, use concise English commit messages that explain the change and reason.

## Update Protocols

| Trigger | Action |
| --- | --- |
| New design decision | Update `docs/design.md` and `docs/progress.md`. |
| New system or architecture change | Update `docs/design.md` and `docs/progress.md`. |
| Operational progress only | Update `docs/progress.md`. |
| Bug identified | Log it in `docs/bugs.md`. |
| Bug resolved | Move or summarize it under `docs/bugs.md` fixed items. |
| Documentation sync | Cross-check docs against project reality and update every affected doc. |

## Session Start Checklist

1. Read `AGENTS.md`.
2. Read `docs/progress.md` for the last known state.
3. Cross-reference `docs/design.md` for gameplay and architecture constraints.
4. Check `docs/bugs.md` for blockers and technical risks.
5. Inspect the relevant Unity project files before editing; confirm with the user only if the requested task is unclear.
