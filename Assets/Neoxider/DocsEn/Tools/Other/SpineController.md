# SpineController

**Purpose:** A universal controller for Spine animations (`SkeletonAnimation`). Manages animation playback by index or name, skin switching with `PlayerPrefs` persistence, and auto-return to a default (idle) animation.

> ⚠️ Requires the **Spine Unity Runtime** package (`SPINE_UNITY` define).

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Skeleton Animation** | Reference to `SkeletonAnimation` (auto-assigned). |
| **Auto Populate Animations / Skins** | Automatically populate lists from `SkeletonDataAsset`. |
| **Default Animation Name / Index** | Idle animation played by default. |
| **Play Default On Enable** | Play the default animation when the component is enabled. |
| **Queue Default After Non Looping** | Automatically return to idle after a one-shot animation. |
| **Persist Skin Selection** | Save the selected skin index to `PlayerPrefs`. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `TrackEntry Play(string name, bool loop, float mix, bool queueDefault)` | Play an animation by name. |
| `TrackEntry Play(int index, bool loop, float mix, bool queueDefault)` | Play an animation by index. |
| `void PlayDefault()` | Return to the default animation. |
| `void Stop()` | Clear all animation tracks. |
| `void SetSkinByIndex(int skinIndex)` | Set a skin by index. |
| `void SetSkin(string skinName)` | Set a skin by name. |
| `void NextSkin()` / `void PreviousSkin()` | Cycle to the next/previous skin. |
| `string CurrentAnimationName { get; }` | Name of the currently playing animation. |
| `int CurrentSkinIndex { get; }` | Index of the current skin. |

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
