using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using UnityEngine;

/// <summary>
/// Unity 6000.6+ generates settings.gradle with Unity's internal artifactory listed first.
/// It is unreachable outside Unity's network, which fails Gradle plugin resolution, so strip it.
/// </summary>
internal class StripUnityInternalGradleRepositories : IPostGenerateGradleAndroidProject
{
    private static readonly Regex internalRepository = new Regex(
        @"maven\s*\{\s*url\s*'https://bfartifactory\.bf\.unity3d\.com[^']*'\s*\}\s*",
        RegexOptions.Compiled);

    public int callbackOrder => 0;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var settingsPath = Path.Combine(Directory.GetParent(path).FullName, "settings.gradle");

        if (!File.Exists(settingsPath)) { return; }

        var settings = File.ReadAllText(settingsPath);
        var stripped = internalRepository.Replace(settings, string.Empty);

        if (stripped == settings) { return; }

        File.WriteAllText(settingsPath, stripped);
        Debug.Log($"Removed Unity internal artifactory repositories from {settingsPath}");
    }
}
