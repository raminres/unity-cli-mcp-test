using UnityEngine;

namespace Arcade.BlockBreaker
{
    public enum BlockSpecialType
    {
        Normal = 0,
        ScoreMultiplier2x = 1,
        PaddleExpander = 2
    }

    /// <summary>
    /// Metadata helper for special modifier blocks (2x Multipliers and Paddle Expanders).
    /// </summary>
    public static class BlockModifierExtensions
    {
        public const float PADDLE_EXPANSION_PERCENT = 0.10f; // +10% width
        public const int SCORE_MULTIPLIER_2X = 2;

        public static string GetBadgeText(this BlockSpecialType type) => type switch
        {
            BlockSpecialType.ScoreMultiplier2x => "x2",
            BlockSpecialType.PaddleExpander => "+10%",
            _ => string.Empty
        };

        public static Color GetBadgeColor(this BlockSpecialType type) => type switch
        {
            BlockSpecialType.ScoreMultiplier2x => new Color(1.0f, 0.85f, 0.2f), // Glowing Gold
            BlockSpecialType.PaddleExpander => new Color(0.2f, 0.9f, 1.0f),    // Neon Cyan
            _ => Color.white
        };
    }
}
