using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using GUI = UnityEngine.GUI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Neo.Editor
{
    /// <summary>
    ///     Base class for custom editors that provides common functionality
    /// </summary>
    public abstract class CustomEditorBase : UnityEditor.Editor
    {
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

        private static readonly Dictionary<string, bool> _neoFoldouts = new();
        private static readonly Dictionary<string, Vector2> _neoDocScrollPositions = new();
        private static Texture2D _neoDocDarkTexture;
        private static GUIStyle _neoDocBoxStyle;

        // Reflection helpers (suppress Unity's built-in HeaderAttribute drawing when we already render our own sections)
        private static readonly Type _scriptAttributeUtilityType =
            typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ScriptAttributeUtility");

        private static readonly MethodInfo _getHandlerMethod =
            _scriptAttributeUtilityType?.GetMethod("GetHandler",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(SerializedProperty) },
                null);

        protected Dictionary<string, bool> _buttonFoldouts = new();

        protected Dictionary<string, object> _buttonParameterValues = new();

        private Rect _componentOutlineRect;
        protected Dictionary<string, bool> _isFirstRun = new();

        private Rect _rainbowLineStartRect;
        private float _rainbowLineStartY;
        private bool _unityEventOnlyWithListeners;

        private string _unityEventSearch = string.Empty;

        private bool _wasResetPressed;

        /// <summary>
        ///     Позволяет производным редакторам (модулям) рисовать свой UI,
        ///     но внутри фирменного Neoxider-стиля (рамка/фон/радужная линия/Actions).
        /// </summary>
        protected virtual bool UseCustomNeoxiderInspectorGUI => false;

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
            if (!string.IsNullOrEmpty(sourceFilePath))
            {
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
                PackageInfo packageInfo =
                    PackageInfo.FindForAssembly(typeof(CustomEditorBase).Assembly);
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
            }

            try
            {
                string directory = Path.GetDirectoryName(GetScriptPath());

                while (!string.IsNullOrEmpty(directory))
                {
                    string packagePath = Path.Combine(directory, "package.json");

                    if (File.Exists(packagePath))
                    {
                        string json = File.ReadAllText(packagePath);

                        if (json.Contains("\"displayName\": \"Neoxider Tools\"") ||
                            json.Contains("\"name\": \"com.neoxider.tools\""))
                        {
                            if (string.IsNullOrEmpty(_cachedNeoxiderRootPath))
                            {
                                _cachedNeoxiderRootPath = TryConvertToUnityProjectRelativePath(directory);
                            }

                            if (string.IsNullOrEmpty(_cachedVersion))
                            {
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

                            if (!string.IsNullOrEmpty(_cachedNeoxiderRootPath) && !string.IsNullOrEmpty(_cachedVersion))
                            {
                                return;
                            }
                        }
                    }

                    directory = Directory.GetParent(directory)?.FullName;
                }
            }
            catch
            {
            }

            // Фоллбек для _cachedNeoxiderRootPath: пробуем известные пути
            if (string.IsNullOrEmpty(_cachedNeoxiderRootPath))
            {
                string[] knownPaths =
                {
                    "Assets/Neoxider",
                    "Packages/com.neoxider.tools"
                };

                foreach (string kp in knownPaths)
                {
                    string testPath = Path.Combine(kp, "package.json");
                    if (File.Exists(testPath) ||
                        AssetDatabase.LoadAssetAtPath<TextAsset>(testPath) != null)
                    {
                        _cachedNeoxiderRootPath = kp;
                        break;
                    }
                }
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
            }

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
                EnsureNeoxiderPackageInfo();
                string root = _cachedNeoxiderRootPath;

                if (!string.IsNullOrEmpty(root))
                {
                    string iconPath = $"{root}/NeoLogo.png".Replace('\\', '/');
                    _cachedLibraryIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);

                    if (_cachedLibraryIcon == null)
                    {
                        string legacyIconPath = $"{root}/Editor/Icons/NeoxiderToolsIcon.png".Replace('\\', '/');
                        _cachedLibraryIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(legacyIconPath);
                    }
                }
            }
            catch
            {
            }

            if (_cachedLibraryIcon == null)
            {
                try
                {
                    _cachedLibraryIcon = NeoxiderEditorAssets.FindNeoLogo();
                }
                catch
                {
                }
            }

            return _cachedLibraryIcon;
        }

        /// <summary>
        ///     Проверяет, установлен ли Odin Inspector в проекте
        /// </summary>
        protected virtual bool IsOdinInspectorAvailable()
        {
            if (_odinInspectorAvailable.HasValue)
            {
                return _odinInspectorAvailable.Value;
            }

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
            if (Event.current.commandName == "Reset")
            {
                _wasResetPressed = true;
            }

            bool isOdinActive = IsOdinInspectorAvailable();

            bool hasNeoNamespace = false;

            if (target != null && target.GetType().Namespace != null)
            {
                string targetNamespace = target.GetType().Namespace;

                hasNeoNamespace = targetNamespace == "Neo" || targetNamespace.StartsWith("Neo.");

                if (!hasNeoNamespace && targetNamespace.Contains("."))
                {
                    string[] parts = targetNamespace.Split('.');
                    hasNeoNamespace = parts.Length > 0 && parts[0] == "Neo";
                }
            }

            if (hasNeoNamespace)
            {
                DrawNeoxiderSignature();
                DrawDocumentationFoldout();

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
                if (UseCustomNeoxiderInspectorGUI)
                {
                    DrawCustomNeoxiderInspectorGUI();
                }
                else
                {
                    DrawNeoPropertiesWithCollapsibleUnityEvents();
                }
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

            ProcessAttributeAssignments();

            if (!isOdinActive)
            {
                if (hasNeoNamespace)
                {
                    DrawActionsFoldout();
                }
                else
                {
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

        /// <summary>
        ///     Кастомная отрисовка инспектора (вызывается только если <see cref="UseCustomNeoxiderInspectorGUI" /> == true).
        /// </summary>
        protected virtual void DrawCustomNeoxiderInspectorGUI()
        {
        }

        /// <summary>
        ///     Собирает все UnityEvent-свойства из serializedObject и рисует их
        ///     в фирменном сворачиваемом разделе «Events». Используйте в <see cref="DrawCustomNeoxiderInspectorGUI" />
        ///     чтобы отображать события в едином стиле Neoxider.
        /// </summary>
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
            // Tick() запускает проверку обновлений если прошёл интервал, иначе читает кеш.
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
                else if (CustomEditorSettings.EnableRainbowSignature &&
                         CustomEditorSettings.EnableRainbowSignatureAnimation)
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

            // Всегда показываем строку статуса версии с кнопкой обновления
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUIContent refreshContent = EditorGUIUtility.IconContent("d_Refresh");
                    if (refreshContent == null || refreshContent.image == null)
                    {
                        refreshContent = new GUIContent("⟳");
                    }

                    if (GUILayout.Button(refreshContent, GUILayout.Width(20), GUILayout.Height(18)))
                    {
                        // Гарантируем что путь найден перед проверкой
                        EnsureNeoxiderPackageInfo();
                        NeoUpdateChecker.RequestImmediateCheck(version, _cachedNeoxiderRootPath);
                        EnsureRepaint();
                        Repaint();
                    }

                    switch (updateState.Status)
                    {
                        case NeoUpdateChecker.UpdateStatus.UpdateAvailable:
                        {
                            GUIStyle newVersionStyle = new(EditorStyles.miniBoldLabel)
                            {
                                normal = { textColor = new Color(1f, 0.25f, 0.25f, 1f) }
                            };
                            GUILayout.Label(
                                $"Новая версия {updateState.LatestVersion}",
                                newVersionStyle);
                            GUILayout.FlexibleSpace();

                            if (!string.IsNullOrEmpty(updateState.UpdateUrl) &&
                                GUILayout.Button("Обновить", GUILayout.Width(90), GUILayout.Height(20)))
                            {
                                Application.OpenURL(updateState.UpdateUrl);
                            }

                            break;
                        }

                        case NeoUpdateChecker.UpdateStatus.UpToDate:
                        {
                            GUIStyle okStyle = new(EditorStyles.miniBoldLabel)
                            {
                                normal = { textColor = new Color(0.35f, 1f, 0.35f, 1f) }
                            };
                            EditorGUILayout.LabelField("Актуальная версия", okStyle);
                            break;
                        }

                        case NeoUpdateChecker.UpdateStatus.Ahead:
                        {
                            GUIStyle devStyle = new(EditorStyles.miniBoldLabel)
                            {
                                normal = { textColor = new Color(1f, 0.75f, 0.25f, 1f) }
                            };
                            EditorGUILayout.LabelField(
                                $"Не опубликована (последняя: {updateState.LatestVersion})",
                                devStyle);
                            break;
                        }

                        case NeoUpdateChecker.UpdateStatus.Checking:
                        {
                            GUIStyle checkingStyle = new(EditorStyles.miniBoldLabel)
                            {
                                normal = { textColor = new Color(0.7f, 0.7f, 0.7f, 1f) }
                            };
                            EditorGUILayout.LabelField("Проверка обновлений...", checkingStyle);
                            EnsureRepaint();
                            break;
                        }

                        default: // Unknown
                        {
                            string errorMsg = updateState.Error;
                            if (!string.IsNullOrEmpty(errorMsg))
                            {
                                GUIStyle errorStyle = new(EditorStyles.miniBoldLabel)
                                {
                                    normal = { textColor = new Color(1f, 0.6f, 0.2f, 1f) }
                                };
                                EditorGUILayout.LabelField(errorMsg, errorStyle);
                            }
                            else
                            {
                                GUIStyle unknownStyle = new(EditorStyles.miniBoldLabel)
                                {
                                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f, 1f) }
                                };
                                EditorGUILayout.LabelField("Нажмите ⟳ для проверки обновлений", unknownStyle);
                            }

                            break;
                        }
                    }
                }
            }

            EditorGUILayout.Space(CustomEditorSettings.SignatureSpacing);
        }

        private static GUIStyle GetNeoDocBoxStyle()
        {
            if (_neoDocBoxStyle != null) return _neoDocBoxStyle;
            _neoDocDarkTexture = new Texture2D(1, 1);
            _neoDocDarkTexture.SetPixel(0, 0, new Color(0.14f, 0.15f, 0.18f, 1f));
            _neoDocDarkTexture.Apply();
            _neoDocBoxStyle = new GUIStyle
            {
                padding = new RectOffset(18, 18, 14, 14),
                normal = { background = _neoDocDarkTexture }
            };
            return _neoDocBoxStyle;
        }

        private void DrawDocumentationFoldout()
        {
            if (target == null || string.IsNullOrEmpty(_cachedNeoxiderRootPath)) return;
            Type type = target.GetType();
            string docPath = NeoDocHelper.GetDocPathForType(_cachedNeoxiderRootPath, type);
            string key = "NeoDoc_Foldout_" + type.FullName;
            string scrollKey = "NeoDoc_Scroll_" + type.FullName;
            bool expanded = _neoFoldouts.TryGetValue(key, out bool v) && v;

            using (new EditorGUILayout.VerticalScope())
            {
                Color accentBase = new(0.35f, 0.6f, 1f, 1f);
                Color accentDark = Color.Lerp(accentBase, Color.black, 0.45f);
                Color accent = expanded ? accentDark : accentBase;
                int count = string.IsNullOrEmpty(docPath) ? 0 : 1;

                expanded = DrawNeoSectionHeader(expanded, "Documentation", count, accent, "d_TextAsset Icon",
                    expanded ? Color.white : accentBase,
                    expanded ? new Color(1f, 1f, 1f, 0.75f) : new Color(accentBase.r, accentBase.g, accentBase.b, 0.75f));

                if (expanded)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        if (!string.IsNullOrEmpty(docPath))
                        {
                            string preview = NeoDocHelper.GetDocPreview(docPath, 40);
                            if (!string.IsNullOrEmpty(preview))
                            {
                                string richText = NeoDocHelper.MarkdownToUnityRichText(preview);
                                if (!_neoDocScrollPositions.TryGetValue(scrollKey, out Vector2 scroll))
                                    scroll = Vector2.zero;
                                const float docAreaMinHeight = 220f;
                                const float docAreaMaxHeight = 420f;
                                const float docHorizontalPadding = 18f * 2f;
                                GUIStyle docStyle = new GUIStyle(EditorStyles.label)
                                {
                                    wordWrap = true,
                                    richText = true,
                                    normal = { textColor = new Color(0.88f, 0.90f, 0.92f, 1f) }
                                };
                                float contentWidth = Mathf.Max(100f, EditorGUIUtility.currentViewWidth - 60f - docHorizontalPadding);
                                float contentHeight = docStyle.CalcHeight(new GUIContent(richText), contentWidth);
                                EditorGUILayout.Space(4);
                                scroll = EditorGUILayout.BeginScrollView(scroll, false, true, GUILayout.MinHeight(docAreaMinHeight), GUILayout.MaxHeight(docAreaMaxHeight));
                                _neoDocScrollPositions[scrollKey] = scroll;
                                GUIStyle boxStyle = GetNeoDocBoxStyle();
                                EditorGUILayout.BeginVertical(boxStyle, GUILayout.MinHeight(contentHeight + 28));
                                EditorGUILayout.LabelField(richText, docStyle);
                                EditorGUILayout.EndVertical();
                                EditorGUILayout.EndScrollView();
                                EditorGUILayout.Space(4);
                            }
                            Color docAccent = new(0.35f, 0.6f, 1f, 1f);
                            GUIStyle openBtnStyle = new(EditorStyles.miniButton)
                            {
                                fixedHeight = 24,
                                fontStyle = FontStyle.Bold,
                                alignment = TextAnchor.MiddleCenter,
                                normal = { textColor = Color.white },
                                hover = { textColor = Color.white },
                                active = { textColor = Color.white },
                                focused = { textColor = Color.white }
                            };
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.FlexibleSpace();
                                Rect btnRect = GUILayoutUtility.GetRect(new GUIContent(" Open in window "), openBtnStyle, GUILayout.MinWidth(140), GUILayout.Height(24));
                                bool isHover = btnRect.Contains(Event.current.mousePosition);
                                Color prevBg = GUI.backgroundColor;
                                GUI.backgroundColor = isHover ? Color.Lerp(docAccent, Color.white, 0.3f) : docAccent;
                                if (GUI.Button(btnRect, " Open in window ", openBtnStyle))
                                    NeoDocHelper.OpenDocInWindow(docPath);
                                GUI.backgroundColor = prevBg;
                                GUILayout.FlexibleSpace();
                            }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("No documentation linked. Add [NeoDoc(\"Module/File.md\")] or place " + type.Name + ".md in Docs.", MessageType.Info);
                        }
                    }
                }
            }

            _neoFoldouts[key] = expanded;
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
            Color darkGreen = Color.Lerp(baseGreen, Color.black, 0.75f);
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

                if (!excludeFromMinFieldsRule &&
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
            Color darkGreen = Color.Lerp(baseGreen, Color.black, 0.75f);

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

            if (suppressUnityHeaderDecorators && !string.IsNullOrEmpty(TryGetHeaderTitleForProperty(property)))
            {
                DrawPropertyFieldWithoutHeaderDecorator(property);
                return;
            }

            EditorGUILayout.PropertyField(property, true);
        }

        private static void DrawPropertyFieldWithoutHeaderDecorator(SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            // If reflection fails (Unity internal API changed), fallback to default drawing.
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

                    // Suppress only HeaderAttribute rendering; keep Space / other decorators intact.
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
                        // ignore
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

            const float buttonWidth = 48f;
            Rect buttonRect = new(contentRect.xMax - buttonWidth, contentRect.y, buttonWidth, contentRect.height);

            bool value = property.boolValue;

            Color oldBg = GUI.backgroundColor;
            Color oldColor = GUI.color;

            GUI.backgroundColor = value
                ? new Color(0.25f, 0.95f, 0.45f, 1f)
                : new Color(0.25f, 0.25f, 0.25f, 1f);

            GUI.color = Color.white;

            GUIStyle style = new(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fixedHeight = 18f
            };

            if (GUI.Button(buttonRect, value ? "ON" : "OFF", style))
            {
                property.boolValue = !value;
            }

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

                bool nextIsHeaderOrEnd = i >= properties.Count ||
                                         !string.IsNullOrEmpty(TryGetHeaderTitleForProperty(properties[i]));

                bool looksLikeWarning =
                    headerTitle.StartsWith("⚠", StringComparison.Ordinal) ||
                    headerTitle.StartsWith("!", StringComparison.Ordinal) ||
                    headerTitle.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                    headerTitle.Contains("предуп", StringComparison.OrdinalIgnoreCase) ||
                    headerTitle.Contains("ошиб", StringComparison.OrdinalIgnoreCase);

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

        private void DrawPlainHeader(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            GUIStyle style = new(EditorStyles.boldLabel)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = Color.white }
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
                object[] attrs = fieldInfo.GetCustomAttributes(typeof(HeaderAttribute), true);
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

            // 1) Fast-path: Unity often reports a type string containing "UnityEvent"
            //    (but custom subclasses like ColliderEvent may not contain it).
            string typeName = property.type;
            if (!string.IsNullOrEmpty(typeName) && typeName.Contains("UnityEvent"))
            {
                return true;
            }

            // 2) Robust path: any UnityEventBase derivative has the internal persistent calls array.
            //    This catches custom serializable UnityEvent subclasses used in PhysicsEvents2D/3D etc.
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
                bool hasMethod = methodProp != null && methodProp.propertyType == SerializedPropertyType.String &&
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

                string methodName = methodProp != null && methodProp.propertyType == SerializedPropertyType.String &&
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

                current = DrawNeoSectionHeader(current, "Events", unityEvents.Count, accent, "d_EventSystem Icon",
                    current ? Color.white : accentBase,
                    current
                        ? new Color(1f, 1f, 1f, 0.75f)
                        : new Color(accentBase.r, accentBase.g, accentBase.b, 0.75f));

                if (current)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                        {
                            GUIStyle searchStyle = EditorStyles.toolbarSearchField;
                            GUIStyle cancelStyle =
                                GUI.skin.FindStyle("ToolbarSearchCancelButton") ??
                                GUI.skin.FindStyle("ToolbarSeachCancelButton"); // старые Unity (опечатка в названии)
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
                                    $"Есть {brokenCount} сломанных listener(ов): проверь Target/Method.",
                                    warningMini);
                            }

                            shown++;
                        }

                        if (shown == 0)
                        {
                            EditorGUILayout.LabelField("Ничего не найдено по текущему фильтру.",
                                EditorStyles.centeredGreyMiniLabel);
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

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
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
                Color topColor = GradientButtonSettings.TopColor;
                Color bottomColor = GradientButtonSettings.BottomColor;

                bool isHover = buttonRect.Contains(Event.current.mousePosition);
                if (isHover)
                {
                    topColor = Color.Lerp(topColor, Color.white, GradientButtonSettings.HoverBrightness);
                    bottomColor = Color.Lerp(bottomColor, Color.white, GradientButtonSettings.HoverBrightness);
                }

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

                DrawRoundedCorners(buttonRect, GradientButtonSettings.CornerRadius, topColor, bottomColor);

                Handles.BeginGUI();

                if (GradientButtonSettings.EnableNeonGlow)
                {
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

                    Handles.color = GradientButtonSettings.NeonGlowColor;
                    Handles.DrawAAPolyLine(1.5f, points);
                }
                else
                {
                    Handles.color = GradientButtonSettings.HighlightColor;
                    Handles.DrawAAPolyLine(GradientButtonSettings.HighlightWidth,
                        new Vector3(buttonRect.x + GradientButtonSettings.CornerRadius, buttonRect.y),
                        new Vector3(buttonRect.xMax - GradientButtonSettings.CornerRadius, buttonRect.y)
                    );
                }

                Handles.EndGUI();
            }

            if (GradientButtonSettings.EnableNeonGlow)
            {
                GUIStyle shadowStyle = new(EditorStyles.label)
                {
                    alignment = GradientButtonSettings.TextAlignment,
                    fontStyle = GradientButtonSettings.TextStyle,
                    normal = { textColor = new Color(0, 0, 0, 0.5f) }
                };

                Rect shadowRect = new(buttonRect.x, buttonRect.y + 1, buttonRect.width, buttonRect.height);
                GUI.Label(shadowRect, text, shadowStyle);
            }

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

            DrawCornerMask(new Rect(rect.x, rect.y, radius, radius), radius, bgColor, true, true);
            DrawCornerMask(new Rect(rect.xMax - radius, rect.y, radius, radius), radius, bgColor, false, true);

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

            int steps = GradientButtonSettings.CornerMaskSteps;
            float pixelSize = cornerRect.width / steps;

            for (int x = 0; x < steps; x++)
            {
                for (int y = 0; y < steps; y++)
                {
                    float px = cornerRect.x + (x + 0.5f) / steps * cornerRect.width;
                    float py = cornerRect.y + (y + 0.5f) / steps * cornerRect.height;

                    float dist = Vector2.Distance(new Vector2(px, py), center);

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

                EditorGUILayout.BeginHorizontal();

                bool buttonPressed = DrawGradientButton(buttonText, buttonAttribute.Width);

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

                GUILayout.Space(CustomEditorSettings.ButtonParameterSpacing);

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
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(0);

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

                GUILayout.Space(GradientButtonSettings.ButtonSpacing);
            }
        }

        protected object DrawParameterField(string label, object value, Type type)
        {
            try
            {
                EditorGUILayout.BeginHorizontal();

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