using System.IO;
using Arcade.Audio;
using Arcade.BlockBreaker;
using Arcade.Core;
using Arcade.Input;
using Arcade.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace Arcade.Editor
{
    public static class SetupBlockBreakerScenes
    {
        [MenuItem("Tools/Arcade/Setup All Block Breaker Scenes")]
        public static void SetupAllScenes()
        {
            CreateOrUpdateLevelPresets();
            BuildMainMenuScene();
            BuildGameplayScene();
            ConfigureBuildSettings();
            Debug.Log("<color=green>Block Breaker scenes constructed and registered successfully!</color>");
        }

        public static void CreateOrUpdateLevelPresets()
        {
            if (!Directory.Exists("Assets/Settings/Levels"))
            {
                Directory.CreateDirectory("Assets/Settings/Levels");
            }

            // Level 1: First Flight
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_01.asset", 1, "Level 1: First Flight",
                "Gentle warmup grid with comfortable ball speed. Introduces the Paddle Expander to widen your paddle and master bounce angles.",
                BlockColorPattern.InvertedTiered, 5, 1, 0.85f, 5.5f,
                mult2x: 0, mult3x: 0, expanders: 1, bombs: 0, glass: 0, heart: 0, shield: 0, multiBall: 0, shieldDuration: 10f);

            // Level 2: Glass & Gold
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_02.asset", 2, "Level 2: Glass & Gold",
                "Introduce durable glass-encased bricks requiring two strikes and score multiplier targets for big points.",
                BlockColorPattern.InvertedTiered, 6, 1, 0.95f, 5.0f,
                mult2x: 1, mult3x: 0, expanders: 1, bombs: 0, glass: 2, heart: 0, shield: 0, multiBall: 0, shieldDuration: 10f);

            // Level 3: Chain Reaction
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_03.asset", 3, "Level 3: Chain Reaction",
                "Denser checkerboard formation introducing explosive Bomb bricks. Trigger cascading perimeter blasts to clear columns rapidly.",
                BlockColorPattern.Checkerboard, 7, 2, 1.05f, 5.0f,
                mult2x: 2, mult3x: 0, expanders: 1, bombs: 2, glass: 0, heart: 0, shield: 0, multiBall: 0, shieldDuration: 10f);

            // Level 4: Kinetic Aegis
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_04.asset", 4, "Level 4: Kinetic Aegis",
                "Ball velocity surges. Deploy the bottom laser Shield power-up for a 10-second safety net, and collect extra heart lives.",
                BlockColorPattern.InvertedTiered, 8, 2, 1.15f, 5.0f,
                mult2x: 1, mult3x: 0, expanders: 1, bombs: 1, glass: 2, heart: 1, shield: 1, multiBall: 0, shieldDuration: 10f);

            // Level 5: Multi-Ball Mayhem
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_05.asset", 5, "Level 5: Multi-Ball Mayhem",
                "High-octane arcade juggling! Trigger Multi-Ball to split into 3 active balls simultaneously, supported by emergency shields.",
                BlockColorPattern.Checkerboard, 8, 2, 1.20f, 5.0f,
                mult2x: 1, mult3x: 0, expanders: 0, bombs: 1, glass: 2, heart: 0, shield: 1, multiBall: 2, shieldDuration: 10f);

            // Level 6: The High Roller
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_06.asset", 6, "Level 6: The High Roller",
                "High stakes, blistering velocity, and the rare x3 score multiplier awarding 90 points on blue bricks. Precision is key.",
                BlockColorPattern.InvertedTiered, 9, 2, 1.28f, 5.0f,
                mult2x: 2, mult3x: 1, expanders: 1, bombs: 2, glass: 3, heart: 1, shield: 1, multiBall: 1, shieldDuration: 10f);

            // Level 7: Arcade Chaos Gauntlet
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_07.asset", 7, "Level 7: Arcade Chaos Gauntlet",
                "The 90-block grand climax! Fully randomized neon patterns, top velocity, and a non-stop orchestra of bombs, multi-balls, shields, and multipliers.",
                BlockColorPattern.Randomized, 10, 3, 1.38f, 5.0f,
                mult2x: 2, mult3x: 2, expanders: 2, bombs: 3, glass: 4, heart: 1, shield: 2, multiBall: 2, shieldDuration: 10f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateOrConfigureLevel(string path, int levelNumber, string name, string desc,
            BlockColorPattern pattern, int cols, int rowsPerTier, float speed, float paddleWidth,
            int mult2x, int mult3x, int expanders, int bombs, int glass, int heart, int shield, int multiBall, float shieldDuration = 10f)
        {
            var config = AssetDatabase.LoadAssetAtPath<LevelConfiguration>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<LevelConfiguration>();
                AssetDatabase.CreateAsset(config, path);
            }

            var so = new SerializedObject(config);
            so.FindProperty("levelNumber").intValue = levelNumber;
            so.FindProperty("levelName").stringValue = name;
            so.FindProperty("levelDescription").stringValue = desc;
            so.FindProperty("colorPattern").enumValueIndex = (int)pattern;
            so.FindProperty("columns").intValue = cols;
            so.FindProperty("rowsPerTier").intValue = rowsPerTier;
            so.FindProperty("blockSize").floatValue = 1.0f;
            so.FindProperty("horizontalSpacing").floatValue = 1.25f;
            so.FindProperty("verticalSpacing").floatValue = 1.3f;
            so.FindProperty("startCenterY").floatValue = 15.5f;
            so.FindProperty("ballSpeedMultiplier").floatValue = speed;
            so.FindProperty("initialPaddleWidth").floatValue = paddleWidth;
            so.FindProperty("multiplier2xCount").intValue = mult2x;
            so.FindProperty("multiplier3xCount").intValue = mult3x;
            so.FindProperty("paddleExpanderCount").intValue = expanders;
            so.FindProperty("bombCount").intValue = bombs;
            so.FindProperty("glassEnclosedCount").intValue = glass;
            so.FindProperty("extraHeartCount").intValue = heart;
            so.FindProperty("shieldCount").intValue = shield;
            so.FindProperty("multiBallCount").intValue = multiBall;
            so.FindProperty("shieldDuration").floatValue = shieldDuration;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
        }

        public static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "LV_BlockBreaker_MainMenu";

            // 1. Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.047f, 0.051f, 0.086f, 1f); // #0c0d16
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

            // 2. Audio Manager (Persistent)
            var audioGo = new GameObject("AudioManager");
            ConfigureAudioManager(audioGo);

            // 3. UI Panel Renderer & Manager
            var uiGo = new GameObject("UI_MainMenu");
            var panelRenderer = uiGo.AddComponent<PanelRenderer>();
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/MainMenuUI.uxml");
            if (uxml != null) panelRenderer.visualTreeAsset = uxml;
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/ArcadePanelSettings.asset");
            if (panelSettings != null) panelRenderer.panelSettings = panelSettings;
            var menuMgr = uiGo.AddComponent<MainMenuUIManager>();
            var menuSo = new SerializedObject(menuMgr);
            var menuPresetsProp = menuSo.FindProperty("levelPresets");
            menuPresetsProp.arraySize = 7;
            for (int i = 0; i < 7; i++)
            {
                var lvl = AssetDatabase.LoadAssetAtPath<LevelConfiguration>($"Assets/Settings/Levels/SO_Level_{i + 1:D2}.asset");
                menuPresetsProp.GetArrayElementAtIndex(i).objectReferenceValue = lvl;
            }
            menuSo.ApplyModifiedProperties();
            uiGo.AddComponent<SafeAreaController>();

            // Save scene
            var path = "Assets/Scenes/LV_BlockBreaker_MainMenu.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("Created Main Menu scene at: " + path);
        }

        public static void BuildGameplayScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "LV_BlockBreaker";

            // 1. Perspective Camera with narrow FOV for subtle 3D depth effect
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.047f, 0.051f, 0.086f, 1f); // #0c0d16
            cam.fieldOfView = 38f; // Perspective with tactile 3D depth and complete arena framing
            cam.transform.position = new Vector3(0f, 6.0f, -32f);
            cam.transform.rotation = Quaternion.identity;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<ResponsiveCameraController>();

            // 2. Studio Lighting
            var keyLightGo = new GameObject("Key Light");
            var keyLight = keyLightGo.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1.0f, 0.92f, 0.78f, 1f);
            keyLight.intensity = 1.6f;
            keyLight.shadows = LightShadows.Soft;
            keyLightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            var fillLightGo = new GameObject("Fill Light");
            var fillLight = fillLightGo.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.35f, 0.65f, 1.0f, 1f);
            fillLight.intensity = 0.8f;
            fillLight.shadows = LightShadows.None;
            fillLightGo.transform.rotation = Quaternion.Euler(30f, 140f, 0f);

            // 3. Post-Processing Volume with Bloom
            var volGo = new GameObject("Global Volume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
            if (profile != null)
            {
                vol.sharedProfile = profile;
                if (profile.TryGet<Bloom>(out var bloom))
                {
                    bloom.intensity.value = 1.3f;
                    bloom.threshold.value = 0.85f;
                    bloom.scatter.value = 0.7f;
                }
            }

            // 4. PhysicMaterial for bouncy, frictionless ball bounces
            var bounceMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/BlockBreaker/PM_ArcadeBounce.physicMaterial");
            if (bounceMat == null)
            {
                bounceMat = new PhysicsMaterial("PM_ArcadeBounce")
                {
                    bounciness = 1.0f,
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Maximum
                };
                AssetDatabase.CreateAsset(bounceMat, "Assets/Materials/BlockBreaker/PM_ArcadeBounce.physicMaterial");
            }

            // 5. Materials
            var borderMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Playfield_Border.mat");
            var paddleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Paddle.mat");
            var ballMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Ball.mat");
            var redMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Red.mat");
            var greenMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Green.mat");
            var blueMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Blue.mat");
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/VFX/VFX_BlockShatter.vfx");
            var debrisMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Debris.mat");

            // 6. Playfield Boundaries
            var boundariesRoot = new GameObject("Boundaries");

            // Left Wall
            var leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWall";
            leftWall.transform.SetParent(boundariesRoot.transform);
            leftWall.transform.position = new Vector3(-10.25f, 8.5f, 0f);
            leftWall.transform.localScale = new Vector3(0.5f, 32f, 2f);
            if (borderMat != null) leftWall.GetComponent<MeshRenderer>().sharedMaterial = borderMat;
            leftWall.GetComponent<BoxCollider>().sharedMaterial = bounceMat;

            // Right Wall
            var rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "RightWall";
            rightWall.transform.SetParent(boundariesRoot.transform);
            rightWall.transform.position = new Vector3(10.25f, 8.5f, 0f);
            rightWall.transform.localScale = new Vector3(0.5f, 32f, 2f);
            if (borderMat != null) rightWall.GetComponent<MeshRenderer>().sharedMaterial = borderMat;
            rightWall.GetComponent<BoxCollider>().sharedMaterial = bounceMat;

            // Top Wall
            var topWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topWall.name = "TopWall";
            topWall.transform.SetParent(boundariesRoot.transform);
            topWall.transform.position = new Vector3(0f, 24.25f, 0f);
            topWall.transform.localScale = new Vector3(21f, 0.5f, 2f);
            if (borderMat != null) topWall.GetComponent<MeshRenderer>().sharedMaterial = borderMat;
            topWall.GetComponent<BoxCollider>().sharedMaterial = bounceMat;

            // Bottom Kill Zone Trigger
            var killZone = new GameObject("KillZone");
            killZone.tag = "KillZone";
            killZone.AddComponent<KillZone>();
            killZone.transform.SetParent(boundariesRoot.transform);
            killZone.transform.position = new Vector3(0f, -9.0f, 0f);
            var killCol = killZone.AddComponent<BoxCollider>();
            killCol.size = new Vector3(24f, 2.0f, 4f);
            killCol.isTrigger = true;

            // 7. Paddle Platform (5:1 ratio)
            var paddleGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paddleGo.name = "Paddle";
            paddleGo.transform.position = new Vector3(0f, -6.5f, 0f);
            paddleGo.transform.localScale = new Vector3(5.0f, 1.0f, 1.0f); // 5:1 ratio
            if (paddleMat != null) paddleGo.GetComponent<MeshRenderer>().sharedMaterial = paddleMat;
            paddleGo.GetComponent<BoxCollider>().sharedMaterial = bounceMat;
            var paddleRb = paddleGo.AddComponent<Rigidbody>();
            paddleRb.isKinematic = true;
            var paddleCtrl = paddleGo.AddComponent<PaddleController>();

            // 8. Ball (Sphere)
            var ballGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballGo.name = "Ball";
            ballGo.transform.position = new Vector3(0f, -5.65f, 0f);
            ballGo.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            if (ballMat != null) ballGo.GetComponent<MeshRenderer>().sharedMaterial = ballMat;
            var ballCol = ballGo.GetComponent<SphereCollider>();
            ballCol.sharedMaterial = bounceMat;
            var ballRb = ballGo.AddComponent<Rigidbody>();
            ballRb.useGravity = false;
            ballRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            ballRb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            var ballCtrl = ballGo.AddComponent<BallController>();

            // Wire BallController to Paddle
            var ballSo = new SerializedObject(ballCtrl);
            ballSo.FindProperty("paddle").objectReferenceValue = paddleCtrl;
            ballSo.FindProperty("rb").objectReferenceValue = ballRb;
            ballSo.ApplyModifiedProperties();

            // 9. Audio Manager (Fallback if entering gameplay directly)
            var audioGo = new GameObject("AudioManager");
            ConfigureAudioManager(audioGo);

            // 10. Game Coordinators
            var gmGo = new GameObject("GameManager");
            gmGo.AddComponent<ArcadeGameManager>();
            gmGo.AddComponent<ArcadeInputHandler>();

            var vfxMgr = gmGo.AddComponent<BlockVFXManager>();
            var vfxSo = new SerializedObject(vfxMgr);
            if (vfxAsset != null) vfxSo.FindProperty("shatterVfxAsset").objectReferenceValue = vfxAsset;
            if (debrisMat != null) vfxSo.FindProperty("debrisMaterial").objectReferenceValue = debrisMat;
            vfxSo.ApplyModifiedProperties();

            var levelGen = gmGo.AddComponent<LevelGenerator>();
            var levelSo = new SerializedObject(levelGen);
            levelSo.FindProperty("matRedBlock").objectReferenceValue = redMat;
            levelSo.FindProperty("matGreenBlock").objectReferenceValue = greenMat;
            levelSo.FindProperty("matBlueBlock").objectReferenceValue = blueMat;
            var glassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Glass.mat");
            if (glassMat != null) levelSo.FindProperty("matGlass").objectReferenceValue = glassMat;

            var presetsProp = levelSo.FindProperty("levelPresets");
            presetsProp.arraySize = 7;
            for (int i = 0; i < 7; i++)
            {
                var lvl = AssetDatabase.LoadAssetAtPath<LevelConfiguration>($"Assets/Settings/Levels/SO_Level_{i + 1:D2}.asset");
                presetsProp.GetArrayElementAtIndex(i).objectReferenceValue = lvl;
            }

            var badgeSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/BlockWorldPanelSettings.asset");
            var badgeUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BlockBadgeUI.uxml");
            levelSo.FindProperty("badgePanelSettings").objectReferenceValue = badgeSettings;
            levelSo.FindProperty("badgeVisualTreeAsset").objectReferenceValue = badgeUxml;
            levelSo.ApplyModifiedProperties();

            // 11. In-Game UI Panel Renderer & Manager
            var uiGo = new GameObject("UI_HUD");
            var panelRenderer = uiGo.AddComponent<PanelRenderer>();
            var hudUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            if (hudUxml != null) panelRenderer.visualTreeAsset = hudUxml;
            var hudPanelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/ArcadePanelSettings.asset");
            if (hudPanelSettings != null) panelRenderer.panelSettings = hudPanelSettings;
            var uiMgr = uiGo.AddComponent<ArcadeUIManager>();
            var uiMgrSo = new SerializedObject(uiMgr);
            var heartFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Heart_Fill.png");
            var heartEmpty = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Heart_Empty.png");
            if (heartFill != null) uiMgrSo.FindProperty("heartFillSprite").objectReferenceValue = heartFill;
            if (heartEmpty != null) uiMgrSo.FindProperty("heartEmptySprite").objectReferenceValue = heartEmpty;

            var lvlIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Level_Settings.png");
            var muteIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Volume_Mute.png");
            var volUpIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Volume_Up.png");
            var setIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Settings.png");
            var pauseIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Pause.png");
            var playIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Play.png");

            if (lvlIcon != null) uiMgrSo.FindProperty("levelSettingsSprite").objectReferenceValue = lvlIcon;
            if (muteIcon != null) uiMgrSo.FindProperty("volumeMuteSprite").objectReferenceValue = muteIcon;
            if (volUpIcon != null) uiMgrSo.FindProperty("volumeUpSprite").objectReferenceValue = volUpIcon;
            if (setIcon != null) uiMgrSo.FindProperty("settingsSprite").objectReferenceValue = setIcon;
            if (pauseIcon != null) uiMgrSo.FindProperty("pauseSprite").objectReferenceValue = pauseIcon;
            if (playIcon != null) uiMgrSo.FindProperty("playSprite").objectReferenceValue = playIcon;

            var shieldIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Shield.png");
            var multiBallIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Multi_Ball.png");
            if (shieldIcon != null) uiMgrSo.FindProperty("shieldSprite").objectReferenceValue = shieldIcon;
            if (multiBallIcon != null) uiMgrSo.FindProperty("multiBallSprite").objectReferenceValue = multiBallIcon;

            uiMgrSo.ApplyModifiedProperties();
            uiGo.AddComponent<SafeAreaController>();

            // Save Scene
            var path = "Assets/Scenes/LV_BlockBreaker.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("Created Gameplay scene at: " + path);
        }

        public static void ConfigureBuildSettings()
        {
            var menuScenePath = "Assets/Scenes/LV_BlockBreaker_MainMenu.unity";
            var gameScenePath = "Assets/Scenes/LV_BlockBreaker.unity";

            var scenes = new[]
            {
                new EditorBuildSettingsScene(menuScenePath, true),
                new EditorBuildSettingsScene(gameScenePath, true)
            };

            EditorBuildSettings.scenes = scenes;
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureAudioManager(GameObject audioGo)
        {
            var audioMgr = audioGo.AddComponent<ArcadeAudioManager>();
            var audioSo = new SerializedObject(audioMgr);

            var popClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Pop.mp3");
            var breakClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Break.mp3");
            var powerupClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Powerup.mp3");
            var gameOverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Game_Over.mp3");
            var levelSuccessClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Level_Success.mp3");
            var buttonPressClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Button_Press.mp3");
            var lifeLostClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Life_Lost.mp3");
            var shieldDeflectClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Powerup_Shield.mp3");

            if (popClip != null) audioSo.FindProperty("clipPop").objectReferenceValue = popClip;
            if (breakClip != null) audioSo.FindProperty("clipBreak").objectReferenceValue = breakClip;
            if (powerupClip != null) audioSo.FindProperty("clipPowerup").objectReferenceValue = powerupClip;
            if (gameOverClip != null) audioSo.FindProperty("clipGameOver").objectReferenceValue = gameOverClip;
            if (levelSuccessClip != null) audioSo.FindProperty("clipLevelSuccess").objectReferenceValue = levelSuccessClip;
            if (buttonPressClip != null) audioSo.FindProperty("clipButtonPress").objectReferenceValue = buttonPressClip;
            if (lifeLostClip != null) audioSo.FindProperty("clipLifeLost").objectReferenceValue = lifeLostClip;
            if (shieldDeflectClip != null) audioSo.FindProperty("clipShieldDeflect").objectReferenceValue = shieldDeflectClip;

            if (popClip != null)
            {
                audioSo.FindProperty("clipPaddleBounce").objectReferenceValue = popClip;
                audioSo.FindProperty("clipWallBounce").objectReferenceValue = popClip;
            }
            if (breakClip != null)
            {
                audioSo.FindProperty("clipBlockHitRed").objectReferenceValue = breakClip;
                audioSo.FindProperty("clipBlockHitGreen").objectReferenceValue = breakClip;
                audioSo.FindProperty("clipBlockHitBlue").objectReferenceValue = breakClip;
            }
            if (levelSuccessClip != null) audioSo.FindProperty("clipLevelClear").objectReferenceValue = levelSuccessClip;

            audioSo.ApplyModifiedProperties();
        }
    }
}
