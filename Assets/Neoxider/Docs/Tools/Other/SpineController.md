# SpineController

**Purpose:** A universal controller for Spine animations (`SkeletonAnimation`). Manages animation playback by index or name, skin switching with `PlayerPrefs` persistence, and auto-return to a default (idle) animation.

> ⚠️ Requires the **Spine Unity Runtime** package (`SPINE_UNITY` define).

`SpineController` is in the global namespace, in `Neo.Tools.Other`. That assembly must be able to reference the installed Spine runtime assemblies when `SPINE_UNITY` is enabled. Without the define, only a placeholder component is compiled: it disables itself in `Awake` and has no animation/skin methods.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Skeleton Animation** | Reference to `SkeletonAnimation` (auto-assigned). |
| **Auto Populate Animations / Skins** | Automatically populate lists from `SkeletonDataAsset`. |
| **Default Animation Name / Index** | Idle animation played by default. |
| **Play Default On Enable** | Play the default animation when the component is enabled. |
| **Queue Default After Non Looping** | Automatically return to idle after a one-shot animation. |
| **Persist Skin Selection** | Save the selected skin index to `PlayerPrefs`. |
| **Skin Prefs Key** | PlayerPrefs key; use a unique key for independently persisted characters. Empty/whitespace falls back to `SkinChanger`. |
| **Skin Index Offset** | Number of leading skins to skip. Clamped to the available list; the legacy offset toggle adds one more skipped skin where available. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `TrackEntry Play(string name, bool loop, float mix, bool queueDefault)` | Play an animation by name. |
| `TrackEntry Play(int index, bool loop, float mix, bool queueDefault)` | Play an animation by index. |
| `void PlayDefault()` | Return to the default animation. |
| `void PlayDefault(bool forceRestart)` | Start idle if no controller-owned entry exists, or force its restart. Works after `Stop()`. |
| `void Stop()` | Clear all animation tracks. |
| `void SetSkinByIndex(int skinIndex)` | Set a skin by index. |
| `void SetSkinByIndex(int skinIndex, bool persist)` | Set a logical skin index after the offset; `false` suppresses saving this selection. |
| `void SetSkin(string skinName)` | Set a skin by name. |
| `void NextSkin()` / `void PreviousSkin()` | Cycle to the next/previous skin. |
| `string CurrentAnimationName { get; }` | Name of the currently playing animation. |
| `int CurrentSkinIndex { get; }` | Index of the current skin. |

Invalid animation names/indices return `null` without replacing the current animation. Animation lists are initialized before an index is resolved. `Stop()` removes the controller's completion callback and clears animation tracks; `PlayDefault()` can start idle again. For compatibility, `CurrentAnimationName` falls back to the configured default name when no entry exists, so it is not a playback-state flag.

Skin indices are clamped within the logical list after the offset. A skin change updates `CurrentSkinIndex`/`CurrentSkinName`, persists the index, and invokes `OnSwapSkin` only after the skin is found and applied to a live skeleton. An unavailable skeleton or a stale custom skin name leaves the previous successful selection intact. Disabling persistence suppresses PlayerPrefs reads/writes; the `persist` overload cannot override that global switch.

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnSwapSkin` | *(none)* | Skin was changed. |

## Examples

### No-Code Example (Inspector)
On an object with `SkeletonAnimation`, add `SpineController`. Animation and skin lists auto-populate. Set `Default Animation = idle`. Wire a UI button to `SpineController.NextSkin()` to cycle skins.

### Code Example
```csharp
[SerializeField] private SpineController _spine;

public void PlayAttackAnimation()
{
    _spine.Play("attack", false, 0.1f, true);
}
```

## See Also
- ← [Tools/Other](README.md)
