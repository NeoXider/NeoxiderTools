# AnimationFly

**Что это:** Синглтон анимации «полёт бонуса»: спавн префабов с дуговой анимацией к цели (DOTween). Список префабов, множитель количества, задержка, кривая.

**Как использовать:** см. разделы ниже.

---


Синглтон анимации «полёт бонуса»: спавн префабов с дуговой анимацией к цели (DOTween). Список префабов, множитель количества, задержка, кривая.

**Добавить:** Neoxider → UI → AnimationFly (или через Singleton).

## Основное

- **Bonus Prefab List** — префабы и цели.
- **Arc Strength**, **Ease** — параметры дуги и сглаживания.
- **Count Multiplier**, **Delay Between Bonuses** — количество и задержка между спавнами.

Используется для визуализации начисления валюты/очков (полёт монет к счётчику).


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `BonusPrefabData` | Bonus Prefab Data. |
| `arcStrength` | Arc Strength. |
| `bonusPrefabList` | Bonus Prefab List. |
| `bonusType` | Bonus Type. |
| `countMultiplier` | Count Multiplier. |
| `delayBetweenBonuses` | Delay Between Bonuses. |
| `easyEnd` | Easy End. |
| `easyStart` | Easy Start. |
| `endPos` | End Pos. |
| `flyDuration` | Fly Duration. |
| `ignoreZ` | Ignore Z. |
| `isWorldSpace` | Is World Space. |
| `maxBonusCount` | Max Bonus Count. |
| `middlePoint` | Middle Point. |
| `multY` | Mult Y. |
| `parentCanvas` | Parent Canvas. |
| `prefab` | Prefab. |
| `scaleMult` | Scale Mult. |
| `spawnParent` | Spawn Parent. |
| `useUnscaledTime` | Use Unscaled Time. |