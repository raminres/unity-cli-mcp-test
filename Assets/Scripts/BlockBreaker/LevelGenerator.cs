using System.Collections.Generic;
using Arcade.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Procedurally generates grids of 1:1 cube blocks organized into tiered color rows,
    /// driven by LevelConfiguration ScriptableObjects with inverted colors and special modifier blocks.
    /// </summary>
    public class LevelGenerator : MonoBehaviour
    {
        public const string PREF_SELECTED_LEVEL = "Arcade_SelectedLevel";

        [Header("Level Configurations")]
        [SerializeField] private LevelConfiguration[] levelPresets;
        [SerializeField] private LevelConfiguration currentLevelConfig;

        [Header("Fallback Grid Settings (If no config assigned)")]
        [SerializeField] private int fallbackColumns = 8;
        [SerializeField] private int fallbackRowsPerTier = 2; // 2 Blue (top), 2 Green (mid), 2 Red (bottom) = 6 rows total
        [SerializeField] private float blockSize = 1.0f;
        [SerializeField] private float horizontalSpacing = 1.25f;
        [SerializeField] private float verticalSpacing = 1.3f;
        [SerializeField] private float startCenterY = 15.5f;

        [Header("Materials")]
        [SerializeField] private Material matRedBlock;
        [SerializeField] private Material matGreenBlock;
        [SerializeField] private Material matBlueBlock;

        [Header("VFX Particle Colors")]
        [SerializeField] private Color vfxRedColor = new Color(1.0f, 0.2f, 0.3f);
        [SerializeField] private Color vfxGreenColor = new Color(0.15f, 0.95f, 0.45f);
        [SerializeField] private Color vfxBlueColor = new Color(0.15f, 0.7f, 1.0f);

        [Header("UI Toolkit World Space Badge Assets")]
        [SerializeField] private PanelSettings badgePanelSettings;
        [SerializeField] private VisualTreeAsset badgeVisualTreeAsset;

        [Header("Parent Container")]
        [SerializeField] private Transform blocksContainer;

        public LevelConfiguration CurrentConfig => currentLevelConfig;
        public LevelConfiguration[] LevelPresets => levelPresets;

        private void Awake()
        {
            InitializeLevelConfig();
        }

        private void Start()
        {
            GenerateLevel();
        }

        private void InitializeLevelConfig()
        {
            int selectedLevel = PlayerPrefs.GetInt(PREF_SELECTED_LEVEL, 1);
            LevelConfiguration foundConfig = GetLevelConfig(selectedLevel);

            if (foundConfig != null)
            {
                currentLevelConfig = foundConfig.Clone();
            }
            else if (levelPresets != null && levelPresets.Length > 0 && levelPresets[0] != null)
            {
                currentLevelConfig = levelPresets[0].Clone();
            }
        }

        public LevelConfiguration GetLevelConfig(int levelNumber)
        {
            if (levelPresets == null) return null;
            for (int i = 0; i < levelPresets.Length; i++)
            {
                if (levelPresets[i] != null && levelPresets[i].LevelNumber == levelNumber)
                {
                    return levelPresets[i];
                }
            }
            return null;
        }

        public void SelectAndLoadLevel(int levelNumber)
        {
            PlayerPrefs.SetInt(PREF_SELECTED_LEVEL, levelNumber);
            PlayerPrefs.Save();

            LevelConfiguration baseConfig = GetLevelConfig(levelNumber);
            if (baseConfig != null)
            {
                currentLevelConfig = baseConfig.Clone();
            }

            GenerateLevel();

            // Reset ball to paddle
            var ball = FindAnyObjectByType<BallController>();
            if (ball != null)
            {
                ball.ResetBallToPaddle();
            }
        }

        public void ApplyCustomConfigAndReload(LevelConfiguration customConfig)
        {
            if (customConfig == null) return;
            currentLevelConfig = customConfig;
            PlayerPrefs.SetInt(PREF_SELECTED_LEVEL, customConfig.LevelNumber);
            PlayerPrefs.Save();

            GenerateLevel();

            // Reset ball to paddle
            var ball = FindAnyObjectByType<BallController>();
            if (ball != null)
            {
                ball.ResetBallToPaddle();
            }
        }

        public void LoadLevel(LevelConfiguration config)
        {
            ApplyCustomConfigAndReload(config);
        }

        public void GenerateLevel()
        {
            if (blocksContainer == null)
            {
                var existing = transform.Find("BlocksContainer");
                if (existing != null) blocksContainer = existing;
                else
                {
                    GameObject containerGo = new GameObject("BlocksContainer");
                    containerGo.transform.SetParent(transform);
                    blocksContainer = containerGo.transform;
                }
            }

            // Clear any existing blocks
            for (int i = blocksContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(blocksContainer.GetChild(i).gameObject);
            }

            int cols = currentLevelConfig != null ? currentLevelConfig.Columns : fallbackColumns;
            int rowsPerTier = currentLevelConfig != null ? currentLevelConfig.RowsPerTier : fallbackRowsPerTier;
            float spacingX = currentLevelConfig != null ? currentLevelConfig.HorizontalSpacing : horizontalSpacing;
            float spacingY = currentLevelConfig != null ? currentLevelConfig.VerticalSpacing : verticalSpacing;
            float centerY = currentLevelConfig != null ? currentLevelConfig.StartCenterY : startCenterY;
            float size = currentLevelConfig != null ? currentLevelConfig.BlockSize : blockSize;

            int totalRows = rowsPerTier * 3;
            float totalWidth = (cols - 1) * spacingX;
            float startX = -totalWidth * 0.5f;
            float totalHeight = (totalRows - 1) * spacingY;
            float topY = centerY + (totalHeight * 0.5f);

            int totalBlocksCreated = cols * totalRows;

            // Determine special block placements (unique indices)
            int multCount = currentLevelConfig != null ? currentLevelConfig.Multiplier2xCount : 1;
            int expCount = currentLevelConfig != null ? currentLevelConfig.PaddleExpanderCount : 1;

            var specialMap = DistributeSpecialBlocks(totalBlocksCreated, multCount, expCount);

            int blockIndex = 0;
            for (int r = 0; r < totalRows; r++)
            {
                float y = topY - (r * spacingY);

                // INVERTED Row ordering per requirements:
                // Top rows: Blue (tier 3, 30 pts)
                // Middle rows: Green (tier 2, 20 pts)
                // Bottom rows: Red (tier 1, 10 pts)
                BlockColorTier tier;
                Material mat;
                Color vfxColor;

                if (r < rowsPerTier)
                {
                    tier = BlockColorTier.Blue;
                    mat = matBlueBlock;
                    vfxColor = vfxBlueColor;
                }
                else if (r < rowsPerTier * 2)
                {
                    tier = BlockColorTier.Green;
                    mat = matGreenBlock;
                    vfxColor = vfxGreenColor;
                }
                else
                {
                    tier = BlockColorTier.Red;
                    mat = matRedBlock;
                    vfxColor = vfxRedColor;
                }

                for (int c = 0; c < cols; c++)
                {
                    float x = startX + (c * spacingX);
                    Vector3 blockPos = new Vector3(x, y, 0f);

                    GameObject blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    blockObj.name = $"Block_{tier}_{r}_{c}";
                    blockObj.transform.SetParent(blocksContainer);
                    blockObj.transform.position = blockPos;
                    blockObj.transform.localScale = Vector3.one * size;

                    BlockSpecialType special = specialMap.TryGetValue(blockIndex, out var sType) ? sType : BlockSpecialType.Normal;

                    Block blockComp = blockObj.AddComponent<Block>();
                    blockComp.Initialize(tier, mat, vfxColor, special);

                    if (special != BlockSpecialType.Normal && badgePanelSettings != null && badgeVisualTreeAsset != null)
                    {
                        CreateBadgeForBlock(blockObj, special);
                    }

                    blockIndex++;
                }
            }

            // Apply configurations to Ball and Paddle
            if (currentLevelConfig != null)
            {
                var ball = FindAnyObjectByType<BallController>();
                if (ball != null)
                {
                    ball.SetSpeedMultiplier(currentLevelConfig.BallSpeedMultiplier);
                }

                var paddle = FindAnyObjectByType<PaddleController>();
                if (paddle != null)
                {
                    paddle.ResetWidth(currentLevelConfig.InitialPaddleWidth);
                }
            }

            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.RegisterLevelBlocks(totalBlocksCreated);
            }
        }

        public Dictionary<int, BlockSpecialType> DistributeSpecialBlocks(int totalBlocks, int mult2xCount, int expanderCount)
        {
            var map = new Dictionary<int, BlockSpecialType>();
            if (totalBlocks <= 0) return map;

            var availableIndices = new List<int>(totalBlocks);
            for (int i = 0; i < totalBlocks; i++) availableIndices.Add(i);

            // Fisher-Yates Shuffle
            for (int i = availableIndices.Count - 1; i > 0; i--)
            {
                int rnd = Random.Range(0, i + 1);
                (availableIndices[i], availableIndices[rnd]) = (availableIndices[rnd], availableIndices[i]);
            }

            int cursor = 0;
            for (int i = 0; i < mult2xCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.ScoreMultiplier2x;
            }

            for (int i = 0; i < expanderCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.PaddleExpander;
            }

            return map;
        }

        private void CreateBadgeForBlock(GameObject blockObj, BlockSpecialType special)
        {
            GameObject badgeGo = new GameObject("UI_Badge");
            badgeGo.transform.SetParent(blockObj.transform);
            badgeGo.transform.localPosition = new Vector3(0f, 0f, -0.52f);
            badgeGo.transform.localRotation = Quaternion.identity;
            badgeGo.transform.localScale = Vector3.one;

            var badge = badgeGo.AddComponent<BlockBadge>();
            badge.Setup(special, badgePanelSettings, badgeVisualTreeAsset);
        }
    }
}
