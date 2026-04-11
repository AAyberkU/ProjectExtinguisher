using UnityEngine;

namespace ProjectExtinguisher.Gameplay.Hex
{
    public sealed class HexCell : MonoBehaviour
    {
        public enum CatapultDirection
        {
            E = 0,
            NE = 1,
            NW = 2,
            W = 3,
            SW = 4,
            SE = 5
        }

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs;

        [Header("Coordinates")]
        [SerializeField] private Vector2Int gridIndex;

        [Header("State")]
        [SerializeField] private bool isActive;
        [SerializeField] private bool isWalkable = true;
        [SerializeField] private bool isStart;
        [SerializeField] private bool isGoal;
        [SerializeField] private bool isHighlighted;

        [Header("Catapult")]
        [SerializeField] private bool isCatapult;
        [SerializeField] private CatapultDirection catapultDirection;
        [SerializeField] [Min(1)] private int catapultLaunchDistance = 2;

        [Header("Visual References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D cellCollider;
        [SerializeField] private bool driveSpriteColor = true;
        [SerializeField] private bool driveColliderEnabledState;

        [Header("Visual Colors")]
        [SerializeField] private Color inactiveColor = new(0.45f, 0.48f, 0.5f, 1f);
        [SerializeField] private Color highlightColor = new(0.35f, 0.85f, 1f, 1f);

        public Vector2Int GridIndex => gridIndex;
        public int Column => gridIndex.x;
        public int Row => gridIndex.y;
        public bool IsActive => isActive;
        public bool IsWalkable => isWalkable;
        public bool IsStart => isStart;
        public bool IsGoal => isGoal;
        public bool IsHighlighted => isHighlighted;
        public bool IsCatapult => isCatapult;
        public CatapultDirection LaunchDirection => catapultDirection;
        public int CatapultLaunchDistance => Mathf.Max(1, catapultLaunchDistance);
        public SpriteRenderer SpriteRenderer => spriteRenderer;
        public Collider2D CellCollider => cellCollider;
        public Sprite CurrentSprite => spriteRenderer == null ? null : spriteRenderer.sprite;

        private void Reset()
        {
            CacheOptionalReferences();
            RefreshVisuals();
        }

        private void Awake()
        {
            CacheOptionalReferences();
            RefreshVisuals();
        }

        private void OnValidate()
        {
            CacheOptionalReferences();
            RefreshVisuals();
        }

        public void Initialize(Vector2Int coordinates, bool active, bool walkable, bool start = false, bool goal = false)
        {
            gridIndex = coordinates;
            isActive = active;
            isWalkable = walkable;
            isStart = start;
            isGoal = goal;
            isHighlighted = false;

            RefreshVisuals();
            Log($"Initialized cell at {gridIndex} with state {BuildStateSummary()}.");
        }

        public void SetCoordinates(int column, int row)
        {
            Vector2Int newIndex = new(column, row);
            if (gridIndex == newIndex)
            {
                return;
            }

            Vector2Int previousIndex = gridIndex;
            gridIndex = newIndex;
            Log($"Coordinates changed from {previousIndex} to {gridIndex}.");
        }

        public void SetActive(bool value)
        {
            if (isActive == value)
            {
                return;
            }

            isActive = value;
            RefreshVisuals();
            Log($"Active state changed to {isActive} for {gridIndex}.");
        }

        public void SetWalkable(bool value)
        {
            if (isWalkable == value)
            {
                return;
            }

            isWalkable = value;
            RefreshVisuals();
            Log($"Walkable state changed to {isWalkable} for {gridIndex}.");
        }

        public void SetHighlighted(bool value)
        {
            if (isHighlighted == value)
            {
                return;
            }

            isHighlighted = value;
            RefreshVisuals();
            Log($"Highlight state changed to {isHighlighted} for {gridIndex}.");
        }

        public void SetStart(bool value)
        {
            if (isStart == value)
            {
                return;
            }

            isStart = value;
            RefreshVisuals();
            Log($"Start marker changed to {isStart} for {gridIndex}.");
        }

        public void SetGoal(bool value)
        {
            if (isGoal == value)
            {
                return;
            }

            isGoal = value;
            RefreshVisuals();
            Log($"Goal marker changed to {isGoal} for {gridIndex}.");
        }

        public void ApplyState(bool active, bool walkable, bool start, bool goal, bool highlighted = false)
        {
            bool changed = isActive != active
                || isWalkable != walkable
                || isStart != start
                || isGoal != goal
                || isHighlighted != highlighted;

            if (!changed)
            {
                return;
            }

            isActive = active;
            isWalkable = walkable;
            isStart = start;
            isGoal = goal;
            isHighlighted = highlighted;

            RefreshVisuals();
            Log($"Applied state {BuildStateSummary()} to {gridIndex}.");
        }

        public void SetVisualSprite(Sprite sprite)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == sprite)
            {
                return;
            }

            spriteRenderer.sprite = sprite;
            Log($"Visual sprite changed on {gridIndex}.");
        }

        public void ConfigureCatapult(bool value, CatapultDirection direction, int launchDistance = 2)
        {
            int clampedLaunchDistance = Mathf.Max(1, launchDistance);
            bool changed = isCatapult != value
                || catapultDirection != direction
                || catapultLaunchDistance != clampedLaunchDistance;

            if (!changed)
            {
                return;
            }

            isCatapult = value;
            catapultDirection = direction;
            catapultLaunchDistance = clampedLaunchDistance;

            RefreshVisuals();
            Log($"Catapult state changed to enabled={isCatapult}, direction={catapultDirection}, launchDistance={catapultLaunchDistance} for {gridIndex}.");
        }

        public Vector2Int GetCatapultOffset()
        {
            return GetAxialOffset(catapultDirection);
        }

        public static Vector2Int GetAxialOffset(CatapultDirection direction)
        {
            return direction switch
            {
                CatapultDirection.E => new Vector2Int(1, 0),
                CatapultDirection.NE => new Vector2Int(1, -1),
                CatapultDirection.NW => new Vector2Int(0, -1),
                CatapultDirection.W => new Vector2Int(-1, 0),
                CatapultDirection.SW => new Vector2Int(-1, 1),
                CatapultDirection.SE => new Vector2Int(0, 1),
                _ => Vector2Int.zero
            };
        }

        [ContextMenu("Refresh Visuals")]
        public void RefreshVisuals()
        {
            if (driveSpriteColor && spriteRenderer != null)
            {
                spriteRenderer.color = ResolveCellColor();
            }

            if (driveColliderEnabledState && cellCollider != null)
            {
                cellCollider.enabled = isActive && isWalkable;
            }
        }

        private void CacheOptionalReferences()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (cellCollider == null)
            {
                cellCollider = GetComponent<Collider2D>();
            }
        }

        private Color ResolveCellColor()
        {
            if (isHighlighted)
            {
                return highlightColor;
            }

            if (isCatapult)
            {
                return Color.white;
            }

            if (!isWalkable)
            {
                return Color.white;
            }

            if (!isActive && !isStart)
            {
                return inactiveColor;
            }

            return Color.white;
        }

        private string BuildStateSummary()
        {
            return $"[active={isActive}, walkable={isWalkable}, start={isStart}, goal={isGoal}, highlighted={isHighlighted}, catapult={isCatapult}, direction={catapultDirection}, launchDistance={CatapultLaunchDistance}]";
        }

        private void Log(string message)
        {
            if (!enableDebugLogs)
            {
                return;
            }

            Debug.Log($"[HexCell:{name}] {message}", this);
        }
    }
}
