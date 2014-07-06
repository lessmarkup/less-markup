/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Timers;
using LessMarkup.Engine.Helpers;
using LessMarkup.Interfaces.Exceptions;

namespace LessMarkup.Engine.Logging
{
    public static class LoggingHelper
    {
        private static LogLevel _logLevel = LogLevel.Error;
        private static readonly string _logFolder;
        private static readonly object _syncWrite = new object();
        private static readonly List<string> _logCache = new List<string>();
        private static readonly Timer _timer;

        static LoggingHelper()
        {
            _logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LessMarkup");
            Directory.CreateDirectory(_logFolder);

            _timer = new Timer(1000);
            _timer.Elapsed += (sender, args) => SyncCache();

            GC.KeepAlive(_timer);

            ResetTimer();
        }

        private static void ResetTimer()
        {
            lock (_syncWrite)
            {
                _timer.Enabled = false;

                if (_logLevel == LogLevel.None)
                {
                    return;
                }

                _timer.Enabled = true;
            }
        }

        public static LogLevel Level
        {
            get { return _logLevel; }
            set
            {
                _logLevel = value;
                ResetTimer();
            }
        }

        public static void LogDebug(this object sender, string message, [CallerMemberName] string methodName = null)
        {
            // ReSharper disable ExplicitCallerInfoArgument
            Log(sender, LogLevel.Debug, message, methodName);
            // ReSharper restore ExplicitCallerInfoArgument
        }

        public static void LogWarning(this object sender, string message, [CallerMemberName] string methodName = null)
        {
            // ReSharper disable ExplicitCallerInfoArgument
            Log(sender, LogLevel.Warning, message, methodName);
            // ReSharper restore ExplicitCallerInfoArgument
        }

        public static void LogError(this object sender, string message, [CallerMemberName] string methodName = null)
        {
            // ReSharper disable ExplicitCallerInfoArgument
            Log(sender, LogLevel.Error, message, methodName);
            // ReSharper restore ExplicitCallerInfoArgument
        }

        private static void LogExceptionRecursive(Exception exception, List<string> lines)
        {
            lines.Add((lines.Count > 0 ? "Inner Exception: ": "Exception: ") + exception.GetType().Name + " / " + exception.Message);
            var extendedMessageException = exception as ExtendedMessageException;
            if (extendedMessageException != null)
            {
                lines.Add(extendedMessageException.ExtendedMessage);
            }
            if (!string.IsNullOrWhiteSpace(exception.StackTrace))
            {
                lines.Add("Stack Trace\r\n" + exception.StackTrace);
            }
            if (exception.InnerException != null)
            {
                LogExceptionRecursive(exception.InnerException, lines);
            }
        }

        public static void Flush()
        {
            SyncCache();
        }

        public static void LogException(this object sender, Exception exception, [CallerMemberName] string methodName = null)
        {
            if (exception == null)
            {
                return;
            }
            StatisticsHelper.FlagError(exception.Message);
            var lines = new List<string>();
            LogExceptionRecursive(exception, lines);
            LogLines(sender, LogLevel.Error, lines, methodName);
        }

        private static void LogLines(this object sender, LogLevel logLevel, IEnumerable<string> lines, string methodName)
        {
            if (logLevel > _logLevel)
            {
                return;
            }

            string start = string.Format("[{0}] [{1}/{2}] [{3}] ", DateTime.Now.ToString("HH:mm:ss.fff"), sender.GetType().Name, methodName, logLevel);

            lock (_syncWrite)
            {
                foreach (var line in lines)
                {
                    _logCache.Add(start + line);
                }
            }
        }

        public static void Log(this object sender, LogLevel logLevel, string message, [CallerMemberName] string methodName = null)
        {
            if (logLevel > _logLevel)
            {
                return;
            }

            string fullMessage = string.Format("[{0}] [{1}/{2}] [{3}] {4}", DateTime.Now.ToString("HH:mm:ss.fff"),
                sender.GetType().Name, methodName, logLevel, message);

            lock (_syncWrite)
            {
                _logCache.Add(fullMessage);
            }
        }

        private static void SyncCache()
        {
            if (_logCache.Count == 0)
            {
                return;
            }

            lock (_syncWrite)
            {
                var logFilePath = Path.Combine(_logFolder, string.Format("Engine.{0}.Log", DateTime.Now.ToString("yyyyMMdd")));
                File.AppendAllLines(logFilePath, _logCache);
                _logCache.Clear();
            }
        }
    }
}
