# HandView

**Что это:** визуальное представление руки игрока. Реализует IHandView, управляет раскладкой и анимацией карт (Fan, Line, Grid и др.). Пространство имён `Neo.Cards`, файл `Scripts/Cards/View/HandView.cs`.

**Как использовать:** добавить на объект руки вместе с HandComponent; задать Layout Type и параметры раскладки в инспекторе.

---

## Основное

- **Layout Type / Spacing / Arc Angle / Arc Radius** — тип раскладки и параметры (веер, линия, сетка).
- **Grid Settings** — колонки и отступ между рядами для сетки.
- **Arrange Duration / Arrange Ease** — длительность и кривая анимации раскладки.

См. также [HandComponent](../HandComponent.md).
