// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utilities.Editor.BuildPipeline.Logging
{
    /// <summary>
    /// Base abstract logger to use when creating custom loggers.
    /// </summary>
    public abstract class AbstractCILogger : ICILogger
    {
        private readonly ILogHandler defaultLogger;

        protected AbstractCILogger()
        {
            defaultLogger = Debug.unityLogger.logHandler;
            Debug.unityLogger.logHandler = this;
        }

        /// <inheritdoc />
        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            var skip = false;

            foreach (var arg in args)
            {
                if (arg is string message)
                {
                    skip = string.IsNullOrWhiteSpace(message) ||
                           CILoggingUtility.IgnoredLogs.Any(message.Contains);
                }
            }

            if (CILoggingUtility.LoggingEnabled && !skip)
            {
                switch (logType)
                {
                    case LogType.Log:
                        format = $"\n{Log}{LogColor}{format}{ResetColor}";
                        break;
                    case LogType.Assert:
                    case LogType.Error:
                    case LogType.Exception:
                        format = $"\n{Error}{ErrorColor}{format}{ResetColor}";
                        break;
                    case LogType.Warning:
                        format = $"\n{Warning}{WarningColor}{format}{WarningColor}";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
                }
            }

            defaultLogger.LogFormat(logType, context, format, args);
        }

        /// <inheritdoc />
        public void LogException(Exception exception, Object context)
        {
            if (CILoggingUtility.LoggingEnabled)
            {
                exception = new Exception($"\n{Error}{exception.Message}", exception);
            }

            defaultLogger.LogException(exception, context);
        }

        /// <inheritdoc />
        public virtual string Log => string.Empty;

        /// <inheritdoc />
        public virtual string LogColor => @"\`e[32m";

        /// <inheritdoc />
        public virtual string Warning => string.Empty;

        /// <inheritdoc />
        public virtual string WarningColor => @"\`e[0;33m";

        /// <inheritdoc />
        public virtual string Error => @"\`e[0;31m";

        /// <inheritdoc />
        public virtual string ErrorColor => @"\`e[0;31m;";

        /// <inheritdoc />
        public virtual string ResetColor => @"\`e[0m";
    }
}
