using Arcade.Audio;
using Arcade.Core;
using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Manages ball physics, docked launch state, constant speed maintenance, and dynamic paddle deflection.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour
    {
        [Header("Ball Movement Settings")]
        [SerializeField] private float baseSpeed = 14f;
        [SerializeField] private float maxSpeed = 22f;
        [SerializeField] private float speedIncrementPerHit = 0.15f;
        [SerializeField] private float launchYOffset = 0.85f;
        [SerializeField] private float maxDeflectionAngleDegrees = 60f;

        [Header("References")]
        [SerializeField] private PaddleController paddle;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private BallTrail ballTrail;
        [SerializeField] private Color primaryBallColor = new Color(0f, 0.95f, 1f, 1f); // Electric Cyan

        private float currentSpeed;
        private bool isLaunched = false;
        private bool isPrimaryBall = true;
        private MaterialPropertyBlock propBlock;
        private Renderer ballRenderer;
        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");

        public bool IsLaunched => isLaunched;
        public float CurrentSpeed => currentSpeed;
        public BallTrail Trail => ballTrail != null ? ballTrail : (ballTrail = GetComponent<BallTrail>() ?? gameObject.AddComponent<BallTrail>());
        public bool IsPrimaryBall
        {
            get => isPrimaryBall;
            set => isPrimaryBall = value;
        }

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();

            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

            currentSpeed = baseSpeed;

            if (ballTrail == null)
            {
                ballTrail = GetComponent<BallTrail>() ?? gameObject.AddComponent<BallTrail>();
            }

            ballRenderer = GetComponent<Renderer>();
            propBlock = new MaterialPropertyBlock();

            if (isPrimaryBall)
            {
                SetTrailColor(primaryBallColor);
            }
        }

        private void OnEnable()
        {
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.OnStateChanged -= HandleStateChanged;
                ArcadeGameManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.UnregisterBall(this);
            }
        }

        private void Start()
        {
            if (paddle == null)
            {
                paddle = FindAnyObjectByType<PaddleController>();
            }

            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.OnStateChanged -= HandleStateChanged;
                ArcadeGameManager.Instance.OnStateChanged += HandleStateChanged;
                ArcadeGameManager.Instance.RegisterBall(this);
            }

            if (isPrimaryBall)
            {
                ResetBallToPaddle();
            }
        }

        public void HandleStateChangedDirect(GameState state)
        {
            HandleStateChanged(state);
        }

        public void Initialize(ArcadeGameManager manager, PaddleController paddleController)
        {
            if (paddleController != null) paddle = paddleController;
            if (manager != null)
            {
                manager.OnStateChanged -= HandleStateChanged;
                manager.OnStateChanged += HandleStateChanged;
            }
        }

        private void Update()
        {
            if (!isLaunched && paddle != null)
            {
                // Lock ball position directly above paddle center
                Vector3 paddlePos = paddle.transform.position;
                transform.position = new Vector3(paddlePos.x, paddlePos.y + launchYOffset, 0f);
            }
        }

        private void FixedUpdate()
        {
            if (!isLaunched) return;

            // Maintain constant Z = 0
            Vector3 pos = transform.position;
            if (Mathf.Abs(pos.z) > 0.01f)
            {
                pos.z = 0f;
                transform.position = pos;
            }

            // Maintain target speed and prevent stagnation
            Vector3 vel = rb.linearVelocity;
            vel.z = 0f;

            // Guard against near-horizontal or dead locks
            if (Mathf.Abs(vel.y) < 1.5f && vel.sqrMagnitude > 1f)
            {
                vel.y = vel.y >= 0 ? 2.5f : -2.5f;
            }

            if (vel.sqrMagnitude > 0.001f)
            {
                rb.linearVelocity = vel.normalized * currentSpeed;
            }
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            float m = Mathf.Clamp(multiplier, 0.5f, 3.0f);
            baseSpeed = 14f * m;
            maxSpeed = 22f * m;
            if (!isLaunched)
            {
                currentSpeed = baseSpeed;
            }
        }

        public void SetTrailColor(Color color)
        {
            if (Trail != null)
            {
                Trail.SetTrailColor(color);
            }
            ApplyBallColor(color);
        }

        private void ApplyBallColor(Color color)
        {
            if (ballRenderer == null) ballRenderer = GetComponent<Renderer>();
            if (ballRenderer != null)
            {
                if (propBlock == null) propBlock = new MaterialPropertyBlock();
                ballRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor(EmissionColorProp, color * 1.15f);
                propBlock.SetColor(BaseColorProp, Color.Lerp(Color.white, color, 0.25f));
                ballRenderer.SetPropertyBlock(propBlock);
            }
        }

        public void StopAndDockBall()
        {
            isLaunched = false;
            currentSpeed = baseSpeed;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (paddle != null)
            {
                Vector3 paddlePos = paddle.transform.position;
                transform.position = new Vector3(paddlePos.x, paddlePos.y + launchYOffset, 0f);
            }

            if (Trail != null)
            {
                Trail.SetEmitting(false);
                Trail.Clear();
            }
        }

        public void SetBallActive(bool active)
        {
            var rend = GetComponent<Renderer>();
            if (rend != null) rend.enabled = active;

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = active;

            if (Trail != null && !active)
            {
                Trail.SetEmitting(false);
                Trail.Clear();
            }
        }

        public void ResetBallToPaddle()
        {
            SetBallActive(true);
            StopAndDockBall();
        }

        public void Launch()
        {
            if (isLaunched) return;

            SetBallActive(true);
            isLaunched = true;
            currentSpeed = baseSpeed;

            if (Trail != null)
            {
                Trail.Clear();
                Trail.SetEmitting(true);
            }

            // Launch upwards with slight random angular bias (+- 15 degrees off vertical)
            float randomAngleOffset = UnityEngine.Random.Range(-15f, 15f);
            float launchRad = (90f + randomAngleOffset) * Mathf.Deg2Rad;
            Vector3 launchDirection = new Vector3(Mathf.Cos(launchRad), Mathf.Sin(launchRad), 0f).normalized;

            if (rb != null)
            {
                rb.linearVelocity = launchDirection * currentSpeed;
            }
        }

        public void LaunchWithDirection(Vector3 direction, float speed)
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            SetBallActive(true);
            isLaunched = true;
            currentSpeed = speed > 0f ? speed : baseSpeed;

            if (Trail != null)
            {
                Trail.Clear();
                Trail.SetEmitting(true);
            }

            if (rb != null)
            {
                rb.linearVelocity = direction.normalized * currentSpeed;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (!isPrimaryBall)
            {
                if (state != GameState.Playing && state != GameState.Paused)
                {
                    if (Application.isPlaying) Destroy(gameObject);
                    else DestroyImmediate(gameObject);
                }
                return;
            }

            if (state == GameState.Playing)
            {
                if (!isLaunched)
                {
                    Launch();
                }
                else if (ballTrail != null)
                {
                    ballTrail.SetEmitting(true);
                }
            }
            else if (state == GameState.BallLost || state == GameState.ReadyToLaunch)
            {
                ResetBallToPaddle();
            }
            else if (state == GameState.LevelClear)
            {
                StopAndDockBall();
                SetBallActive(false);
            }
            else if (state == GameState.GameOver)
            {
                StopAndDockBall();
                SetBallActive(false);
            }
            else if (state == GameState.Paused)
            {
                if (ballTrail != null)
                {
                    ballTrail.SetEmitting(false);
                }
            }
        }

        public const float MIN_UPWARD_NORMAL_Y = 0.25f;

        /// <summary>
        /// Validates that a collision normal points predominantly upward, preventing side/bottom edge saves.
        /// </summary>
        public static bool IsValidPaddleBounceNormal(Vector3 normal)
        {
            return normal.y >= MIN_UPWARD_NORMAL_Y;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isLaunched) return;

            // Check if we hit the paddle
            PaddleController hitPaddle = collision.gameObject.GetComponent<PaddleController>()
                ?? collision.gameObject.GetComponentInParent<PaddleController>();
            if (hitPaddle != null)
            {
                // Contact Normal Guard:
                // Only upward-facing contacts on the top strike deck count as paddle saves.
                // If contactNormal.y < MIN_UPWARD_NORMAL_Y, it's a side-wall or underneath collision;
                // do not trigger upward deflection so balls that missed the top drop into killzone.
                if (collision.contacts.Length > 0)
                {
                    Vector3 contactNormal = collision.contacts[0].normal;
                    if (!IsValidPaddleBounceNormal(contactNormal))
                    {
                        if (ArcadeAudioManager.Instance != null)
                        {
                            ArcadeAudioManager.Instance.PlayWallBounce();
                        }
                        return;
                    }
                }

                HandlePaddleCollision(hitPaddle);
                hitPaddle.TriggerImpactRecoil();
                return;
            }

            // Check if we hit a block
            Block block = collision.gameObject.GetComponent<Block>();
            if (block != null)
            {
                currentSpeed = Mathf.Min(currentSpeed + speedIncrementPerHit, maxSpeed);
                return;
            }

            // Hit wall/ceiling
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayWallBounce();
            }
        }

        /// <summary>
        /// Computes a physics-informed paddle deflection vector:
        /// 1. Takes natural optical ray reflection off the horizontal paddle (preserving forward horizontal momentum).
        /// 2. Applies paddle steering based on normalized contact hitOffset (-1 to +1).
        /// 3. Clamps final bounce angle to playable arcade bounds (25° to 155°) to prevent horizontal locks.
        /// </summary>
        public static Vector3 CalculatePaddleDeflection(Vector3 inVelocity, float hitOffset, float steerStrength = 32f, float minAngleDeg = 25f, float maxAngleDeg = 155f)
        {
            float inX = inVelocity.x;
            float inY = Mathf.Abs(inVelocity.y); // Upward reflection normal

            // Fallback for near-zero incoming velocities
            if (Mathf.Abs(inX) < 0.01f && inY < 0.01f)
            {
                inY = 1.0f;
            }

            // Natural optical ray reflection angle in degrees (0 to 180)
            float rayAngleDeg = Mathf.Atan2(inY, inX) * Mathf.Rad2Deg;

            // Paddle steering influence:
            // Positive offset (right of center) biases angle toward shallow right (-deg).
            // Negative offset (left of center) biases angle toward shallow left (+deg).
            float steerAngleDeg = -hitOffset * steerStrength;

            // Clamped final angle preserving natural momentum while allowing sharp cuts
            float finalAngleDeg = Mathf.Clamp(rayAngleDeg + steerAngleDeg, minAngleDeg, maxAngleDeg);
            float finalRad = finalAngleDeg * Mathf.Deg2Rad;

            return new Vector3(Mathf.Cos(finalRad), Mathf.Sin(finalRad), 0f).normalized;
        }

        private void HandlePaddleCollision(PaddleController hitPaddle)
        {
            float hitOffset = hitPaddle.CalculateHitOffset(transform.position.x);

            Vector3 inVelocity = rb != null ? rb.linearVelocity : Vector3.down * currentSpeed;
            Vector3 newDir = CalculatePaddleDeflection(inVelocity, hitOffset, 32f, 25f, 155f);

            if (rb != null)
            {
                rb.linearVelocity = newDir * currentSpeed;
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayPaddleBounce();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<KillZone>() != null || other.name.Contains("KillZone"))
            {
                if (ArcadeGameManager.Instance != null)
                {
                    ArcadeGameManager.Instance.HandleBallFell(this);
                }
            }
        }
    }
}
