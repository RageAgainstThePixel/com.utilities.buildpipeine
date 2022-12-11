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
        public override string ExecutableFileExtension => ".apk";

        public override void ParseCommandLineArgs()
        {
            base.ParseCommandLineArgs();

            var arguments = Environment.GetCommandLineArgs();

            for (int i = 0; i < arguments.Length; ++i)
            {
                switch (arguments[i])
                {
                    case "-splitApk":
                        PlayerSettings.Android.useAPKExpansionFiles = true;
                        break;
                    case "-keyaliasName":
                        PlayerSettings.Android.keyaliasName = arguments[++i];
                        PlayerSettings.Android.useCustomKeystore = true;
                        break;
                    case "-keyaliasPass":
                        PlayerSettings.Android.keyaliasPass = arguments[++i];
                        PlayerSettings.Android.useCustomKeystore = true;
                        break;
                    case "-keystoreName":
                        PlayerSettings.Android.keystoreName = arguments[++i];
                        PlayerSettings.Android.useCustomKeystore = true;
                        break;
                    case "-keystorePass":
                        PlayerSettings.Android.useCustomKeystore = true;
                        PlayerSettings.Android.keystorePass = arguments[++i];
                        break;
                }
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
