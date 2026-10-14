# AudioFocusMute

**Purpose:** Silences all game audio while the application has no focus — a minimized window, another browser tab
or another application in front — and restores the volume it found when focus returns.

It works through `AudioListener.volume`, so music, effects, footsteps and UI sounds go quiet at once and keep their
playback position. Nothing is paused: sounds triggered while unfocused do not burst out together on return, and a
game's own pause logic on `AudioListener.pause` is left alone.

## Setup

One call at startup is enough for the whole game:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
private static void Install() => AudioFocusMute.Install();
```

`Install()` returns the existing instance or creates a persistent one (`DontDestroyOnLoad`). You can also add
`Add Component > Neoxider > Audio > Audio Focus Mute` to a persistent object by hand.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_muteWhenUnfocused` | Silence audio while unfocused. Turning it off while muted restores the volume at once. |
| `_muteInEditor` | Also silence an unfocused Unity Editor in Play Mode. `Install(false)` turns it off. |

## API

| Member | Effect |
|--------|--------|
| `Install()` / `Install(bool muteInEditor)` | Finds or creates the instance and applies the editor option. |
| `SetFocused(bool)` | Applies a focus change; the Unity callbacks route here, hosts and tests may call it too. |
| `IsMuted` | Whether the component currently holds the listener volume at zero. |
| `MuteWhenUnfocused`, `MuteInEditor` | Runtime switches for the two fields above. |

## Behaviour

- The state is applied on enable as well (`Application.isFocused`), so a game opened in a background tab starts
  silent instead of waiting for the first focus change.
- Disabling the component while muted restores the volume.
- In WebGL `OnApplicationFocus` follows the page window: switching tabs, minimizing the browser and focusing another
  window count as losing focus. Focusing an `iframe` on the same page counts too.
- Audio played outside Unity (an HTML video, an `iframe`) is not affected.

## See also

- [Audio module](README.md)
- [AM](AM.md)
