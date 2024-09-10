// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEditor;
using UnityEngine;

namespace Buildalon.Editor.BuildPipeline
{
    public class WSAPlayerBuildInfo : BuildInfo
    {
        /// <inheritdoc />
        public override BuildTarget BuildTarget => BuildTarget.WSAPlayer;

        /// <inheritdoc />
        public override BuildTargetGroup BuildTargetGroup => BuildTargetGroup.WSA;

        /// <inheritdoc />
        public override string FullOutputPath => OutputDirectory;

        /// <inheritdoc />
        public override void ParseCommandLineArgs()
        {
            base.ParseCommandLineArgs();
            var arguments = Environment.GetCommandLineArgs();

            for (int i = 0; i < arguments.Length; ++i)
            {
                switch (arguments[i])
                {
                    case "-arch":
                        var arch = arguments[++i];

                        if (!string.IsNullOrWhiteSpace(arch))
                        {
                            EditorUserBuildSettings.wsaArchitecture = arch;
                        }
                        else
                        {
                            Debug.LogError($"Failed to parse -arch \"{arguments[i]}\"");
                        }
                        break;
#if !UNITY_2021_1_OR_NEWER
                    case "-wsaSubtarget":
                        if (Enum.TryParse<WSASubtarget>(arguments[++i], out var subTarget))
                        {
                            EditorUserBuildSettings.wsaSubtarget = subTarget;
                        }
                        else
                        {
                            Debug.LogError($"Failed to parse -wsaSubtarget \"{arguments[i]}\"");
                        }
                        break;
#endif
                    case "-wsaUWPBuildType":
                        if (Enum.TryParse<WSAUWPBuildType>(arguments[++i], out var buildType))
                        {
                            EditorUserBuildSettings.wsaUWPBuildType = buildType;
                        }
                        else
                        {
                            Debug.LogError($"Failed to parse -wsaUWPBuildType \"{arguments[i]}\"");
                        }
                        break;
                    case "-wsaSetDeviceFamily":
                        if (Enum.TryParse<PlayerSettings.WSATargetFamily>(arguments[++i], out var family))
                        {
                            PlayerSettings.WSA.SetTargetDeviceFamily(family, true);
                        }
                        else
                        {
                            Debug.LogError($"Failed to parse -wsaSetDeviceFamily \"{arguments[i]}\"");
                        }
                        break;
                    case "-wsaUWPSDK":
                        EditorUserBuildSettings.wsaUWPSDK = arguments[++i];
                        break;
                    case "-wsaMinUWPSDK":
                        EditorUserBuildSettings.wsaMinUWPSDK = arguments[++i];
                        break;
                    case "-wsaCertificate":
                        var path = arguments[++i];

                        if (string.IsNullOrWhiteSpace(path))
                        {
                            Debug.LogError("Failed to parse -wsaCertificate. Missing path!");
                            break;
                        }

                        var password = arguments[++i];

                        if (string.IsNullOrWhiteSpace(password))
                        {
                            Debug.LogError("Failed to parse -wsaCertificate. Missing password!");
                            break;
                        }

                        PlayerSettings.WSA.SetCertificate(path, password);
                        break;
                }
            }
        }
    }
}
