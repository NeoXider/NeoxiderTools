# Cards module

Card game utilities: MVP (Model, View, Presenter), deck/hand/board components, poker combinations, and sample games (e.g. Drunkard). Scripts in `Scripts/Cards/`. Key entry points below.

## Entry pages

| Page | Description |
|------|-------------|
| [CardData](./CardData.md) | Core card value type and comparison helpers |
| [DeckConfig](./DeckConfig.md) | Deck visuals, generation rules, and validation |
| [Cards editor](./Editor.md) | DeckConfig authoring, sprite auto-fill, previews, and validation |
| [CardComponent](./CardComponent.md) | Scene-facing card component, interaction, and animation |
| [HandComponent](./HandComponent.md) | Scene-facing hand, layout, events, and finite-hand notes |

## Custom cards and standalone views

Cards are no longer limited to 36/52/54-card decks. Use `CardData.CreateCustom(...)` for TCG, deckbuilder, board-game, ability, or item cards. `DeckConfig` can generate a custom deck from `Custom Cards`, while `DeckModel.Initialize(IEnumerable<CardData>)` still accepts any explicit card list.

`CardComponent`, `CardView`, and `CardViewUniversal` can also run without a `DeckConfig` by calling `SetSpriteOverrides(faceSprite, backSprite)`. This keeps the card view reusable in standalone card projects where card identity, rules, and art come from another data model.

`BoardComponent` exposes `MaxCards`, `FaceUp`, `CanPlaceCard(...)`, `SetCapacity(...)`, and `SetFaceUp(...)` so one board component can support table rows, lanes, discard piles, market rows, or custom TCG zones.

## Runtime models and reset

Models work without scene objects. The final list entry is the top of a `DeckModel`; `Draw()` removes it, while `PeekBottom()` reads the first entry.

```csharp
CardData wolf = CardData.CreateCustom("wolf", "Wolf", 3, "Beast");
CardData bear = CardData.CreateCustom("bear", "Bear", 8, "Beast");
DeckModel deck = new DeckModel();
deck.Initialize(new[] { wolf, bear, wolf }, shuffle: false);
CardData? drawn = deck.Draw();
deck.Reset(shuffle: false);
// Restores wolf, bear, wolf, including duplicate copies and their original order.
```

Explicit initialization snapshots its input before changing the deck, including deferred queries over `deck.Cards`. A null or failing source leaves the previous deck and discard pile intact. `Reset(...)` restores the last successful initialization and clears discards; a later `Initialize(DeckType, ...)` switches back to a standard deck. `DeckComponent` and `DeckPresenter` both generate their cards through `DeckConfig.GenerateDeck()`, including custom decks.

`BoardModel(maxCards)` enforces capacity through `Add(...)`, including calls through `ICardContainer`. Check `CanAdd(...)` before an expected rejection; `Add(...)` throws `InvalidOperationException` if the container refuses the card. Capacity zero accepts no cards; a negative board capacity is invalid. The legacy `CardContainerModel.Mutable` list bypasses validation and notifications, so use the typed mutation methods for normal gameplay.

## Scene and presenter ownership

Assign a `DeckConfig` and card prefab on `DeckComponent`, configure hand/board limits and layouts, then call the component APIs from C# or Inspector events. When using `HandComponent` or `HandPresenter`, change cards through that owner so model order, view order, and click subscriptions stay together. Directly changing its exposed `Model` does not create or remove scene views.

A hand or board ignores repeated insertion of the same scene card; `HandPresenter` and `HandView` also ignore the same presenter/view instance added twice. Separate instances with equal `CardData` remain valid duplicate copies. Remove a card from its current owner before transferring it to another zone. `HandComponent.TryAddCardAsync(...)` returns acceptance. Competing `DeckComponent.DealToHandAsync(...)` calls recheck admission after flight: rejection restores the drawn data and its stack view to the same source generation, without `OnCardDealt`. Reset/reinitialize cancels pending deals; old cards cannot be restored into a replacement deck. Accepted cards belong to the hand and survive a later deck-only reset.

`HandPresenter.SortByRankAsync(...)` and `SortBySuitAsync(...)` synchronize all three orders. The built-in `HandView` implements optional `IOrderedHandView.SetOrderAsync(...)` and applies one layout without remove/add callbacks. Existing custom `IHandView` implementations retain their nonanimated remove/add fallback and must preserve removed objects for reuse. Hand/board layouts cancel obsolete motion when membership changes. See [HandView](./View/HandView.md).

Presenter add/remove calls also honor a newer mutation made by a model callback: `Model.OnCardAdded`/`OnCardRemoved` can call `presenter.Clear()` without stale view insertion, stale indexed removal or click subscription. A superseded asynchronous call does not publish an old presenter add/remove notification after its custom view finishes. Ordinary completed calls keep their synchronous event order.

В 11.0 выдача возвращает карту в исходную колоду при отказе руки после перелёта; отменённая выдача не меняет новую раздачу. `RestartGame()` примера Drunkard можно вызывать во время хода: прежние анимации и продолжения отменяются до очистки состояния. Настройка ассетов описана в [Cards editor](./Editor.md), программный путь — в страницах компонентов.

`CardPresenter.IsFaceUp` reports the actual view state, including fixed display modes and direct view flips. If a face or back sprite cannot be resolved, built-in card renderers clear their previous sprite. A missing back therefore produces a blank card instead of exposing the old face.

Poker and suit/rank helpers implement classic-card rules. `PokerHandEvaluator` rejects custom cards and invalid suit/rank values, while ignoring jokers as before. Use game-specific rules for custom groups and sort values.

## Finite hands and backpack rails

`HandModel` can now represent both unlimited card hands and finite card rails. Leave `Capacity` at `0` for the legacy unlimited behaviour, or set it to a positive value for CCG hand limits, autobattler benches, backpack rows, market rows, and draft trays.

Use `CanAdd(...)` or `TryAdd(...)` for player-facing flows where overflow should be rejected without exceptions. `Add(...)` and `AddRange(...)` keep strict behaviour and throw if a finite hand would overflow. `RemainingCapacity`, `IsFull`, and `AddRangeUntilFull(...)` are intended for UI badges, reward overflow conversion, and bulk draw/recruit flows.

`AddRange(...)` snapshots its source, so `hand.AddRange(hand.Cards)` adds one copy of the original contents when capacity permits. `AddRangeUntilFull(...)` consumes its input incrementally and stops before advancing past available capacity; use an independent source when streaming cards into a hand.

## Components

| Page | Description |
|------|-------------|
| [CardData](./CardData.md) | Card data structure |
| [DeckConfig](./DeckConfig.md) | Standard and custom deck configuration |
| [CardComponent](./CardComponent.md) | Card component |
| [DeckComponent](./DeckComponent.md) | Deck component |
| [HandComponent](./HandComponent.md) | Hand component |
| [BoardComponent](./BoardComponent.md) | Board component |
| [View (CardView, DeckView, HandView)](./View/CardView.md) | MVP view layer |
| [Poker](./Poker/README.md) | Poker combinations and rules |
| [Drunkard example](./Examples/Drunkard.md) | DrunkardGame sample |

## See also

- [Tools/Components](../Tools/Components/README.md)
- [Save](../Save/README.md)
