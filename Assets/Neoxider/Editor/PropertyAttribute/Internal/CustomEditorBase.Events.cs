using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    public abstract partial class CustomEditorBase
    {
        /// <summary>
        ///     Collects every UnityEvent field of the inspected object and draws them as the collapsible "Events"
        ///     section. For module editors that draw their own UI via <see cref="UseCustomNeoxiderInspectorGUI" />.
        /// </summary>
        protected void DrawCollapsibleUnityEvents()
        {
            if (serializedObject == null || target == null)
            {
                return;
            }

            List<SerializedProperty> unityEvents = GetPropertyPlan().UnityEvents;
            if (unityEvents.Count > 0)
            {
                NeoInspectorStyles.Space(4);
                DrawUnityEventsFoldout(unityEvents);
            }
        }

        /// <summary>Listener numbers and texts of one UnityEvent field, rebuilt only when the object changes.</summary>
        private sealed class EventStats
        {
            public int Calls;
            public int Broken;
            public readonly GUIContent Label = new();
            public string Preview;
            public string Warning;
            public string DisplayName;
            public string PropertyPath;
        }

        private readonly Dictionary<string, EventStats> _eventStats = new(StringComparer.Ordinal);
        private readonly List<EventStats> _eventStatsInOrder = new();
        private List<SerializedProperty> _eventStatsSource;
        private int _eventStatsDirtyCount = -1;
        private double _eventStatsAt = -1000.0;
        private int _eventTotalListeners;
        private int _eventTotalBroken;
        private string _eventSummary;

        // WHY: every collapsed Events header still needs the listener totals, and each total walked every persistent
        // call with FindPropertyRelative (several native strings per call) on every IMGUI event. The numbers only
        // change when the object does: recount on a Layout event after the object's dirty count moved, or once a
        // second as a backstop for changes that do not mark it dirty.
        private void RefreshEventStats(List<SerializedProperty> unityEvents)
        {
            Event current = Event.current;
            bool layout = current == null || current.type == EventType.Layout;
            int dirtyCount = target != null ? EditorUtility.GetDirtyCount(target) : 0;
            double now = EditorApplication.timeSinceStartup;
            bool sameSource = ReferenceEquals(unityEvents, _eventStatsSource) &&
                              _eventStatsInOrder.Count == unityEvents.Count;
            if (sameSource && (!layout || (dirtyCount == _eventStatsDirtyCount && now - _eventStatsAt < 1.0)))
            {
                return;
            }

            _eventStatsSource = unityEvents;
            _eventStatsDirtyCount = dirtyCount;
            _eventStatsAt = now;
            _eventStatsInOrder.Clear();
            _eventTotalListeners = 0;
            _eventTotalBroken = 0;

            for (int i = 0; i < unityEvents.Count; i++)
            {
                SerializedProperty p = unityEvents[i];
                string path = p != null ? p.propertyPath : string.Empty;
                if (!_eventStats.TryGetValue(path, out EventStats stats))
                {
                    stats = new EventStats { PropertyPath = path };
                    _eventStats[path] = stats;
                }

                _eventStatsInOrder.Add(stats);
                if (p == null)
                {
                    continue;
                }

                int calls = GetPersistentCallCount(p);
                int broken = GetBrokenPersistentCallCount(p);
                string displayName = p.displayName;
                bool textChanged = calls != stats.Calls || broken != stats.Broken ||
                                   !string.Equals(displayName, stats.DisplayName, StringComparison.Ordinal) ||
                                   stats.Label.text == null;
                stats.Calls = calls;
                stats.Broken = broken;
                stats.DisplayName = displayName;
                if (textChanged)
                {
                    stats.Label.text = broken > 0 ? $"⚠ {displayName} ({calls})" : $"{displayName} ({calls})";
                    stats.Label.tooltip = p.tooltip;
                    stats.Warning = broken > 0 ? $"{broken} broken listener(s): check Target/Method." : null;
                }

                // WHY: the preview names targets and methods, which can change without the count changing.
                stats.Preview = calls > 0 ? BuildPersistentCallPreview(p, 2) : null;
                _eventTotalListeners += calls;
                _eventTotalBroken += broken;
            }

            _eventSummary = null;
        }

        private string GetEventsSummary(int events)
        {
            return _eventSummary ??= _eventTotalBroken > 0
                ? $"{events} event field(s) • {_eventTotalListeners} listener(s) • {_eventTotalBroken} broken"
                : $"{events} event field(s) • {_eventTotalListeners} listener(s)";
        }

        private static readonly Color EventsAccent = new(0.25f, 0.82f, 0.78f, 1f);

        /// <summary>
        ///     Draws <paramref name="unityEvents" /> as the collapsible "Events" section: listener totals, a search
        ///     filter, an "Only active" toggle and a short listener preview for collapsed events.
        /// </summary>
        protected void DrawUnityEventsFoldout(List<SerializedProperty> unityEvents)
        {
            if (unityEvents == null || unityEvents.Count == 0 || target == null)
            {
                return;
            }

            RefreshEventStats(unityEvents);

            string key = GetTypeFoldoutKeys(target.GetType()).Events;
            bool current = _neoFoldouts.TryGetValue(key, out bool value) && value;
            float amount = GetFoldAmount(key, current);
            current = DrawNeoSectionHeader(current, amount, "Events", _eventTotalListeners, EventsAccent,
                "EventSystem Icon", false);
            _neoFoldouts[key] = current;
            amount = GetFoldAmount(key, current);

            if (BeginSectionBody(amount, EventsAccent, out bool fading))
            {
                DrawUnityEventsBody(unityEvents);
                EndSectionBody(fading);
            }
        }

        private void DrawUnityEventsBody(List<SerializedProperty> unityEvents)
        {
            GUIStyle summaryStyle = NeoInspectorStyles.EventsSummary(_eventTotalBroken > 0
                ? (NeoInspectorTheme.IsDark ? new Color(1f, 0.74f, 0.36f, 1f) : new Color(0.62f, 0.38f, 0.04f, 1f))
                : NeoInspectorTheme.MutedText);
            EditorGUILayout.LabelField(GetEventsSummary(unityEvents.Count), summaryStyle);
            NeoInspectorStyles.Space(2);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUIStyle searchStyle = EditorStyles.toolbarSearchField;
                GUIStyle cancelStyle =
                    GUI.skin.FindStyle("ToolbarSearchCancelButton") ??
                    GUI.skin.FindStyle("ToolbarSeachCancelButton");
                GUIContent cancelContent = GUIContent.none;
                float cancelWidth = 18f;
                if (cancelStyle == null)
                {
                    cancelStyle = EditorStyles.toolbarButton;
                    cancelContent = NeoInspectorStyles.TempContent("×");
                    cancelWidth = 20f;
                }

                _unityEventSearch ??= string.Empty;
                _unityEventSearch = GUILayout.TextField(_unityEventSearch, searchStyle, NeoInspectorStyles.MinWidth(120f));

                if (GUILayout.Button(cancelContent, cancelStyle, NeoInspectorStyles.Width(cancelWidth)))
                {
                    _unityEventSearch = string.Empty;
                    GUI.FocusControl(null);
                }

                NeoInspectorStyles.HorizontalSpace(6f);

                _unityEventOnlyWithListeners = GUILayout.Toggle(_unityEventOnlyWithListeners,
                    NeoInspectorStyles.TempContent("Only active", "Hide events without persistent listeners."),
                    EditorStyles.toolbarButton, NeoInspectorStyles.Width(80f));

                GUILayout.FlexibleSpace();
            }

            NeoInspectorStyles.Space(2);
            EditorGUI.indentLevel++;

            bool searching = !string.IsNullOrWhiteSpace(_unityEventSearch);
            int shown = 0;
            for (int i = 0; i < unityEvents.Count; i++)
            {
                SerializedProperty p = unityEvents[i];
                if (p == null || i >= _eventStatsInOrder.Count)
                {
                    continue;
                }

                EventStats stats = _eventStatsInOrder[i];
                if (_unityEventOnlyWithListeners && stats.Calls == 0)
                {
                    continue;
                }

                if (searching &&
                    (stats.DisplayName ?? string.Empty).IndexOf(_unityEventSearch,
                        StringComparison.OrdinalIgnoreCase) < 0 &&
                    (stats.PropertyPath ?? string.Empty).IndexOf(_unityEventSearch,
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (shown > 0)
                {
                    NeoInspectorStyles.Space(2);
                }

                DrawNetworkReplicationMarker(p);
                EditorGUILayout.PropertyField(p, stats.Label, true);

                if (!p.isExpanded && !string.IsNullOrEmpty(stats.Preview))
                {
                    EditorGUILayout.LabelField(stats.Preview, NeoInspectorStyles.EventsPreview);
                }

                if (stats.Warning != null && !p.isExpanded)
                {
                    EditorGUILayout.LabelField(stats.Warning, NeoInspectorStyles.EventsWarning);
                }

                shown++;
            }

            if (shown == 0)
            {
                EditorGUILayout.LabelField("Nothing matches the current filter.",
                    EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUI.indentLevel--;
        }
    }
}
