// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Utilities.Editor.BuildPipeline.Logging
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
        /// Error message prefix.
        /// </summary>
        string Error { get; }

        /// <summary>
        /// Warning message prefix.
        /// </summary>
        string Warning { get; }
    }
}
