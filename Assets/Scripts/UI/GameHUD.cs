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
        private const string StartGameButtonText = "Start Game";
        private const string ReturnToMainMenuButtonText = "Return to Main Menu";
        private const string StartGameCreditsMarkup = "<color=#E4D8D4>Artist</color>\n<color=#E6A914>Elif Ilgin Tilev</color>\n\n<color=#E4D8D4>Designer</color>\n<color=#365CC3>Defne Gurcu</color>\n\n<color=#E4D8D4>Developer</color>\n<color=#BD2680>Ahmet Ayberk Uzun</color>\n\n<color=#E4D8D4>Music</color>\n<color=#7FB7A3>Onurhan Karabag</color>";
        private static readonly Color StartGameOverlayColor = new(0f, 0f, 0f, 0f);
        private static readonly Color StartGameButtonColor = new(0.1f, 0.14f, 0.18f, 0.96f);
        private static readonly Color StartGameLabelColor = new(0.97f, 0.97f, 0.97f, 1f);
        private static readonly Color BorderHudTextColor = new Color32(0x37, 0x02, 0x5A, 0xFF);
        private static readonly Vector2 StartGameButtonSize = new(320f, 96f);
        private static readonly Vector2 StartGameButtonPosition = new(0f, 210f);
        private static readonly Vector2 StartGameCreditsPanelSize = new(520f, 430f);
        private static readonly Vector2 StartGameCreditsPanelPosition = new(0f, -95f);

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

        [Header("Font Override")]
        [SerializeField] private TMP_FontAsset fontOverride;

        // ──────────────────────────────────────────────────────────────────────────
        // Private state
        // ──────────────────────────────────────────────────────────────────────────

        private bool outcomeShown;
        private Coroutine hintRoutine;
        private Coroutine outcomeRoutine;
        private CanvasGroup startGameOverlay;
        private Button startGameButton;
        private TextMeshProUGUI startGameButtonLabel;
        private TextMeshProUGUI startGameCreditsLabel;
        private bool startGameOverlayVisible;

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

        public bool ShowStartGameOverlay()
        {
            EnsureStartGameOverlay();
            if (startGameOverlay == null)
            {
                return false;
            }

            startGameOverlayVisible = true;

            if (hintRoutine != null)
            {
                StopCoroutine(hintRoutine);
                hintRoutine = null;
            }

            if (outcomeRoutine != null)
            {
                StopCoroutine(outcomeRoutine);
                outcomeRoutine = null;
            }

            SetLabelAlpha(movesLeftLabel, 0f);
            SetLabelAlpha(levelLabel, 0f);
            SetLabelAlpha(resetHintLabel, 0f);
            SetLabelAlpha(gameplayHintLabel, 0f);
            SetOutcomePanelAlpha(0f);
            SetNextLevelButtonVisible(false);

            if (startGameButtonLabel != null)
            {
                startGameButtonLabel.text = StartGameButtonText;
            }

            startGameOverlay.gameObject.SetActive(true);
            startGameOverlay.transform.SetAsLastSibling();
            startGameOverlay.alpha = 1f;
            startGameOverlay.interactable = true;
            startGameOverlay.blocksRaycasts = true;

            if (startGameButton != null)
            {
                startGameButton.interactable = true;
            }

            return true;
        }

        public void HideStartGameOverlay()
        {
            if (startGameOverlay == null)
            {
                return;
            }

            startGameOverlayVisible = false;

            startGameOverlay.alpha = 0f;
            startGameOverlay.interactable = false;
            startGameOverlay.blocksRaycasts = false;
            startGameOverlay.gameObject.SetActive(false);

            SetLabelAlpha(movesLeftLabel, 1f);
            SetLabelAlpha(levelLabel, 1f);
            SetLabelAlpha(resetHintLabel, 1f);
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
            ApplyFontOverride();
            ApplyBorderLabelColors();

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

            movesLeftLabel.text = $"Moves Left: {remaining:D2}";

            movesLeftLabel.color = BorderHudTextColor;

            SetLabelAlpha(movesLeftLabel, startGameOverlayVisible ? 0f : 1f);
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
            bool canReturnToMainMenu = isWin && levelLoader != null && !levelLoader.HasNextLevel;

            if (outcomeHeadline != null)
            {
                outcomeHeadline.text = isWin ? winHeadline : loseHeadline;
            }

            if (outcomeSubline != null)
            {
                outcomeSubline.text = isWin && !canLoadNextLevel ? finalLevelSublineText : outcomeSublineText;
            }

            SetNextLevelButtonVisible(canLoadNextLevel || canReturnToMainMenu);
            UpdateOutcomeActionButtonLabel(canLoadNextLevel, canReturnToMainMenu);

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

            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveListener(HandleStartGameClicked);
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

        private void HandleStartGameClicked()
        {
            TryCacheLevelLoader();
            if (startGameButton != null)
            {
                startGameButton.interactable = false;
            }

            if (levelLoader != null && levelLoader.BeginStartupGame())
            {
                return;
            }

            if (startGameButton != null)
            {
                startGameButton.interactable = true;
            }
        }

        private void HandleNextLevelClicked()
        {
            if (levelLoader == null)
            {
                return;
            }

            if (controller != null && controller.HasWon && !levelLoader.HasNextLevel)
            {
                levelLoader.ReturnToStartupGate();
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

        private void UpdateOutcomeActionButtonLabel(bool canLoadNextLevel, bool canReturnToMainMenu)
        {
            if (nextLevelButtonLabel == null)
            {
                return;
            }

            if (canReturnToMainMenu)
            {
                nextLevelButtonLabel.text = ReturnToMainMenuButtonText;
                return;
            }

            nextLevelButtonLabel.text = canLoadNextLevel ? nextLevelButtonText : nextLevelButtonText;
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

        private void EnsureStartGameOverlay()
        {
            if (startGameOverlay != null && startGameButton != null && startGameButtonLabel != null && startGameCreditsLabel != null)
            {
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform parent = canvas != null ? canvas.transform as RectTransform : transform as RectTransform;
            if (parent == null)
            {
                return;
            }

            GameObject overlayObject = new GameObject("Start Game Overlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            RectTransform overlayTransform = overlayObject.GetComponent<RectTransform>();
            overlayTransform.SetParent(parent, false);
            overlayTransform.anchorMin = Vector2.zero;
            overlayTransform.anchorMax = Vector2.one;
            overlayTransform.offsetMin = Vector2.zero;
            overlayTransform.offsetMax = Vector2.zero;

            Image overlayImage = overlayObject.GetComponent<Image>();
            overlayImage.color = StartGameOverlayColor;

            startGameOverlay = overlayObject.GetComponent<CanvasGroup>();

            GameObject buttonObject = new GameObject("Start Game Button", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform buttonTransform = buttonObject.GetComponent<RectTransform>();
            buttonTransform.SetParent(overlayTransform, false);
            buttonTransform.anchorMin = new Vector2(0.5f, 0.5f);
            buttonTransform.anchorMax = new Vector2(0.5f, 0.5f);
            buttonTransform.pivot = new Vector2(0.5f, 0.5f);
            buttonTransform.sizeDelta = StartGameButtonSize;
            buttonTransform.anchoredPosition = StartGameButtonPosition;

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = StartGameButtonColor;

            startGameButton = buttonObject.GetComponent<Button>();
            startGameButton.targetGraphic = buttonImage;
            startGameButton.onClick.RemoveListener(HandleStartGameClicked);
            startGameButton.onClick.AddListener(HandleStartGameClicked);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelTransform = labelObject.GetComponent<RectTransform>();
            labelTransform.SetParent(buttonTransform, false);
            labelTransform.anchorMin = Vector2.zero;
            labelTransform.anchorMax = Vector2.one;
            labelTransform.offsetMin = new Vector2(16f, 10f);
            labelTransform.offsetMax = new Vector2(-16f, -10f);

            startGameButtonLabel = labelObject.GetComponent<TextMeshProUGUI>();
            startGameButtonLabel.alignment = TextAlignmentOptions.Center;
            startGameButtonLabel.color = StartGameLabelColor;
            startGameButtonLabel.enableAutoSizing = true;
            startGameButtonLabel.fontSizeMin = 18f;
            startGameButtonLabel.fontSizeMax = 42f;
            startGameButtonLabel.text = StartGameButtonText;

            TMP_FontAsset fontAsset = ResolveHudFontAsset();
            if (fontAsset != null)
            {
                startGameButtonLabel.font = fontAsset;
            }

            GameObject creditsPanelObject = new GameObject("Start Game Credits Panel", typeof(RectTransform), typeof(Image));
            RectTransform creditsPanelTransform = creditsPanelObject.GetComponent<RectTransform>();
            creditsPanelTransform.SetParent(overlayTransform, false);
            creditsPanelTransform.anchorMin = new Vector2(0.5f, 0.5f);
            creditsPanelTransform.anchorMax = new Vector2(0.5f, 0.5f);
            creditsPanelTransform.pivot = new Vector2(0.5f, 0.5f);
            creditsPanelTransform.sizeDelta = StartGameCreditsPanelSize;
            creditsPanelTransform.anchoredPosition = StartGameCreditsPanelPosition;

            Image creditsPanelImage = creditsPanelObject.GetComponent<Image>();
            creditsPanelImage.color = StartGameButtonColor;

            GameObject creditsLabelObject = new GameObject("Credits Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform creditsLabelTransform = creditsLabelObject.GetComponent<RectTransform>();
            creditsLabelTransform.SetParent(creditsPanelTransform, false);
            creditsLabelTransform.anchorMin = Vector2.zero;
            creditsLabelTransform.anchorMax = Vector2.one;
            creditsLabelTransform.offsetMin = new Vector2(26f, 22f);
            creditsLabelTransform.offsetMax = new Vector2(-26f, -22f);

            startGameCreditsLabel = creditsLabelObject.GetComponent<TextMeshProUGUI>();
            startGameCreditsLabel.alignment = TextAlignmentOptions.Center;
            startGameCreditsLabel.enableAutoSizing = true;
            startGameCreditsLabel.fontSizeMin = 18f;
            startGameCreditsLabel.fontSizeMax = 36f;
            startGameCreditsLabel.lineSpacing = 6f;
            startGameCreditsLabel.text = StartGameCreditsMarkup;

            if (fontAsset != null)
            {
                startGameCreditsLabel.font = fontAsset;
            }

            HideStartGameOverlay();
        }

        private TMP_FontAsset ResolveHudFontAsset()
        {
            if (fontOverride != null)
            {
                return fontOverride;
            }

            if (levelLabel != null)
            {
                return levelLabel.font;
            }

            if (movesLeftLabel != null)
            {
                return movesLeftLabel.font;
            }

            if (gameplayHintLabel != null)
            {
                return gameplayHintLabel.font;
            }

            if (outcomeHeadline != null)
            {
                return outcomeHeadline.font;
            }

            return TMP_Settings.defaultFontAsset;
        }

        private void ApplyFontOverride()
        {
            if (fontOverride == null)
            {
                return;
            }

            ApplyFontToLabel(movesLeftLabel);
            ApplyFontToLabel(levelLabel);
            ApplyFontToLabel(resetHintLabel);
            ApplyFontToLabel(outcomeHeadline);
            ApplyFontToLabel(outcomeSubline);
            ApplyFontToLabel(nextLevelButtonLabel);
            ApplyFontToLabel(gameplayHintLabel);
            ApplyFontToLabel(startGameButtonLabel);
            ApplyFontToLabel(startGameCreditsLabel);
        }

        private void ApplyBorderLabelColors()
        {
            ApplyColorToLabel(movesLeftLabel);
            ApplyColorToLabel(levelLabel);
            ApplyColorToLabel(resetHintLabel);
            ApplyColorToLabel(gameplayHintLabel);
        }

        private void ApplyFontToLabel(TMP_Text label)
        {
            if (label != null && fontOverride != null)
            {
                label.font = fontOverride;
            }
        }

        private void ApplyColorToLabel(TMP_Text label)
        {
            if (label != null)
            {
                label.color = BorderHudTextColor;
            }
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
