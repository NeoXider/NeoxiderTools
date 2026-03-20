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
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Neo.Editor
{
    /// <summary>
    ///     Base class for custom editors that provides common functionality
    /// </summary>
    public abstract partial class CustomEditorBase : UnityEditor.Editor
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
                var packageInfo =
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
                var odinInspectorType =
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
                    DrawActionsFoldout();
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

            if (!isOdinActive && !hasNeoNamespace)
            {
                DrawMethodButtons();
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
    }
}
