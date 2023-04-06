// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

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

        private static readonly string[] suffix = { "bytes", "KB", "MB", "GB", "TB" };

        private static string FormatFileSize(ulong fileSize)
        {
            const int offset = 1024;
            var i = 0;

            while (fileSize > offset && i < suffix.Length)
            {
                fileSize /= offset;
                i++;
            }

            return $"{fileSize} {suffix[i]}";
        }

        /// <inheritdoc />
        public override void GenerateBuildSummary(BuildReport buildReport, Stopwatch stopwatch)
        {
            // temp disable logging to get the right messages sent.
            CILoggingUtility.LoggingEnabled = false;
            var buildResultMessage = $"{buildReport.summary.platform} Build {buildReport.summary.result}!";
            var summary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (summary == null) { return; }

            using var summaryWriter = new StreamWriter(summary, true, Encoding.UTF8);
            summaryWriter.WriteLine($"# {buildResultMessage}");
            summaryWriter.WriteLine("");

            if (buildReport.summary.totalErrors > 0)
            {
                summaryWriter.WriteLine($"Errors: {buildReport.summary.totalErrors}");
            }

            if (buildReport.summary.totalWarnings > 0)
            {
                summaryWriter.WriteLine($"Warnings: {buildReport.summary.totalWarnings}");
            }

            summaryWriter.WriteLine($"Total duration: {stopwatch.Elapsed:g}");
            summaryWriter.WriteLine($"Size: {FormatFileSize(buildReport.summary.totalSize)}");
            summaryWriter.WriteLine("");
            summaryWriter.WriteLine("## Logs");
            summaryWriter.WriteLine("");

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


            summaryWriter.WriteLine("| log type | message |");
            summaryWriter.WriteLine("| -------- | ------- |");

            foreach (var step in buildReport.steps)
            {
                stepNumber++;
                totalBuildTime += step.duration;

                var nameIndex = step.name.IndexOf("=", StringComparison.Ordinal);

                if (nameIndex < 0)
                {
                    nameIndex = step.name.Length;
                }

                var buildStepMessage = $"{stepNumber}. {step.name[..nameIndex]}";
                Debug.Log(buildStepMessage);

                var hasMessages = step.messages.Length > 0;

                if (!hasMessages)
                {
                    continue;
                }

                foreach (var message in step.messages)
                {
                    var logMessage = message.content.Replace("\n", string.Empty);
                    logMessage = logMessage.Replace("\r", string.Empty);
                    logMessage = logMessage.Replace(Error, string.Empty);
                    logMessage = logMessage.Replace(Warning, string.Empty);
                    logMessage = logMessage.Replace(ErrorColor, string.Empty);
                    logMessage = logMessage.Replace(WarningColor, string.Empty);
                    logMessage = logMessage.Replace(ResetColor, string.Empty);
                    logMessage = logMessage.Replace(LogColor, string.Empty);

                    switch (message.type)
                    {
                        case LogType.Error:
                        case LogType.Assert:
                        case LogType.Exception:
                            summaryWriter.WriteLine($"| :boom: {message.type} | {logMessage} |");
                            break;
                        case LogType.Warning:
                            summaryWriter.WriteLine($"| :warning: {message.type} | {logMessage} |");
                            break;
                        case LogType.Log:
                            summaryWriter.WriteLine($"| {message.type} | {logMessage} |");
                            break;
                    }

                    switch (message.type)
                    {
                        case LogType.Error:
                        case LogType.Assert:
                        case LogType.Exception:
                            Debug.Log($"{Error}{ErrorColor}{logMessage}{ResetColor}");
                            break;
                        case LogType.Warning:
                            Debug.Log($"{Warning}{WarningColor}{logMessage}{ResetColor}");
                            break;
                        case LogType.Log:
                            Debug.Log($"{logMessage}");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            summaryWriter.Close();
            summaryWriter.Dispose();
            CILoggingUtility.LoggingEnabled = true;
        }
    }
}
