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
    /// <summary>
    ///     Upper bound for keys with their own throttle window. Keys built from per-object ids would otherwise grow
    ///     the table for the whole session. A running window is never forgotten. A new key arriving at the bound
    ///     takes the slot of a window that has already elapsed; when every window is still running, it shares one
    ///     overflow window with all other such keys: the first of them logs, the rest are counted and the count is
    ///     appended to the next overflow warning.
    /// </summary>
    public const int MaxThrottleKeys = 1024;

    private static readonly Dictionary<string, ThrottleWindow> LastMessageTimes = new();
    private static readonly List<string> EvictedThrottleKeys = new();

    /// <summary>Earliest time a remembered window can elapse; the table is only scanned for free slots from then on.</summary>
    private static float _nextThrottleExpiry = float.PositiveInfinity;

    private static float _overflowLastTime = float.NegativeInfinity;
    private static int _overflowSuppressedCount;

    /// <summary>True when <see cref="Log" /> output is enabled. Off by default.</summary>
    public static bool RuntimeLogsEnabled { get; private set; }

    /// <summary>
    ///     True when <see cref="LogWarning" /> and <see cref="LogWarningThrottled" /> output is enabled. Off by default.
    ///     Check it before building an expensive warning message.
    /// </summary>
    public static bool RuntimeWarningsEnabled { get; private set; }

    /// <summary>True when <see cref="LogError" /> and <see cref="LogException" /> output is enabled. On by default.</summary>
    public static bool RuntimeErrorsEnabled { get; private set; } = true;

    /// <summary>Restores the defaults (logs and warnings off, errors on) and forgets every throttle window.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStaticState()
    {
        RuntimeLogsEnabled = false;
        RuntimeWarningsEnabled = false;
        RuntimeErrorsEnabled = true;
        LastMessageTimes.Clear();
        EvictedThrottleKeys.Clear();
        _nextThrottleExpiry = float.PositiveInfinity;
        _overflowLastTime = float.NegativeInfinity;
        _overflowSuppressedCount = 0;
    }

    /// <summary>Enables or disables output channels; a null argument leaves that channel unchanged.</summary>
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

    /// <summary>Info log, emitted only when <see cref="RuntimeLogsEnabled" /> or <paramref name="force" /> is set.</summary>
    public static void Log(string message, Object context = null, bool force = false)
    {
        if (!force && !RuntimeLogsEnabled)
        {
            return;
        }

        Debug.Log(message, context);
    }

    /// <summary>Warning, emitted only when <see cref="RuntimeWarningsEnabled" /> or <paramref name="force" /> is set.</summary>
    public static void LogWarning(string message, Object context = null, bool force = false)
    {
        if (!force && !RuntimeWarningsEnabled)
        {
            return;
        }

        Debug.LogWarning(message, context);
    }

    /// <summary>
    ///     Stable per-object identity for throttle keys and diagnostic text.
    /// </summary>
    // WHY: Unity 6.5 made Object.GetInstanceID obsolete-as-error in favour of GetEntityId, and on 6.5 the
    // EntityId->int cast is obsolete too. One switch here instead of one per call site: the per-site
    // version already went stale once - new code missed it and broke the build in a 6.5 project.
    public static string StableId(Object target)
    {
        if (target == null)
        {
            return "null";
        }

#if UNITY_6000_5_OR_NEWER
        return target.GetEntityId().ToString();
#else
        return target.GetInstanceID().ToString();
#endif
    }

    /// <summary>
    ///     Gated warning that repeats at most once per <paramref name="seconds" /> (realtime) for the same
    ///     <paramref name="key" />. A null or empty key throttles by the message text; a zero, negative or NaN
    ///     window does not throttle. While <see cref="MaxThrottleKeys" /> windows are running, every other key shares
    ///     one overflow window; the number of warnings it suppressed is appended to the next throttled warning that
    ///     logs.
    /// </summary>
    public static void LogWarningThrottled(string key, string message, Object context = null, float seconds = 1f,
        bool force = false)
    {
        if (!force && !RuntimeWarningsEnabled)
        {
            return;
        }

        if (float.IsNaN(seconds))
        {
            seconds = 0f;
        }

        string resolvedKey = string.IsNullOrEmpty(key) ? message : key;
        float now = Time.realtimeSinceStartup;
        if (LastMessageTimes.TryGetValue(resolvedKey, out ThrottleWindow window))
        {
            if (seconds > 0f && now - window.LastTime < seconds)
            {
                return;
            }
        }
        else if (LastMessageTimes.Count >= MaxThrottleKeys && !TryFreeElapsedThrottleWindows(now))
        {
            LogOverflowWarning(message, context, seconds, now);
            return;
        }

        LastMessageTimes[resolvedKey] = new ThrottleWindow(now, seconds);
        float expiry = now + seconds;
        if (expiry < _nextThrottleExpiry)
        {
            _nextThrottleExpiry = expiry;
        }

        EmitThrottledWarning(message, context);
    }

    private static bool TryFreeElapsedThrottleWindows(float now)
    {
        // WHY: a running window is never forgotten. Clearing the table, or evicting the least recently logged key,
        // lets a burst that cycles through more keys than the bound log every key again on every pass. The O(n)
        // scan only runs once the earliest remembered window can have elapsed, so a sustained overflow stays O(1).
        if (now < _nextThrottleExpiry)
        {
            return false;
        }

        float nextExpiry = float.PositiveInfinity;
        EvictedThrottleKeys.Clear();
        foreach (KeyValuePair<string, ThrottleWindow> pair in LastMessageTimes)
        {
            float expiry = pair.Value.LastTime + pair.Value.Seconds;
            if (now >= expiry)
            {
                EvictedThrottleKeys.Add(pair.Key);
            }
            else if (expiry < nextExpiry)
            {
                nextExpiry = expiry;
            }
        }

        for (int i = 0; i < EvictedThrottleKeys.Count; i++)
        {
            LastMessageTimes.Remove(EvictedThrottleKeys[i]);
        }

        bool freed = EvictedThrottleKeys.Count > 0;
        EvictedThrottleKeys.Clear();
        _nextThrottleExpiry = nextExpiry;
        return freed;
    }

    private static void LogOverflowWarning(string message, Object context, float seconds, float now)
    {
        if (seconds > 0f)
        {
            if (now - _overflowLastTime < seconds)
            {
                _overflowSuppressedCount++;
                return;
            }

            _overflowLastTime = now;
        }

        EmitThrottledWarning(message, context);
    }

    private static void EmitThrottledWarning(string message, Object context)
    {
        if (_overflowSuppressedCount == 0)
        {
            Debug.LogWarning(message, context);
            return;
        }

        int suppressed = _overflowSuppressedCount;
        _overflowSuppressedCount = 0;
        Debug.LogWarning(message + "\n(NeoDiagnostics: " + suppressed.ToString() +
                         " more throttled warning(s) were suppressed while all " + MaxThrottleKeys.ToString() +
                         " throttle windows were running.)", context);
    }

    /// <summary>Error, emitted while <see cref="RuntimeErrorsEnabled" /> (default) or when <paramref name="force" /> is set.</summary>
    public static void LogError(string message, Object context = null, bool force = false)
    {
        if (!force && !RuntimeErrorsEnabled)
        {
            return;
        }

        Debug.LogError(message, context);
    }

    /// <summary>Exception log through the error gate; a null exception is ignored.</summary>
    public static void LogException(Exception exception, Object context = null, bool force = false)
    {
        if (exception == null || (!force && !RuntimeErrorsEnabled))
        {
            return;
        }

        Debug.LogException(exception, context);
    }

    /// <summary>One remembered throttle window: when the key last logged and the window it logged with.</summary>
    private readonly struct ThrottleWindow
    {
        public readonly float LastTime;
        public readonly float Seconds;

        public ThrottleWindow(float lastTime, float seconds)
        {
            LastTime = lastTime;
            Seconds = seconds;
        }
    }
}
