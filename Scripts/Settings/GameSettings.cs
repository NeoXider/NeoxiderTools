using System;
using Neo.Save;
using UnityEngine;

namespace Neo.Settings
{
    /// <summary>
    ///     Static application settings: read-only properties, <c>Set*</c> mutations with
    ///     <see cref="SettingsPersistMode"/>, and <see cref="Neo.Save.SaveProvider"/> persistence.
    ///     Attach <see cref="GameSettingsComponent"/> before relying on save/load and preset tables.
    /// </summary>
    public static class GameSettings
    {
        private const float MinMouseSensitivity = 0.01f;
        private const float MaxMouseSensitivity = 50f;

        private const int SaveKeyCount = 9;
        private const int MouseSensitivityKey = 0;
        private const int GraphicsPresetKey = 1;
        private const int QualityLevelKey = 2;
        private const int FullScreenKey = 3;
        private const int FullScreenModeKey = 4;
        private const int ResolutionAutoKey = 5;
        private const int ResolutionIndexKey = 6;
        private const int FramerateCapKey = 7;
        private const int VSyncKey = 8;

        private static GameSettingsComponent s_context;
        private static ISettingsLocalization s_localization;

        // WHY: Immediate persistence runs on every slider/toggle callback; compose prefixed keys once per prefix.
        // One immutable object keeps prefix and keys consistent when tests snapshot and restore the statics.
        private static SaveKeySet s_saveKeys;

        private static float s_mouseSensitivity = 2f;
        private static GraphicsPreset s_graphicsPreset = GraphicsPreset.High;
        private static int s_qualityLevelIndex;
        private static bool s_fullScreen = true;
        private static FullScreenMode s_fullScreenMode = FullScreenMode.FullScreenWindow;
        private static bool s_resolutionAuto = true;
        private static int s_resolutionIndex;
        private static int s_framerateCap = -1;
        private static bool s_vSync;

        /// <summary>Raised after any applied change from Set* or LoadState.</summary>
        public static event Action OnSettingsChanged;

        /// <summary>Raised once after <see cref="LoadState"/> finishes applying values.</summary>
        public static event Action OnAfterSettingsLoaded;

        /// <summary>Active context from <see cref="GameSettingsComponent"/>; null before Attach.</summary>
        public static GameSettingsComponent Context => s_context;

        /// <summary>Mouse/look sensitivity, clamped to [0.01, 50].</summary>
        public static float MouseSensitivity => s_mouseSensitivity;

        /// <summary>Active named graphics tier; <c>Custom</c> when the quality level matches no tier.</summary>
        public static GraphicsPreset GraphicsPreset => s_graphicsPreset;

        /// <summary>Active <see cref="QualitySettings" /> level index.</summary>
        public static int QualityLevelIndex => s_qualityLevelIndex;

        /// <summary>Whether the window is in any fullscreen mode.</summary>
        public static bool FullScreen => s_fullScreen;

        /// <summary>Applied <see cref="FullScreenMode" />; always <c>Windowed</c> when <see cref="FullScreen" /> is false.</summary>
        public static FullScreenMode FullScreenModeValue => s_fullScreenMode;

        /// <summary>When true the engine resolution is left alone; otherwise <see cref="ResolutionIndex" /> is applied.</summary>
        public static bool ResolutionAuto => s_resolutionAuto;

        /// <summary>Index into the runtime resolution list (see <see cref="TryGetRuntimeResolution" />).</summary>
        public static int ResolutionIndex => s_resolutionIndex;

        /// <summary><see cref="Application.targetFrameRate" /> value; -1 means unlimited.</summary>
        public static int FramerateCap => s_framerateCap;

        /// <summary>Whether VSync (<c>vSyncCount = 1</c>) is on.</summary>
        public static bool VSync => s_vSync;

        /// <summary>Optional UI strings provider; keys are plain when null.</summary>
        public static void SetLocalizationProvider(ISettingsLocalization localization)
        {
            s_localization = localization;
        }

        /// <summary>Returns localized label or the key back verbatim.</summary>
        public static string Localize(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            return s_localization != null ? s_localization.Get(key) : key;
        }

        /// <summary>Binds SaveProvider prefix and preset tables to this component. Call from service Awake.</summary>
        public static void Attach(GameSettingsComponent component)
        {
            if (component == null)
            {
                NeoDiagnostics.LogError("[GameSettings] Attach called with null.");
                return;
            }

            if (s_context != null && s_context != component)
            {
                NeoDiagnostics.LogWarning("[GameSettings] Replacing existing GameSettingsComponent context.",
                    component);
            }

            s_context = component;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            s_context = null;
            s_localization = null;
            s_saveKeys = null;
            s_mouseSensitivity = 2f;
            s_graphicsPreset = GraphicsPreset.High;
            s_qualityLevelIndex = 0;
            s_fullScreen = true;
            s_fullScreenMode = FullScreenMode.FullScreenWindow;
            s_resolutionAuto = true;
            s_resolutionIndex = 0;
            s_framerateCap = -1;
            s_vSync = false;
            // WHY: Prevent listener leaks when Domain Reload is disabled
            OnSettingsChanged = null;
            OnAfterSettingsLoaded = null;
        }

        /// <summary>Alias for tests that need to reset state explicitly.</summary>
        internal static void ResetStaticStateForTesting()
        {
            ResetStaticState();
        }

        /// <summary>Releases context when the service is destroyed.</summary>
        public static void Detach(GameSettingsComponent component)
        {
            if (component == null)
            {
                return;
            }

            if (s_context == component)
            {
                s_context = null;
            }
        }

        /// <summary>Loads values from SaveProvider and applies them to the engine.</summary>
        public static void LoadState()
        {
            if (s_context == null)
            {
                NeoDiagnostics.LogWarning("[GameSettings] LoadState: no GameSettingsComponent attached.");
                return;
            }

            string[] keys = GetSaveKeys(s_context.SaveKeyPrefix);

            s_mouseSensitivity = ClampMouseSensitivity(
                SaveProvider.GetFloat(keys[MouseSensitivityKey], s_context.DefaultMouseSensitivity),
                s_context.DefaultMouseSensitivity);

            int presetInt = SaveProvider.GetInt(keys[GraphicsPresetKey], (int)s_context.DefaultGraphicsPreset);
            s_graphicsPreset = IsKnownPreset((GraphicsPreset)presetInt)
                ? (GraphicsPreset)presetInt
                : GetDefaultGraphicsPreset();

            int savedQuality = SaveProvider.GetInt(keys[QualityLevelKey], s_context.DefaultCustomQualityLevel);
            savedQuality = ClampQuality(savedQuality);
            if (s_graphicsPreset == GraphicsPreset.Custom)
            {
                s_qualityLevelIndex = savedQuality;
            }
            else
            {
                s_qualityLevelIndex = ClampQuality(s_context.GetQualityLevelForPreset(s_graphicsPreset));
            }

            s_fullScreen = SaveProvider.GetBool(keys[FullScreenKey], s_context.DefaultFullScreen);
            s_fullScreenMode =
                (FullScreenMode)SaveProvider.GetInt(keys[FullScreenModeKey], (int)s_context.DefaultFullScreenMode);
            if (!IsKnownFullScreenMode(s_fullScreenMode))
            {
                s_fullScreenMode = s_context.DefaultFullScreenMode;
            }

            s_fullScreenMode = ResolveFullScreenMode(s_fullScreenMode, s_fullScreen);
            s_resolutionAuto = SaveProvider.GetBool(keys[ResolutionAutoKey], s_context.DefaultResolutionAuto);
            s_resolutionIndex = SaveProvider.GetInt(keys[ResolutionIndexKey], s_context.DefaultResolutionIndex);

            s_framerateCap = Mathf.Max(-1, SaveProvider.GetInt(keys[FramerateCapKey], s_context.DefaultFramerateCap));
            s_vSync = SaveProvider.GetBool(keys[VSyncKey], s_context.DefaultVSync);

            s_context.RebuildResolutionList();
            s_resolutionIndex = ClampResolutionIndex(s_resolutionIndex);

            ApplyAllEngine();
            OnAfterSettingsLoaded?.Invoke();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>Writes current values to SaveProvider when persist flags allow.</summary>
        public static void SaveState()
        {
            if (s_context == null)
            {
                return;
            }

            string[] keys = GetSaveKeys(s_context.SaveKeyPrefix);

            if (s_context.PersistInput)
            {
                SaveProvider.SetFloat(keys[MouseSensitivityKey], s_mouseSensitivity);
            }

            if (s_context.PersistGraphics)
            {
                SaveProvider.SetInt(keys[GraphicsPresetKey], (int)s_graphicsPreset);
                SaveProvider.SetInt(keys[QualityLevelKey], s_qualityLevelIndex);
            }

            if (s_context.PersistDisplay)
            {
                SaveProvider.SetBool(keys[FullScreenKey], s_fullScreen);
                SaveProvider.SetInt(keys[FullScreenModeKey], (int)s_fullScreenMode);
                SaveProvider.SetBool(keys[ResolutionAutoKey], s_resolutionAuto);
                SaveProvider.SetInt(keys[ResolutionIndexKey], s_resolutionIndex);
            }

            if (s_context.PersistPerformance)
            {
                SaveProvider.SetInt(keys[FramerateCapKey], s_framerateCap);
                SaveProvider.SetBool(keys[VSyncKey], s_vSync);
            }
        }

        /// <summary>Stops debounce coroutine and saves immediately.</summary>
        public static void FlushPendingSettingsSave()
        {
            if (s_context != null)
            {
                s_context.FlushDeferredSaveCoroutine();
            }

            SaveState();
        }

        /// <summary>Sets mouse sensitivity (NaN keeps the current value; clamped to [0.01, 50]).</summary>
        public static void SetMouseSensitivity(float value, SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            s_mouseSensitivity = ClampMouseSensitivity(value, s_mouseSensitivity);
            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        /// <summary>Applies a named tier through the preset table; <c>Custom</c> keeps the current quality level.</summary>
        public static void SetGraphicsPreset(GraphicsPreset preset,
            SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            if (!IsKnownPreset(preset))
            {
                NeoDiagnostics.LogWarning("[GameSettings] Invalid graphics preset; change ignored.");
                return;
            }

            if (s_context == null)
            {
                NeoDiagnostics.LogWarning("[GameSettings] SetGraphicsPreset: no context.");
                return;
            }

            if (preset == GraphicsPreset.Custom)
            {
                s_graphicsPreset = GraphicsPreset.Custom;
            }
            else
            {
                s_graphicsPreset = preset;
                s_qualityLevelIndex = ClampQuality(s_context.GetQualityLevelForPreset(preset));
                ApplyQualityLevel(s_qualityLevelIndex);
                QualitySettings.vSyncCount = s_vSync ? 1 : 0;
            }

            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        /// <summary>Applies a quality level (clamped) and derives the matching preset or <c>Custom</c>.</summary>
        public static void SetQualityLevel(int index, SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            if (s_context == null)
            {
                NeoDiagnostics.LogWarning("[GameSettings] SetQualityLevel: no context.");
                return;
            }

            s_qualityLevelIndex = ClampQuality(index);
            ApplyQualityLevel(s_qualityLevelIndex);
            QualitySettings.vSyncCount = s_vSync ? 1 : 0;
            s_graphicsPreset = s_context.GetPresetForQualityLevel(s_qualityLevelIndex);
            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        /// <summary>Toggles fullscreen; turning it on restores a fullscreen mode, off selects <c>Windowed</c>.</summary>
        public static void SetFullScreen(bool fullScreen, SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            s_fullScreen = fullScreen;
            s_fullScreenMode = ResolveFullScreenMode(s_fullScreenMode, fullScreen);
            Screen.fullScreenMode = s_fullScreenMode;
            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        /// <summary>Applies an explicit <see cref="FullScreenMode" /> and updates <see cref="FullScreen" />.</summary>
        public static void SetFullScreenMode(FullScreenMode fullScreenMode,
            SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            if (!IsKnownFullScreenMode(fullScreenMode))
            {
                NeoDiagnostics.LogWarning("[GameSettings] Invalid fullscreen mode; change ignored.");
                return;
            }

            s_fullScreenMode = fullScreenMode;
            Screen.fullScreenMode = fullScreenMode;
            s_fullScreen = fullScreenMode != FullScreenMode.Windowed;
            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        /// <summary>Switches automatic resolution; turning it off applies <see cref="ResolutionIndex" />.</summary>
        public static void SetResolutionAuto(bool auto, SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            s_resolutionAuto = auto;
            if (!auto)
            {
                ApplyCurrentResolutionFromIndex();
            }

            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        /// <summary>Selects and applies a runtime resolution (clamped) and turns automatic resolution off.</summary>
        public static void SetResolutionIndex(int index, SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            s_resolutionAuto = false;
            s_resolutionIndex = ClampResolutionIndex(index);
            ApplyCurrentResolutionFromIndex();
            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        /// <summary>Sets <see cref="Application.targetFrameRate" />; values below -1 become -1 (unlimited).</summary>
        public static void SetFramerateCap(int targetFrameRate,
            SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            s_framerateCap = targetFrameRate < -1 ? -1 : targetFrameRate;
            Application.targetFrameRate = s_framerateCap;
            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        /// <summary>Turns VSync on or off.</summary>
        public static void SetVSync(bool enabled, SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            s_vSync = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        /// <summary>Restores one group to the component defaults, applies the engine state and saves immediately.</summary>
        public static void ResetGroup(SettingsGroup group)
        {
            if (s_context == null)
            {
                return;
            }

            switch (group)
            {
                case SettingsGroup.Input:
                    s_mouseSensitivity = ClampMouseSensitivity(s_context.DefaultMouseSensitivity, 2f);
                    break;
                case SettingsGroup.Graphics:
                    s_graphicsPreset = GetDefaultGraphicsPreset();
                    s_qualityLevelIndex = ClampQuality(s_context.DefaultCustomQualityLevel);
                    if (s_graphicsPreset != GraphicsPreset.Custom)
                    {
                        s_qualityLevelIndex = ClampQuality(s_context.GetQualityLevelForPreset(s_graphicsPreset));
                    }

                    // WHY: an explicit reset re-applies the level even when active, restoring runtime tweaks.
                    QualitySettings.SetQualityLevel(s_qualityLevelIndex, true);
                    break;
                case SettingsGroup.Display:
                    s_fullScreen = s_context.DefaultFullScreen;
                    s_fullScreenMode = ResolveFullScreenMode(s_context.DefaultFullScreenMode, s_fullScreen);
                    s_resolutionAuto = s_context.DefaultResolutionAuto;
                    s_resolutionIndex =
                        ClampResolutionIndex(s_context.DefaultResolutionIndex);
                    break;
                case SettingsGroup.Performance:
                    s_framerateCap = Mathf.Max(-1, s_context.DefaultFramerateCap);
                    s_vSync = s_context.DefaultVSync;
                    Application.targetFrameRate = s_framerateCap;
                    QualitySettings.vSyncCount = s_vSync ? 1 : 0;
                    break;
            }

            ApplyAllEngine();
            OnSettingsChanged?.Invoke();
            SaveState();
        }

        /// <summary>Number of entries in the runtime resolution list; 0 without a context.</summary>
        public static int GetRuntimeResolutionCount()
        {
            return s_context != null ? s_context.RuntimeResolutions.Count : 0;
        }

        /// <summary>Reads one runtime resolution entry.</summary>
        public static bool TryGetRuntimeResolution(int index, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (s_context == null || index < 0 || index >= s_context.RuntimeResolutions.Count)
            {
                return false;
            }

            ResolutionPresetEntry e = s_context.RuntimeResolutions[index];
            width = e.Width;
            height = e.Height;
            return true;
        }

        /// <summary>Finds a matching preset index for the current screen size, or 0.</summary>
        public static int FindResolutionIndexForCurrentScreen()
        {
            if (s_context == null)
            {
                return 0;
            }

            int w = Screen.width;
            int h = Screen.height;
            for (int i = 0; i < s_context.RuntimeResolutions.Count; i++)
            {
                ResolutionPresetEntry e = s_context.RuntimeResolutions[i];
                if (e.Width == w && e.Height == h)
                {
                    return i;
                }
            }

            return 0;
        }

        private static string[] GetSaveKeys(string prefix)
        {
            prefix ??= string.Empty;
            SaveKeySet cached = s_saveKeys;
            if (cached != null && string.Equals(cached.Prefix, prefix, StringComparison.Ordinal))
            {
                return cached.Keys;
            }

            cached = new SaveKeySet(prefix);
            s_saveKeys = cached;
            return cached.Keys;
        }

        /// <summary>
        ///     Switches the quality level only when it differs: <c>SetQualityLevel(index, true)</c> applies expensive
        ///     changes (anti-aliasing, render targets) even for the level that is already active.
        /// </summary>
        private static void ApplyQualityLevel(int index)
        {
            if (QualitySettings.GetQualityLevel() != index)
            {
                QualitySettings.SetQualityLevel(index, true);
            }
        }

        private sealed class SaveKeySet
        {
            public readonly string Prefix;
            public readonly string[] Keys = new string[SaveKeyCount];

            public SaveKeySet(string prefix)
            {
                Prefix = prefix;
                Keys[MouseSensitivityKey] = prefix + GameSettingsSaveKeys.MouseSensitivity;
                Keys[GraphicsPresetKey] = prefix + GameSettingsSaveKeys.GraphicsPreset;
                Keys[QualityLevelKey] = prefix + GameSettingsSaveKeys.QualityLevel;
                Keys[FullScreenKey] = prefix + GameSettingsSaveKeys.FullScreen;
                Keys[FullScreenModeKey] = prefix + GameSettingsSaveKeys.FullScreenMode;
                Keys[ResolutionAutoKey] = prefix + GameSettingsSaveKeys.ResolutionAuto;
                Keys[ResolutionIndexKey] = prefix + GameSettingsSaveKeys.ResolutionIndex;
                Keys[FramerateCapKey] = prefix + GameSettingsSaveKeys.FramerateCap;
                Keys[VSyncKey] = prefix + GameSettingsSaveKeys.VSync;
            }
        }

        private static float ClampMouseSensitivity(float value, float fallback)
        {
            if (float.IsNaN(value))
            {
                value = float.IsNaN(fallback) ? 2f : fallback;
            }

            return Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity);
        }

        private static GraphicsPreset GetDefaultGraphicsPreset()
        {
            return IsKnownPreset(s_context.DefaultGraphicsPreset)
                ? s_context.DefaultGraphicsPreset
                : GraphicsPreset.High;
        }

        // WHY: Enum.IsDefined(Type, object) boxes on every call; these checks run on per-change setters.
        private static bool IsKnownPreset(GraphicsPreset preset)
        {
            switch (preset)
            {
                case GraphicsPreset.Minimal:
                case GraphicsPreset.Low:
                case GraphicsPreset.Medium:
                case GraphicsPreset.High:
                case GraphicsPreset.Maximum:
                case GraphicsPreset.Custom:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsKnownFullScreenMode(FullScreenMode mode)
        {
            switch (mode)
            {
                case FullScreenMode.ExclusiveFullScreen:
                case FullScreenMode.FullScreenWindow:
                case FullScreenMode.MaximizedWindow:
                case FullScreenMode.Windowed:
                    return true;
                default:
                    return false;
            }
        }

        private static FullScreenMode ResolveFullScreenMode(FullScreenMode mode, bool fullScreen)
        {
            if (!fullScreen)
            {
                return FullScreenMode.Windowed;
            }

            return mode == FullScreenMode.Windowed || !IsKnownFullScreenMode(mode)
                ? FullScreenMode.FullScreenWindow
                : mode;
        }

        private static void TryPersist(SettingsPersistMode mode)
        {
            if (s_context == null)
            {
                return;
            }

            switch (mode)
            {
                case SettingsPersistMode.Immediate:
                    SaveState();
                    break;
                case SettingsPersistMode.Deferred:
                    s_context.ScheduleDeferredSave();
                    break;
                case SettingsPersistMode.SkipUntilFlush:
                    break;
            }
        }

        private static void ApplyAllEngine()
        {
            if (QualitySettings.count > 0)
            {
                ApplyQualityLevel(s_qualityLevelIndex);
            }

            Screen.fullScreenMode = s_fullScreenMode;
            Screen.fullScreen = s_fullScreen;
            if (!s_resolutionAuto)
            {
                ApplyCurrentResolutionFromIndex();
            }

            QualitySettings.vSyncCount = s_vSync ? 1 : 0;
            Application.targetFrameRate = s_framerateCap;
        }

        private static void ApplyCurrentResolutionFromIndex()
        {
            if (s_context == null || s_resolutionAuto)
            {
                return;
            }

            if (!TryGetRuntimeResolution(s_resolutionIndex, out int w, out int h))
            {
                NeoDiagnostics.LogWarning("[GameSettings] Invalid resolution index; skipping SetResolution.");
                return;
            }

            Screen.SetResolution(w, h, s_fullScreenMode);
        }

        private static int ClampQuality(int index)
        {
            int max = QualitySettings.count - 1;
            if (max < 0)
            {
                NeoDiagnostics.LogWarning("[GameSettings] No quality levels in project.");
                return 0;
            }

            if (index < 0 || index > max)
            {
                NeoDiagnostics.LogWarning($"[GameSettings] Quality level {index} clamped to [0,{max}].");
                return Mathf.Clamp(index, 0, max);
            }

            return index;
        }

        private static int ClampResolutionIndex(int index)
        {
            if (s_context == null || s_context.RuntimeResolutions.Count == 0)
            {
                return 0;
            }

            int max = s_context.RuntimeResolutions.Count - 1;
            if (index < 0 || index > max)
            {
                NeoDiagnostics.LogWarning($"[GameSettings] Resolution index {index} clamped.");
                return Mathf.Clamp(index, 0, max);
            }

            return index;
        }
    }
}
