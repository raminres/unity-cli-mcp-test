using UnityEngine;
using UnityEngine.UIElements;

namespace Arcade.UI
{
    /// <summary>
    /// Applies device safe area insets (notches, dynamic islands, home bars) to UI Toolkit visual trees.
    /// Incorporates extra breathing room margins so UI elements never sit directly against the safe area limits.
    /// Supports automatic adaptation to screen orientation and editor notch simulation.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SafeAreaController : MonoBehaviour
    {
        [Header("Target Element")]
        [Tooltip("Optional name of child VisualElement to apply padding to. If empty, applies to rootVisualElement.")]
        [SerializeField] private string targetContainerName = "";

        [Header("Breathing Room (Extra Safe Margins)")]
        [Tooltip("Extra margin percentage added on top of the physical hardware safe area.")]
        [Range(0f, 10f)] [SerializeField] private float extraTopPercent = 2.0f;
        [Range(0f, 10f)] [SerializeField] private float extraBottomPercent = 2.0f;
        [Range(0f, 10f)] [SerializeField] private float extraSidePercent = 1.5f;

        [Header("Editor Simulation")]
        [SerializeField] private bool simulateInEditor = false;
        [SerializeField] private float simulatedTopInsetPixels = 120f;
        [SerializeField] private float simulatedBottomInsetPixels = 70f;
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

            var (leftPct, rightPct, topPct, bottomPct) = CalculateInsets(safeArea, screenW, screenH, extraSidePercent, extraTopPercent, extraBottomPercent);

            targetElement.style.paddingLeft = Length.Percent(leftPct);
            targetElement.style.paddingRight = Length.Percent(rightPct);
            targetElement.style.paddingTop = Length.Percent(topPct);
            targetElement.style.paddingBottom = Length.Percent(bottomPct);
        }

        /// <summary>
        /// Pure helper to compute safe area percentage insets from screen rects with breathing margins.
        /// </summary>
        public static (float left, float right, float top, float bottom) CalculateInsets(
            Rect safeArea, float screenW, float screenH,
            float extraSide = 0f, float extraTop = 0f, float extraBottom = 0f)
        {
            if (screenW <= 0 || screenH <= 0) return (0, 0, 0, 0);

            float left = Mathf.Max(0f, (safeArea.x / screenW) * 100f + extraSide);
            float right = Mathf.Max(0f, ((screenW - (safeArea.x + safeArea.width)) / screenW) * 100f + extraSide);
            float bottom = Mathf.Max(0f, (safeArea.y / screenH) * 100f + extraBottom);
            float top = Mathf.Max(0f, ((screenH - (safeArea.y + safeArea.height)) / screenH) * 100f + extraTop);

            return (left, right, top, bottom);
        }
    }
}
