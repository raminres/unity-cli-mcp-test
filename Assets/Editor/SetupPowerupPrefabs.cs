using System.IO;
using Arcade.BlockBreaker;
using Arcade.Core;
using UnityEditor;
using UnityEngine;

namespace Arcade.Editor
{
    public static class SetupPowerupPrefabs
    {
        public const string PREFABS_DIR = "Assets/Prefabs/Powerups";
        public const string POWERUP_PREFAB_PATH = PREFABS_DIR + "/PF_Drop_Powerup.prefab";
        public const string HAZARD_PREFAB_PATH = PREFABS_DIR + "/PF_Drop_Hazard.prefab";

        public const string MAT_CAPSULE_PATH = "Assets/Materials/BlockBreaker/MI_Powerup_Capsule.mat";
        public const string MAT_VFX_PATH = "Assets/Materials/BlockBreaker/MI_VFX_Burst.mat";
        public const string ICON_SET_PATH = "Assets/Settings/SO_PowerupIcons.asset";

        [MenuItem("Tools/Arcade/Generate Powerup and Hazard Drop Prefabs")]
        public static void GeneratePowerupPrefabs()
        {
            if (!Directory.Exists(PREFABS_DIR))
            {
                Directory.CreateDirectory(PREFABS_DIR);
                AssetDatabase.Refresh();
            }

            Material matCapsule = AssetDatabase.LoadAssetAtPath<Material>(MAT_CAPSULE_PATH);
            Material matVfx = AssetDatabase.LoadAssetAtPath<Material>(MAT_VFX_PATH);
            PowerupIconSet iconSet = AssetDatabase.LoadAssetAtPath<PowerupIconSet>(ICON_SET_PATH);

            Color cyanColor = new Color(0f, 0.949f, 0.996f, 1f); // #00f2fe
            Color crimsonColor = new Color(1f, 0.09f, 0.267f, 1f); // #ff1744

            Sprite defaultPowerupSprite = iconSet != null ? iconSet.GetSprite(BlockSpecialType.PaddleExpander) : null;
            Sprite defaultHazardSprite = iconSet != null ? iconSet.GetSprite(BlockSpecialType.PaddleShortener) : null;

            // 1. Create PF_Drop_Powerup (Capsule 3D Model, Cyan Glow, Falling Spark Trail)
            CreateDropPrefab(POWERUP_PREFAB_PATH, "PF_Drop_Powerup", BlockSpecialType.PaddleExpander, cyanColor, PrimitiveType.Capsule, new Vector3(0.85f, 0.85f, 0.85f), Quaternion.identity, matCapsule, defaultPowerupSprite, matVfx);

            // 2. Create PF_Drop_Hazard (Faceted Diamond 3D Model, Crimson Glow, Falling Ember Trail)
            CreateDropPrefab(HAZARD_PREFAB_PATH, "PF_Drop_Hazard", BlockSpecialType.PaddleShortener, crimsonColor, PrimitiveType.Cube, new Vector3(0.72f, 0.72f, 0.72f), Quaternion.Euler(45f, 45f, 0f), matCapsule, defaultHazardSprite, matVfx);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>Falling drop prefabs (PF_Drop_Powerup, PF_Drop_Hazard) successfully generated in " + PREFABS_DIR + "!</color>");
        }

        private static void CreateDropPrefab(string assetPath, string prefabName, BlockSpecialType defaultType, Color glowColor, PrimitiveType primitiveType, Vector3 modelScale, Quaternion modelRot, Material matCapsule, Sprite defaultSprite, Material matVfx)
        {
            GameObject rootGo = new GameObject(prefabName);
            rootGo.transform.position = Vector3.zero;
            rootGo.transform.rotation = Quaternion.identity;
            rootGo.transform.localScale = Vector3.one;

            var rb = rootGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var box = rootGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.6f, 1.6f, 4.0f);
            box.center = new Vector3(0f, 0f, 1.0f);

            var capsuleComp = rootGo.AddComponent<PowerupCapsule>();

            // 1. Child 'Visual_Capsule' (3D Tumbling Mesh)
            GameObject modelGo = GameObject.CreatePrimitive(primitiveType);
            modelGo.name = "Visual_Capsule";
            modelGo.transform.SetParent(rootGo.transform);
            modelGo.transform.localPosition = Vector3.zero;
            modelGo.transform.localRotation = modelRot;
            modelGo.transform.localScale = modelScale;

            var modelCol = modelGo.GetComponent<Collider>();
            if (modelCol != null) Object.DestroyImmediate(modelCol);

            var modelMr = modelGo.GetComponent<MeshRenderer>();
            if (modelMr != null && matCapsule != null)
            {
                modelMr.sharedMaterial = matCapsule;
            }

            // 2. Child 'Icon_Billboard' (Billboard Icon)
            GameObject spriteGo = new GameObject("Icon_Billboard");
            spriteGo.transform.SetParent(rootGo.transform);
            spriteGo.transform.localPosition = new Vector3(0f, 0f, -0.60f);
            spriteGo.transform.localRotation = Quaternion.identity;
            spriteGo.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);

            var sr = spriteGo.AddComponent<SpriteRenderer>();
            if (defaultSprite != null) sr.sprite = defaultSprite;
            sr.color = Color.white;
            sr.sortingOrder = 35;

            // 3. Child 'Falling_Vfx' (Particle System Trail)
            GameObject vfxGo = new GameObject("Falling_Vfx");
            vfxGo.transform.SetParent(rootGo.transform);
            vfxGo.transform.localPosition = Vector3.zero;
            vfxGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // upward emission
            vfxGo.transform.localScale = Vector3.one;

            var ps = vfxGo.AddComponent<ParticleSystem>();
            var psr = vfxGo.GetComponent<ParticleSystemRenderer>();
            if (matVfx != null) psr.sharedMaterial = matVfx;

            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.22f);
            main.startColor = glowColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 25f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);

            // Serialize properties onto PowerupCapsule
            SerializedObject so = new SerializedObject(capsuleComp);
            so.FindProperty("specialType").intValue = (int)defaultType;
            so.FindProperty("glowColor").colorValue = glowColor;
            so.FindProperty("visualCapsuleTransform").objectReferenceValue = modelGo.transform;
            so.FindProperty("capsuleRenderer").objectReferenceValue = modelMr;
            so.FindProperty("iconTransform").objectReferenceValue = spriteGo.transform;
            so.FindProperty("iconRenderer").objectReferenceValue = sr;
            so.FindProperty("fallingVfxTransform").objectReferenceValue = vfxGo.transform;
            so.FindProperty("fallingVfx").objectReferenceValue = ps;
            so.ApplyModifiedPropertiesWithoutUndo();

            capsuleComp.Initialize(defaultType, glowColor, matCapsule, modelGo.transform, spriteGo.transform);

            PrefabUtility.SaveAsPrefabAsset(rootGo, assetPath);
            Object.DestroyImmediate(rootGo);
        }
    }
}
