using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
///     Shared runtime diagnostics gate for Neoxider modules.
///     Info and warning output is disabled by default to keep package code quiet in consuming projects.
/// </summary>
public static class NeoDiagnostics
{
    private static readonly Dictionary<string, float> LastMessageTimes = new();

    public static bool RuntimeLogsEnabled { get; private set; }
    public static bool RuntimeWarningsEnabled { get; private set; }
    public static bool RuntimeErrorsEnabled { get; private set; } = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStaticState()
    {
        RuntimeLogsEnabled = false;
        RuntimeWarningsEnabled = false;
        RuntimeErrorsEnabled = true;
        LastMessageTimes.Clear();
    }

    public static void Configure(bool? logs = null, bool? warnings = null, bool? errors = null)
    {
        if (logs.HasValue)
        {
            RuntimeLogsEnabled = logs.Value;
        }

        if (warnings.HasValue)
        {
            RuntimeWarningsEnabled = warnings.Value;
        }

        if (errors.HasValue)
        {
            RuntimeErrorsEnabled = errors.Value;
        }
    }

    public static void Log(string message, Object context = null, bool force = false)
    {
        if (!force && !RuntimeLogsEnabled)
        {
            return;
        }

        Debug.Log(message, context);
    }

    public static void LogWarning(string message, Object context = null, bool force = false)
    {
        if (!force && !RuntimeWarningsEnabled)
        {
            return;
        }

        Debug.LogWarning(message, context);
    }

    public static void LogWarningThrottled(string key, string message, Object context = null, float seconds = 1f,
        bool force = false)
    {
        if (!force && !RuntimeWarningsEnabled)
        {
            return;
        }

        string resolvedKey = string.IsNullOrEmpty(key) ? message : key;
        float now = Time.realtimeSinceStartup;
        if (seconds > 0f &&
            LastMessageTimes.TryGetValue(resolvedKey, out float lastTime) &&
            now - lastTime < seconds)
        {
            return;
        }

        LastMessageTimes[resolvedKey] = now;
        Debug.LogWarning(message, context);
    }

    public static void LogError(string message, Object context = null, bool force = false)
    {
        if (!force && !RuntimeErrorsEnabled)
        {
            return;
        }

        Debug.LogError(message, context);
    }

    public static void LogException(Exception exception, Object context = null, bool force = false)
    {
        if (exception == null || (!force && !RuntimeErrorsEnabled))
        {
            return;
        }

        Debug.LogException(exception, context);
    }
}
