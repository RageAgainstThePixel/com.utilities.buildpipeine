// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Buildalon.Editor.BuildPipeline.Logging
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
            if (Application.isBatchMode == false)
            {
                return;
            }

            if (buildReport == null)
            {
                Debug.LogError("Build report is null. Cannot generate Git Hub Build Summary!");
                return;
            }

            // temp disable logging to get the right messages sent.
            CILoggingUtility.LoggingEnabled = false;
            var summary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");

            if (summary == null)
            {
                Debug.LogError("GITHUB_STEP_SUMMARY env var not found. Cannot generate Git Hub Build Summary!");
                return;
            }

            using (var summaryWriter = new StreamWriter(summary, true, Encoding.UTF8))
            {
                try
                {
                    var buildResultMessage = $"Unity {buildReport.summary.platform} " +
#if UNITY_6000_0_OR_NEWER
                                             $"{buildReport.summary.buildType} " +
#endif
                                             $"Build {buildReport.summary.result}!";
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

                    if (stopwatch != null)
                    {
                        summaryWriter.WriteLine($"Total duration: {stopwatch.Elapsed:g}");
                    }
                    summaryWriter.WriteLine($"Size: {FormatFileSize(buildReport.summary.totalSize)}");
                    summaryWriter.WriteLine($"Output Path: {buildReport.summary.outputPath}");
                    summaryWriter.WriteLine("");

                    var totalBuildTime = TimeSpan.Zero;
                    var errorLogs = new List<BuildStepMessage>();
                    var warningLogs = new List<BuildStepMessage>();
                    var infoLogs = new List<BuildStepMessage>();

                    foreach (var step in buildReport.steps)
                    {
                        totalBuildTime += step.duration;
                        var hasMessages = step.messages.Length > 0;

                        if (!hasMessages) { continue; }

                        errorLogs.AddRange(
                            step.messages
                                .Where(message => message.type == LogType.Error || message.type == LogType.Assert || message.type == LogType.Exception)
                                .Where(message => !CILoggingUtility.IgnoredLogs.Any(message.content.Contains)));
                        warningLogs.AddRange(
                            step.messages
                                .Where(message => message.type == LogType.Warning)
                                .Where(message => !CILoggingUtility.IgnoredLogs.Any(message.content.Contains)));
                        infoLogs.AddRange(
                            step.messages
                                .Where(message => message.type == LogType.Log)
                                .Where(message => !CILoggingUtility.IgnoredLogs.Any(message.content.Contains)));
                    }

                    string ProcessLogMessage(BuildStepMessage message)
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
                                return $"| :boom: {message.type} | {logMessage} |";
                            case LogType.Warning:
                                return $"| :warning: {message.type} | {logMessage} |";
                            case LogType.Log:
                            default:
                                return $"| {message.type} | {logMessage} |";
                        }
                    }

                    void WriteLogSummary(List<BuildStepMessage> logs)
                    {
                        if (logs.Count == 0) { return; }
                        var isErrorMessages = logs.Any(log => log.type == LogType.Error || log.type == LogType.Assert || log.type == LogType.Exception);
                        var isWarningMessages = logs.Any(log => log.type == LogType.Warning);

                        if (isErrorMessages)
                        {
                            summaryWriter.WriteLine($"<details open><summary>Errors ({logs.Count})</summary>");
                        }
                        else if (isWarningMessages)
                        {
                            summaryWriter.WriteLine($"<details><summary>Warnings ({logs.Count})</summary>");
                        }
                        else
                        {
                            summaryWriter.WriteLine($"<details><summary>Logs ({logs.Count})</summary>");
                        }

                        summaryWriter.WriteLine("");
                        summaryWriter.WriteLine("| log type | message |");
                        summaryWriter.WriteLine("| -------- | ------- |");

                        foreach (var log in logs)
                        {
                            summaryWriter.WriteLine(ProcessLogMessage(log));
                        }

                        summaryWriter.WriteLine("</details>");
                        summaryWriter.WriteLine("");
                    }

                    WriteLogSummary(errorLogs);
                    WriteLogSummary(warningLogs);
                    WriteLogSummary(infoLogs);

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
                }
                finally
                {
                    summaryWriter.Close();
                }
            }
            CILoggingUtility.LoggingEnabled = true;
        }
    }
}
