using System.IO;
using Arcade.BlockBreaker;
using Arcade.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arcade.Editor
{
    public static class SetupBlockPrefabs
    {
        public const string PREFABS_DIR = "Assets/Prefabs/Blocks";
        public const string BASE_PREFAB_PATH = PREFABS_DIR + "/PF_Block_Base.prefab";
        public const string RED_PREFAB_PATH = PREFABS_DIR + "/PF_Block_Red.prefab";
        public const string GREEN_PREFAB_PATH = PREFABS_DIR + "/PF_Block_Green.prefab";
        public const string BLUE_PREFAB_PATH = PREFABS_DIR + "/PF_Block_Blue.prefab";
        public const string BOMB_PREFAB_PATH = PREFABS_DIR + "/PF_Block_Bomb.prefab";
        public const string GLASS_PREFAB_PATH = PREFABS_DIR + "/PF_Block_Glass.prefab";

        public const string MAT_RED_PATH = "Assets/Materials/BlockBreaker/MI_Block_Red.mat";
        public const string MAT_GREEN_PATH = "Assets/Materials/BlockBreaker/MI_Block_Green.mat";
        public const string MAT_BLUE_PATH = "Assets/Materials/BlockBreaker/MI_Block_Blue.mat";
        public const string MAT_GLASS_PATH = "Assets/Materials/BlockBreaker/MI_Block_Glass.mat";
        public const string ICON_SET_PATH = "Assets/Settings/SO_PowerupIcons.asset";
        public const string BADGE_SETTINGS_PATH = "Assets/UI/BlockWorldPanelSettings.asset";
        public const string BADGE_UXML_PATH = "Assets/UI/BlockBadgeUI.uxml";

        [MenuItem("Tools/Arcade/Generate Block Prefabs")]
        public static void GenerateBlockPrefabs()
        {
            if (!Directory.Exists(PREFABS_DIR))
            {
                Directory.CreateDirectory(PREFABS_DIR);
                AssetDatabase.Refresh();
            }

            Material matRed = AssetDatabase.LoadAssetAtPath<Material>(MAT_RED_PATH);
            Material matGreen = AssetDatabase.LoadAssetAtPath<Material>(MAT_GREEN_PATH);
            Material matBlue = AssetDatabase.LoadAssetAtPath<Material>(MAT_BLUE_PATH);
            Material matGlass = AssetDatabase.LoadAssetAtPath<Material>(MAT_GLASS_PATH);
            PowerupIconSet iconSet = AssetDatabase.LoadAssetAtPath<PowerupIconSet>(ICON_SET_PATH);
            PanelSettings badgeSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(BADGE_SETTINGS_PATH);
            VisualTreeAsset badgeUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BADGE_UXML_PATH);

            Color vfxRed = new Color(1.0f, 0.2f, 0.3f);
            Color vfxGreen = new Color(0.15f, 0.95f, 0.45f);
            Color vfxBlue = new Color(0.15f, 0.7f, 1.0f);

            // 1. Create Base Prefab
            GameObject baseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseGo.name = "PF_Block_Base";
            BoxCollider col = baseGo.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.size = Vector3.one;
                col.center = Vector3.zero;
            }

            MeshRenderer baseMr = baseGo.GetComponent<MeshRenderer>();
            if (baseMr != null && matRed != null)
            {
                baseMr.sharedMaterial = matRed;
            }

            Block baseBlock = baseGo.AddComponent<Block>();
            ConfigureBlockSerialized(baseBlock, BlockColorTier.Red, BlockSpecialType.Normal, 10, 1, 1, vfxRed, baseMr, null);

            GameObject basePrefab = PrefabUtility.SaveAsPrefabAsset(baseGo, BASE_PREFAB_PATH);
            Object.DestroyImmediate(baseGo);

            if (basePrefab == null)
            {
                Debug.LogError("Failed to create base block prefab at " + BASE_PREFAB_PATH);
                return;
            }

            // 2. Create Red Variant
            CreateColorVariant(basePrefab, RED_PREFAB_PATH, "PF_Block_Red", BlockColorTier.Red, matRed, vfxRed, 10);

            // 3. Create Green Variant
            CreateColorVariant(basePrefab, GREEN_PREFAB_PATH, "PF_Block_Green", BlockColorTier.Green, matGreen, vfxGreen, 20);

            // 4. Create Blue Variant
            CreateColorVariant(basePrefab, BLUE_PREFAB_PATH, "PF_Block_Blue", BlockColorTier.Blue, matBlue, vfxBlue, 30);

            // 5. Create Bomb Variant
            CreateBombVariant(basePrefab, BOMB_PREFAB_PATH, matRed, vfxRed, iconSet, badgeSettings, badgeUxml);

            // 6. Create Glass Variant
            CreateGlassVariant(basePrefab, GLASS_PREFAB_PATH, matRed, vfxRed, matGlass);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>Block prefabs and variants successfully generated in " + PREFABS_DIR + "!</color>");
        }

        private static void CreateColorVariant(GameObject basePrefab, string assetPath, string name, BlockColorTier tier, Material mat, Color vfxColor, int points)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = name;

            MeshRenderer mr = instance.GetComponent<MeshRenderer>();
            if (mr != null && mat != null)
            {
                mr.sharedMaterial = mat;
            }

            Block block = instance.GetComponent<Block>();
            if (block != null)
            {
                ConfigureBlockSerialized(block, tier, BlockSpecialType.Normal, points, 1, 1, vfxColor, mr, null);
            }

            PrefabUtility.SaveAsPrefabAsset(instance, assetPath);
            Object.DestroyImmediate(instance);
        }

        private static void CreateBombVariant(GameObject basePrefab, string assetPath, Material mat, Color vfxColor, PowerupIconSet iconSet, PanelSettings badgeSettings, VisualTreeAsset badgeUxml)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = "PF_Block_Bomb";

            MeshRenderer mr = instance.GetComponent<MeshRenderer>();
            if (mr != null && mat != null)
            {
                mr.sharedMaterial = mat;
            }

            Block block = instance.GetComponent<Block>();
            if (block != null)
            {
                ConfigureBlockSerialized(block, BlockColorTier.Red, BlockSpecialType.Bomb, 10, 1, 1, vfxColor, mr, null);
            }

            // Create Badge child
            GameObject badgeGo = new GameObject("UI_Badge");
            badgeGo.transform.SetParent(instance.transform);
            badgeGo.transform.localPosition = new Vector3(0f, 0f, -0.52f);
            badgeGo.transform.localRotation = Quaternion.identity;
            badgeGo.transform.localScale = Vector3.one;

            var panelRenderer = badgeGo.AddComponent<PanelRenderer>();
            if (badgeSettings != null) panelRenderer.panelSettings = badgeSettings;
            if (badgeUxml != null) panelRenderer.visualTreeAsset = badgeUxml;
            panelRenderer.worldSpaceSizeMode = WorldSpaceSizeMode.Fixed;
            panelRenderer.worldSpaceSize = new Vector2(0.80f, 0.80f);
            panelRenderer.pivot = Pivot.Center;

            var badge = badgeGo.AddComponent<BlockBadge>();
            Sprite bombSprite = iconSet != null ? iconSet.GetSprite(BlockSpecialType.Bomb) : null;
            if (bombSprite != null) badge.SetSprite(bombSprite);

            SerializedObject badgeSo = new SerializedObject(badge);
            badgeSo.FindProperty("panelRenderer").objectReferenceValue = panelRenderer;
            badgeSo.FindProperty("specialType").intValue = (int)BlockSpecialType.Bomb;
            if (bombSprite != null) badgeSo.FindProperty("iconSprite").objectReferenceValue = bombSprite;
            badgeSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(instance, assetPath);
            Object.DestroyImmediate(instance);
        }

        private static void CreateGlassVariant(GameObject basePrefab, string assetPath, Material mat, Color vfxColor, Material matGlass)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = "PF_Block_Glass";

            MeshRenderer mr = instance.GetComponent<MeshRenderer>();
            if (mr != null && mat != null)
            {
                mr.sharedMaterial = mat;
            }

            // Create Glass Shell child
            GameObject shellObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shellObj.name = "Glass_Shell";
            shellObj.transform.SetParent(instance.transform);
            shellObj.transform.localPosition = Vector3.zero;
            shellObj.transform.localRotation = Quaternion.identity;
            shellObj.transform.localScale = Vector3.one * 1.18f;

            var col = shellObj.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var shellMr = shellObj.GetComponent<MeshRenderer>();
            if (shellMr != null && matGlass != null)
            {
                shellMr.sharedMaterial = matGlass;
            }

            Block block = instance.GetComponent<Block>();
            if (block != null)
            {
                ConfigureBlockSerialized(block, BlockColorTier.Red, BlockSpecialType.GlassEnclosed, 10, 2, 2, vfxColor, mr, shellObj);
                block.SetGlassShell(shellObj);
            }

            PrefabUtility.SaveAsPrefabAsset(instance, assetPath);
            Object.DestroyImmediate(instance);
        }

        private static void ConfigureBlockSerialized(Block block, BlockColorTier tier, BlockSpecialType special, int basePoints, int scoreMultiplier, int hp, Color particleColor, MeshRenderer mr, GameObject glassShell)
        {
            block.Initialize(tier, mr != null ? mr.sharedMaterial : null, particleColor, special);

            SerializedObject so = new SerializedObject(block);
            so.FindProperty("colorTier").intValue = (int)tier;
            so.FindProperty("specialType").intValue = (int)special;
            so.FindProperty("basePoints").intValue = basePoints;
            so.FindProperty("scoreMultiplier").intValue = scoreMultiplier;
            so.FindProperty("paddleExpansionPercent").floatValue = 0.10f;
            so.FindProperty("hitPoints").intValue = hp;
            so.FindProperty("particleColor").colorValue = particleColor;
            if (mr != null) so.FindProperty("meshRenderer").objectReferenceValue = mr;
            if (glassShell != null) so.FindProperty("glassShell").objectReferenceValue = glassShell;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
