using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Arcade.Core
{
    /// <summary>
    /// Utility for managing sequential build versioning and subfolder paths for BlockBreaker builds.
    /// Supports detecting existing build directories (e.g. "_build1", "build_2", "build3"),
    /// incrementing build numbers and bundle versions, and generating organized build destinations.
    /// </summary>
    public static class BuildVersionUtility
    {
        public const string DefaultBuildsRoot = "Builds/BlockBreakerBuilds";

        private static readonly Regex BuildFolderRegex = new Regex(
            @"^(?:_?build[-_]?|v)(\d+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Parses a sequential build number from folder names like "_build1", "build_2", "build3", "_build_4", "v5".
        /// Returns -1 if no valid build number pattern is found.
        /// </summary>
        public static int ParseBuildNumber(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return -1;

            var match = BuildFolderRegex.Match(folderName.Trim());
            if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
            {
                return number;
            }

            return -1;
        }

        /// <summary>
        /// Scans the specified base directory for existing build folders and returns the highest build number found.
        /// Returns 0 if the directory does not exist or contains no matching build folders.
        /// </summary>
        public static int GetHighestExistingBuildNumber(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory) || !Directory.Exists(baseDirectory))
                return 0;

            int highest = 0;
            var dirs = new DirectoryInfo(baseDirectory).GetDirectories();
            foreach (var dir in dirs)
            {
                int num = ParseBuildNumber(dir.Name);
                if (num > highest)
                {
                    highest = num;
                }
            }
            return highest;
        }

        /// <summary>
        /// Calculates the next sequential build number based on existing folders in baseDirectory
        /// and the current build number setting, guaranteeing strictly increasing numbers.
        /// </summary>
        public static int GetNextBuildNumber(string baseDirectory, int currentBuildNumber = 0)
        {
            int highestFolder = GetHighestExistingBuildNumber(baseDirectory);
            int baseNumber = Mathf.Max(highestFolder, currentBuildNumber);
            return baseNumber + 1;
        }

        /// <summary>
        /// Formats the subfolder name for a given build number (e.g. "build_1", "build_2").
        /// </summary>
        public static string FormatBuildFolderName(int buildNumber)
        {
            return $"build_{buildNumber}";
        }

        /// <summary>
        /// Increments the patch component of a version string (e.g. "0.1.0" -> "0.1.1", "1.2" -> "1.3", "5" -> "6").
        /// </summary>
        public static string IncrementVersionString(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return "0.1.1";

            var parts = version.Trim().Split('.');
            if (parts.Length > 0 && int.TryParse(parts[parts.Length - 1], out int lastPart))
            {
                parts[parts.Length - 1] = (lastPart + 1).ToString();
                return string.Join(".", parts);
            }

            if (int.TryParse(version.Trim(), out int intVersion))
            {
                return (intVersion + 1).ToString();
            }

            return $"{version}.1";
        }

#if UNITY_EDITOR
        /// <summary>
        /// Iterates PlayerSettings.iOS.buildNumber and bundleVersion, ensures the versioned subfolder
        /// exists, saves editor assets, and returns the normalized relative path to the new build folder.
        /// (e.g. "Builds/BlockBreakerBuilds/build_1").
        /// </summary>
        public static string PrepareNextBuild(string baseDirectory = DefaultBuildsRoot, bool incrementPatchVersion = true)
        {
            if (!Directory.Exists(baseDirectory))
            {
                Directory.CreateDirectory(baseDirectory);
            }

            int currentBuildNumber = 0;
            if (int.TryParse(PlayerSettings.iOS.buildNumber, out int parsed))
            {
                currentBuildNumber = parsed;
            }

            int nextBuildNumber = GetNextBuildNumber(baseDirectory, currentBuildNumber);
            string folderName = FormatBuildFolderName(nextBuildNumber);
            string targetPath = Path.Combine(baseDirectory, folderName).Replace('\\', '/');

            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            PlayerSettings.iOS.buildNumber = nextBuildNumber.ToString();

            if (incrementPatchVersion)
            {
                string oldVersion = PlayerSettings.bundleVersion;
                PlayerSettings.bundleVersion = IncrementVersionString(oldVersion);
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"[BuildVersionUtility] Advanced iOS build to #{nextBuildNumber} (bundleVersion: {PlayerSettings.bundleVersion}). Output subfolder: {targetPath}");

            return targetPath;
        }
#endif
    }
}
