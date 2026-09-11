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
        private Label menuLevelName;
        private Label menuLevelDesc;
        private int selectedLevelNumber = 1;

        // Options controls
        private Slider sliderVolume;
        private Toggle toggleMute;
        private Button btnToggleFps;

        private int targetFps = 60;

        private void Awake()
        {
            panelRenderer = GetComponent<PanelRenderer>();
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
            var tabsContainer = root.Q<VisualElement>(className: "level-tabs-container");
            if (tabsContainer != null)
            {
                var buttons = tabsContainer.Query<Button>(className: "level-tab-btn").ToList();
                for (int i = 0; i < buttons.Count; i++)
                {
                    int lvlNum = i + 1;
                    var btn = buttons[i];
                    menuLevelTabButtons.Add(btn);
                    btn.clicked += () => SelectLevel(lvlNum);
                }
            }

            menuLevelName = root.Q<Label>("menu-level-name");
            menuLevelDesc = root.Q<Label>("menu-level-desc");

            sliderVolume = root.Q<Slider>("slider-volume");
            toggleMute = root.Q<Toggle>("toggle-mute");
            btnToggleFps = root.Q<Button>("btn-toggle-fps");

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
                });
            }

            if (btnToggleFps != null) btnToggleFps.clicked += ToggleFpsSetting;
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
                if (toggleMute != null) toggleMute.value = ArcadeAudioManager.Instance.IsMuted;
            }

            selectedLevelNumber = PlayerPrefs.GetInt("Arcade_SelectedLevel", 1);
            SelectLevel(selectedLevelNumber, false);

            targetFps = PlayerPrefs.GetInt("Arcade_TargetFPS", 60);
            Application.targetFrameRate = targetFps;
            if (btnToggleFps != null) btnToggleFps.text = $"{targetFps} FPS";
        }

        private void SelectLevel(int levelNumber, bool playSound = true)
        {
            if (playSound && ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            selectedLevelNumber = levelNumber;

            for (int i = 0; i < menuLevelTabButtons.Count; i++)
            {
                if (i + 1 == levelNumber)
                    menuLevelTabButtons[i].AddToClassList("level-tab-active");
                else
                    menuLevelTabButtons[i].RemoveFromClassList("level-tab-active");
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
                        dateLabel.text = scores[i].date;
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
    }
}
