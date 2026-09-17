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
        public void BlockPrefab_HasModularChildHierarchy()
        {
            string[] prefabPaths = { BasePrefabPath, RedPrefabPath, GreenPrefabPath, BluePrefabPath, BombPrefabPath, GlassPrefabPath };

            foreach (var path in prefabPaths)
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(root, $"Prefab at {path} should load.");

                // Root should have BoxCollider & Block component
                Assert.IsNotNull(root.GetComponent<BoxCollider>(), $"{path} root must have BoxCollider.");
                var block = root.GetComponent<Block>();
                Assert.IsNotNull(block, $"{path} root must have Block component.");

                // 4 Modular Child Objects
                var brick = root.transform.Find("brick");
                Assert.IsNotNull(brick, $"{path} must have child 'brick' (3D model).");
                Assert.IsNotNull(brick.GetComponent<MeshFilter>(), $"{path} child 'brick' must have MeshFilter.");
                Assert.IsNotNull(brick.GetComponent<MeshRenderer>(), $"{path} child 'brick' must have MeshRenderer.");

                var frost = root.transform.Find("brick frost");
                Assert.IsNotNull(frost, $"{path} must have child 'brick frost' (3D model).");
                Assert.IsNotNull(frost.GetComponent<MeshFilter>(), $"{path} child 'brick frost' must have MeshFilter.");
                Assert.IsNotNull(frost.GetComponent<MeshRenderer>(), $"{path} child 'brick frost' must have MeshRenderer.");

                var special = root.transform.Find("brick special");
                Assert.IsNotNull(special, $"{path} must have child 'brick special' (UI/sprite element).");
                Assert.IsNotNull(special.GetComponent<PanelRenderer>(), $"{path} child 'brick special' must have PanelRenderer.");
                Assert.IsNotNull(special.GetComponent<BlockBadge>(), $"{path} child 'brick special' must have BlockBadge.");

                var vfx = root.transform.Find("brick vfx");
                Assert.IsNotNull(vfx, $"{path} must have child 'brick vfx' (VFX anchor).");

                // Modular property binding
                Assert.AreEqual(brick.gameObject, block.BrickModel, $"{path} block.BrickModel must point to 'brick'.");
                Assert.AreEqual(frost.gameObject, block.BrickFrost, $"{path} block.BrickFrost must point to 'brick frost'.");
                Assert.AreEqual(special.gameObject, block.BrickSpecial, $"{path} block.BrickSpecial must point to 'brick special'.");
                Assert.AreEqual(vfx.gameObject, block.BrickVfx, $"{path} block.BrickVfx must point to 'brick vfx'.");
            }
        }

        [Test]
        public void BlockPrefab_ColorVariants_ConfiguredCorrectly()
        {
            var redGo = AssetDatabase.LoadAssetAtPath<GameObject>(RedPrefabPath);
            var redBlock = redGo.GetComponent<Block>();
            Assert.AreEqual(BlockColorTier.Red, redBlock.Tier);
            Assert.AreEqual(10, redBlock.Points);
            Assert.IsTrue(redGo.transform.Find("brick").GetComponent<MeshRenderer>().sharedMaterial.name.Contains("MI_Block_Red"));
            Assert.IsTrue(PrefabUtility.IsPartOfVariantPrefab(redGo), "PF_Block_Red must be a Prefab Variant.");

            var greenGo = AssetDatabase.LoadAssetAtPath<GameObject>(GreenPrefabPath);
            var greenBlock = greenGo.GetComponent<Block>();
            Assert.AreEqual(BlockColorTier.Green, greenBlock.Tier);
            Assert.AreEqual(20, greenBlock.Points);
            Assert.IsTrue(greenGo.transform.Find("brick").GetComponent<MeshRenderer>().sharedMaterial.name.Contains("MI_Block_Green"));
            Assert.IsTrue(PrefabUtility.IsPartOfVariantPrefab(greenGo), "PF_Block_Green must be a Prefab Variant.");

            var blueGo = AssetDatabase.LoadAssetAtPath<GameObject>(BluePrefabPath);
            var blueBlock = blueGo.GetComponent<Block>();
            Assert.AreEqual(BlockColorTier.Blue, blueBlock.Tier);
            Assert.AreEqual(30, blueBlock.Points);
            Assert.IsTrue(blueGo.transform.Find("brick").GetComponent<MeshRenderer>().sharedMaterial.name.Contains("MI_Block_Blue"));
            Assert.IsTrue(PrefabUtility.IsPartOfVariantPrefab(blueGo), "PF_Block_Blue must be a Prefab Variant.");
        }

        [Test]
        public void BlockPrefab_Bomb_ConfiguredCorrectly()
        {
            var bombGo = AssetDatabase.LoadAssetAtPath<GameObject>(BombPrefabPath);
            var bombBlock = bombGo.GetComponent<Block>();
            Assert.AreEqual(BlockSpecialType.Bomb, bombBlock.SpecialType);
            Assert.IsTrue(PrefabUtility.IsPartOfVariantPrefab(bombGo), "PF_Block_Bomb must be a Prefab Variant.");

            var specialTransform = bombGo.transform.Find("brick special");
            Assert.IsNotNull(specialTransform, "PF_Block_Bomb must have a 'brick special' child.");
            Assert.IsTrue(specialTransform.gameObject.activeSelf, "'brick special' must be active on PF_Block_Bomb.");

            var badge = specialTransform.GetComponent<BlockBadge>();
            Assert.IsNotNull(badge, "'brick special' must have a BlockBadge component.");
            Assert.AreEqual(BlockSpecialType.Bomb, badge.SpecialType);
        }

        [Test]
        public void BlockPrefab_Glass_ConfiguredCorrectly()
        {
            var glassGo = AssetDatabase.LoadAssetAtPath<GameObject>(GlassPrefabPath);
            var glassBlock = glassGo.GetComponent<Block>();
            Assert.AreEqual(BlockSpecialType.GlassEnclosed, glassBlock.SpecialType);
            Assert.AreEqual(2, glassBlock.HitPoints, "Glass block must have 2 hitpoints.");
            Assert.AreEqual(2, glassBlock.ScoreMultiplier, "Glass block must have 2x multiplier.");
            Assert.IsTrue(PrefabUtility.IsPartOfVariantPrefab(glassGo), "PF_Block_Glass must be a Prefab Variant.");

            var frostTransform = glassGo.transform.Find("brick frost");
            Assert.IsNotNull(frostTransform, "PF_Block_Glass must have a 'brick frost' child GameObject.");
            Assert.IsTrue(frostTransform.gameObject.activeSelf, "'brick frost' must be active on PF_Block_Glass.");

            var frostRenderer = frostTransform.GetComponent<MeshRenderer>();
            Assert.IsNotNull(frostRenderer, "'brick frost' must have a MeshRenderer.");
            Assert.IsTrue(frostRenderer.sharedMaterial.name.Contains("MI_Block_Glass"));

            Assert.IsNull(frostTransform.GetComponent<Collider>(), "'brick frost' should not have its own collider (root has collider).");
            Assert.AreEqual(frostTransform.gameObject, glassBlock.GlassShell, "glassBlock.GlassShell must reference the 'brick frost' child.");
        }

        [Test]
        public void LevelGenerator_GeneratesBlocksFromModularPrefabs()
        {
            var genGo = new GameObject("TestLevelGen");
            var gen = genGo.AddComponent<LevelGenerator>();

            var pRed = AssetDatabase.LoadAssetAtPath<GameObject>(RedPrefabPath);
            var pGreen = AssetDatabase.LoadAssetAtPath<GameObject>(GreenPrefabPath);
            var pBlue = AssetDatabase.LoadAssetAtPath<GameObject>(BluePrefabPath);
            var pBomb = AssetDatabase.LoadAssetAtPath<GameObject>(BombPrefabPath);
            var pGlass = AssetDatabase.LoadAssetAtPath<GameObject>(GlassPrefabPath);
            gen.SetBlockPrefabs(pRed, pGreen, pBlue, pBomb, pGlass);

            var config = ScriptableObject.CreateInstance<LevelConfiguration>();
            config.SetColumns(4);
            config.SetRowsPerTier(1);
            config.SetPaddleExpanderCount(0);
            config.SetMultiplier2xCount(0);
            config.SetBombCount(1);
            config.SetGlassEnclosedCount(1);

            gen.LoadLevel(config);

            var container = genGo.transform.Find("BlocksContainer");
            Assert.IsNotNull(container, "BlocksContainer must be created.");
            Assert.AreEqual(config.TotalBlocks, container.childCount, "Container must match config.TotalBlocks.");

            bool foundBomb = false;
            bool foundGlass = false;

            for (int i = 0; i < container.childCount; i++)
            {
                var blockObj = container.GetChild(i).gameObject;
                var block = blockObj.GetComponent<Block>();
                Assert.IsNotNull(block, "Each block object must have a Block component.");

                // Check modular child hierarchy exists on every block
                Assert.IsNotNull(block.BrickModel, "Every instantiated block must have modular BrickModel resolved.");
                Assert.IsNotNull(block.BrickFrost, "Every instantiated block must have modular BrickFrost resolved.");
                Assert.IsNotNull(block.BrickSpecial, "Every instantiated block must have modular BrickSpecial resolved.");
                Assert.IsNotNull(block.BrickVfx, "Every instantiated block must have modular BrickVfx resolved.");

                if (block.SpecialType == BlockSpecialType.Bomb)
                {
                    foundBomb = true;
                    Assert.IsTrue(block.BrickSpecial.activeSelf, "Bomb block must have active BrickSpecial.");
                }
                else if (block.SpecialType == BlockSpecialType.GlassEnclosed)
                {
                    foundGlass = true;
                    Assert.IsTrue(block.BrickFrost.activeSelf, "Glass block must have active BrickFrost.");
                }
            }

            Assert.IsTrue(foundBomb, "Bomb block must have been spawned from prefab.");
            Assert.IsTrue(foundGlass, "Glass block must have been spawned from prefab.");

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(genGo);
        }

        [Test]
        public void LevelGenerator_EnsureCornerChamfers_PreservesArenaWalls()
        {
            var wallsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Arena/PF_Walls.prefab");
            Assert.IsNotNull(wallsPrefab, "PF_Walls prefab must exist.");

            var wallsGo = Object.Instantiate(wallsPrefab);
            wallsGo.name = "Boundaries";

            int initialChildCount = wallsGo.transform.childCount;

            var genGo = new GameObject("TestLevelGen");
            var gen = genGo.AddComponent<LevelGenerator>();

            gen.EnsureCornerChamfers();

            Assert.AreEqual(initialChildCount, wallsGo.transform.childCount, "EnsureCornerChamfers must not add procedural cubes to PF_Walls.");
            Assert.IsNull(wallsGo.transform.Find("Chamfer_TopLeft"), "No procedural Chamfer_TopLeft should be created on PF_Walls.");
            Assert.IsNull(wallsGo.transform.Find("Chamfer_TopRight"), "No procedural Chamfer_TopRight should be created on PF_Walls.");

            Object.DestroyImmediate(wallsGo);
            Object.DestroyImmediate(genGo);
        }
    }
}
