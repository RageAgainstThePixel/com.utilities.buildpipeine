// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Utilities.Editor.BuildPipeline
{
    public class IOSBuildInfo : BuildInfo
    {
        public override BuildTarget BuildTarget => BuildTarget.iOS;

        public override BuildTargetGroup BuildTargetGroup => BuildTargetGroup.iOS;

        public override void OnPostProcessBuild(BuildReport report)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget) { return; }
#if PLATFORM_IOS
#if !UNITY_2021_1_OR_NEWER
            // https://discussions.unity.com/t/bitcode-bundle-could-not-be-generated-issue/792591/4
            var projectPath = $"{report.summary.outputPath}/Unity-iPhone.xcodeproj/project.pbxproj";
            var pbxProject = new UnityEditor.iOS.Xcode.PBXProject();
            pbxProject.ReadFromFile(projectPath);
#if UNITY_2019_3_OR_NEWER
            var targetGuid = pbxProject.GetUnityMainTargetGuid();
#else
            var targetName = PBXProject.GetUnityTargetName();
            var targetGuid = pbxProject.TargetGuidByName(targetName);
#endif
            pbxProject.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");
            pbxProject.WriteToFile(projectPath);
            var projectInString = System.IO.File.ReadAllText(projectPath);
            projectInString = projectInString.Replace("ENABLE_BITCODE = YES;", "ENABLE_BITCODE = NO;");
            System.IO.File.WriteAllText(projectPath, projectInString);
#endif
#endif
        }
    }
}
