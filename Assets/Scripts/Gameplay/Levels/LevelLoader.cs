using System.Collections;
using System.Collections.Generic;
using ProjectExtinguisher.Gameplay.Hex;
using ProjectExtinguisher.Gameplay.Larry;
using ProjectExtinguisher.UI;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField] private BackgroundPresentationController backgroundPresentation;

        [Header("Progression")]
        [SerializeField] private List<LevelData> orderedLevels = new();

        [Header("Application")]
        [SerializeField] private bool applySelectedLevelOnStart = true;
        [SerializeField] private bool showStartGameOnStartup = true;
        [SerializeField] private bool defaultInitialActiveState;

        [Header("Level Intro")]
        [SerializeField] private bool playLevelIntroOnLoad = true;
        [SerializeField] private Camera introCamera;
        [FormerlySerializedAs("introDropHeight")]
        [SerializeField] [Min(0f)] private float introOffscreenPadding = 2f;
        [SerializeField] [Min(0f)] private float introLineDelay = 0.04f;
        [FormerlySerializedAs("introFallDuration")]
        [SerializeField] [Min(0.01f)] private float introBaseFallDuration = 0.22f;
        [SerializeField] [Min(0.1f)] private float introSpeedMultiplier = 1f;
        [SerializeField] private AudioClip introStartClip;
        [SerializeField] [Min(0f)] private float introStartDelay;
        [SerializeField] [Range(0f, 1f)] private float introStartVolume = 1f;
        [SerializeField] private AnimationCurve introFallCurve = new(
            new Keyframe(0f, 0f, 0f, 2.8f),
            new Keyframe(1f, 1f, 2.1f, 0f));

        [Header("Visual Palette")]
        [SerializeField] private bool autoCaptureVisualPalette = true;
        [SerializeField] private Sprite defaultPathSprite;
        [SerializeField] private Sprite startSprite;
        [SerializeField] private Sprite goalSprite;
        [SerializeField] private Sprite blockedSprite;
        [SerializeField] private Sprite catapultSprite;
        [SerializeField] private Sprite healthSprite;
        [SerializeField] private List<Sprite> blockedVariantSprites = new();
        [SerializeField] private List<Sprite> catapultVariantSprites = new();
        [SerializeField] private List<Sprite> pathVariantSprites = new();

        private readonly Dictionary<Vector2Int, LevelData.CellLevelState> stateByCoordinate = new();
        private readonly Dictionary<HexCell, Vector3> introTargetPositions = new();
        private LevelData currentLevel;
        private LevelData startupLevelReference;
        private LevelData pendingStartupLevel;
        private int currentLevelIndex = -1;
        private bool lastApplyLevelSucceeded;
        private Coroutine levelIntroRoutine;
        private Coroutine introAudioRoutine;
        private AudioSource introAudioSource;
        private bool startupGatePending;

        public LevelData SelectedLevel => selectedLevel;
        public LevelData CurrentLevel => currentLevel;
        public int CurrentLevelIndex => currentLevelIndex;
        public bool HasNextLevel => TryGetNextLevel(out _);
        public bool IsStartupGatePending => startupGatePending;

        private void Reset()
        {
            CacheReferences();
            CacheStartupLevelReference();
        }

        private void Awake()
        {
            CacheReferences();
            CacheStartupLevelReference();

            if (ShouldUseStartupGateOnStartup())
            {
                PrepareStartupGateVisualState();
            }
        }

        private void OnDisable()
        {
            StopLevelIntroAnimation();
        }

        private void Start()
        {
            if (applySelectedLevelOnStart)
            {
                LevelData startupLevel = GetStartupLevelReference();
                if (startupLevel != null)
                {
                    if (ShouldUseStartupGateOnStartup())
                    {
                        PrepareStartupGate(startupLevel);
                    }
                    else
                    {
                        ApplyLevel(startupLevel);
                    }
                }
            }
        }

        private void OnValidate()
        {
            CacheReferences();
            CacheStartupLevelReference();
            introOffscreenPadding = Mathf.Max(0f, introOffscreenPadding);
            introLineDelay = Mathf.Max(0f, introLineDelay);
            introBaseFallDuration = Mathf.Max(0.01f, introBaseFallDuration);
            introSpeedMultiplier = Mathf.Max(0.1f, introSpeedMultiplier);
            introStartDelay = Mathf.Max(0f, introStartDelay);
            introStartVolume = Mathf.Clamp01(introStartVolume);
        }

        [ContextMenu("Apply Selected Level")]
        public void ApplySelectedLevel()
        {
            ApplyLevel(selectedLevel);
        }

        public bool ShouldUseStartupGateOnStartup()
        {
            return showStartGameOnStartup
                && applySelectedLevelOnStart
                && GetStartupLevelReference() != null;
        }

        public bool BeginStartupGame()
        {
            if (!startupGatePending || pendingStartupLevel == null)
            {
                return false;
            }

            LevelData startupLevel = pendingStartupLevel;

            if (backgroundPresentation != null)
            {
                backgroundPresentation.SetGameplayState();
            }

            ApplyLevel(startupLevel);

            if (!lastApplyLevelSucceeded)
            {
                pendingStartupLevel = startupLevel;
                startupGatePending = true;
                PrepareStartupGateVisualState();

                if (gameHUD != null)
                {
                    gameHUD.ShowStartGameOverlay();
                }

                return false;
            }

            startupGatePending = false;
            pendingStartupLevel = null;

            if (gameHUD != null)
            {
                gameHUD.HideStartGameOverlay();
            }

            return true;
        }

        public bool ReturnToStartupGate()
        {
            LevelData startupLevel = GetStartupLevelReference();
            if (startupLevel == null)
            {
                return false;
            }

            PrepareStartupGate(startupLevel);
            return startupGatePending;
        }

        private void PrepareStartupGate(LevelData startupLevel)
        {
            pendingStartupLevel = startupLevel;
            startupGatePending = true;

            PrepareStartupGateVisualState();

            if (gameHUD != null)
            {
                gameHUD.BindLevelLoader(this);
                if (gameHUD.ShowStartGameOverlay())
                {
                    return;
                }
            }

            BeginStartupGame();
        }

        private void PrepareStartupGateVisualState()
        {
            if (gridManager != null)
            {
                gridManager.RebuildRegistry();
            }

            SetGridVisualsVisible(false);

            if (larryController != null)
            {
                larryController.SetVisualVisible(false);
            }

            if (tileActivationController != null)
            {
                tileActivationController.SetGameState(GameState.PreGame);
                tileActivationController.SetInputLocked(true);
            }

            if (backgroundPresentation != null)
            {
                backgroundPresentation.SetMenuState();
            }
        }

        public bool LoadNextLevel()
        {
            if (!TryGetNextLevel(out LevelData nextLevel))
            {
                Log("LoadNextLevel skipped because there is no next level configured.");
                return false;
            }

            ApplyLevel(nextLevel);
            return lastApplyLevelSucceeded;
        }

        public bool TryGetNextLevel(out LevelData nextLevel)
        {
            nextLevel = null;

            if (orderedLevels.Count == 0)
            {
                return false;
            }

            int startIndex = currentLevelIndex >= 0 ? currentLevelIndex + 1 : 0;
            for (int index = startIndex; index < orderedLevels.Count; index++)
            {
                if (orderedLevels[index] == null)
                {
                    continue;
                }

                nextLevel = orderedLevels[index];
                return true;
            }

            return false;
        }

        public void ApplyLevel(LevelData level)
        {
            CacheReferences();
            StopLevelIntroAnimation();
            lastApplyLevelSucceeded = false;

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

            SetCurrentLevel(level);

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
                gameHUD.BindLevelLoader(this);
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

            if (ShouldPlayLevelIntro())
            {
                levelIntroRoutine = StartCoroutine(PlayLevelIntro());
            }
            else
            {
                SetGridVisualsVisible(true);

                if (tileActivationController != null)
                {
                    tileActivationController.SetInputLocked(false);
                }

                if (larryController != null)
                {
                    larryController.SetVisualVisible(true);
                }
            }

            lastApplyLevelSucceeded = true;
            Log($"Applied level '{level.GetDisplayName()}' with move limit {level.MoveLimit}.");
        }

        private bool ShouldPlayLevelIntro()
        {
            return Application.isPlaying
                && playLevelIntroOnLoad
                && introBaseFallDuration > 0f
                && gridManager != null
                && gridManager.CellCount > 0;
        }

        private IEnumerator PlayLevelIntro()
        {
            IReadOnlyList<HexCell> cells = gridManager.GetAllCells();
            if (cells == null || cells.Count == 0)
            {
                CompleteLevelIntro();
                yield break;
            }

            if (tileActivationController != null)
            {
                tileActivationController.SetInputLocked(true);
            }

            if (larryController != null)
            {
                larryController.SetVisualVisible(false);
            }

            PlayIntroStartAudio();

            Dictionary<int, List<HexCell>> cellsByLine = BuildIntroLines(cells);
            if (cellsByLine.Count == 0)
            {
                CompleteLevelIntro();
                yield break;
            }

            float introDuration = GetIntroFallDuration();

            introTargetPositions.Clear();

            for (int index = 0; index < cells.Count; index++)
            {
                HexCell cell = cells[index];
                if (cell == null)
                {
                    continue;
                }

                Vector3 targetPosition = cell.transform.position;
                introTargetPositions[cell] = targetPosition;
                cell.transform.position = ResolveIntroStartPosition(targetPosition);
            }

            SetGridVisualsVisible(true);

            List<int> orderedLines = new(cellsByLine.Keys);
            orderedLines.Sort();

            for (int lineIndex = 0; lineIndex < orderedLines.Count; lineIndex++)
            {
                List<HexCell> lineCells = cellsByLine[orderedLines[lineIndex]];
                for (int cellIndex = 0; cellIndex < lineCells.Count; cellIndex++)
                {
                    HexCell cell = lineCells[cellIndex];
                    if (cell == null || !introTargetPositions.TryGetValue(cell, out Vector3 targetPosition))
                    {
                        continue;
                    }

                    StartCoroutine(AnimateIntroDrop(cell, targetPosition, introDuration));
                }

                if (lineIndex < orderedLines.Count - 1 && introLineDelay > 0f)
                {
                    yield return new WaitForSeconds(introLineDelay);
                }
            }

            yield return new WaitForSeconds(introDuration);
            CompleteLevelIntro();
        }

        private IEnumerator AnimateIntroDrop(HexCell cell, Vector3 targetPosition, float duration)
        {
            if (cell == null)
            {
                yield break;
            }

            Vector3 startPosition = cell.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float easedTime = EvaluateIntroCurve(normalizedTime);
                cell.transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, easedTime);
                yield return null;
            }

            cell.transform.position = targetPosition;
        }

        private Dictionary<int, List<HexCell>> BuildIntroLines(IReadOnlyList<HexCell> cells)
        {
            Dictionary<int, List<HexCell>> cellsByLine = new();
            List<float> projections = new(cells.Count);
            List<float> cellProjections = new(cells.Count);
            float minimumProjection = float.MaxValue;

            for (int index = 0; index < cells.Count; index++)
            {
                HexCell cell = cells[index];
                if (cell == null)
                {
                    cellProjections.Add(0f);
                    continue;
                }

                float projection = GetIntroProjection(cell.transform.position);
                cellProjections.Add(projection);
                projections.Add(projection);
                minimumProjection = Mathf.Min(minimumProjection, projection);
            }

            if (projections.Count == 0)
            {
                return cellsByLine;
            }

            float lineSpacing = ResolveIntroLineSpacing(projections);

            for (int index = 0; index < cells.Count; index++)
            {
                HexCell cell = cells[index];
                if (cell == null)
                {
                    continue;
                }

                int lineIndex = Mathf.RoundToInt((cellProjections[index] - minimumProjection) / lineSpacing);
                if (!cellsByLine.TryGetValue(lineIndex, out List<HexCell> lineCells))
                {
                    lineCells = new List<HexCell>();
                    cellsByLine.Add(lineIndex, lineCells);
                }

                lineCells.Add(cell);
            }

            return cellsByLine;
        }

        private void StopLevelIntroAnimation()
        {
            bool hadActiveIntro = levelIntroRoutine != null || introAudioRoutine != null || introTargetPositions.Count > 0;

            if (levelIntroRoutine != null)
            {
                StopCoroutine(levelIntroRoutine);
                levelIntroRoutine = null;
            }

            if (introAudioRoutine != null)
            {
                StopCoroutine(introAudioRoutine);
                introAudioRoutine = null;
            }

            if (!hadActiveIntro)
            {
                return;
            }

            SnapIntroCellsToTargets();
            SetGridVisualsVisible(true);

            if (tileActivationController != null)
            {
                tileActivationController.SetInputLocked(false);
            }

            if (larryController != null)
            {
                larryController.SetVisualVisible(true);
            }
        }

        private void CompleteLevelIntro()
        {
            SnapIntroCellsToTargets();
            SetGridVisualsVisible(true);

            if (larryController != null)
            {
                larryController.ResetToInitialState();
                larryController.SetVisualVisible(true);
            }

            if (tileActivationController != null)
            {
                tileActivationController.SetInputLocked(false);
            }

            levelIntroRoutine = null;
        }

        private void SnapIntroCellsToTargets()
        {
            foreach (KeyValuePair<HexCell, Vector3> entry in introTargetPositions)
            {
                if (entry.Key != null)
                {
                    entry.Key.transform.position = entry.Value;
                }
            }

            introTargetPositions.Clear();
        }

        private void SetGridVisualsVisible(bool visible)
        {
            if (gridManager == null)
            {
                return;
            }

            IReadOnlyList<HexCell> cells = gridManager.GetAllCells();
            for (int index = 0; index < cells.Count; index++)
            {
                HexCell cell = cells[index];
                if (cell != null)
                {
                    cell.SetVisualVisible(visible);
                }
            }
        }

        private float GetIntroFallDuration()
        {
            return introBaseFallDuration / Mathf.Max(0.1f, introSpeedMultiplier);
        }

        private void PlayIntroStartAudio()
        {
            if (introStartClip == null || introStartVolume <= 0f)
            {
                return;
            }

            if (introAudioRoutine != null)
            {
                StopCoroutine(introAudioRoutine);
                introAudioRoutine = null;
            }

            introAudioRoutine = StartCoroutine(PlayIntroStartAudioRoutine());
        }

        private IEnumerator PlayIntroStartAudioRoutine()
        {
            if (introStartDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(introStartDelay);
            }

            EnsureIntroAudioSource();
            if (introAudioSource == null)
            {
                introAudioRoutine = null;
                yield break;
            }

            introAudioSource.PlayOneShot(introStartClip, introStartVolume);
            introAudioRoutine = null;
        }

        private void EnsureIntroAudioSource()
        {
            if (introAudioSource == null)
            {
                GameObject audioObject = new GameObject("Level Intro Audio Source");
                audioObject.transform.SetParent(transform, false);
                introAudioSource = audioObject.AddComponent<AudioSource>();
            }

            introAudioSource.playOnAwake = false;
            introAudioSource.loop = false;
            introAudioSource.spatialBlend = 0f;
            introAudioSource.pitch = 1f;
            introAudioSource.volume = 1f;
        }

        private Vector3 ResolveIntroStartPosition(Vector3 targetPosition)
        {
            float startY = targetPosition.y + introOffscreenPadding;
            Camera camera = ResolveIntroCamera();
            if (camera != null && camera.orthographic)
            {
                float cameraTopY = camera.transform.position.y + camera.orthographicSize;
                startY = Mathf.Max(startY, cameraTopY + introOffscreenPadding);
            }

            return new Vector3(targetPosition.x, startY, targetPosition.z);
        }

        private Camera ResolveIntroCamera()
        {
            if (introCamera == null)
            {
                introCamera = Camera.main;
            }

            return introCamera;
        }

        private float EvaluateIntroCurve(float normalizedTime)
        {
            if (introFallCurve == null || introFallCurve.length == 0)
            {
                return normalizedTime;
            }

            return introFallCurve.Evaluate(normalizedTime);
        }

        private static float GetIntroProjection(Vector3 position)
        {
            return position.x - position.y;
        }

        private static float ResolveIntroLineSpacing(List<float> projections)
        {
            projections.Sort();

            float smallestSpacing = float.MaxValue;
            const float minimumSpacing = 0.01f;

            for (int index = 1; index < projections.Count; index++)
            {
                float spacing = projections[index] - projections[index - 1];
                if (spacing > minimumSpacing && spacing < smallestSpacing)
                {
                    smallestSpacing = spacing;
                }
            }

            return smallestSpacing == float.MaxValue ? 1f : smallestSpacing;
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

                if (catapultSprite == null && cell.IsCatapult)
                {
                    catapultSprite = cell.CurrentSprite;
                }

                if (healthSprite == null && cell.IsMoveBonus)
                {
                    healthSprite = cell.CurrentSprite;
                }

                if (defaultPathSprite == null && cell.IsWalkable && !cell.IsStart && !cell.IsGoal && !cell.IsCatapult && !cell.IsMoveBonus)
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
            bool isCatapult = false;
            HexCell.CatapultDirection catapultDirection = HexCell.CatapultDirection.E;
            bool isMoveBonus = false;
            int moveBonusAmount = 1;
            bool useVisualVariant = false;
            int visualVariantIndex = -1;

            if (stateByCoordinate.TryGetValue(coordinate, out LevelData.CellLevelState overrideState))
            {
                walkable = !overrideState.blocked;
                active = overrideState.initiallyActive;
                isCatapult = overrideState.catapult;
                catapultDirection = overrideState.catapultDirection;
                isMoveBonus = overrideState.moveBonus;
                moveBonusAmount = Mathf.Max(1, overrideState.moveBonusAmount);
                useVisualVariant = overrideState.usePathVariant;
                visualVariantIndex = overrideState.pathVariantIndex;
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

            cell.ConfigureCatapult(isCatapult, catapultDirection);
            cell.ConfigureMoveBonus(isMoveBonus, moveBonusAmount);
            cell.SetBlockerVariantSortingIndex(walkable ? -1 : visualVariantIndex);
            cell.ApplyState(active, walkable, isStart, isGoal, false);
            ApplyCellSprite(cell, walkable, isStart, isGoal, isCatapult, catapultDirection, isMoveBonus, useVisualVariant, visualVariantIndex);
        }

        private void ApplyCellSprite(HexCell cell, bool walkable, bool isStart, bool isGoal, bool isCatapult, HexCell.CatapultDirection catapultDirection, bool isMoveBonus, bool useVisualVariant, int visualVariantIndex)
        {
            if (cell == null)
            {
                return;
            }

            Sprite sprite = null;

            if (isCatapult)
            {
                sprite = GetCatapultSprite(catapultDirection);
            }
            else if (isMoveBonus)
            {
                sprite = healthSprite;
            }
            else if (!walkable)
            {
                if (useVisualVariant && visualVariantIndex >= 0 && visualVariantIndex < blockedVariantSprites.Count)
                {
                    sprite = blockedVariantSprites[visualVariantIndex];
                }
                else if (blockedSprite != null)
                {
                    sprite = blockedSprite;
                }
                else if (blockedVariantSprites.Count > 0)
                {
                    sprite = blockedVariantSprites[0];
                }
            }
            else if (isStart)
            {
                sprite = startSprite;
            }
            else if (isGoal)
            {
                sprite = goalSprite;
            }
            else if (useVisualVariant && visualVariantIndex >= 0 && visualVariantIndex < pathVariantSprites.Count)
            {
                sprite = pathVariantSprites[visualVariantIndex];
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

        private Sprite GetCatapultSprite(HexCell.CatapultDirection direction)
        {
            int variantIndex = (int)direction;
            if (variantIndex >= 0 && variantIndex < catapultVariantSprites.Count)
            {
                Sprite variantSprite = catapultVariantSprites[variantIndex];
                if (variantSprite != null)
                {
                    return variantSprite;
                }
            }

            return catapultSprite;
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

            if (introCamera == null)
            {
                introCamera = Camera.main;
            }

            if (backgroundPresentation == null)
            {
                backgroundPresentation = FindFirstObjectByType<BackgroundPresentationController>();
            }

            if (gameHUD != null)
            {
                gameHUD.BindLevelLoader(this);
            }
        }

        private void CacheStartupLevelReference()
        {
            if (Application.isPlaying && startupLevelReference != null)
            {
                return;
            }

            startupLevelReference = ResolveConfiguredStartupLevel();
        }

        private LevelData GetStartupLevelReference()
        {
            if (startupLevelReference == null)
            {
                startupLevelReference = ResolveConfiguredStartupLevel();
            }

            return startupLevelReference;
        }

        private LevelData ResolveConfiguredStartupLevel()
        {
            if (selectedLevel != null)
            {
                return selectedLevel;
            }

            for (int index = 0; index < orderedLevels.Count; index++)
            {
                if (orderedLevels[index] != null)
                {
                    return orderedLevels[index];
                }
            }

            return null;
        }

        private void SetCurrentLevel(LevelData level)
        {
            currentLevel = level;
            selectedLevel = level;
            currentLevelIndex = FindLevelIndex(level);
        }

        private int FindLevelIndex(LevelData level)
        {
            if (level == null)
            {
                return -1;
            }

            for (int index = 0; index < orderedLevels.Count; index++)
            {
                if (orderedLevels[index] == level)
                {
                    return index;
                }
            }

            return -1;
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
