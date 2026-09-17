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

            // 1. Create Base Prefab with modular 4-child hierarchy:
            // Root: BoxCollider (1,1,1) + Block.cs
            //   ├── brick (3D model, scale 1,1,1)
            //   ├── brick frost (3D model, scale 1.08,1.08,1.08, inactive by default)
            //   ├── brick special (UI Toolkit badge at Z = -0.52, inactive by default)
            //   └── brick vfx (VFX container at Z = 0)
            GameObject baseGo = new GameObject("PF_Block_Base");
            BoxCollider col = baseGo.AddComponent<BoxCollider>();
            col.size = Vector3.one;
            col.center = Vector3.zero;

            Block baseBlock = baseGo.AddComponent<Block>();

            // Child 1: brick (3D model)
            GameObject brickGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            brickGo.name = "brick";
            brickGo.transform.SetParent(baseGo.transform);
            brickGo.transform.localPosition = Vector3.zero;
            brickGo.transform.localRotation = Quaternion.identity;
            brickGo.transform.localScale = Vector3.one;
            var brickCol = brickGo.GetComponent<Collider>();
            if (brickCol != null) Object.DestroyImmediate(brickCol);
            MeshRenderer brickMr = brickGo.GetComponent<MeshRenderer>();
            if (brickMr != null && matRed != null) brickMr.sharedMaterial = matRed;

            // Child 2: brick frost (3D model)
            GameObject frostGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frostGo.name = "brick frost";
            frostGo.transform.SetParent(baseGo.transform);
            frostGo.transform.localPosition = Vector3.zero;
            frostGo.transform.localRotation = Quaternion.identity;
            frostGo.transform.localScale = Vector3.one * 1.08f;
            var frostCol = frostGo.GetComponent<Collider>();
            if (frostCol != null) Object.DestroyImmediate(frostCol);
            MeshRenderer frostMr = frostGo.GetComponent<MeshRenderer>();
            if (frostMr != null && matGlass != null) frostMr.sharedMaterial = matGlass;
            frostGo.SetActive(false);

            // Child 3: brick special (special sprite/ui element)
            GameObject specialGo = new GameObject("brick special");
            specialGo.transform.SetParent(baseGo.transform);
            specialGo.transform.localPosition = new Vector3(0f, 0f, -0.52f);
            specialGo.transform.localRotation = Quaternion.identity;
            specialGo.transform.localScale = Vector3.one;
            var panelRenderer = specialGo.AddComponent<PanelRenderer>();
            if (badgeSettings != null) panelRenderer.panelSettings = badgeSettings;
            if (badgeUxml != null) panelRenderer.visualTreeAsset = badgeUxml;
            panelRenderer.worldSpaceSizeMode = WorldSpaceSizeMode.Fixed;
            panelRenderer.worldSpaceSize = new Vector2(0.80f, 0.80f);
            panelRenderer.pivot = Pivot.Center;
            specialGo.AddComponent<BlockBadge>();
            specialGo.SetActive(false);

            // Child 4: brick vfx
            GameObject vfxGo = new GameObject("brick vfx");
            vfxGo.transform.SetParent(baseGo.transform);
            vfxGo.transform.localPosition = Vector3.zero;
            vfxGo.transform.localRotation = Quaternion.identity;
            vfxGo.transform.localScale = Vector3.one;

            ConfigureBlockSerialized(baseBlock, BlockColorTier.Red, BlockSpecialType.Normal, 10, 1, 1, vfxRed, brickMr, null, brickGo, frostGo, specialGo, vfxGo);

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
            CreateBombVariant(basePrefab, BOMB_PREFAB_PATH, matRed, vfxRed, iconSet);

            // 6. Create Glass Variant
            CreateGlassVariant(basePrefab, GLASS_PREFAB_PATH, matRed, vfxRed, matGlass);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>Modular block prefabs (brick, brick frost, brick special, brick vfx) successfully generated in " + PREFABS_DIR + "!</color>");
        }

        private static void CreateColorVariant(GameObject basePrefab, string assetPath, string name, BlockColorTier tier, Material mat, Color vfxColor, int points)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = name;

            Transform brickTransform = instance.transform.Find("brick");
            MeshRenderer mr = brickTransform != null ? brickTransform.GetComponent<MeshRenderer>() : null;
            if (mr != null && mat != null)
            {
                mr.sharedMaterial = mat;
            }

            Block block = instance.GetComponent<Block>();
            if (block != null)
            {
                GameObject brickGo = brickTransform != null ? brickTransform.gameObject : null;
                GameObject frostGo = instance.transform.Find("brick frost")?.gameObject;
                GameObject specialGo = instance.transform.Find("brick special")?.gameObject;
                GameObject vfxGo = instance.transform.Find("brick vfx")?.gameObject;
                ConfigureBlockSerialized(block, tier, BlockSpecialType.Normal, points, 1, 1, vfxColor, mr, null, brickGo, frostGo, specialGo, vfxGo);
            }

            PrefabUtility.SaveAsPrefabAsset(instance, assetPath);
            Object.DestroyImmediate(instance);
        }

        private static void CreateBombVariant(GameObject basePrefab, string assetPath, Material mat, Color vfxColor, PowerupIconSet iconSet)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = "PF_Block_Bomb";

            Transform brickTransform = instance.transform.Find("brick");
            MeshRenderer mr = brickTransform != null ? brickTransform.GetComponent<MeshRenderer>() : null;
            if (mr != null && mat != null)
            {
                mr.sharedMaterial = mat;
            }

            Transform specialTransform = instance.transform.Find("brick special");
            if (specialTransform != null)
            {
                specialTransform.gameObject.SetActive(true);
                var badge = specialTransform.GetComponent<BlockBadge>();
                Sprite bombSprite = iconSet != null ? iconSet.GetSprite(BlockSpecialType.Bomb) : null;
                if (badge != null)
                {
                    if (bombSprite != null) badge.SetSprite(bombSprite);
                    SerializedObject badgeSo = new SerializedObject(badge);
                    badgeSo.FindProperty("specialType").intValue = (int)BlockSpecialType.Bomb;
                    if (bombSprite != null) badgeSo.FindProperty("iconSprite").objectReferenceValue = bombSprite;
                    badgeSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Block block = instance.GetComponent<Block>();
            if (block != null)
            {
                GameObject brickGo = brickTransform != null ? brickTransform.gameObject : null;
                GameObject frostGo = instance.transform.Find("brick frost")?.gameObject;
                GameObject specialGo = specialTransform != null ? specialTransform.gameObject : null;
                GameObject vfxGo = instance.transform.Find("brick vfx")?.gameObject;
                ConfigureBlockSerialized(block, BlockColorTier.Red, BlockSpecialType.Bomb, 10, 1, 1, vfxColor, mr, null, brickGo, frostGo, specialGo, vfxGo);
            }

            PrefabUtility.SaveAsPrefabAsset(instance, assetPath);
            Object.DestroyImmediate(instance);
        }

        private static void CreateGlassVariant(GameObject basePrefab, string assetPath, Material mat, Color vfxColor, Material matGlass)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = "PF_Block_Glass";

            Transform brickTransform = instance.transform.Find("brick");
            MeshRenderer mr = brickTransform != null ? brickTransform.GetComponent<MeshRenderer>() : null;
            if (mr != null && mat != null)
            {
                mr.sharedMaterial = mat;
            }

            Transform frostTransform = instance.transform.Find("brick frost");
            GameObject frostGo = null;
            if (frostTransform != null)
            {
                frostGo = frostTransform.gameObject;
                frostGo.SetActive(true);
                MeshRenderer frostMr = frostGo.GetComponent<MeshRenderer>();
                if (frostMr != null && matGlass != null)
                {
                    frostMr.sharedMaterial = matGlass;
                }
            }

            Block block = instance.GetComponent<Block>();
            if (block != null)
            {
                GameObject brickGo = brickTransform != null ? brickTransform.gameObject : null;
                GameObject specialGo = instance.transform.Find("brick special")?.gameObject;
                GameObject vfxGo = instance.transform.Find("brick vfx")?.gameObject;
                ConfigureBlockSerialized(block, BlockColorTier.Red, BlockSpecialType.GlassEnclosed, 10, 2, 2, vfxColor, mr, frostGo, brickGo, frostGo, specialGo, vfxGo);
                block.SetGlassShell(frostGo);
            }

            PrefabUtility.SaveAsPrefabAsset(instance, assetPath);
            Object.DestroyImmediate(instance);
        }

        private static void ConfigureBlockSerialized(Block block, BlockColorTier tier, BlockSpecialType special, int basePoints, int scoreMultiplier, int hp, Color particleColor, MeshRenderer mr, GameObject glassShell, GameObject brickGo, GameObject frostGo, GameObject specialGo, GameObject vfxGo)
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
            if (brickGo != null) so.FindProperty("brickModel").objectReferenceValue = brickGo;
            if (frostGo != null) so.FindProperty("brickFrost").objectReferenceValue = frostGo;
            if (specialGo != null) so.FindProperty("brickSpecial").objectReferenceValue = specialGo;
            if (vfxGo != null) so.FindProperty("brickVfx").objectReferenceValue = vfxGo;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
