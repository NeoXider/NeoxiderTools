# CardView

**Что это:** визуальное представление одной карты. Реализует ICardView, поддерживает переворот, перемещение, hover и клики. Пространство имён `Neo.Cards`, файл `Scripts/Cards/View/CardView.cs`.

**Как использовать:** добавить на префаб карты; привязать Card Image или Sprite Renderer; настраивать длительности и hover в инспекторе.

---

## Основное

- **Card Image / Sprite Renderer** — отображение карты (UI или 2D).
- **Flip Duration / Move Duration** — длительность анимаций переворота и перемещения.
- **Hover Scale / Hover Duration / Hover Y Offset** — эффект при наведении.

См. также [CardComponent](../CardComponent.md), [CardViewUniversal](./CardViewUniversal.md), [CustomCardViewGuide](./CustomCardViewGuide.md).


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `20f` | 20f. |
| `Data` | Data. |
| `IsFaceUp` | Is Face Up. |
| `Transform` | Transform. |
| `_cardImage` | Card Image. |
| `_flipDuration` | Flip Duration. |
| `_flipEase` | Flip Ease. |
| `_hoverDuration` | Hover Duration. |
| `_hoverScale` | Hover Scale. |
| `_moveDuration` | Move Duration. |
| `_moveEase` | Move Ease. |
| `_spriteRenderer` | Sprite Renderer. |