using UnityEngine;
using UnityEngine.UIElements;

namespace Arcade.UI
{
    /// <summary>
    /// Applies device safe area insets (notches, dynamic islands, home bars) to UI Toolkit visual trees.
    /// Incorporates extra breathing room margins so UI elements never sit directly against the safe area limits.
    /// Supports automatic adaptation to screen orientation and editor notch simulation.
    /// Uses Unity 6 PanelRenderer component.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class SafeAreaController : MonoBehaviour
    {
        [Header("Target Element")]
        [Tooltip("Optional name of child VisualElement to apply padding to. If empty, automatically targets the root content container (e.g. hud-root or root-container).")]
        [SerializeField] private string targetContainerName = "";

        [Header("Breathing Room (Extra Safe Margins)")]
        [Tooltip("Extra margin percentage added on top of the physical hardware safe area.")]
        [Range(0f, 15f)] [SerializeField] private float extraTopPercent = 3.5f;
        [Range(0f, 15f)] [SerializeField] private float extraBottomPercent = 2.5f;
        [Range(0f, 15f)] [SerializeField] private float extraSidePercent = 2.0f;

        [Header("Editor Simulation")]
        [SerializeField] private bool simulateInEditor = true;
        [SerializeField] private float simulatedTopInsetPixels = 160f;
        [SerializeField] private float simulatedBottomInsetPixels = 80f;
        [SerializeField] private float simulatedSideInsetPixels = 16f;

        private PanelRenderer panelRenderer;
        private VisualElement root;
        private VisualElement targetElement;
        private Rect lastSafeArea = Rect.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;
        private Vector2Int lastResolution = Vector2Int.zero;

        private void Awake()
        {
            panelRenderer = GetComponent<PanelRenderer>();
            if (panelRenderer != null)
            {
                // Toggle enabled to force PanelRenderer tree attachment across scene loads in Unity 6
                panelRenderer.enabled = false;
                panelRenderer.enabled = true;
            }
        }

        private void OnEnable()
        {
            if (panelRenderer == null) panelRenderer = GetComponent<PanelRenderer>();
            if (panelRenderer != null)
            {
                panelRenderer.RegisterUIReloadCallback(OnUIReload);
            }
            ApplySafeArea();
        }

        private void OnDisable()
        {
            if (panelRenderer != null)
            {
                panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            }
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement newRoot, int version)
        {
            root = newRoot;
            ApplySafeArea();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplySafeArea();
        }
#endif

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
            if (panelRenderer == null) panelRenderer = GetComponent<PanelRenderer>();
            if (root == null && panelRenderer != null)
            {
#if UNITY_EDITOR
                var prop = panelRenderer.GetType().GetProperty("rootVisualElement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                root = prop?.GetValue(panelRenderer) as VisualElement;
#endif
            }

            if (root == null) return;

            // Target the actual content container (hud-root / root-container / first child)
            targetElement = !string.IsNullOrEmpty(targetContainerName)
                ? root.Q(targetContainerName) ?? root
                : (root.Q("hud-root") ?? root.Q("root-container") ?? (root.childCount > 0 ? root[0] : root));

            if (targetElement == null) targetElement = root;

            Rect safeArea = Screen.safeArea;
            float screenW = Screen.width > 0 ? Screen.width : 1920f;
            float screenH = Screen.height > 0 ? Screen.height : 1080f;

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

            // Apply padding to target container element (e.g. hud-root) so all child bars and banners shift down below notch
            targetElement.style.paddingLeft = Length.Percent(leftPct);
            targetElement.style.paddingRight = Length.Percent(rightPct);
            targetElement.style.paddingTop = Length.Percent(topPct);
            targetElement.style.paddingBottom = Length.Percent(bottomPct);

            // If targetElement is a child of root, reset root's padding so layout isn't duplicated
            if (targetElement != root)
            {
                root.style.paddingLeft = Length.Percent(0);
                root.style.paddingRight = Length.Percent(0);
                root.style.paddingTop = Length.Percent(0);
                root.style.paddingBottom = Length.Percent(0);
            }
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
