using System.Collections;
using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Physical energy barrier deployed at the bottom of the arena when the Shield powerup is collected.
    /// Deflects balls upward into play with paddle-like physics, preserves combos/rallies,
    /// and exhibits animated tween-in/out transitions, idle pulsation, and rapid warning blinking before expiration.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ShieldWall : MonoBehaviour
    {
        [Header("Dimensions & Placement")]
        [SerializeField] private Vector3 targetScale = new Vector3(20.4f, 0.45f, 2.0f);
        [SerializeField] private float wallY = -7.6f;

        [Header("Tween & Animation Parameters")]
        [Tooltip("Duration in seconds for the shield to expand into place upon activation.")]
        [SerializeField] private float appearDuration = 0.35f;

        [Tooltip("Duration in seconds for the shield to collapse when deactivated.")]
        [SerializeField] private float disappearDuration = 0.20f;

        [Tooltip("Easing curve used when expanding into place. Includes spring-damper overshoot by default.")]
        [SerializeField] private AnimationCurve appearCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2.8f),
            new Keyframe(0.75f, 1.10f, 0.4f, -0.4f),
            new Keyframe(1.0f, 1.0f, -0.2f, 0f)
        );

        [Tooltip("Easing curve used when collapsing upon expiration.")]
        [SerializeField] private AnimationCurve disappearCurve = new AnimationCurve(
            new Keyframe(0f, 1.0f, 0f, -0.5f),
            new Keyframe(1.0f, 0f, -2.5f, 0f)
        );

        [Header("Pulsation & Warning Parameters")]
        [Tooltip("Frequency in Hz of the gentle idle ambient breathing pulse.")]
        [SerializeField] private float idlePulseFrequency = 3.0f;

        [Tooltip("Time remaining threshold in seconds below which rapid warning blinking begins.")]
        [SerializeField] private float warningThreshold = 2.5f;

        [Tooltip("Frequency in Hz of the rapid warning blink when the shield is about to expire.")]
        [SerializeField] private float warningPulseFrequency = 12.0f;

        [Header("References")]
        [SerializeField] private Material shieldMaterial;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private BoxCollider boxCollider;

        private Coroutine activeTransitionCoroutine;
        private Coroutine hitPulseCoroutine;
        private MaterialPropertyBlock propertyBlock;
        private float timeRemaining = 0f;
        private bool isWarningActive = false;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private static readonly Color BaseCyan = new Color(0.0f, 0.95f, 1.0f, 1.0f);
        private static readonly Color GlowCyan = new Color(0.0f, 1.9f, 2.2f, 1.0f);

        // Public getters for testing & inspector inspection
        public Vector3 TargetScale => targetScale;
        public float WallY => wallY;
        public float AppearDuration { get => appearDuration; set => appearDuration = value; }
        public float DisappearDuration { get => disappearDuration; set => disappearDuration = value; }
        public AnimationCurve AppearCurve
        {
            get
            {
                if (appearCurve == null || appearCurve.length == 0) InitializeCurves();
                return appearCurve;
            }
            set => appearCurve = value;
        }
        public AnimationCurve DisappearCurve
        {
            get
            {
                if (disappearCurve == null || disappearCurve.length == 0) InitializeCurves();
                return disappearCurve;
            }
            set => disappearCurve = value;
        }
        public float WarningThreshold { get => warningThreshold; set => warningThreshold = value; }
        public bool IsWarningActive => isWarningActive;
        public float TimeRemaining => timeRemaining;
        public BoxCollider BoxCollider => boxCollider != null ? boxCollider : (boxCollider = GetComponent<BoxCollider>());
        public MeshRenderer MeshRenderer => meshRenderer != null ? meshRenderer : (meshRenderer = GetComponent<MeshRenderer>());

        private void Awake()
        {
            EnsureComponents();
            InitializeCurves();
        }

        public void EnsureComponents()
        {
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.size = Vector3.one;
                boxCollider.center = Vector3.zero;
            }

            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                var filter = GetComponent<MeshFilter>();
                if (filter == null)
                {
                    filter = gameObject.AddComponent<MeshFilter>();
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    filter.sharedMesh = cube.GetComponent<MeshFilter>().sharedMesh;
                    if (Application.isPlaying) Destroy(cube);
                    else DestroyImmediate(cube);
                }
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

            EnsureMaterial();
        }

        public void EnsureMaterial()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                var filter = GetComponent<MeshFilter>();
                if (filter == null)
                {
                    filter = gameObject.AddComponent<MeshFilter>();
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    filter.sharedMesh = cube.GetComponent<MeshFilter>().sharedMesh;
                    if (Application.isPlaying) Destroy(cube);
                    else DestroyImmediate(cube);
                }
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (shieldMaterial == null)
            {
#if UNITY_EDITOR
                shieldMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BlockBreaker/MI_ShieldWall.mat");
#endif
                if (shieldMaterial == null)
                {
                    Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit");
                    if (urpShader != null)
                    {
                        shieldMaterial = new Material(urpShader);
                        shieldMaterial.name = "MI_ShieldWall_RuntimeFallback";
                        shieldMaterial.SetColor(BaseColorId, BaseCyan);
                        shieldMaterial.SetColor(EmissionColorId, GlowCyan);
                    }
                }
            }

            if (shieldMaterial != null && meshRenderer != null)
            {
                meshRenderer.sharedMaterial = shieldMaterial;
            }
        }

        private void InitializeCurves()
        {
            if (appearCurve == null || appearCurve.length == 0)
            {
                // Spring-damper style overshoot curve: expands from 0, overshoots to 1.10 at 75%, settles at 1.0
                appearCurve = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 2.8f),
                    new Keyframe(0.75f, 1.10f, 0.4f, -0.4f),
                    new Keyframe(1.0f, 1.0f, -0.2f, 0f)
                );
            }

            if (disappearCurve == null || disappearCurve.length == 0)
            {
                // Crisp ease-in collapse to 0
                disappearCurve = new AnimationCurve(
                    new Keyframe(0f, 1.0f, 0f, -0.5f),
                    new Keyframe(1.0f, 0f, -2.5f, 0f)
                );
            }
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy) return;

            UpdateVisualPulsation();
        }

        /// <summary>
        /// Modulates the emissive glow using MaterialPropertyBlock to prevent runtime material leaks and preserve SRP Batcher.
        /// </summary>
        private void UpdateVisualPulsation()
        {
            if (meshRenderer == null) return;

            float freq = isWarningActive ? warningPulseFrequency : idlePulseFrequency;
            float pulse = Mathf.Sin(Time.time * freq * Mathf.PI * 2f);

            Color currentGlow;
            if (isWarningActive)
            {
                // Rapid on/off blink warning
                float blinkAlpha = pulse > 0f ? 1.0f : 0.25f;
                currentGlow = GlowCyan * (blinkAlpha * 2.5f);
            }
            else
            {
                // Subtle high-tech ambient breathing pulse (1.6x to 2.2x emission)
                float pulseIntensity = 1.9f + (pulse * 0.35f);
                currentGlow = GlowCyan * pulseIntensity;
            }

            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(EmissionColorId, currentGlow);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        /// <summary>
        /// Activates the shield wall with a horizontal expanding tween animation.
        /// </summary>
        public void Activate(float duration)
        {
            EnsureComponents();
            timeRemaining = duration;
            isWarningActive = false;

            transform.position = new Vector3(0f, wallY, 0f);

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (activeTransitionCoroutine != null)
            {
                StopCoroutine(activeTransitionCoroutine);
                activeTransitionCoroutine = null;
            }

            if (Application.isPlaying)
            {
                activeTransitionCoroutine = StartCoroutine(AppearCoroutine());
            }
            else
            {
                transform.localScale = targetScale;
            }
        }

        private IEnumerator AppearCoroutine()
        {
            float elapsed = 0f;
            transform.localScale = new Vector3(0f, targetScale.y * 0.5f, targetScale.z);

            while (elapsed < appearDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / appearDuration);
                float eval = appearCurve != null ? appearCurve.Evaluate(t) : t;

                // Expand outward from center along X, with slight Y scale overshoot
                transform.localScale = new Vector3(
                    targetScale.x * Mathf.Max(0f, eval),
                    targetScale.y * Mathf.Max(0.1f, Mathf.Lerp(0.5f, 1f, eval)),
                    targetScale.z
                );

                yield return null;
            }

            transform.localScale = targetScale;
            activeTransitionCoroutine = null;
        }

        /// <summary>
        /// Deactivates the shield wall, smoothly collapsing it before disabling the GameObject.
        /// </summary>
        public void Deactivate(bool immediate = false)
        {
            if (activeTransitionCoroutine != null)
            {
                StopCoroutine(activeTransitionCoroutine);
                activeTransitionCoroutine = null;
            }

            if (immediate || !gameObject.activeInHierarchy || appearDuration <= 0f || !Application.isPlaying)
            {
                transform.localScale = Vector3.zero;
                gameObject.SetActive(false);
                return;
            }

            activeTransitionCoroutine = StartCoroutine(DisappearCoroutine());
        }

        private IEnumerator DisappearCoroutine()
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < disappearDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / disappearDuration);
                float eval = disappearCurve != null ? disappearCurve.Evaluate(t) : (1f - t);

                transform.localScale = new Vector3(
                    startScale.x * Mathf.Max(0f, eval),
                    startScale.y * Mathf.Max(0f, eval),
                    startScale.z
                );

                yield return null;
            }

            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
            activeTransitionCoroutine = null;
        }

        /// <summary>
        /// Updates time remaining, initiating the warning blink when nearing expiration.
        /// </summary>
        public void SetTimeRemaining(float remaining)
        {
            timeRemaining = remaining;
            isWarningActive = (timeRemaining > 0f && timeRemaining <= warningThreshold);
        }

        /// <summary>
        /// Triggers a brief kinetic impact scale pop and flash when a ball strikes the shield.
        /// </summary>
        public void PulseOnHit()
        {
            if (!gameObject.activeInHierarchy || !Application.isPlaying) return;

            if (hitPulseCoroutine != null)
            {
                StopCoroutine(hitPulseCoroutine);
            }
            hitPulseCoroutine = StartCoroutine(HitPulseRoutine());
        }

        private IEnumerator HitPulseRoutine()
        {
            Vector3 current = transform.localScale;
            Vector3 bumped = new Vector3(current.x, current.y * 1.35f, current.z);

            transform.localScale = bumped;
            yield return new WaitForSeconds(0.10f);

            float elapsed = 0f;
            while (elapsed < 0.10f)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(bumped, targetScale, elapsed / 0.10f);
                yield return null;
            }

            transform.localScale = targetScale;
            hitPulseCoroutine = null;
        }

        private void OnCollisionEnter(Collision collision)
        {
            var ball = collision.gameObject.GetComponent<BallController>()
                ?? collision.gameObject.GetComponentInParent<BallController>();

            if (ball != null)
            {
                Vector3 contact = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
                ball.BounceFromShield(contact);
                PulseOnHit();
            }
        }
    }
}
