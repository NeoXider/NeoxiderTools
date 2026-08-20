using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Auto-save settings of the Scene Saver, persisted in <see cref="EditorPrefs" />.
    /// </summary>
    /// <remarks>
    ///     Until 10.12.0 the settings lived only inside the window's GUI instance, and the background
    ///     checker held a second instance of its own. Turning auto-save off in the window therefore did
    ///     not stop the background saver and was forgotten on the next domain reload, so the feature
    ///     could not be switched off durably at all. Every setter writes through to
    ///     <see cref="EditorPrefs" /> immediately; keys carry a package prefix so a game project using
    ///     the same names cannot collide with them.
    /// </remarks>
    public sealed class SceneSaverSettings
    {
        /// <summary><see cref="EditorPrefs" /> key backing <see cref="IsEnabled" />.</summary>
        public const string EnabledKey = "Neoxider.SceneSaver.Enabled";

        /// <summary><see cref="EditorPrefs" /> key backing <see cref="IntervalMinutes" />.</summary>
        public const string IntervalMinutesKey = "Neoxider.SceneSaver.IntervalMinutes";

        /// <summary><see cref="EditorPrefs" /> key backing <see cref="SaveEvenIfNotDirty" />.</summary>
        public const string SaveEvenIfNotDirtyKey = "Neoxider.SceneSaver.SaveEvenIfNotDirty";

        /// <summary>Auto-save is on out of the box; the user can switch it off permanently.</summary>
        public const bool DefaultEnabled = true;

        /// <summary>Default interval between two backup copies, in minutes.</summary>
        public const float DefaultIntervalMinutes = 3f;

        /// <summary>
        ///     Smallest accepted interval, in minutes. A zero or negative value typed into the window
        ///     would mean "save on every editor tick", which is a guaranteed freeze.
        /// </summary>
        public const float MinIntervalMinutes = 0.25f;

        private static SceneSaverSettings _shared;

        private bool _isEnabled;
        private float _intervalMinutes;
        private bool _saveEvenIfNotDirty;

        /// <summary>Creates an instance and loads the persisted values.</summary>
        public SceneSaverSettings()
        {
            Reload();
        }

        /// <summary>
        ///     Instance shared by the background checker and every Scene Saver window, so a toggle in the
        ///     window takes effect immediately instead of only inside the window's own copy.
        /// </summary>
        public static SceneSaverSettings Shared
        {
            get
            {
                if (_shared == null)
                {
                    _shared = new SceneSaverSettings();
                }

                return _shared;
            }
        }

        /// <summary>Whether the background auto-save is enabled. Persisted.</summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                {
                    return;
                }

                _isEnabled = value;
                EditorPrefs.SetBool(EnabledKey, value);
            }
        }

        /// <summary>
        ///     Interval between two backup copies, in minutes. Clamped to
        ///     <see cref="MinIntervalMinutes" />. Persisted.
        /// </summary>
        public float IntervalMinutes
        {
            get => _intervalMinutes;
            set
            {
                float clamped = Mathf.Max(MinIntervalMinutes, value);
                if (Mathf.Approximately(_intervalMinutes, clamped))
                {
                    return;
                }

                _intervalMinutes = clamped;
                EditorPrefs.SetFloat(IntervalMinutesKey, clamped);
            }
        }

        /// <summary>Whether a backup is written even when the scene has no unsaved changes. Persisted.</summary>
        public bool SaveEvenIfNotDirty
        {
            get => _saveEvenIfNotDirty;
            set
            {
                if (_saveEvenIfNotDirty == value)
                {
                    return;
                }

                _saveEvenIfNotDirty = value;
                EditorPrefs.SetBool(SaveEvenIfNotDirtyKey, value);
            }
        }

        /// <summary>Re-reads every value from <see cref="EditorPrefs" />, discarding unsaved edits.</summary>
        public void Reload()
        {
            _isEnabled = EditorPrefs.GetBool(EnabledKey, DefaultEnabled);
            _intervalMinutes = Mathf.Max(MinIntervalMinutes,
                EditorPrefs.GetFloat(IntervalMinutesKey, DefaultIntervalMinutes));
            _saveEvenIfNotDirty = EditorPrefs.GetBool(SaveEvenIfNotDirtyKey, false);
        }

        /// <summary>
        ///     Removes the persisted keys and restores the defaults. Used by the window's reset action
        ///     and by tests that must not leave the user's preferences changed.
        /// </summary>
        public void ResetToDefaults()
        {
            EditorPrefs.DeleteKey(EnabledKey);
            EditorPrefs.DeleteKey(IntervalMinutesKey);
            EditorPrefs.DeleteKey(SaveEvenIfNotDirtyKey);
            Reload();
        }
    }
}
