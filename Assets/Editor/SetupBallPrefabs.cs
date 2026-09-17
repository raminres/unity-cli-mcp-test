using System.IO;
using Arcade.BlockBreaker;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arcade.Editor
{
    public static class SetupBallPrefabs
    {
        public const string PREFABS_DIR = "Assets/Prefabs/Balls";
        public const string BALL_PREFAB_PATH = PREFABS_DIR + "/PF_Ball_Standard.prefab";

        public const string MAT_BALL_PATH = "Assets/Materials/BlockBreaker/MI_Ball.mat";
        public const string MAT_TRAIL_PATH = "Assets/Materials/BlockBreaker/MI_BallTrail.mat";
        public const string PHYS_BOUNCE_PATH = "Assets/Materials/BlockBreaker/PM_ArcadeBounce.physicMaterial";

        [MenuItem("Tools/Arcade/Generate Ball Prefabs")]
        public static void GenerateBallPrefabs()
        {
            if (!Directory.Exists(PREFABS_DIR))
            {
                Directory.CreateDirectory(PREFABS_DIR);
                AssetDatabase.Refresh();
            }

            Material matBall = AssetDatabase.LoadAssetAtPath<Material>(MAT_BALL_PATH);
            Material matTrail = AssetDatabase.LoadAssetAtPath<Material>(MAT_TRAIL_PATH);
            PhysicsMaterial physBounce = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(PHYS_BOUNCE_PATH);

            GameObject rootGo = new GameObject("PF_Ball_Standard");
            rootGo.transform.position = Vector3.zero;
            rootGo.transform.rotation = Quaternion.identity;
            rootGo.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            // 1. Root SphereCollider & Rigidbody
            var col = rootGo.AddComponent<SphereCollider>();
            col.radius = 0.5f;
            col.center = Vector3.zero;
            if (physBounce != null) col.sharedMaterial = physBounce;

            var rb = rootGo.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

            // 2. Child 'model' (Sphere 3D Mesh)
            GameObject modelGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            modelGo.name = "model";
            modelGo.transform.SetParent(rootGo.transform, false);
            modelGo.transform.localPosition = Vector3.zero;
            modelGo.transform.localRotation = Quaternion.identity;
            modelGo.transform.localScale = Vector3.one;

            var modelCol = modelGo.GetComponent<Collider>();
            if (modelCol != null) Object.DestroyImmediate(modelCol);

            var mr = modelGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (matBall != null) mr.sharedMaterial = matBall;
                mr.shadowCastingMode = ShadowCastingMode.On;
                mr.receiveShadows = true;
            }

            // 3. Child 'trail' (Container for Trail_Outer and Trail_Inner)
            GameObject trailGo = new GameObject("trail");
            trailGo.transform.SetParent(rootGo.transform, false);
            trailGo.transform.localPosition = Vector3.zero;
            trailGo.transform.localRotation = Quaternion.identity;
            trailGo.transform.localScale = Vector3.one;

            GameObject outerObj = new GameObject("Trail_Outer");
            outerObj.transform.SetParent(trailGo.transform, false);
            outerObj.transform.localPosition = Vector3.zero;
            var outerTr = outerObj.AddComponent<TrailRenderer>();

            GameObject innerObj = new GameObject("Trail_Inner");
            innerObj.transform.SetParent(trailGo.transform, false);
            innerObj.transform.localPosition = Vector3.zero;
            var innerTr = innerObj.AddComponent<TrailRenderer>();

            // 4. Child 'vfx' (Persistent Aura / Body VFX container for future ball skins)
            GameObject vfxGo = new GameObject("vfx");
            vfxGo.transform.SetParent(rootGo.transform, false);
            vfxGo.transform.localPosition = Vector3.zero;
            vfxGo.transform.localRotation = Quaternion.identity;
            vfxGo.transform.localScale = Vector3.one;
            vfxGo.SetActive(false);

            if (matTrail != null)
            {
                outerTr.sharedMaterial = matTrail;
                innerTr.sharedMaterial = matTrail;
            }
            ConfigureTrailRenderer(outerTr, 0.22f, 0.55f);
            ConfigureTrailRenderer(innerTr, 0.16f, 0.25f);

            // 5. Setup BallController on Root
            var ballCtrl = rootGo.AddComponent<BallController>();
            var ballSo = new SerializedObject(ballCtrl);
            ballSo.FindProperty("rb").objectReferenceValue = rb;
            ballSo.FindProperty("modelChild").objectReferenceValue = modelGo;
            ballSo.FindProperty("trailChild").objectReferenceValue = trailGo;
            ballSo.FindProperty("vfxChild").objectReferenceValue = vfxGo;
            ballSo.FindProperty("outerTrail").objectReferenceValue = outerTr;
            ballSo.FindProperty("innerTrail").objectReferenceValue = innerTr;
            ballSo.ApplyModifiedProperties();

            // Save Prefab
            PrefabUtility.SaveAsPrefabAsset(rootGo, BALL_PREFAB_PATH);
            Object.DestroyImmediate(rootGo);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>Standard Ball prefab (PF_Ball_Standard) successfully generated at " + BALL_PREFAB_PATH + "!</color>");
        }

        private static void ConfigureTrailRenderer(TrailRenderer tr, float time, float startWidth)
        {
            tr.time = time;
            tr.minVertexDistance = 0.05f;
            tr.numCornerVertices = 4;
            tr.numCapVertices = 4;
            tr.alignment = LineAlignment.View;
            tr.generateLightingData = false;
            tr.shadowCastingMode = ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.autodestruct = false;
            tr.emitting = false;
            tr.widthCurve = new AnimationCurve(new Keyframe(0f, startWidth, 0f, -startWidth * 1.5f), new Keyframe(1f, 0f, 0f, 0f));
        }
    }
}
