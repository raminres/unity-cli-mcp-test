using UnityEngine;
using UnityEngine.UIElements;

namespace Arcade.UI
{
    /// <summary>
    /// Applies device safe area insets (notches, dynamic islands, home bars) to UI Toolkit visual trees.
    /// Supports automatic adaptation to screen orientation and editor notch simulation.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SafeAreaController : MonoBehaviour
    {
        [Header("Target Element")]
        [Tooltip("Optional name of child VisualElement to apply padding to. If empty, applies to rootVisualElement.")]
        [SerializeField] private string targetContainerName = "";

        [Header("Editor Simulation")]
        [SerializeField] private bool simulateInEditor = false;
        [SerializeField] private float simulatedTopInsetPixels = 100f;
        [SerializeField] private float simulatedBottomInsetPixels = 60f;
        [SerializeField] private float simulatedSideInsetPixels = 0f;

        private UIDocument uiDocument;
        private VisualElement targetElement;
        private Rect lastSafeArea = Rect.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;
        private Vector2Int lastResolution = Vector2Int.zero;

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea ||
                Screen.orientation != lastOrientation ||
                Screen.width != lastResolution.x ||
                Screen.height != lastResolution.y)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;

            targetElement = string.IsNullOrEmpty(targetContainerName)
                ? uiDocument.rootVisualElement
                : uiDocument.rootVisualElement.Q(targetContainerName) ?? uiDocument.rootVisualElement;

            Rect safeArea = Screen.safeArea;
            float screenW = Screen.width;
            float screenH = Screen.height;

#if UNITY_EDITOR
            if (simulateInEditor)
            {
                safeArea = new Rect(
                    simulatedSideInsetPixels,
                    simulatedBottomInsetPixels,
                    screenW - (simulatedSideInsetPixels * 2),
                    screenH - (simulatedTopInsetPixels + simulatedBottomInsetPixels)
                );
            }
#endif

            lastSafeArea = safeArea;
            lastOrientation = Screen.orientation;
            lastResolution = new Vector2Int(Screen.width, Screen.height);

            if (screenW <= 0 || screenH <= 0) return;

            // Compute insets as percentages of total screen dimensions
            float leftPct = (safeArea.x / screenW) * 100f;
            float rightPct = ((screenW - (safeArea.x + safeArea.width)) / screenW) * 100f;
            float bottomPct = (safeArea.y / screenH) * 100f;
            float topPct = ((screenH - (safeArea.y + safeArea.height)) / screenH) * 100f;

            targetElement.style.paddingLeft = Length.Percent(Mathf.Max(0f, leftPct));
            targetElement.style.paddingRight = Length.Percent(Mathf.Max(0f, rightPct));
            targetElement.style.paddingTop = Length.Percent(Mathf.Max(0f, topPct));
            targetElement.style.paddingBottom = Length.Percent(Mathf.Max(0f, bottomPct));
        }

        /// <summary>
        /// Pure helper to compute safe area percentage insets from screen rects.
        /// </summary>
        public static (float left, float right, float top, float bottom) CalculateInsets(Rect safeArea, float screenW, float screenH)
        {
            if (screenW <= 0 || screenH <= 0) return (0, 0, 0, 0);

            float left = (safeArea.x / screenW) * 100f;
            float right = ((screenW - (safeArea.x + safeArea.width)) / screenW) * 100f;
            float bottom = (safeArea.y / screenH) * 100f;
            float top = ((screenH - (safeArea.y + safeArea.height)) / screenH) * 100f;

            return (left, right, top, bottom);
        }
    }
}
