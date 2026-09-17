using Arcade.BlockBreaker;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arcade.Tests
{
    [TestFixture]
    public class BlockPrefabTests
    {
        private const string BasePrefabPath = "Assets/Prefabs/Blocks/PF_Block_Base.prefab";
        private const string RedPrefabPath = "Assets/Prefabs/Blocks/PF_Block_Red.prefab";
        private const string GreenPrefabPath = "Assets/Prefabs/Blocks/PF_Block_Green.prefab";
        private const string BluePrefabPath = "Assets/Prefabs/Blocks/PF_Block_Blue.prefab";
        private const string BombPrefabPath = "Assets/Prefabs/Blocks/PF_Block_Bomb.prefab";
        private const string GlassPrefabPath = "Assets/Prefabs/Blocks/PF_Block_Glass.prefab";

        [Test]
        public void BlockPrefabs_AllExistOnDisk()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath), "PF_Block_Base.prefab must exist.");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(RedPrefabPath), "PF_Block_Red.prefab must exist.");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(GreenPrefabPath), "PF_Block_Green.prefab must exist.");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(BluePrefabPath), "PF_Block_Blue.prefab must exist.");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(BombPrefabPath), "PF_Block_Bomb.prefab must exist.");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(GlassPrefabPath), "PF_Block_Glass.prefab must exist.");
        }

        [Test]
        public void BlockPrefab_Base_HasRequiredComponents()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
            Assert.IsNotNull(go.GetComponent<BoxCollider>(), "Base block must have a BoxCollider.");
            Assert.IsNotNull(go.GetComponent<MeshFilter>(), "Base block must have a MeshFilter.");
            Assert.IsNotNull(go.GetComponent<MeshRenderer>(), "Base block must have a MeshRenderer.");

            var block = go.GetComponent<Block>();
            Assert.IsNotNull(block, "Base block must have a Block component.");
            Assert.AreEqual(1, block.HitPoints, "Base block default hitpoints should be 1.");
        }

        [Test]
        public void BlockPrefab_ColorVariants_ConfiguredCorrectly()
        {
            var redGo = AssetDatabase.LoadAssetAtPath<GameObject>(RedPrefabPath);
            var redBlock = redGo.GetComponent<Block>();
            Assert.AreEqual(BlockColorTier.Red, redBlock.Tier);
            Assert.AreEqual(10, redBlock.Points);
            Assert.IsTrue(redGo.GetComponent<MeshRenderer>().sharedMaterial.name.Contains("MI_Block_Red"));
            Assert.IsTrue(PrefabUtility.IsPartOfVariantPrefab(redGo), "PF_Block_Red must be a Prefab Variant.");

            var greenGo = AssetDatabase.LoadAssetAtPath<GameObject>(GreenPrefabPath);
            var greenBlock = greenGo.GetComponent<Block>();
            Assert.AreEqual(BlockColorTier.Green, greenBlock.Tier);
            Assert.AreEqual(20, greenBlock.Points);
            Assert.IsTrue(greenGo.GetComponent<MeshRenderer>().sharedMaterial.name.Contains("MI_Block_Green"));
            Assert.IsTrue(PrefabUtility.IsPartOfVariantPrefab(greenGo), "PF_Block_Green must be a Prefab Variant.");

            var blueGo = AssetDatabase.LoadAssetAtPath<GameObject>(BluePrefabPath);
            var blueBlock = blueGo.GetComponent<Block>();
            Assert.AreEqual(BlockColorTier.Blue, blueBlock.Tier);
            Assert.AreEqual(30, blueBlock.Points);
            Assert.IsTrue(blueGo.GetComponent<MeshRenderer>().sharedMaterial.name.Contains("MI_Block_Blue"));
            Assert.IsTrue(PrefabUtility.IsPartOfVariantPrefab(blueGo), "PF_Block_Blue must be a Prefab Variant.");
        }

        [Test]
        public void BlockPrefab_Bomb_HasBadgeAndBombSpecialType()
        {
            var bombGo = AssetDatabase.LoadAssetAtPath<GameObject>(BombPrefabPath);
            var bombBlock = bombGo.GetComponent<Block>();
            Assert.AreEqual(BlockSpecialType.Bomb, bombBlock.SpecialType);
            Assert.IsTrue(PrefabUtility.IsPartOfVariantPrefab(bombGo), "PF_Block_Bomb must be a Prefab Variant.");

            var badgeTransform = bombGo.transform.Find("UI_Badge");
            Assert.IsNotNull(badgeTransform, "PF_Block_Bomb must have a UI_Badge child GameObject.");

            var badge = badgeTransform.GetComponent<BlockBadge>();
            Assert.IsNotNull(badge, "UI_Badge must have a BlockBadge component.");
            Assert.AreEqual(BlockSpecialType.Bomb, badge.SpecialType);

            var panelRenderer = badgeTransform.GetComponent<PanelRenderer>();
            Assert.IsNotNull(panelRenderer, "UI_Badge must have a PanelRenderer component.");
        }

        [Test]
        public void BlockPrefab_Glass_HasGlassShellAndProperties()
        {
            var glassGo = AssetDatabase.LoadAssetAtPath<GameObject>(GlassPrefabPath);
            var glassBlock = glassGo.GetComponent<Block>();
            Assert.AreEqual(BlockSpecialType.GlassEnclosed, glassBlock.SpecialType);
            Assert.AreEqual(2, glassBlock.HitPoints, "Glass block must have 2 hitpoints.");
            Assert.AreEqual(2, glassBlock.ScoreMultiplier, "Glass block must have 2x multiplier.");
            Assert.IsTrue(PrefabUtility.IsPartOfVariantPrefab(glassGo), "PF_Block_Glass must be a Prefab Variant.");

            var shellTransform = glassGo.transform.Find("Glass_Shell");
            Assert.IsNotNull(shellTransform, "PF_Block_Glass must have a Glass_Shell child GameObject.");

            var shellRenderer = shellTransform.GetComponent<MeshRenderer>();
            Assert.IsNotNull(shellRenderer, "Glass_Shell must have a MeshRenderer.");
            Assert.IsTrue(shellRenderer.sharedMaterial.name.Contains("MI_Block_Glass"));

            Assert.IsNull(shellTransform.GetComponent<Collider>(), "Glass_Shell should not have its own collider (root has collider).");
            Assert.AreEqual(shellTransform.gameObject, glassBlock.GlassShell, "glassBlock.GlassShell must reference the Glass_Shell child.");
        }
    }
}
