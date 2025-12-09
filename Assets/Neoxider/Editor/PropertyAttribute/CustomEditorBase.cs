using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using GUI = UnityEngine.GUI;

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

        private static bool _isAnimating;

        private static bool? _odinInspectorAvailable;
        private static string _cachedVersion;

        protected Dictionary<string, bool> _buttonFoldouts = new();

        protected Dictionary<string, object> _buttonParameterValues = new();

        private Rect _componentOutlineRect;
        protected Dictionary<string, bool> _isFirstRun = new();

        private Rect _rainbowLineStartRect;
        private float _rainbowLineStartY;

        // Track if Reset was pressed
        private bool _wasResetPressed;

        protected virtual void OnDisable()
        {
            if (_isAnimating)
            {
                EditorApplication.update -= OnEditorUpdate;
                _isAnimating = false;
            }
        }

        protected void EnsureRepaint()
        {
            if (!_isAnimating)
            {
                _isAnimating = true;
                EditorApplication.update += OnEditorUpdate;
            }
        }

        private void OnEditorUpdate()
        {
            if (target != null)
            {
                Repaint();
            }
        }

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

        /// <summary>
        ///     Получает путь к текущему файлу скрипта
        /// </summary>
        private static string GetScriptPath([CallerFilePath] string sourceFilePath = "")
        {
            // CallerFilePath возвращает абсолютный путь, нужно преобразовать в относительный Unity путь
            if (!string.IsNullOrEmpty(sourceFilePath))
            {
                // Для работы как из Assets, так и из Packages
                string projectPath = Directory.GetCurrentDirectory();
                if (sourceFilePath.StartsWith(projectPath))
                {
                    sourceFilePath = sourceFilePath.Substring(projectPath.Length + 1).Replace('\\', '/');
                }
            }

            return sourceFilePath;
        }

        /// <summary>
        ///     Получает версию пакета Neoxider из package.json
        /// </summary>
        protected virtual string GetNeoxiderVersion()
        {
            if (!string.IsNullOrEmpty(_cachedVersion))
            {
                return _cachedVersion;
            }

            try
            {
                // Получаем путь к текущему скрипту и поднимаемся до package.json
                string directory = Path.GetDirectoryName(GetScriptPath());

                while (!string.IsNullOrEmpty(directory))
                {
                    string packagePath = Path.Combine(directory, "package.json");

                    if (File.Exists(packagePath))
                    {
                        string json = File.ReadAllText(packagePath);

                        // Проверяем, что это Neoxider Tools пакет
                        if (json.Contains("\"displayName\": \"Neoxider Tools\"") ||
                            json.Contains("\"name\": \"com.neoxider.tools\""))
                        {
                            // Парсим версию
                            int versionIndex = json.IndexOf("\"version\":");
                            if (versionIndex != -1)
                            {
                                int startQuote = json.IndexOf("\"", versionIndex + 10);
                                int endQuote = json.IndexOf("\"", startQuote + 1);
                                if (startQuote != -1 && endQuote != -1)
                                {
                                    _cachedVersion = json.Substring(startQuote + 1, endQuote - startQuote - 1).Trim();
                                    return _cachedVersion;
                                }
                            }
                        }
                    }

                    // Поднимаемся на уровень выше
                    directory = Directory.GetParent(directory)?.FullName;
                }
            }
            catch
            {
                // Игнорируем ошибки
            }

            _cachedVersion = "Unknown";
            return _cachedVersion;
        }

        /// <summary>
        ///     Проверяет, установлен ли Odin Inspector в проекте
        /// </summary>
        protected virtual bool IsOdinInspectorAvailable()
        {
            // Кэшируем результат проверки
            if (_odinInspectorAvailable.HasValue)
            {
                return _odinInspectorAvailable.Value;
            }

            // Проверяем наличие Odin Inspector через рефлексию
            try
            {
                Type odinInspectorType =
                    Type.GetType("Sirenix.OdinInspector.Editor.OdinInspector, Sirenix.OdinInspector.Editor");
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
            if (Event.current.commandName == "Reset")
            {
                _wasResetPressed = true;
            }

            // Проверяем, установлен ли Odin Inspector
            // Если Odin Inspector активен, он сам обрабатывает кнопки и оформление
            // Поэтому мы пропускаем наше оформление и кнопки, чтобы не дублировать
            bool isOdinActive = IsOdinInspectorAvailable();

            // Check if component has Neo namespace (including Neo.Tools, Neo.Shop, etc.)
            bool hasNeoNamespace = false;

            if (target != null && target.GetType().Namespace != null)
            {
                string targetNamespace = target.GetType().Namespace;

                // Проверяем точное совпадение "Neo" или начало с "Neo."
                hasNeoNamespace = targetNamespace == "Neo" || targetNamespace.StartsWith("Neo.");

                // Дополнительная проверка для вложенных namespace (например, Neo { namespace Audio)
                // В таких случаях полный namespace будет "Neo.Audio"
                if (!hasNeoNamespace && targetNamespace.Contains("."))
                {
                    // Проверяем каждую часть namespace
                    string[] parts = targetNamespace.Split('.');
                    hasNeoNamespace = parts.Length > 0 && parts[0] == "Neo";
                }

                // Debug для диагностики
                // Раскомментируйте эти строки если компоненты Neo.Tools не отображаются с оформлением:
                // if (targetNamespace.Contains("Neo") && targetNamespace.Contains("Tools"))
                // {
                //     Debug.Log($"[Neo Debug] Component: {target.GetType().FullName}, Namespace: {targetNamespace}, " +
                //              $"Has Neo Namespace: {hasNeoNamespace}, Assembly: {target.GetType().Assembly.GetName().Name}");
                // }
            }

            if (hasNeoNamespace)
            {
                DrawNeoxiderSignature();

                Color originalBgColor = GUI.backgroundColor;
                GUI.backgroundColor = CustomEditorSettings.NeoBackgroundColor;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = originalBgColor;

                if (CustomEditorSettings.EnableRainbowComponentOutline)
                {
                    BeginRainbowLineTracking();
                }
            }

            base.OnInspectorGUI();

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
            // Всегда рисуем кнопки, независимо от того, установлен Odin Inspector или нет:
            // - Neo.ButtonAttribute - всегда рисуем
            // - Odin Inspector ButtonAttribute - всегда рисуем (для совместимости и гарантии видимости)
            DrawMethodButtons();

            if (hasNeoNamespace)
            {
                if (CustomEditorSettings.EnableRainbowComponentOutline)
                {
                    EndRainbowLineTracking();
                }

                EditorGUILayout.EndVertical();
            }
        }

        protected virtual void DrawNeoxiderSignature()
        {
            EditorGUILayout.Space(CustomEditorSettings.SignatureSpacing);

            if (CustomEditorSettings.EnableRainbowSignature && CustomEditorSettings.EnableRainbowSignatureAnimation)
            {
                EnsureRepaint();
            }

            Color originalTextColor = GUI.color;

            Color signatureColor = CustomEditorSettings.SignatureColor;
            if (CustomEditorSettings.EnableRainbowSignature && CustomEditorSettings.EnableRainbowSignatureAnimation)
            {
                signatureColor = GetRainbowColor(CustomEditorSettings.RainbowSpeed);
            }

            GUI.color = signatureColor;
            GUIStyle style = new(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = CustomEditorSettings.SignatureFontSize,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = CustomEditorSettings.SignatureFontStyle,
                normal = { textColor = signatureColor }
            };

            string version = GetNeoxiderVersion();
            string signatureText = $"by Neoxider v{version}";

            if (CustomEditorSettings.EnableRainbowOutline)
            {
                DrawTextWithRainbowOutline(signatureText, style);
            }
            else
            {
                EditorGUILayout.LabelField(signatureText, style);
            }

            GUI.color = originalTextColor;

            EditorGUILayout.Space(CustomEditorSettings.SignatureSpacing);
        }

        /// <summary>
        ///     Генерирует радужный цвет на основе времени
        /// </summary>
        private Color GetRainbowColor(float speed)
        {
            float time = (float)EditorApplication.timeSinceStartup * speed;
            float hue = Mathf.Repeat(time, 1f);
            return Color.HSVToRGB(hue, CustomEditorSettings.RainbowSaturation, CustomEditorSettings.RainbowBrightness);
        }

        /// <summary>
        ///     Рисует текст с радужной обводкой
        /// </summary>
        private void DrawTextWithRainbowOutline(string text, GUIStyle baseStyle)
        {
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(text), baseStyle);

            float outlineSize = CustomEditorSettings.RainbowOutlineSize;
            float time = (float)EditorApplication.timeSinceStartup * CustomEditorSettings.RainbowSpeed;

            GUIStyle outlineStyle = new(baseStyle);

            for (int angle = 0; angle < 360; angle += 45)
            {
                float radian = angle * Mathf.Deg2Rad;
                float offsetX = Mathf.Cos(radian) * outlineSize;
                float offsetY = Mathf.Sin(radian) * outlineSize;

                float hue = Mathf.Repeat(time + angle / 360f * 0.2f, 1f);
                Color outlineColor = Color.HSVToRGB(hue,
                    CustomEditorSettings.RainbowSaturation,
                    CustomEditorSettings.RainbowBrightness * 0.8f);
                outlineColor.a = CustomEditorSettings.RainbowOutlineAlpha;

                outlineStyle.normal.textColor = outlineColor;

                Rect offsetRect = new(rect.x + offsetX, rect.y + offsetY, rect.width, rect.height);
                GUI.Label(offsetRect, text, outlineStyle);
            }

            GUI.Label(rect, text, baseStyle);
        }

        /// <summary>
        ///     Начинает рисование радужной обводки вокруг компонента
        /// </summary>
        private void DrawRainbowComponentOutlineBegin()
        {
            _componentOutlineRect = EditorGUILayout.BeginVertical();
        }

        /// <summary>
        ///     Завершает рисование радужной обводки вокруг компонента
        /// </summary>
        private void DrawRainbowComponentOutlineEnd()
        {
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                float time = (float)EditorApplication.timeSinceStartup * CustomEditorSettings.RainbowSpeed;
                float borderWidth = CustomEditorSettings.RainbowComponentOutlineWidth;

                Rect rect = _componentOutlineRect;
                rect.x -= borderWidth;
                rect.y -= borderWidth;
                rect.width += borderWidth * 2;
                rect.height += borderWidth * 2;

                DrawRainbowBorder(rect, borderWidth, time);
            }
        }

        /// <summary>
        ///     Начинает отслеживание позиции для радужной линии
        /// </summary>
        private void BeginRainbowLineTracking()
        {
            if (CustomEditorSettings.EnableRainbowLineAnimation)
            {
                EnsureRepaint();
            }

            // Используем небольшой rect чтобы получить текущую Y позицию
            Rect rect = EditorGUILayout.GetControlRect(false, 0);

            if (Event.current.type == EventType.Repaint)
            {
                _rainbowLineStartY = rect.y;
            }
        }

        /// <summary>
        ///     Завершает отслеживание и рисует радужную линию
        /// </summary>
        private void EndRainbowLineTracking()
        {
            if (Event.current.type == EventType.Repaint)
            {
                float lineWidth = 3f;
                float time = CustomEditorSettings.EnableRainbowLineAnimation
                    ? (float)EditorApplication.timeSinceStartup * CustomEditorSettings.RainbowSpeed * 5f
                    : 0f;

                Color[] rainbowColors =
                {
                    new(0.9f, 0.2f, 0.2f),
                    new(1f, 0.5f, 0.2f),
                    new(1f, 0.9f, 0.2f),
                    new(0.3f, 0.9f, 0.3f),
                    new(0.2f, 0.7f, 1f),
                    new(0.3f, 0.3f, 1f),
                    new(0.7f, 0.3f, 1f)
                };

                Rect lastRect = GUILayoutUtility.GetLastRect();
                float lineHeight = lastRect.yMax - _rainbowLineStartY;

                if (lineHeight > 0)
                {
                    float inspectorWidth = EditorGUIUtility.currentViewWidth;
                    float lineX = 16f;

                    int segments = Mathf.Max(10, Mathf.FloorToInt(lineHeight / 5f));
                    float segmentHeight = lineHeight / segments;

                    for (int i = 0; i < segments; i++)
                    {
                        float t = i / (float)segments;
                        t = Mathf.Repeat(t + time, 1f);

                        int colorIndex = Mathf.FloorToInt(t * (rainbowColors.Length - 1));
                        float localT = t * (rainbowColors.Length - 1) - colorIndex;

                        Color color = Color.Lerp(
                            rainbowColors[Mathf.Min(colorIndex, rainbowColors.Length - 1)],
                            rainbowColors[Mathf.Min(colorIndex + 1, rainbowColors.Length - 1)],
                            localT
                        );

                        Rect segmentRect = new(
                            lineX,
                            _rainbowLineStartY + i * segmentHeight,
                            lineWidth,
                            segmentHeight + 1
                        );

                        EditorGUI.DrawRect(segmentRect, color);
                    }
                }
            }
        }

        /// <summary>
        ///     Рисует радужную рамку вокруг прямоугольника
        /// </summary>
        private void DrawRainbowBorder(Rect rect, float borderWidth, float time)
        {
            int segments = 40;
            float perimeter = (rect.width + rect.height) * 2;
            float segmentLength = perimeter / segments;

            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / segments;
                float hue = Mathf.Repeat(time + t, 1f);
                Color color = Color.HSVToRGB(hue,
                    CustomEditorSettings.RainbowSaturation,
                    CustomEditorSettings.RainbowBrightness);

                float nextT = (float)(i + 1) / segments;
                Vector2 start = GetPointOnRectPerimeter(rect, t);
                Vector2 end = GetPointOnRectPerimeter(rect, nextT);

                Handles.BeginGUI();
                Handles.color = color;
                Handles.DrawAAPolyLine(borderWidth, start, end);
                Handles.EndGUI();
            }
        }

        /// <summary>
        ///     Получает точку на периметре прямоугольника
        /// </summary>
        private Vector2 GetPointOnRectPerimeter(Rect rect, float t)
        {
            float perimeter = (rect.width + rect.height) * 2;
            float distance = t * perimeter;

            if (distance < rect.width)
            {
                return new Vector2(rect.x + distance, rect.y);
            }

            distance -= rect.width;

            if (distance < rect.height)
            {
                return new Vector2(rect.xMax, rect.y + distance);
            }

            distance -= rect.height;

            if (distance < rect.width)
            {
                return new Vector2(rect.xMax - distance, rect.yMax);
            }

            distance -= rect.width;

            return new Vector2(rect.x, rect.yMax - distance);
        }

        /// <summary>
        ///     Рисует закруглённую кнопку с градиентом
        /// </summary>
        private bool DrawGradientButton(string text, float width, float height = 0)
        {
            if (height == 0)
            {
                height = GradientButtonSettings.DefaultButtonHeight;
            }

            Rect buttonRect = GUILayoutUtility.GetRect(width, height);

            if (Event.current.type == EventType.Repaint)
            {
                // Получаем цвета из настроек
                Color topColor = GradientButtonSettings.TopColor;
                Color bottomColor = GradientButtonSettings.BottomColor;

                // Проверка на hover
                bool isHover = buttonRect.Contains(Event.current.mousePosition);
                if (isHover)
                {
                    topColor = Color.Lerp(topColor, Color.white, GradientButtonSettings.HoverBrightness);
                    bottomColor = Color.Lerp(bottomColor, Color.white, GradientButtonSettings.HoverBrightness);
                }

                // Рисуем градиент через несколько прямоугольников
                for (int i = 0; i < GradientButtonSettings.GradientSegments; i++)
                {
                    float t = i / (float)GradientButtonSettings.GradientSegments;

                    Color segmentColor = Color.Lerp(topColor, bottomColor, t);

                    Rect segmentRect = new(
                        buttonRect.x,
                        buttonRect.y + buttonRect.height * t,
                        buttonRect.width,
                        buttonRect.height / GradientButtonSettings.GradientSegments + 1
                    );

                    EditorGUI.DrawRect(segmentRect, segmentColor);
                }

                // Рисуем закруглённые углы поверх (эффект скругления)
                DrawRoundedCorners(buttonRect, GradientButtonSettings.CornerRadius, topColor, bottomColor);

                Handles.BeginGUI();

                // Неоновая обводка (если включена)
                if (GradientButtonSettings.EnableNeonGlow)
                {
                    // Внешнее свечение
                    Handles.color = new Color(
                        GradientButtonSettings.NeonGlowColor.r,
                        GradientButtonSettings.NeonGlowColor.g,
                        GradientButtonSettings.NeonGlowColor.b,
                        0.15f
                    );

                    float glowWidth = 4f;
                    Vector3[] points =
                    {
                        new(buttonRect.x + GradientButtonSettings.CornerRadius, buttonRect.y - 1),
                        new(buttonRect.xMax - GradientButtonSettings.CornerRadius, buttonRect.y - 1),
                        new(buttonRect.xMax + 1, buttonRect.y + GradientButtonSettings.CornerRadius),
                        new(buttonRect.xMax + 1, buttonRect.yMax - GradientButtonSettings.CornerRadius),
                        new(buttonRect.xMax - GradientButtonSettings.CornerRadius, buttonRect.yMax + 1),
                        new(buttonRect.x + GradientButtonSettings.CornerRadius, buttonRect.yMax + 1),
                        new(buttonRect.x - 1, buttonRect.yMax - GradientButtonSettings.CornerRadius),
                        new(buttonRect.x - 1, buttonRect.y + GradientButtonSettings.CornerRadius),
                        new(buttonRect.x + GradientButtonSettings.CornerRadius, buttonRect.y - 1)
                    };

                    Handles.DrawAAPolyLine(glowWidth, points);

                    // Яркая внутренняя обводка
                    Handles.color = GradientButtonSettings.NeonGlowColor;
                    Handles.DrawAAPolyLine(1.5f, points);
                }
                else
                {
                    // Стандартная обводка сверху
                    Handles.color = GradientButtonSettings.HighlightColor;
                    Handles.DrawAAPolyLine(GradientButtonSettings.HighlightWidth,
                        new Vector3(buttonRect.x + GradientButtonSettings.CornerRadius, buttonRect.y),
                        new Vector3(buttonRect.xMax - GradientButtonSettings.CornerRadius, buttonRect.y)
                    );
                }

                Handles.EndGUI();
            }

            // Текст кнопки с тенью для киберпанк стиля
            if (GradientButtonSettings.EnableNeonGlow)
            {
                // Тень текста
                GUIStyle shadowStyle = new(EditorStyles.label)
                {
                    alignment = GradientButtonSettings.TextAlignment,
                    fontStyle = GradientButtonSettings.TextStyle,
                    normal = { textColor = new Color(0, 0, 0, 0.5f) }
                };

                Rect shadowRect = new(buttonRect.x, buttonRect.y + 1, buttonRect.width, buttonRect.height);
                GUI.Label(shadowRect, text, shadowStyle);
            }

            // Основной текст
            GUIStyle textStyle = new(EditorStyles.label)
            {
                alignment = GradientButtonSettings.TextAlignment,
                fontStyle = GradientButtonSettings.TextStyle,
                normal = { textColor = GradientButtonSettings.TextColor }
            };

            GUI.Label(buttonRect, text, textStyle);

            return Event.current.type == EventType.MouseDown && buttonRect.Contains(Event.current.mousePosition);
        }

        /// <summary>
        ///     Рисует эффект закруглённых углов
        /// </summary>
        private void DrawRoundedCorners(Rect rect, float radius, Color topColor, Color bottomColor)
        {
            Color bgColor = GradientButtonSettings.InspectorBackgroundColor;

            // Верхние углы
            DrawCornerMask(new Rect(rect.x, rect.y, radius, radius), radius, bgColor, true, true);
            DrawCornerMask(new Rect(rect.xMax - radius, rect.y, radius, radius), radius, bgColor, false, true);

            // Нижние углы
            DrawCornerMask(new Rect(rect.x, rect.yMax - radius, radius, radius), radius, bgColor, true, false);
            DrawCornerMask(new Rect(rect.xMax - radius, rect.yMax - radius, radius, radius), radius, bgColor, false,
                false);
        }

        /// <summary>
        ///     Рисует маску угла для создания эффекта скругления
        /// </summary>
        private void DrawCornerMask(Rect cornerRect, float radius, Color bgColor, bool isLeft, bool isTop)
        {
            Vector2 center = isLeft
                ? isTop ? new Vector2(cornerRect.xMax, cornerRect.yMax) : new Vector2(cornerRect.xMax, cornerRect.y)
                : isTop
                    ? new Vector2(cornerRect.x, cornerRect.yMax)
                    : new Vector2(cornerRect.x, cornerRect.y);

            // Используем более высокое разрешение для плавного скругления
            int steps = GradientButtonSettings.CornerMaskSteps;
            float pixelSize = cornerRect.width / steps;

            for (int x = 0; x < steps; x++)
            {
                for (int y = 0; y < steps; y++)
                {
                    // Центр каждого "пикселя"
                    float px = cornerRect.x + (x + 0.5f) / steps * cornerRect.width;
                    float py = cornerRect.y + (y + 0.5f) / steps * cornerRect.height;

                    float dist = Vector2.Distance(new Vector2(px, py), center);

                    // Более точная проверка расстояния с небольшим сглаживанием
                    if (dist > radius + pixelSize * 0.5f)
                    {
                        Rect pixelRect = new(
                            cornerRect.x + x * pixelSize,
                            cornerRect.y + y * pixelSize,
                            pixelSize + 0.5f,
                            pixelSize + 0.5f
                        );
                        EditorGUI.DrawRect(pixelRect, bgColor);
                    }
                }
            }
        }

        protected abstract void ProcessAttributeAssignments();

        /// <summary>
        ///     Находит ButtonAttribute из Neo или Odin Inspector и извлекает информацию
        /// </summary>
        protected ButtonInfo? FindButtonAttribute(MethodInfo method)
        {
            if (method == null)
            {
                return null;
            }

            // Получаем все атрибуты и проверяем их по типу
            // Это более надежный способ, так как не зависит от using директив
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

                // Сначала проверяем Neo.ButtonAttribute (приоритет)
                if (fullName == "Neo.ButtonAttribute" ||
                    (namespaceName == "Neo" && typeName == "ButtonAttribute"))
                {
                    try
                    {
                        ButtonAttribute neoAttr = attr as ButtonAttribute;
                        if (neoAttr != null)
                        {
                            // Neo.ButtonAttribute всегда рисуем, независимо от Odin Inspector
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
                    // Всегда рисуем кнопки Odin Inspector через наш CustomEditorBase
                    // Это гарантирует, что кнопки будут видны даже если Odin Inspector не рисует их
                    try
                    {
                        // Извлекаем информацию из Odin Inspector ButtonAttribute через рефлексию
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

            // Fallback: пробуем найти через типизированный поиск (для совместимости)
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
                // Ignore
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

            foreach (MethodInfo method in methods)
            {
                if (method == null)
                {
                    continue;
                }

                // Ищем ButtonAttribute из Neo или Odin Inspector
                ButtonInfo? buttonInfo = FindButtonAttribute(method);
                if (!buttonInfo.HasValue)
                {
                    continue;
                }

                // Проверяем, установлен ли Odin Inspector, но все равно рисуем кнопки
                // Это гарантирует, что кнопки будут видны даже если Odin Inspector не рисует их

                ButtonInfo buttonAttribute = buttonInfo.Value;

                ParameterInfo[] parameters = method.GetParameters();
                string buttonText = string.IsNullOrEmpty(buttonAttribute.ButtonName)
                    ? method.Name
                    : buttonAttribute.ButtonName;

                if (buttonText.Length > CustomEditorSettings.ButtonTextMaxLength)
                {
                    buttonText = buttonText.Substring(0, CustomEditorSettings.ButtonTextMaxLength - 2) + "...";
                }

                EditorGUILayout.BeginHorizontal();

                // Draw gradient button with rounded corners
                bool buttonPressed = DrawGradientButton(buttonText, buttonAttribute.Width);

                // Add small spacing after button
                GUILayout.Space(GradientButtonSettings.ButtonSpacing);

                if (buttonPressed)
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
                            object value = null;

                            if (!_isFirstRun.ContainsKey(key))
                            {
                                _isFirstRun[key] = true;
                            }

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
                                Debug.LogWarning(
                                    $"Parameter '{param.Name}' of method '{method.Name}' is null. Using default value.");
                                value = GetDefaultValue(param.ParameterType);
                                _buttonParameterValues[key] = value;
                            }

                            paramValues[i] = value;
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

                // Add spacing between button and parameters
                GUILayout.Space(CustomEditorSettings.ButtonParameterSpacing);

                // Draw parameters foldout on the right
                if (parameters.Length > 0)
                {
                    string foldoutKey = $"{method.Name}_foldout";
                    if (!_buttonFoldouts.ContainsKey(foldoutKey))
                    {
                        _buttonFoldouts[foldoutKey] = false;
                    }

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

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            ParameterInfo param = parameters[i];
                            if (param == null)
                            {
                                continue;
                            }

                            string key = $"{method.Name}_{param.Name}";
                            object value = null;

                            if (!_isFirstRun.ContainsKey(key))
                            {
                                _isFirstRun[key] = true;
                            }

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

                            EditorGUI.BeginChangeCheck();
                            object newValue = DrawParameterField(param.Name, value, param.ParameterType);
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

                // Add space between button methods
                GUILayout.Space(GradientButtonSettings.ButtonSpacing);
            }
        }

        protected object DrawParameterField(string label, object value, Type type)
        {
            try
            {
                // Begin horizontal to control layout
                EditorGUILayout.BeginHorizontal();

                // Remove any existing indent
                int indent = EditorGUI.indentLevel;
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
                    bool currentValue = value is bool boolValue ? boolValue : false;
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
                    if (value == null || !type.IsInstanceOfType(value))
                    {
                        value = Enum.GetValues(type).GetValue(0);
                    }

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
    }
}