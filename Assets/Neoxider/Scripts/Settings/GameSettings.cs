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

        private static GameSettingsComponent s_context;
        private static ISettingsLocalization s_localization;

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

        public static float MouseSensitivity => s_mouseSensitivity;

        public static GraphicsPreset GraphicsPreset => s_graphicsPreset;

        public static int QualityLevelIndex => s_qualityLevelIndex;

        public static bool FullScreen => s_fullScreen;

        public static FullScreenMode FullScreenModeValue => s_fullScreenMode;

        public static bool ResolutionAuto => s_resolutionAuto;

        public static int ResolutionIndex => s_resolutionIndex;

        public static int FramerateCap => s_framerateCap;

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
                Debug.LogError("[GameSettings] Attach called with null.");
                return;
            }

            if (s_context != null && s_context != component)
            {
                Debug.LogWarning("[GameSettings] Replacing existing GameSettingsComponent context.", component);
            }

            s_context = component;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            s_context = null;
            s_localization = null;
            s_mouseSensitivity = 2f;
            s_graphicsPreset = GraphicsPreset.High;
            s_qualityLevelIndex = 0;
            s_fullScreen = true;
            s_fullScreenMode = FullScreenMode.FullScreenWindow;
            s_resolutionAuto = true;
            s_resolutionIndex = 0;
            s_framerateCap = -1;
            s_vSync = false;
            // Prevent listener leaks when Domain Reload is disabled
            OnSettingsChanged = null;
            OnAfterSettingsLoaded = null;
        }

        /// <summary>Alias for tests that need to reset state explicitly.</summary>
        internal static void ResetStaticStateForTesting() => ResetStaticState();

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
                Debug.LogWarning("[GameSettings] LoadState: no GameSettingsComponent attached.");
                return;
            }

            string p = s_context.SaveKeyPrefix;

            s_mouseSensitivity = Mathf.Clamp(
                SaveProvider.GetFloat(Key(p, GameSettingsSaveKeys.MouseSensitivity), s_context.DefaultMouseSensitivity),
                MinMouseSensitivity,
                MaxMouseSensitivity);

            int presetInt = SaveProvider.GetInt(Key(p, GameSettingsSaveKeys.GraphicsPreset),
                (int)s_context.DefaultGraphicsPreset);
            s_graphicsPreset = Enum.IsDefined(typeof(GraphicsPreset), presetInt)
                ? (GraphicsPreset)presetInt
                : s_context.DefaultGraphicsPreset;

            int savedQuality = SaveProvider.GetInt(Key(p, GameSettingsSaveKeys.QualityLevel),
                s_context.DefaultCustomQualityLevel);
            savedQuality = ClampQuality(savedQuality);
            if (s_graphicsPreset == GraphicsPreset.Custom)
            {
                s_qualityLevelIndex = savedQuality;
            }
            else
            {
                s_qualityLevelIndex = ClampQuality(s_context.GetQualityLevelForPreset(s_graphicsPreset));
            }

            s_fullScreen = SaveProvider.GetBool(Key(p, GameSettingsSaveKeys.FullScreen), s_context.DefaultFullScreen);
            s_fullScreenMode =
                (FullScreenMode)SaveProvider.GetInt(Key(p, GameSettingsSaveKeys.FullScreenMode),
                    (int)s_context.DefaultFullScreenMode);
            s_resolutionAuto =
                SaveProvider.GetBool(Key(p, GameSettingsSaveKeys.ResolutionAuto), s_context.DefaultResolutionAuto);
            s_resolutionIndex =
                SaveProvider.GetInt(Key(p, GameSettingsSaveKeys.ResolutionIndex), s_context.DefaultResolutionIndex);

            s_framerateCap =
                SaveProvider.GetInt(Key(p, GameSettingsSaveKeys.FramerateCap), s_context.DefaultFramerateCap);
            s_vSync = SaveProvider.GetBool(Key(p, GameSettingsSaveKeys.VSync), s_context.DefaultVSync);

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

            string p = s_context.SaveKeyPrefix;

            if (s_context.PersistInput)
            {
                SaveProvider.SetFloat(Key(p, GameSettingsSaveKeys.MouseSensitivity), s_mouseSensitivity);
            }

            if (s_context.PersistGraphics)
            {
                SaveProvider.SetInt(Key(p, GameSettingsSaveKeys.GraphicsPreset), (int)s_graphicsPreset);
                SaveProvider.SetInt(Key(p, GameSettingsSaveKeys.QualityLevel), s_qualityLevelIndex);
            }

            if (s_context.PersistDisplay)
            {
                SaveProvider.SetBool(Key(p, GameSettingsSaveKeys.FullScreen), s_fullScreen);
                SaveProvider.SetInt(Key(p, GameSettingsSaveKeys.FullScreenMode), (int)s_fullScreenMode);
                SaveProvider.SetBool(Key(p, GameSettingsSaveKeys.ResolutionAuto), s_resolutionAuto);
                SaveProvider.SetInt(Key(p, GameSettingsSaveKeys.ResolutionIndex), s_resolutionIndex);
            }

            if (s_context.PersistPerformance)
            {
                SaveProvider.SetInt(Key(p, GameSettingsSaveKeys.FramerateCap), s_framerateCap);
                SaveProvider.SetBool(Key(p, GameSettingsSaveKeys.VSync), s_vSync);
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

        public static void SetMouseSensitivity(float value, SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            s_mouseSensitivity = Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity);
            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        public static void SetGraphicsPreset(GraphicsPreset preset,
            SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            if (s_context == null)
            {
                Debug.LogWarning("[GameSettings] SetGraphicsPreset: no context.");
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
                QualitySettings.SetQualityLevel(s_qualityLevelIndex, true);
            }

            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        public static void SetQualityLevel(int index, SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            if (s_context == null)
            {
                Debug.LogWarning("[GameSettings] SetQualityLevel: no context.");
                return;
            }

            s_qualityLevelIndex = ClampQuality(index);
            QualitySettings.SetQualityLevel(s_qualityLevelIndex, true);
            s_graphicsPreset = s_context.GetPresetForQualityLevel(s_qualityLevelIndex);
            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        public static void SetFullScreen(bool fullScreen, SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            s_fullScreen = fullScreen;
            Screen.fullScreen = fullScreen;
            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        public static void SetFullScreenMode(FullScreenMode fullScreenMode,
            SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            s_fullScreenMode = fullScreenMode;
            Screen.fullScreenMode = fullScreenMode;
            s_fullScreen = fullScreenMode != FullScreenMode.Windowed;
            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

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

        public static void SetResolutionIndex(int index, SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            s_resolutionAuto = false;
            s_resolutionIndex = ClampResolutionIndex(index);
            ApplyCurrentResolutionFromIndex();
            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        public static void SetFramerateCap(int targetFrameRate,
            SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            s_framerateCap = targetFrameRate < -1 ? -1 : targetFrameRate;
            Application.targetFrameRate = s_framerateCap;
            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        public static void SetVSync(bool enabled, SettingsPersistMode mode = SettingsPersistMode.Immediate)
        {
            s_vSync = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            OnSettingsChanged?.Invoke();
            TryPersist(mode);
        }

        public static void ResetGroup(SettingsGroup group)
        {
            if (s_context == null)
            {
                return;
            }

            switch (group)
            {
                case SettingsGroup.Input:
                    s_mouseSensitivity = s_context.DefaultMouseSensitivity;
                    break;
                case SettingsGroup.Graphics:
                    s_graphicsPreset = s_context.DefaultGraphicsPreset;
                    s_qualityLevelIndex = ClampQuality(s_context.DefaultCustomQualityLevel);
                    if (s_graphicsPreset != GraphicsPreset.Custom)
                    {
                        s_qualityLevelIndex = ClampQuality(s_context.GetQualityLevelForPreset(s_graphicsPreset));
                    }

                    QualitySettings.SetQualityLevel(s_qualityLevelIndex, true);
                    break;
                case SettingsGroup.Display:
                    s_fullScreen = s_context.DefaultFullScreen;
                    s_fullScreenMode = s_context.DefaultFullScreenMode;
                    s_resolutionAuto = s_context.DefaultResolutionAuto;
                    s_resolutionIndex =
                        ClampResolutionIndex(s_context.DefaultResolutionIndex);
                    break;
                case SettingsGroup.Performance:
                    s_framerateCap = s_context.DefaultFramerateCap;
                    s_vSync = s_context.DefaultVSync;
                    Application.targetFrameRate = s_framerateCap;
                    QualitySettings.vSyncCount = s_vSync ? 1 : 0;
                    break;
            }

            ApplyAllEngine();
            OnSettingsChanged?.Invoke();
            SaveState();
        }

        public static int GetRuntimeResolutionCount()
        {
            return s_context != null ? s_context.RuntimeResolutions.Count : 0;
        }

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

        private static string Key(string prefix, string suffix)
        {
            return prefix + suffix;
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
            if (QualitySettings.names.Length > 0)
            {
                QualitySettings.SetQualityLevel(s_qualityLevelIndex, true);
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
                Debug.LogWarning("[GameSettings] Invalid resolution index; skipping SetResolution.");
                return;
            }

            Screen.SetResolution(w, h, s_fullScreenMode);
        }

        private static int ClampQuality(int index)
        {
            int max = QualitySettings.names.Length - 1;
            if (max < 0)
            {
                Debug.LogWarning("[GameSettings] No quality levels in project.");
                return 0;
            }

            if (index < 0 || index > max)
            {
                Debug.LogWarning($"[GameSettings] Quality level {index} clamped to [0,{max}].");
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
                Debug.LogWarning($"[GameSettings] Resolution index {index} clamped.");
                return Mathf.Clamp(index, 0, max);
            }

            return index;
        }
    }
}
