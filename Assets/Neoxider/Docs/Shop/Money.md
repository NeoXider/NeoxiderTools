# Money

**Что это:** `Money` — компонент валюты и источник `IMoneySpend` / `IMoneyAdd` для магазина и других систем. Может использоваться как основной singleton `Money.I` или как отдельный источник валюты по прямой ссылке. Файл: `Scripts/Shop/Money.cs`, пространство имён: `Neo.Shop`.

**Как использовать:**
1. Добавьте `Money` на сцену.
2. Для основного источника валюты оставьте включённым `Set Instance On Awake`.
3. Для дополнительных источников валюты отключите singleton-флаг и передавайте их в компоненты по ссылке, например через `Money Source` в `TextMoney` или `moneySpendSource` в `Shop`.
4. Используйте `Add()`, `Spend()`, `SetMoney()`, `AddLevelMoney()`, `SetMoneyForLevel()` в зависимости от сценария.

---

## Поля

- **Money Save** — базовый ключ сохранения.
- **CurrentMoney** — текущий баланс.
- **LevelMoney** — сумма, накопленная за уровень или текущую сессию.
- **AllMoney** — общий накопленный объём валюты.
- **LastChangeMoney** — последнее изменение суммы.
- **st_money / t_money** — привязки для обновления текущего баланса в UI.
- **st_levelMoney / t_levelMoney** — привязки для отображения `LevelMoney`.

## Публичные свойства и методы

| API | Описание |
|-----|----------|
| `money` | Текущий баланс. |
| `levelMoney` | Текущее значение `LevelMoney`. |
| `allMoney` | Общее накопленное значение. |
| `LastChangeMoneyValue` | Последнее изменение суммы. |
| `Add(float amount)` | Добавляет валюту к текущему и общему балансу. |
| `Spend(float amount)` | Пытается списать валюту. Возвращает `true`, если средств достаточно. |
| `CanSpend(float count)` | Проверяет, хватает ли денег. |
| `AddLevelMoney(float count)` | Добавляет сумму в `LevelMoney`. |
| `SetLevelMoney(float count = 0)` | Устанавливает `LevelMoney`. |
| `SetMoney(float count = 0)` | Прямо задаёт текущий баланс. |
| `SetMoneyForLevel(bool resetLevelMoney = true)` | Переносит `LevelMoney` в `CurrentMoney`. |

## Реактивные поля

В текущей версии компонент использует не `UnityEvent`, а `ReactivePropertyFloat`:

- `CurrentMoney`
- `LevelMoney`
- `AllMoney`
- `LastChangeMoney`

Подписываться нужно на `.OnChanged` у этих полей, а не искать старые события вида `OnChangeMoney`.

## Сохранение

- `Money` использует `SaveProvider.SetFloat()` и `SaveProvider.GetFloat()`.
- Сохраняются `CurrentMoney` и `AllMoney`.
- `LevelMoney` не сохраняется как постоянный баланс и обычно относится к текущей игровой сессии или уровню.

## Когда использовать несколько Money

Несколько экземпляров полезны, если в проекте есть разные типы ресурсов:
- монеты;
- энергия;
- soft currency / hard currency;
- отдельная валюта режима игры.

В таком случае только один экземпляр должен оставаться `Money.I`. Остальные следует передавать явно по ссылке.

## См. также

- [Shop](./Shop.md)
- [TextMoney](./TextMoney.md)
- [SaveProvider](../Save/SaveProvider.md)
