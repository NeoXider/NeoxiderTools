# Своя реализация карты (Custom Card View)

**Что это:** Пошаговая инструкция: как сделать свою вью карты и подключить её к системе (HandView, анимации, раскладки).

**Как использовать:** см. разделы ниже.

---


Пошаговая инструкция: как сделать свою вью карты и подключить её к системе (HandView, анимации, раскладки).

---

## Шаг 1. Минимальный ICardView

Реализуйте интерфейс [ICardView](../Interfaces.md): Data, IsFaceUp, Transform, SetData, Flip, FlipAsync, MoveToAsync, SetInteractable, события OnClicked/OnHovered/OnUnhovered.

Пример скелета:

```csharp
public class MyCardView : MonoBehaviour, ICardView
{
    public CardData Data { get; private set; }
    public bool IsFaceUp { get; private set; }
    public Transform Transform => transform;
    public event Action<ICardView> OnClicked;
    public event Action<ICardView> OnHovered;
    public event Action<ICardView> OnUnhovered;

    public void SetData(CardData data, bool faceUp = true)
    {
        Data = data;
        IsFaceUp = faceUp;
        // обновить визуал (спрайт, текст и т.д.)
    }

    public void Flip()
    {
        IsFaceUp = !IsFaceUp;
        // обновить визуал
    }

    public async UniTask FlipAsync(float duration = 0.3f)
    {
        // анимация переворота (например scaleX 0 -> 1 и смена спрайта)
        IsFaceUp = !IsFaceUp;
        await UniTask.CompletedTask;
    }

    public async UniTask MoveToAsync(Vector3 position, float duration = 0.2f)
    {
        var tween = transform.DOMove(position, duration).SetEase(Ease.OutQuad);
        await UniTask.WaitUntil(() => !tween.IsActive());
    }

    public void SetInteractable(bool interactable) { /* raycast, кнопка */ }
}
```

Подключение: передайте экземпляр в **HandView.AddCardAsync(myCardView)** или в **CardPresenter**.

---

## Шаг 2. Опционально: ICardDisplayMode

Если карта всегда открыта или всегда закрыта — реализуйте **ICardDisplayMode** и возвращайте **AlwaysFaceUp** или **AlwaysFaceDown**. Тогда Flip/FlipAsync можно оставить пустыми или no-op.

---

## Шаг 3. Опционально: ICardViewAnimations

Реализуйте **ICardViewAnimations**: PlayOneShotAsync, PlayLooped, StopLooped, StopAllLooped. Внутри вызывайте [CardViewAnimationTemplates](CardViewUniversal.md#переиспользование-шаблонов) для своего `transform`:

```csharp
public async UniTask PlayOneShotAsync(CardViewAnimationType type, float? duration = null, CancellationToken cancellation = default)
{
    Tween t = CardViewAnimationTemplates.PlayOneShot(transform, type, duration ?? 0.3f);
    if (t != null)
        await UniTask.WaitUntil(() => !t.IsActive(), cancellationToken: cancellation);
}
```

Зацикленные анимации: храните активные твины в словаре по типу и по StopLooped убивайте их.

---

## Шаг 4. Подключение к системе

- **HandView** работает с любым **ICardView**: добавление/удаление карт, раскладка (Fan, Line, Grid).
- **CardPresenter** принимает (ICardView view, DeckConfig config) и связывает данные с вью.
- Для дропа/перетаскивания: обрабатывайте клик или **IDragHandler** на своей вью и вызывайте **MoveToAsync** или перенос между зонами в логике игры.

---

## Шаг 5. Когда использовать CardComponent vs свою ICardView

| Вариант | Когда использовать |
|---------|--------------------|
| **CardComponent** | No-code, настройка в инспекторе, классические карты (колода 36/52/54), существующие примеры (Пьяница). |
| **Своя ICardView / CardViewUniversal** | MVP с кодом, произвольные карты, кастомный визуал и анимации, режимы «всегда открыта» и т.д. |

---

## Когда карта не CardData (CCG, roguelike)

Если у вас своя модель карты (id, название, стоимость, арт):

- Сделайте **свой интерфейс вью** (например IMyGameCardView) с **SetData(MyCardData)** и при необходимости свои события.
- Визуал и логику зон реализуйте сами; для анимаций и раскладок **переиспользуйте**:
  - [CardViewAnimationTemplates](CardViewUniversal.md#переиспользование-шаблонов) — Bounce, Pulse, Shake, Highlight, FlyIn, Idle;
  - **CardLayoutCalculator** и **CardLayoutSettings** (Config, Utils) для позиций карт в руке/зоне.

Так вы не привязаны к CardData, но используете общие анимации и раскладки.

---

## См. также

- [Interfaces](../Interfaces.md)
- [CardViewUniversal](CardViewUniversal.md)
- [README Cards](../README.md) — две ветки (классические карты vs произвольные)
