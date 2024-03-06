// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Utilities.Editor.BuildPipeline.Logging;

namespace Utilities.Editor.BuildPipeline
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
        public bool IsCommandLine { get; } = Application.isBatchMode;

        private string outputDirectory;

        /// <inheritdoc />
        public virtual string OutputDirectory
        {
            get => string.IsNullOrEmpty(outputDirectory)
                ? outputDirectory = BuildDeployPreferences.BuildDirectory
                : outputDirectory;
            set => outputDirectory = !string.IsNullOrWhiteSpace(value) && Path.IsPathRooted(value)
                ? Path.GetRelativePath(Directory.GetParent(EditorPreferences.ApplicationDataPath)!.FullName, value)
                : value;
        }

        /// <inheritdoc />
        public virtual string AbsoluteOutputDirectory
        {
            get
            {
                var rootBuildDirectory = OutputDirectory;
                var dirCharIndex = rootBuildDirectory.IndexOf(Path.DirectorySeparatorChar, StringComparison.Ordinal);

                if (dirCharIndex != -1)
                {
                    rootBuildDirectory = rootBuildDirectory[..dirCharIndex];
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
                    case "-ignoreCompilerErrors":
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
                        OutputDirectory = arguments[++i].Replace('\'', char.MinValue).Replace('"', char.MinValue);
                        break;
                    case "-acceptExternalModificationsToPlayer":
                        BuildOptions = BuildOptions.SetFlag(BuildOptions.AcceptExternalModificationsToPlayer);
                        break;
                    case "-colorSpace":
                        ColorSpace = (ColorSpace)Enum.Parse(typeof(ColorSpace), arguments[++i]);
                        break;
                    case "-buildConfiguration":
                        var configuration = arguments[++i][1..].ToLower();

                        switch (configuration)
                        {
                            case "debug":
                            case "master":
                            case "release":
                                Configuration = configuration;
                                break;
                            default:
                                Debug.LogError($"Failed to parse -buildConfiguration: {configuration}");
                                break;
                        }

                        break;
                    case "-export":
#if PLATFORM_STANDALONE_WIN
                        UnityEditor.WindowsStandalone.UserBuildSettings.createSolution = true;
#endif
                        break;
                    case "-symlinkSources":
                        EditorUserBuildSettings.symlinkSources = true;
                        break;
                    case "-disableDebugging":
                        EditorUserBuildSettings.allowDebugging = false;
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
