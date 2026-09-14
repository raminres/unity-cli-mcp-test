using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcade.Core
{
    [Serializable]
    public class HighScoreEntry
    {
        public int score;
        public int level;
        public string date;
        public float time; // run elapsed time in seconds
        public string sessionId; // unique ID of the play session/run

        public HighScoreEntry() { }

        public HighScoreEntry(int score, int level = 1, string date = "", float time = 0f, string sessionId = "")
        {
            this.score = score;
            this.level = level;
            this.date = string.IsNullOrEmpty(date) ? DateTime.Now.ToString("yyyy-MM-dd") : date;
            this.time = time;
            this.sessionId = sessionId ?? "";
        }
    }

    [Serializable]
    public class HighScoreData
    {
        public List<HighScoreEntry> scores = new List<HighScoreEntry>();
    }

    /// <summary>
    /// Persistent high score tracking manager preserving the top 10 player scores,
    /// formatted dates, and support for score resetting.
    /// </summary>
    public static class HighScoreManager
    {
        private const string PREF_TOP_SCORES = "Arcade_TopScores_v1";
        private const string PREF_HIGH_SCORE = "Arcade_HighScore";
        public const int MAX_SCORES = 10;

        public static event Action OnHighScoresChanged;

        private static List<HighScoreEntry> cachedScores;

        public static int HighestScore
        {
            get
            {
                var scores = GetTopScores();
                return scores.Count > 0 ? scores[0].score : 0;
            }
        }

        public static List<HighScoreEntry> GetTopScores()
        {
            if (cachedScores != null) return new List<HighScoreEntry>(cachedScores);

            cachedScores = new List<HighScoreEntry>();
            string json = PlayerPrefs.GetString(PREF_TOP_SCORES, "");

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<HighScoreData>(json);
                    if (data != null && data.scores != null)
                    {
                        cachedScores = data.scores;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[HighScoreManager] Failed to parse high scores: {e.Message}");
                }
            }

            // Fallback / legacy migration from single Arcade_HighScore
            if (cachedScores.Count == 0)
            {
                int legacyHigh = PlayerPrefs.GetInt(PREF_HIGH_SCORE, 0);
                if (legacyHigh > 0)
                {
                    cachedScores.Add(new HighScoreEntry(legacyHigh, 1));
                    SaveScores();
                }
            }

            SortAndClampScores();
            return new List<HighScoreEntry>(cachedScores);
        }

        public static bool RecordScore(int score, int level = 1, float time = 0f, string sessionId = "")
        {
            if (score <= 0) return false;

            // Ensure cachedScores is initialized and sorted
            GetTopScores();

            // If a valid sessionId is provided, check if an entry for this session already exists
            if (!string.IsNullOrEmpty(sessionId))
            {
                var existing = cachedScores.Find(e => e.sessionId == sessionId);
                if (existing != null)
                {
                    existing.score = Mathf.Max(existing.score, score);
                    existing.level = Mathf.Max(existing.level, level);
                    existing.time = time;
                    existing.date = DateTime.Now.ToString("yyyy-MM-dd");

                    SortAndClampScores();
                    SaveScores();

                    OnHighScoresChanged?.Invoke();
                    return true;
                }
            }

            // If we already have 10 scores and this score is <= the 10th score, do not record
            if (cachedScores.Count >= MAX_SCORES && score <= cachedScores[MAX_SCORES - 1].score)
            {
                return false;
            }

            cachedScores.Add(new HighScoreEntry(score, level, "", time, sessionId));
            SortAndClampScores();
            SaveScores();

            OnHighScoresChanged?.Invoke();
            return true;
        }

        public static void ResetScores()
        {
            if (cachedScores == null) cachedScores = new List<HighScoreEntry>();
            cachedScores.Clear();

            SaveScores();
            PlayerPrefs.SetInt(PREF_HIGH_SCORE, 0);
            PlayerPrefs.Save();

            OnHighScoresChanged?.Invoke();
        }

        private static void SortAndClampScores()
        {
            if (cachedScores == null) cachedScores = new List<HighScoreEntry>();

            cachedScores.Sort((a, b) => b.score.CompareTo(a.score));

            if (cachedScores.Count > MAX_SCORES)
            {
                cachedScores.RemoveRange(MAX_SCORES, cachedScores.Count - MAX_SCORES);
            }
        }

        private static void SaveScores()
        {
            var data = new HighScoreData { scores = cachedScores };
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PREF_TOP_SCORES, json);

            int topScore = cachedScores.Count > 0 ? cachedScores[0].score : 0;
            PlayerPrefs.SetInt(PREF_HIGH_SCORE, topScore);
            PlayerPrefs.Save();
        }

        public static void ClearCacheForTesting()
        {
            cachedScores = null;
        }

        #region Level Star Ratings & Best Clear Times

        public const string PREF_LEVEL_STARS_PREFIX = "BlockBreaker_Stars_Level_";
        public const string PREF_LEVEL_TIME_PREFIX = "BlockBreaker_BestTime_Level_";

        public static event Action<int, int> OnLevelStarsChanged; // (levelNumber, stars)
        public static event Action<int, float> OnLevelBestTimeChanged; // (levelNumber, bestTime)

        public static int GetLevelStars(int levelNumber)
        {
            return PlayerPrefs.GetInt(PREF_LEVEL_STARS_PREFIX + levelNumber, 0);
        }

        public static bool SetLevelStars(int levelNumber, int stars)
        {
            if (stars <= 0) return false;
            int currentStars = GetLevelStars(levelNumber);
            if (stars > currentStars)
            {
                PlayerPrefs.SetInt(PREF_LEVEL_STARS_PREFIX + levelNumber, Mathf.Clamp(stars, 1, 3));
                PlayerPrefs.Save();
                OnLevelStarsChanged?.Invoke(levelNumber, stars);
                return true;
            }
            return false;
        }

        public static float GetLevelBestTime(int levelNumber)
        {
            return PlayerPrefs.GetFloat(PREF_LEVEL_TIME_PREFIX + levelNumber, 0f);
        }

        public static bool RecordLevelTime(int levelNumber, float timeSeconds)
        {
            if (timeSeconds <= 0f) return false;
            float currentBest = GetLevelBestTime(levelNumber);
            // If no recorded time yet, or new time is faster
            if (currentBest <= 0f || timeSeconds < currentBest)
            {
                PlayerPrefs.SetFloat(PREF_LEVEL_TIME_PREFIX + levelNumber, timeSeconds);
                PlayerPrefs.Save();
                OnLevelBestTimeChanged?.Invoke(levelNumber, timeSeconds);
                return true;
            }
            return false;
        }

        public static string FormatTime(float timeSeconds)
        {
            if (timeSeconds <= 0f) return "--:--";
            int totalSeconds = Mathf.FloorToInt(timeSeconds);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }

        #endregion
    }
}
