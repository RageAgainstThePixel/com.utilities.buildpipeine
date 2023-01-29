﻿// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.Build.Reporting;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Utilities.Editor.BuildPipeline.Logging
{
    /// <summary>
    /// Github Actions logger
    /// https://docs.github.com/en/actions/learn-github-actions/workflow-commands-for-github-actions#about-workflow-commands
    /// </summary>
    public class GitHubActionsLogger : AbstractCILogger
    {
        /// <inheritdoc />
        public override string Warning => "::warning::";

        /// <inheritdoc />
        public override string Error => "::error::";

        /// <inheritdoc />
        public override void GenerateBuildSummary(BuildReport buildReport)
        {
            // temp disable logging to get the right messages sent.
            CILoggingUtility.LoggingEnabled = false;
            var buildResultMessage = $"Build {buildReport.summary.result}";
            var summary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (summary == null) { return; }

            using var summaryWriter = new StreamWriter(summary, true, Encoding.UTF8);
            summaryWriter.WriteLine($"# {buildResultMessage}");

            switch (buildReport.summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"{buildResultMessage}");
                    break;
                case BuildResult.Unknown:
                case BuildResult.Cancelled:
                    Debug.Log($"{WarningColor}{buildResultMessage}{ResetColor}");
                    break;
                case BuildResult.Failed:
                    Debug.Log($"{ErrorColor}{buildResultMessage}{ResetColor}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var totalBuildTime = TimeSpan.Zero;
            var stepNumber = 0;

            foreach (var step in buildReport.steps)
            {
                stepNumber++;
                totalBuildTime += step.duration;

                var buildStepMessage = $"{stepNumber}. {step.name[..step.name.IndexOf("=", StringComparison.Ordinal)]}";
                Debug.Log(buildStepMessage);

                var hasMessages = step.messages.Length > 0;
                summaryWriter.WriteLine($"## {buildStepMessage}");
                summaryWriter.WriteLine($"Completed in {step.duration:g}");

                if (!hasMessages)
                {
                    continue;
                }

                summaryWriter.WriteLine($"<details open><summary>{step.messages.Length} Log Messages</summary>");
                summaryWriter.WriteLine("");
                summaryWriter.WriteLine("| log type | message |");
                summaryWriter.WriteLine("| -------- | ------- |");

                foreach (var message in step.messages)
                {
                    summaryWriter.WriteLine($"| {message.type} | {message.content} |");

                    switch (message.type)
                    {
                        case LogType.Error:
                        case LogType.Assert:
                        case LogType.Exception:
                            Debug.Log($"{Error}{ErrorColor}{message.content}{ResetColor}");
                            break;
                        case LogType.Warning:
                            Debug.Log($"{Warning}{WarningColor}{message.content}{ResetColor}");
                            break;
                        case LogType.Log:
                            Debug.Log($"{message.content}");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            summaryWriter.WriteLine("</details>");
            summaryWriter.WriteLine("");
            summaryWriter.WriteLine($"## Total build time: {totalBuildTime:g}");
            summaryWriter.Close();
            summaryWriter.Dispose();
            CILoggingUtility.LoggingEnabled = true;
        }
    }
}
