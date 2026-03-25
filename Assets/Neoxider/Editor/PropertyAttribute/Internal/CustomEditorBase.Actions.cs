using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor
{
    public abstract partial class CustomEditorBase
    {
        private static readonly Color ActionCardBackground = new(0.15f, 0.16f, 0.20f, 0.96f);
        private static readonly Color ActionCardAccent = new(0.44f, 0.56f, 0.94f, 1f);
        private static readonly Color ActionCardLine = new(1f, 1f, 1f, 0.06f);
        private static readonly Color ActionMetaColor = new(0.70f, 0.76f, 0.86f, 0.95f);
        private static readonly Color ActionTypeColor = new(0.56f, 0.64f, 0.78f, 0.92f);
        private static readonly Color ActionButtonAccent = new(0.46f, 0.58f, 0.96f, 1f);
        private static readonly Color ActionSecondaryAccent = new(0.58f, 0.62f, 0.74f, 1f);

        private void DrawActionsFoldout()
        {
            if (target == null)
            {
                return;
            }

            MethodInfo[] methods = GetButtonMethods();
            if (methods.Length == 0)
            {
                return;
            }

            string key = $"{target.GetType().FullName}.NeoFoldout.Actions";
            bool current = _neoFoldouts.TryGetValue(key, out bool value) && value;

            using (new EditorGUILayout.VerticalScope())
            {
                Color accentBase = new(0.75f, 0.35f, 1f, 1f);
                var accentDark = Color.Lerp(accentBase, Color.black, 0.45f);
                Color accent = current ? accentDark : accentBase;

                current = DrawNeoSectionHeader(current, "Actions", methods.Length, accent, "d_PlayButton",
                    current ? Color.white : accentBase,
                    current
                        ? new Color(1f, 1f, 1f, 0.75f)
                        : new Color(accentBase.r, accentBase.g, accentBase.b, 0.75f));

                if (current)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        DrawMethodButtons(methods);
                    }
                }
            }

            _neoFoldouts[key] = current;
        }

        protected ButtonInfo? FindButtonAttribute(MethodInfo method)
        {
            if (method == null)
            {
                return null;
            }

            object[] allAttributes = method.GetCustomAttributes(false);

            foreach (object attr in allAttributes)
            {
                if (attr == null)
                {
                    continue;
                }

                Type attrType = attr.GetType();
                string fullName = attrType.FullName;
                string typeName = attrType.Name;
                string namespaceName = attrType.Namespace;

                if (fullName == "Neo.ButtonAttribute" ||
                    (namespaceName == "Neo" && typeName == "ButtonAttribute"))
                {
                    try
                    {
                        var neoAttr = attr as ButtonAttribute;
                        if (neoAttr != null)
                        {
                            return new ButtonInfo(neoAttr.ButtonName, neoAttr.Width);
                        }
                    }
                    catch
                    {
                    }
                }

                if (namespaceName == "Sirenix.OdinInspector" && typeName == "ButtonAttribute")
                {
                    try
                    {
                        PropertyInfo nameProperty =
                            attrType.GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                        if (nameProperty == null)
                        {
                            nameProperty = attrType.GetProperty("ButtonName",
                                BindingFlags.Public | BindingFlags.Instance);
                        }

                        string name = nameProperty?.GetValue(attr) as string;

                        if (string.IsNullOrEmpty(name))
                        {
                            name = null;
                        }

                        float width = 120f;
                        PropertyInfo widthProperty =
                            attrType.GetProperty("Width", BindingFlags.Public | BindingFlags.Instance);
                        if (widthProperty != null)
                        {
                            object widthValue = widthProperty.GetValue(attr);
                            if (widthValue is float f)
                            {
                                width = f;
                            }
                            else if (widthValue is int i)
                            {
                                width = i;
                            }
                        }

                        return new ButtonInfo(name, width);
                    }
                    catch
                    {
                        return new ButtonInfo(null, 120f);
                    }
                }
            }

            try
            {
                ButtonAttribute neoButtonAttribute = method.GetCustomAttribute<ButtonAttribute>();
                if (neoButtonAttribute != null)
                {
                    return new ButtonInfo(neoButtonAttribute.ButtonName, neoButtonAttribute.Width);
                }
            }
            catch
            {
            }

            return null;
        }

        protected virtual void DrawMethodButtons()
        {
            if (target == null)
            {
                Debug.LogWarning("[NeoCustomEditor] DrawMethodButtons: target == null");
                return;
            }

            MethodInfo[] methods = target.GetType().GetMethods(
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic);

            DrawMethodButtons(methods);
        }

        protected virtual void DrawMethodButtons(MethodInfo[] methods)
        {
            if (target == null || methods == null)
            {
                return;
            }

            foreach (MethodInfo method in methods)
            {
                if (method == null)
                {
                    continue;
                }

                ButtonInfo? buttonInfo = FindButtonAttribute(method);
                if (!buttonInfo.HasValue)
                {
                    continue;
                }

                ButtonInfo buttonAttribute = buttonInfo.Value;

                ParameterInfo[] parameters = method.GetParameters();
                string buttonText = string.IsNullOrEmpty(buttonAttribute.ButtonName)
                    ? method.Name
                    : buttonAttribute.ButtonName;

                if (buttonText.Length > CustomEditorSettings.ButtonTextMaxLength)
                {
                    buttonText = buttonText.Substring(0, CustomEditorSettings.ButtonTextMaxLength - 2) + "...";
                }

                string title = ObjectNames.NicifyVariableName(buttonText);

                Rect cardRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawActionCardChrome(cardRect,
                    parameters.Length > 0 ? ActionCardAccent : new Color(0.46f, 0.46f, 0.52f, 1f));
                using (new EditorGUILayout.VerticalScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUIStyle titleStyle = new(EditorStyles.boldLabel)
                        {
                            fontSize = 13,
                            normal = { textColor = new Color(0.95f, 0.97f, 1f, 1f) }
                        };
                        GUIStyle metaStyle = new(EditorStyles.miniBoldLabel)
                        {
                            alignment = TextAnchor.MiddleRight,
                            normal = { textColor = ActionMetaColor }
                        };

                        EditorGUILayout.LabelField(title, titleStyle);
                        GUILayout.FlexibleSpace();

                        string meta = parameters.Length > 0 ? $"{parameters.Length} params" : "No params";
                        if (method.IsStatic)
                        {
                            meta += " • static";
                        }

                        GUILayout.Label(meta, metaStyle, GUILayout.Width(110f));
                    }

                    NeoxiderEditorGUI.DrawCaption($"Method `{method.Name}`");

                    if (parameters.Length > 0)
                    {
                        string foldoutKey = $"{method.Name}_foldout";
                        if (!_buttonFoldouts.ContainsKey(foldoutKey))
                        {
                            _buttonFoldouts[foldoutKey] = false;
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            string toggleLabel = _buttonFoldouts[foldoutKey]
                                ? $"▼ Parameters ({parameters.Length})"
                                : $"▶ Parameters ({parameters.Length})";

                            if (DrawInlineActionButton(toggleLabel, ActionButtonAccent, 18f, true))
                            {
                                _buttonFoldouts[foldoutKey] = !_buttonFoldouts[foldoutKey];
                            }

                            if (_buttonFoldouts[foldoutKey] &&
                                DrawInlineActionButton("Reset", ActionSecondaryAccent, 18f, false, 60f))
                            {
                                ResetStoredButtonParameters(method, parameters);
                            }
                        }

                        if (_buttonFoldouts[foldoutKey])
                        {
                            Rect paramsRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            DrawActionCardChrome(paramsRect,
                                new Color(ActionCardAccent.r, ActionCardAccent.g, ActionCardAccent.b, 0.65f), 2f, 2f);
                            using (new EditorGUILayout.VerticalScope())
                            {
                                NeoxiderEditorGUI.DrawCaption(
                                    "Parameters keep the last entered value for the current inspector session.");

                                for (int i = 0; i < parameters.Length; i++)
                                {
                                    ParameterInfo param = parameters[i];
                                    if (param == null)
                                    {
                                        continue;
                                    }

                                    string key = $"{method.Name}_{param.Name}";
                                    object value = GetStoredButtonParameterValue(method, param, key);

                                    EditorGUI.BeginChangeCheck();
                                    object newValue = DrawParameterField(param.Name, value, param.ParameterType);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        _buttonParameterValues[key] = newValue;
                                        _isFirstRun[key] = false;
                                    }
                                }
                            }

                            EditorGUILayout.EndVertical();
                        }
                    }

                    EditorGUILayout.Space(3f);

                    bool buttonPressed = DrawGradientButton(title, 0f, 22f);
                    if (buttonPressed)
                    {
                        InvokeButtonMethod(method, parameters);
                    }
                }

                EditorGUILayout.EndVertical();

                GUILayout.Space(6f);
            }
        }

        protected object DrawParameterField(string label, object value, Type type)
        {
            try
            {
                Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 4f);
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.02f));
                    EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, 2f, rowRect.height),
                        new Color(ActionCardAccent.r, ActionCardAccent.g, ActionCardAccent.b, 0.85f));
                    EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.yMax - 1f, rowRect.width, 1f),
                        new Color(1f, 1f, 1f, 0.04f));
                }

                Rect contentRect = new(rowRect.x + 6f, rowRect.y + 2f, rowRect.width - 12f,
                    EditorGUIUtility.singleLineHeight);
                Rect labelRect = new(contentRect.x, contentRect.y, 126f, contentRect.height);
                Rect typeRect = new(contentRect.xMax - 70f, contentRect.y, 70f, contentRect.height);
                Rect fieldRect = new(labelRect.xMax + 8f, contentRect.y, typeRect.x - labelRect.xMax - 14f,
                    contentRect.height);

                GUIStyle titleStyle = new(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.88f, 0.91f, 0.98f, 1f) }
                };
                GUIStyle typeStyle = new(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = ActionTypeColor }
                };

                GUI.Label(labelRect, ObjectNames.NicifyVariableName(label), titleStyle);
                GUI.Label(typeRect, type.Name, typeStyle);

                object result;
                if (type == typeof(int))
                {
                    result = EditorGUI.IntField(fieldRect, value is int intValue ? intValue : 0);
                }
                else if (type == typeof(float))
                {
                    result = EditorGUI.FloatField(fieldRect, value is float floatValue ? floatValue : 0f);
                }
                else if (type == typeof(string))
                {
                    result = EditorGUI.TextField(fieldRect, value as string ?? string.Empty);
                }
                else if (type == typeof(bool))
                {
                    result = EditorGUI.Toggle(fieldRect, value is bool boolValue && boolValue);
                }
                else if (typeof(Object).IsAssignableFrom(type))
                {
                    result = EditorGUI.ObjectField(fieldRect, value as Object, type, true);
                }
                else if (type.IsEnum)
                {
                    if (value == null || !type.IsInstanceOfType(value))
                    {
                        value = Enum.GetValues(type).GetValue(0);
                    }

                    result = EditorGUI.EnumPopup(fieldRect, (Enum)value);
                }
                else if (type == typeof(Vector2))
                {
                    result = EditorGUI.Vector2Field(fieldRect, GUIContent.none,
                        value is Vector2 vector2Value ? vector2Value : Vector2.zero);
                }
                else if (type == typeof(Vector3))
                {
                    result = EditorGUI.Vector3Field(fieldRect, GUIContent.none,
                        value is Vector3 vector3Value ? vector3Value : Vector3.zero);
                }
                else if (type == typeof(Color))
                {
                    result = EditorGUI.ColorField(fieldRect, GUIContent.none,
                        value is Color colorValue ? colorValue : Color.white);
                }
                else
                {
                    EditorGUI.LabelField(fieldRect, "Unsupported");
                    result = value;
                }

                return result;
            }
            catch (InvalidCastException)
            {
                Debug.LogError($"Invalid cast for parameter {label} of type {type.Name}. Resetting to default value.");
                return GetDefaultValue(type);
            }
        }

        private static void DrawActionCardChrome(Rect rect, Color accent, float topStripeHeight = 3f,
            float leftStripeWidth = 3f)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EditorGUI.DrawRect(rect, ActionCardBackground);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, topStripeHeight),
                new Color(accent.r, accent.g, accent.b, 0.9f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, leftStripeWidth, rect.height),
                new Color(accent.r, accent.g, accent.b, 0.95f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), ActionCardLine);
        }

        private static bool DrawInlineActionButton(string text, Color accent, float height, bool expandWidth,
            float fixedWidth = 0f)
        {
            GUIContent content = new(text);
            Rect rect = fixedWidth > 0f
                ? GUILayoutUtility.GetRect(content, EditorStyles.miniButton, GUILayout.Width(fixedWidth),
                    GUILayout.Height(height))
                : GUILayoutUtility.GetRect(content, EditorStyles.miniButton, GUILayout.Height(height),
                    expandWidth ? GUILayout.ExpandWidth(true) : GUILayout.MinWidth(90f));

            bool isHover = rect.Contains(Event.current.mousePosition);
            if (Event.current.type == EventType.Repaint)
            {
                var bg = new Color(accent.r, accent.g, accent.b, isHover ? 0.24f : 0.16f);
                EditorGUI.DrawRect(rect, bg);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), new Color(1f, 1f, 1f, 0.05f));
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height),
                    new Color(accent.r, accent.g, accent.b, 0.92f));
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(1f, 1f, 1f, 0.06f));

                GUIStyle style = new(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.Lerp(Color.white, accent, 0.10f) }
                };
                style.Draw(rect, content, false, false, false, false);
            }

            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private object GetStoredButtonParameterValue(MethodInfo method, ParameterInfo param, string key)
        {
            if (!_isFirstRun.ContainsKey(key))
            {
                _isFirstRun[key] = true;
            }

            object value;
            if (_isFirstRun[key] && param.HasDefaultValue)
            {
                value = param.DefaultValue;
                _buttonParameterValues[key] = value;
                _isFirstRun[key] = false;
            }
            else if (_buttonParameterValues.TryGetValue(key, out object storedValue))
            {
                value = storedValue;
            }
            else
            {
                value = GetDefaultValue(param.ParameterType);
                _buttonParameterValues[key] = value;
            }

            if (value == null && !param.ParameterType.IsValueType)
            {
                value = GetDefaultValue(param.ParameterType);
                _buttonParameterValues[key] = value;
            }

            return value;
        }

        private void ResetStoredButtonParameters(MethodInfo method, ParameterInfo[] parameters)
        {
            if (method == null || parameters == null)
            {
                return;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo param = parameters[i];
                if (param == null)
                {
                    continue;
                }

                string key = $"{method.Name}_{param.Name}";
                object value = param.HasDefaultValue ? param.DefaultValue : GetDefaultValue(param.ParameterType);
                _buttonParameterValues[key] = value;
                _isFirstRun[key] = false;
            }
        }

        private void InvokeButtonMethod(MethodInfo method, ParameterInfo[] parameters)
        {
            try
            {
                object[] paramValues = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo param = parameters[i];
                    if (param == null)
                    {
                        continue;
                    }

                    string key = $"{method.Name}_{param.Name}";
                    paramValues[i] = GetStoredButtonParameterValue(method, param, key);
                }

                if (method.IsStatic)
                {
                    method.Invoke(null, paramValues);
                }
                else
                {
                    method.Invoke(target, paramValues);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Error calling method {method.Name}: {e.InnerException?.Message ?? e.Message}\nStack trace: {e.InnerException?.StackTrace ?? e.StackTrace}");
            }
        }

        protected object GetDefaultValue(Type type)
        {
            if (type == typeof(int))
            {
                return 0;
            }

            if (type == typeof(float))
            {
                return 0f;
            }

            if (type == typeof(string))
            {
                return string.Empty;
            }

            if (type == typeof(bool))
            {
                return false;
            }

            if (type == typeof(Vector2))
            {
                return Vector2.zero;
            }

            if (type == typeof(Vector3))
            {
                return Vector3.zero;
            }

            if (type == typeof(Color))
            {
                return Color.white;
            }

            if (type.IsEnum)
            {
                return Enum.GetValues(type).GetValue(0);
            }

            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }
    }
}
