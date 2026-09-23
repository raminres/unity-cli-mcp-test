using Arcade.Audio;
using Arcade.BlockBreaker;
using Arcade.Core;
using Arcade.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;
using UnityEditor.Localization;

namespace Arcade.Tests
{
    [TestFixture]
    public class DeviceUIAndLocalizationTests
    {
        [TearDown]
        public void Teardown()
        {
            LocalizationManager.SetLanguage(GameLanguage.English);
        }

        [Test]
        public void LocalizationManager_DefaultLanguage_IsEnglish()
        {
            LocalizationManager.SetLanguage(GameLanguage.English);
            Assert.AreEqual(GameLanguage.English, LocalizationManager.CurrentLanguage);
            Assert.AreEqual("OPTIONS", LocalizationManager.Get("menu_options"));
            Assert.AreEqual("TAP TO LAUNCH", LocalizationManager.Get("hud_tap_launch"));
            Assert.AreEqual("SELECT LEVEL", LocalizationManager.Get("level_select_title"));
        }

        [Test]
        public void LocalizationManager_SetLanguage_SwitchesToTurkishAndFiresEvent()
        {
            bool eventFired = false;

            System.Action handler = () =>
            {
                eventFired = true;
            };

            LocalizationManager.OnLanguageChanged += handler;
            LocalizationManager.SetLanguage(GameLanguage.Turkish);
            LocalizationManager.OnLanguageChanged -= handler;

            Assert.IsTrue(eventFired, "OnLanguageChanged event must fire when language changes.");
            Assert.AreEqual(GameLanguage.Turkish, LocalizationManager.CurrentLanguage);

            Assert.AreEqual("AYARLAR", LocalizationManager.Get("menu_options"));
            Assert.AreEqual("FIRLATMAK İÇİN DOKUN", LocalizationManager.Get("hud_tap_launch"));
            Assert.AreEqual("BÖLÜM SEÇ", LocalizationManager.Get("level_select_title"));
            Assert.AreEqual("YAPIMCILAR", LocalizationManager.Get("credits_title"));
        }

        [Test]
        public void LocalizationTables_AssetsExistOnDisk_AndAreConfiguredCorrectly()
        {
            var enLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/en.asset");
            var trLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/tr.asset");

            Assert.IsNotNull(enLocale, "en.asset must exist in Assets/Localization/Locales/");
            Assert.IsNotNull(trLocale, "tr.asset must exist in Assets/Localization/Locales/");
            Assert.AreEqual("en", enLocale.Identifier.Code);
            Assert.AreEqual("tr", trLocale.Identifier.Code);

            var collection = AssetDatabase.LoadAssetAtPath<StringTableCollection>("Assets/Localization/Tables/ArcadeTable.asset");
            Assert.IsNotNull(collection, "ArcadeTable.asset must exist in Assets/Localization/Tables/");

            var enTable = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/Tables/ArcadeTable_en.asset");
            var trTable = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/Tables/ArcadeTable_tr.asset");

            Assert.IsNotNull(enTable, "ArcadeTable_en.asset must exist in Assets/Localization/Tables/");
            Assert.IsNotNull(trTable, "ArcadeTable_tr.asset must exist in Assets/Localization/Tables/");

            Assert.IsTrue(enTable.Count >= 45, $"English table should have at least 45 entries, has {enTable.Count}");
            Assert.IsTrue(trTable.Count >= 45, $"Turkish table should have at least 45 entries, has {trTable.Count}");
        }

        [Test]
        public void LocalizationManager_ReadsDirectlyFromTable_AndAllowsChangingTranslations()
        {
            LocalizationManager.LoadTables();
            Assert.IsNotNull(LocalizationManager.EnglishTable, "LocalizationManager.EnglishTable must be loaded.");
            Assert.IsNotNull(LocalizationManager.TurkishTable, "LocalizationManager.TurkishTable must be loaded.");

            // Verify table values for English
            LocalizationManager.SetLanguage(GameLanguage.English);
            Assert.AreEqual(LocalizationManager.EnglishTable.GetEntry("menu_options")?.Value, LocalizationManager.Get("menu_options"));

            // Verify table values for Turkish
            LocalizationManager.SetLanguage(GameLanguage.Turkish);
            Assert.AreEqual(LocalizationManager.TurkishTable.GetEntry("menu_options")?.Value, LocalizationManager.Get("menu_options"));

            // Verify that modifying an entry in the Turkish table immediately affects LocalizationManager.Get
            var trEntry = LocalizationManager.TurkishTable.GetEntry("hud_tap_launch");
            Assert.IsNotNull(trEntry);
            string originalValue = trEntry.Value;

            try
            {
                trEntry.Value = "TEST_TURKISH_CUSTOM_LAUNCH";
                Assert.AreEqual("TEST_TURKISH_CUSTOM_LAUNCH", LocalizationManager.Get("hud_tap_launch"),
                    "LocalizationManager.Get must dynamically reflect modifications made to the Turkish StringTable!");
            }
            finally
            {
                trEntry.Value = originalValue;
            }
        }

        [Test]
        public void ResponsiveCameraController_TabletAspect_CalculatesDistanceCorrectly()
        {
            var go = new GameObject("TestCamera", typeof(Camera), typeof(ResponsiveCameraController));
            var cam = go.GetComponent<Camera>();
            var controller = go.GetComponent<ResponsiveCameraController>();

            cam.fieldOfView = 38f;

            float width = controller.BoundsSize.x + controller.Padding;
            float height = controller.BoundsSize.y + controller.Padding;

            // iPad Mini 6th gen aspect: 1488 x 2266 ~= 0.6566
            float tabletAspect = 1488f / 2266f;
            float tabletDistance = ResponsiveCameraController.CalculateRequiredDistance(width, height + 7.5f, cam.fieldOfView, tabletAspect);

            // Phone aspect: 1179 x 2556 ~= 0.4612
            float phoneAspect = 1179f / 2556f;
            float phoneDistance = ResponsiveCameraController.CalculateRequiredDistance(width, height, cam.fieldOfView, phoneAspect);

            float unpaddedTabletDistance = ResponsiveCameraController.CalculateRequiredDistance(width, height, cam.fieldOfView, tabletAspect);

            Assert.IsTrue(tabletDistance >= 60f, $"Tablet distance ({tabletDistance}) should pull camera back to allow HUD pods clearance.");
            Assert.IsTrue(tabletDistance > unpaddedTabletDistance, $"Padded tablet distance ({tabletDistance}) must provide more clearance than unpadded tablet distance ({unpaddedTabletDistance}).");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ArcadeAudioManager_MusicControls_SetVolumeAndMuteCorrectly()
        {
            var go = new GameObject("TestAudioManager", typeof(ArcadeAudioManager));
            var audioMgr = go.GetComponent<ArcadeAudioManager>();

            bool volumeChanged = false;
            bool muteChanged = false;

            audioMgr.OnMusicVolumeChanged += _ => volumeChanged = true;
            audioMgr.OnMusicMuteToggled += _ => muteChanged = true;

            audioMgr.MusicVolume = 0.65f;
            Assert.IsTrue(volumeChanged, "OnMusicVolumeChanged should fire.");
            Assert.AreEqual(0.65f, audioMgr.MusicVolume, 0.001f);

            audioMgr.IsMusicMuted = true;
            Assert.IsTrue(muteChanged, "OnMusicMuteToggled should fire.");
            Assert.IsTrue(audioMgr.IsMusicMuted);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SafeAreaController_ProvidesDynamicIslandExtraTopPadding()
        {
            var go = new GameObject("TestSafeArea", typeof(PanelRenderer), typeof(SafeAreaController));
            var controller = go.GetComponent<SafeAreaController>();

            Assert.IsNotNull(controller);
            Assert.AreEqual(4.5f, controller.ExtraTopPercent, "Extra top percent should default to 4.5% for Dynamic Island clearance.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SafeAreaController_CalculateYogaInsets_ComputesWidthRelativePercentagesAccurately()
        {
            // iPhone 15 Pro resolution: 1179 x 2556
            // Dynamic Island cutout: 177px, bottom home indicator: 102px
            float screenW = 1179f;
            float screenH = 2556f;
            float topInset = 177f;
            float bottomInset = 102f;
            Rect safeArea = new Rect(0f, bottomInset, screenW, screenH - (topInset + bottomInset));

            var (left, right, top, bottom) = SafeAreaController.CalculateYogaInsets(safeArea, screenW, screenH, extraSide: 0f, extraTop: 4.5f, extraBottom: 2.5f);

            Assert.AreEqual(0f, left, 0.01f);
            Assert.AreEqual(0f, right, 0.01f);
            Assert.AreEqual((bottomInset / screenW) * 100f + 2.5f, bottom, 0.01f);
            Assert.AreEqual((topInset / screenW) * 100f + 4.5f, top, 0.01f);
        }

        [Test]
        public void BlockBreakerHUD_UXML_WrapsTopBarInSafeAreaContent()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsNotNull(uxml, "BlockBreakerHUD.uxml must exist.");

            var root = uxml.Instantiate();
            var safeAreaContent = root.Q("safe-area-content");
            Assert.IsNotNull(safeAreaContent, "BlockBreakerHUD.uxml must contain 'safe-area-content' wrapper.");

            var topBar = safeAreaContent.Q("top-bar");
            Assert.IsNotNull(topBar, "'top-bar' must be inside 'safe-area-content'.");

            var powerupContainer = safeAreaContent.Q("powerup-status-container");
            Assert.IsNotNull(powerupContainer, "'powerup-status-container' must be inside 'safe-area-content'.");

            // Modals must remain outside safe-area-content for 100% full-bleed backdrop coverage
            var pauseModal = root.Q("pause-modal");
            Assert.IsNotNull(pauseModal, "pause-modal must exist in visual tree.");
            Assert.IsNull(safeAreaContent.Q("pause-modal"), "pause-modal must NOT be inside safe-area-content (must remain full bleed).");
        }

        [Test]
        public void BlockBreakerHUD_UXML_ContainsMinimalTouchGuidelineWithIgnorePicking()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsNotNull(uxml, "BlockBreakerHUD.uxml must exist.");

            var root = uxml.Instantiate();
            var guideline = root.Q("touch-guideline");
            Assert.IsNotNull(guideline, "BlockBreakerHUD.uxml must contain 'touch-guideline' visual element.");
            Assert.AreEqual(PickingMode.Ignore, guideline.pickingMode, "touch-guideline must have pickingMode=Ignore so it never blocks gameplay input.");
        }

        [Test]
        public void PlayerSettings_iOS_DefersSystemGesturesModeToPreventInputLoss()
        {
            Assert.AreEqual(UnityEngine.iOS.SystemGestureDeferMode.All, UnityEditor.PlayerSettings.iOS.deferSystemGesturesMode,
                "PlayerSettings.iOS.deferSystemGesturesMode must be set to All so iOS does not swallow bottom edge drag inputs.");
        }

        [Test]
        public void CustomIcons_AllRequiredPngAssetsExistOnDisk()
        {
            string[] requiredIcons = new string[]
            {
                "Assets/UI/Icons/TX_Icon_Finger.png",
                "Assets/UI/Icons/TX_Icon_Arrow_Left.png",
                "Assets/UI/Icons/TX_Icon_Arrow_Right.png",
                "Assets/UI/Icons/TX_Flag_GB.png",
                "Assets/UI/Icons/TX_Flag_TR.png",
                "Assets/UI/Icons/TX_Icon_Mail.png",
                "Assets/UI/Icons/TX_Icon_Web.png",
                "Assets/UI/Icons/TX_Icon_LinkedIn.png",
                "Assets/UI/Icons/TX_Icon_GitHub.png"
            };

            foreach (var path in requiredIcons)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.IsNotNull(texture, $"Icon asset must exist and load at: {path}");
            }
        }

        [Test]
        public void BlockBreakerHUD_UXML_ContainsSelectLevelAndCreditsModalsAndButtons()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsNotNull(uxml, "BlockBreakerHUD.uxml should exist and load as VisualTreeAsset.");

            var root = uxml.CloneTree();
            var pauseModal = root.Q<VisualElement>("pause-modal");
            Assert.IsNotNull(pauseModal, "pause-modal must exist.");

            var btnLevelSelect = pauseModal.Q<Button>("btn-level-select-pause");
            Assert.IsNotNull(btnLevelSelect, "pause-modal must contain btn-level-select-pause.");

            var btnCredits = pauseModal.Q<Button>("btn-credits-pause");
            Assert.IsNotNull(btnCredits, "pause-modal must contain btn-credits-pause.");

            var howToPlayModal = root.Q<VisualElement>("how-to-play-modal");
            Assert.IsNotNull(howToPlayModal, "how-to-play-modal must exist in HUD.");

            var creditsModal = root.Q<VisualElement>("credits-modal");
            Assert.IsNotNull(creditsModal, "credits-modal must exist in HUD.");

            var levelModal = root.Q<VisualElement>("level-modal");
            Assert.IsNotNull(levelModal, "level-modal must exist in HUD.");
        }

        [Test]
        public void ArcadeUIManager_CreditsAndLevelModals_LifecycleAndVisibility()
        {
            var uiObj = new GameObject("UI_HUD_Test", typeof(PanelRenderer), typeof(ArcadeUIManager));
            var uiMgr = uiObj.GetComponent<ArcadeUIManager>();

            var root = new VisualElement();
            var pauseModal = new VisualElement { name = "pause-modal" };
            var creditsModal = new VisualElement { name = "credits-modal" };
            var levelModal = new VisualElement { name = "level-modal" };
            var howToPlayModal = new VisualElement { name = "how-to-play-modal" };

            pauseModal.AddToClassList("modal-hidden");
            creditsModal.AddToClassList("modal-hidden");
            levelModal.AddToClassList("modal-hidden");
            howToPlayModal.AddToClassList("modal-hidden");

            root.Add(pauseModal);
            root.Add(creditsModal);
            root.Add(levelModal);
            root.Add(howToPlayModal);

            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(ArcadeUIManager).GetField("root", flags).SetValue(uiMgr, root);
            typeof(ArcadeUIManager).GetField("pauseModal", flags).SetValue(uiMgr, pauseModal);
            typeof(ArcadeUIManager).GetField("creditsModal", flags).SetValue(uiMgr, creditsModal);
            typeof(ArcadeUIManager).GetField("levelModal", flags).SetValue(uiMgr, levelModal);
            typeof(ArcadeUIManager).GetField("howToPlayModal", flags).SetValue(uiMgr, howToPlayModal);

            Assert.IsFalse(uiMgr.IsAnyModalVisible());

            // Credits modal
            uiMgr.ShowCredits();
            Assert.IsTrue(uiMgr.IsAnyModalVisible(), "IsAnyModalVisible should be true when credits modal is shown.");
            Assert.IsFalse(creditsModal.ClassListContains("modal-hidden"));
            uiMgr.HideCredits();
            Assert.IsTrue(creditsModal.ClassListContains("modal-hidden"));

            // Level modal
            uiMgr.ShowLevelModal();
            Assert.IsTrue(uiMgr.IsAnyModalVisible(), "IsAnyModalVisible should be true when level modal is shown.");
            Assert.IsFalse(levelModal.ClassListContains("modal-hidden"));
            uiMgr.HideLevelModal();
            Assert.IsTrue(levelModal.ClassListContains("modal-hidden"));

            // How To Play modal
            uiMgr.ShowHowToPlay();
            Assert.IsTrue(uiMgr.IsAnyModalVisible(), "IsAnyModalVisible should be true when how to play modal is shown.");
            Assert.IsFalse(howToPlayModal.ClassListContains("modal-hidden"));
            uiMgr.HideHowToPlay();
            Assert.IsTrue(howToPlayModal.ClassListContains("modal-hidden"));

            Object.DestroyImmediate(uiObj);
        }

        [Test]
        public void OptionsModal_MainMenuAndHUD_ShareIdenticalStructureAndFlagPresentation()
        {
            var menuUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/MainMenuUI.uxml");
            var hudUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");

            Assert.IsNotNull(menuUxml);
            Assert.IsNotNull(hudUxml);

            var menuRoot = menuUxml.CloneTree();
            var hudRoot = hudUxml.CloneTree();

            var menuOptions = menuRoot.Q<VisualElement>("options-modal");
            var hudOptions = hudRoot.Q<VisualElement>("options-modal");

            Assert.IsNotNull(menuOptions, "MainMenuUI must contain options-modal.");
            Assert.IsNotNull(hudOptions, "BlockBreakerHUD must contain options-modal.");

            // Both must have flags with no text labels (pure flag icons)
            foreach (var root in new[] { menuRoot, hudRoot })
            {
                var enBtn = root.Q<Button>("btn-lang-en");
                var trBtn = root.Q<Button>("btn-lang-tr");

                Assert.IsNotNull(enBtn);
                Assert.IsNotNull(trBtn);

                Assert.IsNotNull(enBtn.Q(className: "flag-gb"));
                Assert.IsNotNull(trBtn.Q(className: "flag-tr"));

                Assert.IsNull(enBtn.Q<Label>(), "EN button should not contain text Label, displaying flag icon only.");
                Assert.IsNull(trBtn.Q<Label>(), "TR button should not contain text Label, displaying flag icon only.");

                // Both must have SFX and Music mute toggles with setting-toggle class
                var sfxToggle = root.Q<Toggle>("toggle-mute");
                var musicToggle = root.Q<Toggle>("toggle-music-mute");

                Assert.IsNotNull(sfxToggle);
                Assert.IsNotNull(musicToggle);
                Assert.IsTrue(sfxToggle.ClassListContains("setting-toggle"));
                Assert.IsTrue(musicToggle.ClassListContains("setting-toggle"));

                // Both must have sliders and fps toggle
                Assert.IsNotNull(root.Q<Slider>("slider-volume"));
                Assert.IsNotNull(root.Q<Slider>("slider-music-volume"));
                Assert.IsNotNull(root.Q<Button>("btn-toggle-fps"));
                Assert.IsNotNull(root.Q<Button>("btn-close-options"));
            }
        }

        [Test]
        public void MainMenuUIManager_And_ArcadeUIManager_UpdateToggleMuteIconCorrectly()
        {
            var menuObj = new GameObject("TestMainMenuMgr", typeof(PanelRenderer), typeof(MainMenuUIManager));
            var menuMgr = menuObj.GetComponent<MainMenuUIManager>();

            var hudObj = new GameObject("TestArcadeUIMgr", typeof(PanelRenderer), typeof(ArcadeUIManager));
            var hudMgr = hudObj.GetComponent<ArcadeUIManager>();

            var toggle = new Toggle();
            var checkmark = toggle.Q(className: "unity-toggle__checkmark");
            if (checkmark == null)
            {
                checkmark = new VisualElement();
                checkmark.AddToClassList("unity-toggle__checkmark");
                toggle.Add(checkmark);
            }

            // Test unmuted state (#21d4fd: r=0.13, g=0.83, b=0.99)
            menuMgr.UpdateToggleMuteIcon(toggle, false);
            Assert.AreEqual(new Color(0.13f, 0.83f, 0.99f, 1f), checkmark.style.unityBackgroundImageTintColor.value);


            // Test muted state (#ff3b56: r=1.0, g=0.231, b=0.337)
            menuMgr.UpdateToggleMuteIcon(toggle, true);
            Assert.AreEqual(new Color(1f, 0.231f, 0.337f, 1f), checkmark.style.unityBackgroundImageTintColor.value);

            // Test ArcadeUIManager unmuted
            hudMgr.UpdateToggleMuteIcon(toggle, false);
            Assert.AreEqual(new Color(0.13f, 0.83f, 0.99f, 1f), checkmark.style.unityBackgroundImageTintColor.value);

            // Test ArcadeUIManager muted
            hudMgr.UpdateToggleMuteIcon(toggle, true);
            Assert.AreEqual(new Color(1f, 0.231f, 0.337f, 1f), checkmark.style.unityBackgroundImageTintColor.value);

            Object.DestroyImmediate(menuObj);
            Object.DestroyImmediate(hudObj);
        }

        [Test]
        public void PauseModal_ExtrudedGradientStyles_AreScopedAndPresent()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsNotNull(uxml, "BlockBreakerHUD.uxml must exist.");
            var root = uxml.CloneTree();

            var pauseModal = root.Q<VisualElement>("pause-modal");
            Assert.IsNotNull(pauseModal, "pause-modal must exist.");

            var pauseCard = pauseModal.Q<VisualElement>(className: "pause-card");
            Assert.IsNotNull(pauseCard, "pause-card must exist in pause-modal.");
            Assert.IsTrue(pauseCard.ClassListContains("modal-card"), "pause-card should also have modal-card class.");

            // Verify children buttons exist
            Assert.IsNotNull(pauseCard.Q<Button>("btn-resume"), "btn-resume must exist.");
            Assert.IsNotNull(pauseCard.Q<Button>("btn-level-select-pause"), "btn-level-select-pause must exist.");
            Assert.IsNotNull(pauseCard.Q<Button>("btn-options-pause"), "btn-options-pause must exist.");

            // Verify stylesheet contains scoped extruded 3D gradient rules
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI/BlockBreakerHUD.uss");
            Assert.IsNotNull(uss, "BlockBreakerHUD.uss must exist.");
            var ussText = System.IO.File.ReadAllText("Assets/UI/BlockBreakerHUD.uss");

            Assert.IsTrue(ussText.Contains("#pause-modal.modal-backdrop"), "USS must contain scoped #pause-modal backdrop styling.");
            Assert.IsTrue(ussText.Contains(".pause-card {"), "USS must contain .pause-card styling.");
            Assert.IsTrue(ussText.Contains("rgb(18, 24, 40)"), "pause-card must have solid opaque background.");
            Assert.IsTrue(ussText.Contains("TX_Grad_Card_Bg.png"), "pause-card must use card gradient texture.");
            Assert.IsTrue(ussText.Contains("border-bottom-width: 7px;"), "arcade-btn must have 3D extruded shelf.");
            Assert.IsTrue(ussText.Contains(".pause-card .btn-primary"), "pause-card must have scoped primary button.");
            Assert.IsTrue(ussText.Contains("TX_Grad_Ruby_Btn.png"), "primary button must use ruby gradient texture.");
            Assert.IsTrue(ussText.Contains(".pause-card .btn-secondary"), "pause-card must have scoped secondary button.");
            Assert.IsTrue(ussText.Contains("TX_Grad_Emerald_Btn.png"), "secondary button must use emerald gradient texture.");
            Assert.IsTrue(ussText.Contains(".pause-card .btn-default"), "pause-card must have scoped default button.");
            Assert.IsTrue(ussText.Contains("TX_Grad_Titanium_Btn.png"), "default button must use titanium gradient texture.");

            // Verify other modals do not use pause-card class
            var highscoresModal = root.Q<VisualElement>("highscores-modal");
            Assert.IsNotNull(highscoresModal);
            Assert.IsNull(highscoresModal.Q<VisualElement>(className: "pause-card"), "highscores-modal must NOT use pause-card class.");
        }

        [Test]
        public void Step4_FirstThreeLevels_HaveEnlargedBlockScale_AndValidWallClearance()
        {
            // First 3 levels have +20% block scale (1.20f) for beginner onboarding & touch comfort
            for (int i = 1; i <= 3; i++)
            {
                var config = AssetDatabase.LoadAssetAtPath<LevelConfiguration>($"Assets/Settings/Levels/SO_Level_{i:D2}.asset");
                Assert.IsNotNull(config, $"SO_Level_{i:D2} must exist.");
                Assert.AreEqual(1.20f, config.BlockSize, 0.001f, $"Level {i} must have +20% enlarged block size of 1.20f.");
                
                // Arena walls are at X = +/- 10.25. Base block mesh half-width is 1.15f / 2 = 0.575f.
                float scaledHalfWidth = (1.15f * config.BlockSize) * 0.5f;
                float halfGridSpan = (config.Columns - 1) * config.HorizontalSpacing * 0.5f;
                float maxBlockX = halfGridSpan + scaledHalfWidth;

                // Wall clearance must be strictly greater than 1.0 unit
                float wallX = 10.25f;
                float clearance = wallX - maxBlockX;
                Assert.Greater(clearance, 1.0f, $"Level {i} outer block edge (X={maxBlockX:F2}) must maintain > 1.0u clearance to wall (X={wallX}). Clearance was {clearance:F2}u.");
            }

            // Levels 4 through 15 retain standard 1.0f scale
            for (int i = 4; i <= 15; i++)
            {
                var config = AssetDatabase.LoadAssetAtPath<LevelConfiguration>($"Assets/Settings/Levels/SO_Level_{i:D2}.asset");
                Assert.IsNotNull(config, $"SO_Level_{i:D2} must exist.");
                Assert.AreEqual(1.00f, config.BlockSize, 0.001f, $"Level {i} must retain standard block size of 1.00f.");
            }
        }

        [Test]
        public void Step3_UIPropagation_ModalsAndButtons_HaveExtrudedStylesAndOpaqueGradients()
        {
            // Verify MainMenuUI.uss propagation
            var mainMenuUss = System.IO.File.ReadAllText("Assets/UI/MainMenuUI.uss");
            Assert.IsTrue(mainMenuUss.Contains(".modal-card {"), "MainMenuUI.uss must define .modal-card.");
            Assert.IsTrue(mainMenuUss.Contains(".level-modal-card {"), "MainMenuUI.uss must define .level-modal-card.");
            Assert.IsTrue(mainMenuUss.Contains("TX_Grad_Card_Bg.png"), "MainMenuUI.uss must use card gradient texture.");
            Assert.IsTrue(mainMenuUss.Contains("border-bottom-width: 7px;"), "MainMenuUI.uss arcade-button must have 7px extruded shelf.");
            Assert.IsTrue(mainMenuUss.Contains("TX_Grad_Ruby_Btn.png"), "MainMenuUI.uss must use ruby button gradient.");
            Assert.IsTrue(mainMenuUss.Contains("TX_Grad_Emerald_Btn.png"), "MainMenuUI.uss must use emerald button gradient.");
            Assert.IsTrue(mainMenuUss.Contains("TX_Grad_Titanium_Btn.png"), "MainMenuUI.uss must use titanium button gradient.");

            // Verify BlockBreakerHUD.uss propagation
            var hudUss = System.IO.File.ReadAllText("Assets/UI/BlockBreakerHUD.uss");
            Assert.IsTrue(hudUss.Contains(".modal-card {"), "BlockBreakerHUD.uss must define .modal-card.");
            Assert.IsTrue(hudUss.Contains(".level-modal-card {"), "BlockBreakerHUD.uss must define .level-modal-card.");
            Assert.IsTrue(hudUss.Contains(".scorecard-card {"), "BlockBreakerHUD.uss must define .scorecard-card.");
            Assert.IsTrue(hudUss.Contains(".warning-btn {"), "BlockBreakerHUD.uss must define .warning-btn.");
            Assert.IsTrue(hudUss.Contains("border-bottom-width: 6px;"), "Modals must have 6px bottom beveled border.");
        }
    }
}


