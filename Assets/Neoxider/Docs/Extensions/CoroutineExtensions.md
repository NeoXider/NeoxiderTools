# CoroutineExtensions

Run delayed/conditional actions from C# through `using Neo.Extensions;`. Each call returns a `CoroutineExtensions.CoroutineHandle` for tracked cancellation and status. Reference `Neo.Extensions` from a custom asmdef. The complete ownership contract is in [Lifecycle and ownership](LifecycleAndOwnership.md).

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
| `CoroutineExtensions.WaitUntil`, `WaitWhile`, `DelayFrames` | Global variants of the corresponding owner-bound methods. |
| `CoroutineExtensions.Start(IEnumerator)` | Start a custom coroutine globally. |

The `this` methods above target a `MonoBehaviour`. `GameObject` overloads exist for Delay, WaitUntil, WaitWhile and DelayFrames and use a package runner on that object. Static variants use a persistent global helper; no manual runner setup is needed.

## CoroutineHandle

| Property / Method | Description |
|-------------------|-------------|
| `bool IsRunning` | Package-tracked running state; false after tracked completion/cancellation. |
| `Coroutine Coroutine` | Native Unity coroutine for interoperation, such as yielding. Can be null after synchronous completion. |
| `void Stop()` | Idempotent tracked cancellation; releases nested iterators and clears native/running state. |

An operation can finish and invoke its action before the call returns, for example DelayFrames with zero frames. Its returned handle stays completed. `CoroutineExtensions.Start(null)` returns an inactive handle without creating a helper.

## Owner lifetime and cancellation

Destroying the supplied MonoBehaviour cancels its operation even if its GameObject survives. Deactivating its GameObject or an ancestor also cancels; re-enabling does not resume the old operation. Starting on an inactive hierarchy completes without invoking the action. Disabling only the component's enabled flag keeps its coroutines running, so cancel explicitly if that is the view's policy. Null/destroyed owners use the global fallback and do not provide scene-bound cancellation.

`handle.Stop()` releases tracked nested IEnumerator values from child to parent, including iterator finally/IDisposable cleanup. Cancellation during advancement defers disposal until that advancement returns; cleanup faults use the diagnostics gate while remaining tracked iterators are released. CustomYieldInstruction values retain Unity's polling phase. A separately started native Coroutine yielded by a routine keeps its own lifetime.

**Raw native stop boundary:** `owner.StopCoroutine(handle.Coroutine)` and `owner.StopAllCoroutines()` do not notify this utility reliably. The handle may still report IsRunning and retain iterator state. Use `handle.Stop()`; it is safe after an external native stop too. Exposing Coroutine does not promise automatic observation of every Unity stop.

## C# example

Inside a MonoBehaviour:

```csharp
private CoroutineExtensions.CoroutineHandle _pending;

public void Schedule()
{
    _pending?.Stop();
    _pending = this.Delay(2f, Refresh, useUnscaledTime: true);
}

public void Cancel()
{
    _pending?.Stop();
    _pending = null;
}

private void OnDisable()
{
    Cancel(); // This component also cancels when only its enabled flag changes.
}

private void Refresh()
{
    // Update the component's state.
}
```

Use the same Schedule/Cancel methods from scene buttons or UnityEvents when a NoCode workflow is desired. Keep the handle on the owning component. Choose scaled Delay for game-time waits or useUnscaledTime for waits that continue at timeScale zero; fixed-update/end-of-frame operations still require their native Unity scheduling phases. These APIs are for Play Mode and Unity's supported thread.

**По-русски:** сохраняйте handle и отменяйте через `Stop()`. Уничтожение компонента и деактивация иерархии отменяют связанную операцию, а отключение только компонента само по себе её не останавливает. Прямой `StopCoroutine` не гарантирует обновления handle; скрытые runner/tracker вручную настраивать не требуется.

## See Also

- [Lifecycle and ownership](LifecycleAndOwnership.md)
- [AudioExtensions](AudioExtensions.md) — uses `CoroutineHandle` for fading
- ← [Extensions](README.md)
