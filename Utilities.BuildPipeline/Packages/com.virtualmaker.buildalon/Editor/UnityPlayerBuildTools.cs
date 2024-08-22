// Licensed under the MIT License. See LICENSE in the project root for license information.

using Buildalon.Editor.BuildPipeline.Logging;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Buildalon.Editor.BuildPipeline
{
    /// <summary>
    /// Cross-platform player build tools
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
                        case BuildTarget.Android:
                            buildInfoInstance = new AndroidBuildInfo();
                            break;
                        case BuildTarget.iOS:
                            buildInfoInstance = new IOSBuildInfo();
                            break;
                        // TODO: Add additional platform specific build info classes as needed
                        //case BuildTarget.StandaloneWindows:
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
                    return $"{parts[0]}.{parts[1]}.{parts[2].Replace("-preview", string.Empty)}.{parts[parts.Length - 1]}";
            }
        }

        [MenuItem("Tools/Build Player", false, 999)]
        public static void BuildPlayerMenu()
        {
            var result = BuildUnityPlayer();
            EditorUtility.RevealInFinder(result.summary.outputPath);
        }

        /// <summary>
        /// Starts the build process with the provided <see cref="IBuildInfo"/>
        /// </summary>
        /// <returns>The <see cref="BuildReport"/> from Unity's <see cref="BuildPipeline"/></returns>
        public static BuildReport BuildUnityPlayer()
        {
            if (BuildInfo == null) { throw new ArgumentNullException(nameof(BuildInfo)); }
            EditorUtility.DisplayProgressBar($"{BuildInfo.BuildTarget} Build Pipeline", "Gathering Build Data...", 0.25f);

            if (BuildInfo.IsCommandLine)
            {
                BuildInfo.ParseCommandLineArgs();
            }

            // use https://semver.org/
            // major.minor.build
            var version = new Version(
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
            // Update WSA bc the Application.version isn't synced like Android & iOS
            PlayerSettings.WSA.packageVersion = new Version(version.Major, version.Minor, version.Build, 0);

            var buildTargetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(buildInfo.BuildTarget);
#if UNITY_2023_1_OR_NEWER
            var oldBuildIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));
#else
            var oldBuildIdentifier = PlayerSettings.GetApplicationIdentifier(buildTargetGroup);
#endif // UNITY_2023_1_OR_NEWER

            if (!string.IsNullOrWhiteSpace(buildInfo.BundleIdentifier))
            {
#if UNITY_2023_1_OR_NEWER
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), buildInfo.BundleIdentifier);
#else
                PlayerSettings.SetApplicationIdentifier(buildTargetGroup, buildInfo.BundleIdentifier);
#endif // UNITY_2023_1_OR_NEWER
            }

#if UNITY_2023_1_OR_NEWER
            var playerBuildSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));
#else
            var playerBuildSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
#endif // UNITY_2023_1_OR_NEWER

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
#if UNITY_2023_1_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), buildInfo.BuildSymbols);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, buildInfo.BuildSymbols);
#endif // UNITY_2023_1_OR_NEWER
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

            BuildReport buildReport = default;

            if (Application.isBatchMode)
            {
                Debug.Log($"Build Target: {buildInfo.BuildTarget}");
                Debug.Log($"Build Options: {buildInfo.BuildOptions}");
                Debug.Log($"Target output: \"{buildInfo.FullOutputPath}\"");
                Debug.Log($"Scenes in build:\n{string.Join("\n    ", buildInfo.Scenes.Select(scene => scene.path))}");
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
                Debug.LogException(e);
            }
            finally
            {
                PlayerSettings.colorSpace = oldColorSpace;

#if UNITY_2023_1_OR_NEWER
                if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup)) != oldBuildIdentifier)
                {
                    PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), oldBuildIdentifier);
                }
#else
                if (PlayerSettings.GetApplicationIdentifier(buildTargetGroup) != oldBuildIdentifier)
                {
                    PlayerSettings.SetApplicationIdentifier(buildTargetGroup, oldBuildIdentifier);
                }
#endif // UNITY_2023_1_OR_NEWER
                EditorUtility.ClearProgressBar();
            }

            return buildReport;
        }

        /// <summary>
        /// Validates the Unity Project assets by forcing a symbolic link sync and creates solution files.
        /// </summary>
        [UsedImplicitly]
        public static async void ValidateProject()
        {
            CILoggingUtility.LoggingEnabled = false;

            try
            {
                var arguments = Environment.GetCommandLineArgs();

                for (int i = 0; i < arguments.Length; ++i)
                {
                    switch (arguments[i])
                    {
                        case "-importTMProEssentialsAsset":
                            await ImportTMProEssentialAssetsAsync();
                            break;
                    }
                }

                SyncSolution();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }

            Debug.Log("Project Validation Completed");
            EditorApplication.Exit(0);
        }

        private static async Task ImportTMProEssentialAssetsAsync()
        {
#if TEXT_MESH_PRO
            Debug.Log("TextMesh Pro Essentials Import started....");

            // Check if the TextMesh Pro folder already exists
            if (System.IO.Directory.Exists("Assets/TextMesh Pro")) { return; }

            byte[] settingsBackup = null;
            string settingsFilePath = null;

            // Check if the TMP Settings asset is already present in the project.
            var settings = AssetDatabase.FindAssets("t:TMP_Settings");

            var tcs = new TaskCompletionSource<bool>();

            if (settings.Length > 0)
            {
                // Save assets just in case the TMP Setting were modified before import.
                AssetDatabase.SaveAssets();

                // Copy existing TMP Settings asset to a byte[]
                settingsFilePath = AssetDatabase.GUIDToAssetPath(settings[0]);
#if UNITY_2021_1_OR_NEWER
                settingsBackup = await System.IO.File.ReadAllBytesAsync(settingsFilePath).ConfigureAwait(true);
#else
                settingsBackup = System.IO.File.ReadAllBytes(settingsFilePath);
#endif //  UNITY_2021_1_OR_NEWER
            }

            AssetDatabase.importPackageCompleted += ImportCallback;

            var packageFullPath = TMPro.EditorUtilities.TMP_EditorUtility.packageFullPath;
            var importPath = $"{packageFullPath}/Package Resources/TMP Essential Resources.unitypackage";
            Debug.Log($"TextMesh Pro Essentials Import from {importPath}");

            if (!System.IO.File.Exists(importPath))
            {
                throw new System.IO.FileNotFoundException($"Unable to find the TextMesh Pro package at {importPath}");
            }

            ImportPackageImmediately(importPath);

            void ImportCallback(string packageName)
            {
                Debug.Log("TextMesh Pro Essentials Import::ImportCallback");

                if (settingsFilePath != null && settingsBackup != null)
                {
                    // Restore backup of TMP Settings from byte[]
                    System.IO.File.WriteAllBytes(settingsFilePath, settingsBackup);
                }

                AssetDatabase.importPackageCompleted -= ImportCallback;
                tcs.TrySetResult(true);
            }

            await tcs.Task.ConfigureAwait(true);

            if (!System.IO.Directory.Exists("Assets/TextMesh Pro"))
            {
                throw new Exception("Failed to import TextMeshPro resources!");
            }

            Debug.Log("TextMesh Pro Essentials Import Completed");
#else
            await Task.CompletedTask;
#endif // TEXT_MESH_PRO
        }

        private static void ImportPackageImmediately(string importPath)
        {
            var importImmediate = typeof(AssetDatabase).GetMethod(nameof(ImportPackageImmediately), BindingFlags.NonPublic | BindingFlags.Static);
            Debug.Assert(importImmediate != null);
            importImmediate.Invoke(null, new object[] { importPath });
        }

        /// <summary>
        /// Force Unity to update CSProj files and generates solution.
        /// </summary>
        public static void SyncSolution()
        {
            Debug.Log(nameof(SyncSolution));
            var syncVs = Type.GetType("UnityEditor.SyncVS,UnityEditor");
            Debug.Assert(syncVs != null);
            var syncSolution = syncVs.GetMethod("SyncSolution", BindingFlags.Public | BindingFlags.Static);
            Debug.Assert(syncSolution != null);
            syncSolution.Invoke(null, null);
        }

        /// <summary>
        /// Start a build using command line arguments.
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
            CILoggingUtility.GenerateBuildReport(buildReport, stopwatch);
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
