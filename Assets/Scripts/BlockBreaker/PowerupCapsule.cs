using Arcade.Audio;
using Arcade.Core;
using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Collectible falling powerup capsule dropped by destroyed special blocks.
    /// Tumbler physics, glowing neon shader tint, and paddle intercept collection.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PowerupCapsule : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float fallSpeed = 4.5f;
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float destroyY = -9.5f;

        [Header("Powerup Properties")]
        [SerializeField] private BlockSpecialType specialType = BlockSpecialType.PaddleExpander;
        [SerializeField] private Color glowColor = Color.cyan;

        private Renderer capsuleRenderer;
        private MaterialPropertyBlock propBlock;
        private bool isCollected = false;

        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");

        public BlockSpecialType SpecialType => specialType;
        public float FallSpeed { get => fallSpeed; set => fallSpeed = value; }

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            capsuleRenderer = GetComponent<Renderer>();
            propBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            ApplyGlowColor();
        }

        private void Update()
        {
            if (isCollected) return;

            // Pause check
            if (ArcadeGameManager.Instance != null)
            {
                var state = ArcadeGameManager.Instance.State;
                if (state == GameState.Paused) return;

                if (state == GameState.GameOver || state == GameState.LevelClear)
                {
                    DestroySelf();
                    return;
                }
            }

            // Fall downward
            transform.position += Vector3.down * (fallSpeed * Time.deltaTime);

            // 3D tumbling rotation
            transform.Rotate(new Vector3(30f, rotationSpeed, 45f) * Time.deltaTime, Space.Self);

            // Destroy if fallen into bottom abyss
            if (transform.position.y < destroyY)
            {
                DestroySelf();
            }
        }

        public void Initialize(BlockSpecialType type, Color color, Material sharedMat = null)
        {
            specialType = type;
            glowColor = color;

            if (capsuleRenderer == null) capsuleRenderer = GetComponent<Renderer>();
            if (sharedMat != null && capsuleRenderer != null)
            {
                capsuleRenderer.sharedMaterial = sharedMat;
            }

            ApplyGlowColor();
        }

        private void ApplyGlowColor()
        {
            if (capsuleRenderer == null) capsuleRenderer = GetComponent<Renderer>();
            if (capsuleRenderer != null)
            {
                if (propBlock == null) propBlock = new MaterialPropertyBlock();
                capsuleRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor(BaseColorProp, Color.Lerp(Color.white, glowColor, 0.4f));
                propBlock.SetColor(EmissionColorProp, glowColor * 1.8f);
                capsuleRenderer.SetPropertyBlock(propBlock);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            // Check if intercepted by paddle
            var paddle = other.GetComponent<PaddleController>() ?? other.GetComponentInParent<PaddleController>();
            if (paddle != null)
            {
                Collect(paddle);
            }
        }

        /// <summary>
        /// Triggered when the player's paddle catches the falling capsule.
        /// </summary>
        public void Collect(PaddleController paddle)
        {
            if (isCollected) return;
            isCollected = true;

            // 1. Play Powerup Audio
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayPowerup();
            }

            // 2. Trigger appropriate buff
            switch (specialType)
            {
                case BlockSpecialType.PaddleExpander:
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ActivatePaddleExpander(BlockModifierExtensions.DEFAULT_PADDLE_EXPAND_DURATION);
                    }
                    else if (paddle != null)
                    {
                        paddle.ExpandWidth(BlockModifierExtensions.PADDLE_EXPANSION_PERCENT);
                    }
                    break;

                case BlockSpecialType.ExtraHeart:
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.AddLife(1);
                    }
                    if (UI.ArcadeUIManager.Instance != null)
                    {
                        UI.ArcadeUIManager.Instance.AnimateFlyingHeart(transform.position);
                    }
                    break;

                case BlockSpecialType.Shield:
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ActivateShield(BlockModifierExtensions.DEFAULT_SHIELD_DURATION);
                    }
                    break;

                case BlockSpecialType.MultiBall:
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ActivateMultiBall();
                    }
                    break;
            }

            // 3. VFX Burst
            if (BlockVFXManager.Instance != null)
            {
                BlockVFXManager.Instance.PlayBlockShatter(transform.position, glowColor, Vector3.up);
            }

            DestroySelf();
        }

        private void DestroySelf()
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Factory method to spawn a 3D collectible powerup capsule.
        /// </summary>
        public static PowerupCapsule Spawn(Vector3 position, BlockSpecialType type, Material baseMat = null)
        {
            var capsuleGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsuleGo.name = $"Powerup_{type}";
            capsuleGo.transform.position = position;
            capsuleGo.transform.localScale = new Vector3(0.40f, 0.40f, 0.40f);

            var rb = capsuleGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var col = capsuleGo.GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = 0.6f; // generous touch margin for paddle catch
            }

            var comp = capsuleGo.AddComponent<PowerupCapsule>();
            Color color = type.GetBadgeColor();

            if (baseMat == null)
            {
#if UNITY_EDITOR
                baseMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Powerup_Capsule.mat");
#endif
            }

            comp.Initialize(type, color, baseMat);
            return comp;
        }
    }
}
