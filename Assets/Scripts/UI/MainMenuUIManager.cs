using Arcade.Audio;
using Arcade.BlockBreaker;
using Arcade.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Arcade.UI
{
    /// <summary>
    /// Coordinates UI Toolkit interactions in the fast-loading Main Menu start scene,
    /// including level selection, options, and continue game logic.
    /// Uses Unity 6 PanelRenderer component.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class MainMenuUIManager : MonoBehaviour
    {
        private PanelRenderer panelRenderer;
        private VisualElement root;

        private Button btnNewGame;
        private Button btnLevelSelect;
        private Button btnContinue;
        private Button btnHighscores;
        private Button btnHowToPlay;
        private Button btnOptions;
        private Button btnCredits;

        // Modals
        private VisualElement levelModal;
        private VisualElement optionsModal;
        private VisualElement creditsModal;
        private VisualElement highscoresModal;
        private VisualElement howToPlayModal;

        private Button btnCloseLevelModal;
        private Button btnStartSelectedLevel;
        [Header("Level Presets")]
        [SerializeField] private LevelConfiguration[] levelPresets;

        private Button btnCloseOptions;
        private Button btnCloseCredits;
        private Button btnCloseHighscores;
        private Button btnResetHighscores;
        private Button btnCloseHowToPlay;

        // Credit links
        private Button btnCreditEmail;
        private Button btnCreditWebsite;
        private Button btnCreditLinkedin;
        private Button btnCreditGithub;

        // Level Select controls
        private readonly System.Collections.Generic.List<Button> menuLevelTabButtons = new System.Collections.Generic.List<Button>();
        private Label menuLevelTag;
        private Label menuLevelDiff;
        private Label menuLevelName;
        private Label menuLevelDesc;
        private Label menuLevelStars;
        private Label menuLevelBestTime;
        private VisualElement levelPreviewThumb;
        private int selectedLevelNumber = 1;

        // Options controls
        private Slider sliderVolume;
        private Toggle toggleMute;
        private Slider sliderMusicVolume;
        private Toggle toggleMusicMute;
        private Button btnLangEn;
        private Button btnLangTr;
        private Button btnToggleFps;

        [Header("Audio Toggle Icons")]
        [SerializeField] private Sprite volumeUpSprite;
        [SerializeField] private Sprite volumeMuteSprite;

        // Localized Labels
        private Label labelSettingLang;
        private Label labelSettingSfx;
        private Label labelSettingSfxMute;
        private Label labelSettingMusic;
        private Label labelSettingMusicMute;
        private Label labelSettingFps;
        private Label modalTitleSettings;
        private Label modalTitleSelectLevel;
        private Label modalSubSelectLevel;
        private Label modalTitleCredits;
        private Label labelDiffSubtitle;
        private Label labelStarsSubtitle;
        private Label labelTimeSubtitle;

        private int targetFps = 60;

        private void Awake()
        {
            panelRenderer = GetComponent<PanelRenderer>();
#if UNITY_EDITOR
            if (volumeUpSprite == null)
                volumeUpSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Volume_Up.png");
            if (volumeMuteSprite == null)
                volumeMuteSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Volume_Mute.png");
#endif
        }

        private void Start()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            if (root == null || btnNewGame == null)
            {
                EnsureInitialized();
            }
        }

        public void EnsureInitialized()
        {
            if (root == null)
            {
                root = GetRootVisualElement();
            }

            if (root != null && btnNewGame == null)
            {
                UnbindElements();
                BindElements();
                InitializeValues();
            }
        }

        private VisualElement GetRootVisualElement()
        {
            if (root != null) return root;

            if (panelRenderer == null) panelRenderer = GetComponent<PanelRenderer>();
            if (panelRenderer != null)
            {
                var prop = panelRenderer.GetType().GetProperty("rootVisualElement", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (prop != null)
                {
                    root = prop.GetValue(panelRenderer) as VisualElement;
                }

                if (root == null)
                {
                    var panelProp = panelRenderer.GetType().GetProperty("containerPanel", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var panel = panelProp?.GetValue(panelRenderer) as IPanel;
                    if (panel != null)
                    {
                        root = panel.visualTree;
                    }
                }
            }

            if (root == null)
            {
                var doc = GetComponent<UIDocument>();
                if (doc != null) root = doc.rootVisualElement;
            }

            return root;
        }

        private void OnEnable()
        {
            if (panelRenderer == null) panelRenderer = GetComponent<PanelRenderer>();
            if (panelRenderer != null)
            {
                panelRenderer.RegisterUIReloadCallback(OnUIReload);
            }
            EnsureInitialized();
        }

        private void OnDisable()
        {
            if (panelRenderer != null)
            {
                panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            }
            UnbindElements();
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement newRoot, int version)
        {
            if (newRoot == null) return;
            root = newRoot;
            UnbindElements();
            BindElements();
            InitializeValues();
        }

        private void UnbindElements()
        {
            if (btnNewGame != null) btnNewGame.clicked -= HandleNewGameClicked;
            if (btnLevelSelect != null) btnLevelSelect.clicked -= ShowLevelModal;
            if (btnContinue != null) btnContinue.clicked -= HandleContinueClicked;
            if (btnHighscores != null) btnHighscores.clicked -= ShowHighScoresModal;
            if (btnHowToPlay != null) btnHowToPlay.clicked -= ShowHowToPlayModal;
            if (btnOptions != null) btnOptions.clicked -= ShowOptions;
            if (btnCredits != null) btnCredits.clicked -= ShowCredits;

            if (btnCloseLevelModal != null) btnCloseLevelModal.clicked -= HideLevelModal;
            if (btnStartSelectedLevel != null) btnStartSelectedLevel.clicked -= HandleStartSelectedLevel;

            if (btnCloseOptions != null) btnCloseOptions.clicked -= HideOptions;
            if (btnCloseCredits != null) btnCloseCredits.clicked -= HideCredits;
            if (btnCloseHighscores != null) btnCloseHighscores.clicked -= HideHighScoresModal;
            if (btnResetHighscores != null) btnResetHighscores.clicked -= HandleResetHighScores;
            if (btnCloseHowToPlay != null) btnCloseHowToPlay.clicked -= HideHowToPlayModal;

            if (btnCreditEmail != null) btnCreditEmail.clicked -= OpenEmail;
            if (btnCreditWebsite != null) btnCreditWebsite.clicked -= OpenWebsite;
            if (btnCreditLinkedin != null) btnCreditLinkedin.clicked -= OpenLinkedIn;
            if (btnCreditGithub != null) btnCreditGithub.clicked -= OpenGitHub;

            if (btnToggleFps != null) btnToggleFps.clicked -= ToggleFpsSetting;
            if (btnLangEn != null) btnLangEn.clicked -= HandleLangEnClicked;
            if (btnLangTr != null) btnLangTr.clicked -= HandleLangTrClicked;
            LocalizationManager.OnLanguageChanged -= UpdateLocalizedTexts;

            menuLevelTabButtons.Clear();
        }

        private void BindElements()
        {
            if (root == null) return;

            btnNewGame = root.Q<Button>("btn-new-game");
            btnLevelSelect = root.Q<Button>("btn-level-select");
            btnContinue = root.Q<Button>("btn-continue");
            btnHighscores = root.Q<Button>("btn-highscores");
            btnHowToPlay = root.Q<Button>("btn-how-to-play");
            btnOptions = root.Q<Button>("btn-options");
            btnCredits = root.Q<Button>("btn-credits");

            levelModal = root.Q<VisualElement>("level-modal");
            optionsModal = root.Q<VisualElement>("options-modal");
            creditsModal = root.Q<VisualElement>("credits-modal");
            highscoresModal = root.Q<VisualElement>("highscores-modal");
            howToPlayModal = root.Q<VisualElement>("how-to-play-modal");

            btnCloseLevelModal = root.Q<Button>("btn-close-level-modal");
            btnStartSelectedLevel = root.Q<Button>("btn-start-selected-level");
            btnCloseOptions = root.Q<Button>("btn-close-options");
            btnCloseCredits = root.Q<Button>("btn-close-credits");
            btnCloseHighscores = root.Q<Button>("btn-close-highscores");
            btnResetHighscores = root.Q<Button>("btn-reset-highscores");
            btnCloseHowToPlay = root.Q<Button>("btn-close-how-to-play");

            btnCreditEmail = root.Q<Button>("btn-credit-email");
            btnCreditWebsite = root.Q<Button>("btn-credit-website");
            btnCreditLinkedin = root.Q<Button>("btn-credit-linkedin");
            btnCreditGithub = root.Q<Button>("btn-credit-github");

            menuLevelTabButtons.Clear();
            var tabsContainer = root.Q<VisualElement>(className: "level-grid-container") ?? root.Q<VisualElement>(className: "level-tabs-container");
            if (tabsContainer != null)
            {
                tabsContainer.Clear();
                int totalLevels = levelPresets != null && levelPresets.Length > 0 ? levelPresets.Length : 15;
                for (int i = 0; i < totalLevels; i++)
                {
                    int lvlNum = i + 1;
                    int stars = HighScoreManager.GetLevelStars(lvlNum);
                    float bestTime = HighScoreManager.GetLevelBestTime(lvlNum);

                    var cardBtn = new Button();
                    cardBtn.AddToClassList("level-grid-card");
                    cardBtn.AddToClassList("level-tab-btn");

                    var numLbl = new Label(lvlNum.ToString());
                    numLbl.AddToClassList("level-card-num");
                    numLbl.pickingMode = PickingMode.Ignore;
                    cardBtn.Add(numLbl);

                    var starsRow = new VisualElement();
                    starsRow.AddToClassList("level-card-stars-row");
                    starsRow.pickingMode = PickingMode.Ignore;
                    for (int s = 0; s < 3; s++)
                    {
                        var star = new Label("★");
                        star.AddToClassList("card-star");
                        star.AddToClassList(s < stars ? "star-active" : "star-inactive");
                        star.pickingMode = PickingMode.Ignore;
                        starsRow.Add(star);
                    }
                    cardBtn.Add(starsRow);

                    var timeLbl = new Label(bestTime > 0f ? HighScoreManager.FormatTime(bestTime) : "--:--");
                    timeLbl.AddToClassList("level-card-time");
                    timeLbl.pickingMode = PickingMode.Ignore;
                    cardBtn.Add(timeLbl);

                    tabsContainer.Add(cardBtn);
                    menuLevelTabButtons.Add(cardBtn);
                    int captureLvl = lvlNum;
                    cardBtn.clicked += () => SelectLevel(captureLvl);
                }
            }

            menuLevelTag = root.Q<Label>("menu-level-tag");
            menuLevelDiff = root.Q<Label>("menu-level-diff");
            menuLevelName = root.Q<Label>("menu-level-name");
            menuLevelDesc = root.Q<Label>("menu-level-desc");
            menuLevelStars = root.Q<Label>("menu-level-stars");
            menuLevelBestTime = root.Q<Label>("menu-level-best-time");
            levelPreviewThumb = root.Q<VisualElement>("level-preview-thumb");

            sliderVolume = root.Q<Slider>("slider-volume");
            toggleMute = root.Q<Toggle>("toggle-mute");
            sliderMusicVolume = root.Q<Slider>("slider-music-volume");
            toggleMusicMute = root.Q<Toggle>("toggle-music-mute");
            btnLangEn = root.Q<Button>("btn-lang-en");
            btnLangTr = root.Q<Button>("btn-lang-tr");
            btnToggleFps = root.Q<Button>("btn-toggle-fps");

            labelSettingLang = root.Q<Label>("label-setting-lang");
            labelSettingSfx = root.Q<Label>("label-setting-sfx");
            labelSettingSfxMute = root.Q<Label>("label-setting-sfx-mute");
            labelSettingMusic = root.Q<Label>("label-setting-music");
            labelSettingMusicMute = root.Q<Label>("label-setting-music-mute");
            labelSettingFps = root.Q<Label>("label-setting-fps");
            modalTitleSettings = root.Q<Label>("modal-title-settings");
            modalTitleSelectLevel = root.Q<Label>("modal-title-select-level");
            modalSubSelectLevel = root.Q<Label>("modal-sub-select-level");
            modalTitleCredits = root.Q<Label>("modal-title-credits");
            labelDiffSubtitle = root.Q<Label>("label-diff-subtitle");
            labelStarsSubtitle = root.Q<Label>("label-stars-subtitle");
            labelTimeSubtitle = root.Q<Label>("label-time-subtitle");

            if (btnNewGame != null) btnNewGame.clicked += HandleNewGameClicked;
            if (btnLevelSelect != null) btnLevelSelect.clicked += ShowLevelModal;
            if (btnContinue != null) btnContinue.clicked += HandleContinueClicked;
            if (btnHighscores != null) btnHighscores.clicked += ShowHighScoresModal;
            if (btnHowToPlay != null) btnHowToPlay.clicked += ShowHowToPlayModal;
            if (btnOptions != null) btnOptions.clicked += ShowOptions;
            if (btnCredits != null) btnCredits.clicked += ShowCredits;

            if (btnCloseLevelModal != null) btnCloseLevelModal.clicked += HideLevelModal;
            if (btnStartSelectedLevel != null) btnStartSelectedLevel.clicked += HandleStartSelectedLevel;

            if (btnCloseOptions != null) btnCloseOptions.clicked += HideOptions;
            if (btnLangEn != null) btnLangEn.clicked += HandleLangEnClicked;
            if (btnLangTr != null) btnLangTr.clicked += HandleLangTrClicked;

            if (btnCloseCredits != null) btnCloseCredits.clicked += HideCredits;
            if (btnCloseHighscores != null) btnCloseHighscores.clicked += HideHighScoresModal;
            if (btnResetHighscores != null) btnResetHighscores.clicked += HandleResetHighScores;
            if (btnCloseHowToPlay != null) btnCloseHowToPlay.clicked += HideHowToPlayModal;

            if (btnCreditEmail != null) btnCreditEmail.clicked += OpenEmail;
            if (btnCreditWebsite != null) btnCreditWebsite.clicked += OpenWebsite;
            if (btnCreditLinkedin != null) btnCreditLinkedin.clicked += OpenLinkedIn;
            if (btnCreditGithub != null) btnCreditGithub.clicked += OpenGitHub;

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
                    UpdateToggleMuteIcon(toggleMute, evt.newValue);
                });
            }

            if (sliderMusicVolume != null)
            {
                sliderMusicVolume.RegisterValueChangedCallback(evt =>
                {
                    if (ArcadeAudioManager.Instance != null)
                        ArcadeAudioManager.Instance.MusicVolume = evt.newValue;
                });
            }

            if (toggleMusicMute != null)
            {
                toggleMusicMute.RegisterValueChangedCallback(evt =>
                {
                    if (ArcadeAudioManager.Instance != null)
                        ArcadeAudioManager.Instance.IsMusicMuted = evt.newValue;
                    UpdateToggleMuteIcon(toggleMusicMute, evt.newValue);
                });
            }

            if (btnToggleFps != null) btnToggleFps.clicked += ToggleFpsSetting;

            LocalizationManager.OnLanguageChanged += UpdateLocalizedTexts;
            UpdateLocalizedTexts();
        }

        private void InitializeValues()
        {
            bool hasSaved = ArcadeGameManager.HasSavedGame;
            if (btnContinue != null)
            {
                btnContinue.SetEnabled(hasSaved);
                btnContinue.style.display = hasSaved ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (ArcadeAudioManager.Instance != null)
            {
                if (sliderVolume != null) sliderVolume.value = ArcadeAudioManager.Instance.Volume;
                if (toggleMute != null)
                {
                    toggleMute.value = ArcadeAudioManager.Instance.IsMuted;
                    UpdateToggleMuteIcon(toggleMute, ArcadeAudioManager.Instance.IsMuted);
                }
                if (sliderMusicVolume != null) sliderMusicVolume.value = ArcadeAudioManager.Instance.MusicVolume;
                if (toggleMusicMute != null)
                {
                    toggleMusicMute.value = ArcadeAudioManager.Instance.IsMusicMuted;
                    UpdateToggleMuteIcon(toggleMusicMute, ArcadeAudioManager.Instance.IsMusicMuted);
                }
            }

            selectedLevelNumber = PlayerPrefs.GetInt("Arcade_SelectedLevel", 1);
            SelectLevel(selectedLevelNumber, false);

            targetFps = PlayerPrefs.GetInt("Arcade_TargetFPS", 60);
            Application.targetFrameRate = targetFps;
            if (btnToggleFps != null) btnToggleFps.text = $"{targetFps} FPS";

            UpdateLocalizedTexts();
        }

        private void HandleLangEnClicked()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            LocalizationManager.SetLanguage(GameLanguage.English);
        }

        private void HandleLangTrClicked()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            LocalizationManager.SetLanguage(GameLanguage.Turkish);
        }

        private void UpdateLocalizedTexts()
        {
            bool isTr = LocalizationManager.CurrentLanguage == GameLanguage.Turkish;
            if (btnLangEn != null)
            {
                if (!isTr) btnLangEn.AddToClassList("lang-btn-active");
                else btnLangEn.RemoveFromClassList("lang-btn-active");
            }
            if (btnLangTr != null)
            {
                if (isTr) btnLangTr.AddToClassList("lang-btn-active");
                else btnLangTr.RemoveFromClassList("lang-btn-active");
            }

            if (btnNewGame != null) btnNewGame.text = LocalizationManager.Get("menu_start_new", "START NEW GAME");
            if (btnLevelSelect != null) btnLevelSelect.text = LocalizationManager.Get("menu_select_level", "SELECT LEVEL");
            if (btnContinue != null) btnContinue.text = LocalizationManager.Get("menu_continue", "CONTINUE");
            if (btnHighscores != null) btnHighscores.text = LocalizationManager.Get("menu_high_scores", "HIGH SCORES");
            if (btnHowToPlay != null) btnHowToPlay.text = LocalizationManager.Get("menu_how_to_play", "HOW TO PLAY");
            if (btnOptions != null) btnOptions.text = LocalizationManager.Get("menu_options", "OPTIONS");
            if (btnCredits != null) btnCredits.text = LocalizationManager.Get("menu_credits", "CREDITS");

            if (btnStartSelectedLevel != null) btnStartSelectedLevel.text = LocalizationManager.Get("level_play_btn", "PLAY LEVEL");
            if (btnCloseLevelModal != null) btnCloseLevelModal.text = LocalizationManager.Get("btn_back", "BACK");
            if (btnCloseOptions != null) btnCloseOptions.text = LocalizationManager.Get("btn_back", "BACK");
            if (btnCloseCredits != null) btnCloseCredits.text = LocalizationManager.Get("btn_back", "BACK");
            if (btnCloseHighscores != null) btnCloseHighscores.text = LocalizationManager.Get("btn_back", "BACK");
            if (btnCloseHowToPlay != null) btnCloseHowToPlay.text = LocalizationManager.Get("btn_back", "BACK");
            if (btnResetHighscores != null) btnResetHighscores.text = LocalizationManager.Get("btn_reset_scores", "RESET SCORES");

            if (modalTitleSettings != null) modalTitleSettings.text = LocalizationManager.Get("settings_title", "SETTINGS");
            if (labelSettingLang != null) labelSettingLang.text = LocalizationManager.Get("settings_language", "Language");
            if (labelSettingSfx != null) labelSettingSfx.text = LocalizationManager.Get("settings_sfx_vol", "SFX Volume");
            if (labelSettingSfxMute != null) labelSettingSfxMute.text = LocalizationManager.Get("settings_sfx_mute", "Mute SFX");
            if (labelSettingMusic != null) labelSettingMusic.text = LocalizationManager.Get("settings_music_vol", "Music Volume");
            if (labelSettingMusicMute != null) labelSettingMusicMute.text = LocalizationManager.Get("settings_music_mute", "Mute Music");
            if (labelSettingFps != null) labelSettingFps.text = LocalizationManager.Get("settings_target_fps", "Target Frame Rate");

            if (modalTitleSelectLevel != null) modalTitleSelectLevel.text = LocalizationManager.Get("level_select_title", "SELECT LEVEL");
            if (modalSubSelectLevel != null) modalSubSelectLevel.text = LocalizationManager.Get("level_select_subtitle", "Choose your next challenge");
            if (labelDiffSubtitle != null) labelDiffSubtitle.text = LocalizationManager.Get("level_difficulty", "DIFFICULTY");
            if (labelStarsSubtitle != null) labelStarsSubtitle.text = "STARS";
            if (labelTimeSubtitle != null) labelTimeSubtitle.text = LocalizationManager.Get("level_best_time", "BEST TIME");
            if (modalTitleCredits != null) modalTitleCredits.text = LocalizationManager.Get("credits_title", "CREDITS");

            SelectLevel(selectedLevelNumber, false);
        }

        private void SelectLevel(int levelNumber, bool playSound = true)
        {
            if (playSound && ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            selectedLevelNumber = levelNumber;

            for (int i = 0; i < menuLevelTabButtons.Count; i++)
            {
                if (i + 1 == levelNumber)
                {
                    menuLevelTabButtons[i].AddToClassList("level-tab-active");
                    menuLevelTabButtons[i].AddToClassList("level-card-selected");
                }
                else
                {
                    menuLevelTabButtons[i].RemoveFromClassList("level-tab-active");
                    menuLevelTabButtons[i].RemoveFromClassList("level-card-selected");
                }
            }

            var config = GetLevelConfig(levelNumber);
            if (config != null)
            {
                if (menuLevelName != null) menuLevelName.text = config.LevelName;
                if (menuLevelDesc != null) menuLevelDesc.text = config.Description;
            }
            else
            {
                if (menuLevelName != null) menuLevelName.text = $"Level {levelNumber}";
                if (menuLevelDesc != null) menuLevelDesc.text = "Arcade block breaker challenge.";
            }

            if (menuLevelTag != null)
            {
                menuLevelTag.text = $"LEVEL {levelNumber}";
            }

            if (menuLevelDiff != null)
            {
                if (levelNumber <= 4)
                {
                    menuLevelDiff.text = LocalizationManager.Get("diff_easy", "EASY");
                    menuLevelDiff.style.color = new StyleColor(new Color(0.15f, 0.91f, 0.52f));
                }
                else if (levelNumber <= 10)
                {
                    menuLevelDiff.text = LocalizationManager.Get("diff_medium", "MEDIUM");
                    menuLevelDiff.style.color = new StyleColor(new Color(1f, 0.67f, 0f));
                }
                else
                {
                    menuLevelDiff.text = LocalizationManager.Get("diff_hard", "HARD");
                    menuLevelDiff.style.color = new StyleColor(new Color(1f, 0.23f, 0.34f));
                }
            }

            int levelStars = HighScoreManager.GetLevelStars(levelNumber);
            float bestTime = HighScoreManager.GetLevelBestTime(levelNumber);
            if (menuLevelStars != null)
            {
                string starStr = "";
                for (int s = 0; s < 3; s++) starStr += s < levelStars ? "★" : "☆";
                menuLevelStars.text = starStr;
                menuLevelStars.style.color = levelStars > 0 ? new StyleColor(new Color(1f, 0.843f, 0f, 1f)) : new StyleColor(new Color(0.6f, 0.65f, 0.75f, 0.4f));
            }
            if (menuLevelBestTime != null)
            {
                menuLevelBestTime.text = bestTime > 0 ? HighScoreManager.FormatTime(bestTime) : "--:--";
            }
        }

        public LevelConfiguration GetLevelConfig(int levelNumber)
        {
            if (levelPresets != null && levelPresets.Length > 0)
            {
                for (int i = 0; i < levelPresets.Length; i++)
                {
                    if (levelPresets[i] != null && levelPresets[i].LevelNumber == levelNumber)
                        return levelPresets[i];
                }
            }
#if UNITY_EDITOR
            string path = $"Assets/Settings/Levels/SO_Level_{levelNumber:D2}.asset";
            var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfiguration>(path);
            if (loaded != null) return loaded;
#endif
            return null;
        }

        private void HandleNewGameClicked()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            PlayerPrefs.SetInt("Arcade_LoadSavedGameOnStart", 0);
            PlayerPrefs.SetInt("Arcade_SelectedLevel", 1);
            PlayerPrefs.Save();
            ArcadeGameManager.ClearSavedGame();
            SceneManager.LoadScene("LV_BlockBreaker");
        }

        private void HandleStartSelectedLevel()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            PlayerPrefs.SetInt("Arcade_LoadSavedGameOnStart", 0);
            PlayerPrefs.SetInt("Arcade_SelectedLevel", selectedLevelNumber);
            PlayerPrefs.Save();
            ArcadeGameManager.ClearSavedGame();
            SceneManager.LoadScene("LV_BlockBreaker");
        }

        private void HandleContinueClicked()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            PlayerPrefs.SetInt("Arcade_LoadSavedGameOnStart", 1);
            int savedLevel = PlayerPrefs.GetInt("Arcade_SavedLevel", 1);
            PlayerPrefs.SetInt("Arcade_SelectedLevel", savedLevel);
            PlayerPrefs.Save();
            SceneManager.LoadScene("LV_BlockBreaker");
        }

        private void ShowLevelModal()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (levelModal != null) levelModal.RemoveFromClassList("modal-hidden");
        }

        private void HideLevelModal()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (levelModal != null) levelModal.AddToClassList("modal-hidden");
        }

        private void ShowOptions()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (optionsModal != null) optionsModal.RemoveFromClassList("modal-hidden");
        }

        private void HideOptions()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (optionsModal != null) optionsModal.AddToClassList("modal-hidden");
        }

        private void ShowCredits()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (creditsModal != null) creditsModal.RemoveFromClassList("modal-hidden");
        }

        private void HideCredits()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (creditsModal != null) creditsModal.AddToClassList("modal-hidden");
        }

        private void ShowHighScoresModal()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            PopulateHighScoresTable();
            if (highscoresModal != null) highscoresModal.RemoveFromClassList("modal-hidden");
        }

        private void HideHighScoresModal()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (highscoresModal != null) highscoresModal.AddToClassList("modal-hidden");
        }

        private void HandleResetHighScores()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            HighScoreManager.ResetScores();
            PopulateHighScoresTable();
        }

        private void PopulateHighScoresTable()
        {
            if (root == null) return;
            var scores = HighScoreManager.GetTopScores();

            for (int i = 0; i < HighScoreManager.MAX_SCORES; i++)
            {
                var valLabel = root.Q<Label>($"score-val-{i}");
                var dateLabel = root.Q<Label>($"score-date-{i}");

                if (valLabel != null)
                {
                    if (i < scores.Count && scores[i].score > 0)
                        valLabel.text = scores[i].score.ToString("#,##0");
                    else
                        valLabel.text = "---";
                }

                if (dateLabel != null)
                {
                    if (i < scores.Count && scores[i].score > 0)
                    {
                        string timeStr = scores[i].time > 0f ? $" • {HighScoreManager.FormatTime(scores[i].time)}" : "";
                        dateLabel.text = $"L{scores[i].level}{timeStr} • {scores[i].date}";
                    }
                    else
                        dateLabel.text = "---";
                }
            }
        }

        private void ShowHowToPlayModal()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (howToPlayModal != null) howToPlayModal.RemoveFromClassList("modal-hidden");
        }

        private void HideHowToPlayModal()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (howToPlayModal != null) howToPlayModal.AddToClassList("modal-hidden");
        }

        private void OpenEmail()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            Application.OpenURL("mailto:ramin.rasulzade@gmail.com");
        }

        private void OpenWebsite()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            Application.OpenURL("https://raminrasulzade.com");
        }

        private void OpenLinkedIn()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            Application.OpenURL("https://www.linkedin.com/in/ramin-rasulzade/");
        }

        private void OpenGitHub()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            Application.OpenURL("https://github.com/raminres");
        }

        private void ToggleFpsSetting()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            targetFps = targetFps == 60 ? 120 : 60;
            Application.targetFrameRate = targetFps;
            PlayerPrefs.SetInt("Arcade_TargetFPS", targetFps);
            PlayerPrefs.Save();
            if (btnToggleFps != null) btnToggleFps.text = $"{targetFps} FPS";
        }

        public void UpdateToggleMuteIcon(Toggle toggle, bool isMuted)
        {
            if (toggle == null) return;
            var checkmark = toggle.Q(className: "unity-toggle__checkmark");
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
    }
}

