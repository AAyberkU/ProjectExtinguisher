using System.Collections;
using UnityEngine;

namespace ProjectExtinguisher.UI
{
    public sealed class BackgroundPresentationController : MonoBehaviour
    {
        private const int BackgroundSortingOrder = -100;
        private const int OverlaySortingOrder = -99;

        public enum PresentationState
        {
            Gameplay,
            Menu
        }

        [Header("Background")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Color gameplayTint = Color.white;
        [SerializeField] private Camera targetCamera;

        [Header("Menu Overlay")]
        [SerializeField] private Color menuOverlayColor = new(0f, 0f, 0f, 0.45f);
        [SerializeField] [Min(1f)] private float menuZoomScale = 1.03f;
        [SerializeField] private Color menuBackgroundTint = new(0.7f, 0.7f, 0.7f, 1f);

        [Header("Transition")]
        [SerializeField] [Min(0f)] private float transitionDuration = 0.3f;

        [Header("State")]
        [SerializeField] private PresentationState initialState = PresentationState.Menu;

        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer overlayRenderer;
        private Transform backgroundRoot;
        private PresentationState currentState;
        private Coroutine transitionRoutine;

        private Color currentBackgroundTint;
        private Color currentOverlayColor;
        private float currentZoom;

        private void Awake()
        {
            CacheCamera();
            EnsureBackgroundObjects();
            currentState = initialState;
            ApplyStateImmediate(currentState);
        }

        private void LateUpdate()
        {
            FitBackgroundToCamera();
        }

        private void OnValidate()
        {
            CacheCamera();
            menuZoomScale = Mathf.Max(1f, menuZoomScale);
            transitionDuration = Mathf.Max(0f, transitionDuration);

            if (Application.isPlaying && backgroundRenderer != null)
            {
                backgroundRenderer.sprite = backgroundSprite;
                ApplyStateImmediate(currentState);
            }
        }

        public void SetState(PresentationState state)
        {
            if (currentState == state && transitionRoutine == null)
            {
                return;
            }

            currentState = state;

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            if (transitionDuration <= 0f || !Application.isPlaying)
            {
                ApplyStateImmediate(state);
                return;
            }

            transitionRoutine = StartCoroutine(AnimateTransition(state));
        }

        public void SetMenuState()
        {
            SetState(PresentationState.Menu);
        }

        public void SetGameplayState()
        {
            SetState(PresentationState.Gameplay);
        }

        private void ApplyStateImmediate(PresentationState state)
        {
            EnsureBackgroundObjects();

            if (state == PresentationState.Menu)
            {
                currentBackgroundTint = menuBackgroundTint;
                currentOverlayColor = menuOverlayColor;
                currentZoom = menuZoomScale;
            }
            else
            {
                currentBackgroundTint = gameplayTint;
                currentOverlayColor = new Color(0f, 0f, 0f, 0f);
                currentZoom = 1f;
            }

            ApplyVisuals();
        }

        private IEnumerator AnimateTransition(PresentationState targetState)
        {
            Color startTint = currentBackgroundTint;
            Color startOverlay = currentOverlayColor;
            float startZoom = currentZoom;

            Color endTint;
            Color endOverlay;
            float endZoom;

            if (targetState == PresentationState.Menu)
            {
                endTint = menuBackgroundTint;
                endOverlay = menuOverlayColor;
                endZoom = menuZoomScale;
            }
            else
            {
                endTint = gameplayTint;
                endOverlay = new Color(0f, 0f, 0f, 0f);
                endZoom = 1f;
            }

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, transitionDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smooth = t * t * (3f - 2f * t);

                currentBackgroundTint = Color.Lerp(startTint, endTint, smooth);
                currentOverlayColor = Color.Lerp(startOverlay, endOverlay, smooth);
                currentZoom = Mathf.Lerp(startZoom, endZoom, smooth);

                ApplyVisuals();
                yield return null;
            }

            currentBackgroundTint = endTint;
            currentOverlayColor = endOverlay;
            currentZoom = endZoom;
            ApplyVisuals();

            transitionRoutine = null;
        }

        private void ApplyVisuals()
        {
            if (backgroundRenderer != null)
            {
                backgroundRenderer.color = currentBackgroundTint;
            }

            if (overlayRenderer != null)
            {
                overlayRenderer.color = currentOverlayColor;
            }
        }

        private void FitBackgroundToCamera()
        {
            if (backgroundRenderer == null || backgroundRenderer.sprite == null)
            {
                return;
            }

            Camera camera = ResolveCamera();
            if (camera == null || !camera.orthographic)
            {
                return;
            }

            float cameraHeight = camera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * camera.aspect;

            Sprite sprite = backgroundRenderer.sprite;
            float spriteWidth = sprite.rect.width / sprite.pixelsPerUnit;
            float spriteHeight = sprite.rect.height / sprite.pixelsPerUnit;

            if (spriteWidth <= 0f || spriteHeight <= 0f)
            {
                return;
            }

            float scaleX = cameraWidth / spriteWidth;
            float scaleY = cameraHeight / spriteHeight;
            float fillScale = Mathf.Max(scaleX, scaleY) * currentZoom;

            Vector3 cameraPosition = camera.transform.position;
            if (backgroundRoot != null)
            {
                backgroundRoot.position = new Vector3(cameraPosition.x, cameraPosition.y, 10f);
                backgroundRoot.localScale = new Vector3(fillScale, fillScale, 1f);
            }
        }

        private void EnsureBackgroundObjects()
        {
            if (backgroundRoot != null && backgroundRenderer != null && overlayRenderer != null)
            {
                return;
            }

            if (backgroundRoot == null)
            {
                GameObject rootObject = new GameObject("Background Presentation Root");
                backgroundRoot = rootObject.transform;
                backgroundRoot.SetParent(transform, false);
            }

            if (backgroundRenderer == null)
            {
                GameObject bgObject = new GameObject("Background Sprite");
                bgObject.transform.SetParent(backgroundRoot, false);
                bgObject.transform.localPosition = Vector3.zero;

                backgroundRenderer = bgObject.AddComponent<SpriteRenderer>();
                backgroundRenderer.sprite = backgroundSprite;
                backgroundRenderer.sortingOrder = BackgroundSortingOrder;
                backgroundRenderer.drawMode = SpriteDrawMode.Simple;
            }

            if (overlayRenderer == null)
            {
                GameObject overlayObject = new GameObject("Background Overlay");
                overlayObject.transform.SetParent(backgroundRoot, false);
                overlayObject.transform.localPosition = Vector3.zero;

                overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
                overlayRenderer.sprite = backgroundSprite;
                overlayRenderer.sortingOrder = OverlaySortingOrder;
                overlayRenderer.drawMode = SpriteDrawMode.Simple;
                overlayRenderer.color = new Color(0f, 0f, 0f, 0f);
            }
        }

        private void CacheCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private Camera ResolveCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            return targetCamera;
        }
    }
}
