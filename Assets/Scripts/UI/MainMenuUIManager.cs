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
        private Button btnCloseOptions;
        private Button btnCloseCredits;

        // Level Select controls
        private Button btnMenuLvl1;
        private Button btnMenuLvl2;
        private Button btnMenuLvl3;
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

            if (btnMenuLvl1 != null) btnMenuLvl1.clicked -= () => SelectLevel(1);
            if (btnMenuLvl2 != null) btnMenuLvl2.clicked -= () => SelectLevel(2);
            if (btnMenuLvl3 != null) btnMenuLvl3.clicked -= () => SelectLevel(3);
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

            btnMenuLvl1 = root.Q<Button>("btn-menu-lvl-1");
            btnMenuLvl2 = root.Q<Button>("btn-menu-lvl-2");
            btnMenuLvl3 = root.Q<Button>("btn-menu-lvl-3");
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

            if (btnMenuLvl1 != null) btnMenuLvl1.clicked += () => SelectLevel(1);
            if (btnMenuLvl2 != null) btnMenuLvl2.clicked += () => SelectLevel(2);
            if (btnMenuLvl3 != null) btnMenuLvl3.clicked += () => SelectLevel(3);

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
            int high = PlayerPrefs.GetInt("Arcade_HighScore", 0);
            if (highscoreLabel != null)
            {
                highscoreLabel.text = $"ALL-TIME HIGH SCORE: {high}";
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

            if (btnMenuLvl1 != null) { if (levelNumber == 1) btnMenuLvl1.AddToClassList("level-tab-active"); else btnMenuLvl1.RemoveFromClassList("level-tab-active"); }
            if (btnMenuLvl2 != null) { if (levelNumber == 2) btnMenuLvl2.AddToClassList("level-tab-active"); else btnMenuLvl2.RemoveFromClassList("level-tab-active"); }
            if (btnMenuLvl3 != null) { if (levelNumber == 3) btnMenuLvl3.AddToClassList("level-tab-active"); else btnMenuLvl3.RemoveFromClassList("level-tab-active"); }

            switch (levelNumber)
            {
                case 1:
                    if (menuLevelName != null) menuLevelName.text = "Level 1: Classic Inverted";
                    if (menuLevelDesc != null) menuLevelDesc.text = "Classic 3-tier block setup with Red on bottom, Green in middle, and Blue on top. Features random x2 multiplier and paddle expander blocks.";
                    break;
                case 2:
                    if (menuLevelName != null) menuLevelName.text = "Level 2: Wide Grid";
                    if (menuLevelDesc != null) menuLevelDesc.text = "Wider 9-column grid with increased ball speed and dual x2 score multipliers.";
                    break;
                case 3:
                    if (menuLevelName != null) menuLevelName.text = "Level 3: Dense Gauntlet";
                    if (menuLevelDesc != null) menuLevelDesc.text = "Dense 10-column, 9-row gauntlet with fast velocity and multiple power-up blocks.";
                    break;
            }
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
