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

        // Quick control icon visual elements
        private VisualElement iconQuickLevels;
        private VisualElement iconQuickMute;
        private VisualElement iconQuickOptions;
        private VisualElement iconQuickPause;
        private float settingsRotationAngle = 0f;

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
        private readonly System.Collections.Generic.List<Button> hudLevelTabButtons = new System.Collections.Generic.List<Button>();
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
        private SliderInt sliderBomb;
        private Label valBomb;
        private SliderInt sliderGlass;
        private Label valGlass;
        private SliderInt sliderHeart;
        private Label valHeart;
        private SliderInt sliderShield;
        private Label valShield;
        private SliderInt sliderMultiBall;
        private Label valMultiBall;

        // Active Powerup Badges
        private VisualElement shieldStatusBadge;
        private Label shieldTimerLabel;
        private VisualElement multiballStatusBadge;
        private Label multiballCountLabel;
        private VisualElement paddleStatusBadge;
        private Label paddleTimerLabel;
        private VisualElement multiplierStatusBadge;
        private Label multiplierValueLabel;
        private Label multiplierTimerLabel;

        private Button btnApplyLevel;
        private Button btnCloseLevelSettings;

        [Header("Lives Icons")]
        [SerializeField] private Sprite heartFillSprite;
        [SerializeField] private Sprite heartEmptySprite;

        [Header("Power-Up Sprites")]
        [SerializeField] private Sprite shieldSprite;
        [SerializeField] private Sprite multiBallSprite;
        [SerializeField] private Sprite paddleExpandSprite;
        [SerializeField] private Sprite multiplierSprite;

        [Header("Quick Control Icons")]
        [SerializeField] private Sprite levelSettingsSprite;
        [SerializeField] private Sprite volumeMuteSprite;
        [SerializeField] private Sprite volumeUpSprite;
        [SerializeField] private Sprite settingsSprite;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Sprite playSprite;

        // Public accessors for testing & verification
        public VisualElement IconQuickPause => iconQuickPause;
        public VisualElement IconQuickMute => iconQuickMute;
        public VisualElement IconQuickOptions => iconQuickOptions;
        public VisualElement IconQuickLevels => iconQuickLevels;
        public float SettingsRotationAngle => settingsRotationAngle;
        public Toggle ToggleMute => toggleMute;
        public Button BtnQuickPause => btnQuickPause;
        public Button BtnQuickMute => btnQuickMute;
        public Button BtnQuickOptions => btnQuickOptions;
        public Button BtnQuickLevels => btnQuickLevels;
        public bool WasPausedByOptions => wasPausedByOptions;
        public bool WasPausedByLevelSettings => wasPausedByLevelSettings;
        public VisualElement ShieldStatusBadge => shieldStatusBadge;
        public Label ShieldTimerLabel => shieldTimerLabel;
        public VisualElement MultiballStatusBadge => multiballStatusBadge;
        public Label MultiballCountLabel => multiballCountLabel;
        public VisualElement PaddleStatusBadge => paddleStatusBadge;
        public Label PaddleTimerLabel => paddleTimerLabel;
        public VisualElement MultiplierStatusBadge => multiplierStatusBadge;
        public Label MultiplierValueLabel => multiplierValueLabel;
        public Label MultiplierTimerLabel => multiplierTimerLabel;

        private bool wasPausedByOptions = false;
        private bool wasPausedByLevelSettings = false;
        private LevelConfiguration activeEditableConfig;
        private LevelGenerator levelGenerator;
        private int targetFps = 60;

        public static ArcadeUIManager Instance { get; private set; }

        public static void SetInstanceForTesting(ArcadeUIManager instance)
        {
            Instance = instance;
        }

        private void Awake()
        {
            Instance = this;
            panelRenderer = GetComponent<PanelRenderer>();
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
                panelRenderer.enabled = false;
                panelRenderer.enabled = true;
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
            if (btnQuickOptions != null) btnQuickOptions.clicked -= HandleQuickOptionsClicked;
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
            hudLevelTabButtons.Clear();

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
                root.Q<VisualElement>("life-pip-3"),
                root.Q<VisualElement>("life-pip-4"),
                root.Q<VisualElement>("life-pip-5")
            };

            if (heartFillSprite == null)
            {
#if UNITY_EDITOR
                heartFillSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Heart_Fill.png");
#endif
            }
            if (heartEmptySprite == null)
            {
#if UNITY_EDITOR
                heartEmptySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Heart_Empty.png");
#endif
            }

#if UNITY_EDITOR
            if (levelSettingsSprite == null)
                levelSettingsSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Level_Settings.png");
            if (volumeMuteSprite == null)
                volumeMuteSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Volume_Mute.png");
            if (volumeUpSprite == null)
                volumeUpSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Volume_Up.png");
            if (settingsSprite == null)
                settingsSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Settings.png");
            if (pauseSprite == null)
                pauseSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Pause.png");
            if (playSprite == null)
                playSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Play.png");
            if (shieldSprite == null)
                shieldSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Shield.png");
            if (multiBallSprite == null)
                multiBallSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Multi_Ball.png");
            if (paddleExpandSprite == null)
                paddleExpandSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Arrows_Outward.png");
            if (multiplierSprite == null)
                multiplierSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Extra_Points.png");
#endif

            shieldStatusBadge = root.Q<VisualElement>("shield-status-badge");
            shieldTimerLabel = root.Q<Label>("shield-timer-label");
            multiballStatusBadge = root.Q<VisualElement>("multiball-status-badge");
            multiballCountLabel = root.Q<Label>("multiball-count-label");
            paddleStatusBadge = root.Q<VisualElement>("paddle-status-badge");
            paddleTimerLabel = root.Q<Label>("paddle-timer-label");
            multiplierStatusBadge = root.Q<VisualElement>("multiplier-status-badge");
            multiplierValueLabel = root.Q<Label>("multiplier-value-label");
            multiplierTimerLabel = root.Q<Label>("multiplier-timer-label");

            var iconShield = root.Q<VisualElement>("shield-status-icon");
            if (iconShield != null && shieldSprite != null)
                iconShield.style.backgroundImage = new StyleBackground(shieldSprite);

            var iconMulti = root.Q<VisualElement>("multiball-status-icon");
            if (iconMulti != null && multiBallSprite != null)
                iconMulti.style.backgroundImage = new StyleBackground(multiBallSprite);

            var iconPaddle = root.Q<VisualElement>("paddle-status-icon");
            if (iconPaddle != null && paddleExpandSprite != null)
                iconPaddle.style.backgroundImage = new StyleBackground(paddleExpandSprite);

            var iconMultiplier = root.Q<VisualElement>("multiplier-status-icon");
            if (iconMultiplier != null && multiplierSprite != null)
                iconMultiplier.style.backgroundImage = new StyleBackground(multiplierSprite);

            btnQuickLevels = root.Q<Button>("btn-quick-levels");
            btnQuickMute = root.Q<Button>("btn-quick-mute");
            btnQuickOptions = root.Q<Button>("btn-quick-options");
            btnQuickPause = root.Q<Button>("btn-quick-pause");

            iconQuickLevels = root.Q<VisualElement>("icon-quick-levels");
            iconQuickMute = root.Q<VisualElement>("icon-quick-mute");
            iconQuickOptions = root.Q<VisualElement>("icon-quick-options");
            iconQuickPause = root.Q<VisualElement>("icon-quick-pause");

            if (iconQuickLevels != null && levelSettingsSprite != null)
                iconQuickLevels.style.backgroundImage = new StyleBackground(levelSettingsSprite);
            if (iconQuickOptions != null && settingsSprite != null)
                iconQuickOptions.style.backgroundImage = new StyleBackground(settingsSprite);

            settingsRotationAngle = 0f;
            if (iconQuickOptions != null)
                iconQuickOptions.style.rotate = new StyleRotate(new Rotate(Angle.Degrees(0f)));

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
            sliderBomb = root.Q<SliderInt>("slider-bomb");
            valBomb = root.Q<Label>("val-bomb");
            sliderGlass = root.Q<SliderInt>("slider-glass");
            valGlass = root.Q<Label>("val-glass");
            sliderHeart = root.Q<SliderInt>("slider-heart");
            valHeart = root.Q<Label>("val-heart");
            sliderShield = root.Q<SliderInt>("slider-shield");
            valShield = root.Q<Label>("val-shield");
            sliderMultiBall = root.Q<SliderInt>("slider-multiball");
            valMultiBall = root.Q<Label>("val-multiball");

            btnApplyLevel = root.Q<Button>("btn-apply-level");
            btnCloseLevelSettings = root.Q<Button>("btn-close-level-settings");

            // Wire quick buttons
            if (btnQuickLevels != null) btnQuickLevels.clicked += ShowLevelSettings;
            if (btnQuickMute != null) btnQuickMute.clicked += HandleQuickMuteClicked;
            if (btnQuickOptions != null) btnQuickOptions.clicked += HandleQuickOptionsClicked;
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
            hudLevelTabButtons.Clear();
            if (levelSettingsModal != null)
            {
                var tabsContainer = levelSettingsModal.Q<VisualElement>(className: "level-tabs-container");
                if (tabsContainer != null)
                {
                    var buttons = tabsContainer.Query<Button>(className: "level-tab-btn").ToList();
                    for (int i = 0; i < buttons.Count; i++)
                    {
                        int lvlNum = i + 1;
                        var btn = buttons[i];
                        hudLevelTabButtons.Add(btn);
                        btn.clicked += () => SelectLevelTab(lvlNum);
                    }
                }
            }

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

            if (sliderBomb != null)
            {
                sliderBomb.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetBombCount(evt.newValue);
                    if (valBomb != null) valBomb.text = evt.newValue.ToString();
                });
            }

            if (sliderGlass != null)
            {
                sliderGlass.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetGlassEnclosedCount(evt.newValue);
                    if (valGlass != null) valGlass.text = evt.newValue.ToString();
                });
            }

            if (sliderHeart != null)
            {
                sliderHeart.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetExtraHeartCount(evt.newValue);
                    if (valHeart != null) valHeart.text = evt.newValue.ToString();
                });
            }

            if (sliderShield != null)
            {
                sliderShield.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetShieldCount(evt.newValue);
                    if (valShield != null) valShield.text = evt.newValue.ToString();
                });
            }

            if (sliderMultiBall != null)
            {
                sliderMultiBall.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetMultiBallCount(evt.newValue);
                    if (valMultiBall != null) valMultiBall.text = evt.newValue.ToString();
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
                ArcadeGameManager.Instance.OnShieldStateChanged += HandleShieldStateChanged;
                ArcadeGameManager.Instance.OnShieldTick += HandleShieldTick;
                ArcadeGameManager.Instance.OnActiveBallCountChanged += HandleActiveBallCountChanged;
                ArcadeGameManager.Instance.OnPaddleExpandStateChanged += HandlePaddleExpandStateChanged;
                ArcadeGameManager.Instance.OnPaddleExpandTick += HandlePaddleExpandTick;
                ArcadeGameManager.Instance.OnScoreMultiplierStateChanged += HandleScoreMultiplierStateChanged;
                ArcadeGameManager.Instance.OnScoreMultiplierTick += HandleScoreMultiplierTick;
            }
        }

        private void UnsubscribeEvents()
        {
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.OnScoreChanged -= UpdateScoreDisplay;
                ArcadeGameManager.Instance.OnLivesChanged -= UpdateLivesDisplay;
                ArcadeGameManager.Instance.OnStateChanged -= HandleGameStateChanged;
                ArcadeGameManager.Instance.OnShieldStateChanged -= HandleShieldStateChanged;
                ArcadeGameManager.Instance.OnShieldTick -= HandleShieldTick;
                ArcadeGameManager.Instance.OnActiveBallCountChanged -= HandleActiveBallCountChanged;
                ArcadeGameManager.Instance.OnPaddleExpandStateChanged -= HandlePaddleExpandStateChanged;
                ArcadeGameManager.Instance.OnPaddleExpandTick -= HandlePaddleExpandTick;
                ArcadeGameManager.Instance.OnScoreMultiplierStateChanged -= HandleScoreMultiplierStateChanged;
                ArcadeGameManager.Instance.OnScoreMultiplierTick -= HandleScoreMultiplierTick;
            }
        }

        private void InitializeDisplay()
        {
            if (ArcadeGameManager.Instance != null)
            {
                UpdateScoreDisplay(ArcadeGameManager.Instance.Score, 0);
                UpdateLivesDisplay(ArcadeGameManager.Instance.Lives);
                HandleGameStateChanged(ArcadeGameManager.Instance.State);
                HandleShieldStateChanged(ArcadeGameManager.Instance.IsShieldActive, ArcadeGameManager.Instance.ShieldTimeRemaining);
                HandleActiveBallCountChanged(ArcadeGameManager.Instance.ActiveBallCount);
                HandlePaddleExpandStateChanged(ArcadeGameManager.Instance.IsPaddleExpanded, ArcadeGameManager.Instance.PaddleExpandTimeRemaining);
                HandleScoreMultiplierStateChanged(ArcadeGameManager.Instance.ActiveScoreMultiplier > 1, ArcadeGameManager.Instance.ActiveScoreMultiplier, ArcadeGameManager.Instance.MultiplierTimeRemaining);
            }

            if (ArcadeAudioManager.Instance != null)
            {
                if (sliderVolume != null) sliderVolume.value = ArcadeAudioManager.Instance.Volume;
                if (toggleMute != null) toggleMute.value = ArcadeAudioManager.Instance.IsMuted;
                UpdateMuteButtonIcon();
            }

            UpdatePauseButtonIcon(ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State == GameState.Paused);

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

                // Base 3 pips are always displayed (active or lost outline).
                // Bonus pips 4 and 5 are only shown when lives > 3 or when active.
                if (i >= 3)
                {
                    if (i < lives)
                    {
                        lifePips[i].RemoveFromClassList("pip-hidden");
                        lifePips[i].style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        lifePips[i].AddToClassList("pip-hidden");
                        lifePips[i].style.display = DisplayStyle.None;
                    }
                }
                else
                {
                    lifePips[i].RemoveFromClassList("pip-hidden");
                    lifePips[i].style.display = DisplayStyle.Flex;
                }

                if (i < lives)
                {
                    lifePips[i].AddToClassList("pip-active");
                    lifePips[i].RemoveFromClassList("pip-lost");
                    if (heartFillSprite != null)
                    {
                        lifePips[i].style.backgroundImage = new StyleBackground(heartFillSprite);
                        lifePips[i].style.unityBackgroundImageTintColor = new StyleColor(new Color(1f, 0.231f, 0.337f, 1f)); // #ff3b56
                    }
                }
                else
                {
                    lifePips[i].RemoveFromClassList("pip-active");
                    lifePips[i].AddToClassList("pip-lost");
                    if (heartEmptySprite != null)
                    {
                        lifePips[i].style.backgroundImage = new StyleBackground(heartEmptySprite);
                        lifePips[i].style.unityBackgroundImageTintColor = new StyleColor(new Color(1f, 0.231f, 0.337f, 0.35f)); // dimmed red outline
                    }
                }
            }
        }

        public void AnimateFlyingHeart(Vector3 worldPosition)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(DoFlyingHeartAnimation(worldPosition));
            }
        }

        private System.Collections.IEnumerator DoFlyingHeartAnimation(Vector3 worldPos)
        {
            if (root == null) yield break;

            Camera cam = Camera.main;
            Vector2 startPanelPos;
            if (cam != null)
            {
                Vector3 screenPt = cam.WorldToScreenPoint(worldPos);
                startPanelPos = new Vector2(screenPt.x, Screen.height - screenPt.y);
                if (root.panel != null)
                {
                    startPanelPos = RuntimePanelUtils.ScreenToPanel(root.panel, startPanelPos);
                }
            }
            else
            {
                startPanelPos = new Vector2(root.resolvedStyle.width * 0.5f, root.resolvedStyle.height * 0.5f);
            }

            int targetIndex = Mathf.Clamp(ArcadeGameManager.Instance != null ? ArcadeGameManager.Instance.Lives - 1 : 2, 0, lifePips.Length - 1);
            VisualElement targetPip = (lifePips != null && targetIndex >= 0 && targetIndex < lifePips.Length) ? lifePips[targetIndex] : null;

            Vector2 targetPanelPos;
            if (targetPip != null && targetPip.worldBound.width > 0)
            {
                targetPanelPos = targetPip.worldBound.center;
            }
            else
            {
                targetPanelPos = new Vector2(root.resolvedStyle.width * 0.5f, 35f);
            }

            VisualElement flyingHeart = new VisualElement();
            flyingHeart.AddToClassList("flying-heart");
            if (heartFillSprite != null)
            {
                flyingHeart.style.backgroundImage = new StyleBackground(heartFillSprite);
            }
            flyingHeart.pickingMode = PickingMode.Ignore;
            flyingHeart.style.left = startPanelPos.x - 20f;
            flyingHeart.style.top = startPanelPos.y - 20f;
            root.Add(flyingHeart);

            float duration = 0.55f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = Mathf.SmoothStep(0f, 1f, t);

                Vector2 currentPos = Vector2.Lerp(startPanelPos, targetPanelPos, curvedT);
                currentPos.y += Mathf.Sin(t * Mathf.PI) * -60f;

                flyingHeart.style.left = currentPos.x - 20f;
                flyingHeart.style.top = currentPos.y - 20f;

                float scale = Mathf.Lerp(1.5f, 1.0f, t);
                flyingHeart.style.scale = new StyleScale(new Scale(new Vector2(scale, scale)));

                yield return null;
            }

            if (root.Contains(flyingHeart))
            {
                root.Remove(flyingHeart);
            }

            if (targetPip != null)
            {
                targetPip.AddToClassList("pip-pop");
                yield return new WaitForSecondsRealtime(0.18f);
                targetPip.RemoveFromClassList("pip-pop");
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            UpdatePauseButtonIcon(state == GameState.Paused);

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

        public void HandleShieldStateChanged(bool active, float remaining)
        {
            if (shieldStatusBadge == null) return;
            if (active)
            {
                shieldStatusBadge.RemoveFromClassList("powerup-hidden");
                shieldStatusBadge.style.display = DisplayStyle.Flex;
                if (shieldTimerLabel != null) shieldTimerLabel.text = $"{Mathf.CeilToInt(remaining)}s";
            }
            else
            {
                shieldStatusBadge.AddToClassList("powerup-hidden");
                shieldStatusBadge.style.display = DisplayStyle.None;
            }
        }

        public void HandleShieldTick(float timeRemaining)
        {
            if (shieldTimerLabel != null)
                shieldTimerLabel.text = $"{Mathf.CeilToInt(timeRemaining)}s";
        }

        public void HandleActiveBallCountChanged(int count)
        {
            if (multiballStatusBadge == null) return;
            if (count > 1)
            {
                multiballStatusBadge.RemoveFromClassList("powerup-hidden");
                multiballStatusBadge.style.display = DisplayStyle.Flex;
                if (multiballCountLabel != null) multiballCountLabel.text = $"{count} BALLS";
            }
            else
            {
                multiballStatusBadge.AddToClassList("powerup-hidden");
                multiballStatusBadge.style.display = DisplayStyle.None;
            }
        }

        public void HandlePaddleExpandStateChanged(bool active, float remaining)
        {
            if (paddleStatusBadge == null) return;
            if (active)
            {
                paddleStatusBadge.RemoveFromClassList("powerup-hidden");
                paddleStatusBadge.style.display = DisplayStyle.Flex;
                if (paddleTimerLabel != null) paddleTimerLabel.text = $"{Mathf.CeilToInt(remaining)}s";
            }
            else
            {
                paddleStatusBadge.AddToClassList("powerup-hidden");
                paddleStatusBadge.style.display = DisplayStyle.None;
            }
        }

        public void HandlePaddleExpandTick(float timeRemaining)
        {
            if (paddleTimerLabel != null)
                paddleTimerLabel.text = $"{Mathf.CeilToInt(timeRemaining)}s";
        }

        public void HandleScoreMultiplierStateChanged(bool active, int multiplier, float remaining)
        {
            if (multiplierStatusBadge == null) return;
            if (active && multiplier > 1)
            {
                multiplierStatusBadge.RemoveFromClassList("powerup-hidden");
                multiplierStatusBadge.style.display = DisplayStyle.Flex;

                multiplierStatusBadge.RemoveFromClassList("mult-tier-2x");
                multiplierStatusBadge.RemoveFromClassList("mult-tier-3x");
                multiplierStatusBadge.RemoveFromClassList("mult-tier-4x");
                multiplierStatusBadge.RemoveFromClassList("mult-tier-5x");
                multiplierStatusBadge.AddToClassList($"mult-tier-{multiplier}x");

                if (multiplierValueLabel != null) multiplierValueLabel.text = $"{multiplier}X";
                if (multiplierTimerLabel != null) multiplierTimerLabel.text = $"{Mathf.CeilToInt(remaining)}s";
            }
            else
            {
                multiplierStatusBadge.AddToClassList("powerup-hidden");
                multiplierStatusBadge.style.display = DisplayStyle.None;
            }
        }

        public void HandleScoreMultiplierTick(float timeRemaining)
        {
            if (multiplierTimerLabel != null)
                multiplierTimerLabel.text = $"{Mathf.CeilToInt(timeRemaining)}s";
        }

        private void HandleQuickMuteClicked()
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayButtonPress();
                ArcadeAudioManager.Instance.ToggleMute();
                if (toggleMute != null) toggleMute.value = ArcadeAudioManager.Instance.IsMuted;
                UpdateMuteButtonIcon();
            }
        }

        public void UpdateMuteButtonIcon(bool? isMutedOverride = null)
        {
            bool isMuted = isMutedOverride ?? (ArcadeAudioManager.Instance != null && ArcadeAudioManager.Instance.IsMuted);

            if (btnQuickMute != null)
            {
                btnQuickMute.text = string.Empty;
                btnQuickMute.tooltip = isMuted ? "Unmute Sound" : "Mute Sound";
            }

            if (iconQuickMute != null)
            {
                // Unmuted -> show TX_Volume_Mute (indicates ability to mute)
                // Muted -> show TX_Volume_Up (indicates ability to unmute)
                iconQuickMute.RemoveFromClassList("icon-volume-mute");
                iconQuickMute.RemoveFromClassList("icon-volume-up");

                if (isMuted)
                {
                    iconQuickMute.AddToClassList("icon-volume-up");
                    if (volumeUpSprite != null)
                    {
                        iconQuickMute.style.backgroundImage = new StyleBackground(volumeUpSprite);
                        iconQuickMute.style.unityBackgroundImageTintColor = new StyleColor(new Color(1f, 0.231f, 0.337f, 1f)); // #ff3b56
                    }
                }
                else
                {
                    iconQuickMute.AddToClassList("icon-volume-mute");
                    if (volumeMuteSprite != null)
                    {
                        iconQuickMute.style.backgroundImage = new StyleBackground(volumeMuteSprite);
                        iconQuickMute.style.unityBackgroundImageTintColor = new StyleColor(new Color(0.886f, 0.910f, 0.941f, 1f)); // #e2e8f0
                    }
                }
            }

            UpdateToggleMuteIcon(isMuted);
        }

        public void UpdateToggleMuteIcon(bool isMuted)
        {
            if (toggleMute == null) return;
            var checkmark = toggleMute.Q(className: "unity-toggle__checkmark");
            if (checkmark != null)
            {
                if (isMuted)
                {
                    if (volumeMuteSprite != null)
                        checkmark.style.backgroundImage = new StyleBackground(volumeMuteSprite);
                    checkmark.style.unityBackgroundImageTintColor = new StyleColor(new Color(1f, 0.231f, 0.337f, 1f)); // #ff3b56
                }
                else
                {
                    if (volumeUpSprite != null)
                        checkmark.style.backgroundImage = new StyleBackground(volumeUpSprite);
                    checkmark.style.unityBackgroundImageTintColor = new StyleColor(new Color(0.13f, 0.83f, 0.99f, 1f)); // #21d4fd
                }
            }
        }

        public void UpdatePauseButtonIcon(bool isPaused)
        {
            if (btnQuickPause != null)
            {
                btnQuickPause.text = string.Empty;
                btnQuickPause.tooltip = isPaused ? "Resume Game" : "Pause Game";
            }

            if (iconQuickPause != null)
            {
                iconQuickPause.RemoveFromClassList("icon-pause");
                iconQuickPause.RemoveFromClassList("icon-play");

                if (isPaused)
                {
                    // Paused: show Play icon to indicate ability to resume
                    iconQuickPause.AddToClassList("icon-play");
                    if (playSprite != null)
                    {
                        iconQuickPause.style.backgroundImage = new StyleBackground(playSprite);
                        iconQuickPause.style.unityBackgroundImageTintColor = new StyleColor(new Color(0.149f, 0.910f, 0.522f, 1f)); // #26e885
                    }
                }
                else
                {
                    // Playing / Ready: show Pause icon to indicate ability to pause
                    iconQuickPause.AddToClassList("icon-pause");
                    if (pauseSprite != null)
                    {
                        iconQuickPause.style.backgroundImage = new StyleBackground(pauseSprite);
                        iconQuickPause.style.unityBackgroundImageTintColor = new StyleColor(new Color(0.886f, 0.910f, 0.941f, 1f)); // #e2e8f0
                    }
                }
            }
        }

        public void TriggerSettingsButtonSpin()
        {
            if (iconQuickOptions == null) return;
            settingsRotationAngle += 360f;
            iconQuickOptions.style.rotate = new StyleRotate(new Rotate(Angle.Degrees(settingsRotationAngle)));
        }

        private void HandleQuickOptionsClicked()
        {
            if (optionsModal != null && !optionsModal.ClassListContains("modal-hidden"))
            {
                HideOptions();
            }
            else
            {
                ShowOptions();
            }
        }

        private void HandleQuickPauseClicked()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.TogglePause();
            }
            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        private void HandleResumeClicked()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.TogglePause();
            }
            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        private void HandleRestartClicked()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.RestartGame();
            }
        }

        private void HandleNextLevelClicked()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
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
            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        private void HandleMenuClicked()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.LoadMainMenu();
            }
        }

        public void ShowOptions()
        {
            TriggerSettingsButtonSpin();
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();

            if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State != GameState.Paused)
            {
                wasPausedByOptions = true;
                ArcadeGameManager.Instance.PauseGame();
            }
            else
            {
                wasPausedByOptions = false;
            }

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }

            if (optionsModal != null) optionsModal.RemoveFromClassList("modal-hidden");
        }

        public void HideOptions()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (optionsModal != null) optionsModal.AddToClassList("modal-hidden");

            if (wasPausedByOptions)
            {
                wasPausedByOptions = false;
                if (ArcadeGameManager.Instance != null)
                {
                    ArcadeGameManager.Instance.ResumeGame();
                }
            }

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        public void ShowLevelSettings()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();

            if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State != GameState.Paused)
            {
                wasPausedByLevelSettings = true;
                ArcadeGameManager.Instance.PauseGame();
            }
            else
            {
                wasPausedByLevelSettings = false;
            }

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }

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
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (levelSettingsModal != null) levelSettingsModal.AddToClassList("modal-hidden");

            if (wasPausedByLevelSettings)
            {
                wasPausedByLevelSettings = false;
                if (ArcadeGameManager.Instance != null)
                {
                    ArcadeGameManager.Instance.ResumeGame();
                }
            }
            else
            {
                // If game was paused before opening level settings (e.g. from pause modal), re-show pause modal
                if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State == GameState.Paused && pauseModal != null)
                {
                    pauseModal.RemoveFromClassList("modal-hidden");
                }
            }

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        private void SelectLevelTab(int levelNumber)
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
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
            for (int i = 0; i < hudLevelTabButtons.Count; i++)
            {
                if (i + 1 == lvl)
                    hudLevelTabButtons[i].AddToClassList("level-tab-active");
                else
                    hudLevelTabButtons[i].RemoveFromClassList("level-tab-active");
            }

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

            if (sliderBomb != null) sliderBomb.value = activeEditableConfig.BombCount;
            if (valBomb != null) valBomb.text = activeEditableConfig.BombCount.ToString();

            if (sliderGlass != null) sliderGlass.value = activeEditableConfig.GlassEnclosedCount;
            if (valGlass != null) valGlass.text = activeEditableConfig.GlassEnclosedCount.ToString();

            if (sliderHeart != null) sliderHeart.value = activeEditableConfig.ExtraHeartCount;
            if (valHeart != null) valHeart.text = activeEditableConfig.ExtraHeartCount.ToString();

            if (sliderShield != null) sliderShield.value = activeEditableConfig.ShieldCount;
            if (valShield != null) valShield.text = activeEditableConfig.ShieldCount.ToString();

            if (sliderMultiBall != null) sliderMultiBall.value = activeEditableConfig.MultiBallCount;
            if (valMultiBall != null) valMultiBall.text = activeEditableConfig.MultiBallCount.ToString();
        }

        private void ApplyLevelSettingsAndRestart()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();

            wasPausedByLevelSettings = false;
            wasPausedByOptions = false;

            if (levelGenerator != null && activeEditableConfig != null)
            {
                levelGenerator.ApplyCustomConfigAndReload(activeEditableConfig);
            }

            if (levelSettingsModal != null) levelSettingsModal.AddToClassList("modal-hidden");

            // Level reloaded: unpause and transition to ReadyToLaunch so tap-to-launch works immediately
            Time.timeScale = 1f;
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.SetState(GameState.ReadyToLaunch);
            }

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        private void ToggleFpsSetting()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            targetFps = targetFps == 60 ? 120 : 60;
            Application.targetFrameRate = targetFps;
            PlayerPrefs.SetInt("Arcade_TargetFPS", targetFps);
            PlayerPrefs.Save();
            if (btnFps != null) btnFps.text = $"{targetFps} FPS";
        }

        public bool IsAnyModalVisible()
        {
            return (optionsModal != null && !optionsModal.ClassListContains("modal-hidden")) ||
                   (levelSettingsModal != null && !levelSettingsModal.ClassListContains("modal-hidden")) ||
                   (pauseModal != null && !pauseModal.ClassListContains("modal-hidden")) ||
                   (gameOverModal != null && !gameOverModal.ClassListContains("modal-hidden")) ||
                   (levelClearModal != null && !levelClearModal.ClassListContains("modal-hidden"));
        }

        public bool IsPointerOverUI(Vector2 screenPos)
        {
            if (IsAnyModalVisible()) return true;

            if (root != null && root.panel != null)
            {
                Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, screenPos);
                VisualElement picked = root.panel.Pick(panelPos);
                if (picked != null && picked != root && picked.name != "hud-root")
                {
                    return true;
                }
            }
            return false;
        }
    }
}
