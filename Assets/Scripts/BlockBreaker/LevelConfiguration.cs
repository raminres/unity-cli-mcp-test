using UnityEngine;

namespace Arcade.BlockBreaker
{
    public enum BlockColorPattern
    {
        InvertedTiered = 0, // Top Blue, Middle Green, Bottom Red
        Randomized = 1,     // Randomly dispersed Blue, Green, Red blocks
        Checkerboard = 2    // Alternating grid coordinates (r + c) % 3
    }

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

        [Header("Grid Layout & Appearance")]
        [SerializeField] private BlockColorPattern colorPattern = BlockColorPattern.InvertedTiered;
        [Range(4, 14)] [SerializeField] private int columns = 8;
        [Range(1, 4)] [SerializeField] private int rowsPerTier = 2; // 2 Blue, 2 Green, 2 Red = 6 rows total
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

        [Tooltip("Number of random blocks with x3 score multiplier badges.")]
        [Range(0, 8)] [SerializeField] private int multiplier3xCount = 0;

        [Tooltip("Number of random blocks with x4 score multiplier badges.")]
        [Range(0, 8)] [SerializeField] private int multiplier4xCount = 0;

        [Tooltip("Number of random blocks with x5 score multiplier badges.")]
        [Range(0, 8)] [SerializeField] private int multiplier5xCount = 0;

        [Tooltip("Number of random blocks that expand paddle width by +10% when destroyed.")]
        [Range(0, 5)] [SerializeField] private int paddleExpanderCount = 1;

        [Tooltip("Number of random explosive bomb blocks.")]
        [Range(0, 5)] [SerializeField] private int bombCount = 0;

        [Tooltip("Number of random reinforced glass-enclosed bricks (2 hits, 2x score).")]
        [Range(0, 8)] [SerializeField] private int glassEnclosedCount = 0;

        [Tooltip("Number of random extra heart powerup blocks (+1 life).")]
        [Range(0, 3)] [SerializeField] private int extraHeartCount = 0;

        [Tooltip("Number of random shield powerup blocks.")]
        [Range(0, 4)] [SerializeField] private int shieldCount = 0;

        [Tooltip("Number of random multi-ball powerup blocks.")]
        [Range(0, 4)] [SerializeField] private int multiBallCount = 0;

        [Tooltip("Duration of shield protection in seconds.")]
        [Range(5f, 30f)] [SerializeField] private float shieldDuration = 10f;

        // Properties
        public int LevelNumber => levelNumber;
        public string LevelName => levelName;
        public string Description => levelDescription;
        public BlockColorPattern ColorPattern => colorPattern;
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
        public int Multiplier3xCount => multiplier3xCount;
        public int Multiplier4xCount => multiplier4xCount;
        public int Multiplier5xCount => multiplier5xCount;
        public int PaddleExpanderCount => paddleExpanderCount;
        public int BombCount => bombCount;
        public int GlassEnclosedCount => glassEnclosedCount;
        public int ExtraHeartCount => extraHeartCount;
        public int ShieldCount => shieldCount;
        public int MultiBallCount => multiBallCount;
        public float ShieldDuration => shieldDuration;

        /// <summary>
        /// Creates a runtime clone of this level configuration for live tweaking in the Level Settings modal.
        /// </summary>
        public LevelConfiguration Clone()
        {
            var clone = CreateInstance<LevelConfiguration>();
            clone.levelNumber = levelNumber;
            clone.levelName = levelName;
            clone.levelDescription = levelDescription;
            clone.colorPattern = colorPattern;
            clone.columns = columns;
            clone.rowsPerTier = rowsPerTier;
            clone.blockSize = blockSize;
            clone.horizontalSpacing = horizontalSpacing;
            clone.verticalSpacing = verticalSpacing;
            clone.startCenterY = startCenterY;
            clone.ballSpeedMultiplier = ballSpeedMultiplier;
            clone.initialPaddleWidth = initialPaddleWidth;
            clone.multiplier2xCount = multiplier2xCount;
            clone.multiplier3xCount = multiplier3xCount;
            clone.multiplier4xCount = multiplier4xCount;
            clone.multiplier5xCount = multiplier5xCount;
            clone.paddleExpanderCount = paddleExpanderCount;
            clone.bombCount = bombCount;
            clone.glassEnclosedCount = glassEnclosedCount;
            clone.extraHeartCount = extraHeartCount;
            clone.shieldCount = shieldCount;
            clone.multiBallCount = multiBallCount;
            clone.shieldDuration = shieldDuration;
            return clone;
        }

        // Runtime setters for editable modal
        public void SetColorPattern(BlockColorPattern pattern) => colorPattern = pattern;
        public void SetColumns(int val) => columns = Mathf.Clamp(val, 4, 14);
        public void SetRowsPerTier(int val) => rowsPerTier = Mathf.Clamp(val, 1, 4);
        public void SetBallSpeedMultiplier(float val) => ballSpeedMultiplier = Mathf.Clamp(val, 0.6f, 2.5f);
        public void SetMultiplier2xCount(int val) => multiplier2xCount = Mathf.Clamp(val, 0, 8);
        public void SetMultiplier3xCount(int val) => multiplier3xCount = Mathf.Clamp(val, 0, 8);
        public void SetMultiplier4xCount(int val) => multiplier4xCount = Mathf.Clamp(val, 0, 8);
        public void SetMultiplier5xCount(int val) => multiplier5xCount = Mathf.Clamp(val, 0, 8);
        public void SetPaddleExpanderCount(int val) => paddleExpanderCount = Mathf.Clamp(val, 0, 5);
        public void SetBombCount(int val) => bombCount = Mathf.Clamp(val, 0, 5);
        public void SetGlassEnclosedCount(int val) => glassEnclosedCount = Mathf.Clamp(val, 0, 8);
        public void SetExtraHeartCount(int val) => extraHeartCount = Mathf.Clamp(val, 0, 3);
        public void SetShieldCount(int val) => shieldCount = Mathf.Clamp(val, 0, 4);
        public void SetMultiBallCount(int val) => multiBallCount = Mathf.Clamp(val, 0, 4);
        public void SetShieldDuration(float val) => shieldDuration = Mathf.Clamp(val, 5f, 30f);
        public void SetInitialPaddleWidth(float val) => initialPaddleWidth = Mathf.Clamp(val, 3.0f, 8.0f);
    }
}
