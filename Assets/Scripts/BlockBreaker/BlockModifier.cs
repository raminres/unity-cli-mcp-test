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
        ExtraHeart = 6,
        Shield = 7,
        MultiBall = 8,
        ScoreMultiplier4x = 9,
        ScoreMultiplier5x = 10
    }

    /// <summary>
    /// Metadata helper for special modifier blocks (Multipliers, Expanders, Bombs, Glass, Extra Hearts, Shield, and MultiBall).
    /// </summary>
    public static class BlockModifierExtensions
    {
        public const float PADDLE_EXPANSION_PERCENT = 0.10f; // +10% width
        public const int SCORE_MULTIPLIER_2X = 2;
        public const int SCORE_MULTIPLIER_3X = 3;
        public const int SCORE_MULTIPLIER_4X = 4;
        public const int SCORE_MULTIPLIER_5X = 5;
        public const float DEFAULT_SHIELD_DURATION = 10f;
        public const float DEFAULT_MULTIPLIER_DURATION = 10f;
        public const float DEFAULT_PADDLE_EXPAND_DURATION = 10f;

        public static string GetBadgeText(this BlockSpecialType type) => type switch
        {
            BlockSpecialType.ScoreMultiplier2x => "x2",
            BlockSpecialType.ScoreMultiplier3x => "x3",
            BlockSpecialType.ScoreMultiplier4x => "x4",
            BlockSpecialType.ScoreMultiplier5x => "x5",
            BlockSpecialType.PaddleExpander => "+10%",
            BlockSpecialType.ExtraHeart => "+1",
            _ => string.Empty
        };

        public static Color GetBadgeColor(this BlockSpecialType type) => type switch
        {
            BlockSpecialType.ScoreMultiplier2x => new Color(1.0f, 0.85f, 0.2f),  // Glowing Gold (#ffd700)
            BlockSpecialType.ScoreMultiplier3x => new Color(1.0f, 0.32f, 0.22f), // Fiery Neon Orange/Red (#ff8c00)
            BlockSpecialType.ScoreMultiplier4x => new Color(1.0f, 0.09f, 0.27f), // Vivid Crimson (#ff1744)
            BlockSpecialType.ScoreMultiplier5x => new Color(0.83f, 0.0f, 0.98f),  // Radiant Hyper-Magenta (#d500f9)
            BlockSpecialType.PaddleExpander => new Color(0.2f, 0.9f, 1.0f),      // Neon Cyan
            BlockSpecialType.Bomb => new Color(1.0f, 0.42f, 0.18f),               // Fiery Blaze Orange
            BlockSpecialType.ExtraHeart => new Color(1.0f, 0.23f, 0.34f),        // Radiant Neon Pink/Red (#ff3b56)
            BlockSpecialType.Shield => new Color(0.0f, 0.95f, 1.0f),             // Electric Cyan / Shield Glow (#00f2fe)
            BlockSpecialType.MultiBall => new Color(0.88f, 0.34f, 0.99f),        // Radiant Neon Purple/Magenta (#e056fd)
            _ => Color.white
        };
    }
}
