using System.Collections;
using ProjectExtinguisher.Gameplay;
using ProjectExtinguisher.Gameplay.Levels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectExtinguisher.UI
{
    /// <summary>
    /// Drives the in-game HUD: moves-left counter (with color shift), level label,
    /// reset hint, win/lose overlay, and a fading gameplay hint.
    /// Wire <see cref="controller"/> to the scene's <see cref="TileActivationController"/>.
    /// </summary>
    public sealed class GameHUD : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────────────
        // Inspector fields
        // ──────────────────────────────────────────────────────────────────────────

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs;

        [Header("References")]
        [SerializeField] private TileActivationController controller;
        [SerializeField] private LevelLoader levelLoader;

        [Header("Moves Left")]
        [SerializeField] private TMP_Text movesLeftLabel;
        [SerializeField] private Color movesColorNormal  = new Color(0.95f, 0.95f, 0.95f, 1f);
        [SerializeField] private Color movesColorAmber   = new Color(1.00f, 0.65f, 0.10f, 1f);
        [SerializeField] private Color movesColorCritical = new Color(0.95f, 0.18f, 0.18f, 1f);
        [Tooltip("Fraction of move limit at or below which the counter turns amber.")]
        [SerializeField] [Range(0.01f, 1f)] private float amberThreshold  = 0.50f;
        [Tooltip("Fraction of move limit at or below which the counter turns red.")]
        [SerializeField] [Range(0.01f, 1f)] private float criticalThreshold = 0.25f;

        [Header("Level Label")]
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private string levelDisplayName = "Level 1";

        [Header("Reset Hint")]
        [SerializeField] private TMP_Text resetHintLabel;
        [SerializeField] private string resetHintText = "R  Reset";

        [Header("Win / Lose Overlay")]
        [SerializeField] private CanvasGroup outcomePanel;
        [SerializeField] private TMP_Text outcomeHeadline;
        [SerializeField] private TMP_Text outcomeSubline;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private TMP_Text nextLevelButtonLabel;
        [SerializeField] private string winHeadline  = "You Win";
        [SerializeField] private string loseHeadline = "Out of Moves";
        [SerializeField] private string outcomeSublineText = "Press R to reset";
        [SerializeField] private string finalLevelSublineText = "Final Level - Press R to replay";
        [SerializeField] private string nextLevelButtonText = "Next Level";
        [SerializeField] [Min(0f)] private float outcomeFadeDuration = 0.45f;
        [SerializeField] private Vector2 outcomeSlideOffset = new Vector2(0f, -30f);

        [Header("Gameplay Hint")]
        [SerializeField] private TMP_Text gameplayHintLabel;
        [SerializeField] private string gameplayHintText = "Click a neighboring tile to move Larry";
        [SerializeField] [Min(0f)] private float hintVisibleDuration = 3.5f;
        [SerializeField] [Min(0f)] private float hintFadeDuration    = 0.8f;

        // ──────────────────────────────────────────────────────────────────────────
        // Private state
        // ──────────────────────────────────────────────────────────────────────────

        private bool outcomeShown;
        private Coroutine hintRoutine;
        private Coroutine outcomeRoutine;

        // ──────────────────────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────────────────────

        public void ResetHUD()
        {
            if (outcomeRoutine != null)
            {
                StopCoroutine(outcomeRoutine);
                outcomeRoutine = null;
            }

            SetOutcomePanelAlpha(0f);
            SetNextLevelButtonVisible(false);
            outcomeShown = false;

            if (gameplayHintLabel != null)
            {
                SetLabelAlpha(gameplayHintLabel, 1f);
                if (hintRoutine != null)
                {
                    StopCoroutine(hintRoutine);
                }
                hintRoutine = StartCoroutine(FadeHintOut());
            }

            RefreshMovesLabel();
            Log("HUD reset.");
        }

        public void SetLevelLabel(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return;
            }

            levelDisplayName = displayName;

            if (levelLabel != null)
            {
                levelLabel.text = levelDisplayName;
            }

            Log($"Level label set to '{levelDisplayName}'.");
        }

        public void BindLevelLoader(LevelLoader loader)
        {
            levelLoader = loader;
            UpdateNextLevelButtonLabel();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ──────────────────────────────────────────────────────────────────────────

        private void Reset()
        {
            TryCacheController();
            TryCacheLevelLoader();
        }

        private void Awake()
        {
            TryCacheController();
            TryCacheLevelLoader();
            RegisterNextLevelButton();
        }

        private void Start()
        {
            // Static / one-time labels
            if (levelLabel != null)
            {
                levelLabel.text = levelDisplayName;
            }

            if (resetHintLabel != null)
            {
                resetHintLabel.text = resetHintText;
            }

            // Hide outcome panel immediately
            SetOutcomePanelAlpha(0f);
            SetNextLevelButtonVisible(false);
            outcomeShown = false;

            // Show gameplay hint then fade it
            if (gameplayHintLabel != null)
            {
                gameplayHintLabel.text = gameplayHintText;
                SetLabelAlpha(gameplayHintLabel, 1f);
                hintRoutine = StartCoroutine(FadeHintOut());
            }

            // Initial moves display
            RefreshMovesLabel();

            Log("GameHUD started.");
        }

        private void Update()
        {
            if (controller == null)
            {
                return;
            }

            RefreshMovesLabel();
            CheckOutcome();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // HUD refresh helpers
        // ──────────────────────────────────────────────────────────────────────────

        private void RefreshMovesLabel()
        {
            if (movesLeftLabel == null || controller == null)
            {
                return;
            }

            int remaining = controller.PlanningMovesRemaining;
            int limit     = controller.PlanningMoveLimit;

            movesLeftLabel.text = $"Moves Left: {remaining:D2}";

            float fraction = limit > 0 ? (float)remaining / limit : 1f;
            if (fraction <= criticalThreshold)
            {
                movesLeftLabel.color = movesColorCritical;
            }
            else if (fraction <= amberThreshold)
            {
                movesLeftLabel.color = movesColorAmber;
            }
            else
            {
                movesLeftLabel.color = movesColorNormal;
            }
        }

        private void CheckOutcome()
        {
            if (outcomeShown)
            {
                return;
            }

            if (controller.HasWon)
            {
                ShowOutcome(isWin: true);
            }
            else if (controller.HasLost)
            {
                ShowOutcome(isWin: false);
            }
        }

        private void ShowOutcome(bool isWin)
        {
            outcomeShown = true;
            bool canLoadNextLevel = isWin && levelLoader != null && levelLoader.HasNextLevel;

            if (outcomeHeadline != null)
            {
                outcomeHeadline.text = isWin ? winHeadline : loseHeadline;
            }

            if (outcomeSubline != null)
            {
                outcomeSubline.text = isWin && !canLoadNextLevel ? finalLevelSublineText : outcomeSublineText;
            }

            SetNextLevelButtonVisible(canLoadNextLevel);

            if (outcomeRoutine != null)
            {
                StopCoroutine(outcomeRoutine);
            }

            outcomeRoutine = StartCoroutine(FadeInOutcome());
            Log($"Outcome shown: {(isWin ? "Win" : "Lose")}.");
        }

        private void OnDestroy()
        {
            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveListener(HandleNextLevelClicked);
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Coroutines
        // ──────────────────────────────────────────────────────────────────────────

        private IEnumerator FadeHintOut()
        {
            yield return new WaitForSeconds(hintVisibleDuration);

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, hintFadeDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / duration);
                SetLabelAlpha(gameplayHintLabel, alpha);
                yield return null;
            }

            SetLabelAlpha(gameplayHintLabel, 0f);
            hintRoutine = null;
        }

        private IEnumerator FadeInOutcome()
        {
            if (outcomePanel == null)
            {
                yield break;
            }

            // Capture the panel's RectTransform for the slide
            RectTransform rt = outcomePanel.GetComponent<RectTransform>();
            Vector2 anchoredEnd   = rt != null ? rt.anchoredPosition : Vector2.zero;
            Vector2 anchoredStart = anchoredEnd + outcomeSlideOffset;

            SetOutcomePanelAlpha(0f);
            if (rt != null)
            {
                rt.anchoredPosition = anchoredStart;
            }

            float elapsed  = 0f;
            float duration = Mathf.Max(0.01f, outcomeFadeDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smooth = t * t * (3f - 2f * t); // smoothstep
                SetOutcomePanelAlpha(smooth);
                if (rt != null)
                {
                    rt.anchoredPosition = Vector2.LerpUnclamped(anchoredStart, anchoredEnd, smooth);
                }

                yield return null;
            }

            SetOutcomePanelAlpha(1f);
            if (rt != null)
            {
                rt.anchoredPosition = anchoredEnd;
            }

            outcomeRoutine = null;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Utility
        // ──────────────────────────────────────────────────────────────────────────

        private void SetOutcomePanelAlpha(float alpha)
        {
            if (outcomePanel == null)
            {
                return;
            }

            outcomePanel.alpha          = alpha;
            outcomePanel.interactable   = alpha > 0.99f;
            outcomePanel.blocksRaycasts = alpha > 0.99f;
        }

        private static void SetLabelAlpha(TMP_Text label, float alpha)
        {
            if (label == null)
            {
                return;
            }

            Color c = label.color;
            c.a = alpha;
            label.color = c;
        }

        private void TryCacheController()
        {
            if (controller == null)
            {
                controller = FindFirstObjectByType<TileActivationController>();
            }
        }

        private void TryCacheLevelLoader()
        {
            if (levelLoader == null)
            {
                levelLoader = FindFirstObjectByType<LevelLoader>();
            }
        }

        private void RegisterNextLevelButton()
        {
            if (nextLevelButton == null)
            {
                return;
            }

            nextLevelButton.onClick.RemoveListener(HandleNextLevelClicked);
            nextLevelButton.onClick.AddListener(HandleNextLevelClicked);
            UpdateNextLevelButtonLabel();
        }

        private void HandleNextLevelClicked()
        {
            if (levelLoader == null)
            {
                return;
            }

            levelLoader.LoadNextLevel();
        }

        private void UpdateNextLevelButtonLabel()
        {
            if (nextLevelButtonLabel != null)
            {
                nextLevelButtonLabel.text = nextLevelButtonText;
            }
        }

        private void SetNextLevelButtonVisible(bool visible)
        {
            if (nextLevelButton == null)
            {
                return;
            }

            nextLevelButton.gameObject.SetActive(visible);
            nextLevelButton.interactable = visible;
        }

        private void Log(string message)
        {
            if (!enableDebugLogs)
            {
                return;
            }

            Debug.Log($"[GameHUD:{name}] {message}", this);
        }
    }
}
