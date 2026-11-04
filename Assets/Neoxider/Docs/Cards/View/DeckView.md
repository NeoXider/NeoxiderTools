# DeckView

**What it is:** a minimal deck view (`MonoBehaviour`, `Scripts/Cards/View/DeckView.cs`, namespace `Neo.Cards`)
implementing `IDeckView`: it shows the deck back while cards remain, optionally shows a face-up top card (for
example the trump), and provides the spawn point for drawn cards.

**How to use:**
1. Add the component (menu `Neoxider/Cards/DeckView`) and assign a [DeckConfig](../DeckConfig.md) plus a deck
   **Image** or **Sprite Renderer**; add a top-card Image/Sprite Renderer if the trump should be visible.
2. Pass it to `new DeckPresenter(model, deckView, config, cardPrefab)`; the presenter calls `UpdateVisual`,
   `ShowTopCard` and `HideTopCard` as the deck changes and spawns cards at `SpawnPoint`.
3. Without a presenter, call `UpdateVisual(remainingCount)` and `ShowTopCard(card)` yourself.

---

## Fields

| Header | Field | Description |
|--------|-------|-------------|
| References | Spawn Point | Where drawn cards appear; defaults to this transform. |
| References | Deck Image / Deck Sprite | UI or world renderer for the deck back (`DeckConfig.BackSprite`). |
| References | Top Card Image / Top Card Sprite | UI or world renderer for the face-up top card. |
| Settings | Visible Card Count | Count passed to `UpdateVisual` in `Start`; clamped to `0` or more. |
| Settings | Card Offset | Serialized for custom stack layouts; the built-in view does not read it. |
| Config | Config | `DeckConfig` that supplies the back and face sprites; setting `Config` refreshes the view. |

## Methods

| Member | Description |
|--------|-------------|
| `void UpdateVisual(int remainingCount)` | Shows the deck back while `remainingCount > 0`, hides it otherwise. |
| `void ShowTopCard(CardData card)` / `void HideTopCard()` | Shows or hides the face-up top card. |
| `Transform SpawnPoint` | Spawn transform for drawn cards. |

The scene-level [DeckComponent](../DeckComponent.md) has its own visual stack and trump display and does not need
this view.

## See also

- [CardView](CardView.md), [HandView](HandView.md)
- [Cards module](../README.md)
