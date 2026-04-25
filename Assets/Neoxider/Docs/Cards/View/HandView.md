# HandView

**Что это:** визуальное представление руки игрока. Реализует IHandView, управляет раскладкой и анимацией карт (Fan, Line, Grid и др.). Пространство имён `Neo.Cards`, файл `Scripts/Cards/View/HandView.cs`.

**Как использовать:** добавить на объект руки вместе с HandComponent; задать Layout Type и параметры раскладки в инспекторе.

---

## Основное

- **Layout Type / Spacing / Arc Angle / Arc Radius** — тип раскладки и параметры (веер, линия, сетка).
- **Grid Settings** — колонки и отступ между рядами для сетки.
- **Arrange Duration / Arrange Ease** — длительность и кривая анимации раскладки.

См. также [HandComponent](../HandComponent.md).


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `30f` | 30f. |
| `400f` | 400f. |
| `5` | 5. |
| `60f` | 60f. |
| `80f` | 80f. |
| `CardViews` | Card Views. |
| `Count` | Count. |
| `LayoutType` | Layout Type. |
| `_arrangeDuration` | Arrange Duration. |
| `_arrangeEase` | Arrange Ease. |
| `_layoutType` | Layout Type. |