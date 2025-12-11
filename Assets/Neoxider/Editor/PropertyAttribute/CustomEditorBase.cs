using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static string _cachedNeoxiderRootPath;
        private static Texture2D _cachedLibraryIcon;
        private static bool _isLibraryIconLoadAttempted;

        protected Dictionary<string, bool> _buttonFoldouts = new();

        protected Dictionary<string, object> _buttonParameterValues = new();

        private static readonly Dictionary<string, bool> _neoFoldouts = new();

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
        protected new static T FindFirstObjectByType<T>() where T : Object
        {
            return Object.FindFirstObjectByType<T>();
        }

        protected new static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode = FindObjectsSortMode.None)
            where T : Object
        {
            return Object.FindObjectsByType<T>(sortMode);
        }

        protected new static Object FindFirstObjectByType(Type type)
        {
            return Object.FindFirstObjectByType(type);
        }

        protected new static Object[] FindObjectsByType(Type type,
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
            EnsureNeoxiderPackageInfo();
            return string.IsNullOrEmpty(_cachedVersion) ? "Unknown" : _cachedVersion;
        }

        private static void EnsureNeoxiderPackageInfo()
        {
            if (!string.IsNullOrEmpty(_cachedVersion) && !string.IsNullOrEmpty(_cachedNeoxiderRootPath))
            {
                return;
            }

            try
            {
                // Самый надежный путь для пакетов: Unity выдаёт assetPath вида "Packages/com.neoxider.tools"
                UnityEditor.PackageManager.PackageInfo packageInfo =
                    UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(CustomEditorBase).Assembly);
                if (packageInfo != null)
                {
                    if (string.IsNullOrEmpty(_cachedNeoxiderRootPath) && !string.IsNullOrEmpty(packageInfo.assetPath))
                    {
                        _cachedNeoxiderRootPath = packageInfo.assetPath.Replace('\\', '/');
                    }

                    if (string.IsNullOrEmpty(_cachedVersion) && !string.IsNullOrEmpty(packageInfo.version))
                    {
                        _cachedVersion = packageInfo.version;
                    }

                    if (!string.IsNullOrEmpty(_cachedNeoxiderRootPath) && !string.IsNullOrEmpty(_cachedVersion))
                    {
                        return;
                    }
                }
            }
            catch
            {
                // Ignore
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
                            if (string.IsNullOrEmpty(_cachedNeoxiderRootPath))
                            {
                                _cachedNeoxiderRootPath = TryConvertToUnityProjectRelativePath(directory);
                            }

                            if (string.IsNullOrEmpty(_cachedVersion))
                            {
                                // Парсим версию
                                int versionIndex = json.IndexOf("\"version\":", StringComparison.Ordinal);
                                if (versionIndex != -1)
                                {
                                    int startQuote = json.IndexOf("\"", versionIndex + 10, StringComparison.Ordinal);
                                    int endQuote = json.IndexOf("\"", startQuote + 1, StringComparison.Ordinal);
                                    if (startQuote != -1 && endQuote != -1)
                                    {
                                        _cachedVersion = json.Substring(startQuote + 1, endQuote - startQuote - 1)
                                            .Trim();
                                    }
                                }
                            }

                            // Если уже нашли и root, и version — выходим
                            if (!string.IsNullOrEmpty(_cachedNeoxiderRootPath) && !string.IsNullOrEmpty(_cachedVersion))
                            {
                                return;
                            }
                        }
                    }

                    // Поднимаемся на уровень выше
                    directory = Directory.GetParent(directory)?.FullName;
                }
            }
            catch
            {
                // Ignore
            }

            if (string.IsNullOrEmpty(_cachedVersion))
            {
                _cachedVersion = "Unknown";
            }
        }

        private static string TryConvertToUnityProjectRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string normalized = path.Replace('\\', '/');

            // Если путь уже Unity-relative
            if (normalized.StartsWith("Assets/") || normalized.StartsWith("Packages/"))
            {
                return normalized;
            }

            try
            {
                string projectPath = Directory.GetCurrentDirectory().Replace('\\', '/');
                if (!string.IsNullOrEmpty(projectPath) && normalized.StartsWith(projectPath))
                {
                    return normalized.Substring(projectPath.Length + 1);
                }
            }
            catch
            {
                // Ignore
            }

            // Фоллбек: пытаемся вытащить часть с Assets/ или Packages/
            int assetsIndex = normalized.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
            {
                return normalized.Substring(assetsIndex + 1);
            }

            int packagesIndex = normalized.IndexOf("/Packages/", StringComparison.OrdinalIgnoreCase);
            if (packagesIndex >= 0)
            {
                return normalized.Substring(packagesIndex + 1);
            }

            return null;
        }

        private static Texture2D GetLibraryIcon()
        {
            if (_isLibraryIconLoadAttempted)
            {
                return _cachedLibraryIcon;
            }

            _isLibraryIconLoadAttempted = true;

            try
            {
                // Ожидаемый путь: <NeoxiderRoot>/NeoLogo.png
                EnsureNeoxiderPackageInfo();
                string root = _cachedNeoxiderRootPath;

                if (!string.IsNullOrEmpty(root))
                {
                    string iconPath = $"{root}/NeoLogo.png".Replace('\\', '/');
                    _cachedLibraryIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);

                    // Backward-compat (на случай старого расположения иконки)
                    if (_cachedLibraryIcon == null)
                    {
                        string legacyIconPath = $"{root}/Editor/Icons/NeoxiderToolsIcon.png".Replace('\\', '/');
                        _cachedLibraryIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(legacyIconPath);
                    }
                }
            }
            catch
            {
                // Ignore
            }

            if (_cachedLibraryIcon == null)
            {
                // Фоллбек: попробуем найти по имени (один раз, т.к. есть кеширование)
                try
                {
                    string[] guids = AssetDatabase.FindAssets("NeoLogo t:Texture2D");
                    if (guids != null && guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        _cachedLibraryIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    }
                }
                catch
                {
                    // Ignore
                }
            }

            return _cachedLibraryIcon;
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

            if (hasNeoNamespace && !isOdinActive)
            {
                DrawNeoPropertiesWithCollapsibleUnityEvents();
            }
            else
            {
                base.OnInspectorGUI();
            }

            if (_wasResetPressed)
            {
                _buttonParameterValues.Clear();
                _buttonFoldouts.Clear();
                _isFirstRun.Clear();
                _wasResetPressed = false;
            }

            // Process custom attributes if enabled
            ProcessAttributeAssignments();

            // Buttons / Actions
            // Если Odin активен — пусть Odin сам управляет кнопками.
            if (!isOdinActive)
            {
                if (hasNeoNamespace)
                {
                    DrawActionsFoldout();
                }
                else
                {
                    // Поведение как раньше для не-Neo компонентов
                    DrawMethodButtons();
                }
            }

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

            Texture2D icon = GetLibraryIcon();
            EnsureNeoxiderPackageInfo();
            string version = GetNeoxiderVersion();
            string signatureText = $"v{version}";
            NeoUpdateChecker.State updateState = NeoUpdateChecker.Tick(version, _cachedNeoxiderRootPath);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (icon != null)
                {
                    const float iconSize = 50f;
                    GUILayout.Label(icon, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                    GUILayout.Space(6);
                }

                GUIStyle libraryNameStyle = new(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 16,
                    clipping = TextClipping.Clip,
                    normal = { textColor = CustomEditorSettings.ScriptNameColor }
                };
                GUILayout.Label("Neoxider Tools", libraryNameStyle, GUILayout.MinWidth(0));

                GUILayout.FlexibleSpace();

                Color originalTextColor = GUI.color;

                Color signatureColor = CustomEditorSettings.SignatureColor;
                bool updateAvailable = updateState.Status == NeoUpdateChecker.UpdateStatus.UpdateAvailable &&
                                       !string.IsNullOrEmpty(updateState.LatestVersion) &&
                                       !string.IsNullOrEmpty(updateState.UpdateUrl);

                if (updateAvailable)
                {
                    EnsureRepaint();
                    float t = 0.5f + 0.5f * Mathf.Sin((float)EditorApplication.timeSinceStartup * 4f);
                    signatureColor = Color.Lerp(new Color(1f, 0.25f, 0.25f, 1f), new Color(1f, 0.65f, 0.65f, 1f), t);
                }
                else if (CustomEditorSettings.EnableRainbowSignature && CustomEditorSettings.EnableRainbowSignatureAnimation)
                {
                    signatureColor = GetRainbowColor(CustomEditorSettings.RainbowSpeed);
                }

                GUIStyle signatureStyle = new(EditorStyles.centeredGreyMiniLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleRight,
                    fontStyle = CustomEditorSettings.SignatureFontStyle,
                    normal = { textColor = signatureColor }
                };

                GUI.color = signatureColor;

                if (updateAvailable)
                {
                    DrawTextWithColorOutline(signatureText, signatureStyle, signatureColor,
                        Mathf.Max(1.2f, CustomEditorSettings.RainbowOutlineSize),
                        GUILayout.ExpandWidth(false));
                }
                else
                {
                    if (CustomEditorSettings.EnableRainbowOutline)
                    {
                        DrawTextWithRainbowOutline(signatureText, signatureStyle, GUILayout.ExpandWidth(false));
                    }
                    else
                    {
                        GUILayout.Label(signatureText, signatureStyle, GUILayout.ExpandWidth(false));
                    }
                }

                GUI.color = originalTextColor;
            }

            if (!string.IsNullOrEmpty(updateState.LatestVersion))
            {
                switch (updateState.Status)
                {
                    case NeoUpdateChecker.UpdateStatus.UpdateAvailable:
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUIStyle newVersionStyle = new(EditorStyles.miniBoldLabel)
                                {
                                    normal = { textColor = new Color(1f, 0.25f, 0.25f, 1f) }
                                };

                                GUILayout.Label($"Новая версия {updateState.LatestVersion}", newVersionStyle);
                                GUILayout.FlexibleSpace();

                                if (!string.IsNullOrEmpty(updateState.UpdateUrl) &&
                                    GUILayout.Button("Обновить", GUILayout.Width(90), GUILayout.Height(20)))
                                {
                                    Application.OpenURL(updateState.UpdateUrl);
                                }
                            }
                        }
                        break;

                    case NeoUpdateChecker.UpdateStatus.UpToDate:
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            GUIStyle okStyle = new(EditorStyles.miniBoldLabel)
                            {
                                normal = { textColor = new Color(0.35f, 1f, 0.35f, 1f) }
                            };
                            EditorGUILayout.LabelField("Актуальная версия", okStyle);
                        }
                        break;

                    case NeoUpdateChecker.UpdateStatus.Ahead:
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            GUIStyle devStyle = new(EditorStyles.miniBoldLabel)
                            {
                                normal = { textColor = new Color(1f, 0.75f, 0.25f, 1f) }
                            };
                            EditorGUILayout.LabelField($"Не опубликована (последняя: {updateState.LatestVersion})", devStyle);
                        }
                        break;
                }
            }

            EditorGUILayout.Space(CustomEditorSettings.SignatureSpacing);
        }

        private void DrawTextWithColorOutline(string text, GUIStyle baseStyle, Color outlineColor, float outlineSize,
            params GUILayoutOption[] options)
        {
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(text), baseStyle, options);

            GUIStyle outlineStyle = new(baseStyle);
            outlineStyle.normal.textColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.35f);

            for (int angle = 0; angle < 360; angle += 45)
            {
                float radian = angle * Mathf.Deg2Rad;
                float offsetX = Mathf.Cos(radian) * outlineSize;
                float offsetY = Mathf.Sin(radian) * outlineSize;

                Rect offsetRect = new(rect.x + offsetX, rect.y + offsetY, rect.width, rect.height);
                GUI.Label(offsetRect, text, outlineStyle);
            }

            GUI.Label(rect, text, baseStyle);
        }

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

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeaderSections(List<SerializedProperty> properties)
        {
            if (properties == null || properties.Count == 0 || target == null)
            {
                return;
            }

            // Если в объекте нет ни одного [Header] — рисуем как обычно (без лишних секций).
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
                    EditorGUILayout.PropertyField(properties[i], true);
                }

                return;
            }

            List<HeaderSection> sections = BuildHeaderSections(properties);
            Color baseGreen = CustomEditorSettings.ScriptNameColor;
            Color darkGreen = Color.Lerp(baseGreen, Color.black, 0.75f);

            for (int i = 0; i < sections.Count; i++)
            {
                HeaderSection section = sections[i];
                if (section.IsWarningHeader)
                {
                    DrawWarningHeader(section.Title);
                    for (int p = 0; p < section.Properties.Count; p++)
                    {
                        EditorGUILayout.PropertyField(section.Properties[p], true);
                    }

                    continue;
                }

                string key = $"{target.GetType().FullName}.NeoFoldout.Header.{section.Title}";

                bool expanded = GetFoldoutState(key, defaultValue: true);

                // Когда секция раскрыта — делаем заголовок/акцент темнее (чтобы было компактнее и спокойнее визуально)
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
                        EditorGUILayout.PropertyField(section.Properties[p], true);
                    }

                    EditorGUI.indentLevel--;
                }
            }
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
                    // General: все поля до первого Header
                    List<SerializedProperty> general = new();
                    while (i < properties.Count && string.IsNullOrEmpty(TryGetHeaderTitleForProperty(properties[i])))
                    {
                        general.Add(properties[i]);
                        i++;
                    }

                    if (general.Count > 0)
                    {
                        sections.Add(new HeaderSection("General", general, isWarningHeader: false));
                    }

                    continue;
                }

                // Секция начинается на поле с HeaderAttribute и продолжается до следующего HeaderAttribute
                List<SerializedProperty> sectionProps = new();
                sectionProps.Add(p);
                i++;

                while (i < properties.Count && string.IsNullOrEmpty(TryGetHeaderTitleForProperty(properties[i])))
                {
                    sectionProps.Add(properties[i]);
                    i++;
                }

                bool nextIsHeaderOrEnd = i >= properties.Count ||
                                        !string.IsNullOrEmpty(TryGetHeaderTitleForProperty(properties[i]));

                bool looksLikeWarning =
                    headerTitle.StartsWith("⚠", StringComparison.Ordinal) ||
                    headerTitle.StartsWith("!", StringComparison.Ordinal) ||
                    headerTitle.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                    headerTitle.Contains("предуп", StringComparison.OrdinalIgnoreCase) ||
                    headerTitle.Contains("ошиб", StringComparison.OrdinalIgnoreCase);

                // Если Header "пустой" (идёт сразу следующий Header или конец) — не делаем категорию, а рисуем как предупреждение.
                bool isEmptyHeaderGroup = sectionProps.Count == 1 && nextIsHeaderOrEnd;
                bool isWarningHeader = looksLikeWarning || isEmptyHeaderGroup;

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

            GUIStyle style = new(EditorStyles.boldLabel)
            {
                fontSize = 16,
                wordWrap = true,
                normal = { textColor = new Color(1f, 0.25f, 0.25f, 1f) }
            };

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(text, style);
            EditorGUILayout.Space(2);
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
                // GetCustomAttribute<T> может бросать AmbiguousMatchException, если по какой-то причине
                // на поле оказалось несколько HeaderAttribute. Берём первый безопасным способом.
                object[] attrs = fieldInfo.GetCustomAttributes(typeof(HeaderAttribute), inherit: true);
                if (attrs == null || attrs.Length == 0)
                {
                    return null;
                }

                HeaderAttribute header = attrs[0] as HeaderAttribute;
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

            // Пример путей:
            // - myField
            // - nested.someField
            // - list.Array.data[0].someField
            string[] parts = propertyPath.Split('.');
            Type currentType = rootType;
            FieldInfo currentField = null;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                if (part == "Array")
                {
                    // Следующий токен будет data[x] — пропускаем
                    continue;
                }

                if (part.StartsWith("data[", StringComparison.Ordinal))
                {
                    // Индекс массива/листа — пропускаем, тип уже должен быть elementType
                    continue;
                }

                currentField = currentType.GetField(part,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (currentField == null)
                {
                    return false;
                }

                currentType = currentField.FieldType;

                // Если это список/массив — дальше ожидается Array.data[x]
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

            // UnityEvent сериализуется как "UnityEvent" / "UnityEvent`1" и т.п.
            string typeName = property.type;
            return !string.IsNullOrEmpty(typeName) && typeName.Contains("UnityEvent");
        }

        private void DrawUnityEventsFoldout(List<SerializedProperty> unityEvents)
        {
            if (unityEvents == null || unityEvents.Count == 0 || target == null)
            {
                return;
            }

            string key = $"{target.GetType().FullName}.NeoFoldout.Events";
            bool current = _neoFoldouts.TryGetValue(key, out bool value) && value;

            using (new EditorGUILayout.VerticalScope())
            {
                Color accentBase = new(0.25f, 0.9f, 0.85f, 1f);
                Color accentDark = Color.Lerp(accentBase, Color.black, 0.45f);
                Color accent = current ? accentDark : accentBase;

                current = DrawNeoSectionHeader(current, "Events", unityEvents.Count, accent, "d_UnityEvent",
                    current ? Color.white : accentBase,
                    current
                        ? new Color(1f, 1f, 1f, 0.75f)
                        : new Color(accentBase.r, accentBase.g, accentBase.b, 0.75f));

                if (current)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUI.indentLevel++;
                        foreach (SerializedProperty p in unityEvents)
                        {
                            if (p == null)
                            {
                                continue;
                            }

                            EditorGUILayout.PropertyField(p, true);
                        }

                        EditorGUI.indentLevel--;
                    }
                }
            }

            _neoFoldouts[key] = current;
        }

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
                Color accentDark = Color.Lerp(accentBase, Color.black, 0.45f);
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
                        EditorGUI.indentLevel++;
                        DrawMethodButtons(methods);
                        EditorGUI.indentLevel--;
                    }
                }
            }

            _neoFoldouts[key] = current;
        }

        private bool DrawNeoSectionHeader(bool expanded,
            string title,
            int count,
            Color accent,
            string iconName,
            Color titleColor,
            Color countColor)
        {
            const float height = 30f;

            Rect rect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
            rect = EditorGUI.IndentedRect(rect);
            rect.height = height;

            bool isHover = rect.Contains(Event.current.mousePosition);

            Color bg;
            if (isHover)
            {
                bg = new Color(accent.r, accent.g, accent.b, 0.18f);
            }
            else if (expanded)
            {
                bg = new Color(accent.r, accent.g, accent.b, 0.10f);
            }
            else
            {
                bg = new Color(0f, 0f, 0f, 0.06f);
            }

            EditorGUI.DrawRect(rect, bg);

            Rect accentRect = new(rect.x, rect.y, 4f, rect.height);
            EditorGUI.DrawRect(accentRect, accent);

            // Click handling
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                expanded = !expanded;
                GUI.FocusControl(null);
                Event.current.Use();
            }

            Rect foldoutRect = new(rect.x + 8f, rect.y + 6f, 14f, 14f);
            expanded = EditorGUI.Foldout(foldoutRect, expanded, GUIContent.none, true);

            float x = rect.x + 26f;

            GUIContent iconContent = string.IsNullOrEmpty(iconName) ? null : EditorGUIUtility.IconContent(iconName);
            if (iconContent != null && iconContent.image != null)
            {
                Rect iconRect = new(x, rect.y + 6f, 16f, 16f);
                GUI.DrawTexture(iconRect, (Texture2D)iconContent.image, ScaleMode.ScaleToFit, true);
                x += 20f;
            }

            GUIStyle titleStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = titleColor }
            };

            Rect titleRect = new(x, rect.y + 3f, rect.width - x - 70f, rect.height - 6f);
            GUI.Label(titleRect, title, titleStyle);

            GUIStyle countStyle = new(EditorStyles.miniBoldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = countColor }
            };

            Rect countRect = new(rect.xMax - 60f, rect.y + 6f, 52f, 16f);
            GUI.Label(countRect, $"({count})", countStyle);

            // Bottom separator line
            Rect lineRect = new(rect.x, rect.yMax - 1f, rect.width, 1f);
            EditorGUI.DrawRect(lineRect, new Color(1f, 1f, 1f, 0.06f));

            return expanded;
        }

        private MethodInfo[] GetButtonMethods()
        {
            if (target == null)
            {
                return Array.Empty<MethodInfo>();
            }

            MethodInfo[] methods = target.GetType().GetMethods(
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic);

            return methods
                .Where(m => m != null && FindButtonAttribute(m).HasValue)
                .ToArray();
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
        private void DrawTextWithRainbowOutline(string text, GUIStyle baseStyle, params GUILayoutOption[] options)
        {
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(text), baseStyle, options);

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