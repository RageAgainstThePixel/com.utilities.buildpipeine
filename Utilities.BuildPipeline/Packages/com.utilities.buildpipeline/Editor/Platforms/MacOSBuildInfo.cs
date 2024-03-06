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

        public override void ParseCommandLineArgs()
        {
            base.ParseCommandLineArgs();
#if UNITY_STANDALONE_OSX
            var arguments = Environment.GetCommandLineArgs();

            for (int i = 0; i < arguments.Length; i++)
            {
                switch (arguments[i])
                {
                    case "-export":
                        UserBuildSettings.createXcodeProject = true;
                        break;
                    case "-arch"
                        var arch = arguments[++i];

                        switch (arch)
                        {
                            case "x64":
                                UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64;
                                break;
                            case "ARM64":
                                UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.ARM64;
                                break;
                            case "x64ARM64":
                                UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64ARM64;
                                break;
                            default:
                                throw new Exception($"Unsupported architecture: {arch}");
                        }
                }
            }
#endif
        }
    }
}
