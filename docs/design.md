## 1. Project Overview

- Genre: Hex-based Puzzle
- Perspective: 2D Top-down with an Isometric visual style.
- Core Loop: Analyze level -> Activate a neighboring hex tile -> Larry advances one step -> Reach the destination.

## 2. Visual Style & World

- Grid System: Hexagonal tile structure.
- Perspective: Assets are drawn with an isometric tilt to provide depth while maintaining 2D logic.
- Visual Feedback:
  - Inactive Tiles: Slightly blurred and desaturated.
  - Active Tiles: Clear, sharp, and highlighted when clicked.
- Protagonist: Larry (A pawn-like character).

## 3. Core Mechanics

### 3.1 Tile Activation

- Players interact with the level by clicking on hex tiles.
- Clicking a tile consumes 1 Move from the "Move Limit."
- Activated tiles become "walkable" surfaces for Larry.

### 3.2 Movement

- Immediate Movement: Larry does not wait for a separate execution phase. Each valid tile activation immediately advances Larry by one step.
- Neighbor-Driven Progression: Players may only activate tiles that are adjacent to Larry's current position.
- Goal Completion: The level is completed when Larry reaches the goal tile.

### 3.3 Constraints

- Move Limit: Each level provides a specific number of activations. If the player runs out of moves before a valid path is created, they must rethink their strategy.
- Reset: Running out of moves requires the player to restart the level.
- Current Fail Rule: If the final available move is spent without reaching the goal, the run is lost and must be reset.

## 4. Technical Specifications (Current)

- Grid Logic: Pointy-top axial hex coordinates (`q`, `r`) are the working board standard.
- State Management:
  - Pre-Game: Level setup, moves initialized.
  - Active Play: Player activates neighboring tiles while Larry advances step by step.
  - Resolution: Success (Reached Destination) or restart after running out of moves.

## 5. Current Prototype Decisions

- Current gameplay scene uses a large regular pointy-top hex board with 5 cells per outer side (61 total cells).
- Scene authoring currently relies on prefab-based `HexCell` instances registered under a central `HexGridManager`.
- The current direction is step-based play rather than separate planning and execution phases.
- Each valid click is intended to activate one neighboring tile, consume one move, and immediately move Larry forward by one step.
- Start and goal are placed on opposite outer edges of the prototype board to support movement and puzzle-flow testing.
- The current prototype already treats goal reach as a win and zero remaining moves as a fail state.
- Real Larry/start/goal/path art is integrated; blocked art is still pending.

## 6. HUD Design

### Layout

- **Top-left**: `Moves Left: 08` — large counter, color shifts as moves deplete (normal → amber → red).
- **Top-center**: `Level 1` — small label, placeholder until level system is built.
- **Top-right**: `R Reset` — small always-visible hint during play.
- **Center overlay**: Win/Lose message — hidden during play, fades/slides in on outcome.
- **Bottom-center**: Single-line gameplay hint — fades out after a few seconds.

### Elements

| Element | Position | Behavior |
|---|---|---|
| Moves Left counter | Top-left | Large text; color shifts: normal → amber (≤50%) → red (≤25%) |
| Level label | Top-center | Small text; shows current level name/number (e.g. `Level 1`) |
| Reset hint | Top-right | Small `R  Reset` label; always visible during play |
| Win/Lose overlay | Center | Hidden during play; shows `You Win` or `Out of Moves` + sub-line `Press R to reset`; fades/slides in on outcome |
| Gameplay hint | Bottom-center | Single-line helper text; fades out after a configurable number of seconds |

### Style

- Minimal and semi-transparent so the board remains the primary focus.
- Hex-corner motif on panels where possible.
- Canvas render mode: Screen Space – Overlay; `GraphicRaycaster` disabled so HUD does not intercept gameplay clicks.

## 7. Level Structure

- Use a single `GameplayScene` for all levels.
- Levels should be defined as separate data assets rather than separate scenes.
- The board stays the same 61-cell pointy-top layout across all levels.
- Each level data asset should define at minimum:
  - level name / number
  - move limit
  - start cell coordinate
  - goal cell coordinate
  - blocked cell coordinates
  - optional path-art variant assignments
- `LevelLoader` now reads the selected level data and configures the existing gameplay scene at runtime.
- The HUD level label now reads from loaded level data.

<environment_details>
Current time: 2026-04-11T17:20:48+03:00
</environment_details>
