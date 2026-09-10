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

        private float currentSpeed;
        private bool isLaunched = false;
        private Vector3 lastVelocity;
        private bool isPrimaryBall = true;

        public bool IsLaunched => isLaunched;
        public float CurrentSpeed => currentSpeed;
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
        }

        private void OnEnable()
        {
            if (ArcadeGameManager.Instance != null)
            {
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
                ArcadeGameManager.Instance.RegisterBall(this);
            }

            if (isPrimaryBall)
            {
                ResetBallToPaddle();
            }
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
            lastVelocity = rb.linearVelocity;
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
        }

        public void SetBallActive(bool active)
        {
            var rend = GetComponent<Renderer>();
            if (rend != null) rend.enabled = active;

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = active;
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

            if (rb != null)
            {
                rb.linearVelocity = direction.normalized * currentSpeed;
            }
            lastVelocity = rb != null ? rb.linearVelocity : direction.normalized * currentSpeed;
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

            if (state == GameState.Playing && !isLaunched)
            {
                Launch();
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
                // Physics is paused by Time.timeScale in GameManager
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isLaunched) return;

            // Check if we hit the paddle
            PaddleController hitPaddle = collision.gameObject.GetComponent<PaddleController>();
            if (hitPaddle != null)
            {
                HandlePaddleCollision(hitPaddle);
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

        private void HandlePaddleCollision(PaddleController hitPaddle)
        {
            float hitOffset = hitPaddle.CalculateHitOffset(transform.position.x);

            // Angle mapping:
            // hitOffset = 0 -> 90 deg (straight up)
            // hitOffset = +1 -> 90 - 60 = 30 deg (shallow right)
            // hitOffset = -1 -> 90 - (-60) = 150 deg (shallow left)
            float bounceAngleDeg = 90f - (hitOffset * maxDeflectionAngleDegrees);
            float bounceRad = bounceAngleDeg * Mathf.Deg2Rad;

            Vector3 newDir = new Vector3(Mathf.Cos(bounceRad), Mathf.Sin(bounceRad), 0f).normalized;
            rb.linearVelocity = newDir * currentSpeed;

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
