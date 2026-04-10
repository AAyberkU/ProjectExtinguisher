using System.Collections.Generic;
using ProjectExtinguisher.Gameplay.Hex;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectExtinguisher.Gameplay
{
    public sealed class TileActivationController : MonoBehaviour
    {
        private struct CellPlanningSnapshot
        {
            public HexCell Cell;
            public bool IsActive;
            public bool IsWalkable;
            public bool IsStart;
            public bool IsGoal;
            public bool IsHighlighted;
        }

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs;

        [Header("References")]
        [SerializeField] private HexGridManager gridManager;
        [SerializeField] private Camera targetCamera;

        [Header("State")]
        [SerializeField] private GameState currentGameState = GameState.Planning;
        [SerializeField] private int planningMoveLimit = 3;
        [SerializeField] private int planningMovesRemaining = 3;

        [Header("Input")]
        [SerializeField] private bool allowMouseActivation = true;
        [SerializeField] private bool allowKeyboardReset = true;

        private readonly List<CellPlanningSnapshot> initialSnapshots = new();
        private bool hasCapturedInitialState;

        public GameState CurrentGameState => currentGameState;
        public int PlanningMoveLimit => planningMoveLimit;
        public int PlanningMovesRemaining => planningMovesRemaining;

        private void Reset()
        {
            CacheReferences();
            SyncPlanningMovesForEditor();
        }

        private void Awake()
        {
            CacheReferences();
            SyncPlanningMovesForEditor();
            CaptureInitialPlanningState();
        }

        private void OnValidate()
        {
            CacheReferences();
            planningMoveLimit = Mathf.Max(0, planningMoveLimit);

            if (!Application.isPlaying)
            {
                SyncPlanningMovesForEditor();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            HandleResetInput();
            HandlePlanningClick();
        }

        public void SetGameState(GameState nextState)
        {
            if (currentGameState == nextState)
            {
                return;
            }

            GameState previousState = currentGameState;
            currentGameState = nextState;
            Log($"Game state changed from {previousState} to {currentGameState}.");
        }

        [ContextMenu("Capture Initial Planning State")]
        public void CaptureInitialPlanningState()
        {
            if (gridManager == null)
            {
                LogWarning("Cannot capture planning state because the grid manager reference is missing.");
                return;
            }

            gridManager.RebuildRegistry();
            initialSnapshots.Clear();

            IReadOnlyList<HexCell> cells = gridManager.GetAllCells();
            for (int index = 0; index < cells.Count; index++)
            {
                HexCell cell = cells[index];
                if (cell == null)
                {
                    continue;
                }

                initialSnapshots.Add(new CellPlanningSnapshot
                {
                    Cell = cell,
                    IsActive = cell.IsActive,
                    IsWalkable = cell.IsWalkable,
                    IsStart = cell.IsStart,
                    IsGoal = cell.IsGoal,
                    IsHighlighted = cell.IsHighlighted
                });
            }

            hasCapturedInitialState = initialSnapshots.Count > 0;
            planningMovesRemaining = planningMoveLimit;
            Log($"Captured planning snapshot for {initialSnapshots.Count} cells.");
        }

        [ContextMenu("Reset Planning State")]
        public void ResetPlanningState()
        {
            if (!hasCapturedInitialState)
            {
                CaptureInitialPlanningState();
            }

            if (!hasCapturedInitialState)
            {
                LogWarning("Planning reset skipped because no initial snapshot is available.");
                return;
            }

            for (int index = 0; index < initialSnapshots.Count; index++)
            {
                CellPlanningSnapshot snapshot = initialSnapshots[index];
                if (snapshot.Cell == null)
                {
                    continue;
                }

                snapshot.Cell.ApplyState(
                    snapshot.IsActive,
                    snapshot.IsWalkable,
                    snapshot.IsStart,
                    snapshot.IsGoal,
                    snapshot.IsHighlighted);
            }

            planningMovesRemaining = planningMoveLimit;
            currentGameState = GameState.Planning;

            if (gridManager != null)
            {
                gridManager.RebuildRegistry();
            }

            Log($"Planning state reset. Remaining moves restored to {planningMovesRemaining}.");
        }

        private void HandleResetInput()
        {
            if (!allowKeyboardReset)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.rKey.wasPressedThisFrame)
            {
                return;
            }

            ResetPlanningState();
        }

        private void HandlePlanningClick()
        {
            if (!allowMouseActivation || currentGameState != GameState.Planning)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (targetCamera == null)
            {
                LogWarning("Cannot process tile click because the target camera reference is missing.");
                return;
            }

            Vector3 worldPoint = targetCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            Collider2D hitCollider = Physics2D.OverlapPoint(new Vector2(worldPoint.x, worldPoint.y));

            if (hitCollider == null || !hitCollider.TryGetComponent(out HexCell cell))
            {
                Log("Left click did not hit a HexCell collider.");
                return;
            }

            TryActivateCell(cell);
        }

        private void TryActivateCell(HexCell cell)
        {
            if (cell == null)
            {
                LogWarning("Activation skipped because the clicked cell reference was null.");
                return;
            }

            if (!IsRegisteredGridCell(cell))
            {
                LogWarning($"Ignored unregistered cell '{cell.name}' at {cell.GridIndex}.");
                return;
            }

            if (!cell.IsWalkable)
            {
                Log($"Ignored blocked cell '{cell.name}' at {cell.GridIndex}.");
                return;
            }

            if (cell.IsActive)
            {
                Log($"Ignored already active cell '{cell.name}' at {cell.GridIndex}. Remaining moves: {planningMovesRemaining}.");
                return;
            }

            if (planningMovesRemaining <= 0)
            {
                Log($"No planning moves remaining. Could not activate '{cell.name}' at {cell.GridIndex}.");
                return;
            }

            cell.SetActive(true);
            planningMovesRemaining--;
            Log($"Activated '{cell.name}' at {cell.GridIndex}. Remaining moves: {planningMovesRemaining}.");
        }

        private void CacheReferences()
        {
            if (gridManager == null)
            {
                gridManager = GetComponent<HexGridManager>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private bool IsRegisteredGridCell(HexCell cell)
        {
            if (gridManager == null || cell == null)
            {
                return false;
            }

            if (!gridManager.TryGetCell(cell.GridIndex, out HexCell registeredCell))
            {
                return false;
            }

            return registeredCell == cell;
        }

        private void SyncPlanningMovesForEditor()
        {
            planningMovesRemaining = planningMoveLimit;
        }

        private void Log(string message)
        {
            if (!enableDebugLogs)
            {
                return;
            }

            Debug.Log($"[TileActivationController:{name}] {message}", this);
        }

        private void LogWarning(string message)
        {
            if (!enableDebugLogs)
            {
                return;
            }

            Debug.LogWarning($"[TileActivationController:{name}] {message}", this);
        }
    }
}
