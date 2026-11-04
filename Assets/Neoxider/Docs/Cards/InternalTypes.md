# Cards Module — Internal Types

**What it is:** a one-page reference of the supporting enums, interfaces, models, presenters and utilities of the Cards module (`Scripts/Cards/`, namespace `Neo.Cards`).

**How to use:** look up a type here, then follow the linked component page or the XML docs in code for the full API.

---

## Config

### CardLayoutSettings
**Purpose:** Serializable layout settings for spacing, fan angles/radius, grids, stacks, scattering, and position/rotation jitter. `Default` supplies reusable starting values.

### CardSettingsRuntime
**Purpose:** Holds the optional `GlobalAnimationConfig` fallback. Decks may assign it at startup; it is shared runtime state, not a per-card layout instance. It is cleared at the start of every play session, so a config from a previous session never leaks in when domain reload is disabled.

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
**Purpose:** Draw/discard piles, shuffle, return, and reset. Explicit initialization snapshots composition; reset restores that snapshot including order/duplicates when shuffling is disabled. Shuffling uses an injectable `System.Random`: `new DeckModel(seed)`, `new DeckModel(random)`, the `ShuffleRandom` property, or a one-off `Shuffle(random)`.

### HandModel
**Purpose:** Player hand logical model (add, remove, stable sort). Supports `Capacity` (0 = unlimited), `RemainingCapacity`, `IsFull`, `TryAdd(...)`, and `AddRangeUntilFull(...)` for finite hands, benches, draft trays, and market rows. `Sort(IComparer<CardData>)`/`Sort(Comparison<CardData>)` accept any rule; queries allocate nothing, and so do `SortByRank`/`SortBySuit`/`Sort(Comparison)` on hands of up to 32 cards (larger hands use a merge sort with two temporary arrays; `Sort(IComparer)` creates one delegate per call).

## Poker

### PokerCombination
**Purpose:** Poker combination enum (`HighCard` – `RoyalFlush`).

### PokerHandEvaluator
**Purpose:** Best five-card hand (`Evaluate`) and allocation-free comparable strength (`EvaluateStrength`, `GetCombination`). See [PokerHandEvaluator](Poker/PokerHandEvaluator.md).

### PokerHandResult
**Purpose:** Poker hand evaluation result (combination, tie-break ranks, kickers, best hand). See [PokerHandResult](Poker/PokerHandResult.md).

### PokerRules
**Purpose:** Hand comparison, winners/split pots, Texas Hold'em and outs. See [PokerRules](Poker/PokerRules.md).

## Presenters

### CardPresenter
**Purpose:** Links `CardData` and `ICardView`, delegates interaction/animations, and reports the view's actual face state. Dispose detaches its click subscription.

### DeckPresenter
**Purpose:** Links `DeckModel`, `IDeckView`, `DeckConfig`, and a `CardView` prefab. Initialization honors standard and custom config generation. For a standard 36-card game deck the view shows the bottom (trump) card and refreshes it whenever the model changes (shuffle, reset, returns); otherwise the top card stays hidden.

### HandPresenter
**Purpose:** Links `HandModel`, ordered `CardPresenter` instances, and `IHandView`. Sort (`SortByRankAsync`, `SortBySuitAsync`, `SortAsync(comparer)`) and removal keep all three orders aligned; duplicate presenter insertion is ignored.

## Utils

### CardComparer
**Purpose:** Ready comparers: `ByRankAscending/Descending`, `BySuitThenRankAscending/Descending`, `WithTrump(suit)` for standard cards, and `ByValueAscending/Descending`, `ByGroupThenValueAscending` for custom cards (uses `CardData.CompareTo`).

### CardLayoutCalculator
**Purpose:** Local positions/rotations for `Fan`, `Line`, `Slots`, `Grid`, `Stack` and `Scattered` layouts. Scatter and jitter are deterministic per card index and optional seed (no `UnityEngine.Random` use), so re-layouts keep existing cards in place. List-filling overloads `CalculatePositions(type, count, settings, results, seed)` and `CalculateRotations(...)` reuse caller buffers without allocating.

### CardViewAnimationTemplates
**Purpose:** Animation preset templates for card visuals (DOTween presets).

## DrunkardGame
**Purpose:** Complete "War" (Drunkard) card game implementation — self-contained component with round logic.

## See Also
- [BoardComponent](BoardComponent.md) | [CardComponent](CardComponent.md) | [DeckComponent](DeckComponent.md) | [HandComponent](HandComponent.md)
- [CardData](CardData.md) | [DeckConfig](DeckConfig.md)
- ← [Cards](README.md)
