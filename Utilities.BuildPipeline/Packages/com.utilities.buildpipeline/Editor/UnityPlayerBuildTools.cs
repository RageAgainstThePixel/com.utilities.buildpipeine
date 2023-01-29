// Licensed under the MIT License. See LICENSE in the project root for license information.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Utilities.Editor.BuildPipeline.Logging;
using Debug = UnityEngine.Debug;

namespace Utilities.Editor.BuildPipeline
{
    /// <summary>
    /// Cross platform player build tools
    /// </summary>
    public class UnityPlayerBuildTools : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        // Build configurations. Exactly one of these should be defined for any given build.
        public const string BuildSymbolDebug = "debug";
        public const string BuildSymbolRelease = "release";
        public const string BuildSymbolMaster = "master";

        private static IBuildInfo buildInfo;

        /// <summary>
        /// Gets or creates an instance of the <see cref="IBuildInfo"/> to use when building.
        /// </summary>
        /// <returns>A new instance of <see cref="IBuildInfo"/>.</returns>
        public static IBuildInfo BuildInfo
        {
            get
            {
                BuildInfo buildInfoInstance;
                var currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;

                var isBuildInfoNull = buildInfo == null;

                if (isBuildInfoNull ||
                    buildInfo.BuildTarget != currentBuildTarget)
                {
                    switch (currentBuildTarget)
                    {
                        // TODO: Add additional platform specific build info classes
                        //case BuildTarget.StandaloneOSX:
                        //    break;
                        //case BuildTarget.StandaloneWindows:
                        //    break;
                        //case BuildTarget.iOS:
                        //    break;
                        case BuildTarget.Android:
                            buildInfoInstance = new AndroidBuildInfo();
                            break;
                        //case BuildTarget.StandaloneWindows64:
                        //    break;
                        //case BuildTarget.WebGL:
                        //    break;
                        //case BuildTarget.WSAPlayer:
                        //    break;
                        //case BuildTarget.StandaloneLinux64:
                        //    break;
                        //case BuildTarget.PS4:
                        //    break;
                        //case BuildTarget.XboxOne:
                        //    break;
                        //case BuildTarget.tvOS:
                        //    break;
                        //case BuildTarget.Switch:
                        //    break;
                        //case BuildTarget.Lumin:
                        //    break;
                        //case BuildTarget.Stadia:
                        //    break;
                        //case BuildTarget.GameCoreXboxOne:
                        //    break;
                        //case BuildTarget.PS5:
                        //    break;
                        //case BuildTarget.EmbeddedLinux:
                        //    break;
                        default:
                            buildInfoInstance = new BuildInfo();
                            break;
                    }
                }
                else
                {
                    buildInfoInstance = buildInfo as BuildInfo;
                }

                if (buildInfoInstance == null)
                {
                    return null;
                }

                buildInfo = buildInfoInstance;
                Debug.Assert(buildInfo != null);

                return buildInfo;
            }
            internal set => buildInfo = value;
        }

        public static string GetValidVersionString(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return "1.0.0";
            }

            var parts = version.Split('.');

            switch (parts.Length)
            {
                case 0:
                    return "1.0.0";
                case 1:
                    return $"{parts[0]}.0.0";
                case 2:
                    return $"{parts[0]}.{parts[1]}.0";
                case 3:
                    return $"{parts[0]}.{parts[1]}.{parts[2].Replace("-preview", string.Empty)}";
                default:
                    return $"{parts[0]}.{parts[1]}.{parts[2].Replace("-preview", string.Empty)}.{parts[^1]}";
            }
        }

        /// <summary>
        /// Starts the build process with the provided <see cref="IBuildInfo"/>
        /// </summary>
        /// <returns>The <see cref="BuildReport"/> from Unity's <see cref="BuildPipeline"/></returns>
        public static BuildReport BuildUnityPlayer()
        {
            if (BuildInfo == null)
            {
                throw new ArgumentNullException(nameof(BuildInfo));
            }

            EditorUtility.DisplayProgressBar($"{BuildInfo.BuildTarget} Build Pipeline", "Gathering Build Data...", 0.25f);

            if (BuildInfo.IsCommandLine)
            {
                BuildInfo.ParseCommandLineArgs();
            }

            // use https://semver.org/
            // major.minor.build
            Version version = new Version(
                (buildInfo.Version == null || buildInfo.AutoIncrement)
                    ? string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion)
                        ? GetValidVersionString(Application.version)
                        : GetValidVersionString(PlayerSettings.bundleVersion)
                    : GetValidVersionString(buildInfo.Version.ToString()));

            // Only auto incitement if the version wasn't specified in the build info.
            if (buildInfo.Version == null &&
                buildInfo.AutoIncrement)
            {
                version = new Version(version.Major, version.Minor, version.Build + 1);
            }

            // Updates the Application.version and syncs Android and iOS bundle version strings
            PlayerSettings.bundleVersion = version.ToString();
            // Update Lumin bc the Application.version isn't synced like Android & iOS
            PlayerSettings.Lumin.versionName = PlayerSettings.bundleVersion;
            // Update WSA bc the Application.version isn't synced line Android & iOS
            PlayerSettings.WSA.packageVersion = new Version(version.Major, version.Minor, version.Build, 0);

            var buildTargetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(buildInfo.BuildTarget);
            var oldBuildIdentifier = PlayerSettings.GetApplicationIdentifier(buildTargetGroup);

            if (!string.IsNullOrWhiteSpace(buildInfo.BundleIdentifier))
            {
                PlayerSettings.SetApplicationIdentifier(buildTargetGroup, buildInfo.BundleIdentifier);
            }

            var playerBuildSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            if (!string.IsNullOrEmpty(playerBuildSymbols))
            {
                if (buildInfo.HasConfigurationSymbol())
                {
                    buildInfo.AppendWithoutConfigurationSymbols(playerBuildSymbols);
                }
                else
                {
                    buildInfo.AppendSymbols(playerBuildSymbols.Split(';'));
                }
            }

            if (!string.IsNullOrEmpty(buildInfo.BuildSymbols))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, buildInfo.BuildSymbols);
            }

            if ((buildInfo.BuildOptions & BuildOptions.Development) == BuildOptions.Development &&
                !buildInfo.HasConfigurationSymbol())
            {
                buildInfo.AppendSymbols(BuildSymbolDebug);
            }

            if (buildInfo.HasAnySymbols(BuildSymbolDebug))
            {
                buildInfo.BuildOptions |= BuildOptions.Development | BuildOptions.AllowDebugging;
            }

            if (buildInfo.HasAnySymbols(BuildSymbolRelease))
            {
                // Unity automatically adds the DEBUG symbol if the BuildOptions.Development flag is
                // specified. In order to have debug symbols and the RELEASE symbols we have to
                // inject the symbol Unity relies on to enable the /debug+ flag of csc.exe which is "DEVELOPMENT_BUILD"
                buildInfo.AppendSymbols("DEVELOPMENT_BUILD");
            }

            var oldColorSpace = PlayerSettings.colorSpace;

            if (buildInfo.ColorSpace.HasValue)
            {
                PlayerSettings.colorSpace = buildInfo.ColorSpace.Value;
            }

            var cacheDirectory = $"{Directory.GetParent(Application.dataPath)}{Path.DirectorySeparatorChar}Library{Path.DirectorySeparatorChar}il2cpp_cache{Path.DirectorySeparatorChar}{buildInfo.BuildTarget}";

            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            PlayerSettings.SetAdditionalIl2CppArgs(buildInfo.BuildTarget != BuildTarget.Android
                ? $"--cachedirectory=\"{cacheDirectory}\""
                : string.Empty);

            BuildReport buildReport = default;

            if (Application.isBatchMode)
            {
                Debug.Log("Scenes in build:");

                foreach (var scene in buildInfo.Scenes)
                {
                    Debug.Log($"    {scene.path}");
                }
            }

            try
            {
                buildReport = UnityEditor.BuildPipeline.BuildPlayer(
                    buildInfo.Scenes.ToArray(),
                    buildInfo.FullOutputPath,
                    buildInfo.BuildTarget,
                    buildInfo.BuildOptions);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            if (PlayerSettings.GetApplicationIdentifier(buildTargetGroup) != oldBuildIdentifier)
            {
                PlayerSettings.SetApplicationIdentifier(buildTargetGroup, oldBuildIdentifier);
            }

            PlayerSettings.colorSpace = oldColorSpace;

            EditorUtility.ClearProgressBar();

            return buildReport;
        }

        /// <summary>
        /// Validates the Unity Project assets by forcing a symbolic link sync and creates solution files.
        /// </summary>
        [UsedImplicitly]
        public static void ValidateProject()
        {
            CILoggingUtility.LoggingEnabled = false;

            try
            {
                SyncSolution();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }

            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Force Unity To Write Project Files
        /// </summary>
        public static void SyncSolution()
        {
            var syncVs = Type.GetType("UnityEditor.SyncVS,UnityEditor");
            Debug.Assert(syncVs != null);
            var syncSolution = syncVs.GetMethod("SyncSolution", BindingFlags.Public | BindingFlags.Static);
            Debug.Assert(syncSolution != null);
            syncSolution.Invoke(null, null);
        }

        /// <summary>
        /// Start a build using Unity's command line. Valid arguments:<para/>
        /// -autoIncrement : Increments the build revision number.<para/>
        /// -sceneList : A list of scenes to include in the build in CSV format.<para/>
        /// -sceneListFile : A json file with a list of scenes to include in the build.<para/>
        /// -buildOutput : The target directory you'd like the build to go.<para/>
        /// -colorSpace : The <see cref="ColorSpace"/> settings for the build.<para/>
        /// -x86 / -x64 / -ARM / -ARM64 : The target build platform. (Default is x86)<para/>
        /// -debug / -release / -master : The target build configuration. (Default is master)<para/>
        /// </summary>
        [UsedImplicitly]
        public static void StartCommandLineBuild()
        {
            // We don't need stack traces on all our logs. Makes things a lot easier to read.
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Debug.Log($"Starting command line build for {EditorPreferences.ApplicationProductName}...");

            BuildReport buildReport = default;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                SyncSolution();

                if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                {
                    var androidSdkPath = EditorPrefs.GetString("AndroidSdkRoot",
#if UNITY_EDITOR_WIN
                        "C:\\Program Files (x86)\\Android\\android-sdk"
#else
                        string.Empty
#endif
                        );
                    Debug.Log($"AndroidSdkRoot: {androidSdkPath}");
                }

                buildReport = BuildUnityPlayer();
            }
            catch (Exception e)
            {
                Debug.LogError($"Build Failed!\n{e.Message}\n{e.StackTrace}");
            }

            if (buildReport == null)
            {
                Debug.LogError("Failed to find a valid build report!");
                EditorApplication.Exit(1);
                return;
            }

            stopwatch.Stop();

            if (buildInfo.IsCommandLine)
            {
                CILoggingUtility.GenerateBuildReport(buildReport, stopwatch);
            }

            Debug.Log("Exiting command line build...");
            EditorApplication.Exit(buildReport.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        internal static bool CheckBuildScenes()
        {
            if (EditorBuildSettings.scenes.Length == 0)
            {
                return EditorUtility.DisplayDialog(
                    "Attention!",
                    "No scenes are present in the build settings.\n" +
                    "The build requires at least one scene to be defined.\n\n" +
                    "Do you want to cancel and add one?",
                    "Continue Anyway",
                    "Cancel Build");
            }

            return true;
        }

        /// <summary>
        /// Splits the scene list provided in CSV format to an array of scene path strings.
        /// </summary>
        /// <param name="sceneList">A CSV list of scenes to split.</param>
        /// <returns>An array of scene path strings.</returns>
        public static IEnumerable<EditorBuildSettingsScene> SplitSceneList(string sceneList)
        {
            var sceneListArray = sceneList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return sceneListArray
                .Where(scenePath => !string.IsNullOrWhiteSpace(scenePath))
                .Select(scene => new EditorBuildSettingsScene(scene.Trim(), true));
        }

        #region IOrderedCallback

        /// <inheritdoc />
        public int callbackOrder { get; }

        /// <inheritdoc />
        public void OnPreprocessBuild(BuildReport report) => buildInfo?.OnPreProcessBuild(report);

        /// <inheritdoc />
        public void OnPostprocessBuild(BuildReport report) => buildInfo?.OnPostProcessBuild(report);

        #endregion IOrderedCallback
    }
}
