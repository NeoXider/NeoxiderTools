using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Editor.Windows
{
    /// <summary>
    ///     GUI for the Scene Saver window.
    /// </summary>
    public class SceneSaverGUI : EditorWindowGUI
    {
        /// <summary><see cref="EditorPrefs" /> key backing <see cref="LogEachSave" />.</summary>
        internal const string LogEachSaveKey = "Neoxider.SceneSaver.LogEachSave";

        private static readonly GUIContent LogEachSaveContent = new("Log Each Auto-Save",
            "Write a console line for every backup copy. Failed saves are always reported.");

        private static bool s_logEachSaveLoaded;
        private static bool s_logEachSave;

        private readonly SceneSaverSettings _settings;
        private string _currentScenePath;
        private string _lastSaveStatus = "";

        private NeoxiderEditorGUI.Badge[] _summaryBadges;
        private bool _badgeEnabled;
        private float _badgeInterval = -1f;
        private bool _badgeDirty;

        /// <summary>Creates a GUI bound to the shared, persisted settings.</summary>
        public SceneSaverGUI() : this(SceneSaverSettings.Shared)
        {
        }

        /// <summary>Creates a GUI bound to the given settings instance.</summary>
        /// <param name="settings">Settings to read and edit; <c>null</c> falls back to the shared instance.</param>
        public SceneSaverGUI(SceneSaverSettings settings)
        {
            _settings = settings ?? SceneSaverSettings.Shared;
        }

        /// <summary>
        ///     Whether the saver script is enabled.
        /// </summary>
        public bool IsScriptEnabled => _settings.IsEnabled;

        /// <summary>
        ///     Auto-save interval in minutes.
        /// </summary>
        public float IntervalMinutes => _settings.IntervalMinutes;

        /// <summary>
        ///     Whether to save even when the scene is not dirty.
        /// </summary>
        public bool SaveEvenIfNotDirty => _settings.SaveEvenIfNotDirty;

        /// <summary>
        ///     Whether every successful backup writes a console line. Off by default, so the background saver does
        ///     not add a log entry every few minutes; failed saves are reported regardless. Persisted in
        ///     <see cref="EditorPrefs" />.
        /// </summary>
        /// <remarks>
        ///     Internal on purpose: the persisted Scene Saver settings belong in <see cref="SceneSaverSettings" />,
        ///     and keeping this off the public surface lets it move there without an obsolete forwarder.
        /// </remarks>
        internal static bool LogEachSave
        {
            get
            {
                if (!s_logEachSaveLoaded)
                {
                    s_logEachSave = EditorPrefs.GetBool(LogEachSaveKey, false);
                    s_logEachSaveLoaded = true;
                }

                return s_logEachSave;
            }
            set
            {
                if (s_logEachSaveLoaded && s_logEachSave == value)
                {
                    return;
                }

                s_logEachSave = value;
                s_logEachSaveLoaded = true;
                EditorPrefs.SetBool(LogEachSaveKey, value);
            }
        }

        /// <summary>Removes the persisted <see cref="LogEachSave" /> key, restoring the default (off).</summary>
        internal static void ResetLogEachSave()
        {
            EditorPrefs.DeleteKey(LogEachSaveKey);
            s_logEachSave = false;
            s_logEachSaveLoaded = true;
        }

        /// <summary>
        ///     Refreshes the active scene path.
        /// </summary>
        public void UpdateCurrentScenePath()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            _currentScenePath = activeScene.path;

            if (string.IsNullOrEmpty(_currentScenePath))
            {
                _currentScenePath = "Untitled";
            }
        }

        /// <summary>
        ///     Draws the window GUI.
        /// </summary>
        public override void OnGUI(EditorWindow window)
        {
            UpdateCurrentScenePath();
            Scene activeScene = EditorSceneManager.GetActiveScene();
            bool hasSavedScene = !string.IsNullOrEmpty(activeScene.path);

            NeoxiderEditorGUI.DrawSummaryCard("Scene Saver",
                "Automatically saves a backup copy of the active scene to `Assets/Scenes/AutoSaves`.",
                GetSummaryBadges(IsScriptEnabled, IntervalMinutes, activeScene.isDirty));

            if (!hasSavedScene)
            {
                EditorGUILayout.HelpBox(
                    "The active scene is not saved as an asset yet. Auto-save will work after the first normal Save Scene.",
                    MessageType.Info);
            }

            NeoxiderEditorGUI.BeginSection("Auto Save",
                "Basic automatic save options. Stored in EditorPrefs, so they survive an editor restart.");
            _settings.IsEnabled = EditorGUILayout.Toggle("Enable Scene Saver Script", IsScriptEnabled);
            // WHY: delayed, because a plain FloatField commits on every keystroke and the minimum clamp
            // would rewrite "0.5" to "0.25" while it is still being typed.
            _settings.IntervalMinutes = EditorGUILayout.DelayedFloatField("Interval (minutes)", IntervalMinutes);
            _settings.SaveEvenIfNotDirty = EditorGUILayout.Toggle("Save Even If Not Dirty", SaveEvenIfNotDirty);
            LogEachSave = EditorGUILayout.Toggle(LogEachSaveContent, LogEachSave);

            if (GUILayout.Button("Reset Settings"))
            {
                _settings.ResetToDefaults();
                ResetLogEachSave();
            }

            NeoxiderEditorGUI.EndSection();

            NeoInspectorStyles.Space(4f);

            NeoxiderEditorGUI.BeginSection("Current Scene",
                "State of the open scene and the last save result.");
            EditorGUILayout.LabelField("Current Scene", _currentScenePath);
            EditorGUILayout.LabelField("Last Save Status",
                string.IsNullOrEmpty(_lastSaveStatus) ? "No saves yet." : _lastSaveStatus);
            NeoxiderEditorGUI.EndSection();

            NeoInspectorStyles.Space(4f);

            EditorGUI.BeginDisabledGroup(!hasSavedScene && !activeScene.isDirty);
            if (GUILayout.Button("Save Now", NeoInspectorStyles.Height(24f)))
            {
                SaveSceneClone();
            }

            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        ///     Summary badges for the given state. The array and the interval text are rebuilt only when a value
        ///     changes, so the window repaints without allocating.
        /// </summary>
        internal NeoxiderEditorGUI.Badge[] GetSummaryBadges(bool enabled, float intervalMinutes, bool sceneDirty)
        {
            if (_summaryBadges != null && enabled == _badgeEnabled && sceneDirty == _badgeDirty &&
                intervalMinutes.Equals(_badgeInterval))
            {
                return _summaryBadges;
            }

            _badgeEnabled = enabled;
            _badgeInterval = intervalMinutes;
            _badgeDirty = sceneDirty;
            _summaryBadges = new NeoxiderEditorGUI.Badge[]
            {
                new(enabled ? "Enabled" : "Disabled",
                    enabled ? new Color(0.18f, 0.62f, 0.32f, 1f) : new Color(0.46f, 0.46f, 0.50f, 1f)),
                new("Interval " + intervalMinutes.ToString("0.##") + " min", new Color(0.20f, 0.50f, 0.78f, 1f)),
                new(sceneDirty ? "Scene Dirty" : "Scene Clean",
                    sceneDirty ? new Color(0.78f, 0.46f, 0.18f, 1f) : new Color(0.24f, 0.60f, 0.60f, 1f))
            };
            return _summaryBadges;
        }

        /// <summary>
        ///     Saves a copy of the scene to the auto-save folder.
        /// </summary>
        /// <remarks>
        ///     The copy is written with <c>saveAsCopy: true</c>, which leaves the edited scene untouched —
        ///     and, importantly, still dirty. Scheduling therefore must not treat the dirty flag as "not
        ///     backed up yet"; that is <see cref="SceneSaverAutoSaveScheduler" />'s job, and this method
        ///     reports the finished attempt to it.
        /// </remarks>
        public void SaveSceneClone()
        {
            if (EditorApplication.isPlaying)
            {
                _lastSaveStatus = "Skipping auto-save in Play mode.";
                return;
            }

            Scene currentScene = EditorSceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(currentScene.path))
            {
                _lastSaveStatus = "Scene not saved yet, skipping auto-save.";
                return;
            }

            if (!currentScene.isDirty && !SaveEvenIfNotDirty)
            {
                _lastSaveStatus = "Scene is not dirty, skipping auto-save.";
                return;
            }

            try
            {
                UpdateCurrentScenePath();
                bool saved = SaveSceneBackup(currentScene, out string newScenePath);
                ReportBackupResult(saved, newScenePath);
            }
            catch (Exception e)
            {
                _lastSaveStatus = "Error: " + e.Message;
                Debug.LogError("[SceneSaver] " + e.Message);
            }
            finally
            {
                // WHY: a manual "Save Now" counts as handling the current revision too, otherwise the
                // background check writes the identical file again a moment later.
                global::SceneSaver.MarkActiveSceneHandled();
            }
        }

        /// <summary>
        ///     Records the result of one backup attempt in the window's status line and the console: a success is
        ///     logged only with <see cref="LogEachSave" /> on, a failure always.
        /// </summary>
        internal void ReportBackupResult(bool saved, string backupPath)
        {
            if (saved)
            {
                _lastSaveStatus = "Auto-saved: " + backupPath;
                // WHY: the background saver runs every few minutes; the window's status line already shows
                // the result, so the console line is opt-in.
                if (LogEachSave)
                {
                    Debug.Log("[SceneSaver] " + _lastSaveStatus);
                }

                return;
            }

            _lastSaveStatus = "Auto-save failed.";
            // WHY: a failed backup is always reported; nobody watches the window while it runs unattended.
            Debug.LogWarning("[SceneSaver] Failed to save scene to " + backupPath);
        }

        /// <summary>Status line of the last save attempt, as the window shows it.</summary>
        internal string LastSaveStatus => _lastSaveStatus;

        internal static string GetBackupPath(string scenePath)
        {
            string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
            if (string.IsNullOrEmpty(sceneGuid))
            {
                throw new InvalidOperationException("The source scene must have a valid asset GUID.");
            }

            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            return $"Assets/Scenes/AutoSaves/{sceneGuid}/{sceneName}_AutoSave.unity";
        }

        /// <summary>
        ///     Writes the backup copy with the same writer as Save Now, without touching the shared scheduler or
        ///     settings, so tests can address it directly.
        /// </summary>
        internal static bool SaveSceneBackup(Scene scene, out string backupPath)
        {
            backupPath = GetBackupPath(scene.path);
            string directory = Path.GetDirectoryName(backupPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            return EditorSceneManager.SaveScene(scene, backupPath, true);
        }
    }
}
