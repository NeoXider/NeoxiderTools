using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Neo;

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

        private static bool? _odinInspectorAvailable = null;
        
        /// <summary>
        ///     Проверяет, установлен ли Odin Inspector в проекте
        /// </summary>
        protected virtual bool IsOdinInspectorAvailable()
        {
            // Кэшируем результат проверки
            if (_odinInspectorAvailable.HasValue)
                return _odinInspectorAvailable.Value;
             
            // Проверяем наличие Odin Inspector через рефлексию
            try
            {
                var odinInspectorType = System.Type.GetType("Sirenix.OdinInspector.Editor.OdinInspector, Sirenix.OdinInspector.Editor");
                _odinInspectorAvailable = odinInspectorType != null;
            }
            catch
            {
                _odinInspectorAvailable = false;
            }
            
            return _odinInspectorAvailable.Value;
        }

        public override void OnInspectorGUI()
        {
            // Check if Reset was pressed
            if (Event.current.commandName == "Reset") _wasResetPressed = true;

            // Проверяем, установлен ли Odin Inspector
            // Если Odin Inspector активен, он сам обрабатывает кнопки и оформление
            // Поэтому мы пропускаем наше оформление и кнопки, чтобы не дублировать
            var isOdinActive = IsOdinInspectorAvailable();
            
            // Временная отладка для проверки вызова метода
            // Раскомментируйте для отладки:
            //Debug.Log($"[NeoCustomEditor] OnInspectorGUI вызван для {target?.GetType().Name}, namespace: {target?.GetType().Namespace}, Odin активен: {isOdinActive}");

            // Check if component has Neo namespace (including Neo.Tools, Neo.Shop, etc.)
            var hasNeoNamespace = target != null && target.GetType().Namespace != null && 
                                  (target.GetType().Namespace == "Neo" || target.GetType().Namespace.StartsWith("Neo."));

            // Draw "by Neoxider" signature and background for all Neo components
            // Фон и подпись рисуем всегда для Neo компонентов, независимо от Odin Inspector
            if (hasNeoNamespace)
            {
                DrawNeoxiderSignature();
                
                var originalBgColor = GUI.backgroundColor;
                GUI.backgroundColor = CustomEditorSettings.NeoBackgroundColor;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = originalBgColor;
            }

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
            // Всегда рисуем кнопки, но DrawMethodButtons сам решит, какие рисовать:
            // - Neo.ButtonAttribute - всегда рисуем
            // - Odin Inspector ButtonAttribute - рисуем только если Odin не установлен
            DrawMethodButtons();

            // Close background box for Neo components
            if (hasNeoNamespace)
            {
                EditorGUILayout.EndVertical();
            }
        }

        protected virtual void DrawNeoxiderSignature()
        {
            EditorGUILayout.Space(CustomEditorSettings.SignatureSpacing);
            
            var originalTextColor = GUI.color;
            GUI.color = CustomEditorSettings.SignatureColor;
            var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = CustomEditorSettings.SignatureFontSize,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = CustomEditorSettings.SignatureFontStyle,
                normal = { textColor = CustomEditorSettings.SignatureColor }
            };
            EditorGUILayout.LabelField("by Neoxider", style);
            GUI.color = originalTextColor;
            
            EditorGUILayout.Space(CustomEditorSettings.SignatureSpacing);
        }

        protected abstract void ProcessAttributeAssignments();

        /// <summary>
        ///     Структура для хранения информации о кнопке из разных типов ButtonAttribute
        /// </summary>
        protected struct ButtonInfo
        {
            public string ButtonName;
            public float Width;

            public ButtonInfo(string name, float width)
            {
                ButtonName = name;
                Width = width;
            }
        }

        /// <summary>
        ///     Находит ButtonAttribute из Neo или Odin Inspector и извлекает информацию
        /// </summary>
        protected ButtonInfo? FindButtonAttribute(MethodInfo method)
        {
            if (method == null) return null;

            // Получаем все атрибуты и проверяем их по типу
            // Это более надежный способ, так как не зависит от using директив
            var allAttributes = method.GetCustomAttributes(false);
            
            // Временная отладка
            // Debug.Log($"[NeoCustomEditor] FindButtonAttribute для метода {method.Name}, найдено атрибутов: {allAttributes.Length}");
            
            foreach (var attr in allAttributes)
            {
                if (attr == null) continue;
                
                var attrType = attr.GetType();
                var fullName = attrType.FullName;
                var typeName = attrType.Name;
                var namespaceName = attrType.Namespace;
                
                // Сначала проверяем Neo.ButtonAttribute (приоритет)
                if (fullName == "Neo.ButtonAttribute" || 
                    (namespaceName == "Neo" && typeName == "ButtonAttribute"))
                {
                    try
                    {
                        var neoAttr = attr as ButtonAttribute;
                        if (neoAttr != null)
                        {
                            // Neo.ButtonAttribute всегда рисуем, независимо от Odin Inspector
                            //Debug.Log($"[NeoCustomEditor] Найден Neo.ButtonAttribute для метода {method.Name}");
                            return new ButtonInfo(neoAttr.ButtonName, neoAttr.Width);
                        }
                    }
                    catch
                    {
                        // Ignore
                    }
                }
                
                // Затем проверяем Odin Inspector ButtonAttribute
                if (namespaceName == "Sirenix.OdinInspector" && typeName == "ButtonAttribute")
                {
                    // Если Odin Inspector установлен, он сам нарисует кнопки
                    // Но мы все равно рисуем их, чтобы гарантировать, что кнопки будут видны
                    // (Odin Inspector может не рисовать кнопки в некоторых случаях)
                    //Debug.Log($"[NeoCustomEditor] Найден Odin Inspector ButtonAttribute для метода {method.Name}");
                    try
                    {
                        // Извлекаем информацию из Odin Inspector ButtonAttribute через рефлексию
                        var nameProperty = attrType.GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                        if (nameProperty == null)
                        {
                            nameProperty = attrType.GetProperty("ButtonName", BindingFlags.Public | BindingFlags.Instance);
                        }
                        
                        var name = nameProperty?.GetValue(attr) as string;
                        
                        if (string.IsNullOrEmpty(name))
                        {
                            name = null;
                        }
                        
                        float width = 120f;
                        var widthProperty = attrType.GetProperty("Width", BindingFlags.Public | BindingFlags.Instance);
                        if (widthProperty != null)
                        {
                            var widthValue = widthProperty.GetValue(attr);
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
            
            // Fallback: пробуем найти через типизированный поиск (для совместимости)
            try
            {
                var neoButtonAttribute = method.GetCustomAttribute<ButtonAttribute>();
                if (neoButtonAttribute != null)
                {
                    return new ButtonInfo(neoButtonAttribute.ButtonName, neoButtonAttribute.Width);
                }
            }
            catch
            {
                // Ignore
            }
            
            return null;
        }

        protected virtual void DrawMethodButtons()
        {
            if (target == null) return;

            var methods = target.GetType().GetMethods(
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public 
                | BindingFlags.NonPublic);

            // Временная отладка (можно закомментировать после проверки)
            //Debug.Log($"[NeoCustomEditor] DrawMethodButtons для {target.GetType().Name}, найдено методов: {methods.Length}");

            foreach (var method in methods)
            {
                if (method == null) continue;

                // Ищем ButtonAttribute из Neo или Odin Inspector
                var buttonInfo = FindButtonAttribute(method);
                if (!buttonInfo.HasValue) continue;
                
                // Временная отладка (можно закомментировать после проверки)
                //Debug.Log($"[NeoCustomEditor] Найдена кнопка для метода: {method.Name}");
                
                var buttonAttribute = buttonInfo.Value;

                var parameters = method.GetParameters();
                var buttonText = string.IsNullOrEmpty(buttonAttribute.ButtonName)
                    ? method.Name
                    : buttonAttribute.ButtonName;

                if (buttonText.Length > CustomEditorSettings.ButtonTextMaxLength) 
                    buttonText = buttonText.Substring(0, CustomEditorSettings.ButtonTextMaxLength - 2) + "...";

                EditorGUILayout.BeginHorizontal();

                // Store original GUI colors
                var originalBgColor = GUI.backgroundColor;
                var originalTextColor = GUI.contentColor;
                var originalColor = GUI.color;
                
                // Create custom button style with cyberpunk look
                var buttonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    normal = { textColor = CustomEditorSettings.ButtonTextColor },
                    hover = { textColor = CustomEditorSettings.ButtonTextColor },
                    active = { textColor = CustomEditorSettings.ButtonTextColor },
                    focused = { textColor = CustomEditorSettings.ButtonTextColor }
                };
                
                // Set cyberpunk purple background for button
                GUI.backgroundColor = CustomEditorSettings.ButtonBackgroundColor;
                GUI.contentColor = CustomEditorSettings.ButtonTextColor;
                GUI.color = CustomEditorSettings.ButtonTextColor;

                // Draw button on the left with compact style
                if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Width(buttonAttribute.Width)))
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

                // Restore original GUI colors
                GUI.backgroundColor = originalBgColor;
                GUI.contentColor = originalTextColor;
                GUI.color = originalColor;

                // Add spacing between button and parameters
                GUILayout.Space(CustomEditorSettings.ButtonParameterSpacing);

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
                GUILayout.Space(CustomEditorSettings.ButtonSpacing);
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