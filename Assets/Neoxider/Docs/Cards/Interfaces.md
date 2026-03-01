# Интерфейсы карт (MVP, произвольные карточки)

Краткое описание контрактов для визуала карт и что переиспользовать в разных карточных играх.

---

## ICardView

Базовый интерфейс визуального представления одной карты.

- **Data**, **IsFaceUp**, **Transform**
- **SetData(CardData, bool faceUp)** — установка данных и стороны
- **Flip()**, **FlipAsync(float duration)** — переворот
- **MoveToAsync(Vector3 position, float duration)** — перемещение с анимацией
- **SetInteractable(bool)**
- События: **OnClicked**, **OnHovered**, **OnUnhovered**

Реализации: [CardView](./View/CardView.md), [CardViewUniversal](./View/CardViewUniversal.md), любая своя вью по [гайду](./View/CustomCardViewGuide.md).

---

## ICardDisplayMode (опционально)

Режим отображения: переворот по запросу или всегда открыта/закрыта.

- **Mode** — `CardDisplayMode`: `WithFlip` | `AlwaysFaceUp` | `AlwaysFaceDown`

Если интерфейс не реализован, считается `WithFlip`. Реализует [CardViewUniversal](./View/CardViewUniversal.md).

---

## ICardViewAnimations (опционально)

Воспроизведение готовых анимаций (разовые и зацикленные).

- **PlayOneShotAsync(CardViewAnimationType, float?, CancellationToken)** — разовая анимация
- **PlayLooped(CardViewAnimationType, float?)** — запуск зацикленной
- **StopLooped(CardViewAnimationType)**, **StopAllLooped()**

Типы анимаций: **CardViewAnimationType** (Bounce, Pulse, PulseLooped, Shake, Highlight, FlyIn, Idle). Реализует [CardViewUniversal](./View/CardViewUniversal.md); в своей вью можно вызывать [CardViewAnimationTemplates](./View/CardViewUniversal.md#переиспользование-шаблонов) напрямую.

---

## IHandView

Интерфейс контейнера карт (рука, зона).

- **CardViews**, **Count**
- **AddCardAsync(ICardView, bool animate)**, **RemoveCardAsync(ICardView, bool animate)**
- **ArrangeCardsAsync(bool animate)**, **Clear()**

Реализация: [HandView](./View/HandView.md). Для нескольких зон — несколько HandView или своих контейнеров по тому же принципу.

---

## IDeckView

Интерфейс визуала колоды (точка спавна, счётчик, козырь). Привязан к CardData для отображения верхней карты.

---

## Когда какой интерфейс реализовывать

| Сценарий | Рекомендация |
|----------|--------------|
| Классические карты (дурак, пьяница), no-code | CardComponent + HandComponent/DeckComponent/BoardComponent (без ICardView). |
| Классические карты с кодом (MVP) | ICardView (CardView или CardViewUniversal) + HandView + CardPresenter. |
| Произвольные карты, свои анимации и режимы | CardViewUniversal (ICardView + ICardDisplayMode + ICardViewAnimations). |
| Своя модель карты (CCG, roguelike) | Свой интерфейс вью + свои данные; анимации и раскладки — [CardViewAnimationTemplates](View/CardViewUniversal.md#переиспользование-шаблонов), [CardLayoutCalculator](../Scripts/Cards/Utils/CardLayoutCalculator.cs). |

---

## См. также

- [Что переиспользовать в разных карточных играх](./README.md) (раздел в README)
- [CardViewUniversal](./View/CardViewUniversal.md) — режимы, анимации, переиспользование шаблонов
- [CustomCardViewGuide](./View/CustomCardViewGuide.md) — пошаговая своя реализация карты
