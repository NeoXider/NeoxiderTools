# CardView

**What it is:** the default card view (`MonoBehaviour`, `Scripts/Cards/View/CardView.cs`, namespace `Neo.Cards`)
implementing `ICardView`: it shows a card face or back on an `Image` or a `SpriteRenderer`, flips, moves and
rotates with DOTween, and raises click/hover events through the EventSystem. For display modes and animation
presets use [CardViewUniversal](CardViewUniversal.md).

**How to use:**
1. Put the component on a card prefab and assign **Card Image** (UI) or **Sprite Renderer** (world).
2. Call `Initialize(deckConfig)` so faces and backs resolve from a [DeckConfig](../DeckConfig.md), or
   `SetSpriteOverrides(face, back)` when the art comes from your own data.
3. Call `SetData(card, faceUp)` to show a card, then drive it with `FlipAsync`, `MoveToAsync`,
   `MoveToLocalAsync` and `RotateToAsync`, or hand it to a `CardPresenter` / [HandView](HandView.md).
4. Subscribe to `OnClicked`, `OnHovered` and `OnUnhovered`; `SetInteractable(false)` mutes them.

---

## Fields

| Header | Field | Default | Description |
|--------|-------|---------|-------------|
| Visual | Card Image | — | UI `Image` that shows the sprite. |
| Visual | Sprite Renderer | — | World `SpriteRenderer` alternative. |
| Visual | Face Sprite Override, Back Sprite Override | — | Optional sprites used without a `DeckConfig`. |
| Animation | Flip Duration, Move Duration | `0.3`, `0.2` | Default tween lengths in seconds. |
| Animation | Flip Ease, Move Ease | `OutQuad` | DOTween easing. |
| Hover | Hover Scale | `0.1` | Scale increase on hover (`0.1` = 10%). |
| Hover | Hover Duration, Hover Y Offset | `0.15`, `20` | Hover tween length and lift. |

## Properties and events

| Member | Description |
|--------|-------------|
| `CardData Data` | Card currently shown. |
| `bool IsFaceUp` | Whether the face is visible. |
| `Transform Transform` | The view's transform. |
| `event Action<ICardView> OnClicked / OnHovered / OnUnhovered` | Pointer events while interactable. |

## Standalone use

Call `SetSpriteOverrides(faceSprite, backSprite)` when the card art comes from a custom TCG/deckbuilder data model instead of `DeckConfig`. `ClearSpriteOverrides()` returns lookup to the assigned `DeckConfig`.

Hover tweens are owned by the view: repeated hover/exit kills previous tweens, and destroying the GameObject clears active hover motion. A UI card moves by `anchoredPosition`; a world card moves its transform.

The existing `ICardView` API remains unchanged. Concrete `CardView` additionally accepts a `CancellationToken` as the last argument of `FlipAsync(float, CancellationToken)`, `MoveToAsync(Vector3, float, CancellationToken)`, `MoveToLocalAsync(Vector3, float, CancellationToken)` and `RotateToAsync(Quaternion, float, CancellationToken)`. Rotation retains its existing world-space behavior.

Move/flip/rotate operations own their exact awaited tween. A replacement operation or destruction cancels the previous one; caller cancellation kills that operation's tween and reports `OperationCanceledException`. An interrupted flip restores its full scale, and an old continuation cannot write a new operation's transform. HandView separately owns its layout tweens and cancels them on membership/order changes.

## See also

- [CardViewUniversal](CardViewUniversal.md), [HandView](HandView.md), [DeckView](DeckView.md)
- [Custom card view guide](CustomCardViewGuide.md)
- [Cards module](../README.md)
