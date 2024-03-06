// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
#if UNITY_STANDALONE_OSX
using System;
using UnityEditor.OSXStandalone;
#endif


namespace Utilities.Editor.BuildPipeline
{
    public class MacOSBuildInfo : BuildInfo
    {
        public override BuildTarget BuildTarget => BuildTarget.StandaloneOSX;

        public override string ExecutableFileExtension => ".app";

#if UNITY_STANDALONE_OSX

        public override string FullOutputPath => UserBuildSettings.createXcodeProject
            ? OutputDirectory
            : base.FullOutputPath;

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
                    case "-arch":
                        var arch = arguments[++i];
                        UserBuildSettings.architecture = arch switch
                        {
#if UNITY_2022_1_OR_NEWER
                            "x64" => UnityEditor.Build.OSArchitecture.x64,
                            "ARM64" => UnityEditor.Build.OSArchitecture.ARM64,
                            "x64ARM64" => UnityEditor.Build.OSArchitecture.x64ARM64,
#else
                            "x64" => MacOSArchitecture.x64,
                            "ARM64" => MacOSArchitecture.ARM64,
                            "x64ARM64" => MacOSArchitecture.x64ARM64,
#endif
                            _ => throw new Exception($"Unsupported architecture: {arch}"),
                        };
                        break;
                }
            }
        }
#endif
    }
}
