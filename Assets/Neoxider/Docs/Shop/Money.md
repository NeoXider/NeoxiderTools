# Money

Синглтон валюты: общая сумма, уровеньовая валюта, сохранение через SaveProvider (при изменении вызывается SaveProvider.Save()). Реализует IMoneySpend, IMoneyAdd для трат и начислений. Массивы SetText/TMP_Text для вывода опциональны — при null обновление пропускается.

**Добавить:** GameObject → Neoxider → Shop → Money.

## Поля

- **Money Save** — ключ сохранения.
- **All Money** / **Level Money** / **Money** — типы валют (всего, за уровень, текущая).
- **SetText** — массив полей для отображения (опционально).

## События

- **OnChangeAllMoney**, **OnChangedLevelMoney**, **OnChangedMoney**, **OnChangeLastMoney** — при изменении соответствующих сумм.

## См. также

- [Shop](./Shop.md) — магазин.
- [SaveManager](../Save/README.md) — сохранение.
