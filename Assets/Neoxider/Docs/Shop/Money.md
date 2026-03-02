# Money

**Что это:** компонент валюты (пространство имён `Neo.Shop`, файл `Assets/Neoxider/Scripts/Shop/Money.cs`). Singleton `Money.I` или несколько экземпляров. Реализует IMoneySpend, IMoneyAdd. Хранит текущий баланс, уровень и всего; сохраняет через SaveProvider по ключу **Money Save**.

**Как использовать:** добавить компонент (GameObject → Neoxider → Shop → Money). Один экземпляр — включить **Set Instance On Awake** для `Money.I`. Несколько (монеты + энергия): у главного включить Set Instance; у остальных выключить и в [TextMoney](TextMoney.md) указать **Money Source**. Методы `Add(amount)`, `Spend(amount)`; свойства `money`, `levelMoney`, `allMoney`.

---

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
