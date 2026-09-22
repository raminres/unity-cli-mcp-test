using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcade.Core
{
    public enum GameLanguage
    {
        English = 0,
        Turkish = 1
    }

    /// <summary>
    /// Centralized lightweight localization manager providing English and Turkish translations
    /// for all HUD elements, main menu navigation, modals, and gameplay banners.
    /// Persists language preference in PlayerPrefs and triggers OnLanguageChanged.
    /// </summary>
    public static class LocalizationManager
    {
        private const string PrefKey = "Arcade_Language";
        private static GameLanguage currentLanguage = GameLanguage.English;
        private static bool isInitialized = false;

        public static event Action OnLanguageChanged;

        public static GameLanguage CurrentLanguage
        {
            get
            {
                EnsureInitialized();
                return currentLanguage;
            }
            set
            {
                SetLanguage(value);
            }
        }

        private static readonly Dictionary<string, (string en, string tr)> Strings = new Dictionary<string, (string, string)>
        {
            // Main Menu
            { "menu_start_new", ("START NEW GAME", "YENİ OYUN BAŞLAT") },
            { "menu_select_level", ("SELECT LEVEL", "BÖLÜM SEÇ") },
            { "menu_continue", ("CONTINUE", "DEVAM ET") },
            { "menu_high_scores", ("HIGH SCORES", "EN YÜKSEK SKORLAR") },
            { "menu_how_to_play", ("HOW TO PLAY", "NASIL OYNANIR") },
            { "menu_options", ("OPTIONS", "AYARLAR") },
            { "menu_credits", ("CREDITS", "YAPIMCILAR") },
            { "menu_subtitle", ("3D ARCADE RETRO-MODERN", "3D RETRO-MODERN ATARİ") },

            // Level Select
            { "level_select_title", ("SELECT LEVEL", "BÖLÜM SEÇ") },
            { "level_select_subtitle", ("Choose your next challenge", "Meydan okumanı seç") },
            { "level_play_btn", ("PLAY LEVEL", "BÖLÜMÜ OYNA") },
            { "level_locked", ("LOCKED", "KİLİTLİ") },
            { "level_best_time", ("BEST TIME", "EN İYİ SÜRE") },
            { "level_difficulty", ("DIFFICULTY", "ZORLUK") },
            { "diff_easy", ("EASY", "KOLAY") },
            { "diff_medium", ("MEDIUM", "ORTA") },
            { "diff_hard", ("HARD", "ZOR") },

            // General Modals & Buttons
            { "btn_back", ("BACK", "GERİ") },
            { "btn_resume", ("RESUME", "DEVAM ET") },
            { "btn_restart", ("RESTART LEVEL", "YENİDEN BAŞLAT") },
            { "btn_main_menu", ("MAIN MENU", "ANA MENÜ") },
            { "btn_next_level", ("NEXT LEVEL", "SONRAKİ BÖLÜM") },
            { "btn_reset_scores", ("RESET SCORES", "SKORLARI SIFIRLA") },
            { "btn_apply_restart", ("APPLY & RESTART", "UYGULA VE YENİDEN BAŞLAT") },

            // Options / Settings
            { "settings_title", ("SETTINGS", "AYARLAR") },
            { "settings_language", ("Language", "Dil") },
            { "settings_sfx_vol", ("SFX Volume", "Ses Efektleri") },
            { "settings_sfx_mute", ("Mute SFX", "Sesi Kapat") },
            { "settings_music_vol", ("Music Volume", "Müzik Sesi") },
            { "settings_music_mute", ("Mute Music", "Müziği Kapat") },
            { "settings_target_fps", ("Target Frame Rate", "Hedef Kare Hızı") },

            // HUD & Gameplay
            { "hud_score", ("SCORE", "SKOR") },
            { "hud_tap_launch", ("TAP TO LAUNCH", "FIRLATMAK İÇİN DOKUN") },
            { "hud_paused", ("PAUSED", "DURAKLATILDI") },
            { "hud_level_clear", ("LEVEL CLEAR!", "BÖLÜM GEÇİLDİ!") },
            { "hud_game_over", ("GAME OVER", "OYUN BİTTİ") },

            // Credits
            { "credits_title", ("CREDITS", "YAPIMCILAR") },
            { "credits_dev_by", ("Developed by Ramin Rasulzade", "Geliştirici: Ramin Rasulzade") },
            { "credits_footnote", ("Unity 6000.6 (URP) • Apple Metal • WebGPU", "Unity 6000.6 (URP) • Apple Metal • WebGPU") },

            // How To Play
            { "guide_title", ("HOW TO PLAY", "NASIL OYNANIR") },
            { "guide_h1", ("1. CONTROLS & LAUNCH", "1. KONTROLLER VE FIRLATMA") },
            { "guide_p1", ("• Drag touch or mouse horizontally to guide the paddle.\n• Keyboard: A/D or Left/Right arrows.\n• Tap screen or press Space to launch.", "• Raketi yönlendirmek için parmağınızı veya fareyi yatay kaydırın.\n• Klavye: A/D veya Sol/Sağ oklar.\n• Topu fırlatmak için ekrana dokunun veya Boşluk tuşuna basın.") },
            { "guide_h2", ("2. DYNAMIC PADDLE DEFLECTION", "2. DİNAMİK RAKET SEKMESİ") },
            { "guide_p2", ("• Rebounds calculate angle from hit offset! Center bounces vertical (90°); outer edges yield sharp angles (30°) for surgical precision.", "• Sekme açısı temas noktasına göre hesaplanır! Merkez dikey (90°), kenarlar keskin açılar (30°) üretir.") },
            { "guide_h3", ("3. SCORING & GLASS BRICKS", "3. PUANLAMA VE CAM BLOKLAR") },
            { "guide_p3", ("• Red = 10 pts • Green = 20 pts • Blue = 30 pts.\n• Glass Bricks require 2 hits: Hit 1 shatters shell; Hit 2 breaks brick for 2x points!", "• Kırmızı = 10 puan • Yeşil = 20 puan • Mavi = 30 puan.\n• Cam Bloklar 2 vuruş gerektirir: 1. vuruş kabuğu kırar, 2. vuruş bloğu kırıp 2 kat puan verir!") },
            { "guide_h4", ("4. POWERUPS & HAZARDS", "4. GÜÇLENDİRMELER VE TEHLİKELER") },
            { "guide_p4", ("• Expander: Widens paddle.\n• Multipliers: 2X-5X bonus points.\n• Shield: Defensive kinetic barrier.\n• Multi-Ball: 3 balls active!\n• Laser Blaster: Twin cannons pierce brick rows!\n• Bombs: Radial detonation explodes neighbors!", "• Genişletici: Raketi büyütür.\n• Çarpanlar: 2X-5X bonus puan.\n• Kalkan: Koruyucu kinetik bariyer.\n• Çoklu Top: 3 top aynı anda sahada!\n• Lazer: Blok sıralarını delen ikiz toplar!\n• Bomba: Etrafındaki komşu blokları patlatır!") }
        };

        private static void EnsureInitialized()
        {
            if (isInitialized) return;
            string savedLang = PlayerPrefs.GetString(PrefKey, "English");
            if (Enum.TryParse<GameLanguage>(savedLang, true, out var parsed))
            {
                currentLanguage = parsed;
            }
            else
            {
                currentLanguage = Application.systemLanguage == SystemLanguage.Turkish 
                    ? GameLanguage.Turkish 
                    : GameLanguage.English;
            }
            isInitialized = true;
        }

        public static void SetLanguage(GameLanguage language)
        {
            EnsureInitialized();
            if (currentLanguage == language) return;
            currentLanguage = language;
            PlayerPrefs.SetString(PrefKey, language.ToString());
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }

        public static string Get(string key, string fallback = "")
        {
            EnsureInitialized();
            if (Strings.TryGetValue(key, out var pair))
            {
                return currentLanguage == GameLanguage.Turkish ? pair.tr : pair.en;
            }
            return string.IsNullOrEmpty(fallback) ? key : fallback;
        }
    }
}
