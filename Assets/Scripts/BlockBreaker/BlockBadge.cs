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

        public BlockSpecialType SpecialType => specialType;
        public Vector2 WorldSpaceSize => panelRenderer != null ? panelRenderer.worldSpaceSize : Vector2.zero;

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
                panelRenderer.worldSpaceSize = new Vector2(0.80f, 0.80f);
                panelRenderer.pivot = Pivot.Center;

                // Toggle enable to force clean reinsertion in Unity 6
                panelRenderer.enabled = false;
                panelRenderer.enabled = true;
            }
        }

        public void UpdateUI(VisualElement root)
        {
            OnUIReload(panelRenderer, root, 0);
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            if (root == null) return;
            var icon = root.Q<VisualElement>("badge-icon");
            var label = root.Q<Label>("badge-text");
            var plate = root.Q<VisualElement>("badge-plate");

            if (icon != null)
            {
                icon.RemoveFromClassList("badge-icon-expander");
                icon.RemoveFromClassList("badge-icon-points");
                icon.RemoveFromClassList("badge-icon-points-x2");
                icon.RemoveFromClassList("badge-icon-points-x3");

                if (specialType == BlockSpecialType.PaddleExpander)
                {
                    icon.style.display = DisplayStyle.Flex;
                    icon.AddToClassList("badge-icon-expander");
                }
                else if (specialType == BlockSpecialType.ScoreMultiplier2x)
                {
                    icon.style.display = DisplayStyle.Flex;
                    icon.AddToClassList("badge-icon-points");
                    icon.AddToClassList("badge-icon-points-x2");
                }
                else if (specialType == BlockSpecialType.ScoreMultiplier3x)
                {
                    icon.style.display = DisplayStyle.Flex;
                    icon.AddToClassList("badge-icon-points");
                    icon.AddToClassList("badge-icon-points-x3");
                }
                else
                {
                    icon.style.display = DisplayStyle.None;
                }
            }

            if (label != null)
            {
                label.RemoveFromClassList("badge-text-x2");
                label.RemoveFromClassList("badge-text-x3");
                label.RemoveFromClassList("badge-text-expander");

                if (specialType == BlockSpecialType.ScoreMultiplier2x)
                {
                    label.style.display = DisplayStyle.Flex;
                    label.text = "x2";
                    label.AddToClassList("badge-text-x2");
                }
                else if (specialType == BlockSpecialType.ScoreMultiplier3x)
                {
                    label.style.display = DisplayStyle.Flex;
                    label.text = "x3";
                    label.AddToClassList("badge-text-x3");
                }
                else
                {
                    // For paddle expander and other non-text powerups, hide the text completely
                    label.style.display = DisplayStyle.None;
                    label.AddToClassList("badge-text-expander");
                }
            }

            if (plate != null)
            {
                plate.RemoveFromClassList("badge-plate-x2");
                plate.RemoveFromClassList("badge-plate-x3");
                plate.RemoveFromClassList("badge-plate-expander");
                if (specialType == BlockSpecialType.ScoreMultiplier2x)
                    plate.AddToClassList("badge-plate-x2");
                else if (specialType == BlockSpecialType.ScoreMultiplier3x)
                    plate.AddToClassList("badge-plate-x3");
                else if (specialType == BlockSpecialType.PaddleExpander)
                    plate.AddToClassList("badge-plate-expander");
            }
        }
    }
}
