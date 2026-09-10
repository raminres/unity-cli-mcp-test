using UnityEngine;

namespace Arcade.BlockBreaker
{
    public enum BlockSpecialType
    {
        Normal = 0,
        ScoreMultiplier2x = 1,
        PaddleExpander = 2,
        ScoreMultiplier3x = 3,
        Bomb = 4,
        GlassEnclosed = 5,
        ExtraHeart = 6
    }

    /// <summary>
    /// Metadata helper for special modifier blocks (Multipliers, Expanders, Bombs, Glass, and Extra Hearts).
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
            BlockSpecialType.ExtraHeart => "+1",
            _ => string.Empty
        };

        public static Color GetBadgeColor(this BlockSpecialType type) => type switch
        {
            BlockSpecialType.ScoreMultiplier2x => new Color(1.0f, 0.85f, 0.2f),  // Glowing Gold
            BlockSpecialType.ScoreMultiplier3x => new Color(1.0f, 0.32f, 0.22f), // Fiery Neon Orange/Red
            BlockSpecialType.PaddleExpander => new Color(0.2f, 0.9f, 1.0f),     // Neon Cyan
            BlockSpecialType.Bomb => new Color(1.0f, 0.42f, 0.18f),               // Fiery Blaze Orange
            BlockSpecialType.ExtraHeart => new Color(1.0f, 0.23f, 0.34f),        // Radiant Neon Pink/Red (#ff3b56)
            _ => Color.white
        };
    }
}
