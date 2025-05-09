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
            if (buildReport == null)
            {
                Debug.LogError("Build report is null. Cannot generate Git Hub Build Summary!");
                return;
            }

            // temp disable logging to get the right messages sent.
            CILoggingUtility.LoggingEnabled = false;
            var summary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (summary == null) { return; }

            using (var summaryWriter = new StreamWriter(summary, true, Encoding.UTF8))
            {
                try
                {
                    var buildResultMessage = $"Unity {buildReport.summary.platform} Build {buildReport.summary.result}!";
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
                    summaryWriter.WriteLine($"Output Path: {buildReport.summary.outputPath}");
                    summaryWriter.WriteLine("");

                    var totalBuildTime = TimeSpan.Zero;
                    var logs = new List<string>();

                    foreach (var step in buildReport.steps)
                    {
                        totalBuildTime += step.duration;
                        var hasMessages = step.messages.Length > 0;

                        if (!hasMessages)
                        {
                            continue;
                        }

                        var errorMessages = step.messages
                            .Where(message => message.type == LogType.Error || message.type == LogType.Assert || message.type == LogType.Exception)
                            .ToList();

                        var warningMessages = step.messages
                            .Where(message => message.type == LogType.Warning)
                            .ToList();

                        var logMessages = step.messages
                            .Where(message => message.type == LogType.Log)
                            .ToList();

                        var sortedMessages = new List<BuildStepMessage>();
                        sortedMessages.AddRange(errorMessages);
                        sortedMessages.AddRange(warningMessages);
                        sortedMessages.AddRange(logMessages);

                        foreach (var message in sortedMessages)
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
                                    logs.Add($"| :boom: {message.type} | {logMessage} |");

                                    break;
                                case LogType.Warning:
                                    logs.Add($"| :warning: {message.type} | {logMessage} |");

                                    break;
                                case LogType.Log:
                                default:
                                    logs.Add($"| {message.type} | {logMessage} |");

                                    break;
                            }
                        }
                    }

                    if (logs.Count > 0)
                    {
                        var hasErrors = logs.Any(log => log.Contains($"| :boom: {LogType.Error} |"));

                        summaryWriter.WriteLine(hasErrors
                            ? "<details open><summary>Logs</summary>"
                            : "<details><summary>Logs</summary>");

                        summaryWriter.WriteLine("");
                        summaryWriter.WriteLine("| log type | message |");
                        summaryWriter.WriteLine("| -------- | ------- |");

                        foreach (var log in logs)
                        {
                            summaryWriter.WriteLine(log);
                        }

                        summaryWriter.WriteLine("</details>");
                        summaryWriter.WriteLine("");
                    }

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
