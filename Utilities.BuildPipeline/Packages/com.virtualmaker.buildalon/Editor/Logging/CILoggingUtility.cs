﻿// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Buildalon.Editor.BuildPipeline.Logging
{
    /// <summary>
    /// Logging utility designed to properly output logs to continuous integration workflow logging consoles.
    /// </summary>
    [InitializeOnLoad]
    public static class CILoggingUtility
    {
        /// <summary>
        /// The logger to use.
        /// </summary>
        public static ICILogger Logger { get; set; }

        private static bool loggingEnabled = Application.isBatchMode;

        /// <summary>
        /// Is CI Logging currently enabled?
        /// </summary>
        public static bool LoggingEnabled
        {
            get => loggingEnabled;
            set
            {
                if (loggingEnabled == value) { return; }

                Debug.Log(value ? "CI Logging Enabled" : "CI Logging Disabled");

                loggingEnabled = value;
            }
        }

        /// <summary>
        /// List of ignored log messages.
        /// </summary>
        public static readonly List<string> IgnoredLogs = new List<string>
        {
            @".android\repositories.cfg could not be loaded",
            @"Using symlinks in Unity projects may cause your project to become corrupted",
            @"Skipping WindowsDictationDataProvider registration",
            @"Skipping WindowsSpeechDataProvider registration",
            @"Cancelling DisplayDialog: Built in VR Detected XR Plug-in Management has detected that this project is using built in VR.",
            @"Reference Rewriter found some errors while running with command",
            @"Reference rewriter: Error: method `System.Numerics.Vector3[] Windows.Perception.Spatial.SpatialStageFrameOfReference::TryGetMovementBounds(Windows.Perception.Spatial.SpatialCoordinateSystem)` doesn't exist in target framework. It is referenced from XRTK.WindowsMixedReality.dll at System.Boolean XRTK.WindowsMixedReality.Providers.BoundarySystem.WindowsMixedRealityBoundaryDataProvider::TryGetBoundaryGeometry",
            @"Selected Visual Studio is missing required components and may not be able to build the generated project. You can install the missing Visual Studio components by opening the generated project in Visual Studio.",
            @"GfxDevice renderer is null. Unity cannot update the Ambient Probe and Reflection Probes that the SkyManager generates. Run the Editor without the -nographics argument or generate lighting for your scene.",
            @"Light baking could not be started because no valid OpenCL device could be found."
        };

        static CILoggingUtility()
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TF_BUILD", EnvironmentVariableTarget.Process)))
            {
                Logger = new AzurePipelinesLogger();
            }

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS", EnvironmentVariableTarget.Process)))
            {
                Logger = new GitHubActionsLogger();
            }
        }

        public static void GenerateBuildReport(BuildReport buildReport, Stopwatch stopwatch) => Logger?.GenerateBuildSummary(buildReport, stopwatch);
    }
}
