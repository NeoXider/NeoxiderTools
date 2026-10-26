# SettingMixer

**Purpose:** Controls `AudioMixer` parameters from C# or UnityEvents. Normalized volume (`0..1`) maps
to silence through `0 dB`; direct dB methods accept `-80..20 dB`.

## Setup

1. Add the component `Add Component > Neoxider > Audio > SettingMixer`.
2. Configure the parameter type (`Master`, `Music`, `Efx`, or `Custom`).
3. Bind `Slider.OnValueChanged(float)` to the `SettingMixer.Set(float)` method.
4. Assign the mixer and expose the matching parameter on it. Use `SetVolumeDb(float)` for direct dB input;
   normalized `Set(float)` / `SetVolume(float)` use `0..1` (silence through `0 dB`).

For C# setup, assign `audioMixer` and `parameterType`, then call `Set(0.5f)`; custom parameters can use
`SetCustom("parameter", 0.5f)`. Non-default enum choices (`Music`, `Efx`, `Custom`) survive initialization.
Legacy serialized parameter names still migrate to their matching enum when no explicit non-default
target was selected; existing serialized names and fields remain compatible.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `parameterType` | Built-in channel type (`Master`, `Music`, `Efx`) or `Custom` for your own parameter. |
| `customParameterName` | The name of the parameter in the mixer (used if `parameterType` = `Custom`). |
| `audioMixer` | Reference to the mixer to control. |

## See Also
- [AudioControl](View\AudioControl.md) - A smarter component that automatically finds the Slider and synchronizes itself.
- [Module Root](../README.md)
