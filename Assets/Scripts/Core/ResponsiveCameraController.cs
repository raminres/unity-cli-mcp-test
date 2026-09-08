using UnityEngine;

namespace Arcade.Core
{
    /// <summary>
    /// Dynamically adjusts camera distance or FOV so that the entire playfield bounds
    /// (left wall, right wall, paddle, ball, and top wall) are always 100% visible on any
    /// device aspect ratio (PC 16:9, iPad 4:3, iPhone 19.5:9, or portrait simulator).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class ResponsiveCameraController : MonoBehaviour
    {
        [Header("Target Bounds To Always Keep In View")]
        [SerializeField] private Vector2 boundsCenter = new Vector2(0f, 8.5f);
        [SerializeField] private Vector2 boundsSize = new Vector2(23.0f, 32.5f); // Width +-11.5, Height covering -7.5 to 25.0
        [SerializeField] private float paddingMargin = 2.0f;

        [Header("Camera Configuration")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float minDistance = 20f;
        [SerializeField] private float maxDistance = 90f;

        private float lastAspectRatio = -1f;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;

        public Vector2 BoundsCenter => boundsCenter;
        public Vector2 BoundsSize => boundsSize;
        public float Padding => paddingMargin;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>();
            AdjustCameraFraming();
        }

        private void Start()
        {
            AdjustCameraFraming();
        }

        private void Update()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                AdjustCameraFraming();
            }
        }

        public void AdjustCameraFraming()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>();
            if (targetCamera == null) return;

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            float currentAspect = targetCamera.aspect;
            if (currentAspect <= 0.001f)
            {
                currentAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            }
            lastAspectRatio = currentAspect;

            float requiredDistance = CalculateRequiredDistance(
                boundsSize.x + paddingMargin,
                boundsSize.y + paddingMargin,
                targetCamera.fieldOfView,
                currentAspect
            );

            requiredDistance = Mathf.Clamp(requiredDistance, minDistance, maxDistance);

            // Update camera position
            Vector3 pos = transform.position;
            pos.x = boundsCenter.x;
            pos.y = boundsCenter.y;
            pos.z = -requiredDistance;
            transform.position = pos;
        }

        /// <summary>
        /// Pure mathematical calculation of required distance to frame both width and height within frustum.
        /// </summary>
        public static float CalculateRequiredDistance(float targetWidth, float targetHeight, float verticalFovDeg, float aspectRatio)
        {
            if (aspectRatio <= 0.001f) aspectRatio = 1.0f;

            float verticalHalfFovRad = (verticalFovDeg * 0.5f) * Mathf.Deg2Rad;
            float tanVert = Mathf.Tan(verticalHalfFovRad);

            // Distance to fit height
            float distanceForHeight = (targetHeight * 0.5f) / tanVert;

            // Distance to fit width: tan(horizHalfFov) = aspectRatio * tan(vertHalfFov)
            float tanHoriz = aspectRatio * tanVert;
            float distanceForWidth = (targetWidth * 0.5f) / tanHoriz;

            return Mathf.Max(distanceForHeight, distanceForWidth);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(new Vector3(boundsCenter.x, boundsCenter.y, 0f), new Vector3(boundsSize.x, boundsSize.y, 1f));
        }
    }
}
