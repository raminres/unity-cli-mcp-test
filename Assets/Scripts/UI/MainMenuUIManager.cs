using Arcade.Audio;
using Arcade.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Arcade.UI
{
    /// <summary>
    /// Coordinates UI Toolkit interactions in the fast-loading Main Menu start scene.
    /// Uses Unity 6 PanelRenderer component.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class MainMenuUIManager : MonoBehaviour
    {
        private PanelRenderer panelRenderer;
        private VisualElement root;

        private Button btnNewGame;
        private Button btnContinue;
        private Button btnOptions;
        private Button btnCredits;
        private Label highscoreLabel;

        // Modals
        private VisualElement optionsModal;
        private VisualElement creditsModal;
        private Button btnCloseOptions;
        private Button btnCloseCredits;

        // Options controls
        private Slider sliderVolume;
        private Toggle toggleMute;
        private Button btnToggleFps;

        private int targetFps = 60;

        private void OnEnable()
        {
            panelRenderer = GetComponent<PanelRenderer>();
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
            if (btnContinue != null) btnContinue.clicked -= HandleContinueClicked;
            if (btnOptions != null) btnOptions.clicked -= ShowOptions;
            if (btnCredits != null) btnCredits.clicked -= ShowCredits;

            if (btnCloseOptions != null) btnCloseOptions.clicked -= HideOptions;
            if (btnCloseCredits != null) btnCloseCredits.clicked -= HideCredits;

            if (btnToggleFps != null) btnToggleFps.clicked -= ToggleFpsSetting;
        }

        private void BindElements()
        {
            if (root == null) return;

            btnNewGame = root.Q<Button>("btn-new-game");
            btnContinue = root.Q<Button>("btn-continue");
            btnOptions = root.Q<Button>("btn-options");
            btnCredits = root.Q<Button>("btn-credits");
            highscoreLabel = root.Q<Label>("menu-highscore-label");

            optionsModal = root.Q<VisualElement>("options-modal");
            creditsModal = root.Q<VisualElement>("credits-modal");
            btnCloseOptions = root.Q<Button>("btn-close-options");
            btnCloseCredits = root.Q<Button>("btn-close-credits");

            sliderVolume = root.Q<Slider>("slider-volume");
            toggleMute = root.Q<Toggle>("toggle-mute");
            btnToggleFps = root.Q<Button>("btn-toggle-fps");

            if (btnNewGame != null) btnNewGame.clicked += HandleNewGameClicked;
            if (btnContinue != null) btnContinue.clicked += HandleContinueClicked;
            if (btnOptions != null) btnOptions.clicked += ShowOptions;
            if (btnCredits != null) btnCredits.clicked += ShowCredits;

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

            targetFps = PlayerPrefs.GetInt("Arcade_TargetFPS", 60);
            Application.targetFrameRate = targetFps;
            if (btnToggleFps != null) btnToggleFps.text = $"{targetFps} FPS";
        }

        private void HandleNewGameClicked()
        {
            PlayerPrefs.SetInt("Arcade_LoadSavedGameOnStart", 0);
            ArcadeGameManager.ClearSavedGame();
            SceneManager.LoadScene("LV_BlockBreaker");
        }

        private void HandleContinueClicked()
        {
            PlayerPrefs.SetInt("Arcade_LoadSavedGameOnStart", 1);
            SceneManager.LoadScene("LV_BlockBreaker");
        }

        private void ShowOptions()
        {
            if (optionsModal != null) optionsModal.RemoveFromClassList("modal-hidden");
        }

        private void HideOptions()
        {
            if (optionsModal != null) optionsModal.AddToClassList("modal-hidden");
        }

        private void ShowCredits()
        {
            if (creditsModal != null) creditsModal.RemoveFromClassList("modal-hidden");
        }

        private void HideCredits()
        {
            if (creditsModal != null) creditsModal.AddToClassList("modal-hidden");
        }

        private void ToggleFpsSetting()
        {
            targetFps = targetFps == 60 ? 120 : 60;
            Application.targetFrameRate = targetFps;
            PlayerPrefs.SetInt("Arcade_TargetFPS", targetFps);
            PlayerPrefs.Save();
            if (btnToggleFps != null) btnToggleFps.text = $"{targetFps} FPS";
        }
    }
}
