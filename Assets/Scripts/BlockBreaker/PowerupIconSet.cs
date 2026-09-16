using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// ScriptableObject defining the sprite icon set for all powerups and modifier blocks.
    /// Exposed in the Unity Inspector UI so developers can visually assign,
    /// inspect, and verify all icons directly in Unity.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_PowerupIcons", menuName = "Arcade/BlockBreaker/Powerup Icon Set")]
    public class PowerupIconSet : ScriptableObject
    {
        [Header("Powerup & Modifier Sprites (Assigned in Unity UI)")]
        [SerializeField] private Sprite expanderSprite;
        [SerializeField] private Sprite bombSprite;
        [SerializeField] private Sprite extraHeartSprite;
        [SerializeField] private Sprite shieldSprite;
        [SerializeField] private Sprite multiBallSprite;
        [SerializeField] private Sprite multiplierSprite;
        [SerializeField] private Sprite laserSprite;

        [Header("Powerdown Sprites (Assigned in Unity UI)")]
        [SerializeField] private Sprite paddleShortenerSprite;
        [SerializeField] private Sprite paddleSlowerSprite;
        [SerializeField] private Sprite brickFreezerSprite;
        [SerializeField] private Sprite ballSizeDecreaserSprite;
        [SerializeField] private Sprite ballSlowerSprite;
        [SerializeField] private Sprite paddleFreezerSprite;

        public Sprite ExpanderSprite => expanderSprite;
        public Sprite BombSprite => bombSprite;
        public Sprite ExtraHeartSprite => extraHeartSprite;
        public Sprite ShieldSprite => shieldSprite;
        public Sprite MultiBallSprite => multiBallSprite;
        public Sprite MultiplierSprite => multiplierSprite;
        public Sprite LaserSprite => laserSprite;

        public Sprite PaddleShortenerSprite => paddleShortenerSprite;
        public Sprite PaddleSlowerSprite => paddleSlowerSprite;
        public Sprite BrickFreezerSprite => brickFreezerSprite;
        public Sprite BallSizeDecreaserSprite => ballSizeDecreaserSprite;
        public Sprite BallSlowerSprite => ballSlowerSprite;
        public Sprite PaddleFreezerSprite => paddleFreezerSprite;

        public void SetSprites(Sprite expander, Sprite bomb, Sprite extraHeart, Sprite shield, Sprite multiBall, Sprite multiplier, Sprite laser)
        {
            expanderSprite = expander;
            bombSprite = bomb;
            extraHeartSprite = extraHeart;
            shieldSprite = shield;
            multiBallSprite = multiBall;
            multiplierSprite = multiplier;
            laserSprite = laser;
        }

        public void SetPowerdownSprites(Sprite shortener, Sprite slower, Sprite brickFreezer, Sprite sizeDecreaser, Sprite ballSlower, Sprite paddleFreezer)
        {
            paddleShortenerSprite = shortener;
            paddleSlowerSprite = slower;
            brickFreezerSprite = brickFreezer;
            ballSizeDecreaserSprite = sizeDecreaser;
            ballSlowerSprite = ballSlower;
            paddleFreezerSprite = paddleFreezer;
        }

        public Sprite GetSprite(BlockSpecialType type)
        {
            switch (type)
            {
                case BlockSpecialType.PaddleExpander:
                    return expanderSprite;
                case BlockSpecialType.Bomb:
                    return bombSprite;
                case BlockSpecialType.ExtraHeart:
                    return extraHeartSprite;
                case BlockSpecialType.Shield:
                    return shieldSprite;
                case BlockSpecialType.MultiBall:
                    return multiBallSprite;
                case BlockSpecialType.ScoreMultiplier2x:
                case BlockSpecialType.ScoreMultiplier3x:
                case BlockSpecialType.ScoreMultiplier4x:
                case BlockSpecialType.ScoreMultiplier5x:
                    return multiplierSprite;
                case BlockSpecialType.Laser:
                    return laserSprite;
                case BlockSpecialType.PaddleShortener:
                    return paddleShortenerSprite;
                case BlockSpecialType.PaddleSlower:
                    return paddleSlowerSprite;
                case BlockSpecialType.BrickFreezer:
                    return brickFreezerSprite;
                case BlockSpecialType.BallSizeDecreaser:
                    return ballSizeDecreaserSprite;
                case BlockSpecialType.BallSlower:
                    return ballSlowerSprite;
                case BlockSpecialType.PaddleFreezer:
                    return paddleFreezerSprite;
                default:
                    return null;
            }
        }
    }
}
