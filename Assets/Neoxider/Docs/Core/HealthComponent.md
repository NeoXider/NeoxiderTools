# Health Component

**Что это:** MonoBehaviour, реализует `IResourcePoolProvider`. Управляет несколькими ресурсными пулами по id: HP, Mana и произвольные (ярость, энергия). Основной сценарий — здоровье и мана; те же API и события для любых пулов. Путь: `Scripts/Core/Resources/Components/HealthComponent.cs`, пространство имён `Neo.Core.Resources`.

**Как использовать:**
1. Добавить компонент на GameObject (Add Component → Neoxider/Core/Health Component).
2. В списке пулов задать записи с id (например "HP", "Mana"), current/max, при необходимости реген и лимиты за раз.
3. Из кода вызывать GetCurrent(id), Decrease(id, amount), Increase(id, amount), TrySpend(id, amount, out reason). Для скиллов с тратой ресурса — RpgAttackDefinition.CostResourceId / CostAmount.
4. RpgStatsManager и RpgCombatant при назначенном HealthComponent делегируют им HP/Mana и TrySpendResource.
5. События задаются **в каждой записи пула** в инспекторе: OnChanged(current, max), OnDepleted, для HP — OnDamage, OnHeal, OnDeath, OnChangeMax. На уровне компонента только глобальное **OnPoolsChanged** (когда меняется список пулов). Для NeoCondition использовать свойства HpCurrentValue, HpPercentValue, ManaCurrentValue, ManaPercentValue.

---

## Поля и свойства

| Имя | Тип | Назначение |
|-----|-----|------------|
| HpCurrentValue, HpPercentValue | float | Текущее HP и доля 0–1 (для NeoCondition); читают из пула HP. |
| ManaCurrentValue, ManaPercentValue | float | Текущая мана и доля 0–1 (для NeoCondition); читают из пула Mana. |

У **каждой записи пула** в инспекторе: **CurrentState**, **PercentState** (ReactivePropertyFloat) — текущее значение и доля 0–1; подписка через `entry.CurrentState.OnChanged`, биндинг UI к полю пула. Остальные поля пула: id, current, max, regenPerSecond, regenInterval, maxDecreaseAmount, maxIncreaseAmount (-1 = без лимита), restoreOnAwake, ignoreCanHeal, healAmount, healDelay. События записи: **OnChanged** (current, max; факт опустошения — по current <= 0), для HP — **OnDamage**, **OnHeal**, **OnDeath**, **OnChangeMax**.

## Методы (IResourcePoolProvider)

| Сигнатура | Возврат | Описание |
|-----------|---------|----------|
| GetCurrent(string resourceId) | float | Текущее значение пула. |
| GetMax(string resourceId) | float | Максимум пула. |
| TrySpend(string resourceId, float amount, out string failReason) | bool | Проверка и списание; false при нехватке, reason — причина. |
| Decrease(string resourceId, float amount) | float | Уменьшение (урон и т.п.); возвращает фактически снятое. |
| Increase(string resourceId, float amount) | float | Увеличение (хил и т.п.); возвращает фактически добавленное. |
| IsDepleted(string resourceId) | bool | true, если текущее ≤ 0. |
| Restore(string resourceId) | void | Заполнить пул до максимума. |
| SetMax(string resourceId, float max) | void | Установить максимум пула. |
| SetMaxHp(float max) | void | Установить макс. HP (удобный алиас). |

Константы id: `RpgResourceId.Hp`, `RpgResourceId.Mana`.

## События (UnityEvent)

**В каждой записи пула (ResourceEntryInspector):** ReactivePropertyFloat **CurrentState**, **PercentState** (текущее значение и доля 0–1). События: OnChanged(current, max) — опустошение по current <= 0; для HP — OnDamage(actual), OnHeal(actual), OnDeath, OnChangeMax(max).

**Только на компоненте (глобальные):** OnPoolsChanged — при перестроении списка пулов (init, смена набора пулов).

## Примеры

```csharp
IResourcePoolProvider res = go.GetComponent<HealthComponent>();
res.Decrease(RpgResourceId.Hp, 15f);
if (res.TrySpend(RpgResourceId.Mana, 25f, out string reason))
    CastSpell();
```

Использование как Mana: добавить пул с id = "Mana", задать current/max и при необходимости реген. Скиллы указывают CostResourceId = "Mana", CostAmount в RpgAttackDefinition.

## См. также

- [Level.md](./Level.md) — уровень и XP.
- [Rpg/README.md](../Rpg/README.md) — RpgStatsManager, RpgCombatant, стоимость атак.
