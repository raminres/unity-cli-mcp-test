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

        private static Material defaultCapsuleMaterial;

        public static void SetDefaultMaterial(Material mat)
        {
            defaultCapsuleMaterial = mat;
        }

        public static Material GetOrCreateCapsuleMaterial()
        {
            if (defaultCapsuleMaterial != null) return defaultCapsuleMaterial;

#if UNITY_EDITOR
            var editorMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_Powerup_Capsule.mat");
            if (editorMat != null)
            {
                defaultCapsuleMaterial = editorMat;
                return defaultCapsuleMaterial;
            }
#endif

            // Standalone / iOS fallback shader resolution (eliminates pink missing shaders on Metal)
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Arcade/VFX_BlockDebris")
                      ?? Shader.Find("Universal Render Pipeline/Unlit");

            if (shader != null)
            {
                defaultCapsuleMaterial = new Material(shader)
                {
                    name = "M_Powerup_Capsule_RuntimeFallback"
                };
                defaultCapsuleMaterial.SetFloat("_Metallic", 0.8f);
                defaultCapsuleMaterial.SetFloat("_Smoothness", 0.9f);
                defaultCapsuleMaterial.EnableKeyword("_EMISSION");
            }

            return defaultCapsuleMaterial;
        }

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            propBlock = new MaterialPropertyBlock();
            // Note: Visual hierarchy is managed cleanly in EnsureVisualHierarchy/Initialize
        }

        private void Start()
        {
            EnsureVisualHierarchy();
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
            if (icon != null) iconTransform = icon;

            EnsureVisualHierarchy(sharedMat);
            ApplyGlowColor();

            Sprite sprite = GetSpriteForType(type);
            if (iconRenderer != null && sprite != null)
            {
                iconRenderer.sprite = sprite;
                iconRenderer.color = Color.white;
            }
        }

        /// <summary>
        /// Ensures exactly 1 rotating 3D capsule mesh child (Visual_Capsule) and exactly 1 non-rotating upright
        /// camera-facing billboard sprite child (Icon_Billboard), stripping any duplicate components or pink shaders.
        /// </summary>
        public void EnsureVisualHierarchy(Material baseMat = null)
        {
            // 1. Ensure Root container does NOT have any stray MeshRenderer, MeshFilter, or SpriteRenderer
            var rootMr = GetComponent<MeshRenderer>();
            if (rootMr != null) { if (Application.isPlaying) Destroy(rootMr); else DestroyImmediate(rootMr); }
            var rootMf = GetComponent<MeshFilter>();
            if (rootMf != null) { if (Application.isPlaying) Destroy(rootMf); else DestroyImmediate(rootMf); }
            var rootSr = GetComponent<SpriteRenderer>();
            if (rootSr != null) { if (Application.isPlaying) Destroy(rootSr); else DestroyImmediate(rootSr); }

            // 2. Clean up duplicate Visual_Capsule children (keep only the first)
            Transform primaryVisual = null;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name == "Visual_Capsule")
                {
                    if (primaryVisual == null)
                    {
                        primaryVisual = child;
                    }
                    else
                    {
                        if (Application.isPlaying) Destroy(child.gameObject);
                        else DestroyImmediate(child.gameObject);
                    }
                }
            }

            // 3. Clean up duplicate Icon_Billboard children (keep only the first)
            Transform primaryIcon = null;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name == "Icon_Billboard")
                {
                    if (primaryIcon == null)
                    {
                        primaryIcon = child;
                    }
                    else
                    {
                        if (Application.isPlaying) Destroy(child.gameObject);
                        else DestroyImmediate(child.gameObject);
                    }
                }
            }

            // 4. Configure Visual_Capsule (3D rotating mesh child)
            if (primaryVisual == null)
            {
                var visualGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visualGo.name = "Visual_Capsule";
                visualGo.transform.SetParent(transform, false);
                visualGo.transform.localPosition = Vector3.zero;
                visualGo.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                primaryVisual = visualGo.transform;
            }

            visualCapsuleTransform = primaryVisual;

            // Strip any collider and any stray SpriteRenderer on the visual mesh child
            var col = visualCapsuleTransform.GetComponent<Collider>();
            if (col != null) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
            var straySr = visualCapsuleTransform.GetComponent<SpriteRenderer>();
            if (straySr != null) { if (Application.isPlaying) Destroy(straySr); else DestroyImmediate(straySr); }

            capsuleRenderer = visualCapsuleTransform.GetComponent<MeshRenderer>();
            if (capsuleRenderer == null)
            {
                capsuleRenderer = visualCapsuleTransform.gameObject.AddComponent<MeshRenderer>();
            }

            Material resolvedMat = baseMat ?? GetOrCreateCapsuleMaterial();
            if (resolvedMat != null && capsuleRenderer.sharedMaterial != resolvedMat)
            {
                capsuleRenderer.sharedMaterial = resolvedMat;
            }

            // 5. Configure Icon_Billboard (2D non-rotating camera-facing sprite child)
            if (primaryIcon == null)
            {
                var iconGo = new GameObject("Icon_Billboard");
                iconGo.transform.SetParent(transform, false);
                iconGo.transform.localPosition = new Vector3(0f, 0f, -0.60f);
                iconGo.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);
                primaryIcon = iconGo.transform;
            }

            iconTransform = primaryIcon;

            // Strip any MeshFilter, MeshRenderer, or Collider on the icon child
            var strayMf = iconTransform.GetComponent<MeshFilter>();
            if (strayMf != null) { if (Application.isPlaying) Destroy(strayMf); else DestroyImmediate(strayMf); }
            var strayMr = iconTransform.GetComponent<MeshRenderer>();
            if (strayMr != null) { if (Application.isPlaying) Destroy(strayMr); else DestroyImmediate(strayMr); }
            var strayCol = iconTransform.GetComponent<Collider>();
            if (strayCol != null) { if (Application.isPlaying) Destroy(strayCol); else DestroyImmediate(strayCol); }

            iconRenderer = iconTransform.GetComponent<SpriteRenderer>();
            if (iconRenderer == null)
            {
                iconRenderer = iconTransform.gameObject.AddComponent<SpriteRenderer>();
            }
            iconRenderer.sortingOrder = 35;
        }

        public void EnsureBillboardIcon()
        {
            EnsureVisualHierarchy(null);
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
                TryIntercept(paddle);
            }
        }

        /// <summary>
        /// Attempts to intercept the falling capsule with the paddle.
        /// Returns false and does not collect if the game is not actively playing (e.g. while docked in ReadyToLaunch or BallLost).
        /// </summary>
        public bool TryIntercept(PaddleController paddle)
        {
            if (isCollected) return false;

            if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State != GameState.Playing)
            {
                return false;
            }

            Collect(paddle);
            return true;
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

                case BlockSpecialType.Laser:
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ActivateLaserPowerup(BlockModifierExtensions.DEFAULT_LASER_DURATION);
                    }
                    else if (paddle != null && paddle.LaserController != null)
                    {
                        paddle.LaserController.ActivateLaserBlaster(BlockModifierExtensions.DEFAULT_LASER_DURATION);
                    }
                    break;
            }

            // 3. VFX Burst using dedicated URP shaded particles
            if (BlockVFXManager.Instance != null)
            {
                BlockVFXManager.Instance.PlayPowerupCollect(transform.position, glowColor);
            }

            DestroySelf();
        }

        /// <summary>
        /// Clears and destroys all active falling powerup capsules in the scene.
        /// Called when a ball is lost, when a level is cleared, or on game over to prevent stale pickups while docked.
        /// </summary>
        public static void ClearAllFallingCapsules()
        {
            var capsules = FindObjectsByType<PowerupCapsule>();
            for (int i = 0; i < capsules.Length; i++)
            {
                if (capsules[i] != null)
                {
                    capsules[i].DestroySelf();
                }
            }
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

#if UNITY_EDITOR
        private static Sprite LoadSpriteSafe(string path)
        {
            var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;

            var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            if (all != null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] is Sprite s) return s;
                }
            }
            return null;
        }
#endif

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
                    case BlockSpecialType.Laser:
                        if (UI.ArcadeUIManager.Instance.LaserSprite != null)
                            return UI.ArcadeUIManager.Instance.LaserSprite;
                        break;
                }
            }

#if UNITY_EDITOR
            switch (type)
            {
                case BlockSpecialType.PaddleExpander:
                    return LoadSpriteSafe("Assets/UI/Icons/TX_Powerup_Arrows_Outward.png");
                case BlockSpecialType.ExtraHeart:
                    var heartPlus = LoadSpriteSafe("Assets/UI/Icons/TX_Powerup_Heart_Plus.png");
                    return heartPlus != null ? heartPlus : LoadSpriteSafe("Assets/UI/TX_Heart_Fill.png");
                case BlockSpecialType.Shield:
                    return LoadSpriteSafe("Assets/UI/Icons/TX_Powerup_Shield.png");
                case BlockSpecialType.MultiBall:
                    return LoadSpriteSafe("Assets/UI/Icons/TX_Powerup_Multi_Ball.png");
                case BlockSpecialType.ScoreMultiplier2x:
                case BlockSpecialType.ScoreMultiplier3x:
                case BlockSpecialType.ScoreMultiplier4x:
                case BlockSpecialType.ScoreMultiplier5x:
                    return LoadSpriteSafe("Assets/UI/Icons/TX_Powerup_Extra_Points.png");
                case BlockSpecialType.Laser:
                    return LoadSpriteSafe("Assets/UI/Icons/TX_Powerup_Gun.png");
            }
#endif
            return null;
        }

        /// <summary>
        /// Factory method to spawn a 3D collectible powerup capsule.
        /// Decoupled hierarchy: non-rotating container with exactly 1 tumbling visual capsule child and 1 independent foreground billboard icon.
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

            baseMat = baseMat ?? GetOrCreateCapsuleMaterial();
            Color color = type.GetBadgeColor();

            // Initialize ensures exactly 1 Visual_Capsule mesh child and 1 Icon_Billboard sprite child
            comp.Initialize(type, color, baseMat);
            return comp;
        }
    }
}
