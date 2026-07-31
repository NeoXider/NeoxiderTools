using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_6000_5_OR_NEWER
// WHY: Unity 6.5 made EntityId->int obsolete-as-error; key the cache by EntityId itself there.
using StableObjectId = UnityEngine.EntityId;
#else
using StableObjectId = System.Int32;
#endif

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
        internal const string SessionKey = "Neo.ComponentHealth.v1";

        // WHY: Unity prints managed frames as "Namespace.Type:Method (" or "at Namespace.Type.Method (".
        private static readonly Regex StackTypePattern = new(
            @"(?:^|\n)\s*(?:at\s+)?([A-Za-z_][\w]*(?:\.[A-Za-z_][\w]*)+)[.:][A-Za-z_][\w]*\s*\(",
            RegexOptions.Compiled);

        private static readonly Dictionary<string, ErrorRecord> ErrorsByType = new(StringComparer.Ordinal);
        private static readonly Dictionary<StableObjectId, ValidationEntry> ValidationCache = new();

        private const double ConsolePollInterval = 0.5;

        private static string _lastCondition;
        private static double _lastConditionAt;
        private static bool _persistScheduled;
        private static double _nextConsolePollAt;

        static NeoComponentHealth()
        {
            Application.logMessageReceived -= OnLogMessage;
            Application.logMessageReceived += OnLogMessage;
            ConsoleWindowUtility.consoleLogsChanged -= OnConsoleLogsChanged;
            ConsoleWindowUtility.consoleLogsChanged += OnConsoleLogsChanged;
            // WHY: consoleLogsChanged is raised by the Console window; with the console hidden behind
            // another tab it never fires, so a throttled O(1) count poll is the reliable backstop.
            EditorApplication.update -= PollConsoleCounts;
            EditorApplication.update += PollConsoleCounts;
            Restore();
            // WHY: No extra startup reconcile needed — the poll's first tick after Restore() covers
            // the "console cleared before the domain reload" case (its throttle starts at zero).
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
                ValidationCache.Remove(StableId(target));
            }
        }

        // WHY: Unity 6.5 deprecated GetInstanceID / objectReferenceInstanceIDValue (obsolete-as-error) in
        // favour of the EntityId APIs; keep the package compiling on both 6.0-6.4 and 6.5+. On 6.5 the
        // EntityId->int cast is itself obsolete, so key the cache by EntityId and never cast it to int.
        private static StableObjectId StableId(Object target)
        {
#if UNITY_6000_5_OR_NEWER
            return target.GetEntityId();
#else
            return target.GetInstanceID();
#endif
        }

        private static bool HasDanglingReference(SerializedProperty prop)
        {
#if UNITY_6000_5_OR_NEWER
            return prop.objectReferenceValue == null && prop.objectReferenceEntityIdValue.IsValid();
#else
            return prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0;
#endif
        }

        private static void OnConsoleLogsChanged()
        {
            ConsoleWindowUtility.GetConsoleLogCounts(out int errors, out _, out _);
            SyncConsoleErrorCount(errors);
        }

        private static void PollConsoleCounts()
        {
            if (ErrorsByType.Count == 0)
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now < _nextConsolePollAt)
            {
                return;
            }

            _nextConsolePollAt = now + ConsolePollInterval;
            OnConsoleLogsChanged();
        }

        /// <summary>
        ///     Mirrors the console: once the visible error count reaches zero, every remembered error
        ///     is forgotten (memory + SessionState) so the mascot never stays angry about messages the
        ///     user already cleared. Returns true when remembered errors were wiped.
        /// </summary>
        internal static bool SyncConsoleErrorCount(int consoleErrors)
        {
            // WHY: O(1) during error storms — only the zero-crossing with non-empty memory does work.
            if (consoleErrors > 0 || ErrorsByType.Count == 0)
            {
                return false;
            }

            ErrorsByType.Clear();
            SessionState.EraseString(SessionKey);
            // WHY: Editors read health lazily; without a repaint the mascot stays angry on screen
            // until the user next moves the mouse over the inspector.
            InternalEditorUtility.RepaintAllViews();
            return true;
        }

        /// <summary>Test hook: resets remembered errors, throttle state and persisted session data.</summary>
        internal static void ResetForTests()
        {
            ErrorsByType.Clear();
            ValidationCache.Clear();
            _lastCondition = null;
            _lastConditionAt = 0.0;
            SessionState.EraseString(SessionKey);
        }

        internal static void OnLogMessage(string condition, string stackTrace, LogType type)
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
                    typeName.StartsWith("NUnit", StringComparison.Ordinal) ||
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
            StableObjectId id = StableId(target);
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
                            if (HasDanglingReference(prop))
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
