# TextLevel

**Purpose:** A UI component that automatically subscribes to the `LevelManager` and displays the current or maximum level in a `TextMeshPro` text field.

## Setup

1. Add `Add Component > Neoxider > Level > TextLevel` to an object with `TextMeshPro`.
2. Select the display mode (`Current` or `Max`).
3. On game start, the text automatically updates and reacts to level changes.

The component uses the existing `SetText` API and formatting/animation settings. `IndexOffset` is initialized from `_displayOffset`; the underlying level remains zero-based. `Max` displays the absolute progress frontier even when the selected level wraps on a looping map.

In 11.0, integer updates finish with the exact formatted integer, including `IndexOffset`, number options, prefix, and suffix. For example, level `16777216` with offset 1 finishes as `16777217` in Plain mode or `16,777,217` with comma grouping. Animation frames and the inherited `currentNum` remain approximate floats. Completion may emit a final `OnTextUpdated` when the exact text differs from the last interpolated value; later formatting-only changes retain the exact integer.

If no manager exists yet, the enabled label waits for one. Disabling the component cancels that wait and removes its event listener. Re-enabling starts a fresh wait or immediately binds the available manager, including when the manager is spawned after the label was hidden and shown. A disabled label does not acquire a deferred subscription.

Binding schedules the manager snapshot through `SetText`'s numeric tween. Even a zero animation duration uses the configured DOTween update clock; it does not guarantee that `text.text` changes before `OnEnable` returns. A paused clock can postpone that write while native frames continue. Use `OnTextUpdated` to observe the actual formatted output. The TMP target must be assigned or available on the same object when the component initializes.

Привязка к менеджеру и запись текста — разные этапы: числовой снимок проходит через tween `SetText`, даже при нулевой длительности. Пауза его часов может отложить запись при продолжающихся кадрах Unity. `OnTextUpdated` сообщает о фактическом выводе; ссылка на TMP должна быть назначена либо находиться на том же объекте при инициализации.

`TextLevel` also forwards enable/disable to the base `SetText` lifecycle. With the existing `_onEnableAnim` option enabled, disabling only the component pauses its tween; re-enabling resumes it and refreshes the current manager snapshot when a source is available. A value queued while disabled can resume while the label waits for a manager. Subclasses overriding `OnEnable` or `OnDisable` must call base once to retain both tween handling and level listener/wait ownership.

## C# and Inspector events

For automatic binding, configure `_levelSource` in the Inspector or leave it empty for the singleton fallback. Use inherited `Set(int)`, `Set(float)`, and `Set(string)` for explicit text output; inherited `OnTextUpdated` reports formatted text. Other views can subscribe directly to `LevelManager.OnChangeLevel` or `OnChangeMaxLevel` instead of using this component.

```csharp
label.IndexOffset = 1;
label.Set(16777216);        // Exact final display: 16777217; applies IndexOffset once.
label.SetInteger(2147483648L); // Already offset display value; does not apply IndexOffset.
```

`Set(int)` remains the method used by level UnityEvents; it reuses `Neo.Tools.SetText`'s exact integer path. `SetInteger(long)` is the additive API for a final integer already computed by the caller. No extra formatter, scene service, or serialized migration is required. `Set(float)` retains float semantics; `SetBigInteger` remains the immediate exact display path.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_displayMode` | `Current` — display the currently played level. `Max` — display the maximum unlocked level. |
| `_displayOffset` | How much to offset the number for display (since levels in code start at 0, an offset of `1` displays level "1"). |
| `_levelSource` | Optional reference to a specific `LevelManager` (needed only if there are multiple in the scene). Otherwise, uses the singleton `LevelManager.I`. |

## See Also
- [LevelManager](LevelManager.md)
- [Level module](README.md)
