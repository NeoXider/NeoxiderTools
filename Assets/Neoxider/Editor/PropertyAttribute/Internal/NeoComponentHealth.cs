using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor
{
    /// <summary>
    ///     Health backend for the inspector mascot ("slime linter"). Two independent sources:
    ///     console errors remembered per component type (attributed via stack-trace parsing) and a
    ///     cheap cached validation scan of the selected component (missing references, NaN floats).
    ///     Designed to be effectively free: the console hook costs O(1) per error, the validation
    ///     scan runs only for inspected objects, is throttled and capped.
    /// </summary>
    [InitializeOnLoad]
    internal static class NeoComponentHealth
    {
        internal enum Mood
        {
            Ok = 0,
            Worried = 1,
            Alarmed = 2
        }

        internal struct Report
        {
            public int ConsoleErrors;
            public string LastConsoleMessage;
            public double LastConsoleAt;
            public int MissingReferences;
            public int InvalidNumbers;

            public int TotalIssues => ConsoleErrors + MissingReferences + InvalidNumbers;

            public Mood Mood =>
                ConsoleErrors > 0 || InvalidNumbers > 0 ? Mood.Alarmed :
                MissingReferences > 0 ? Mood.Worried : Mood.Ok;
        }

        private sealed class ErrorRecord
        {
            public int Count;
            public string LastMessage;
            public double LastAt;
        }

        private sealed class ValidationEntry
        {
            public int MissingReferences;
            public int InvalidNumbers;
            public double NextCheckAt;
        }

        private const int MaxTrackedTypes = 128;
        private const int MaxScannedProperties = 400;
        private const int MaxValidationCache = 512;
        private const double ValidationInterval = 2.0;
        private const string SessionKey = "Neo.ComponentHealth.v1";

        // WHY: Unity prints managed frames as "Namespace.Type:Method (" or "at Namespace.Type.Method (".
        private static readonly Regex StackTypePattern = new(
            @"(?:^|\n)\s*(?:at\s+)?([A-Za-z_][\w]*(?:\.[A-Za-z_][\w]*)+)[.:][A-Za-z_][\w]*\s*\(",
            RegexOptions.Compiled);

        private static readonly Dictionary<string, ErrorRecord> ErrorsByType = new(StringComparer.Ordinal);
        private static readonly Dictionary<int, ValidationEntry> ValidationCache = new();

        private static string _lastCondition;
        private static double _lastConditionAt;
        private static bool _persistScheduled;

        static NeoComponentHealth()
        {
            Application.logMessageReceived -= OnLogMessage;
            Application.logMessageReceived += OnLogMessage;
            Restore();
        }

        /// <summary>Combined report for one inspected object. Cheap: dictionary lookup + throttled scan.</summary>
        internal static Report GetReport(Object target)
        {
            var report = new Report();
            if (target == null)
            {
                return report;
            }

            if (ErrorsByType.TryGetValue(target.GetType().FullName ?? string.Empty, out ErrorRecord record))
            {
                report.ConsoleErrors = record.Count;
                report.LastConsoleMessage = record.LastMessage;
                report.LastConsoleAt = record.LastAt;
            }

            ValidationEntry validation = GetValidation(target);
            report.MissingReferences = validation.MissingReferences;
            report.InvalidNumbers = validation.InvalidNumbers;
            return report;
        }

        /// <summary>Forgets remembered console errors for one component type.</summary>
        internal static void ClearConsoleErrors(Type type)
        {
            if (type?.FullName != null && ErrorsByType.Remove(type.FullName))
            {
                SchedulePersist();
            }
        }

        /// <summary>Forces the next validation scan for an object (e.g. after an undo).</summary>
        internal static void InvalidateValidation(Object target)
        {
            if (target != null)
            {
                ValidationCache.Remove(target.GetInstanceID());
            }
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            // WHY: Error storms (a broken Update spamming per frame) must not cost regex time per line.
            double now = EditorApplication.timeSinceStartup;
            if (condition == _lastCondition && now - _lastConditionAt < 0.25)
            {
                _lastConditionAt = now;
                return;
            }

            _lastCondition = condition;
            _lastConditionAt = now;

            if (string.IsNullOrEmpty(stackTrace))
            {
                return;
            }

            string head = stackTrace.Length > 1400 ? stackTrace.Substring(0, 1400) : stackTrace;
            string message = condition.Length > 180 ? condition.Substring(0, 180) : condition;
            message = message.Replace('\n', ' ').Replace('|', '/');

            int attributed = 0;
            // WHY: One error must count once per type even when several stack frames belong to it.
            var seenTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in StackTypePattern.Matches(head))
            {
                string typeName = match.Groups[1].Value;
                if (typeName.StartsWith("UnityEngine", StringComparison.Ordinal) ||
                    typeName.StartsWith("UnityEditor", StringComparison.Ordinal) ||
                    typeName.StartsWith("System", StringComparison.Ordinal) ||
                    !seenTypes.Add(typeName))
                {
                    continue;
                }

                if (!ErrorsByType.TryGetValue(typeName, out ErrorRecord record))
                {
                    if (ErrorsByType.Count >= MaxTrackedTypes)
                    {
                        TrimOldest();
                    }

                    record = new ErrorRecord();
                    ErrorsByType[typeName] = record;
                }

                record.Count++;
                record.LastMessage = message;
                record.LastAt = now;

                if (++attributed >= 4)
                {
                    break;
                }
            }

            if (attributed > 0)
            {
                SchedulePersist();
            }
        }

        private static ValidationEntry GetValidation(Object target)
        {
            int id = target.GetInstanceID();
            double now = EditorApplication.timeSinceStartup;
            if (ValidationCache.TryGetValue(id, out ValidationEntry entry) && now < entry.NextCheckAt)
            {
                return entry;
            }

            if (entry == null)
            {
                if (ValidationCache.Count >= MaxValidationCache)
                {
                    ValidationCache.Clear();
                }

                entry = new ValidationEntry();
                ValidationCache[id] = entry;
            }

            entry.NextCheckAt = now + ValidationInterval;
            entry.MissingReferences = 0;
            entry.InvalidNumbers = 0;

            try
            {
                using var so = new SerializedObject(target);
                SerializedProperty prop = so.GetIterator();
                int visited = 0;
                bool enterChildren = true;
                while (prop.Next(enterChildren) && visited++ < MaxScannedProperties)
                {
                    // WHY: Strings/arrays of primitives cannot hold refs; skipping their children keeps the cap meaningful.
                    enterChildren = prop.propertyType == SerializedPropertyType.Generic;

                    switch (prop.propertyType)
                    {
                        case SerializedPropertyType.ObjectReference:
                            if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                            {
                                entry.MissingReferences++;
                            }

                            break;

                        case SerializedPropertyType.Float:
                            if (float.IsNaN(prop.floatValue) || float.IsInfinity(prop.floatValue))
                            {
                                entry.InvalidNumbers++;
                            }

                            break;
                    }
                }
            }
            catch
            {
                // WHY: A half-destroyed target during teardown must not break the inspector chrome.
            }

            return entry;
        }

        private static void TrimOldest()
        {
            string oldestKey = null;
            double oldestAt = double.MaxValue;
            foreach (KeyValuePair<string, ErrorRecord> kv in ErrorsByType)
            {
                if (kv.Value.LastAt < oldestAt)
                {
                    oldestAt = kv.Value.LastAt;
                    oldestKey = kv.Key;
                }
            }

            if (oldestKey != null)
            {
                ErrorsByType.Remove(oldestKey);
            }
        }

        private static void SchedulePersist()
        {
            if (_persistScheduled)
            {
                return;
            }

            _persistScheduled = true;
            EditorApplication.delayCall += () =>
            {
                _persistScheduled = false;
                Persist();
            };
        }

        // WHY: SessionState survives domain reloads (play mode, recompile) but resets with the editor,
        // matching "remembered for this session" semantics.
        private static void Persist()
        {
            var sb = new StringBuilder(ErrorsByType.Count * 64);
            foreach (KeyValuePair<string, ErrorRecord> kv in ErrorsByType)
            {
                sb.Append(kv.Key).Append('|')
                    .Append(kv.Value.Count).Append('|')
                    .Append(kv.Value.LastMessage ?? string.Empty)
                    .Append('\n');
            }

            SessionState.SetString(SessionKey, sb.ToString());
        }

        private static void Restore()
        {
            string data = SessionState.GetString(SessionKey, string.Empty);
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            foreach (string line in data.Split('\n'))
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                string[] parts = line.Split('|');
                if (parts.Length < 3 || !int.TryParse(parts[1], out int count))
                {
                    continue;
                }

                ErrorsByType[parts[0]] = new ErrorRecord
                {
                    Count = count,
                    LastMessage = parts[2],
                    LastAt = 0.0
                };
            }
        }
    }
}
