# SlotEconomyDefinition

**Что это:** `Neo.Bonus.SlotEconomyDefinition` — ScriptableObject экономики слот-машины: таблица символов (вес выпадения, `moneyReward`, `bonusReward`, флаг спец-символа) + оценка линии. Создание: `Create → Neoxider → Bonus → Slot Economy`.

**Как использовать со SpinController:** на спин — `PickWeightedId()` на каждый барабан → `ApplySpecialRule(ids)` (при включённом `ForceLineOnSpecial` один спец-символ превращает всю линию) → скормить ids барабанам → после остановки `EvaluateLine(ids)` → `LineResult` (`MoneyReward`, `BonusReward`, `SpecialTriggered`) → выплата в `Money`/энергию.
