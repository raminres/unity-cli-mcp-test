using Arcade.Core;
using Arcade.Input;
using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// Controls the player platform (paddle) with 5:1 aspect ratio and boundary clamping.
    /// </summary>
    public class PaddleController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 16f;
        [SerializeField] private float minX = -7.5f;
        [SerializeField] private float maxX = 7.5f;
        [SerializeField] private float paddleWidth = 5.0f;

        [Header("References")]
        [SerializeField] private Rigidbody rb;

        public float Width => paddleWidth;
        public float MinX => minX;
        public float MaxX => maxX;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
        }

        private void Update()
        {
            if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State == GameState.Paused)
                return;

            float inputAxis = ArcadeInputHandler.Instance != null ? ArcadeInputHandler.Instance.HorizontalAxis : 0f;
            if (Mathf.Abs(inputAxis) > 0.001f)
            {
                Vector3 pos = transform.position;
                pos.x += inputAxis * moveSpeed * Time.deltaTime;
                pos.x = Mathf.Clamp(pos.x, minX, maxX);
                transform.position = pos;
            }
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
