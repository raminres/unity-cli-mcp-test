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

            // Break Blue block (30 pts)
            gameManager.RecordBlockDestroyed(30, 3);
            Assert.AreEqual(60, gameManager.Score);
            Assert.AreEqual(0, gameManager.RemainingBlocks);

            // All blocks destroyed -> State must transition to LevelClear
            Assert.AreEqual(GameState.LevelClear, gameManager.State);
        }

        #endregion

        #region 2. Paddle Deflection & Boundary Math Tests

        [Test]
        public void Paddle_CalculateHitOffset_CenterReturnsZero()
        {
            paddle.transform.position = Vector3.zero;
            float offset = paddle.CalculateHitOffset(0f);

            Assert.AreEqual(0f, offset, 0.001f, "Hitting paddle center must produce deflection offset 0.");
        }

        [Test]
        public void Paddle_CalculateHitOffset_EdgesReturnPositiveAndNegativeOne()
        {
            // Paddle width is 5.0, so half-width is 2.5
            paddle.transform.position = Vector3.zero;

            float rightEdgeOffset = paddle.CalculateHitOffset(2.5f);
            float leftEdgeOffset = paddle.CalculateHitOffset(-2.5f);

            Assert.AreEqual(1.0f, rightEdgeOffset, 0.001f, "Hitting extreme right edge must produce offset +1.0.");
            Assert.AreEqual(-1.0f, leftEdgeOffset, 0.001f, "Hitting extreme left edge must produce offset -1.0.");
        }

        [Test]
        public void Paddle_CalculateHitOffset_BeyondEdgesIsClamped()
        {
            paddle.transform.position = Vector3.zero;

            float farRightOffset = paddle.CalculateHitOffset(10.0f);
            float farLeftOffset = paddle.CalculateHitOffset(-10.0f);

            Assert.AreEqual(1.0f, farRightOffset, "Offset beyond right edge must be clamped to 1.0.");
            Assert.AreEqual(-1.0f, farLeftOffset, "Offset beyond left edge must be clamped to -1.0.");
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
            gen.AdvanceToNextLevel();
            gameManager.AdvanceToNextLevel();
            Assert.AreEqual(2, gen.CurrentConfig.LevelNumber);
            Assert.AreEqual(30, gameManager.Score, "Score must be preserved when advancing to next level.");
            Assert.AreEqual(GameState.ReadyToLaunch, gameManager.State);

            // Advance to level 3
            gen.AdvanceToNextLevel();
            gameManager.AdvanceToNextLevel();
            Assert.AreEqual(3, gen.CurrentConfig.LevelNumber);
            Assert.AreEqual(30, gameManager.Score);

            // Advance from level 3 loops back to level 1
            gen.AdvanceToNextLevel();
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

        #region 7. Audio System (AU_*) Tests

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

        #region 12. Powerup Icons, Badge Margins, and VFX Shader Tests

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

        #region 12. iOS Controls, Modal Pause & Level Clear Ball Handling Tests

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

        #region 12. New Powerup Mechanics (Glass, Bomb, Extra Heart) Tests

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

        #region 12. Shield & Multi-Ball Power-Up Tests

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
            Assert.AreEqual(GameState.ReadyToLaunch, gameManager.State, "Game must reset to ReadyToLaunch.");

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
            Assert.AreEqual(GameState.ReadyToLaunch, gameManager.State, "Game must reset to ReadyToLaunch.");

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

        #region 13. Progressive 7-Level Campaign Tests

        [Test]
        public void Campaign_AllSevenLevelsExist_AndEnforceProgressiveSpeedAndBlockScaling()
        {
            float lastSpeed = 0f;
            int lastBlocks = 0;

            for (int i = 1; i <= 7; i++)
            {
                string path = $"Assets/Settings/Levels/SO_Level_{i:D2}.asset";
                var config = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>(path);
                Assert.IsNotNull(config, $"Level {i} asset must exist at {path}.");
                Assert.AreEqual(i, config.LevelNumber, $"Level {i} must have matching LevelNumber.");
                Assert.IsFalse(string.IsNullOrEmpty(config.LevelName), $"Level {i} must have a non-empty name.");
                Assert.IsFalse(string.IsNullOrEmpty(config.Description), $"Level {i} must have a non-empty description.");

                Assert.Greater(config.BallSpeedMultiplier, lastSpeed, $"Level {i} speed ({config.BallSpeedMultiplier}) must be strictly faster than Level {i - 1} speed ({lastSpeed}).");
                Assert.GreaterOrEqual(config.TotalBlocks, lastBlocks, $"Level {i} blocks ({config.TotalBlocks}) must be >= Level {i - 1} blocks ({lastBlocks}).");

                lastSpeed = config.BallSpeedMultiplier;
                lastBlocks = config.TotalBlocks;
            }

            Assert.AreEqual(0.85f, UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_01.asset").BallSpeedMultiplier, 0.001f);
            Assert.AreEqual(1.38f, UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_07.asset").BallSpeedMultiplier, 0.001f);
            Assert.AreEqual(15, UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_01.asset").TotalBlocks);
            Assert.AreEqual(90, UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_07.asset").TotalBlocks);
        }

        [Test]
        public void Level1_WarmupGrid_IsAccessibleAndHasZeroHazards()
        {
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_01.asset");
            Assert.IsNotNull(config);

            // Level 1: 5 cols * 3 rows = 15 blocks
            Assert.AreEqual(5, config.Columns);
            Assert.AreEqual(1, config.RowsPerTier);
            Assert.AreEqual(3, config.TotalRows);
            Assert.AreEqual(15, config.TotalBlocks);

            // Generous warmup paddle and comfortable speed
            Assert.AreEqual(5.5f, config.InitialPaddleWidth, 0.001f);
            Assert.AreEqual(0.85f, config.BallSpeedMultiplier, 0.001f);

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
        public void LevelGenerator_AdvanceToNextLevel_CyclesSevenLevelsSeamlessly()
        {
            var genObj = new GameObject("TestGenerator");
            var gen = genObj.AddComponent<LevelGenerator>();

            var levelConfigs = new LevelConfiguration[7];
            for (int i = 0; i < 7; i++)
            {
                levelConfigs[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>($"Assets/Settings/Levels/SO_Level_{i + 1:D2}.asset");
                Assert.IsNotNull(levelConfigs[i]);
            }

            var genSo = new UnityEditor.SerializedObject(gen);
            var presetsProp = genSo.FindProperty("levelPresets");
            presetsProp.arraySize = 7;
            for (int i = 0; i < 7; i++)
            {
                presetsProp.GetArrayElementAtIndex(i).objectReferenceValue = levelConfigs[i];
            }
            genSo.ApplyModifiedProperties();

            // Start at Level 1
            gen.SelectAndLoadLevel(1);
            Assert.AreEqual(1, gen.CurrentConfig.LevelNumber);

            // Advance through 2, 3, 4, 5, 6, 7
            for (int expectedLvl = 2; expectedLvl <= 7; expectedLvl++)
            {
                gen.AdvanceToNextLevel();
                Assert.AreEqual(expectedLvl, gen.CurrentConfig.LevelNumber, $"Expected advancing to Level {expectedLvl}.");
            }

            // Advancing from Level 7 must loop back to Level 1
            gen.AdvanceToNextLevel();
            Assert.AreEqual(1, gen.CurrentConfig.LevelNumber, "Advancing beyond Level 7 must loop back to Level 1 for endless arcade progression.");

            Object.DestroyImmediate(genObj);
        }

        [Test]
        public void LevelGenerator_SpecialBlockDistribution_MaintainsExactCountsWithoutCollisionsAcrossAll7Levels()
        {
            var genObj = new GameObject("TestGenerator");
            var gen = genObj.AddComponent<LevelGenerator>();

            for (int i = 1; i <= 7; i++)
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

        #endregion

        #region Region 14: Timed Buffs & Combo Multipliers Tests

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
        public void Campaign_Level6And7_Contain4xAnd5xMultipliers()
        {
            var lvl6 = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_06.asset");
            Assert.IsNotNull(lvl6);
            Assert.AreEqual(1, lvl6.Multiplier4xCount, "Level 6 must introduce 1x 4X multiplier.");

            var lvl7 = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_07.asset");
            Assert.IsNotNull(lvl7);
            Assert.AreEqual(2, lvl7.Multiplier4xCount, "Level 7 must contain 2x 4X multipliers.");
            Assert.AreEqual(1, lvl7.Multiplier5xCount, "Level 7 must introduce 1x 5X multiplier.");
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

            var balls = Object.FindObjectsByType<BallController>(FindObjectsSortMode.None);
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
    }
}


