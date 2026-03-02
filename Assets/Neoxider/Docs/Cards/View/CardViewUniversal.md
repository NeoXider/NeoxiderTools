# CardViewUniversal

**Что это:** универсальная вью карты: реализует ICardView, ICardDisplayMode, ICardViewAnimations. Режимы отображения (переворот / всегда открыта / всегда закрыта), готовые анимации через CardViewAnimationTemplates. Пространство имён `Neo.Cards`, файл `Scripts/Cards/View/CardViewUniversal.cs`.

**Как использовать:** добавить на префаб карты вместо или вместе с CardView; задать Display Mode и при необходимости шаблоны анимаций; вызывать SetData, Flip, MoveToAsync из кода или через карточную систему. См. секции ниже.

---

## Режимы отображения (Display Mode)

| Режим | Поведение |
|-------|-----------|
| **WithFlip** | Переворот по запросу (Flip/FlipAsync). Лицо/рубашка по SetData(faceUp). |
| **AlwaysFaceUp** | Всегда показывать лицо. Flip/FlipAsync — no-op. |
| **AlwaysFaceDown** | Всегда показывать рубашку. Flip/FlipAsync — no-op. |

В инспекторе: **Display** → **Display Mode**.

---

## Анимации

- **Разовые:** Bounce, Pulse, Shake, Highlight — через `PlayOneShotAsync(type, duration?, cancellation)`.
- **Зацикленные:** PulseLooped, Idle — через `PlayLooped(type, duration?)`; остановка — `StopLooped(type)` или `StopAllLooped()`.

Параметры (длительность, интенсивность) задаются в **Card Animation Config** (секция "Card View") или в полях компонента.

---

## Инспектор

- **Visual** — Card Image, Sprite Renderer (как у CardView).
- **Display** — Display Mode.
- **Animation** — Flip/Move duration, Ease, опционально **Card Animation Config**.
- **Hover** — Scale, Duration, Y Offset.

Инициализация: **Initialize(DeckConfig)** (как у CardView).

---

## Переиспользование шаблонов

`CardViewAnimationTemplates` (`Scripts/Cards/View/CardViewAnimationTemplates.cs`) — статический класс с готовыми анимациями. Можно вызывать из **любой** вью (в т.ч. своей), не только из CardViewUniversal.

Пример из своей вью:

```csharp
// Разовая анимация
CardViewAnimationTemplates.Bounce(transform, 0.25f, 0.15f, animationConfig);

// Зацикленная (сохраните Tween и вызовите Kill() при необходимости)
Tween idleTween = CardViewAnimationTemplates.Idle(transform, 1.2f, 0.02f, animationConfig);
```

Методы: **Bounce**, **Pulse**, **PulseLooped**, **Shake**, **Highlight**, **HighlightGraphic**, **FlyIn**, **Idle**. Перегрузки с явными параметрами или с **CardAnimationConfig**.

---

## См. также

- [Interfaces](../Interfaces.md) — ICardView, ICardDisplayMode, ICardViewAnimations
- [CardView](./CardView.md) — простая вью без режимов и набора анимаций
- [CustomCardViewGuide](./CustomCardViewGuide.md) — как сделать свою реализацию карты
