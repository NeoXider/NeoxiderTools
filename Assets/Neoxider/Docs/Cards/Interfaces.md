# Card Interfaces (MVP, arbitrary cards)

**What it is:** contracts for card visuals: ICardView (data, flipping, movement, clicks), IHandView, IDeckView; optionally ICardDisplayMode, ICardViewAnimations. Used by CardView, CardViewUniversal, HandView, DeckView, and custom views.

**How to use:** implement the interfaces in your own components or use the ready-made views; see the tables for each interface below.

---

## ICardView

Base interface for the visual representation of a single card.

- **Data**, **IsFaceUp**, **Transform**
- **SetData(CardData, bool faceUp)** — set the data and the facing side
- **Flip()**, **FlipAsync(float duration)** — flipping
- **MoveToAsync(Vector3 position, float duration)** — animated movement
- **SetInteractable(bool)**
- Events: **OnClicked**, **OnHovered**, **OnUnhovered**

Implementations: [CardView](./View/CardView.md), [CardViewUniversal](./View/CardViewUniversal.md), or any custom view following the [guide](./View/CustomCardViewGuide.md).

---

## ICardDisplayMode (optional)

Display mode: flip on demand or always face up/face down.

- **Mode** — `CardDisplayMode`: `WithFlip` | `AlwaysFaceUp` | `AlwaysFaceDown`

If the interface is not implemented, `WithFlip` is assumed. Implemented by [CardViewUniversal](./View/CardViewUniversal.md).

---

## ICardViewAnimations (optional)

Playback of ready-made animations (one-shot and looped).

- **PlayOneShotAsync(CardViewAnimationType, float?, CancellationToken)** — one-shot animation
- **PlayLooped(CardViewAnimationType, float?)** — start a looped animation
- **StopLooped(CardViewAnimationType)**, **StopAllLooped()**

Animation types: **CardViewAnimationType** (Bounce, Pulse, PulseLooped, Shake, Highlight, FlyIn, Idle). Implemented by [CardViewUniversal](./View/CardViewUniversal.md); in a custom view you can call [CardViewAnimationTemplates](./View/CardViewUniversal.md#переиспользование-шаблонов) directly.

---

## IHandView

Interface of a card container (hand, zone).

- **CardViews**, **Count**
- **AddCardAsync(ICardView, bool animate)**, **RemoveCardAsync(ICardView, bool animate)**
- **ArrangeCardsAsync(bool animate)**, **Clear()**

Implementation: [HandView](./View/HandView.md). For multiple zones — use several HandViews or your own containers following the same approach.

---

## IDeckView

Interface for deck visuals (spawn point, counter, trump card). Bound to CardData for displaying the top card.

---

## Which Interface to Implement When

| Scenario | Recommendation |
|----------|--------------|
| Classic cards (Durak, War/Drunkard), inspector setup | CardComponent + HandComponent/DeckComponent/BoardComponent (no ICardView). |
| Classic cards with code (MVP) | ICardView (CardView or CardViewUniversal) + HandView + CardPresenter. |
| Arbitrary cards, custom animations and modes | CardViewUniversal (ICardView + ICardDisplayMode + ICardViewAnimations). |
| Custom card model (CCG, roguelike) | Your own view interface + your own data; animations and layouts — [CardViewAnimationTemplates](View/CardViewUniversal.md#переиспользование-шаблонов), [CardLayoutCalculator](../../Scripts/Cards/Utils/CardLayoutCalculator.cs). |

---

## See Also

- [What to reuse across different card games](./README.md) (section in the README)
- [CardViewUniversal](./View/CardViewUniversal.md) — modes, animations, template reuse
- [CustomCardViewGuide](./View/CustomCardViewGuide.md) — step-by-step custom card implementation
