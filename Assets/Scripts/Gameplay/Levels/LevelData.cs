using System;
using System.Collections.Generic;
using ProjectExtinguisher.Gameplay.Hex;
using UnityEngine;

namespace ProjectExtinguisher.Gameplay.Levels
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "ProjectExtinguisher/Gameplay/Level Data")]
    public sealed class LevelData : ScriptableObject
    {
        [Serializable]
        public struct CellLevelState
        {
            public Vector2Int coordinate;
            public bool blocked;
            public bool initiallyActive;
            public bool catapult;
            public HexCell.CatapultDirection catapultDirection;
            public bool usePathVariant;
            public int pathVariantIndex;
        }

        [Header("Identity")]
        [SerializeField] [Min(1)] private int levelNumber = 1;
        [SerializeField] private string levelName = "Level 1";

        [Header("Rules")]
        [SerializeField] [Min(0)] private int moveLimit = 12;
        [SerializeField] private Vector2Int startCell = new(-4, 0);
        [SerializeField] private Vector2Int goalCell = new(4, 0);
        [SerializeField] private bool defaultInitialActiveState;

        [Header("Per-Cell Overrides")]
        [SerializeField] private List<CellLevelState> cellStates = new();

        public int LevelNumber => levelNumber;
        public string LevelName => levelName;
        public int MoveLimit => moveLimit;
        public Vector2Int StartCell => startCell;
        public Vector2Int GoalCell => goalCell;
        public bool DefaultInitialActiveState => defaultInitialActiveState;
        public IReadOnlyList<CellLevelState> CellStates => cellStates;

        public string GetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(levelName))
            {
                return levelName;
            }

            return $"Level {Mathf.Max(1, levelNumber)}";
        }

        private void OnValidate()
        {
            levelNumber = Mathf.Max(1, levelNumber);
            moveLimit = Mathf.Max(0, moveLimit);
        }
    }
}
