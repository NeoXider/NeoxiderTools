using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor
{
    public abstract partial class CustomEditorBase
    {
        private void DrawNeoPropertiesWithCollapsibleUnityEvents()
        {
            if (serializedObject == null)
            {
                return;
            }

            serializedObject.Update();

            List<SerializedProperty> unityEvents = new();
            List<SerializedProperty> properties = new();
            SerializedProperty scriptProp = null;

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.name == "m_Script")
                {
                    scriptProp = iterator.Copy();
                    continue;
                }

                if (IsUnityEventProperty(iterator))
                {
                    unityEvents.Add(iterator.Copy());
                    continue;
                }

                properties.Add(iterator.Copy());
            }

            if (scriptProp != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(scriptProp, true);
                }
            }

            DrawHeaderSections(properties);

            if (unityEvents.Count > 0)
            {
                EditorGUILayout.Space(4);
                DrawUnityEventsFoldout(unityEvents);
            }

            DrawActionsFoldout();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeaderSections(List<SerializedProperty> properties)
        {
            if (properties == null || properties.Count == 0 || target == null)
            {
                return;
            }

            bool hasAnyHeader = false;
            for (int i = 0; i < properties.Count; i++)
            {
                if (!string.IsNullOrEmpty(TryGetHeaderTitleForProperty(properties[i])))
                {
                    hasAnyHeader = true;
                    break;
                }
            }

            if (!hasAnyHeader)
            {
                for (int i = 0; i < properties.Count; i++)
                {
                    DrawPropertyField(properties[i], false);
                }

                return;
            }

            List<HeaderSection> sections = BuildHeaderSections(properties);
            Color baseGreen = CustomEditorSettings.ScriptNameColor;
            var darkGreen = Color.Lerp(baseGreen, Color.black, 0.75f);
            int minFieldsForCategory = Mathf.Max(0, CustomEditorSettings.MinFieldsForHeaderCategory);

            for (int i = 0; i < sections.Count; i++)
            {
                HeaderSection section = sections[i];
                if (section.IsWarningHeader)
                {
                    DrawWarningHeader(section.Title);
                    for (int p = 0; p < section.Properties.Count; p++)
                    {
                        DrawPropertyField(section.Properties[p], true);
                    }

                    continue;
                }

                bool excludeFromMinFieldsRule =
                    string.Equals(section.Title, "Actions", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(section.Title, "Events", StringComparison.OrdinalIgnoreCase);

                if (!excludeFromMinFieldsRule && section.Properties.Count == 1)
                {
                    DrawPlainHeader(section.Title);
                    for (int p = 0; p < section.Properties.Count; p++)
                    {
                        DrawPropertyField(section.Properties[p], true);
                    }

                    continue;
                }

                bool shouldForceFoldout = ShouldForceFoldoutHeader(section);

                if (!excludeFromMinFieldsRule &&
                    !shouldForceFoldout &&
                    minFieldsForCategory > 0 &&
                    section.Properties.Count < minFieldsForCategory)
                {
                    DrawPlainHeader(section.Title);
                    for (int p = 0; p < section.Properties.Count; p++)
                    {
                        DrawPropertyField(section.Properties[p], true);
                    }

                    continue;
                }

                string key = $"{target.GetType().FullName}.NeoFoldout.Header.{section.Title}";
                bool expanded = GetFoldoutState(key, true);

                Color accent = expanded ? darkGreen : baseGreen;
                Color titleColor = expanded ? Color.white : baseGreen;
                Color countColor = expanded
                    ? new Color(1f, 1f, 1f, 0.75f)
                    : new Color(baseGreen.r, baseGreen.g, baseGreen.b, 0.75f);

                expanded = DrawNeoSectionHeader(expanded, section.Title, section.Properties.Count, accent,
                    "d_Folder Icon", titleColor, countColor);
                _neoFoldouts[key] = expanded;

                if (!expanded)
                {
                    continue;
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUI.indentLevel++;
                    for (int p = 0; p < section.Properties.Count; p++)
                    {
                        DrawPropertyField(section.Properties[p], true);
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }

        private static bool ShouldForceFoldoutHeader(HeaderSection section)
        {
            if (section.Properties == null || section.Properties.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < section.Properties.Count; i++)
            {
                SerializedProperty property = section.Properties[i];
                if (property == null)
                {
                    continue;
                }

                if (property.isArray && property.propertyType != SerializedPropertyType.String)
                {
                    return true;
                }

                if (property.propertyType == SerializedPropertyType.Generic && property.hasVisibleChildren)
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawAutoSections(List<SerializedProperty> properties)
        {
            if (properties == null || properties.Count == 0 || target == null)
            {
                return;
            }

            List<SerializedProperty> references = new();
            List<SerializedProperty> settings = new();
            List<SerializedProperty> debug = new();

            for (int i = 0; i < properties.Count; i++)
            {
                SerializedProperty p = properties[i];
                if (p == null)
                {
                    continue;
                }

                if (IsDebugProperty(p))
                {
                    debug.Add(p);
                }
                else if (IsReferenceProperty(p))
                {
                    references.Add(p);
                }
                else
                {
                    settings.Add(p);
                }
            }

            Color baseGreen = CustomEditorSettings.ScriptNameColor;
            var darkGreen = Color.Lerp(baseGreen, Color.black, 0.75f);

            DrawAutoSection("References", references, baseGreen, darkGreen);
            DrawAutoSection("Settings", settings, baseGreen, darkGreen);
            DrawAutoSection("Debug", debug, baseGreen, darkGreen);
        }

        private void DrawAutoSection(string title, List<SerializedProperty> props, Color baseColor, Color darkColor)
        {
            if (props == null || props.Count == 0 || target == null)
            {
                return;
            }

            string key = $"{target.GetType().FullName}.NeoFoldout.Auto.{title}";
            bool expanded = GetFoldoutState(key, true);

            Color accent = expanded ? darkColor : baseColor;
            Color titleColor = expanded ? Color.white : baseColor;
            Color countColor = expanded
                ? new Color(1f, 1f, 1f, 0.75f)
                : new Color(baseColor.r, baseColor.g, baseColor.b, 0.75f);

            expanded = DrawNeoSectionHeader(expanded, title, props.Count, accent, "d_Folder Icon", titleColor,
                countColor);
            _neoFoldouts[key] = expanded;

            if (!expanded)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < props.Count; i++)
                {
                    DrawPropertyField(props[i], false);
                }

                EditorGUI.indentLevel--;
            }
        }

        private bool IsReferenceProperty(SerializedProperty property)
        {
            if (property == null || target == null)
            {
                return false;
            }

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                return true;
            }

            if (!TryGetFieldInfoForPropertyPath(target.GetType(), property.propertyPath, out FieldInfo fieldInfo))
            {
                return false;
            }

            Type t = fieldInfo.FieldType;
            if (t == null)
            {
                return false;
            }

            if (typeof(Object).IsAssignableFrom(t))
            {
                return true;
            }

            if (t.IsArray)
            {
                Type element = t.GetElementType();
                return element != null && typeof(Object).IsAssignableFrom(element);
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type element = t.GetGenericArguments()[0];
                return element != null && typeof(Object).IsAssignableFrom(element);
            }

            return false;
        }

        private static bool IsDebugProperty(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            string name = property.name ?? string.Empty;
            string path = property.propertyPath ?? string.Empty;

            return name.Contains("debug", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("gizmo", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("editor", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("debug", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("gizmo", StringComparison.OrdinalIgnoreCase);
        }

        private void DrawPropertyField(SerializedProperty property, bool suppressUnityHeaderDecorators)
        {
            if (property == null)
            {
                return;
            }

            if (property.propertyType == SerializedPropertyType.Boolean)
            {
                DrawBooleanToggle(property);
                return;
            }

            if (!CustomEditorSettings.UseDefaultListAndArrayDrawing && ShouldUseCollectionPropertyFoldout(property))
            {
                DrawCollectionPropertyFoldout(property);
                return;
            }

            if (ShouldUseNestedPropertyFoldout(property))
            {
                DrawNestedPropertyFoldout(property);
                return;
            }

            if (suppressUnityHeaderDecorators && !string.IsNullOrEmpty(TryGetHeaderTitleForProperty(property)))
            {
                DrawPropertyFieldWithoutHeaderDecorator(property);
                return;
            }

            EditorGUILayout.PropertyField(property, true);
        }

        private void DrawCollectionPropertyFoldout(SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            Color accent = IsDebugProperty(property)
                ? new Color(0.94f, 0.34f, 0.34f, 1f)
                : new Color(0.92f, 0.72f, 0.28f, 1f);

            property.isExpanded = DrawNestedFoldoutHeader(property.displayName, property.arraySize, property.isExpanded,
                accent);
            if (!property.isExpanded)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                SerializedProperty iterator = property.Copy();
                SerializedProperty endProperty = iterator.GetEndProperty();
                int directChildDepth = property.depth + 1;
                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
                {
                    enterChildren = false;
                    if (iterator.depth != directChildDepth)
                    {
                        continue;
                    }

                    DrawPropertyField(iterator.Copy(), false);
                }
            }
        }

        private void DrawNestedPropertyFoldout(SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            int childCount = CountDirectVisibleChildren(property);
            Color accent = IsDebugProperty(property)
                ? new Color(0.92f, 0.30f, 0.30f, 1f)
                : new Color(0.40f, 0.66f, 0.98f, 1f);

            property.isExpanded =
                DrawNestedFoldoutHeader(property.displayName, childCount, property.isExpanded, accent);
            if (!property.isExpanded)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                SerializedProperty iterator = property.Copy();
                SerializedProperty endProperty = iterator.GetEndProperty();
                int directChildDepth = property.depth + 1;
                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
                {
                    enterChildren = false;
                    if (iterator.depth != directChildDepth)
                    {
                        continue;
                    }

                    DrawPropertyField(iterator.Copy(), false);
                }
            }
        }

        private static bool ShouldUseCollectionPropertyFoldout(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            if (IsUnityEventProperty(property))
            {
                return false;
            }

            return property.isArray && property.propertyType != SerializedPropertyType.String;
        }

        private static bool ShouldUseNestedPropertyFoldout(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            if (IsUnityEventProperty(property))
            {
                return false;
            }

            if (property.propertyType != SerializedPropertyType.Generic || !property.hasVisibleChildren)
            {
                return false;
            }

            if (property.isArray)
            {
                return false;
            }

            return CountDirectVisibleChildren(property) > 0;
        }

        private static int CountDirectVisibleChildren(SerializedProperty property)
        {
            if (property == null || !property.hasVisibleChildren)
            {
                return 0;
            }

            SerializedProperty iterator = property.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();
            int directChildDepth = property.depth + 1;
            int count = 0;
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;
                if (iterator.depth == directChildDepth)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool DrawNestedFoldoutHeader(string title, int count, bool expanded, Color accent)
        {
            const float height = 24f;

            Rect rect = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            rect = EditorGUI.IndentedRect(rect);

            bool isHover = rect.Contains(Event.current.mousePosition);
            var background = Color.Lerp(new Color(0.11f, 0.12f, 0.16f, 0.92f), accent,
                expanded ? 0.16f : isHover ? 0.12f : 0.08f);

            EditorGUI.DrawRect(rect, background);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height),
                new Color(accent.r, accent.g, accent.b, 0.95f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(1f, 1f, 1f, 0.05f));

            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                expanded = !expanded;
                GUI.FocusControl(null);
                Event.current.Use();
            }

            GUIStyle arrowStyle = new(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.Lerp(Color.white, accent, 0.15f) }
            };
            GUIStyle titleStyle = new(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.92f, 0.95f, 1f, 1f) }
            };
            GUIStyle countStyle = new(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.88f, 0.92f, 0.98f, 0.95f) }
            };

            Rect arrowRect = new(rect.x + 6f, rect.y + 3f, 14f, rect.height - 6f);
            Rect countRect = new(rect.xMax - 34f, rect.y + 3f, 24f, rect.height - 6f);
            Rect titleRect = new(arrowRect.xMax + 4f, rect.y + 2f, countRect.x - arrowRect.xMax - 10f,
                rect.height - 4f);

            GUI.Label(arrowRect, expanded ? "▼" : "▶", arrowStyle);
            GUI.Label(titleRect, title, titleStyle);

            EditorGUI.DrawRect(countRect, new Color(accent.r, accent.g, accent.b, expanded ? 0.28f : 0.18f));
            EditorGUI.DrawRect(new Rect(countRect.x, countRect.yMax - 1f, countRect.width, 1f),
                new Color(1f, 1f, 1f, 0.05f));
            GUI.Label(countRect, count.ToString(), countStyle);

            return expanded;
        }

        private static void DrawPropertyFieldWithoutHeaderDecorator(SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            if (_getHandlerMethod == null)
            {
                EditorGUILayout.PropertyField(property, true);
                return;
            }

            object handler = null;
            try
            {
                handler = _getHandlerMethod.Invoke(null, new object[] { property });
            }
            catch
            {
                EditorGUILayout.PropertyField(property, true);
                return;
            }

            if (handler == null)
            {
                EditorGUILayout.PropertyField(property, true);
                return;
            }

            FieldInfo decoratorsField =
                handler.GetType().GetField("m_DecoratorDrawers", BindingFlags.Instance | BindingFlags.NonPublic);
            if (decoratorsField == null)
            {
                EditorGUILayout.PropertyField(property, true);
                return;
            }

            object originalDecorators = null;
            try
            {
                originalDecorators = decoratorsField.GetValue(handler);
                if (originalDecorators is not IList list)
                {
                    EditorGUILayout.PropertyField(property, true);
                    return;
                }

                IList filtered;
                try
                {
                    filtered = (IList)Activator.CreateInstance(decoratorsField.FieldType);
                }
                catch
                {
                    EditorGUILayout.PropertyField(property, true);
                    return;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    object d = list[i];
                    if (d == null)
                    {
                        continue;
                    }

                    if (d.GetType().Name == "HeaderDrawer")
                    {
                        continue;
                    }

                    filtered.Add(d);
                }

                decoratorsField.SetValue(handler, filtered);
                EditorGUILayout.PropertyField(property, true);
            }
            catch
            {
                EditorGUILayout.PropertyField(property, true);
            }
            finally
            {
                if (originalDecorators != null)
                {
                    try
                    {
                        decoratorsField.SetValue(handler, originalDecorators);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void DrawBooleanToggle(SerializedProperty property)
        {
            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            GUIContent label = new(property.displayName, property.tooltip);

            EditorGUI.BeginProperty(rect, label, property);
            Rect contentRect = EditorGUI.PrefixLabel(rect, label);

            const float toggleWidth = 54f;
            const float toggleHeight = 18f;
            Rect buttonRect = new(contentRect.xMax - toggleWidth, contentRect.y + 1f, toggleWidth, toggleHeight);

            bool value = property.boolValue;
            Color oldBg = GUI.backgroundColor;
            Color oldColor = GUI.color;

            if (Event.current.type == EventType.Repaint)
            {
                Color bg = value
                    ? new Color(0.20f, 0.76f, 0.42f, 1f)
                    : new Color(0.20f, 0.22f, 0.26f, 1f);
                EditorGUI.DrawRect(buttonRect, bg);
                EditorGUI.DrawRect(new Rect(buttonRect.x, buttonRect.yMax - 1f, buttonRect.width, 1f),
                    new Color(1f, 1f, 1f, 0.12f));

                float knobSize = toggleHeight - 4f;
                float knobX = value ? buttonRect.xMax - knobSize - 2f : buttonRect.x + 2f;
                Rect knobRect = new(knobX, buttonRect.y + 2f, knobSize, knobSize);
                EditorGUI.DrawRect(knobRect, value ? Color.white : new Color(0.78f, 0.80f, 0.84f, 1f));
            }

            GUI.color = Color.clear;
            if (GUI.Button(buttonRect, GUIContent.none, GUIStyle.none))
            {
                property.boolValue = !value;
            }

            GUI.color = Color.white;

            GUIStyle stateStyle = new(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(buttonRect, value ? "ON" : "OFF", stateStyle);

            GUI.backgroundColor = oldBg;
            GUI.color = oldColor;

            EditorGUI.EndProperty();
        }

        private List<HeaderSection> BuildHeaderSections(List<SerializedProperty> properties)
        {
            List<HeaderSection> sections = new();
            int i = 0;
            while (i < properties.Count)
            {
                SerializedProperty p = properties[i];
                string headerTitle = TryGetHeaderTitleForProperty(p);

                if (string.IsNullOrEmpty(headerTitle))
                {
                    List<SerializedProperty> general = new();
                    while (i < properties.Count && string.IsNullOrEmpty(TryGetHeaderTitleForProperty(properties[i])))
                    {
                        general.Add(properties[i]);
                        i++;
                    }

                    if (general.Count > 0)
                    {
                        sections.Add(new HeaderSection("General", general, false));
                    }

                    continue;
                }

                List<SerializedProperty> sectionProps = new();
                sectionProps.Add(p);
                i++;

                while (i < properties.Count && string.IsNullOrEmpty(TryGetHeaderTitleForProperty(properties[i])))
                {
                    sectionProps.Add(properties[i]);
                    i++;
                }

                bool looksLikeWarning =
                    headerTitle.StartsWith("⚠", StringComparison.Ordinal) ||
                    headerTitle.StartsWith("!", StringComparison.Ordinal) ||
                    headerTitle.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                    headerTitle.Contains("предуп", StringComparison.OrdinalIgnoreCase) ||
                    headerTitle.Contains("ошиб", StringComparison.OrdinalIgnoreCase);

                bool isWarningHeader = looksLikeWarning;
                sections.Add(new HeaderSection(headerTitle, sectionProps, isWarningHeader));
            }

            return sections;
        }

        private void DrawWarningHeader(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Rect rect = GUILayoutUtility.GetRect(0f, 28f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.42f, 0.10f, 0.10f, 0.55f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 5f, rect.height), new Color(1f, 0.28f, 0.28f, 1f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(1f, 1f, 1f, 0.08f));

            GUIStyle style = new(EditorStyles.boldLabel)
            {
                fontSize = 16,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 0.85f, 0.85f, 1f) }
            };

            GUI.Label(new Rect(rect.x + 12f, rect.y + 3f, rect.width - 18f, rect.height - 6f), text, style);
            EditorGUILayout.Space(2);
        }

        private static bool IsDebugHeaderTitle(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return text.Contains("debug", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("отлад", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("gizmo", StringComparison.OrdinalIgnoreCase);
        }

        private void DrawPlainHeader(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            bool isDebug = IsDebugHeaderTitle(text);
            Rect rect = GUILayoutUtility.GetRect(0f, 24f, GUILayout.ExpandWidth(true));
            Color background = isDebug
                ? new Color(0.22f, 0.08f, 0.08f, 0.82f)
                : new Color(0.10f, 0.12f, 0.16f, 0.72f);
            Color accent = isDebug
                ? new Color(0.94f, 0.28f, 0.28f, 1f)
                : new Color(0.52f, 0.74f, 1f, 1f);
            Color titleColor = isDebug
                ? new Color(1f, 0.88f, 0.88f, 1f)
                : new Color(0.90f, 0.94f, 1f, 1f);

            EditorGUI.DrawRect(rect, background);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f),
                new Color(accent.r, accent.g, accent.b, 0.48f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), accent);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(1f, 1f, 1f, 0.06f));

            GUIStyle style = new(EditorStyles.boldLabel)
            {
                fontSize = 13,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = titleColor }
            };

            GUI.Label(new Rect(rect.x + 11f, rect.y + 2f, rect.width - 18f, rect.height - 4f), text, style);
            EditorGUILayout.Space(1);
        }

        private string TryGetHeaderTitleForProperty(SerializedProperty property)
        {
            if (property == null || target == null)
            {
                return null;
            }

            if (!TryGetFieldInfoForPropertyPath(target.GetType(), property.propertyPath, out FieldInfo fieldInfo))
            {
                return null;
            }

            try
            {
                object[] attrs = fieldInfo.GetCustomAttributes(typeof(HeaderAttribute), true);
                if (attrs == null || attrs.Length == 0)
                {
                    return null;
                }

                var header = attrs[0] as HeaderAttribute;
                return header?.header;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryGetFieldInfoForPropertyPath(Type rootType, string propertyPath, out FieldInfo fieldInfo)
        {
            fieldInfo = null;
            if (rootType == null || string.IsNullOrEmpty(propertyPath))
            {
                return false;
            }

            string[] parts = propertyPath.Split('.');
            Type currentType = rootType;
            FieldInfo currentField = null;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                if (part == "Array")
                {
                    continue;
                }

                if (part.StartsWith("data[", StringComparison.Ordinal))
                {
                    continue;
                }

                currentField = currentType.GetField(part,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (currentField == null)
                {
                    return false;
                }

                currentType = currentField.FieldType;

                if (currentType.IsArray)
                {
                    currentType = currentType.GetElementType();
                }
                else if (currentType.IsGenericType &&
                         currentType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    currentType = currentType.GetGenericArguments()[0];
                }
            }

            fieldInfo = currentField;
            return fieldInfo != null;
        }

        private bool GetFoldoutState(string key, bool defaultValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                return defaultValue;
            }

            if (_neoFoldouts.TryGetValue(key, out bool value))
            {
                return value;
            }

            _neoFoldouts[key] = defaultValue;
            return defaultValue;
        }

        private static bool IsUnityEventProperty(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            string typeName = property.type;
            if (!string.IsNullOrEmpty(typeName) && typeName.Contains("UnityEvent"))
            {
                return true;
            }

            if (property.propertyType != SerializedPropertyType.Generic)
            {
                return false;
            }

            SerializedProperty calls = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            return calls != null && calls.isArray;
        }

        private static SerializedProperty GetPersistentCallsArray(SerializedProperty unityEventProperty)
        {
            if (unityEventProperty == null)
            {
                return null;
            }

            SerializedProperty calls = unityEventProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
            if (calls != null && calls.isArray)
            {
                return calls;
            }

            return null;
        }

        private static int GetPersistentCallCount(SerializedProperty unityEventProperty)
        {
            SerializedProperty calls = GetPersistentCallsArray(unityEventProperty);
            return calls != null ? calls.arraySize : 0;
        }

        private static int GetBrokenPersistentCallCount(SerializedProperty unityEventProperty)
        {
            SerializedProperty calls = GetPersistentCallsArray(unityEventProperty);
            if (calls == null)
            {
                return 0;
            }

            int broken = 0;
            for (int i = 0; i < calls.arraySize; i++)
            {
                SerializedProperty call = calls.GetArrayElementAtIndex(i);
                if (call == null)
                {
                    continue;
                }

                SerializedProperty targetProp = call.FindPropertyRelative("m_Target");
                SerializedProperty methodProp = call.FindPropertyRelative("m_MethodName");

                bool hasTarget = targetProp != null &&
                                 targetProp.propertyType == SerializedPropertyType.ObjectReference &&
                                 targetProp.objectReferenceValue != null;
                bool hasMethod = methodProp != null &&
                                 methodProp.propertyType == SerializedPropertyType.String &&
                                 !string.IsNullOrWhiteSpace(methodProp.stringValue);

                if (!hasTarget || !hasMethod)
                {
                    broken++;
                }
            }

            return broken;
        }

        private static string BuildPersistentCallPreview(SerializedProperty unityEventProperty, int maxItems)
        {
            SerializedProperty calls = GetPersistentCallsArray(unityEventProperty);
            if (calls == null || calls.arraySize == 0 || maxItems <= 0)
            {
                return null;
            }

            int count = Mathf.Min(calls.arraySize, maxItems);
            List<string> parts = new(count);

            for (int i = 0; i < count; i++)
            {
                SerializedProperty call = calls.GetArrayElementAtIndex(i);
                if (call == null)
                {
                    continue;
                }

                SerializedProperty targetProp = call.FindPropertyRelative("m_Target");
                SerializedProperty methodProp = call.FindPropertyRelative("m_MethodName");

                string targetName = targetProp != null &&
                                    targetProp.propertyType == SerializedPropertyType.ObjectReference &&
                                    targetProp.objectReferenceValue != null
                    ? targetProp.objectReferenceValue.name
                    : "<Missing Target>";

                string methodName = methodProp != null &&
                                    methodProp.propertyType == SerializedPropertyType.String &&
                                    !string.IsNullOrWhiteSpace(methodProp.stringValue)
                    ? methodProp.stringValue
                    : "<Missing Method>";

                parts.Add($"{targetName}.{methodName}");
            }

            if (parts.Count == 0)
            {
                return null;
            }

            string preview = string.Join(", ", parts);
            if (calls.arraySize > maxItems)
            {
                preview += $" … (+{calls.arraySize - maxItems})";
            }

            return preview;
        }

        private bool DrawNeoSectionHeader(bool expanded,
            string title,
            int count,
            Color accent,
            string iconName,
            Color titleColor,
            Color countColor)
        {
            const float height = 34f;

            Rect rect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
            rect = EditorGUI.IndentedRect(rect);
            rect.height = height;

            bool isHover = rect.Contains(Event.current.mousePosition);
            Color baseBackground = new(0.09f, 0.10f, 0.13f, 0.96f);
            float tintStrength = isHover ? 0.22f : expanded ? 0.16f : 0.10f;
            var background = Color.Lerp(baseBackground, accent, tintStrength);

            EditorGUI.DrawRect(rect, background);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), new Color(1f, 1f, 1f, 0.04f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 3f),
                new Color(accent.r, accent.g, accent.b, expanded ? 0.92f : isHover ? 0.82f : 0.72f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height),
                new Color(accent.r, accent.g, accent.b, 0.96f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(1f, 1f, 1f, 0.06f));

            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                expanded = !expanded;
                GUI.FocusControl(null);
                Event.current.Use();
            }

            GUIStyle arrowStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.Lerp(Color.white, accent, 0.20f) }
            };
            Rect foldoutRect = new(rect.x + 8f, rect.y + 7f, 16f, 18f);
            GUI.Label(foldoutRect, expanded ? "▼" : "▶", arrowStyle);

            float x = rect.x + 28f;

            GUIContent iconContent = string.IsNullOrEmpty(iconName) ? null : EditorGUIUtility.IconContent(iconName);
            if (iconContent != null && iconContent.image != null)
            {
                Rect iconRect = new(x, rect.y + 8f, 16f, 16f);
                Color oldGuiColor = GUI.color;
                GUI.color = Color.Lerp(Color.white, accent, 0.14f);
                GUI.DrawTexture(iconRect, (Texture2D)iconContent.image, ScaleMode.ScaleToFit, true);
                GUI.color = oldGuiColor;
                x += 22f;
            }

            GUIStyle titleStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = titleColor }
            };

            GUIStyle countStyle = new(EditorStyles.miniBoldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = countColor }
            };
            string countText = count.ToString();
            float countWidth = Mathf.Max(34f, countStyle.CalcSize(new GUIContent(countText)).x + 18f);

            Rect titleRect = new(x, rect.y + 3f, rect.width - x - countWidth - 22f, rect.height - 6f);
            GUI.Label(titleRect, title, titleStyle);

            Rect countBgRect = new(rect.xMax - countWidth - 12f, rect.y + 7f, countWidth, 20f);
            EditorGUI.DrawRect(countBgRect,
                new Color(accent.r, accent.g, accent.b, expanded ? 0.34f : isHover ? 0.28f : 0.20f));
            EditorGUI.DrawRect(new Rect(countBgRect.x, countBgRect.y, countBgRect.width, 1f),
                new Color(1f, 1f, 1f, 0.06f));
            EditorGUI.DrawRect(new Rect(countBgRect.x, countBgRect.yMax - 1f, countBgRect.width, 1f),
                new Color(1f, 1f, 1f, 0.08f));

            GUI.Label(countBgRect, countText, countStyle);

            return expanded;
        }

        private readonly struct HeaderSection
        {
            public readonly string Title;
            public readonly List<SerializedProperty> Properties;
            public readonly bool IsWarningHeader;

            public HeaderSection(string title, List<SerializedProperty> properties, bool isWarningHeader)
            {
                Title = title;
                Properties = properties;
                IsWarningHeader = isWarningHeader;
            }
        }
    }
}
