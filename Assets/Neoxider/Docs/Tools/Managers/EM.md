# EM

## Overview
`EM` is the global event manager for gameplay states, pause flow, and a few application-level callbacks.

- **Namespace**: `Neo.Tools`
- **Path**: `Assets/Neoxider/Scripts/Tools/Managers/EM.cs`

## How to use
1. Add `EM` as one of your global managers.
2. Connect UI, audio, analytics, and other systems to its `UnityEvent` fields.
3. Trigger events through static helpers such as `EM.Menu()`, `EM.GameStart()`, `EM.Pause()`, and so on.
4. In most projects `GM` changes the state and `EM` broadcasts reactions.

The static helpers broadcast only: they do not change `GM.State`, pause time, or create a `GM`. Use `GM.Pause()` / `GM.Resume()` for gameplay pause. Inspector action buttons are available in Play Mode; C# and UnityEvent calls use the same public helpers.

## Main events
- `OnMenu`
- `OnPreparing`
- `OnGameStart`
- `OnRestart`
- `OnStopGame`
- `OnWin`
- `OnLose`
- `OnEnd`
- `OnStateChange`
- `OnPause`
- `OnResume`
- `OnPlayerDeath`
- `OnAwake`
- `OnFocusApplication`
- `OnPauseApplication`
- `OnQuitApplication`

## Static helpers
- `Preparing()`
- `GameStart()`
- `Lose()`
- `Win()`
- `End()`
- `StopGame()`
- `PlayerDied()`
- `Pause()`
- `Resume()`
- `Menu()`
- `Restart()`

## Typical flow
1. `GM` changes its state.
2. `GM` publishes the matching EM gameplay event only while its captured `TransitionVersion` is current.
3. `EM` broadcasts the event to all listeners.

In 11.0, a GM callback that changes state suppresses the remaining gameplay notifications of the old transition. For example, resuming inside `OnStateChange(Pause)` does not produce a later obsolete `OnPause`. Normal event order stays synchronous; an already-entered UnityEvent listener batch completes normally. This guard belongs to GM. Direct `EM.Pause()`, `EM.Resume()` and other helpers continue to broadcast explicitly without changing or checking GM state/version. See [GM transition versions](./GM.md#transition-version-and-reentrant-notifications) for C# ownership and callback rules.

## C# listeners and lifetime

Use an explicitly assigned event hub or `EM.TryGetInstance(out EM events)` for an optional dependency. Static helpers use `EM.I`, so they may resolve an inactive hub or auto-create one when `EM.CreateInstance` is enabled. A missing hub or an unassigned event is a no-op.

```csharp
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

public sealed class GameStartListener : MonoBehaviour
{
    [SerializeField] private EM _events;

    private void OnEnable()
    {
        if (_events != null)
        {
            _events.OnGameStart ??= new UnityEvent();
            _events.OnGameStart.AddListener(HandleGameStart);
        }
    }

    private void OnDisable()
    {
        if (_events != null)
        {
            _events.OnGameStart?.RemoveListener(HandleGameStart);
        }
    }

    private void HandleGameStart()
    {
        // React to the announced game start.
    }
}
```

For Inspector workflows, connect persistent listeners directly to the event fields. `OnAwake` runs during singleton initialization, which can happen through lazy `I` resolution before the component's Unity `Awake`. Focus, pause, and quit application callbacks forward Unity's values without changing game state.

## See also
- [`GM`](./GM.md)
- [`Singleton`](./Singleton.md)
