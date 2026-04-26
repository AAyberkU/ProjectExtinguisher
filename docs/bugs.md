## Confirmed Bugs

- None currently confirmed.

## Open Technical Risks

- Board framing currently targets the active landscape camera view; narrow aspect ratios, final jam resolution, or later UI overlays may require camera/background/HUD tuning.
- `GameHUD` still exposes normal/amber/critical move-color fields, but `RefreshMovesLabel()` currently applies one fixed border/HUD text color. Decide whether this is intentional style direction or a missing color-shift implementation.
- `TileActivationController` contains a startup background-music suppression helper, but the inspected startup flow does not call it. Confirm whether background music should play or stay muted on the start-game overlay.
- Catapult chains consume moves and can chain until the configured chain limit; Levels 3-5 need manual balance testing to ensure chains are readable, fair, and cannot create confusing fail states.
- Catapult, blocked, health, and path visuals depend on `LevelLoader` palette references staying synced with the prefab/art set.
- `LevelData.CellLevelState` currently reuses `usePathVariant` and `pathVariantIndex` for both path and blocked visual variants; future tile visual expansion may need clearer serialized fields.
- Neighbor adjacency logic currently lives inside `TileActivationController`; if another gameplay system starts using adjacency, centralize it to avoid rule drift.
- `GameState.Execution` is retained in the enum but unused by the current immediate-movement loop; future agents should not assume there is a separate execution phase.
- Several imported art filenames are informal jam placeholders. Rename only during a controlled Unity asset cleanup pass that preserves `.meta` GUID references.

## Fixed

- Fixed reset bug where pressing `R` did not fully restore planning state because stray duplicate support-tile instances could be activated outside the registered grid snapshot.
- Fixed HUD outcome panel (`You Win` / `Out of Moves`) not disappearing on reset; `ResetHUD()` is now called from `ResetPlanningState()` to clear `outcomeShown` and hide the panel.
