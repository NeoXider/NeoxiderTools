using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    public abstract partial class CustomEditorBase
    {
        protected void DrawCollapsibleUnityEvents()
        {
            if (serializedObject == null)
            {
                return;
            }

            List<SerializedProperty> unityEvents = new();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script")
                {
                    continue;
                }

                if (IsUnityEventProperty(iterator))
                {
                    unityEvents.Add(iterator.Copy());
                }
            }

            if (unityEvents.Count > 0)
            {
                EditorGUILayout.Space(4);
                DrawUnityEventsFoldout(unityEvents);
            }
        }

        private void DrawUnityEventsFoldout(List<SerializedProperty> unityEvents)
        {
            if (unityEvents == null || unityEvents.Count == 0 || target == null)
            {
                return;
            }

            string key = $"{target.GetType().FullName}.NeoFoldout.Events";
            bool current = _neoFoldouts.TryGetValue(key, out bool value) && value;
            int totalListeners = 0;
            int totalBrokenListeners = 0;

            for (int i = 0; i < unityEvents.Count; i++)
            {
                SerializedProperty p = unityEvents[i];
                if (p == null)
                {
                    continue;
                }

                totalListeners += GetPersistentCallCount(p);
                totalBrokenListeners += GetBrokenPersistentCallCount(p);
            }

            using (new EditorGUILayout.VerticalScope())
            {
                Color accentBase = new(0.25f, 0.9f, 0.85f, 1f);
                var accentDark = Color.Lerp(accentBase, Color.black, 0.45f);
                Color accent = current ? accentDark : accentBase;

                current = DrawNeoSectionHeader(current, "Events", totalListeners, accent, "d_EventSystem Icon",
                    current ? Color.white : accentBase,
                    current
                        ? new Color(1f, 1f, 1f, 0.75f)
                        : new Color(accentBase.r, accentBase.g, accentBase.b, 0.75f));

                if (current)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        string summaryText = $"{unityEvents.Count} event field(s) • {totalListeners} listener(s)";
                        if (totalBrokenListeners > 0)
                        {
                            summaryText += $" • {totalBrokenListeners} broken";
                        }

                        GUIStyle summaryStyle = new(EditorStyles.miniBoldLabel)
                        {
                            wordWrap = true,
                            normal =
                            {
                                textColor = totalBrokenListeners > 0
                                    ? new Color(1f, 0.72f, 0.32f, 1f)
                                    : new Color(0.72f, 0.90f, 0.96f, 1f)
                            }
                        };
                        EditorGUILayout.LabelField(summaryText, summaryStyle);
                        EditorGUILayout.Space(3);

                        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                        {
                            GUIStyle searchStyle = EditorStyles.toolbarSearchField;
                            GUIStyle cancelStyle =
                                GUI.skin.FindStyle("ToolbarSearchCancelButton") ??
                                GUI.skin.FindStyle("ToolbarSeachCancelButton");
                            GUIContent cancelContent = GUIContent.none;
                            GUILayoutOption cancelWidth = null;
                            if (cancelStyle == null)
                            {
                                cancelStyle = EditorStyles.toolbarButton;
                                cancelContent = new GUIContent("×");
                                cancelWidth = GUILayout.Width(20);
                            }

                            _unityEventSearch ??= string.Empty;
                            _unityEventSearch =
                                GUILayout.TextField(_unityEventSearch, searchStyle, GUILayout.MinWidth(140));

                            if (GUILayout.Button(cancelContent, cancelStyle, cancelWidth ?? GUILayout.Width(18)))
                            {
                                _unityEventSearch = string.Empty;
                                GUI.FocusControl(null);
                            }

                            GUILayout.Space(6);

                            _unityEventOnlyWithListeners = GUILayout.Toggle(_unityEventOnlyWithListeners, "Only active",
                                EditorStyles.toolbarButton, GUILayout.Width(80));

                            GUILayout.FlexibleSpace();
                        }

                        EditorGUI.indentLevel++;

                        GUIStyle warningMini = new(EditorStyles.miniLabel)
                        {
                            wordWrap = true,
                            normal = { textColor = new Color(1f, 0.35f, 0.35f, 1f) }
                        };

                        int shown = 0;
                        for (int i = 0; i < unityEvents.Count; i++)
                        {
                            SerializedProperty p = unityEvents[i];
                            if (p == null)
                            {
                                continue;
                            }

                            int callCount = GetPersistentCallCount(p);
                            if (_unityEventOnlyWithListeners && callCount == 0)
                            {
                                continue;
                            }

                            if (!string.IsNullOrWhiteSpace(_unityEventSearch))
                            {
                                string dn = p.displayName ?? string.Empty;
                                string pp = p.propertyPath ?? string.Empty;
                                if (dn.IndexOf(_unityEventSearch, StringComparison.OrdinalIgnoreCase) < 0 &&
                                    pp.IndexOf(_unityEventSearch, StringComparison.OrdinalIgnoreCase) < 0)
                                {
                                    continue;
                                }
                            }

                            int brokenCount = GetBrokenPersistentCallCount(p);
                            string label =
                                brokenCount > 0 ? $"⚠ {p.displayName} ({callCount})" : $"{p.displayName} ({callCount})";

                            if (shown > 0)
                            {
                                EditorGUILayout.Space(2);
                            }

                            EditorGUILayout.PropertyField(p, new GUIContent(label), true);

                            if (!p.isExpanded && callCount > 0)
                            {
                                string preview = BuildPersistentCallPreview(p, 2);
                                if (!string.IsNullOrEmpty(preview))
                                {
                                    EditorGUILayout.LabelField(preview, EditorStyles.miniLabel);
                                }
                            }

                            if (brokenCount > 0 && !p.isExpanded)
                            {
                                EditorGUILayout.LabelField(
                                    $"{brokenCount} broken listener(s): check Target/Method.",
                                    warningMini);
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

            _neoFoldouts[key] = current;
        }
    }
}
