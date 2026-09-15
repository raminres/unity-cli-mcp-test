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

        [Header("Dynamic Volley Pacing")]
        [SerializeField] private float speedRampInterval = 10f; // Seconds between speed boosts during volley
        [SerializeField] private float speedRampStepMultiplier = 0.08f; // +8% of base speed per step (~ +1.1 - 1.4 units/s)
        private float activeVolleyTime = 0f;
        private float nextSpeedRampTime = 10f;

        [Header("Anti-Trap & Deflection Guard Settings")]
        [SerializeField] private float minVerticalAngleDeg = 20f; // Floor to prevent horizontal wall-to-wall traps
        [SerializeField] private float minHorizontalAngleDeg = 5f; // Floor to prevent vertical 90-degree ping-pong loops
        [SerializeField] private float consecutiveWallSteepAngleDeg = 35f; // Boost angle on repeated side-wall bounces
        [SerializeField] private float verticalDeadzoneAngleDeg = 5f; // Exclusion half-angle around 90-degree vertical
        [SerializeField] private float paddleVelocityInfluence = 0.5f; // Momentum transfer from paddle movement
        private int consecutiveSideWallBounces = 0;

        private float currentSpeed;
        private bool isLaunched = false;
        private bool isPrimaryBall = true;
        private int currentVolleyStreak = 0;
        private MaterialPropertyBlock propBlock;
        private Renderer ballRenderer;
        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");

        public bool IsLaunched => isLaunched;
        public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
        public float CurrentSpeed => currentSpeed;
        public float ActiveVolleyTime => activeVolleyTime;
        public float NextSpeedRampTime => nextSpeedRampTime;
        public float SpeedRampInterval => speedRampInterval;
        public float MinVerticalAngleDeg => minVerticalAngleDeg;
        public float MinHorizontalAngleDeg => minHorizontalAngleDeg;
        public float ConsecutiveWallSteepAngleDeg => consecutiveWallSteepAngleDeg;
        public float VerticalDeadzoneAngleDeg => verticalDeadzoneAngleDeg;
        public float PaddleVelocityInfluence => paddleVelocityInfluence;
        public int ConsecutiveSideWallBounces => consecutiveSideWallBounces;
        public void SetConsecutiveSideWallBouncesForTesting(int count) => consecutiveSideWallBounces = count;
        public int CurrentVolleyStreak => currentVolleyStreak;
        public int CurrentVolleyMultiplier => GetVolleyMultiplier(currentVolleyStreak);
        public void SetVolleyStreakForTesting(int streak) => currentVolleyStreak = streak;

        public static int GetVolleyMultiplier(int streak)
        {
            if (streak <= 2) return 1;
            if (streak <= 4) return 2;
            if (streak <= 7) return 3;
            if (streak <= 10) return 4;
            return 5;
        }

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

            // Freeze ball physics when level clear celebration is active
            if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.IsLevelClearPending)
            {
                FreezeBall();
                return;
            }

            // Maintain constant Z = 0
            Vector3 pos = transform.position;
            if (Mathf.Abs(pos.z) > 0.01f)
            {
                pos.z = 0f;
                transform.position = pos;
            }

            // Dynamic time-based speed escalation during active volleys
            if (ArcadeGameManager.Instance == null || ArcadeGameManager.Instance.State == GameState.Playing)
            {
                ApplyTimeBasedSpeedRamp(Time.fixedDeltaTime);
            }

            // Maintain target speed and prevent stagnation (Options A3 & B3)
            Vector3 vel = rb.linearVelocity;
            vel.z = 0f;

            if (vel.sqrMagnitude > 0.001f)
            {
                rb.linearVelocity = SanitizeTrajectory(vel, currentSpeed, minVerticalAngleDeg, minHorizontalAngleDeg, transform.position.x);
            }
        }

        /// <summary>
        /// Clamps and sanitizes a 2D velocity vector to prevent shallow horizontal traps (< minVertAngleDeg)
        /// and pure vertical ping-pong loops (< minHorizAngleDeg), preserving target speed and vector signs.
        /// </summary>
        public static Vector3 SanitizeTrajectory(Vector3 velocity, float targetSpeed, float minVertAngleDeg = 20f, float minHorizAngleDeg = 5f, float positionX = 0f)
        {
            if (velocity.sqrMagnitude < 0.0001f || targetSpeed <= 0f)
                return velocity;

            velocity.z = 0f;
            float vx = velocity.x;
            float vy = velocity.y;

            // 1. Option A3: Guard against near-horizontal trap (|vy| >= speed * sin(minVertAngleDeg))
            if (minVertAngleDeg > 0f)
            {
                float sinMinVert = Mathf.Sin(minVertAngleDeg * Mathf.Deg2Rad);
                float minVy = targetSpeed * sinMinVert;
                if (Mathf.Abs(vy) < minVy)
                {
                    vy = (vy >= 0f ? 1f : -1f) * minVy;
                    float remainingSpeedSqr = Mathf.Max(0.01f, (targetSpeed * targetSpeed) - (vy * vy));
                    vx = (vx >= 0f ? 1f : -1f) * Mathf.Sqrt(remainingSpeedSqr);
                }
            }

            // 2. Option B3: Guard against near-vertical loop (|vx| >= speed * sin(minHorizAngleDeg))
            if (minHorizAngleDeg > 0f)
            {
                float sinMinHoriz = Mathf.Sin(minHorizAngleDeg * Mathf.Deg2Rad);
                float minVx = targetSpeed * sinMinHoriz;
                if (Mathf.Abs(vx) < minVx)
                {
                    // If horizontal component is near zero, drift subtly away from center or rightward
                    float dirX = Mathf.Abs(vx) > 0.0001f ? Mathf.Sign(vx) : (positionX >= 0f ? 1f : -1f);
                    vx = dirX * minVx;
                    float remainingSpeedSqr = Mathf.Max(0.01f, (targetSpeed * targetSpeed) - (vx * vx));
                    vy = (vy >= 0f ? 1f : -1f) * Mathf.Sqrt(remainingSpeedSqr);
                }
            }

            return new Vector3(vx, vy, 0f).normalized * targetSpeed;
        }

        /// <summary>
        /// Progressively escalates ball speed during prolonged volleys (e.g. every 10 seconds),
        /// keeping volleys dynamic, preventing stalemates, and ramping up arcade excitement.
        /// </summary>
        public void ApplyTimeBasedSpeedRamp(float dt)
        {
            if (!isLaunched || dt <= 0f) return;

            activeVolleyTime += dt;
            while (activeVolleyTime >= nextSpeedRampTime)
            {
                float speedBoost = baseSpeed * speedRampStepMultiplier;
                currentSpeed = Mathf.Min(currentSpeed + speedBoost, maxSpeed);
                nextSpeedRampTime += speedRampInterval;
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
                activeVolleyTime = 0f;
                nextSpeedRampTime = speedRampInterval;
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
            activeVolleyTime = 0f;
            nextSpeedRampTime = speedRampInterval;
            consecutiveSideWallBounces = 0;
            currentVolleyStreak = 0;
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

        /// <summary>
        /// Instantly stops the ball's movement and angular velocity.
        /// Used when level clear transition begins to freeze playfield entities cleanly.
        /// </summary>
        public void FreezeBall()
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
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
            activeVolleyTime = 0f;
            nextSpeedRampTime = speedRampInterval;
            consecutiveSideWallBounces = 0;
            currentVolleyStreak = 0;
            currentSpeed = baseSpeed;

            if (Trail != null)
            {
                Trail.Clear();
                Trail.SetEmitting(true);
            }

            // Launch upwards with slight random angular bias (+- 15 degrees off vertical)
            float randomAngleOffset = UnityEngine.Random.Range(-15f, 15f);
            // Ensure launch trajectory never falls within vertical deadzone (-5° to +5°)
            if (Mathf.Abs(randomAngleOffset) < 5f)
            {
                randomAngleOffset = randomAngleOffset >= 0f ? 5f : -5f;
            }

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
            activeVolleyTime = 0f;
            nextSpeedRampTime = speedRampInterval;
            consecutiveSideWallBounces = 0;
            currentVolleyStreak = 0;
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
                consecutiveSideWallBounces = 0; // Reset consecutive wall bounces on block impact
                currentVolleyStreak++;
                currentSpeed = Mathf.Min(currentSpeed + speedIncrementPerHit, maxSpeed);

                if (ArcadeGameManager.Instance != null)
                {
                    ArcadeGameManager.Instance.NotifyVolleyHit(this, currentVolleyStreak);
                }
                return;
            }

            // Hit wall/ceiling
            if (collision.contacts.Length > 0)
            {
                Vector3 normal = collision.contacts[0].normal;
                // Side wall contact: normal points predominantly in +/- X direction
                if (Mathf.Abs(normal.x) > 0.7f && Mathf.Abs(normal.y) < 0.5f)
                {
                    consecutiveSideWallBounces++;
                    if (consecutiveSideWallBounces >= 2)
                    {
                        // Option A3: Boost vertical angle to >= consecutiveWallSteepAngleDeg (e.g. 35°)
                        ApplyConsecutiveWallSteepening();
                    }
                }
                else
                {
                    // Top ceiling, angled corner chamfer or other horizontal/diagonal surface
                    consecutiveSideWallBounces = 0;
                }
            }

            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayWallBounce();
            }
        }

        /// <summary>
        /// Steepens the ball's vertical velocity component after repeated side-wall bounces,
        /// breaking horizontal traps and forcing rapid downward/upward progression.
        /// </summary>
        public void ApplyConsecutiveWallSteepening()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb == null) return;

            Vector3 vel = rb.linearVelocity;
            vel.z = 0f;
            float effectiveSpeed = currentSpeed > 0.01f ? currentSpeed : (vel.magnitude > 0.01f ? vel.magnitude : baseSpeed);
            float steepSin = Mathf.Sin(consecutiveWallSteepAngleDeg * Mathf.Deg2Rad);
            float minSteepVy = effectiveSpeed * steepSin;

            if (Mathf.Abs(vel.y) < minSteepVy)
            {
                float vy = (vel.y >= 0f ? 1f : -1f) * minSteepVy;
                float remainingSpeedSqr = Mathf.Max(0.01f, (effectiveSpeed * effectiveSpeed) - (vy * vy));
                float vx = (vel.x >= 0f ? 1f : -1f) * Mathf.Sqrt(remainingSpeedSqr);
                rb.linearVelocity = new Vector3(vx, vy, 0f);
            }
        }

        /// <summary>
        /// Computes a physics-informed paddle deflection vector:
        /// 1. Takes natural optical ray reflection off the horizontal paddle (preserving forward horizontal momentum).
        /// 2. Applies paddle steering based on normalized contact hitOffset (-1 to +1).
        /// 3. Incorporates paddle movement velocity momentum transfer.
        /// 4. Disallows trajectories in the near-vertical deadzone [90° - deadzone, 90° + deadzone].
        /// 5. Clamps final bounce angle to playable arcade bounds (minAngleDeg to maxAngleDeg) to prevent horizontal locks.
        /// </summary>
        public static Vector3 CalculatePaddleDeflection(
            Vector3 inVelocity,
            float hitOffset,
            float steerStrength = 32f,
            float minAngleDeg = 25f,
            float maxAngleDeg = 155f,
            float paddleVelocityX = 0f,
            float velocityInfluence = 0.5f,
            float verticalDeadzoneAngleDeg = 5f)
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

            // Paddle steering influence from contact offset:
            // Positive offset (right of center) biases angle toward shallow right (-deg).
            // Negative offset (left of center) biases angle toward shallow left (+deg).
            float steerAngleDeg = -hitOffset * steerStrength;

            // Paddle velocity momentum transfer:
            // Sliding paddle right (+X) biases angle right (-deg in polar coordinates).
            // Sliding paddle left (-X) biases angle left (+deg in polar coordinates).
            float velocitySteerDeg = Mathf.Clamp(-paddleVelocityX * velocityInfluence, -12f, 12f);

            float computedAngleDeg = rayAngleDeg + steerAngleDeg + velocitySteerDeg;

            // Option B3: Exclude near-vertical deadzone [90° - deadzone, 90° + deadzone]
            if (verticalDeadzoneAngleDeg > 0f)
            {
                float minDeadzone = 90f - verticalDeadzoneAngleDeg;
                float maxDeadzone = 90f + verticalDeadzoneAngleDeg;

                if (computedAngleDeg >= minDeadzone && computedAngleDeg <= maxDeadzone)
                {
                    // If paddle velocity or hit offset has a rightward bias, push to right (minDeadzone)
                    if (paddleVelocityX > 0.1f || hitOffset > 0.02f)
                    {
                        computedAngleDeg = minDeadzone;
                    }
                    // If paddle velocity or hit offset has a leftward bias, push to left (maxDeadzone)
                    else if (paddleVelocityX < -0.1f || hitOffset < -0.02f)
                    {
                        computedAngleDeg = maxDeadzone;
                    }
                    else
                    {
                        // Default tie-break: push based on incoming velocity or slight rightward bias
                        computedAngleDeg = inX >= 0f ? minDeadzone : maxDeadzone;
                    }
                }
            }

            // Clamped final angle preserving natural momentum while allowing sharp cuts
            float finalAngleDeg = Mathf.Clamp(computedAngleDeg, minAngleDeg, maxAngleDeg);
            float finalRad = finalAngleDeg * Mathf.Deg2Rad;

            return new Vector3(Mathf.Cos(finalRad), Mathf.Sin(finalRad), 0f).normalized;
        }

        private void HandlePaddleCollision(PaddleController hitPaddle)
        {
            consecutiveSideWallBounces = 0; // Reset consecutive wall bounces on paddle save

            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.NotifyVolleySaved(this, currentVolleyStreak);
            }

            currentVolleyStreak = 0;

            float hitOffset = hitPaddle.CalculateHitOffset(transform.position.x);
            float paddleVelX = hitPaddle.VelocityX;

            Vector3 inVelocity = rb != null ? rb.linearVelocity : Vector3.down * currentSpeed;
            Vector3 newDir = CalculatePaddleDeflection(
                inVelocity,
                hitOffset,
                32f,
                25f,
                155f,
                paddleVelX,
                paddleVelocityInfluence,
                verticalDeadzoneAngleDeg);

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
