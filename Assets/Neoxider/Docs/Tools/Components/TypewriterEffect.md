# TypewriterEffectComponent

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.

## Runtime controls

- `PlayAutoText()` starts typing the configured auto-start or target text.
- `Stop()` stops typing and keeps the currently visible text.
- `Clear()` stops the effect, resets it, and clears the target text.

All three methods are exposed as Inspector buttons in Play Mode. The buttons are disabled outside Play
Mode, so preview actions do not change serialized scene content.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `Effect` | Effect. |
| `IsTyping` | Is Typing. |
| `OnCharacterTyped` | On Character Typed. |
| `OnComplete` | On Complete. |
| `OnStart` | On Start. |
| `Progress` | Progress. |
| `ProgressValue` | Progress Value. |
| `TargetText` | Target Text. |
| `_autoStartText` | Auto Start Text. |
| `_effect` | Effect. |
| `_playOnEnable` | Play On Enable. |
| `_targetText` | Target Text. |

## See Also

- [Module Root](../README.md)
