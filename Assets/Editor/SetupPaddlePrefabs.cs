using System.IO;
using Arcade.BlockBreaker;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arcade.Editor
{
    public static class SetupPaddlePrefabs
    {
        public const string PREFABS_DIR = "Assets/Prefabs/Paddle";
        public const string PADDLE_PREFAB_PATH = PREFABS_DIR + "/PF_Paddle.prefab";

        public const string MAT_DECK_PATH = "Assets/Materials/BlockBreaker/MI_Paddle_Deck.mat";
        public const string MAT_MID_PATH = "Assets/Materials/BlockBreaker/MI_Paddle.mat";
        public const string MAT_CORE_PATH = "Assets/Materials/BlockBreaker/MI_Paddle_Core.mat";
        public const string MAT_FROST_PATH = "Assets/Materials/BlockBreaker/MI_Block_Glass.mat";
        public const string MAT_HYPERBEAM_PATH = "Assets/Materials/BlockBreaker/MI_LaserHyperBeam.mat";
        public const string PHYS_BOUNCE_PATH = "Assets/Materials/BlockBreaker/PM_ArcadeBounce.physicMaterial";

        [MenuItem("Tools/Arcade/Generate Paddle Prefabs")]
        public static void GeneratePaddlePrefabs()
        {
            if (!Directory.Exists(PREFABS_DIR))
            {
                Directory.CreateDirectory(PREFABS_DIR);
                AssetDatabase.Refresh();
            }

            Material matDeck = AssetDatabase.LoadAssetAtPath<Material>(MAT_DECK_PATH);
            Material matMid = AssetDatabase.LoadAssetAtPath<Material>(MAT_MID_PATH);
            Material matCore = AssetDatabase.LoadAssetAtPath<Material>(MAT_CORE_PATH);
            Material matFrost = AssetDatabase.LoadAssetAtPath<Material>(MAT_FROST_PATH);
            Material matHyperBeam = AssetDatabase.LoadAssetAtPath<Material>(MAT_HYPERBEAM_PATH);
            PhysicsMaterial physBounce = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(PHYS_BOUNCE_PATH);

            GameObject rootGo = new GameObject("PF_Paddle");
            rootGo.transform.position = Vector3.zero;
            rootGo.transform.rotation = Quaternion.identity;
            rootGo.transform.localScale = new Vector3(5.0f, 1.0f, 1.0f);

            // 1. Root BoxCollider (Primary Strike Deck trigger & bounce)
            var rootCol = rootGo.AddComponent<BoxCollider>();
            rootCol.center = new Vector3(0f, 0.38f, 0f);
            rootCol.size = new Vector3(1.0f, 0.24f, 2.8f);
            if (physBounce != null) rootCol.sharedMaterial = physBounce;

            // 2. Root Rigidbody
            var rb = rootGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            // 3. Controllers
            var paddleCtrl = rootGo.AddComponent<PaddleController>();
            var laserCtrl = rootGo.AddComponent<PaddleLaserController>();

            // 4. Child 'visuals' (Stepped Pyramid Tiers - pure meshes, no colliders)
            GameObject visualsGo = new GameObject("visuals");
            visualsGo.transform.SetParent(rootGo.transform, false);

            // Tier 1: Deck (paddle top)
            GameObject deckGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deckGo.name = "deck";
            deckGo.transform.SetParent(visualsGo.transform, false);
            deckGo.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            deckGo.transform.localScale = new Vector3(1.0f, 0.24f, 1.0f);
            Object.DestroyImmediate(deckGo.GetComponent<Collider>());
            if (matDeck != null) deckGo.GetComponent<MeshRenderer>().sharedMaterial = matDeck;

            // Tier 2: Chassis (paddle mid)
            GameObject chassisGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassisGo.name = "chassis";
            chassisGo.transform.SetParent(visualsGo.transform, false);
            chassisGo.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            chassisGo.transform.localScale = new Vector3(0.72f, 0.20f, 0.88f);
            Object.DestroyImmediate(chassisGo.GetComponent<Collider>());
            if (matMid != null) chassisGo.GetComponent<MeshRenderer>().sharedMaterial = matMid;

            // Tier 3: Keel (paddle bottom)
            GameObject keelGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keelGo.name = "keel";
            keelGo.transform.SetParent(visualsGo.transform, false);
            keelGo.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            keelGo.transform.localScale = new Vector3(0.44f, 0.16f, 0.72f);
            Object.DestroyImmediate(keelGo.GetComponent<Collider>());
            if (matCore != null) keelGo.GetComponent<MeshRenderer>().sharedMaterial = matCore;

            // 5. Child 'guns' (Twin Laser Blasters - active during Laser Blaster powerup)
            GameObject gunsGo = new GameObject("guns");
            gunsGo.transform.SetParent(rootGo.transform, false);

            // Left Gun
            GameObject gunLeft = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            gunLeft.name = "Gun_Left";
            gunLeft.transform.SetParent(gunsGo.transform, false);
            gunLeft.transform.localPosition = new Vector3(-0.42f, 0.38f, 0f);
            gunLeft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            gunLeft.transform.localScale = new Vector3(0.10f, 0.22f, 0.10f);
            Object.DestroyImmediate(gunLeft.GetComponent<Collider>());
            if (matDeck != null) gunLeft.GetComponent<MeshRenderer>().sharedMaterial = matDeck;

            GameObject muzzleLeft = new GameObject("Muzzle_Left");
            muzzleLeft.transform.SetParent(gunLeft.transform, false);
            muzzleLeft.transform.localPosition = new Vector3(0f, 0.24f, 0f);

            // Right Gun
            GameObject gunRight = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            gunRight.name = "Gun_Right";
            gunRight.transform.SetParent(gunsGo.transform, false);
            gunRight.transform.localPosition = new Vector3(0.42f, 0.38f, 0f);
            gunRight.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            gunRight.transform.localScale = new Vector3(0.10f, 0.22f, 0.10f);
            Object.DestroyImmediate(gunRight.GetComponent<Collider>());
            if (matDeck != null) gunRight.GetComponent<MeshRenderer>().sharedMaterial = matDeck;

            GameObject muzzleRight = new GameObject("Muzzle_Right");
            muzzleRight.transform.SetParent(gunRight.transform, false);
            muzzleRight.transform.localPosition = new Vector3(0f, 0.24f, 0f);

            gunsGo.SetActive(false);

            // 6. Child 'laser_gun' (Clutch Mode Hyper-Beam Railgun)
            GameObject laserGunGo = new GameObject("laser_gun");
            laserGunGo.transform.SetParent(rootGo.transform, false);

            GameObject apertureGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            apertureGo.name = "Aperture";
            apertureGo.transform.SetParent(laserGunGo.transform, false);
            apertureGo.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            apertureGo.transform.localScale = new Vector3(0.20f, 0.12f, 0.32f);
            Object.DestroyImmediate(apertureGo.GetComponent<Collider>());
            if (matDeck != null) apertureGo.GetComponent<MeshRenderer>().sharedMaterial = matDeck;

            GameObject beamGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            beamGo.name = "VFX_Railgun_HyperBeam";
            beamGo.transform.SetParent(laserGunGo.transform, false);
            beamGo.transform.localPosition = new Vector3(0f, 15.88f, -0.3f);
            beamGo.transform.localScale = new Vector3(3.2f, 31f, 1f);
            Object.DestroyImmediate(beamGo.GetComponent<Collider>());
            var beamRend = beamGo.GetComponent<MeshRenderer>();
            if (beamRend != null)
            {
                if (matHyperBeam != null) beamRend.sharedMaterial = matHyperBeam;
                beamRend.shadowCastingMode = ShadowCastingMode.Off;
                beamRend.receiveShadows = false;
            }
            beamGo.SetActive(false);
            laserGunGo.SetActive(false);

            // 7. Child 'frost' (Ice encasement shell - active during PaddleFreezer hazard)
            GameObject frostGo = new GameObject("frost");
            frostGo.transform.SetParent(rootGo.transform, false);

            GameObject frostMeshGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frostMeshGo.name = "Ice_Shell";
            frostMeshGo.transform.SetParent(frostGo.transform, false);
            frostMeshGo.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            frostMeshGo.transform.localScale = new Vector3(1.04f, 0.62f, 1.08f);
            Object.DestroyImmediate(frostMeshGo.GetComponent<Collider>());
            if (matFrost != null) frostMeshGo.GetComponent<MeshRenderer>().sharedMaterial = matFrost;

            frostGo.SetActive(false);

            // 8. Child 'vfx' (Thrusters and Kinetic Spark origin)
            GameObject vfxGo = new GameObject("vfx");
            vfxGo.transform.SetParent(rootGo.transform, false);

            GameObject thrusterLeft = new GameObject("Thruster_Left");
            thrusterLeft.transform.SetParent(vfxGo.transform, false);
            thrusterLeft.transform.localPosition = new Vector3(-0.15f, -0.10f, 0f);

            GameObject thrusterRight = new GameObject("Thruster_Right");
            thrusterRight.transform.SetParent(vfxGo.transform, false);
            thrusterRight.transform.localPosition = new Vector3(0.15f, -0.10f, 0f);

            GameObject sparkAnchor = new GameObject("Spark_Anchor");
            sparkAnchor.transform.SetParent(vfxGo.transform, false);
            sparkAnchor.transform.localPosition = new Vector3(0f, 0.50f, 0f);

            // 9. Child 'dock_point' (Anchor transform for ball docking at round start)
            GameObject dockPointGo = new GameObject("dock_point");
            dockPointGo.transform.SetParent(rootGo.transform, false);
            dockPointGo.transform.localPosition = new Vector3(0f, 0.88f, 0f);

            // Wire Serialized Properties on PaddleController
            var paddleSo = new SerializedObject(paddleCtrl);
            paddleSo.FindProperty("stepTop").objectReferenceValue = deckGo.transform;
            paddleSo.FindProperty("stepMid").objectReferenceValue = chassisGo.transform;
            paddleSo.FindProperty("stepBottom").objectReferenceValue = keelGo.transform;
            paddleSo.FindProperty("visualsRoot").objectReferenceValue = visualsGo;
            paddleSo.FindProperty("gunsRoot").objectReferenceValue = gunsGo;
            paddleSo.FindProperty("laserGunRoot").objectReferenceValue = laserGunGo;
            paddleSo.FindProperty("frostRoot").objectReferenceValue = frostGo;
            paddleSo.FindProperty("vfxRoot").objectReferenceValue = vfxGo;
            paddleSo.FindProperty("dockPoint").objectReferenceValue = dockPointGo.transform;
            paddleSo.FindProperty("rootCollider").objectReferenceValue = rootCol;
            paddleSo.FindProperty("rb").objectReferenceValue = rb;
            paddleSo.FindProperty("laserController").objectReferenceValue = laserCtrl;
            paddleSo.ApplyModifiedProperties();

            // Wire Serialized Properties on PaddleLaserController
            var laserSo = new SerializedObject(laserCtrl);
            laserSo.FindProperty("gunsRoot").objectReferenceValue = gunsGo;
            laserSo.FindProperty("muzzleLeft").objectReferenceValue = muzzleLeft.transform;
            laserSo.FindProperty("muzzleRight").objectReferenceValue = muzzleRight.transform;
            laserSo.FindProperty("laserGunRoot").objectReferenceValue = laserGunGo;
            laserSo.FindProperty("laserGunAperture").objectReferenceValue = apertureGo;
            if (matHyperBeam != null) laserSo.FindProperty("hyperBeamMaterialAsset").objectReferenceValue = matHyperBeam;
            laserSo.ApplyModifiedProperties();

            // Save Prefab
            PrefabUtility.SaveAsPrefabAsset(rootGo, PADDLE_PREFAB_PATH);
            Object.DestroyImmediate(rootGo);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SetupPaddlePrefabs] Successfully generated PF_Paddle prefab at: {PADDLE_PREFAB_PATH}");
        }
    }
}
