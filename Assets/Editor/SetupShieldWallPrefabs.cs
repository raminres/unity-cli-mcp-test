using System.IO;
using Arcade.BlockBreaker;
using UnityEditor;
using UnityEngine;

namespace Arcade.Editor
{
    /// <summary>
    /// Editor utility to generate the modular PF_ShieldWall prefab with decoupled model mesh
    /// and shimmering cyan energy particle VFX.
    /// </summary>
    public static class SetupShieldWallPrefabs
    {
        public const string PREFABS_DIR = "Assets/Prefabs/Arena";
        public const string SHIELD_WALL_PREFAB_PATH = PREFABS_DIR + "/PF_ShieldWall.prefab";

        public const string MAT_SHIELD_PATH = "Assets/Materials/BlockBreaker/MI_ShieldWall.mat";
        public const string PHYS_BOUNCE_PATH = "Assets/Materials/BlockBreaker/PM_ArcadeBounce.physicMaterial";
        public const string MAT_PARTICLE_PATH = "Assets/Materials/BlockBreaker/MI_VFX_Burst.mat";

        [MenuItem("Tools/Arcade/Generate Shield Wall Prefabs")]
        public static void GenerateShieldWallPrefab()
        {
            if (!Directory.Exists(PREFABS_DIR))
            {
                Directory.CreateDirectory(PREFABS_DIR);
                AssetDatabase.Refresh();
            }

            Material shieldMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_SHIELD_PATH);
            PhysicsMaterial bounceMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(PHYS_BOUNCE_PATH);
            Material particleMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_PARTICLE_PATH);

            // 1. Root Container
            GameObject root = new GameObject("PF_ShieldWall");
            root.transform.position = new Vector3(0f, -7.6f, 0f);
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.zero;

            var col = root.AddComponent<BoxCollider>();
            col.size = Vector3.one;
            col.center = Vector3.zero;
            if (bounceMat != null) col.sharedMaterial = bounceMat;

            var shieldWall = root.AddComponent<ShieldWall>();

            // 2. Child 1: model (Visual Energy Barrier Mesh, Zero Collider)
            GameObject modelGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modelGo.name = "model";
            modelGo.transform.SetParent(root.transform, false);
            modelGo.transform.localPosition = Vector3.zero;
            modelGo.transform.localRotation = Quaternion.identity;
            modelGo.transform.localScale = Vector3.one;
            var modelCol = modelGo.GetComponent<Collider>();
            if (modelCol != null) Object.DestroyImmediate(modelCol);

            var modelRend = modelGo.GetComponent<MeshRenderer>();
            if (shieldMat != null && modelRend != null) modelRend.sharedMaterial = shieldMat;

            // 3. Child 2: vfx (Energy Barrier Shimmering / Particle Sparks Effect)
            GameObject vfxGo = new GameObject("vfx");
            vfxGo.transform.SetParent(root.transform, false);
            vfxGo.transform.localPosition = Vector3.zero;
            vfxGo.transform.localRotation = Quaternion.identity;
            vfxGo.transform.localScale = Vector3.one;

            var ps = vfxGo.AddComponent<ParticleSystem>();
            var psr = vfxGo.GetComponent<ParticleSystemRenderer>();
            if (particleMat != null) psr.sharedMaterial = particleMat;

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startColor = new Color(0.0f, 0.95f, 1.0f, 0.85f); // Electric Cyan
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 18f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20.0f, 0.45f, 1.0f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);

            // 4. Wire Serialized Fields on ShieldWall
            var so = new SerializedObject(shieldWall);
            so.FindProperty("modelChild").objectReferenceValue = modelGo;
            so.FindProperty("vfxChild").objectReferenceValue = vfxGo;
            so.FindProperty("shieldVfx").objectReferenceValue = ps;
            so.FindProperty("meshRenderer").objectReferenceValue = modelRend;
            so.FindProperty("boxCollider").objectReferenceValue = col;
            so.FindProperty("shieldMaterial").objectReferenceValue = shieldMat;
            so.ApplyModifiedProperties();

            // 5. Save Prefab
            PrefabUtility.SaveAsPrefabAsset(root, SHIELD_WALL_PREFAB_PATH);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=cyan>PF_ShieldWall prefab saved successfully at {SHIELD_WALL_PREFAB_PATH}</color>");
        }
    }
}
