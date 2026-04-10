## 1. Project Overview

- Genre: Hex-based Pathfinding Puzzle
- Perspective: 2D Top-down with an Isometric visual style.
- Core Loop: Analyze level -> Activate strategic hex tiles -> Press "Start" -> Larry moves to destination.

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

### 3.2 Movement & Pathfinding

- The "Start" Button: Larry remains stationary until the player triggers the movement phase.
- Efficient Pathing: If multiple paths exist to the destination, Larry will automatically calculate and take the most efficient (shortest) route.
- Falling Mechanic: If Larry reaches an edge or a gap where no active tiles lead to the destination, he falls, resulting in a Level Failure.

### 3.3 Constraints

- Move Limit: Each level provides a specific number of activations. If the player runs out of moves before a valid path is created, they must rethink their strategy.
- Reset: Failure (falling) or running out of moves requires the player to restart the level.

## 4. Technical Specifications (Initial)

- Pathfinding Algorithm: A* (A-Star) or Dijkstra optimized for hexagonal grids.
- Grid Logic: Flat-top or Pointy-top hex coordinates (to be decided during implementation).
- State Management:
  - Pre-Game: Level setup, moves initialized.
  - Planning: Player activating tiles.
  - Execution: Larry moving after "Start" is pressed.
  - Resolution: Success (Reached Destination) or Failure (Fell/No Path).

<environment_details>
Current time: 2026-04-10T20:36:40+03:00
</environment_details>
