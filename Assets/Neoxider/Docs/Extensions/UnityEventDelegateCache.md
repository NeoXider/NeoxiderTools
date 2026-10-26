# UnityEventDelegateCache

**Purpose:** Keep the exact `UnityAction` used by each indexed subscription so it can be removed later.

- `SubscribeAt(index, evt, action)` grows the cache and replaces that slot's previous tracked subscription. It removes the previous listener even when the new event is different. Negative indices throw `ArgumentOutOfRangeException`.
- `UnsubscribeAt(index, evt)` removes the stored action from the supplied event and clears the matching slot. An unrelated event does not discard a tracked subscription.
- `Add(action)` caches a manually registered delegate; supply its original event to `UnsubscribeAt`.
- `Count` and the indexer expose cached slots. `Clear()` clears slots without removing listeners; unsubscribe first.

**По-русски:** повторный `SubscribeAt` заменяет подписку в слоте и снимает старый listener. `Clear()` очищает только кэш; оставшиеся подписки нужно снять через `UnsubscribeAt` заранее.

## See Also
- ← [Extensions](README.md)
