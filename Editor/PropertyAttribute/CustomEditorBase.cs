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
        /// <summary>Instance fields of any visibility; the field set the attribute processors inspect.</summary>
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

        protected Dictionary<string, bool> _isFirstRun = new();

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
        private static bool s_tickerWasFocused = true;
        private static double s_lastAnimationRepaintAt;
        private static double s_lastLiveRepaintAt;
        private static double s_lastStatusRepaintAt;

        internal static int RepaintClientCount => s_repaintClients.Count;

        internal bool IsRepaintClient => _isRepaintClient;

        private int _tickerRepaints;

        /// <summary>How many repaints the shared ticker requested for this editor (test hook).</summary>
        internal int TickerRepaintCount => _tickerRepaints;

        private void RepaintFromTicker()
        {
            _tickerRepaints++;
            Repaint();
        }

        /// <summary>
        ///     Inspects this editor's leases without ticking other clients or changing shared repaint clocks.
        /// </summary>
        internal bool HasRepaintLease(double now)
        {
            return target != null &&
                   ((NeoInspectorAnimation.IsActive() && now < _animationLeaseUntil) ||
                    now < _liveLeaseUntil || now < _statusLeaseUntil);
        }

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
        ///     Keeps its rate while Unity is in the background, unlike <see cref="EnsureRepaint" />.
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
            TickRepaintClients(EditorApplication.timeSinceStartup, EditorApplication.isFocused);
        }

        /// <summary>Ticks the shared repaint clock as if the editor were focused.</summary>
        internal static void TickRepaintClients(double now)
        {
            TickRepaintClients(now, true);
        }

        // WHY: One shared ticker with one throttle per channel. Per-editor subscriptions each kept their own timer,
        // so N components on a GameObject repainted N times per interval and the fps cap did nothing.
        internal static void TickRepaintClients(double now, bool editorFocused)
        {
            // WHY: with Unity in the background the decorative animation only burnt CPU behind another application,
            // so that channel idles. Live data (a NeoCondition result, update-check progress) and the 2 Hz status
            // keep their rate: Unity on a second monitor still shows them. Clients are kept while idle (their
            // animation lease is not judged) and each gets one repaint when the editor is focused again, which
            // renews the animation leases of the inspectors still visible.
            bool focused = NeoInspectorAnimation.ShouldTick(editorFocused);
            if (focused && !s_tickerWasFocused)
            {
                for (int i = s_repaintClients.Count - 1; i >= 0; i--)
                {
                    CustomEditorBase client = s_repaintClients[i];
                    if (client != null && client.target != null)
                    {
                        client._statusLeaseUntil = Math.Max(client._statusLeaseUntil, now + RepaintLeaseSeconds);
                        client.RepaintFromTicker();
                    }
                }
            }

            s_tickerWasFocused = focused;
            bool animationActive = focused && NeoInspectorAnimation.IsActive();
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
                if (!focused)
                {
                    if ((wantsLive && liveDue) || (wantsStatus && statusDue))
                    {
                        client.RepaintFromTicker();
                    }

                    continue;
                }

                if (!client.HasRepaintLease(now))
                {
                    s_repaintClients.RemoveAt(i);
                    client._isRepaintClient = false;
                    continue;
                }

                if ((wantsAnimation && animationDue) || (wantsLive && liveDue) || (wantsStatus && statusDue))
                {
                    client.RepaintFromTicker();
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

        // WHY: Timestamp (EditorApplication.timeSinceStartup) of this editor's last logo "poke"; large-negative so
        // there is no pop on the first draw. Per editor, so poking one mascot no longer bounces every other one.
        private double _logoPopStart = -1000.0;

        private string _pendingMascotDocPath;

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
                    // WHY: NeoLogo.png is also a runtime UI sprite (demo scenes), so it keeps its large, compressed,
                    // mip-less import. Drawn at banner size between the mipmapped 256px face variants it visibly
                    // changed quality on every blink, so an editor-only copy with the variants' import settings is
                    // preferred when present.
                    string neutralPath = $"{root}/Editor/Icons/NeoLogoNeutral.png".Replace('\\', '/');
                    _cachedLibraryIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(neutralPath);

                    if (_cachedLibraryIcon == null)
                    {
                        string iconPath = $"{root}/NeoLogo.png".Replace('\\', '/');
                        _cachedLibraryIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
                    }

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

        // WHY: every transition of one editor reads one clock per IMGUI frame. The clock is sampled on the Layout
        // event that starts each frame, so Layout and the event after it agree on how far a section has opened;
        // two different readings would give them different layouts and desync GUILayout.
        private double _motionNow = -1.0;

        /// <summary>Time used by this editor's transitions in the current IMGUI frame.</summary>
        internal double MotionNow => _motionNow;

        /// <summary>Test hook: pins the transition clock as if a Layout event had sampled <paramref name="now" />.</summary>
        internal void SetMotionClockForTests(double now)
        {
            _motionNow = now;
        }

        private void SampleMotionClock()
        {
            Event current = Event.current;
            if (_motionNow < 0.0 || (current != null && current.type == EventType.Layout))
            {
                _motionNow = EditorApplication.timeSinceStartup;
            }
        }

        public override void OnInspectorGUI()
        {
            Event current = Event.current;
            // WHY: commandName is marshalled from native code on every read; only command events carry one.
            if ((current.type == EventType.ExecuteCommand || current.type == EventType.ValidateCommand) &&
                current.commandName == "Reset")
            {
                _wasResetPressed = true;
            }

            SampleMotionClock();

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

        /// <summary>
        ///     Draws the header (full banner or compact row, with the version/update pill and the health badge) and
        ///     the health panel above the properties of a Neo component.
        /// </summary>
        protected virtual void DrawNeoxiderSignature()
        {
            NeoInspectorStyles.Space(CustomEditorSettings.SignatureSpacing);

            Texture2D icon = GetLibraryIcon();
            EnsureNeoxiderPackageInfo();
            string version = GetNeoxiderVersion();
            // WHY: Tick() runs an update check when the interval elapsed, otherwise reads the cache.
            NeoUpdateChecker.State updateState = NeoUpdateChecker.Tick(version, _cachedNeoxiderRootPath);

            // WHY: One health report per frame; the header badge and the panel below it read the same numbers.
            NeoComponentHealth.Report health = NeoComponentHealth.GetReport(target);
            if (UseCompactHeader())
            {
                DrawCompactHeader(icon, version, updateState, NeoxiderModuleName, health);
            }
            else
            {
                DrawNeoxiderBanner(icon, version, updateState, NeoxiderModuleName, health);
            }

            DrawHealthPanel(health);

            NeoInspectorStyles.Space(CustomEditorSettings.SignatureSpacing);
        }

        private static readonly List<Component> s_componentScratch = new();
        private static int s_hierarchyEpoch;
        private static bool s_hierarchyHooked;
        private int _bannerEpoch = -1;
        private double _bannerCheckedAt = -1000.0;
        private bool _isFirstNeoComponent = true;

        /// <summary>
        ///     True when this inspector draws the compact header: the banner is switched off, or another Neo component
        ///     above it on the same GameObject already shows the full banner.
        /// </summary>
        internal bool UseCompactHeader()
        {
            if (!CustomEditorSettings.EnableRainbowSignature)
            {
                return true;
            }

            return CustomEditorSettings.CompactRepeatedBanners && !IsFirstNeoComponentOnGameObject();
        }

        // WHY: the component order only changes with the hierarchy; GetComponents runs again after a hierarchy
        // change and at most every two seconds as a backstop for edits that raise no hierarchy event.
        private bool IsFirstNeoComponentOnGameObject()
        {
            if (!s_hierarchyHooked)
            {
                s_hierarchyHooked = true;
                EditorApplication.hierarchyChanged += () => s_hierarchyEpoch++;
            }

            double now = EditorApplication.timeSinceStartup;
            if (_bannerEpoch == s_hierarchyEpoch && now - _bannerCheckedAt < 2.0)
            {
                return _isFirstNeoComponent;
            }

            _bannerEpoch = s_hierarchyEpoch;
            _bannerCheckedAt = now;
            _isFirstNeoComponent = ComputeIsFirstNeoComponent(target as Component);
            return _isFirstNeoComponent;
        }

        internal static bool ComputeIsFirstNeoComponent(Component component)
        {
            if (component == null)
            {
                return true;
            }

            try
            {
                component.GetComponents(s_componentScratch);
                for (int i = 0; i < s_componentScratch.Count; i++)
                {
                    Component candidate = s_componentScratch[i];
                    // WHY: a component hidden from the Inspector draws no header, so it cannot be the one that keeps
                    // the banner for the others.
                    if (candidate is MonoBehaviour && (candidate.hideFlags & HideFlags.HideInInspector) == 0 &&
                        IsNeoNamespaceTarget(candidate))
                    {
                        return candidate == component;
                    }
                }

                return true;
            }
            finally
            {
                s_componentScratch.Clear();
            }
        }

        private const float BannerHeight = 44f;
        private const float CompactHeaderHeight = 24f;
        private const float VersionPillHeight = 18f;

        /// <summary>
        ///     The full header: brand gradient card with the mascot chip, title and tagline, and the version/update
        ///     pill on the right. 44px: one third lighter than the v10 banner plus its separate update strip.
        /// </summary>
        private void DrawNeoxiderBanner(Texture2D icon, string version, NeoUpdateChecker.State updateState,
            string moduleName, in NeoComponentHealth.Report health)
        {
            Rect full = GUILayoutUtility.GetRect(0f, BannerHeight, NeoInspectorStyles.ExpandWidth);
            Rect rect = NeoInspectorTheme.Snap(new Rect(full.x + 1f, full.y, full.width - 2f, full.height));

            NeoInspectorTheme.DrawRoundedTexture(rect, NeoInspectorTheme.BannerGradient,
                new Color(1f, 1f, 1f, 0.12f), NeoInspectorTheme.RadiusCard, Color.white, 1f);
            if (Event.current.type == EventType.Repaint)
            {
                // WHY: a faint top sheen and a darker foot give the flat gradient a little depth without noise.
                NeoInspectorTheme.DrawRoundedRect(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f,
                    rect.height * 0.45f), new Color(1f, 1f, 1f, 0.05f), NeoInspectorTheme.RadiusCard - 1f);
                EditorGUI.DrawRect(new Rect(rect.x + 8f, rect.yMax - 1f, rect.width - 16f, 1f),
                    new Color(0f, 0f, 0f, 0.12f));
            }

            const float pad = 5f;
            float chip = BannerHeight - pad * 2f;
            Rect chipRect = new(rect.x + pad, rect.y + pad, chip, chip);
            NeoInspectorTheme.DrawRoundedRect(chipRect, new Color(1f, 1f, 1f, 0.14f),
                new Color(1f, 1f, 1f, 0.22f), NeoInspectorTheme.RadiusCard, 1f);

            DrawMascot(chipRect, icon, health, 1.24f, true);

            // WHY: Badge handles its own click and Uses the event, so it wins over the slime poke below.
            DrawHealthBadge(chipRect, health);

            Rect pillArea = DrawVersionCluster(rect, version, updateState, true);

            float textX = chipRect.xMax + 9f;
            float textW = pillArea.x - textX - 8f;
            Rect titleRect = new(textX, rect.y + 6f, Mathf.Max(0f, textW), 18f);
            Rect taglineRect = new(textX, titleRect.yMax, Mathf.Max(0f, textW), 14f);
            // WHY: below ~48px of room the clipped title is noise; the mascot and the pill still identify the header.
            if (textW >= 48f)
            {
                GUI.Label(titleRect, NeoInspectorStyles.CachedContent("Neoxider Tools"), NeoInspectorStyles.BannerTitle);
                GUI.Label(taglineRect, GetBannerTagline(moduleName), NeoInspectorStyles.BannerTagline);
            }

            // WHY: Poke the slime: the chip + title/tagline are clickable (but never the version pill).
            Rect logoHitRect = Rect.MinMaxRect(chipRect.xMin, rect.yMin, Mathf.Max(chipRect.xMax, pillArea.x - 4f),
                rect.yMax);
            HandleMascotPoke(logoHitRect, chipRect);
        }

        /// <summary>
        ///     One-row header used when the banner is off or repeats on the same GameObject: small mascot, title and
        ///     module, version pill. Health badge and update state stay visible.
        /// </summary>
        private void DrawCompactHeader(Texture2D icon, string version, NeoUpdateChecker.State updateState,
            string moduleName, in NeoComponentHealth.Report health)
        {
            Rect full = GUILayoutUtility.GetRect(0f, CompactHeaderHeight, NeoInspectorStyles.ExpandWidth);
            Rect rect = NeoInspectorTheme.Snap(new Rect(full.x + 1f, full.y, full.width - 2f, full.height));

            NeoInspectorTheme.DrawRoundedRect(rect, NeoInspectorTheme.PanelBackground, NeoInspectorTheme.Separator,
                NeoInspectorTheme.RadiusRow, 1f);
            NeoInspectorTheme.DrawAccentRail(rect, NeoInspectorTheme.BrandViolet, 3f, 5f);

            Rect chipRect = new(rect.x + 8f, rect.y + 2f, rect.height - 4f, rect.height - 4f);
            DrawMascot(chipRect, icon, health, 1.18f, false);
            DrawHealthBadge(chipRect, health);

            Rect pillArea = DrawVersionCluster(rect, version, updateState, false);

            float textX = chipRect.xMax + 6f;
            float textW = pillArea.x - textX - 6f;
            if (textW >= 40f)
            {
                GUI.Label(new Rect(textX, rect.y, textW, rect.height),
                    NeoInspectorStyles.CachedContent(GetCompactTitle(moduleName)),
                    NeoInspectorStyles.CompactTitle(NeoInspectorTheme.TitleText));
            }

            HandleMascotPoke(Rect.MinMaxRect(chipRect.xMin, rect.yMin, Mathf.Max(chipRect.xMax, pillArea.x - 4f),
                rect.yMax), chipRect);
        }

        /// <summary>
        ///     Draws the mascot face into <paramref name="chipRect" /> (clipped), with breathing, blink and the
        ///     click pop when the mascot may animate. One still frame otherwise.
        /// </summary>
        private void DrawMascot(Rect chipRect, Texture2D icon, in NeoComponentHealth.Report health, float zoom,
            bool bob)
        {
            bool animating = NeoInspectorAnimation.IsActive() && CustomEditorSettings.EnableRainbowSignatureAnimation;
            if (animating)
            {
                EnsureRepaint();
            }
            else
            {
                // WHY: still mascot: the badge must still notice a new console error or missing reference.
                EnsureStatusRepaint();
            }

            double realNow = EditorApplication.timeSinceStartup;
            // WHY: A clock frozen at 0 collapses breathing, bob and pop to their rest values.
            double now = animating ? realNow : 0.0;

            // WHY: Idle "breathing": a cosine wave (no corners at the turning points) over 2.8s: +3% scale and the
            // face rises by a pixel while it inhales.
            float breath = NeoInspectorAnimation.Breath(now, 2.8);
            float breatheScale = 1f + 0.03f * breath;
            float bobY = bob ? -1f * breath : 0f;

            // WHY: Click-to-pop: a damped spring that overshoots to ~1.25x and settles exactly on the baseline.
            const double popDuration = 0.55;
            double popElapsed = now - _logoPopStart;
            bool inPop = animating && popElapsed >= 0.0 && popElapsed < popDuration;
            float popScale = inPop
                ? 1f + NeoInspectorAnimation.SpringPop((float)(popElapsed / popDuration), 0.62f)
                : 1f;

            // WHY: Eye-blink: swap the confident face for the squeezed frame in short windows (a double blink).
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

            float faceScale = breatheScale * popScale;

            // WHY: GUI.BeginGroup takes a control id. Inside a Repaint-only branch it shifted every control id after
            // the header by one on Repaint only, so a press was drawn on the button above the one clicked and text
            // fields lost their selection highlight. The group opens on every event; only the drawing is Repaint-only.
            GUI.BeginGroup(chipRect);
            if (Event.current.type == EventType.Repaint)
            {
                if (faceIcon != null)
                {
                    // WHY: The mascot art carries ~25% transparent padding; overscan + clip turns it
                    // into a close-up that nearly fills the chip (clip keeps the pop inside the frame).
                    const float contentCenterShift = 0.018f;
                    float size = chipRect.width * zoom * faceScale;
                    float cx = chipRect.width * 0.5f;
                    float cy = chipRect.height * 0.5f - chipRect.width * zoom * contentCenterShift + bobY;
                    Rect scaled = new(cx - size * 0.5f, cy - size * 0.5f, size, size);
                    DrawMascotFace(scaled, faceIcon);
                }
                else
                {
                    GUI.Label(new Rect(0f, 0f, chipRect.width, chipRect.height), NeoInspectorStyles.CachedContent("N"),
                        NeoInspectorStyles.Glyph);
                }
            }

            GUI.EndGroup();
        }

        /// <summary>
        ///     Header click handling: anywhere in <paramref name="hitRect" /> pokes the slime; a single click on the
        ///     mascot chip itself (<paramref name="docRect" />) also opens the component's documentation page.
        /// </summary>
        private void HandleMascotPoke(Rect hitRect, Rect docRect)
        {
            HandleMascotPoke(Event.current, hitRect, docRect);
        }

        /// <summary>
        ///     Handles one header event: the link cursor on Repaint, a poke bounce for a left MouseDown inside
        ///     <paramref name="hitRect" /> and a documentation open for a single click inside <paramref name="docRect" />.
        /// </summary>
        internal void HandleMascotPoke(Event current, Rect hitRect, Rect docRect)
        {
            if (current == null)
            {
                return;
            }

            if (current.type == EventType.Repaint)
            {
                // WHY: the link cursor tells the chip opens something; the rest of the header only bounces the slime.
                if (!string.IsNullOrEmpty(GetMascotDocPath()))
                {
                    EditorGUIUtility.AddCursorRect(docRect, MouseCursor.Link);
                }

                return;
            }

            if (current.type != EventType.MouseDown || current.button != 0 || !hitRect.Contains(current.mousePosition))
            {
                return;
            }

            // WHY: A poke plays a spring bounce plus a startled face (with animation off there is nothing to play).
            if (NeoInspectorAnimation.IsActive() && CustomEditorSettings.EnableRainbowSignatureAnimation)
            {
                double realNow = EditorApplication.timeSinceStartup;
                _logoPopStart = realNow;
                _surpriseStart = realNow;
            }

            TryQueueMascotDocOpen(current.mousePosition, current.clickCount, docRect);
            current.Use();
            Repaint();
        }

        /// <summary>
        ///     Queues the documentation page of a mascot click: only a single click inside <paramref name="docRect" />
        ///     of a component that has a page (the one the Documentation section's button opens) queues it, and
        ///     repeated clicks before the queue runs open the page once.
        /// </summary>
        /// <returns>True when the page is queued.</returns>
        internal bool TryQueueMascotDocOpen(Vector2 mousePosition, int clickCount, Rect docRect)
        {
            if (clickCount > 1 || !docRect.Contains(mousePosition))
            {
                return false;
            }

            string docPath = GetMascotDocPath();
            if (string.IsNullOrEmpty(docPath))
            {
                return false;
            }

            if (_pendingMascotDocPath == null)
            {
                // WHY: deferred to after this GUI pass. The viewer window, or the fallback that selects the .md
                // file, can rebuild this inspector while its header is still being laid out.
                EditorApplication.delayCall += OpenPendingMascotDoc;
            }

            _pendingMascotDocPath = docPath;
            return true;
        }

        /// <summary>Page a mascot click queued and the next editor tick opens, or null.</summary>
        internal string PendingMascotDocPath => _pendingMascotDocPath;

        /// <summary>Drops a queued mascot page without opening it (test hook).</summary>
        internal void CancelPendingMascotDoc()
        {
            if (_pendingMascotDocPath == null)
            {
                return;
            }

            _pendingMascotDocPath = null;
            EditorApplication.delayCall -= OpenPendingMascotDoc;
        }

        private void OpenPendingMascotDoc()
        {
            string docPath = _pendingMascotDocPath;
            _pendingMascotDocPath = null;
            if (!string.IsNullOrEmpty(docPath))
            {
                NeoDocHelper.OpenDocInWindow(docPath);
            }
        }

        /// <summary>
        ///     Documentation page a mascot click opens: the page the Documentation section shows for the inspected
        ///     type (its [NeoDoc] link, TypeName.md or a base type's page), or null when there is none.
        /// </summary>
        internal string GetMascotDocPath()
        {
            if (target == null)
            {
                return null;
            }

            EnsureNeoxiderPackageInfo();
            if (string.IsNullOrEmpty(_cachedNeoxiderRootPath))
            {
                return null;
            }

            return NeoDocHelper.GetDocPathForType(_cachedNeoxiderRootPath, target.GetType());
        }

        /// <summary>
        ///     Draws the right-aligned version pill (status dot + version; click to check or to open the release) and,
        ///     when a newer version is published, the "Update" pill before it. Returns the area both occupy.
        /// </summary>
        private Rect DrawVersionCluster(Rect header, string version, NeoUpdateChecker.State updateState,
            bool onGradient)
        {
            string versionText = NeoInspectorStyles.VersionLabel(version);
            float pillW = NeoInspectorStyles.VersionPillWidth(versionText);
            float pad = onGradient ? 8f : 6f;
            Rect pillRect = NeoInspectorTheme.Snap(new Rect(header.xMax - pad - pillW,
                header.y + (header.height - VersionPillHeight) * 0.5f, pillW, VersionPillHeight));

            bool updateAvailable = updateState.Status == NeoUpdateChecker.UpdateStatus.UpdateAvailable &&
                                   !string.IsNullOrEmpty(updateState.UpdateUrl);
            if (updateState.Status == NeoUpdateChecker.UpdateStatus.Checking)
            {
                EnsureLiveRepaint();
            }

            Color status = UpdateStatusColor(updateState);
            bool hover = DrawPill(pillRect, versionText, GetVersionTooltip(updateState), status, onGradient, false,
                out bool clicked);
            if (clicked)
            {
                if (updateAvailable)
                {
                    Application.OpenURL(updateState.UpdateUrl);
                }
                else
                {
                    EnsureNeoxiderPackageInfo();
                    NeoUpdateChecker.RequestImmediateCheck(version, _cachedNeoxiderRootPath);
                    EnsureLiveRepaint();
                    Repaint();
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                Rect dot = NeoInspectorTheme.Snap(new Rect(pillRect.x + 8f, pillRect.center.y - 3f, 6f, 6f));
                float dotAlpha = 1f;
                if (updateState.Status == NeoUpdateChecker.UpdateStatus.Checking && NeoInspectorAnimation.IsActive())
                {
                    dotAlpha = 0.35f + 0.65f * NeoInspectorAnimation.Breath(EditorApplication.timeSinceStartup, 0.9);
                }

                status.a *= dotAlpha * (hover ? 1f : 0.92f);
                NeoInspectorTheme.DrawRoundedRect(dot, status, 3f);
            }

            if (!updateAvailable)
            {
                return pillRect;
            }

            GUIContent updateContent = GetUpdateButtonContent(updateState.LatestVersion);
            float updateW = 58f;
            Rect updateRect = NeoInspectorTheme.Snap(new Rect(pillRect.x - 6f - updateW, pillRect.y, updateW,
                VersionPillHeight));
            if (updateRect.x < header.x + header.width * 0.45f)
            {
                return pillRect;
            }

            DrawPill(updateRect, updateContent.text, updateContent.tooltip, UpdateAvailableColor, onGradient, true,
                out bool updateClicked);
            if (updateClicked)
            {
                Application.OpenURL(updateState.UpdateUrl);
            }

            return Rect.MinMaxRect(updateRect.xMin, pillRect.yMin, pillRect.xMax, pillRect.yMax);
        }

        private static readonly Color UpdateAvailableColor = new(0.96f, 0.42f, 0.40f, 1f);

        private static Color UpdateStatusColor(NeoUpdateChecker.State state)
        {
            switch (state.Status)
            {
                case NeoUpdateChecker.UpdateStatus.UpdateAvailable:
                    return UpdateAvailableColor;
                case NeoUpdateChecker.UpdateStatus.UpToDate:
                    return new Color(0.36f, 0.84f, 0.52f, 1f);
                case NeoUpdateChecker.UpdateStatus.Ahead:
                    return new Color(1f, 0.74f, 0.32f, 1f);
                case NeoUpdateChecker.UpdateStatus.Checking:
                    return new Color(0.42f, 0.74f, 1f, 1f);
                default:
                    return string.IsNullOrEmpty(state.Error)
                        ? new Color(0.62f, 0.66f, 0.74f, 1f)
                        : new Color(1f, 0.60f, 0.26f, 1f);
            }
        }

        /// <summary>
        ///     A rounded pill with a centred label and a tooltip; returns whether the mouse is over it and reports a
        ///     click. On the gradient it is dark glass with white text, on a panel an accent-tinted chip.
        /// </summary>
        private static bool DrawPill(Rect rect, string text, string tooltip, Color accent, bool onGradient,
            bool filled, out bool clicked)
        {
            bool hover = rect.Contains(Event.current.mousePosition) && GUI.enabled;
            if (Event.current.type == EventType.Repaint)
            {
                float hoverAmount = NeoInspectorAnimation.Hover(NeoInspectorAnimation.ControlKey(rect), hover);
                Color bg;
                Color edge;
                Color textColor;
                if (filled)
                {
                    bg = Color.Lerp(accent, Color.white, 0.12f * hoverAmount);
                    bg.a = onGradient ? 0.92f : 1f;
                    edge = new Color(1f, 1f, 1f, 0.28f + 0.2f * hoverAmount);
                    textColor = Color.white;
                }
                else if (onGradient)
                {
                    bg = new Color(0.04f, 0.04f, 0.10f, 0.42f + 0.14f * hoverAmount);
                    edge = new Color(1f, 1f, 1f, 0.22f + 0.18f * hoverAmount);
                    textColor = Color.white;
                }
                else
                {
                    bg = NeoInspectorTheme.AccentFill(accent, 0.7f + 0.5f * hoverAmount);
                    edge = new Color(accent.r, accent.g, accent.b, 0.25f + 0.25f * hoverAmount);
                    textColor = NeoInspectorTheme.TitleText;
                }

                NeoInspectorTheme.DrawRoundedRect(rect, bg, edge, rect.height * 0.5f, 1f);
                GUIStyle style = NeoInspectorStyles.VersionPill;
                Color previous = style.normal.textColor;
                style.normal.textColor = textColor;
                Rect textRect = filled ? rect : new Rect(rect.x + 10f, rect.y, rect.width - 12f, rect.height);
                GUI.Label(textRect, NeoInspectorStyles.CachedContent(text), style);
                style.normal.textColor = previous;
            }

            clicked = ClickZone(rect, tooltip);
            return hover;
        }

        /// <summary>
        ///     Tooltip plus a left-click on <paramref name="rect" />, without a control id: header pills and badges
        ///     appear and disappear with state that can change between a Layout and its Repaint, and an id that
        ///     comes and goes would shift every control id after the header.
        /// </summary>
        private static bool ClickZone(Rect rect, string tooltip)
        {
            Event current = Event.current;
            if (current.type == EventType.Repaint && !string.IsNullOrEmpty(tooltip))
            {
                GUI.Label(rect, NeoInspectorStyles.CachedTooltip(tooltip), GUIStyle.none);
            }

            if (current.type == EventType.MouseDown && current.button == 0 && GUI.enabled &&
                rect.Contains(current.mousePosition))
            {
                current.Use();
                return true;
            }

            return false;
        }

        private static string s_taglineModule;
        private static GUIContent s_taglineContent;
        private static string s_compactModule;
        private static string s_compactTitle;
        private static NeoUpdateChecker.UpdateStatus s_tooltipStatus = (NeoUpdateChecker.UpdateStatus)(-1);
        private static string s_tooltipLatest;
        private static string s_tooltipError;
        private static string s_tooltipText;
        private static string s_updateLatest;
        private static GUIContent s_updateContent;

        // WHY: the banner redraws on every event of every Neo inspector; the tagline only changes with the module.
        private static GUIContent GetBannerTagline(string moduleName)
        {
            string module = string.IsNullOrWhiteSpace(moduleName) ? null : moduleName;
            if (s_taglineContent == null || !string.Equals(s_taglineModule, module, StringComparison.Ordinal))
            {
                s_taglineModule = module;
                s_taglineContent = new GUIContent(module == null ? "Modular Unity Toolkit" : "Module · " + module,
                    "Neoxider Tools · Modular Unity Toolkit");
            }

            return s_taglineContent;
        }

        private static string GetCompactTitle(string moduleName)
        {
            string module = string.IsNullOrWhiteSpace(moduleName) ? null : moduleName;
            if (s_compactTitle == null || !string.Equals(s_compactModule, module, StringComparison.Ordinal))
            {
                s_compactModule = module;
                s_compactTitle = module == null ? "Neoxider Tools" : "Neoxider · " + module;
            }

            return s_compactTitle;
        }

        /// <summary>Hover text of the version pill for <paramref name="state" />, rebuilt only when the state changes.</summary>
        internal static string GetVersionTooltip(NeoUpdateChecker.State state)
        {
            if (s_tooltipText != null && state.Status == s_tooltipStatus &&
                string.Equals(state.LatestVersion, s_tooltipLatest, StringComparison.Ordinal) &&
                string.Equals(state.Error, s_tooltipError, StringComparison.Ordinal))
            {
                return s_tooltipText;
            }

            s_tooltipStatus = state.Status;
            s_tooltipLatest = state.LatestVersion;
            s_tooltipError = state.Error;
            switch (state.Status)
            {
                case NeoUpdateChecker.UpdateStatus.UpdateAvailable:
                    s_tooltipText = "Version " + state.LatestVersion + " is published. Click to open the release page.";
                    break;
                case NeoUpdateChecker.UpdateStatus.UpToDate:
                    s_tooltipText = "Up to date. Click to check again.";
                    break;
                case NeoUpdateChecker.UpdateStatus.Ahead:
                    s_tooltipText = "Newer than the latest published release (" + state.LatestVersion +
                                    "). Click to check again.";
                    break;
                case NeoUpdateChecker.UpdateStatus.Checking:
                    s_tooltipText = "Checking for updates…";
                    break;
                default:
                    // WHY: the stored errors come from several sources, some with a closing period and some without.
                    s_tooltipText = string.IsNullOrEmpty(state.Error)
                        ? "Update status unknown. Click to check for updates."
                        : state.Error.TrimEnd(' ', '.') + ". Click to try again.";
                    break;
            }

            return s_tooltipText;
        }

        private static GUIContent GetUpdateButtonContent(string latestVersion)
        {
            if (s_updateContent == null || !string.Equals(s_updateLatest, latestVersion, StringComparison.Ordinal))
            {
                s_updateLatest = latestVersion;
                s_updateContent = new GUIContent("Update", "Open the release page of version " + latestVersion + ".");
            }

            return s_updateContent;
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

        /// <summary>Per-type foldout persistence keys of the fixed sections (Events, Actions, Documentation).</summary>
        private sealed class TypeFoldoutKeys
        {
            public string Events;
            public string Actions;
            public string DocFoldout;
            public string DocScroll;
        }

        // WHY: the keys were rebuilt with string interpolation for every section on every IMGUI event.
        private static readonly Dictionary<Type, TypeFoldoutKeys> s_typeFoldoutKeys = new();

        private static TypeFoldoutKeys GetTypeFoldoutKeys(Type type)
        {
            if (!s_typeFoldoutKeys.TryGetValue(type, out TypeFoldoutKeys keys))
            {
                string fullName = type.FullName;
                keys = new TypeFoldoutKeys
                {
                    Events = $"{fullName}.NeoFoldout.Events",
                    Actions = $"{fullName}.NeoFoldout.Actions",
                    DocFoldout = "NeoDoc_Foldout_" + fullName,
                    DocScroll = "NeoDoc_Scroll_" + fullName
                };
                s_typeFoldoutKeys[type] = keys;
            }

            return keys;
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

        /// <summary>
        ///     Runs after every inspector pass; implementations apply attribute-driven assignments such as
        ///     <see cref="ComponentDrawer.ProcessComponentAttributes" />. Called on every IMGUI event, so keep it cheap.
        /// </summary>
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
