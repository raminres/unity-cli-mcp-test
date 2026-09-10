using System;
using Arcade.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arcade.Core
{
    /// <summary>
    /// Central game coordinator managing state machine, scoring, lives, and high-level gameplay events.
    /// </summary>
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

        private GameState previousStateBeforePause;

        // Events for UI and gameplay decoupled subscribers
        public event Action<int, int> OnScoreChanged;
        public event Action<int> OnLivesChanged;
        public event Action<GameState> OnStateChanged;
        public event Action<bool> OnPauseToggled;

        public GameState State => currentState;
        public int Score => currentScore;
        public int HighScore => highScore;
        public int Lives => remainingLives;
        public int RemainingBlocks => remainingBlocks;
        public int TotalBlocks => totalBlocksInLevel;

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
            highScore = PlayerPrefs.GetInt(PREF_HIGH_SCORE, 0);
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
        }

        public void RecordBlockDestroyed(int points, int colorTier)
        {
            if (currentState != GameState.Playing) return;

            currentScore += points;
            remainingBlocks = Mathf.Max(0, remainingBlocks - 1);

            if (currentScore > highScore)
            {
                highScore = currentScore;
                PlayerPrefs.SetInt(PREF_HIGH_SCORE, highScore);
                PlayerPrefs.Save();
            }

            OnScoreChanged?.Invoke(currentScore, points);
            SaveCurrentGameSession();

            if (remainingBlocks <= 0)
            {
                OnLevelCleared();
            }
        }

        public void RecordBallLost()
        {
            if (currentState != GameState.Playing) return;

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
            SetState(GameState.LevelClear);

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayLevelClear();
            }

            SaveCurrentGameSession();
        }

        public void AdvanceToNextLevel()
        {
            Time.timeScale = 1f;
            SetState(GameState.ReadyToLaunch);
            SaveCurrentGameSession();
        }

        private void OnGameOver()
        {
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
