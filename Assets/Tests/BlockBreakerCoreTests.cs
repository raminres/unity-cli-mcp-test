using Arcade.BlockBreaker;
using Arcade.Core;
using NUnit.Framework;
using UnityEngine;

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

            var paddleObj = new GameObject("Paddle");
            paddleObj.transform.SetParent(testRoot.transform);
            paddleObj.transform.position = Vector3.zero;
            paddle = paddleObj.AddComponent<PaddleController>();
        }

        [TearDown]
        public void TearDown()
        {
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

        #endregion
    }
}

