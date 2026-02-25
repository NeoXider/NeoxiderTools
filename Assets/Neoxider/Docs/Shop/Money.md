# Money

Компонент валюты: общая сумма, уровеньовая валюта, сохранение через SaveProvider (при изменении вызывается SaveProvider.Save()). Реализует IMoneySpend, IMoneyAdd. Может использоваться как синглтон (`Money.I`) или несколько экземпляров (монеты и энергия с разными **Money Save**).

**Несколько экземпляров (монеты + энергия):** на «главный» (например монеты) включите **Set Instance On Awake**; на остальные (энергия) выключите. Тогда `Money.I` будет указывать на главный; для энергии используйте в **TextMoney** поле **Money Source** — укажите компонент Money с энергией.

**Добавить:** GameObject → Neoxider → Shop → Money.

## Поля

- **Money Save** — ключ сохранения (например "Money" / "Energy" для разных экземпляров).
- **Set Instance On Awake** (из Singleton) — включите только у одного экземпляра, который должен быть `Money.I`.
- **All Money** / **Level Money** / **Money** — типы валют (всего, за уровень, текущая).
- **SetText** — массив полей для отображения (опционально).

## События

- **OnChangeAllMoney**, **OnChangedLevelMoney**, **OnChangedMoney**, **OnChangeLastMoney** — при изменении соответствующих сумм.

## См. также

- [Shop](./Shop.md) — магазин.
- [SaveManager](../Save/README.md) — сохранение.
