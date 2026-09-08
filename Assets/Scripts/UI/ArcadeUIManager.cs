using Arcade.Audio;
using Arcade.BlockBreaker;
using Arcade.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arcade.UI
{
    /// <summary>
    /// Connects UI Toolkit In-Game HUD, quick action buttons, editable Level Settings modal,
    /// next level advancement, and state modals to ArcadeGameManager and LevelGenerator.
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
        private Button btnQuickLevels;
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
        private VisualElement levelSettingsModal;

        // Modal Labels & Buttons
        private Label clearScoreLabel;
        private Label overScoreLabel;
        private Label overHighLabel;

        private Button btnResume;
        private Button btnLevelSettingsPause;
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

        // Level Settings controls
        private Button btnLvl1;
        private Button btnLvl2;
        private Button btnLvl3;
        private Label levelNameLabel;
        private Label levelDescLabel;

        private SliderInt sliderColumns;
        private Label valColumns;
        private SliderInt sliderRows;
        private Label valRows;
        private Slider sliderSpeed;
        private Label valSpeed;
        private SliderInt sliderMultiplier2x;
        private Label valMultiplier2x;
        private SliderInt sliderMultiplier3x;
        private Label valMultiplier3x;
        private SliderInt sliderExpander;
        private Label valExpander;

        private Button btnApplyLevel;
        private Button btnCloseLevelSettings;

        private LevelConfiguration activeEditableConfig;
        private LevelGenerator levelGenerator;
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

        private void Start()
        {
            if (levelGenerator == null)
            {
                levelGenerator = FindAnyObjectByType<LevelGenerator>();
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
            if (btnQuickLevels != null) btnQuickLevels.clicked -= ShowLevelSettings;
            if (btnQuickMute != null) btnQuickMute.clicked -= HandleQuickMuteClicked;
            if (btnQuickOptions != null) btnQuickOptions.clicked -= ShowOptions;
            if (btnQuickPause != null) btnQuickPause.clicked -= HandleQuickPauseClicked;

            if (btnResume != null) btnResume.clicked -= HandleResumeClicked;
            if (btnLevelSettingsPause != null) btnLevelSettingsPause.clicked -= ShowLevelSettings;
            if (btnRestartPause != null) btnRestartPause.clicked -= HandleRestartClicked;
            if (btnMenuPause != null) btnMenuPause.clicked -= HandleMenuClicked;

            if (btnNextLevel != null) btnNextLevel.clicked -= HandleNextLevelClicked;
            if (btnClearMenu != null) btnClearMenu.clicked -= HandleMenuClicked;

            if (btnRetry != null) btnRetry.clicked -= HandleRestartClicked;
            if (btnOverMenu != null) btnOverMenu.clicked -= HandleMenuClicked;

            if (btnCloseOptions != null) btnCloseOptions.clicked -= HideOptions;
            if (btnFps != null) btnFps.clicked -= ToggleFpsSetting;

            if (btnLvl1 != null) btnLvl1.clicked -= () => SelectLevelTab(1);
            if (btnLvl2 != null) btnLvl2.clicked -= () => SelectLevelTab(2);
            if (btnLvl3 != null) btnLvl3.clicked -= () => SelectLevelTab(3);

            if (btnApplyLevel != null) btnApplyLevel.clicked -= ApplyLevelSettingsAndRestart;
            if (btnCloseLevelSettings != null) btnCloseLevelSettings.clicked -= HideLevelSettings;
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

            btnQuickLevels = root.Q<Button>("btn-quick-levels");
            btnQuickMute = root.Q<Button>("btn-quick-mute");
            btnQuickOptions = root.Q<Button>("btn-quick-options");
            btnQuickPause = root.Q<Button>("btn-quick-pause");

            launchBanner = root.Q<VisualElement>("launch-banner");

            pauseModal = root.Q<VisualElement>("pause-modal");
            optionsModal = root.Q<VisualElement>("options-modal");
            levelClearModal = root.Q<VisualElement>("level-clear-modal");
            gameOverModal = root.Q<VisualElement>("game-over-modal");
            levelSettingsModal = root.Q<VisualElement>("level-settings-modal");

            clearScoreLabel = root.Q<Label>("clear-score-label");
            overScoreLabel = root.Q<Label>("over-score-label");
            overHighLabel = root.Q<Label>("over-high-label");

            btnResume = root.Q<Button>("btn-resume");
            btnLevelSettingsPause = root.Q<Button>("btn-level-settings-pause");
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

            // Level Settings controls
            btnLvl1 = root.Q<Button>("btn-lvl-1");
            btnLvl2 = root.Q<Button>("btn-lvl-2");
            btnLvl3 = root.Q<Button>("btn-lvl-3");
            levelNameLabel = root.Q<Label>("level-name-label");
            levelDescLabel = root.Q<Label>("level-desc-label");

            sliderColumns = root.Q<SliderInt>("slider-columns");
            valColumns = root.Q<Label>("val-columns");
            sliderRows = root.Q<SliderInt>("slider-rows");
            valRows = root.Q<Label>("val-rows");
            sliderSpeed = root.Q<Slider>("slider-speed");
            valSpeed = root.Q<Label>("val-speed");
            sliderMultiplier2x = root.Q<SliderInt>("slider-multiplier2x");
            valMultiplier2x = root.Q<Label>("val-multiplier2x");
            sliderMultiplier3x = root.Q<SliderInt>("slider-multiplier3x");
            valMultiplier3x = root.Q<Label>("val-multiplier3x");
            sliderExpander = root.Q<SliderInt>("slider-expander");
            valExpander = root.Q<Label>("val-expander");

            btnApplyLevel = root.Q<Button>("btn-apply-level");
            btnCloseLevelSettings = root.Q<Button>("btn-close-level-settings");

            // Wire quick buttons
            if (btnQuickLevels != null) btnQuickLevels.clicked += ShowLevelSettings;
            if (btnQuickMute != null) btnQuickMute.clicked += HandleQuickMuteClicked;
            if (btnQuickOptions != null) btnQuickOptions.clicked += ShowOptions;
            if (btnQuickPause != null) btnQuickPause.clicked += HandleQuickPauseClicked;

            // Wire modal buttons
            if (btnResume != null) btnResume.clicked += HandleResumeClicked;
            if (btnLevelSettingsPause != null) btnLevelSettingsPause.clicked += ShowLevelSettings;
            if (btnRestartPause != null) btnRestartPause.clicked += HandleRestartClicked;
            if (btnMenuPause != null) btnMenuPause.clicked += HandleMenuClicked;

            if (btnNextLevel != null) btnNextLevel.clicked += HandleNextLevelClicked;
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

            // Level settings wiring
            if (btnLvl1 != null) btnLvl1.clicked += () => SelectLevelTab(1);
            if (btnLvl2 != null) btnLvl2.clicked += () => SelectLevelTab(2);
            if (btnLvl3 != null) btnLvl3.clicked += () => SelectLevelTab(3);

            if (sliderColumns != null)
            {
                sliderColumns.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetColumns(evt.newValue);
                    if (valColumns != null) valColumns.text = evt.newValue.ToString();
                });
            }

            if (sliderRows != null)
            {
                sliderRows.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetRowsPerTier(evt.newValue);
                    if (valRows != null) valRows.text = $"{evt.newValue} ({evt.newValue * 3})";
                });
            }

            if (sliderSpeed != null)
            {
                sliderSpeed.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetBallSpeedMultiplier(evt.newValue);
                    if (valSpeed != null) valSpeed.text = $"{evt.newValue:F1}x";
                });
            }

            if (sliderMultiplier2x != null)
            {
                sliderMultiplier2x.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetMultiplier2xCount(evt.newValue);
                    if (valMultiplier2x != null) valMultiplier2x.text = evt.newValue.ToString();
                });
            }

            if (sliderMultiplier3x != null)
            {
                sliderMultiplier3x.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetMultiplier3xCount(evt.newValue);
                    if (valMultiplier3x != null) valMultiplier3x.text = evt.newValue.ToString();
                });
            }

            if (sliderExpander != null)
            {
                sliderExpander.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetPaddleExpanderCount(evt.newValue);
                    if (valExpander != null) valExpander.text = evt.newValue.ToString();
                });
            }

            if (btnApplyLevel != null) btnApplyLevel.clicked += ApplyLevelSettingsAndRestart;
            if (btnCloseLevelSettings != null) btnCloseLevelSettings.clicked += HideLevelSettings;
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

                    if (btnNextLevel != null)
                    {
                        if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();
                        int currentLvl = levelGenerator != null && levelGenerator.CurrentConfig != null ? levelGenerator.CurrentConfig.LevelNumber : 1;
                        int nextLvl = currentLvl + 1;
                        int total = levelGenerator != null ? levelGenerator.TotalLevels : 3;
                        btnNextLevel.text = nextLvl > total ? "PLAY AGAIN (LOOP)" : $"NEXT LEVEL ({nextLvl})";
                    }

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

        private void HandleNextLevelClicked()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayPaddleBounce();
            if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();

            if (levelGenerator != null)
            {
                levelGenerator.AdvanceToNextLevel();
            }

            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.AdvanceToNextLevel();
            }

            if (levelClearModal != null) levelClearModal.AddToClassList("modal-hidden");
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

        public void ShowLevelSettings()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayPaddleBounce();

            if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();

            if (levelGenerator != null && levelGenerator.CurrentConfig != null)
            {
                activeEditableConfig = levelGenerator.CurrentConfig.Clone();
            }
            else
            {
                activeEditableConfig = ScriptableObject.CreateInstance<LevelConfiguration>();
            }

            UpdateLevelSettingsUI();

            if (pauseModal != null) pauseModal.AddToClassList("modal-hidden");
            if (levelSettingsModal != null) levelSettingsModal.RemoveFromClassList("modal-hidden");
        }

        public void HideLevelSettings()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayPaddleBounce();
            if (levelSettingsModal != null) levelSettingsModal.AddToClassList("modal-hidden");

            // If game was paused, show pause modal again
            if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State == GameState.Paused && pauseModal != null)
            {
                pauseModal.RemoveFromClassList("modal-hidden");
            }
        }

        private void SelectLevelTab(int levelNumber)
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayPaddleBounce();
            if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();

            LevelConfiguration baseConfig = levelGenerator != null ? levelGenerator.GetLevelConfig(levelNumber) : null;
            if (baseConfig != null)
            {
                activeEditableConfig = baseConfig.Clone();
            }

            UpdateLevelSettingsUI();
        }

        private void UpdateLevelSettingsUI()
        {
            if (activeEditableConfig == null) return;

            // Update tab button active states
            int lvl = activeEditableConfig.LevelNumber;
            if (btnLvl1 != null) { if (lvl == 1) btnLvl1.AddToClassList("level-tab-active"); else btnLvl1.RemoveFromClassList("level-tab-active"); }
            if (btnLvl2 != null) { if (lvl == 2) btnLvl2.AddToClassList("level-tab-active"); else btnLvl2.RemoveFromClassList("level-tab-active"); }
            if (btnLvl3 != null) { if (lvl == 3) btnLvl3.AddToClassList("level-tab-active"); else btnLvl3.RemoveFromClassList("level-tab-active"); }

            if (levelNameLabel != null) levelNameLabel.text = activeEditableConfig.LevelName;
            if (levelDescLabel != null) levelDescLabel.text = activeEditableConfig.Description;

            if (sliderColumns != null) sliderColumns.value = activeEditableConfig.Columns;
            if (valColumns != null) valColumns.text = activeEditableConfig.Columns.ToString();

            if (sliderRows != null) sliderRows.value = activeEditableConfig.RowsPerTier;
            if (valRows != null) valRows.text = $"{activeEditableConfig.RowsPerTier} ({activeEditableConfig.TotalRows})";

            if (sliderSpeed != null) sliderSpeed.value = activeEditableConfig.BallSpeedMultiplier;
            if (valSpeed != null) valSpeed.text = $"{activeEditableConfig.BallSpeedMultiplier:F1}x";

            if (sliderMultiplier2x != null) sliderMultiplier2x.value = activeEditableConfig.Multiplier2xCount;
            if (valMultiplier2x != null) valMultiplier2x.text = activeEditableConfig.Multiplier2xCount.ToString();

            if (sliderMultiplier3x != null) sliderMultiplier3x.value = activeEditableConfig.Multiplier3xCount;
            if (valMultiplier3x != null) valMultiplier3x.text = activeEditableConfig.Multiplier3xCount.ToString();

            if (sliderExpander != null) sliderExpander.value = activeEditableConfig.PaddleExpanderCount;
            if (valExpander != null) valExpander.text = activeEditableConfig.PaddleExpanderCount.ToString();
        }

        private void ApplyLevelSettingsAndRestart()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayPaddleBounce();
            if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();

            if (levelGenerator != null && activeEditableConfig != null)
            {
                levelGenerator.ApplyCustomConfigAndReload(activeEditableConfig);
            }

            if (levelSettingsModal != null) levelSettingsModal.AddToClassList("modal-hidden");

            // Unpause game if paused
            if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State == GameState.Paused)
            {
                ArcadeGameManager.Instance.TogglePause();
            }
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
