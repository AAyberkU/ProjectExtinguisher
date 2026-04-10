## Confirmed Bugs

- None currently confirmed.

## Open Technical Risks

- Board framing currently targets a normal landscape view; narrow aspect ratios or later UI overlays may require camera tuning or dynamic framing.
- Neighbor logic currently lives inside `TileActivationController`; if a second gameplay system starts using hex adjacency, it should be centralized to avoid rule drift.

## Fixed

- Fixed reset bug where pressing `R` did not fully restore planning state because stray duplicate support-tile instances could be activated outside the registered grid snapshot.
- Fixed HUD outcome panel (You Win / Out of Moves) not disappearing on reset; `ResetHUD()` is now called from `ResetPlanningState()` to clear `outcomeShown` and hide the panel.
