// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
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
            var buildResultMessage = $"Build success? {buildReport.summary.result} | Duration: {buildReport.summary.totalTime:g}";

            switch (buildReport.summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"::notice::{buildResultMessage}");
                    break;
                case BuildResult.Unknown:
                case BuildResult.Cancelled:
                    Debug.Log($"{Warning}{WarningColor}{buildResultMessage}{ResetColor}");
                    break;
                case BuildResult.Failed:
                    Debug.Log($"{Error}{ErrorColor}{buildResultMessage}{ResetColor}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            foreach (var step in buildReport.steps)
            {
                Debug.Log($"::notice::Build Step: {step.name} | Duration: {step.duration:g}");

                foreach (var message in step.messages)
                {
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

            CILoggingUtility.LoggingEnabled = true;
        }
    }
}
