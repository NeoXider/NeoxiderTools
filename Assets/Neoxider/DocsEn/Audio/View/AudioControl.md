# AudioControl

**Purpose:** A smart UI component that automatically finds a `Slider` or `Toggle` on the object and binds it to the volume/mute settings in `AMSettings`. You don't need to write code or configure Unity Events — the component automatically synchronizes its state upon loading or when audio settings change.

## Setup

1. Add a standard UI `Toggle` (for mute/unmute) or `Slider` (for volume) to the scene.
2. Attach the `AudioControl` script to the exact same object.
3. Select what it should control (Master, Music, Efx).

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `controlType` | What to control: `Master`, `Music`, `Efx` or `Custom`. |
| `uiType` | The UI element type. `Auto` will automatically find the `Toggle` or `Slider` on this GameObject. |
| `backendType` | If `AudioSourceAndMixer`, changes volume in both the `AudioSource` and the mixer. If `MixerOnly`, changes only the Exposed parameter in the mixer. |
| `onSetActiveCustom` | Events fired when toggled (used only for `Custom` type). |
| `onSetPercentCustom` | Events fired when the Slider changes (only for `Custom`). |
| `forceSliderNormalizedRange`| If `true`, the script forces the Slider's min/max range to 0 and 1. |
| `unmutePercent` | The volume level (0-1) to restore to when unmuting via a Toggle. |

## See Also
- [AMSettings](../AMSettings.md) - The manager this component synchronizes with.
- [Module Root](../../README.md)
