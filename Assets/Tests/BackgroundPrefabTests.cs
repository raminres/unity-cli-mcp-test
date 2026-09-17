using Arcade.BlockBreaker;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arcade.Tests
{
    [TestFixture]
    public class BackgroundPrefabTests
    {
        private const string BackgroundPrefabPath = "Assets/Prefabs/Arena/PF_Background.prefab";

        [Test]
        public void BackgroundPrefab_ExistsOnDisk()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(BackgroundPrefabPath);
            Assert.IsNotNull(go, "PF_Background.prefab must exist in Assets/Prefabs/Arena/.");
        }

        [Test]
        public void BackgroundPrefab_HasControllerAndTextures()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(BackgroundPrefabPath);
            Assert.IsNotNull(go, "Prefab must exist.");

            var ctrl = go.GetComponent<LevelBackgroundController>();
            Assert.IsNotNull(ctrl, "PF_Background root must have LevelBackgroundController component.");

            Assert.IsNotNull(ctrl.BackgroundTextures, "BackgroundTextures must be configured.");
            Assert.AreEqual(4, ctrl.BackgroundTextures.Length, "Must have 4 gradient textures assigned.");
            for (int i = 0; i < 4; i++)
            {
                Assert.IsNotNull(ctrl.BackgroundTextures[i], $"Texture at index {i} must not be null.");
            }

            Assert.IsNotNull(ctrl.PlaneChild, "PlaneChild reference must be linked.");
            Assert.IsNotNull(ctrl.TargetRenderer, "TargetRenderer reference must be linked.");
        }

        [Test]
        public void BackgroundPrefab_ChildPlane_HasQuadMeshAndNoCollider()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(BackgroundPrefabPath);
            Assert.IsNotNull(go, "Prefab must exist.");

            var plane = go.transform.Find("plane");
            Assert.IsNotNull(plane, "PF_Background must have child 'plane'.");

            var mf = plane.GetComponent<MeshFilter>();
            Assert.IsNotNull(mf, "Child 'plane' must have MeshFilter.");
            Assert.IsNotNull(mf.sharedMesh, "MeshFilter must have sharedMesh.");
            Assert.IsTrue(mf.sharedMesh.name.Contains("Quad"), "Mesh must be a Quad.");

            var mr = plane.GetComponent<MeshRenderer>();
            Assert.IsNotNull(mr, "Child 'plane' must have MeshRenderer.");
            Assert.IsNotNull(mr.sharedMaterial, "MeshRenderer must have a background material assigned.");

            Assert.IsNull(plane.GetComponent<Collider>(), "Child 'plane' must NOT have a Collider.");
        }

        [Test]
        public void BackgroundPrefab_Instantiation_AppliesGradientAndCanCycle()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BackgroundPrefabPath);
            Assert.IsNotNull(prefab, "Prefab must exist.");

            var instance = Object.Instantiate(prefab);
            var ctrl = instance.GetComponent<LevelBackgroundController>();
            Assert.IsNotNull(ctrl, "LevelBackgroundController must exist on instance.");

            // Verify initial texture application
            Assert.IsNotNull(ctrl.CurrentTexture, "CurrentTexture must be initialized on start.");

            // Test explicit switching
            ctrl.SetBackgroundByIndex(2);
            Assert.AreEqual(2, ctrl.CurrentTextureIndex, "CurrentTextureIndex must update to 2.");
            Assert.AreEqual(ctrl.BackgroundTextures[2], ctrl.CurrentTexture);

            // Test randomization
            ctrl.RandomizeBackground(avoidSameAsCurrent: true);
            Assert.AreNotEqual(2, ctrl.CurrentTextureIndex, "RandomizeBackground must pick a different texture.");

            Object.DestroyImmediate(instance);
        }
    }
}
