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
        [SerializeField] private AudioClip clipShieldDeflect;   // Shield protection intercept sound
        [SerializeField] private AudioClip clipMultiBall;       // Multi-ball spawn sound
        [SerializeField] private AudioClip clipLaserShoot;      // AU_Powerup_Laser.mp3 (paddle laser blast / railgun discharge)

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
        public AudioClip ClipLifeLost => clipLifeLost;
        public AudioClip ClipShieldDeflect => clipShieldDeflect;
        public AudioClip ClipMultiBall => clipMultiBall;
        public AudioClip ClipLaserShoot => clipLaserShoot;
        public void SetClipLaserShootForTesting(AudioClip clip) => clipLaserShoot = clip;

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
                clipBombExplosion = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Bomb_Explosion.mp3");
                if (clipBombExplosion == null) clipBombExplosion = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Bomb_Explosion.wav");
                if (clipBombExplosion == null) clipBombExplosion = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Bomb_Explosioon.mp3");
                if (clipBombExplosion == null) clipBombExplosion = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Bomb_Explosioon.wav");
            }
            if (clipLifeLost == null)
            {
                clipLifeLost = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Life_Lost.mp3");
                if (clipLifeLost == null) clipLifeLost = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Life_Lost.wav");
            }
            if (clipShieldDeflect == null)
            {
                clipShieldDeflect = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Powerup_Shield.mp3");
                if (clipShieldDeflect == null) clipShieldDeflect = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Powerup_Shield.wav");
                if (clipShieldDeflect == null) clipShieldDeflect = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Shield_Deflect.mp3");
            }
            if (clipLaserShoot == null)
            {
                clipLaserShoot = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Powerup_Laser.mp3");
                if (clipLaserShoot == null) clipLaserShoot = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/AU_Powerup_Laser.wav");
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
        /// Plays Break sound (AU_Break.mp3) when the ball hits/destroys a brick,
        /// dynamically pitch-scaling upwards by +1 semitone per consecutive volley combo streak.
        /// </summary>
        public void PlayBreak(int comboStreak = 0)
        {
            float pitch = 1.0f;
            if (comboStreak > 1)
            {
                // Each streak hit scales up by 1 semitone: 2^((streak - 1)/12)
                int semitones = Mathf.Clamp(comboStreak - 1, 0, 9);
                pitch = Mathf.Pow(1.059463f, semitones);
            }
            PlaySound(clipBreak != null ? clipBreak : clipBlockHitRed, pitch);
        }

        public void PlayBlockHit(int colorTier = 1)
        {
            PlayBreak(0);
        }

        /// <summary>
        /// Plays triumphant banking chime when an active unreturned volley streak returns to the paddle.
        /// </summary>
        public void PlayComboBank()
        {
            if (synthComboBankClip == null)
            {
                synthComboBankClip = SynthesizeComboBankChime();
            }
            if (synthComboBankClip != null)
            {
                PlaySound(synthComboBankClip, 1.0f);
            }
            else
            {
                PlayPop();
            }
        }

        /// <summary>
        /// Plays celebratory star-earned chime (index 1, 2, or 3) during the end-of-level scorecard reveal.
        /// </summary>
        public void PlayStarEarned(int starIndex)
        {
            if (synthStarClip == null)
            {
                synthStarClip = SynthesizeStarChime();
            }
            float pitch = 1.0f + Mathf.Clamp(starIndex - 1, 0, 2) * 0.25f;
            if (synthStarClip != null)
            {
                PlaySound(synthStarClip, pitch);
            }
            else
            {
                PlayPowerup();
            }
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

        private AudioClip synthLaserClip;

        /// <summary>
        /// Plays laser blast sound (AU_Powerup_Laser.mp3) when firing paddle laser or railgun beam.
        /// </summary>
        public void PlayLaserShoot()
        {
            if (clipLaserShoot != null)
            {
                PlaySound(clipLaserShoot, 1.0f);
            }
            else
            {
                if (synthLaserClip == null)
                {
                    synthLaserClip = SynthesizeLaserChirp();
                }
                if (synthLaserClip != null)
                {
                    PlaySound(synthLaserClip, 0.9f);
                }
                else
                {
                    PlayPop();
                }
            }
        }

        private AudioClip SynthesizeLaserChirp()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            float startFreq = 1600f;
            float endFreq = 280f;
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, t * t);
                phase += 2f * Mathf.PI * currentFreq / sampleRate;
                float envelope = 1f - t;
                samples[i] = Mathf.Sin(phase) * envelope * 0.45f;
            }

            var clip = AudioClip.Create("SynthLaser", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip synthComboBankClip;
        private AudioClip synthStarClip;

        private AudioClip SynthesizeComboBankChime()
        {
            int sampleRate = 44100;
            float duration = 0.28f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            float[] freqs = new float[] { 523.25f, 659.25f, 783.99f }; // C5 - E5 - G5 major triad arpeggio
            float noteDuration = duration / freqs.Length;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                int noteIndex = Mathf.Clamp(Mathf.FloorToInt(t * freqs.Length), 0, freqs.Length - 1);
                float noteT = (t * freqs.Length) - noteIndex;
                float envelope = Mathf.Exp(-4f * noteT);
                float phase = 2f * Mathf.PI * freqs[noteIndex] * (i / (float)sampleRate);
                samples[i] = Mathf.Sin(phase) * envelope * 0.4f;
            }

            var clip = AudioClip.Create("SynthComboBank", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip SynthesizeStarChime()
        {
            int sampleRate = 44100;
            float duration = 0.38f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            float baseFreq = 880f; // A5 bell chime

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-5f * t);
                // Fundamental + octave + 5th overtone for metallic sparkle
                float s1 = Mathf.Sin(2f * Mathf.PI * baseFreq * t);
                float s2 = 0.5f * Mathf.Sin(2f * Mathf.PI * baseFreq * 2f * t);
                float s3 = 0.25f * Mathf.Sin(2f * Mathf.PI * baseFreq * 3.01f * t);
                samples[i] = (s1 + s2 + s3) * envelope * 0.4f;
            }

            var clip = AudioClip.Create("SynthStar", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
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
        /// Plays Shield Deflect sound when shield catches and resets a falling ball.
        /// </summary>
        public void PlayShieldDeflect()
        {
            if (clipShieldDeflect != null)
            {
                PlaySound(clipShieldDeflect, 1.0f);
            }
            else
            {
                PlaySound(clipPowerup != null ? clipPowerup : clipPop, 1.35f);
            }
        }

        /// <summary>
        /// Plays Multi-Ball spawn sound when extra balls appear.
        /// </summary>
        public void PlayMultiBall()
        {
            if (clipMultiBall != null)
            {
                PlaySound(clipMultiBall, 1.0f);
            }
            else
            {
                PlaySound(clipPowerup != null ? clipPowerup : clipPop, 1.15f);
            }
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

            if (clipShieldDeflect == null)
            {
                // Sci-fi resonating shield deflection pulse (380Hz -> 960Hz upward chirp with exponential decay)
                clipShieldDeflect = GenerateSynthClip("AU_ShieldDeflect", 0.28f, sampleRate, t =>
                {
                    float decay = Mathf.Exp(-t * 12f);
                    float freq = Mathf.Lerp(380f, 960f, t / 0.28f);
                    float tone1 = Mathf.Sin(2f * Mathf.PI * freq * t);
                    float tone2 = Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.4f;
                    return (tone1 + tone2) * decay * 0.85f;
                });
            }

            if (clipMultiBall == null)
            {
                // Rapid dual-harmonic rising chirp (520Hz -> 1200Hz)
                clipMultiBall = GenerateSynthClip("AU_MultiBall", 0.25f, sampleRate, t =>
                {
                    float decay = Mathf.Clamp01(1f - t / 0.25f);
                    float freq = Mathf.Lerp(520f, 1200f, t / 0.25f);
                    float tone1 = Mathf.Sin(2f * Mathf.PI * freq * t);
                    float tone2 = Mathf.Sin(2f * Mathf.PI * (freq * 2f) * t) * 0.35f;
                    return (tone1 + tone2) * decay * 0.8f;
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
