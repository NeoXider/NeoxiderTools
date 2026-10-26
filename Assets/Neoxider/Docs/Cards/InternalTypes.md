# Cards Module — Internal Types

## Config

### CardLayoutSettings
**Purpose:** Serializable layout settings for spacing, fan angles/radius, grids, stacks, scattering, and position/rotation jitter. `Default` supplies reusable starting values.

### CardSettingsRuntime
**Purpose:** Holds the optional `GlobalAnimationConfig` fallback. Decks may assign it at startup; it is shared runtime state, not a per-card layout instance.

### HandLayoutType
**Purpose:** Obsolete compatibility enum. Use `CardLayoutType`: `Fan`, `Line`, `Stack`, `Grid`, `Slots`, `Scattered`.

## Enums

### BoardMode
**Purpose:** `Table` uses the selected layout; `Beat` resolves to a scattered pile.

### CardDisplayMode
**Purpose:** `WithFlip`, `AlwaysFaceUp`, or `AlwaysFaceDown` for views implementing `ICardDisplayMode`.

### CardLocation
**Purpose:** Card location (`Deck`, `Hand`, `Board`, `Discard`, `Pool`).

### CardViewAnimationType
**Purpose:** `Bounce`, `Pulse`, `PulseLooped`, `Shake`, `Highlight`, `FlyIn`, and `Idle`. A fly-in template requires an explicit source position; the generic one-shot dispatcher does not supply one.

### DeckType
**Purpose:** `Standard36`, `Standard52`, or `Standard54`. Custom decks use explicit `CardData` lists or `DeckConfig.UseCustomDeck`, without a separate enum value.

### Rank
**Purpose:** Standard ranks from `Two = 2` through `Ace = 14`. Jokers and custom cards are distinguished by `CardData` flags, not rank enum members.

### ShuffleVisualType
**Purpose:** `None`, `Shake`, `Cut`, or `Riffle`. Cut/riffle currently provide timing placeholders rather than physical shuffle motion.

### StackZSortingStrategy
**Purpose:** `TopCardFirst` or `BottomCardFirst` sibling ordering.

### Suit
**Purpose:** Card suit (`Hearts`, `Diamonds`, `Clubs`, `Spades`).

## Interfaces

### ICardContainer
**Purpose:** `Location`, `Count`, `Data`, `OnChanged`, `CanAdd`, `Add`, `Remove`, `RemoveAll`, and `Clear`. Concrete models supply game-independent container behaviour.

### ICardDisplayMode
**Purpose:** Exposes the view's read-only `Mode`.

### ICardView
**Purpose:** Data, face state, transform, pointer events, `SetData`, `Flip`/`FlipAsync`, `MoveToAsync`, and `SetInteractable`. Extra animation presets belong to `ICardViewAnimations`.

### ICardViewAnimations
**Purpose:** Card visual animation interface.

### IDeckView
**Purpose:** Spawn point, visible-card setting, `UpdateVisual`, `ShowTopCard`, and `HideTopCard`. Drawing/shuffling belong to the model or owning deck adapter.

### IHandView
**Purpose:** Ordered `CardViews`, `Count`, async add/remove/arrange, and `Clear`. Existing custom views retain membership-based sorting; removal keeps views alive for reuse.

### IOrderedHandView
**Purpose:** Optional `IHandView` extension with `SetOrderAsync(IReadOnlyList<ICardView>, bool, CancellationToken)`. Built-in HandView validates a complete member permutation, commits its order and performs one layout without remove/add callbacks. Existing custom IHandView implementations need no new members.

## Models

### CardContainerModel
**Purpose:** Base logical model with change notifications. `Add` enforces `CanAdd`; legacy `Mutable` access bypasses that contract and notifications.

### BoardModel
**Purpose:** Logical container with a nonnegative finite maximum. `Add` throws on capacity rejection; zero capacity accepts no cards.

### DeckModel
**Purpose:** Draw/discard piles, shuffle, return, and reset. Explicit initialization snapshots composition; reset restores that snapshot including order/duplicates when shuffling is disabled.

### HandModel
**Purpose:** Player hand logical model (add, remove, sort). Supports `Capacity` (0 = unlimited), `RemainingCapacity`, `IsFull`, `TryAdd(...)`, and `AddRangeUntilFull(...)` for finite hands, benches, draft trays, and market rows.

## Poker

### PokerCombination
**Purpose:** Poker combination enum (`HighCard` – `RoyalFlush`).

### PokerHandEvaluator
**Purpose:** Poker hand evaluator (determines combination and strength).

### PokerHandResult
**Purpose:** Poker hand evaluation result (combination, kickers).

### PokerRules
**Purpose:** Poker rules (hand comparison, winner determination).

## Presenters

### CardPresenter
**Purpose:** Links `CardData` and `ICardView`, delegates interaction/animations, and reports the view's actual face state. Dispose detaches its click subscription.

### DeckPresenter
**Purpose:** Links `DeckModel`, `IDeckView`, `DeckConfig`, and a `CardView` prefab. Initialization honors standard and custom config generation.

### HandPresenter
**Purpose:** Links `HandModel`, ordered `CardPresenter` instances, and `IHandView`. Sort/removal keep all three orders aligned; duplicate presenter insertion is ignored.

## Utils

### CardComparer
**Purpose:** Card comparer (sort by suit, rank, custom rules).

### CardLayoutCalculator
**Purpose:** Card position calculator for layouts (fan, line, grid).

### CardViewAnimationTemplates
**Purpose:** Animation preset templates for card visuals (DOTween presets).

## DrunkardGame
**Purpose:** Complete "War" (Drunkard) card game implementation — self-contained component with round logic.

## See Also
- [BoardComponent](BoardComponent.md) | [CardComponent](CardComponent.md) | [DeckComponent](DeckComponent.md) | [HandComponent](HandComponent.md)
- [CardData](CardData.md) | [DeckConfig](DeckConfig.md)
- ← [Cards](README.md)
