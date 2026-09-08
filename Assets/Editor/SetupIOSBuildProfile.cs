using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace Arcade.Editor
{
    /// <summary>
    /// Automated setup tool that configures the iOS Build Profile, Swift Xcode project settings,
    /// and required gameplay scenes. Can be run via menu or CLI to ensure consistent Xcode builds.
    /// </summary>
    public static class SetupIOSBuildProfile
    {
        public const string ProfilePath = "Assets/Settings/Build Profiles/iOS.asset";
        public const string OutputBuildPath = "/Users/raminrasulzade/Documents/UnityProjects/Builds/BlockBreakerBuilds";
        public const string CompanyName = "RaminRasulzade";
        public const string ProductName = "BlockBreaker";
        public const string BundleIdentifier = "com.RaminRasulzade.BlockBreaker";
        public const string IosMinVersion = "26.0";

        public static readonly string[] RequiredScenes = new[]
        {
            "Assets/Scenes/LV_BlockBreaker_MainMenu.unity",
            "Assets/Scenes/LV_BlockBreaker.unity"
        };

        [MenuItem("Tools/Arcade/Setup iOS Build Profile & Player Settings")]
        public static void ApplySettings()
        {
            // 1. Configure Player Settings
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleIdentifier);
            PlayerSettings.iOS.applicationDisplayName = ProductName;
            PlayerSettings.iOS.targetOSVersionString = IosMinVersion;
            PlayerSettings.xcodeProjectType = XcodeProjectType.Swift;

            // 2. Ensure folder exists
            string folder = Path.GetDirectoryName(ProfilePath);
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            // 3. Load or create iOS BuildProfile
            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath);
            if (profile == null)
            {
                var guid = new GUID("ad48d16a66894befa4d8181998c3cb09");
                var method = typeof(BuildProfile).GetMethod("CreateInstance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
                    null,
                    new[] { typeof(GUID), typeof(string) },
                    null);
                if (method != null)
                {
                    profile = method.Invoke(null, new object[] { guid, ProfilePath }) as BuildProfile;
                }
            }

            if (profile != null)
            {
                profile.overrideGlobalScenes = true;
                profile.scenes = RequiredScenes.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
                EditorUtility.SetDirty(profile);

                // Set as active build profile
                var prop = typeof(EditorUserBuildSettings).GetProperty("activeBuildProfile",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                prop?.SetValue(null, profile);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[SetupIOSBuildProfile] Successfully applied iOS build profile & player settings for {ProductName} ({BundleIdentifier}).");
        }

        [MenuItem("Tools/Arcade/Build Xcode Project")]
        public static void BuildXcode()
        {
            ApplySettings();

            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                Debug.LogWarning("[SetupIOSBuildProfile] Xcode project export requires macOS with Xcode. Skipping build execution on Windows.");
                return;
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                Debug.LogError("[SetupIOSBuildProfile] iOS build support module is not installed in this Unity Editor installation.");
                return;
            }

            string outPath = OutputBuildPath;
            if (!Directory.Exists(outPath))
            {
                Directory.CreateDirectory(outPath);
            }

            var buildOptions = new BuildPlayerOptions
            {
                scenes = RequiredScenes,
                locationPathName = outPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None
            };

            Debug.Log($"[SetupIOSBuildProfile] Starting build to {outPath}...");
            var report = BuildPipeline.BuildPlayer(buildOptions);
            Debug.Log($"[SetupIOSBuildProfile] Build completed with result: {report.summary.result} ({report.summary.totalErrors} errors).");
        }
    }
}
