// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Buildalon.Editor.BuildPipeline
{
    public class IOSBuildInfo : BuildInfo
    {
        public override BuildTarget BuildTarget => BuildTarget.iOS;

        public override BuildTargetGroup BuildTargetGroup => BuildTargetGroup.iOS;

        public override void OnPostProcessBuild(BuildReport report)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget) { return; }
#if PLATFORM_IOS
            var projectPath = $"{report.summary.outputPath}/Unity-iPhone.xcodeproj/project.pbxproj";
            var pbxProject = new UnityEditor.iOS.Xcode.PBXProject();
            pbxProject.ReadFromFile(projectPath);
#if UNITY_2019_3_OR_NEWER
            var mainTargetGuid = pbxProject.GetUnityMainTargetGuid();
#else
            var targetName = PBXProject.GetUnityTargetName();
            var targetGuid = pbxProject.TargetGuidByName(targetName);
#endif // UNITY_2019_3_OR_NEWER
            // https://discussions.unity.com/t/2019-3-validation-on-upload-to-store-gives-unityframework-framework-contains-disallowed-file/759612/27
            pbxProject.SetBuildProperty(pbxProject.GetUnityFrameworkTargetGuid(), "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
            pbxProject.SetBuildProperty(mainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
#if !UNITY_2022_1_OR_NEWER
            // https://discussions.unity.com/t/bitcode-bundle-could-not-be-generated-issue/792591/4
            pbxProject.SetBuildProperty(mainTargetGuid, "ENABLE_BITCODE", "NO");
            pbxProject.WriteToFile(projectPath);
            var projectInString = System.IO.File.ReadAllText(projectPath);
            projectInString = projectInString.Replace("ENABLE_BITCODE = YES;", "ENABLE_BITCODE = NO;");
            System.IO.File.WriteAllText(projectPath, projectInString);
#else
            pbxProject.WriteToFile(projectPath);
#endif // !UNITY_2022_1_OR_NEWER
#endif // PLATFORM_IOS
        }
    }
}
