# HandView

**Purpose:** `IHandView` implementation that owns an ordered list of card views and lays them out independently of card-game rules.

## Setup

- Add the component via the Unity menu.
- Set layout, spacing, and animation fields. Add views directly with `AddCardAsync(...)`, or construct `HandPresenter(new HandModel(), handView)` and manage cards through that presenter.

## Runtime contract

`CardViews` is the view's membership and layout order. `AddCardAsync(...)` adds a view once; adding the same instance again is ignored. Distinct instances with equal card data are allowed. `RemoveCardAsync(...)` removes membership without destroying the card; the caller owns its next destination and lifetime. `Clear()` clears membership without destroying card objects.

`HandPresenter.SortByRankAsync(...)` and `SortBySuitAsync(...)` reorder model, presenter list, and view membership before arranging. This also keeps index-based removal aligned after sorting. Built-in `HandView` implements the optional `IOrderedHandView` capability:

```csharp
IOrderedHandView ordered = handView;
await ordered.SetOrderAsync(new ICardView[] { second, first }, animate: true, cancellation: token);
```

The supplied order must be an exact permutation of current members. Null, missing, foreign or duplicate members are rejected before mutation. Passing `handView.CardViews` itself is safe. Order commits before animation; cancellation stops its motion and preserves that order. The operation performs one layout pass, does not reparent cards and does not simulate membership callbacks. Layout replacement/removal/clear cancels old tweens before another owner receives a card.

Custom views can implement `IOrderedHandView` to receive this single call. Existing `IHandView` implementations retain the nonanimated remove/add fallback followed by the final arrangement; they must retain supplied objects for reuse. If a callback clears or changes the presenter during fallback synchronization, the obsolete pass stops rather than repopulating old members. No measured FPS claim is made for either path.

`HandPresenter.AddCardAsync` and `RemoveCardAsync` check for supersession after model callbacks and after awaiting the view. A model listener's `Clear()` therefore wins before stale list/index/subscription work, and delayed completion cannot publish an old add/remove notification for a replacement admission. Custom views remain responsible for cancelling their own stale visual work; no new mandatory interface member was added.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `CardViews` | Card Views. |
| `Count` | Count. |
| `LayoutType` | Layout Type. |
| `Spacing` | Spacing. |
| `_arrangeDuration` | Arrange Duration. |
| `_arrangeEase` | Arrange Ease. |
| `_layoutType` | Layout Type. |

## See Also

- [Module Root](../README.md)
