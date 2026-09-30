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

        // WHY: Repaint leases are per-instance state; the ticker itself is shared (see TickRepaintClients).
        // A static "is animating" flag once let one closing editor strand every other editor's repaint loop.
        private bool _isRepaintClient;
        private double _animationLeaseUntil = -1.0;
        private double _liveLeaseUntil = -1.0;
        private double _statusLeaseUntil = -1.0;

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
        private bool _unityEventOnlyWithListeners;

        private string _unityEventSearch = string.Empty;

        private bool _wasResetPressed;

        /// <summary>
        ///     Lets derived editors (modules) draw their own UI inside the Neoxider look
        ///     (frame/background/rainbow line/Actions).
        /// </summary>
        protected virtual bool UseCustomNeoxiderInspectorGUI => false;

        /// <summary>
        ///     Optional feature/module label shown by the single canonical Neoxider banner.
        /// </summary>
        protected virtual string NeoxiderModuleName => null;

        /// <summary>
        ///     Invoked right after <see cref="DrawNeoPropertiesWithCollapsibleUnityEvents" /> (default MonoBehaviour
        ///     property pass). Use for extra notes or UI that should sit with the main inspector block.
        /// </summary>
        protected virtual void OnAfterDrawNeoProperties()
        {
        }

        protected virtual void OnDisable()
        {
            LeaveRepaintClients();
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

        // WHY: An editor asks for repaints as short leases, renewed by every draw. A hidden or collapsed inspector
        // stops drawing, its lease lapses and it drops out of the ticker instead of repainting forever.
        private const double RepaintLeaseSeconds = 0.5;

        // WHY: Even with animation off the header must notice a fresh console error or missing reference,
        // so it keeps a slow status refresh instead of a frame-rate loop.
        private const int StatusFps = 2;

        private static readonly List<CustomEditorBase> s_repaintClients = new();
        private static bool s_tickerHooked;
        private static double s_lastAnimationRepaintAt;
        private static double s_lastLiveRepaintAt;
        private static double s_lastStatusRepaintAt;

        internal static int RepaintClientCount => s_repaintClients.Count;

        internal bool IsRepaintClient => _isRepaintClient;

        /// <summary>
        ///     Requests decorative-animation repaints (mascot, rainbow frame). Does nothing while animation is off
        ///     (setting, or Play Mode without <see cref="CustomEditorSettings.AnimateInPlayMode" />) and is capped at
        ///     <see cref="CustomEditorSettings.AnimationFps" />.
        /// </summary>
        protected void EnsureRepaint()
        {
            if (!NeoInspectorAnimation.IsActive())
            {
                return;
            }

            _animationLeaseUntil = EditorApplication.timeSinceStartup + RepaintLeaseSeconds;
            JoinRepaintClients();
        }

        /// <summary>
        ///     Requests repaints for content that must stay current whatever the animation setting says
        ///     (update-check progress, a live condition result), at <see cref="NeoInspectorAnimation.LiveFps" />.
        /// </summary>
        protected void EnsureLiveRepaint()
        {
            _liveLeaseUntil = EditorApplication.timeSinceStartup + RepaintLeaseSeconds;
            JoinRepaintClients();
        }

        private void EnsureStatusRepaint()
        {
            _statusLeaseUntil = EditorApplication.timeSinceStartup + RepaintLeaseSeconds;
            JoinRepaintClients();
        }

        private void JoinRepaintClients()
        {
            if (_isRepaintClient)
            {
                return;
            }

            _isRepaintClient = true;
            s_repaintClients.Add(this);
            if (!s_tickerHooked)
            {
                s_tickerHooked = true;
                EditorApplication.update += TickRepaintClients;
            }
        }

        private void LeaveRepaintClients()
        {
            if (!_isRepaintClient)
            {
                return;
            }

            _isRepaintClient = false;
            s_repaintClients.Remove(this);
            if (s_repaintClients.Count == 0)
            {
                UnhookTicker();
            }
        }

        private static void UnhookTicker()
        {
            if (s_tickerHooked)
            {
                s_tickerHooked = false;
                EditorApplication.update -= TickRepaintClients;
            }
        }

        private static void TickRepaintClients()
        {
            TickRepaintClients(EditorApplication.timeSinceStartup);
        }

        // WHY: One shared ticker with one throttle per channel. Per-editor subscriptions each kept their own timer,
        // so N components on a GameObject repainted N times per interval and the fps cap did nothing.
        internal static void TickRepaintClients(double now)
        {
            bool animationActive = NeoInspectorAnimation.IsActive();
            bool animationDue = animationActive && NeoInspectorAnimation.IsRepaintDue(now,
                s_lastAnimationRepaintAt, CustomEditorSettings.AnimationFps);
            bool liveDue = NeoInspectorAnimation.IsRepaintDue(now, s_lastLiveRepaintAt,
                NeoInspectorAnimation.LiveFps);
            bool statusDue = NeoInspectorAnimation.IsRepaintDue(now, s_lastStatusRepaintAt, StatusFps);

            for (int i = s_repaintClients.Count - 1; i >= 0; i--)
            {
                CustomEditorBase client = s_repaintClients[i];
                if (client == null || client.target == null)
                {
                    s_repaintClients.RemoveAt(i);
                    if (client != null)
                    {
                        client._isRepaintClient = false;
                    }

                    continue;
                }

                bool wantsAnimation = animationActive && now < client._animationLeaseUntil;
                bool wantsLive = now < client._liveLeaseUntil;
                bool wantsStatus = now < client._statusLeaseUntil;
                if (!wantsAnimation && !wantsLive && !wantsStatus)
                {
                    s_repaintClients.RemoveAt(i);
                    client._isRepaintClient = false;
                    continue;
                }

                if ((wantsAnimation && animationDue) || (wantsLive && liveDue) || (wantsStatus && statusDue))
                {
                    client.Repaint();
                }
            }

            if (animationDue)
            {
                s_lastAnimationRepaintAt = now;
            }

            if (liveDue)
            {
                s_lastLiveRepaintAt = now;
            }

            if (statusDue)
            {
                s_lastStatusRepaintAt = now;
            }

            if (s_repaintClients.Count == 0)
            {
                UnhookTicker();
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

            bool hasNeoNamespace = IsNeoNamespaceTarget(target);

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
                Rect neoPanelRect = EditorGUILayout.BeginVertical(GetNeoPropertyPanelStyle());
                if (Event.current.type == EventType.Repaint && neoPanelRect.width > 0f)
                {
                    _neoPanelRect = neoPanelRect;
                    DrawNeoPropertyPanelBackground(neoPanelRect);
                }

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

            // WHY: One health report per frame; the banner badge and the panel below it read the same numbers.
            NeoComponentHealth.Report health = NeoComponentHealth.GetReport(target);
            DrawNeoxiderBanner(icon, version, updateAvailable, rainbow, NeoxiderModuleName, health);
            DrawNeoxiderUpdateStrip(version, updateState);
            DrawHealthPanel(health);

            EditorGUILayout.Space(CustomEditorSettings.SignatureSpacing);
        }

        /// <summary>
        ///     Draws the premium gradient hero banner (logo chip, title, tagline, version pill).
        /// </summary>
        private void DrawNeoxiderBanner(Texture2D icon, string version, bool updateAvailable, bool rainbow,
            string moduleName, in NeoComponentHealth.Report health)
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
            // With animation off (setting, or Play Mode) it renders one still frame and only polls its status.
            bool animating = NeoInspectorAnimation.IsActive();
            if (animating)
            {
                EnsureRepaint();
            }
            else
            {
                EnsureStatusRepaint();
            }

            double realNow = EditorApplication.timeSinceStartup;
            // WHY: A clock frozen at 0 collapses breathing, bob and pop to their rest values (sin 0 = 0).
            double now = animating ? realNow : 0.0;

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
            if (animating && blinkIcon != null)
            {
                double phase = now % 4.6;
                blinking = phase < 0.12 || (phase >= 0.22 && phase < 0.34);
            }

            // WHY: Face priority: click-pop (laugh) > surprised > alarmed/worried > watching (play) > blink > neutral.
            Texture2D laughIcon = GetLaughIcon();
            Texture2D faceIcon = SelectMascotFace(icon, blinkIcon, blinking, realNow, animating, health);
            if (inPop && laughIcon != null)
            {
                faceIcon = laughIcon;
            }

            // WHY: Composite scale: breathing baseline multiplied by the pop so the pop settles seamlessly into breathing.
            float faceScale = breatheScale * popScale;

            if (Event.current.type == EventType.Repaint)
            {
                if (faceIcon != null)
                {
                    // WHY: The mascot art carries ~25% transparent padding; overscan + clip turns it
                    // into a close-up that nearly fills the chip (clip keeps the pop inside the frame).
                    const float zoom = 1.28f;
                    const float contentCenterShift = 0.018f;
                    GUI.BeginGroup(chipRect);
                    float size = chipRect.width * zoom * faceScale;
                    float cx = chipRect.width * 0.5f;
                    float cy = chipRect.height * 0.5f - chipRect.width * zoom * contentCenterShift + bobY;
                    Rect scaled = new(cx - size * 0.5f, cy - size * 0.5f, size, size);
                    DrawMascotFace(scaled, faceIcon);
                    GUI.EndGroup();
                }
                else
                {
                    GUI.Label(chipRect, "N", NeoInspectorStyles.Glyph);
                }
            }

            // WHY: Badge handles its own click and Uses the event, so it wins over the slime poke below.
            DrawHealthBadge(chipRect, health);

            // WHY: Version pill (right aligned) — measure first so the title can flow up to it.
            string versionText = NeoInspectorStyles.VersionLabel(version);
            GUIStyle pillTextStyle = NeoInspectorStyles.VersionPill;
            float pillW = NeoInspectorStyles.VersionPillWidth(versionText);
            const float pillH = 22f;
            Rect pillRect = new(rect.xMax - pad - pillW, rect.y + (height - pillH) * 0.5f, pillW, pillH);

            // WHY: Solid dark pill so the version stays legible over the bright gradient in any state.
            Color pillBg = new(0.05f, 0.05f, 0.09f, 0.58f);
            Color pillEdge = new(1f, 1f, 1f, 0.34f);

            if (updateAvailable)
            {
                EnsureRepaint();
                float t = animating ? 0.5f + 0.5f * Mathf.Sin((float)realNow * 4f) : 0.5f;
                pillBg = Color.Lerp(new Color(0.78f, 0.18f, 0.20f, 0.78f), new Color(0.98f, 0.40f, 0.40f, 0.92f), t);
                pillEdge = new Color(1f, 0.72f, 0.72f, 0.6f);
            }

            NeoInspectorTheme.DrawRoundedRect(pillRect, pillBg, pillEdge, NeoInspectorTheme.RadiusPill, 1f);
            GUI.Label(pillRect, versionText, pillTextStyle);

            float textX = chipRect.xMax + 12f;
            float textW = Mathf.Max(20f, pillRect.x - textX - 10f);

            GUIStyle titleStyle = NeoInspectorStyles.BannerTitle;
            GUIStyle taglineStyle = NeoInspectorStyles.BannerTagline;

            Rect titleRect = new(textX, rect.y + 10f, textW, 22f);
            Rect taglineRect = new(textX, titleRect.yMax - 1f, textW, 16f);
            GUI.Label(titleRect, "Neoxider Tools", titleStyle);
            string tagline = string.IsNullOrWhiteSpace(moduleName)
                ? "Modular Unity Toolkit"
                : "Module · " + moduleName;
            GUI.Label(taglineRect, new GUIContent(tagline, "Neoxider Tools · Modular Unity Toolkit"), taglineStyle);

            // WHY: Poke the slime: the chip + title/tagline are clickable (but never the version pill / update strip).
            float hitRight = Mathf.Min(titleRect.xMax, pillRect.x - 2f);
            Rect logoHitRect = Rect.MinMaxRect(chipRect.xMin, rect.yMin, hitRight, rect.yMax);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                logoHitRect.Contains(Event.current.mousePosition))
            {
                // WHY: Poking the slime is pure fun for now — a pop bounce plus a startled face.
                // With animation off there is nothing to play, so the click is only consumed.
                // TODO: decide what a mascot click should DO (docs? field filter? changelog popup?).
                if (animating)
                {
                    _logoPopStart = realNow;
                    _surpriseStart = realNow;
                }

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
            if (DrawNeoMiniButton(refreshRect, NeoInspectorStyles.RefreshIcon, NeoPropAccent, false))
            {
                EnsureNeoxiderPackageInfo();
                NeoUpdateChecker.RequestImmediateCheck(version, _cachedNeoxiderRootPath);
                EnsureLiveRepaint();
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
                    EnsureLiveRepaint();
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
            GUI.Label(labelRect, label, NeoInspectorStyles.Status(color));
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

        // WHY: Method lists and their [Button] metadata cannot change without a domain reload, yet every IMGUI
        // event used to reflect over all methods and read their attributes twice. Reflect once per type.
        private static readonly Dictionary<Type, MethodInfo[]> s_allMethodsCache = new();
        private static readonly Dictionary<Type, MethodInfo[]> s_buttonMethodsCache = new();
        private static readonly Dictionary<Type, bool> s_neoNamespaceCache = new();

        internal static MethodInfo[] GetCachedMethods(Type type)
        {
            if (!s_allMethodsCache.TryGetValue(type, out MethodInfo[] methods))
            {
                methods = type.GetMethods(
                    BindingFlags.Instance
                    | BindingFlags.Static
                    | BindingFlags.Public
                    | BindingFlags.NonPublic);
                s_allMethodsCache[type] = methods;
            }

            return methods;
        }

        internal MethodInfo[] GetButtonMethods()
        {
            if (target == null)
            {
                return Array.Empty<MethodInfo>();
            }

            Type type = target.GetType();
            if (!s_buttonMethodsCache.TryGetValue(type, out MethodInfo[] buttons))
            {
                buttons = GetCachedMethods(type)
                    .Where(m => m != null && FindButtonAttribute(m).HasValue)
                    .ToArray();
                s_buttonMethodsCache[type] = buttons;
            }

            return buttons;
        }

        private static bool IsNeoNamespaceTarget(Object inspected)
        {
            if (inspected == null)
            {
                return false;
            }

            Type type = inspected.GetType();
            if (!s_neoNamespaceCache.TryGetValue(type, out bool isNeo))
            {
                isNeo = IsNeoNamespace(type.Namespace);
                s_neoNamespaceCache[type] = isNeo;
            }

            return isNeo;
        }

        internal static bool IsNeoNamespace(string typeNamespace)
        {
            return typeNamespace != null
                   && (typeNamespace == "Neo" || typeNamespace.StartsWith("Neo.", StringComparison.Ordinal));
        }

        protected abstract void ProcessAttributeAssignments();

        /// <summary>
        ///     Holds button metadata from different ButtonAttribute types.
        /// </summary>
        protected struct ButtonInfo
        {
            public string ButtonName;
            public bool PlayModeOnly;
            public float Width;

            public ButtonInfo(string name, float width, bool playModeOnly = false)
            {
                ButtonName = name;
                Width = width;
                PlayModeOnly = playModeOnly;
            }
        }
    }
}
