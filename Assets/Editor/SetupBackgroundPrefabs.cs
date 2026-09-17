using System.IO;
using Arcade.BlockBreaker;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arcade.Editor
{
    public static class SetupBackgroundPrefabs
    {
        public const string PREFABS_DIR = "Assets/Prefabs/Arena";
        public const string BACKGROUND_PREFAB_PATH = PREFABS_DIR + "/PF_Background.prefab";

        public const string MAT_BG_PATH = "Assets/Materials/BlockBreaker/MI_Background_Gradient.mat";
        public const string TEX_BG_A_PATH = "Assets/Textures/Backgrounds/TX_Background_Gradient_A.png";
        public const string TEX_BG_B_PATH = "Assets/Textures/Backgrounds/TX_Background_Gradient_B.png";
        public const string TEX_BG_C_PATH = "Assets/Textures/Backgrounds/TX_Background_Gradient_C.png";
        public const string TEX_BG_D_PATH = "Assets/Textures/Backgrounds/TX_Background_Gradient_D.png";

        [MenuItem("Tools/Arcade/Generate Background Prefabs")]
        public static void GenerateBackgroundPrefabs()
        {
            if (!Directory.Exists(PREFABS_DIR))
            {
                Directory.CreateDirectory(PREFABS_DIR);
                AssetDatabase.Refresh();
            }

            Material bgMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_BG_PATH);
            var texA = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_BG_A_PATH);
            var texB = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_BG_B_PATH);
            var texC = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_BG_C_PATH);
            var texD = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_BG_D_PATH);

            GameObject rootGo = new GameObject("PF_Background");
            rootGo.transform.position = new Vector3(0f, 8.5f, 6.0f);
            rootGo.transform.rotation = Quaternion.identity;
            rootGo.transform.localScale = new Vector3(40f, 80f, 1f);

            var bgCtrl = rootGo.AddComponent<LevelBackgroundController>();

            // Child 'plane' (Mesh Quad / Plane)
            GameObject planeGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            planeGo.name = "plane";
            planeGo.transform.SetParent(rootGo.transform, false);
            planeGo.transform.localPosition = Vector3.zero;
            planeGo.transform.localRotation = Quaternion.identity;
            planeGo.transform.localScale = Vector3.one;

            var col = planeGo.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var mr = planeGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (bgMat != null) mr.sharedMaterial = bgMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            // Wire SerializedObject on LevelBackgroundController
            var bgSo = new SerializedObject(bgCtrl);
            bgSo.FindProperty("meshRenderer").objectReferenceValue = mr;
            bgSo.FindProperty("planeChild").objectReferenceValue = planeGo;

            var texturesProp = bgSo.FindProperty("backgroundTextures");
            texturesProp.arraySize = 4;
            texturesProp.GetArrayElementAtIndex(0).objectReferenceValue = texA;
            texturesProp.GetArrayElementAtIndex(1).objectReferenceValue = texB;
            texturesProp.GetArrayElementAtIndex(2).objectReferenceValue = texC;
            texturesProp.GetArrayElementAtIndex(3).objectReferenceValue = texD;
            bgSo.ApplyModifiedProperties();

            // Save Prefab
            PrefabUtility.SaveAsPrefabAsset(rootGo, BACKGROUND_PREFAB_PATH);
            Object.DestroyImmediate(rootGo);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>Modular Background prefab (PF_Background) successfully generated at " + BACKGROUND_PREFAB_PATH + "!</color>");
        }
    }
}
