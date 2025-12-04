# Poker — покерные правила

Модуль для определения покерных комбинаций и сравнения рук.

---

## Комбинации

| Комбинация | Описание | Пример |
|------------|----------|--------|
| `HighCard` | Старшая карта | A♠ K♥ 9♦ 7♣ 2♠ |
| `Pair` | Пара | K♠ K♥ 9♦ 7♣ 2♠ |
| `TwoPair` | Две пары | K♠ K♥ 9♦ 9♣ 2♠ |
| `ThreeOfAKind` | Тройка | K♠ K♥ K♦ 7♣ 2♠ |
| `Straight` | Стрит | 9♠ 8♥ 7♦ 6♣ 5♠ |
| `Flush` | Флеш | A♠ K♠ 9♠ 7♠ 2♠ |
| `FullHouse` | Фулл хаус | K♠ K♥ K♦ 9♣ 9♠ |
| `FourOfAKind` | Каре | K♠ K♥ K♦ K♣ 2♠ |
| `StraightFlush` | Стрит-флеш | 9♠ 8♠ 7♠ 6♠ 5♠ |
| `RoyalFlush` | Роял-флеш | A♠ K♠ Q♠ J♠ 10♠ |

---

## Быстрый старт

```csharp
using Neo.Cards;
using Neo.Cards.Poker;

// Оценка руки из 5-7 карт
var cards = new List<CardData>
{
    new(Suit.Hearts, Rank.Ace),
    new(Suit.Hearts, Rank.King),
    new(Suit.Hearts, Rank.Queen),
    new(Suit.Hearts, Rank.Jack),
    new(Suit.Hearts, Rank.Ten)
};

var result = PokerHandEvaluator.Evaluate(cards);
Debug.Log(result.Combination); // RoyalFlush
Debug.Log(result.ToString());  // "Роял-флеш (A)"
```

---

## PokerHandEvaluator

Определяет лучшую комбинацию из 5-7 карт.

```csharp
// Оценка 5 карт
var result = PokerHandEvaluator.Evaluate(fiveCards);

// Оценка 7 карт (Texas Hold'em) — автоматически выбирает лучшие 5
var result = PokerHandEvaluator.Evaluate(sevenCards);
```

---

## PokerHandResult

Результат оценки руки.

### Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Combination` | `PokerCombination` | Тип комбинации |
| `CombinationRanks` | `IReadOnlyList<Rank>` | Ранги в комбинации |
| `Kickers` | `IReadOnlyList<Rank>` | Кикеры |
| `BestHand` | `IReadOnlyList<CardData>` | Лучшие 5 карт |

### Сравнение

```csharp
if (result1.IsStrongerThan(result2))
    Debug.Log("Первая рука сильнее");

if (result1.IsEqualTo(result2))
    Debug.Log("Руки равны");

// Или через CompareTo
int compare = result1.CompareTo(result2);
```

---

## PokerRules

Утилиты для определения победителя.

### CompareHands

```csharp
int result = PokerRules.CompareHands(hand1, hand2);
// > 0 — hand1 сильнее
// < 0 — hand2 сильнее
// = 0 — равны (split pot)
```

### GetWinners

```csharp
var hands = new List<PokerHandResult> { result1, result2, result3 };
List<int> winners = PokerRules.GetWinners(hands);
// Может вернуть несколько индексов при split pot
```

### GetWinnersTexasHoldem

```csharp
// communityCards — 5 карт на столе
// playerHoleCards — по 2 карты у каждого игрока
var winners = PokerRules.GetWinnersTexasHoldem(
    communityCards,
    playerHoleCards
);
```

### EvaluateTexasHoldem

```csharp
var result = PokerRules.EvaluateTexasHoldem(
    communityCards,  // 5 карт на столе
    holeCards        // 2 карты игрока
);
```

### CountOuts

```csharp
// Подсчёт аутсов для улучшения руки
int outs = PokerRules.CountOuts(
    currentHand,
    PokerCombination.Flush,
    remainingDeck
);
Debug.Log($"Аутсов на флеш: {outs}");
```

---

## Пример: Полная игра

```csharp
public class PokerGame : MonoBehaviour
{
    [SerializeField] private DeckComponent _deck;
    [SerializeField] private BoardComponent _board;
    [SerializeField] private HandComponent[] _playerHands;

    private List<List<CardData>> _holeCards = new();

    public async void StartGame()
    {
        _deck.Initialize();
        _holeCards.Clear();

        // Раздача по 2 карты каждому игроку
        foreach (var hand in _playerHands)
        {
            var cards = new List<CardData>();
            for (int i = 0; i < 2; i++)
            {
                var card = _deck.DrawCard(faceUp: false);
                await hand.AddCardAsync(card);
                cards.Add(card.Data);
            }
            _holeCards.Add(cards);
        }
    }

    public async void DealFlop()
    {
        for (int i = 0; i < 3; i++)
        {
            var card = _deck.DrawCard(faceUp: true);
            await _board.PlaceCardAsync(card);
        }
    }

    public async void DealTurn() => 
        await _board.PlaceCardAsync(_deck.DrawCard(true));

    public async void DealRiver() => 
        await _board.PlaceCardAsync(_deck.DrawCard(true));

    public void ShowWinner()
    {
        var communityCards = _board.GetAllCardData();
        var winners = PokerRules.GetWinnersTexasHoldem(communityCards, _holeCards);

        foreach (int winnerIndex in winners)
        {
            Debug.Log($"Победитель: Игрок {winnerIndex + 1}");
            
            var result = PokerRules.EvaluateTexasHoldem(
                communityCards, 
                _holeCards[winnerIndex]
            );
            Debug.Log($"Комбинация: {result}");
        }
    }
}
```

---

## См. также

- [CardData](../CardData.md)
- [BoardComponent](../BoardComponent.md)
- [HandComponent](../HandComponent.md)

