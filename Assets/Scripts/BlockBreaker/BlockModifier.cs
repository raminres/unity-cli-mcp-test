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
        ScoreMultiplier5x = 10,
        Laser = 11,
        PaddleShortener = 12,
        PaddleSlower = 13,
        BrickFreezer = 14,
        BallSizeDecreaser = 15,
        BallSlower = 16,
        PaddleFreezer = 17
    }

    /// <summary>
    /// Metadata helper for special modifier blocks (Powerups, Powerdowns, Bombs, and Glass).
    /// </summary>
    public static class BlockModifierExtensions
    {
        public const float PADDLE_EXPANSION_PERCENT = 0.10f; // +10% width
        public const float PADDLE_SHORTEN_PERCENT = 0.18f;   // -18% width
        public const int SCORE_MULTIPLIER_2X = 2;
        public const int SCORE_MULTIPLIER_3X = 3;
        public const int SCORE_MULTIPLIER_4X = 4;
        public const int SCORE_MULTIPLIER_5X = 5;
        public const float DEFAULT_SHIELD_DURATION = 10f;
        public const float DEFAULT_MULTIPLIER_DURATION = 10f;
        public const float DEFAULT_PADDLE_EXPAND_DURATION = 10f;
        public const float DEFAULT_LASER_DURATION = 10f;
        public const float DEFAULT_PADDLE_SHORTEN_DURATION = 10f;
        public const float DEFAULT_PADDLE_SLOW_DURATION = 8f;
        public const float DEFAULT_BALL_SIZE_DURATION = 10f;
        public const float DEFAULT_BALL_SIZE_DECREASE_DURATION = 10f;
        public const float DEFAULT_BALL_SLOW_DURATION = 8f;
        public const float DEFAULT_PADDLE_FREEZE_DURATION = 1.2f;
        public const int DEFAULT_FROZEN_BRICK_COUNT = 5;

        public static readonly Color UnifiedPowerupColor = new Color(0.0f, 0.95f, 1.0f);   // Electric Cyan / Emerald glow (#00f2fe)
        public static readonly Color UnifiedPowerdownColor = new Color(1.0f, 0.10f, 0.25f); // Warning Crimson / Hazard (#ff1744)

        public static bool IsPowerdown(this BlockSpecialType type) => type switch
        {
            BlockSpecialType.PaddleShortener => true,
            BlockSpecialType.PaddleSlower => true,
            BlockSpecialType.BrickFreezer => true,
            BlockSpecialType.BallSizeDecreaser => true,
            BlockSpecialType.BallSlower => true,
            BlockSpecialType.PaddleFreezer => true,
            _ => false
        };

        public static bool IsPowerup(this BlockSpecialType type) => type switch
        {
            BlockSpecialType.PaddleExpander => true,
            BlockSpecialType.ExtraHeart => true,
            BlockSpecialType.Shield => true,
            BlockSpecialType.MultiBall => true,
            BlockSpecialType.Laser => true,
            BlockSpecialType.ScoreMultiplier2x => true,
            BlockSpecialType.ScoreMultiplier3x => true,
            BlockSpecialType.ScoreMultiplier4x => true,
            BlockSpecialType.ScoreMultiplier5x => true,
            _ => false
        };

        public static string GetBadgeText(this BlockSpecialType type) => type switch
        {
            BlockSpecialType.ScoreMultiplier2x => "x2",
            BlockSpecialType.ScoreMultiplier3x => "x3",
            BlockSpecialType.ScoreMultiplier4x => "x4",
            BlockSpecialType.ScoreMultiplier5x => "x5",
            BlockSpecialType.PaddleExpander => "+10%",
            BlockSpecialType.ExtraHeart => "+1",
            BlockSpecialType.PaddleShortener => "-18%",
            BlockSpecialType.PaddleSlower => "SLOW",
            BlockSpecialType.BrickFreezer => "ICE",
            BlockSpecialType.BallSizeDecreaser => "TINY",
            BlockSpecialType.BallSlower => "SLOW",
            BlockSpecialType.PaddleFreezer => "LOCK",
            _ => string.Empty
        };

        public static Color GetBadgeColor(this BlockSpecialType type)
        {
            if (type.IsPowerdown()) return UnifiedPowerdownColor;

            return type switch
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
                BlockSpecialType.Laser => new Color(1.0f, 0.16f, 0.28f),             // High-Luminance Neon Ruby (#ff2a47)
                _ => Color.white
            };
        }
    }
}
