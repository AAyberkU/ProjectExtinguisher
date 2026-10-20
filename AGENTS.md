# AGENTS.md

Operational guide for agents working on this Unity project. Living docs are in `docs/`.

## Which Doc To Read

Default: `docs/progress.md` (current state and next steps). Read others only when the task needs them.

| Task | Read |
| --- | --- |
| Current state, next steps | `docs/progress.md` |
| Gameplay, mechanics, scene architecture, technical decisions | `docs/design.md` |
| Blockers, open risks, fixed issues | `docs/bugs.md` |

## Workflow

- Inspect the relevant Unity assets, scenes, scripts, and settings before changing them. Ask only when the request is unclear or conflicts with a manual Unity scene setup.
- After a gameplay-affecting change, stop and let the user test manually before broad QA or polish.
- Run targeted checks, Unity tests, or a tester pass only when requested or clearly appropriate.
- Commit or push only when the user explicitly asks. Never push automatically. Commit messages are concise English explaining change and reason.
- Documentation-only sync tasks skip implementation and manual handoff unless the sync finds a blocking bug.

## Project Conventions

- Inspector-driven: prefer serialized fields, prefabs, and scene references over hardcoded lookups for tunable gameplay data.
- Expose gameplay-feel values in the Inspector so iteration needs no code edits.
- Keep practical debug logs in gameplay scripts behind an Inspector toggle.

## Update Protocols

Update docs last, in this order, to match actual project state.

| Trigger | Action |
| --- | --- |
| New design decision, or system/architecture change | Update `docs/design.md`, then `docs/progress.md`. |
| Operational progress only | Update `docs/progress.md`. |
| Bug identified | Log it in `docs/bugs.md`. |
| Bug resolved | Move or summarize it under `docs/bugs.md` fixed items. |
| Documentation sync | Cross-check docs against project reality and update every affected doc. |

If you notice other problems while working, mention them briefly in your report.
