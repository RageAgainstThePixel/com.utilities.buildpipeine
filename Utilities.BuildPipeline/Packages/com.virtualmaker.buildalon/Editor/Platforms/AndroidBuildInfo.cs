// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Buildalon.Editor.BuildPipeline
{
    public class AndroidBuildInfo : BuildInfo
    {
        /// <inheritdoc />
        public override BuildTarget BuildTarget => BuildTarget.Android;

        /// <inheritdoc />
        public override BuildTargetGroup BuildTargetGroup => BuildTargetGroup.Android;

        /// <inheritdoc />
        public override string ExecutableFileExtension => EditorUserBuildSettings.buildAppBundle ? ".abb" : ".apk";

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
                    case "-appBundle":
                        EditorUserBuildSettings.buildAppBundle = true;
                        break;
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
                    case "-keystorePath":
                        PlayerSettings.Android.keystoreName = arguments[++i];
                        useCustomKeystore = true;
                        break;
                    case "-keystorePass":
                        PlayerSettings.Android.keystorePass = arguments[++i];
                        useCustomKeystore = true;
                        break;
                    case "-keyaliasName":
                        PlayerSettings.Android.keyaliasName = arguments[++i];
                        useCustomKeystore = true;
                        break;
                    case "-keyaliasPass":
                        PlayerSettings.Android.keyaliasPass = arguments[++i];
                        useCustomKeystore = true;
                        break;
                    case "-export":
                        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
                        break;
                    case "-symbols":
#if UNITY_6000_0_OR_NEWER
#if PLATFORM_ANDROID
                        var symbols = arguments[++i] switch
                        {
                            "public" => Unity.Android.Types.DebugSymbolLevel.SymbolTable,
                            "debugging" => Unity.Android.Types.DebugSymbolLevel.Full,
                            _ => Unity.Android.Types.DebugSymbolLevel.None
                        };

                        UnityEditor.Android.UserBuildSettings.DebugSymbols.level = symbols;
                        UnityEditor.Android.UserBuildSettings.DebugSymbols.format = Unity.Android.Types.DebugSymbolFormat.Zip;
#endif // PLATFORM_ANDROID
#else
                        var symbols = arguments[++i] switch
                        {
                            "public" => AndroidCreateSymbols.Public,
                            "debugging" => AndroidCreateSymbols.Debugging,
                            _ => AndroidCreateSymbols.Disabled
                        };
                        EditorUserBuildSettings.androidCreateSymbols = symbols;
#pragma warning disable CS0618 // Type or member is obsolete
                        EditorUserBuildSettings.androidCreateSymbolsZip = true;
#pragma warning restore CS0618 // Type or member is obsolete
#endif // UNITY_6000_0_OR_NEWER
                        break;
                    case "-versionCode":
                        BuildNumber = arguments[++i];
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
            base.OnPreProcessBuild(report);

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget)
            {
                return;
            }

            if (Application.isBatchMode)
            {
                // Disable to prevent gradle form killing parallel builds on same build machine
                EditorPrefs.SetBool("AndroidGradleStopDaemonsOnExit", false);
            }
        }
    }
}
