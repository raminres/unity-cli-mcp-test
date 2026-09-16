using System.Collections.Generic;
using Arcade.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Procedurally generates grids of 1:1 cube blocks organized into tiered color rows,
    /// driven by LevelConfiguration ScriptableObjects with inverted colors, patterns, and special modifier blocks.
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
        [SerializeField] private Material matGlass;

        [Header("VFX Particle Colors")]
        [SerializeField] private Color vfxRedColor = new Color(1.0f, 0.2f, 0.3f);
        [SerializeField] private Color vfxGreenColor = new Color(0.15f, 0.95f, 0.45f);
        [SerializeField] private Color vfxBlueColor = new Color(0.15f, 0.7f, 1.0f);

        [Header("UI Toolkit World Space Badge Assets")]
        [SerializeField] private PanelSettings badgePanelSettings;
        [SerializeField] private VisualTreeAsset badgeVisualTreeAsset;

        [Header("Badge & Powerup Icons (Assigned in Unity UI)")]
        [SerializeField] private PowerupIconSet iconSet;

        [Header("Parent Container")]
        [SerializeField] private Transform blocksContainer;

        public static LevelGenerator Instance { get; private set; }
        public static void SetInstance(LevelGenerator inst) => Instance = inst;
        public PowerupIconSet IconSet => iconSet;
        public void SetIconSet(PowerupIconSet set) => iconSet = set;
        public LevelConfiguration CurrentConfig => currentLevelConfig;
        public LevelConfiguration[] LevelPresets => levelPresets;
        public int TotalLevels => levelPresets != null && levelPresets.Length > 0 ? levelPresets.Length : 1;
        public Transform BlocksContainer => blocksContainer;

        private void Awake()
        {
            Instance = this;
            InitializeLevelConfig();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            EnsureCornerChamfers();
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

        public void AdvanceToNextLevel()
        {
            int currentLvl = currentLevelConfig != null ? currentLevelConfig.LevelNumber : 1;
            int nextLvl = currentLvl + 1;
            if (GetLevelConfig(nextLvl) == null)
            {
                nextLvl = 1; // Loop back to level 1 for continuous arcade run
            }

            SelectAndLoadLevel(nextLvl);
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

            // Clear any existing blocks, in-flight projectiles, and falling capsules
            PowerupCapsule.ClearAllFallingCapsules();
            LaserBolt.ClearAllActiveBolts();

            for (int i = blocksContainer.childCount - 1; i >= 0; i--)
            {
                var child = blocksContainer.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }

            int cols = currentLevelConfig != null ? currentLevelConfig.Columns : fallbackColumns;
            int rowsPerTier = currentLevelConfig != null ? currentLevelConfig.RowsPerTier : fallbackRowsPerTier;
            float spacingX = currentLevelConfig != null ? currentLevelConfig.HorizontalSpacing : horizontalSpacing;
            float spacingY = currentLevelConfig != null ? currentLevelConfig.VerticalSpacing : verticalSpacing;
            float centerY = currentLevelConfig != null ? currentLevelConfig.StartCenterY : startCenterY;
            float size = currentLevelConfig != null ? currentLevelConfig.BlockSize : blockSize;
            BlockColorPattern pattern = currentLevelConfig != null ? currentLevelConfig.ColorPattern : BlockColorPattern.InvertedTiered;

            int totalRows = rowsPerTier * 3;
            float totalWidth = (cols - 1) * spacingX;
            float startX = -totalWidth * 0.5f;
            float totalHeight = (totalRows - 1) * spacingY;
            float topY = centerY + (totalHeight * 0.5f);

            int totalActiveBlocks = 0;
            for (int r = 0; r < totalRows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (currentLevelConfig == null || currentLevelConfig.HasBlockAt(r, c))
                    {
                        totalActiveBlocks++;
                    }
                }
            }

            // Determine special block placements (unique indices based on active blocks)
            int mult2x = currentLevelConfig != null ? currentLevelConfig.Multiplier2xCount : 1;
            int mult3x = currentLevelConfig != null ? currentLevelConfig.Multiplier3xCount : 0;
            int mult4x = currentLevelConfig != null ? currentLevelConfig.Multiplier4xCount : 0;
            int mult5x = currentLevelConfig != null ? currentLevelConfig.Multiplier5xCount : 0;
            int expCount = currentLevelConfig != null ? currentLevelConfig.PaddleExpanderCount : 1;
            int bombCount = currentLevelConfig != null ? currentLevelConfig.BombCount : 0;
            int glassCount = currentLevelConfig != null ? currentLevelConfig.GlassEnclosedCount : 0;
            int heartCount = currentLevelConfig != null ? currentLevelConfig.ExtraHeartCount : 0;
            int shieldCount = currentLevelConfig != null ? currentLevelConfig.ShieldCount : 0;
            int multiBallCount = currentLevelConfig != null ? currentLevelConfig.MultiBallCount : 0;
            int laserCount = currentLevelConfig != null ? currentLevelConfig.LaserCount : 0;
            int shortenCount = currentLevelConfig != null ? currentLevelConfig.PaddleShortenerCount : 0;
            int slowCount = currentLevelConfig != null ? currentLevelConfig.PaddleSlowerCount : 0;
            int freezeCount = currentLevelConfig != null ? currentLevelConfig.BrickFreezerCount : 0;
            int ballShrinkCount = currentLevelConfig != null ? currentLevelConfig.BallSizeDecreaserCount : 0;
            int ballSlowCount = currentLevelConfig != null ? currentLevelConfig.BallSlowerCount : 0;
            int paddleFreezeCount = currentLevelConfig != null ? currentLevelConfig.PaddleFreezerCount : 0;

            var specialMap = DistributeSpecialBlocks(totalActiveBlocks, mult2x, mult3x, mult4x, mult5x, expCount, bombCount, glassCount, heartCount, shieldCount, multiBallCount, laserCount, shortenCount, slowCount, freezeCount, ballShrinkCount, ballSlowCount, paddleFreezeCount);

            int blockIndex = 0;
            for (int r = 0; r < totalRows; r++)
            {
                float y = topY - (r * spacingY);

                for (int c = 0; c < cols; c++)
                {
                    if (currentLevelConfig != null && !currentLevelConfig.HasBlockAt(r, c))
                    {
                        continue; // Skip empty space
                    }

                    BlockColorTier? explicitTier = currentLevelConfig != null ? currentLevelConfig.GetExplicitColorAt(r, c) : null;
                    BlockColorTier tier;

                    if (explicitTier.HasValue)
                    {
                        tier = explicitTier.Value;
                    }
                    else if (pattern == BlockColorPattern.Randomized)
                    {
                        int rnd = Random.Range(0, 3);
                        tier = rnd == 0 ? BlockColorTier.Blue : (rnd == 1 ? BlockColorTier.Green : BlockColorTier.Red);
                    }
                    else if (pattern == BlockColorPattern.Checkerboard)
                    {
                        int check = (r + c) % 3;
                        tier = check == 0 ? BlockColorTier.Blue : (check == 1 ? BlockColorTier.Green : BlockColorTier.Red);
                    }
                    else // InvertedTiered (Default)
                    {
                        if (r < rowsPerTier)
                            tier = BlockColorTier.Blue;
                        else if (r < rowsPerTier * 2)
                            tier = BlockColorTier.Green;
                        else
                            tier = BlockColorTier.Red;
                    }

                    Material mat = tier switch
                    {
                        BlockColorTier.Blue => matBlueBlock,
                        BlockColorTier.Green => matGreenBlock,
                        _ => matRedBlock
                    };

                    Color vfxColor = tier switch
                    {
                        BlockColorTier.Blue => vfxBlueColor,
                        BlockColorTier.Green => vfxGreenColor,
                        _ => vfxRedColor
                    };

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

                    if (special == BlockSpecialType.GlassEnclosed)
                    {
                        CreateGlassShellForBlock(blockObj, blockComp);
                    }
                    else if (special != BlockSpecialType.Normal && badgePanelSettings != null && badgeVisualTreeAsset != null)
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
                ArcadeGameManager.Instance.RegisterLevelBlocks(totalActiveBlocks, currentLevelConfig != null ? currentLevelConfig.LevelNumber : 1);
            }

            // Randomize background gradient texture for each level
            if (LevelBackgroundController.Instance != null)
            {
                LevelBackgroundController.Instance.RandomizeBackground();
            }
            else
            {
                var bg = FindAnyObjectByType<LevelBackgroundController>();
                if (bg != null) bg.RandomizeBackground();
            }
        }

        public Dictionary<int, BlockSpecialType> DistributeSpecialBlocks(int totalBlocks, int mult2xCount, int expanderCount)
        {
            return DistributeSpecialBlocks(totalBlocks, mult2xCount, 0, expanderCount, 0, 0, 0);
        }

        public Dictionary<int, BlockSpecialType> DistributeSpecialBlocks(int totalBlocks, int mult2xCount, int mult3xCount, int expanderCount)
        {
            return DistributeSpecialBlocks(totalBlocks, mult2xCount, mult3xCount, expanderCount, 0, 0, 0);
        }

        public Dictionary<int, BlockSpecialType> DistributeSpecialBlocks(int totalBlocks, int mult2xCount, int mult3xCount, int expanderCount, int bombCount, int glassCount, int heartCount)
        {
            return DistributeSpecialBlocks(totalBlocks, mult2xCount, mult3xCount, 0, 0, expanderCount, bombCount, glassCount, heartCount, 0, 0);
        }

        public Dictionary<int, BlockSpecialType> DistributeSpecialBlocks(int totalBlocks, int mult2xCount, int mult3xCount, int expanderCount, int bombCount, int glassCount, int heartCount, int shieldCount, int multiBallCount)
        {
            return DistributeSpecialBlocks(totalBlocks, mult2xCount, mult3xCount, 0, 0, expanderCount, bombCount, glassCount, heartCount, shieldCount, multiBallCount);
        }

        public Dictionary<int, BlockSpecialType> DistributeSpecialBlocks(int totalBlocks, int mult2xCount, int mult3xCount, int mult4xCount, int mult5xCount, int expanderCount, int bombCount, int glassCount, int heartCount, int shieldCount, int multiBallCount, int laserCount = 0, int shortenCount = 0, int slowCount = 0, int freezeCount = 0, int ballShrinkCount = 0, int ballSlowCount = 0, int paddleFreezeCount = 0)
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

            for (int i = 0; i < mult3xCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.ScoreMultiplier3x;
            }

            for (int i = 0; i < mult4xCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.ScoreMultiplier4x;
            }

            for (int i = 0; i < mult5xCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.ScoreMultiplier5x;
            }

            for (int i = 0; i < expanderCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.PaddleExpander;
            }

            for (int i = 0; i < bombCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.Bomb;
            }

            for (int i = 0; i < glassCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.GlassEnclosed;
            }

            for (int i = 0; i < heartCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.ExtraHeart;
            }

            for (int i = 0; i < shieldCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.Shield;
            }

            for (int i = 0; i < multiBallCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.MultiBall;
            }

            for (int i = 0; i < laserCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.Laser;
            }

            for (int i = 0; i < shortenCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.PaddleShortener;
            }

            for (int i = 0; i < slowCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.PaddleSlower;
            }

            for (int i = 0; i < freezeCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.BrickFreezer;
            }

            for (int i = 0; i < ballShrinkCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.BallSizeDecreaser;
            }

            for (int i = 0; i < ballSlowCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.BallSlower;
            }

            for (int i = 0; i < paddleFreezeCount && cursor < availableIndices.Count; i++, cursor++)
            {
                map[availableIndices[cursor]] = BlockSpecialType.PaddleFreezer;
            }

            return map;
        }

        private void CreateGlassShellForBlock(GameObject blockObj, Block blockComp)
        {
            GameObject shellObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shellObj.name = "Glass_Shell";
            shellObj.transform.SetParent(blockObj.transform);
            shellObj.transform.localPosition = Vector3.zero;
            shellObj.transform.localRotation = Quaternion.identity;
            shellObj.transform.localScale = Vector3.one * 1.18f;

            var col = shellObj.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            var mr = shellObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (matGlass != null)
                {
                    mr.sharedMaterial = matGlass;
                }
                else
                {
#if UNITY_EDITOR
                    matGlass = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Glass.mat");
                    if (matGlass != null) mr.sharedMaterial = matGlass;
#endif
                }
            }

            if (blockComp != null)
            {
                blockComp.SetGlassShell(shellObj);
            }
        }

        private void CreateBadgeForBlock(GameObject blockObj, BlockSpecialType special)
        {
            GameObject badgeGo = new GameObject("UI_Badge");
            badgeGo.transform.SetParent(blockObj.transform);
            badgeGo.transform.localPosition = new Vector3(0f, 0f, -0.52f);
            badgeGo.transform.localRotation = Quaternion.identity;
            badgeGo.transform.localScale = Vector3.one;

            var badge = badgeGo.AddComponent<BlockBadge>();
            Sprite badgeSprite = iconSet != null ? iconSet.GetSprite(special) : null;
            badge.Setup(special, badgePanelSettings, badgeVisualTreeAsset, badgeSprite);
        }

        public void EnsureCornerChamfers()
        {
            var boundariesRoot = GameObject.Find("Boundaries") ?? GameObject.Find("ArenaBoundaries");
            if (boundariesRoot == null) return;

            Transform topWall = boundariesRoot.transform.Find("TopWall");
            Material borderMat = null;
            PhysicsMaterial bounceMat = null;
            if (topWall != null)
            {
                var rend = topWall.GetComponent<MeshRenderer>();
                if (rend != null) borderMat = rend.sharedMaterial;
                var col = topWall.GetComponent<BoxCollider>();
                if (col != null) bounceMat = col.sharedMaterial;

                // Ensure TopWall is shortened so the perimeter forms a continuous polygonal frame
                topWall.position = new Vector3(0f, 24.25f, 0f);
                topWall.localScale = new Vector3(17.4f, 0.5f, 2f);
            }

            Transform leftWall = boundariesRoot.transform.Find("LeftWall");
            if (leftWall != null)
            {
                leftWall.position = new Vector3(-10.25f, 7.60f, 0f);
                leftWall.localScale = new Vector3(0.5f, 30.2f, 2f);
            }

            Transform rightWall = boundariesRoot.transform.Find("RightWall");
            if (rightWall != null)
            {
                rightWall.position = new Vector3(10.25f, 7.60f, 0f);
                rightWall.localScale = new Vector3(0.5f, 30.2f, 2f);
            }

            Transform leftChamfer = boundariesRoot.transform.Find("Chamfer_TopLeft");
            if (leftChamfer == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Chamfer_TopLeft";
                go.transform.SetParent(boundariesRoot.transform);
                go.transform.position = new Vector3(-9.40f, 23.40f, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
                go.transform.localScale = new Vector3(2.5f, 0.5f, 2f);
                if (borderMat != null) go.GetComponent<MeshRenderer>().sharedMaterial = borderMat;
                if (bounceMat != null) go.GetComponent<BoxCollider>().sharedMaterial = bounceMat;
            }
            else
            {
                leftChamfer.position = new Vector3(-9.40f, 23.40f, 0f);
                leftChamfer.rotation = Quaternion.Euler(0f, 0f, 45f);
                leftChamfer.localScale = new Vector3(2.5f, 0.5f, 2f);
            }

            Transform rightChamfer = boundariesRoot.transform.Find("Chamfer_TopRight");
            if (rightChamfer == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Chamfer_TopRight";
                go.transform.SetParent(boundariesRoot.transform);
                go.transform.position = new Vector3(9.40f, 23.40f, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, -45f);
                go.transform.localScale = new Vector3(2.5f, 0.5f, 2f);
                if (borderMat != null) go.GetComponent<MeshRenderer>().sharedMaterial = borderMat;
                if (bounceMat != null) go.GetComponent<BoxCollider>().sharedMaterial = bounceMat;
            }
            else
            {
                rightChamfer.position = new Vector3(9.40f, 23.40f, 0f);
                rightChamfer.rotation = Quaternion.Euler(0f, 0f, -45f);
                rightChamfer.localScale = new Vector3(2.5f, 0.5f, 2f);
            }
        }
    }
}
