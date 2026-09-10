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
        private Button btnOptions;
        private Button btnCredits;
        private Label highscoreLabel;

        // Modals
        private VisualElement levelModal;
        private VisualElement optionsModal;
        private VisualElement creditsModal;

        private Button btnCloseLevelModal;
        private Button btnStartSelectedLevel;
        [Header("Level Presets")]
        [SerializeField] private LevelConfiguration[] levelPresets;

        private Button btnCloseOptions;
        private Button btnCloseCredits;

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
            if (btnOptions != null) btnOptions.clicked -= ShowOptions;
            if (btnCredits != null) btnCredits.clicked -= ShowCredits;

            if (btnCloseLevelModal != null) btnCloseLevelModal.clicked -= HideLevelModal;
            if (btnStartSelectedLevel != null) btnStartSelectedLevel.clicked -= HandleStartSelectedLevel;

            if (btnCloseOptions != null) btnCloseOptions.clicked -= HideOptions;
            if (btnCloseCredits != null) btnCloseCredits.clicked -= HideCredits;

            if (btnToggleFps != null) btnToggleFps.clicked -= ToggleFpsSetting;

            menuLevelTabButtons.Clear();
        }

        private void BindElements()
        {
            if (root == null) return;

            btnNewGame = root.Q<Button>("btn-new-game");
            btnLevelSelect = root.Q<Button>("btn-level-select");
            btnContinue = root.Q<Button>("btn-continue");
            btnOptions = root.Q<Button>("btn-options");
            btnCredits = root.Q<Button>("btn-credits");
            highscoreLabel = root.Q<Label>("menu-highscore-label");

            levelModal = root.Q<VisualElement>("level-modal");
            optionsModal = root.Q<VisualElement>("options-modal");
            creditsModal = root.Q<VisualElement>("credits-modal");

            btnCloseLevelModal = root.Q<Button>("btn-close-level-modal");
            btnStartSelectedLevel = root.Q<Button>("btn-start-selected-level");
            btnCloseOptions = root.Q<Button>("btn-close-options");
            btnCloseCredits = root.Q<Button>("btn-close-credits");

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
            if (btnOptions != null) btnOptions.clicked += ShowOptions;
            if (btnCredits != null) btnCredits.clicked += ShowCredits;

            if (btnCloseLevelModal != null) btnCloseLevelModal.clicked += HideLevelModal;
            if (btnStartSelectedLevel != null) btnStartSelectedLevel.clicked += HandleStartSelectedLevel;

            if (btnCloseOptions != null) btnCloseOptions.clicked += HideOptions;
            if (btnCloseCredits != null) btnCloseCredits.clicked += HideCredits;

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
            if (highscoreLabel != null)
            {
                int highscore = PlayerPrefs.GetInt("Arcade_HighScore", 0);
                highscoreLabel.text = $"ALL-TIME HIGH SCORE: {highscore}";
            }

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
