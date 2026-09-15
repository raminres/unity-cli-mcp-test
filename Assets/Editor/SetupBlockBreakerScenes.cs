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
            GetOrCreatePowerupIconSet();
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

            // Level 1: First Flight (Pyramid)
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_01.asset", 1, "Level 1: First Flight",
                "Gentle warmup stepped pyramid with comfortable ball speed. Introduces the Paddle Expander to widen your paddle and master bounce angles.",
                BlockColorPattern.InvertedTiered, LevelLayoutType.Pyramid, 7, 1, 0.92f, 5.5f,
                mult2x: 0, mult3x: 0, mult4x: 0, mult5x: 0, expanders: 1, bombs: 0, glass: 0, heart: 0, shield: 0, multiBall: 0,
                laser: 0, customLayout: null, shieldDuration: 10f,
                parTime: 30f, timeBonusMax: 2500, starThresholds: new[] { 300, 800, 1400 });

            // Level 2: Glass & Gold (Diamond)
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_02.asset", 2, "Level 2: Glass & Gold",
                "A sparkling diamond gem formation. Introduces durable glass-encased bricks requiring two strikes and score multiplier targets for big points.",
                BlockColorPattern.InvertedTiered, LevelLayoutType.Diamond, 7, 2, 0.96f, 5.2f,
                mult2x: 1, mult3x: 0, mult4x: 0, mult5x: 0, expanders: 1, bombs: 0, glass: 2, heart: 0, shield: 0, multiBall: 0,
                laser: 0, customLayout: null, shieldDuration: 10f,
                parTime: 35f, timeBonusMax: 3000, starThresholds: new[] { 600, 1400, 2400 });

            // Level 3: Twin Pillars (Pillars)
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_03.asset", 3, "Level 3: Twin Pillars",
                "Vertical block columns with open alleyways. Sneak the ball up the corridors for high-velocity top-row cascades, and trigger explosive Bomb bricks.",
                BlockColorPattern.Checkerboard, LevelLayoutType.Pillars, 7, 2, 1.00f, 5.0f,
                mult2x: 2, mult3x: 0, mult4x: 0, mult5x: 0, expanders: 1, bombs: 2, glass: 0, heart: 0, shield: 0, multiBall: 0,
                laser: 1, customLayout: null, shieldDuration: 10f,
                parTime: 40f, timeBonusMax: 3000, starThresholds: new[] { 900, 2000, 3200 });

            // Level 4: Kinetic Shield (Shield)
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_04.asset", 4, "Level 4: Kinetic Shield",
                "An imposing heraldic crest shield. Deploy the bottom laser Shield power-up for a 10-second safety net, and collect extra heart lives.",
                BlockColorPattern.InvertedTiered, LevelLayoutType.Shield, 8, 2, 1.04f, 5.0f,
                mult2x: 1, mult3x: 0, mult4x: 0, mult5x: 0, expanders: 1, bombs: 1, glass: 2, heart: 1, shield: 1, multiBall: 0,
                laser: 0, customLayout: null, shieldDuration: 10f,
                parTime: 45f, timeBonusMax: 3500, starThresholds: new[] { 1200, 2800, 4200 });

            // Level 5: Multi-Ball Ring (HollowBox)
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_05.asset", 5, "Level 5: Multi-Ball Ring",
                "A perimeter fortress framing a hollow bouncing chamber. Trigger Multi-Ball to unleash 3 balls ricocheting inside the inner sanctum!",
                BlockColorPattern.Checkerboard, LevelLayoutType.HollowBox, 8, 2, 1.08f, 5.0f,
                mult2x: 1, mult3x: 1, mult4x: 0, mult5x: 0, expanders: 0, bombs: 1, glass: 2, heart: 0, shield: 1, multiBall: 2,
                laser: 0, customLayout: null, shieldDuration: 10f,
                parTime: 40f, timeBonusMax: 3500, starThresholds: new[] { 1600, 3600, 5500 });

            // Level 6: Royal Crown (Crown)
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_06.asset", 6, "Level 6: Royal Crown",
                "A triple-peaked royal crown with blazing 3X score multipliers and reinforced glass towers. Precision rebounds at high angles are rewarded.",
                BlockColorPattern.InvertedTiered, LevelLayoutType.Crown, 9, 2, 1.12f, 5.0f,
                mult2x: 1, mult3x: 2, mult4x: 0, mult5x: 0, expanders: 1, bombs: 2, glass: 3, heart: 1, shield: 1, multiBall: 1,
                laser: 1, customLayout: null, shieldDuration: 10f,
                parTime: 55f, timeBonusMax: 4000, starThresholds: new[] { 2200, 5000, 7500 });

            // Level 7: Neon Heart (Heart)
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_07.asset", 7, "Level 7: Neon Heart",
                "An arcade heart silhouette with extra heart drops. Keep the rhythm alive as ball velocity continues to accelerate.",
                BlockColorPattern.InvertedTiered, LevelLayoutType.Heart, 9, 2, 1.16f, 5.0f,
                mult2x: 2, mult3x: 1, mult4x: 0, mult5x: 0, expanders: 1, bombs: 1, glass: 2, heart: 2, shield: 1, multiBall: 1,
                laser: 0, customLayout: null, shieldDuration: 10f,
                parTime: 45f, timeBonusMax: 4000, starThresholds: new[] { 2800, 6200, 9200 });

            // Level 8: Space Invader (Invader)
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_08.asset", 8, "Level 8: Space Invader",
                "Retro 8-bit space invader alien silhouette! Battle past explosive perimeter bombs and collect 3X multipliers while juggling multi-balls.",
                BlockColorPattern.Randomized, LevelLayoutType.Invader, 9, 2, 1.20f, 5.0f,
                mult2x: 2, mult3x: 2, mult4x: 0, mult5x: 0, expanders: 1, bombs: 2, glass: 2, heart: 1, shield: 1, multiBall: 1,
                laser: 1, customLayout: null, shieldDuration: 10f,
                parTime: 45f, timeBonusMax: 4500, starThresholds: new[] { 3500, 7500, 11000 });

            // Level 9: Crossfire (Cross)
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_09.asset", 9, "Level 9: Crossfire",
                "Four intersecting firing arms meet at an explosive nexus. Introduces the rare 4X score multiplier and devastating bomb chain reactions.",
                BlockColorPattern.Checkerboard, LevelLayoutType.Cross, 9, 2, 1.24f, 5.0f,
                mult2x: 2, mult3x: 1, mult4x: 1, mult5x: 0, expanders: 1, bombs: 2, glass: 3, heart: 1, shield: 1, multiBall: 1,
                laser: 0, customLayout: null, shieldDuration: 10f,
                parTime: 50f, timeBonusMax: 4500, starThresholds: new[] { 4200, 9000, 13000 });

            // Level 10: The Hourglass (Hourglass)
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_10.asset", 10, "Level 10: The Hourglass",
                "A high-tension hourglass funnel pinching the center. Thread the ball through the bottleneck to sweep out the upper chamber.",
                BlockColorPattern.InvertedTiered, LevelLayoutType.Hourglass, 9, 2, 1.28f, 5.0f,
                mult2x: 2, mult3x: 2, mult4x: 1, mult5x: 0, expanders: 1, bombs: 2, glass: 3, heart: 1, shield: 1, multiBall: 1,
                laser: 0, customLayout: null, shieldDuration: 10f,
                parTime: 55f, timeBonusMax: 5000, starThresholds: new[] { 5000, 10500, 15000 });

            // Level 11: Chevron Strike (Chevron)
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_11.asset", 11, "Level 11: Chevron Strike",
                "Aggressive forward arrowhead formation. Deflect angled strikes cleanly and ride high-velocity multi-ball surges.",
                BlockColorPattern.Checkerboard, LevelLayoutType.Chevron, 9, 2, 1.32f, 5.0f,
                mult2x: 2, mult3x: 2, mult4x: 1, mult5x: 0, expanders: 1, bombs: 2, glass: 3, heart: 1, shield: 1, multiBall: 2,
                laser: 1, customLayout: null, shieldDuration: 10f,
                parTime: 40f, timeBonusMax: 5000, starThresholds: new[] { 6000, 12000, 17500 });

            // Level 12: Castle Bastion (Castle)
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_12.asset", 12, "Level 12: Castle Bastion",
                "Fortified medieval battlements with reinforced twin watchtower turrets, glass fortifications, and explosive armory vaults.",
                BlockColorPattern.InvertedTiered, LevelLayoutType.Castle, 10, 2, 1.36f, 5.0f,
                mult2x: 2, mult3x: 2, mult4x: 2, mult5x: 0, expanders: 2, bombs: 3, glass: 4, heart: 1, shield: 2, multiBall: 2,
                laser: 0, customLayout: null, shieldDuration: 10f,
                parTime: 60f, timeBonusMax: 5500, starThresholds: new[] { 7000, 14000, 20000 });

            // Level 13: Quantum Lattice (CheckerboardEmpty)
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_13.asset", 13, "Level 13: Quantum Lattice",
                "A 50% density quantum lattice mesh with high-frequency empty gaps. Unlocks the supreme 5X combo multiplier!",
                BlockColorPattern.Randomized, LevelLayoutType.CheckerboardEmpty, 10, 2, 1.40f, 5.0f,
                mult2x: 2, mult3x: 2, mult4x: 2, mult5x: 1, expanders: 2, bombs: 3, glass: 4, heart: 1, shield: 2, multiBall: 2,
                laser: 0, customLayout: null, shieldDuration: 10f,
                parTime: 50f, timeBonusMax: 5500, starThresholds: new[] { 8200, 16000, 23000 });

            // Level 14: Striped Vault (Stripes)
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_14.asset", 14, "Level 14: Striped Vault",
                "Horizontal clearance tiers separating fortified block bands. Precision bank shots between layers rack up massive scores with 5X multipliers.",
                BlockColorPattern.InvertedTiered, LevelLayoutType.Stripes, 10, 2, 1.44f, 5.0f,
                mult2x: 2, mult3x: 2, mult4x: 2, mult5x: 2, expanders: 2, bombs: 3, glass: 4, heart: 1, shield: 2, multiBall: 2,
                laser: 1, customLayout: null, shieldDuration: 10f,
                parTime: 55f, timeBonusMax: 6000, starThresholds: new[] { 9500, 18500, 26500 });

            // Level 15: Chaos Labyrinth (Custom)
            string lvl15Custom =
                "XXXXXXXXXX\n" +
                "X..XX..XX.\n" +
                "XXXX.XXXXX\n" +
                "X..XX..XX.\n" +
                "XXXX.XXXXX\n" +
                "XXXXXXXXXX";
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_15.asset", 15, "Level 15: Chaos Labyrinth",
                "The ultimate 15-level grand climax! A master custom-authored ASCII labyrinth with tactical pockets, top velocity, and the entire powerup arsenal.",
                BlockColorPattern.Randomized, LevelLayoutType.Custom, 10, 2, 1.48f, 5.0f,
                mult2x: 2, mult3x: 2, mult4x: 2, mult5x: 2, expanders: 2, bombs: 4, glass: 4, heart: 2, shield: 2, multiBall: 2,
                laser: 2, customLayout: lvl15Custom, shieldDuration: 10f,
                parTime: 65f, timeBonusMax: 7000, starThresholds: new[] { 11000, 22000, 31000 });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateOrConfigureLevel(string path, int levelNumber, string name, string desc,
            BlockColorPattern pattern, LevelLayoutType layoutType, int cols, int rowsPerTier, float speed, float paddleWidth,
            int mult2x, int mult3x, int mult4x, int mult5x, int expanders, int bombs, int glass, int heart, int shield, int multiBall,
            int laser = 0, string customLayout = null, float shieldDuration = 10f,
            float parTime = 40f, int timeBonusMax = 3000, int[] starThresholds = null)
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
            so.FindProperty("layoutType").enumValueIndex = (int)layoutType;
            so.FindProperty("columns").intValue = cols;
            so.FindProperty("rowsPerTier").intValue = rowsPerTier;
            so.FindProperty("blockSize").floatValue = 1.0f;
            so.FindProperty("horizontalSpacing").floatValue = 1.25f;
            so.FindProperty("verticalSpacing").floatValue = 1.3f;
            so.FindProperty("startCenterY").floatValue = 15.5f;
            so.FindProperty("ballSpeedMultiplier").floatValue = speed;
            so.FindProperty("initialPaddleWidth").floatValue = paddleWidth;
            so.FindProperty("customLayout").stringValue = customLayout ?? string.Empty;
            so.FindProperty("multiplier2xCount").intValue = mult2x;
            so.FindProperty("multiplier3xCount").intValue = mult3x;
            so.FindProperty("multiplier4xCount").intValue = mult4x;
            so.FindProperty("multiplier5xCount").intValue = mult5x;
            so.FindProperty("paddleExpanderCount").intValue = expanders;
            so.FindProperty("bombCount").intValue = bombs;
            so.FindProperty("glassEnclosedCount").intValue = glass;
            so.FindProperty("extraHeartCount").intValue = heart;
            so.FindProperty("shieldCount").intValue = shield;
            so.FindProperty("multiBallCount").intValue = multiBall;
            var laserProp = so.FindProperty("laserCount");
            if (laserProp != null) laserProp.intValue = laser;
            so.FindProperty("shieldDuration").floatValue = shieldDuration;

            var parProp = so.FindProperty("parTime");
            if (parProp != null) parProp.floatValue = parTime;
            var bonusProp = so.FindProperty("timeBonusMax");
            if (bonusProp != null) bonusProp.intValue = timeBonusMax;
            if (starThresholds != null && starThresholds.Length == 3)
            {
                var starsProp = so.FindProperty("starThresholds");
                if (starsProp != null && starsProp.isArray)
                {
                    starsProp.arraySize = 3;
                    for (int s = 0; s < 3; s++)
                    {
                        starsProp.GetArrayElementAtIndex(s).intValue = starThresholds[s];
                    }
                }
            }

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
            menuPresetsProp.arraySize = 15;
            for (int i = 0; i < 15; i++)
            {
                var lvl = AssetDatabase.LoadAssetAtPath<LevelConfiguration>($"Assets/Settings/Levels/SO_Level_{i + 1:D2}.asset");
                menuPresetsProp.GetArrayElementAtIndex(i).objectReferenceValue = lvl;
            }
            menuSo.ApplyModifiedProperties();
            uiGo.AddComponent<SafeAreaController>();

            // 4. Background Cosmic Gradient Quad
            var bgGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgGo.name = "Background_Plane";
            bgGo.transform.position = new Vector3(0f, 0f, 5.0f);
            bgGo.transform.localScale = new Vector3(40f, 80f, 1f);
            Object.DestroyImmediate(bgGo.GetComponent<Collider>());
            var bgMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Background_Gradient.mat");
            if (bgMat != null) bgGo.GetComponent<MeshRenderer>().sharedMaterial = bgMat;
            var bgCtrl = bgGo.AddComponent<LevelBackgroundController>();
            ConfigureBackgroundTextures(bgCtrl);

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

            // 4. Background Cosmic Gradient Quad
            var bgGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgGo.name = "Background_Plane";
            bgGo.transform.position = new Vector3(0f, 8.5f, 6.0f);
            bgGo.transform.localScale = new Vector3(40f, 80f, 1f);
            Object.DestroyImmediate(bgGo.GetComponent<Collider>());
            var bgMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Background_Gradient.mat");
            if (bgMat != null) bgGo.GetComponent<MeshRenderer>().sharedMaterial = bgMat;
            var bgCtrl = bgGo.AddComponent<LevelBackgroundController>();
            ConfigureBackgroundTextures(bgCtrl);

            // 5. PhysicMaterial for bouncy, frictionless ball bounces
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
            var paddleDeckMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Paddle_Deck.mat");
            var paddleMidMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Paddle.mat");
            var paddleCoreMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Paddle_Core.mat");
            var ballMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Ball.mat");
            var trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_BallTrail.mat");
            var redMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Red.mat");
            var greenMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Green.mat");
            var blueMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Blue.mat");
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/VFX/VFX_BlockShatter.vfx");
            var debrisMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Debris.mat");
            var burstMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_VFX_Burst.mat");

            // 6. Playfield Boundaries
            var boundariesRoot = new GameObject("Boundaries");

            // Left Wall (shortened to 30.2 to join continuous 45° corner chamfer)
            var leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWall";
            leftWall.transform.SetParent(boundariesRoot.transform);
            leftWall.transform.position = new Vector3(-10.25f, 7.60f, 0f);
            leftWall.transform.localScale = new Vector3(0.5f, 30.2f, 2f);
            if (borderMat != null) leftWall.GetComponent<MeshRenderer>().sharedMaterial = borderMat;
            leftWall.GetComponent<BoxCollider>().sharedMaterial = bounceMat;

            // Right Wall (shortened to 30.2 to join continuous 45° corner chamfer)
            var rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "RightWall";
            rightWall.transform.SetParent(boundariesRoot.transform);
            rightWall.transform.position = new Vector3(10.25f, 7.60f, 0f);
            rightWall.transform.localScale = new Vector3(0.5f, 30.2f, 2f);
            if (borderMat != null) rightWall.GetComponent<MeshRenderer>().sharedMaterial = borderMat;
            rightWall.GetComponent<BoxCollider>().sharedMaterial = bounceMat;

            // Top Wall (shortened to 17.4 to join continuous 45° corner chamfers)
            var topWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topWall.name = "TopWall";
            topWall.transform.SetParent(boundariesRoot.transform);
            topWall.transform.position = new Vector3(0f, 24.25f, 0f);
            topWall.transform.localScale = new Vector3(17.4f, 0.5f, 2f);
            if (borderMat != null) topWall.GetComponent<MeshRenderer>().sharedMaterial = borderMat;
            topWall.GetComponent<BoxCollider>().sharedMaterial = bounceMat;

            // Top-Left 45° Corner Chamfer (redirects vertical balls diagonally into playfield, continuous perimeter)
            var chamferTopLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chamferTopLeft.name = "Chamfer_TopLeft";
            chamferTopLeft.transform.SetParent(boundariesRoot.transform);
            chamferTopLeft.transform.position = new Vector3(-9.40f, 23.40f, 0f);
            chamferTopLeft.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            chamferTopLeft.transform.localScale = new Vector3(2.5f, 0.5f, 2f);
            if (borderMat != null) chamferTopLeft.GetComponent<MeshRenderer>().sharedMaterial = borderMat;
            chamferTopLeft.GetComponent<BoxCollider>().sharedMaterial = bounceMat;

            // Top-Right 45° Corner Chamfer (redirects vertical balls diagonally into playfield, continuous perimeter)
            var chamferTopRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chamferTopRight.name = "Chamfer_TopRight";
            chamferTopRight.transform.SetParent(boundariesRoot.transform);
            chamferTopRight.transform.position = new Vector3(9.40f, 23.40f, 0f);
            chamferTopRight.transform.rotation = Quaternion.Euler(0f, 0f, -45f);
            chamferTopRight.transform.localScale = new Vector3(2.5f, 0.5f, 2f);
            if (borderMat != null) chamferTopRight.GetComponent<MeshRenderer>().sharedMaterial = borderMat;
            chamferTopRight.GetComponent<BoxCollider>().sharedMaterial = bounceMat;

            // Bottom Kill Zone Trigger
            var killZone = new GameObject("KillZone");
            killZone.tag = "KillZone";
            killZone.AddComponent<KillZone>();
            killZone.transform.SetParent(boundariesRoot.transform);
            killZone.transform.position = new Vector3(0f, -9.0f, 0f);
            var killCol = killZone.AddComponent<BoxCollider>();
            killCol.size = new Vector3(24f, 2.0f, 4f);
            killCol.isTrigger = true;

            // 7. Paddle Platform (Inverted Stepped Pyramid / Trapezoid)
            var paddleGo = new GameObject("Paddle");
            paddleGo.transform.position = new Vector3(0f, -6.5f, 0f);
            paddleGo.transform.localScale = new Vector3(5.0f, 1.0f, 1.0f);

            var paddleCol = paddleGo.AddComponent<BoxCollider>();
            paddleCol.center = new Vector3(0f, 0.38f, 0f);
            paddleCol.size = new Vector3(1.0f, 0.24f, 2.8f);
            paddleCol.sharedMaterial = bounceMat;

            var paddleRb = paddleGo.AddComponent<Rigidbody>();
            paddleRb.isKinematic = true;
            paddleRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            // Tier 1: Top Strike Deck (100% width, H = 0.24, Z = 1.0)
            var stepTopGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stepTopGo.name = "Step_Top";
            stepTopGo.transform.SetParent(paddleGo.transform, false);
            stepTopGo.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            stepTopGo.transform.localScale = new Vector3(1.0f, 0.24f, 1.0f);
            if (paddleDeckMat != null) stepTopGo.GetComponent<MeshRenderer>().sharedMaterial = paddleDeckMat;
            Object.DestroyImmediate(stepTopGo.GetComponent<Collider>());

            // Tier 2: Mid Chassis (72% width, H = 0.20, Z = 0.88)
            var stepMidGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stepMidGo.name = "Step_Mid";
            stepMidGo.transform.SetParent(paddleGo.transform, false);
            stepMidGo.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            stepMidGo.transform.localScale = new Vector3(0.72f, 0.20f, 0.88f);
            if (paddleMidMat != null) stepMidGo.GetComponent<MeshRenderer>().sharedMaterial = paddleMidMat;
            Object.DestroyImmediate(stepMidGo.GetComponent<Collider>());

            // Tier 3: Bottom Keel / Thrusters (44% width, H = 0.16, Z = 0.72)
            var stepBottomGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stepBottomGo.name = "Step_Bottom";
            stepBottomGo.transform.SetParent(paddleGo.transform, false);
            stepBottomGo.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            stepBottomGo.transform.localScale = new Vector3(0.44f, 0.16f, 0.72f);
            if (paddleCoreMat != null) stepBottomGo.GetComponent<MeshRenderer>().sharedMaterial = paddleCoreMat;
            Object.DestroyImmediate(stepBottomGo.GetComponent<Collider>());

            var paddleCtrl = paddleGo.AddComponent<PaddleController>();
            paddleCtrl.EnsureSteppedMeshHierarchy();
            var laserCtrl = paddleGo.AddComponent<PaddleLaserController>();
            var beamMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_LaserHyperBeam.mat");
            if (beamMat != null)
            {
                var laserSo = new SerializedObject(laserCtrl);
                laserSo.FindProperty("hyperBeamMaterialAsset").objectReferenceValue = beamMat;
                laserSo.ApplyModifiedProperties();
            }

            // 8. Ball (Sphere) with Dual-Layer Trail
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
            
            var ballTrail = ballGo.AddComponent<BallTrail>();
            if (trailMat != null)
            {
                ballTrail.Initialize(trailMat, new Color(0f, 0.95f, 1f, 1f));
            }
            var ballCtrl = ballGo.AddComponent<BallController>();

            // Wire BallController to Paddle and BallTrail
            var ballSo = new SerializedObject(ballCtrl);
            ballSo.FindProperty("paddle").objectReferenceValue = paddleCtrl;
            ballSo.FindProperty("rb").objectReferenceValue = ballRb;
            ballSo.FindProperty("ballTrail").objectReferenceValue = ballTrail;
            ballSo.ApplyModifiedProperties();

            // 9. Audio Manager (Fallback if entering gameplay directly)
            var audioGo = new GameObject("AudioManager");
            ConfigureAudioManager(audioGo);

            // 10. Game Coordinators
            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<ArcadeGameManager>();
            var capsuleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Powerup_Capsule.mat");
            if (capsuleMat != null)
            {
                var gmSo = new SerializedObject(gm);
                gmSo.FindProperty("powerupCapsuleMaterial").objectReferenceValue = capsuleMat;
                gmSo.ApplyModifiedProperties();
            }
            gmGo.AddComponent<ArcadeInputHandler>();

            var vfxMgr = gmGo.AddComponent<BlockVFXManager>();
            var vfxSo = new SerializedObject(vfxMgr);
            if (vfxAsset != null) vfxSo.FindProperty("shatterVfxAsset").objectReferenceValue = vfxAsset;
            if (debrisMat != null) vfxSo.FindProperty("debrisMaterial").objectReferenceValue = debrisMat;
            if (burstMat != null) vfxSo.FindProperty("particleMaterial").objectReferenceValue = burstMat;
            vfxSo.ApplyModifiedProperties();

            var levelGen = gmGo.AddComponent<LevelGenerator>();
            var levelSo = new SerializedObject(levelGen);
            levelSo.FindProperty("matRedBlock").objectReferenceValue = redMat;
            levelSo.FindProperty("matGreenBlock").objectReferenceValue = greenMat;
            levelSo.FindProperty("matBlueBlock").objectReferenceValue = blueMat;
            var glassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Block_Glass.mat");
            if (glassMat != null) levelSo.FindProperty("matGlass").objectReferenceValue = glassMat;

            var presetsProp = levelSo.FindProperty("levelPresets");
            presetsProp.arraySize = 15;
            for (int i = 0; i < 15; i++)
            {
                var lvl = AssetDatabase.LoadAssetAtPath<LevelConfiguration>($"Assets/Settings/Levels/SO_Level_{i + 1:D2}.asset");
                presetsProp.GetArrayElementAtIndex(i).objectReferenceValue = lvl;
            }

            var badgeSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/BlockWorldPanelSettings.asset");
            var badgeUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BlockBadgeUI.uxml");
            levelSo.FindProperty("badgePanelSettings").objectReferenceValue = badgeSettings;
            levelSo.FindProperty("badgeVisualTreeAsset").objectReferenceValue = badgeUxml;
            var iconSet = GetOrCreatePowerupIconSet();
            if (iconSet != null) levelSo.FindProperty("iconSet").objectReferenceValue = iconSet;
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
            if (iconSet != null) uiMgrSo.FindProperty("iconSet").objectReferenceValue = iconSet;
            var heartFill = LoadSpriteFromPath("Assets/UI/Icons/TX_Heart_Fill.png");
            var heartEmpty = LoadSpriteFromPath("Assets/UI/Icons/TX_Heart_Empty.png");
            if (heartFill != null) uiMgrSo.FindProperty("heartFillSprite").objectReferenceValue = heartFill;
            if (heartEmpty != null) uiMgrSo.FindProperty("heartEmptySprite").objectReferenceValue = heartEmpty;

            var lvlIcon = LoadSpriteFromPath("Assets/UI/Icons/TX_Level_Settings.png");
            var muteIcon = LoadSpriteFromPath("Assets/UI/Icons/TX_Volume_Mute.png");
            var volUpIcon = LoadSpriteFromPath("Assets/UI/Icons/TX_Volume_Up.png");
            var setIcon = LoadSpriteFromPath("Assets/UI/Icons/TX_Settings.png");
            var pauseIcon = LoadSpriteFromPath("Assets/UI/Icons/TX_Pause.png");
            var playIcon = LoadSpriteFromPath("Assets/UI/Icons/TX_Play.png");

            if (lvlIcon != null) uiMgrSo.FindProperty("levelSettingsSprite").objectReferenceValue = lvlIcon;
            if (muteIcon != null) uiMgrSo.FindProperty("volumeMuteSprite").objectReferenceValue = muteIcon;
            if (volUpIcon != null) uiMgrSo.FindProperty("volumeUpSprite").objectReferenceValue = volUpIcon;
            if (setIcon != null) uiMgrSo.FindProperty("settingsSprite").objectReferenceValue = setIcon;
            if (pauseIcon != null) uiMgrSo.FindProperty("pauseSprite").objectReferenceValue = pauseIcon;
            if (playIcon != null) uiMgrSo.FindProperty("playSprite").objectReferenceValue = playIcon;

            var shieldIcon = LoadSpriteFromPath("Assets/UI/Icons/TX_Powerup_Shield.png");
            var multiBallIcon = LoadSpriteFromPath("Assets/UI/Icons/TX_Powerup_Multi_Ball.png");
            var paddleExpandIcon = LoadSpriteFromPath("Assets/UI/Icons/TX_Powerup_Arrows_Outward.png");
            var multiplierIcon = LoadSpriteFromPath("Assets/UI/Icons/TX_Powerup_Extra_Points.png");
            var laserIcon = LoadSpriteFromPath("Assets/UI/Icons/TX_Powerup_Gun.png");

            if (shieldIcon != null) uiMgrSo.FindProperty("shieldSprite").objectReferenceValue = shieldIcon;
            if (multiBallIcon != null) uiMgrSo.FindProperty("multiBallSprite").objectReferenceValue = multiBallIcon;
            if (paddleExpandIcon != null) uiMgrSo.FindProperty("paddleExpandSprite").objectReferenceValue = paddleExpandIcon;
            if (multiplierIcon != null) uiMgrSo.FindProperty("multiplierSprite").objectReferenceValue = multiplierIcon;
            if (laserIcon != null) uiMgrSo.FindProperty("laserSprite").objectReferenceValue = laserIcon;

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
            var glassBreakClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Glass_Break.mp3");
            var bombExplosionClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Bomb_Explosion.mp3");

            if (popClip != null) audioSo.FindProperty("clipPop").objectReferenceValue = popClip;
            if (breakClip != null) audioSo.FindProperty("clipBreak").objectReferenceValue = breakClip;
            if (powerupClip != null) audioSo.FindProperty("clipPowerup").objectReferenceValue = powerupClip;
            if (gameOverClip != null) audioSo.FindProperty("clipGameOver").objectReferenceValue = gameOverClip;
            if (levelSuccessClip != null) audioSo.FindProperty("clipLevelSuccess").objectReferenceValue = levelSuccessClip;
            if (buttonPressClip != null) audioSo.FindProperty("clipButtonPress").objectReferenceValue = buttonPressClip;
            if (lifeLostClip != null) audioSo.FindProperty("clipLifeLost").objectReferenceValue = lifeLostClip;
            if (shieldDeflectClip != null) audioSo.FindProperty("clipShieldDeflect").objectReferenceValue = shieldDeflectClip;
            if (glassBreakClip != null) audioSo.FindProperty("clipGlassBreak").objectReferenceValue = glassBreakClip;
            if (bombExplosionClip != null) audioSo.FindProperty("clipBombExplosion").objectReferenceValue = bombExplosionClip;

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

        private static void ConfigureBackgroundTextures(LevelBackgroundController bgCtrl)
        {
            if (bgCtrl == null) return;
            var texA = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds/TX_Background_Gradient_A.png");
            var texB = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds/TX_Background_Gradient_B.png");
            var texC = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds/TX_Background_Gradient_C.png");
            var texD = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds/TX_Background_Gradient_D.png");

            var bgSo = new SerializedObject(bgCtrl);
            var texturesProp = bgSo.FindProperty("backgroundTextures");
            texturesProp.arraySize = 4;
            texturesProp.GetArrayElementAtIndex(0).objectReferenceValue = texA;
            texturesProp.GetArrayElementAtIndex(1).objectReferenceValue = texB;
            texturesProp.GetArrayElementAtIndex(2).objectReferenceValue = texC;
            texturesProp.GetArrayElementAtIndex(3).objectReferenceValue = texD;
            bgSo.ApplyModifiedProperties();
        }

        public static PowerupIconSet GetOrCreatePowerupIconSet()
        {
            string path = "Assets/Settings/SO_PowerupIcons.asset";
            var iconSet = AssetDatabase.LoadAssetAtPath<PowerupIconSet>(path);
            if (iconSet == null)
            {
                if (!Directory.Exists("Assets/Settings"))
                {
                    Directory.CreateDirectory("Assets/Settings");
                }
                iconSet = ScriptableObject.CreateInstance<PowerupIconSet>();
                AssetDatabase.CreateAsset(iconSet, path);
            }

            var expander = LoadSpriteFromPath("Assets/UI/Icons/TX_Powerup_Arrows_Outward.png");
            var bomb = LoadSpriteFromPath("Assets/UI/Icons/TX_Powerup_Bomb.png");
            var extraHeart = LoadSpriteFromPath("Assets/UI/Icons/TX_Powerup_Heart_Plus.png");
            var shield = LoadSpriteFromPath("Assets/UI/Icons/TX_Powerup_Shield.png");
            var multiBall = LoadSpriteFromPath("Assets/UI/Icons/TX_Powerup_Multi_Ball.png");
            var multiplier = LoadSpriteFromPath("Assets/UI/Icons/TX_Powerup_Extra_Points.png");
            var laser = LoadSpriteFromPath("Assets/UI/Icons/TX_Powerup_Gun.png");

            var so = new SerializedObject(iconSet);
            if (expander != null) so.FindProperty("expanderSprite").objectReferenceValue = expander;
            if (bomb != null) so.FindProperty("bombSprite").objectReferenceValue = bomb;
            if (extraHeart != null) so.FindProperty("extraHeartSprite").objectReferenceValue = extraHeart;
            if (shield != null) so.FindProperty("shieldSprite").objectReferenceValue = shield;
            if (multiBall != null) so.FindProperty("multiBallSprite").objectReferenceValue = multiBall;
            if (multiplier != null) so.FindProperty("multiplierSprite").objectReferenceValue = multiplier;
            if (laser != null) so.FindProperty("laserSprite").objectReferenceValue = laser;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(iconSet);
            AssetDatabase.SaveAssets();
            return iconSet;
        }

        private static Sprite LoadSpriteFromPath(string path)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            if (all != null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] is Sprite s) return s;
                }
            }
            return null;
        }
    }
}
