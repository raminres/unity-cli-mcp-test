using Arcade.Audio;
using Arcade.Core;
using Arcade.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
    }
}
