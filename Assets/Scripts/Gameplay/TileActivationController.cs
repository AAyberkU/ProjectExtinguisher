using System.Collections;
using System.Collections.Generic;
using ProjectExtinguisher.Gameplay.Hex;
using ProjectExtinguisher.Gameplay.Larry;
using ProjectExtinguisher.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

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
            public bool IsMoveBonusConsumed;
        }

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs;

        [Header("References")]
        [SerializeField] private HexGridManager gridManager;
        [SerializeField] private LarryController larryController;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private GameHUD gameHUD;

        [Header("State")]
        [SerializeField] private GameState currentGameState = GameState.Planning;
        [SerializeField] private int planningMoveLimit = 3;
        [SerializeField] private int planningMovesRemaining = 3;
        [SerializeField] private bool hasWon;
        [SerializeField] private bool hasLost;
        [FormerlySerializedAs("requireFrontierAdjacency")]
        [SerializeField] private bool requireLarryAdjacency = true;

        [Header("Input")]
        [SerializeField] private bool allowMouseActivation = true;
        [SerializeField] private bool allowKeyboardReset = true;

        private readonly List<CellPlanningSnapshot> initialSnapshots = new();
        private bool hasCapturedInitialState;
        private Coroutine movementResolutionRoutine;

        [Header("Movement Effects")]
        [SerializeField] [Min(1)] private int maxCatapultChainCount = 8;

        public GameState CurrentGameState => currentGameState;
        public bool HasWon => hasWon;
        public bool HasLost => hasLost;
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

        public void ApplyLoadedLevelSetup(int moveLimit)
        {
            StopMovementResolution();
            planningMoveLimit = Mathf.Max(0, moveLimit);
            planningMovesRemaining = planningMoveLimit;
            hasWon = false;
            hasLost = false;
            currentGameState = GameState.Planning;

            CaptureInitialPlanningState();

            if (gameHUD != null)
            {
                gameHUD.ResetHUD();
            }

            Log($"Applied loaded level setup with move limit {planningMoveLimit}.");
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
                    IsHighlighted = cell.IsHighlighted,
                    IsMoveBonusConsumed = cell.IsMoveBonusConsumed
                });
            }

            hasCapturedInitialState = initialSnapshots.Count > 0;
            planningMovesRemaining = planningMoveLimit;
            hasWon = false;
            hasLost = false;

            if (larryController != null)
            {
                larryController.CaptureInitialState();
            }
            else
            {
                LogWarning("Captured board state without a LarryController reference.");
            }

            Log($"Captured planning snapshot for {initialSnapshots.Count} cells.");
        }

        [ContextMenu("Reset Planning State")]
        public void ResetPlanningState()
        {
            StopMovementResolution();

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
                snapshot.Cell.SetMoveBonusConsumed(snapshot.IsMoveBonusConsumed);
            }

            planningMovesRemaining = planningMoveLimit;
            hasWon = false;
            hasLost = false;
            SetGameState(GameState.Planning);

            if (gridManager != null)
            {
                gridManager.RebuildRegistry();
            }

            if (larryController != null)
            {
                larryController.ResetToInitialState();
            }

            if (gameHUD != null)
            {
                gameHUD.ResetHUD();
            }

            Log($"Planning state reset. Remaining moves restored to {planningMovesRemaining}. Larry: {DescribeCell(GetLarryCurrentCell())}.");
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
            if (!allowMouseActivation)
            {
                return;
            }

            if (hasWon || hasLost || currentGameState != GameState.Planning || movementResolutionRoutine != null)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Log("Ignored planning click because the pointer is over UI.");
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

            HexCell larryCell = GetLarryCurrentCell();
            if (larryCell == null)
            {
                LogWarning("Activation skipped because Larry does not currently occupy a registered cell.");
                return;
            }

            if (requireLarryAdjacency && !IsAdjacentToCell(larryCell, cell))
            {
                Log($"Ignored non-adjacent cell '{cell.name}' at {cell.GridIndex}. Larry is at {DescribeCell(larryCell)}.");
                return;
            }

            if (movementResolutionRoutine != null || (larryController != null && larryController.IsMoving))
            {
                Log($"Ignored click on '{cell.name}' at {cell.GridIndex} because Larry is already mid-hop.");
                return;
            }

            if (larryController != null && !larryController.MoveToCell(cell))
            {
                LogWarning($"Larry could not move onto '{cell.name}' at {cell.GridIndex}.");
                return;
            }

            cell.SetActive(true);
            planningMovesRemaining--;

            Log($"Activated '{cell.name}' at {cell.GridIndex}. Remaining moves: {planningMovesRemaining}. Larry started moving to {DescribeCell(cell)}.");
            movementResolutionRoutine = StartCoroutine(ResolveMovementSequence(cell));
        }

        private IEnumerator ResolveMovementSequence(HexCell landedCell)
        {
            yield return WaitForLarryHopToFinish();

            HexCell finalCell = landedCell;
            int chainCount = 0;

            while (finalCell != null)
            {
                TryApplyLandingMoveBonus(finalCell);

                if (!finalCell.IsCatapult || chainCount >= maxCatapultChainCount)
                {
                    break;
                }

                if (!TryResolveCatapultDestination(finalCell, out HexCell launchDestination))
                {
                    break;
                }

                if (planningMovesRemaining <= 0)
                {
                    Log($"Catapult at {DescribeCell(finalCell)} could not launch Larry because no planning moves remain.");
                    break;
                }

                if (!launchDestination.IsActive)
                {
                    launchDestination.SetActive(true);
                }

                if (larryController == null || !larryController.MoveToCell(launchDestination))
                {
                    LogWarning($"Catapult launch from {DescribeCell(finalCell)} to {DescribeCell(launchDestination)} could not start.");
                    break;
                }

                planningMovesRemaining--;
                chainCount++;
                Log($"Catapult chain {chainCount} launched Larry from {DescribeCell(finalCell)} to {DescribeCell(launchDestination)}. Remaining moves: {planningMovesRemaining}.");

                yield return WaitForLarryHopToFinish();
                finalCell = launchDestination;
            }

            if (finalCell != null && finalCell.IsCatapult && chainCount >= maxCatapultChainCount)
            {
                LogWarning($"Stopped catapult resolution after reaching chain limit {maxCatapultChainCount} at {DescribeCell(finalCell)}.");
            }

            movementResolutionRoutine = null;
            FinalizeMoveOutcome(finalCell);
        }

        private IEnumerator WaitForLarryHopToFinish()
        {
            while (larryController != null && larryController.IsMoving)
            {
                yield return null;
            }
        }

        private void TryApplyLandingMoveBonus(HexCell landedCell)
        {
            if (landedCell == null || !landedCell.TryConsumeMoveBonus(out int bonusAmount))
            {
                return;
            }

            planningMovesRemaining += bonusAmount;
            Log($"Move bonus tile at {DescribeCell(landedCell)} granted {bonusAmount} move(s). Remaining moves: {planningMovesRemaining}.");
        }

        private bool TryResolveCatapultDestination(HexCell catapultCell, out HexCell destinationCell)
        {
            destinationCell = null;

            if (catapultCell == null || !catapultCell.IsCatapult)
            {
                return false;
            }

            Vector2Int landingIndex = catapultCell.GridIndex + (catapultCell.GetCatapultOffset() * catapultCell.CatapultLaunchDistance);
            if (gridManager == null || !gridManager.TryGetCell(landingIndex, out destinationCell) || destinationCell == null)
            {
                Log($"Catapult at {DescribeCell(catapultCell)} could not launch Larry because landing cell {landingIndex} is off-board.");
                destinationCell = null;
                return false;
            }

            if (!destinationCell.IsWalkable)
            {
                Log($"Catapult at {DescribeCell(catapultCell)} could not launch Larry because landing cell {DescribeCell(destinationCell)} is blocked.");
                destinationCell = null;
                return false;
            }

            return true;
        }

        private void FinalizeMoveOutcome(HexCell finalCell)
        {
            Log($"Movement resolved on {DescribeCell(finalCell)}. Remaining moves: {planningMovesRemaining}.");

            if (finalCell != null && finalCell.IsGoal)
            {
                EnterWinState(finalCell);
                return;
            }

            if (planningMovesRemaining == 0)
            {
                EnterFailState(finalCell);
            }
        }

        private void EnterWinState(HexCell goalCell)
        {
            if (hasWon)
            {
                return;
            }

            hasWon = true;
            SetGameState(GameState.Resolution);
            Log($"Larry reached the goal at {DescribeCell(goalCell)}. Win state entered. Tile activation input is now locked until reset.");
        }

        private void EnterFailState(HexCell finalCell)
        {
            if (hasWon || hasLost)
            {
                return;
            }

            hasLost = true;
            SetGameState(GameState.Resolution);
            Log($"Larry ran out of moves after activating {DescribeCell(finalCell)}. Fail state entered. Tile activation input is now locked until reset.");
        }

        private void CacheReferences()
        {
            if (gridManager == null)
            {
                gridManager = GetComponent<HexGridManager>();
            }

            if (larryController == null)
            {
                larryController = GetComponentInChildren<LarryController>(true);
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (gameHUD == null)
            {
                gameHUD = FindFirstObjectByType<GameHUD>();
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

        private HexCell GetLarryCurrentCell()
        {
            return larryController == null ? null : larryController.CurrentCell;
        }

        private bool IsAdjacentToCell(HexCell origin, HexCell candidate)
        {
            if (origin == null || candidate == null)
            {
                return false;
            }

            Vector2Int originIndex = origin.GridIndex;
            Vector2Int candidateIndex = candidate.GridIndex;

            for (int index = 0; index < 6; index++)
            {
                if (originIndex + HexCell.GetAxialOffset((HexCell.CatapultDirection)index) == candidateIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private string DescribeCell(HexCell cell)
        {
            return cell == null ? "none" : $"'{cell.name}' at {cell.GridIndex}";
        }

        private void SyncPlanningMovesForEditor()
        {
            planningMovesRemaining = planningMoveLimit;
        }

        private void StopMovementResolution()
        {
            if (movementResolutionRoutine == null)
            {
                return;
            }

            StopCoroutine(movementResolutionRoutine);
            movementResolutionRoutine = null;
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
