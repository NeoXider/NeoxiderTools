# TextScore

`Neo.Tools.TextScore` extends [SetText](../Text/SetText.md) and displays current or best score from a
[ScoreManager](ScoreManager.md), preserving the base component's formatting and animation settings.

## Scene setup

Add **Neoxider / Tools / TextScore** to a TMP label, assign the base **Text** reference if needed, and
choose **Display Mode**. Set **Score Source** explicitly when the view belongs to a particular manager;
otherwise the component falls back to `ScoreManager.I`. The hidden legacy Best flag still selects
best-score mode for existing serialized scenes.

Binding starts in `Start`. If the source is not yet available, the component waits. Disabling the view
cancels that wait and disposes the exact reactive subscription that was acquired. Re-enabling resolves
the source again and displays its latest value. Changes to the selected mode or explicit source take
effect on the next re-enable, and cannot leave the previous source subscribed.

`TextScore` calls the inherited `SetText` enable/disable hooks. With **On Enable Anim** enabled,
disabling either the component or its GameObject pauses an in-progress numeric animation as well as
releasing the binding. Re-enabling binds the latest score and animates toward it, including score
changes made while disabled. Turning that base option off retains the base animation policy; binding
cleanup still happens. Destroying the view keeps the base tween cleanup.

## C# alternative

For a view managed entirely from code, use the same typed score and text APIs directly:

```csharp
ScoreManager source = ScoreManager.I;
IDisposable subscription = source.Score.Subscribe(scoreLabel.Set, invokeImmediately: true);
// Dispose when this view is disabled or destroyed.
subscription.Dispose();
```

Here `scoreLabel` is an assigned `SetText`, and `IDisposable` comes from `System`. Use `BestScore`
instead of `Score` for a record label. `TextScore` supplies this subscription lifecycle automatically.

See [Components](README.md) and [ScoreManager](ScoreManager.md).
