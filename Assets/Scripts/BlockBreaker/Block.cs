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
    /// Represents a 1:1 3D cube block with tier scoring, special modifiers (x2 multiplier, paddle expander),
    /// audio response, and shatter VFX trigger.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Block : MonoBehaviour
    {
        [Header("Block Properties")]
        [SerializeField] private BlockColorTier colorTier = BlockColorTier.Red;
        [SerializeField] private BlockSpecialType specialType = BlockSpecialType.Normal;
        [SerializeField] private int basePoints = 10;
        [SerializeField] private int scoreMultiplier = 1;
        [SerializeField] private float paddleExpansionPercent = 0.10f;
        [SerializeField] private int hitPoints = 1;
        [SerializeField] private Color particleColor = new Color(1f, 0.2f, 0.3f);

        [Header("References")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private GameObject glassShell;

        private bool isDestroyed = false;

        public BlockColorTier Tier => colorTier;
        public BlockSpecialType SpecialType => specialType;
        public int Points => basePoints * scoreMultiplier;
        public int ScoreMultiplier => scoreMultiplier;
        public Color ParticleColor => particleColor;
        public int HitPoints => hitPoints;
        public bool IsDestroyed => isDestroyed;
        public GameObject GlassShell => glassShell;

        private void Awake()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        }

        public void SetGlassShell(GameObject shell)
        {
            glassShell = shell;
        }

        public void Initialize(BlockColorTier tier, Material material, Color vfxColor, BlockSpecialType special = BlockSpecialType.Normal)
        {
            colorTier = tier;
            particleColor = vfxColor;
            specialType = special;
            isDestroyed = false;

            basePoints = tier switch
            {
                BlockColorTier.Blue => 30,
                BlockColorTier.Green => 20,
                _ => 10
            };

            scoreMultiplier = special switch
            {
                BlockSpecialType.ScoreMultiplier3x => 3,
                BlockSpecialType.ScoreMultiplier2x => 2,
                BlockSpecialType.GlassEnclosed => 2,
                _ => 1
            };

            hitPoints = special switch
            {
                BlockSpecialType.GlassEnclosed => 2,
                _ => 1
            };

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

            Vector3 hitNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector3.down;
            TakeHit(hitNormal);
        }

        public void TakeHit(Vector3 hitNormal)
        {
            if (isDestroyed) return;

            hitPoints--;
            if (hitPoints > 0)
            {
                BreakGlassShell(hitNormal);
            }
            else
            {
                DestroyBlock(hitNormal);
            }
        }

        public void BreakGlassShell(Vector3 hitNormal)
        {
            if (glassShell != null)
            {
                if (Application.isPlaying)
                    Destroy(glassShell);
                else
                    DestroyImmediate(glassShell);

                glassShell = null;
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayGlassBreak();
            }

            if (BlockVFXManager.Instance != null)
            {
                BlockVFXManager.Instance.PlayBlockShatter(transform.position, new Color(0.85f, 0.95f, 1.0f, 0.5f), hitNormal);
            }
        }

        public void DestroyBlock(Vector3 hitNormal)
        {
            if (isDestroyed) return;
            isDestroyed = true;

            // 1. Play SFX
            if (ArcadeAudioManager.Instance != null)
            {
                if (specialType == BlockSpecialType.Bomb)
                {
                    ArcadeAudioManager.Instance.PlayBombExplosion();
                }
                else
                {
                    ArcadeAudioManager.Instance.PlayBreak();
                    if (specialType != BlockSpecialType.Normal && specialType != BlockSpecialType.GlassEnclosed)
                    {
                        ArcadeAudioManager.Instance.PlayPowerup();
                    }
                }
            }

            // 2. Trigger VFX Graph Shatter
            if (BlockVFXManager.Instance != null)
            {
                BlockVFXManager.Instance.PlayBlockShatter(transform.position, particleColor, hitNormal);
            }

            // 3. Apply Special Modifier Effects
            if (specialType == BlockSpecialType.PaddleExpander)
            {
                var paddle = FindAnyObjectByType<PaddleController>();
                if (paddle != null)
                {
                    paddle.ExpandWidth(paddleExpansionPercent);
                }
            }
            else if (specialType == BlockSpecialType.ExtraHeart)
            {
                if (ArcadeGameManager.Instance != null)
                {
                    ArcadeGameManager.Instance.AddLife(1);
                }

                if (UI.ArcadeUIManager.Instance != null)
                {
                    UI.ArcadeUIManager.Instance.AnimateFlyingHeart(transform.position);
                }
            }
            else if (specialType == BlockSpecialType.Bomb)
            {
                ExplodePerimeter();
            }

            // 4. Notify Game Manager with multiplied points
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.RecordBlockDestroyed(Points, (int)colorTier);
            }

            // 5. Destroy block
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        public void ExplodePerimeter(float explosionRadius = 2.5f)
        {
            if (transform.parent == null) return;

            var allBlocks = transform.parent.GetComponentsInChildren<Block>();
            for (int i = 0; i < allBlocks.Length; i++)
            {
                var neighbor = allBlocks[i];
                if (neighbor != null && neighbor != this && !neighbor.IsDestroyed)
                {
                    float dist = Vector3.Distance(transform.position, neighbor.transform.position);
                    if (dist <= explosionRadius)
                    {
                        Vector3 outward = (neighbor.transform.position - transform.position).normalized;
                        if (outward == Vector3.zero) outward = Vector3.up;
                        neighbor.DestroyBlock(outward);
                    }
                }
            }
        }
    }
}
