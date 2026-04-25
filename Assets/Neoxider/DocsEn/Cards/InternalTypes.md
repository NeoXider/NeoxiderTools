# Cards Module — Internal Types

## Config

### CardLayoutSettings
**Purpose:** Card layout configuration (spacing, fan angles, scale).

### CardSettingsRuntime
**Purpose:** Runtime card settings created from `CardLayoutSettings` on startup.

### HandLayoutType
**Purpose:** Enum/config for hand layout type (fan, line, stack).

## Enums

### BoardMode
**Purpose:** Board mode (`FreePlay`, `Rules`).

### CardDisplayMode
**Purpose:** Card display mode (`FaceUp`, `FaceDown`, `Highlighted`).

### CardLocation
**Purpose:** Card location (`Deck`, `Hand`, `Board`, `Discard`).

### CardViewAnimationType
**Purpose:** Animation type for card visuals (`Flip`, `Slide`, `Scale`, etc.).

### DeckType
**Purpose:** Deck type (`Standard52`, `Standard36`, `Custom`).

### Rank
**Purpose:** Card rank (`Ace` – `King`, plus `Joker`).

### ShuffleVisualType
**Purpose:** Visual shuffle style (`Riffle`, `Overhand`, `None`).

### StackZSortingStrategy
**Purpose:** Z-sorting strategy for stacked cards (`ByOrder`, `ByIndex`).

### Suit
**Purpose:** Card suit (`Hearts`, `Diamonds`, `Clubs`, `Spades`).

## Interfaces

### ICardContainer
**Purpose:** Card container interface (Add, Remove, Contains).

### ICardDisplayMode
**Purpose:** Interface for managing card display mode.

### ICardView
**Purpose:** Card visual interface (SetData, Flip, Highlight, SetInteractable).

### ICardViewAnimations
**Purpose:** Card visual animation interface.

### IDeckView
**Purpose:** Deck visual interface (Draw, Shuffle, Count).

### IHandView
**Purpose:** Hand visual interface (Add, Remove, Sort, Fan).

## Models

### CardContainerModel
**Purpose:** Base logical model for card containers.

### BoardModel
**Purpose:** Board logical model.

### DeckModel
**Purpose:** Deck logical model (card list, shuffle, deal).

### HandModel
**Purpose:** Player hand logical model (add, remove, sort).

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
**Purpose:** Card presenter — links `CardComponent` with `ICardView`.

### DeckPresenter
**Purpose:** Deck presenter — links `DeckComponent` with `IDeckView`.

### HandPresenter
**Purpose:** Hand presenter — links `HandComponent` with `IHandView`.

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
