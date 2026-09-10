using System;
using UnityEngine;

namespace Arcade.Audio
{
    /// <summary>
    /// Central audio manager for arcade sound effects and volume management.
    /// Provides procedural synthesized arcade SFX out-of-the-box with support for Inspector clip overrides.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ArcadeAudioManager : MonoBehaviour
    {
        public static ArcadeAudioManager Instance { get; private set; }

        [Header("Audio Settings")]
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.8f;
        [SerializeField] private bool isMuted = false;

        [Header("Audio Clips (AU_*)")]
        [SerializeField] private AudioClip clipPop;            // AU_Pop.mp3 (paddle and wall bounces)
        [SerializeField] private AudioClip clipBreak;          // AU_Break.mp3 (block break)
        [SerializeField] private AudioClip clipPowerup;        // AU_Powerup.mp3 (power-up collected)
        [SerializeField] private AudioClip clipLevelSuccess;    // AU_Level_Success.mp3 (level clear)
        [SerializeField] private AudioClip clipGameOver;       // AU_Game_Over.mp3 (game over)
        [SerializeField] private AudioClip clipButtonPress;    // AU_Button_Press.mp3 (UI button click)
        [SerializeField] private AudioClip clipGlassBreak;      // AU_Glass_Break.mp3 (reinforced glass shield shatter)
        [SerializeField] private AudioClip clipBombExplosion;   // AU_Bomb_Explosioon.mp3 / AU_Bomb_Explosion.mp3 (bomb detonation)

        [Header("Legacy / Fallback Clip Overrides")]
        [SerializeField] private AudioClip clipPaddleBounce;
        [SerializeField] private AudioClip clipWallBounce;
        [SerializeField] private AudioClip clipBlockHitRed;
        [SerializeField] private AudioClip clipBlockHitGreen;
        [SerializeField] private AudioClip clipBlockHitBlue;
        [SerializeField] private AudioClip clipLifeLost;
        [SerializeField] private AudioClip clipLevelClear;

        private AudioSource audioSource;

        // Public accessors for testing & verification
        public AudioClip ClipPop => clipPop != null ? clipPop : clipPaddleBounce;
        public AudioClip ClipBreak => clipBreak != null ? clipBreak : clipBlockHitRed;
        public AudioClip ClipPowerup => clipPowerup;
        public AudioClip ClipGameOver => clipGameOver;
        public AudioClip ClipLevelSuccess => clipLevelSuccess != null ? clipLevelSuccess : clipLevelClear;
        public AudioClip ClipButtonPress => clipButtonPress;
        public AudioClip ClipGlassBreak => clipGlassBreak;
        public AudioClip ClipBombExplosion => clipBombExplosion;

        public float Volume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat("Arcade_SFX_Volume", sfxVolume);
                PlayerPrefs.Save();
            }
        }

        public bool IsMuted
        {
            get => isMuted;
            set
            {
                isMuted = value;
                PlayerPrefs.SetInt("Arcade_SFX_Muted", isMuted ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public event Action<bool> OnMuteToggled;
        public event Action<float> OnVolumeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D flat stereo sound for crisp arcade feel

            sfxVolume = PlayerPrefs.GetFloat("Arcade_SFX_Volume", 0.8f);
            isMuted = PlayerPrefs.GetInt("Arcade_SFX_Muted", 0) == 1;

            LoadClipsIfEmpty();
        }

        public void LoadClipsIfEmpty()
        {
#if UNITY_EDITOR
            if (clipPop == null) clipPop = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Pop.mp3");
            if (clipBreak == null) clipBreak = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Break.mp3");
            if (clipPowerup == null) clipPowerup = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Powerup.mp3");
            if (clipGameOver == null) clipGameOver = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Game_Over.mp3");
            if (clipLevelSuccess == null) clipLevelSuccess = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Level_Success.mp3");
            if (clipButtonPress == null) clipButtonPress = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Button_Press.mp3");

            if (clipGlassBreak == null)
            {
                clipGlassBreak = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Glass_Break.mp3");
                if (clipGlassBreak == null) clipGlassBreak = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Glass_Break.wav");
            }
            if (clipBombExplosion == null)
            {
                clipBombExplosion = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Bomb_Explosioon.mp3");
                if (clipBombExplosion == null) clipBombExplosion = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Bomb_Explosioon.wav");
                if (clipBombExplosion == null) clipBombExplosion = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Bomb_Explosion.mp3");
                if (clipBombExplosion == null) clipBombExplosion = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Bomb_Explosion.wav");
            }
#endif

            if (clipPaddleBounce == null) clipPaddleBounce = clipPop;
            if (clipWallBounce == null) clipWallBounce = clipPop;
            if (clipBlockHitRed == null) clipBlockHitRed = clipBreak;
            if (clipBlockHitGreen == null) clipBlockHitGreen = clipBreak;
            if (clipBlockHitBlue == null) clipBlockHitBlue = clipBreak;
            if (clipLevelClear == null) clipLevelClear = clipLevelSuccess;

            GenerateProceduralClipsIfEmpty();
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            OnMuteToggled?.Invoke(IsMuted);
        }

        public void SetVolume(float newVolume)
        {
            Volume = newVolume;
            OnVolumeChanged?.Invoke(Volume);
        }

        /// <summary>
        /// Plays Pop sound (AU_Pop.mp3) for ball contacting the paddle or arena boundaries.
        /// </summary>
        public void PlayPop()
        {
            PlaySound(clipPop != null ? clipPop : clipPaddleBounce, 1.0f);
        }

        public void PlayPaddleBounce()
        {
            PlayPop();
        }

        public void PlayWallBounce()
        {
            PlayPop();
        }

        /// <summary>
        /// Plays Break sound (AU_Break.mp3) when the ball hits/destroys a brick.
        /// </summary>
        public void PlayBreak()
        {
            PlaySound(clipBreak != null ? clipBreak : clipBlockHitRed, 1.0f);
        }

        public void PlayBlockHit(int colorTier = 1)
        {
            PlayBreak();
        }

        /// <summary>
        /// Plays Power-up sound (AU_Powerup.mp3) when special modifier block is collected.
        /// </summary>
        public void PlayPowerup()
        {
            if (clipPowerup != null)
            {
                PlaySound(clipPowerup, 1.0f);
            }
        }

        /// <summary>
        /// Plays Glass Break sound (AU_Glass_Break.mp3) when a reinforced glass shell is cracked.
        /// </summary>
        public void PlayGlassBreak()
        {
            if (clipGlassBreak != null)
            {
                PlaySound(clipGlassBreak, 1.0f);
            }
            else
            {
                PlaySound(clipBreak != null ? clipBreak : clipBlockHitBlue, 1.4f);
            }
        }

        /// <summary>
        /// Plays Bomb Explosion sound (AU_Bomb_Explosioon.mp3) when an explosive brick detonates.
        /// </summary>
        public void PlayBombExplosion()
        {
            if (clipBombExplosion != null)
            {
                PlaySound(clipBombExplosion, 1.0f);
            }
            else
            {
                PlaySound(clipBreak != null ? clipBreak : clipBlockHitRed, 0.65f);
            }
        }

        /// <summary>
        /// Plays tactile button click sound (AU_Button_Press.mp3) for all UI interactions.
        /// </summary>
        public void PlayButtonPress()
        {
            if (clipButtonPress != null)
            {
                PlaySound(clipButtonPress, 1.0f);
            }
            else
            {
                PlayPop();
            }
        }

        public void PlayLifeLost()
        {
            PlaySound(clipLifeLost, 1.0f);
        }

        /// <summary>
        /// Plays Level Success victory sound (AU_Level_Success.mp3) upon clearing all bricks.
        /// </summary>
        public void PlayLevelClear()
        {
            PlaySound(clipLevelSuccess != null ? clipLevelSuccess : clipLevelClear, 1.0f);
        }

        /// <summary>
        /// Plays Game Over sound (AU_Game_Over.mp3) when running out of lives.
        /// </summary>
        public void PlayGameOver()
        {
            PlaySound(clipGameOver, 1.0f);
        }

        private void PlaySound(AudioClip clip, float pitch = 1.0f)
        {
            if (isMuted || clip == null || audioSource == null) return;

            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, sfxVolume);
        }

        private void GenerateProceduralClipsIfEmpty()
        {
            const int sampleRate = 44100;

            if (clipPaddleBounce == null)
            {
                // Resonant wooden/plastic paddle pop: 220Hz exponential decay
                clipPaddleBounce = GenerateSynthClip("AU_PaddleBounce", 0.09f, sampleRate, t =>
                {
                    float decay = Mathf.Exp(-t * 45f);
                    float freq = Mathf.Lerp(260f, 130f, t * 12f);
                    return Mathf.Sin(2f * Mathf.PI * freq * t) * decay;
                });
            }

            if (clipWallBounce == null)
            {
                // Crisp click: 600Hz short tick
                clipWallBounce = GenerateSynthClip("AU_WallBounce", 0.05f, sampleRate, t =>
                {
                    float decay = Mathf.Exp(-t * 80f);
                    return Mathf.Sin(2f * Mathf.PI * 650f * t) * decay * 0.7f;
                });
            }

            if (clipBlockHitRed == null)
            {
                // Red block chime: C5 (523Hz) with fast decay
                clipBlockHitRed = GenerateSynthClip("AU_BlockHit_Red", 0.14f, sampleRate, t =>
                {
                    float decay = Mathf.Exp(-t * 22f);
                    float tone1 = Mathf.Sin(2f * Mathf.PI * 523.25f * t);
                    float tone2 = Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * 0.3f;
                    return (tone1 + tone2) * decay * 0.7f;
                });
            }

            if (clipBlockHitGreen == null)
            {
                // Green block chime: E5 (659Hz)
                clipBlockHitGreen = GenerateSynthClip("AU_BlockHit_Green", 0.16f, sampleRate, t =>
                {
                    float decay = Mathf.Exp(-t * 20f);
                    float tone1 = Mathf.Sin(2f * Mathf.PI * 659.25f * t);
                    float tone2 = Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * 0.3f;
                    return (tone1 + tone2) * decay * 0.75f;
                });
            }

            if (clipBlockHitBlue == null)
            {
                // Blue block chime: G5 (784Hz)
                clipBlockHitBlue = GenerateSynthClip("AU_BlockHit_Blue", 0.18f, sampleRate, t =>
                {
                    float decay = Mathf.Exp(-t * 18f);
                    float tone1 = Mathf.Sin(2f * Mathf.PI * 783.99f * t);
                    float tone2 = Mathf.Sin(2f * Mathf.PI * 1567.98f * t) * 0.35f;
                    return (tone1 + tone2) * decay * 0.8f;
                });
            }

            if (clipLifeLost == null)
            {
                // Descending retro buzz
                clipLifeLost = GenerateSynthClip("AU_LifeLost", 0.35f, sampleRate, t =>
                {
                    float decay = Mathf.Clamp01(1f - t / 0.35f);
                    float freq = Mathf.Lerp(300f, 90f, t / 0.35f);
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t) > 0 ? 0.6f : -0.6f; // slight square saturation
                    return wave * decay * 0.6f;
                });
            }

            if (clipLevelClear == null)
            {
                // Ascending bright victory fanfare (C5 -> E5 -> G5 -> C6)
                clipLevelClear = GenerateSynthClip("AU_LevelClear", 0.6f, sampleRate, t =>
                {
                    float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };
                    int noteIndex = Mathf.Clamp((int)(t / 0.15f), 0, 3);
                    float noteTime = t % 0.15f;
                    float decay = Mathf.Exp(-noteTime * 15f);
                    float tone = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t);
                    return tone * decay * 0.8f;
                });
            }

            if (clipGameOver == null)
            {
                // Melancholic descending tone (E4 -> C4 -> A3)
                clipGameOver = GenerateSynthClip("AU_GameOver", 0.7f, sampleRate, t =>
                {
                    float[] notes = { 329.63f, 261.63f, 220.0f };
                    int noteIndex = Mathf.Clamp((int)(t / 0.23f), 0, 2);
                    float noteTime = t % 0.23f;
                    float decay = Mathf.Exp(-noteTime * 8f);
                    float tone = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t);
                    return tone * decay * 0.7f;
                });
            }

            if (clipGlassBreak == null)
            {
                // High-pitched crystal glass shatter ping (1760Hz with fast sparkle harmonics)
                clipGlassBreak = GenerateSynthClip("AU_Glass_Break", 0.22f, sampleRate, t =>
                {
                    float decay = Mathf.Exp(-t * 16f);
                    float tone1 = Mathf.Sin(2f * Mathf.PI * 1760f * t);
                    float tone2 = Mathf.Sin(2f * Mathf.PI * 2640f * t) * 0.4f;
                    float tone3 = Mathf.Sin(2f * Mathf.PI * 3520f * t) * 0.2f;
                    return (tone1 + tone2 + tone3) * decay * 0.85f;
                });
            }

            if (clipBombExplosion == null)
            {
                // Low-frequency explosive impact rumble (120Hz sliding down to 35Hz with saturation)
                clipBombExplosion = GenerateSynthClip("AU_Bomb_Explosion", 0.45f, sampleRate, t =>
                {
                    float decay = Mathf.Clamp01(1f - t / 0.45f);
                    float freq = Mathf.Lerp(120f, 35f, t / 0.45f);
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                    float crunch = Mathf.Clamp(wave * 1.6f, -0.9f, 0.9f);
                    return crunch * decay * 0.9f;
                });
            }
        }

        private static AudioClip GenerateSynthClip(string clipName, float durationSeconds, int sampleRate, Func<float, float> waveFunc)
        {
            int totalSamples = (int)(sampleRate * durationSeconds);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                samples[i] = Mathf.Clamp(waveFunc(t), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
