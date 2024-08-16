// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
#if UNITY_STANDALONE_OSX
using System;
using UnityEditor.OSXStandalone;
#endif

namespace Buildalon.Editor.BuildPipeline
{
    public class MacOSBuildInfo : BuildInfo
    {
        /// <inheritdoc />
        public override BuildTarget BuildTarget => BuildTarget.StandaloneOSX;

        /// <inheritdoc />
        public override BuildTargetGroup BuildTargetGroup => BuildTargetGroup.Standalone;

        /// <inheritdoc />
        public override string ExecutableFileExtension => ".app";

#if UNITY_STANDALONE_OSX

        /// <inheritdoc />
        public override string FullOutputPath => UserBuildSettings.createXcodeProject
            ? OutputDirectory
            : base.FullOutputPath;

        /// <inheritdoc />
        public override void ParseCommandLineArgs()
        {
            base.ParseCommandLineArgs();
            var arguments = Environment.GetCommandLineArgs();

            for (int i = 0; i < arguments.Length; i++)
            {
                switch (arguments[i])
                {
                    case "-export":
                        UserBuildSettings.createXcodeProject = true;
                        break;
#if UNITY_2020_1_OR_NEWER
                    case "-arch":
                        var arch = arguments[++i].ToLower();
                        UserBuildSettings.architecture = arch switch
                        {
#if UNITY_2022_1_OR_NEWER
                            "x64" => UnityEditor.Build.OSArchitecture.x64,
                            "arm64" => UnityEditor.Build.OSArchitecture.ARM64,
                            "x64arm64" => UnityEditor.Build.OSArchitecture.x64ARM64,
#else
                            "x64" => MacOSArchitecture.x64,
                            "arm64" => MacOSArchitecture.ARM64,
                            "x64arm64" => MacOSArchitecture.x64ARM64,
#endif // UNITY_2020_1_OR_NEWER
                            _ => throw new Exception($"Unsupported architecture: {arch}"),
                        };
                        break;
#endif // UNITY_2020_1_OR_NEWER
                }
            }
        }
#endif
    }
}
