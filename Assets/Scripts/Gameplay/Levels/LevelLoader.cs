using System.Collections.Generic;
using ProjectExtinguisher.Gameplay.Hex;
using ProjectExtinguisher.Gameplay.Larry;
using ProjectExtinguisher.UI;
using UnityEngine;

namespace ProjectExtinguisher.Gameplay.Levels
{
    public sealed class LevelLoader : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs;

        [Header("References")]
        [SerializeField] private LevelData selectedLevel;
        [SerializeField] private HexGridManager gridManager;
        [SerializeField] private TileActivationController tileActivationController;
        [SerializeField] private LarryController larryController;
        [SerializeField] private GameHUD gameHUD;

        [Header("Application")]
        [SerializeField] private bool applySelectedLevelOnStart = true;
        [SerializeField] private bool defaultInitialActiveState;

        [Header("Visual Palette")]
        [SerializeField] private bool autoCaptureVisualPalette = true;
        [SerializeField] private Sprite defaultPathSprite;
        [SerializeField] private Sprite startSprite;
        [SerializeField] private Sprite goalSprite;
        [SerializeField] private Sprite blockedSprite;
        [SerializeField] private List<Sprite> pathVariantSprites = new();

        private readonly Dictionary<Vector2Int, LevelData.CellLevelState> stateByCoordinate = new();

        public LevelData SelectedLevel => selectedLevel;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void Start()
        {
            if (applySelectedLevelOnStart && selectedLevel != null)
            {
                ApplySelectedLevel();
            }
        }

        private void OnValidate()
        {
            CacheReferences();
        }

        [ContextMenu("Apply Selected Level")]
        public void ApplySelectedLevel()
        {
            ApplyLevel(selectedLevel);
        }

        public void ApplyLevel(LevelData level)
        {
            CacheReferences();

            if (level == null)
            {
                LogWarning("Level apply skipped because no LevelData asset is selected.");
                return;
            }

            if (gridManager == null)
            {
                LogWarning("Level apply skipped because the HexGridManager reference is missing.");
                return;
            }

            gridManager.RebuildRegistry();

            if (autoCaptureVisualPalette)
            {
                CaptureVisualPaletteFromGrid();
            }

            BuildCellStateLookup(level);

            if (!gridManager.TryGetCell(level.StartCell, out HexCell startCell) || startCell == null)
            {
                LogWarning($"Level apply skipped because start cell {level.StartCell} is not on the board.");
                return;
            }

            if (!gridManager.TryGetCell(level.GoalCell, out HexCell goalCell) || goalCell == null)
            {
                LogWarning($"Level apply skipped because goal cell {level.GoalCell} is not on the board.");
                return;
            }

            IReadOnlyList<HexCell> cells = gridManager.GetAllCells();
            for (int index = 0; index < cells.Count; index++)
            {
                HexCell cell = cells[index];
                if (cell == null)
                {
                    continue;
                }

                ApplyCellState(level, cell);
            }

            gridManager.RebuildRegistry();

            if (gameHUD != null)
            {
                gameHUD.SetLevelLabel(level.GetDisplayName());
            }

            if (tileActivationController != null)
            {
                tileActivationController.ApplyLoadedLevelSetup(level.MoveLimit);
            }
            else if (larryController != null)
            {
                larryController.CaptureInitialState();
            }

            Log($"Applied level '{level.GetDisplayName()}' with move limit {level.MoveLimit}.");
        }

        [ContextMenu("Capture Visual Palette")]
        public void CaptureVisualPaletteFromGrid()
        {
            if (gridManager == null)
            {
                return;
            }

            gridManager.RebuildRegistry();

            if (startSprite == null)
            {
                HexCell startCell = gridManager.GetStartCell();
                if (startCell != null)
                {
                    startSprite = startCell.CurrentSprite;
                }
            }

            if (goalSprite == null)
            {
                HexCell goalCell = gridManager.GetGoalCell();
                if (goalCell != null)
                {
                    goalSprite = goalCell.CurrentSprite;
                }
            }

            IReadOnlyList<HexCell> cells = gridManager.GetAllCells();
            for (int index = 0; index < cells.Count; index++)
            {
                HexCell cell = cells[index];
                if (cell == null)
                {
                    continue;
                }

                if (blockedSprite == null && !cell.IsWalkable)
                {
                    blockedSprite = cell.CurrentSprite;
                }

                if (defaultPathSprite == null && cell.IsWalkable && !cell.IsStart && !cell.IsGoal)
                {
                    defaultPathSprite = cell.CurrentSprite;
                }
            }
        }

        private void ApplyCellState(LevelData level, HexCell cell)
        {
            Vector2Int coordinate = cell.GridIndex;
            bool isStart = coordinate == level.StartCell;
            bool isGoal = coordinate == level.GoalCell;
            bool active = level.DefaultInitialActiveState || defaultInitialActiveState;
            bool walkable = true;
            bool usePathVariant = false;
            int pathVariantIndex = -1;

            if (stateByCoordinate.TryGetValue(coordinate, out LevelData.CellLevelState overrideState))
            {
                walkable = !overrideState.blocked;
                active = overrideState.initiallyActive;
                usePathVariant = overrideState.usePathVariant;
                pathVariantIndex = overrideState.pathVariantIndex;
            }

            if (isStart)
            {
                walkable = true;
                active = true;
            }
            else if (isGoal)
            {
                walkable = true;
            }

            cell.ApplyState(active, walkable, isStart, isGoal, false);
            ApplyCellSprite(cell, walkable, isStart, isGoal, usePathVariant, pathVariantIndex);
        }

        private void ApplyCellSprite(HexCell cell, bool walkable, bool isStart, bool isGoal, bool usePathVariant, int pathVariantIndex)
        {
            Sprite sprite = null;

            if (!walkable)
            {
                sprite = blockedSprite;
            }
            else if (isStart)
            {
                sprite = startSprite;
            }
            else if (isGoal)
            {
                sprite = goalSprite;
            }
            else if (usePathVariant && pathVariantIndex >= 0 && pathVariantIndex < pathVariantSprites.Count)
            {
                sprite = pathVariantSprites[pathVariantIndex];
            }
            else
            {
                sprite = defaultPathSprite;
            }

            if (sprite != null)
            {
                cell.SetVisualSprite(sprite);
            }
        }

        private void BuildCellStateLookup(LevelData level)
        {
            stateByCoordinate.Clear();

            IReadOnlyList<LevelData.CellLevelState> cellStates = level.CellStates;
            for (int index = 0; index < cellStates.Count; index++)
            {
                LevelData.CellLevelState cellState = cellStates[index];
                stateByCoordinate[cellState.coordinate] = cellState;
            }
        }

        private void CacheReferences()
        {
            if (gridManager == null)
            {
                gridManager = GetComponent<HexGridManager>();
            }

            if (tileActivationController == null)
            {
                tileActivationController = GetComponent<TileActivationController>();
            }

            if (larryController == null)
            {
                larryController = GetComponentInChildren<LarryController>(true);
            }

            if (gameHUD == null)
            {
                gameHUD = FindFirstObjectByType<GameHUD>();
            }
        }

        private void Log(string message)
        {
            if (!enableDebugLogs)
            {
                return;
            }

            Debug.Log($"[LevelLoader:{name}] {message}", this);
        }

        private void LogWarning(string message)
        {
            if (!enableDebugLogs)
            {
                return;
            }

            Debug.LogWarning($"[LevelLoader:{name}] {message}", this);
        }
    }
}
