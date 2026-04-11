// Editor-only utility: run once via the menu to build the HUD in the active scene.
// Safe to delete after the HUD is in place.
using ProjectExtinguisher.Gameplay;
using ProjectExtinguisher.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace ProjectExtinguisher.Editor
{
    public static class GameHUDBuilder
    {
        [MenuItem("ProjectExtinguisher/Build HUD in Active Scene")]
        public static void BuildHUD()
        {
            // Remove any existing HUD canvas to avoid duplicates
            var existing = GameObject.Find("HUD_Canvas");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
                Debug.Log("[GameHUDBuilder] Removed existing HUD_Canvas.");
            }

            // ── 1. Canvas ─────────────────────────────────────────────────────
            var canvasGO = new GameObject("HUD_Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;

            // Keep raycaster enabled so outcome actions remain clickable.
            var raycaster = canvasGO.AddComponent<GraphicRaycaster>();
            raycaster.enabled = true;

            // ── 2. Top-left: Moves Left ───────────────────────────────────────
            var movesLabel = MakeLabel(
                "MovesLeftLabel", canvasGO.transform,
                "Moves Left: 03", 52f,
                TextAlignmentOptions.TopLeft,
                new Color(0.95f, 0.95f, 0.95f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -20f),
                new Vector2(480f, 70f));

            // ── 3. Top-center: Level label ────────────────────────────────────
            var levelLabel = MakeLabel(
                "LevelLabel", canvasGO.transform,
                "Level 1", 28f,
                TextAlignmentOptions.Top,
                new Color(0.85f, 0.85f, 0.85f, 0.85f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -20f),
                new Vector2(300f, 50f));

            // ── 4. Top-right: Reset hint ──────────────────────────────────────
            var resetLabel = MakeLabel(
                "ResetHintLabel", canvasGO.transform,
                "R  Reset", 26f,
                TextAlignmentOptions.TopRight,
                new Color(0.80f, 0.80f, 0.80f, 0.75f),
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -20f),
                new Vector2(200f, 50f));

            // ── 5. Center: Outcome overlay panel ─────────────────────────────
            var panelGO = new GameObject("OutcomePanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRT = panelGO.AddComponent<RectTransform>();
            panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRT.pivot            = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta        = new Vector2(560f, 260f);

            var panelImg = panelGO.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.05f, 0.05f, 0.72f);
            panelImg.raycastTarget = false;

            var panelCG = panelGO.AddComponent<CanvasGroup>();
            panelCG.alpha          = 0f;
            panelCG.interactable   = false;
            panelCG.blocksRaycasts = false;

            var headlineLabel = MakeLabel(
                "OutcomeHeadline", panelGO.transform,
                "You Win", 58f,
                TextAlignmentOptions.Center,
                Color.white,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 30f),
                new Vector2(0f, 80f));

            var sublineLabel = MakeLabel(
                "OutcomeSubline", panelGO.transform,
                "Press R to reset", 26f,
                TextAlignmentOptions.Center,
                new Color(0.85f, 0.85f, 0.85f, 1f),
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f),
                new Vector2(0f, 40f));

            var nextLevelButton = MakeButton(
                "NextLevelButton", panelGO.transform,
                "Next Level",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 36f),
                new Vector2(240f, 56f),
                out var nextLevelButtonLabel);

            nextLevelButton.gameObject.SetActive(false);

            // ── 6. Bottom-center: Gameplay hint ───────────────────────────────
            var hintLabel = MakeLabel(
                "GameplayHintLabel", canvasGO.transform,
                "Click a neighboring tile to move Larry", 24f,
                TextAlignmentOptions.Bottom,
                new Color(0.80f, 0.80f, 0.80f, 0.90f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 28f),
                new Vector2(800f, 50f));

            // ── 7. Attach GameHUD and wire references ─────────────────────────
            var hud = canvasGO.AddComponent<GameHUD>();
            var so  = new SerializedObject(hud);

            so.FindProperty("movesLeftLabel").objectReferenceValue    = movesLabel;
            so.FindProperty("levelLabel").objectReferenceValue        = levelLabel;
            so.FindProperty("resetHintLabel").objectReferenceValue    = resetLabel;
            so.FindProperty("outcomePanel").objectReferenceValue      = panelCG;
            so.FindProperty("outcomeHeadline").objectReferenceValue   = headlineLabel;
            so.FindProperty("outcomeSubline").objectReferenceValue    = sublineLabel;
            so.FindProperty("nextLevelButton").objectReferenceValue   = nextLevelButton;
            so.FindProperty("nextLevelButtonLabel").objectReferenceValue = nextLevelButtonLabel;
            so.FindProperty("gameplayHintLabel").objectReferenceValue = hintLabel;

            var controller = Object.FindFirstObjectByType<TileActivationController>();
            if (controller != null)
            {
                so.FindProperty("controller").objectReferenceValue = controller;
            }
            else
            {
                Debug.LogWarning("[GameHUDBuilder] TileActivationController not found in scene. Wire it manually.");
            }

            so.ApplyModifiedProperties();

            // ── 8. Mark scene dirty ───────────────────────────────────────────
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log($"[GameHUDBuilder] HUD_Canvas built. Controller: {(controller != null ? controller.name : "NOT FOUND")}.");
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private static TMP_Text MakeLabel(
            string goName, Transform parent, string text, float fontSize,
            TextAlignmentOptions align, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.alignment = align;
            tmp.color     = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button MakeButton(
            string goName, Transform parent, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta,
            out TMP_Text label)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.92f, 0.92f, 0.92f, 0.96f);

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(0.92f, 0.98f, 0.92f, 1f);
            colors.pressedColor = new Color(0.78f, 0.90f, 0.78f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
            button.colors = colors;
            button.targetGraphic = image;

            label = MakeLabel(
                "Label", go.transform,
                text, 24f,
                TextAlignmentOptions.Center,
                new Color(0.08f, 0.08f, 0.08f, 1f),
                Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);

            if (label.TryGetComponent(out RectTransform labelRect))
            {
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            return button;
        }
    }
}
