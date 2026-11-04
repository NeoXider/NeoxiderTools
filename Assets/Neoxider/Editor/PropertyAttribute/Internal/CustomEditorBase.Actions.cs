using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor
{
    public abstract partial class CustomEditorBase
    {
        private static readonly Color ActionCardAccent = new(0.46f, 0.58f, 0.96f, 1f);
        private static readonly Color ActionsSectionAccent = new(0.66f, 0.46f, 1f, 1f);

        private static Color ActionTypeColor => NeoInspectorTheme.MutedText;

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

            string key = GetTypeFoldoutKeys(target.GetType()).Actions;
            bool current = _neoFoldouts.TryGetValue(key, out bool value) && value;
            float amount = GetFoldAmount(key, current);
            current = DrawNeoSectionHeader(current, amount, "Actions", methods.Length, ActionsSectionAccent,
                "PlayButton", false);
            _neoFoldouts[key] = current;
            amount = GetFoldAmount(key, current);

            if (BeginSectionBody(amount, ActionsSectionAccent, out bool fading))
            {
                DrawMethodButtons(methods);
                EndSectionBody(fading);
            }
        }

        private static readonly Dictionary<MethodInfo, ButtonInfo?> s_buttonInfoCache = new();

        /// <summary>
        ///     Button metadata of <paramref name="method" /> from <see cref="ButtonAttribute" /> or Odin's
        ///     <c>ButtonAttribute</c>; null when the method is not an action. Resolved once per method.
        /// </summary>
        protected ButtonInfo? FindButtonAttribute(MethodInfo method)
        {
            if (method == null)
            {
                return null;
            }

            if (!s_buttonInfoCache.TryGetValue(method, out ButtonInfo? info))
            {
                info = ResolveButtonAttribute(method);
                s_buttonInfoCache[method] = info;
            }

            return info;
        }

        private static ButtonInfo? ResolveButtonAttribute(MethodInfo method)
        {
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
                        ButtonAttribute neoAttr = attr as ButtonAttribute;
                        if (neoAttr != null)
                        {
                            return new ButtonInfo(neoAttr.ButtonName, neoAttr.Width, neoAttr.PlayModeOnly);
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
                    return new ButtonInfo(neoButtonAttribute.ButtonName, neoButtonAttribute.Width,
                        neoButtonAttribute.PlayModeOnly);
                }
            }
            catch
            {
            }

            return null;
        }

        /// <summary>Draws an action button for every [Button] method of the inspected type.</summary>
        protected virtual void DrawMethodButtons()
        {
            if (target == null)
            {
                return;
            }

            // WHY: the fallback editor calls this on every IMGUI event of every non-Neo MonoBehaviour; the cached
            // [Button] list spares a walk over every inherited Component/Object method per event.
            DrawMethodButtons(GetButtonMethods());
        }

        private const float ActionButtonHeight = 22f;
        private const float ParamsChipWidth = 74f;

        /// <summary>
        ///     Draws one action row for each method of <paramref name="methods" /> that carries a button attribute;
        ///     other methods are skipped. A method without parameters is a single button (its signature in the
        ///     tooltip); a method with parameters adds a chip that unfolds editable parameter rows.
        /// </summary>
        protected virtual void DrawMethodButtons(MethodInfo[] methods)
        {
            if (target == null || methods == null)
            {
                return;
            }

            bool isPlaying = EditorApplication.isPlaying;
            int drawn = 0;
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

                if (drawn > 0)
                {
                    NeoInspectorStyles.Space(4f);
                }

                drawn++;
                ButtonInfo buttonAttribute = buttonInfo.Value;
                ActionMethodView view = GetActionMethodView(method, buttonAttribute);
                ParameterInfo[] parameters = view.Parameters;
                bool enabled = IsButtonEnabled(buttonAttribute.PlayModeOnly, isPlaying);

                if (parameters.Length == 0)
                {
                    bool pressedPlain;
                    using (new EditorGUI.DisabledScope(!enabled))
                    {
                        pressedPlain = DrawGradientButton(view.ButtonContent, 0f, ActionButtonHeight);
                    }

                    if (pressedPlain)
                    {
                        InvokeButtonMethod(method, parameters);
                    }

                    continue;
                }

                string foldoutKey = view.FoldoutKey;
                bool open = _buttonFoldouts.TryGetValue(foldoutKey, out bool storedOpen) && storedOpen;

                Rect cardRect = EditorGUILayout.BeginVertical(NeoInspectorStyles.ActionsPanel);
                DrawActionCardChrome(cardRect, ActionCardAccent);

                bool buttonPressed;
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!enabled))
                    {
                        buttonPressed = DrawGradientButton(view.ButtonContent, 0f, ActionButtonHeight);
                    }

                    NeoInspectorStyles.HorizontalSpace(4f);
                    if (DrawParamsChip(open ? view.ExpandedToggleLabel : view.CollapsedToggleLabel, open))
                    {
                        open = !open;
                        _buttonFoldouts[foldoutKey] = open;
                        GUI.FocusControl(null);
                    }
                }

                if (open)
                {
                    NeoInspectorStyles.Space(3f);
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        ParameterInfo param = parameters[i];
                        if (param == null)
                        {
                            continue;
                        }

                        string key = view.ParameterKeys[i];
                        object value = GetStoredButtonParameterValue(method, param, key);

                        EditorGUI.BeginChangeCheck();
                        object newValue = DrawParameterFieldCore(view.ParameterLabels[i],
                            view.ParameterTypeNames[i], value, param.ParameterType, param.Name);
                        if (EditorGUI.EndChangeCheck())
                        {
                            _buttonParameterValues[key] = newValue;
                            _isFirstRun[key] = false;
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        NeoxiderEditorGUI.DrawCaption("Values are kept for this inspector session.");
                        if (DrawInlineActionButton("Reset", ActionCardAccent, 18f, false, 56f))
                        {
                            ResetStoredButtonParameters(method, parameters);
                            GUI.FocusControl(null);
                        }
                    }
                }

                EditorGUILayout.EndVertical();

                if (buttonPressed)
                {
                    InvokeButtonMethod(method, parameters);
                }
            }
        }

        /// <summary>Everything an action row shows for one [Button] method that never changes between events.</summary>
        private sealed class ActionMethodView
        {
            public ParameterInfo[] Parameters;
            public string Title;
            public GUIContent ButtonContent;
            public string FoldoutKey;
            public string CollapsedToggleLabel;
            public string ExpandedToggleLabel;
            public string[] ParameterKeys;
            public string[] ParameterLabels;
            public string[] ParameterTypeNames;
            public int TitleMaxLength;
            public string ButtonName;
            public bool PlayModeOnly;
        }

        // WHY: the row used to call GetParameters (a fresh array), MethodInfo.ToString (a signature string),
        // NicifyVariableName and string interpolation for every [Button] method on every IMGUI event.
        private static readonly Dictionary<MethodInfo, ActionMethodView> s_actionViews = new();

        private static ActionMethodView GetActionMethodView(MethodInfo method, ButtonInfo buttonInfo)
        {
            int maxLength = CustomEditorSettings.ButtonTextMaxLength;
            if (s_actionViews.TryGetValue(method, out ActionMethodView view) && view.TitleMaxLength == maxLength &&
                view.PlayModeOnly == buttonInfo.PlayModeOnly &&
                string.Equals(view.ButtonName, buttonInfo.ButtonName, StringComparison.Ordinal))
            {
                return view;
            }

            ParameterInfo[] parameters = method.GetParameters();
            string title = BuildActionTitle(method.Name, buttonInfo.ButtonName, maxLength);
            view = new ActionMethodView
            {
                Parameters = parameters,
                Title = title,
                ButtonContent = new GUIContent(title, BuildActionTooltip(method, parameters, buttonInfo.PlayModeOnly)),
                FoldoutKey = $"{method}_foldout",
                CollapsedToggleLabel = parameters.Length == 1 ? "1 param" : $"{parameters.Length} params",
                ExpandedToggleLabel = parameters.Length == 1 ? "1 param" : $"{parameters.Length} params",
                ParameterKeys = new string[parameters.Length],
                ParameterLabels = new string[parameters.Length],
                ParameterTypeNames = new string[parameters.Length],
                TitleMaxLength = maxLength,
                ButtonName = buttonInfo.ButtonName,
                PlayModeOnly = buttonInfo.PlayModeOnly
            };

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo param = parameters[i];
                if (param == null)
                {
                    continue;
                }

                view.ParameterKeys[i] = GetParameterKey(method, param);
                view.ParameterLabels[i] = ObjectNames.NicifyVariableName(param.Name);
                view.ParameterTypeNames[i] = param.ParameterType.Name;
            }

            s_actionViews[method] = view;
            return view;
        }

        /// <summary>Hover text of an action button: the C# signature plus when the button is available.</summary>
        internal static string BuildActionTooltip(MethodInfo method, ParameterInfo[] parameters, bool playModeOnly)
        {
            System.Text.StringBuilder builder = new();
            if (method.IsStatic)
            {
                builder.Append("static ");
            }

            builder.Append(method.Name).Append('(');
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(parameters[i].ParameterType.Name).Append(' ').Append(parameters[i].Name);
            }

            builder.Append(')');
            if (playModeOnly)
            {
                builder.Append("\nAvailable in Play Mode only.");
            }

            return builder.ToString();
        }

        /// <summary>Button caption: the explicit name or the method name, shortened to the configured length.</summary>
        internal static string BuildActionTitle(string methodName, string buttonName, int maxLength)
        {
            string buttonText = string.IsNullOrEmpty(buttonName) ? methodName : buttonName;
            if (maxLength > 0 && buttonText.Length > maxLength)
            {
                // WHY: a max length below the ellipsis width used to pass a negative length to Substring.
                buttonText = buttonText.Substring(0, Math.Max(0, maxLength - 2)) + "...";
            }

            return ObjectNames.NicifyVariableName(buttonText);
        }

        // WHY: the signature string keeps overloads apart; its format is the persisted session key, keep it stable.
        private static string GetParameterKey(MethodInfo method, ParameterInfo param)
        {
            return $"{method}_{param.Name}";
        }

        /// <summary>
        ///     Draws one editable action parameter row and returns the edited value. Supports int, float, string,
        ///     bool, enums, Vector2, Vector3, Color and UnityEngine.Object types; others show "Unsupported".
        /// </summary>
        /// <param name="label">Parameter name; nicified for display.</param>
        /// <param name="value">Current value.</param>
        /// <param name="type">Declared parameter type.</param>
        protected object DrawParameterField(string label, object value, Type type)
        {
            return DrawParameterFieldCore(ObjectNames.NicifyVariableName(label), type.Name, value, type, label);
        }

        private object DrawParameterFieldCore(string displayLabel, string typeName, object value, Type type,
            string rawLabel)
        {
            try
            {
                Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f);
                if (Event.current.type == EventType.Repaint)
                {
                    NeoInspectorTheme.DrawRoundedRect(rowRect, NeoInspectorTheme.RowTint, NeoInspectorTheme.RadiusRow);
                }

                Rect contentRect = new(rowRect.x + 6f, rowRect.y + 1f, rowRect.width - 10f,
                    EditorGUIUtility.singleLineHeight);
                // WHY: at narrow inspector widths the type hint goes first, so the label and the field keep room.
                bool showType = contentRect.width >= 260f;
                float labelWidth = Mathf.Clamp(contentRect.width * 0.36f, 56f, 150f);
                Rect labelRect = new(contentRect.x, contentRect.y, labelWidth, contentRect.height);
                Rect typeRect = showType
                    ? new Rect(contentRect.xMax - 64f, contentRect.y, 64f, contentRect.height)
                    : new Rect(contentRect.xMax, contentRect.y, 0f, contentRect.height);
                float fieldX = labelRect.xMax + 6f;
                Rect fieldRect = new(fieldX, contentRect.y,
                    Mathf.Max(20f, (showType ? typeRect.x - 6f : contentRect.xMax) - fieldX), contentRect.height);

                GUI.Label(labelRect, displayLabel, NeoInspectorStyles.ParameterLabel(NeoInspectorTheme.TitleText));
                if (showType)
                {
                    GUI.Label(typeRect, typeName, NeoInspectorStyles.ParameterType(ActionTypeColor));
                }

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
                    result = NeoxiderEditorGUI.DrawPillToggle(
                        new Rect(fieldRect.x, fieldRect.y, NeoxiderEditorGUI.PillToggleWidth, fieldRect.height),
                        value is bool boolValue && boolValue);
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
                Debug.LogError($"Invalid cast for parameter {rawLabel} of type {type.Name}. Resetting to default value.");
                return GetDefaultValue(type);
            }
        }

        internal static bool IsButtonEnabled(bool playModeOnly, bool isPlaying)
        {
            return !playModeOnly || isPlaying;
        }

        private static void DrawActionCardChrome(Rect rect, Color accent)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            NeoInspectorTheme.DrawRoundedRect(NeoInspectorTheme.Snap(rect), NeoInspectorTheme.SectionBackground,
                Color.Lerp(NeoInspectorTheme.Separator, new Color(accent.r, accent.g, accent.b, 0.30f), 0.5f),
                NeoInspectorTheme.RadiusCard, 1f);
        }

        /// <summary>
        ///     The chip beside a parameterised action: "N params" plus a chevron that turns when the parameter rows
        ///     unfold. Returns true when clicked.
        /// </summary>
        private static bool DrawParamsChip(string label, bool open)
        {
            Rect rect = GUILayoutUtility.GetRect(ParamsChipWidth, ActionButtonHeight,
                NeoInspectorStyles.Width(ParamsChipWidth));
            if (Event.current.type == EventType.Repaint)
            {
                Rect r = NeoInspectorTheme.Snap(rect);
                bool enabled = GUI.enabled;
                float hover = enabled
                    ? NeoInspectorAnimation.Hover(NeoInspectorAnimation.ControlKey(r),
                        r.Contains(Event.current.mousePosition))
                    : 0f;
                NeoInspectorTheme.DrawRoundedRect(r, NeoInspectorTheme.AccentFill(ActionCardAccent, 0.8f + 0.6f * hover),
                    new Color(ActionCardAccent.r, ActionCardAccent.g, ActionCardAccent.b, 0.30f + 0.25f * hover),
                    NeoInspectorTheme.RadiusButton - 1f, 1f);
                Color text = NeoInspectorTheme.AccentText(ActionCardAccent);
                GUI.Label(new Rect(r.x, r.y, r.width - 14f, r.height), label, NeoInspectorStyles.MiniChipText(text));
                NeoInspectorTheme.DrawChevron(new Rect(r.xMax - 18f, r.y, 12f, r.height), open ? 1f : 0f, text, 6f,
                    1.6f);
            }

            return GUI.Button(rect, NeoInspectorStyles.TempContent(null,
                "Show or hide the parameters this action is called with."), GUIStyle.none);
        }

        private static bool DrawInlineActionButton(string text, Color accent, float height, bool expandWidth,
            float fixedWidth = 0f)
        {
            Rect rect = fixedWidth > 0f
                ? GUILayoutUtility.GetRect(fixedWidth, height, NeoInspectorStyles.Width(fixedWidth))
                : GUILayoutUtility.GetRect(90f, height, expandWidth
                    ? NeoInspectorStyles.ExpandWidth
                    : NeoInspectorStyles.MinWidth(90f));

            if (Event.current.type == EventType.Repaint)
            {
                Rect r = NeoInspectorTheme.Snap(rect);
                float hover = GUI.enabled
                    ? NeoInspectorAnimation.Hover(NeoInspectorAnimation.ControlKey(r),
                        r.Contains(Event.current.mousePosition))
                    : 0f;
                NeoInspectorTheme.DrawRoundedRect(r, NeoInspectorTheme.AccentFill(accent, 0.7f + 0.6f * hover),
                    new Color(accent.r, accent.g, accent.b, 0.28f + 0.25f * hover), NeoInspectorTheme.RadiusRow, 1f);
                GUI.Label(r, text, NeoInspectorStyles.MiniChipText(NeoInspectorTheme.AccentText(accent)));
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

                string key = GetParameterKey(method, param);
                object value = param.HasDefaultValue ? param.DefaultValue : GetDefaultValue(param.ParameterType);
                _buttonParameterValues[key] = value;
                _isFirstRun[key] = false;
            }
        }

        /// <summary>
        ///     Runs a [Button] method with the stored parameter values. Static methods run once; instance methods run
        ///     on every inspected object, because the inspector edits multiple objects and a button pressed with
        ///     several objects selected is expected to act on each of them. Outside Play Mode the call is recorded
        ///     with Undo, so serialized changes it makes are undoable and mark the object dirty.
        /// </summary>
        internal void InvokeButtonMethod(MethodInfo method, ParameterInfo[] parameters)
        {
            object[] paramValues;
            try
            {
                paramValues = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo param = parameters[i];
                    if (param == null)
                    {
                        continue;
                    }

                    paramValues[i] = GetStoredButtonParameterValue(method, param, GetParameterKey(method, param));
                }
            }
            catch (Exception e)
            {
                LogButtonFailure(method, e);
                return;
            }

            if (method.IsStatic)
            {
                InvokeButtonOn(method, null, paramValues);
                return;
            }

            Object[] inspected = targets;
            if (inspected == null || inspected.Length <= 1)
            {
                InvokeButtonOn(method, target, paramValues);
                return;
            }

            for (int i = 0; i < inspected.Length; i++)
            {
                if (inspected[i] != null)
                {
                    InvokeButtonOn(method, inspected[i], paramValues);
                }
            }
        }

        private static void InvokeButtonOn(MethodInfo method, Object instance, object[] paramValues)
        {
            try
            {
                if (instance != null && !EditorApplication.isPlaying)
                {
                    Undo.RecordObject(instance, ObjectNames.NicifyVariableName(method.Name));
                }

                method.Invoke(instance, paramValues);
            }
            catch (Exception e)
            {
                LogButtonFailure(method, e);
            }
        }

        private static void LogButtonFailure(MethodInfo method, Exception e)
        {
            Debug.LogError(
                $"Error calling method {method.Name}: {e.InnerException?.Message ?? e.Message}\nStack trace: {e.InnerException?.StackTrace ?? e.StackTrace}");
        }

        /// <summary>Default value used for an action parameter without a declared default.</summary>
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
