using Arcade.Core;
using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Procedurally generates grids of 1:1 cube blocks organized into tiered color rows.
    /// </summary>
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Grid Layout Settings")]
        [SerializeField] private int columns = 8;
        [SerializeField] private int rowsPerTier = 2; // 2 Red, 2 Green, 2 Blue = 6 rows total
        [SerializeField] private float blockSize = 1.0f;
        [SerializeField] private float horizontalSpacing = 1.25f;
        [SerializeField] private float verticalSpacing = 1.3f;
        [SerializeField] private float startCenterY = 15.5f;

        [Header("Materials")]
        [SerializeField] private Material matRedBlock;
        [SerializeField] private Material matGreenBlock;
        [SerializeField] private Material matBlueBlock;

        [Header("VFX Particle Colors")]
        [SerializeField] private Color vfxRedColor = new Color(1.0f, 0.2f, 0.3f);
        [SerializeField] private Color vfxGreenColor = new Color(0.15f, 0.95f, 0.45f);
        [SerializeField] private Color vfxBlueColor = new Color(0.15f, 0.7f, 1.0f);

        [Header("Parent Container")]
        [SerializeField] private Transform blocksContainer;

        private void Start()
        {
            GenerateLevel();
        }

        public void GenerateLevel()
        {
            if (blocksContainer == null)
            {
                GameObject containerGo = new GameObject("BlocksContainer");
                containerGo.transform.SetParent(transform);
                blocksContainer = containerGo.transform;
            }

            // Clear any existing blocks
            for (int i = blocksContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(blocksContainer.GetChild(i).gameObject);
            }

            int totalRows = rowsPerTier * 3;
            float totalWidth = (columns - 1) * horizontalSpacing;
            float startX = -totalWidth * 0.5f;
            float totalHeight = (totalRows - 1) * verticalSpacing;
            float topY = startCenterY + (totalHeight * 0.5f);

            int totalBlocksCreated = 0;

            for (int r = 0; r < totalRows; r++)
            {
                float y = topY - (r * verticalSpacing);

                // Row ordering:
                // Top rows: Red (tier 1, 10 pts)
                // Middle rows: Green (tier 2, 20 pts)
                // Bottom rows: Blue (tier 3, 30 pts)
                BlockColorTier tier;
                Material mat;
                Color vfxColor;

                if (r < rowsPerTier)
                {
                    tier = BlockColorTier.Red;
                    mat = matRedBlock;
                    vfxColor = vfxRedColor;
                }
                else if (r < rowsPerTier * 2)
                {
                    tier = BlockColorTier.Green;
                    mat = matGreenBlock;
                    vfxColor = vfxGreenColor;
                }
                else
                {
                    tier = BlockColorTier.Blue;
                    mat = matBlueBlock;
                    vfxColor = vfxBlueColor;
                }

                for (int c = 0; c < columns; c++)
                {
                    float x = startX + (c * horizontalSpacing);
                    Vector3 blockPos = new Vector3(x, y, 0f);

                    GameObject blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    blockObj.name = $"Block_{tier}_{r}_{c}";
                    blockObj.transform.SetParent(blocksContainer);
                    blockObj.transform.position = blockPos;
                    blockObj.transform.localScale = Vector3.one * blockSize;

                    Block blockComp = blockObj.AddComponent<Block>();
                    blockComp.Initialize(tier, mat, vfxColor);

                    totalBlocksCreated++;
                }
            }

            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.RegisterLevelBlocks(totalBlocksCreated);
            }
        }
    }
}
