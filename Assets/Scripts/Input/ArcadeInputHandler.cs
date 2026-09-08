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
        [SerializeField] private float touchSensitivity = 1.2f;
        [SerializeField] private float launchSwipeThreshold = 50f;

        private float horizontalInput = 0f;
        private Vector2 touchStartPos;
        private bool isDragging = false;
        private Camera mainCam;

        public float HorizontalAxis => horizontalInput;

        public event Action OnLaunchTriggered;
        public event Action OnPauseTriggered;

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

                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    TriggerLaunch();
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

            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                TriggerLaunch();
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
                pointerUp = true;
            }
#endif

            if (pointerDown)
            {
                touchStartPos = screenPos;
                isDragging = true;
            }
            else if (pointerHeld && isDragging)
            {
                // In touch drag, compute target X in world coordinates
                if (mainCam != null)
                {
                    Vector3 worldPos = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCam.transform.position.z));
                    // We can expose the world position or map to delta
                    float deltaX = screenPos.x - touchStartPos.x;
                    horizontalInput = Mathf.Clamp(deltaX / (Screen.width * 0.15f) * touchSensitivity, -1f, 1f);
                }

                // Check for upward swipe to launch
                if (screenPos.y - touchStartPos.y > launchSwipeThreshold)
                {
                    TriggerLaunch();
                }
            }
            else if (pointerUp)
            {
                // Short tap with little movement launches ball
                if (Vector2.Distance(screenPos, touchStartPos) < 25f)
                {
                    TriggerLaunch();
                }

                isDragging = false;
                horizontalInput = 0f;
            }
        }

        public void TriggerLaunch()
        {
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
