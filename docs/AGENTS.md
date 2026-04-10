This file serves as the entry point and operational guide for any AI agent (generalist or sub-agent) joining a session. It defines the workflow, communication protocols, and links to the project's living documentation.

## Documentation Structure

All project documentation is located in the `docs/` directory. Each agent must read these in the following order:

1. `AGENTS.md`: (Current File) Operational rules and sub-agent definitions.
2. `design.md`: Game design specifications, mechanics, and architectural decisions.
3. `progress.md`: Current status, completed tasks, and the immediate "to-do" list.
4. `bugs.md`: Known issues, reproduction steps, and fix statuses.

## Agent Roles & Workflow

You may operate as the primary orchestrator or delegate tasks to specific sub-agents:

- `developer`: Responsible for implementation, refactoring, and technical logic.
- `tester`: Responsible for bug hunting, regression testing, and verifying fixes.
- `designer`: Handles gameplay feel, UX/UI layouts, and visual/audio polish.
- `docs-maintainer`: Ensures all `.md` files are updated after every significant change.

## Standard Execution Loop

1. Analysis: Read the `docs/` files to understand the current state.
2. Implementation: Perform the task via `developer`.
3. Handoff Pause (Critical): After development is complete, STOP. Do not hand over to the tester yet. Ask the user to perform a manual test.
4. User Verification: Wait for the user to provide feedback or say continue/proceed.
5. Quality Assurance: If the user proceeds, hand over to `tester` for a formal pass.
6. Documentation: Update the relevant files via `docs-maintainer`.
7. Finalization (Git Push): Once the docs are updated and no blocking issues remain, push the changes to Git. This is the final step of any feature or bug fix.

## Working Rules

- Small Steps: Progress in incremental, functional steps rather than massive architectural shifts.
- User-Centric: When in doubt, or if a decision conflicts with existing manual scene setups, ask the user.
- Inspector-Driven: Prefer Unity Inspector-friendly solutions (serialized fields, prefabs) over hardcoded references.
- Operational Memory: Every session must begin by summarizing the status found in `progress.md`.
- Commit Messages: Use concise English commit titles and bodies that explain the related change.

## Update Protocols (The Rules of Update)

To prevent documentation rot, follow these strict rules whenever a change is made. After updating, always proceed to Git Push.

| Trigger | Action |
| --- | --- |
| New Decision Made | Update `docs/progress.md` and `docs/design.md`. |
| New System / Architecture Change | Update `docs/design.md`. |
| Bug Identified | Log it immediately in `docs/bugs.md`. |
| Operational Progress Only | Update `docs/progress.md`. |
| Bug Resolved | Move entry to "Fixed" section in `docs/bugs.md`. |

## Session Start Checklist

Every time a new session starts, the agent must:

1. Read `docs/AGENTS.md` for rules.
2. Check `docs/progress.md` for the last known state.
3. Cross-reference `docs/design.md` for technical constraints.
4. Check `docs/bugs.md` for any blockers that need immediate attention.
5. Confirm the current task with the user before writing any code.
