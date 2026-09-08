using Arcade.Audio;
using Arcade.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arcade.UI
{
    /// <summary>
    /// Connects UI Toolkit In-Game HUD, quick action buttons, and state modals to ArcadeGameManager.
    /// Uses Unity 6 PanelRenderer component.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class ArcadeUIManager : MonoBehaviour
    {
        private PanelRenderer panelRenderer;
        private VisualElement root;

        // Top bar elements
        private Label scoreLabel;
        private Label highscoreLabel;
        private VisualElement[] lifePips;
        private Button btnQuickMute;
        private Button btnQuickOptions;
        private Button btnQuickPause;

        // Banner
        private VisualElement launchBanner;

        // Modals
        private VisualElement pauseModal;
        private VisualElement optionsModal;
        private VisualElement levelClearModal;
        private VisualElement gameOverModal;

        // Modal Labels & Buttons
        private Label clearScoreLabel;
        private Label overScoreLabel;
        private Label overHighLabel;

        private Button btnResume;
        private Button btnRestartPause;
        private Button btnMenuPause;
        private Button btnNextLevel;
        private Button btnClearMenu;
        private Button btnRetry;
        private Button btnOverMenu;

        // Options controls
        private Slider sliderVolume;
        private Toggle toggleMute;
        private Button btnFps;
        private Button btnCloseOptions;

        private int targetFps = 60;

        private void Awake()
        {
            panelRenderer = GetComponent<PanelRenderer>();
            if (panelRenderer != null)
            {
                panelRenderer.enabled = false;
                panelRenderer.enabled = true;
            }
        }

        private void OnEnable()
        {
            if (panelRenderer == null) panelRenderer = GetComponent<PanelRenderer>();
            if (panelRenderer != null)
            {
                panelRenderer.RegisterUIReloadCallback(OnUIReload);
            }
            SubscribeEvents();
        }

        private void OnDisable()
        {
            if (panelRenderer != null)
            {
                panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            }
            UnsubscribeEvents();
            UnbindElements();
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement newRoot, int version)
        {
            if (newRoot == null) return;
            root = newRoot;
            UnbindElements();
            BindElements();
            InitializeDisplay();
        }

        private void UnbindElements()
        {
            if (btnQuickMute != null) btnQuickMute.clicked -= HandleQuickMuteClicked;
            if (btnQuickOptions != null) btnQuickOptions.clicked -= ShowOptions;
            if (btnQuickPause != null) btnQuickPause.clicked -= HandleQuickPauseClicked;

            if (btnResume != null) btnResume.clicked -= HandleResumeClicked;
            if (btnRestartPause != null) btnRestartPause.clicked -= HandleRestartClicked;
            if (btnMenuPause != null) btnMenuPause.clicked -= HandleMenuClicked;

            if (btnNextLevel != null) btnNextLevel.clicked -= HandleRestartClicked;
            if (btnClearMenu != null) btnClearMenu.clicked -= HandleMenuClicked;

            if (btnRetry != null) btnRetry.clicked -= HandleRestartClicked;
            if (btnOverMenu != null) btnOverMenu.clicked -= HandleMenuClicked;

            if (btnCloseOptions != null) btnCloseOptions.clicked -= HideOptions;
            if (btnFps != null) btnFps.clicked -= ToggleFpsSetting;
        }

        private void BindElements()
        {
            if (root == null) return;

            scoreLabel = root.Q<Label>("score-label");
            highscoreLabel = root.Q<Label>("highscore-label");

            lifePips = new[]
            {
                root.Q<VisualElement>("life-pip-1"),
                root.Q<VisualElement>("life-pip-2"),
                root.Q<VisualElement>("life-pip-3")
            };

            btnQuickMute = root.Q<Button>("btn-quick-mute");
            btnQuickOptions = root.Q<Button>("btn-quick-options");
            btnQuickPause = root.Q<Button>("btn-quick-pause");

            launchBanner = root.Q<VisualElement>("launch-banner");

            pauseModal = root.Q<VisualElement>("pause-modal");
            optionsModal = root.Q<VisualElement>("options-modal");
            levelClearModal = root.Q<VisualElement>("level-clear-modal");
            gameOverModal = root.Q<VisualElement>("game-over-modal");

            clearScoreLabel = root.Q<Label>("clear-score-label");
            overScoreLabel = root.Q<Label>("over-score-label");
            overHighLabel = root.Q<Label>("over-high-label");

            btnResume = root.Q<Button>("btn-resume");
            btnRestartPause = root.Q<Button>("btn-restart-pause");
            btnMenuPause = root.Q<Button>("btn-menu-pause");
            btnNextLevel = root.Q<Button>("btn-next-level");
            btnClearMenu = root.Q<Button>("btn-clear-menu");
            btnRetry = root.Q<Button>("btn-retry");
            btnOverMenu = root.Q<Button>("btn-over-menu");

            sliderVolume = root.Q<Slider>("slider-volume");
            toggleMute = root.Q<Toggle>("toggle-mute");
            btnFps = root.Q<Button>("btn-fps");
            btnCloseOptions = root.Q<Button>("btn-close-options");

            // Wire quick buttons
            if (btnQuickMute != null) btnQuickMute.clicked += HandleQuickMuteClicked;
            if (btnQuickOptions != null) btnQuickOptions.clicked += ShowOptions;
            if (btnQuickPause != null) btnQuickPause.clicked += HandleQuickPauseClicked;

            // Wire modal buttons
            if (btnResume != null) btnResume.clicked += HandleResumeClicked;
            if (btnRestartPause != null) btnRestartPause.clicked += HandleRestartClicked;
            if (btnMenuPause != null) btnMenuPause.clicked += HandleMenuClicked;

            if (btnNextLevel != null) btnNextLevel.clicked += HandleRestartClicked;
            if (btnClearMenu != null) btnClearMenu.clicked += HandleMenuClicked;

            if (btnRetry != null) btnRetry.clicked += HandleRestartClicked;
            if (btnOverMenu != null) btnOverMenu.clicked += HandleMenuClicked;

            if (btnCloseOptions != null) btnCloseOptions.clicked += HideOptions;

            if (sliderVolume != null)
            {
                sliderVolume.RegisterValueChangedCallback(evt =>
                {
                    if (ArcadeAudioManager.Instance != null)
                        ArcadeAudioManager.Instance.SetVolume(evt.newValue);
                });
            }

            if (toggleMute != null)
            {
                toggleMute.RegisterValueChangedCallback(evt =>
                {
                    if (ArcadeAudioManager.Instance != null)
                        ArcadeAudioManager.Instance.IsMuted = evt.newValue;
                    UpdateMuteButtonIcon();
                });
            }

            if (btnFps != null) btnFps.clicked += ToggleFpsSetting;
        }

        private void SubscribeEvents()
        {
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.OnScoreChanged += UpdateScoreDisplay;
                ArcadeGameManager.Instance.OnLivesChanged += UpdateLivesDisplay;
                ArcadeGameManager.Instance.OnStateChanged += HandleGameStateChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.OnScoreChanged -= UpdateScoreDisplay;
                ArcadeGameManager.Instance.OnLivesChanged -= UpdateLivesDisplay;
                ArcadeGameManager.Instance.OnStateChanged -= HandleGameStateChanged;
            }
        }

        private void InitializeDisplay()
        {
            if (ArcadeGameManager.Instance != null)
            {
                UpdateScoreDisplay(ArcadeGameManager.Instance.Score, 0);
                UpdateLivesDisplay(ArcadeGameManager.Instance.Lives);
                HandleGameStateChanged(ArcadeGameManager.Instance.State);
            }

            if (ArcadeAudioManager.Instance != null)
            {
                if (sliderVolume != null) sliderVolume.value = ArcadeAudioManager.Instance.Volume;
                if (toggleMute != null) toggleMute.value = ArcadeAudioManager.Instance.IsMuted;
                UpdateMuteButtonIcon();
            }

            targetFps = PlayerPrefs.GetInt("Arcade_TargetFPS", 60);
            Application.targetFrameRate = targetFps;
            if (btnFps != null) btnFps.text = $"{targetFps} FPS";
        }

        public void UpdateScoreDisplay(int currentScore, int delta)
        {
            if (scoreLabel != null) scoreLabel.text = $"SCORE: {currentScore}";
            if (highscoreLabel != null && ArcadeGameManager.Instance != null)
                highscoreLabel.text = $"BEST: {ArcadeGameManager.Instance.HighScore}";
        }

        public void UpdateLivesDisplay(int lives)
        {
            if (lifePips == null) return;
            for (int i = 0; i < lifePips.Length; i++)
            {
                if (lifePips[i] == null) continue;
                if (i < lives)
                {
                    lifePips[i].AddToClassList("pip-active");
                    lifePips[i].RemoveFromClassList("pip-lost");
                }
                else
                {
                    lifePips[i].RemoveFromClassList("pip-active");
                    lifePips[i].AddToClassList("pip-lost");
                }
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            // Banner visibility
            if (launchBanner != null)
            {
                if (state == GameState.ReadyToLaunch || state == GameState.BallLost)
                    launchBanner.RemoveFromClassList("launch-banner-hidden");
                else
                    launchBanner.AddToClassList("launch-banner-hidden");
            }

            // Pause Modal
            if (pauseModal != null)
            {
                if (state == GameState.Paused) pauseModal.RemoveFromClassList("modal-hidden");
                else pauseModal.AddToClassList("modal-hidden");
            }

            // Level Clear Modal
            if (levelClearModal != null)
            {
                if (state == GameState.LevelClear)
                {
                    if (clearScoreLabel != null && ArcadeGameManager.Instance != null)
                        clearScoreLabel.text = $"FINAL SCORE: {ArcadeGameManager.Instance.Score}";
                    levelClearModal.RemoveFromClassList("modal-hidden");
                }
                else
                {
                    levelClearModal.AddToClassList("modal-hidden");
                }
            }

            // Game Over Modal
            if (gameOverModal != null)
            {
                if (state == GameState.GameOver)
                {
                    if (overScoreLabel != null && ArcadeGameManager.Instance != null)
                        overScoreLabel.text = $"FINAL SCORE: {ArcadeGameManager.Instance.Score}";
                    if (overHighLabel != null && ArcadeGameManager.Instance != null)
                        overHighLabel.text = $"HIGH SCORE: {ArcadeGameManager.Instance.HighScore}";
                    gameOverModal.RemoveFromClassList("modal-hidden");
                }
                else
                {
                    gameOverModal.AddToClassList("modal-hidden");
                }
            }
        }

        private void HandleQuickMuteClicked()
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.ToggleMute();
                if (toggleMute != null) toggleMute.value = ArcadeAudioManager.Instance.IsMuted;
                UpdateMuteButtonIcon();
            }
        }

        private void UpdateMuteButtonIcon()
        {
            if (btnQuickMute == null || ArcadeAudioManager.Instance == null) return;
            btnQuickMute.text = ArcadeAudioManager.Instance.IsMuted ? "🔇" : "🔊";
        }

        private void HandleQuickPauseClicked()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayPaddleBounce();
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.TogglePause();
            }
        }

        private void HandleResumeClicked()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayPaddleBounce();
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.TogglePause();
            }
        }

        private void HandleRestartClicked()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayPaddleBounce();
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.RestartGame();
            }
        }

        private void HandleMenuClicked()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayPaddleBounce();
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.LoadMainMenu();
            }
        }

        private void ShowOptions()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayPaddleBounce();
            if (optionsModal != null) optionsModal.RemoveFromClassList("modal-hidden");
        }

        private void HideOptions()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayPaddleBounce();
            if (optionsModal != null) optionsModal.AddToClassList("modal-hidden");
        }

        private void ToggleFpsSetting()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayPaddleBounce();
            targetFps = targetFps == 60 ? 120 : 60;
            Application.targetFrameRate = targetFps;
            PlayerPrefs.SetInt("Arcade_TargetFPS", targetFps);
            PlayerPrefs.Save();
            if (btnFps != null) btnFps.text = $"{targetFps} FPS";
        }
    }
}
