// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Utilities.Editor.BuildPipeline
{
    public class AndroidBuildInfo : BuildInfo
    {
        /// <inheritdoc />
        public override BuildTarget BuildTarget => BuildTarget.Android;

        /// <inheritdoc />
        public override BuildTargetGroup BuildTargetGroup => BuildTargetGroup.Android;

        /// <inheritdoc />
        public override string ExecutableFileExtension => ".apk";

        /// <inheritdoc />
        public override string FullOutputPath => PlayerSettings.Android.buildApkPerCpuArchitecture || EditorUserBuildSettings.exportAsGoogleAndroidProject
            ? OutputDirectory
            : base.FullOutputPath;

        public override void ParseCommandLineArgs()
        {
            base.ParseCommandLineArgs();

            var arguments = Environment.GetCommandLineArgs();
            var useCustomKeystore = false;

            for (int i = 0; i < arguments.Length; ++i)
            {
                switch (arguments[i])
                {
                    case "-splitBinary":
                        PlayerSettings.Android.buildApkPerCpuArchitecture = true;
                        break;
                    case "-splitApk":
#if UNITY_2023_1_OR_NEWER
                        PlayerSettings.Android.splitApplicationBinary = true;
#else
                        PlayerSettings.Android.useAPKExpansionFiles = true;
#endif // UNITY_2023_1_OR_NEWER
                        break;
                    case "-keyaliasPass":
#if UNITY_2023_1_OR_NEWER
                        PlayerSettings.Android.keyaliasPass = arguments[++i];
#else
                        PlayerSettings.keyaliasPass = arguments[++i];
#endif // UNITY_2023_1_OR_NEWER
                        useCustomKeystore = true;
                        break;
                    case "-keystorePass":
#if UNITY_2023_1_OR_NEWER
                        PlayerSettings.Android.keystorePass = arguments[++i];
#else
                        PlayerSettings.keystorePass = arguments[++i];
#endif // UNITY_2023_1_OR_NEWER
                        useCustomKeystore = true;
                        break;
                    case "-export":
                        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
                        break;
                    case "-symbols":
#if UNITY_2021_1_OR_NEWER
                        var symbols = arguments[++i] switch
                        {
                            "public" => AndroidCreateSymbols.Public,
                            "debugging" => AndroidCreateSymbols.Debugging,
                            _ => AndroidCreateSymbols.Disabled
                        };
#if UNITY_6000_0_OR_NEWER
#pragma warning disable CS0618 // Type or member is obsolete
                        EditorUserBuildSettings.androidCreateSymbols = symbols;
#pragma warning restore CS0618
#else
                        EditorUserBuildSettings.androidCreateSymbols = symbols;
#endif // UNITY_6000_0_OR_NEWER
#else
                        EditorUserBuildSettings.androidCreateSymbolsZip = true;
#endif // UNITY_2021_1_OR_NEWER
                        break;
                }
            }

            if (useCustomKeystore)
            {
                PlayerSettings.Android.useCustomKeystore = true;
            }
        }

        /// <inheritdoc />
        public override void OnPreProcessBuild(BuildReport report)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget)
            {
                return;
            }

            if (Application.isBatchMode)
            {
                // Disable to prevent gradle form killing parallel builds
                EditorPrefs.SetBool("AndroidGradleStopDaemonsOnExit", false);
            }

            if (VersionCode.HasValue)
            {
                PlayerSettings.Android.bundleVersionCode = VersionCode.Value;
            }
            else if (AutoIncrement)
            {
                // Usually version codes are unique and not tied to the usual semver versions
                // see https://developer.android.com/studio/publish/versioning#appversioning
                // versionCode - A positive integer used as an internal version number.
                // This number is used only to determine whether one version is more recent than another,
                // with higher numbers indicating more recent versions. The Android system uses the
                // versionCode value to protect against downgrades by preventing users from installing
                // an APK with a lower versionCode than the version currently installed on their device.
                PlayerSettings.Android.bundleVersionCode++;
            }
        }
    }
}
