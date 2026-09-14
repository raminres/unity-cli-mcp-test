using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcade.BlockBreaker
{
    public enum BlockColorPattern
    {
        InvertedTiered = 0, // Top Blue, Middle Green, Bottom Red
        Randomized = 1,     // Randomly dispersed Blue, Green, Red blocks
        Checkerboard = 2    // Alternating grid coordinates (r + c) % 3
    }

    public enum LevelLayoutType
    {
        FullGrid = 0,          // Solid rectangular grid
        Diamond = 1,           // Symmetric diamond / rhombus
        Pyramid = 2,           // Upward-pointing stepped pyramid
        InvertedPyramid = 3,   // Downward-pointing funnel
        Hourglass = 4,         // Wide top and bottom, narrow waist
        Cross = 5,             // Intersecting horizontal & vertical bars
        HollowBox = 6,         // Perimeter frame with hollow center
        Pillars = 7,           // Vertical columns separated by open alleys
        Stripes = 8,           // Horizontal tiers separated by open lanes
        CheckerboardEmpty = 9, // 50% density alternating lattice mesh
        Heart = 10,            // Arcade heart silhouette
        Invader = 11,          // Retro 8-bit space invader alien
        Shield = 12,           // Heraldic Aegis shield silhouette
        Chevron = 13,          // Forward-swept wedge / arrowhead
        Crown = 14,            // Royal 3-peaked crown
        Castle = 15,           // Fortified battlement with twin turrets
        Custom = 16            // Multi-line ASCII text or string row layout
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
        [SerializeField] private LevelLayoutType layoutType = LevelLayoutType.FullGrid;
        [SerializeField] private BlockColorPattern colorPattern = BlockColorPattern.InvertedTiered;
        [Range(4, 14)] [SerializeField] private int columns = 8;
        [Range(1, 4)] [SerializeField] private int rowsPerTier = 2; // 2 Blue, 2 Green, 2 Red = 6 rows total
        [SerializeField] private float blockSize = 1.0f;
        [SerializeField] private float horizontalSpacing = 1.25f;
        [SerializeField] private float verticalSpacing = 1.3f;
        [SerializeField] private float startCenterY = 15.5f;

        [Header("Custom Layout Pattern (When LayoutType is Custom)")]
        [Tooltip("Multi-line ASCII layout where '.' or space is empty, 'X'/'#' is filled, and 'B','G','R' specify explicit colors.")]
        [TextArea(6, 14)]
        [SerializeField] private string customLayout;

        [Tooltip("Optional explicit array of row strings for custom layout.")]
        [SerializeField] private string[] customLayoutRows;

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

        [Tooltip("Number of random laser blaster powerup blocks (fires twin penetrating bolts from paddle).")]
        [Range(0, 4)] [SerializeField] private int laserCount = 0;

        [Tooltip("Duration of laser blaster powerup in seconds.")]
        [Range(5f, 25f)] [SerializeField] private float laserDuration = 10f;

        // Cached custom layout parsed lines
        private string[] cachedCustomLines;
        private string cachedCustomRaw;

        // Properties
        public int LevelNumber => levelNumber;
        public string LevelName => levelName;
        public string Description => levelDescription;
        public LevelLayoutType LayoutType => layoutType;
        public BlockColorPattern ColorPattern => colorPattern;
        public int Columns => columns;
        public int RowsPerTier => rowsPerTier;
        public int TotalRows => rowsPerTier * 3;
        public float BlockSize => blockSize;
        public float HorizontalSpacing => horizontalSpacing;
        public float VerticalSpacing => verticalSpacing;
        public float StartCenterY => startCenterY;
        public string CustomLayout => customLayout;
        public string[] CustomLayoutRows => customLayoutRows;
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
        public int LaserCount => laserCount;
        public float LaserDuration => laserDuration;

        /// <summary>
        /// Total count of active, filled blocks that will be generated for this level.
        /// </summary>
        public int TotalBlocks
        {
            get
            {
                if (layoutType == LevelLayoutType.FullGrid)
                {
                    return columns * TotalRows;
                }

                int count = 0;
                int rows = TotalRows;
                int cols = columns;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (HasBlockAt(r, c)) count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Evaluates whether a block should exist at grid position (row, col).
        /// </summary>
        public bool HasBlockAt(int row, int col)
        {
            int rows = TotalRows;
            int cols = columns;

            if (row < 0 || row >= rows || col < 0 || col >= cols)
                return false;

            return layoutType switch
            {
                LevelLayoutType.FullGrid => true,
                LevelLayoutType.Diamond => EvaluateDiamond(row, col, rows, cols),
                LevelLayoutType.Pyramid => EvaluatePyramid(row, col, rows, cols),
                LevelLayoutType.InvertedPyramid => EvaluateInvertedPyramid(row, col, rows, cols),
                LevelLayoutType.Hourglass => EvaluateHourglass(row, col, rows, cols),
                LevelLayoutType.Cross => EvaluateCross(row, col, rows, cols),
                LevelLayoutType.HollowBox => EvaluateHollowBox(row, col, rows, cols),
                LevelLayoutType.Pillars => EvaluatePillars(row, col, rows, cols),
                LevelLayoutType.Stripes => EvaluateStripes(row, col, rows, cols),
                LevelLayoutType.CheckerboardEmpty => EvaluateCheckerboardEmpty(row, col, rows, cols),
                LevelLayoutType.Heart => EvaluateHeart(row, col, rows, cols),
                LevelLayoutType.Invader => EvaluateInvader(row, col, rows, cols),
                LevelLayoutType.Shield => EvaluateShield(row, col, rows, cols),
                LevelLayoutType.Chevron => EvaluateChevron(row, col, rows, cols),
                LevelLayoutType.Crown => EvaluateCrown(row, col, rows, cols),
                LevelLayoutType.Castle => EvaluateCastle(row, col, rows, cols),
                LevelLayoutType.Custom => EvaluateCustom(row, col),
                _ => true
            };
        }

        /// <summary>
        /// Returns an explicit color tier if defined in a custom layout (e.g. 'B', 'G', 'R'); otherwise null.
        /// </summary>
        public BlockColorTier? GetExplicitColorAt(int row, int col)
        {
            if (layoutType != LevelLayoutType.Custom) return null;

            char ch = GetCustomCharAt(row, col);
            return ch switch
            {
                'B' or 'b' => BlockColorTier.Blue,
                'G' or 'g' => BlockColorTier.Green,
                'R' or 'r' => BlockColorTier.Red,
                _ => null
            };
        }

        #region Procedural Shape Predicates

        private bool EvaluateDiamond(int r, int c, int rows, int cols)
        {
            float cy = (rows - 1) / 2f;
            float cx = (cols - 1) / 2f;
            float dy = Mathf.Abs(r - cy) / Mathf.Max(0.5f, rows / 2f);
            float dx = Mathf.Abs(c - cx) / Mathf.Max(0.5f, cols / 2f);
            return (dx + dy) <= 1.05f;
        }

        private bool EvaluatePyramid(int r, int c, int rows, int cols)
        {
            float cx = (cols - 1) / 2f;
            float frac = (r + 1f) / rows;
            float halfWidth = (cols / 2f) * frac;
            return Mathf.Abs(c - cx) <= halfWidth;
        }

        private bool EvaluateInvertedPyramid(int r, int c, int rows, int cols)
        {
            float cx = (cols - 1) / 2f;
            float frac = (rows - r) / (float)rows;
            float halfWidth = (cols / 2f) * frac;
            return Mathf.Abs(c - cx) <= halfWidth;
        }

        private bool EvaluateHourglass(int r, int c, int rows, int cols)
        {
            float cy = (rows - 1) / 2f;
            float cx = (cols - 1) / 2f;
            float vDist = Mathf.Abs(r - cy) / Mathf.Max(1f, rows / 2f);
            float halfWidth = (cols / 2f) * (0.35f + 0.65f * vDist);
            return Mathf.Abs(c - cx) <= halfWidth;
        }

        private bool EvaluateCross(int r, int c, int rows, int cols)
        {
            float cy = (rows - 1) / 2f;
            float cx = (cols - 1) / 2f;
            bool isHoriz = Mathf.Abs(r - cy) <= Mathf.Max(0.75f, rows * 0.18f);
            bool isVert = Mathf.Abs(c - cx) <= Mathf.Max(0.75f, cols * 0.18f);
            return isHoriz || isVert;
        }

        private bool EvaluateHollowBox(int r, int c, int rows, int cols)
        {
            return r == 0 || r == rows - 1 || c == 0 || c == cols - 1;
        }

        private bool EvaluatePillars(int r, int c, int rows, int cols)
        {
            return (c % 2) == 0;
        }

        private bool EvaluateStripes(int r, int c, int rows, int cols)
        {
            return (r % 2) == 0;
        }

        private bool EvaluateCheckerboardEmpty(int r, int c, int rows, int cols)
        {
            return (r + c) % 2 == 0;
        }

        private bool EvaluateHeart(int r, int c, int rows, int cols)
        {
            float cx = (cols - 1) / 2f;
            float normY = 1f - (r / (float)(rows - 1)); // 0 at bottom, 1 at top
            float normX = (c - cx) / Mathf.Max(1f, cols / 2f); // -1 to 1

            if (normY <= 0.55f)
            {
                return Mathf.Abs(normX) <= (normY / 0.55f) * 0.95f;
            }
            else
            {
                float lobeCenter = 0.45f;
                float distLeft = Mathf.Sqrt(Mathf.Pow(normX + lobeCenter, 2f) + Mathf.Pow((normY - 0.75f) * 1.5f, 2f));
                float distRight = Mathf.Sqrt(Mathf.Pow(normX - lobeCenter, 2f) + Mathf.Pow((normY - 0.75f) * 1.5f, 2f));
                return distLeft <= 0.48f || distRight <= 0.48f;
            }
        }

        private bool EvaluateInvader(int r, int c, int rows, int cols)
        {
            float cx = (cols - 1) / 2f;
            int dx = Mathf.RoundToInt(Mathf.Abs(c - cx));
            int normR = Mathf.Clamp(Mathf.RoundToInt((r / (float)(rows - 1)) * 5f), 0, 5);

            return normR switch
            {
                0 => dx == 1 || dx == 3,           // Antennas
                1 => dx <= 2,                      // Head
                2 => dx != 1 && dx <= 3,           // Eyes
                3 => dx <= 3,                      // Torso
                4 => dx == 0 || dx == 3,           // Arms / mouth
                5 => dx == 1 || dx == 2,           // Feet
                _ => true
            };
        }

        private bool EvaluateShield(int r, int c, int rows, int cols)
        {
            float cx = (cols - 1) / 2f;
            float vFrac = r / (float)(rows - 1);
            if (vFrac <= 0.45f)
            {
                return Mathf.Abs(c - cx) <= (cols / 2f) * 0.95f;
            }
            float taper = 1f - Mathf.Pow((vFrac - 0.45f) / 0.55f, 1.4f);
            return Mathf.Abs(c - cx) <= (cols / 2f) * Mathf.Max(0.12f, taper);
        }

        private bool EvaluateChevron(int r, int c, int rows, int cols)
        {
            float cx = (cols - 1) / 2f;
            float targetRow = Mathf.Abs(c - cx) * 1.25f;
            return Mathf.Abs(r - targetRow) <= 1.0f;
        }

        private bool EvaluateCrown(int r, int c, int rows, int cols)
        {
            float cx = (cols - 1) / 2f;
            if (r >= rows / 2) return true;

            float leftDist = Mathf.Abs(c - 0);
            float midDist = Mathf.Abs(c - cx);
            float rightDist = Mathf.Abs(c - (cols - 1));
            float minDist = Mathf.Min(leftDist, Mathf.Min(midDist, rightDist));
            return (r + minDist) <= (rows / 2) + 0.75f;
        }

        private bool EvaluateCastle(int r, int c, int rows, int cols)
        {
            // Turrets on outer edges
            if (c <= 1 || c >= cols - 2) return true;
            // Lower battlement
            if (r >= rows / 3)
            {
                return (r != rows / 3) || (c % 2 == 0);
            }
            return false;
        }

        private bool EvaluateCustom(int r, int c)
        {
            char ch = GetCustomCharAt(r, c);
            if (ch == '\0') return false;
            // '.' or '_' or ' ' or '0' means empty space
            return ch != '.' && ch != '_' && ch != ' ' && ch != '0';
        }

        private char GetCustomCharAt(int r, int c)
        {
            EnsureCustomLayoutParsed();
            if (cachedCustomLines == null || r < 0 || r >= cachedCustomLines.Length)
                return '\0';

            string line = cachedCustomLines[r];
            if (string.IsNullOrEmpty(line) || c < 0 || c >= line.Length)
                return '\0';

            return line[c];
        }

        private void EnsureCustomLayoutParsed()
        {
            if (customLayoutRows != null && customLayoutRows.Length > 0)
            {
                cachedCustomLines = customLayoutRows;
                return;
            }

            if (cachedCustomRaw == customLayout && cachedCustomLines != null)
                return;

            cachedCustomRaw = customLayout;
            if (string.IsNullOrWhiteSpace(customLayout))
            {
                cachedCustomLines = Array.Empty<string>();
                return;
            }

            cachedCustomLines = customLayout.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        }

        #endregion

        /// <summary>
        /// Creates a runtime clone of this level configuration for live tweaking in the Level Settings modal.
        /// </summary>
        public LevelConfiguration Clone()
        {
            var clone = CreateInstance<LevelConfiguration>();
            clone.levelNumber = levelNumber;
            clone.levelName = levelName;
            clone.levelDescription = levelDescription;
            clone.layoutType = layoutType;
            clone.colorPattern = colorPattern;
            clone.columns = columns;
            clone.rowsPerTier = rowsPerTier;
            clone.blockSize = blockSize;
            clone.horizontalSpacing = horizontalSpacing;
            clone.verticalSpacing = verticalSpacing;
            clone.startCenterY = startCenterY;
            clone.customLayout = customLayout;
            if (customLayoutRows != null)
            {
                clone.customLayoutRows = (string[])customLayoutRows.Clone();
            }
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
            clone.laserCount = laserCount;
            clone.laserDuration = laserDuration;
            return clone;
        }

        // Runtime setters for editable modal & scripts
        public void SetLayoutType(LevelLayoutType type) => layoutType = type;
        public void SetCustomLayout(string layout)
        {
            customLayout = layout;
            cachedCustomRaw = null;
            cachedCustomLines = null;
        }
        public void SetCustomLayoutRows(string[] rows)
        {
            customLayoutRows = rows;
            cachedCustomLines = rows;
        }
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
        public void SetLaserCount(int val) => laserCount = Mathf.Clamp(val, 0, 4);
        public void SetLaserDuration(float val) => laserDuration = Mathf.Clamp(val, 5f, 25f);
        public void SetInitialPaddleWidth(float val) => initialPaddleWidth = Mathf.Clamp(val, 3.0f, 8.0f);
    }
}
