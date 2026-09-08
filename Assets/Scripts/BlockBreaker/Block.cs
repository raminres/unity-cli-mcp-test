using Arcade.Audio;
using Arcade.Core;
using UnityEngine;

namespace Arcade.BlockBreaker
{
    public enum BlockColorTier
    {
        Red = 1,
        Green = 2,
        Blue = 3
    }

    /// <summary>
    /// Represents a 1:1 3D cube block with tier scoring, audio response, and shatter VFX trigger.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Block : MonoBehaviour
    {
        [Header("Block Properties")]
        [SerializeField] private BlockColorTier colorTier = BlockColorTier.Red;
        [SerializeField] private int basePoints = 10;
        [SerializeField] private int hitPoints = 1;
        [SerializeField] private Color particleColor = new Color(1f, 0.2f, 0.3f);

        [Header("References")]
        [SerializeField] private MeshRenderer meshRenderer;

        public BlockColorTier Tier => colorTier;
        public int Points => basePoints;
        public Color ParticleColor => particleColor;

        private void Awake()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        }

        public void Initialize(BlockColorTier tier, Material material, Color vfxColor)
        {
            colorTier = tier;
            particleColor = vfxColor;

            basePoints = tier switch
            {
                BlockColorTier.Blue => 30,
                BlockColorTier.Green => 20,
                _ => 10
            };

            hitPoints = 1;

            if (meshRenderer != null && material != null)
            {
                meshRenderer.sharedMaterial = material;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Only balls destroy blocks
            BallController ball = collision.gameObject.GetComponent<BallController>();
            if (ball == null) return;

            hitPoints--;
            if (hitPoints <= 0)
            {
                DestroyBlock(collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector3.down);
            }
        }

        private void DestroyBlock(Vector3 hitNormal)
        {
            // 1. Play SFX
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayBlockHit((int)colorTier);
            }

            // 2. Trigger VFX Graph Shatter
            if (BlockVFXManager.Instance != null)
            {
                BlockVFXManager.Instance.PlayBlockShatter(transform.position, particleColor, hitNormal);
            }

            // 3. Notify Game Manager
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.RecordBlockDestroyed(basePoints, (int)colorTier);
            }

            // 4. Destroy block
            Destroy(gameObject);
        }
    }
}
