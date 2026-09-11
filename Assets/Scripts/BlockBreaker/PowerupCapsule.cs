using Arcade.Audio;
using Arcade.Core;
using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Collectible falling powerup capsule dropped by destroyed special blocks.
    /// Features decoupled hierarchy (tumbling visual capsule mesh child + independent upright billboard icon),
    /// enlarged dimensions (4x larger icon, 2.1x larger capsule) for instant visibility,
    /// and foreground depth (-1.0f) preventing occlusion behind lower bricks.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PowerupCapsule : MonoBehaviour
    {
        public const float FOREGROUND_Z = -1.0f;

        [Header("Movement Settings")]
        [SerializeField] private float fallSpeed = 4.5f;
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float destroyY = -9.5f;

        [Header("Powerup Properties")]
        [SerializeField] private BlockSpecialType specialType = BlockSpecialType.PaddleExpander;
        [SerializeField] private Color glowColor = Color.cyan;

        [Header("Visual Components")]
        [SerializeField] private Transform visualCapsuleTransform;
        [SerializeField] private Renderer capsuleRenderer;
        [SerializeField] private Transform iconTransform;
        [SerializeField] private SpriteRenderer iconRenderer;

        private MaterialPropertyBlock propBlock;
        private bool isCollected = false;

        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");

        public BlockSpecialType SpecialType => specialType;
        public float FallSpeed { get => fallSpeed; set => fallSpeed = value; }
        public Transform VisualCapsuleTransform => visualCapsuleTransform;
        public SpriteRenderer IconRenderer => iconRenderer;
        public Transform IconTransform => iconTransform;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            propBlock = new MaterialPropertyBlock();
            EnsureBillboardIcon();
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

            // Fall downward while strictly maintaining foreground Z position
            Vector3 pos = transform.position;
            pos.y -= fallSpeed * Time.deltaTime;
            pos.z = FOREGROUND_Z;
            transform.position = pos;

            // 3D tumbling rotation ONLY on the visual capsule mesh child
            if (visualCapsuleTransform != null)
            {
                visualCapsuleTransform.Rotate(new Vector3(30f, rotationSpeed, 45f) * Time.deltaTime, Space.Self);
            }

            // Destroy if fallen into bottom abyss
            if (transform.position.y < destroyY)
            {
                DestroySelf();
            }
        }

        public void UpdateBillboardOrientation()
        {
            // Lock billboard icon upright facing camera in world space, placed strictly in front of capsule
            if (iconTransform != null)
            {
                iconTransform.position = transform.position + new Vector3(0f, 0f, -0.60f);
                iconTransform.rotation = Quaternion.identity;
            }
        }

        private void LateUpdate()
        {
            UpdateBillboardOrientation();
        }

        public void Initialize(BlockSpecialType type, Color color, Material sharedMat = null, Transform visual = null, Transform icon = null)
        {
            specialType = type;
            glowColor = color;

            if (visual != null) visualCapsuleTransform = visual;
            if (icon != null)
            {
                iconTransform = icon;
                iconRenderer = icon.GetComponent<SpriteRenderer>();
            }

            EnsureBillboardIcon();

            if (capsuleRenderer == null && visualCapsuleTransform != null)
            {
                capsuleRenderer = visualCapsuleTransform.GetComponent<Renderer>();
            }
            if (sharedMat != null && capsuleRenderer != null)
            {
                capsuleRenderer.sharedMaterial = sharedMat;
            }

            ApplyGlowColor();

            Sprite sprite = GetSpriteForType(type);
            if (iconRenderer != null && sprite != null)
            {
                iconRenderer.sprite = sprite;
                iconRenderer.color = Color.white;
            }
        }

        public void EnsureBillboardIcon()
        {
            if (visualCapsuleTransform == null)
            {
                var visualChild = transform.Find("Visual_Capsule");
                if (visualChild != null)
                {
                    visualCapsuleTransform = visualChild;
                    capsuleRenderer = visualChild.GetComponent<Renderer>();
                }
                else
                {
                    // Fallback to self renderer if legacy primitive or create child
                    capsuleRenderer = GetComponent<Renderer>();
                    if (capsuleRenderer == null)
                    {
                        var visualGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        visualGo.name = "Visual_Capsule";
                        visualGo.transform.SetParent(transform, false);
                        visualGo.transform.localPosition = Vector3.zero;
                        visualGo.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                        var meshCol = visualGo.GetComponent<Collider>();
                        if (meshCol != null)
                        {
                            if (Application.isPlaying) Destroy(meshCol);
                            else DestroyImmediate(meshCol);
                        }

                        visualCapsuleTransform = visualGo.transform;
                        capsuleRenderer = visualGo.GetComponent<Renderer>();
                    }
                    else
                    {
                        visualCapsuleTransform = transform;
                    }
                }
            }

            if (iconTransform == null)
            {
                var child = transform.Find("Icon_Billboard");
                if (child != null)
                {
                    iconTransform = child;
                    iconRenderer = child.GetComponent<SpriteRenderer>();
                }
                else
                {
                    var iconGo = new GameObject("Icon_Billboard");
                    iconTransform = iconGo.transform;
                    iconTransform.SetParent(transform, false);
                    iconTransform.localPosition = new Vector3(0f, 0f, -0.60f);
                    iconTransform.localScale = new Vector3(0.95f, 0.95f, 0.95f);
                    iconRenderer = iconGo.AddComponent<SpriteRenderer>();
                    iconRenderer.sortingOrder = 35;
                }
            }
        }

        private void ApplyGlowColor()
        {
            if (capsuleRenderer == null && visualCapsuleTransform != null)
            {
                capsuleRenderer = visualCapsuleTransform.GetComponent<Renderer>();
            }
            if (capsuleRenderer == null)
            {
                capsuleRenderer = GetComponent<Renderer>();
            }

            if (capsuleRenderer != null)
            {
                if (propBlock == null) propBlock = new MaterialPropertyBlock();
                capsuleRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor(BaseColorProp, Color.Lerp(Color.white, glowColor, 0.4f));
                propBlock.SetColor(EmissionColorProp, glowColor * 2.2f);
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

                case BlockSpecialType.ScoreMultiplier2x:
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ActivateScoreMultiplier(BlockModifierExtensions.SCORE_MULTIPLIER_2X, BlockModifierExtensions.DEFAULT_MULTIPLIER_DURATION);
                    }
                    break;

                case BlockSpecialType.ScoreMultiplier3x:
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ActivateScoreMultiplier(BlockModifierExtensions.SCORE_MULTIPLIER_3X, BlockModifierExtensions.DEFAULT_MULTIPLIER_DURATION);
                    }
                    break;

                case BlockSpecialType.ScoreMultiplier4x:
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ActivateScoreMultiplier(BlockModifierExtensions.SCORE_MULTIPLIER_4X, BlockModifierExtensions.DEFAULT_MULTIPLIER_DURATION);
                    }
                    break;

                case BlockSpecialType.ScoreMultiplier5x:
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ActivateScoreMultiplier(BlockModifierExtensions.SCORE_MULTIPLIER_5X, BlockModifierExtensions.DEFAULT_MULTIPLIER_DURATION);
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
        /// Resolves the billboard sprite icon matching the powerup archetype.
        /// </summary>
        public static Sprite GetSpriteForType(BlockSpecialType type)
        {
            if (UI.ArcadeUIManager.Instance != null)
            {
                switch (type)
                {
                    case BlockSpecialType.PaddleExpander:
                        if (UI.ArcadeUIManager.Instance.PaddleExpandSprite != null)
                            return UI.ArcadeUIManager.Instance.PaddleExpandSprite;
                        break;
                    case BlockSpecialType.ExtraHeart:
                        if (UI.ArcadeUIManager.Instance.HeartSprite != null)
                            return UI.ArcadeUIManager.Instance.HeartSprite;
                        break;
                    case BlockSpecialType.Shield:
                        if (UI.ArcadeUIManager.Instance.ShieldSprite != null)
                            return UI.ArcadeUIManager.Instance.ShieldSprite;
                        break;
                    case BlockSpecialType.MultiBall:
                        if (UI.ArcadeUIManager.Instance.MultiBallSprite != null)
                            return UI.ArcadeUIManager.Instance.MultiBallSprite;
                        break;
                    case BlockSpecialType.ScoreMultiplier2x:
                    case BlockSpecialType.ScoreMultiplier3x:
                    case BlockSpecialType.ScoreMultiplier4x:
                    case BlockSpecialType.ScoreMultiplier5x:
                        if (UI.ArcadeUIManager.Instance.MultiplierSprite != null)
                            return UI.ArcadeUIManager.Instance.MultiplierSprite;
                        break;
                }
            }

#if UNITY_EDITOR
            switch (type)
            {
                case BlockSpecialType.PaddleExpander:
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Arrows_Outward.png");
                case BlockSpecialType.ExtraHeart:
                    var heartPlus = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Heart_Plus.png");
                    return heartPlus != null ? heartPlus : UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/TX_Heart_Fill.png");
                case BlockSpecialType.Shield:
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Shield.png");
                case BlockSpecialType.MultiBall:
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Multi_Ball.png");
                case BlockSpecialType.ScoreMultiplier2x:
                case BlockSpecialType.ScoreMultiplier3x:
                case BlockSpecialType.ScoreMultiplier4x:
                case BlockSpecialType.ScoreMultiplier5x:
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Extra_Points.png");
            }
#endif
            return null;
        }

        /// <summary>
        /// Factory method to spawn a 3D collectible powerup capsule.
        /// Decoupled hierarchy: non-rotating container with tumbling visual capsule child and independent foreground billboard icon.
        /// </summary>
        public static PowerupCapsule Spawn(Vector3 position, BlockSpecialType type, Material baseMat = null)
        {
            position.z = FOREGROUND_Z;

            // 1. Root container (does NOT rotate, clean physics)
            var rootGo = new GameObject($"Powerup_{type}");
            rootGo.transform.position = position;
            rootGo.transform.localScale = Vector3.one;
            rootGo.transform.rotation = Quaternion.identity;

            var rb = rootGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var box = rootGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.6f, 1.6f, 4.0f);
            box.center = new Vector3(0f, 0f, 1.0f);

            var comp = rootGo.AddComponent<PowerupCapsule>();

            // 2. Child 1: Visual Capsule (Chunky 3D Mesh, only this child rotates)
            var visualGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visualGo.name = "Visual_Capsule";
            visualGo.transform.SetParent(rootGo.transform, false);
            visualGo.transform.localPosition = Vector3.zero;
            visualGo.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            var meshCol = visualGo.GetComponent<Collider>();
            if (meshCol != null)
            {
                if (Application.isPlaying) Destroy(meshCol);
                else DestroyImmediate(meshCol);
            }

            // 3. Child 2: Billboard Icon (Large, upright, always in front of capsule)
            var iconGo = new GameObject("Icon_Billboard");
            iconGo.transform.SetParent(rootGo.transform, false);
            iconGo.transform.localPosition = new Vector3(0f, 0f, -0.60f);
            iconGo.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);

            var sr = iconGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 35;

            Color color = type.GetBadgeColor();

            if (baseMat == null)
            {
#if UNITY_EDITOR
                baseMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Powerup_Capsule.mat");
#endif
            }

            comp.Initialize(type, color, baseMat, visualGo.transform, iconGo.transform);
            return comp;
        }
    }
}
