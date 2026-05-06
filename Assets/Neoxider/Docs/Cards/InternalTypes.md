# Cards Module — Internal Types

## Config

### CardLayoutSettings
**Назначение:** Конфигурация расположения карт (отступы, углы веера, масштаб).

### CardSettingsRuntime
**Назначение:** Рантайм-настройки карт, создаваемые из `CardLayoutSettings` при запуске.

### HandLayoutType
**Назначение:** Enum/конфигурация типа раскладки руки (веер, линия, стек).

## Enums

### BoardMode
**Назначение:** Режим игрового поля (`FreePlay`, `Rules`).

### CardDisplayMode
**Назначение:** Режим отображения карты (`FaceUp`, `FaceDown`, `Highlighted`).

### CardLocation
**Назначение:** Местоположение карты (`Deck`, `Hand`, `Board`, `Discard`).

### CardViewAnimationType
**Назначение:** Тип анимации для визуала карты (`Flip`, `Slide`, `Scale`, и т.д.).

### DeckType
**Назначение:** Тип колоды (`Standard52`, `Standard36`, `Custom`).

### Rank
**Назначение:** Ранг карты (`Ace` – `King`, плюс `Joker`).

### ShuffleVisualType
**Назначение:** Визуальный стиль перемешивания (`Riffle`, `Overhand`, `None`).

### StackZSortingStrategy
**Назначение:** Стратегия сортировки карт по Z (`ByOrder`, `ByIndex`).

### Suit
**Назначение:** Масть карты (`Hearts`, `Diamonds`, `Clubs`, `Spades`).

## Interfaces

### ICardContainer
**Назначение:** Интерфейс контейнера карт (Add, Remove, Contains).

### ICardDisplayMode
**Назначение:** Интерфейс управления режимом отображения карт.

### ICardView
**Назначение:** Интерфейс визуала карты (SetData, Flip, Highlight, SetInteractable).

### ICardViewAnimations
**Назначение:** Интерфейс анимаций для визуала карты.

### IDeckView
**Назначение:** Интерфейс визуала колоды (Draw, Shuffle, Count).

### IHandView
**Назначение:** Интерфейс визуала руки (Add, Remove, Sort, Fan).

## Models

### CardContainerModel
**Назначение:** Базовая логическая модель контейнера карт.

### BoardModel
**Назначение:** Логическая модель игрового поля.

### DeckModel
**Назначение:** Логическая модель колоды (список карт, перемешивание, выдача).

### HandModel
**Назначение:** Логическая модель руки игрока (добавление, удаление, сортировка).

## Poker

### PokerCombination
**Назначение:** Enum покерных комбинаций (`HighCard` – `RoyalFlush`).

### PokerHandEvaluator
**Назначение:** Класс оценки покерной руки (определение комбинации и силы).

### PokerHandResult
**Назначение:** Результат оценки покерной руки (комбинация, кикеры).

### PokerRules
**Назначение:** Правила покера (сравнение рук, определение победителя).

## Presenters

### CardPresenter
**Назначение:** Презентер карты — связывает `CardComponent` с `ICardView`.

### DeckPresenter
**Назначение:** Презентер колоды — связывает `DeckComponent` с `IDeckView`.

### HandPresenter
**Назначение:** Презентер руки — связывает `HandComponent` с `IHandView`.

## Utils

### CardComparer
**Назначение:** Компаратор карт (сортировка по масти, рангу, кастомным правилам).

### CardLayoutCalculator
**Назначение:** Вычислитель позиций карт для раскладок (веер, линия, сетка).

### CardViewAnimationTemplates
**Назначение:** Шаблоны анимаций для визуалов карт (набор пресетов DOTween).

## DrunkardGame
**Назначение:** Полная реализация карточной игры "Пьяница" (DrunkardGame) — самодостаточный компонент с логикой раундов.

## См. также
- [BoardComponent](BoardComponent.md) | [CardComponent](CardComponent.md) | [DeckComponent](DeckComponent.md) | [HandComponent](HandComponent.md)
- [CardData](CardData.md) | [DeckConfig](DeckConfig.md)
- ← [Cards](README.md)
