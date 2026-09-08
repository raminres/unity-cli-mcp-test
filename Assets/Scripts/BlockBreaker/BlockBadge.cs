using UnityEngine;
using UnityEngine.UIElements;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// World-space UI Toolkit badge attached to special modifier blocks (e.g. x2 multiplier, paddle expander).
    /// Uses Unity 6 PanelRenderer in WorldSpace mode.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class BlockBadge : MonoBehaviour
    {
        [SerializeField] private PanelRenderer panelRenderer;
        [SerializeField] private BlockSpecialType specialType = BlockSpecialType.ScoreMultiplier2x;

        private void Awake()
        {
            if (panelRenderer == null) panelRenderer = GetComponent<PanelRenderer>();
            if (panelRenderer != null)
            {
                panelRenderer.RegisterUIReloadCallback(OnUIReload);
            }
        }

        private void OnDestroy()
        {
            if (panelRenderer != null)
            {
                panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            }
        }

        public void Setup(BlockSpecialType type, PanelSettings settings, VisualTreeAsset asset)
        {
            specialType = type;
            if (panelRenderer == null) panelRenderer = GetComponent<PanelRenderer>();
            if (panelRenderer != null)
            {
                panelRenderer.panelSettings = settings;
                panelRenderer.visualTreeAsset = asset;
                panelRenderer.worldSpaceSizeMode = WorldSpaceSizeMode.Fixed;
                panelRenderer.worldSpaceSize = new Vector2(0.85f, 0.85f);
                panelRenderer.pivot = Pivot.Center;

                // Toggle enable to force clean reinsertion in Unity 6
                panelRenderer.enabled = false;
                panelRenderer.enabled = true;
            }
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            if (root == null) return;
            var label = root.Q<Label>("badge-text");
            var plate = root.Q<VisualElement>("badge-plate");

            if (label != null)
            {
                label.text = specialType.GetBadgeText();
                label.RemoveFromClassList("badge-text-x2");
                label.RemoveFromClassList("badge-text-expander");
                if (specialType == BlockSpecialType.ScoreMultiplier2x)
                    label.AddToClassList("badge-text-x2");
                else if (specialType == BlockSpecialType.PaddleExpander)
                    label.AddToClassList("badge-text-expander");
            }

            if (plate != null)
            {
                plate.RemoveFromClassList("badge-plate-x2");
                plate.RemoveFromClassList("badge-plate-expander");
                if (specialType == BlockSpecialType.ScoreMultiplier2x)
                    plate.AddToClassList("badge-plate-x2");
                else if (specialType == BlockSpecialType.PaddleExpander)
                    plate.AddToClassList("badge-plate-expander");
            }
        }
    }
}
