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

        // WHY: Per-instance: a static flag here made one closing editor strand every other editor's
        // repaint loop (flag stayed true, subscription gone), freezing stale frames on screen.
        private bool _isAnimating;

        private static bool? _odinInspectorAvailable;
        private static string _cachedVersion;
        private static string _cachedNeoxiderRootPath;
        private static Texture2D _cachedLibraryIcon;
        private static bool _isLibraryIconLoadAttempted;

        private static readonly Dictionary<string, bool> _neoFoldouts = new();
        private static readonly Dictionary<string, Vector2> _neoDocScrollPositions = new();
        private static Texture2D _neoDocDarkTexture;
        private static GUIStyle _neoDocBoxStyle;

        // WHY: Reflection helpers (suppress Unity's built-in HeaderAttribute drawing when we already render our own sections)
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
        ///     Lets derived editors (modules) draw their own UI inside the Neoxider look
        ///     (frame/background/rainbow line/Actions).
        /// </summary>
        protected virtual bool UseCustomNeoxiderInspectorGUI => false;

        /// <summary>
        ///     Invoked right after <see cref="DrawNeoPropertiesWithCollapsibleUnityEvents" /> (default MonoBehaviour
        ///     property pass). Use for extra notes or UI that should sit with the main inspector block.
        /// </summary>
        protected virtual void OnAfterDrawNeoProperties()
        {
        }

        protected virtual void OnDisable()
        {
            if (_isAnimating)
            {
                EditorApplication.update -= OnEditorUpdate;
                _isAnimating = false;
            }
        }

        private static readonly HashSet<string> _chromeErrorsLogged = new();

        private static void LogChromeErrorOnce(Exception ex)
        {
            string key = ex.GetType().Name + ":" + ex.Message;
            if (_chromeErrorsLogged.Add(key))
            {
                Debug.LogWarning("[Neoxider] Inspector header error (suppressed): " + ex);
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
        ///     Gets the Neoxider package version from package.json.
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

                        if (json.Contains("\"displayName\": \"NeoxiderTools\"") ||
                            json.Contains("\"displayName\": \"Neoxider Tools\"") ||
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

        private static Texture2D _cachedBlinkIcon;
        private static bool _isBlinkIconLoadAttempted;

        private static Texture2D _cachedLaughIcon;
        private static bool _isLaughIconLoadAttempted;

        // WHY: Timestamp (EditorApplication.timeSinceStartup) of the last logo "poke"; large-negative so no pop on first draw.
        private static double _logoPopStart = -1000.0;

        /// <summary>The "eyes-squeezed" blink frame shown occasionally over the logo in the banner.</summary>
        private static Texture2D GetBlinkIcon()
        {
            if (_isBlinkIconLoadAttempted)
            {
                return _cachedBlinkIcon;
            }

            _isBlinkIconLoadAttempted = true;

            try
            {
                EnsureNeoxiderPackageInfo();
                string root = _cachedNeoxiderRootPath;
                if (!string.IsNullOrEmpty(root))
                {
                    string p = $"{root}/Editor/Icons/NeoLogoBlink.png".Replace('\\', '/');
                    _cachedBlinkIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                }
            }
            catch
            {
            }

            return _cachedBlinkIcon;
        }

        /// <summary>The "laughing" frame shown for a short burst when the logo is clicked (poked).</summary>
        private static Texture2D GetLaughIcon()
        {
            if (_isLaughIconLoadAttempted)
            {
                return _cachedLaughIcon;
            }

            _isLaughIconLoadAttempted = true;

            try
            {
                EnsureNeoxiderPackageInfo();
                string root = _cachedNeoxiderRootPath;
                if (!string.IsNullOrEmpty(root))
                {
                    string p = $"{root}/Editor/Icons/NeoLogoLaugh.png".Replace('\\', '/');
                    _cachedLaughIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                }
            }
            catch
            {
            }

            return _cachedLaughIcon;
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
        ///     Returns whether Odin Inspector is present in the project.
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
                // WHY: Chrome is decorative: an exception inside it must never scramble the
                // property layout below (a mid-frame error desyncs GUILayout entries).
                try
                {
                    DrawNeoxiderSignature();
                    DrawDocumentationFoldout();
                }
                catch (ExitGUIException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogChromeErrorOnce(ex);
                }

                EditorGUILayout.Space(2f);
                EditorGUILayout.BeginVertical(GetNeoPropertyPanelStyle());

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
                    OnAfterDrawNeoProperties();
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

            if (target == null)
            {
                if (hasNeoNamespace)
                {
                    if (CustomEditorSettings.EnableRainbowComponentOutline)
                    {
                        EndRainbowLineTracking();
                    }

                    EditorGUILayout.EndVertical();
                }

                return;
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
        ///     Custom inspector drawing (only used when <see cref="UseCustomNeoxiderInspectorGUI" /> is true).
        /// </summary>
        protected virtual void DrawCustomNeoxiderInspectorGUI()
        {
        }

        protected virtual void DrawNeoxiderSignature()
        {
            EditorGUILayout.Space(CustomEditorSettings.SignatureSpacing);

            bool rainbow = CustomEditorSettings.EnableRainbowSignature &&
                           CustomEditorSettings.EnableRainbowSignatureAnimation;
            if (rainbow)
            {
                EnsureRepaint();
            }

            Texture2D icon = GetLibraryIcon();
            EnsureNeoxiderPackageInfo();
            string version = GetNeoxiderVersion();
            // WHY: Tick() runs an update check when the interval elapsed, otherwise reads the cache.
            NeoUpdateChecker.State updateState = NeoUpdateChecker.Tick(version, _cachedNeoxiderRootPath);

            bool updateAvailable = updateState.Status == NeoUpdateChecker.UpdateStatus.UpdateAvailable &&
                                   !string.IsNullOrEmpty(updateState.LatestVersion) &&
                                   !string.IsNullOrEmpty(updateState.UpdateUrl);

            DrawNeoxiderBanner(icon, version, updateAvailable, rainbow);
            DrawNeoxiderUpdateStrip(version, updateState);

            EditorGUILayout.Space(CustomEditorSettings.SignatureSpacing);
        }

        /// <summary>
        ///     Draws the premium gradient hero banner (logo chip, title, tagline, version pill).
        /// </summary>
        private void DrawNeoxiderBanner(Texture2D icon, string version, bool updateAvailable, bool rainbow)
        {
            const float height = 60f;
            Rect full = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            Rect rect = new(full.x + 1f, full.y, full.width - 2f, full.height);

            NeoInspectorTheme.DrawRoundedTexture(rect, NeoInspectorTheme.BannerGradient,
                new Color(1f, 1f, 1f, 0.16f), NeoInspectorTheme.RadiusCard, Color.white, 1f);
            // WHY: Soft depth: darken the lower band a touch.
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(rect.x + 6f, rect.yMax - 1f, rect.width - 12f, 1f),
                    new Color(0f, 0f, 0f, 0.14f));
            }

            const float pad = 8f;
            // WHY: The mascot reads better slightly larger than the old pad-derived 44px chip.
            float chip = height - pad * 2f + 6f;
            Rect chipRect = new(rect.x + pad, rect.y + (height - chip) * 0.5f, chip, chip);
            NeoInspectorTheme.DrawRoundedRect(chipRect, new Color(1f, 1f, 1f, 0.15f),
                new Color(1f, 1f, 1f, 0.24f), 9f, 1f);

            // WHY: The header is meant to feel alive: keep repainting so breathing / blink / pop stay smooth.
            EnsureRepaint();

            double now = EditorApplication.timeSinceStartup;

            // WHY: Idle "breathing": a slow ±4% scale pulse over a ~2.6s cycle, plus a ~1px vertical bob.
            const double breathePeriod = 2.6;
            const double bobPeriod = 3.2;
            float breatheScale = 1f + 0.04f * Mathf.Sin((float)(now * (Math.PI * 2.0 / breathePeriod)));
            float bobY = 1f * Mathf.Sin((float)(now * (Math.PI * 2.0 / bobPeriod)));

            // WHY: Click-to-pop: for ~0.5s after a poke, a springy ease-out-back overshoot (~1.35x) that settles back.
            const double popDuration = 0.5;
            double popElapsed = now - _logoPopStart;
            bool inPop = popElapsed >= 0.0 && popElapsed < popDuration;
            float popScale = 1f;
            if (inPop)
            {
                float t = Mathf.Clamp01((float)(popElapsed / popDuration));
                // WHY: One big overshoot hump (to ~1.35x) plus a small springy settle, decaying to the baseline at t = 1.
                float envelope = 1f - t;
                float wobble = Mathf.Sin(t * Mathf.PI * 2.2f);
                popScale = 1f + 0.45f * envelope * wobble;
            }

            // WHY: Eye-blink: swap the confident face for the squeezed frame in short windows (unchanged timing).
            Texture2D blinkIcon = GetBlinkIcon();
            bool blinking = false;
            if (blinkIcon != null)
            {
                double phase = now % 4.6;
                blinking = phase < 0.12 || (phase >= 0.22 && phase < 0.34);
            }

            // WHY: Face priority: click-pop (laugh) > blink > normal.
            Texture2D laughIcon = GetLaughIcon();
            Texture2D faceIcon = icon;
            if (inPop && laughIcon != null)
            {
                faceIcon = laughIcon;
            }
            else if (blinking)
            {
                faceIcon = blinkIcon;
            }

            // WHY: Composite scale: breathing baseline multiplied by the pop so the pop settles seamlessly into breathing.
            float faceScale = breatheScale * popScale;

            if (Event.current.type == EventType.Repaint)
            {
                if (faceIcon != null)
                {
                    Rect baseIconRect =
                        new(chipRect.x + 2f, chipRect.y + 2f, chipRect.width - 4f, chipRect.height - 4f);
                    float baseW = baseIconRect.width;
                    float baseH = baseIconRect.height;
                    Vector2 center = new(baseIconRect.center.x, baseIconRect.center.y + bobY);
                    Rect scaled = new(center.x - baseW * 0.5f * faceScale, center.y - baseH * 0.5f * faceScale,
                        baseW * faceScale, baseH * faceScale);
                    GUI.DrawTexture(scaled, faceIcon, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    GUIStyle glyph = new(EditorStyles.boldLabel)
                    {
                        fontSize = 26,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white }
                    };
                    GUI.Label(chipRect, "N", glyph);
                }
            }

            // WHY: Version pill (right aligned) — measure first so the title can flow up to it.
            string versionText = $"v{version}";
            GUIStyle pillTextStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            float pillW = Mathf.Max(46f, pillTextStyle.CalcSize(new GUIContent(versionText)).x + 20f);
            const float pillH = 22f;
            Rect pillRect = new(rect.xMax - pad - pillW, rect.y + (height - pillH) * 0.5f, pillW, pillH);

            // WHY: Solid dark pill so the version stays legible over the bright gradient in any state.
            Color pillBg = new(0.05f, 0.05f, 0.09f, 0.58f);
            Color pillEdge = new(1f, 1f, 1f, 0.34f);

            if (updateAvailable)
            {
                EnsureRepaint();
                float t = 0.5f + 0.5f * Mathf.Sin((float)EditorApplication.timeSinceStartup * 4f);
                pillBg = Color.Lerp(new Color(0.78f, 0.18f, 0.20f, 0.78f), new Color(0.98f, 0.40f, 0.40f, 0.92f), t);
                pillEdge = new Color(1f, 0.72f, 0.72f, 0.6f);
            }

            NeoInspectorTheme.DrawRoundedRect(pillRect, pillBg, pillEdge, NeoInspectorTheme.RadiusPill, 1f);
            // WHY: Version text is always solid white (a rainbow tint here made it vanish against the pill).
            pillTextStyle.normal.textColor = Color.white;
            GUI.Label(pillRect, versionText, pillTextStyle);

            float textX = chipRect.xMax + 12f;
            float textW = Mathf.Max(20f, pillRect.x - textX - 10f);

            GUIStyle titleStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.LowerLeft,
                clipping = TextClipping.Clip,
                normal = { textColor = new Color(1f, 1f, 1f, 0.98f) }
            };
            GUIStyle taglineStyle = new(EditorStyles.miniLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Clip,
                normal = { textColor = new Color(1f, 1f, 1f, 0.72f) }
            };

            Rect titleRect = new(textX, rect.y + 10f, textW, 22f);
            Rect taglineRect = new(textX, titleRect.yMax - 1f, textW, 16f);
            GUI.Label(titleRect, "Neoxider Tools", titleStyle);
            GUI.Label(taglineRect, "Modular Unity Toolkit", taglineStyle);

            // WHY: Poke the slime: the chip + title/tagline are clickable (but never the version pill / update strip).
            float hitRight = Mathf.Min(titleRect.xMax, pillRect.x - 2f);
            Rect logoHitRect = Rect.MinMaxRect(chipRect.xMin, rect.yMin, hitRight, rect.yMax);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                logoHitRect.Contains(Event.current.mousePosition))
            {
                _logoPopStart = EditorApplication.timeSinceStartup;
                Event.current.Use();
                Repaint();
            }
        }

        /// <summary>
        ///     Slim themed row that surfaces the package update status and its action (preserves all states).
        /// </summary>
        private void DrawNeoxiderUpdateStrip(string version, NeoUpdateChecker.State updateState)
        {
            EditorGUILayout.Space(4f);

            const float h = 26f;
            Rect full = GUILayoutUtility.GetRect(0f, h, GUILayout.ExpandWidth(true));
            Rect rect = new(full.x + 1f, full.y, full.width - 2f, full.height);

            NeoInspectorTheme.DrawRoundedRect(rect, NeoInspectorTheme.PanelBackground,
                NeoInspectorTheme.Separator, NeoInspectorTheme.RadiusRow, 1f);

            const float pad = 6f;
            Rect refreshRect = new(rect.x + pad, rect.y + (h - 18f) * 0.5f, 24f, 18f);
            GUIContent refreshContent = EditorGUIUtility.IconContent("d_Refresh");
            if (refreshContent == null || refreshContent.image == null)
            {
                refreshContent = new GUIContent("⟳");
            }

            if (DrawNeoMiniButton(refreshRect, refreshContent, NeoPropAccent, false))
            {
                EnsureNeoxiderPackageInfo();
                NeoUpdateChecker.RequestImmediateCheck(version, _cachedNeoxiderRootPath);
                EnsureRepaint();
                Repaint();
            }

            string label;
            Color color;
            string actionLabel = null;
            string actionUrl = null;

            switch (updateState.Status)
            {
                case NeoUpdateChecker.UpdateStatus.UpdateAvailable:
                    label = $"New version {updateState.LatestVersion}";
                    color = new Color(1f, 0.42f, 0.42f, 1f);
                    if (!string.IsNullOrEmpty(updateState.UpdateUrl))
                    {
                        actionLabel = "Update";
                        actionUrl = updateState.UpdateUrl;
                    }

                    break;

                case NeoUpdateChecker.UpdateStatus.UpToDate:
                    label = "Up to date";
                    color = new Color(0.35f, 0.85f, 0.48f, 1f);
                    break;

                case NeoUpdateChecker.UpdateStatus.Ahead:
                    label = $"Not published (latest: {updateState.LatestVersion})";
                    color = new Color(1f, 0.75f, 0.32f, 1f);
                    break;

                case NeoUpdateChecker.UpdateStatus.Checking:
                    label = "Checking for updates…";
                    color = new Color(0.40f, 0.72f, 1f, 1f);
                    EnsureRepaint();
                    break;

                default:
                    label = string.IsNullOrEmpty(updateState.Error)
                        ? "Click ⟳ to check for updates"
                        : updateState.Error;
                    color = string.IsNullOrEmpty(updateState.Error)
                        ? NeoInspectorTheme.MutedText
                        : new Color(1f, 0.6f, 0.24f, 1f);
                    break;
            }

            Rect dotRect = new(refreshRect.xMax + 8f, rect.y + h * 0.5f - 3f, 6f, 6f);
            NeoInspectorTheme.DrawRoundedRect(dotRect, color, 3f);

            float labelRight = rect.xMax - pad;
            if (actionLabel != null)
            {
                const float actionW = 78f;
                Rect actionRect = new(rect.xMax - pad - actionW, rect.y + (h - 18f) * 0.5f, actionW, 18f);
                if (DrawNeoMiniButton(actionRect, new GUIContent(actionLabel), new Color(0.98f, 0.42f, 0.42f, 1f), true))
                {
                    Application.OpenURL(actionUrl);
                }

                labelRight = actionRect.x - 6f;
            }

            float labelX = dotRect.xMax + 7f;
            Rect labelRect = new(labelX, rect.y, Mathf.Max(10f, labelRight - labelX), h);
            GUIStyle statusStyle = new(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                normal = { textColor = color }
            };
            GUI.Label(labelRect, label, statusStyle);
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
        ///     Holds button metadata from different ButtonAttribute types.
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
