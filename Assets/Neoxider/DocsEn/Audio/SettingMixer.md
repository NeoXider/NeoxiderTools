# SettingMixer

**Purpose:** Utility for directly controlling `AudioMixer` parameters via Unity Events (e.g., from standard UI sliders). It automatically converts normalized volume (0-1) into decibels (-80 to 20 dB).

## Setup

1. Add the component `Add Component > Neoxider > Audio > SettingMixer`.
2. Configure the parameter type (`Master`, `Music`, `Efx`, or `Custom`).
3. Bind `Slider.OnValueChanged(float)` to the `SettingMixer.Set(float)` method.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `parameterType` | Built-in channel type (`Master`, `Music`, `Efx`) or `Custom` for your own parameter. |
| `customParameterName` | The name of the parameter in the mixer (used if `parameterType` = `Custom`). |
| `audioMixer` | Reference to the mixer to control. |

## See Also
- [AudioControl](View\AudioControl.md) - A smarter component that automatically finds the Slider and synchronizes itself.
- [Module Root](../README.md)
