using System.Collections;
using System.Collections.Generic;
using ProjectExtinguisher.Gameplay.Hex;
using UnityEngine;

namespace ProjectExtinguisher.Gameplay.Larry
{
    public sealed class LarryController : MonoBehaviour
    {
        private const int LarrySortingOrder = 10;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs;

        [Header("References")]
        [SerializeField] private HexGridManager gridManager;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer visualSpriteRenderer;

        [Header("Placement")]
        [SerializeField] private Vector3 cellVisualOffset = new(0f, 0.18f, -0.5f);
        [SerializeField] private HexCell currentCell;

        [Header("Movement")]
        [SerializeField] [Min(0.01f)] private float moveDuration = 0.22f;
        [SerializeField] [Min(0f)] private float hopHeight = 0.35f;
        [SerializeField] private AnimationCurve moveProgressCurve = new(
            new Keyframe(0f, 0f, 0f, 2.6f),
            new Keyframe(1f, 1f, 2.1f, 0f));
        [SerializeField] private AnimationCurve hopHeightCurve = new(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.5f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, 0f, 0f));

        [Header("Audio")]
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] [Range(0f, 1f)] private float jumpVolume = 1f;
        [SerializeField] private AudioSource jumpAudioSource;

        [Header("Settle")]
        [SerializeField] [Min(0f)] private float settleDuration = 0.1f;
        [SerializeField] [Range(0f, 0.3f)] private float landingSquash = 0.08f;
        [SerializeField] private AnimationCurve settleCurve = new(
            new Keyframe(0f, 0f, 0f, 4f),
            new Keyframe(0.35f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -3f, 0f));

        private HexCell initialCell;
        private bool hasCapturedInitialState;
        private Coroutine moveRoutine;
        private Vector3 cachedVisualLocalScale = Vector3.one;
        private bool hasCachedVisualScale;

        public HexCell CurrentCell => currentCell;
        public bool IsMoving => moveRoutine != null;

        private void Reset()
        {
            CacheReferences();
            RefreshVisualSorting();
        }

        private void Awake()
        {
            CacheReferences();
            RefreshVisualSorting();
            EnsureAudioSource();
            CaptureInitialState();
        }

        private void OnDisable()
        {
            StopActiveMove(snapToCurrentCell: true);
        }

        private void OnValidate()
        {
            CacheReferences();
            RefreshVisualSorting();
            jumpVolume = Mathf.Clamp01(jumpVolume);

            if (Application.isPlaying)
            {
                EnsureAudioSource();
            }

            if (!Application.isPlaying && currentCell != null)
            {
                SnapVisualToCell(currentCell);
            }
        }

        [ContextMenu("Capture Initial State")]
        public void CaptureInitialState()
        {
            StopActiveMove(snapToCurrentCell: true);

            if (gridManager == null)
            {
                LogWarning("Cannot capture Larry state because the grid manager reference is missing.");
                return;
            }

            gridManager.RebuildRegistry();

            initialCell = ResolveInitialCell();
            hasCapturedInitialState = initialCell != null;

            if (!hasCapturedInitialState)
            {
                currentCell = null;
                LogWarning("Larry state capture could not find a valid start cell.");
                return;
            }

            currentCell = initialCell;
            SnapVisualToCell(currentCell);
            Log($"Captured Larry initial cell {DescribeCell(initialCell)}.");
        }

        [ContextMenu("Reset To Initial State")]
        public void ResetToInitialState()
        {
            StopActiveMove(snapToCurrentCell: false);

            if (!hasCapturedInitialState)
            {
                CaptureInitialState();
            }

            if (!hasCapturedInitialState)
            {
                LogWarning("Larry reset skipped because no initial cell was captured.");
                return;
            }

            currentCell = initialCell;
            SnapVisualToCell(currentCell);
            Log($"Larry reset to {DescribeCell(currentCell)}.");
        }

        public bool MoveToCell(HexCell targetCell)
        {
            return MoveToCell(targetCell, true);
        }

        public void SetVisualVisible(bool visible)
        {
            if (visualSpriteRenderer != null)
            {
                visualSpriteRenderer.enabled = visible;
            }
        }

        public bool MoveToCell(HexCell targetCell, bool playJumpAudio)
        {
            if (!IsRegisteredGridCell(targetCell))
            {
                LogWarning($"Larry move skipped because {DescribeCell(targetCell)} is not a registered cell.");
                return false;
            }

            if (moveRoutine != null)
            {
                Log($"Larry move to {DescribeCell(targetCell)} ignored because a hop is already in progress.");
                return false;
            }

            if (currentCell == targetCell)
            {
                SnapVisualToCell(currentCell);
                Log($"Larry move ignored because he is already at {DescribeCell(currentCell)}.");
                return true;
            }

            HexCell previousCell = currentCell;

            currentCell = targetCell;

            if (playJumpAudio)
            {
                PlayJumpAudio();
            }

            if (previousCell == null || visualRoot == null || moveDuration <= 0f || hopHeight <= 0f)
            {
                SnapVisualToCell(currentCell);
                Log($"Larry moved to {DescribeCell(currentCell)}.");
                return true;
            }

            moveRoutine = StartCoroutine(AnimateMove(currentCell));
            Log($"Larry started hop from {DescribeCell(previousCell)} to {DescribeCell(currentCell)}.");
            return true;
        }

        private void CacheReferences()
        {
            if (gridManager == null)
            {
                gridManager = GetComponentInParent<HexGridManager>();
            }

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            CacheVisualScale();

            if (visualSpriteRenderer == null)
            {
                visualSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (jumpAudioSource == null)
            {
                jumpAudioSource = GetComponent<AudioSource>();
            }
        }

        private void EnsureAudioSource()
        {
            if (jumpAudioSource != null)
            {
                ConfigureAudioSource(jumpAudioSource);
                return;
            }

            jumpAudioSource = GetComponent<AudioSource>();
            if (jumpAudioSource == null)
            {
                jumpAudioSource = gameObject.AddComponent<AudioSource>();
            }

            ConfigureAudioSource(jumpAudioSource);
        }

        private void RefreshVisualSorting()
        {
            if (visualSpriteRenderer != null)
            {
                visualSpriteRenderer.sortingOrder = LarrySortingOrder;
            }
        }

        private void ConfigureAudioSource(AudioSource audioSource)
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private void PlayJumpAudio()
        {
            EnsureAudioSource();

            if (jumpAudioSource == null || jumpClip == null || jumpVolume <= 0f)
            {
                return;
            }

            jumpAudioSource.PlayOneShot(jumpClip, jumpVolume);
        }

        private HexCell ResolveInitialCell()
        {
            HexCell startCell = gridManager.GetStartCell();
            if (startCell != null && startCell.IsActive)
            {
                return startCell;
            }

            IReadOnlyList<HexCell> cells = gridManager.GetAllCells();
            for (int index = 0; index < cells.Count; index++)
            {
                HexCell cell = cells[index];
                if (cell != null && cell.IsStart)
                {
                    return cell;
                }
            }

            for (int index = 0; index < cells.Count; index++)
            {
                HexCell cell = cells[index];
                if (cell != null && cell.IsActive && cell.IsWalkable)
                {
                    return cell;
                }
            }

            return null;
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

        private void SnapVisualToCell(HexCell cell)
        {
            if (visualRoot == null || cell == null)
            {
                return;
            }

            RestoreVisualScale();
            visualRoot.position = cell.transform.position + cellVisualOffset;
        }

        private IEnumerator AnimateMove(HexCell targetCell)
        {
            Vector3 startPosition = visualRoot.position;
            Vector3 endPosition = targetCell.transform.position + cellVisualOffset;
            float duration = Mathf.Max(0.01f, moveDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float horizontalT = EvaluateCurve(moveProgressCurve, normalizedTime);
                float verticalT = EvaluateCurve(hopHeightCurve, normalizedTime);
                Vector3 position = Vector3.LerpUnclamped(startPosition, endPosition, horizontalT);
                position.y += hopHeight * verticalT;
                visualRoot.position = position;
                yield return null;
            }

            visualRoot.position = endPosition;

            if (landingSquash > 0f && settleDuration > 0f)
            {
                float settleElapsed = 0f;
                float settleTime = Mathf.Max(0.01f, settleDuration);

                while (settleElapsed < settleTime)
                {
                    settleElapsed += Time.deltaTime;
                    float normalizedTime = Mathf.Clamp01(settleElapsed / settleTime);
                    ApplyLandingScale(EvaluateCurve(settleCurve, normalizedTime));
                    yield return null;
                }
            }

            RestoreVisualScale();
            moveRoutine = null;
            Log($"Larry finished hop to {DescribeCell(targetCell)}.");
        }

        private void StopActiveMove(bool snapToCurrentCell)
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
                moveRoutine = null;
            }

            RestoreVisualScale();

            if (snapToCurrentCell)
            {
                SnapVisualToCell(currentCell);
            }
        }

        private void CacheVisualScale()
        {
            if (visualRoot == null)
            {
                return;
            }

            cachedVisualLocalScale = visualRoot.localScale;
            hasCachedVisualScale = true;
        }

        private void RestoreVisualScale()
        {
            if (visualRoot == null)
            {
                return;
            }

            if (!hasCachedVisualScale)
            {
                CacheVisualScale();
            }

            visualRoot.localScale = cachedVisualLocalScale;
        }

        private void ApplyLandingScale(float normalizedAmount)
        {
            if (visualRoot == null)
            {
                return;
            }

            if (!hasCachedVisualScale)
            {
                CacheVisualScale();
            }

            float squash = landingSquash * Mathf.Clamp01(normalizedAmount);
            float horizontal = 1f + squash;
            float vertical = 1f - squash;
            visualRoot.localScale = new Vector3(
                cachedVisualLocalScale.x * horizontal,
                cachedVisualLocalScale.y * vertical,
                cachedVisualLocalScale.z);
        }

        private static float EvaluateCurve(AnimationCurve curve, float normalizedTime)
        {
            return curve == null || curve.length == 0 ? normalizedTime : curve.Evaluate(normalizedTime);
        }

        private string DescribeCell(HexCell cell)
        {
            return cell == null ? "none" : $"'{cell.name}' at {cell.GridIndex}";
        }

        private void Log(string message)
        {
            if (!enableDebugLogs)
            {
                return;
            }

            Debug.Log($"[LarryController:{name}] {message}", this);
        }

        private void LogWarning(string message)
        {
            if (!enableDebugLogs)
            {
                return;
            }

            Debug.LogWarning($"[LarryController:{name}] {message}", this);
        }
    }
}
