# HealthComponent

**Назначение:** Компонент ресурсов (HP, Mana, произвольные пулы) — регенерация, лимиты, события урона/лечения/смерти.

## Подключение

- Добавить: **Add Component → Neoxider → Core → Health Component**.

## Основные настройки (Inspector)

| Поле | Описание |
|------|----------|
| `HpCurrentValue` | Hp Current Value. |
| `HpMaxValue` | Hp Max Value. |
| `HpPercentValue` | Hp Percent Value. |
| `ManaCurrentValue` | Mana Current Value. |
| `ManaMaxValue` | Mana Max Value. |
| `ManaPercentValue` | Mana Percent Value. |
| `OnPoolsChanged` | On Pools Changed. |
| `_loadOnAwake` | Load On Awake. |
| `_onPoolsChanged` | On Pools Changed. |
| `_saveKey` | Save Key. |
| `restoreOnAwake` | Restore On Awake. |
| `true` | True. |

## Runtime контракт

- `Decrease(resourceId, amount)` уменьшает выбранный ресурс, вызывает `OnDamage` при фактическом уроне и `OnDeath` ровно один раз при переходе ресурса из `> 0` в `<= 0`.
- `OnDeath` работает для любого пула ресурсов, не только для `HP`, поэтому Mana/Stamina/Shield могут иметь собственные depleted-события.
- `Increase(resourceId, amount)` не лечит ресурс с нуля, если у пула не включен `ignoreCanHeal`.
- `ResourcePoolModel` остается pure C# ядром без зависимости от Unity scene; `HealthComponent` только синхронизирует inspector-пулы, события и reactive states.

## См. также

- [Корень модуля](../../README.md)
