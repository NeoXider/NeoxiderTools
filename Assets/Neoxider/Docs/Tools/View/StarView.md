# StarView

**Purpose:** Display `ScoreManager.CountStars` using GameObjects, `VisualToggle` components, or both.
Namespace: `Neo.Tools`; assembly: `Neo.Tools.View`.

## Setup

- Assign `scoreManager` and ordered `starObjects` / `stars` arrays before activating the view.
  The first `count` slots are on; remaining slots are off. Empty/destroyed slots are skipped and retain
  their index, so an absent first star does not shift the second star's threshold.
- `Awake` displays the current count and subscribes to `CountStarsReactive`. Without a manager it leaves
  the authored visuals unchanged. The `FindInScene` attribute is an authoring convenience; explicit
  references avoid scene-search dependencies.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `scoreManager` | Explicit score source. |
| `starObjects` | Optional ordered GameObjects; uses `SetActive`. |
| `stars` | Optional ordered `Neo.UI.VisualToggle` components; uses their typed `SetActive(bool)` API. |

## C# and lifecycle

```csharp
// Configure an inactive view before its first activation.
view.scoreManager = scoreManager;
view.starObjects = starObjects;
view.gameObject.SetActive(true);
scoreManager.CountStars = 2;
```

The existing public `Awake()` can explicitly rebind an already active view after assigning a new source;
repeated calls keep one subscription. Merely assigning the field does not rebind. Subscriptions continue
while the view component is disabled and are removed on destruction, including when the public manager
field has since changed. A surviving external star object is no longer updated after the view is destroyed.

## See Also

- [Module Root](../README.md)
