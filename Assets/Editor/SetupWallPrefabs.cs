using System.IO;
using Arcade.BlockBreaker;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arcade.Editor
{
    public static class SetupWallPrefabs
    {
        public const string PREFABS_DIR = "Assets/Prefabs/Arena";
        public const string WALL_PREFAB_PATH = PREFABS_DIR + "/PF_Walls.prefab";

        public const string MAT_BORDER_PATH = "Assets/Materials/BlockBreaker/MI_Playfield_Border.mat";
        public const string PHYS_BOUNCE_PATH = "Assets/Materials/BlockBreaker/PM_ArcadeBounce.physicMaterial";

        [MenuItem("Tools/Arcade/Generate Wall Prefabs")]
        public static void GenerateWallPrefabs()
        {
            if (!Directory.Exists(PREFABS_DIR))
            {
                Directory.CreateDirectory(PREFABS_DIR);
                AssetDatabase.Refresh();
            }

            Material matBorder = AssetDatabase.LoadAssetAtPath<Material>(MAT_BORDER_PATH);
            PhysicsMaterial physBounce = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(PHYS_BOUNCE_PATH);

            GameObject rootGo = new GameObject("PF_Walls");
            rootGo.transform.position = Vector3.zero;
            rootGo.transform.rotation = Quaternion.identity;
            rootGo.transform.localScale = Vector3.one;

            var arenaWalls = rootGo.AddComponent<ArenaWalls>();

            // 1. side left (X = -10.25, Y = 7.60, L = 30.2)
            GameObject sideLeft = CreateWallSection(rootGo, "side left",
                new Vector3(-10.25f, 7.60f, 0f), Quaternion.identity,
                new Vector3(0.5f, 30.2f, 2f), new Vector3(0.5f, 30.2f, 2f),
                matBorder, physBounce);

            // 2. side right (X = 10.25, Y = 7.60, L = 30.2)
            GameObject sideRight = CreateWallSection(rootGo, "side right",
                new Vector3(10.25f, 7.60f, 0f), Quaternion.identity,
                new Vector3(0.5f, 30.2f, 2f), new Vector3(0.5f, 30.2f, 2f),
                matBorder, physBounce);

            // 3. top (X = 0.00, Y = 24.25, W = 17.4)
            GameObject top = CreateWallSection(rootGo, "top",
                new Vector3(0f, 24.25f, 0f), Quaternion.identity,
                new Vector3(17.4f, 0.5f, 2f), new Vector3(17.4f, 0.5f, 2f),
                matBorder, physBounce);

            // 4. chamfer left (X = -9.40, Y = 23.40, rot: 45°, L = 2.5)
            GameObject chamferLeft = CreateWallSection(rootGo, "chamfer left",
                new Vector3(-9.40f, 23.40f, 0f), Quaternion.Euler(0f, 0f, 45f),
                new Vector3(2.5f, 0.5f, 2f), new Vector3(2.5f, 0.5f, 2f),
                matBorder, physBounce);

            // 5. chamfer right (X = 9.40, Y = 23.40, rot: -45°, L = 2.5)
            GameObject chamferRight = CreateWallSection(rootGo, "chamfer right",
                new Vector3(9.40f, 23.40f, 0f), Quaternion.Euler(0f, 0f, -45f),
                new Vector3(2.5f, 0.5f, 2f), new Vector3(2.5f, 0.5f, 2f),
                matBorder, physBounce);

            // Wire ArenaWalls SerializedObject
            var wallsSo = new SerializedObject(arenaWalls);
            wallsSo.FindProperty("sideLeft").objectReferenceValue = sideLeft;
            wallsSo.FindProperty("sideRight").objectReferenceValue = sideRight;
            wallsSo.FindProperty("top").objectReferenceValue = top;
            wallsSo.FindProperty("chamferLeft").objectReferenceValue = chamferLeft;
            wallsSo.FindProperty("chamferRight").objectReferenceValue = chamferRight;
            wallsSo.ApplyModifiedProperties();

            // Save Prefab
            PrefabUtility.SaveAsPrefabAsset(rootGo, WALL_PREFAB_PATH);
            Object.DestroyImmediate(rootGo);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>Modular Arena Walls prefab (PF_Walls) successfully generated at " + WALL_PREFAB_PATH + "!</color>");
        }

        private static GameObject CreateWallSection(GameObject root, string sectionName, Vector3 position, Quaternion rotation, Vector3 modelScale, Vector3 colliderSize, Material borderMat, PhysicsMaterial bounceMat)
        {
            GameObject sectionGo = new GameObject(sectionName);
            sectionGo.transform.SetParent(root.transform, false);
            sectionGo.transform.localPosition = position;
            sectionGo.transform.localRotation = rotation;
            sectionGo.transform.localScale = Vector3.one;

            // 1. Child 'model' (Visual 3D mesh without collision)
            GameObject modelGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modelGo.name = "model";
            modelGo.transform.SetParent(sectionGo.transform, false);
            modelGo.transform.localPosition = Vector3.zero;
            modelGo.transform.localRotation = Quaternion.identity;
            modelGo.transform.localScale = modelScale;

            var modelCol = modelGo.GetComponent<Collider>();
            if (modelCol != null) Object.DestroyImmediate(modelCol);

            var mr = modelGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (borderMat != null) mr.sharedMaterial = borderMat;
                mr.shadowCastingMode = ShadowCastingMode.On;
                mr.receiveShadows = true;
            }

            // 2. Child 'collider' (Invisible physical bounce plane)
            GameObject colliderGo = new GameObject("collider");
            colliderGo.transform.SetParent(sectionGo.transform, false);
            colliderGo.transform.localPosition = Vector3.zero;
            colliderGo.transform.localRotation = Quaternion.identity;
            colliderGo.transform.localScale = Vector3.one;

            var box = colliderGo.AddComponent<BoxCollider>();
            box.size = colliderSize;
            box.center = Vector3.zero;
            if (bounceMat != null) box.sharedMaterial = bounceMat;

            return sectionGo;
        }
    }
}
