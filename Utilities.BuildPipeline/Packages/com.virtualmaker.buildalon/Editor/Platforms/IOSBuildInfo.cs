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
#if !UNITY_2021_1_OR_NEWER
            // https://support.unity.com/hc/en-us/articles/207942813-How-can-I-disable-Bitcode-support
            var projectPath = $"{report.summary.outputPath}/Unity-iPhone.xcodeproj/project.pbxproj";
            var pbxProject = new UnityEditor.iOS.Xcode.PBXProject();
            pbxProject.ReadFromFile(projectPath);
            var target = pbxProject.GetUnityMainTargetGuid();
            // ReSharper disable once InconsistentNaming
            const string ENABLE_BITCODE = nameof(ENABLE_BITCODE);
            pbxProject.SetBuildProperty(target, ENABLE_BITCODE, "NO");
            target = pbxProject.TargetGuidByName(UnityEditor.iOS.Xcode.PBXProject.GetUnityTestTargetName());
            pbxProject.SetBuildProperty(target, ENABLE_BITCODE, "NO");
            target = pbxProject.GetUnityFrameworkTargetGuid();
            pbxProject.SetBuildProperty(target, ENABLE_BITCODE, "NO");
            pbxProject.WriteToFile(projectPath);
#endif
#endif
        }
    }
}
