using Arcade.BlockBreaker;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arcade.Tests
{
    [TestFixture]
    public class WallPrefabTests
    {
        private const string WallPrefabPath = "Assets/Prefabs/Arena/PF_Walls.prefab";

        [Test]
        public void WallPrefab_ExistsOnDisk()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(WallPrefabPath);
            Assert.IsNotNull(go, "PF_Walls.prefab must exist in Assets/Prefabs/Arena/.");
        }

        [Test]
        public void WallPrefab_HasArenaWallsComponent_AndAllFiveSections()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(WallPrefabPath);
            Assert.IsNotNull(go, "Prefab must exist.");

            var arenaWalls = go.GetComponent<ArenaWalls>();
            Assert.IsNotNull(arenaWalls, "PF_Walls root must have ArenaWalls component.");

            Assert.IsNotNull(arenaWalls.SideLeft, "SideLeft section must be assigned.");
            Assert.IsNotNull(arenaWalls.SideRight, "SideRight section must be assigned.");
            Assert.IsNotNull(arenaWalls.Top, "Top section must be assigned.");
            Assert.IsNotNull(arenaWalls.ChamferLeft, "ChamferLeft section must be assigned.");
            Assert.IsNotNull(arenaWalls.ChamferRight, "ChamferRight section must be assigned.");
        }

        [Test]
        public void WallPrefab_EachSection_HasSeparatedVisualModelAndPhysicsCollider()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(WallPrefabPath);
            Assert.IsNotNull(go, "Prefab must exist.");

            string[] sectionNames = { "side left", "side right", "top", "chamfer left", "chamfer right" };

            foreach (var name in sectionNames)
            {
                var section = go.transform.Find(name);
                Assert.IsNotNull(section, $"Section '{name}' must exist under PF_Walls.");

                // 1. Model child
                var model = section.Find("model");
                Assert.IsNotNull(model, $"Section '{name}' must have child 'model'.");
                Assert.IsNotNull(model.GetComponent<MeshFilter>(), $"'{name}/model' must have MeshFilter.");
                Assert.IsNotNull(model.GetComponent<MeshRenderer>(), $"'{name}/model' must have MeshRenderer.");
                Assert.IsNull(model.GetComponent<Collider>(), $"'{name}/model' must NOT have a Collider (colliders must be decoupled).");

                // 2. Collider child
                var collider = section.Find("collider");
                Assert.IsNotNull(collider, $"Section '{name}' must have child 'collider'.");
                var box = collider.GetComponent<BoxCollider>();
                Assert.IsNotNull(box, $"'{name}/collider' must have a BoxCollider.");
                Assert.IsNull(collider.GetComponent<MeshRenderer>(), $"'{name}/collider' must NOT have a MeshRenderer.");
                Assert.IsNotNull(box.sharedMaterial, $"'{name}/collider' BoxCollider must have a physics bounce material.");
                Assert.AreEqual(1f, box.sharedMaterial.bounciness, 0.01f, "Physics material bounciness must be 1.0.");
            }
        }

        [Test]
        public void WallPrefab_CalibratedDimensionsAndPositions()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(WallPrefabPath);
            Assert.IsNotNull(go, "Prefab must exist.");

            var arenaWalls = go.GetComponent<ArenaWalls>();

            // Side Left
            Assert.AreEqual(-10.25f, arenaWalls.SideLeft.transform.localPosition.x, 0.01f);
            Assert.AreEqual(7.60f, arenaWalls.SideLeft.transform.localPosition.y, 0.01f);
            Assert.AreEqual(30.2f, arenaWalls.SideLeftCollider.size.y, 0.01f);

            // Side Right
            Assert.AreEqual(10.25f, arenaWalls.SideRight.transform.localPosition.x, 0.01f);
            Assert.AreEqual(7.60f, arenaWalls.SideRight.transform.localPosition.y, 0.01f);
            Assert.AreEqual(30.2f, arenaWalls.SideRightCollider.size.y, 0.01f);

            // Top
            Assert.AreEqual(0.00f, arenaWalls.Top.transform.localPosition.x, 0.01f);
            Assert.AreEqual(24.25f, arenaWalls.Top.transform.localPosition.y, 0.01f);
            Assert.AreEqual(17.4f, arenaWalls.TopCollider.size.x, 0.01f);

            // Chamfer Left
            Assert.AreEqual(-9.40f, arenaWalls.ChamferLeft.transform.localPosition.x, 0.01f);
            Assert.AreEqual(23.40f, arenaWalls.ChamferLeft.transform.localPosition.y, 0.01f);
            Assert.AreEqual(45f, arenaWalls.ChamferLeft.transform.localEulerAngles.z, 0.5f);
            Assert.AreEqual(2.5f, arenaWalls.ChamferLeftCollider.size.x, 0.01f);

            // Chamfer Right
            Assert.AreEqual(9.40f, arenaWalls.ChamferRight.transform.localPosition.x, 0.01f);
            Assert.AreEqual(23.40f, arenaWalls.ChamferRight.transform.localPosition.y, 0.01f);
            Assert.AreEqual(315f, arenaWalls.ChamferRight.transform.localEulerAngles.z, 0.5f); // 315 deg = -45 deg
            Assert.AreEqual(2.5f, arenaWalls.ChamferRightCollider.size.x, 0.01f);
        }
    }
}
