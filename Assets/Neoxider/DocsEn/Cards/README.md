# Cards module

Card game utilities: MVP (Model, View, Presenter), deck/hand/board components, poker combinations, and sample games (e.g. Drunkard). Scripts in `Scripts/Cards/`. Full documentation is in Russian; key entry points below.

## Entry pages

| Page | Description |
|------|-------------|
| [CardData](./CardData.md) | Core card value type and comparison helpers |
| [DeckConfig](./DeckConfig.md) | Deck visuals, generation rules, and validation |
| [CardComponent](./CardComponent.md) | Scene-facing card component, interaction, and animation |
| [HandComponent](./HandComponent.md) | Scene-facing hand, layout, events, and finite-hand notes |
| [Cards README](../../Docs/Cards/README.md) | Full Russian module documentation |

## Custom cards and standalone views

Cards are no longer limited to 36/52/54-card decks. Use `CardData.CreateCustom(...)` for TCG, deckbuilder, board-game, ability, or item cards. `DeckConfig` can generate a custom deck from `Custom Cards`, while `DeckModel.Initialize(IEnumerable<CardData>)` still accepts any explicit card list.

`CardComponent`, `CardView`, and `CardViewUniversal` can also run without a `DeckConfig` by calling `SetSpriteOverrides(faceSprite, backSprite)`. This keeps the card view reusable in standalone card projects where card identity, rules, and art come from another data model.

`BoardComponent` exposes `MaxCards`, `FaceUp`, `CanPlaceCard(...)`, `SetCapacity(...)`, and `SetFaceUp(...)` so one board component can support table rows, lanes, discard piles, market rows, or custom TCG zones.

## Finite hands and backpack rails

`HandModel` can now represent both unlimited card hands and finite card rails. Leave `Capacity` at `0` for the legacy unlimited behaviour, or set it to a positive value for CCG hand limits, autobattler benches, backpack rows, market rows, and draft trays.

Use `CanAdd(...)` or `TryAdd(...)` for player-facing flows where overflow should be rejected without exceptions. `Add(...)` and `AddRange(...)` keep strict behaviour and throw if a finite hand would overflow. `RemainingCapacity`, `IsFull`, and `AddRangeUntilFull(...)` are intended for UI badges, reward overflow conversion, and bulk draw/recruit flows.

## Main entry (Russian)

- [Cards README](../../Docs/Cards/README.md) — quick start, layout types, card comparison, dependencies

## Components (Russian)

| Page | Description |
|------|-------------|
| [CardData](../../Docs/Cards/CardData.md) | Card data structure |
| [DeckConfig](../../Docs/Cards/DeckConfig.md) | Deck configuration (36/52/54) |
| [CardComponent](../../Docs/Cards/CardComponent.md) | Card component |
| [DeckComponent](../../Docs/Cards/DeckComponent.md) | Deck component |
| [HandComponent](../../Docs/Cards/HandComponent.md) | Hand component |
| [BoardComponent](../../Docs/Cards/BoardComponent.md) | Board component |
| [View (CardView, DeckView, HandView)](../../Docs/Cards/View/CardView.md) | MVP view layer |
| [Poker](../../Docs/Cards/Poker/README.md) | Poker combinations and rules |
| [Drunkard example](../../Docs/Cards/Examples/Drunkard.md) | DrunkardGame sample |

## See also

- [Tools/Components](../Tools/Components/README.md)
- [Save](../Save/README.md)
