using System;
using Arcade.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Arcade.Input
{
    /// <summary>
    /// Cross-platform input coordinator supporting PC keyboard, Mobile touch drag/tap, and WebGPU mouse/keys.
    /// </summary>
    public class ArcadeInputHandler : MonoBehaviour
    {
        public static ArcadeInputHandler Instance { get; private set; }

        [Header("Touch / Mouse Settings")]
        [SerializeField] private float touchSensitivity = 1.0f;
        [SerializeField] private float launchSwipeThreshold = 40f;
        [SerializeField] private float dragDeadzonePixels = 15f;
        [SerializeField] private float tapSlopPixels = 45f;
        [SerializeField] private float maxTapDuration = 0.40f;

        private float horizontalInput = 0f;
        private Vector2 touchStartScreenPos;
        private float touchStartTime = 0f;
        private float touchStartPaddleX = 0f;
        private float touchStartWorldX = 0f;
        private bool isDragging = false;
        private bool hasDirectTargetX = false;
        private float directTargetWorldX = 0f;
        private bool touchStartedOverUI = false;
        private float launchSuppressedUntil = 0f;
        private Camera mainCam;

        public float HorizontalAxis => horizontalInput;
        public bool HasDirectTargetX => hasDirectTargetX;
        public float DirectTargetWorldX => directTargetWorldX;
        public bool IsTouchDragging => isDragging;

        public event Action OnLaunchTriggered;
        public event Action OnPauseTriggered;

        public static void SetInstanceForTesting(ArcadeInputHandler instance)
        {
            Instance = instance;
        }

        public bool IsLaunchSuppressed => Time.unscaledTime < launchSuppressedUntil;

        public void SuppressLaunch(float durationSeconds = 0.25f)
        {
            launchSuppressedUntil = Mathf.Max(launchSuppressedUntil, Time.unscaledTime + durationSeconds);
        }

        public void SetDirectTargetWorldXForTesting(float targetX)
        {
            directTargetWorldX = targetX;
            hasDirectTargetX = true;
            isDragging = true;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            mainCam = Camera.main;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (mainCam == null)
            {
                mainCam = Camera.main;
            }

            PollKeyboardInput();
            PollPointerInput();
        }

        private void PollKeyboardInput()
        {
            float axis = 0f;

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
                    axis -= 1f;
                if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
                    axis += 1f;

                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                {
                    if (Arcade.UI.ArcadeUIManager.Instance == null || !Arcade.UI.ArcadeUIManager.Instance.IsAnyModalVisible())
                    {
                        TriggerLaunch();
                    }
                }

                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
                {
                    TriggerPause();
                }
            }
#else
            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow) || UnityEngine.Input.GetKey(KeyCode.A))
                axis -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.RightArrow) || UnityEngine.Input.GetKey(KeyCode.D))
                axis += 1f;

            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || UnityEngine.Input.GetKeyDown(KeyCode.Space) || UnityEngine.Input.GetKeyDown(KeyCode.W))
            {
                if (Arcade.UI.ArcadeUIManager.Instance == null || !Arcade.UI.ArcadeUIManager.Instance.IsAnyModalVisible())
                {
                    TriggerLaunch();
                }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.P))
            {
                TriggerPause();
            }
#endif

            // Only override with keyboard if player pressed keys
            if (Mathf.Abs(axis) > 0.01f)
            {
                horizontalInput = axis;
                hasDirectTargetX = false;
            }
            else if (!isDragging)
            {
                horizontalInput = 0f;
            }
        }

        private void PollPointerInput()
        {
            Vector2 screenPos = Vector2.zero;
            bool pointerDown = false;
            bool pointerUp = false;
            bool pointerHeld = false;

#if ENABLE_INPUT_SYSTEM
            var touchScreen = Touchscreen.current;
            var mouse = Mouse.current;

            if (touchScreen != null && touchScreen.primaryTouch.press.isPressed)
            {
                screenPos = touchScreen.primaryTouch.position.ReadValue();
                pointerDown = touchScreen.primaryTouch.press.wasPressedThisFrame;
                pointerHeld = true;
            }
            else if (touchScreen != null && touchScreen.primaryTouch.press.wasReleasedThisFrame)
            {
                screenPos = touchScreen.primaryTouch.position.ReadValue();
                pointerUp = true;
            }
            else if (mouse != null && mouse.leftButton.isPressed)
            {
                screenPos = mouse.position.ReadValue();
                pointerDown = mouse.leftButton.wasPressedThisFrame;
                pointerHeld = true;
            }
            else if (mouse != null && mouse.leftButton.wasReleasedThisFrame)
            {
                screenPos = mouse.position.ReadValue();
                pointerUp = true;
            }
#else
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch t = UnityEngine.Input.GetTouch(0);
                screenPos = t.position;
                pointerDown = t.phase == TouchPhase.Began;
                pointerHeld = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary;
                pointerUp = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
            }
            else if (UnityEngine.Input.GetMouseButton(0))
            {
                screenPos = UnityEngine.Input.mousePosition;
                pointerDown = UnityEngine.Input.GetMouseButtonDown(0);
                pointerHeld = true;
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                screenPos = UnityEngine.Input.mousePosition;
                pointerUp = true;
            }
#endif

            if (pointerDown)
            {
                touchStartScreenPos = screenPos;
                touchStartTime = Time.unscaledTime;
                isDragging = false;
                hasDirectTargetX = false;

                // Shield gameplay input if touch originated over active UI
                if (Arcade.UI.ArcadeUIManager.Instance != null && Arcade.UI.ArcadeUIManager.Instance.IsPointerOverUI(screenPos))
                {
                    touchStartedOverUI = true;
                    return;
                }

                touchStartedOverUI = false;

                // Cache start world X on Z=0 playfield plane
                if (mainCam != null)
                {
                    float camZDist = Mathf.Abs(mainCam.transform.position.z);
                    Vector3 worldPt = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, camZDist));
                    touchStartWorldX = worldPt.x;
                }

                // Cache initial paddle position
                var paddle = FindAnyObjectByType<Arcade.BlockBreaker.PaddleController>();
                touchStartPaddleX = paddle != null ? paddle.transform.position.x : 0f;
            }
            else if (pointerHeld && !touchStartedOverUI)
            {
                float screenDist = Vector2.Distance(screenPos, touchStartScreenPos);
                if (!isDragging && screenDist > dragDeadzonePixels)
                {
                    isDragging = true;
                }

                if (isDragging && mainCam != null)
                {
                    float camZDist = Mathf.Abs(mainCam.transform.position.z);
                    Vector3 currWorldPt = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, camZDist));
                    float deltaWorldX = (currWorldPt.x - touchStartWorldX) * touchSensitivity;
                    directTargetWorldX = touchStartPaddleX + deltaWorldX;
                    hasDirectTargetX = true;

                    // Fallback relative axis for axis-based callers
                    float screenDeltaX = screenPos.x - touchStartScreenPos.x;
                    horizontalInput = Mathf.Clamp(screenDeltaX / (Screen.width * 0.15f) * touchSensitivity, -1f, 1f);
                }

                // Upward swipe to launch while dragging
                if (screenPos.y - touchStartScreenPos.y > launchSwipeThreshold)
                {
                    TriggerLaunch();
                }
            }
            else if (pointerUp)
            {
                bool pointerOverUIOnRelease = Arcade.UI.ArcadeUIManager.Instance != null && Arcade.UI.ArcadeUIManager.Instance.IsPointerOverUI(screenPos);
                if (!touchStartedOverUI && !pointerOverUIOnRelease && Time.unscaledTime >= launchSuppressedUntil)
                {
                    float tapDist = Vector2.Distance(screenPos, touchStartScreenPos);
                    float duration = Time.unscaledTime - touchStartTime;

                    // Adaptive tap detection: small distance moved OR quick tap (<0.40s) without large displacement
                    bool isTap = tapDist < tapSlopPixels || (duration < maxTapDuration && tapDist < launchSwipeThreshold * 1.5f);
                    if (isTap)
                    {
                        TriggerLaunch();
                    }
                }

                touchStartedOverUI = false;
                ResetTouchState();
            }
        }

        public void ResetTouchState()
        {
            isDragging = false;
            hasDirectTargetX = false;
            directTargetWorldX = 0f;
            horizontalInput = 0f;
            touchStartedOverUI = false;
            touchStartPaddleX = 0f;
            touchStartWorldX = 0f;
        }

        public void TriggerLaunch()
        {
            if (Time.unscaledTime < launchSuppressedUntil) return;
            if (Arcade.UI.ArcadeUIManager.Instance != null && Arcade.UI.ArcadeUIManager.Instance.IsAnyModalVisible()) return;

            OnLaunchTriggered?.Invoke();
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.LaunchBall();
            }
        }

        public void TriggerPause()
        {
            OnPauseTriggered?.Invoke();
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.TogglePause();
            }
        }
    }
}
