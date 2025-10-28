using System;
using System.Collections.Generic;
using Serilog.Events;
using UnityEngine;

namespace Neo.Runtime.Logging
{
    /// <summary>
    /// Configuration for application logging system.
    /// Controls log levels, outputs, and filtering.
    /// </summary>
    [CreateAssetMenu(fileName = "LoggingConfig", menuName = "Neo/Core/Logging Config")]
    public class LoggingConfig : ScriptableObject
    {
        [Header("General Settings")]
        [Tooltip("Minimum log level to display")]
        public LogEventLevel minimumLevel = LogEventLevel.Debug;

        [Header("Output Settings")]
        [Tooltip("Enable logging to Unity Console")]
        public bool enableUnityConsole = true;

        [Tooltip("Enable logging to file in persistent data path")]
        public bool enableFileLogging = false;

        [Header("File Settings")]
        [Tooltip("Maximum file size in MB before rolling")]
        [Range(1, 100)]
        public int maxFileSizeMB = 10;

        [Tooltip("Number of days to keep old log files")]
        [Range(1, 30)]
        public int retainedFileCountLimit = 7;

        [Header("Filtering")]
        [Tooltip("Namespaces to enable logging for (empty = all)")]
        public List<string> enabledNamespaces = new List<string>();

        [Tooltip("Namespaces to disable logging for")]
        public List<string> disabledNamespaces = new List<string>();

        [Header("Advanced")]
        [Tooltip("Show source context (namespace/class) in logs")]
        public bool showSourceContext = true;

        [Tooltip("Show timestamps in Unity Console")]
        public bool showTimestamps = false;

        [Header("Unity Console Formatting")]
        [Tooltip("Enable colored output in Unity Console")]
        public bool enableColors = true;

        /// <summary>
        /// Check if logging is enabled for the given source context
        /// </summary>
        public bool IsEnabled(string sourceContext)
        {
            if (string.IsNullOrEmpty(sourceContext))
                return true;

            // Check disabled first (higher priority)
            foreach (var disabled in disabledNamespaces)
            {
                if (!string.IsNullOrEmpty(disabled) && sourceContext.StartsWith(disabled))
                    return false;
            }

            // If enabledNamespaces is empty, allow all
            if (enabledNamespaces.Count == 0)
                return true;

            // Check if explicitly enabled
            foreach (var enabled in enabledNamespaces)
            {
                if (!string.IsNullOrEmpty(enabled) && sourceContext.StartsWith(enabled))
                    return true;
            }

            return false;
        }
    }
}

