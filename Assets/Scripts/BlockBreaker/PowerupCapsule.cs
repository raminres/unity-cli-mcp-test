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
        [SerializeField] private Transform fallingVfxTransform;
        [SerializeField] private ParticleSystem fallingVfx;

        private MaterialPropertyBlock propBlock;
        private bool isCollected = false;

        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");

        public BlockSpecialType SpecialType => specialType;
        public float FallSpeed { get => fallSpeed; set => fallSpeed = value; }
        public Transform VisualCapsuleTransform => visualCapsuleTransform;
        public Renderer CapsuleRenderer => capsuleRenderer;
        public SpriteRenderer IconRenderer => iconRenderer;
        public Transform IconTransform => iconTransform;
        public Transform FallingVfxTransform => fallingVfxTransform;
        public ParticleSystem FallingVfx => fallingVfx;

        public Transform ModelTransform => visualCapsuleTransform;
        public Transform SpriteTransform => iconTransform;

        public static GameObject PowerupPrefab { get; set; }
        public static GameObject HazardPrefab { get; set; }

        private static Material defaultCapsuleMaterial;
        private static Material defaultParticleMaterial;

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

        public static Material GetOrCreateParticleMaterial()
        {
            if (defaultParticleMaterial != null) return defaultParticleMaterial;

#if UNITY_EDITOR
            var editorMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_VFX_Burst.mat");
            if (editorMat != null)
            {
                defaultParticleMaterial = editorMat;
                return defaultParticleMaterial;
            }
#endif

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                      ?? Shader.Find("Arcade/VFX_ParticleBurst");

            if (shader != null)
            {
                defaultParticleMaterial = new Material(shader)
                {
                    name = "M_Drop_Trail_Fallback"
                };
            }

            return defaultParticleMaterial;
        }

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            propBlock = new MaterialPropertyBlock();
            ResolveModularChildren();
        }

        public void ResolveModularChildren()
        {
            if (visualCapsuleTransform == null)
            {
                visualCapsuleTransform = transform.Find("Visual_Capsule") ?? transform.Find("model");
            }
            if (capsuleRenderer == null && visualCapsuleTransform != null)
            {
                capsuleRenderer = visualCapsuleTransform.GetComponent<Renderer>();
            }

            if (iconTransform == null)
            {
                iconTransform = transform.Find("Icon_Billboard") ?? transform.Find("sprite");
            }
            if (iconRenderer == null && iconTransform != null)
            {
                iconRenderer = iconTransform.GetComponent<SpriteRenderer>();
            }

            if (fallingVfxTransform == null)
            {
                fallingVfxTransform = transform.Find("Falling_Vfx") ?? transform.Find("falling vfx");
            }
            if (fallingVfx == null && fallingVfxTransform != null)
            {
                fallingVfx = fallingVfxTransform.GetComponent<ParticleSystem>();
            }
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
                if (state == GameState.Paused || ArcadeGameManager.Instance.IsLevelClearPending) return;

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
            glowColor = type.IsPowerdown() ? BlockModifierExtensions.UnifiedPowerdownColor : type.GetBadgeColor();

            if (visual != null) visualCapsuleTransform = visual;
            if (icon != null) iconTransform = icon;

            EnsureVisualHierarchy(sharedMat);
            ApplyGlowColor();

            Sprite sprite = GetSpriteForType(type);
            if (iconRenderer != null)
            {
                iconRenderer.sprite = sprite;
                iconRenderer.color = Color.white;
            }

            if (fallingVfx != null)
            {
                var main = fallingVfx.main;
                main.startColor = glowColor;
            }
        }

        /// <summary>
        /// Ensures exactly 1 rotating 3D capsule mesh child (Visual_Capsule), exactly 1 non-rotating upright
        /// camera-facing billboard sprite child (Icon_Billboard), and 1 falling particle VFX child (Falling_Vfx).
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
                if (child.name == "Visual_Capsule" || child.name == "model")
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
                if (child.name == "Icon_Billboard" || child.name == "sprite")
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

            // 4. Clean up duplicate Falling_Vfx children (keep only the first)
            Transform primaryVfx = null;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name == "Falling_Vfx" || child.name == "falling vfx")
                {
                    if (primaryVfx == null)
                    {
                        primaryVfx = child;
                    }
                    else
                    {
                        if (Application.isPlaying) Destroy(child.gameObject);
                        else DestroyImmediate(child.gameObject);
                    }
                }
            }

            // 5. Configure Visual_Capsule (3D rotating mesh child)
            bool isPowerdown = specialType.IsPowerdown();
            PrimitiveType desiredPrimitive = isPowerdown ? PrimitiveType.Cube : PrimitiveType.Capsule;

            // Clean up existing visual if it has wrong primitive type (e.g. Capsule when Cube is needed for diamond powerdown)
            if (primaryVisual != null)
            {
                var mf = primaryVisual.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    bool isCubeMesh = mf.sharedMesh.name.IndexOf("Cube", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    if (isPowerdown != isCubeMesh)
                    {
                        if (Application.isPlaying) Destroy(primaryVisual.gameObject);
                        else DestroyImmediate(primaryVisual.gameObject);
                        primaryVisual = null;
                    }
                }
            }

            if (primaryVisual == null)
            {
                var visualGo = GameObject.CreatePrimitive(desiredPrimitive);
                visualGo.name = "Visual_Capsule";
                visualGo.transform.SetParent(transform, false);
                visualGo.transform.localPosition = Vector3.zero;
                if (isPowerdown)
                {
                    visualGo.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
                    visualGo.transform.localScale = new Vector3(0.72f, 0.72f, 0.72f);
                }
                else
                {
                    visualGo.transform.localRotation = Quaternion.identity;
                    visualGo.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                }
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

            // 6. Configure Icon_Billboard (2D non-rotating camera-facing sprite child)
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

            // 7. Configure Falling_Vfx (Particle System Trail)
            if (primaryVfx == null)
            {
                var vfxGo = new GameObject("Falling_Vfx");
                vfxGo.transform.SetParent(transform, false);
                vfxGo.transform.localPosition = Vector3.zero;
                vfxGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

                var ps = vfxGo.AddComponent<ParticleSystem>();
                var psr = vfxGo.GetComponent<ParticleSystemRenderer>();
                Material vfxMat = GetOrCreateParticleMaterial();
                if (vfxMat != null) psr.sharedMaterial = vfxMat;

                var main = ps.main;
                main.playOnAwake = true;
                main.loop = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 2.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.22f);
                main.startColor = glowColor;
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                var emission = ps.emission;
                emission.enabled = true;
                emission.rateOverTime = 25f;

                var shape = ps.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.35f;

                var velocityOverLifetime = ps.velocityOverLifetime;
                velocityOverLifetime.enabled = true;
                velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);

                primaryVfx = vfxGo.transform;
            }

            fallingVfxTransform = primaryVfx;
            fallingVfx = primaryVfx.GetComponent<ParticleSystem>();
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

            if (ArcadeGameManager.Instance != null && (ArcadeGameManager.Instance.State != GameState.Playing || ArcadeGameManager.Instance.IsLevelClearPending))
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

            // 1. Play Audio (hazard tone for powerdown, powerup chime for buff)
            if (ArcadeAudioManager.Instance != null)
            {
                if (specialType.IsPowerdown())
                {
                    ArcadeAudioManager.Instance.PlayPowerdown();
                }
                else
                {
                    ArcadeAudioManager.Instance.PlayPowerup();
                }
            }

            // 2. Trigger appropriate buff or powerdown hazard
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
                    if (paddle != null && paddle.LaserController != null)
                    {
                        paddle.LaserController.ActivateLaserBlaster(BlockModifierExtensions.DEFAULT_LASER_DURATION);
                    }
                    break;

                case BlockSpecialType.PaddleShortener:
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ActivatePaddleShortener(BlockModifierExtensions.DEFAULT_PADDLE_SHORTEN_DURATION);
                    }
                    else if (paddle != null)
                    {
                        paddle.ShrinkWidth(BlockModifierExtensions.PADDLE_SHORTEN_PERCENT);
                    }
                    break;

                case BlockSpecialType.PaddleSlower:
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ActivatePaddleSlower(BlockModifierExtensions.DEFAULT_PADDLE_SLOW_DURATION);
                    }
                    else if (paddle != null)
                    {
                        paddle.SetSlowed(true);
                    }
                    break;

                case BlockSpecialType.BrickFreezer:
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ActivateBrickFreezer();
                    }
                    break;

                case BlockSpecialType.BallSizeDecreaser:
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ActivateBallSizeDecreaser(BlockModifierExtensions.DEFAULT_BALL_SIZE_DECREASE_DURATION);
                    }
                    break;

                case BlockSpecialType.BallSlower:
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ActivateBallSlower(BlockModifierExtensions.DEFAULT_BALL_SLOW_DURATION);
                    }
                    break;

                case BlockSpecialType.PaddleFreezer:
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ActivatePaddleFreezer(BlockModifierExtensions.DEFAULT_PADDLE_FREEZE_DURATION);
                    }
                    else if (paddle != null)
                    {
                        paddle.SetFrozen(true);
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
            if (LevelGenerator.Instance != null && LevelGenerator.Instance.IconSet != null)
            {
                var sp = LevelGenerator.Instance.IconSet.GetSprite(type);
                if (sp != null) return sp;
            }

            if (UI.ArcadeUIManager.Instance != null)
            {
                if (UI.ArcadeUIManager.Instance.IconSet != null)
                {
                    var sp = UI.ArcadeUIManager.Instance.IconSet.GetSprite(type);
                    if (sp != null) return sp;
                }

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
                case BlockSpecialType.PaddleShortener:
                    return LoadSpriteSafe("Assets/UI/Icons/TX_Powerdown_Arrows_Inward.png");
                case BlockSpecialType.PaddleSlower:
                    return LoadSpriteSafe("Assets/UI/Icons/TX_Powerdown_Slower_Paddle.png");
                case BlockSpecialType.BrickFreezer:
                    return LoadSpriteSafe("Assets/UI/Icons/TX_Powerdown_Frozen_Brick.png");
                case BlockSpecialType.BallSizeDecreaser:
                    return LoadSpriteSafe("Assets/UI/Icons/TX_Powerdown_Smaller_Ball.png");
                case BlockSpecialType.BallSlower:
                    return LoadSpriteSafe("Assets/UI/Icons/TX_Powerdown_Slower_Ball.png");
                case BlockSpecialType.PaddleFreezer:
                    return LoadSpriteSafe("Assets/UI/Icons/TX_Powerdown_Frozen_Paddle.png");
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
            bool isHazard = type.IsPowerdown();

            GameObject template = isHazard ? HazardPrefab : PowerupPrefab;
            if (template == null)
            {
#if UNITY_EDITOR
                string path = isHazard 
                    ? "Assets/Prefabs/Powerups/PF_Drop_Hazard.prefab" 
                    : "Assets/Prefabs/Powerups/PF_Drop_Powerup.prefab";
                template = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#endif
            }

            PowerupCapsule comp;
            if (template != null)
            {
                GameObject go = Object.Instantiate(template, position, Quaternion.identity);
                go.name = $"Powerup_{type}";
                comp = go.GetComponent<PowerupCapsule>();
            }
            else
            {
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

                comp = rootGo.AddComponent<PowerupCapsule>();
            }

            baseMat = baseMat ?? GetOrCreateCapsuleMaterial();
            Color color = isHazard ? BlockModifierExtensions.UnifiedPowerdownColor : type.GetBadgeColor();

            // Initialize ensures exactly 1 Visual_Capsule mesh child, 1 Icon_Billboard sprite child, and 1 Falling_Vfx child
            comp.Initialize(type, color, baseMat);
            return comp;
        }
    }
}
