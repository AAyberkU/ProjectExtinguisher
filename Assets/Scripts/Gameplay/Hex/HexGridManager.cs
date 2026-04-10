using System.Collections.Generic;
using UnityEngine;

namespace ProjectExtinguisher.Gameplay.Hex
{
    public sealed class HexGridManager : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs;

        [Header("Collection")]
        [SerializeField] private bool autoCollectFromChildren = true;
        [SerializeField] private bool includeInactiveChildren = true;
        [SerializeField] private List<HexCell> cells = new();

        [Header("Validation")]
        [SerializeField] private bool warnOnNullCells = true;
        [SerializeField] private bool warnOnDuplicateCoordinates = true;
        [SerializeField] private bool enforceSingleStartCell = true;
        [SerializeField] private bool enforceSingleGoalCell = true;

        private readonly Dictionary<Vector2Int, HexCell> cellsByGridIndex = new();
        private readonly List<HexCell> orderedCells = new();

        private HexCell startCell;
        private HexCell goalCell;

        public int CellCount => orderedCells.Count;

        private void Reset()
        {
            CollectCellsFromChildren();
            RebuildRegistry();
        }

        private void Awake()
        {
            RebuildRegistry();
        }

        private void OnValidate()
        {
            if (autoCollectFromChildren)
            {
                CollectCellsFromChildren();
            }

            RebuildRegistry();
        }

        [ContextMenu("Rebuild Registry")]
        public void RebuildRegistry()
        {
            cellsByGridIndex.Clear();
            orderedCells.Clear();
            startCell = null;
            goalCell = null;

            List<HexCell> candidates = new(cells.Count);
            HashSet<HexCell> uniqueCells = new();

            for (int index = 0; index < cells.Count; index++)
            {
                HexCell cell = cells[index];
                if (cell == null)
                {
                    if (warnOnNullCells)
                    {
                        LogWarning($"Found a null cell reference at serialized index {index}.");
                    }

                    continue;
                }

                if (!uniqueCells.Add(cell))
                {
                    Log($"Skipped duplicate cell reference '{cell.name}'.");
                    continue;
                }

                candidates.Add(cell);
            }

            if (autoCollectFromChildren)
            {
                HexCell[] childCells = GetComponentsInChildren<HexCell>(includeInactiveChildren);
                for (int index = 0; index < childCells.Length; index++)
                {
                    HexCell childCell = childCells[index];
                    if (childCell == null || !uniqueCells.Add(childCell))
                    {
                        continue;
                    }

                    candidates.Add(childCell);
                }
            }

            for (int index = 0; index < candidates.Count; index++)
            {
                HexCell cell = candidates[index];
                Vector2Int gridIndex = cell.GridIndex;

                if (cellsByGridIndex.TryGetValue(gridIndex, out HexCell existingCell))
                {
                    if (warnOnDuplicateCoordinates)
                    {
                        LogWarning($"Duplicate grid index {gridIndex} found on '{cell.name}'. Keeping '{existingCell.name}'.");
                    }

                    continue;
                }

                cellsByGridIndex.Add(gridIndex, cell);
                orderedCells.Add(cell);
                RegisterSpecialCell(cell);
            }

            Log($"Registry rebuilt with {orderedCells.Count} unique cells.");
        }

        public bool TryGetCell(Vector2Int gridIndex, out HexCell cell)
        {
            return cellsByGridIndex.TryGetValue(gridIndex, out cell);
        }

        public HexCell GetCell(Vector2Int gridIndex)
        {
            if (TryGetCell(gridIndex, out HexCell cell))
            {
                return cell;
            }

            LogWarning($"No cell registered at grid index {gridIndex}.");
            return null;
        }

        public IReadOnlyList<HexCell> GetAllCells()
        {
            return orderedCells;
        }

        public HexCell GetStartCell()
        {
            return startCell;
        }

        public HexCell GetGoalCell()
        {
            return goalCell;
        }

        public void ClearHighlights()
        {
            int clearedCount = 0;

            for (int index = 0; index < orderedCells.Count; index++)
            {
                HexCell cell = orderedCells[index];
                if (cell == null || !cell.IsHighlighted)
                {
                    continue;
                }

                cell.SetHighlighted(false);
                clearedCount++;
            }

            Log($"Cleared highlights on {clearedCount} cells.");
        }

        private void CollectCellsFromChildren()
        {
            HexCell[] childCells = GetComponentsInChildren<HexCell>(includeInactiveChildren);
            cells.Clear();
            cells.AddRange(childCells);
            Log($"Collected {cells.Count} cells from children.");
        }

        private void RegisterSpecialCell(HexCell cell)
        {
            if (cell.IsStart)
            {
                if (startCell == null)
                {
                    startCell = cell;
                }
                else if (enforceSingleStartCell)
                {
                    LogWarning($"Multiple start cells found. Keeping '{startCell.name}' and ignoring '{cell.name}'.");
                }
                else
                {
                    Log($"Additional start cell '{cell.name}' detected.");
                }
            }

            if (cell.IsGoal)
            {
                if (goalCell == null)
                {
                    goalCell = cell;
                }
                else if (enforceSingleGoalCell)
                {
                    LogWarning($"Multiple goal cells found. Keeping '{goalCell.name}' and ignoring '{cell.name}'.");
                }
                else
                {
                    Log($"Additional goal cell '{cell.name}' detected.");
                }
            }
        }

        private void Log(string message)
        {
            if (!enableDebugLogs)
            {
                return;
            }

            Debug.Log($"[HexGridManager:{name}] {message}", this);
        }

        private void LogWarning(string message)
        {
            if (!enableDebugLogs)
            {
                return;
            }

            Debug.LogWarning($"[HexGridManager:{name}] {message}", this);
        }
    }
}
