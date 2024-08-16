// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using UnityEditor;

namespace Buildalon.Editor.BuildPipeline
{
    /// <summary>
    /// BuildPipeline Editor Preferences.
    /// </summary>
    public static class BuildDeployPreferences
    {
        // Constants
        private const string EDITOR_PREF_BUILD_KEY = "Utilities.BuildDirectory";

        /// <summary>
        /// The Build Directory to build to.
        /// </summary>
        /// <remarks>
        /// This is a root build folder path. Each platform build will be put into a child directory with the name of the current active build target.
        /// </remarks>
        public static string BuildDirectory
        {
            get => $"{EditorPreferences.Get(EDITOR_PREF_BUILD_KEY, "Builds")}/{EditorUserBuildSettings.activeBuildTarget}";
            set => EditorPreferences.Set(EDITOR_PREF_BUILD_KEY, value.Replace($"/{EditorUserBuildSettings.activeBuildTarget}", string.Empty));
        }

        /// <summary>
        /// The absolute path to <see cref="BuildDirectory"/>
        /// </summary>
        public static string AbsoluteBuildDirectory
        {
            get
            {
                var rootBuildDirectory = BuildDirectory;
                var dirCharIndex = rootBuildDirectory.IndexOf($"{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

                if (dirCharIndex != -1)
                {
                    rootBuildDirectory = rootBuildDirectory.Substring(0, dirCharIndex);
                }

                return Path.GetFullPath(Path.Combine(Path.Combine(EditorPreferences.ApplicationDataPath, ".."), rootBuildDirectory));
            }
        }
    }
}
