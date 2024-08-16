// Licensed under the MIT License. See LICENSE in the project root for license information.

using Buildalon.Editor.BuildPipeline.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Buildalon.Editor.BuildPipeline
{
    /// <summary>
    /// A generic build info class.
    /// </summary>
    public class BuildInfo : IBuildInfo
    {
        /// <inheritdoc />
        public bool AutoIncrement { get; set; }

        private string bundleIdentifier;

        /// <inheritdoc />
        public string BundleIdentifier
        {
            get
            {
                if (string.IsNullOrWhiteSpace(bundleIdentifier))
                {
                    bundleIdentifier = PlayerSettings.applicationIdentifier;
                }

                return bundleIdentifier;
            }
            set
            {
                bundleIdentifier = value;
                PlayerSettings.applicationIdentifier = bundleIdentifier;
            }
        }

        /// <inheritdoc />
        public virtual Version Version { get; set; }

        /// <inheritdoc />
        public int? VersionCode { get; set; }

        /// <inheritdoc />
        public virtual BuildTarget BuildTarget { get; } = EditorUserBuildSettings.activeBuildTarget;

        /// <inheritdoc />
        public virtual BuildTargetGroup BuildTargetGroup { get; } = EditorUserBuildSettings.selectedBuildTargetGroup;

        /// <inheritdoc />
        public bool IsCommandLine { get; } = Application.isBatchMode;

        private string outputDirectory;

        /// <inheritdoc />
        public virtual string OutputDirectory
        {
            get => string.IsNullOrEmpty(outputDirectory)
                ? outputDirectory = BuildDeployPreferences.BuildDirectory
                : outputDirectory;
            set
            {
                var projectRoot = Directory.GetParent(EditorPreferences.ApplicationDataPath).FullName.Replace("\\", "/");
                var newValue = value?.Replace("\\", "/");
                outputDirectory = !string.IsNullOrWhiteSpace(newValue) && Path.IsPathRooted(newValue)
                    ? newValue.Contains(projectRoot)
                        ? newValue.Replace($"{projectRoot}/", string.Empty)
                        : GetRelativePath(projectRoot, newValue)
                    : newValue;
            }
        }

        private static string GetRelativePath(string fromPath, string toPath)
        {
#if UNITY_2021_1_OR_NEWER
            return Path.GetRelativePath(fromPath, toPath).Replace("\\", "/");
#else
            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);
            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
            return relativePath.Replace("\\", "/");
#endif // UNITY_2021_1_OR_NEWER
        }

        /// <inheritdoc />
        public virtual string AbsoluteOutputDirectory
        {
            get
            {
                var rootBuildDirectory = OutputDirectory;
                var dirCharIndex = rootBuildDirectory.IndexOf($"{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

                if (dirCharIndex != -1)
                {
                    rootBuildDirectory = rootBuildDirectory.Substring(0, dirCharIndex);
                }

                return Path.GetFullPath(Path.Combine(Path.Combine(EditorPreferences.ApplicationDataPath, ".."), rootBuildDirectory));
            }
        }

        /// <inheritdoc />
        public virtual string FullOutputPath => $"{OutputDirectory}{Path.DirectorySeparatorChar}{BundleIdentifier}{ExecutableFileExtension}";

        /// <inheritdoc />
        public virtual string ExecutableFileExtension
        {
            get
            {
                switch (BuildTarget)
                {
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                        return ".exe";
                    case BuildTarget.StandaloneOSX:
                        return ".app";
                    case BuildTarget.StandaloneLinux64:
                        return string.Empty;
                    default:
                        return Path.DirectorySeparatorChar.ToString();
                }
            }
        }

        private List<EditorBuildSettingsScene> scenes;

        /// <inheritdoc />
        public IEnumerable<EditorBuildSettingsScene> Scenes
        {
            get
            {
                if (scenes == null || !scenes.Any())
                {
                    scenes = EditorBuildSettings.scenes.Where(scene => !string.IsNullOrWhiteSpace(scene.path)).Where(scene => scene.enabled).ToList();
                }

                return scenes;
            }
            set => scenes = value.ToList();
        }

        /// <inheritdoc />
        public BuildOptions BuildOptions { get; set; }

        /// <inheritdoc />
        public ColorSpace? ColorSpace { get; set; }

        /// <inheritdoc />
        public string BuildSymbols { get; set; } = string.Empty;

        /// <inheritdoc />
        public virtual void ParseCommandLineArgs()
        {
            var arguments = Environment.GetCommandLineArgs();

            for (int i = 0; i < arguments.Length; ++i)
            {
                switch (arguments[i])
                {
                    case "-ignorecompilererrors":
                        CILoggingUtility.LoggingEnabled = false;
                        break;
                    case "-autoIncrement":
                        AutoIncrement = true;
                        break;
                    case "-versionName":
                        var versionString = UnityPlayerBuildTools.GetValidVersionString(arguments[++i]);

                        if (Version.TryParse(versionString, out var version))
                        {
                            Version = version;
                        }
                        else
                        {
                            Debug.LogError($"Failed to parse -versionName \"{arguments[i]}\"");
                        }
                        break;
                    case "-versionCode":
                        if (int.TryParse(arguments[++i], out var versionCode))
                        {
                            VersionCode = versionCode;
                        }
                        else
                        {
                            Debug.LogError($"Failed to parse -versionCode \"{arguments[i]}\"");
                        }
                        break;
                    case "-bundleIdentifier":
                        BundleIdentifier = arguments[++i];
                        break;
                    case "-sceneList":
                        Scenes = UnityPlayerBuildTools.SplitSceneList(arguments[++i]);
                        break;
                    case "-sceneListFile":
                        Scenes = UnityPlayerBuildTools.SplitSceneList(File.ReadAllText(arguments[++i]));
                        break;
                    case "-buildOutputDirectory":
                        OutputDirectory = arguments[++i]?.Replace("'", string.Empty).Replace("\"", string.Empty);
                        break;
                    case "-acceptExternalModificationsToPlayer":
                        BuildOptions = BuildOptions.SetFlag(BuildOptions.AcceptExternalModificationsToPlayer);
                        break;
                    case "-development":
                        EditorUserBuildSettings.development = true;
                        BuildOptions = BuildOptions.SetFlag(BuildOptions.Development);
                        break;
                    case "-colorSpace":
                        ColorSpace = (ColorSpace)Enum.Parse(typeof(ColorSpace), arguments[++i]);
                        break;
                    case "-buildConfiguration":
                        var configuration = arguments[++i].Substring(1).ToLower();

                        switch (configuration)
                        {
                            case "debug":
                            case "master":
                            case "release":
                                Configuration = configuration;
                                break;
                            default:
                                Debug.LogError($"Failed to parse -buildConfiguration: \"{configuration}\"");
                                break;
                        }

                        break;
                    case "-export":
#if PLATFORM_STANDALONE_WIN
                        UnityEditor.WindowsStandalone.UserBuildSettings.createSolution = true;
#endif
                        break;
                    case "-symlinkSources":
#if UNITY_2021_1_OR_NEWER
                        EditorUserBuildSettings.symlinkSources = true;
#else
                        EditorUserBuildSettings.symlinkLibraries = true;
#endif // UNITY_2021_1_OR_NEWER
                        break;
                    case "-disableDebugging":
                        EditorUserBuildSettings.allowDebugging = false;
                        Debug.LogWarning("This arg has been deprecated. use \"-allowDebugging false\" instead.");
                        break;
                    case "-allowDebugging":
                        var value = arguments[++i];

                        switch (value.ToLower())
                        {
                            case "true":
                                EditorUserBuildSettings.allowDebugging = true;
                                break;
                            case "false":
                                EditorUserBuildSettings.allowDebugging = false;
                                break;
                            default:
                                Debug.LogError($"Failed to parse -allowDebugging: \"{value}\"");
                                break;
                        }

                        break;
                    case "-dotnetApiCompatibilityLevel":
                        var apiCompatibilityLevelString = arguments[++i];

                        if (Enum.TryParse(apiCompatibilityLevelString, true, out ApiCompatibilityLevel apiCompatibility))
                        {
#if UNITY_2023_1_OR_NEWER
                            PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup), apiCompatibility);
#else
                            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup, apiCompatibility);
#endif // UNITY_2023_1_OR_NEWER
                        }
                        else
                        {
                            Debug.LogError($"Failed to parse -dotnetApiCompatibilityLevel: \"{apiCompatibilityLevelString}\"");
                        }

                        break;
                    case "-scriptingBackend":
                        var scriptingBackendString = arguments[++i].Substring(1).ToLower();

                        switch (scriptingBackendString)
                        {
                            case "mono":
#if UNITY_2023_1_OR_NEWER
                                PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup), ScriptingImplementation.Mono2x);
#else
                                PlayerSettings.SetScriptingBackend(BuildTargetGroup, ScriptingImplementation.Mono2x);
#endif // UNITY_2023_1_OR_NEWER
                                break;
                            case "il2cpp":
#if UNITY_2023_1_OR_NEWER
                                PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup), ScriptingImplementation.IL2CPP);
#else
                                PlayerSettings.SetScriptingBackend(BuildTargetGroup, ScriptingImplementation.IL2CPP);
#endif // UNITY_2023_1_OR_NEWER
                                break;
                            case "winrt":
#if UNITY_2023_1_OR_NEWER
                                PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup), ScriptingImplementation.WinRTDotNET);
#else
                                PlayerSettings.SetScriptingBackend(BuildTargetGroup, ScriptingImplementation.WinRTDotNET);
#endif // UNITY_2023_1_OR_NEWER
                                break;
                            default:
                                Debug.LogError($"Unsupported -scriptingBackend: \"{scriptingBackendString}\"");
                                break;
                        }

                        break;
                    case "-autoConnectProfiler":
                        EditorUserBuildSettings.connectProfiler = true;
                        BuildOptions = BuildOptions.SetFlag(BuildOptions.ConnectWithProfiler);
                        break;
                    case "-buildWithDeepProfilingSupport":
                        EditorUserBuildSettings.buildWithDeepProfilingSupport = true;
                        BuildOptions = BuildOptions.SetFlag(BuildOptions.EnableDeepProfilingSupport);
                        break;
                }
            }
        }

        /// <inheritdoc />
        public virtual bool Install { get; set; }

        /// <inheritdoc />
        public string Configuration
        {
            get
            {
                if (!this.HasConfigurationSymbol())
                {
                    return UnityPlayerBuildTools.BuildSymbolMaster;
                }

                return this.HasAnySymbols(UnityPlayerBuildTools.BuildSymbolDebug)
                    ? UnityPlayerBuildTools.BuildSymbolDebug
                    : this.HasAnySymbols(UnityPlayerBuildTools.BuildSymbolRelease)
                        ? UnityPlayerBuildTools.BuildSymbolRelease
                        : UnityPlayerBuildTools.BuildSymbolMaster;
            }
            set
            {
                if (this.HasConfigurationSymbol())
                {
                    this.RemoveSymbols(new[]
                    {
                        UnityPlayerBuildTools.BuildSymbolDebug,
                        UnityPlayerBuildTools.BuildSymbolRelease,
                        UnityPlayerBuildTools.BuildSymbolMaster
                    });
                }

                this.AppendSymbols(value);
            }
        }

        /// <inheritdoc />
        public virtual void OnPreProcessBuild(BuildReport report)
        {
        }

        /// <inheritdoc />
        public virtual void OnPostProcessBuild(BuildReport report)
        {
        }
    }
}
