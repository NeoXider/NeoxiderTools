# Scene Saver Utility

**What it is:** A background editor utility that writes a backup copy of the open scene into a separate `Assets/Scenes/AutoSaves` folder without touching the main scene file. The copy protects your work from a Unity crash or an accidental close without saving.

**How to use:** open `Neoxider/Tools/Scene Saver`, set the interval or switch auto-save off. The settings are stored in `EditorPrefs` and survive an editor restart. `Save Now` writes a copy immediately.

---


## 1. Introduction

`SceneSaver` saves a backup copy of the currently open scene at a configurable interval. It never overwrites the scene you are editing: the copy is written with `EditorSceneManager.SaveScene(scene, path, saveAsCopy: true)` into `Assets/Scenes/AutoSaves/<SceneName>_AutoSave.unity`.

Auto-save is enabled out of the box with a 3-minute interval and can be switched off permanently.

---

## 2. Tool Description

### SceneSaver
- **Namespace**: `(global)`
- **File path**: `Assets/Neoxider/Editor/Scene/SceneSaver.cs`
- **GUI class**: `SceneSaverGUI` (`Assets/Neoxider/Editor/GUI/SceneSaverGUI.cs`)
- **Menu access**: `Neoxider/Tools/Scene Saver`

**Description**
An editor script that automatically saves backup copies of the active scene. Logic, settings, scheduling and GUI rendering live in separate classes.

**Key features**
- **Cheap background check**: the `EditorApplication.update` callback only compares numbers. The save itself is deferred to `EditorApplication.delayCall`, so a full scene serialization never runs inside the editor tick.
- **One backup per scene revision**: the same scene state is never written twice (see section 3).
- **Persistent settings**: enabled state, interval and the "save even if not dirty" option are stored in `EditorPrefs`.
- **Configurable interval**: in minutes, clamped to a 0.25-minute minimum — a zero interval would mean "save on every tick".
- **Safe saving**: the copy carries the `_AutoSave` suffix in a separate folder and never overwrites your scene file.
- **Idle when it must be**: skipped in Play mode, in batch mode, while the editor compiles or imports assets, and while a prefab stage is open.

**Public methods**
- `ShowWindow()`: static, opens the settings window. Invoked via `MenuItem`.
- `MarkActiveSceneHandled()`: static, marks the current state of the active scene as already backed up. Called after every save attempt (including a manual `Save Now`) so the background check does not write an identical copy right afterwards.

---

## 3. Why a backup is not written on every check

`saveAsCopy: true` deliberately leaves the edited scene **dirty**. Treating "the scene is dirty" as "not backed up yet" therefore repeats forever: a scene that stays dirty — the normal state while you work, and the permanent state when some tool keeps dirtying it — used to be re-serialized in full every interval for as long as the editor stayed open.

Scheduling is now based on a **scene revision** instead of the dirty flag (`SceneSaverAutoSaveScheduler`):

- a revision is the scene path, plus a change token (`Undo.GetCurrentGroup()`), plus the number of clean-to-dirty transitions observed;
- the interval must have elapsed, and the revision must differ from the one already handled;
- the handled revision is recorded after every attempt — successful, skipped or failed alike, so a failing save cannot turn into a retry loop.

A backup is written again only after the scene actually changes. A change made by a script that neither creates an undo entry nor cleans the scene first cannot be detected this way; use `Save Now` if you need a copy of such a state immediately.

---

## 4. Settings

| Field (window) | Type | Description |
|---|---|---|
| Enable Scene Saver Script | `bool` | Turns the background auto-save on or off. Default: on. |
| Interval (minutes) | `float` | Time between two backup copies. Default: 3, minimum: 0.25. |
| Save Even If Not Dirty | `bool` | Also back up a scene without unsaved changes. Default: off. Still writes at most one copy per revision. |
| Reset Settings | button | Deletes the persisted keys and restores the defaults. |

### SceneSaverSettings
- **Namespace**: `Neo.Editor`
- **File path**: `Assets/Neoxider/Editor/Scene/SceneSaverSettings.cs`

Persists the settings in `EditorPrefs`. Every setter writes through immediately.

**Public properties**
- `IsEnabled` (`bool`), `IntervalMinutes` (`float`, clamped to `MinIntervalMinutes`), `SaveEvenIfNotDirty` (`bool`).
- `Shared` (`static SceneSaverSettings`): the instance used by the background check and by every open window, so a toggle takes effect at once.

**Public methods**
- `Reload()`: re-reads every value from `EditorPrefs`.
- `ResetToDefaults()`: deletes the persisted keys and reloads the defaults.

**EditorPrefs keys** (package-prefixed so a game project cannot collide with them):
`Neoxider.SceneSaver.Enabled`, `Neoxider.SceneSaver.IntervalMinutes`, `Neoxider.SceneSaver.SaveEvenIfNotDirty`.

---

## 5. Related classes

### SceneSaverAutoSaveScheduler
- **Namespace**: `Neo.Editor`
- **File path**: `Assets/Neoxider/Editor/Scene/SceneSaverAutoSaveScheduler.cs`

Pure decision logic — no UnityEditor calls and no file access, so it is cheap enough for the editor tick and directly testable.

- `ResetTimer(double now)`: restarts the interval countdown (e.g. after another scene was opened).
- `ShouldSave(SceneSaverCheckContext context)` returns `bool` — `true` when a backup copy is due now.
- `IsAlreadyHandled(string scenePath, long revision)` returns `bool` — whether this exact revision was already written.
- `MarkHandled(string scenePath, long revision, double now)`: records the revision as dealt with and restarts the countdown.
- `DirtyEpoch` (`long`): number of clean-to-dirty transitions observed so far.

### SceneSaverCheckContext
- **Namespace**: `Neo.Editor`
- **File path**: `Assets/Neoxider/Editor/Scene/SceneSaverCheckContext.cs`

Immutable snapshot passed to the scheduler: `Now`, `IntervalSeconds`, `ScenePath`, `Revision`, `IsDirty`, `SaveEvenIfNotDirty`.

---

## 6. See also

- ← [Editor](README.md)
