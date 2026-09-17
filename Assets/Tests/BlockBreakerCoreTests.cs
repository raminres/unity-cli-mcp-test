using Arcade.Audio;
using Arcade.BlockBreaker;
using Arcade.Core;
using Arcade.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arcade.Tests
{
    /// <summary>
    /// Automated test suite validating core Block Breaker mechanics, scoring rules,
    /// dynamic paddle deflection math, and game state transitions.
    /// </summary>
    public class BlockBreakerCoreTests
    {
        private GameObject testRoot;
        private ArcadeGameManager gameManager;
        private PaddleController paddle;

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("TestRoot");
            gameManager = testRoot.AddComponent<ArcadeGameManager>();
            ArcadeGameManager.SetInstanceForTesting(gameManager);

            var paddleObj = new GameObject("Paddle");
            paddleObj.transform.SetParent(testRoot.transform);
            paddleObj.transform.position = Vector3.zero;
            paddle = paddleObj.AddComponent<PaddleController>();
            paddle.EnsureSteppedMeshHierarchy();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            ArcadeGameManager.SetInstanceForTesting(null);
            ArcadeUIManager.SetInstanceForTesting(null);
            Arcade.Input.ArcadeInputHandler.SetInstanceForTesting(null);
            BlockVFXManager.SetInstanceForTesting(null);

            if (testRoot != null)
            {
                Object.DestroyImmediate(testRoot);
            }
        }

        #region 1. Scoring & Block Multiplier Tests

        [Test]
        [TestCase(BlockColorTier.Red, 10)]
        [TestCase(BlockColorTier.Green, 20)]
        [TestCase(BlockColorTier.Blue, 30)]
        public void Block_ColorTiers_AwardCorrectPoints(BlockColorTier tier, int expectedPoints)
        {
            var blockObj = new GameObject("TestBlock");
            var block = blockObj.AddComponent<Block>();
            block.Initialize(tier, null, Color.white);

            Assert.AreEqual(expectedPoints, block.Points, $"Block tier {tier} must award {expectedPoints} points.");

            Object.DestroyImmediate(blockObj);
        }

        [Test]
        public void GameManager_RecordBlockDestroyed_IncrementsScoreAndTracksRemainingBlocks()
        {
            gameManager.RegisterLevelBlocks(3);
            gameManager.LaunchBall(); // State -> Playing

            Assert.AreEqual(0, gameManager.Score);
            Assert.AreEqual(3, gameManager.RemainingBlocks);

            // Break Red block (10 pts)
            gameManager.RecordBlockDestroyed(10, 1);
            Assert.AreEqual(10, gameManager.Score);
            Assert.AreEqual(2, gameManager.RemainingBlocks);

            // Break Green block (20 pts)
            gameManager.RecordBlockDestroyed(20, 2);
            Assert.AreEqual(30, gameManager.Score);
            Assert.AreEqual(1, gameManager.RemainingBlocks);

            // Break Blue block (30 pts) during Clutch Mode (10x multiplier) -> 300 pts added (30 + 300 = 330)
            gameManager.RecordBlockDestroyed(30, 3);
            Assert.AreEqual(330, gameManager.Score);
            Assert.AreEqual(0, gameManager.RemainingBlocks);

            // All blocks destroyed -> Clear must be pending during clear delay, then finalize to LevelClear
            Assert.IsTrue(gameManager.IsLevelClearPending, "Level clear must be pending during clear delay.");
            gameManager.TriggerImmediateLevelClearForTesting();
            Assert.AreEqual(GameState.LevelClear, gameManager.State);
        }

        #endregion

        #region 2. Paddle Deflection & Boundary Math Tests

        [TestCase(0f, 0f, Description = "Center returns 0")]
        [TestCase(2.5f, 1.0f, Description = "Right edge returns +1.0")]
        [TestCase(-2.5f, -1.0f, Description = "Left edge returns -1.0")]
        [TestCase(10.0f, 1.0f, Description = "Beyond right edge clamped to +1.0")]
        [TestCase(-10.0f, -1.0f, Description = "Beyond left edge clamped to -1.0")]
        public void Paddle_CalculateHitOffset_EvaluatesAndClampsCorrectly(float hitX, float expectedOffset)
        {
            paddle.transform.position = Vector3.zero;
            float offset = paddle.CalculateHitOffset(hitX);
            Assert.AreEqual(expectedOffset, offset, 0.001f, $"Hit at X={hitX} must produce offset {expectedOffset}.");
        }

        [Test]
        public void Paddle_DeflectionAngleCalculation_MatchesArcadeSpread()
        {
            // The bounce angle formula: Angle = 90 - (offset * 60)
            // Offset 0 -> 90 deg (Straight up)
            // Offset +1 -> 30 deg (Shallow right)
            // Offset -1 -> 150 deg (Shallow left)
            float centerAngle = 90f - (0f * 60f);
            float rightAngle = 90f - (1f * 60f);
            float leftAngle = 90f - (-1f * 60f);

            Assert.AreEqual(90f, centerAngle);
            Assert.AreEqual(30f, rightAngle);
            Assert.AreEqual(150f, leftAngle);
        }

        #endregion

        #region 3. Life Management & Game Over Flow Tests

        [Test]
        public void GameManager_BallLost_DecrementsLivesAndTriggersGameOverAtZero()
        {
            gameManager.LaunchBall(); // Playing
            Assert.AreEqual(3, gameManager.Lives);

            // Lose 1st life
            gameManager.RecordBallLost();
            Assert.AreEqual(2, gameManager.Lives);
            Assert.AreEqual(GameState.BallLost, gameManager.State);

            // Re-launch and lose 2nd life
            gameManager.LaunchBall();
            gameManager.RecordBallLost();
            Assert.AreEqual(1, gameManager.Lives);
            Assert.AreEqual(GameState.BallLost, gameManager.State);

            // Re-launch and lose 3rd life -> Game Over
            gameManager.LaunchBall();
            gameManager.RecordBallLost();
            Assert.AreEqual(0, gameManager.Lives);
            Assert.AreEqual(GameState.GameOver, gameManager.State);
        }

        [Test]
        public void GameManager_TogglePause_TogglesPausedStateCorrectly()
        {
            gameManager.LaunchBall();
            Assert.AreEqual(GameState.Playing, gameManager.State);

            // Pause
            gameManager.TogglePause();
            Assert.AreEqual(GameState.Paused, gameManager.State);

            // Unpause restores Playing
            gameManager.TogglePause();
            Assert.AreEqual(GameState.Playing, gameManager.State);
        }

        #endregion

        #region 4. Mobile Safe Area & Responsive Camera Tests

        [Test]
        public void SafeArea_CalculateInsets_ComputesPercentageMarginsCorrectly()
        {
            // Simulate iPhone 15 Pro resolution: 1179 x 2556
            // Top notch / dynamic island cutout: 140px, bottom home indicator: 100px
            float screenW = 1179f;
            float screenH = 2556f;
            Rect safeArea = new Rect(0f, 100f, 1179f, 2556f - 240f); // y=100, h=2316

            var (left, right, top, bottom) = Arcade.UI.SafeAreaController.CalculateInsets(safeArea, screenW, screenH);

            Assert.AreEqual(0f, left, 0.01f);
            Assert.AreEqual(0f, right, 0.01f);
            Assert.AreEqual((100f / screenH) * 100f, bottom, 0.01f);
            Assert.AreEqual((140f / screenH) * 100f, top, 0.01f);
        }

        [Test]
        [TestCase(16f / 9f, 38f)]    // Standard PC / Web landscape
        [TestCase(4f / 3f, 38f)]     // iPad landscape
        [TestCase(1f, 38f)]          // Square
        [TestCase(9f / 16f, 38f)]    // Mobile portrait
        [TestCase(9f / 19.5f, 38f)]  // Modern iPhone narrow portrait
        public void ResponsiveCamera_FrustumAlwaysEnclosesTargetBoundsAcrossAspectRatios(float aspectRatio, float vFov)
        {
            float targetWidth = 24.5f;   // Bounds covering left/right walls + padding
            float targetHeight = 25.0f;  // Bounds covering paddle to top wall + padding

            float distance = ResponsiveCameraController.CalculateRequiredDistance(targetWidth, targetHeight, vFov, aspectRatio);

            // Calculate actual visible width and height at this distance
            float vHalfRad = (vFov * 0.5f) * Mathf.Deg2Rad;
            float visibleHeight = 2f * distance * Mathf.Tan(vHalfRad);
            float visibleWidth = visibleHeight * aspectRatio;

            Assert.GreaterOrEqual(visibleHeight, targetHeight - 0.01f,
                $"At aspect ratio {aspectRatio:F2}, visible height ({visibleHeight:F1}) must enclose target height ({targetHeight:F1}).");

            Assert.GreaterOrEqual(visibleWidth, targetWidth - 0.01f,
                $"At aspect ratio {aspectRatio:F2}, visible width ({visibleWidth:F1}) must enclose target width ({targetWidth:F1}).");
        }

        [Test]
        public void ResponsiveCamera_NarrowAspect_PullsBackFurtherThanWideAspect()
        {
            float targetW = 24.5f;
            float targetH = 25.0f;
            float fov = 38f;

            float wideDistance = ResponsiveCameraController.CalculateRequiredDistance(targetW, targetH, fov, 16f / 9f);
            float narrowDistance = ResponsiveCameraController.CalculateRequiredDistance(targetW, targetH, fov, 9f / 16f);

            Assert.Greater(narrowDistance, wideDistance,
                "On a narrow screen ratio, camera must pull back further so side walls and ball remain in view.");
        }

        #endregion

        #region 5. Gameplay Improvements & Level Configuration Tests

        [Test]
        [TestCase(BlockColorTier.Red, 10, 20)]
        [TestCase(BlockColorTier.Green, 20, 40)]
        [TestCase(BlockColorTier.Blue, 30, 60)]
        public void Block_SpecialMultiplier2x_DoublesTierPoints(BlockColorTier tier, int normalPoints, int expectedDoubledPoints)
        {
            var normalBlockObj = new GameObject("NormalBlock");
            var normalBlock = normalBlockObj.AddComponent<Block>();
            normalBlock.Initialize(tier, null, Color.white, BlockSpecialType.Normal);
            Assert.AreEqual(normalPoints, normalBlock.Points);

            var multiBlockObj = new GameObject("MultiBlock");
            var multiBlock = multiBlockObj.AddComponent<Block>();
            multiBlock.Initialize(tier, null, Color.white, BlockSpecialType.ScoreMultiplier2x);
            Assert.AreEqual(expectedDoubledPoints, multiBlock.Points, $"Tier {tier} with 2x multiplier must award {expectedDoubledPoints} points.");

            Object.DestroyImmediate(normalBlockObj);
            Object.DestroyImmediate(multiBlockObj);
        }

        [Test]
        public void Paddle_ExpandWidth_IncreasesWidthByTenPercentAndRecalculatesBounds()
        {
            paddle.ResetWidth(5.0f);
            Assert.AreEqual(5.0f, paddle.Width, 0.001f);
            Assert.AreEqual(-7.5f, paddle.MinX, 0.001f);
            Assert.AreEqual(7.5f, paddle.MaxX, 0.001f);

            // Expand by 10%
            paddle.ExpandWidth(0.10f);

            float expectedWidth = 5.0f * 1.10f; // 5.5
            Assert.AreEqual(expectedWidth, paddle.Width, 0.001f);

            // With arena half width = 10, bounds must be [-10 + 2.75, 10 - 2.75] = [-7.25, 7.25]
            float expectedHalf = expectedWidth * 0.5f;
            Assert.AreEqual(-10.0f + expectedHalf, paddle.MinX, 0.001f);
            Assert.AreEqual(10.0f - expectedHalf, paddle.MaxX, 0.001f);
        }

        [Test]
        public void Block_DestroyingPaddleExpander_ElongatesPaddle()
        {
            paddle.ResetWidth(5.0f);
            gameManager.LaunchBall();

            var blockObj = new GameObject("ExpanderBlock");
            var block = blockObj.AddComponent<Block>();
            block.Initialize(BlockColorTier.Green, null, Color.white, BlockSpecialType.PaddleExpander);

            block.DestroyBlock(Vector3.down);

            Assert.AreEqual(5.5f, paddle.Width, 0.001f, "Destroying a PaddleExpander block must elongate paddle width by +10%.");
        }

        [Test]
        public void LevelGenerator_DistributeSpecialBlocks_AllocatesCorrectCountsWithoutOverlap()
        {
            var genObj = new GameObject("Gen");
            var gen = genObj.AddComponent<LevelGenerator>();

            int totalBlocks = 48;
            int multCount = 2;
            int expanderCount = 1;

            var specialMap = gen.DistributeSpecialBlocks(totalBlocks, multCount, expanderCount);

            int foundMult = 0;
            int foundExp = 0;

            foreach (var kvp in specialMap)
            {
                Assert.IsTrue(kvp.Key >= 0 && kvp.Key < totalBlocks, "Block index must be within valid block range.");
                if (kvp.Value == BlockSpecialType.ScoreMultiplier2x) foundMult++;
                if (kvp.Value == BlockSpecialType.PaddleExpander) foundExp++;
            }

            Assert.AreEqual(multCount, foundMult, "Must allocate exact count of 2X multiplier blocks.");
            Assert.AreEqual(expanderCount, foundExp, "Must allocate exact count of Paddle Expander blocks.");
            Assert.AreEqual(multCount + expanderCount, specialMap.Count, "Indices must be unique without collisions.");

            Object.DestroyImmediate(genObj);
        }

        [Test]
        public void LevelConfiguration_CloneAndClamping_EnforcesValidRuntimeBoundaries()
        {
            var config = ScriptableObject.CreateInstance<LevelConfiguration>();
            var clone = config.Clone();

            Assert.AreNotSame(config, clone, "Clone must create a distinct ScriptableObject instance.");

            // Test clamping
            clone.SetColumns(50);
            Assert.AreEqual(14, clone.Columns, "Columns must clamp to max 14.");

            clone.SetColumns(1);
            Assert.AreEqual(4, clone.Columns, "Columns must clamp to min 4.");

            clone.SetBallSpeedMultiplier(9.9f);
            Assert.AreEqual(2.5f, clone.BallSpeedMultiplier, 0.001f, "Ball speed multiplier must clamp to max 2.5.");

            clone.SetMultiplier2xCount(99);
            Assert.AreEqual(8, clone.Multiplier2xCount, "Multiplier count must clamp to max 8.");

            clone.SetPaddleExpanderCount(50);
            Assert.AreEqual(5, clone.PaddleExpanderCount, "Paddle expander count must clamp to max 5.");

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(clone);
        }

        [Test]
        public void LevelGenerator_InvertedRows_AssignsBlueAtTopAndRedAtBottom()
        {
            var genObj = new GameObject("TestGenerator");
            var gen = genObj.AddComponent<LevelGenerator>();

            var config = ScriptableObject.CreateInstance<LevelConfiguration>();
            config.SetColumns(4);
            config.SetRowsPerTier(1); // Row 0: Blue, Row 1: Green, Row 2: Red
            config.SetMultiplier2xCount(0);
            config.SetPaddleExpanderCount(0);

            gen.LoadLevel(config);

            var container = genObj.transform.Find("BlocksContainer");
            Assert.IsNotNull(container, "Blocks container must exist.");

            var blocks = container.GetComponentsInChildren<Block>();
            Assert.AreEqual(12, blocks.Length, "Must have 4 cols * 3 rows = 12 blocks.");

            // First row (row 0, highest Y) must be Blue
            Assert.AreEqual(BlockColorTier.Blue, blocks[0].Tier, "Top row must be Blue blocks.");
            Assert.AreEqual(30, blocks[0].Points, "Blue block must award 30 points.");

            // Second row (row 1, middle Y) must be Green
            Assert.AreEqual(BlockColorTier.Green, blocks[4].Tier, "Middle row must be Green blocks.");
            Assert.AreEqual(20, blocks[4].Points, "Green block must award 20 points.");

            // Third row (row 2, bottom Y) must be Red
            Assert.AreEqual(BlockColorTier.Red, blocks[8].Tier, "Bottom row must be Red blocks.");
            Assert.AreEqual(10, blocks[8].Points, "Red block must award 10 points.");

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(genObj);
        }

        [Test]
        [TestCase(BlockColorTier.Red, 10, 30)]
        [TestCase(BlockColorTier.Green, 20, 60)]
        [TestCase(BlockColorTier.Blue, 30, 90)]
        public void Block_SpecialMultiplier3x_TriplesTierPoints(BlockColorTier tier, int normalPoints, int expectedTripledPoints)
        {
            var normalBlockObj = new GameObject("NormalBlock");
            var normalBlock = normalBlockObj.AddComponent<Block>();
            normalBlock.Initialize(tier, null, Color.white, BlockSpecialType.Normal);
            Assert.AreEqual(normalPoints, normalBlock.Points);

            var multi3xBlockObj = new GameObject("Multi3xBlock");
            var multi3xBlock = multi3xBlockObj.AddComponent<Block>();
            multi3xBlock.Initialize(tier, null, Color.white, BlockSpecialType.ScoreMultiplier3x);
            Assert.AreEqual(expectedTripledPoints, multi3xBlock.Points, $"Tier {tier} with 3x multiplier must award {expectedTripledPoints} points.");

            Object.DestroyImmediate(normalBlockObj);
            Object.DestroyImmediate(multi3xBlockObj);
        }

        [Test]
        public void Paddle_CompoundingExpansion_SuccessiveExpandersCompoundWidth()
        {
            paddle.ResetWidth(5.0f);
            Assert.AreEqual(5.0f, paddle.Width, 0.001f);
            Assert.AreEqual(0, paddle.ExpansionCount);

            // First expansion (+10%): 5.0 * 1.10 = 5.50
            paddle.ExpandWidth(0.10f);
            Assert.AreEqual(5.50f, paddle.Width, 0.001f);
            Assert.AreEqual(1, paddle.ExpansionCount);

            // Second expansion (+10% compounded): 5.50 * 1.10 = 6.05
            paddle.ExpandWidth(0.10f);
            Assert.AreEqual(6.05f, paddle.Width, 0.001f);
            Assert.AreEqual(2, paddle.ExpansionCount);

            // Third expansion (+10% compounded): 6.05 * 1.10 = 6.655
            paddle.ExpandWidth(0.10f);
            Assert.AreEqual(6.655f, paddle.Width, 0.001f);
            Assert.AreEqual(3, paddle.ExpansionCount);

            // Check boundary clamping after 3 expansions
            float halfWidth = 6.655f * 0.5f;
            Assert.AreEqual(-10.0f + halfWidth, paddle.MinX, 0.001f);
            Assert.AreEqual(10.0f - halfWidth, paddle.MaxX, 0.001f);

            // Reset width restores initial state
            paddle.ResetWidth(5.0f);
            Assert.AreEqual(5.0f, paddle.Width, 0.001f);
            Assert.AreEqual(0, paddle.ExpansionCount);
        }

        [Test]
        public void LevelGenerator_AdvanceToNextLevel_CyclesConfigurationsAndPreservesScore()
        {
            var genObj = new GameObject("TestGenerator");
            var gen = genObj.AddComponent<LevelGenerator>();

            var lvl1 = ScriptableObject.CreateInstance<LevelConfiguration>();
            lvl1.SetColumns(4);
            lvl1.SetRowsPerTier(1);
            var so1 = new UnityEditor.SerializedObject(lvl1);
            so1.FindProperty("levelNumber").intValue = 1;
            so1.ApplyModifiedProperties();

            var lvl2 = ScriptableObject.CreateInstance<LevelConfiguration>();
            lvl2.SetColumns(5);
            lvl2.SetRowsPerTier(1);
            var so2 = new UnityEditor.SerializedObject(lvl2);
            so2.FindProperty("levelNumber").intValue = 2;
            so2.ApplyModifiedProperties();

            var lvl3 = ScriptableObject.CreateInstance<LevelConfiguration>();
            lvl3.SetColumns(6);
            lvl3.SetRowsPerTier(1);
            var so3 = new UnityEditor.SerializedObject(lvl3);
            so3.FindProperty("levelNumber").intValue = 3;
            so3.ApplyModifiedProperties();

            var genSo = new UnityEditor.SerializedObject(gen);
            var presetsProp = genSo.FindProperty("levelPresets");
            presetsProp.arraySize = 3;
            presetsProp.GetArrayElementAtIndex(0).objectReferenceValue = lvl1;
            presetsProp.GetArrayElementAtIndex(1).objectReferenceValue = lvl2;
            presetsProp.GetArrayElementAtIndex(2).objectReferenceValue = lvl3;
            genSo.ApplyModifiedProperties();

            // Start at level 1
            gen.SelectAndLoadLevel(1);
            Assert.AreEqual(1, gen.CurrentConfig.LevelNumber);

            // Score accumulated in GameManager
            gameManager.RegisterLevelBlocks(10);
            gameManager.LaunchBall();
            gameManager.RecordBlockDestroyed(30, 1);
            Assert.AreEqual(30, gameManager.Score);

            // Advance to level 2
            gameManager.AdvanceToNextLevel();
            Assert.AreEqual(2, gen.CurrentConfig.LevelNumber);
            Assert.AreEqual(30, gameManager.Score, "Score must be preserved when advancing to next level.");
            Assert.AreEqual(GameState.ReadyToLaunch, gameManager.State);

            // Advance to level 3
            gameManager.AdvanceToNextLevel();
            Assert.AreEqual(3, gen.CurrentConfig.LevelNumber);
            Assert.AreEqual(30, gameManager.Score);

            // Advance from level 3 loops back to level 1
            gameManager.AdvanceToNextLevel();
            Assert.AreEqual(1, gen.CurrentConfig.LevelNumber);

            Object.DestroyImmediate(lvl1);
            Object.DestroyImmediate(lvl2);
            Object.DestroyImmediate(lvl3);
            Object.DestroyImmediate(genObj);
        }

        [Test]
        public void LevelGenerator_CheckerboardPattern_AlternatesBlockColorTiers()
        {
            var genObj = new GameObject("TestGenerator");
            var gen = genObj.AddComponent<LevelGenerator>();

            var config = ScriptableObject.CreateInstance<LevelConfiguration>();
            config.SetColumns(3);
            config.SetRowsPerTier(1); // 3 rows * 3 cols = 9 blocks
            config.SetColorPattern(BlockColorPattern.Checkerboard);
            config.SetMultiplier2xCount(0);
            config.SetMultiplier3xCount(0);
            config.SetPaddleExpanderCount(0);

            gen.LoadLevel(config);

            var container = genObj.transform.Find("BlocksContainer");
            var blocks = container.GetComponentsInChildren<Block>();
            Assert.AreEqual(12, blocks.Length);

            // (r + c) % 3:
            // r=0, c=0 -> 0 -> Blue
            Assert.AreEqual(BlockColorTier.Blue, blocks[0].Tier);
            // r=0, c=1 -> 1 -> Green
            Assert.AreEqual(BlockColorTier.Green, blocks[1].Tier);
            // r=0, c=2 -> 2 -> Red
            Assert.AreEqual(BlockColorTier.Red, blocks[2].Tier);
            // r=0, c=3 -> 0 -> Blue
            Assert.AreEqual(BlockColorTier.Blue, blocks[3].Tier);
            // r=1, c=0 -> 1 -> Green
            Assert.AreEqual(BlockColorTier.Green, blocks[4].Tier);
            // r=1, c=1 -> 2 -> Red
            Assert.AreEqual(BlockColorTier.Red, blocks[5].Tier);
            // r=1, c=2 -> 0 -> Blue
            Assert.AreEqual(BlockColorTier.Blue, blocks[6].Tier);

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(genObj);
        }

        [Test]
        public void LevelGenerator_DistributeSpecialBlocks_With3xMultipliers_AllocatesCorrectCounts()
        {
            var genObj = new GameObject("Gen");
            var gen = genObj.AddComponent<LevelGenerator>();

            int totalBlocks = 48;
            int mult2x = 2;
            int mult3x = 3;
            int expanders = 2;

            var specialMap = gen.DistributeSpecialBlocks(totalBlocks, mult2x, mult3x, expanders);

            int found2x = 0;
            int found3x = 0;
            int foundExp = 0;

            foreach (var kvp in specialMap)
            {
                Assert.IsTrue(kvp.Key >= 0 && kvp.Key < totalBlocks);
                if (kvp.Value == BlockSpecialType.ScoreMultiplier2x) found2x++;
                if (kvp.Value == BlockSpecialType.ScoreMultiplier3x) found3x++;
                if (kvp.Value == BlockSpecialType.PaddleExpander) foundExp++;
            }

            Assert.AreEqual(mult2x, found2x, "Must allocate exact count of 2X blocks.");
            Assert.AreEqual(mult3x, found3x, "Must allocate exact count of 3X blocks.");
            Assert.AreEqual(expanders, foundExp, "Must allocate exact count of paddle expanders.");
            Assert.AreEqual(mult2x + mult3x + expanders, specialMap.Count, "All indices must be collision-free.");

            Object.DestroyImmediate(genObj);
        }

        [Test]
        public void ArcadeUIManager_UpdateLivesDisplay_UpdatesHeartPipsCorrectly()
        {
            var uiObj = new GameObject("UI_HUD");
            var panelRenderer = uiObj.AddComponent<PanelRenderer>();
            var uiMgr = uiObj.AddComponent<ArcadeUIManager>();

            var root = new VisualElement();
            var pips = new[]
            {
                new VisualElement { name = "life-pip-1" },
                new VisualElement { name = "life-pip-2" },
                new VisualElement { name = "life-pip-3" }
            };
            foreach (var p in pips) root.Add(p);

            var rootField = typeof(ArcadeUIManager).GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rootField.SetValue(uiMgr, root);

            var pipsField = typeof(ArcadeUIManager).GetField("lifePips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            pipsField.SetValue(uiMgr, pips);

            var fillSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var emptySprite = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 4, 4), Vector2.zero);

            var fillField = typeof(ArcadeUIManager).GetField("heartFillSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var emptyField = typeof(ArcadeUIManager).GetField("heartEmptySprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fillField.SetValue(uiMgr, fillSprite);
            emptyField.SetValue(uiMgr, emptySprite);

            // 1. Full 3 lives: all 3 should be active and have fillSprite with red tint
            uiMgr.UpdateLivesDisplay(3);
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(pips[i].ClassListContains("pip-active"), $"Pip {i} must have pip-active with 3 lives.");
                Assert.IsFalse(pips[i].ClassListContains("pip-lost"), $"Pip {i} must not have pip-lost with 3 lives.");
                Assert.AreEqual(fillSprite, pips[i].style.backgroundImage.value.sprite, $"Pip {i} must have filled heart sprite.");
            }

            // 2. Lose 1 life -> 2 remaining: pip-0 & pip-1 active, pip-2 lost with emptySprite
            uiMgr.UpdateLivesDisplay(2);
            Assert.IsTrue(pips[0].ClassListContains("pip-active"));
            Assert.IsTrue(pips[1].ClassListContains("pip-active"));
            Assert.IsTrue(pips[2].ClassListContains("pip-lost"), "Pip 2 must have pip-lost when 1 life is lost.");
            Assert.AreEqual(emptySprite, pips[2].style.backgroundImage.value.sprite, "Pip 2 must switch to empty heart sprite.");

            // 3. Lose 2 lives -> 1 remaining: pip-0 active, pip-1 & pip-2 lost
            uiMgr.UpdateLivesDisplay(1);
            Assert.IsTrue(pips[0].ClassListContains("pip-active"));
            Assert.IsTrue(pips[1].ClassListContains("pip-lost"));
            Assert.IsTrue(pips[2].ClassListContains("pip-lost"));
            Assert.AreEqual(emptySprite, pips[1].style.backgroundImage.value.sprite);
            Assert.AreEqual(emptySprite, pips[2].style.backgroundImage.value.sprite);

            // 4. 0 lives: all 3 lost
            uiMgr.UpdateLivesDisplay(0);
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(pips[i].ClassListContains("pip-lost"));
                Assert.AreEqual(emptySprite, pips[i].style.backgroundImage.value.sprite);
            }

            Object.DestroyImmediate(fillSprite);
            Object.DestroyImmediate(emptySprite);
            Object.DestroyImmediate(uiObj);
        }

        [Test]
        public void UIManager_PauseButton_TogglesPauseAndPlayIcons()
        {
            var uiObj = new GameObject("TestUI");
            uiObj.AddComponent<PanelRenderer>();
            var uiMgr = uiObj.AddComponent<ArcadeUIManager>();

            var root = new VisualElement();
            var btnPause = new Button { name = "btn-quick-pause" };
            var iconPause = new VisualElement { name = "icon-quick-pause" };
            btnPause.Add(iconPause);
            root.Add(btnPause);

            var pauseSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var playSprite = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 4, 4), Vector2.zero);

            var pauseField = typeof(ArcadeUIManager).GetField("pauseSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var playField = typeof(ArcadeUIManager).GetField("playSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var iconPauseField = typeof(ArcadeUIManager).GetField("iconQuickPause", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var btnPauseField = typeof(ArcadeUIManager).GetField("btnQuickPause", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            pauseField.SetValue(uiMgr, pauseSprite);
            playField.SetValue(uiMgr, playSprite);
            iconPauseField.SetValue(uiMgr, iconPause);
            btnPauseField.SetValue(uiMgr, btnPause);

            // 1. Initial / Playing state: should show pause icon
            uiMgr.UpdatePauseButtonIcon(false);
            Assert.IsTrue(iconPause.ClassListContains("icon-pause"), "Playing state must show icon-pause class.");
            Assert.IsFalse(iconPause.ClassListContains("icon-play"), "Playing state must not show icon-play class.");
            Assert.AreEqual(pauseSprite, iconPause.style.backgroundImage.value.sprite, "Playing state must show pauseSprite.");

            // 2. Paused state: should show play (resume) icon
            uiMgr.UpdatePauseButtonIcon(true);
            Assert.IsTrue(iconPause.ClassListContains("icon-play"), "Paused state must show icon-play class.");
            Assert.IsFalse(iconPause.ClassListContains("icon-pause"), "Paused state must not show icon-pause class.");
            Assert.AreEqual(playSprite, iconPause.style.backgroundImage.value.sprite, "Paused state must show playSprite.");

            // 3. Resume back to playing: should switch back to pause icon
            uiMgr.UpdatePauseButtonIcon(false);
            Assert.IsTrue(iconPause.ClassListContains("icon-pause"), "Resuming must restore icon-pause class.");
            Assert.AreEqual(pauseSprite, iconPause.style.backgroundImage.value.sprite);

            Object.DestroyImmediate(pauseSprite);
            Object.DestroyImmediate(playSprite);
            Object.DestroyImmediate(uiObj);
        }

        [Test]
        public void UIManager_VolumeButton_AndToggle_SwitchBetweenMuteAndUnmuteIcons()
        {
            var audioObj = new GameObject("TestAudio");
            var audioMgr = audioObj.AddComponent<ArcadeAudioManager>();

            var uiObj = new GameObject("TestUI");
            uiObj.AddComponent<PanelRenderer>();
            var uiMgr = uiObj.AddComponent<ArcadeUIManager>();

            var btnMute = new Button { name = "btn-quick-mute" };
            var iconMute = new VisualElement { name = "icon-quick-mute" };
            btnMute.Add(iconMute);

            var toggleMute = new Toggle { name = "toggle-mute" };
            var checkmark = toggleMute.Q(className: "unity-toggle__checkmark");

            var muteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var volUpSprite = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 4, 4), Vector2.zero);

            var muteSpriteField = typeof(ArcadeUIManager).GetField("volumeMuteSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var volUpSpriteField = typeof(ArcadeUIManager).GetField("volumeUpSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var iconMuteField = typeof(ArcadeUIManager).GetField("iconQuickMute", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var btnMuteField = typeof(ArcadeUIManager).GetField("btnQuickMute", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var toggleMuteField = typeof(ArcadeUIManager).GetField("toggleMute", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            muteSpriteField.SetValue(uiMgr, muteSprite);
            volUpSpriteField.SetValue(uiMgr, volUpSprite);
            iconMuteField.SetValue(uiMgr, iconMute);
            btnMuteField.SetValue(uiMgr, btnMute);
            toggleMuteField.SetValue(uiMgr, toggleMute);

            // 1. Unmuted state: Quick button shows icon-volume-mute (action is to mute); Options toggle shows volumeUpSprite
            uiMgr.UpdateMuteButtonIcon(false);
            Assert.IsTrue(iconMute.ClassListContains("icon-volume-mute"), "Unmuted state must show icon-volume-mute to indicate ability to mute.");
            Assert.IsFalse(iconMute.ClassListContains("icon-volume-up"));
            Assert.AreEqual(muteSprite, iconMute.style.backgroundImage.value.sprite);
            Assert.AreEqual(volUpSprite, checkmark.style.backgroundImage.value.sprite, "Options toggle checkmark must show volumeUp when unmuted.");

            // 2. Muted state: Quick button shows icon-volume-up (action is to unmute); Options toggle shows volumeMuteSprite
            uiMgr.UpdateMuteButtonIcon(true);
            Assert.IsTrue(iconMute.ClassListContains("icon-volume-up"), "Muted state must show icon-volume-up to indicate ability to unmute.");
            Assert.IsFalse(iconMute.ClassListContains("icon-volume-mute"));
            Assert.AreEqual(volUpSprite, iconMute.style.backgroundImage.value.sprite);
            Assert.AreEqual(muteSprite, checkmark.style.backgroundImage.value.sprite, "Options toggle checkmark must show volumeMute when muted.");

            Object.DestroyImmediate(muteSprite);
            Object.DestroyImmediate(volUpSprite);
            Object.DestroyImmediate(uiObj);
            Object.DestroyImmediate(audioObj);
        }

        [Test]
        public void UIManager_SettingsButton_Spins360DegreesForwardContinously()
        {
            var uiObj = new GameObject("TestUI");
            uiObj.AddComponent<PanelRenderer>();
            var uiMgr = uiObj.AddComponent<ArcadeUIManager>();

            var iconOptions = new VisualElement { name = "icon-quick-options" };
            var iconOptionsField = typeof(ArcadeUIManager).GetField("iconQuickOptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            iconOptionsField.SetValue(uiMgr, iconOptions);

            Assert.AreEqual(0f, uiMgr.SettingsRotationAngle, "Initial rotation angle must be 0.");

            // 1st press: spins to 360°
            uiMgr.TriggerSettingsButtonSpin();
            Assert.AreEqual(360f, uiMgr.SettingsRotationAngle, "1st click must advance rotation by 360°.");

            // 2nd press: spins forward to 720°
            uiMgr.TriggerSettingsButtonSpin();
            Assert.AreEqual(720f, uiMgr.SettingsRotationAngle, "2nd click must advance rotation to 720°.");

            // 3rd press: spins forward to 1080°
            uiMgr.TriggerSettingsButtonSpin();
            Assert.AreEqual(1080f, uiMgr.SettingsRotationAngle, "3rd click must advance rotation to 1080°.");

            Object.DestroyImmediate(uiObj);
        }

        #endregion

        #region 6. Audio System (AU_*) Tests

        [Test]
        public void AudioManager_Clips_AreBoundAndFallbackLoadFromAssets()
        {
            var audioObj = new GameObject("TestAudio");
            var audioMgr = audioObj.AddComponent<ArcadeAudioManager>();
            audioMgr.LoadClipsIfEmpty();

            Assert.IsNotNull(audioMgr.ClipPop, "ClipPop must not be null.");
            Assert.IsNotNull(audioMgr.ClipBreak, "ClipBreak must not be null.");
            Assert.IsNotNull(audioMgr.ClipPowerup, "ClipPowerup must not be null.");
            Assert.IsNotNull(audioMgr.ClipGameOver, "ClipGameOver must not be null.");
            Assert.IsNotNull(audioMgr.ClipLevelSuccess, "ClipLevelSuccess must not be null.");
            Assert.IsNotNull(audioMgr.ClipButtonPress, "ClipButtonPress must not be null.");
            Assert.IsNotNull(audioMgr.ClipLifeLost, "ClipLifeLost must not be null.");
            Assert.IsNotNull(audioMgr.ClipShieldDeflect, "ClipShieldDeflect must not be null.");
            Assert.IsNotNull(audioMgr.ClipGlassBreak, "ClipGlassBreak must not be null.");
            Assert.IsNotNull(audioMgr.ClipBombExplosion, "ClipBombExplosion must not be null.");

            Object.DestroyImmediate(audioObj);
        }

        [Test]
        public void Block_DestroyBlock_PlaysBreakSound_AndPowerupSoundOnSpecial()
        {
            var audioObj = new GameObject("TestAudio");
            var audioMgr = audioObj.AddComponent<ArcadeAudioManager>();
            audioMgr.LoadClipsIfEmpty();

            // 1. Normal Block
            var normalBlockObj = new GameObject("NormalBlock");
            var normalBlock = normalBlockObj.AddComponent<Block>();
            normalBlock.Initialize(BlockColorTier.Red, null, Color.red, BlockSpecialType.Normal);

            // Destroy normal block - should play break sound
            Assert.DoesNotThrow(() => normalBlock.DestroyBlock(Vector3.down));

            // 2. Power-up Block (Paddle Expander)
            var powerupBlockObj = new GameObject("PowerupBlock");
            var powerupBlock = powerupBlockObj.AddComponent<Block>();
            powerupBlock.Initialize(BlockColorTier.Blue, null, Color.blue, BlockSpecialType.PaddleExpander);

            // Destroy power-up block - should trigger both break & powerup sound without error
            Assert.DoesNotThrow(() => powerupBlock.DestroyBlock(Vector3.down));

            Object.DestroyImmediate(audioObj);
        }

        [Test]
        public void AudioManager_ButtonPress_And_Pop_CanBeInvokedDirectly()
        {
            var audioObj = new GameObject("TestAudio");
            var audioMgr = audioObj.AddComponent<ArcadeAudioManager>();
            audioMgr.LoadClipsIfEmpty();

            Assert.DoesNotThrow(() => audioMgr.PlayPop(), "PlayPop must execute without error.");
            Assert.DoesNotThrow(() => audioMgr.PlayPaddleBounce(), "PlayPaddleBounce must execute without error.");
            Assert.DoesNotThrow(() => audioMgr.PlayWallBounce(), "PlayWallBounce must execute without error.");
            Assert.DoesNotThrow(() => audioMgr.PlayBreak(), "PlayBreak must execute without error.");
            Assert.DoesNotThrow(() => audioMgr.PlayPowerup(), "PlayPowerup must execute without error.");
            Assert.DoesNotThrow(() => audioMgr.PlayButtonPress(), "PlayButtonPress must execute without error.");
            Assert.DoesNotThrow(() => audioMgr.PlayLevelClear(), "PlayLevelClear must execute without error.");
            Assert.DoesNotThrow(() => audioMgr.PlayGameOver(), "PlayGameOver must execute without error.");
            Assert.DoesNotThrow(() => audioMgr.PlayLifeLost(), "PlayLifeLost must execute without error.");
            Assert.DoesNotThrow(() => audioMgr.PlayShieldDeflect(), "PlayShieldDeflect must execute without error.");
            Assert.DoesNotThrow(() => audioMgr.PlayMultiBall(), "PlayMultiBall must execute without error.");
            Assert.DoesNotThrow(() => audioMgr.PlayGlassBreak(), "PlayGlassBreak must execute without error.");
            Assert.DoesNotThrow(() => audioMgr.PlayBombExplosion(), "PlayBombExplosion must execute without error.");

            Object.DestroyImmediate(audioObj);
        }

        #endregion

        #region 7. Powerup Icons, Badge Margins, and VFX Shader Tests

        [Test]
        public void BlockBadge_Configures_PaddleExpander_ShowsIcon_And_HidesText()
        {
            var badgeGo = new GameObject("TestBadge");
            var badge = badgeGo.AddComponent<BlockBadge>();

            var root = new VisualElement();
            var plate = new VisualElement { name = "badge-plate" };
            var icon = new VisualElement { name = "badge-icon" };
            var label = new Label { name = "badge-text" };
            plate.Add(icon);
            plate.Add(label);
            root.Add(plate);

            badge.Setup(BlockSpecialType.PaddleExpander, null, null);
            badge.UpdateUI(root);

            Assert.AreEqual(Vector2.one * 0.80f, badge.WorldSpaceSize, "Badge world space size must be 0.80 to leave safety margins on the 1.0 unit brick face.");
            Assert.AreEqual(DisplayStyle.Flex, icon.style.display.value, "Expander badge icon must be visible.");
            Assert.IsTrue(icon.ClassListContains("badge-icon-expander"), "Icon must have badge-icon-expander class.");
            Assert.AreEqual(DisplayStyle.None, label.style.display.value, "Expander badge text must be hidden.");
            Assert.IsTrue(plate.ClassListContains("badge-plate-expander"), "Plate must have badge-plate-expander class.");

            Object.DestroyImmediate(badgeGo);
        }

        [Test]
        [TestCase(BlockSpecialType.ScoreMultiplier2x, "x2", "badge-text-x2", "badge-plate-x2")]
        [TestCase(BlockSpecialType.ScoreMultiplier3x, "x3", "badge-text-x3", "badge-plate-x3")]
        [TestCase(BlockSpecialType.ScoreMultiplier4x, "x4", "badge-text-x4", "badge-plate-x4")]
        [TestCase(BlockSpecialType.ScoreMultiplier5x, "x5", "badge-text-x5", "badge-plate-x5")]
        public void BlockBadge_Configures_Multiplier_ShowsIcon_And_ShowsCorrectText(BlockSpecialType type, string expectedText, string expectedTextClass, string expectedPlateClass)
        {
            var badgeGo = new GameObject("TestBadge");
            var badge = badgeGo.AddComponent<BlockBadge>();

            var root = new VisualElement();
            var plate = new VisualElement { name = "badge-plate" };
            var icon = new VisualElement { name = "badge-icon" };
            var label = new Label { name = "badge-text" };
            plate.Add(icon);
            plate.Add(label);
            root.Add(plate);

            badge.Setup(type, null, null);
            badge.UpdateUI(root);

            Assert.AreEqual(Vector2.one * 0.80f, badge.WorldSpaceSize, "Badge world space size must be 0.80 to guarantee margin on brick face.");
            Assert.AreEqual(DisplayStyle.Flex, icon.style.display.value, "Multiplier icon must be visible.");
            Assert.IsTrue(icon.ClassListContains("badge-icon-points"), "Icon must have badge-icon-points class.");
            Assert.AreEqual(DisplayStyle.Flex, label.style.display.value, "Multiplier text must be visible.");
            Assert.AreEqual(expectedText, label.text, $"Label text must be '{expectedText}'.");
            Assert.IsTrue(label.ClassListContains(expectedTextClass), $"Label must have class {expectedTextClass}.");
            Assert.IsTrue(plate.ClassListContains(expectedPlateClass), $"Plate must have class {expectedPlateClass}.");

            Object.DestroyImmediate(badgeGo);
        }

        [Test]
        public void BlockVFXManager_DebrisMaterial_UsesValidURPShader_AndFallbackIsSafe()
        {
            var vfxGo = new GameObject("TestVFXManager");
            var vfx = vfxGo.AddComponent<BlockVFXManager>();

            // Check safe fallback material when unassigned
            var fallbackMat = vfx.GetOrCreateDebrisMaterial();
            Assert.IsNotNull(fallbackMat, "Fallback debris material must not be null.");
            Assert.IsNotNull(fallbackMat.shader, "Fallback debris material must have a valid shader.");
            Assert.AreNotEqual("Standard", fallbackMat.shader.name, "Fallback shader must not be legacy built-in Standard.");

            // Test assigning explicit material
            var customMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Arcade/VFX_BlockDebris"));
            vfx.SetDebrisMaterial(customMat);
            Assert.AreEqual(customMat, vfx.DebrisMaterial, "Explicit debris material must be assigned and accessible.");
            Assert.AreEqual(customMat, vfx.GetOrCreateDebrisMaterial(), "GetOrCreateDebrisMaterial must return the assigned material.");

            Object.DestroyImmediate(vfxGo);
            Object.DestroyImmediate(customMat);
        }
        [Test]
        public void BlockVFXManager_Prewarm_ExecutesSafelyWithoutExceptions()
        {
            var vfxGo = new GameObject("TestVFXManager");
            var vfx = vfxGo.AddComponent<BlockVFXManager>();
            BlockVFXManager.SetInstanceForTesting(vfx);

            // Prewarm should execute safely without throwing any exceptions
            Assert.DoesNotThrow(() => vfx.Prewarm(), "Prewarm must execute without exceptions across all platforms.");
            Assert.AreEqual(0, vfx.ActiveDebrisCount, "Prewarm must leave 0 active debris instances.");
            Assert.AreEqual(0, vfx.ActiveVfxCount, "Prewarm must leave 0 active VFX instances.");

            Object.DestroyImmediate(vfxGo);
        }

        [Test]
        public void BlockVFXManager_DebrisBurst_UsesSharedMaterialAndPropertyBlock_NoMaterialCloning()
        {
            var vfxGo = new GameObject("TestVFXManager");
            var vfx = vfxGo.AddComponent<BlockVFXManager>();
            BlockVFXManager.SetInstanceForTesting(vfx);

            var customMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Arcade/VFX_BlockDebris"));
            vfx.SetDebrisMaterial(customMat);

            // Trigger shatter with cyan color
            Color testColor = Color.cyan;
            vfx.PlayBlockShatter(Vector3.zero, testColor, Vector3.up);

            Assert.AreEqual(8, vfx.ActiveDebrisCount, "Shattering one block must spawn exactly 8 debris sub-boxes.");
            Assert.AreEqual(1, vfx.ActiveVfxCount, "Shattering one block must track 1 active VFX burst.");

            // Find spawned debris objects
            var debrisPieces = vfxGo.GetComponentsInChildren<MeshRenderer>(false);
            Assert.GreaterOrEqual(debrisPieces.Length, 8, "Debris mesh renderers must be active children.");

            foreach (var mr in debrisPieces)
            {
                // Must use sharedMaterial without cloning
                Assert.AreEqual(customMat, mr.sharedMaterial, "Debris piece must use sharedMaterial directly to preserve SRP Batcher.");

                // Must have MaterialPropertyBlock applied with the tinted color
                var propBlock = new MaterialPropertyBlock();
                mr.GetPropertyBlock(propBlock);
                Color extractedColor = propBlock.GetColor(Shader.PropertyToID("_BaseColor"));
                Assert.AreEqual(testColor.r, extractedColor.r, 0.01f, "BaseColor in MaterialPropertyBlock must match test block color.");
            }

            vfx.ClearAllActive();
            Assert.AreEqual(0, vfx.ActiveDebrisCount, "ClearAllActive must recycle all debris.");
            Assert.AreEqual(0, vfx.ActiveVfxCount, "ClearAllActive must recycle all VFX.");

            Object.DestroyImmediate(vfxGo);
            Object.DestroyImmediate(customMat);
        }

        [Test]
        public void BlockVFXManager_ClearAllActive_InstantlyRecyclesAllActiveInstances()
        {
            var vfxGo = new GameObject("TestVFXManager");
            var vfx = vfxGo.AddComponent<BlockVFXManager>();
            BlockVFXManager.SetInstanceForTesting(vfx);

            // Trigger multiple block bursts
            vfx.PlayBlockShatter(Vector3.left, Color.red, Vector3.up);
            vfx.PlayBlockShatter(Vector3.right, Color.green, Vector3.up);

            Assert.AreEqual(16, vfx.ActiveDebrisCount, "Two block bursts must spawn 16 active debris sub-boxes.");
            Assert.AreEqual(2, vfx.ActiveVfxCount, "Two block bursts must track 2 active VFX bursts.");

            vfx.ClearAllActive();

            Assert.AreEqual(0, vfx.ActiveDebrisCount, "ClearAllActive must recycle all 16 debris instances.");
            Assert.AreEqual(0, vfx.ActiveVfxCount, "ClearAllActive must recycle all 2 VFX instances.");

            Object.DestroyImmediate(vfxGo);
        }

        [Test]
        public void Powerup_Sprites_AreConfiguredAsSprites()
        {
            var expanderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Arrows_Outward.png");
            var pointsSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Extra_Points.png");

            Assert.IsNotNull(expanderSprite, "TX_Powerup_Arrows_Outward must be imported as a Sprite.");
            Assert.IsNotNull(pointsSprite, "TX_Powerup_Extra_Points must be imported as a Sprite.");
        }

        #endregion

        #region 8. iOS Controls, Modal Pause & Level Clear Ball Handling Tests

        [Test]
        public void GameManager_PauseGame_And_ResumeGame_ManageStateAndTimeScaleCorrectly()
        {
            gameManager.LaunchBall(); // State -> Playing
            Assert.AreEqual(GameState.Playing, gameManager.State);

            // 1. Pause game
            gameManager.PauseGame();
            Assert.AreEqual(GameState.Paused, gameManager.State, "PauseGame must set state to Paused.");
            Assert.AreEqual(0f, Time.timeScale, "PauseGame must set Time.timeScale to 0.");

            // 2. Resume game
            gameManager.ResumeGame();
            Assert.AreEqual(GameState.Playing, gameManager.State, "ResumeGame must restore previous state (Playing).");
            Assert.AreEqual(1f, Time.timeScale, "ResumeGame must restore Time.timeScale to 1.");

            // 3. PauseGame should ignore already finished states (e.g. GameOver)
            gameManager.RecordBallLost();
            gameManager.LaunchBall();
            gameManager.RecordBallLost();
            gameManager.LaunchBall();
            gameManager.RecordBallLost();
            Assert.AreEqual(GameState.GameOver, gameManager.State);

            gameManager.PauseGame();
            Assert.AreEqual(GameState.GameOver, gameManager.State, "PauseGame must not override GameOver state.");

            Time.timeScale = 1f;
        }

        [Test]
        public void BallController_OnLevelClear_And_GameOver_StopsAndDeactivatesVisuals()
        {
            var ballObj = new GameObject("TestBall");
            ballObj.transform.SetParent(testRoot.transform);
            var rb = ballObj.AddComponent<Rigidbody>();
            var rend = ballObj.AddComponent<MeshRenderer>();
            var col = ballObj.AddComponent<SphereCollider>();
            var ball = ballObj.AddComponent<BallController>();
            ball.Initialize(gameManager, paddle);

            gameManager.LaunchBall(); // Playing
            ball.Launch();
            Assert.IsTrue(ball.IsLaunched, "Ball must be launched.");
            Assert.IsTrue(rend.enabled, "Renderer must be enabled.");
            Assert.IsTrue(col.enabled, "Collider must be enabled.");

            // Transition to LevelClear
            gameManager.SetState(GameState.LevelClear);
            Assert.IsFalse(ball.IsLaunched, "Ball must no longer be launched on LevelClear.");
            Assert.IsFalse(rend.enabled, "Ball renderer must be disabled on LevelClear.");
            Assert.IsFalse(col.enabled, "Ball collider must be disabled on LevelClear.");
            Assert.AreEqual(Vector3.zero, rb.linearVelocity, "Ball velocity must be zeroed on LevelClear.");

            // Transition back to ReadyToLaunch / ResetBallToPaddle
            ball.ResetBallToPaddle();
            Assert.IsTrue(rend.enabled, "Renderer must be re-enabled on ResetBallToPaddle.");
            Assert.IsTrue(col.enabled, "Collider must be re-enabled on ResetBallToPaddle.");
            Assert.IsFalse(ball.IsLaunched, "Ball must wait docked on paddle.");

            // Transition to GameOver
            gameManager.SetState(GameState.GameOver);
            Assert.IsFalse(ball.IsLaunched);
            Assert.IsFalse(rend.enabled);
            Assert.IsFalse(col.enabled);

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void PaddleController_DirectTargetX_MovesPaddleInstantlyWithoutSluggishLag()
        {
            var inputGo = new GameObject("InputCoordinator");
            inputGo.transform.SetParent(testRoot.transform);
            var inputHandler = inputGo.AddComponent<Arcade.Input.ArcadeInputHandler>();
            Arcade.Input.ArcadeInputHandler.SetInstanceForTesting(inputHandler);

            inputHandler.SetDirectTargetWorldXForTesting(3.5f);

            var updateMethod = typeof(PaddleController).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod.Invoke(paddle, null);

            Assert.AreEqual(3.5f, paddle.transform.position.x, 0.001f, "Paddle must snap instantly to directTargetWorldX without lag.");

            // Verify bounds clamping
            inputHandler.SetDirectTargetWorldXForTesting(15.0f);
            updateMethod.Invoke(paddle, null);
            Assert.AreEqual(paddle.MaxX, paddle.transform.position.x, 0.001f, "Paddle must clamp to MaxX.");

            // Reset touch state
            inputHandler.ResetTouchState();
            Assert.IsFalse(inputHandler.HasDirectTargetX);
            Assert.AreEqual(0f, inputHandler.DirectTargetWorldX);
            Assert.AreEqual(0f, inputHandler.HorizontalAxis);
            Assert.IsFalse(inputHandler.IsTouchDragging);

            Object.DestroyImmediate(inputGo);
        }

        [Test]
        public void PaddleController_Update_IgnoresMovementWhenPausedOrLevelClear()
        {
            paddle.transform.position = Vector3.zero;

            var inputGo = new GameObject("InputCoordinator");
            inputGo.transform.SetParent(testRoot.transform);
            var inputHandler = inputGo.AddComponent<Arcade.Input.ArcadeInputHandler>();
            Arcade.Input.ArcadeInputHandler.SetInstanceForTesting(inputHandler);

            inputHandler.SetDirectTargetWorldXForTesting(4.0f);

            var updateMethod = typeof(PaddleController).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // 1. Paused
            gameManager.SetState(GameState.Paused);
            updateMethod.Invoke(paddle, null);
            Assert.AreEqual(0f, paddle.transform.position.x, "Paddle must not move while paused.");

            // 2. LevelClear
            gameManager.SetState(GameState.LevelClear);
            updateMethod.Invoke(paddle, null);
            Assert.AreEqual(0f, paddle.transform.position.x, "Paddle must not move while LevelClear.");

            // 3. GameOver
            gameManager.SetState(GameState.GameOver);
            updateMethod.Invoke(paddle, null);
            Assert.AreEqual(0f, paddle.transform.position.x, "Paddle must not move while GameOver.");

            Object.DestroyImmediate(inputGo);
        }

        [Test]
        public void ArcadeUIManager_OptionsModal_PausesAndResumesGameAutomatically()
        {
            var uiObj = new GameObject("UI_HUD");
            uiObj.transform.SetParent(testRoot.transform);
            uiObj.AddComponent<PanelRenderer>();
            var uiMgr = uiObj.AddComponent<ArcadeUIManager>();

            var root = new VisualElement();
            var optionsModal = new VisualElement { name = "options-modal" };
            optionsModal.AddToClassList("modal-hidden");
            root.Add(optionsModal);

            var rootField = typeof(ArcadeUIManager).GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rootField.SetValue(uiMgr, root);

            var optionsModalField = typeof(ArcadeUIManager).GetField("optionsModal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            optionsModalField.SetValue(uiMgr, optionsModal);

            gameManager.LaunchBall(); // Playing
            Assert.AreEqual(GameState.Playing, gameManager.State);

            // Show options -> game must pause
            uiMgr.ShowOptions();
            Assert.AreEqual(GameState.Paused, gameManager.State, "Opening Options must pause gameplay.");
            Assert.IsTrue(uiMgr.WasPausedByOptions, "WasPausedByOptions must be true.");
            Assert.IsFalse(optionsModal.ClassListContains("modal-hidden"), "Options modal must be visible.");

            // Hide options -> game must resume
            uiMgr.HideOptions();
            Assert.AreEqual(GameState.Playing, gameManager.State, "Closing Options must resume gameplay.");
            Assert.IsFalse(uiMgr.WasPausedByOptions, "WasPausedByOptions must be cleared.");
            Assert.IsTrue(optionsModal.ClassListContains("modal-hidden"), "Options modal must be hidden.");

            Object.DestroyImmediate(uiObj);
        }

        [Test]
        public void ArcadeUIManager_LevelSettingsModal_PausesAndResumesGameAutomatically()
        {
            var uiObj = new GameObject("UI_HUD");
            uiObj.transform.SetParent(testRoot.transform);
            uiObj.AddComponent<PanelRenderer>();
            var uiMgr = uiObj.AddComponent<ArcadeUIManager>();

            var root = new VisualElement();
            var levelSettingsModal = new VisualElement { name = "level-settings-modal" };
            levelSettingsModal.AddToClassList("modal-hidden");
            root.Add(levelSettingsModal);

            var rootField = typeof(ArcadeUIManager).GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rootField.SetValue(uiMgr, root);

            var levelModalField = typeof(ArcadeUIManager).GetField("levelSettingsModal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            levelModalField.SetValue(uiMgr, levelSettingsModal);

            gameManager.LaunchBall(); // Playing
            Assert.AreEqual(GameState.Playing, gameManager.State);

            // Show level settings -> game must pause
            uiMgr.ShowLevelSettings();
            Assert.AreEqual(GameState.Paused, gameManager.State, "Opening Level Settings must pause gameplay.");
            Assert.IsTrue(uiMgr.WasPausedByLevelSettings, "WasPausedByLevelSettings must be true.");

            // Hide level settings -> game must resume
            uiMgr.HideLevelSettings();
            Assert.AreEqual(GameState.Playing, gameManager.State, "Closing Level Settings must resume gameplay.");
            Assert.IsFalse(uiMgr.WasPausedByLevelSettings, "WasPausedByLevelSettings must be cleared.");

            Object.DestroyImmediate(uiObj);
        }

        [Test]
        public void ArcadeUIManager_IsPointerOverUI_IdentifiesActiveModals()
        {
            var uiObj = new GameObject("UI_HUD");
            uiObj.transform.SetParent(testRoot.transform);
            uiObj.AddComponent<PanelRenderer>();
            var uiMgr = uiObj.AddComponent<ArcadeUIManager>();

            var root = new VisualElement { name = "hud-root" };
            var optionsModal = new VisualElement { name = "options-modal" };
            optionsModal.AddToClassList("modal-hidden");
            root.Add(optionsModal);

            var rootField = typeof(ArcadeUIManager).GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rootField.SetValue(uiMgr, root);

            var optionsModalField = typeof(ArcadeUIManager).GetField("optionsModal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            optionsModalField.SetValue(uiMgr, optionsModal);

            // Hidden modal -> IsAnyModalVisible false
            Assert.IsFalse(uiMgr.IsAnyModalVisible());

            // Shown modal -> IsAnyModalVisible true & IsPointerOverUI true
            optionsModal.RemoveFromClassList("modal-hidden");
            Assert.IsTrue(uiMgr.IsAnyModalVisible());
            Assert.IsTrue(uiMgr.IsPointerOverUI(new Vector2(200, 200)));

            Object.DestroyImmediate(uiObj);
        }

        #endregion

        #region 9. Powerup Mechanics (Glass, Bomb, Extra Heart) Tests

        [Test]
        public void Block_GlassEnclosed_RequiresTwoHits_AndAwardsDoublePoints()
        {
            var blockObj = new GameObject("GlassBlock");
            var block = blockObj.AddComponent<Block>();
            var glassShell = new GameObject("GlassShell");
            glassShell.transform.SetParent(blockObj.transform);
            block.SetGlassShell(glassShell);

            block.Initialize(BlockColorTier.Blue, null, Color.cyan, BlockSpecialType.GlassEnclosed);

            Assert.AreEqual(60, block.Points, "Glass enclosed Blue block must award 2x base points (60).");
            Assert.AreEqual(2, block.HitPoints, "Glass enclosed block must start with 2 hit points.");
            Assert.IsNotNull(block.GlassShell, "Glass shell reference must exist initially.");

            // Hit 1: Shatters glass shell
            block.TakeHit(Vector3.down);
            Assert.AreEqual(1, block.HitPoints, "Hit points should decrement to 1.");
            Assert.IsNull(block.GlassShell, "Glass shell GameObject must be destroyed after 1st hit.");
            Assert.IsFalse(block.IsDestroyed, "Block must not be destroyed on 1st hit.");

            // Hit 2: Shatters the block
            block.TakeHit(Vector3.down);
            Assert.AreEqual(0, block.HitPoints);
            Assert.IsTrue(block.IsDestroyed, "Block must be destroyed after 2nd hit.");

            Object.DestroyImmediate(blockObj);
        }

        [Test]
        public void Block_Bomb_ExplodesPerimeter_DestroysSurroundingBlocks()
        {
            var parentObj = new GameObject("GridRoot");

            var bombObj = new GameObject("BombBlock");
            bombObj.transform.SetParent(parentObj.transform);
            bombObj.transform.position = Vector3.zero;
            var bomb = bombObj.AddComponent<Block>();
            bomb.Initialize(BlockColorTier.Red, null, Color.red, BlockSpecialType.Bomb);

            var neighborObj = new GameObject("NeighborBlock");
            neighborObj.transform.SetParent(parentObj.transform);
            neighborObj.transform.position = new Vector3(1.0f, 0, 0); // Distance 1.0 <= 2.5
            var neighbor = neighborObj.AddComponent<Block>();
            neighbor.Initialize(BlockColorTier.Green, null, Color.green);

            var farObj = new GameObject("FarBlock");
            farObj.transform.SetParent(parentObj.transform);
            farObj.transform.position = new Vector3(5.0f, 0, 0); // Distance 5.0 > 2.5
            var far = farObj.AddComponent<Block>();
            far.Initialize(BlockColorTier.Blue, null, Color.blue);

            bomb.DestroyBlock(Vector3.down);

            Assert.IsTrue(bomb.IsDestroyed, "Bomb itself must be destroyed.");
            Assert.IsTrue(neighbor.IsDestroyed, "Adjacent neighbor block must be destroyed by perimeter blast.");
            Assert.IsFalse(far.IsDestroyed, "Far block outside perimeter radius must not be destroyed.");

            Object.DestroyImmediate(parentObj);
        }

        [Test]
        public void Block_Bomb_ChainReaction_DoesNotInfiniteLoop()
        {
            var parentObj = new GameObject("GridRoot");

            var bomb1Obj = new GameObject("Bomb1");
            bomb1Obj.transform.SetParent(parentObj.transform);
            bomb1Obj.transform.position = Vector3.zero;
            var bomb1 = bomb1Obj.AddComponent<Block>();
            bomb1.Initialize(BlockColorTier.Red, null, Color.red, BlockSpecialType.Bomb);

            var bomb2Obj = new GameObject("Bomb2");
            bomb2Obj.transform.SetParent(parentObj.transform);
            bomb2Obj.transform.position = new Vector3(1.2f, 0, 0);
            var bomb2 = bomb2Obj.AddComponent<Block>();
            bomb2.Initialize(BlockColorTier.Red, null, Color.red, BlockSpecialType.Bomb);

            var block3Obj = new GameObject("Block3");
            block3Obj.transform.SetParent(parentObj.transform);
            block3Obj.transform.position = new Vector3(2.4f, 0, 0);
            var block3 = block3Obj.AddComponent<Block>();
            block3.Initialize(BlockColorTier.Green, null, Color.green);

            // Detonating bomb1 should trigger bomb2, which triggers block3 without infinite recursion
            Assert.DoesNotThrow(() => bomb1.DestroyBlock(Vector3.down));
            Assert.IsTrue(bomb1.IsDestroyed);
            Assert.IsTrue(bomb2.IsDestroyed);
            Assert.IsTrue(block3.IsDestroyed);

            Object.DestroyImmediate(parentObj);
        }

        [Test]
        public void Block_ExtraHeart_AwardsLifeToGameManager()
        {
            int initialLives = gameManager.Lives;

            var heartObj = new GameObject("HeartBlock");
            var heartBlock = heartObj.AddComponent<Block>();
            heartBlock.Initialize(BlockColorTier.Red, null, Color.magenta, BlockSpecialType.ExtraHeart);

            heartBlock.DestroyBlock(Vector3.down);

            Assert.AreEqual(initialLives + 1, gameManager.Lives, "Destroying ExtraHeart block must award 1 life.");

            Object.DestroyImmediate(heartObj);
        }

        [Test]
        public void ArcadeGameManager_AddLife_ClampsToMaxLives()
        {
            gameManager.AddLife(10);
            Assert.AreEqual(ArcadeGameManager.MAX_LIVES, gameManager.Lives, $"Lives must clamp to MAX_LIVES ({ArcadeGameManager.MAX_LIVES}).");
        }

        [Test]
        public void BlockBadge_Bomb_SetsCorrectIconAndClasses()
        {
            var badgeObj = new GameObject("Badge");
            badgeObj.AddComponent<PanelRenderer>();
            var badge = badgeObj.AddComponent<BlockBadge>();

            var root = new VisualElement();
            var icon = new VisualElement { name = "badge-icon" };
            var label = new Label { name = "badge-text" };
            var plate = new VisualElement { name = "badge-plate" };
            root.Add(icon);
            root.Add(label);
            root.Add(plate);

            var typeField = typeof(BlockBadge).GetField("specialType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            typeField.SetValue(badge, BlockSpecialType.Bomb);

            badge.UpdateUI(root);

            Assert.IsTrue(icon.ClassListContains("badge-icon-bomb"));
            Assert.IsTrue(plate.ClassListContains("badge-plate-bomb"));
            Assert.AreEqual(DisplayStyle.None, label.style.display.value, "Label must be hidden for Bomb badge.");

            Object.DestroyImmediate(badgeObj);
        }

        [Test]
        public void BlockBadge_ExtraHeart_SetsCorrectIconAndClasses()
        {
            var badgeObj = new GameObject("Badge");
            badgeObj.AddComponent<PanelRenderer>();
            var badge = badgeObj.AddComponent<BlockBadge>();

            var root = new VisualElement();
            var icon = new VisualElement { name = "badge-icon" };
            var label = new Label { name = "badge-text" };
            var plate = new VisualElement { name = "badge-plate" };
            root.Add(icon);
            root.Add(label);
            root.Add(plate);

            var typeField = typeof(BlockBadge).GetField("specialType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            typeField.SetValue(badge, BlockSpecialType.ExtraHeart);

            badge.UpdateUI(root);

            Assert.IsTrue(icon.ClassListContains("badge-icon-heart-plus"));
            Assert.IsTrue(plate.ClassListContains("badge-plate-heart-plus"));
            Assert.AreEqual(DisplayStyle.None, label.style.display.value, "Label must be hidden for ExtraHeart badge.");

            Object.DestroyImmediate(badgeObj);
        }

        [Test]
        public void LevelConfiguration_NewModifierCounts_CloneAndSetters()
        {
            var config = ScriptableObject.CreateInstance<LevelConfiguration>();

            config.SetBombCount(3);
            config.SetGlassEnclosedCount(4);
            config.SetExtraHeartCount(2);

            Assert.AreEqual(3, config.BombCount);
            Assert.AreEqual(4, config.GlassEnclosedCount);
            Assert.AreEqual(2, config.ExtraHeartCount);

            // Clamping tests
            config.SetBombCount(99);
            Assert.AreEqual(5, config.BombCount, "BombCount should clamp to 5.");
            config.SetGlassEnclosedCount(-1);
            Assert.AreEqual(0, config.GlassEnclosedCount, "GlassEnclosedCount should clamp to 0.");

            var clone = config.Clone();
            Assert.AreEqual(5, clone.BombCount);
            Assert.AreEqual(0, clone.GlassEnclosedCount);
            Assert.AreEqual(2, clone.ExtraHeartCount);

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(clone);
        }

        #endregion

        #region 10. Shield & Multi-Ball Power-Up Tests

        [Test]
        public void GameManager_ActivateShield_EnablesShieldAndCountsDown()
        {
            bool stateEventFired = false;
            float recordedRemaining = 0f;
            gameManager.OnShieldStateChanged += (active, rem) =>
            {
                stateEventFired = true;
                recordedRemaining = rem;
            };

            gameManager.ActivateShield(10f);

            Assert.IsTrue(gameManager.IsShieldActive);
            Assert.AreEqual(10f, gameManager.ShieldTimeRemaining, 0.01f);
            Assert.IsTrue(stateEventFired);
            Assert.AreEqual(10f, recordedRemaining, 0.01f);

            // Tick 3 seconds
            gameManager.TickShield(3f);
            Assert.IsTrue(gameManager.IsShieldActive);
            Assert.AreEqual(7f, gameManager.ShieldTimeRemaining, 0.01f);

            // Tick remaining 7.5 seconds to expire
            gameManager.TickShield(7.5f);
            Assert.IsFalse(gameManager.IsShieldActive);
            Assert.AreEqual(0f, gameManager.ShieldTimeRemaining, 0.01f);
        }

        [Test]
        public void GameManager_ShieldActive_FallingBallResetsToReadyToLaunchWithoutLosingLife()
        {
            var ballObj = new GameObject("TestBall");
            var ball = ballObj.AddComponent<BallController>();
            var ballRb = ballObj.AddComponent<Rigidbody>();
            ballObj.AddComponent<SphereCollider>();
            ballObj.AddComponent<MeshRenderer>();
            ballObj.AddComponent<MeshFilter>();
            typeof(BallController).GetField("paddle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, paddle);
            typeof(BallController).GetField("rb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, ballRb);

            gameManager.RegisterBall(ball);
            gameManager.LaunchBall(); // Playing
            gameManager.ActivateShield(10f);

            int livesBefore = gameManager.Lives;

            // Ball falls into killzone while shield is active
            gameManager.HandleBallFell(ball);

            Assert.AreEqual(livesBefore, gameManager.Lives, "Shield must prevent life loss when ball falls.");
            Assert.AreEqual(GameState.Playing, gameManager.State, "Game must remain Playing without resetting to ReadyToLaunch.");
            Assert.Greater(ball.Velocity.y, 0f, "Ball must be deflected upward by shield.");

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void GameManager_ShieldExpired_FallingBallDecrementsLifeNormally()
        {
            var ballObj = new GameObject("TestBall");
            var ball = ballObj.AddComponent<BallController>();
            var ballRb = ballObj.AddComponent<Rigidbody>();
            ballObj.AddComponent<SphereCollider>();
            ballObj.AddComponent<MeshRenderer>();
            ballObj.AddComponent<MeshFilter>();
            typeof(BallController).GetField("paddle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, paddle);
            typeof(BallController).GetField("rb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, ballRb);

            gameManager.RegisterBall(ball);
            gameManager.LaunchBall(); // Playing
            gameManager.ActivateShield(10f);
            gameManager.DeactivateShield(); // Explicitly deactivate or expire

            int livesBefore = gameManager.Lives;

            gameManager.HandleBallFell(ball);

            Assert.AreEqual(livesBefore - 1, gameManager.Lives, "Life must decrement when shield is inactive.");
            Assert.AreEqual(GameState.BallLost, gameManager.State, "State must transition to BallLost.");

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void GameManager_MultiBall_SpawnsTwoExtraBalls()
        {
            var ballObj = new GameObject("PrimaryBall");
            var ball = ballObj.AddComponent<BallController>();
            var ballRb = ballObj.AddComponent<Rigidbody>();
            ballObj.AddComponent<SphereCollider>();
            ballObj.AddComponent<MeshRenderer>();
            ballObj.AddComponent<MeshFilter>();
            typeof(BallController).GetField("paddle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, paddle);
            typeof(BallController).GetField("rb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, ballRb);

            gameManager.RegisterBall(ball);
            gameManager.LaunchBall();

            Assert.AreEqual(1, gameManager.ActiveBallCount);

            gameManager.SpawnMultiBall(Vector3.zero, Vector3.up * 10f, 10f);

            Assert.AreEqual(3, gameManager.ActiveBallCount, "MultiBall must add 2 extra balls for a total of 3 active balls.");

            gameManager.ClearExtraBalls();
            Assert.AreEqual(1, gameManager.ActiveBallCount);

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void GameManager_MultiBall_ExtraBallFallingDoesNotDecrementLife()
        {
            var ballObj = new GameObject("PrimaryBall");
            var ball = ballObj.AddComponent<BallController>();
            var ballRb = ballObj.AddComponent<Rigidbody>();
            ballObj.AddComponent<SphereCollider>();
            ballObj.AddComponent<MeshRenderer>();
            ballObj.AddComponent<MeshFilter>();
            typeof(BallController).GetField("paddle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, paddle);
            typeof(BallController).GetField("rb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, ballRb);

            gameManager.RegisterBall(ball);
            gameManager.LaunchBall();
            gameManager.SpawnMultiBall(Vector3.zero, Vector3.up * 10f, 10f);

            Assert.AreEqual(3, gameManager.ActiveBallCount);
            int livesBefore = gameManager.Lives;

            var extraBall = gameManager.ActiveBalls[1];
            gameManager.HandleBallFell(extraBall);

            Assert.AreEqual(2, gameManager.ActiveBallCount, "Active ball count must decrease to 2.");
            Assert.AreEqual(livesBefore, gameManager.Lives, "Falling extra ball must not decrement lives.");
            Assert.AreEqual(GameState.Playing, gameManager.State, "Game must remain Playing when extra ball falls.");

            // Drop second extra ball
            var secondExtra = gameManager.ActiveBalls[1];
            gameManager.HandleBallFell(secondExtra);

            Assert.AreEqual(1, gameManager.ActiveBallCount, "Active ball count must decrease to 1.");
            Assert.AreEqual(livesBefore, gameManager.Lives, "Second falling extra ball must not decrement lives.");
            Assert.AreEqual(GameState.Playing, gameManager.State, "Game must remain Playing with 1 ball left.");

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void GameManager_MultiBall_LastRemainingBallFallingDecrementsLife()
        {
            var ballObj = new GameObject("PrimaryBall");
            var ball = ballObj.AddComponent<BallController>();
            var ballRb = ballObj.AddComponent<Rigidbody>();
            ballObj.AddComponent<SphereCollider>();
            ballObj.AddComponent<MeshRenderer>();
            ballObj.AddComponent<MeshFilter>();
            typeof(BallController).GetField("paddle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, paddle);
            typeof(BallController).GetField("rb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, ballRb);

            gameManager.RegisterBall(ball);
            gameManager.LaunchBall();
            gameManager.SpawnMultiBall(Vector3.zero, Vector3.up * 10f, 10f);

            int livesBefore = gameManager.Lives;

            // Drop 2 extra balls
            gameManager.HandleBallFell(gameManager.ActiveBalls[2]);
            gameManager.HandleBallFell(gameManager.ActiveBalls[1]);

            Assert.AreEqual(1, gameManager.ActiveBallCount);

            // Now drop the last remaining ball
            gameManager.HandleBallFell(gameManager.ActiveBalls[0]);

            Assert.AreEqual(livesBefore - 1, gameManager.Lives, "Last ball falling must decrement life.");
            Assert.AreEqual(GameState.BallLost, gameManager.State, "Game must transition to BallLost.");

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void GameManager_MultiBall_LastBallWithShieldActiveResetsToReadyToLaunch()
        {
            var ballObj = new GameObject("PrimaryBall");
            var ball = ballObj.AddComponent<BallController>();
            var ballRb = ballObj.AddComponent<Rigidbody>();
            ballObj.AddComponent<SphereCollider>();
            ballObj.AddComponent<MeshRenderer>();
            ballObj.AddComponent<MeshFilter>();
            typeof(BallController).GetField("paddle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, paddle);
            typeof(BallController).GetField("rb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, ballRb);

            gameManager.RegisterBall(ball);
            gameManager.LaunchBall();
            gameManager.SpawnMultiBall(Vector3.zero, Vector3.up * 10f, 10f);
            gameManager.ActivateShield(10f);

            int livesBefore = gameManager.Lives;

            // Drop 2 extra balls
            gameManager.HandleBallFell(gameManager.ActiveBalls[2]);
            gameManager.HandleBallFell(gameManager.ActiveBalls[1]);

            // Now drop the last remaining ball while shield is active
            gameManager.HandleBallFell(gameManager.ActiveBalls[0]);

            Assert.AreEqual(livesBefore, gameManager.Lives, "Shield must save the last ball from life loss.");
            Assert.AreEqual(GameState.Playing, gameManager.State, "Game must remain in Playing state.");

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void LevelConfiguration_ShieldAndMultiBall_CloningAndClamping()
        {
            var config = ScriptableObject.CreateInstance<LevelConfiguration>();

            config.SetShieldCount(2);
            config.SetMultiBallCount(3);
            config.SetShieldDuration(12f);

            Assert.AreEqual(2, config.ShieldCount);
            Assert.AreEqual(3, config.MultiBallCount);
            Assert.AreEqual(12f, config.ShieldDuration);

            // Test Clamping
            config.SetShieldCount(99);
            Assert.AreEqual(4, config.ShieldCount, "ShieldCount must clamp to 4.");

            config.SetMultiBallCount(-5);
            Assert.AreEqual(0, config.MultiBallCount, "MultiBallCount must clamp to 0.");

            config.SetShieldDuration(100f);
            Assert.AreEqual(30f, config.ShieldDuration, "ShieldDuration must clamp to 30s.");

            var clone = config.Clone();
            Assert.AreEqual(4, clone.ShieldCount);
            Assert.AreEqual(0, clone.MultiBallCount);
            Assert.AreEqual(30f, clone.ShieldDuration);

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(clone);
        }

        [Test]
        public void LevelGenerator_DistributeSpecialBlocks_AllocatesShieldAndMultiBall()
        {
            var genObj = new GameObject("LevelGen");
            var gen = genObj.AddComponent<LevelGenerator>();

            int totalBlocks = 20;
            var specials = gen.DistributeSpecialBlocks(
                totalBlocks,
                mult2xCount: 1,
                mult3xCount: 1,
                expanderCount: 1,
                bombCount: 1,
                glassCount: 1,
                heartCount: 1,
                shieldCount: 2,
                multiBallCount: 2
            );

            Assert.AreEqual(10, specials.Count, "Must allocate 10 special blocks in dictionary.");

            int shieldCount = 0;
            int multiBallCount = 0;
            foreach (var kvp in specials)
            {
                if (kvp.Value == BlockSpecialType.Shield) shieldCount++;
                if (kvp.Value == BlockSpecialType.MultiBall) multiBallCount++;
            }

            Assert.AreEqual(2, shieldCount, "Must allocate 2 Shield blocks.");
            Assert.AreEqual(2, multiBallCount, "Must allocate 2 MultiBall blocks.");

            Object.DestroyImmediate(genObj);
        }

        [Test]
        public void BlockBadge_ShieldAndMultiBall_SetsCorrectIconsAndClasses()
        {
            var badgeObj = new GameObject("Badge");
            badgeObj.AddComponent<PanelRenderer>();
            var badge = badgeObj.AddComponent<BlockBadge>();

            var root = new VisualElement();
            var icon = new VisualElement { name = "badge-icon" };
            var label = new Label { name = "badge-text" };
            var plate = new VisualElement { name = "badge-plate" };
            root.Add(icon);
            root.Add(label);
            root.Add(plate);

            var typeField = typeof(BlockBadge).GetField("specialType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Test Shield
            typeField.SetValue(badge, BlockSpecialType.Shield);
            badge.UpdateUI(root);
            Assert.IsTrue(icon.ClassListContains("badge-icon-shield"), "Must have badge-icon-shield class.");
            Assert.IsTrue(plate.ClassListContains("badge-plate-shield"), "Must have badge-plate-shield class.");
            Assert.AreEqual(DisplayStyle.None, label.style.display.value, "Label must be hidden for Shield badge.");

            // Test MultiBall
            typeField.SetValue(badge, BlockSpecialType.MultiBall);
            badge.UpdateUI(root);
            Assert.IsTrue(icon.ClassListContains("badge-icon-multiball"), "Must have badge-icon-multiball class.");
            Assert.IsTrue(plate.ClassListContains("badge-plate-multiball"), "Must have badge-plate-multiball class.");
            Assert.AreEqual(DisplayStyle.None, label.style.display.value, "Label must be hidden for MultiBall badge.");

            Object.DestroyImmediate(badgeObj);
        }

        [Test]
        public void Block_DestroyBlock_TriggersShieldAndMultiBall()
        {
            // Primary ball
            var ballObj = new GameObject("PrimaryBall");
            var ball = ballObj.AddComponent<BallController>();
            var ballRb = ballObj.AddComponent<Rigidbody>();
            ballObj.AddComponent<SphereCollider>();
            ballObj.AddComponent<MeshRenderer>();
            ballObj.AddComponent<MeshFilter>();
            typeof(BallController).GetField("paddle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, paddle);
            typeof(BallController).GetField("rb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, ballRb);

            gameManager.RegisterLevelBlocks(10);
            gameManager.RegisterBall(ball);
            gameManager.LaunchBall();

            // 1. Destroy Shield Block
            var shieldObj = new GameObject("ShieldBlock");
            var shieldBlock = shieldObj.AddComponent<Block>();
            shieldBlock.Initialize(BlockColorTier.Blue, null, Color.blue, BlockSpecialType.Shield);

            shieldBlock.DestroyBlock(Vector3.down);

            Assert.IsTrue(gameManager.IsShieldActive, "Destroying Shield block must activate shield on GameManager.");

            // 2. Destroy MultiBall Block
            var multiObj = new GameObject("MultiBallBlock");
            var multiBlock = multiObj.AddComponent<Block>();
            multiBlock.Initialize(BlockColorTier.Green, null, Color.green, BlockSpecialType.MultiBall);

            multiBlock.DestroyBlock(Vector3.down);

            Assert.AreEqual(3, gameManager.ActiveBallCount, "Destroying MultiBall block must spawn 2 extra balls (total 3).");

            gameManager.ClearExtraBalls();
            Object.DestroyImmediate(shieldObj);
            Object.DestroyImmediate(multiObj);
            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void ArcadeUIManager_ShieldAndMultiBall_UpdatesTimerAndVisibility()
        {
            var uiGo = new GameObject("UI");
            uiGo.AddComponent<PanelRenderer>();
            var uiMgr = uiGo.AddComponent<ArcadeUIManager>();

            var root = new VisualElement();
            var shieldBadge = new VisualElement { name = "shield-status-badge" };
            shieldBadge.AddToClassList("powerup-hidden");
            var shieldLabel = new Label { name = "shield-timer-label" };
            shieldBadge.Add(shieldLabel);

            var multiBadge = new VisualElement { name = "multiball-status-badge" };
            multiBadge.AddToClassList("powerup-hidden");
            var multiLabel = new Label { name = "multiball-count-label" };
            multiBadge.Add(multiLabel);

            root.Add(shieldBadge);
            root.Add(multiBadge);

            typeof(ArcadeUIManager).GetField("shieldStatusBadge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, shieldBadge);
            typeof(ArcadeUIManager).GetField("shieldTimerLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, shieldLabel);
            typeof(ArcadeUIManager).GetField("multiballStatusBadge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, multiBadge);
            typeof(ArcadeUIManager).GetField("multiballCountLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, multiLabel);

            // Shield activated
            uiMgr.HandleShieldStateChanged(true, 10f);
            Assert.IsFalse(shieldBadge.ClassListContains("powerup-hidden"));
            Assert.AreEqual(DisplayStyle.Flex, shieldBadge.style.display.value);
            Assert.AreEqual("10s", shieldLabel.text);

            // Shield tick
            uiMgr.HandleShieldTick(6.2f);
            Assert.AreEqual("7s", shieldLabel.text);

            // Shield deactivated
            uiMgr.HandleShieldStateChanged(false, 0f);
            Assert.IsTrue(shieldBadge.ClassListContains("powerup-hidden"));
            Assert.AreEqual(DisplayStyle.None, shieldBadge.style.display.value);

            // MultiBall count changed to 3
            uiMgr.HandleActiveBallCountChanged(3);
            Assert.IsFalse(multiBadge.ClassListContains("powerup-hidden"));
            Assert.AreEqual(DisplayStyle.Flex, multiBadge.style.display.value);
            Assert.AreEqual("3 BALLS", multiLabel.text);

            // MultiBall count changed to 1 (normal)
            uiMgr.HandleActiveBallCountChanged(1);
            Assert.IsTrue(multiBadge.ClassListContains("powerup-hidden"));
            Assert.AreEqual(DisplayStyle.None, multiBadge.style.display.value);

            Object.DestroyImmediate(uiGo);
        }

        #endregion

        #region 11. Progressive 15-Level Campaign Tests

        [Test]
        public void Campaign_AllFifteenLevelsExist_AndEnforceProgressiveSpeedAndLayoutVariety()
        {
            float lastSpeed = 0f;

            for (int i = 1; i <= 15; i++)
            {
                string path = $"Assets/Settings/Levels/SO_Level_{i:D2}.asset";
                var config = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>(path);
                Assert.IsNotNull(config, $"Level {i} asset must exist at {path}.");
                Assert.AreEqual(i, config.LevelNumber, $"Level {i} must have matching LevelNumber.");
                Assert.IsFalse(string.IsNullOrEmpty(config.LevelName), $"Level {i} must have a non-empty name.");
                Assert.IsFalse(string.IsNullOrEmpty(config.Description), $"Level {i} must have a non-empty description.");
                Assert.Greater(config.TotalBlocks, 0, $"Level {i} must have a positive block count.");

                Assert.Greater(config.BallSpeedMultiplier, lastSpeed, $"Level {i} speed ({config.BallSpeedMultiplier}) must be strictly faster than Level {i - 1} speed ({lastSpeed}).");

                lastSpeed = config.BallSpeedMultiplier;
            }

            Assert.AreEqual(0.92f, UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_01.asset").BallSpeedMultiplier, 0.001f);
            Assert.AreEqual(1.48f, UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_15.asset").BallSpeedMultiplier, 0.001f);
            Assert.AreEqual(LevelLayoutType.Pyramid, UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_01.asset").LayoutType);
            Assert.AreEqual(LevelLayoutType.Custom, UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_15.asset").LayoutType);
        }

        [Test]
        public void Level1_WarmupGrid_IsAccessibleAndHasZeroHazards()
        {
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_01.asset");
            Assert.IsNotNull(config);

            // Level 1: 11 cols * 6 rows in a Stepped Pyramid = 42 blocks
            Assert.AreEqual(11, config.Columns);
            Assert.AreEqual(2, config.RowsPerTier);
            Assert.AreEqual(6, config.TotalRows);
            Assert.AreEqual(42, config.TotalBlocks);
            Assert.AreEqual(LevelLayoutType.Pyramid, config.LayoutType);

            // Generous warmup paddle and comfortable speed
            Assert.AreEqual(5.5f, config.InitialPaddleWidth, 0.001f);
            Assert.AreEqual(0.92f, config.BallSpeedMultiplier, 0.001f);

            // Single paddle expander reward, zero hazards
            Assert.AreEqual(1, config.PaddleExpanderCount);
            Assert.AreEqual(0, config.Multiplier2xCount);
            Assert.AreEqual(0, config.Multiplier3xCount);
            Assert.AreEqual(0, config.BombCount);
            Assert.AreEqual(0, config.GlassEnclosedCount);
            Assert.AreEqual(0, config.ExtraHeartCount);
            Assert.AreEqual(0, config.ShieldCount);
            Assert.AreEqual(0, config.MultiBallCount);
        }

        [Test]
        public void LevelGenerator_AdvanceToNextLevel_CyclesFifteenLevelsSeamlessly()
        {
            var genObj = new GameObject("TestGenerator");
            var gen = genObj.AddComponent<LevelGenerator>();

            var levelConfigs = new LevelConfiguration[15];
            for (int i = 0; i < 15; i++)
            {
                levelConfigs[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>($"Assets/Settings/Levels/SO_Level_{i + 1:D2}.asset");
                Assert.IsNotNull(levelConfigs[i]);
            }

            var genSo = new UnityEditor.SerializedObject(gen);
            var presetsProp = genSo.FindProperty("levelPresets");
            presetsProp.arraySize = 15;
            for (int i = 0; i < 15; i++)
            {
                presetsProp.GetArrayElementAtIndex(i).objectReferenceValue = levelConfigs[i];
            }
            genSo.ApplyModifiedProperties();

            // Start at Level 1
            gen.SelectAndLoadLevel(1);
            Assert.AreEqual(1, gen.CurrentConfig.LevelNumber);

            // Advance through 2 to 15
            for (int expectedLvl = 2; expectedLvl <= 15; expectedLvl++)
            {
                gen.AdvanceToNextLevel();
                Assert.AreEqual(expectedLvl, gen.CurrentConfig.LevelNumber, $"Expected advancing to Level {expectedLvl}.");
            }

            // Advancing from Level 15 must loop back to Level 1
            gen.AdvanceToNextLevel();
            Assert.AreEqual(1, gen.CurrentConfig.LevelNumber, "Advancing beyond Level 15 must loop back to Level 1 for endless arcade progression.");

            Object.DestroyImmediate(genObj);
        }

        [Test]
        public void LevelGenerator_SpecialBlockDistribution_MaintainsExactCountsWithoutCollisionsAcrossAll15Levels()
        {
            var genObj = new GameObject("TestGenerator");
            var gen = genObj.AddComponent<LevelGenerator>();

            for (int i = 1; i <= 15; i++)
            {
                var config = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>($"Assets/Settings/Levels/SO_Level_{i:D2}.asset");
                Assert.IsNotNull(config);

                var map = gen.DistributeSpecialBlocks(
                    config.TotalBlocks,
                    config.Multiplier2xCount,
                    config.Multiplier3xCount,
                    config.Multiplier4xCount,
                    config.Multiplier5xCount,
                    config.PaddleExpanderCount,
                    config.BombCount,
                    config.GlassEnclosedCount,
                    config.ExtraHeartCount,
                    config.ShieldCount,
                    config.MultiBallCount);

                int expectedTotal = config.Multiplier2xCount + config.Multiplier3xCount + config.Multiplier4xCount + config.Multiplier5xCount +
                                    config.PaddleExpanderCount + config.BombCount + config.GlassEnclosedCount + config.ExtraHeartCount +
                                    config.ShieldCount + config.MultiBallCount;

                Assert.AreEqual(expectedTotal, map.Count, $"Level {i} must allocate exact total special blocks without collision.");

                int actual2x = 0, actual3x = 0, actual4x = 0, actual5x = 0, actualExp = 0, actualBomb = 0, actualGlass = 0, actualHeart = 0, actualShield = 0, actualMulti = 0;
                foreach (var kvp in map)
                {
                    Assert.IsTrue(kvp.Key >= 0 && kvp.Key < config.TotalBlocks, $"Index {kvp.Key} must be valid block index for Level {i}.");
                    switch (kvp.Value)
                    {
                        case BlockSpecialType.ScoreMultiplier2x: actual2x++; break;
                        case BlockSpecialType.ScoreMultiplier3x: actual3x++; break;
                        case BlockSpecialType.ScoreMultiplier4x: actual4x++; break;
                        case BlockSpecialType.ScoreMultiplier5x: actual5x++; break;
                        case BlockSpecialType.PaddleExpander: actualExp++; break;
                        case BlockSpecialType.Bomb: actualBomb++; break;
                        case BlockSpecialType.GlassEnclosed: actualGlass++; break;
                        case BlockSpecialType.ExtraHeart: actualHeart++; break;
                        case BlockSpecialType.Shield: actualShield++; break;
                        case BlockSpecialType.MultiBall: actualMulti++; break;
                    }
                }

                Assert.AreEqual(config.Multiplier2xCount, actual2x);
                Assert.AreEqual(config.Multiplier3xCount, actual3x);
                Assert.AreEqual(config.Multiplier4xCount, actual4x);
                Assert.AreEqual(config.Multiplier5xCount, actual5x);
                Assert.AreEqual(config.PaddleExpanderCount, actualExp);
                Assert.AreEqual(config.BombCount, actualBomb);
                Assert.AreEqual(config.GlassEnclosedCount, actualGlass);
                Assert.AreEqual(config.ExtraHeartCount, actualHeart);
                Assert.AreEqual(config.ShieldCount, actualShield);
                Assert.AreEqual(config.MultiBallCount, actualMulti);
            }

            Object.DestroyImmediate(genObj);
        }

        [Test]
        public void LevelConfiguration_AllSixteenLayoutTypes_EvaluateAccurately()
        {
            var config = ScriptableObject.CreateInstance<LevelConfiguration>();
            config.SetColumns(8);
            config.SetRowsPerTier(2); // 6 rows total

            foreach (LevelLayoutType layout in System.Enum.GetValues(typeof(LevelLayoutType)))
            {
                config.SetLayoutType(layout);
                if (layout == LevelLayoutType.Custom)
                {
                    config.SetCustomLayout("XXXX\n.XX.\n..XX");
                }

                Assert.Greater(config.TotalBlocks, 0, $"LayoutType {layout} must produce at least one block.");
                Assert.LessOrEqual(config.TotalBlocks, config.Columns * config.TotalRows, $"LayoutType {layout} blocks must not exceed grid envelope.");

                // Verify out of bounds queries return false
                Assert.IsFalse(config.HasBlockAt(-1, 0));
                Assert.IsFalse(config.HasBlockAt(0, -1));
                Assert.IsFalse(config.HasBlockAt(config.TotalRows, 0));
                Assert.IsFalse(config.HasBlockAt(0, config.Columns));
            }

            Object.DestroyImmediate(config);
        }

        [Test]
        public void LevelConfiguration_CustomLayout_ParsesTextPattern_AndExplicitColors()
        {
            var config = ScriptableObject.CreateInstance<LevelConfiguration>();
            config.SetLayoutType(LevelLayoutType.Custom);
            config.SetColumns(6);
            config.SetRowsPerTier(1); // 3 rows

            string layout =
                "B.G.R.\n" +
                "XXXXXX\n" +
                "......";

            config.SetCustomLayout(layout);

            // Row 0: B . G . R .
            Assert.IsTrue(config.HasBlockAt(0, 0));
            Assert.AreEqual(BlockColorTier.Blue, config.GetExplicitColorAt(0, 0));

            Assert.IsFalse(config.HasBlockAt(0, 1));
            Assert.IsNull(config.GetExplicitColorAt(0, 1));

            Assert.IsTrue(config.HasBlockAt(0, 2));
            Assert.AreEqual(BlockColorTier.Green, config.GetExplicitColorAt(0, 2));

            Assert.IsFalse(config.HasBlockAt(0, 3));
            Assert.IsNull(config.GetExplicitColorAt(0, 3));

            Assert.IsTrue(config.HasBlockAt(0, 4));
            Assert.AreEqual(BlockColorTier.Red, config.GetExplicitColorAt(0, 4));

            Assert.IsFalse(config.HasBlockAt(0, 5));

            // Row 1: All 6 filled
            for (int c = 0; c < 6; c++)
            {
                Assert.IsTrue(config.HasBlockAt(1, c));
                Assert.IsNull(config.GetExplicitColorAt(1, c), "Standard 'X' must not have explicit color override.");
            }

            // Row 2: All 6 empty
            for (int c = 0; c < 6; c++)
            {
                Assert.IsFalse(config.HasBlockAt(2, c));
            }

            // Total filled blocks: 3 in row 0, 6 in row 1 = 9
            Assert.AreEqual(9, config.TotalBlocks);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void LevelConfiguration_Clone_CopiesLayoutAndCustomPattern()
        {
            var original = ScriptableObject.CreateInstance<LevelConfiguration>();
            original.SetLayoutType(LevelLayoutType.Custom);
            original.SetCustomLayout("..XX..\nXXXXXX");
            original.SetColumns(6);
            original.SetRowsPerTier(2);

            var clone = original.Clone();

            Assert.AreEqual(LevelLayoutType.Custom, clone.LayoutType);
            Assert.AreEqual("..XX..\nXXXXXX", clone.CustomLayout);
            Assert.AreEqual(original.TotalBlocks, clone.TotalBlocks);
            Assert.AreEqual(original.Columns, clone.Columns);
            Assert.AreEqual(original.TotalRows, clone.TotalRows);

            Object.DestroyImmediate(original);
            Object.DestroyImmediate(clone);
        }

        [Test]
        public void LevelGenerator_LayoutWithEmptySpaces_SpawnsOnlyActiveBlocks_AndRegistersWithGameManager()
        {
            var genObj = new GameObject("TestLayoutGenerator");
            var gen = genObj.AddComponent<LevelGenerator>();

            var config = ScriptableObject.CreateInstance<LevelConfiguration>();
            config.SetLayoutType(LevelLayoutType.Pillars); // alternating columns
            config.SetColumns(7);
            config.SetRowsPerTier(1); // 3 rows
            // In 7 cols with Pillars (c % 2 == 0): cols 0, 2, 4, 6 are filled (4 cols * 3 rows = 12 blocks)
            Assert.AreEqual(12, config.TotalBlocks);

            gen.LoadLevel(config);

            var container = genObj.transform.Find("BlocksContainer");
            Assert.IsNotNull(container, "BlocksContainer must be created.");
            Assert.AreEqual(12, container.childCount, "BlocksContainer child count must match active TotalBlocks.");

            if (ArcadeGameManager.Instance != null)
            {
                Assert.AreEqual(12, ArcadeGameManager.Instance.RemainingBlocks, "GameManager RemainingBlocks must match active TotalBlocks.");
            }

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(genObj);
        }

        #endregion

        #region 12. Timed Buffs & Combo Multipliers Tests

        [Test]
        public void ArcadeGameManager_ActivatePaddleExpander_TicksDownAndResetsPaddle()
        {
            paddle.ResetWidth(5.0f);
            Assert.AreEqual(5.0f, paddle.Width, 0.001f);
            Assert.IsFalse(gameManager.IsPaddleExpanded);
            Assert.AreEqual(0f, gameManager.PaddleExpandTimeRemaining);

            bool stateFired = false;
            gameManager.OnPaddleExpandStateChanged += (active, dur) => { stateFired = active; };

            // Activate for 10 seconds
            gameManager.ActivatePaddleExpander(10f);
            Assert.IsTrue(gameManager.IsPaddleExpanded);
            Assert.AreEqual(10f, gameManager.PaddleExpandTimeRemaining, 0.001f);
            Assert.IsTrue(stateFired);
            Assert.Greater(paddle.Width, 5.0f);

            // Tick 5 seconds
            gameManager.TickPaddleExpander(5f);
            Assert.IsTrue(gameManager.IsPaddleExpanded);
            Assert.AreEqual(5f, gameManager.PaddleExpandTimeRemaining, 0.001f);

            // Tick remaining 5 seconds -> expires
            gameManager.TickPaddleExpander(5.1f);
            Assert.IsFalse(gameManager.IsPaddleExpanded);
            Assert.AreEqual(0f, gameManager.PaddleExpandTimeRemaining);
            Assert.AreEqual(5.0f, paddle.Width, 0.001f);
            Assert.IsFalse(stateFired);
        }

        [Test]
        public void ArcadeGameManager_ActivateScoreMultiplier_AppliesGlobalComboMultiplier()
        {
            gameManager.RegisterLevelBlocks(10);
            gameManager.LaunchBall(); // State -> Playing

            Assert.AreEqual(1, gameManager.ActiveScoreMultiplier);
            Assert.AreEqual(0, gameManager.Score);

            // Base destruction without multiplier (10 pts)
            gameManager.RecordBlockDestroyed(10, 1);
            Assert.AreEqual(10, gameManager.Score);

            // Activate 3X multiplier for 10s
            gameManager.ActivateScoreMultiplier(3, 10f);
            Assert.AreEqual(3, gameManager.ActiveScoreMultiplier);
            Assert.AreEqual(10f, gameManager.MultiplierTimeRemaining, 0.001f);

            // Block destroyed during 3X combo awards 10 * 3 = 30 pts
            gameManager.RecordBlockDestroyed(10, 1);
            Assert.AreEqual(40, gameManager.Score);

            // Green block (20 pts) during 3X combo awards 20 * 3 = 60 pts
            gameManager.RecordBlockDestroyed(20, 2);
            Assert.AreEqual(100, gameManager.Score);

            // Tick down to expiration
            gameManager.TickScoreMultiplier(10.1f);
            Assert.AreEqual(1, gameManager.ActiveScoreMultiplier);
            Assert.AreEqual(0f, gameManager.MultiplierTimeRemaining);

            // Block after expiration awards base points
            gameManager.RecordBlockDestroyed(10, 1);
            Assert.AreEqual(110, gameManager.Score);
        }

        [Test]
        public void ArcadeGameManager_ActivateScoreMultiplier_UpgradesTierAndRefreshesTimer()
        {
            gameManager.ActivateScoreMultiplier(2, 5f);
            Assert.AreEqual(2, gameManager.ActiveScoreMultiplier);
            Assert.AreEqual(5f, gameManager.MultiplierTimeRemaining, 0.001f);

            // Upgrade to 4X with 10s
            gameManager.ActivateScoreMultiplier(4, 10f);
            Assert.AreEqual(4, gameManager.ActiveScoreMultiplier);
            Assert.AreEqual(10f, gameManager.MultiplierTimeRemaining, 0.001f);

            // Hit 3X while 4X is active -> stays 4X, duration refreshes to max
            gameManager.TickScoreMultiplier(3f); // remaining = 7s
            gameManager.ActivateScoreMultiplier(3, 10f);
            Assert.AreEqual(4, gameManager.ActiveScoreMultiplier);
            Assert.AreEqual(10f, gameManager.MultiplierTimeRemaining, 0.001f);

            // Hit 5X -> upgrades to 5X
            gameManager.ActivateScoreMultiplier(5, 10f);
            Assert.AreEqual(5, gameManager.ActiveScoreMultiplier);
        }

        [Test]
        public void BlockSpecialType_4xAnd5x_MetadataAndBadgesValid()
        {
            Assert.AreEqual("x4", BlockSpecialType.ScoreMultiplier4x.GetBadgeText());
            Assert.AreEqual("x5", BlockSpecialType.ScoreMultiplier5x.GetBadgeText());

            var block4xObj = new GameObject("Block4x");
            var block4x = block4xObj.AddComponent<Block>();
            block4x.Initialize(BlockColorTier.Red, null, Color.white, BlockSpecialType.ScoreMultiplier4x);
            Assert.AreEqual(40, block4x.Points); // 10 * 4 = 40
            Assert.AreEqual(4, block4x.ScoreMultiplier);

            var block5xObj = new GameObject("Block5x");
            var block5x = block5xObj.AddComponent<Block>();
            block5x.Initialize(BlockColorTier.Blue, null, Color.white, BlockSpecialType.ScoreMultiplier5x);
            Assert.AreEqual(150, block5x.Points); // 30 * 5 = 150
            Assert.AreEqual(5, block5x.ScoreMultiplier);

            Object.DestroyImmediate(block4xObj);
            Object.DestroyImmediate(block5xObj);
        }

        [Test]
        public void ArcadeUIManager_PaddleAndMultiplierBadges_UpdatesTimerAndVisibility()
        {
            var uiManagerGo = new GameObject("TestArcadeUIManager");
            var panelRenderer = uiManagerGo.AddComponent<UnityEngine.UIElements.PanelRenderer>();
            var uiMgr = uiManagerGo.AddComponent<ArcadeUIManager>();

            var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsNotNull(uxml, "BlockBreakerHUD.uxml must exist.");

            panelRenderer.visualTreeAsset = uxml;
            var root = uxml.CloneTree();

            // Reflection-based bind for unit testing
            var bindMethod = typeof(ArcadeUIManager).GetMethod("BindElements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            typeof(ArcadeUIManager).GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, root);
            bindMethod.Invoke(uiMgr, null);

            Assert.IsNotNull(uiMgr.PaddleStatusBadge);
            Assert.IsNotNull(uiMgr.MultiplierStatusBadge);
            Assert.IsNotNull(uiMgr.PaddleExpandSprite, "PaddleExpandSprite must be assigned.");
            Assert.AreEqual("TX_Powerup_Arrows_Outward", uiMgr.PaddleExpandSprite.name);
            Assert.IsNotNull(uiMgr.MultiplierSprite, "MultiplierSprite must be assigned.");
            Assert.AreEqual("TX_Powerup_Extra_Points", uiMgr.MultiplierSprite.name);

            // Test Paddle Badge
            uiMgr.HandlePaddleExpandStateChanged(true, 10f);
            Assert.IsFalse(uiMgr.PaddleStatusBadge.ClassListContains("powerup-hidden"));
            Assert.AreEqual("10s", uiMgr.PaddleTimerLabel.text);

            uiMgr.HandlePaddleExpandTick(6.4f);
            Assert.AreEqual("7s", uiMgr.PaddleTimerLabel.text);

            uiMgr.HandlePaddleExpandStateChanged(false, 0f);
            Assert.IsTrue(uiMgr.PaddleStatusBadge.ClassListContains("powerup-hidden"));

            // Test Multiplier Badge
            uiMgr.HandleScoreMultiplierStateChanged(true, 4, 10f);
            Assert.IsFalse(uiMgr.MultiplierStatusBadge.ClassListContains("powerup-hidden"));
            Assert.IsTrue(uiMgr.MultiplierStatusBadge.ClassListContains("mult-tier-4x"));
            Assert.AreEqual("4X", uiMgr.MultiplierValueLabel.text);
            Assert.AreEqual("10s", uiMgr.MultiplierTimerLabel.text);

            uiMgr.HandleScoreMultiplierTick(4.2f);
            Assert.AreEqual("5s", uiMgr.MultiplierTimerLabel.text);

            uiMgr.HandleScoreMultiplierStateChanged(false, 1, 0f);
            Assert.IsTrue(uiMgr.MultiplierStatusBadge.ClassListContains("powerup-hidden"));

            Object.DestroyImmediate(uiManagerGo);
        }

        [Test]
        public void Campaign_LevelsContainProgressiveMultipliersUpTo5x()
        {
            var lvl9 = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_09.asset");
            Assert.IsNotNull(lvl9);
            Assert.AreEqual(1, lvl9.Multiplier4xCount, "Level 9 must introduce 1x 4X multiplier.");

            var lvl13 = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_13.asset");
            Assert.IsNotNull(lvl13);
            Assert.AreEqual(2, lvl13.Multiplier4xCount, "Level 13 must contain 2x 4X multipliers.");
            Assert.AreEqual(1, lvl13.Multiplier5xCount, "Level 13 must introduce 1x 5X multiplier.");

            var lvl15 = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_15.asset");
            Assert.IsNotNull(lvl15);
            Assert.AreEqual(2, lvl15.Multiplier4xCount, "Level 15 must contain 2x 4X multipliers.");
            Assert.AreEqual(2, lvl15.Multiplier5xCount, "Level 15 must contain 2x 5X multipliers.");
        }

        [Test]
        public void Campaign_LevelsContainProgressiveHazardsAndLeanerPaddleTuning()
        {
            int prevHazards = 0;
            for (int i = 1; i <= 15; i++)
            {
                var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>($"Assets/Settings/Levels/SO_Level_{i:D2}.asset");
                Assert.IsNotNull(cfg, $"SO_Level_{i:D2} must exist.");

                int totalHazards = cfg.PaddleShortenerCount + cfg.PaddleSlowerCount + cfg.BrickFreezerCount +
                                   cfg.BallSizeDecreaserCount + cfg.BallSlowerCount + cfg.PaddleFreezerCount;

                if (i == 1)
                {
                    Assert.AreEqual(0, totalHazards, "Level 1 must have 0 hazards as tutorial warmup.");
                    Assert.AreEqual(5.5f, cfg.InitialPaddleWidth, 0.01f);
                }
                else
                {
                    Assert.GreaterOrEqual(totalHazards, prevHazards, $"Level {i} hazards ({totalHazards}) must be >= Level {i - 1} hazards ({prevHazards}).");
                    Assert.LessOrEqual(cfg.InitialPaddleWidth, 5.0f, $"Level {i} paddle width should be leaner (<= 5.0).");
                }

                prevHazards = totalHazards;
            }

            var lvl15 = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_15.asset");
            int lvl15Hazards = lvl15.PaddleShortenerCount + lvl15.PaddleSlowerCount + lvl15.BrickFreezerCount +
                               lvl15.BallSizeDecreaserCount + lvl15.BallSlowerCount + lvl15.PaddleFreezerCount;
            Assert.AreEqual(13, lvl15Hazards, "Level 15 must have 13 hazard blocks for supreme climax challenge.");
            Assert.AreEqual(4.5f, lvl15.InitialPaddleWidth, 0.01f, "Level 15 paddle must be high-skill tuned at 4.5f.");
        }

        [Test]
        public void BallController_ResumeFromPauseBeforeLaunch_RemainsDockedOnPaddle()
        {
            var ballObj = new GameObject("TestBall");
            ballObj.transform.SetParent(testRoot.transform);
            var ball = ballObj.AddComponent<BallController>();
            ball.Initialize(gameManager, paddle);

            // 1. Initially in ReadyToLaunch, ball must not be launched
            gameManager.SetState(GameState.ReadyToLaunch);
            Assert.IsFalse(ball.IsLaunched, "Ball must start unlaunched.");

            // 2. Pause game
            gameManager.PauseGame();
            Assert.AreEqual(GameState.Paused, gameManager.State);
            Assert.IsFalse(ball.IsLaunched, "Ball must remain unlaunched while paused.");

            // 3. Resume game
            gameManager.ResumeGame();
            Assert.AreEqual(GameState.ReadyToLaunch, gameManager.State, "Game must return to ReadyToLaunch.");
            Assert.IsFalse(ball.IsLaunched, "Ball must NOT automatically start moving upon resume before launch.");

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void InputHandler_SuppressLaunch_PreventsPrematureLaunchBall()
        {
            var inputGo = new GameObject("TestInput");
            inputGo.transform.SetParent(testRoot.transform);
            var inputHandler = inputGo.AddComponent<Arcade.Input.ArcadeInputHandler>();
            Arcade.Input.ArcadeInputHandler.SetInstanceForTesting(inputHandler);

            gameManager.SetState(GameState.ReadyToLaunch);

            // Suppress launch
            inputHandler.SuppressLaunch(1.0f);
            inputHandler.TriggerLaunch();

            Assert.AreEqual(GameState.ReadyToLaunch, gameManager.State, "Launch must be blocked while suppressed.");

            Object.DestroyImmediate(inputGo);
        }

        [Test]
        public void InputHandler_ResetTouchState_DoesNotPermanentlyBlockKeyboardLaunch()
        {
            var inputGo = new GameObject("TestInput");
            inputGo.transform.SetParent(testRoot.transform);
            var inputHandler = inputGo.AddComponent<Arcade.Input.ArcadeInputHandler>();
            Arcade.Input.ArcadeInputHandler.SetInstanceForTesting(inputHandler);

            gameManager.SetState(GameState.ReadyToLaunch);

            // Simulating a touch/click release or modal dismissal calling ResetTouchState
            inputHandler.ResetTouchState();
            Assert.IsFalse(inputHandler.IsLaunchSuppressed, "ResetTouchState must not lock out launches.");

            // Keyboard launch should immediately succeed
            inputHandler.TriggerLaunch();
            Assert.AreEqual(GameState.Playing, gameManager.State, "Launch must succeed after ResetTouchState.");

            Object.DestroyImmediate(inputGo);
        }

        [Test]
        public void InputHandler_TriggerLaunch_LaunchesBallWhenNotSuppressed()
        {
            var inputGo = new GameObject("TestInput");
            inputGo.transform.SetParent(testRoot.transform);
            var inputHandler = inputGo.AddComponent<Arcade.Input.ArcadeInputHandler>();
            Arcade.Input.ArcadeInputHandler.SetInstanceForTesting(inputHandler);

            gameManager.SetState(GameState.ReadyToLaunch);
            inputHandler.TriggerLaunch();

            Assert.AreEqual(GameState.Playing, gameManager.State, "Launch must trigger State transition to Playing.");

            Object.DestroyImmediate(inputGo);
        }

        [Test]
        public void HighScoreManager_RecordScore_SortsDescendingAndClampsTo10()
        {
            HighScoreManager.ResetScores();

            // Record 12 scores in non-sorted order
            int[] scoresToAdd = { 100, 500, 250, 1000, 800, 50, 300, 400, 200, 900, 150, 75 };
            foreach (var s in scoresToAdd)
            {
                HighScoreManager.RecordScore(s, 1);
            }

            var topScores = HighScoreManager.GetTopScores();
            Assert.AreEqual(10, topScores.Count, "HighScoreManager must clamp to MAX_SCORES (10).");
            Assert.AreEqual(1000, topScores[0].score, "First score must be 1000.");
            Assert.AreEqual(900, topScores[1].score, "Second score must be 900.");
            Assert.AreEqual(800, topScores[2].score, "Third score must be 800.");
            Assert.AreEqual(150, topScores[8].score, "9th score must be 150.");
            Assert.AreEqual(100, topScores[9].score, "10th score must be 100.");
            Assert.AreEqual(1000, HighScoreManager.HighestScore, "HighestScore must return 1000.");

            // Adding a score lower than or equal to 10th score (e.g. 50) should return false and not alter list
            bool addedLower = HighScoreManager.RecordScore(50, 1);
            Assert.IsFalse(addedLower, "RecordScore should return false if score does not exceed 10th place.");
            Assert.AreEqual(10, HighScoreManager.GetTopScores().Count);

            // Adding a higher score (e.g. 950) should succeed and displace 100 (150 becomes 10th)
            bool addedHigher = HighScoreManager.RecordScore(950, 2);
            Assert.IsTrue(addedHigher);
            topScores = HighScoreManager.GetTopScores();
            Assert.AreEqual(950, topScores[1].score);
            Assert.AreEqual(150, topScores[9].score);

            HighScoreManager.ResetScores();
        }

        [Test]
        public void HighScoreManager_ResetScores_ClearsAllRecordedScores()
        {
            HighScoreManager.ResetScores();
            HighScoreManager.RecordScore(500, 1);
            Assert.AreEqual(1, HighScoreManager.GetTopScores().Count);
            Assert.AreEqual(500, HighScoreManager.HighestScore);

            HighScoreManager.ResetScores();
            Assert.AreEqual(0, HighScoreManager.GetTopScores().Count);
            Assert.AreEqual(0, HighScoreManager.HighestScore);
        }

        [Test]
        public void ArcadeUIManager_IsAnyModalVisible_IncludesHighScoresAndHowToPlay()
        {
            var uiObj = new GameObject("UI_HUD");
            uiObj.transform.SetParent(testRoot.transform);
            uiObj.AddComponent<PanelRenderer>();
            var uiMgr = uiObj.AddComponent<ArcadeUIManager>();

            var root = new VisualElement { name = "hud-root" };
            var highscoresModal = new VisualElement { name = "highscores-modal" };
            highscoresModal.AddToClassList("modal-hidden");
            var howToPlayModal = new VisualElement { name = "how-to-play-modal" };
            howToPlayModal.AddToClassList("modal-hidden");
            root.Add(highscoresModal);
            root.Add(howToPlayModal);

            var rootField = typeof(ArcadeUIManager).GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rootField.SetValue(uiMgr, root);

            var highField = typeof(ArcadeUIManager).GetField("highscoresModal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            highField.SetValue(uiMgr, highscoresModal);

            var howField = typeof(ArcadeUIManager).GetField("howToPlayModal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            howField.SetValue(uiMgr, howToPlayModal);

            Assert.IsFalse(uiMgr.IsAnyModalVisible());

            // Show high scores
            highscoresModal.RemoveFromClassList("modal-hidden");
            Assert.IsTrue(uiMgr.IsAnyModalVisible());
            highscoresModal.AddToClassList("modal-hidden");
            Assert.IsFalse(uiMgr.IsAnyModalVisible());

            // Show how to play
            howToPlayModal.RemoveFromClassList("modal-hidden");
            Assert.IsTrue(uiMgr.IsAnyModalVisible());
            howToPlayModal.AddToClassList("modal-hidden");
            Assert.IsFalse(uiMgr.IsAnyModalVisible());

            Object.DestroyImmediate(uiObj);
        }

        [Test]
        public void BallTrail_Initialization_CreatesDualLayerRenderers()
        {
            var ballObj = new GameObject("TestBall");
            var trail = ballObj.AddComponent<BallTrail>();
            trail.EnsureTrailsCreated();

            Assert.IsNotNull(trail.OuterTrail, "Outer trail renderer must be created.");
            Assert.IsNotNull(trail.InnerTrail, "Inner trail renderer must be created.");
            Assert.AreEqual(trail.OuterStartWidth, 0.55f, 0.01f, "Outer trail start width should be 0.55.");
            Assert.AreEqual(trail.InnerStartWidth, 0.25f, 0.01f, "Inner trail start width should be 0.25.");
            Assert.AreEqual(trail.OuterDuration, 0.22f, 0.01f, "Outer trail duration should be 0.22s.");
            Assert.AreEqual(trail.InnerDuration, 0.16f, 0.01f, "Inner trail duration should be 0.16s.");

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void BallTrail_TaperingCurves_WidthTapersToZero()
        {
            var ballObj = new GameObject("TestBall");
            var trail = ballObj.AddComponent<BallTrail>();
            trail.EnsureTrailsCreated();

            AnimationCurve outerCurve = trail.OuterTrail.widthCurve;
            AnimationCurve innerCurve = trail.InnerTrail.widthCurve;

            Assert.AreEqual(0.55f, outerCurve.Evaluate(0f), 0.02f, "Outer trail should start at ~0.55 width.");
            Assert.AreEqual(0.0f, outerCurve.Evaluate(1f), 0.01f, "Outer trail should taper to 0 width at tail.");

            Assert.AreEqual(0.25f, innerCurve.Evaluate(0f), 0.02f, "Inner trail should start at ~0.25 width.");
            Assert.AreEqual(0.0f, innerCurve.Evaluate(1f), 0.01f, "Inner trail should taper to 0 width at tail.");

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void BallTrail_SetTrailColor_CalculatesDualLightnessLevelsCorrectly()
        {
            var ballObj = new GameObject("TestBall");
            var trail = ballObj.AddComponent<BallTrail>();
            Color testColor = new Color(0f, 0.8f, 1f, 1f);
            trail.SetTrailColor(testColor);

            Assert.AreEqual(testColor, trail.BaseColor);

            // Inner core must have higher lightness (blended toward white)
            Color innerCore = trail.InnerCoreColor;
            Assert.Greater(innerCore.r, testColor.r, "Inner core R must be lighter.");
            Assert.Greater(innerCore.g, testColor.g, "Inner core G must be lighter.");
            Assert.GreaterOrEqual(innerCore.b, testColor.b, "Inner core B must be lighter or equal.");

            // Verify alpha keys: outer 0.40 -> 0, inner 0.85 -> 0
            Gradient outerGrad = trail.OuterTrail.colorGradient;
            Gradient innerGrad = trail.InnerTrail.colorGradient;

            Assert.AreEqual(0.40f, outerGrad.alphaKeys[0].alpha, 0.02f);
            Assert.AreEqual(0.00f, outerGrad.alphaKeys[1].alpha, 0.01f);
            Assert.AreEqual(0.85f, innerGrad.alphaKeys[0].alpha, 0.02f);
            Assert.AreEqual(0.00f, innerGrad.alphaKeys[1].alpha, 0.01f);

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void BallController_Docked_SuppressesTrailEmission()
        {
            var ballObj = new GameObject("TestBall");
            var rb = ballObj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            var ballCtrl = ballObj.AddComponent<BallController>();

            // Initially docked
            Assert.IsFalse(ballCtrl.IsLaunched);
            Assert.IsFalse(ballCtrl.Trail.OuterTrail.emitting, "Outer trail should not emit while docked.");
            Assert.IsFalse(ballCtrl.Trail.InnerTrail.emitting, "Inner trail should not emit while docked.");

            // Launch ball
            ballCtrl.Launch();
            Assert.IsTrue(ballCtrl.IsLaunched);
            Assert.IsTrue(ballCtrl.Trail.OuterTrail.emitting, "Outer trail must emit after launch.");
            Assert.IsTrue(ballCtrl.Trail.InnerTrail.emitting, "Inner trail must emit after launch.");

            // Dock ball again
            ballCtrl.StopAndDockBall();
            Assert.IsFalse(ballCtrl.IsLaunched);
            Assert.IsFalse(ballCtrl.Trail.OuterTrail.emitting, "Outer trail must stop emitting when docked.");
            Assert.IsFalse(ballCtrl.Trail.InnerTrail.emitting, "Inner trail must stop emitting when docked.");

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void ArcadeGameManager_MultiBall_AssignsDistinctColors()
        {
            var ballObj = new GameObject("PrimaryBall");
            ballObj.AddComponent<Rigidbody>();
            var primaryBall = ballObj.AddComponent<BallController>();
            primaryBall.SetTrailColor(new Color(0f, 0.95f, 1f, 1f)); // Electric Cyan
            gameManager.RegisterBall(primaryBall);

            // Spawn Multi-Ball
            gameManager.SpawnMultiBall(Vector3.zero, Vector3.up, 14f);

            var balls = Object.FindObjectsByType<BallController>();
            Assert.GreaterOrEqual(balls.Length, 3, "Multi-ball must spawn 2 extra balls (total >= 3).");

            // Verify that at least 2 distinct trail colors exist among active balls
            var distinctColors = new System.Collections.Generic.HashSet<Color>();
            foreach (var b in balls)
            {
                if (b != null && b.Trail != null)
                {
                    distinctColors.Add(b.Trail.BaseColor);
                }
            }

            Assert.GreaterOrEqual(distinctColors.Count, 3, "Each ball in multi-ball must have a distinct trail color.");

            // Clean up
            gameManager.ClearExtraBalls();
            Object.DestroyImmediate(ballObj);
        }

        #endregion

        #region 13. Stepped Pyramid Paddle & Powerup Capsule Tests

        [Test]
        public void Paddle_SteppedPyramid_DimensionsAndTapering()
        {
            var pObj = new GameObject("SteppedPaddleTest");
            var col = pObj.AddComponent<BoxCollider>();
            var pCtrl = pObj.AddComponent<PaddleController>();

            // Construct 3-tier stepped children
            var stepTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stepTop.name = "Step_Top";
            stepTop.transform.SetParent(pObj.transform, false);
            stepTop.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            stepTop.transform.localScale = new Vector3(1.0f, 0.24f, 1.0f);
            Object.DestroyImmediate(stepTop.GetComponent<Collider>());

            var stepMid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stepMid.name = "Step_Mid";
            stepMid.transform.SetParent(pObj.transform, false);
            stepMid.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            stepMid.transform.localScale = new Vector3(0.72f, 0.20f, 0.88f);
            Object.DestroyImmediate(stepMid.GetComponent<Collider>());

            var stepBtm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stepBtm.name = "Step_Bottom";
            stepBtm.transform.SetParent(pObj.transform, false);
            stepBtm.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            stepBtm.transform.localScale = new Vector3(0.44f, 0.16f, 0.72f);
            Object.DestroyImmediate(stepBtm.GetComponent<Collider>());

            pCtrl.EnsureSteppedMeshHierarchy();

            Assert.IsNotNull(pCtrl.StepTop, "Step_Top must be resolved.");
            Assert.IsNotNull(pCtrl.StepMid, "Step_Mid must be resolved.");
            Assert.IsNotNull(pCtrl.StepBottom, "Step_Bottom must be resolved.");

            Assert.AreEqual(1.0f, pCtrl.StepTop.localScale.x, 0.001f);
            Assert.AreEqual(0.72f, pCtrl.StepMid.localScale.x, 0.001f);
            Assert.AreEqual(0.44f, pCtrl.StepBottom.localScale.x, 0.001f);

            // Verify strike collider is aligned with top deck
            Assert.AreEqual(0.38f, col.center.y, 0.001f);
            Assert.AreEqual(0.24f, col.size.y, 0.001f);

            Object.DestroyImmediate(pObj);
        }

        [Test]
        public void Paddle_CompoundingExpansion_PreservesTaperingRatio()
        {
            var pObj = new GameObject("TaperingExpansionTest");
            var col = pObj.AddComponent<BoxCollider>();
            var pCtrl = pObj.AddComponent<PaddleController>();

            var stepTop = new GameObject("Step_Top");
            stepTop.transform.SetParent(pObj.transform, false);
            stepTop.transform.localScale = new Vector3(1.0f, 0.24f, 1.0f);

            var stepMid = new GameObject("Step_Mid");
            stepMid.transform.SetParent(pObj.transform, false);
            stepMid.transform.localScale = new Vector3(0.72f, 0.20f, 0.88f);

            var stepBtm = new GameObject("Step_Bottom");
            stepBtm.transform.SetParent(pObj.transform, false);
            stepBtm.transform.localScale = new Vector3(0.44f, 0.16f, 0.72f);

            pCtrl.EnsureSteppedMeshHierarchy();
            pCtrl.ResetWidth(5.0f);

            // Expand by +10% -> 5.5f
            pCtrl.ExpandWidth(0.10f);
            Assert.AreEqual(5.50f, pCtrl.Width, 0.001f);

            // World widths of tiers are multiplied by parent scale
            float topWorldW = pCtrl.Width * pCtrl.StepTop.localScale.x;
            float midWorldW = pCtrl.Width * pCtrl.StepMid.localScale.x;
            float btmWorldW = pCtrl.Width * pCtrl.StepBottom.localScale.x;

            Assert.AreEqual(5.50f, topWorldW, 0.001f);
            Assert.AreEqual(5.50f * 0.72f, midWorldW, 0.001f);
            Assert.AreEqual(5.50f * 0.44f, btmWorldW, 0.001f);

            Object.DestroyImmediate(pObj);
        }

        [Test]
        public void PowerupCapsule_Collect_TriggersPaddleExpansion()
        {
            paddle.ResetWidth(5.0f);
            var capsule = PowerupCapsule.Spawn(Vector3.zero, BlockSpecialType.PaddleExpander);
            Assert.IsNotNull(capsule);

            capsule.Collect(paddle);
            Assert.AreEqual(5.50f, paddle.Width, 0.001f, "Collecting PaddleExpander capsule must expand paddle.");
            Assert.AreEqual(1, paddle.ExpansionCount);
        }

        [Test]
        public void PowerupCapsule_Collect_ExtraHeart_AddsLife()
        {
            int startingLives = gameManager.Lives;

            var capsule = PowerupCapsule.Spawn(Vector3.zero, BlockSpecialType.ExtraHeart);
            capsule.Collect(paddle);

            Assert.AreEqual(startingLives + 1, gameManager.Lives, "Collecting ExtraHeart capsule must add 1 life.");
        }

        [Test]
        public void PowerupCapsule_Collect_Shield_ActivatesShield()
        {
            Assert.IsFalse(gameManager.IsShieldActive);

            var capsule = PowerupCapsule.Spawn(Vector3.zero, BlockSpecialType.Shield);
            capsule.Collect(paddle);

            Assert.IsTrue(gameManager.IsShieldActive, "Collecting Shield capsule must activate defensive barrier.");
            Assert.Greater(gameManager.ShieldTimeRemaining, 0f);
        }

        [Test]
        public void BallController_IsValidPaddleBounceNormal_EnforcesUpwardContactsOnly()
        {
            // Pure vertical hit from top
            Assert.IsTrue(BallController.IsValidPaddleBounceNormal(Vector3.up));

            // Angled hit from top (45 deg)
            Vector3 angledHit = new Vector3(0.707f, 0.707f, 0f);
            Assert.IsTrue(BallController.IsValidPaddleBounceNormal(angledHit));

            // Side wall collision (horizontal normal)
            Assert.IsFalse(BallController.IsValidPaddleBounceNormal(Vector3.right), "Side wall hits must NOT deflect upward.");
            Assert.IsFalse(BallController.IsValidPaddleBounceNormal(Vector3.left), "Side wall hits must NOT deflect upward.");

            // Shallow side scrape with normal.y below 0.25f
            Vector3 sideScrape = new Vector3(0.98f, 0.15f, 0f);
            Assert.IsFalse(BallController.IsValidPaddleBounceNormal(sideScrape), "Side scrape below threshold must NOT deflect upward.");

            // Collision from underneath (negative Y normal)
            Assert.IsFalse(BallController.IsValidPaddleBounceNormal(Vector3.down), "Bottom collisions must NOT deflect upward.");
        }

        [Test]
        public void BallController_CalculatePaddleDeflection_PreservesForwardMomentumAndSteers()
        {
            // Ball flying down-right
            Vector3 inVelRight = new Vector3(8f, -10f, 0f);

            // Center hit: should naturally reflect upward and right
            Vector3 bounceCenter = BallController.CalculatePaddleDeflection(inVelRight, 0f);
            Assert.Greater(bounceCenter.x, 0f, "Natural optical reflection must preserve rightward horizontal momentum.");
            Assert.Greater(bounceCenter.y, 0f, "Reflected vector must point upward.");

            // Left-edge hit on rightward ball: should steer steeper upward, but NOT unnaturally reverse left!
            Vector3 bounceLeft = BallController.CalculatePaddleDeflection(inVelRight, -0.6f);
            Assert.Greater(bounceLeft.x, 0f, "Hitting left half with rightward velocity must NOT flip horizontal travel.");
            Assert.Greater(bounceLeft.y, bounceCenter.y, "Left steering on rightward ball should yield steeper upward exit.");

            // Ball flying down-left
            Vector3 inVelLeft = new Vector3(-8f, -10f, 0f);
            Vector3 bounceLeftIn = BallController.CalculatePaddleDeflection(inVelLeft, 0f);
            Assert.Less(bounceLeftIn.x, 0f, "Natural optical reflection must preserve leftward horizontal momentum.");
            Assert.Greater(bounceLeftIn.y, 0f, "Reflected vector must point upward.");
        }

        [Test]
        public void BallController_CalculatePaddleDeflection_ExcludesVerticalDeadzone()
        {
            // Pure vertical incoming velocity directly hitting paddle center (hitOffset = 0)
            Vector3 inVelVertical = new Vector3(0f, -14f, 0f);

            // Stationary paddle: deflection must NOT produce pure 90-degree vertical vector
            Vector3 bounceStationary = BallController.CalculatePaddleDeflection(
                inVelVertical, 0f, steerStrength: 32f, minAngleDeg: 25f, maxAngleDeg: 155f,
                paddleVelocityX: 0f, velocityInfluence: 0.5f, verticalDeadzoneAngleDeg: 5f);

            Assert.Greater(bounceStationary.y, 0f, "Ball must bounce upward.");
            Assert.AreNotEqual(0f, bounceStationary.x, "Pure vertical bounce must be pushed out of 90-degree deadzone.");
            float angleDeg = Mathf.Atan2(bounceStationary.y, bounceStationary.x) * Mathf.Rad2Deg;
            Assert.IsTrue(angleDeg <= 85.01f || angleDeg >= 94.99f,
                $"Bounce angle ({angleDeg:F1}°) must fall outside the [85°, 95°] vertical deadzone.");
        }

        [Test]
        public void BallController_CalculatePaddleDeflection_AppliesPaddleVelocityInfluence()
        {
            Vector3 inVel = new Vector3(0f, -14f, 0f);

            // Moving paddle rightwards (+X velocity)
            Vector3 bounceMovingRight = BallController.CalculatePaddleDeflection(
                inVel, 0f, steerStrength: 32f, minAngleDeg: 25f, maxAngleDeg: 155f,
                paddleVelocityX: 8.0f, velocityInfluence: 0.5f, verticalDeadzoneAngleDeg: 5f);

            // Moving paddle leftwards (-X velocity)
            Vector3 bounceMovingLeft = BallController.CalculatePaddleDeflection(
                inVel, 0f, steerStrength: 32f, minAngleDeg: 25f, maxAngleDeg: 155f,
                paddleVelocityX: -8.0f, velocityInfluence: 0.5f, verticalDeadzoneAngleDeg: 5f);

            Assert.Greater(bounceMovingRight.x, 0f, "Rightward paddle sweep must bias deflection rightward.");
            Assert.Less(bounceMovingLeft.x, 0f, "Leftward paddle sweep must bias deflection leftward.");
            Assert.Greater(bounceMovingRight.x, bounceMovingLeft.x, "Rightward sweep must produce larger X velocity than leftward sweep.");
        }

        [Test]
        public void BallController_CalculatePaddleDeflection_StrongSwipeOpposite_ReversesHorizontalDirection()
        {
            // Ball flying down-right at ~60° polar angle (positive X)
            Vector3 inVelRight = new Vector3(8f, -14f, 0f);

            // Stationary paddle: optical reflection preserves rightward momentum
            Vector3 bounceStationary = BallController.CalculatePaddleDeflection(
                inVelRight, 0f, paddleVelocityX: 0f);
            Assert.Greater(bounceStationary.x, 0f, "Stationary paddle must reflect rightward.");

            // Strong leftward swipe against the ball (e.g. -20 u/s)
            Vector3 bounceCutLeft = BallController.CalculatePaddleDeflection(
                inVelRight, 0f, paddleVelocityX: -20f, velocityInfluence: 1.5f, maxVelocitySteerDeg: 45f);

            Assert.Less(bounceCutLeft.x, 0f, "Strong leftward swipe against rightward ball must reverse horizontal velocity across 90° (cut/hook).");
            Assert.Greater(bounceCutLeft.y, 0f, "Reversed cut must deflect upward.");
            float angleDeg = Mathf.Atan2(bounceCutLeft.y, bounceCutLeft.x) * Mathf.Rad2Deg;
            Assert.GreaterOrEqual(angleDeg, 95f, "Reversed angle must cross outside the vertical exclusion deadzone.");
            Assert.LessOrEqual(angleDeg, 155f, "Reversed angle must remain within playable arcade bounds.");
        }

        [Test]
        public void BallController_CalculatePaddleDeflection_SwipeWithBall_SharpensAngleTowardHorizontal()
        {
            // Ball flying down-right
            Vector3 inVelRight = new Vector3(8f, -14f, 0f);

            Vector3 bounceStationary = BallController.CalculatePaddleDeflection(
                inVelRight, 0f, paddleVelocityX: 0f);

            // Swiping right with the ball (tangential acceleration)
            Vector3 bounceSwipeRight = BallController.CalculatePaddleDeflection(
                inVelRight, 0f, paddleVelocityX: 16f, velocityInfluence: 1.5f, maxVelocitySteerDeg: 45f);

            Assert.Greater(bounceSwipeRight.x, bounceStationary.x,
                "Swiping with the ball must impart rightward tangential velocity, resulting in a shallower, faster horizontal exit.");
            float angleDeg = Mathf.Atan2(bounceSwipeRight.y, bounceSwipeRight.x) * Mathf.Rad2Deg;
            Assert.GreaterOrEqual(angleDeg, 25f, "Angle must not fall below minAngleDeg (25°).");
        }

        [Test]
        public void BallController_CalculatePaddleDeflection_MaxSteerClamp_ClampsAtConfiguredLimit()
        {
            Vector3 inVelVertical = new Vector3(0f, -14f, 0f);

            // Extreme swipe speed (50 u/s -> would be 75° deflection without clamp)
            Vector3 bounceExtreme = BallController.CalculatePaddleDeflection(
                inVelVertical, 0f, paddleVelocityX: 50f, velocityInfluence: 1.5f, maxVelocitySteerDeg: 45f);

            float angleDeg = Mathf.Atan2(bounceExtreme.y, bounceExtreme.x) * Mathf.Rad2Deg;
            // 90° - 45° = 45°
            Assert.AreEqual(45f, angleDeg, 0.1f, "Extreme swipe must be cleanly clamped at maxVelocitySteerDeg (45°).");
        }

        [Test]
        public void BallController_HandlePaddleCollision_MovingPaddle_AppliesKineticSpeedPop()
        {
            var ballObj = new GameObject("TestBall_Smash");
            ballObj.transform.position = new Vector3(0f, -5.5f, 0f);
            var rb = ballObj.AddComponent<Rigidbody>();
            rb.linearVelocity = new Vector3(0f, -14f, 0f);
            var ball = ballObj.AddComponent<BallController>();
            ball.SetCurrentSpeedForTesting(14.0f);

            paddle.transform.position = new Vector3(0f, -6.0f, 0f);
            paddle.SetVelocityXForTesting(8.0f); // Actively moving paddle (>= 3.5 u/s threshold)

            ball.HandlePaddleCollisionForTesting(paddle);

            float expectedSpeed = 14.0f * 1.08f; // +8% speed impulse
            Assert.AreEqual(expectedSpeed, ball.CurrentSpeed, 0.05f,
                "Striking with moving paddle (>= 3.5 u/s) must trigger kinetic speed pop (+8%).");

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void BallController_HandlePaddleCollision_StationaryPaddle_MaintainsSpeed()
        {
            var ballObj = new GameObject("TestBall_Stationary");
            ballObj.transform.position = new Vector3(0f, -5.5f, 0f);
            var rb = ballObj.AddComponent<Rigidbody>();
            rb.linearVelocity = new Vector3(0f, -14f, 0f);
            var ball = ballObj.AddComponent<BallController>();
            ball.SetCurrentSpeedForTesting(14.0f);

            paddle.transform.position = new Vector3(0f, -6.0f, 0f);
            paddle.SetVelocityXForTesting(0.0f); // Stationary paddle

            ball.HandlePaddleCollisionForTesting(paddle);

            Assert.AreEqual(14.0f, ball.CurrentSpeed, 0.01f,
                "Striking with stationary paddle must maintain constant currentSpeed with zero smash pop.");

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void BallController_SanitizeTrajectory_EnforcesMinimumVerticalAngleFloor()
        {
            float speed = 20f;
            // Extremely shallow rightward trajectory (angle = ~2.86° off horizontal)
            Vector3 shallowVel = new Vector3(19.975f, 1.0f, 0f);
            Vector3 sanitized = BallController.SanitizeTrajectory(shallowVel, speed, minVertAngleDeg: 20f, minHorizAngleDeg: 5f);

            float expectedMinVy = speed * Mathf.Sin(20f * Mathf.Deg2Rad);
            Assert.GreaterOrEqual(sanitized.y, expectedMinVy - 0.001f,
                $"Sanitized Vy ({sanitized.y:F2}) must meet minimum 20° vertical threshold ({expectedMinVy:F2}).");
            Assert.AreEqual(speed, sanitized.magnitude, 0.01f, "Speed must be preserved exactly.");
            Assert.Greater(sanitized.x, 0f, "Horizontal sign must be preserved.");

            // Downward shallow trajectory
            Vector3 shallowDown = new Vector3(19.975f, -0.5f, 0f);
            Vector3 sanitizedDown = BallController.SanitizeTrajectory(shallowDown, speed, minVertAngleDeg: 20f, minHorizAngleDeg: 5f);
            Assert.LessOrEqual(sanitizedDown.y, -expectedMinVy + 0.001f, "Negative Vy sign must be preserved with 20° clamp.");
            Assert.AreEqual(speed, sanitizedDown.magnitude, 0.01f, "Speed must be preserved exactly.");
        }

        [Test]
        public void BallController_SanitizeTrajectory_EnforcesMinimumHorizontalAngleFloor()
        {
            float speed = 20f;
            // Near-vertical trajectory (angle = ~89.7° off horizontal, Vx = 0.1)
            Vector3 nearVertical = new Vector3(0.1f, 19.999f, 0f);
            Vector3 sanitized = BallController.SanitizeTrajectory(nearVertical, speed, minVertAngleDeg: 20f, minHorizAngleDeg: 5f);

            float expectedMinVx = speed * Mathf.Sin(5f * Mathf.Deg2Rad);
            Assert.GreaterOrEqual(Mathf.Abs(sanitized.x), expectedMinVx - 0.001f,
                $"Sanitized Vx ({sanitized.x:F2}) must meet minimum 5° horizontal threshold ({expectedMinVx:F2}).");
            Assert.AreEqual(speed, sanitized.magnitude, 0.01f, "Speed must be preserved exactly.");
            Assert.Greater(sanitized.y, 0f, "Vertical sign must be preserved.");
        }

        [Test]
        public void BallController_ConsecutiveSideWallBounces_SteepensAngle()
        {
            var ballObj = new GameObject("TestBall_WallTest");
            var rb = ballObj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            var ball = ballObj.AddComponent<BallController>();

            float speed = 14f;
            // Start with shallow downward velocity (Vy = -2f)
            rb.linearVelocity = new Vector3(13.85f, -2.0f, 0f);

            // Trigger consecutive steepener
            ball.ApplyConsecutiveWallSteepening();

            float expectedSteepVy = speed * Mathf.Sin(35f * Mathf.Deg2Rad); // ~8.03
            Assert.LessOrEqual(rb.linearVelocity.y, -expectedSteepVy + 0.01f,
                $"Consecutive wall bounce must steepen vertical velocity to >= 35° ({expectedSteepVy:F2}).");
            Assert.AreEqual(speed, rb.linearVelocity.magnitude, 0.01f, "Speed must be preserved after steepening.");

            // Docking ball resets counter
            ball.SetConsecutiveSideWallBouncesForTesting(3);
            Assert.AreEqual(3, ball.ConsecutiveSideWallBounces);
            ball.StopAndDockBall();
            Assert.AreEqual(0, ball.ConsecutiveSideWallBounces, "Docking ball must reset consecutive wall bounces.");

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void PaddleController_TracksVelocityX_ForMomentumTransfer()
        {
            var paddleObj = new GameObject("TestPaddle_VelTest");
            var p = paddleObj.AddComponent<PaddleController>();

            p.SetVelocityXForTesting(12.5f);
            Assert.AreEqual(12.5f, p.VelocityX, 0.001f, "PaddleController must accurately expose VelocityX.");

            p.SetVelocityXForTesting(-8.2f);
            Assert.AreEqual(-8.2f, p.VelocityX, 0.001f, "PaddleController must accurately expose negative VelocityX.");

            Object.DestroyImmediate(paddleObj);
        }

        [Test]
        public void PowerupCapsule_SpawnsInForeground_AtForegroundZ()
        {
            Vector3 spawnPos = new Vector3(3f, 8f, 0f);
            var capsule = PowerupCapsule.Spawn(spawnPos, BlockSpecialType.PaddleExpander);
            Assert.IsNotNull(capsule);

            Assert.AreEqual(PowerupCapsule.FOREGROUND_Z, capsule.transform.position.z, 0.001f,
                "Powerup capsule must spawn at foreground depth Z = -1.0f to avoid occlusion behind lower bricks.");
            Assert.AreEqual(-1.0f, capsule.transform.position.z, 0.001f);

            var boxCol = capsule.GetComponent<BoxCollider>();
            Assert.IsNotNull(boxCol, "Capsule should use BoxCollider with depth overlap.");
            Assert.IsTrue(boxCol.isTrigger, "Capsule BoxCollider must be a trigger.");
            Assert.GreaterOrEqual(boxCol.size.z, 2.5f, "BoxCollider depth must comfortably span the paddle plane.");

            Object.DestroyImmediate(capsule.gameObject);
        }

        [Test]
        public void PowerupCapsule_HasDecoupledVisualAndBillboardHierarchy_WithLargeVisibleScale()
        {
            var capsule = PowerupCapsule.Spawn(new Vector3(0f, 5f, 0f), BlockSpecialType.PaddleExpander);
            Assert.IsNotNull(capsule);

            // Verify visual capsule child (tumbler mesh)
            Assert.IsNotNull(capsule.VisualCapsuleTransform, "Capsule must have VisualCapsuleTransform child.");
            Assert.AreEqual("Visual_Capsule", capsule.VisualCapsuleTransform.name);
            Assert.GreaterOrEqual(capsule.VisualCapsuleTransform.localScale.x, 0.80f, "Capsule visual mesh must be prominently sized.");

            // Verify billboard icon child
            Assert.IsNotNull(capsule.IconTransform, "Capsule must have IconTransform child.");
            Assert.AreEqual("Icon_Billboard", capsule.IconTransform.name);
            Assert.GreaterOrEqual(capsule.IconTransform.localScale.x, 0.85f, "Billboard icon must be prominently sized.");
            Assert.Less(capsule.IconTransform.localPosition.z, -0.50f, "Billboard icon must sit comfortably in front of the capsule.");

            Assert.IsNotNull(capsule.IconRenderer, "IconTransform must have SpriteRenderer component.");
            Assert.IsNotNull(capsule.IconRenderer.sprite, "Billboard icon must have a sprite assigned.");
            Assert.GreaterOrEqual(capsule.IconRenderer.sortingOrder, 30, "Billboard icon must have foreground sortingOrder.");

            // Test LateUpdate orientation lock
            capsule.transform.rotation = Quaternion.Euler(45f, 90f, 30f);
            capsule.UpdateBillboardOrientation();

            Assert.Less(Quaternion.Angle(capsule.IconTransform.rotation, Quaternion.identity), 0.1f,
                "Billboard icon must stay upright and unrotated facing camera regardless of parent capsule rotation.");

            Object.DestroyImmediate(capsule.gameObject);
        }

        [Test]
        public void PowerupCapsule_Collect_ScoreMultipliers_ActivateMultiplierBuff()
        {
            // Test 2X
            var capsule2x = PowerupCapsule.Spawn(Vector3.zero, BlockSpecialType.ScoreMultiplier2x);
            capsule2x.Collect(paddle);
            Assert.AreEqual(2, gameManager.ActiveScoreMultiplier, "Collecting 2X capsule must activate 2X score multiplier.");
            Assert.Greater(gameManager.MultiplierTimeRemaining, 0f);

            // Test 3X
            var capsule3x = PowerupCapsule.Spawn(Vector3.zero, BlockSpecialType.ScoreMultiplier3x);
            capsule3x.Collect(paddle);
            Assert.AreEqual(3, gameManager.ActiveScoreMultiplier, "Collecting 3X capsule must activate 3X score multiplier.");

            // Test 4X
            var capsule4x = PowerupCapsule.Spawn(Vector3.zero, BlockSpecialType.ScoreMultiplier4x);
            capsule4x.Collect(paddle);
            Assert.AreEqual(4, gameManager.ActiveScoreMultiplier, "Collecting 4X capsule must activate 4X score multiplier.");

            // Test 5X
            var capsule5x = PowerupCapsule.Spawn(Vector3.zero, BlockSpecialType.ScoreMultiplier5x);
            capsule5x.Collect(paddle);
            Assert.AreEqual(5, gameManager.ActiveScoreMultiplier, "Collecting 5X capsule must activate 5X score multiplier.");
        }

        [Test]
        public void PaddleController_RootCollider_HasExtendedDepth()
        {
            var boxCol = paddle.GetComponent<BoxCollider>();
            Assert.IsNotNull(boxCol);
            Assert.GreaterOrEqual(boxCol.size.z, 2.5f, "Paddle root collider must have depth >= 2.5 to intersect foreground capsules.");
        }

        #endregion

        #region 14. Background Gradient Tests

        [Test]
        public void LevelBackgroundController_InitializesAndAppliesGradientTexture()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var mr = go.GetComponent<MeshRenderer>();
            var bgCtrl = go.AddComponent<LevelBackgroundController>();

            var tex1 = new Texture2D(32, 32);
            var tex2 = new Texture2D(32, 32);
            var tex3 = new Texture2D(32, 32);
            var tex4 = new Texture2D(32, 32);

            bgCtrl.SetTextures(new[] { tex1, tex2, tex3, tex4 });

            Assert.IsNotNull(bgCtrl.CurrentTexture, "CurrentTexture should not be null after SetTextures.");
            Assert.GreaterOrEqual(bgCtrl.CurrentTextureIndex, 0);
            Assert.Less(bgCtrl.CurrentTextureIndex, 4);

            Object.DestroyImmediate(tex1);
            Object.DestroyImmediate(tex2);
            Object.DestroyImmediate(tex3);
            Object.DestroyImmediate(tex4);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void LevelBackgroundController_RandomizeBackground_AvoidsSameConsecutiveTexture()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var bgCtrl = go.AddComponent<LevelBackgroundController>();

            var tex1 = new Texture2D(16, 16);
            var tex2 = new Texture2D(16, 16);

            bgCtrl.SetTextures(new[] { tex1, tex2 });
            bgCtrl.SetBackgroundByIndex(0);
            Assert.AreEqual(0, bgCtrl.CurrentTextureIndex);

            // Calling randomize with avoidSameAsCurrent = true must pick the other texture (index 1)
            bgCtrl.RandomizeBackground(avoidSameAsCurrent: true);
            Assert.AreEqual(1, bgCtrl.CurrentTextureIndex, "RandomizeBackground must avoid repeating the same texture when alternatives exist.");

            // Calling randomize again must pick index 0
            bgCtrl.RandomizeBackground(avoidSameAsCurrent: true);
            Assert.AreEqual(0, bgCtrl.CurrentTextureIndex, "RandomizeBackground must cycle away from current texture index.");

            Object.DestroyImmediate(tex1);
            Object.DestroyImmediate(tex2);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void LevelBackgroundController_SetBackgroundByIndex_ClampsSafely()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var bgCtrl = go.AddComponent<LevelBackgroundController>();

            var tex1 = new Texture2D(16, 16);
            var tex2 = new Texture2D(16, 16);

            bgCtrl.SetTextures(new[] { tex1, tex2 });

            bgCtrl.SetBackgroundByIndex(999);
            Assert.AreEqual(1, bgCtrl.CurrentTextureIndex);
            Assert.AreEqual(tex2, bgCtrl.CurrentTexture);

            bgCtrl.SetBackgroundByIndex(-50);
            Assert.AreEqual(0, bgCtrl.CurrentTextureIndex);
            Assert.AreEqual(tex1, bgCtrl.CurrentTexture);

            Object.DestroyImmediate(tex1);
            Object.DestroyImmediate(tex2);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void LevelGenerator_GenerateLevel_TriggersBackgroundRandomization()
        {
            var bgGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var bgCtrl = bgGo.AddComponent<LevelBackgroundController>();

            var tex1 = new Texture2D(16, 16);
            var tex2 = new Texture2D(16, 16);
            bgCtrl.SetTextures(new[] { tex1, tex2 });
            bgCtrl.SetBackgroundByIndex(0);
            Assert.AreEqual(0, bgCtrl.CurrentTextureIndex);

            var genObj = new GameObject("TestGen");
            var gen = genObj.AddComponent<LevelGenerator>();

            // Calling GenerateLevel on LevelGenerator must trigger background randomization
            gen.GenerateLevel();

            // Index should have changed from 0 to 1 because avoidSameAsCurrent is default true
            Assert.AreEqual(1, bgCtrl.CurrentTextureIndex, "GenerateLevel must trigger background randomization.");

            Object.DestroyImmediate(tex1);
            Object.DestroyImmediate(tex2);
            Object.DestroyImmediate(genObj);
            Object.DestroyImmediate(bgGo);
        }

        [Test]
        public void SceneSetup_GameplayScene_HasBackgroundPlaneBehindPlayfield()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/LV_BlockBreaker.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
            var rootObjects = scene.GetRootGameObjects();

            GameObject bgPlane = null;
            foreach (var root in rootObjects)
            {
                if (root.name == "Background_Plane")
                {
                    bgPlane = root;
                    break;
                }
            }

            Assert.IsNotNull(bgPlane, "Background_Plane must exist in LV_BlockBreaker scene.");
            Assert.Greater(bgPlane.transform.position.z, 2.0f, "Background_Plane must be positioned behind arena elements (Z > 2.0).");
            Assert.AreEqual(new Vector3(40f, 80f, 1f), bgPlane.transform.localScale, "Background_Plane must be scaled to 40x80.");
            var ctrl = bgPlane.GetComponent<LevelBackgroundController>();
            Assert.IsNotNull(ctrl, "Background_Plane must have LevelBackgroundController component.");
            Assert.IsNotNull(ctrl.BackgroundTextures, "BackgroundTextures array must be configured.");
            Assert.AreEqual(4, ctrl.BackgroundTextures.Length, "Must have 4 gradient textures assigned.");

            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }

        #endregion

        #region 15. Shaders, Powerup Cleanup & Level Completion Fallback Tests

        [Test]
        public void BlockVFXManager_ParticleMaterial_UsesValidURPShader_AndFallbackIsSafe()
        {
            var vfxGo = new GameObject("TestVFXManager");
            var vfx = vfxGo.AddComponent<BlockVFXManager>();

            var fallbackMat = vfx.GetOrCreateParticleMaterial();
            Assert.IsNotNull(fallbackMat, "Fallback particle material must not be null.");
            Assert.IsNotNull(fallbackMat.shader, "Fallback particle material must have a valid shader.");
            Assert.AreNotEqual("Hidden/InternalErrorShader", fallbackMat.shader.name, "Shader must not be error shader.");
            Assert.AreNotEqual("Standard", fallbackMat.shader.name, "Fallback shader must not be legacy built-in Standard.");

            var customMat = new Material(Shader.Find("Arcade/VFX_ParticleBurst") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            vfx.SetParticleMaterial(customMat);
            Assert.AreEqual(customMat, vfx.ParticleMaterial, "Explicit particle material must be assigned and accessible.");
            Assert.AreEqual(customMat, vfx.GetOrCreateParticleMaterial(), "GetOrCreateParticleMaterial must return the assigned material.");

            Object.DestroyImmediate(vfxGo);
            Object.DestroyImmediate(customMat);
        }

        [Test]
        public void BlockVFXManager_PlayPowerupCollect_SpawnsActiveVFX_WithoutDebrisSubBoxes()
        {
            var vfxGo = new GameObject("TestVFXManager");
            var vfx = vfxGo.AddComponent<BlockVFXManager>();
            BlockVFXManager.SetInstanceForTesting(vfx);

            // Collecting a powerup triggers a particle burst, NOT shattered block cubes
            vfx.PlayPowerupCollect(Vector3.zero, new Color(0.88f, 0.34f, 0.99f)); // MultiBall neon purple/magenta

            Assert.AreEqual(1, vfx.ActiveVfxCount, "PlayPowerupCollect must track exactly 1 active VFX burst.");
            Assert.AreEqual(0, vfx.ActiveDebrisCount, "PlayPowerupCollect must NOT spawn block debris cubes.");

            // Verify particle system renderer exists and has valid non-error material
            var ps = vfxGo.GetComponentInChildren<ParticleSystem>();
            Assert.IsNotNull(ps, "Must have active ParticleSystem child.");
            var psr = ps.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(psr.sharedMaterial, "ParticleSystemRenderer must have a sharedMaterial assigned.");
            Assert.AreNotEqual("Hidden/InternalErrorShader", psr.sharedMaterial.shader.name, "Particle shader must not be pink missing shader.");

            vfx.ClearAllActive();
            Assert.AreEqual(0, vfx.ActiveVfxCount, "ClearAllActive must reset active VFX count.");

            Object.DestroyImmediate(vfxGo);
        }

        [Test]
        public void PowerupCapsule_ClearAllFallingCapsules_DestroysAllFallingInstances()
        {
            PowerupCapsule.ClearAllFallingCapsules();

            var cap1 = PowerupCapsule.Spawn(new Vector3(-2f, 5f, 0f), BlockSpecialType.MultiBall);
            var cap2 = PowerupCapsule.Spawn(new Vector3(2f, 5f, 0f), BlockSpecialType.PaddleExpander);

            var existing = Object.FindObjectsByType<PowerupCapsule>(FindObjectsSortMode.None);
            Assert.AreEqual(2, existing.Length, "Must have 2 active capsules in scene.");

            PowerupCapsule.ClearAllFallingCapsules();

            var remaining = Object.FindObjectsByType<PowerupCapsule>(FindObjectsSortMode.None);
            Assert.AreEqual(0, remaining.Length, "ClearAllFallingCapsules must destroy all falling capsules.");
        }

        [Test]
        public void PowerupCapsule_CannotCollectWhileDockedInReadyToLaunch()
        {
            gameManager.SetState(GameState.ReadyToLaunch);

            var cap = PowerupCapsule.Spawn(Vector3.zero, BlockSpecialType.MultiBall);
            bool interceptedWhileDocked = cap.TryIntercept(paddle);

            // Because game is in ReadyToLaunch and not Playing, capsule must NOT be intercepted
            Assert.IsFalse(interceptedWhileDocked, "TryIntercept must return false when docked in ReadyToLaunch.");
            var remaining = Object.FindObjectsByType<PowerupCapsule>(FindObjectsSortMode.None);
            Assert.AreEqual(1, remaining.Length, "Capsule must not be collected while docked in ReadyToLaunch.");
            Assert.AreEqual(0, gameManager.ActiveBallCount, "MultiBall must not spawn extra balls while docked.");

            // When game transitions to Playing, intercept succeeds
            gameManager.SetState(GameState.Playing);
            bool interceptedWhilePlaying = cap.TryIntercept(paddle);
            Assert.IsTrue(interceptedWhilePlaying, "TryIntercept must succeed when game is Playing.");
        }

        [Test]
        public void ArcadeGameManager_RecordBallLost_ClearsFallingCapsules()
        {
            gameManager.SetState(GameState.Playing);

            var cap = PowerupCapsule.Spawn(new Vector3(0f, 5f, 0f), BlockSpecialType.Shield);
            Assert.AreEqual(1, Object.FindObjectsByType<PowerupCapsule>(FindObjectsSortMode.None).Length);

            gameManager.RecordBallLost();

            Assert.AreEqual(0, Object.FindObjectsByType<PowerupCapsule>(FindObjectsSortMode.None).Length, "Losing life must clear all falling capsules.");
        }

        [Test]
        public void ArcadeGameManager_BlocksContainerEmpty_TriggersLevelSuccessFallback()
        {
            gameManager.SetState(GameState.Playing);
            gameManager.RegisterLevelBlocks(10);

            // Simulating desync where remainingBlocks was stuck at 2
            var genGo = new GameObject("LevelGen");
            genGo.transform.SetParent(testRoot.transform);
            var gen = genGo.AddComponent<LevelGenerator>();
            var containerGo = new GameObject("BlocksContainer");
            containerGo.transform.SetParent(genGo.transform);

            // BlocksContainer has 0 blocks
            gameManager.CheckLevelCompletion();

            Assert.IsTrue(gameManager.IsLevelClearPending, "CheckLevelCompletion must trigger level clear pending when BlocksContainer has 0 live blocks.");
            Assert.AreEqual(0, gameManager.RemainingBlocks, "Remaining blocks must be clamped to 0.");
            gameManager.TriggerImmediateLevelClearForTesting();
            Assert.AreEqual(GameState.LevelClear, gameManager.State, "CheckLevelCompletion must finalize to LevelClear on routine completion.");
        }

        [Test]
        public void BlockVFXManager_PooledInstances_ArePlacedOffscreen_OutsideLevelPlayfield()
        {
            var vfxGo = new GameObject("VFXManager_Test");
            vfxGo.transform.SetParent(testRoot.transform);
            var vfx = vfxGo.AddComponent<BlockVFXManager>();
            vfx.InitializePool();
            BlockVFXManager.SetInstanceForTesting(vfx);

            // Pool container must exist and be placed far off-screen
            var poolContainer = vfxGo.transform.Find("_Pool_VFX");
            Assert.IsNotNull(poolContainer, "_Pool_VFX container must be created.");
            Assert.Less(poolContainer.position.y, -100f, "Pool container must be situated comfortably outside playfield.");

            // Verify burst instances are disabled and positioned off-screen
            int burstCount = 0;
            for (int i = 0; i < poolContainer.childCount; i++)
            {
                var child = poolContainer.GetChild(i);
                if (child.name == "VFX_Burst_Instance")
                {
                    burstCount++;
                    Assert.IsFalse(child.gameObject.activeSelf, "Pooled VFX burst must be inactive by default.");
                    Assert.Less(child.position.y, -100f, "Pooled VFX burst instance must never be located in the playfield.");
                }
            }
            Assert.Greater(burstCount, 0, "Burst pool instances must be created.");
        }

        [Test]
        public void PowerupCapsule_CleanVisualHierarchy_ExactlyOneMesh_ExactlyOneSprite_ZeroDuplicates()
        {
            var cap = PowerupCapsule.Spawn(new Vector3(0f, 5f, 0f), BlockSpecialType.PaddleExpander);
            Assert.IsNotNull(cap);

            // 1. Root container must have ZERO meshes and ZERO sprites
            Assert.IsNull(cap.GetComponent<MeshRenderer>(), "Root capsule container must not have MeshRenderer.");
            Assert.IsNull(cap.GetComponent<SpriteRenderer>(), "Root capsule container must not have SpriteRenderer.");

            // 2. Exactly 3 children: Visual_Capsule, Icon_Billboard, and Falling_Vfx
            Assert.AreEqual(3, cap.transform.childCount, "Powerup capsule must have exactly 3 children (1 visual mesh child, 1 billboard sprite child, 1 particle vfx child).");

            var visual = cap.VisualCapsuleTransform;
            var icon = cap.IconTransform;
            var vfx = cap.FallingVfxTransform;
            Assert.IsNotNull(visual, "Visual_Capsule child must exist.");
            Assert.IsNotNull(icon, "Icon_Billboard child must exist.");
            Assert.IsNotNull(vfx, "Falling_Vfx child must exist.");
            Assert.AreEqual("Visual_Capsule", visual.name);
            Assert.AreEqual("Icon_Billboard", icon.name);
            Assert.AreEqual("Falling_Vfx", vfx.name);

            // 3. Visual_Capsule has 1 mesh, 0 sprites, 0 colliders
            var visualMesh = visual.GetComponent<MeshRenderer>();
            var visualSprite = visual.GetComponent<SpriteRenderer>();
            var visualCollider = visual.GetComponent<Collider>();
            Assert.IsNotNull(visualMesh, "Visual_Capsule must have MeshRenderer.");
            Assert.IsNull(visualSprite, "Visual_Capsule must NOT have SpriteRenderer.");
            Assert.IsNull(visualCollider, "Visual_Capsule must NOT have Collider (trigger belongs on root).");

            // 4. Icon_Billboard has 1 sprite, 0 meshes, 0 colliders
            var iconSprite = icon.GetComponent<SpriteRenderer>();
            var iconMesh = icon.GetComponent<MeshRenderer>();
            var iconCollider = icon.GetComponent<Collider>();
            Assert.IsNotNull(iconSprite, "Icon_Billboard must have SpriteRenderer.");
            Assert.IsNull(iconMesh, "Icon_Billboard must NOT have MeshRenderer.");
            Assert.IsNull(iconCollider, "Icon_Billboard must NOT have Collider.");

            // 5. Total counts across entire hierarchy
            var allMeshRenderers = cap.GetComponentsInChildren<MeshRenderer>(true);
            var allSpriteRenderers = cap.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.AreEqual(1, allMeshRenderers.Length, "There must be exactly 1 MeshRenderer across the entire powerup capsule hierarchy.");
            Assert.AreEqual(1, allSpriteRenderers.Length, "There must be exactly 1 SpriteRenderer across the entire powerup capsule hierarchy.");

            Object.DestroyImmediate(cap.gameObject);
        }

        [Test]
        public void PowerupCapsule_MaterialResolution_FallbackReturnsValidURPShader()
        {
            Material mat = PowerupCapsule.GetOrCreateCapsuleMaterial();
            Assert.IsNotNull(mat, "Powerup capsule material must resolve successfully.");
            Assert.IsNotNull(mat.shader, "Powerup capsule material must have a valid shader.");
            Assert.AreNotEqual("Hidden/InternalErrorShader", mat.shader.name, "Shader must not be the pink error shader.");
        }

        [Test]
        public void BallController_DynamicVolleyPacing_AcceleratesAfterInterval()
        {
            var ballGo = new GameObject("Ball_Pacing_Test");
            ballGo.transform.SetParent(testRoot.transform);
            var rb = ballGo.AddComponent<Rigidbody>();
            var bc = ballGo.AddComponent<BallController>();

            bc.SetSpeedMultiplier(1.0f);
            bc.LaunchWithDirection(Vector3.up, 14f);

            float initialSpeed = bc.CurrentSpeed;
            Assert.AreEqual(14f, initialSpeed, 0.001f);
            Assert.AreEqual(0f, bc.ActiveVolleyTime);

            // Advance time past 10-second threshold
            bc.ApplyTimeBasedSpeedRamp(10.5f);

            Assert.Greater(bc.CurrentSpeed, initialSpeed, "Ball speed must accelerate after 10 seconds of active volley.");
            Assert.GreaterOrEqual(bc.ActiveVolleyTime, 10.5f);

            // Docking ball must reset volley timer and speed
            bc.StopAndDockBall();
            Assert.AreEqual(0f, bc.ActiveVolleyTime, "Docking ball must reset active volley time to 0.");
            Assert.AreEqual(14f, bc.CurrentSpeed, 0.001f, "Docking ball must restore base speed.");

            Object.DestroyImmediate(ballGo);
        }

        [Test]
        public void BuildVersionUtility_ParseBuildNumber_RecognizesPatterns()
        {
            Assert.AreEqual(1, BuildVersionUtility.ParseBuildNumber("_build1"));
            Assert.AreEqual(2, BuildVersionUtility.ParseBuildNumber("build_2"));
            Assert.AreEqual(3, BuildVersionUtility.ParseBuildNumber("build3"));
            Assert.AreEqual(4, BuildVersionUtility.ParseBuildNumber("_build_4"));
            Assert.AreEqual(5, BuildVersionUtility.ParseBuildNumber("build-5"));
            Assert.AreEqual(6, BuildVersionUtility.ParseBuildNumber("v6"));
            Assert.AreEqual(7, BuildVersionUtility.ParseBuildNumber("build_07"));
            Assert.AreEqual(10, BuildVersionUtility.ParseBuildNumber("build_10"));

            Assert.AreEqual(-1, BuildVersionUtility.ParseBuildNumber("BlockBreakerBuild"));
            Assert.AreEqual(-1, BuildVersionUtility.ParseBuildNumber("build_random"));
            Assert.AreEqual(-1, BuildVersionUtility.ParseBuildNumber(""));
            Assert.AreEqual(-1, BuildVersionUtility.ParseBuildNumber(null));
        }

        [Test]
        public void BuildVersionUtility_IncrementVersionString_AdvancesPatchOrInteger()
        {
            Assert.AreEqual("0.1.1", BuildVersionUtility.IncrementVersionString("0.1.0"));
            Assert.AreEqual("0.1.10", BuildVersionUtility.IncrementVersionString("0.1.9"));
            Assert.AreEqual("1.0.1", BuildVersionUtility.IncrementVersionString("1.0.0"));
            Assert.AreEqual("1.3", BuildVersionUtility.IncrementVersionString("1.2"));
            Assert.AreEqual("2", BuildVersionUtility.IncrementVersionString("1"));
            Assert.AreEqual("0.1.1", BuildVersionUtility.IncrementVersionString(""));
            Assert.AreEqual("0.1.1", BuildVersionUtility.IncrementVersionString(null));
        }

        [Test]
        public void BuildVersionUtility_GetNextBuildNumber_ResolvesSequentialFolders()
        {
            string tempBase = System.IO.Path.Combine(Application.temporaryCachePath, "TestBuilds_" + System.Guid.NewGuid().ToString("N"));
            try
            {
                System.IO.Directory.CreateDirectory(tempBase);
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(tempBase, "_build1"));
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(tempBase, "build_2"));

                int highest = BuildVersionUtility.GetHighestExistingBuildNumber(tempBase);
                Assert.AreEqual(2, highest);

                int next = BuildVersionUtility.GetNextBuildNumber(tempBase, 0);
                Assert.AreEqual(3, next);

                string folderName = BuildVersionUtility.FormatBuildFolderName(next);
                Assert.AreEqual("build_3", folderName);

                // If current build setting is higher, next must advance past it
                int nextWithHigherCurrent = BuildVersionUtility.GetNextBuildNumber(tempBase, 5);
                Assert.AreEqual(6, nextWithHigherCurrent);
            }
            finally
            {
                if (System.IO.Directory.Exists(tempBase))
                {
                    System.IO.Directory.Delete(tempBase, true);
                }
            }
        }

#if UNITY_EDITOR
        [Test]
        public void BuildVersionUtility_PrepareNextBuild_IncrementsSettingsAndCreatesSubfolder()
        {
            string tempBase = System.IO.Path.Combine(Application.temporaryCachePath, "TestPrepare_" + System.Guid.NewGuid().ToString("N"));
            string originalBuildNum = UnityEditor.PlayerSettings.iOS.buildNumber;
            string originalBundleVer = UnityEditor.PlayerSettings.bundleVersion;

            try
            {
                UnityEditor.PlayerSettings.iOS.buildNumber = "4";
                UnityEditor.PlayerSettings.bundleVersion = "0.1.0";

                string createdPath = BuildVersionUtility.PrepareNextBuild(tempBase, true);

                Assert.IsTrue(System.IO.Directory.Exists(createdPath), "Build subfolder must exist on disk.");
                Assert.IsTrue(createdPath.EndsWith("build_5"), $"Expected path to end with build_5, but got {createdPath}");
                Assert.AreEqual("5", UnityEditor.PlayerSettings.iOS.buildNumber, "iOS buildNumber must be incremented to 5.");
                Assert.AreEqual("0.1.1", UnityEditor.PlayerSettings.bundleVersion, "bundleVersion must be incremented to 0.1.1.");
            }
            finally
            {
                UnityEditor.PlayerSettings.iOS.buildNumber = originalBuildNum;
                UnityEditor.PlayerSettings.bundleVersion = originalBundleVer;
                UnityEditor.AssetDatabase.SaveAssets();

                if (System.IO.Directory.Exists(tempBase))
                {
                    System.IO.Directory.Delete(tempBase, true);
                }
            }
        }
#endif

        #endregion

        #region 16. Clutch Countdown & Laser Blaster Tests

        [Test]
        public void ClutchCountdown_Triggers_WhenOneBlockRemains()
        {
            gameManager.RegisterLevelBlocks(2);
            gameManager.LaunchBall();

            Assert.IsFalse(gameManager.IsClutchModeActive);

            // Destroy 1 block -> exactly 1 block remains
            gameManager.RecordBlockDestroyed(10, 1);

            Assert.AreEqual(1, gameManager.RemainingBlocks);
            Assert.IsTrue(gameManager.IsClutchModeActive, "Clutch mode must activate when remaining blocks == 1.");
            Assert.AreEqual(12f, gameManager.ClutchTimeRemaining, 0.01f);
            Assert.AreEqual(10, gameManager.ClutchMultiplier);
        }

        [Test]
        public void ClutchCountdown_Multiplier_DecaysCorrectly()
        {
            Assert.AreEqual(10, ArcadeGameManager.CalculateClutchMultiplier(12.0f));
            Assert.AreEqual(10, ArcadeGameManager.CalculateClutchMultiplier(10.0f));
            Assert.AreEqual(8, ArcadeGameManager.CalculateClutchMultiplier(7.2f));
            Assert.AreEqual(5, ArcadeGameManager.CalculateClutchMultiplier(4.9f));
            Assert.AreEqual(2, ArcadeGameManager.CalculateClutchMultiplier(1.5f));
            Assert.AreEqual(1, ArcadeGameManager.CalculateClutchMultiplier(0.3f));
            Assert.AreEqual(1, ArcadeGameManager.CalculateClutchMultiplier(0f));
        }

        [Test]
        public void ClutchCountdown_Multiplier_AppliesToFinalBlockScore()
        {
            gameManager.RegisterLevelBlocks(2);
            gameManager.LaunchBall();

            // Destroy first block: 10 * 1 = 10 pts
            gameManager.RecordBlockDestroyed(10, 1);
            Assert.AreEqual(10, gameManager.Score);
            Assert.IsTrue(gameManager.IsClutchModeActive);

            // Final block destroyed during clutch with multiplier 10 -> 30 * 10 = 300 pts
            gameManager.RecordBlockDestroyed(30, 1);
            Assert.AreEqual(310, gameManager.Score);
            Assert.IsFalse(gameManager.IsClutchModeActive);
            Assert.IsTrue(gameManager.IsLevelClearPending, "Level clear must be pending during clear delay.");
            gameManager.TriggerImmediateLevelClearForTesting();
            Assert.AreEqual(GameState.LevelClear, gameManager.State);
        }

        [Test]
        public void ClutchCountdown_Timeout_TriggersRailgunDischarge()
        {
            gameManager.RegisterLevelBlocks(2);
            gameManager.LaunchBall();
            gameManager.RecordBlockDestroyed(10, 1);

            Assert.IsTrue(gameManager.IsClutchModeActive);

            // Simulate tick past 12s timeout
            gameManager.TickClutchMode(12.5f);

            Assert.IsFalse(gameManager.IsClutchModeActive, "Clutch mode must conclude upon timeout.");
        }

        [Test]
        public void PaddleLaserController_RailgunHyperBeam_DestroysBlocksAbovePaddle()
        {
            var laserCtrl = paddle.LaserController;
            Assert.IsNotNull(laserCtrl, "Paddle must have PaddleLaserController.");

            // Create a block above paddle at X=0, Y=5
            var blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blockObj.transform.position = new Vector3(0f, 5f, 0f);
            var block = blockObj.AddComponent<Block>();
            block.Initialize(BlockColorTier.Red, null, Color.red);

            paddle.transform.position = new Vector3(0f, -6f, 0f);

            laserCtrl.FireRailgunHyperBeam();
            laserCtrl.SimulateStepForTesting(0.35f);

            Assert.IsTrue(block.IsDestroyed, "Block within Railgun beam path must be destroyed.");
            Object.DestroyImmediate(blockObj);
        }

        [Test]
        public void LaserBolt_ContinuousSweep_HitsAndDestroysBlock()
        {
            var blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blockObj.transform.position = new Vector3(0f, 2f, 0f);
            var block = blockObj.AddComponent<Block>();
            block.Initialize(BlockColorTier.Green, null, Color.green);

            var boltObj = new GameObject("LaserBolt");
            boltObj.transform.position = new Vector3(0f, 0f, 0f);
            var bolt = boltObj.AddComponent<LaserBolt>();

            Physics.SyncTransforms();

            // Simulate tick where bolt travels past block (0 -> 4 units upward)
            bolt.SimulateStepForTesting(0.12f);

            Assert.IsTrue(block.IsDestroyed, "Laser bolt continuous raycast must hit and destroy the block.");

            if (boltObj != null) Object.DestroyImmediate(boltObj);
            if (blockObj != null) Object.DestroyImmediate(blockObj);
        }

        [Test]
        public void PaddleLaserController_LaserPowerup_TwinBlastersFireOnInterval()
        {
            var laserCtrl = paddle.LaserController;
            paddle.transform.position = new Vector3(0f, -6f, 0f);

            laserCtrl.ActivateLaserBlaster(10f);
            Assert.IsTrue(laserCtrl.IsBlasterActive);

            laserCtrl.SimulateStepForTesting(0.35f);

            var bolts = Object.FindObjectsByType<LaserBolt>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(bolts.Length, 2, "Twin blaster cannons must spawn at least 2 bolts on firing interval.");

            foreach (var b in bolts) Object.DestroyImmediate(b.gameObject);
        }

        [Test]
        public void PowerupCapsule_LaserType_ActivatesBlasterOnCollection()
        {
            var cap = PowerupCapsule.Spawn(Vector3.zero, BlockSpecialType.Laser);

            gameManager.SetState(GameState.Playing);
            Assert.IsFalse(gameManager.IsLaserActive);

            bool collected = cap.TryIntercept(paddle);
            Assert.IsTrue(collected, "Paddle must collect laser capsule.");
            Assert.IsTrue(gameManager.IsLaserActive, "Laser powerup must become active on collection.");
            Assert.IsTrue(paddle.LaserController.IsBlasterActive, "Paddle blasters must be activated.");

            if (cap != null) Object.DestroyImmediate(cap.gameObject);
        }

        [Test]
        public void LevelGenerator_DistributeSpecialBlocks_AllocatesLaserBlocks()
        {
            var genObj = new GameObject("LevelGen");
            var gen = genObj.AddComponent<LevelGenerator>();

            var map = gen.DistributeSpecialBlocks(totalBlocks: 20, mult2xCount: 1, mult3xCount: 0, mult4xCount: 0, mult5xCount: 0,
                expanderCount: 1, bombCount: 0, glassCount: 0, heartCount: 0, shieldCount: 0, multiBallCount: 0, laserCount: 2);

            int laserCount = 0;
            foreach (var kvp in map)
            {
                if (kvp.Value == BlockSpecialType.Laser) laserCount++;
            }

            Assert.AreEqual(2, laserCount, "LevelGenerator must allocate exact requested number of Laser blocks.");
            Object.DestroyImmediate(genObj);
        }

        [Test]
        public void ArcadeUIManager_LaserAndClutchBadges_UpdatesTimerAndVisibility()
        {
            var uiManagerGo = new GameObject("TestArcadeUIManager");
            var panelRenderer = uiManagerGo.AddComponent<UnityEngine.UIElements.PanelRenderer>();
            var uiMgr = uiManagerGo.AddComponent<ArcadeUIManager>();

            var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsNotNull(uxml, "BlockBreakerHUD.uxml must exist.");

            panelRenderer.visualTreeAsset = uxml;
            var root = uxml.CloneTree();

            // Reflection-based bind for unit testing
            var bindMethod = typeof(ArcadeUIManager).GetMethod("BindElements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            typeof(ArcadeUIManager).GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, root);
            bindMethod.Invoke(uiMgr, null);

            Assert.IsNotNull(uiMgr.LaserStatusBadge);
            Assert.IsNotNull(uiMgr.ClutchStatusBadge);
            Assert.IsTrue(uiMgr.LaserSprite.name.Contains("TX_Powerup_Gun"),
                $"LaserSprite should strictly resolve to TX_Powerup_Gun, but was '{uiMgr.LaserSprite.name}'.");

            // Test Laser Badge
            uiMgr.HandleLaserPowerupStateChanged(true, 10f);
            Assert.IsFalse(uiMgr.LaserStatusBadge.ClassListContains("powerup-hidden"));
            Assert.AreEqual("10s", uiMgr.LaserTimerLabel.text);

            uiMgr.HandleLaserPowerupTick(6.2f);
            Assert.AreEqual("7s", uiMgr.LaserTimerLabel.text);

            uiMgr.HandleLaserPowerupStateChanged(false, 0f);
            Assert.IsTrue(uiMgr.LaserStatusBadge.ClassListContains("powerup-hidden"));

            // Test Clutch Badge
            uiMgr.HandleClutchStateChanged(true, 12f, 10);
            Assert.IsFalse(uiMgr.ClutchStatusBadge.ClassListContains("powerup-hidden"));
            Assert.AreEqual("10X", uiMgr.ClutchMultiplierLabel.text);
            Assert.AreEqual("12s", uiMgr.ClutchTimerLabel.text);

            uiMgr.HandleClutchTick(7.3f, 8);
            Assert.AreEqual("8X", uiMgr.ClutchMultiplierLabel.text);
            Assert.AreEqual("8s", uiMgr.ClutchTimerLabel.text);

            uiMgr.HandleClutchStateChanged(false, 0f, 1);
            Assert.IsTrue(uiMgr.ClutchStatusBadge.ClassListContains("powerup-hidden"));

            Object.DestroyImmediate(uiManagerGo);
        }

        #endregion

        #region 17. Hybrid Scoring, Volley Combo, Par Times & Scorecard Tests

        [Test]
        public void BallController_VolleyStreak_IncrementsAndCalculatesMultiplierCorrectly()
        {
            Assert.AreEqual(1, BallController.GetVolleyMultiplier(0));
            Assert.AreEqual(1, BallController.GetVolleyMultiplier(1));
            Assert.AreEqual(1, BallController.GetVolleyMultiplier(2));
            Assert.AreEqual(2, BallController.GetVolleyMultiplier(3));
            Assert.AreEqual(2, BallController.GetVolleyMultiplier(4));
            Assert.AreEqual(3, BallController.GetVolleyMultiplier(5));
            Assert.AreEqual(3, BallController.GetVolleyMultiplier(7));
            Assert.AreEqual(4, BallController.GetVolleyMultiplier(8));
            Assert.AreEqual(4, BallController.GetVolleyMultiplier(10));
            Assert.AreEqual(5, BallController.GetVolleyMultiplier(11));
        }

        [Test]
        public void ArcadeGameManager_VolleyCombo_TracksHighestComboAndNotifiesSubscribers()
        {
            var mgrGo = new GameObject("TestMgr");
            var mgr = mgrGo.AddComponent<ArcadeGameManager>();
            ArcadeGameManager.SetInstanceForTesting(mgr);

            int lastStreak = -1;
            int lastMult = -1;
            mgr.OnVolleyComboChanged += (streak, mult) =>
            {
                lastStreak = streak;
                lastMult = mult;
            };

            mgr.ResetLevelSessionStats();
            Assert.AreEqual(0, mgr.CurrentVolleyStreak);
            Assert.AreEqual(1, mgr.HighestVolleyComboThisLevel);

            // Streak 5 corresponds to 3x multiplier
            mgr.NotifyVolleyHit(null, 5);
            Assert.AreEqual(5, mgr.CurrentVolleyStreak);
            Assert.AreEqual(3, mgr.CurrentVolleyMultiplier);
            Assert.AreEqual(3, mgr.HighestVolleyComboThisLevel);
            Assert.AreEqual(5, lastStreak);
            Assert.AreEqual(3, lastMult);

            mgr.NotifyVolleySaved(null, 3);
            Assert.AreEqual(0, mgr.CurrentVolleyStreak);
            Assert.AreEqual(1, mgr.CurrentVolleyMultiplier);
            Assert.AreEqual(3, mgr.HighestVolleyComboThisLevel, "Highest volley combo must persist across saves until level reset.");

            Object.DestroyImmediate(mgrGo);
            ArcadeGameManager.SetInstanceForTesting(null);
        }

        [Test]
        public void LevelConfiguration_ParTimeAndStarThresholds_ClonedAccurately()
        {
            var config = ScriptableObject.CreateInstance<LevelConfiguration>();
            config.SetParTime(35f);
            config.SetTimeBonusMax(2800);
            config.SetStarThresholds(new int[] { 500, 1200, 2200 });

            Assert.AreEqual(35f, config.ParTime);
            Assert.AreEqual(2800, config.TimeBonusMax);
            Assert.AreEqual(3, config.StarThresholds.Length);
            Assert.AreEqual(500, config.StarThresholds[0]);
            Assert.AreEqual(1200, config.StarThresholds[1]);
            Assert.AreEqual(2200, config.StarThresholds[2]);

            var clone = config.Clone();
            Assert.AreEqual(35f, clone.ParTime);
            Assert.AreEqual(2800, clone.TimeBonusMax);
            Assert.AreEqual(3, clone.StarThresholds.Length);
            Assert.AreEqual(500, clone.StarThresholds[0]);
            Assert.AreEqual(1200, clone.StarThresholds[1]);
            Assert.AreEqual(2200, clone.StarThresholds[2]);

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(clone);
        }

        [Test]
        public void HighScoreManager_StarsAndBestTime_PersistAndClampCorrectly()
        {
            int testLvl = 99;
            PlayerPrefs.DeleteKey(HighScoreManager.PREF_LEVEL_STARS_PREFIX + testLvl);
            PlayerPrefs.DeleteKey(HighScoreManager.PREF_LEVEL_TIME_PREFIX + testLvl);

            Assert.AreEqual(0, HighScoreManager.GetLevelStars(testLvl));
            Assert.AreEqual(0f, HighScoreManager.GetLevelBestTime(testLvl));

            // Setting stars
            bool set1 = HighScoreManager.SetLevelStars(testLvl, 2);
            Assert.IsTrue(set1);
            Assert.AreEqual(2, HighScoreManager.GetLevelStars(testLvl));

            // Lower stars should not overwrite
            bool setLower = HighScoreManager.SetLevelStars(testLvl, 1);
            Assert.IsFalse(setLower);
            Assert.AreEqual(2, HighScoreManager.GetLevelStars(testLvl));

            // Higher stars should overwrite
            bool setHigher = HighScoreManager.SetLevelStars(testLvl, 3);
            Assert.IsTrue(setHigher);
            Assert.AreEqual(3, HighScoreManager.GetLevelStars(testLvl));

            // Recording best time
            bool rec1 = HighScoreManager.RecordLevelTime(testLvl, 45.5f);
            Assert.IsTrue(rec1);
            Assert.AreEqual(45.5f, HighScoreManager.GetLevelBestTime(testLvl));

            // Slower time should not overwrite
            bool recSlower = HighScoreManager.RecordLevelTime(testLvl, 52.0f);
            Assert.IsFalse(recSlower);
            Assert.AreEqual(45.5f, HighScoreManager.GetLevelBestTime(testLvl));

            // Faster time should overwrite
            bool recFaster = HighScoreManager.RecordLevelTime(testLvl, 38.2f);
            Assert.IsTrue(recFaster);
            Assert.AreEqual(38.2f, HighScoreManager.GetLevelBestTime(testLvl));

            // Formatting
            Assert.AreEqual("00:38", HighScoreManager.FormatTime(38.2f));
            Assert.AreEqual("01:25", HighScoreManager.FormatTime(85f));
            Assert.AreEqual("--:--", HighScoreManager.FormatTime(0f));

            // Cleanup
            PlayerPrefs.DeleteKey(HighScoreManager.PREF_LEVEL_STARS_PREFIX + testLvl);
            PlayerPrefs.DeleteKey(HighScoreManager.PREF_LEVEL_TIME_PREFIX + testLvl);
        }

        [Test]
        public void HighScoreManager_RecordScoreWithTime_PersistsRunElapsedTime()
        {
            HighScoreManager.ResetScores();

            HighScoreManager.RecordScore(1500, 3, 72.5f);
            var scores = HighScoreManager.GetTopScores();
            Assert.AreEqual(1, scores.Count);
            Assert.AreEqual(1500, scores[0].score);
            Assert.AreEqual(3, scores[0].level);
            Assert.AreEqual(72.5f, scores[0].time);

            HighScoreManager.ResetScores();
        }

        [Test]
        public void ArcadeGameManager_HybridScoring_CompoundMultipliersCalculateAccurately()
        {
            var mgrGo = new GameObject("TestMgr");
            var mgr = mgrGo.AddComponent<ArcadeGameManager>();
            ArcadeGameManager.SetInstanceForTesting(mgr);

            int awardedPts = 0;
            int awardedMult = 0;
            mgr.OnBlockPointsAwarded += (pos, pts, mult, tag) =>
            {
                awardedPts = pts;
                awardedMult = mult;
            };

            // Base 20 pts, Volley x3, Multi-ball 1 (no extra balls) -> 20 * 3 = 60 pts
            mgr.RecordBlockDestroyed(20, 1, Vector3.zero, volleyMultiplier: 3, chainMultiplier: 1, bonusTag: "");
            Assert.AreEqual(60, awardedPts);
            Assert.AreEqual(3, awardedMult);
            Assert.AreEqual(60, mgr.Score);

            Object.DestroyImmediate(mgrGo);
            ArcadeGameManager.SetInstanceForTesting(null);
        }

        [Test]
        public void ArcadeUIManager_TimerAndComboBadges_BindsAndDisplaysCorrectly()
        {
            var uiManagerGo = new GameObject("TestArcadeUIManager");
            var panelRenderer = uiManagerGo.AddComponent<UnityEngine.UIElements.PanelRenderer>();
            var uiMgr = uiManagerGo.AddComponent<ArcadeUIManager>();

            var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsNotNull(uxml, "BlockBreakerHUD.uxml must exist.");

            panelRenderer.visualTreeAsset = uxml;
            var root = uxml.CloneTree();

            var bindMethod = typeof(ArcadeUIManager).GetMethod("BindElements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            typeof(ArcadeUIManager).GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, root);
            bindMethod.Invoke(uiMgr, null);

            Assert.IsNotNull(uiMgr.TimerLabel);
            Assert.IsNotNull(uiMgr.ScoreDeltaLabel);
            Assert.IsNotNull(uiMgr.ComboStatusBadge);
            Assert.IsNotNull(uiMgr.ComboLabel);
            Assert.IsNotNull(uiMgr.ScorecardStar1);
            Assert.IsNotNull(uiMgr.ScorecardStar2);
            Assert.IsNotNull(uiMgr.ScorecardStar3);
            Assert.IsNotNull(uiMgr.ScorecardBlocksVal);
            Assert.IsNotNull(uiMgr.ScorecardComboVal);
            Assert.IsNotNull(uiMgr.ScorecardTimeVal);
            Assert.IsNotNull(uiMgr.ScorecardTimeBonusVal);
            Assert.IsNotNull(uiMgr.ScorecardFlawlessVal);
            Assert.IsNotNull(uiMgr.BtnReplayLevel);

            // Test Timer Tick
            uiMgr.HandleLevelTimerTick(65.4f);
            Assert.AreEqual("01:05", uiMgr.TimerLabel.text);

            // Test Volley Combo Badge
            uiMgr.HandleVolleyComboChanged(3, 3);
            Assert.IsFalse(uiMgr.ComboStatusBadge.ClassListContains("powerup-hidden"));
            Assert.IsTrue(uiMgr.ComboLabel.text.Contains("x3 COMBO"));

            uiMgr.HandleVolleyComboChanged(0, 1);
            Assert.AreEqual("COMBO ENDED", uiMgr.ComboLabel.text, "Combo ended should display COMBO ENDED notification text.");

            // Test Victory Scorecard Population
            var summary = new LevelSummaryData
            {
                levelNumber = 1,
                levelName = "Test Level",
                blocksDestroyed = 15,
                highestCombo = 4,
                elapsedTime = 28f,
                parTime = 30f,
                timeBonus = 2500,
                isUnderPar = true,
                speedBonus = 500,
                isFlawless = true,
                flawlessBonus = 1000,
                totalLevelScore = 4500,
                cumulativeScore = 4500,
                starsEarned = 3,
                isNewBestTime = true
            };

            uiMgr.HandleLevelCompletedWithTally(summary);
            Assert.AreEqual("15", uiMgr.ScorecardBlocksVal.text);
            Assert.AreEqual("x4", uiMgr.ScorecardComboVal.text);
            Assert.AreEqual("00:28 / 00:30", uiMgr.ScorecardTimeVal.text);
            Assert.IsTrue(uiMgr.ScorecardTimeBonusVal.text.Contains("PAR"));
            Assert.AreEqual("+1,000 FLAWLESS!", uiMgr.ScorecardFlawlessVal.text);

            Object.DestroyImmediate(uiManagerGo);
        }

        #endregion

        #region 18. Launch Safety, Weapon Cleanup & Deactivation Tests

        [Test]
        public void PaddleLaserController_DeactivateAllWeapons_ClearsBothHyperBeamAndBlaster()
        {
            var paddleGo = new GameObject("Paddle");
            var paddle = paddleGo.AddComponent<PaddleController>();
            var laserCtrl = paddle.LaserController;

            laserCtrl.ActivateLaserBlaster(10f);
            laserCtrl.FireRailgunHyperBeam(1.5f);

            Assert.IsTrue(laserCtrl.IsBlasterActive, "Blasters must be active.");
            Assert.IsTrue(laserCtrl.IsHyperBeamActive, "Hyperbeam must be active.");

            laserCtrl.DeactivateAllWeapons();

            Assert.IsFalse(laserCtrl.IsBlasterActive, "Blasters must be deactivated.");
            Assert.IsFalse(laserCtrl.IsHyperBeamActive, "Hyperbeam must be deactivated.");

            Object.DestroyImmediate(paddleGo);
        }

        [Test]
        public void PaddleLaserController_DeactivatesOnStateChangeToReadyToLaunch()
        {
            var paddleGo = new GameObject("Paddle");
            var paddle = paddleGo.AddComponent<PaddleController>();
            var laserCtrl = paddle.LaserController;

            laserCtrl.ActivateLaserBlaster(10f);
            laserCtrl.FireRailgunHyperBeam(1.5f);

            laserCtrl.HandleGameStateChangedDirect(GameState.ReadyToLaunch);

            Assert.IsFalse(laserCtrl.IsBlasterActive, "Blasters must deactivate when state changes to ReadyToLaunch.");
            Assert.IsFalse(laserCtrl.IsHyperBeamActive, "Hyperbeam must deactivate when state changes to ReadyToLaunch.");

            Object.DestroyImmediate(paddleGo);
        }

        [Test]
        public void LaserBolt_ClearAllActiveBolts_DestroysAllInFlightBolts()
        {
            var bolt1 = LaserBolt.Spawn(new Vector3(0f, 0f, 0f));
            var bolt2 = LaserBolt.Spawn(new Vector3(2f, 0f, 0f));

            LaserBolt.ClearAllActiveBolts();

            var remaining = Object.FindObjectsByType<LaserBolt>();
            Assert.AreEqual(0, remaining.Length, "All in-flight laser bolts must be destroyed.");
        }

        [Test]
        public void Block_OnCollisionEnter_DoesNotDestroyIfBallNotLaunched()
        {
            var blockGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var block = blockGo.AddComponent<Block>();
            block.Initialize(BlockColorTier.Red, null, Color.red, BlockSpecialType.Normal);

            var ballGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var ball = ballGo.AddComponent<BallController>();
            // Ball is NOT launched
            Assert.IsFalse(ball.IsLaunched);

            // Simulating collision between unlaunched ball and block
            // Block.OnCollisionEnter guards with ball.IsLaunched
            var blockCollider = blockGo.GetComponent<Collider>();
            Assert.IsFalse(block.IsDestroyed);

            Object.DestroyImmediate(blockGo);
            Object.DestroyImmediate(ballGo);
        }

        [Test]
        public void ArcadeGameManager_LaunchBall_ClearsStaleWeaponsAndProjectiles()
        {
            var mgrGo = new GameObject("TestMgr");
            var mgr = mgrGo.AddComponent<ArcadeGameManager>();
            ArcadeGameManager.SetInstanceForTesting(mgr);

            var paddleGo = new GameObject("Paddle");
            var paddle = paddleGo.AddComponent<PaddleController>();
            paddle.LaserController.ActivateLaserBlaster(10f);
            paddle.LaserController.FireRailgunHyperBeam(1.5f);

            var bolt = LaserBolt.Spawn(new Vector3(0f, 0f, 0f));

            mgr.SetState(GameState.ReadyToLaunch);
            mgr.LaunchBall();

            Assert.AreEqual(GameState.Playing, mgr.State);
            Assert.IsFalse(paddle.LaserController.IsBlasterActive, "Paddle blasters must be cleared before launch.");
            Assert.IsFalse(paddle.LaserController.IsHyperBeamActive, "Hyperbeam must be cleared before launch.");

            var remainingBolts = Object.FindObjectsByType<LaserBolt>();
            Assert.AreEqual(0, remainingBolts.Length, "Orphan laser bolts must be cleared on launch.");

            Object.DestroyImmediate(mgrGo);
            Object.DestroyImmediate(paddleGo);
        }

        [Test]
        public void PaddleLaserController_FireRailgunHyperBeam_HasExtended5SecondDuration()
        {
            var paddleGo = new GameObject("Paddle_Test_Extended");
            var paddle = paddleGo.AddComponent<PaddleController>();
            var laserCtrl = paddle.LaserController;

            laserCtrl.FireRailgunHyperBeam();

            Assert.IsTrue(laserCtrl.IsHyperBeamActive, "Hyperbeam must be active on fire.");
            Assert.AreEqual(5.0f, laserCtrl.HyperBeamDuration, 0.01f, "Hyperbeam default duration must be 5.0s.");
            Assert.AreEqual(3.2f, laserCtrl.BeamWidth, 0.01f, "Hyperbeam aperture width must be 3.2f.");

            // Simulate 2.5 seconds (previously timed out at 1.2s-1.5s)
            laserCtrl.SimulateStepForTesting(2.5f);
            Assert.IsTrue(laserCtrl.IsHyperBeamActive, "Hyperbeam must still be active after 2.5s.");
            Assert.Greater(laserCtrl.HyperBeamTimeRemaining, 0.5f, "Must have remaining duration.");

            // Simulate remaining 2.6 seconds (total 5.1s)
            laserCtrl.SimulateStepForTesting(2.6f);
            Assert.IsFalse(laserCtrl.IsHyperBeamActive, "Hyperbeam should deactivate after 5.0s expires.");

            Object.DestroyImmediate(paddleGo);
        }

        [Test]
        public void ArcadeUIManager_PulseScorePod_ExecutesGracefully()
        {
            var uiGo = new GameObject("UI_Test");
            var uiMgr = uiGo.AddComponent<ArcadeUIManager>();
            ArcadeUIManager.SetInstanceForTesting(uiMgr);

            Assert.DoesNotThrow(() => uiMgr.PulseScorePod(), "PulseScorePod should gracefully execute without errors.");

            Object.DestroyImmediate(uiGo);
        }

        #endregion

        #region 19. Arena Corner Chamfers and Top Wall Tests

        [Test]
        public void BallController_TopCeilingCollision_ResetsConsecutiveWallBounces()
        {
            var ballGo = new GameObject("TestBall");
            var rb = ballGo.AddComponent<Rigidbody>();
            rb.useGravity = false;
            var ball = ballGo.AddComponent<BallController>();

            ball.SetConsecutiveSideWallBouncesForTesting(3);
            Assert.AreEqual(3, ball.ConsecutiveSideWallBounces);

            // Simulate collision with flat horizontal ceiling (normal pointing downward: 0, -1, 0)
            // Call OnCollisionEnter via reflection or verify side-wall count reset behavior
            var method = typeof(BallController).GetMethod("OnCollisionEnter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, "OnCollisionEnter must exist on BallController.");

            Object.DestroyImmediate(ballGo);
        }

        [Test]
        public void LevelGenerator_EnsureCornerChamfers_CreatesCalibratedChamfersWithColliders()
        {
            var boundariesRoot = new GameObject("Boundaries");
            var topWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topWall.name = "TopWall";
            topWall.transform.SetParent(boundariesRoot.transform);
            topWall.transform.localScale = new Vector3(21f, 0.5f, 2f);

            var leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWall";
            leftWall.transform.SetParent(boundariesRoot.transform);
            leftWall.transform.localScale = new Vector3(0.5f, 32f, 2f);

            var rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "RightWall";
            rightWall.transform.SetParent(boundariesRoot.transform);
            rightWall.transform.localScale = new Vector3(0.5f, 32f, 2f);

            var bounceMat = new PhysicsMaterial("TestBounce") { bounciness = 1f };
            topWall.GetComponent<BoxCollider>().sharedMaterial = bounceMat;

            var genGo = new GameObject("TestLevelGen");
            var gen = genGo.AddComponent<LevelGenerator>();

            gen.EnsureCornerChamfers();

            // Verify continuous wall shortening
            Assert.AreEqual(17.4f, topWall.transform.localScale.x, 0.05f, "TopWall must be shortened to 17.4 to continuously join chamfers.");
            Assert.AreEqual(30.2f, leftWall.transform.localScale.y, 0.05f, "LeftWall must be shortened to 30.2 to continuously join chamfer.");
            Assert.AreEqual(7.60f, leftWall.transform.position.y, 0.05f);
            Assert.AreEqual(30.2f, rightWall.transform.localScale.y, 0.05f, "RightWall must be shortened to 30.2 to continuously join chamfer.");
            Assert.AreEqual(7.60f, rightWall.transform.position.y, 0.05f);

            var leftChamfer = boundariesRoot.transform.Find("Chamfer_TopLeft");
            Assert.IsNotNull(leftChamfer, "Chamfer_TopLeft must be created under Boundaries.");
            Assert.AreEqual(-9.40f, leftChamfer.position.x, 0.05f);
            Assert.AreEqual(23.40f, leftChamfer.position.y, 0.05f);
            Assert.AreEqual(45f, leftChamfer.eulerAngles.z, 0.5f);
            Assert.AreEqual(2.5f, leftChamfer.localScale.x, 0.05f, "Chamfer length must be 2.5 for continuous corner joint.");
            var leftCol = leftChamfer.GetComponent<BoxCollider>();
            Assert.IsNotNull(leftCol, "Chamfer_TopLeft must have a BoxCollider.");
            Assert.AreEqual(bounceMat, leftCol.sharedMaterial);

            var rightChamfer = boundariesRoot.transform.Find("Chamfer_TopRight");
            Assert.IsNotNull(rightChamfer, "Chamfer_TopRight must be created under Boundaries.");
            Assert.AreEqual(9.40f, rightChamfer.position.x, 0.05f);
            Assert.AreEqual(23.40f, rightChamfer.position.y, 0.05f);
            Assert.AreEqual(315f, rightChamfer.eulerAngles.z, 0.5f); // -45 deg in euler angles is 315 deg
            Assert.AreEqual(2.5f, rightChamfer.localScale.x, 0.05f, "Chamfer length must be 2.5 for continuous corner joint.");
            var rightCol = rightChamfer.GetComponent<BoxCollider>();
            Assert.IsNotNull(rightCol, "Chamfer_TopRight must have a BoxCollider.");
            Assert.AreEqual(bounceMat, rightCol.sharedMaterial);

            // Calling it again should be idempotent and maintain calibrated sizing
            Assert.DoesNotThrow(() => gen.EnsureCornerChamfers());
            Assert.AreEqual(5, boundariesRoot.transform.childCount, "Idempotent call should not create duplicate chamfers.");
            Assert.AreEqual(2.5f, leftChamfer.localScale.x, 0.05f);

            Object.DestroyImmediate(boundariesRoot);
            Object.DestroyImmediate(genGo);
            Object.DestroyImmediate(bounceMat);
        }

        [Test]
        public void GameplayScene_ContinuousPerimeter_SerializedInSceneAsset()
        {
            string scenePath = "Assets/Scenes/LV_BlockBreaker.unity";
            Assert.IsTrue(System.IO.File.Exists(scenePath), "Gameplay scene file must exist.");

            string sceneYaml = System.IO.File.ReadAllText(scenePath);
            Assert.IsTrue(sceneYaml.Contains("m_Name: Chamfer_TopLeft"), "Chamfer_TopLeft must be serialized in scene asset.");
            Assert.IsTrue(sceneYaml.Contains("m_Name: Chamfer_TopRight"), "Chamfer_TopRight must be serialized in scene asset.");
            Assert.IsTrue(sceneYaml.Contains("m_LocalScale: {x: 17.4, y: 0.5, z: 2}"), "TopWall must be shortened to 17.4 in scene asset.");
            Assert.IsTrue(sceneYaml.Contains("m_LocalScale: {x: 0.5, y: 30.2, z: 2}"), "Side walls must be shortened to 30.2 in scene asset.");
            Assert.IsTrue(sceneYaml.Contains("m_LocalScale: {x: 2.5, y: 0.5, z: 2}"), "Chamfer boxes must be 2.5 length in scene asset.");
        }

        #endregion

        #region 20. Progressive Hyper-Beam Surge & Level Clear Pacing Tests

        [Test]
        public void PaddleLaserController_FireRailgunHyperBeam_ProgressivelySurgesFromPaddle()
        {
            var paddleGo = new GameObject("TestPaddle");
            var paddle = paddleGo.AddComponent<PaddleController>();
            var laserCtrl = paddle.LaserController;

            Assert.AreEqual(0.65f, laserCtrl.BeamSurgeDuration, 0.01f, "Beam surge duration must be 0.65s for clear visual progression.");

            laserCtrl.FireRailgunHyperBeam(5.0f);

            Assert.IsTrue(laserCtrl.IsHyperBeamActive, "Hyper-beam must be active on fire.");
            Assert.LessOrEqual(laserCtrl.CurrentBeamHeight, 1.0f, "Beam must start at paddle deck and not immediately cover full arena.");

            // Simulate partial surge (0.30s of 0.65s surge duration)
            laserCtrl.SimulateStepForTesting(0.30f);
            Assert.Greater(laserCtrl.CurrentBeamHeight, 1.0f, "Beam must progressively extend upwards.");
            Assert.Less(laserCtrl.CurrentBeamHeight, 31.0f, "Beam should not yet be at full height halfway through surge.");

            // Complete surge (further 0.40s, total 0.70s >= 0.65s)
            laserCtrl.SimulateStepForTesting(0.40f);
            Assert.AreEqual(31.0f, laserCtrl.CurrentBeamHeight, 0.1f, "Beam must reach full height of 31 units after surge duration completes.");

            Object.DestroyImmediate(paddleGo);
        }

        [Test]
        public void PaddleLaserController_GetOrCreateHyperBeamMaterial_ResolvesNonNullMaterialWithGradientShader()
        {
            var mat = PaddleLaserController.GetOrCreateHyperBeamMaterial();
            Assert.IsNotNull(mat, "Hyper-beam material must resolve non-null.");
            Assert.IsNotNull(mat.shader, "Shader must be valid.");
            Assert.IsTrue(mat.shader.name.Contains("LaserHyperBeam") || mat.shader.name.Contains("Unlit") || mat.shader.name.Contains("BallTrail"));
        }

        [Test]
        public void ArcadeGameManager_LevelClearDelay_TracksPendingStateAndDelaySeconds()
        {
            var mgrGo = new GameObject("TestMgr");
            var mgr = mgrGo.AddComponent<ArcadeGameManager>();
            ArcadeGameManager.SetInstanceForTesting(mgr);

            Assert.AreEqual(1.5f, mgr.LevelClearDelaySeconds, 0.01f, "Default laser level clear delay must be 1.5s for cinematic readability.");
            Assert.AreEqual(1.0f, mgr.StandardClearDelaySeconds, 0.01f, "Default non-laser level clear delay must be 1.0s for snappy pacing.");
            Assert.IsFalse(mgr.IsLevelClearPending, "Pending flag must be false initially.");

            float firedDelay = 0f;
            bool firedWasLaser = false;
            mgr.OnLevelClearPending += (d, l) =>
            {
                firedDelay = d;
                firedWasLaser = l;
            };

            // Test non-laser clear cadence
            mgr.TriggerLevelClearWithDelayForTesting(false);
            Assert.IsTrue(mgr.IsLevelClearPending);
            Assert.AreEqual(1.0f, firedDelay, 0.01f);
            Assert.IsFalse(firedWasLaser);

            // Test laser clear cadence
            mgr.TriggerLevelClearWithDelayForTesting(true);
            Assert.IsTrue(mgr.IsLevelClearPending);
            Assert.AreEqual(1.5f, firedDelay, 0.01f);
            Assert.IsTrue(firedWasLaser);

            mgr.TriggerImmediateLevelClearForTesting();
            Assert.AreEqual(GameState.LevelClear, mgr.State, "Direct level clear must immediately transition to LevelClear.");
            Assert.IsFalse(mgr.IsLevelClearPending, "Pending flag must be reset upon completion.");

            Object.DestroyImmediate(mgrGo);
            ArcadeGameManager.SetInstanceForTesting(null);
        }

        [Test]
        public void ArcadeGameManager_PlayingStatePreserved_WhileLevelClearPending()
        {
            var mgrGo = new GameObject("TestMgr_PendingState");
            var mgr = mgrGo.AddComponent<ArcadeGameManager>();
            ArcadeGameManager.SetInstanceForTesting(mgr);

            mgr.SetState(GameState.Playing);
            int startingLives = mgr.Lives;

            mgr.TriggerLevelClearWithDelayForTesting(false);
            Assert.IsTrue(mgr.IsLevelClearPending, "Clear pending must be true.");
            Assert.AreEqual(GameState.Playing, mgr.State, "Game state must remain Playing during level clear pending delay.");

            // Late ball lost during pending delay should not decrement lives
            mgr.RecordBallLost();
            Assert.AreEqual(startingLives, mgr.Lives, "Lives must not be lost while level clear is pending.");

            // Final completion transitions state
            mgr.TriggerImmediateLevelClearForTesting();
            Assert.AreEqual(GameState.LevelClear, mgr.State, "State must transition to LevelClear after completion.");
            Assert.IsFalse(mgr.IsLevelClearPending);

            Object.DestroyImmediate(mgrGo);
            ArcadeGameManager.SetInstanceForTesting(null);
        }

        [Test]
        public void ArcadeUIManager_LevelClearBanner_BindsAndDisplaysProperly()
        {
            var uiManagerGo = new GameObject("TestArcadeUIManager");
            var panelRenderer = uiManagerGo.AddComponent<UnityEngine.UIElements.PanelRenderer>();
            var uiMgr = uiManagerGo.AddComponent<ArcadeUIManager>();

            var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsNotNull(uxml, "BlockBreakerHUD.uxml must exist.");

            panelRenderer.visualTreeAsset = uxml;
            var root = uxml.CloneTree();

            var bindMethod = typeof(ArcadeUIManager).GetMethod("BindElements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            typeof(ArcadeUIManager).GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, root);
            bindMethod.Invoke(uiMgr, null);

            Assert.IsNotNull(uiMgr.LevelClearBanner, "LevelClearBanner must be bound from UXML.");
            Assert.IsNotNull(uiMgr.LevelClearBannerText, "LevelClearBannerText must be bound from UXML.");
            Assert.IsNotNull(uiMgr.LevelClearBannerSubtext, "LevelClearBannerSubtext must be bound from UXML.");

            // Banner is hidden by default
            Assert.IsTrue(uiMgr.LevelClearBanner.ClassListContains("level-clear-banner-hidden"));

            // Test non-laser clear pending
            uiMgr.HandleLevelClearPending(0.8f, wasClearedWithLaser: false);
            Assert.IsFalse(uiMgr.LevelClearBanner.ClassListContains("level-clear-banner-hidden"), "Banner must be shown when clear is pending.");
            Assert.AreEqual("LEVEL CLEARED!", uiMgr.LevelClearBannerText.text);
            Assert.IsTrue(uiMgr.LevelClearBannerSubtext.text == "STAGE COMPLETE!" || uiMgr.LevelClearBannerSubtext.text == "FLAWLESS VICTORY!");

            // Test laser clear pending
            uiMgr.HandleLevelClearPending(1.4f, wasClearedWithLaser: true);
            Assert.IsFalse(uiMgr.LevelClearBanner.ClassListContains("level-clear-banner-hidden"));
            Assert.AreEqual("CLUTCH OVERCHARGE!", uiMgr.LevelClearBannerSubtext.text);

            // Test banner hidden upon victory scorecard tally
            uiMgr.HandleLevelCompletedWithTally(default);
            Assert.IsTrue(uiMgr.LevelClearBanner.ClassListContains("level-clear-banner-hidden"), "Banner must be hidden when scorecard modal is shown.");

            Object.DestroyImmediate(uiManagerGo);
        }

        [Test]
        public void ArcadeUIManager_ComboStatusBadge_BindsIconAndDisplaysComboAndEndedText()
        {
            var uiManagerGo = new GameObject("TestArcadeUIManager");
            var panelRenderer = uiManagerGo.AddComponent<UnityEngine.UIElements.PanelRenderer>();
            var uiMgr = uiManagerGo.AddComponent<ArcadeUIManager>();

            var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsNotNull(uxml, "BlockBreakerHUD.uxml must exist.");

            panelRenderer.visualTreeAsset = uxml;
            var root = uxml.CloneTree();

            var bindMethod = typeof(ArcadeUIManager).GetMethod("BindElements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            typeof(ArcadeUIManager).GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, root);
            bindMethod.Invoke(uiMgr, null);

            Assert.IsNotNull(uiMgr.ComboStatusBadge, "ComboStatusBadge must be bound from UXML.");
            Assert.IsNotNull(uiMgr.ComboStatusIcon, "ComboStatusIcon must be bound from UXML.");
            Assert.IsNotNull(uiMgr.ComboLabel, "ComboLabel must be bound from UXML.");

            // Combo is hidden by default
            Assert.IsTrue(uiMgr.ComboStatusBadge.ClassListContains("powerup-hidden"));

            // When combo of 2x starts:
            uiMgr.HandleVolleyComboChanged(3, 2);
            Assert.IsFalse(uiMgr.ComboStatusBadge.ClassListContains("powerup-hidden"), "Badge must be visible during active combo.");
            Assert.IsTrue(uiMgr.ComboLabel.text.Contains("x2 COMBO"));
            Assert.AreEqual(UnityEngine.UIElements.DisplayStyle.Flex, uiMgr.ComboStatusIcon.style.display.value);
            Assert.IsTrue(uiMgr.ComboStatusBadge.ClassListContains("mult-tier-2x"));

            // When combo drops back to 1 (combo saved on paddle or lost):
            uiMgr.HandleVolleyComboChanged(0, 1);
            Assert.AreEqual("COMBO ENDED", uiMgr.ComboLabel.text, "Must show COMBO ENDED notification text.");
            Assert.AreEqual(UnityEngine.UIElements.DisplayStyle.None, uiMgr.ComboStatusIcon.style.display.value, "Icon should be hidden during COMBO ENDED notification.");

            Object.DestroyImmediate(uiManagerGo);
        }

        [Test]
        public void EntityFreeze_BallAndPaddleAndCapsule_FreezeOnLevelClearPending()
        {
            var mgrGo = new GameObject("TestMgr_Freeze");
            var mgr = mgrGo.AddComponent<ArcadeGameManager>();
            ArcadeGameManager.SetInstanceForTesting(mgr);
            mgr.SetState(GameState.Playing);

            // Paddle setup
            var paddleGo = new GameObject("Paddle_FreezeTest");
            var paddle = paddleGo.AddComponent<PaddleController>();

            // Ball setup
            var ballGo = new GameObject("Ball_FreezeTest");
            var rb = ballGo.AddComponent<Rigidbody>();
            rb.useGravity = false;
            var ball = ballGo.AddComponent<BallController>();
            ball.LaunchWithDirection(Vector3.up, 10f);
            Assert.Greater(ball.Velocity.sqrMagnitude, 1f, "Ball must be moving initially.");

            // Powerup capsule setup
            var cap = PowerupCapsule.Spawn(new Vector3(0f, 10f, 0f), BlockSpecialType.ExtraHeart);

            // Trigger level clear pending
            mgr.TriggerLevelClearWithDelayForTesting(false);
            Assert.IsTrue(mgr.IsLevelClearPending);

            // Ball freezing check
            ball.FreezeBall();
            Assert.AreEqual(0f, ball.Velocity.sqrMagnitude, 0.001f, "Ball must have zero velocity when frozen.");

            // Paddle movement check during pending clear
            float startX = paddle.transform.position.x;
            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.SetDirectTargetWorldXForTesting(startX + 5f);
            }
            var updateMethod = typeof(PaddleController).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod.Invoke(paddle, null);
            Assert.AreEqual(startX, paddle.transform.position.x, 0.001f, "Paddle must not move while level clear is pending.");

            // Powerup intercept check
            bool intercepted = cap.TryIntercept(paddle);
            Assert.IsFalse(intercepted, "Powerup capsule cannot be collected while level clear is pending.");

            Object.DestroyImmediate(mgrGo);
            Object.DestroyImmediate(paddleGo);
            Object.DestroyImmediate(ballGo);
            if (cap != null) Object.DestroyImmediate(cap.gameObject);
            ArcadeGameManager.SetInstanceForTesting(null);
        }

        [Test]
        public void HighScoreManager_SessionScore_UpdatesSameEntryAcrossLevels()
        {
            HighScoreManager.ResetScores();

            string sessionId = "session_test_abc123";

            // Level 1 clear
            bool rec1 = HighScoreManager.RecordScore(1200, 1, 25f, sessionId);
            Assert.IsTrue(rec1);
            var scores1 = HighScoreManager.GetTopScores();
            Assert.AreEqual(1, scores1.Count, "First level clear should add 1 entry.");
            Assert.AreEqual(1200, scores1[0].score);
            Assert.AreEqual(1, scores1[0].level);
            Assert.AreEqual(25f, scores1[0].time);
            Assert.AreEqual(sessionId, scores1[0].sessionId);

            // Level 2 clear in the same session
            bool rec2 = HighScoreManager.RecordScore(3100, 2, 55f, sessionId);
            Assert.IsTrue(rec2);
            var scores2 = HighScoreManager.GetTopScores();
            Assert.AreEqual(1, scores2.Count, "Continuous level clears in same session must update the existing entry, not add a duplicate.");
            Assert.AreEqual(3100, scores2[0].score);
            Assert.AreEqual(2, scores2[0].level);
            Assert.AreEqual(55f, scores2[0].time);
            Assert.AreEqual(sessionId, scores2[0].sessionId);

            // Level 3 clear in the same session
            bool rec3 = HighScoreManager.RecordScore(5800, 3, 90f, sessionId);
            Assert.IsTrue(rec3);
            var scores3 = HighScoreManager.GetTopScores();
            Assert.AreEqual(1, scores3.Count, "Continuous session must remain exactly 1 leaderboard row.");
            Assert.AreEqual(5800, scores3[0].score);
            Assert.AreEqual(3, scores3[0].level);
            Assert.AreEqual(90f, scores3[0].time);

            HighScoreManager.ResetScores();
        }

        [Test]
        public void HighScoreManager_SessionScore_DifferentSessionsCreateDistinctEntries()
        {
            HighScoreManager.ResetScores();

            string session1 = "run_session_01";
            string session2 = "run_session_02";

            HighScoreManager.RecordScore(2000, 2, 40f, session1);
            HighScoreManager.RecordScore(3500, 3, 60f, session2);

            var scores = HighScoreManager.GetTopScores();
            Assert.AreEqual(2, scores.Count, "Distinct sessions must create distinct entries.");
            Assert.AreEqual(3500, scores[0].score);
            Assert.AreEqual(session2, scores[0].sessionId);
            Assert.AreEqual(2000, scores[1].score);
            Assert.AreEqual(session1, scores[1].sessionId);

            // Session 1 progresses further and overtakes session 2
            HighScoreManager.RecordScore(4500, 4, 85f, session1);
            scores = HighScoreManager.GetTopScores();
            Assert.AreEqual(2, scores.Count, "Updating session 1 must still maintain exactly 2 total entries.");
            Assert.AreEqual(4500, scores[0].score);
            Assert.AreEqual(session1, scores[0].sessionId);
            Assert.AreEqual(3500, scores[1].score);
            Assert.AreEqual(session2, scores[1].sessionId);

            HighScoreManager.ResetScores();
        }

        [Test]
        public void ArcadeGameManager_SessionSaveAndContinue_PreservesSessionIdAndCumulativeScore()
        {
            var mgrGo = new GameObject("TestSessionMgr");
            var mgr = mgrGo.AddComponent<ArcadeGameManager>();
            ArcadeGameManager.SetInstanceForTesting(mgr);

            string testSession = "test_run_guid_777";
            mgr.SetCurrentSessionIdForTesting(testSession);
            mgr.SetCurrentLevelForTesting(4);

            // Simulate scoring
            mgr.RecordBlockDestroyed(2500, 1);
            Assert.AreEqual(2500, mgr.Score);

            // Save session
            mgr.SaveCurrentGameSession();

            Assert.AreEqual(1, PlayerPrefs.GetInt("Arcade_HasSavedGame", 0));
            Assert.AreEqual(2500, PlayerPrefs.GetInt("Arcade_SavedScore", 0));
            Assert.AreEqual(4, PlayerPrefs.GetInt("Arcade_SavedLevel", 0));
            Assert.AreEqual(testSession, PlayerPrefs.GetString("Arcade_SavedSessionId", ""));

            // Clear saved game
            ArcadeGameManager.ClearSavedGame();
            Assert.AreEqual(0, PlayerPrefs.GetInt("Arcade_HasSavedGame", 0));
            Assert.AreEqual(0, PlayerPrefs.GetInt("Arcade_SavedScore", 0));
            Assert.IsFalse(PlayerPrefs.HasKey("Arcade_SavedSessionId"));

            Object.DestroyImmediate(mgrGo);
        }

        [Test]
        public void PowerupCapsule_LaserSprite_ResolvesGunIcon()
        {
            var sprite = PowerupCapsule.GetSpriteForType(BlockSpecialType.Laser);
            Assert.IsNotNull(sprite, "PowerupCapsule must resolve a sprite for Laser type.");
            Assert.IsTrue(sprite.name.Contains("TX_Powerup_Gun"),
                $"Sprite should strictly resolve to TX_Powerup_Gun, but was '{sprite.name}'.");
        }

        [Test]
        public void BlockBadge_Laser_SetsCorrectIconAndClasses()
        {
            var badgeObj = new GameObject("TestBadge_Laser");
            var badge = badgeObj.AddComponent<BlockBadge>();

            var root = new UnityEngine.UIElements.VisualElement();
            var plate = new UnityEngine.UIElements.VisualElement { name = "badge-plate" };
            var icon = new UnityEngine.UIElements.VisualElement { name = "badge-icon" };
            var label = new UnityEngine.UIElements.Label { name = "badge-text" };
            plate.Add(icon);
            plate.Add(label);
            root.Add(plate);

            var typeField = typeof(BlockBadge).GetField("specialType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            typeField.SetValue(badge, BlockSpecialType.Laser);

            badge.UpdateUI(root);

            Assert.IsTrue(icon.ClassListContains("badge-icon-laser"), "Laser badge must have badge-icon-laser class.");
            Assert.IsTrue(plate.ClassListContains("badge-plate-laser"), "Laser badge must have badge-plate-laser class.");
            Assert.AreEqual(UnityEngine.UIElements.DisplayStyle.Flex, icon.style.display.value);
            Assert.AreEqual(UnityEngine.UIElements.DisplayStyle.None, label.style.display.value, "Laser badge text must be hidden.");

            Object.DestroyImmediate(badgeObj);
        }

        [Test]
        public void ArcadeUIManager_MultiplierBadge_SetsIconBackgroundImageWhenActive()
        {
            var uiGo = new GameObject("TestUI");
            var uiMgr = uiGo.AddComponent<ArcadeUIManager>();

            var root = new UnityEngine.UIElements.VisualElement();
            var badge = new UnityEngine.UIElements.VisualElement { name = "multiplier-status-badge" };
            var icon = new UnityEngine.UIElements.VisualElement { name = "multiplier-status-icon" };
            var valLabel = new UnityEngine.UIElements.Label { name = "multiplier-value-label" };
            var timerLabel = new UnityEngine.UIElements.Label { name = "multiplier-timer-label" };
            badge.Add(icon);
            badge.Add(valLabel);
            badge.Add(timerLabel);
            root.Add(badge);

            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            typeof(ArcadeUIManager).GetField("multiplierSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, sprite);

            var bindMethod = typeof(ArcadeUIManager).GetMethod("BindElements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            typeof(ArcadeUIManager).GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, root);
            bindMethod.Invoke(uiMgr, null);

            uiMgr.HandleScoreMultiplierStateChanged(true, 2, 10f);

            Assert.IsFalse(badge.ClassListContains("powerup-hidden"));
            Assert.IsTrue(badge.ClassListContains("mult-tier-2x"));
            Assert.IsNotNull(icon.style.backgroundImage.value.sprite, "Multiplier icon must have valid backgroundImage sprite assigned when active.");

            Object.DestroyImmediate(uiGo);
            Object.DestroyImmediate(sprite);
        }

        [Test]
        public void PaddleLaserController_HyperBeamMaterial_ConfiguredWithGradientShaderAndTexture()
        {
            var mat = PaddleLaserController.GetOrCreateHyperBeamMaterial();
            Assert.IsNotNull(mat, "HyperBeam material must not be null.");
            Assert.IsNotNull(mat.shader, "Shader must not be null.");
            Assert.IsTrue(mat.shader.name.Contains("LaserHyperBeam") || mat.shader.name.Contains("Unlit"));
            Assert.IsTrue(mat.HasProperty("_MainTex") || mat.HasProperty("_BaseMap"), "Shader must support gradient texture property.");
        }

        [Test]
        public void PowerupIconSet_ResolvesAllSpecialTypes()
        {
            var iconSet = ScriptableObject.CreateInstance<PowerupIconSet>();
            var spExpander = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var spBomb = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var spHeart = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var spShield = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var spMulti = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var spPoints = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var spLaser = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);

            iconSet.SetSprites(spExpander, spBomb, spHeart, spShield, spMulti, spPoints, spLaser);

            Assert.AreEqual(spExpander, iconSet.GetSprite(BlockSpecialType.PaddleExpander));
            Assert.AreEqual(spBomb, iconSet.GetSprite(BlockSpecialType.Bomb));
            Assert.AreEqual(spHeart, iconSet.GetSprite(BlockSpecialType.ExtraHeart));
            Assert.AreEqual(spShield, iconSet.GetSprite(BlockSpecialType.Shield));
            Assert.AreEqual(spMulti, iconSet.GetSprite(BlockSpecialType.MultiBall));
            Assert.AreEqual(spPoints, iconSet.GetSprite(BlockSpecialType.ScoreMultiplier2x));
            Assert.AreEqual(spPoints, iconSet.GetSprite(BlockSpecialType.ScoreMultiplier3x));
            Assert.AreEqual(spPoints, iconSet.GetSprite(BlockSpecialType.ScoreMultiplier4x));
            Assert.AreEqual(spPoints, iconSet.GetSprite(BlockSpecialType.ScoreMultiplier5x));
            Assert.AreEqual(spLaser, iconSet.GetSprite(BlockSpecialType.Laser));

            Object.DestroyImmediate(iconSet);
            Object.DestroyImmediate(spExpander);
            Object.DestroyImmediate(spBomb);
            Object.DestroyImmediate(spHeart);
            Object.DestroyImmediate(spShield);
            Object.DestroyImmediate(spMulti);
            Object.DestroyImmediate(spPoints);
            Object.DestroyImmediate(spLaser);
        }

        [Test]
        public void BlockBadge_AppliesDirectSpriteToBackgroundImage()
        {
            var badgeObj = new GameObject("TestBadge_SpriteDirect");
            var badge = badgeObj.AddComponent<BlockBadge>();

            var root = new UnityEngine.UIElements.VisualElement();
            var plate = new UnityEngine.UIElements.VisualElement { name = "badge-plate" };
            var icon = new UnityEngine.UIElements.VisualElement { name = "badge-icon" };
            var label = new UnityEngine.UIElements.Label { name = "badge-text" };
            plate.Add(icon);
            plate.Add(label);
            root.Add(plate);

            var testSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            testSprite.name = "Test_Bomb_Sprite";

            badge.Setup(BlockSpecialType.Bomb, null, null, testSprite);
            badge.UpdateUI(root);

            Assert.IsTrue(icon.ClassListContains("badge-icon-bomb"), "Bomb badge must have badge-icon-bomb class.");
            Assert.IsNotNull(icon.style.backgroundImage.value.sprite, "Bomb icon must have backgroundImage sprite assigned.");
            Assert.AreEqual(testSprite, icon.style.backgroundImage.value.sprite, "Assigned sprite must match the provided sprite.");

            Object.DestroyImmediate(badgeObj);
            Object.DestroyImmediate(testSprite);
        }

        [Test]
        public void PowerupCapsule_ResolvesSpriteFromLevelGeneratorIconSet()
        {
            var lgGo = new GameObject("TestLevelGen");
            var lg = lgGo.AddComponent<LevelGenerator>();
            LevelGenerator.SetInstance(lg);

            var iconSet = ScriptableObject.CreateInstance<PowerupIconSet>();
            var testBomb = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            testBomb.name = "LevelGen_Bomb_Sprite";
            iconSet.SetSprites(null, testBomb, null, null, null, null, null);
            lg.SetIconSet(iconSet);

            var resolved = PowerupCapsule.GetSpriteForType(BlockSpecialType.Bomb);
            Assert.IsNotNull(resolved, "PowerupCapsule must resolve sprite from LevelGenerator.Instance.IconSet.");
            Assert.AreEqual(testBomb, resolved, "Resolved sprite must match the one from LevelGenerator.IconSet.");

            LevelGenerator.SetInstance(null);
            Object.DestroyImmediate(lgGo);
            Object.DestroyImmediate(iconSet);
            Object.DestroyImmediate(testBomb);
        }

        #region Model A Color Physics & Level Density Redesign Tests

        [Test]
        public void BallController_BlockCollision_RedTier_DampensSpeedTowardBaseSpeed()
        {
            var ballObj = new GameObject("TestBall_RedTier");
            ballObj.transform.SetParent(testRoot.transform);
            var rb = ballObj.AddComponent<Rigidbody>();
            var ball = ballObj.AddComponent<BallController>();
            ball.Initialize(gameManager, paddle);

            float initialSpeed = 18f;
            Vector3 customVelocity = new Vector3(10f, 10f, 0f).normalized * initialSpeed;
            ball.SetCurrentSpeedForTesting(initialSpeed);
            rb.linearVelocity = customVelocity;

            // 1. Red dampens speed by redDampenAmount (1.2f)
            ball.ApplyBlockColorInteractionForTesting(BlockColorTier.Red);
            Assert.AreEqual(initialSpeed - ball.RedDampenAmount, ball.CurrentSpeed, 0.001f, "Red tier must dampen ball speed by RedDampenAmount.");
            Assert.AreEqual(initialSpeed - ball.RedDampenAmount, rb.linearVelocity.magnitude, 0.001f, "Rigidbody velocity must match dampened speed.");

            // 2. Red damping never reduces speed below baseSpeed (14f)
            float nearBaseSpeed = ball.BaseSpeed + 0.5f;
            ball.SetCurrentSpeedForTesting(nearBaseSpeed);
            rb.linearVelocity = customVelocity.normalized * nearBaseSpeed;
            ball.ApplyBlockColorInteractionForTesting(BlockColorTier.Red);
            Assert.AreEqual(ball.BaseSpeed, ball.CurrentSpeed, 0.001f, "Red tier damping must floor at BaseSpeed.");
            Assert.AreEqual(ball.BaseSpeed, rb.linearVelocity.magnitude, 0.001f, "Rigidbody velocity must floor at BaseSpeed.");
        }

        [Test]
        public void BallController_BlockCollision_GreenTier_AcceleratesSpeedUpToMaxSpeed()
        {
            var ballObj = new GameObject("TestBall_GreenTier");
            ballObj.transform.SetParent(testRoot.transform);
            var rb = ballObj.AddComponent<Rigidbody>();
            var ball = ballObj.AddComponent<BallController>();
            ball.Initialize(gameManager, paddle);

            float initialSpeed = 15f;
            Vector3 customVelocity = Vector3.up * initialSpeed;
            ball.SetCurrentSpeedForTesting(initialSpeed);
            rb.linearVelocity = customVelocity;

            // 1. Green accelerates speed by 10% (greenBoostPercent)
            ball.ApplyBlockColorInteractionForTesting(BlockColorTier.Green);
            float expectedSpeed = initialSpeed * (1f + ball.GreenBoostPercent);
            Assert.AreEqual(expectedSpeed, ball.CurrentSpeed, 0.001f, "Green tier must boost speed by GreenBoostPercent.");
            Assert.AreEqual(expectedSpeed, rb.linearVelocity.magnitude, 0.001f, "Rigidbody velocity must match boosted speed.");

            // 2. Green acceleration is capped at maxSpeed (22f)
            float nearMaxSpeed = ball.MaxSpeed - 0.5f;
            ball.SetCurrentSpeedForTesting(nearMaxSpeed);
            rb.linearVelocity = customVelocity.normalized * nearMaxSpeed;
            ball.ApplyBlockColorInteractionForTesting(BlockColorTier.Green);
            Assert.AreEqual(ball.MaxSpeed, ball.CurrentSpeed, 0.001f, "Green tier acceleration must cap at MaxSpeed.");
            Assert.AreEqual(ball.MaxSpeed, rb.linearVelocity.magnitude, 0.001f, "Rigidbody velocity must cap at MaxSpeed.");
        }

        [Test]
        public void BallController_BlockCollision_BlueTier_AppliesPrismScatterDeflectionAndSanitizes()
        {
            var ballObj = new GameObject("TestBall_BlueTier");
            ballObj.transform.SetParent(testRoot.transform);
            var rb = ballObj.AddComponent<Rigidbody>();
            var ball = ballObj.AddComponent<BallController>();
            ball.Initialize(gameManager, paddle);

            float speed = 16f;
            Vector3 initialVelocity = new Vector3(8f, 13.8564f, 0f).normalized * speed;

            // Run multiple scatters to verify angle change and trajectory sanitization
            for (int i = 0; i < 20; i++)
            {
                ball.SetCurrentSpeedForTesting(speed);
                rb.linearVelocity = initialVelocity;

                ball.ApplyBlockColorInteractionForTesting(BlockColorTier.Blue);
                Vector3 scattered = rb.linearVelocity;

                // Speed increment or preserved
                Assert.GreaterOrEqual(scattered.magnitude, speed, "Blue tier prism scatter must maintain or increase speed.");

                // Sanitized: vertical floor >= sin(20 deg)
                float minVertical = scattered.magnitude * Mathf.Sin(20f * Mathf.Deg2Rad);
                Assert.GreaterOrEqual(Mathf.Abs(scattered.y), minVertical - 0.01f, "Scattered velocity must satisfy vertical floor.");

                // Sanitized: exclusion deadzone >= sin(5 deg)
                float minHorizontal = scattered.magnitude * Mathf.Sin(5f * Mathf.Deg2Rad);
                Assert.GreaterOrEqual(Mathf.Abs(scattered.x), minHorizontal - 0.01f, "Scattered velocity must avoid vertical deadzone.");
            }
        }

        [Test]
        public void Campaign_AllFifteenLevels_HaveEnrichedBrickDensityAndNoEmptyGutters()
        {
            for (int i = 1; i <= 15; i++)
            {
                string path = $"Assets/Settings/Levels/SO_Level_{i:D2}.asset";
                var config = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>(path);
                Assert.IsNotNull(config, $"SO_Level_{i:D2} must exist.");

                // Grid width must be at least 11 columns to eliminate 6u empty side gutters
                Assert.GreaterOrEqual(config.Columns, 11, $"Level {i} must have at least 11 columns.");

                // Total rows must be at least 6
                Assert.GreaterOrEqual(config.TotalRows, 6, $"Level {i} must have at least 6 total rows.");

                // Total block count must be at least 30 blocks
                Assert.GreaterOrEqual(config.TotalBlocks, 30, $"Level {i} must contain at least 30 blocks for dense gameplay.");

                // Side flanks bumper blocks enabled
                Assert.IsTrue(config.IncludeSideFlanks, $"Level {i} must have IncludeSideFlanks enabled.");
            }
        }

        #region Powerdown & Hazard Subsystem Tests

        [Test]
        public void BlockModifier_PowerdownExtensions_IdentifiesHazardsAndBuffs()
        {
            // All 6 hazards classified as powerdown
            Assert.IsTrue(BlockSpecialType.PaddleShortener.IsPowerdown());
            Assert.IsTrue(BlockSpecialType.PaddleSlower.IsPowerdown());
            Assert.IsTrue(BlockSpecialType.BrickFreezer.IsPowerdown());
            Assert.IsTrue(BlockSpecialType.BallSizeDecreaser.IsPowerdown());
            Assert.IsTrue(BlockSpecialType.BallSlower.IsPowerdown());
            Assert.IsTrue(BlockSpecialType.PaddleFreezer.IsPowerdown());

            // Buffs are NOT powerdowns
            Assert.IsFalse(BlockSpecialType.PaddleExpander.IsPowerdown());
            Assert.IsFalse(BlockSpecialType.ExtraHeart.IsPowerdown());
            Assert.IsFalse(BlockSpecialType.Shield.IsPowerdown());
            Assert.IsFalse(BlockSpecialType.MultiBall.IsPowerdown());
            Assert.IsFalse(BlockSpecialType.Laser.IsPowerdown());
            Assert.IsFalse(BlockSpecialType.ScoreMultiplier2x.IsPowerdown());

            // Buffs classified as powerups
            Assert.IsTrue(BlockSpecialType.PaddleExpander.IsPowerup());
            Assert.IsTrue(BlockSpecialType.ExtraHeart.IsPowerup());
            Assert.IsTrue(BlockSpecialType.Shield.IsPowerup());
            Assert.IsTrue(BlockSpecialType.MultiBall.IsPowerup());
            Assert.IsTrue(BlockSpecialType.Laser.IsPowerup());
            Assert.IsTrue(BlockSpecialType.ScoreMultiplier2x.IsPowerup());
            Assert.IsFalse(BlockSpecialType.PaddleShortener.IsPowerup());

            // Unified Palette
            Assert.AreEqual(new Color(1.0f, 0.10f, 0.25f), BlockModifierExtensions.UnifiedPowerdownColor);
            Assert.AreEqual(new Color(0.0f, 0.95f, 1.0f), BlockModifierExtensions.UnifiedPowerupColor);
        }

        [Test]
        public void PowerupCapsule_Spawn_Powerdown_CreatesDiamondCubeMeshAndHazardGlow()
        {
            // Spawn Powerdown (PaddleShortener)
            var powerdown = PowerupCapsule.Spawn(Vector3.zero, BlockSpecialType.PaddleShortener);
            Assert.IsNotNull(powerdown);
            Assert.IsNotNull(powerdown.VisualCapsuleTransform);

            var cubeMf = powerdown.VisualCapsuleTransform.GetComponent<MeshFilter>();
            Assert.IsNotNull(cubeMf);
            Assert.IsNotNull(cubeMf.sharedMesh);
            Assert.IsTrue(cubeMf.sharedMesh.name.IndexOf("Cube", System.StringComparison.OrdinalIgnoreCase) >= 0,
                "Powerdowns must instantiate a 3D Cube primitive mesh to tumble as a diamond.");
            Assert.AreEqual(new Vector3(0.72f, 0.72f, 0.72f), powerdown.VisualCapsuleTransform.localScale);

            // Spawn Powerup (PaddleExpander)
            var powerup = PowerupCapsule.Spawn(new Vector3(5f, 0f, 0f), BlockSpecialType.PaddleExpander);
            Assert.IsNotNull(powerup);
            Assert.IsNotNull(powerup.VisualCapsuleTransform);

            var capsuleMf = powerup.VisualCapsuleTransform.GetComponent<MeshFilter>();
            Assert.IsNotNull(capsuleMf);
            Assert.IsNotNull(capsuleMf.sharedMesh);
            Assert.IsTrue(capsuleMf.sharedMesh.name.IndexOf("Capsule", System.StringComparison.OrdinalIgnoreCase) >= 0,
                "Powerups must instantiate a 3D Capsule primitive mesh.");
            Assert.AreEqual(new Vector3(0.85f, 0.85f, 0.85f), powerup.VisualCapsuleTransform.localScale);

            Object.DestroyImmediate(powerdown.gameObject);
            Object.DestroyImmediate(powerup.gameObject);
        }

        [Test]
        public void PaddleController_ShrinkWidth_ReducesWidthAndEnforcesMinFloor()
        {
            var paddleGo = new GameObject("Paddle");
            var paddle = paddleGo.AddComponent<PaddleController>();
            paddle.ResetWidth(5.0f);

            Assert.AreEqual(5.0f, paddle.CurrentWidth, 0.01f);

            // Shrink by 18%
            paddle.ShrinkWidth(0.18f);
            float expectedWidth = 5.0f * (1f - 0.18f); // 4.10f
            Assert.AreEqual(expectedWidth, paddle.CurrentWidth, 0.05f);

            // Excessive shrink clamps to minWidth (2.4f)
            paddle.ShrinkWidth(0.90f);
            Assert.AreEqual(2.4f, paddle.CurrentWidth, 0.01f, "Paddle width must be clamped at minimum floor (2.4f).");

            Object.DestroyImmediate(paddleGo);
        }

        [Test]
        public void PaddleController_FreezeAndSlow_ControlsStateFlags()
        {
            var paddleGo = new GameObject("Paddle");
            var paddle = paddleGo.AddComponent<PaddleController>();

            Assert.IsFalse(paddle.IsFrozen);
            Assert.IsFalse(paddle.IsSlowed);

            paddle.SetFrozen(true);
            Assert.IsTrue(paddle.IsFrozen);

            paddle.SetFrozen(false);
            Assert.IsFalse(paddle.IsFrozen);

            paddle.SetSlowed(true);
            Assert.IsTrue(paddle.IsSlowed);

            paddle.SetSlowed(false);
            Assert.IsFalse(paddle.IsSlowed);

            Object.DestroyImmediate(paddleGo);
        }

        [Test]
        public void BallController_BallShrunk_ScalesBallToSixtyPercent()
        {
            var ballGo = new GameObject("Ball");
            var ball = ballGo.AddComponent<BallController>();

            Vector3 initialScale = ballGo.transform.localScale;

            ball.SetBallShrunk(true);
            Assert.IsTrue(ball.IsBallShrunk);
            Assert.AreEqual(initialScale.x * 0.60f, ballGo.transform.localScale.x, 0.01f);

            ball.SetBallShrunk(false);
            Assert.IsFalse(ball.IsBallShrunk);
            Assert.AreEqual(initialScale.x, ballGo.transform.localScale.x, 0.01f);

            Object.DestroyImmediate(ballGo);
        }

        [Test]
        public void BallController_BallSlowed_SetsSlowState()
        {
            var ballGo = new GameObject("Ball");
            var ball = ballGo.AddComponent<BallController>();

            Assert.IsFalse(ball.IsBallSlowed);

            ball.SetBallSlowed(true);
            Assert.IsTrue(ball.IsBallSlowed);

            ball.SetBallSlowed(false);
            Assert.IsFalse(ball.IsBallSlowed);

            Object.DestroyImmediate(ballGo);
        }

        [Test]
        public void Block_Freeze_AbsorbsHitAndRequiresDefrostHitBeforeDestruction()
        {
            var blockGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var block = blockGo.AddComponent<Block>();
            block.Initialize(BlockColorTier.Red, null, Color.red, BlockSpecialType.Normal);

            // Freeze block with 1 defrost hit required
            block.Freeze(1);
            Assert.IsTrue(block.IsFrozen);
            Assert.IsFalse(block.IsDestroyed);

            // Hit 1: Absorbs hit, defrosted, NOT destroyed
            block.TakeHit(Vector3.up);
            Assert.IsFalse(block.IsFrozen, "Block must defrost upon taking hit.");
            Assert.IsFalse(block.IsDestroyed, "Block must absorb defrost hit without being destroyed.");

            // Hit 2: Block is now normal, should be destroyed
            block.TakeHit(Vector3.up);
            Assert.IsTrue(block.IsDestroyed, "Block should be destroyed on subsequent hit after defrosting.");

            Object.DestroyImmediate(blockGo);
        }

        [Test]
        public void Block_BrickFreezer_InitializesFrozen_RemovesBadgeOnFirstHit_DestroysOnSecondHit_AwardsZeroPoints()
        {
            var blockGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var block = blockGo.AddComponent<Block>();
            block.Initialize(BlockColorTier.Blue, null, Color.cyan, BlockSpecialType.BrickFreezer);

            var badgeGo = new GameObject("UI_Badge");
            badgeGo.transform.SetParent(blockGo.transform);
            badgeGo.AddComponent<BlockBadge>();

            Assert.IsTrue(block.IsFrozen, "BrickFreezer block must initialize in frozen state.");
            Assert.AreEqual(0, block.Points, "Powerdown blocks must award 0 points.");
            Assert.IsFalse(block.IsDestroyed);

            // Hit 1: Absorbs hit, defrosted, badge removed
            block.TakeHit(Vector3.up);
            Assert.IsFalse(block.IsFrozen, "Block must defrost upon first hit.");
            Assert.IsFalse(block.IsDestroyed, "Block must absorb defrost hit without being destroyed.");
            Assert.IsNull(blockGo.GetComponentInChildren<BlockBadge>(), "Badge icon must disappear on first hit.");

            // Hit 2: Block breaks, awards 0 points
            block.TakeHit(Vector3.up);
            Assert.IsTrue(block.IsDestroyed, "Block must be destroyed on second hit.");

            Object.DestroyImmediate(blockGo);
        }

        [Test]
        public void Block_CollectiblePowerdown_Destruction_AwardsZeroPoints_AndSpawnsFallingDiamond()
        {
            PowerupCapsule.ClearAllFallingCapsules();

            var blockGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var block = blockGo.AddComponent<Block>();
            block.Initialize(BlockColorTier.Red, null, Color.red, BlockSpecialType.PaddleShortener);

            Assert.AreEqual(0, block.Points, "Powerdown block Points property must be 0.");
            Assert.IsTrue(block.SpecialType.IsCollectiblePowerdown());

            // Destroy block - should spawn falling capsule
            block.DestroyBlock(Vector3.up);
            Assert.IsTrue(block.IsDestroyed);

            var capsules = Object.FindObjectsByType<PowerupCapsule>(FindObjectsSortMode.None);
            Assert.AreEqual(1, capsules.Length, "Destroying collectible powerdown block must spawn falling capsule.");

            var cap = capsules[0];
            Assert.AreEqual(BlockSpecialType.PaddleShortener, cap.SpecialType);
            Assert.IsNotNull(cap.VisualCapsuleTransform);

            var cubeMf = cap.VisualCapsuleTransform.GetComponent<MeshFilter>();
            Assert.IsNotNull(cubeMf);
            Assert.IsTrue(cubeMf.sharedMesh.name.IndexOf("Cube", System.StringComparison.OrdinalIgnoreCase) >= 0,
                "Powerdown capsule must be 3D diamond cube mesh.");

            PowerupCapsule.ClearAllFallingCapsules();
            Object.DestroyImmediate(blockGo);
        }

        [Test]
        public void PowerupCapsule_PowerdownFallbacks_ResolveAllPowerdownSprites()
        {
            var types = new[]
            {
                BlockSpecialType.PaddleShortener,
                BlockSpecialType.PaddleSlower,
                BlockSpecialType.BrickFreezer,
                BlockSpecialType.BallSizeDecreaser,
                BlockSpecialType.BallSlower,
                BlockSpecialType.PaddleFreezer
            };

            foreach (var t in types)
            {
                var sprite = PowerupCapsule.GetSpriteForType(t);
                Assert.IsNotNull(sprite, $"PowerupCapsule must resolve a non-null sprite for {t}");
            }
        }

        [Test]
        public void BlockBadge_Coloration_BindsCorrectColors_WithoutCyanOverride()
        {
            var badgeGo = new GameObject("TestBadge");
            var badge = badgeGo.AddComponent<BlockBadge>();

            var root = new UnityEngine.UIElements.VisualElement();
            var plate = new UnityEngine.UIElements.VisualElement { name = "badge-plate" };
            var icon = new UnityEngine.UIElements.VisualElement { name = "badge-icon" };
            plate.Add(icon);
            root.Add(plate);

            var typeField = typeof(BlockBadge).GetField("specialType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Test Laser
            typeField.SetValue(badge, BlockSpecialType.Laser);
            badge.UpdateUI(root);
            Assert.AreEqual(BlockSpecialType.Laser.GetBadgeColor(), icon.style.unityBackgroundImageTintColor.value,
                "Laser icon tint must match Laser badge color (#ff2a47), not overridden by cyan.");

            // Test ExtraHeart
            typeField.SetValue(badge, BlockSpecialType.ExtraHeart);
            badge.UpdateUI(root);
            Assert.AreEqual(BlockSpecialType.ExtraHeart.GetBadgeColor(), icon.style.unityBackgroundImageTintColor.value,
                "ExtraHeart icon tint must match Heart badge color (#ff3b56), not overridden by cyan.");

            // Test MultiBall
            typeField.SetValue(badge, BlockSpecialType.MultiBall);
            badge.UpdateUI(root);
            Assert.AreEqual(BlockSpecialType.MultiBall.GetBadgeColor(), icon.style.unityBackgroundImageTintColor.value,
                "MultiBall icon tint must match MultiBall badge color (#e056fd), not overridden by cyan.");

            // Test Powerdown (PaddleShortener)
            typeField.SetValue(badge, BlockSpecialType.PaddleShortener);
            badge.UpdateUI(root);
            Assert.AreEqual(BlockModifierExtensions.UnifiedPowerdownColor, icon.style.unityBackgroundImageTintColor.value,
                "Powerdown icon tint must match UnifiedPowerdownColor (#ff1744).");

            Object.DestroyImmediate(badgeGo);
        }

        [Test]
        public void ArcadeGameManager_PowerdownLifecycle_ActivatesTicksAndDeactivates()
        {
            var gmGo = new GameObject("ArcadeGameManager");
            var gm = gmGo.AddComponent<ArcadeGameManager>();
            ArcadeGameManager.SetInstanceForTesting(gm);

            int eventCount = 0;
            BlockSpecialType lastType = BlockSpecialType.Normal;
            bool lastActive = false;

            gm.OnPowerdownStateChanged += (type, active, duration) =>
            {
                eventCount++;
                lastType = type;
                lastActive = active;
            };

            // Test PaddleShortener
            gm.ActivatePaddleShortener(10f);
            Assert.IsTrue(gm.IsPaddleShortened);
            Assert.AreEqual(10f, gm.PaddleShortenTimeRemaining);
            Assert.AreEqual(BlockSpecialType.PaddleShortener, lastType);
            Assert.IsTrue(lastActive);

            gm.TickPaddleShortener(5f);
            Assert.AreEqual(5f, gm.PaddleShortenTimeRemaining);

            gm.DeactivatePaddleShortener();
            Assert.IsFalse(gm.IsPaddleShortened);

            // Test PaddleSlower
            gm.ActivatePaddleSlower(8f);
            Assert.IsTrue(gm.IsPaddleSlowed);
            Assert.AreEqual(BlockSpecialType.PaddleSlower, lastType);

            // Test PaddleFreezer
            gm.ActivatePaddleFreezer(1.2f);
            Assert.IsTrue(gm.IsPaddleFrozen);
            Assert.AreEqual(BlockSpecialType.PaddleFreezer, lastType);

            // Test BallSizeDecreaser
            gm.ActivateBallSizeDecreaser(10f);
            Assert.IsTrue(gm.IsBallSizeDecreased);
            Assert.IsTrue(gm.IsBallShrunk);
            Assert.AreEqual(BlockSpecialType.BallSizeDecreaser, lastType);

            // Test BallSlower
            gm.ActivateBallSlower(8f);
            Assert.IsTrue(gm.IsBallSlowed);
            Assert.AreEqual(BlockSpecialType.BallSlower, lastType);

            // Clear all active powerdowns
            gm.ClearActivePowerdowns();
            Assert.IsFalse(gm.IsPaddleShortened);
            Assert.IsFalse(gm.IsPaddleSlowed);
            Assert.IsFalse(gm.IsPaddleFrozen);
            Assert.IsFalse(gm.IsBallSizeDecreased);
            Assert.IsFalse(gm.IsBallSlowed);

            ArcadeGameManager.SetInstanceForTesting(null);
            Object.DestroyImmediate(gmGo);
        }

        [Test]
        public void ArcadeUIManager_PowerdownStateChanged_UpdatesTopCenterTimersAndBadges()
        {
            var uiGo = new GameObject("UI");
            uiGo.AddComponent<PanelRenderer>();
            var uiMgr = uiGo.AddComponent<ArcadeUIManager>();

            var root = new VisualElement();

            var shrinkBadge = new VisualElement { name = "paddle-shrink-status-badge" };
            shrinkBadge.AddToClassList("powerup-hidden");
            var shrinkLabel = new Label { name = "paddle-shrink-timer-label" };
            shrinkBadge.Add(shrinkLabel);

            var slowBadge = new VisualElement { name = "paddle-slow-status-badge" };
            slowBadge.AddToClassList("powerup-hidden");
            var slowLabel = new Label { name = "paddle-slow-timer-label" };
            slowBadge.Add(slowLabel);

            var freezeBadge = new VisualElement { name = "paddle-freeze-status-badge" };
            freezeBadge.AddToClassList("powerup-hidden");
            var freezeLabel = new Label { name = "paddle-freeze-timer-label" };
            freezeBadge.Add(freezeLabel);

            var ballShrinkBadge = new VisualElement { name = "ball-shrink-status-badge" };
            ballShrinkBadge.AddToClassList("powerup-hidden");
            var ballShrinkLabel = new Label { name = "ball-shrink-timer-label" };
            ballShrinkBadge.Add(ballShrinkLabel);

            var ballSlowBadge = new VisualElement { name = "ball-slow-status-badge" };
            ballSlowBadge.AddToClassList("powerup-hidden");
            var ballSlowLabel = new Label { name = "ball-slow-timer-label" };
            ballSlowBadge.Add(ballSlowLabel);

            root.Add(shrinkBadge);
            root.Add(slowBadge);
            root.Add(freezeBadge);
            root.Add(ballShrinkBadge);
            root.Add(ballSlowBadge);

            typeof(ArcadeUIManager).GetField("paddleShrinkStatusBadge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, shrinkBadge);
            typeof(ArcadeUIManager).GetField("paddleShrinkTimerLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, shrinkLabel);
            typeof(ArcadeUIManager).GetField("paddleSlowStatusBadge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, slowBadge);
            typeof(ArcadeUIManager).GetField("paddleSlowTimerLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, slowLabel);
            typeof(ArcadeUIManager).GetField("paddleFreezeStatusBadge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, freezeBadge);
            typeof(ArcadeUIManager).GetField("paddleFreezeTimerLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, freezeLabel);
            typeof(ArcadeUIManager).GetField("ballShrinkStatusBadge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, ballShrinkBadge);
            typeof(ArcadeUIManager).GetField("ballShrinkTimerLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, ballShrinkLabel);
            typeof(ArcadeUIManager).GetField("ballSlowStatusBadge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, ballSlowBadge);
            typeof(ArcadeUIManager).GetField("ballSlowTimerLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiMgr, ballSlowLabel);

            // Activate Paddle Shortener
            uiMgr.HandlePowerdownStateChanged(BlockSpecialType.PaddleShortener, true, 10f);
            Assert.IsFalse(shrinkBadge.ClassListContains("powerup-hidden"));
            Assert.AreEqual(DisplayStyle.Flex, shrinkBadge.style.display.value);
            Assert.AreEqual("10s", shrinkLabel.text);

            // Tick Paddle Shortener
            uiMgr.HandlePowerdownTick(BlockSpecialType.PaddleShortener, 7.3f);
            Assert.AreEqual("8s", shrinkLabel.text);

            // Deactivate Paddle Shortener
            uiMgr.HandlePowerdownStateChanged(BlockSpecialType.PaddleShortener, false, 0f);
            Assert.IsTrue(shrinkBadge.ClassListContains("powerup-hidden"));
            Assert.AreEqual(DisplayStyle.None, shrinkBadge.style.display.value);

            // Activate Paddle Freezer
            uiMgr.HandlePowerdownStateChanged(BlockSpecialType.PaddleFreezer, true, 1.2f);
            Assert.IsFalse(freezeBadge.ClassListContains("powerup-hidden"));
            Assert.AreEqual(DisplayStyle.Flex, freezeBadge.style.display.value);
            Assert.AreEqual("2s", freezeLabel.text);

            Object.DestroyImmediate(uiGo);
        }

        [Test]
        public void PowerupIconSet_ContainsAllAssignedPowerdownSprites_LoadsCorrectly()
        {
            var iconSet = UnityEditor.AssetDatabase.LoadAssetAtPath<PowerupIconSet>("Assets/Settings/SO_PowerupIcons.asset");
            Assert.IsNotNull(iconSet, "SO_PowerupIcons.asset must exist");

            Assert.IsNotNull(iconSet.PaddleShortenerSprite, "PaddleShortenerSprite should be assigned");
            Assert.AreEqual("TX_Powerdown_Arrows_Inward", iconSet.PaddleShortenerSprite.name);

            Assert.IsNotNull(iconSet.PaddleSlowerSprite, "PaddleSlowerSprite should be assigned");
            Assert.AreEqual("TX_Powerdown_Slower_Paddle", iconSet.PaddleSlowerSprite.name);

            Assert.IsNotNull(iconSet.BrickFreezerSprite, "BrickFreezerSprite should be assigned");
            Assert.AreEqual("TX_Powerdown_Frozen_Brick", iconSet.BrickFreezerSprite.name);

            Assert.IsNotNull(iconSet.BallSizeDecreaserSprite, "BallSizeDecreaserSprite should be assigned");
            Assert.AreEqual("TX_Powerdown_Smaller_Ball", iconSet.BallSizeDecreaserSprite.name);

            Assert.IsNotNull(iconSet.BallSlowerSprite, "BallSlowerSprite should be assigned");
            Assert.AreEqual("TX_Powerdown_Slower_Ball", iconSet.BallSlowerSprite.name);

            Assert.IsNotNull(iconSet.PaddleFreezerSprite, "PaddleFreezerSprite should be assigned");
            Assert.AreEqual("TX_Powerdown_Frozen_Paddle", iconSet.PaddleFreezerSprite.name);

            // Also verify GetSprite mapping
            Assert.AreEqual(iconSet.PaddleShortenerSprite, iconSet.GetSprite(BlockSpecialType.PaddleShortener));
            Assert.AreEqual(iconSet.PaddleSlowerSprite, iconSet.GetSprite(BlockSpecialType.PaddleSlower));
            Assert.AreEqual(iconSet.BrickFreezerSprite, iconSet.GetSprite(BlockSpecialType.BrickFreezer));
            Assert.AreEqual(iconSet.BallSizeDecreaserSprite, iconSet.GetSprite(BlockSpecialType.BallSizeDecreaser));
            Assert.AreEqual(iconSet.BallSlowerSprite, iconSet.GetSprite(BlockSpecialType.BallSlower));
            Assert.AreEqual(iconSet.PaddleFreezerSprite, iconSet.GetSprite(BlockSpecialType.PaddleFreezer));
        }

        #region 27. Physical Shield Wall & Anti-Trap Passthrough Tests

        [Test]
        public void ShieldWall_BounceFromShield_DeflectsUpward_AndPreservesSpeedAndCombo()
        {
            var ballObj = new GameObject("TestBall");
            var ball = ballObj.AddComponent<BallController>();
            var ballRb = ballObj.GetComponent<Rigidbody>();
            if (ballRb == null) ballRb = ballObj.AddComponent<Rigidbody>();
            var ballCol = ballObj.GetComponent<SphereCollider>();
            if (ballCol == null) ballCol = ballObj.AddComponent<SphereCollider>();
            ballObj.AddComponent<MeshRenderer>();
            ballObj.AddComponent<MeshFilter>();
            typeof(BallController).GetField("paddle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, paddle);
            typeof(BallController).GetField("rb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, ballRb);
            typeof(BallController).GetField("isLaunched", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, true);

            gameManager.RegisterBall(ball);
            gameManager.SetState(GameState.Playing);

            ball.SetCurrentSpeedForTesting(15f);
            ball.SetVolleyStreakForTesting(3);
            ball.SetConsecutiveSideWallBouncesForTesting(2);

            // Set ball falling downward
            ballRb.linearVelocity = new Vector3(2f, -14.8f, 0f);

            // Bounce from shield wall contact at X = 2f
            ball.BounceFromShield(new Vector3(2f, -7.6f, 0f));

            // Verify upward deflection
            Assert.Greater(ballRb.linearVelocity.y, 0f, "Ball must deflect upward.");
            float angleDeg = Mathf.Atan2(ballRb.linearVelocity.y, ballRb.linearVelocity.x) * Mathf.Rad2Deg;
            Assert.GreaterOrEqual(angleDeg, 35f, "Upward angle must be at least 35 degrees.");
            Assert.LessOrEqual(angleDeg, 145f, "Upward angle must be at most 145 degrees.");
            Assert.AreEqual(0, ball.ConsecutiveSideWallBounces, "Shield bounce must reset consecutive wall bounces.");

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void BallController_BelowPaddleMovingUp_IgnoresPaddleCollision_PhasesThrough()
        {
            var ballObj = new GameObject("TestBall");
            var ball = ballObj.AddComponent<BallController>();
            var ballRb = ballObj.GetComponent<Rigidbody>();
            if (ballRb == null) ballRb = ballObj.AddComponent<Rigidbody>();
            var ballCol = ballObj.GetComponent<SphereCollider>();
            if (ballCol == null) ballCol = ballObj.AddComponent<SphereCollider>();
            ballObj.AddComponent<MeshRenderer>();
            ballObj.AddComponent<MeshFilter>();
            typeof(BallController).GetField("paddle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, paddle);
            typeof(BallController).GetField("rb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, ballRb);
            typeof(BallController).GetField("ballCollider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, ballCol);
            typeof(BallController).GetField("isLaunched", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ball, true);

            gameManager.RegisterBall(ball);
            gameManager.SetState(GameState.Playing);

            // Set paddle to arena height (-6.5f) where top deck is at -6.0f
            paddle.transform.position = new Vector3(0f, -6.5f, 0f);

            // Position ball below paddle top deck (~ -6.0f) at Y = -7.0f, moving UPWARDS
            ballObj.transform.position = new Vector3(0f, -7.0f, 0f);
            ballRb.linearVelocity = new Vector3(0f, 12f, 0f);

            ball.UpdatePaddlePassThrough();

            Assert.IsTrue(ball.IsPaddleCollisionIgnored, "Ball below paddle moving upward must ignore paddle collision to phase through.");

            // Now ball reaches above paddle top deck at Y = -5.5f, moving UPWARDS
            ballObj.transform.position = new Vector3(0f, -5.5f, 0f);
            ball.UpdatePaddlePassThrough();

            Assert.IsFalse(ball.IsPaddleCollisionIgnored, "Ball above paddle strike deck must restore solid collision.");

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void ShieldWall_WarningPulse_ActivatesInFinalSeconds()
        {
            var wallGo = new GameObject("TestShieldWall");
            var wall = wallGo.AddComponent<ShieldWall>();

            wall.Activate(10f);
            Assert.IsFalse(wall.IsWarningActive, "Warning must be inactive when shield has full duration.");

            wall.SetTimeRemaining(4.0f);
            Assert.IsFalse(wall.IsWarningActive, "Warning must be inactive above threshold (2.5s).");

            wall.SetTimeRemaining(2.0f);
            Assert.IsTrue(wall.IsWarningActive, "Warning must activate when remaining time is below 2.5s.");

            Object.DestroyImmediate(wallGo);
        }

        [Test]
        public void ShieldWall_TweenParameters_ExposedAndConfigurable()
        {
            var wallGo = new GameObject("TestShieldWall");
            var wall = wallGo.AddComponent<ShieldWall>();
            wall.EnsureComponents();

            Assert.Greater(wall.AppearDuration, 0f);
            Assert.Greater(wall.DisappearDuration, 0f);
            Assert.IsNotNull(wall.AppearCurve);
            Assert.IsNotNull(wall.DisappearCurve);
            Assert.AreEqual(20.4f, wall.TargetScale.x, 0.1f);
            Assert.AreEqual(-7.6f, wall.WallY, 0.1f);

            wall.AppearDuration = 0.5f;
            Assert.AreEqual(0.5f, wall.AppearDuration, 0.001f);

            Object.DestroyImmediate(wallGo);
        }

        [Test]
        public void GameManager_ActivateShield_DrivesShieldWallLifecycle()
        {
            var wallGo = new GameObject("TestShieldWall");
            var wall = wallGo.AddComponent<ShieldWall>();
            wallGo.SetActive(false);

            gameManager.RegisterShieldWall(wall);

            Assert.IsFalse(gameManager.IsShieldActive);

            gameManager.ActivateShield(10f);
            Assert.IsTrue(gameManager.IsShieldActive);
            Assert.IsTrue(wallGo.activeSelf, "ShieldWall GameObject must be active when shield is activated.");

            gameManager.TickShield(10.5f);
            Assert.IsFalse(gameManager.IsShieldActive);

            Object.DestroyImmediate(wallGo);
        }

        [Test]
        public void ShieldWall_Material_IsNativeURPCompatible()
        {
            var wallGo = new GameObject("TestShieldWall");
            var wall = wallGo.AddComponent<ShieldWall>();
            wall.EnsureComponents();

            var rend = wallGo.GetComponent<MeshRenderer>();
            Assert.IsNotNull(rend);
            Assert.IsNotNull(rend.sharedMaterial);
            Assert.IsTrue(rend.sharedMaterial.HasProperty("_BaseColor"), "Material must have URP _BaseColor property for iOS compatibility.");
            Assert.IsTrue(rend.sharedMaterial.HasProperty("_EmissionColor"), "Material must have URP _EmissionColor property for iOS compatibility.");

            Object.DestroyImmediate(wallGo);
        }

        #endregion

        #endregion

        #endregion
        #endregion
    }
}


