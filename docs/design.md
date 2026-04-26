## 1. Project Overview

- Genre: Hex-based puzzle game.
- Perspective: 2D top-down gameplay logic with isometric-style art presentation.
- Current Loop: Start from the menu overlay, begin the level, click a valid neighboring hex, spend moves, move Larry step by step, use special tiles when available, and reach the goal before moves run out.
- Current Scope: A playable jam prototype with a single gameplay scene, a 61-cell board, 5 level data assets, HUD, menu/start gate, level intro animation, background presentation, art integration, and audio hooks.

## 2. Visual Style & World

- Grid System: Pointy-top axial hex board with 5 cells per outer side, 61 cells total.
- Board Authoring: The scene stores a complete board under `GridRoot/Cells`; level data changes each cell's runtime state and sprite instead of replacing the whole board.
- Perspective: Tile art uses an isometric tilt while rules remain 2D axial-grid logic.
- Protagonist: Larry, a pawn-like character using `LarrySprite.png` and hop movement.
- Background: `BackgroundPresentationController` fits a background sprite to the orthographic camera and switches between menu and gameplay presentation states.
- Visual Feedback: Inactive normal path tiles use an inactive tint; highlighted tiles use a highlight tint; start, goal, blocked, catapult, health, and active authored sprites preserve their original colors.
- Current Tile Feedback: Larry can create a runtime hover/shadow effect on the current tile while idle.

## 3. Core Mechanics

### 3.1 Startup Flow

- `LevelLoader` can hold the game in `PreGame` with `showStartGameOnStartup` enabled.
- The board and Larry are hidden during the startup gate.
- `GameHUD` builds and shows a runtime `Start Game` overlay with credits.
- Pressing `Start Game` applies the selected startup level, hides the overlay, switches the background to gameplay state, and starts the level intro if enabled.

### 3.2 Tile Activation

- Players interact by clicking hex tiles with the Unity Input System mouse input.
- Pointer-over-UI clicks are ignored through the scene `EventSystem` and `GraphicRaycaster`.
- Only registered `HexCell` instances under the active `HexGridManager` can be activated.
- A clicked tile must be walkable, inactive, and adjacent to Larry's current cell unless the adjacency rule is disabled in the Inspector.
- Each valid manual activation consumes 1 move, marks the tile active, and starts Larry's hop to that tile.

### 3.3 Movement And Resolution

- Larry moves immediately after each valid click; there is no separate path execution phase in the current implementation.
- Movement is animated by `LarryController` with configurable hop duration, hop height, jump SFX, and landing squash.
- Reaching the goal enters win resolution.
- Spending the final move without reaching the goal enters fail resolution.
- Pressing `R` resets the current session level unless input is locked.
- Winning a non-final level shows a `Next Level` action.
- Winning the final configured level shows a return-to-main-menu action through the same outcome button.

### 3.4 Special Tiles

- Blocked tiles are non-walkable and ignored on click.
- Catapult tiles are walkable special tiles. After Larry lands on one, it attempts to launch Larry 2 axial cells in its configured direction.
- Catapult launches consume an additional move, activate the landing tile if needed, play catapult SFX, and can chain up to the configured chain limit.
- Catapults do not launch if the landing cell is off-board, blocked, missing, or if no moves remain.
- Move-bonus/health tiles are walkable special tiles. When Larry lands on one, the tile grants its configured move bonus once, plays heal SFX, and marks the bonus consumed until reset.

### 3.5 Constraints

- Move Limit: Each level provides a fixed number of moves through `LevelData.moveLimit`.
- Reset: Reset restores the loaded level state, Larry position, moves remaining, outcome panel, and consumed move bonuses.
- Loss Rule: If moves reach zero after movement resolution and Larry is not on the goal, the run is lost.

## 4. Technical Specifications

- Coordinate Standard: Pointy-top axial hex coordinates stored as `Vector2Int` where `x = q` and `y = r`.
- Scene Standard: `Assets/Scenes/GameplayScene.unity` is the enabled build scene. `SampleScene` exists but is disabled in Build Settings.
- Runtime State: `GameState` contains `PreGame`, `Planning`, `Execution`, and `Resolution`; current gameplay uses `PreGame`, `Planning`, and `Resolution`.
- Core Scripts: `HexCell`, `HexGridManager`, `GameState`, `TileActivationController`, `LarryController`, `LevelData`, `LevelLoader`, `GameHUD`, and `BackgroundPresentationController`.
- Editor Utility: `GameHUDBuilder` can rebuild a HUD canvas in the active scene but the current scene already contains `HUD_Canvas`.
- Audio: `TileActivationController` owns background music and level/gameplay SFX; `LarryController` owns jump SFX; `LevelLoader` owns intro SFX.

## 5. Scene Architecture

- `Main Camera`: Orthographic camera, currently sized around `15.998`, tagged `MainCamera`.
- `Global Light 2D`: Scene-wide 2D lighting.
- `GridRoot`: Holds `HexGridManager`, `TileActivationController`, and `LevelLoader`.
- `GridRoot/Cells`: Holds 61 `HexCell` prefab instances for the board.
- `GridRoot/Larry`: Holds Larry's `SpriteRenderer` and `LarryController`.
- `HUD_Canvas`: Screen Space Overlay canvas with `CanvasScaler`, `GraphicRaycaster`, and `GameHUD`.
- `EventSystem`: Uses `InputSystemUIInputModule` for UI interaction and gameplay click guarding.
- `Background Presentation`: Holds `BackgroundPresentationController` and creates runtime background/overlay renderers.

## 6. Assets And Prefabs

- Level Assets: `Assets/Levels/Level_001.asset` through `Level_005.asset`.
- Tile Prefabs: `StartHexCell`, `GoalHexCell`, `PathHexCell_01..10`, `BlockedHexCell_01..03`, `CatapultHexCell_E/NE/NW/W/SW/SE`, and `HealthHexCell`.
- Main Character Art: `Assets/Art/Characters/Larry/LarrySprite.png`.
- Main Tile Art: `HexTile_Start`, `HexTile_Goal`, `HexTile_Path_01..10`, `HexTile_Blocked_01`, `HexTile_Catapult_Left`, and additional duck/rough tile images used by current prefab variants.
- Background Art: `Assets/Art/Background/background.png` and `Assets/Art/Background/Bg .png`; the scene references the imported `Bg _0` sprite.
- Audio Assets: `OST_Yangın-Tüpü_R01.wav`, `shuffle.wav`, `duck.wav`, `jump.wav`, `heal.wav`, `restart sfx.wav`, `lose.wav`, and `level passed sfx.mp3`.

## 7. HUD And Presentation

- Moves Counter: Top-left `Moves Left: 08` style text driven every frame from `PlanningMovesRemaining`.
- Current Moves Color: The current implementation applies a fixed dark-purple border/HUD text color; amber/red threshold fields still exist but are not used by `RefreshMovesLabel()`.
- Level Label: Top-center label reads from loaded `LevelData.GetDisplayName()`.
- Reset Hint: Top-right `R  Reset` label.
- Outcome Overlay: Center overlay fades/slides in for win or loss.
- Outcome Action: Shows `Next Level` when another ordered level exists; shows `Return to Main Menu` on final-level win.
- Gameplay Hint: Bottom-center helper text fades after a configurable duration.
- Start Overlay: Runtime overlay with `Start Game` button and credits appears before gameplay when startup gate is enabled.

## 8. Level Structure

- Use a single `GameplayScene` for all playable levels.
- Levels are `LevelData` ScriptableObject assets, not separate scenes.
- `LevelLoader` holds an ordered level list and advances in-session via `LoadNextLevel()`.
- `LevelLoader` selected startup level is currently `Level_001`; ordered progression currently contains levels 1 through 5.
- `LevelData` defines level number/name, move limit, start coordinate, goal coordinate, default initial active state, and per-cell overrides.
- Per-cell overrides can define blocked state, initial active state, catapult state/direction, move-bonus state/amount, and visual variant index.
- Path and blocked visual variants currently share the `usePathVariant` and `pathVariantIndex` fields in `LevelData.CellLevelState`.

| Level | Moves | Start | Goal | Blocked | Catapults | Move Bonuses |
| --- | ---: | --- | --- | ---: | ---: | ---: |
| `Level_001` | 8 | `(-4, 0)` | `(4, 0)` | 0 | 0 | 0 |
| `Level_002` | 9 | `(-4, 0)` | `(2, 2)` | 12 | 0 | 0 |
| `Level_003` | 7 | `(1, 3)` | `(2, -4)` | 6 | 8 | 0 |
| `Level_004` | 8 | `(4, -3)` | `(-4, 3)` | 6 | 4 | 4 |
| `Level_005` | 7 | `(-1, 3)` | `(0, -4)` | 6 | 4 | 2 |

## 9. Current Design Notes

- The current project is content-complete enough for a short jam prototype pass, but level balance still needs manual playtesting.
- Special tiles are runtime states applied to the shared 61-cell board rather than separate authored scene layouts.
- If the move counter should return to normal/amber/red behavior, `GameHUD.RefreshMovesLabel()` needs to be changed or the design should officially keep the fixed border-color treatment.
- Several art filenames are informal jam placeholders and should be cleaned only when doing a controlled asset-renaming pass that preserves Unity meta GUID references.
