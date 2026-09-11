using System;
using Arcade.Audio;
using Arcade.BlockBreaker;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arcade.Core
{
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
        [SerializeField] private int currentScore = 0;
        [SerializeField] private int highScore = 0;
        [SerializeField] private int remainingLives = 3;
        [SerializeField] private int remainingBlocks = 0;
        [SerializeField] private int totalBlocksInLevel = 0;

        [Header("Power-Up States")]
        [SerializeField] private bool isShieldActive = false;
        [SerializeField] private float shieldTimeRemaining = 0f;
        [SerializeField] private bool isPaddleExpanded = false;
        [SerializeField] private float paddleExpandTimeRemaining = 0f;
        [SerializeField] private int activeScoreMultiplier = 1;
        [SerializeField] private float multiplierTimeRemaining = 0f;

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

        public GameState State => currentState;
        public int Score => currentScore;
        public int HighScore => highScore;
        public int Lives => remainingLives;
        public int RemainingBlocks => remainingBlocks;
        public int TotalBlocks => totalBlocksInLevel;
        public bool IsShieldActive => isShieldActive;
        public float ShieldTimeRemaining => shieldTimeRemaining;
        public bool IsPaddleExpanded => isPaddleExpanded;
        public float PaddleExpandTimeRemaining => paddleExpandTimeRemaining;
        public int ActiveScoreMultiplier => activeScoreMultiplier;
        public float MultiplierTimeRemaining => multiplierTimeRemaining;
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
            if (currentState != GameState.Playing) return;

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

        public void RegisterLevelBlocks(int blockCount)
        {
            totalBlocksInLevel = blockCount;
            remainingBlocks = blockCount;
        }

        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            OnStateChanged?.Invoke(currentState);
        }

        public void LaunchBall()
        {
            if (currentState != GameState.ReadyToLaunch && currentState != GameState.BallLost) return;

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
            if (currentState != GameState.Playing) return;

            int awardedPoints = points * activeScoreMultiplier;
            currentScore += awardedPoints;
            remainingBlocks = Mathf.Max(0, remainingBlocks - 1);

            if (currentScore > highScore)
            {
                highScore = currentScore;
                HighScoreManager.RecordScore(currentScore);
            }

            OnScoreChanged?.Invoke(currentScore, awardedPoints);
            SaveCurrentGameSession();

            if (totalBlocksInLevel > 0 && remainingBlocks <= 0)
            {
                HighScoreManager.RecordScore(currentScore);
                OnLevelCleared();
            }
        }

        public void RecordBallLost()
        {
            if (currentState != GameState.Playing) return;

            DeactivateShield();
            DeactivatePaddleExpander();
            DeactivateScoreMultiplier();

            remainingLives--;
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

        private void OnLevelCleared()
        {
            ClearExtraBalls();
            DeactivateShield();
            DeactivatePaddleExpander();
            DeactivateScoreMultiplier();
            SetState(GameState.LevelClear);

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayLevelClear();
            }

            SaveCurrentGameSession();
        }

        public void AdvanceToNextLevel()
        {
            ClearExtraBalls();
            DeactivateShield();
            DeactivatePaddleExpander();
            DeactivateScoreMultiplier();
            Time.timeScale = 1f;
            SetState(GameState.ReadyToLaunch);
            SaveCurrentGameSession();
        }

        private void OnGameOver()
        {
            ClearExtraBalls();
            DeactivateShield();
            DeactivatePaddleExpander();
            DeactivateScoreMultiplier();
            if (currentScore > 0)
            {
                HighScoreManager.RecordScore(currentScore);
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
