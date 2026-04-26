## Status

- Project is in playable jam-prototype/content-polish phase.
- `GameplayScene` is the active build scene and contains the full board, Larry, HUD, startup menu overlay, background presentation, audio wiring, level intro animation, and ordered level progression.
- The project currently has 5 playable `LevelData` assets wired into `LevelLoader`: `Level_001` through `Level_005`.
- Implemented gameplay systems include normal tile activation, Larry-centered adjacency, step-by-step movement, win/fail resolution, reset, blocked tiles, catapult tiles, move-bonus/health tiles, HUD, start-game gate, level intro, and audio feedback.

## Completed

- Added initial hex cell representation with axial coordinates.
- Added `HexGridManager` for board registration and start/goal lookup.
- Added `GameState` and `TileActivationController` for planning-state interaction, move limits, reset, win, and fail resolution.
- Created `GameplayScene` and added it to Build Settings; `GameplayScene` is enabled and `SampleScene` is disabled.
- Switched placeholder visuals to real tile silhouettes and imported production art under `Assets/Art/`.
- Created reusable prefabs for start, goal, path, blocked, catapult, and health/move-bonus hex cells.
- Expanded the board to a 61-cell large pointy-top map with 5 cells per outer side.
- Corrected board spacing, scale, and camera framing so the 61-cell map fits the current camera view.
- Reworked activation to be Larry-centered so only tiles adjacent to Larry can be opened by normal clicks.
- Added Larry pawn visuals, current-cell tracking, reset support, jump SFX, hop animation, landing squash, and current-tile hover/shadow feedback.
- Added immediate movement after each valid activation.
- Added win condition when Larry reaches the goal tile.
- Added out-of-moves fail condition when the last available move is spent without reaching the goal.
- Added `GameHUD` with moves counter, level label, reset hint, outcome overlay, next-level/final-level action button, fading gameplay hint, and runtime start-game overlay with credits.
- Added `EventSystem` and UI click guard so gameplay clicks are blocked while the pointer is over UI.
- Added `LevelData` and `LevelLoader` so move limit, start, goal, per-cell state, visual variants, and level label are driven from data assets.
- Chose a single-scene level architecture: all levels use `GameplayScene` and are configured by `LevelData` assets.
- Added session-based level progression to `LevelLoader` through an ordered level list and `LoadNextLevel()`.
- Created and wired `Level_001.asset` through `Level_005.asset`.
- Added blocked tile variant support with `BlockedHexCell_01..03` and `LevelLoader.blockedVariantSprites`.
- Added catapult tile support with `HexCell.CatapultDirection`, catapult prefab variants for all six axial directions, runtime catapult launch resolution, catapult SFX, and a configurable chain limit.
- Added move-bonus/health tile support with one-time bonus consumption, reset restoration, health sprite support, and heal SFX.
- Added `BackgroundPresentationController` for menu/gameplay background tint, overlay, and camera-fitted background scaling.
- Added startup gate flow: board/Larry hidden in `PreGame`, `Start Game` overlay shown, gameplay starts after the button is clicked.
- Added level intro drop animation with intro SFX before each applied level unlocks input.
- Wired audio assets for background music, jump, catapult, heal, restart, lose, level complete, and intro shuffle.
- Simplified tile tinting so authored active/start/goal/blocked/catapult/health art keeps its original color, while inactive and highlighted states still receive visual filtering.
- Verified through Unity MCP inspection that `GameplayScene` has 61 registered cells, one `LevelLoader`, one `GameHUD`, one `EventSystem`, and no current console errors/warnings at inspection time.

## Architecture Snapshot

- `HexCell` owns coordinate, active/walkable/start/goal/highlight state, catapult state, move-bonus state, sprite/color handling, collider drive option, and sorting order logic.
- `HexGridManager` auto-collects child cells, rebuilds the coordinate registry, validates duplicates, and exposes start/goal/all-cell lookups.
- `TileActivationController` handles Input System mouse activation, keyboard reset, UI click guard, move accounting, outcome resolution, catapult chain resolution, move-bonus application, background music, and gameplay SFX.
- `LarryController` tracks Larry's current cell, resolves the initial start cell, animates hops, plays jump SFX, resets position, and renders the current-tile hover effect.
- `LevelData` stores level identity, move limit, start/goal coordinates, default active state, and per-cell overrides for blocked, initially active, catapult, move bonus, and visual variants.
- `LevelLoader` applies the selected `LevelData` to the shared board, captures the visual palette, binds the HUD, manages ordered progression, controls the startup gate, and plays level intro animation.
- `GameHUD` drives the live HUD, win/fail overlay, next-level/final-level action, fading hint, start-game overlay, and credits.
- `BackgroundPresentationController` manages the background sprite, menu overlay, gameplay/menu tint, zoom transition, and camera fitting.
- `GameHUDBuilder` remains as an editor utility for rebuilding the HUD in an active scene.
- Build Settings currently include disabled `SampleScene` and enabled `GameplayScene`.

## Current Level Set

| Level | Moves | Start | Goal | Blocked | Catapults | Move Bonuses |
| --- | ---: | --- | --- | ---: | ---: | ---: |
| `Level_001` | 8 | `(-4, 0)` | `(4, 0)` | 0 | 0 | 0 |
| `Level_002` | 9 | `(-4, 0)` | `(2, 2)` | 12 | 0 | 0 |
| `Level_003` | 7 | `(1, 3)` | `(2, -4)` | 6 | 8 | 0 |
| `Level_004` | 8 | `(4, -3)` | `(-4, 3)` | 6 | 4 | 4 |
| `Level_005` | 7 | `(-1, 3)` | `(0, -4)` | 6 | 4 | 2 |

## Immediate Next Steps

1. Manually playtest Levels 1-5 end to end and rebalance move limits, catapult placements, and health tile counts where needed.
2. Decide whether the moves counter should keep the current fixed dark-purple HUD color or restore the older normal/amber/red depletion color-shift behavior.
3. Do a controlled art/asset cleanup pass for informal tile filenames and any remaining placeholder-looking sprites while preserving Unity meta GUID references.
4. Tune camera/background/HUD framing for narrow aspect ratios and the final jam target resolution.
5. Run a final build smoke test and packaging checklist once balance and presentation are locked.

## Notes

- The core loop is currently implemented and playable: start gate, level intro, tile activation, Larry movement, special tiles, win/fail resolution, HUD feedback, reset, and multi-level progression work together in one scene.
- `GameState.Execution` still exists as an enum value, but the current design does not use a separate execution phase.
- Current documentation was synced against project scripts, level assets, prefabs, scene hierarchy, Build Settings, art/audio folders, and Unity console state on 2026-04-26.
