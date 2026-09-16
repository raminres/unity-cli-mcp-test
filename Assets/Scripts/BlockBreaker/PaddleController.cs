using System.Collections;
using Arcade.Core;
using Arcade.Input;
using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Controls the player platform (paddle) featuring an inverted stepped pyramid/trapezoid geometry,
    /// dynamic compounding width expansion with spring overshoot animation, and boundary clamping.
    /// </summary>
    public class PaddleController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 16f;
        [SerializeField] private float minX = -7.5f;
        [SerializeField] private float maxX = 7.5f;
        [SerializeField] private float paddleWidth = 5.0f;
        [SerializeField] private float basePaddleWidth = 5.0f;
        [SerializeField] private float arenaHalfWidth = 10.0f;
        [SerializeField] private int expansionCount = 0;

        [Header("Stepped Tiers")]
        [SerializeField] private Transform stepTop;
        [SerializeField] private Transform stepMid;
        [SerializeField] private Transform stepBottom;

        [Header("References")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private BoxCollider rootCollider;
        [SerializeField] private PaddleLaserController laserController;

        private Coroutine expandCoroutine;
        private Coroutine recoilCoroutine;
        private float currentVelocityX;
        private float previousPositionX;
        private bool isSlowed = false;
        private bool isFrozen = false;
        private float frozenAnchorX = 0f;

        public float Width => paddleWidth;
        public float CurrentWidth => paddleWidth;
        public float BaseWidth => basePaddleWidth;
        public float MinX => minX;
        public float MaxX => maxX;
        public int ExpansionCount => expansionCount;
        public float VelocityX => currentVelocityX;
        public bool IsSlowed => isSlowed;
        public bool IsFrozen => isFrozen;
        public PaddleLaserController LaserController => laserController != null ? laserController : (laserController = GetComponent<PaddleLaserController>() ?? gameObject.AddComponent<PaddleLaserController>());

        public Transform StepTop => stepTop;
        public Transform StepMid => stepMid;
        public Transform StepBottom => stepBottom;

        public void SetVelocityXForTesting(float velX) => currentVelocityX = velX;

        public void SetSlowed(bool slowed) => isSlowed = slowed;

        public void SetFrozen(bool frozen)
        {
            isFrozen = frozen;
            if (frozen)
            {
                frozenAnchorX = transform.position.x;
            }
            else
            {
                Vector3 p = transform.position;
                p.x = frozenAnchorX;
                transform.position = p;
            }
        }

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }

            if (rootCollider == null) rootCollider = GetComponent<BoxCollider>();
            if (laserController == null) laserController = GetComponent<PaddleLaserController>() ?? gameObject.AddComponent<PaddleLaserController>();

            EnsureSteppedMeshHierarchy();
            RecalculateBounds();
            previousPositionX = transform.position.x;
        }

        /// <summary>
        /// Ensures the 3-tier inverted stepped pyramid children exist and that the strike collider is configured.
        /// Tier 1: Top Strike Deck (100% width, H = 0.24, Y = +0.38).
        /// Tier 2: Mid Chassis (72% width, H = 0.20, Y = +0.16).
        /// Tier 3: Bottom Keel (44% width, H = 0.16, Y = -0.02).
        /// </summary>
        public void EnsureSteppedMeshHierarchy()
        {
            if (stepTop == null) stepTop = transform.Find("Step_Top");
            if (stepMid == null) stepMid = transform.Find("Step_Mid");
            if (stepBottom == null) stepBottom = transform.Find("Step_Bottom");

            // Remove legacy root mesh renderer if single-cube legacy model is attached
            var rootRenderer = GetComponent<MeshRenderer>();
            var rootFilter = GetComponent<MeshFilter>();
            if (stepTop != null && rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }

            // Configure root strike BoxCollider to cover strictly the thin top deck with extended depth
            if (rootCollider == null) rootCollider = GetComponent<BoxCollider>();
            if (rootCollider == null) rootCollider = gameObject.AddComponent<BoxCollider>();
            if (rootCollider != null)
            {
                rootCollider.center = new Vector3(0f, 0.38f, 0f);
                rootCollider.size = new Vector3(1.0f, 0.24f, 2.8f);
            }
        }

        private void Update()
        {
            float prevX = transform.position.x;

            if (ArcadeGameManager.Instance != null && (ArcadeGameManager.Instance.State == GameState.Paused ||
                                                      ArcadeGameManager.Instance.State == GameState.LevelClear ||
                                                      ArcadeGameManager.Instance.State == GameState.GameOver ||
                                                      ArcadeGameManager.Instance.IsLevelClearPending))
            {
                currentVelocityX = 0f;
                previousPositionX = prevX;
                return;
            }

            if (isFrozen)
            {
                // Visual struggle nudge if player attempts to move while frozen in ice
                bool isTryingToMove = false;
                if (ArcadeInputHandler.Instance != null)
                {
                    if (ArcadeInputHandler.Instance.HasDirectTargetX)
                    {
                        isTryingToMove = Mathf.Abs(ArcadeInputHandler.Instance.DirectTargetWorldX - frozenAnchorX) > 0.15f;
                    }
                    else
                    {
                        isTryingToMove = Mathf.Abs(ArcadeInputHandler.Instance.HorizontalAxis) > 0.05f;
                    }
                }

                if (isTryingToMove)
                {
                    float jitter = Mathf.Sin(Time.time * 45f) * 0.07f;
                    Vector3 p = transform.position;
                    p.x = Mathf.Clamp(frozenAnchorX + jitter, minX, maxX);
                    transform.position = p;
                }
                else
                {
                    Vector3 p = transform.position;
                    p.x = frozenAnchorX;
                    transform.position = p;
                }

                currentVelocityX = 0f;
                previousPositionX = transform.position.x;
                return;
            }

            if (ArcadeInputHandler.Instance != null && ArcadeInputHandler.Instance.HasDirectTargetX)
            {
                float targetX = Mathf.Clamp(ArcadeInputHandler.Instance.DirectTargetWorldX, minX, maxX);
                Vector3 pos = transform.position;
                if (isSlowed)
                {
                    // Sluggish drag lag for mouse/touch
                    pos.x = Mathf.MoveTowards(pos.x, targetX, 8.5f * Time.deltaTime);
                }
                else
                {
                    pos.x = targetX;
                }
                transform.position = pos;
            }
            else
            {
                float inputAxis = ArcadeInputHandler.Instance != null ? ArcadeInputHandler.Instance.HorizontalAxis : 0f;
                if (Mathf.Abs(inputAxis) > 0.001f)
                {
                    float speed = isSlowed ? moveSpeed * 0.5f : moveSpeed;
                    Vector3 pos = transform.position;
                    pos.x += inputAxis * speed * Time.deltaTime;
                    pos.x = Mathf.Clamp(pos.x, minX, maxX);
                    transform.position = pos;
                }
            }

            float dt = Time.deltaTime;
            currentVelocityX = dt > 0.0001f ? (transform.position.x - prevX) / dt : 0f;
            previousPositionX = transform.position.x;
        }

        public const float MIN_PADDLE_WIDTH = 2.4f;
        public const float MAX_PADDLE_WIDTH = 12.0f;

        /// <summary>
        /// Shrinks the paddle width (e.g. 0.18f for -18%) clamped to minimum width.
        /// </summary>
        public void ShrinkWidth(float percentage = 0.18f)
        {
            float targetWidth = Mathf.Clamp(paddleWidth * (1.0f - percentage), MIN_PADDLE_WIDTH, MAX_PADDLE_WIDTH);
            if (Application.isPlaying && gameObject.activeInHierarchy)
            {
                if (expandCoroutine != null) StopCoroutine(expandCoroutine);
                expandCoroutine = StartCoroutine(AnimateExpandOvershoot(targetWidth, 0.30f));
            }
            else
            {
                SetWidth(targetWidth);
            }
        }

        /// <summary>
        /// Updates the paddle's horizontal width and recalculates collision boundary limits.
        /// </summary>
        public void SetWidth(float newWidth)
        {
            paddleWidth = Mathf.Clamp(newWidth, MIN_PADDLE_WIDTH, MAX_PADDLE_WIDTH);
            Vector3 scale = transform.localScale;
            scale.x = paddleWidth;
            transform.localScale = scale;
            RecalculateBounds();

            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            transform.position = pos;
        }

        /// <summary>
        /// Expands the paddle width compoundingly with spring overshoot animation (e.g. 0.10f for +10%).
        /// Multiple expander blocks compound: W_n = W_prev * (1 + percentage).
        /// </summary>
        public void ExpandWidth(float percentage = 0.10f)
        {
            expansionCount++;
            float targetWidth = Mathf.Clamp(paddleWidth * (1.0f + percentage), 2.0f, 12.0f);

            if (Application.isPlaying && gameObject.activeInHierarchy)
            {
                if (expandCoroutine != null) StopCoroutine(expandCoroutine);
                expandCoroutine = StartCoroutine(AnimateExpandOvershoot(targetWidth, 0.35f));
            }
            else
            {
                SetWidth(targetWidth);
            }
        }

        /// <summary>
        /// Smooth spring overshoot animation curve for tactile arcade expansion feedback.
        /// </summary>
        private IEnumerator AnimateExpandOvershoot(float targetWidth, float duration)
        {
            float startWidth = paddleWidth;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);

                // Spring decay function with ~16% overshoot and settle
                float springProgress = 1.0f - Mathf.Exp(-6f * u) * Mathf.Cos(u * Mathf.PI * 2.5f);
                float currentW = Mathf.LerpUnclamped(startWidth, targetWidth, springProgress);

                SetWidth(currentW);

                // Tactile vertical squash & stretch during expansion pulse
                float yFactor = 1.0f - (springProgress - 1.0f) * 0.35f;
                transform.localScale = new Vector3(currentW, Mathf.Clamp(yFactor, 0.88f, 1.06f), 1.0f);

                yield return null;
            }

            SetWidth(targetWidth);
            transform.localScale = new Vector3(targetWidth, 1.0f, 1.0f);
            expandCoroutine = null;
        }

        /// <summary>
        /// Triggers a micro-squash recoil effect on ball contact.
        /// When isSmash is true, delivers a deeper, snappier squash response.
        /// </summary>
        public void TriggerImpactRecoil(bool isSmash = false)
        {
            if (!Application.isPlaying || !gameObject.activeInHierarchy) return;
            if (recoilCoroutine != null) StopCoroutine(recoilCoroutine);
            float duration = isSmash ? 0.14f : 0.12f;
            float squashAmount = isSmash ? 0.18f : 0.12f;
            recoilCoroutine = StartCoroutine(AnimateImpactRecoil(duration, squashAmount));
        }

        private IEnumerator AnimateImpactRecoil(float duration, float squashAmount)
        {
            float elapsed = 0f;
            float currentW = transform.localScale.x;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float squash = 1.0f - Mathf.Sin(u * Mathf.PI) * squashAmount;

                transform.localScale = new Vector3(currentW, squash, 1.0f);
                yield return null;
            }

            transform.localScale = new Vector3(currentW, 1.0f, 1.0f);
            recoilCoroutine = null;
        }

        /// <summary>
        /// Resets the paddle width to initial default (e.g. 5.0f) and clears expansion count.
        /// </summary>
        public void ResetWidth(float defaultWidth = 5.0f)
        {
            if (expandCoroutine != null)
            {
                StopCoroutine(expandCoroutine);
                expandCoroutine = null;
            }

            basePaddleWidth = defaultWidth;
            expansionCount = 0;
            transform.localScale = new Vector3(defaultWidth, 1.0f, 1.0f);
            SetWidth(defaultWidth);
        }

        /// <summary>
        /// Resets the paddle width to the cached level base width without changing base width itself.
        /// </summary>
        public void ResetToBaseWidth()
        {
            if (expandCoroutine != null)
            {
                StopCoroutine(expandCoroutine);
                expandCoroutine = null;
            }

            expansionCount = 0;
            transform.localScale = new Vector3(basePaddleWidth, 1.0f, 1.0f);
            SetWidth(basePaddleWidth);
        }

        public void RecalculateBounds()
        {
            float halfPaddle = paddleWidth * 0.5f;
            minX = -arenaHalfWidth + halfPaddle;
            maxX = arenaHalfWidth - halfPaddle;
        }

        /// <summary>
        /// Computes normalized hit factor where -1 is far left edge, 0 is center, and +1 is far right edge.
        /// </summary>
        public float CalculateHitOffset(float ballWorldX)
        {
            float halfWidth = paddleWidth * 0.5f;
            if (halfWidth <= 0.001f) return 0f;
            return Mathf.Clamp((ballWorldX - transform.position.x) / halfWidth, -1f, 1f);
        }

        public void ResetPosition()
        {
            transform.position = new Vector3(0f, transform.position.y, transform.position.z);
        }

        public void SetBounds(float min, float max)
        {
            minX = min;
            maxX = max;
        }
    }
}
