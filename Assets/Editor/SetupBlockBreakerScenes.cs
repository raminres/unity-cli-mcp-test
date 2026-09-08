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

            // Level 1: Classic Inverted
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_01.asset", 1, "Level 1: Classic Inverted",
                "Classic 3-tier block setup with Red on bottom, Green in middle, and Blue on top. Features random x2 multiplier and paddle expander blocks.",
                BlockColorPattern.InvertedTiered, 8, 2, 1.0f, 5.0f, 1, 0, 1);

            // Level 2: Wide Checkerboard
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_02.asset", 2, "Level 2: Wide Checkerboard",
                "Wider 9-column grid with alternating checkerboard colors, elevated speed, and both x2 and new x3 multipliers.",
                BlockColorPattern.Checkerboard, 9, 2, 1.2f, 5.0f, 2, 1, 1);

            // Level 3: Chaos Gauntlet
            CreateOrConfigureLevel("Assets/Settings/Levels/SO_Level_03.asset", 3, "Level 3: Chaos Gauntlet",
                "Dense 10-column, 9-row gauntlet with fully randomized color dispersion, high velocity, dual x3 multipliers, and compounding paddle expanders.",
                BlockColorPattern.Randomized, 10, 3, 1.35f, 5.0f, 2, 2, 2);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateOrConfigureLevel(string path, int levelNumber, string name, string desc,
            BlockColorPattern pattern, int cols, int rowsPerTier, float speed, float paddleWidth,
            int mult2x, int mult3x, int expanders)
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

            // 2. Audio Manager (Persistent)
            var audioGo = new GameObject("AudioManager");
            audioGo.AddComponent<ArcadeAudioManager>();

            // 3. UI Panel Renderer & Manager
            var uiGo = new GameObject("UI_MainMenu");
            var panelRenderer = uiGo.AddComponent<PanelRenderer>();
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/MainMenuUI.uxml");
            if (uxml != null) panelRenderer.visualTreeAsset = uxml;
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/ArcadePanelSettings.asset");
            if (panelSettings != null) panelRenderer.panelSettings = panelSettings;
            uiGo.AddComponent<MainMenuUIManager>();
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
            audioGo.AddComponent<ArcadeAudioManager>();

            // 10. Game Coordinators
            var gmGo = new GameObject("GameManager");
            gmGo.AddComponent<ArcadeGameManager>();
            gmGo.AddComponent<ArcadeInputHandler>();

            var vfxMgr = gmGo.AddComponent<BlockVFXManager>();
            var vfxSo = new SerializedObject(vfxMgr);
            if (vfxAsset != null) vfxSo.FindProperty("shatterVfxAsset").objectReferenceValue = vfxAsset;
            vfxSo.ApplyModifiedProperties();

            var levelGen = gmGo.AddComponent<LevelGenerator>();
            var levelSo = new SerializedObject(levelGen);
            levelSo.FindProperty("matRedBlock").objectReferenceValue = redMat;
            levelSo.FindProperty("matGreenBlock").objectReferenceValue = greenMat;
            levelSo.FindProperty("matBlueBlock").objectReferenceValue = blueMat;

            var lvl1 = AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_01.asset");
            var lvl2 = AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_02.asset");
            var lvl3 = AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_03.asset");
            var presetsProp = levelSo.FindProperty("levelPresets");
            presetsProp.arraySize = 3;
            presetsProp.GetArrayElementAtIndex(0).objectReferenceValue = lvl1;
            presetsProp.GetArrayElementAtIndex(1).objectReferenceValue = lvl2;
            presetsProp.GetArrayElementAtIndex(2).objectReferenceValue = lvl3;

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
            uiGo.AddComponent<ArcadeUIManager>();
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
            var sampleScenePath = "Assets/Scenes/SampleScene.unity";

            var scenes = new[]
            {
                new EditorBuildSettingsScene(menuScenePath, true),
                new EditorBuildSettingsScene(gameScenePath, true),
                new EditorBuildSettingsScene(sampleScenePath, false)
            };

            EditorBuildSettings.scenes = scenes;
            AssetDatabase.SaveAssets();
        }
    }
}
