using UnityEngine;

namespace Arcade.BlockBreaker
{
    public enum BlockSpecialType
    {
        Normal = 0,
        ScoreMultiplier2x = 1,
        PaddleExpander = 2,
        ScoreMultiplier3x = 3
    }

    /// <summary>
    /// Metadata helper for special modifier blocks (2x / 3x Multipliers and Paddle Expanders).
    /// </summary>
    public static class BlockModifierExtensions
    {
        public const float PADDLE_EXPANSION_PERCENT = 0.10f; // +10% width
        public const int SCORE_MULTIPLIER_2X = 2;
        public const int SCORE_MULTIPLIER_3X = 3;

        public static string GetBadgeText(this BlockSpecialType type) => type switch
        {
            BlockSpecialType.ScoreMultiplier2x => "x2",
            BlockSpecialType.ScoreMultiplier3x => "x3",
            BlockSpecialType.PaddleExpander => "+10%",
            _ => string.Empty
        };

        public static Color GetBadgeColor(this BlockSpecialType type) => type switch
        {
            BlockSpecialType.ScoreMultiplier2x => new Color(1.0f, 0.85f, 0.2f),  // Glowing Gold
            BlockSpecialType.ScoreMultiplier3x => new Color(1.0f, 0.32f, 0.22f), // Fiery Neon Orange/Red
            BlockSpecialType.PaddleExpander => new Color(0.2f, 0.9f, 1.0f),     // Neon Cyan
            _ => Color.white
        };
    }
}
