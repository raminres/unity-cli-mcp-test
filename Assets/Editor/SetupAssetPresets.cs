using System;
using System.IO;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace TechArt.Editor
{
    public static class SetupAssetPresets
    {
        private const string PresetsDir = "Assets/Presets";
        private const string TexturesDir = "Assets/Textures";
        private const string ModelsDir = "Assets/Models";
        private const string LogPath = "Assets/Presets/generation_log.txt";

        [InitializeOnLoadMethod]
        public static void AutoExecute()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(Path.Combine(PresetsDir, "PR_BaseColor.preset")))
                {
                    GeneratePresetsAndConfigureManager();
                }
            };
        }

        [MenuItem("Tools/TechArt/Generate Asset Presets")]
        public static void GeneratePresetsAndConfigureManager()
        {
            try
            {
                EnsureDirectories();
                RemoveLegacyPresets();

                GenerateTexturePresets();
                GenerateModelPresets();
                GenerateAudioPresets();

                ConfigurePresetManagerDefaults();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                File.WriteAllText(LogPath, $"Preset generation completed successfully at {DateTime.Now:O}\n");
                Debug.Log("<color=green>[TechArt]</color> All PR_ asset presets and PresetManager defaults generated successfully!");
            }
            catch (Exception ex)
            {
                File.WriteAllText(LogPath, $"Preset generation failed at {DateTime.Now:O}:\n{ex}\n");
                Debug.LogError($"[TechArt] Preset generation failed: {ex}");
            }
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder(PresetsDir))
                AssetDatabase.CreateFolder("Assets", "Presets");
            if (!AssetDatabase.IsValidFolder(TexturesDir))
                AssetDatabase.CreateFolder("Assets", "Textures");
            if (!AssetDatabase.IsValidFolder(ModelsDir))
                AssetDatabase.CreateFolder("Assets", "Models");
        }

        private static void RemoveLegacyPresets()
        {
            string[] legacyPresets = new string[]
            {
                $"{PresetsDir}/TX_BaseColor.preset",
                $"{PresetsDir}/TX_MetallicSmoothness.preset",
                $"{PresetsDir}/TX_Normal.preset",
                $"{PresetsDir}/TX_AO.preset",
                $"{PresetsDir}/TX_Emissive.preset",
                $"{PresetsDir}/SM_StaticMesh.preset",
                $"{PresetsDir}/SK_SkeletalMesh.preset",
                $"{PresetsDir}/AU_Audio.preset"
            };

            foreach (var path in legacyPresets)
            {
                if (File.Exists(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        private static void GenerateTexturePresets()
        {
            string tempTexPath = Path.Combine(PresetsDir, "_temp_gen_tex.png").Replace("\\", "/");
            Texture2D dummyTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            File.WriteAllBytes(tempTexPath, dummyTex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(dummyTex);

            AssetDatabase.ImportAsset(tempTexPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(tempTexPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Failed to get TextureImporter for temporary texture.");

            // 1. PR_BaseColor
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = true;

            var pcBase = importer.GetPlatformTextureSettings("Standalone");
            pcBase.overridden = true;
            pcBase.format = TextureImporterFormat.BC7;
            pcBase.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(pcBase);

            var iosBase = importer.GetPlatformTextureSettings("iPhone");
            iosBase.overridden = true;
            iosBase.format = TextureImporterFormat.ASTC_6x6;
            iosBase.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(iosBase);

            CreateOrReplacePreset(importer, $"{PresetsDir}/PR_BaseColor.preset");

            // 2. PR_MetallicSmoothness
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;

            var pcMS = importer.GetPlatformTextureSettings("Standalone");
            pcMS.overridden = true;
            pcMS.format = TextureImporterFormat.BC7;
            pcMS.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(pcMS);

            var iosMS = importer.GetPlatformTextureSettings("iPhone");
            iosMS.overridden = true;
            iosMS.format = TextureImporterFormat.ASTC_6x6;
            iosMS.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(iosMS);

            CreateOrReplacePreset(importer, $"{PresetsDir}/PR_MetallicSmoothness.preset");

            // 3. PR_Normal
            importer.textureType = TextureImporterType.NormalMap;

            var pcNorm = importer.GetPlatformTextureSettings("Standalone");
            pcNorm.overridden = true;
            pcNorm.format = TextureImporterFormat.BC5;
            pcNorm.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(pcNorm);

            var iosNorm = importer.GetPlatformTextureSettings("iPhone");
            iosNorm.overridden = true;
            iosNorm.format = TextureImporterFormat.ASTC_6x6;
            iosNorm.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(iosNorm);

            CreateOrReplacePreset(importer, $"{PresetsDir}/PR_Normal.preset");

            // 4. PR_AO
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.None;

            var pcAO = importer.GetPlatformTextureSettings("Standalone");
            pcAO.overridden = true;
            pcAO.format = TextureImporterFormat.BC7;
            pcAO.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(pcAO);

            var iosAO = importer.GetPlatformTextureSettings("iPhone");
            iosAO.overridden = true;
            iosAO.format = TextureImporterFormat.ASTC_6x6;
            iosAO.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(iosAO);

            CreateOrReplacePreset(importer, $"{PresetsDir}/PR_AO.preset");

            // 5. PR_Emissive
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;

            var pcEmiss = importer.GetPlatformTextureSettings("Standalone");
            pcEmiss.overridden = true;
            pcEmiss.format = TextureImporterFormat.BC7;
            pcEmiss.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(pcEmiss);

            var iosEmiss = importer.GetPlatformTextureSettings("iPhone");
            iosEmiss.overridden = true;
            iosEmiss.format = TextureImporterFormat.ASTC_6x6;
            iosEmiss.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(iosEmiss);

            CreateOrReplacePreset(importer, $"{PresetsDir}/PR_Emissive.preset");

            AssetDatabase.DeleteAsset(tempTexPath);
        }

        private static void GenerateModelPresets()
        {
            string tempModelPath = Path.Combine(PresetsDir, "_temp_gen_model.obj").Replace("\\", "/");
            string dummyObj = "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n";
            File.WriteAllText(tempModelPath, dummyObj);

            AssetDatabase.ImportAsset(tempModelPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(tempModelPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Failed to get ModelImporter for temporary OBJ.");

            // 1. PR_StaticMesh
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.generateSecondaryUV = true;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.weldVertices = true;

            CreateOrReplacePreset(importer, $"{PresetsDir}/PR_StaticMesh.preset");

            // 2. PR_SkeletalMesh
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.importBlendShapes = true;
            importer.skinWeights = ModelImporterSkinWeights.Standard;
            importer.importVisibility = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.generateSecondaryUV = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;

            CreateOrReplacePreset(importer, $"{PresetsDir}/PR_SkeletalMesh.preset");

            AssetDatabase.DeleteAsset(tempModelPath);
        }

        private static void GenerateAudioPresets()
        {
            string tempAudioPath = Path.Combine(PresetsDir, "_temp_gen_audio.wav").Replace("\\", "/");
            byte[] wavBytes = new byte[] {
                0x52, 0x49, 0x46, 0x46, // RIFF
                0x26, 0x00, 0x00, 0x00, // Chunk size
                0x57, 0x41, 0x56, 0x45, // WAVE
                0x66, 0x6D, 0x74, 0x20, // fmt 
                0x10, 0x00, 0x00, 0x00, // Subchunk1Size
                0x01, 0x00,             // AudioFormat: PCM
                0x01, 0x00,             // Channels: 1
                0x44, 0xAC, 0x00, 0x00, // 44100 Hz
                0x88, 0x58, 0x01, 0x00, // ByteRate
                0x02, 0x00,             // BlockAlign
                0x10, 0x00,             // BitsPerSample
                0x64, 0x61, 0x74, 0x61, // data
                0x02, 0x00, 0x00, 0x00, // Subchunk2Size
                0x00, 0x00
            };
            File.WriteAllBytes(tempAudioPath, wavBytes);

            AssetDatabase.ImportAsset(tempAudioPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(tempAudioPath) as AudioImporter;
            if (importer == null) throw new InvalidOperationException("Failed to get AudioImporter for temporary WAV.");

            var defaultSettings = importer.defaultSampleSettings;
            defaultSettings.loadType = AudioClipLoadType.CompressedInMemory;
            defaultSettings.compressionFormat = AudioCompressionFormat.Vorbis;
            defaultSettings.quality = 0.7f;
            importer.defaultSampleSettings = defaultSettings;

            var pcAudio = new AudioImporterSampleSettings
            {
                loadType = AudioClipLoadType.CompressedInMemory,
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = 0.7f
            };
            importer.SetOverrideSampleSettings(BuildTargetGroup.Standalone, pcAudio);

            CreateOrReplacePreset(importer, $"{PresetsDir}/PR_Audio.preset");

            AssetDatabase.DeleteAsset(tempAudioPath);

            // Inject iPhone / iOS override into PR_Audio.preset YAML
            InjectIosAudioOverride($"{PresetsDir}/PR_Audio.preset");
        }

        private static void InjectIosAudioOverride(string presetPath)
        {
            if (!File.Exists(presetPath)) return;
            string content = File.ReadAllText(presetPath);
            if (content.Contains("m_PlatformSettingOverrides.Array.data[1].first")) return;

            string target = "propertyPath: m_PlatformSettingOverrides.Array.size\n    value: 1";
            string replacement = @"propertyPath: m_PlatformSettingOverrides.Array.size
    value: 2
    objectReference: {fileID: 0}
  - target: {fileID: 0}
    propertyPath: m_PlatformSettingOverrides.Array.data[1].first
    value: iPhone
    objectReference: {fileID: 0}
  - target: {fileID: 0}
    propertyPath: m_PlatformSettingOverrides.Array.data[1].second.loadType
    value: 1
    objectReference: {fileID: 0}
  - target: {fileID: 0}
    propertyPath: m_PlatformSettingOverrides.Array.data[1].second.sampleRateSetting
    value: 0
    objectReference: {fileID: 0}
  - target: {fileID: 0}
    propertyPath: m_PlatformSettingOverrides.Array.data[1].second.sampleRateOverride
    value: 0
    objectReference: {fileID: 0}
  - target: {fileID: 0}
    propertyPath: m_PlatformSettingOverrides.Array.data[1].second.compressionFormat
    value: 3
    objectReference: {fileID: 0}
  - target: {fileID: 0}
    propertyPath: m_PlatformSettingOverrides.Array.data[1].second.quality
    value: 0.7
    objectReference: {fileID: 0}
  - target: {fileID: 0}
    propertyPath: m_PlatformSettingOverrides.Array.data[1].second.conversionMode
    value: 0
    objectReference: {fileID: 0}
  - target: {fileID: 0}
    propertyPath: m_PlatformSettingOverrides.Array.data[1].second.preloadAudioData
    value: 0";

            content = content.Replace(target, replacement);
            File.WriteAllText(presetPath, content);
        }

        private static void ConfigurePresetManagerDefaults()
        {
            // Texture Importer Defaults
            var texNormal = AssetDatabase.LoadAssetAtPath<Preset>($"{PresetsDir}/PR_Normal.preset");
            var texMS = AssetDatabase.LoadAssetAtPath<Preset>($"{PresetsDir}/PR_MetallicSmoothness.preset");
            var texAO = AssetDatabase.LoadAssetAtPath<Preset>($"{PresetsDir}/PR_AO.preset");
            var texEmiss = AssetDatabase.LoadAssetAtPath<Preset>($"{PresetsDir}/PR_Emissive.preset");
            var texBase = AssetDatabase.LoadAssetAtPath<Preset>($"{PresetsDir}/PR_BaseColor.preset");

            var texDefaults = new DefaultPreset[]
            {
                new DefaultPreset("glob:\"*_Normal*\"", texNormal, true),
                new DefaultPreset("glob:\"*_MetallicSmoothness*\"", texMS, true),
                new DefaultPreset("glob:\"*_AO*\"", texAO, true),
                new DefaultPreset("glob:\"*_Emissive*\"", texEmiss, true),
                new DefaultPreset("glob:\"*BaseColor*\"", texBase, true),
                new DefaultPreset("glob:\"*TX_*\"", texBase, true)
            };

            Preset.SetDefaultPresetsForType(texBase.GetPresetType(), texDefaults);

            // Model Importer Defaults
            var smPreset = AssetDatabase.LoadAssetAtPath<Preset>($"{PresetsDir}/PR_StaticMesh.preset");
            var skPreset = AssetDatabase.LoadAssetAtPath<Preset>($"{PresetsDir}/PR_SkeletalMesh.preset");

            var modelDefaults = new DefaultPreset[]
            {
                new DefaultPreset("glob:\"*SM_*\"", smPreset, true),
                new DefaultPreset("glob:\"*SK_*\"", skPreset, true)
            };

            Preset.SetDefaultPresetsForType(smPreset.GetPresetType(), modelDefaults);

            // Audio Importer Defaults
            var auPreset = AssetDatabase.LoadAssetAtPath<Preset>($"{PresetsDir}/PR_Audio.preset");

            var audioDefaults = new DefaultPreset[]
            {
                new DefaultPreset("glob:\"*AU_*\"", auPreset, true)
            };

            Preset.SetDefaultPresetsForType(auPreset.GetPresetType(), audioDefaults);
        }

        private static void CreateOrReplacePreset(UnityEngine.Object target, string path)
        {
            var preset = new Preset(target);
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.CreateAsset(preset, path);
        }
    }
}
