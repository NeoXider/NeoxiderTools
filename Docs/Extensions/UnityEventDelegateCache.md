# UnityEventDelegateCache

**What it is:** a small class that keeps the exact `UnityAction` used by each indexed subscription so it can be
removed later (lambdas capturing an index cannot be recreated for `RemoveListener`). File:
`Scripts/Extensions/UnityEventDelegateCache.cs`, namespace `Neo.Extensions`.

**How to use:**
1. Keep one cache per list of buttons/items: `private readonly UnityEventDelegateCache _clicks = new();`.
2. Subscribe slot `i` with `_clicks.SubscribeAt(i, buttons[i].onClick, () => Select(index));`, where `index` is a
   copy of `i` made inside the loop (`int index = i;`); a lambda that captures the loop variable itself sees its final
   value (one past the last index) for every button.
3. In `OnDestroy`/`OnDisable` call `_clicks.UnsubscribeAll();`.

---

## Methods

- `SubscribeAt(index, evt, action)` grows the cache and replaces that slot's previous tracked subscription. It removes the previous listener even when the new event is different. Negative indices throw `ArgumentOutOfRangeException`.
- `UnsubscribeAt(index, evt)` removes the stored action from the supplied event and clears the matching slot. An unrelated event does not discard a tracked subscription.
- `UnsubscribeAll()` removes every `SubscribeAt` subscription from its recorded event, clears the cache and returns the number of listeners removed.
- `Add(action)` caches a manually registered delegate; supply its original event to `UnsubscribeAt`. `UnsubscribeAll` cannot detach it (no event is recorded) and only drops it from the cache.
- `Count` and the indexer expose cached slots. `Clear()` clears slots without removing listeners; unsubscribe first or use `UnsubscribeAll()`.

```csharp
private readonly UnityEventDelegateCache _clicks = new();

private void Bind(Button[] buttons)
{
    for (int i = 0; i < buttons.Length; i++)
    {
        int index = i;
        _clicks.SubscribeAt(i, buttons[i].onClick, () => Select(index));
    }
}

private void OnDestroy()
{
    _clicks.UnsubscribeAll();
}
```

**По-русски:** повторный `SubscribeAt` заменяет подписку в слоте и снимает старый listener. `UnsubscribeAll()` снимает все подписки, сделанные через `SubscribeAt`, и очищает кэш; `Clear()` очищает только кэш.

## See Also
- ← [Extensions](README.md)
