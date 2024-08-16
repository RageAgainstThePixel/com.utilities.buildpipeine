// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Buildalon.Editor.BuildPipeline.Logging
{
    /// <summary>
    /// CI Logging interface.
    /// </summary>
    public interface ICILogger : ILogHandler
    {
        /// <summary>
        /// Log message prefix.
        /// </summary>
        string Log { get; }

        /// <summary>
        /// ANSI Log color code.
        /// </summary>
        string LogColor { get; }

        /// <summary>
        /// Warning message prefix.
        /// </summary>
        string Warning { get; }

        /// <summary>
        /// ANSI Warning color code.
        /// </summary>
        string WarningColor { get; }

        /// <summary>
        /// Error message prefix.
        /// </summary>
        string Error { get; }

        /// <summary>
        /// ANSI Error color code.
        /// </summary>
        string ErrorColor { get; }

        /// <summary>
        /// ANSI Reset color code.
        /// </summary>
        string ResetColor { get; }

        /// <summary>
        /// Generate a nicely formatted build summary for the CI Logger.
        /// </summary>
        /// <param name="buildReport"><see cref="BuildReport"/>.</param>
        /// <param name="stopwatch"></param>
        void GenerateBuildSummary(BuildReport buildReport, Stopwatch stopwatch);
    }
}
