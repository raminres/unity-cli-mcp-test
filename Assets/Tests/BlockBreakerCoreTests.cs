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
    }
}

