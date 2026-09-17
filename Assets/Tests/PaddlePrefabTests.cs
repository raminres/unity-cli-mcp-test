using Arcade.BlockBreaker;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arcade.Tests
{
    public class PaddlePrefabTests
    {
        public const string PaddlePrefabPath = "Assets/Prefabs/Paddle/PF_Paddle.prefab";

        [Test]
        public void PaddlePrefab_ExistsOnDisk()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(go, $"Paddle prefab must exist at '{PaddlePrefabPath}'. Run 'Tools > Arcade > Generate Paddle Prefabs' if missing.");
        }

        [Test]
        public void PaddlePrefab_Root_HasPhysicsAndControllers()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(go, "Prefab must exist.");

            var col = go.GetComponent<BoxCollider>();
            Assert.IsNotNull(col, "Root must have a BoxCollider.");
            Assert.AreEqual(0.38f, col.center.y, 0.01f, "BoxCollider center Y must align with strike deck.");
            Assert.AreEqual(1.0f, col.size.x, 0.01f, "BoxCollider width must be normalized (1.0).");
            Assert.AreEqual(0.24f, col.size.y, 0.01f, "BoxCollider height must match strike deck thickness.");

            var rb = go.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb, "Root must have a Rigidbody.");
            Assert.IsTrue(rb.isKinematic, "Rigidbody must be kinematic.");
            Assert.AreEqual(CollisionDetectionMode.ContinuousSpeculative, rb.collisionDetectionMode);

            var paddleCtrl = go.GetComponent<PaddleController>();
            Assert.IsNotNull(paddleCtrl, "Root must have PaddleController.");

            var laserCtrl = go.GetComponent<PaddleLaserController>();
            Assert.IsNotNull(laserCtrl, "Root must have PaddleLaserController.");
        }

        [Test]
        public void PaddlePrefab_Children_HasCompleteHierarchyAndNoCollidersOnVisuals()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(go, "Prefab must exist.");

            // 1. visuals
            var visuals = go.transform.Find("visuals");
            Assert.IsNotNull(visuals, "Paddle must have 'visuals' container child.");
            var deck = visuals.Find("deck");
            var chassis = visuals.Find("chassis");
            var keel = visuals.Find("keel");
            Assert.IsNotNull(deck, "visuals must contain 'deck' (paddle top).");
            Assert.IsNotNull(chassis, "visuals must contain 'chassis' (paddle mid).");
            Assert.IsNotNull(keel, "visuals must contain 'keel' (paddle bottom).");

            Assert.IsNull(deck.GetComponent<Collider>(), "deck must not have collider.");
            Assert.IsNull(chassis.GetComponent<Collider>(), "chassis must not have collider.");
            Assert.IsNull(keel.GetComponent<Collider>(), "keel must not have collider.");

            // 2. guns
            var guns = go.transform.Find("guns");
            Assert.IsNotNull(guns, "Paddle must have 'guns' container child.");
            var gunLeft = guns.Find("Gun_Left");
            var gunRight = guns.Find("Gun_Right");
            Assert.IsNotNull(gunLeft, "guns must contain 'Gun_Left'.");
            Assert.IsNotNull(gunRight, "guns must contain 'Gun_Right'.");
            Assert.IsNotNull(gunLeft.Find("Muzzle_Left"), "Gun_Left must have 'Muzzle_Left' child transform.");
            Assert.IsNotNull(gunRight.Find("Muzzle_Right"), "Gun_Right must have 'Muzzle_Right' child transform.");
            Assert.IsNull(gunLeft.GetComponent<Collider>(), "Gun_Left must not have collider.");
            Assert.IsNull(gunRight.GetComponent<Collider>(), "Gun_Right must not have collider.");

            // 3. laser_gun
            var laserGun = go.transform.Find("laser_gun");
            Assert.IsNotNull(laserGun, "Paddle must have 'laser_gun' container child.");
            Assert.IsNotNull(laserGun.Find("Aperture"), "laser_gun must contain 'Aperture'.");
            Assert.IsNotNull(laserGun.Find("VFX_Railgun_HyperBeam"), "laser_gun must contain 'VFX_Railgun_HyperBeam'.");

            // 4. frost
            var frost = go.transform.Find("frost");
            Assert.IsNotNull(frost, "Paddle must have 'frost' container child.");
            var iceShell = frost.Find("Ice_Shell");
            Assert.IsNotNull(iceShell, "frost must contain 'Ice_Shell'.");
            Assert.IsNull(iceShell.GetComponent<Collider>(), "Ice_Shell must not have collider.");

            // 5. vfx
            var vfx = go.transform.Find("vfx");
            Assert.IsNotNull(vfx, "Paddle must have 'vfx' container child.");
            Assert.IsNotNull(vfx.Find("Thruster_Left"), "vfx must contain 'Thruster_Left'.");
            Assert.IsNotNull(vfx.Find("Thruster_Right"), "vfx must contain 'Thruster_Right'.");
            Assert.IsNotNull(vfx.Find("Spark_Anchor"), "vfx must contain 'Spark_Anchor'.");

            // 6. dock_point
            var dockPoint = go.transform.Find("dock_point");
            Assert.IsNotNull(dockPoint, "Paddle must have 'dock_point' transform child.");
        }

        [Test]
        public void PaddlePrefab_Guns_ToggleWithLaserBlasterActivation()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(prefab, "Prefab must exist.");

            var instance = Object.Instantiate(prefab);
            try
            {
                var laserCtrl = instance.GetComponent<PaddleLaserController>();
                Assert.IsNotNull(laserCtrl);

                var guns = instance.transform.Find("guns");
                Assert.IsNotNull(guns);
                Assert.IsFalse(guns.gameObject.activeSelf, "Guns should be inactive by default.");

                laserCtrl.ActivateLaserBlaster(10f);
                Assert.IsTrue(guns.gameObject.activeSelf, "Guns should be active when Laser Blaster is activated.");

                laserCtrl.DeactivateLaserBlaster();
                Assert.IsFalse(guns.gameObject.activeSelf, "Guns should be inactive when Laser Blaster is deactivated.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PaddlePrefab_Frost_TogglesWithFreezeState()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(prefab, "Prefab must exist.");

            var instance = Object.Instantiate(prefab);
            try
            {
                var paddleCtrl = instance.GetComponent<PaddleController>();
                Assert.IsNotNull(paddleCtrl);

                var frost = instance.transform.Find("frost");
                Assert.IsNotNull(frost);
                Assert.IsFalse(frost.gameObject.activeSelf, "Frost should be inactive by default.");

                paddleCtrl.SetFrozen(true);
                Assert.IsTrue(frost.gameObject.activeSelf, "Frost should be active when paddle is frozen.");

                paddleCtrl.SetFrozen(false);
                Assert.IsFalse(frost.gameObject.activeSelf, "Frost should be inactive when paddle is defrosted.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PaddlePrefab_LaserGun_TogglesWithHyperBeam()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(prefab, "Prefab must exist.");

            var instance = Object.Instantiate(prefab);
            try
            {
                var laserCtrl = instance.GetComponent<PaddleLaserController>();
                Assert.IsNotNull(laserCtrl);

                var laserGun = instance.transform.Find("laser_gun");
                Assert.IsNotNull(laserGun);
                Assert.IsFalse(laserGun.gameObject.activeSelf, "laser_gun should be inactive by default.");

                laserCtrl.FireRailgunHyperBeam(5f);
                Assert.IsTrue(laserGun.gameObject.activeSelf, "laser_gun should be active when hyper beam fires.");

                laserCtrl.DeactivateHyperBeam();
                Assert.IsFalse(laserGun.gameObject.activeSelf, "laser_gun should be inactive when hyper beam finishes.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PaddlePrefab_DockPoint_PositionedAboveDeck()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(prefab, "Prefab must exist.");

            var instance = Object.Instantiate(prefab);
            try
            {
                var paddleCtrl = instance.GetComponent<PaddleController>();
                Assert.IsNotNull(paddleCtrl);
                Assert.IsNotNull(paddleCtrl.DockPoint, "DockPoint must be wired.");
                Assert.Greater(paddleCtrl.DockPoint.localPosition.y, 0.38f, "DockPoint must be positioned above top strike deck (Y = 0.38).");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
