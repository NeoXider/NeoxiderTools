using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor
{
    /// <summary>
    ///     Base class for custom editors that provides common functionality
    /// </summary>
    public abstract class CustomEditorBase : UnityEditor.Editor
    {
        // Binding flags for field reflection
        public const BindingFlags FIELD_FLAGS =
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance;

        protected Dictionary<string, bool> _buttonFoldouts = new();

        protected Dictionary<string, object> _buttonParameterValues = new();
        protected Dictionary<string, bool> _isFirstRun = new();

        // Track if Reset was pressed
        private bool _wasResetPressed;

        // Utility methods for finding objects
        protected static T FindFirstObjectByType<T>() where T : Object
        {
            return Object.FindFirstObjectByType<T>();
        }

        protected static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode = FindObjectsSortMode.None)
            where T : Object
        {
            return Object.FindObjectsByType<T>(sortMode);
        }

        protected static Object FindFirstObjectByType(Type type)
        {
            return Object.FindFirstObjectByType(type);
        }

        protected static Object[] FindObjectsByType(Type type,
            FindObjectsSortMode sortMode = FindObjectsSortMode.None)
        {
            return Object.FindObjectsByType(type, sortMode);
        }

        public override void OnInspectorGUI()
        {
            // Check if Reset was pressed
            if (Event.current.commandName == "Reset") _wasResetPressed = true;

            // Draw default inspector
            base.OnInspectorGUI();

            // Clear button parameters if Reset was pressed
            if (_wasResetPressed)
            {
                _buttonParameterValues.Clear();
                _buttonFoldouts.Clear();
                _isFirstRun.Clear();
                _wasResetPressed = false;
            }

            // Process custom attributes if enabled
            ProcessAttributeAssignments();

            // Draw method buttons
            DrawMethodButtons();
        }

        protected abstract void ProcessAttributeAssignments();

        protected virtual void DrawMethodButtons()
        {
            if (target == null) return;

            var methods = target.GetType().GetMethods(
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                if (method == null) continue;

                var buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();
                if (buttonAttribute == null) continue;

                var parameters = method.GetParameters();
                var buttonText = string.IsNullOrEmpty(buttonAttribute.ButtonName)
                    ? method.Name
                    : buttonAttribute.ButtonName;

                if (buttonText.Length > 16) buttonText = buttonText.Substring(0, 14) + "...";

                EditorGUILayout.BeginHorizontal();

                // Store original GUI background color
                var originalColor = GUI.backgroundColor;
                // Set darker background for button
                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);

                // Draw button on the left with compact style
                if (GUILayout.Button(buttonText, EditorStyles.miniButton, GUILayout.Width(buttonAttribute.Width)))
                    try
                    {
                        var paramValues = new object[parameters.Length];
                        for (var i = 0; i < parameters.Length; i++)
                        {
                            var param = parameters[i];
                            if (param == null) continue;

                            var key = $"{method.Name}_{param.Name}";
                            object value = null;

                            if (!_isFirstRun.ContainsKey(key)) _isFirstRun[key] = true;

                            if (_isFirstRun[key] && param.HasDefaultValue)
                            {
                                value = param.DefaultValue;
                                _buttonParameterValues[key] = value;
                                _isFirstRun[key] = false;
                            }
                            else if (_buttonParameterValues.TryGetValue(key, out var storedValue))
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
                                Debug.LogWarning(
                                    $"Parameter '{param.Name}' of method '{method.Name}' is null. Using default value.");
                                value = GetDefaultValue(param.ParameterType);
                                _buttonParameterValues[key] = value;
                            }

                            paramValues[i] = value;
                        }

                        if (method.IsStatic)
                            method.Invoke(null, paramValues);
                        else
                            method.Invoke(target, paramValues);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"Error calling method {method.Name}: {e.InnerException?.Message ?? e.Message}\nStack trace: {e.InnerException?.StackTrace ?? e.StackTrace}");
                    }

                // Restore original GUI background color
                GUI.backgroundColor = originalColor;

                // Add spacing between button and parameters
                GUILayout.Space(20);

                // Draw parameters foldout on the right
                if (parameters.Length > 0)
                {
                    var foldoutKey = $"{method.Name}_foldout";
                    if (!_buttonFoldouts.ContainsKey(foldoutKey)) _buttonFoldouts[foldoutKey] = false;

                    EditorGUILayout.BeginVertical();
                    _buttonFoldouts[foldoutKey] =
                        EditorGUILayout.Foldout(_buttonFoldouts[foldoutKey], "Parameters", true);

                    if (_buttonFoldouts[foldoutKey])
                    {
                        // Begin a new horizontal group to reset the indentation
                        EditorGUILayout.BeginHorizontal();
                        // Add fixed space instead of indentation
                        GUILayout.Space(0);

                        // Begin vertical group for parameters
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        for (var i = 0; i < parameters.Length; i++)
                        {
                            var param = parameters[i];
                            if (param == null) continue;

                            var key = $"{method.Name}_{param.Name}";
                            object value = null;

                            if (!_isFirstRun.ContainsKey(key)) _isFirstRun[key] = true;

                            if (_isFirstRun[key] && param.HasDefaultValue)
                            {
                                value = param.DefaultValue;
                                _buttonParameterValues[key] = value;
                                _isFirstRun[key] = false;
                            }
                            else if (_buttonParameterValues.TryGetValue(key, out var storedValue))
                            {
                                value = storedValue;
                            }
                            else
                            {
                                value = GetDefaultValue(param.ParameterType);
                                _buttonParameterValues[key] = value;
                            }

                            EditorGUI.BeginChangeCheck();
                            var newValue = DrawParameterField(param.Name, value, param.ParameterType);
                            if (EditorGUI.EndChangeCheck())
                            {
                                _buttonParameterValues[key] = newValue;
                                _isFirstRun[key] = false;
                            }
                        }

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndHorizontal();

                // Add small space between methods
                GUILayout.Space(2);
            }
        }

        protected object DrawParameterField(string label, object value, Type type)
        {
            try
            {
                // Begin horizontal to control layout
                EditorGUILayout.BeginHorizontal();

                // Remove any existing indent
                var indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                object result = null;
                if (type == typeof(int))
                {
                    result = EditorGUILayout.IntField(label, value is int intValue ? intValue : 0);
                }
                else if (type == typeof(float))
                {
                    result = EditorGUILayout.FloatField(label, value is float floatValue ? floatValue : 0f);
                }
                else if (type == typeof(string))
                {
                    result = EditorGUILayout.TextField(label, value is string stringValue ? stringValue : string.Empty);
                }
                else if (type == typeof(bool))
                {
                    var currentValue = value is bool boolValue ? boolValue : false;
                    result = EditorGUILayout.Toggle(label, currentValue);
                }
                else if (type == typeof(GameObject))
                {
                    result = EditorGUILayout.ObjectField(label, value as GameObject, typeof(GameObject), true);
                }
                else if (typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    result = EditorGUILayout.ObjectField(label, value as MonoBehaviour, type, true);
                }
                else if (type.IsEnum)
                {
                    if (value == null || !type.IsInstanceOfType(value)) value = Enum.GetValues(type).GetValue(0);

                    result = EditorGUILayout.EnumPopup(label, (Enum)value);
                }
                else if (type == typeof(Vector2))
                {
                    result = EditorGUILayout.Vector2Field(label,
                        value is Vector2 vector2Value ? vector2Value : Vector2.zero);
                }
                else if (type == typeof(Vector3))
                {
                    result = EditorGUILayout.Vector3Field(label,
                        value is Vector3 vector3Value ? vector3Value : Vector3.zero);
                }
                else if (type == typeof(Color))
                {
                    result = EditorGUILayout.ColorField(label, value is Color colorValue ? colorValue : Color.white);
                }
                else
                {
                    EditorGUILayout.LabelField(label, "Unsupported parameter type: " + type.Name);
                    result = value;
                }

                // Restore the original indent level
                EditorGUI.indentLevel = indent;

                EditorGUILayout.EndHorizontal();
                return result;
            }
            catch (InvalidCastException)
            {
                Debug.LogError($"Invalid cast for parameter {label} of type {type.Name}. Resetting to default value.");
                return GetDefaultValue(type);
            }
        }

        protected object GetDefaultValue(Type type)
        {
            if (type == typeof(int)) return 0;
            if (type == typeof(float)) return 0f;
            if (type == typeof(string)) return string.Empty;
            if (type == typeof(bool)) return false;
            if (type == typeof(Vector2)) return Vector2.zero;
            if (type == typeof(Vector3)) return Vector3.zero;
            if (type == typeof(Color)) return Color.white;
            if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
            if (type.IsValueType) return Activator.CreateInstance(type);
            return null;
        }
    }
}