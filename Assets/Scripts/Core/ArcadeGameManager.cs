using System;
using Arcade.Audio;
using Arcade.BlockBreaker;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arcade.Core
{
    [Serializable]
    public struct LevelSummaryData
    {
        public int levelNumber;
        public string levelName;
        public int blocksDestroyed;
        public int baseBlockPoints;
        public int highestCombo;
        public float elapsedTime;
        public float parTime;
        public int timeBonus;
        public bool isUnderPar;
        public int speedBonus;
        public bool isFlawless;
        public int flawlessBonus;
        public int totalLevelScore;
        public int cumulativeScore;
        public int starsEarned;
        public bool isNewBestTime;
    }

    /// <summary>
    /// Central game coordinator managing state machine, scoring, lives, and high-level gameplay events.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ArcadeGameManager : MonoBehaviour
    {
        public static ArcadeGameManager Instance { get; private set; }

        private const string PREF_HIGH_SCORE = "Arcade_HighScore";
        private const string PREF_SAVED_SCORE = "Arcade_SavedScore";
        private const string PREF_SAVED_LIVES = "Arcade_SavedLives";
        private const string PREF_HAS_SAVED_GAME = "Arcade_HasSavedGame";

        [Header("Game Configuration")]
        public const int MAX_LIVES = 5;
        [SerializeField] private int startingLives = 3;

        [Header("Runtime State")]
        [SerializeField] private GameState currentState = GameState.ReadyToLaunch;
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentScore = 0;
        [SerializeField] private int highScore = 0;
        [SerializeField] private int remainingLives = 3;
        [SerializeField] private int remainingBlocks = 0;
        [SerializeField] private int totalBlocksInLevel = 0;

        // Level Session Statistics
        private float levelElapsedTime = 0f;
        private float totalRunElapsedTime = 0f;
        private int currentVolleyStreak = 0;
        private int highestVolleyComboThisLevel = 1;
        private int blocksDestroyedThisLevel = 0;
        private int baseBlockPointsThisLevel = 0;
        private int livesLostThisLevel = 0;
        private LevelSummaryData currentLevelSummary;

        [Header("Level Clear Pacing")]
        [SerializeField] private float levelClearDelaySeconds = 1.4f;
        [SerializeField] private float standardClearDelaySeconds = 0.8f;
        private bool isLevelClearPending = false;
        private Coroutine levelClearCoroutine;

        public bool IsLevelClearPending => isLevelClearPending;
        public float LevelClearDelaySeconds => levelClearDelaySeconds;
        public float StandardClearDelaySeconds => standardClearDelaySeconds;
        public int LivesLostThisLevel => livesLostThisLevel;

        [Header("Power-Up States")]
        [SerializeField] private bool isShieldActive = false;
        [SerializeField] private float shieldTimeRemaining = 0f;
        [SerializeField] private bool isPaddleExpanded = false;
        [SerializeField] private float paddleExpandTimeRemaining = 0f;
        [SerializeField] private int activeScoreMultiplier = 1;
        [SerializeField] private float multiplierTimeRemaining = 0f;
        [SerializeField] private bool isLaserActive = false;
        [SerializeField] private float laserTimeRemaining = 0f;
        [SerializeField] private float laserDuration = 10f;
        [SerializeField] private bool isClutchModeActive = false;
        [SerializeField] private float clutchTimeRemaining = 0f;
        [SerializeField] private float clutchDuration = 12f;
        [SerializeField] private int clutchMultiplier = 1;

        [Header("Asset References")]
        [SerializeField] private Material powerupCapsuleMaterial;

        private GameState previousStateBeforePause;
        private readonly System.Collections.Generic.List<BallController> activeBalls = new System.Collections.Generic.List<BallController>();

        // Events for UI and gameplay decoupled subscribers
        public event Action<int, int> OnScoreChanged;
        public event Action<int> OnLivesChanged;
        public event Action<GameState> OnStateChanged;
        public event Action<bool> OnPauseToggled;
        public event Action<bool, float> OnShieldStateChanged;
        public event Action<float> OnShieldTick;
        public event Action<int> OnActiveBallCountChanged;
        public event Action<bool, float> OnPaddleExpandStateChanged;
        public event Action<float> OnPaddleExpandTick;
        public event Action<bool, int, float> OnScoreMultiplierStateChanged;
        public event Action<float> OnScoreMultiplierTick;
        public event Action<bool, float> OnLaserPowerupStateChanged;
        public event Action<float> OnLaserPowerupTick;
        public event Action<bool, float, int> OnClutchStateChanged;
        public event Action<float, int> OnClutchTick;
        public event Action<float> OnLevelTimerTick;
        public event Action<int, int> OnVolleyComboChanged; // (currentStreak, multiplier)
        public event Action<Vector3, int, int, string> OnBlockPointsAwarded; // (worldPos, awardedPoints, totalMultiplier, tag)
        public event Action<LevelSummaryData> OnLevelCompletedWithTally;
        public event Action<float, bool> OnLevelClearPending; // (delaySeconds, wasClearedWithLaser)

        public GameState State => currentState;
        public int CurrentLevel => currentLevel;
        public int Score => currentScore;
        public int HighScore => highScore;
        public int Lives => remainingLives;
        public int RemainingBlocks => remainingBlocks;
        public int TotalBlocks => totalBlocksInLevel;
        public float LevelElapsedTime => levelElapsedTime;
        public float TotalRunElapsedTime => totalRunElapsedTime;
        public int CurrentVolleyStreak => currentVolleyStreak;
        public int CurrentVolleyMultiplier => BallController.GetVolleyMultiplier(currentVolleyStreak);
        public int HighestVolleyComboThisLevel => highestVolleyComboThisLevel;
        public LevelSummaryData CurrentLevelSummary => currentLevelSummary;
        public bool IsShieldActive => isShieldActive;
        public float ShieldTimeRemaining => shieldTimeRemaining;
        public bool IsPaddleExpanded => isPaddleExpanded;
        public float PaddleExpandTimeRemaining => paddleExpandTimeRemaining;
        public int ActiveScoreMultiplier => activeScoreMultiplier;
        public float MultiplierTimeRemaining => multiplierTimeRemaining;
        public bool IsLaserActive => isLaserActive;
        public float LaserTimeRemaining => laserTimeRemaining;
        public bool IsClutchModeActive => isClutchModeActive;
        public float ClutchTimeRemaining => clutchTimeRemaining;
        public int ClutchMultiplier => clutchMultiplier;
        public float ClutchDuration => clutchDuration;
        public System.Collections.Generic.IReadOnlyList<BallController> ActiveBalls => activeBalls;
        public int ActiveBallCount => activeBalls.Count;

        public static bool HasSavedGame => PlayerPrefs.GetInt(PREF_HAS_SAVED_GAME, 0) == 1;

        public static void SetInstanceForTesting(ArcadeGameManager instance)
        {
            Instance = instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (powerupCapsuleMaterial != null)
            {
                PowerupCapsule.SetDefaultMaterial(powerupCapsuleMaterial);
            }
            highScore = HighScoreManager.HighestScore;
            HighScoreManager.OnHighScoresChanged += HandleHighScoresChanged;
        }

        private void HandleHighScoresChanged()
        {
            highScore = HighScoreManager.HighestScore;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            HighScoreManager.OnHighScoresChanged -= HandleHighScoresChanged;
        }

        private void Start()
        {
            if (PlayerPrefs.GetInt("Arcade_LoadSavedGameOnStart", 0) == 1)
            {
                PlayerPrefs.SetInt("Arcade_LoadSavedGameOnStart", 0);
                PlayerPrefs.Save();
                currentScore = PlayerPrefs.GetInt(PREF_SAVED_SCORE, 0);
                remainingLives = PlayerPrefs.GetInt(PREF_SAVED_LIVES, startingLives);
            }
            else
            {
                currentScore = 0;
                remainingLives = startingLives;
            }

            SetState(GameState.ReadyToLaunch);
            OnScoreChanged?.Invoke(currentScore, 0);
            OnLivesChanged?.Invoke(remainingLives);
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                levelElapsedTime += Time.deltaTime;
                totalRunElapsedTime += Time.deltaTime;
                OnLevelTimerTick?.Invoke(levelElapsedTime);

                if (isShieldActive)
                {
                    TickShield(Time.deltaTime);
                }
                if (isPaddleExpanded)
                {
                    TickPaddleExpander(Time.deltaTime);
                }
                if (activeScoreMultiplier > 1)
                {
                    TickScoreMultiplier(Time.deltaTime);
                }
                if (isLaserActive)
                {
                    TickLaserPowerup(Time.deltaTime);
                }
                if (isClutchModeActive)
                {
                    TickClutchMode(Time.deltaTime);
                }
            }
        }

        public void ActivateShield(float duration = 10f)
        {
            isShieldActive = true;
            shieldTimeRemaining = duration;
            OnShieldStateChanged?.Invoke(true, duration);
            OnShieldTick?.Invoke(duration);
        }

        public void DeactivateShield()
        {
            if (!isShieldActive && shieldTimeRemaining <= 0f) return;

            isShieldActive = false;
            shieldTimeRemaining = 0f;
            OnShieldStateChanged?.Invoke(false, 0f);
        }

        public void TickShield(float delta)
        {
            if (!isShieldActive) return;

            shieldTimeRemaining -= delta;
            OnShieldTick?.Invoke(Mathf.Max(0f, shieldTimeRemaining));

            if (shieldTimeRemaining <= 0f)
            {
                DeactivateShield();
            }
        }

        public void ActivatePaddleExpander(float duration = 10f)
        {
            isPaddleExpanded = true;
            paddleExpandTimeRemaining = duration;

            var paddle = FindAnyObjectByType<PaddleController>();
            if (paddle != null)
            {
                paddle.ExpandWidth(BlockModifierExtensions.PADDLE_EXPANSION_PERCENT);
            }

            OnPaddleExpandStateChanged?.Invoke(true, duration);
            OnPaddleExpandTick?.Invoke(duration);
        }

        public void DeactivatePaddleExpander()
        {
            if (!isPaddleExpanded && paddleExpandTimeRemaining <= 0f) return;

            isPaddleExpanded = false;
            paddleExpandTimeRemaining = 0f;

            var paddle = FindAnyObjectByType<PaddleController>();
            if (paddle != null)
            {
                paddle.ResetToBaseWidth();
            }

            OnPaddleExpandStateChanged?.Invoke(false, 0f);
        }

        public void TickPaddleExpander(float delta)
        {
            if (!isPaddleExpanded) return;

            paddleExpandTimeRemaining -= delta;
            OnPaddleExpandTick?.Invoke(Mathf.Max(0f, paddleExpandTimeRemaining));

            if (paddleExpandTimeRemaining <= 0f)
            {
                DeactivatePaddleExpander();
            }
        }

        public void ActivateScoreMultiplier(int multiplier, float duration = 10f)
        {
            if (multiplier <= 1) return;

            if (multiplier > activeScoreMultiplier)
            {
                activeScoreMultiplier = multiplier;
                multiplierTimeRemaining = duration;
            }
            else if (multiplier == activeScoreMultiplier)
            {
                multiplierTimeRemaining = duration;
            }
            else
            {
                // Equal or lower multiplier hit while higher tier is active: refresh duration without downgrading
                multiplierTimeRemaining = Mathf.Max(multiplierTimeRemaining, duration);
            }

            OnScoreMultiplierStateChanged?.Invoke(true, activeScoreMultiplier, multiplierTimeRemaining);
            OnScoreMultiplierTick?.Invoke(multiplierTimeRemaining);
        }

        public void DeactivateScoreMultiplier()
        {
            if (activeScoreMultiplier <= 1 && multiplierTimeRemaining <= 0f) return;

            activeScoreMultiplier = 1;
            multiplierTimeRemaining = 0f;
            OnScoreMultiplierStateChanged?.Invoke(false, 1, 0f);
        }

        public void TickScoreMultiplier(float delta)
        {
            if (activeScoreMultiplier <= 1) return;

            multiplierTimeRemaining -= delta;
            OnScoreMultiplierTick?.Invoke(Mathf.Max(0f, multiplierTimeRemaining));

            if (multiplierTimeRemaining <= 0f)
            {
                DeactivateScoreMultiplier();
            }
        }

        public void ActivateLaserPowerup(float duration = 10f)
        {
            isLaserActive = true;
            laserTimeRemaining = Mathf.Max(laserTimeRemaining, duration);

            var paddle = FindAnyObjectByType<PaddleController>();
            if (paddle != null && paddle.LaserController != null)
            {
                paddle.LaserController.ActivateLaserBlaster(duration);
            }

            OnLaserPowerupStateChanged?.Invoke(true, laserTimeRemaining);
            OnLaserPowerupTick?.Invoke(laserTimeRemaining);
        }

        public void DeactivateLaserPowerup()
        {
            isLaserActive = false;
            laserTimeRemaining = 0f;

            var paddle = FindAnyObjectByType<PaddleController>();
            if (paddle != null && paddle.LaserController != null)
            {
                paddle.LaserController.DeactivateAllWeapons();
            }

            OnLaserPowerupStateChanged?.Invoke(false, 0f);
        }

        public void TickLaserPowerup(float delta)
        {
            if (!isLaserActive) return;

            laserTimeRemaining -= delta;
            OnLaserPowerupTick?.Invoke(Mathf.Max(0f, laserTimeRemaining));

            if (laserTimeRemaining <= 0f)
            {
                DeactivateLaserPowerup();
            }
        }

        public void StartClutchMode(float duration = 12f)
        {
            if (isClutchModeActive) return;
            isClutchModeActive = true;
            clutchTimeRemaining = duration > 0f ? duration : clutchDuration;
            clutchMultiplier = CalculateClutchMultiplier(clutchTimeRemaining);
            OnClutchStateChanged?.Invoke(true, clutchTimeRemaining, clutchMultiplier);
            OnClutchTick?.Invoke(clutchTimeRemaining, clutchMultiplier);
        }

        public void EndClutchMode()
        {
            isClutchModeActive = false;
            clutchTimeRemaining = 0f;
            clutchMultiplier = 1;

            var paddle = FindAnyObjectByType<PaddleController>();
            if (paddle != null && paddle.LaserController != null)
            {
                paddle.LaserController.DeactivateHyperBeam();
            }

            OnClutchStateChanged?.Invoke(false, 0f, 1);
        }

        public static int CalculateClutchMultiplier(float timeRemaining)
        {
            if (timeRemaining <= 0f) return 1;
            int mult = Mathf.CeilToInt(timeRemaining);
            return Mathf.Clamp(mult, 1, 10);
        }

        public void TickClutchMode(float delta)
        {
            if (!isClutchModeActive) return;

            clutchTimeRemaining -= delta;
            int newMult = CalculateClutchMultiplier(clutchTimeRemaining);
            if (newMult != clutchMultiplier)
            {
                clutchMultiplier = newMult;
            }

            OnClutchTick?.Invoke(Mathf.Max(0f, clutchTimeRemaining), clutchMultiplier);

            if (clutchTimeRemaining <= 0f)
            {
                TriggerRailgunDischarge();
            }
        }

        private void TriggerRailgunDischarge()
        {
            EndClutchMode();
            var paddle = FindAnyObjectByType<PaddleController>();
            if (paddle != null && paddle.LaserController != null)
            {
                paddle.LaserController.FireRailgunHyperBeam(5.0f);
            }
        }

        public void RegisterBall(BallController ball)
        {
            if (ball == null) return;
            if (!activeBalls.Contains(ball))
            {
                activeBalls.Add(ball);
                OnActiveBallCountChanged?.Invoke(activeBalls.Count);
            }
            OnStateChanged -= ball.HandleStateChangedDirect;
            OnStateChanged += ball.HandleStateChangedDirect;
        }

        public void UnregisterBall(BallController ball)
        {
            if (ball == null) return;
            OnStateChanged -= ball.HandleStateChangedDirect;
            if (activeBalls.Remove(ball))
            {
                OnActiveBallCountChanged?.Invoke(activeBalls.Count);
            }
        }

        public void ClearExtraBalls()
        {
            for (int i = activeBalls.Count - 1; i >= 0; i--)
            {
                var ball = activeBalls[i];
                if (ball != null && !ball.IsPrimaryBall)
                {
                    activeBalls.RemoveAt(i);
                    if (Application.isPlaying)
                        Destroy(ball.gameObject);
                    else
                        DestroyImmediate(ball.gameObject);
                }
            }
            OnActiveBallCountChanged?.Invoke(activeBalls.Count);
        }

        private static readonly Color[] MultiBallColorPalette = new Color[]
        {
            new Color(1f, 0.165f, 0.427f, 1f),     // Neon Magenta (#ff2a6d)
            new Color(1f, 0.843f, 0f, 1f),         // Solar Gold (#ffd700)
            new Color(0f, 0.96f, 0.608f, 1f),      // Neon Emerald (#00f59b)
            new Color(0.608f, 0.365f, 0.898f, 1f)  // Electric Purple (#9b5de5)
        };

        public void ActivateMultiBall()
        {
            BallController primary = activeBalls.Count > 0 ? activeBalls[0] : FindAnyObjectByType<BallController>();
            Vector3 origin = primary != null ? primary.transform.position : Vector3.zero;
            Vector3 baseVel = Vector3.up * 14f;
            float speed = 14f;

            if (primary != null)
            {
                var rb = primary.GetComponent<Rigidbody>();
                if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
                {
                    baseVel = rb.linearVelocity;
                }
                speed = primary.CurrentSpeed;
            }

            SpawnMultiBall(origin, baseVel, speed);
        }

        public void SpawnMultiBall(Vector3 originPosition, Vector3 baseVelocity, float speed)
        {
            BallController primary = activeBalls.Count > 0 ? activeBalls[0] : FindAnyObjectByType<BallController>();
            if (primary == null) return;

            if (baseVelocity.sqrMagnitude < 0.1f)
            {
                baseVelocity = Vector3.up * speed;
            }

            float currentSpeed = speed > 0f ? speed : primary.CurrentSpeed;

            // Angle 1: +35 degrees
            Quaternion rotPos = Quaternion.AngleAxis(35f, Vector3.forward);
            Vector3 dir1 = rotPos * baseVelocity.normalized;

            // Angle 2: -35 degrees
            Quaternion rotNeg = Quaternion.AngleAxis(-35f, Vector3.forward);
            Vector3 dir2 = rotNeg * baseVelocity.normalized;

            CreateExtraBall(primary, originPosition, dir1, currentSpeed, MultiBallColorPalette[0]);
            CreateExtraBall(primary, originPosition, dir2, currentSpeed, MultiBallColorPalette[1]);

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayMultiBall();
            }
        }

        private void CreateExtraBall(BallController template, Vector3 position, Vector3 direction, float speed, Color trailColor)
        {
            GameObject ballObj = Instantiate(template.gameObject, position, Quaternion.identity);
            ballObj.name = "Ball_Extra";
            BallController extraBall = ballObj.GetComponent<BallController>();
            if (extraBall != null)
            {
                extraBall.IsPrimaryBall = false;
                RegisterBall(extraBall);
                extraBall.SetTrailColor(trailColor);
                extraBall.LaunchWithDirection(direction, speed);
            }
        }

        public void HandleBallFell(BallController ball)
        {
            if (currentState != GameState.Playing || isLevelClearPending) return;

            if (activeBalls.Count > 1)
            {
                // Multi-ball: one of multiple balls fell. No life lost!
                UnregisterBall(ball);
                if (ball != null)
                {
                    if (!ball.IsPrimaryBall)
                    {
                        if (Application.isPlaying) Destroy(ball.gameObject);
                        else DestroyImmediate(ball.gameObject);
                    }
                    else
                    {
                        // Promote another ball to primary
                        if (activeBalls.Count > 0 && activeBalls[0] != null)
                        {
                            activeBalls[0].IsPrimaryBall = true;
                        }
                        if (Application.isPlaying) Destroy(ball.gameObject);
                        else DestroyImmediate(ball.gameObject);
                    }
                }
                return;
            }

            // Last remaining ball fell
            if (isShieldActive)
            {
                // Shield saves the ball! Ball resets to paddle in ReadyToLaunch without losing life.
                BlockBreaker.PowerupCapsule.ClearAllFallingCapsules();
                if (ball != null)
                {
                    ball.ResetBallToPaddle();
                }
                SetState(GameState.ReadyToLaunch);

                if (ArcadeAudioManager.Instance != null)
                {
                    ArcadeAudioManager.Instance.PlayShieldDeflect();
                }

                SaveCurrentGameSession();
                return;
            }

            // Normal ball lost
            RecordBallLost();
        }

        public void RegisterLevelBlocks(int blockCount, int levelNumber = 1)
        {
            totalBlocksInLevel = blockCount;
            remainingBlocks = blockCount;
            currentLevel = levelNumber;
            ResetLevelSessionStats();
        }

        public void ResetLevelSessionStats()
        {
            if (levelClearCoroutine != null)
            {
                StopCoroutine(levelClearCoroutine);
                levelClearCoroutine = null;
            }
            isLevelClearPending = false;
            levelElapsedTime = 0f;
            currentVolleyStreak = 0;
            highestVolleyComboThisLevel = 1;
            blocksDestroyedThisLevel = 0;
            baseBlockPointsThisLevel = 0;
            livesLostThisLevel = 0;
            OnVolleyComboChanged?.Invoke(0, 1);
            OnLevelTimerTick?.Invoke(0f);
        }

        public void NotifyVolleyHit(BallController ball, int streak)
        {
            currentVolleyStreak = streak;
            int mult = BallController.GetVolleyMultiplier(streak);
            if (mult > highestVolleyComboThisLevel)
            {
                highestVolleyComboThisLevel = mult;
            }
            OnVolleyComboChanged?.Invoke(streak, mult);
        }

        public void NotifyVolleySaved(BallController ball, int streak)
        {
            currentVolleyStreak = 0;
            OnVolleyComboChanged?.Invoke(0, 1);
        }

        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            if (newState != GameState.Playing && newState != GameState.LevelClear)
            {
                if (levelClearCoroutine != null)
                {
                    StopCoroutine(levelClearCoroutine);
                    levelClearCoroutine = null;
                }
                isLevelClearPending = false;
            }

            currentState = newState;
            OnStateChanged?.Invoke(currentState);
        }

        public void LaunchBall()
        {
            if (currentState != GameState.ReadyToLaunch && currentState != GameState.BallLost) return;

            // Guarantee all weapons and in-flight projectiles are cleared before launch to prevent premature block destruction
            var paddle = FindAnyObjectByType<PaddleController>();
            if (paddle != null && paddle.LaserController != null)
            {
                paddle.LaserController.DeactivateAllWeapons();
            }
            BlockBreaker.LaserBolt.ClearAllActiveBolts();

            SetState(GameState.Playing);

            // Directly guarantee all registered balls launch even if event subscription had timing race
            for (int i = 0; i < activeBalls.Count; i++)
            {
                if (activeBalls[i] != null && !activeBalls[i].IsLaunched)
                {
                    activeBalls[i].Launch();
                }
            }
        }

        public void RecordBlockDestroyed(int points, int colorTier)
        {
            RecordBlockDestroyed(points, colorTier, Vector3.zero, 1, 0, 1, "");
        }

        public void RecordBlockDestroyed(
            int points,
            int colorTier,
            Vector3 worldPos,
            int volleyMultiplier = 1,
            int volleyStreak = 0,
            int chainMultiplier = 1,
            string bonusTag = "")
        {
            if (currentState == GameState.GameOver || currentState == GameState.LevelClear) return;

            int effectiveMultiplier = activeScoreMultiplier;
            if (isClutchModeActive)
            {
                effectiveMultiplier = Mathf.Max(effectiveMultiplier, clutchMultiplier);
            }

            int multiBallMult = Mathf.Max(1, activeBalls.Count);
            int totalMult = effectiveMultiplier * Mathf.Max(1, volleyMultiplier) * Mathf.Max(1, chainMultiplier) * multiBallMult;
            int awardedPoints = points * totalMult;

            currentScore += awardedPoints;
            remainingBlocks = Mathf.Max(0, remainingBlocks - 1);

            blocksDestroyedThisLevel++;
            baseBlockPointsThisLevel += points;
            if (volleyMultiplier > highestVolleyComboThisLevel)
            {
                highestVolleyComboThisLevel = volleyMultiplier;
            }

            if (currentScore > highScore)
            {
                highScore = currentScore;
                HighScoreManager.RecordScore(currentScore, currentLevel, totalRunElapsedTime);
            }

            string displayTag = bonusTag;
            if (string.IsNullOrEmpty(displayTag))
            {
                if (isClutchModeActive) displayTag = $"CLUTCH {clutchMultiplier}X!";
                else if (chainMultiplier > 1) displayTag = $"CHAIN x{chainMultiplier}!";
                else if (volleyMultiplier > 1) displayTag = $"COMBO x{volleyMultiplier}!";
                else if (multiBallMult > 1) displayTag = $"x{multiBallMult} MULTI-BALL!";
            }

            OnBlockPointsAwarded?.Invoke(worldPos, awardedPoints, totalMult, displayTag);
            OnScoreChanged?.Invoke(currentScore, awardedPoints);
            SaveCurrentGameSession();

            // Trigger 12-second Clutch Railgun Overcharge if exactly 1 block remains
            if (remainingBlocks == 1 && !isClutchModeActive && currentState == GameState.Playing)
            {
                StartClutchMode();
            }

            CheckLevelCompletion();
        }

        public void CheckLevelCompletion()
        {
            if (currentState == GameState.GameOver || currentState == GameState.LevelClear) return;
            if (totalBlocksInLevel <= 0) return;

            bool allBlocksCleared = remainingBlocks <= 0;
            if (!allBlocksCleared)
            {
                // Fallback check: verify physical blocks container so player is never stuck if count was desynced
                var generator = FindAnyObjectByType<BlockBreaker.LevelGenerator>();
                Transform container = generator != null ? (generator.BlocksContainer != null ? generator.BlocksContainer : generator.transform.Find("BlocksContainer")) : null;
                if (container != null)
                {
                    int liveBlocks = 0;
                    foreach (Transform child in container)
                    {
                        var block = child.GetComponent<BlockBreaker.Block>();
                        if (block != null && !block.IsDestroyed)
                        {
                            liveBlocks++;
                        }
                    }
                    if (liveBlocks == 0)
                    {
                        remainingBlocks = 0;
                        allBlocksCleared = true;
                    }
                }
            }

            if (allBlocksCleared)
            {
                HighScoreManager.RecordScore(currentScore, currentLevel, totalRunElapsedTime);
                bool wasClearedWithLaser = false;
                var paddle = FindAnyObjectByType<PaddleController>();
                if (paddle != null && paddle.LaserController != null && paddle.LaserController.IsHyperBeamActive)
                {
                    wasClearedWithLaser = true;
                }

                float delay = wasClearedWithLaser ? levelClearDelaySeconds : standardClearDelaySeconds;

                if (Application.isPlaying && gameObject.activeInHierarchy)
                {
                    if (!isLevelClearPending)
                    {
                        isLevelClearPending = true;
                        OnLevelClearPending?.Invoke(delay, wasClearedWithLaser);
                        levelClearCoroutine = StartCoroutine(DelayedLevelClearRoutine(delay));
                    }
                }
                else
                {
                    OnLevelClearPending?.Invoke(delay, wasClearedWithLaser);
                    OnLevelCleared();
                }
            }
        }

        public void RecordBallLost()
        {
            if (currentState != GameState.Playing) return;

            BlockBreaker.PowerupCapsule.ClearAllFallingCapsules();
            BlockBreaker.LaserBolt.ClearAllActiveBolts();

            DeactivateShield();
            DeactivatePaddleExpander();
            DeactivateScoreMultiplier();
            DeactivateLaserPowerup();
            EndClutchMode();

            remainingLives--;
            livesLostThisLevel++;
            currentVolleyStreak = 0;
            OnVolleyComboChanged?.Invoke(0, 1);
            OnLivesChanged?.Invoke(remainingLives);

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayLifeLost();
            }

            if (remainingLives <= 0)
            {
                OnGameOver();
            }
            else
            {
                SetState(GameState.BallLost);
                SaveCurrentGameSession();
            }
        }

        public void AddLife(int amount = 1)
        {
            if (currentState == GameState.GameOver) return;

            remainingLives = Mathf.Min(MAX_LIVES, remainingLives + amount);
            OnLivesChanged?.Invoke(remainingLives);
            SaveCurrentGameSession();
        }

        private System.Collections.IEnumerator DelayedLevelClearRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            isLevelClearPending = false;
            levelClearCoroutine = null;
            OnLevelCleared();
        }

        public void TriggerImmediateLevelClearForTesting()
        {
            if (levelClearCoroutine != null)
            {
                StopCoroutine(levelClearCoroutine);
                levelClearCoroutine = null;
            }
            isLevelClearPending = false;
            OnLevelCleared();
        }

        public void SetLevelClearDelaySecondsForTesting(float delay) => levelClearDelaySeconds = delay;
        public void SetStandardClearDelaySecondsForTesting(float delay) => standardClearDelaySeconds = delay;
        public void TriggerLevelClearWithDelayForTesting(bool wasClearedWithLaser)
        {
            float delay = wasClearedWithLaser ? levelClearDelaySeconds : standardClearDelaySeconds;
            isLevelClearPending = true;
            OnLevelClearPending?.Invoke(delay, wasClearedWithLaser);
        }

        private void OnLevelCleared()
        {
            if (levelClearCoroutine != null)
            {
                StopCoroutine(levelClearCoroutine);
                levelClearCoroutine = null;
            }
            isLevelClearPending = false;

            BlockBreaker.PowerupCapsule.ClearAllFallingCapsules();
            BlockBreaker.LaserBolt.ClearAllActiveBolts();
            ClearExtraBalls();
            DeactivateShield();
            DeactivatePaddleExpander();
            DeactivateScoreMultiplier();
            DeactivateLaserPowerup();
            EndClutchMode();
            SetState(GameState.LevelClear);

            // Fetch current level configuration
            var generator = FindAnyObjectByType<BlockBreaker.LevelGenerator>();
            var config = generator != null ? (generator.CurrentConfig ?? generator.GetLevelConfig(currentLevel)) : null;

            float parTime = config != null ? config.ParTime : 40f;
            int timeBonusPool = config != null ? config.TimeBonusMax : 3000;
            int timeBonus = Mathf.Max(0, timeBonusPool - Mathf.FloorToInt(levelElapsedTime * 35f));
            bool isUnderPar = levelElapsedTime <= parTime;
            int speedBonus = isUnderPar ? 500 : 0;
            bool isFlawless = livesLostThisLevel == 0;
            int flawlessBonus = isFlawless ? 1000 : 0;

            int totalBonuses = timeBonus + speedBonus + flawlessBonus;
            currentScore += totalBonuses;

            if (currentScore > highScore)
            {
                highScore = currentScore;
            }

            int[] thresholds = config != null ? config.StarThresholds : new int[] { 800, 1500, 2500 };
            int stars = 1; // 1 star for clearing the level
            if (currentScore >= thresholds[1]) stars = 2;
            if (currentScore >= thresholds[2]) stars = 3;

            bool isNewBestTime = HighScoreManager.RecordLevelTime(currentLevel, levelElapsedTime);
            HighScoreManager.SetLevelStars(currentLevel, stars);
            HighScoreManager.RecordScore(currentScore, currentLevel, totalRunElapsedTime);

            currentLevelSummary = new LevelSummaryData
            {
                levelNumber = currentLevel,
                levelName = config != null ? config.LevelName : $"Level {currentLevel}",
                blocksDestroyed = blocksDestroyedThisLevel,
                baseBlockPoints = baseBlockPointsThisLevel,
                highestCombo = highestVolleyComboThisLevel,
                elapsedTime = levelElapsedTime,
                parTime = parTime,
                timeBonus = timeBonus,
                isUnderPar = isUnderPar,
                speedBonus = speedBonus,
                isFlawless = isFlawless,
                flawlessBonus = flawlessBonus,
                totalLevelScore = (baseBlockPointsThisLevel * highestVolleyComboThisLevel) + totalBonuses,
                cumulativeScore = currentScore,
                starsEarned = stars,
                isNewBestTime = isNewBestTime
            };

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayLevelClear();
            }

            OnLevelCompletedWithTally?.Invoke(currentLevelSummary);
            SaveCurrentGameSession();
        }

        public void AdvanceToNextLevel()
        {
            BlockBreaker.PowerupCapsule.ClearAllFallingCapsules();
            BlockBreaker.LaserBolt.ClearAllActiveBolts();
            ClearExtraBalls();
            DeactivateShield();
            DeactivatePaddleExpander();
            DeactivateScoreMultiplier();
            DeactivateLaserPowerup();
            EndClutchMode();
            Time.timeScale = 1f;

            var generator = FindAnyObjectByType<BlockBreaker.LevelGenerator>();
            if (generator != null)
            {
                generator.AdvanceToNextLevel();
                currentLevel = generator.CurrentConfig != null ? generator.CurrentConfig.LevelNumber : currentLevel + 1;
            }
            else
            {
                currentLevel++;
            }

            ResetLevelSessionStats();
            SetState(GameState.ReadyToLaunch);
            SaveCurrentGameSession();
        }

        public void ReplayCurrentLevel()
        {
            BlockBreaker.PowerupCapsule.ClearAllFallingCapsules();
            BlockBreaker.LaserBolt.ClearAllActiveBolts();
            ClearExtraBalls();
            DeactivateShield();
            DeactivatePaddleExpander();
            DeactivateScoreMultiplier();
            DeactivateLaserPowerup();
            EndClutchMode();
            Time.timeScale = 1f;

            var generator = FindAnyObjectByType<BlockBreaker.LevelGenerator>();
            if (generator != null)
            {
                generator.SelectAndLoadLevel(currentLevel);
            }

            ResetLevelSessionStats();
            SetState(GameState.ReadyToLaunch);
            SaveCurrentGameSession();
        }

        private void OnGameOver()
        {
            BlockBreaker.PowerupCapsule.ClearAllFallingCapsules();
            BlockBreaker.LaserBolt.ClearAllActiveBolts();
            ClearExtraBalls();
            DeactivateShield();
            DeactivatePaddleExpander();
            DeactivateScoreMultiplier();
            DeactivateLaserPowerup();
            EndClutchMode();
            if (currentScore > 0)
            {
                HighScoreManager.RecordScore(currentScore, currentLevel, totalRunElapsedTime);
            }
            SetState(GameState.GameOver);
            ClearSavedGame();

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayGameOver();
            }
        }

        public void PauseGame()
        {
            if (currentState == GameState.Paused || currentState == GameState.GameOver || currentState == GameState.LevelClear) return;

            previousStateBeforePause = currentState;
            currentState = GameState.Paused;
            Time.timeScale = 0f;
            OnPauseToggled?.Invoke(true);
            OnStateChanged?.Invoke(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (currentState != GameState.Paused) return;

            Time.timeScale = 1f;
            currentState = previousStateBeforePause;
            Arcade.Input.ArcadeInputHandler.Instance?.SuppressLaunch(0.3f);
            OnPauseToggled?.Invoke(false);
            OnStateChanged?.Invoke(currentState);
        }

        public void TogglePause()
        {
            if (currentState == GameState.GameOver || currentState == GameState.LevelClear) return;

            if (currentState == GameState.Paused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            ClearSavedGame();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SaveCurrentGameSession();
            SceneManager.LoadScene("LV_BlockBreaker_MainMenu");
        }

        public void SaveCurrentGameSession()
        {
            if (currentState == GameState.GameOver) return;

            PlayerPrefs.SetInt(PREF_SAVED_SCORE, currentScore);
            PlayerPrefs.SetInt(PREF_SAVED_LIVES, remainingLives);
            PlayerPrefs.SetInt(PREF_HAS_SAVED_GAME, 1);
            PlayerPrefs.Save();
        }

        public static void ClearSavedGame()
        {
            PlayerPrefs.DeleteKey(PREF_SAVED_SCORE);
            PlayerPrefs.DeleteKey(PREF_SAVED_LIVES);
            PlayerPrefs.SetInt(PREF_HAS_SAVED_GAME, 0);
            PlayerPrefs.Save();
        }
    }
}
