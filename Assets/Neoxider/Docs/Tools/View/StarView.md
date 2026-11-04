# StarView

**What it is:** a MonoBehaviour that displays a star count - `ScoreManager.CountStars` or a value set directly -
using ordered GameObjects, `VisualToggle` components, or both. Script: `Scripts/Tools/View/StarView.cs`,
namespace `Neo.Tools`, assembly `Neo.Tools.View`.

**How to use:**
1. Fill `starObjects` and/or `stars` in order (slot 0 = first star).
2. Assign `scoreManager` to follow a score, or call `ShowStars(count)` for a standalone view (level select,
   results screen with saved stars).
3. Rebind at runtime with `Bind(manager)`.

## Setup

- Assign `scoreManager` and ordered `starObjects` / `stars` arrays before activating the view.
  The first `count` slots are on; remaining slots are off. Empty/destroyed slots are skipped and retain
  their index, so an absent first star does not shift the second star's threshold.
- `Awake` displays the current count and subscribes to `CountStarsReactive`. Without a manager it leaves
  the authored visuals unchanged. The `FindInScene` attribute is an authoring convenience: while the field
  is empty (and attribute search is on in the Neoxider settings), the inspector fills it with the scene's
  first `ScoreManager`, so in a scene that has one the field does not stay empty. Explicit references avoid
  scene-search dependencies.
- `ShowStars(count)` works whether or not a manager is bound, but a bound manager overrides it on its next
  `CountStars` change. For a view driven only by `ShowStars`, call `Bind(null)` at runtime (or clear the
  field in a scene without a `ScoreManager`).

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

`Bind(ScoreManager)` assigns the field and rebinds an already active view in one call (null only
unbinds); the older public `Awake()` does the same for the current field value. Repeated calls keep one
subscription. Merely assigning the field does not rebind. `ShowStars(int)` sets the slots directly and is
also a UnityEvent target for NoCode setups without a `ScoreManager`. Subscriptions continue
while the view component is disabled and are removed on destruction, including when the public manager
field has since changed. A surviving external star object is no longer updated after the view is destroyed.

## See Also

- [Module Root](../README.md)
