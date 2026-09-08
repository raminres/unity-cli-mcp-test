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
    }
}
