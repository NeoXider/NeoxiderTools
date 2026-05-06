# CoroutineExtensions

**Purpose:** Extension methods for running delayed and conditional coroutine actions on any `MonoBehaviour` or `GameObject`. Also provides global static methods that work without an explicit owner. Returns a `CoroutineHandle` for stop/status control.

---

## API

| Method | Description |
|--------|-------------|
| `this.Delay(float seconds, Action, bool useUnscaledTime = false)` | Execute action after a delay. Returns `CoroutineHandle`. |
| `this.WaitUntil(Func<bool>, Action)` | Execute action when predicate becomes `true`. |
| `this.WaitWhile(Func<bool>, Action)` | Execute action when predicate becomes `false`. |
| `this.DelayFrames(int count, Action, bool useFixedUpdate = false)` | Execute action after N frames. |
| `this.NextFrame(Action)` | Execute action on the next frame. |
| `this.EndOfFrame(Action)` | Execute action at end of current frame. |
| `this.RepeatUntil(Func<bool>, Action)` | Repeat action each frame until condition is met. |
| `CoroutineExtensions.Delay(seconds, action)` | Global static — no owner needed. |
| `CoroutineExtensions.Start(IEnumerator)` | Start a custom coroutine globally. |

### CoroutineHandle

| Property / Method | Description |
|-------------------|-------------|
| `bool IsRunning` | Whether the coroutine is currently running. |
| `void Stop()` | Stop the coroutine. |

---

## Examples

### Code
```csharp
// Delay on MonoBehaviour
this.Delay(2f, () => Debug.Log("2 sec later"));

// Next frame
this.NextFrame(() => RecalculateLayout());

// Wait until player grounded
this.WaitUntil(() => player.IsGrounded, () => EnableJump());

// Stop a delayed action
var handle = this.Delay(5f, () => Explode());
handle.Stop(); // cancel

// Global delay (no MonoBehaviour needed)
CoroutineExtensions.Delay(1f, () => ShowSplash());
```

---

## See Also
- [AudioExtensions](AudioExtensions.md) — uses `CoroutineHandle` for fading
- ← [Extensions](README.md)
