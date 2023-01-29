// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;
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
            var buildResultMessage = $"Build success? {buildReport.summary.result}";
            Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", $"# {buildResultMessage}");

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
            TimeSpan totalBuildTime = TimeSpan.Zero;

            foreach (var step in buildReport.steps)
            {
                var buildStepMessage = $"Phase: {step.name} | Duration: {step.duration:g}";
                Debug.Log(buildStepMessage);
                totalBuildTime += step.duration;

                Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", $"## {buildStepMessage}");
                Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", "| log type | message |");
                Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", "| -------- | ------- |");

                foreach (var message in step.messages)
                {
                    Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", $"| {message.type} | {message.content} |");

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

            Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", $"## Total build time: {totalBuildTime:g}");
            CILoggingUtility.LoggingEnabled = true;
        }
    }
}
