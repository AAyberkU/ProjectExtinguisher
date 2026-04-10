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

## 4. Technical Specifications (Initial)

- Grid Logic: Flat-top axial hex coordinates (`q`, `r`) are now the working prototype standard.
- State Management:
  - Pre-Game: Level setup, moves initialized.
  - Active Play: Player activates neighboring tiles while Larry advances step by step.
  - Resolution: Success (Reached Destination) or restart after running out of moves.

## 5. Current Prototype Decisions

- Current gameplay scene uses a large regular hex board with 5 cells per outer side (61 total cells).
- Scene authoring currently relies on prefab-based `HexCell` instances registered under a central `HexGridManager`.
- The current direction is step-based play rather than separate planning and execution phases.
- Each valid click is intended to activate one neighboring tile, consume one move, and immediately move Larry forward by one step.
- Start and goal are placed on opposite outer edges of the prototype board to support movement and puzzle-flow testing.
- The current prototype already treats goal reach as a win and zero remaining moves as a fail state.

<environment_details>
Current time: 2026-04-11T01:54:42+03:00
</environment_details>
