# MultiKeyEventTrigger

Invoke one UnityEvent for each configured key-down binding. Bindings are independent keys, not simultaneous-key combinations.

## Setup

1. Add `MultiKeyEventTrigger` from the Neoxider Tools component menu.
2. Edit `keyEventPairs`; choose a `key` and connect its `onKeyPressed` event.
3. Enable `_debug` only when needed; messages pass through the global `NeoDiagnostics` gate.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `keyEventPairs` | Array of `KeyEventPair` values. Empty or null means no bindings. |
| `key` | Unity `KeyCode` to poll once per frame. |
| `onKeyPressed` | Event invoked on key-down. A null event is ignored. |
| `_debug` | Optional gated diagnostics for configured key presses. |

## C# configuration

```csharp
MultiKeyEventTrigger.KeyEventPair jump = new(KeyCode.Space);
jump.onKeyPressed.AddListener(Jump);
trigger.keyEventPairs = new[] { jump };
```

`Jump` is a parameterless method. `KeyEventPair(KeyCode)` initializes its UnityEvent. Assign bindings before enabling the component when setup order matters, and remove listeners when their owner is disposed. Duplicate key entries each invoke their own event. Holding a key does not repeat an event unless the input backend reports another key-down.

Polling uses [KeyInputCompat](KeyInputCompat.md), including its documented Input System key subset. For modifiers, rebinding, action maps or arbitrary text input, use an application input/action layer above this component.

## See Also

- [Module Root](README.md)
