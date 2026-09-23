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
        [Range(0f, 15f)] [SerializeField] private float extraTopPercent = 4.5f;
        [Range(0f, 15f)] [SerializeField] private float extraBottomPercent = 2.5f;
        [Range(0f, 15f)] [SerializeField] private float extraSidePercent = 2.0f;

        public float ExtraTopPercent => extraTopPercent;
        public float ExtraBottomPercent => extraBottomPercent;
        public float ExtraSidePercent => extraSidePercent;

        [Header("Editor Simulation")]
        [Tooltip("When true, simulates notches in standard Game View if no hardware/Device Simulator safe area exists.")]
        [SerializeField] private bool simulateInEditor = true;
        [Tooltip("Force mock simulation even if Device Simulator provides hardware safe area.")]
        [SerializeField] private bool forceEditorSimulation = false;
        [SerializeField] private float simulatedTopInsetPixels = 177f;
        [SerializeField] private float simulatedBottomInsetPixels = 102f;
        [SerializeField] private float simulatedSideInsetPixels = 0f;

        public bool ForceEditorSimulation { get => forceEditorSimulation; set => forceEditorSimulation = value; }

        private PanelRenderer panelRenderer;
        private VisualElement root;
        private VisualElement targetElement;
        private Rect lastSafeArea = Rect.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;
        private Vector2Int lastResolution = Vector2Int.zero;

        private void Awake()
        {
            panelRenderer = GetComponent<PanelRenderer>();
        }

        private void Start()
        {
            ApplySafeArea();
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
                var prop = panelRenderer.GetType().GetProperty("rootVisualElement", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                root = prop?.GetValue(panelRenderer) as VisualElement;

                if (root == null)
                {
                    var panelProp = panelRenderer.GetType().GetProperty("containerPanel", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var panel = panelProp?.GetValue(panelRenderer) as IPanel;
                    if (panel != null) root = panel.visualTree;
                }
            }

            if (root == null) return;

            // Target the actual content container (prefer dedicated safe-area-content or main-menu-content)
            // to allow full-screen root containers and modal backdrops to remain edge-to-edge
            targetElement = !string.IsNullOrEmpty(targetContainerName)
                ? root.Q(targetContainerName) ?? root
                : (root.Q("safe-area-content") ?? root.Q("main-menu-content") ?? root.Q("top-bar") ?? root.Q("hud-root") ?? root.Q("root-container") ?? (root.childCount > 0 ? root[0] : root));

            if (targetElement == null) targetElement = root;

            Rect safeArea = Screen.safeArea;
            float screenW = Screen.width > 0 ? Screen.width : 1920f;
            float screenH = Screen.height > 0 ? Screen.height : 1080f;

            // Detect whether Unity (Device Simulator or physical device) provides a genuine hardware safe area
            bool hasGenuineSafeArea = safeArea.x > 0.5f || safeArea.y > 0.5f ||
                                      (screenW - (safeArea.x + safeArea.width)) > 0.5f ||
                                      (screenH - (safeArea.y + safeArea.height)) > 0.5f;

#if UNITY_EDITOR
            if (forceEditorSimulation || (!hasGenuineSafeArea && simulateInEditor))
            {
                safeArea = new Rect(
                    simulatedSideInsetPixels,
                    simulatedBottomInsetPixels,
                    screenW - (simulatedSideInsetPixels * 2f),
                    screenH - (simulatedTopInsetPixels + simulatedBottomInsetPixels)
                );
            }
#endif

            lastSafeArea = safeArea;
            lastOrientation = Screen.orientation;
            lastResolution = new Vector2Int(Screen.width, Screen.height);

            if (screenW <= 0 || screenH <= 0) return;

            // Calculate insets using Yoga width-relative CSS percentages:
            // In CSS / Yoga Flexbox, all padding percentages (including top and bottom) are calculated relative to the containing element's WIDTH.
            // Dividing by screenW guarantees the rendered pixel inset is mathematically 100% exact.
            var (leftPct, rightPct, topPct, bottomPct) = CalculateYogaInsets(safeArea, screenW, screenH, extraSidePercent, extraTopPercent, extraBottomPercent);

            // Apply padding to target container element (e.g. safe-area-content)
            targetElement.style.paddingLeft = Length.Percent(leftPct);
            targetElement.style.paddingRight = Length.Percent(rightPct);
            targetElement.style.paddingTop = Length.Percent(topPct);
            targetElement.style.paddingBottom = Length.Percent(bottomPct);

            // If targetElement is a child of root, reset root's padding so layout isn't duplicated
            // and root remains 100% full bleed for edge-to-edge modal backdrops
            if (targetElement != root)
            {
                root.style.paddingLeft = Length.Percent(0);
                root.style.paddingRight = Length.Percent(0);
                root.style.paddingTop = Length.Percent(0);
                root.style.paddingBottom = Length.Percent(0);
            }

            var fullBleedRoot = root.Q("hud-root") ?? root.Q("root-container");
            if (fullBleedRoot != null && targetElement != fullBleedRoot)
            {
                fullBleedRoot.style.paddingLeft = Length.Percent(0);
                fullBleedRoot.style.paddingRight = Length.Percent(0);
                fullBleedRoot.style.paddingTop = Length.Percent(0);
                fullBleedRoot.style.paddingBottom = Length.Percent(0);
            }
        }

        /// <summary>
        /// Pure helper to compute safe area percentage insets from screen rects with breathing margins.
        /// Maintained for unit test backward compatibility.
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

        /// <summary>
        /// Computes padding percentages relative to width so that Yoga resolves the exact target pixel insets vertically and horizontally.
        /// </summary>
        public static (float left, float right, float top, float bottom) CalculateYogaInsets(
            Rect safeArea, float screenW, float screenH,
            float extraSide = 0f, float extraTop = 0f, float extraBottom = 0f)
        {
            if (screenW <= 0 || screenH <= 0) return (0, 0, 0, 0);

            float topInsetPixels = Mathf.Max(0f, screenH - (safeArea.y + safeArea.height));
            float bottomInsetPixels = Mathf.Max(0f, safeArea.y);
            float leftInsetPixels = Mathf.Max(0f, safeArea.x);
            float rightInsetPixels = Mathf.Max(0f, screenW - (safeArea.x + safeArea.width));

            // In Yoga / CSS, padding-top: % is resolved against containing block's WIDTH.
            // Therefore, (topInsetPixels / screenW) * 100f ensures Yoga renders exactly topInsetPixels.
            float top = (topInsetPixels / screenW) * 100f + extraTop;
            float bottom = (bottomInsetPixels / screenW) * 100f + extraBottom;
            float left = (leftInsetPixels / screenW) * 100f + extraSide;
            float right = (rightInsetPixels / screenW) * 100f + extraSide;

            return (left, right, top, bottom);
        }
    }
}
