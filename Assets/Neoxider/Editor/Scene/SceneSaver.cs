using System;
using Neo.Editor;
using Neo.Editor.Windows;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
///     Editor window for automatic scene backup saves.
/// </summary>
/// <remarks>
///     The background check runs on <c>EditorApplication.update</c> but only compares numbers there; the
///     save itself is deferred to <c>EditorApplication.delayCall</c> so a full scene serialization can
///     never block the editor tick, and <see cref="SceneSaverAutoSaveScheduler" /> guarantees the same
///     scene revision is never written twice.
/// </remarks>
[InitializeOnLoad]
public class SceneSaver : EditorWindow
{
    private static readonly SceneSaverGUI _staticGUI;
    private static readonly SceneSaverAutoSaveScheduler _scheduler;
    private static bool _isSubscribed;
    private static bool _isSaveQueued;
    private SceneSaverGUI _gui;

    static SceneSaver()
    {
        _staticGUI = new SceneSaverGUI(SceneSaverSettings.Shared);
        _scheduler = new SceneSaverAutoSaveScheduler();

        // WHY: [InitializeOnLoad] runs before the editor has finished opening scenes; reading scene state
        // here is both useless and the worst possible moment for it.
        EditorApplication.delayCall += InitializeAfterLoad;
        Subscribe();
    }

    private void OnEnable()
    {
        _gui = new SceneSaverGUI(SceneSaverSettings.Shared);
        _gui.UpdateCurrentScenePath();

        // WHY: re-arms the background check if a previous failure had to detach it.
        Subscribe();
    }

    private void OnGUI()
    {
        _gui?.OnGUI(this);
    }

    /// <summary>
    ///     Opens the Scene Saver window.
    /// </summary>
    [MenuItem("Neoxider/Tools/Scene Saver", false, 100)]
    public static void ShowWindow()
    {
        SceneSaver window = GetWindow<SceneSaver>("Scene Saver");
        window.minSize = new Vector2(250, 100);
        _staticGUI?.UpdateCurrentScenePath();
    }

    /// <summary>
    ///     Marks the current state of the active scene as already backed up, so the background check does
    ///     not immediately write the very same revision again after a manual save.
    /// </summary>
    public static void MarkActiveSceneHandled()
    {
        if (_scheduler == null)
        {
            return;
        }

        Scene activeScene = EditorSceneManager.GetActiveScene();
        _scheduler.MarkHandled(activeScene.path, Undo.GetCurrentGroup(), EditorApplication.timeSinceStartup);
    }

    private static void InitializeAfterLoad()
    {
        _staticGUI?.UpdateCurrentScenePath();
        _scheduler?.ResetTimer(EditorApplication.timeSinceStartup);
    }

    private static void Subscribe()
    {
        if (_isSubscribed)
        {
            return;
        }

        _isSubscribed = true;

        // WHY: paired -= before every += so a second subscription can never stack two checks per tick.
        EditorApplication.update -= BackgroundSaveCheck;
        EditorApplication.update += BackgroundSaveCheck;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        AssemblyReloadEvents.beforeAssemblyReload -= Unsubscribe;
        AssemblyReloadEvents.beforeAssemblyReload += Unsubscribe;
        EditorApplication.quitting -= Unsubscribe;
        EditorApplication.quitting += Unsubscribe;
    }

    private static void Unsubscribe()
    {
        _isSubscribed = false;
        EditorApplication.update -= BackgroundSaveCheck;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        AssemblyReloadEvents.beforeAssemblyReload -= Unsubscribe;
        EditorApplication.quitting -= Unsubscribe;
    }

    private static void BackgroundSaveCheck()
    {
        try
        {
            if (_isSaveQueued)
            {
                return;
            }

            SceneSaverSettings settings = SceneSaverSettings.Shared;
            if (!settings.IsEnabled)
            {
                return;
            }

            // WHY: batch mode is automation — there is no user whose work needs protecting, and a backup
            // written into the repository mid-run is pure side effect.
            if (Application.isBatchMode)
            {
                return;
            }

            // WHY: play mode owns the scene, and asset import / compilation are exactly the moments a
            // blocking save turns into the "Hold on" dialog.
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying ||
                EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            // WHY: in prefab isolation the active scene is the prefab stage, and copying it would write a
            // bogus "<prefab>_AutoSave.unity" next to real scene backups.
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                return;
            }

            Scene activeScene = EditorSceneManager.GetActiveScene();
            SceneSaverCheckContext context = new SceneSaverCheckContext(
                EditorApplication.timeSinceStartup,
                settings.IntervalMinutes * 60d,
                activeScene.path,
                Undo.GetCurrentGroup(),
                activeScene.isDirty,
                settings.SaveEvenIfNotDirty);

            if (!_scheduler.ShouldSave(context))
            {
                return;
            }

            // WHY: SaveScene serializes the whole scene. Running it inside the update callback freezes the
            // editor for as long as the write takes, which is what the busy dialog reported.
            _isSaveQueued = true;
            EditorApplication.delayCall += RunQueuedSave;
        }
        catch (Exception e)
        {
            // WHY: a throwing update callback repeats its exception every tick. Detach instead, and say
            // how to bring the feature back.
            Unsubscribe();
            Debug.LogError("[SceneSaver] Auto-save check failed and was detached for this session. " +
                           "Reopen Neoxider/Tools/Scene Saver to re-arm it. " + e);
        }
    }

    private static void RunQueuedSave()
    {
        _isSaveQueued = false;

        try
        {
            SceneSaverSettings settings = SceneSaverSettings.Shared;
            if (settings.IsEnabled && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                _staticGUI?.SaveSceneClone();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[SceneSaver] Auto-save failed. " + e);
        }
        finally
        {
            // WHY: marked even when the save was skipped or threw. An unmarked revision is retried on the
            // very next tick, which is the same endless re-save with extra steps.
            MarkActiveSceneHandled();
        }
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        _staticGUI?.UpdateCurrentScenePath();
        _scheduler?.ResetTimer(EditorApplication.timeSinceStartup);
    }
}
