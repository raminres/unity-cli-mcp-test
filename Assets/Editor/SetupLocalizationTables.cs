using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Arcade.Editor
{
    public static class SetupLocalizationTables
    {
        private const string LocalesPath = "Assets/Localization/Locales";
        private const string TablesPath = "Assets/Localization/Tables";
        private const string SettingsPath = "Assets/Localization/LocalizationSettings.asset";
        public const string CollectionName = "ArcadeTable";

        [MenuItem("Tools/Arcade/Setup Localization Tables")]
        public static void Setup()
        {
            // 1. Ensure folders exist
            EnsureFolder("Assets/Localization");
            EnsureFolder(LocalesPath);
            EnsureFolder(TablesPath);

            // 2. Setup or get LocalizationSettings
            var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;

            // 3. Setup Locales (English & Turkish)
            string enPath = $"{LocalesPath}/en.asset";
            var enLocale = AssetDatabase.LoadAssetAtPath<Locale>(enPath);
            if (enLocale == null)
            {
                enLocale = Locale.CreateLocale(SystemLanguage.English);
                AssetDatabase.CreateAsset(enLocale, enPath);
            }

            string trPath = $"{LocalesPath}/tr.asset";
            var trLocale = AssetDatabase.LoadAssetAtPath<Locale>(trPath);
            if (trLocale == null)
            {
                trLocale = Locale.CreateLocale(SystemLanguage.Turkish);
                AssetDatabase.CreateAsset(trLocale, trPath);
            }

            LocalizationEditorSettings.AddLocale(enLocale, false);
            LocalizationEditorSettings.AddLocale(trLocale, false);

            LocalizationSettings.ProjectLocale = enLocale;
            LocalizationSettings.SelectedLocale = enLocale;

            // 4. Create or get StringTableCollection
            var collection = LocalizationEditorSettings.GetStringTableCollection(CollectionName);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(
                    CollectionName,
                    TablesPath,
                    new List<Locale> { enLocale, trLocale }
                );
            }

            var enTable = collection.GetTable(enLocale.Identifier) as StringTable;
            var trTable = collection.GetTable(trLocale.Identifier) as StringTable;

            if (enTable == null) enTable = collection.AddNewTable(enLocale.Identifier) as StringTable;
            if (trTable == null) trTable = collection.AddNewTable(trLocale.Identifier) as StringTable;

            // 5. Populate keys and translations
            var entries = GetDefaultTranslations();
            foreach (var item in entries)
            {
                var enEntry = enTable.GetEntry(item.key);
                if (enEntry == null)
                {
                    enTable.AddEntry(item.key, item.en);
                }
                else if (string.IsNullOrEmpty(enEntry.Value))
                {
                    enEntry.Value = item.en;
                }

                var trEntry = trTable.GetEntry(item.key);
                if (trEntry == null)
                {
                    trTable.AddEntry(item.key, item.tr);
                }
                else if (string.IsNullOrEmpty(trEntry.Value))
                {
                    trEntry.Value = item.tr;
                }
            }

            // 6. Save Assets
            EditorUtility.SetDirty(enTable);
            EditorUtility.SetDirty(trTable);
            if (collection.SharedData != null) EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(settings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SetupLocalizationTables] Successfully configured '{CollectionName}' table with {entries.Count} keys for English and Turkish!");
        }

        private static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        public static List<(string key, string en, string tr)> GetDefaultTranslations()
        {
            return new List<(string, string, string)>
            {
                // Main Menu
                ("menu_start_new", "START NEW GAME", "YENİ OYUN BAŞLAT"),
                ("menu_select_level", "SELECT LEVEL", "BÖLÜM SEÇ"),
                ("menu_continue", "CONTINUE", "DEVAM ET"),
                ("menu_high_scores", "HIGH SCORES", "EN YÜKSEK SKORLAR"),
                ("menu_how_to_play", "HOW TO PLAY", "NASIL OYNANIR"),
                ("menu_options", "OPTIONS", "AYARLAR"),
                ("menu_credits", "CREDITS", "YAPIMCILAR"),
                ("menu_subtitle", "3D ARCADE RETRO-MODERN", "3D RETRO-MODERN ATARİ"),

                // Level Select
                ("level_select_title", "SELECT LEVEL", "BÖLÜM SEÇ"),
                ("level_select_subtitle", "Choose your next challenge", "Meydan okumanı seç"),
                ("level_play_btn", "PLAY LEVEL", "BÖLÜMÜ OYNA"),
                ("level_locked", "LOCKED", "KİLİTLİ"),
                ("level_best_time", "BEST TIME", "EN İYİ SÜRE"),
                ("level_difficulty", "DIFFICULTY", "ZORLUK"),
                ("diff_easy", "EASY", "KOLAY"),
                ("diff_medium", "MEDIUM", "ORTA"),
                ("diff_hard", "HARD", "ZOR"),

                // General Modals & Buttons
                ("btn_back", "BACK", "GERİ"),
                ("btn_resume", "RESUME", "DEVAM ET"),
                ("btn_restart", "RESTART LEVEL", "YENİDEN BAŞLAT"),
                ("btn_main_menu", "MAIN MENU", "ANA MENÜ"),
                ("btn_next_level", "NEXT LEVEL", "SONRAKİ BÖLÜM"),
                ("btn_reset_scores", "RESET SCORES", "SKORLARI SIFIRLA"),
                ("btn_apply_restart", "APPLY & RESTART", "UYGULA VE YENİDEN BAŞLAT"),

                // Options / Settings
                ("settings_title", "SETTINGS", "AYARLAR"),
                ("settings_language", "Language", "Dil"),
                ("settings_sfx_vol", "SFX Volume", "Ses Efektleri"),
                ("settings_sfx_mute", "Mute SFX", "Sesi Kapat"),
                ("settings_music_vol", "Music Volume", "Müzik Sesi"),
                ("settings_music_mute", "Mute Music", "Müziği Kapat"),
                ("settings_target_fps", "Target Frame Rate", "Hedef Kare Hızı"),

                // HUD & Gameplay
                ("hud_score", "SCORE", "SKOR"),
                ("hud_tap_launch", "TAP TO LAUNCH", "FIRLATMAK İÇİN DOKUN"),
                ("hud_paused", "PAUSED", "DURAKLATILDI"),
                ("hud_level_clear", "LEVEL CLEAR!", "BÖLÜM GEÇİLDİ!"),
                ("hud_game_over", "GAME OVER", "OYUN BİTTİ"),

                // Credits
                ("credits_title", "CREDITS", "YAPIMCILAR"),
                ("credits_dev_by", "Developed by Ramin Rasulzade", "Geliştirici: Ramin Rasulzade"),
                ("credits_footnote", "Unity 6000.6 (URP) • Apple Metal • WebGPU", "Unity 6000.6 (URP) • Apple Metal • WebGPU"),

                // How To Play
                ("guide_title", "HOW TO PLAY", "NASIL OYNANIR"),
                ("guide_h1", "1. CONTROLS & LAUNCH", "1. KONTROLLER VE FIRLATMA"),
                ("guide_p1", "• Drag touch or mouse horizontally to guide the paddle.\n• Keyboard: A/D or Left/Right arrows.\n• Tap screen or press Space to launch.", "• Raketi yönlendirmek için parmağınızı veya fareyi yatay kaydırın.\n• Klavye: A/D veya Sol/Sağ oklar.\n• Topu fırlatmak için ekrana dokunun veya Boşluk tuşuna basın."),
                ("guide_h2", "2. DYNAMIC PADDLE DEFLECTION", "2. DİNAMİK RAKET SEKMESİ"),
                ("guide_p2", "• Rebounds calculate angle from hit offset! Center bounces vertical (90°); outer edges yield sharp angles (30°) for surgical precision.", "• Sekme açısı temas noktasına göre hesaplanır! Merkez dikey (90°), kenarlar keskin açılar (30°) üretir."),
                ("guide_h3", "3. SCORING & GLASS BRICKS", "3. PUANLAMA VE CAM BLOKLAR"),
                ("guide_p3", "• Red = 10 pts • Green = 20 pts • Blue = 30 pts.\n• Glass Bricks require 2 hits: Hit 1 shatters shell; Hit 2 breaks brick for 2x points!", "• Kırmızı = 10 puan • Yeşil = 20 puan • Mavi = 30 puan.\n• Cam Bloklar 2 vuruş gerektirir: 1. vuruş kabuğu kırar, 2. vuruş bloğu kırıp 2 kat puan verir!"),
                ("guide_h4", "4. POWERUPS & HAZARDS", "4. GÜÇLENDİRMELER VE TEHLİKELER"),
                ("guide_p4", "• Expander: Widens paddle.\n• Multipliers: 2X-5X bonus points.\n• Shield: Defensive kinetic barrier.\n• Multi-Ball: 3 balls active!\n• Laser Blaster: Twin cannons pierce brick rows!\n• Bombs: Radial detonation explodes neighbors!", "• Genişletici: Raketi büyütür.\n• Çarpanlar: 2X-5X bonus puan.\n• Kalkan: Koruyucu kinetik bariyer.\n• Çoklu Top: 3 top aynı anda sahada!\n• Lazer: Blok sıralarını delen ikiz toplar!\n• Bomba: Etrafındaki komşu blokları patlatır!")
            };
        }
    }
}
