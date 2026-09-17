using Arcade.BlockBreaker;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arcade.Tests
{
    [TestFixture]
    public class BallPrefabTests
    {
        private const string BallPrefabPath = "Assets/Prefabs/Balls/PF_Ball_Standard.prefab";

        [Test]
        public void BallPrefab_ExistsOnDisk()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(BallPrefabPath);
            Assert.IsNotNull(go, "PF_Ball_Standard.prefab must exist in Assets/Prefabs/Balls/.");
        }

        [Test]
        public void BallPrefab_Root_HasPhysicsAndControllerComponents()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(BallPrefabPath);
            Assert.IsNotNull(go, "Prefab must exist.");

            // SphereCollider
            var col = go.GetComponent<SphereCollider>();
            Assert.IsNotNull(col, "Root must have SphereCollider.");
            Assert.IsFalse(col.isTrigger, "SphereCollider must not be a trigger.");
            Assert.AreEqual(0.5f, col.radius, 0.01f, "SphereCollider radius should be 0.5.");
            Assert.IsNotNull(col.sharedMaterial, "SphereCollider must have PM_ArcadeBounce physics material.");

            // Rigidbody
            var rb = go.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb, "Root must have Rigidbody.");
            Assert.IsFalse(rb.isKinematic, "Rigidbody must not be kinematic.");
            Assert.IsFalse(rb.useGravity, "Rigidbody must not use gravity.");
            Assert.AreEqual(CollisionDetectionMode.Continuous, rb.collisionDetectionMode, "Collision detection must be Continuous.");
            Assert.IsTrue((rb.constraints & RigidbodyConstraints.FreezePositionZ) != 0, "Must freeze position Z.");
            Assert.IsTrue((rb.constraints & RigidbodyConstraints.FreezeRotation) != 0, "Must freeze rotation.");

            // BallTrail & BallController
            var trail = go.GetComponent<BallTrail>();
            Assert.IsNotNull(trail, "Root must have BallTrail component.");

            var ctrl = go.GetComponent<BallController>();
            Assert.IsNotNull(ctrl, "Root must have BallController component.");
            Assert.IsNotNull(ctrl.ModelChild, "BallController.ModelChild must be linked.");
            Assert.IsNotNull(ctrl.TrailChild, "BallController.TrailChild must be linked.");
            Assert.IsNotNull(ctrl.VfxChild, "BallController.VfxChild must be linked.");
        }

        [Test]
        public void BallPrefab_Children_HasModelTrailAndVfxHierarchy()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(BallPrefabPath);
            Assert.IsNotNull(go, "Prefab must exist.");

            // 1. Model child
            var model = go.transform.Find("model");
            Assert.IsNotNull(model, "Ball prefab must have child 'model'.");
            var mf = model.GetComponent<MeshFilter>();
            Assert.IsNotNull(mf, "Child 'model' must have MeshFilter.");
            Assert.IsTrue(mf.sharedMesh.name.Contains("Sphere"), "Mesh must be a Sphere.");
            var mr = model.GetComponent<MeshRenderer>();
            Assert.IsNotNull(mr, "Child 'model' must have MeshRenderer.");
            Assert.IsNull(model.GetComponent<Collider>(), "Child 'model' must NOT have a collider (collider belongs on root).");

            // 2. Trail child
            var trail = go.transform.Find("trail");
            Assert.IsNotNull(trail, "Ball prefab must have child 'trail'.");
            var outerTrail = trail.Find("Trail_Outer");
            Assert.IsNotNull(outerTrail, "Child 'trail' must have child 'Trail_Outer'.");
            Assert.IsNotNull(outerTrail.GetComponent<TrailRenderer>(), "Trail_Outer must have TrailRenderer.");
            var innerTrail = trail.Find("Trail_Inner");
            Assert.IsNotNull(innerTrail, "Child 'trail' must have child 'Trail_Inner'.");
            Assert.IsNotNull(innerTrail.GetComponent<TrailRenderer>(), "Trail_Inner must have TrailRenderer.");

            // 3. VFX child
            var vfx = go.transform.Find("vfx");
            Assert.IsNotNull(vfx, "Ball prefab must have child 'vfx'.");
            Assert.IsFalse(vfx.gameObject.activeSelf, "Child 'vfx' should be inactive by default until a skin VFX is equipped.");
        }

        [Test]
        public void BallPrefab_InstantiationAndColoring_WorksCorrectly()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BallPrefabPath);
            Assert.IsNotNull(prefab, "Prefab must exist.");

            var instance = Object.Instantiate(prefab);
            var ctrl = instance.GetComponent<BallController>();
            Assert.IsNotNull(ctrl, "BallController must exist on instance.");

            // Test setting trail color & ball emission tinting
            ctrl.SetTrailColor(Color.magenta);

            // Test SetBallActive enables/disables child model renderer
            var model = instance.transform.Find("model");
            var mr = model.GetComponent<MeshRenderer>();

            ctrl.SetBallActive(false);
            Assert.IsFalse(mr.enabled, "SetBallActive(false) must disable child model renderer.");

            ctrl.SetBallActive(true);
            Assert.IsTrue(mr.enabled, "SetBallActive(true) must enable child model renderer.");

            Object.DestroyImmediate(instance);
        }
    }
}
