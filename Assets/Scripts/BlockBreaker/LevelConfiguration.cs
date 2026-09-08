using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// ScriptableObject defining configurations and rules for a Block Breaker level.
    /// Allows designers and developers to create, tune, and test level variations as assets.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Level_01", menuName = "Arcade/Level Configuration", order = 1)]
    public class LevelConfiguration : ScriptableObject
    {
        [Header("Level Identity")]
        [SerializeField] private int levelNumber = 1;
        [SerializeField] private string levelName = "Level 1: Classic Inverted";
        [TextArea(2, 4)]
        [SerializeField] private string levelDescription = "Classic 3-tier block setup with Red on bottom, Green in middle, and Blue on top. Features random x2 multiplier and paddle expander blocks.";

        [Header("Grid Layout")]
        [Range(4, 14)] [SerializeField] private int columns = 8;
        [Range(1, 4)] [SerializeField] private int rowsPerTier = 2; // 2 Blue (top), 2 Green (mid), 2 Red (bottom) = 6 rows total
        [SerializeField] private float blockSize = 1.0f;
        [SerializeField] private float horizontalSpacing = 1.25f;
        [SerializeField] private float verticalSpacing = 1.3f;
        [SerializeField] private float startCenterY = 15.5f;

        [Header("Gameplay Balancing")]
        [Range(0.6f, 2.5f)] [SerializeField] private float ballSpeedMultiplier = 1.0f;
        [Range(3.0f, 8.0f)] [SerializeField] private float initialPaddleWidth = 5.0f;

        [Header("Special Modifier Blocks")]
        [Tooltip("Number of random blocks with x2 score multiplier badges.")]
        [Range(0, 8)] [SerializeField] private int multiplier2xCount = 1;

        [Tooltip("Number of random blocks that expand paddle width by +10% when destroyed.")]
        [Range(0, 5)] [SerializeField] private int paddleExpanderCount = 1;

        // Properties
        public int LevelNumber => levelNumber;
        public string LevelName => levelName;
        public string Description => levelDescription;
        public int Columns => columns;
        public int RowsPerTier => rowsPerTier;
        public int TotalRows => rowsPerTier * 3;
        public int TotalBlocks => columns * TotalRows;
        public float BlockSize => blockSize;
        public float HorizontalSpacing => horizontalSpacing;
        public float VerticalSpacing => verticalSpacing;
        public float StartCenterY => startCenterY;
        public float BallSpeedMultiplier => ballSpeedMultiplier;
        public float InitialPaddleWidth => initialPaddleWidth;
        public int Multiplier2xCount => multiplier2xCount;
        public int PaddleExpanderCount => paddleExpanderCount;

        /// <summary>
        /// Creates a runtime clone of this level configuration for live tweaking in the Level Settings modal.
        /// </summary>
        public LevelConfiguration Clone()
        {
            var clone = CreateInstance<LevelConfiguration>();
            clone.levelNumber = levelNumber;
            clone.levelName = levelName;
            clone.levelDescription = levelDescription;
            clone.columns = columns;
            clone.rowsPerTier = rowsPerTier;
            clone.blockSize = blockSize;
            clone.horizontalSpacing = horizontalSpacing;
            clone.verticalSpacing = verticalSpacing;
            clone.startCenterY = startCenterY;
            clone.ballSpeedMultiplier = ballSpeedMultiplier;
            clone.initialPaddleWidth = initialPaddleWidth;
            clone.multiplier2xCount = multiplier2xCount;
            clone.paddleExpanderCount = paddleExpanderCount;
            return clone;
        }

        // Runtime setters for editable modal
        public void SetColumns(int val) => columns = Mathf.Clamp(val, 4, 14);
        public void SetRowsPerTier(int val) => rowsPerTier = Mathf.Clamp(val, 1, 4);
        public void SetBallSpeedMultiplier(float val) => ballSpeedMultiplier = Mathf.Clamp(val, 0.6f, 2.5f);
        public void SetMultiplier2xCount(int val) => multiplier2xCount = Mathf.Clamp(val, 0, 8);
        public void SetPaddleExpanderCount(int val) => paddleExpanderCount = Mathf.Clamp(val, 0, 5);
        public void SetInitialPaddleWidth(float val) => initialPaddleWidth = Mathf.Clamp(val, 3.0f, 8.0f);
    }
}
