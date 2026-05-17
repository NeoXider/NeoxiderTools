# RpgConditionAdapter

**Что это:** `MonoBehaviour`-адаптер из `Scripts/Rpg/Bridge/RpgConditionAdapter.cs`, который превращает состояние RPG в проверяемое условие для `NeoCondition` и других систем, работающих через `IConditionEvaluator`.

**Навигация:** [← К RPG](./README.md)

---

## Режимы проверки

| Режим | Описание |
|-------|----------|
| `HpAtLeast` | HP >= `_threshold` |
| `HpPercentAtLeast` | HP в процентах >= `_threshold` (0–100) |
| `LevelAtLeast` | Уровень >= `_levelThreshold` |
| `IsDead` | Персонаж мёртв |
| `HasBuff` | Есть активный бафф по `_buffId` |
| `HasStatus` | Есть активный статус по `_statusId` |
| `CanPerformActions` | Цель сейчас может действовать |
| `IsInvulnerable` | Цель сейчас неуязвима |
| `CanEvade` | `RpgEvadeController` готов к запуску |
| `AttackReady` | `RpgAttackController` может запустить атаку по `_attackId` |
| `ResourceAtLeast` / `ResourceBelow` | Проверяет текущее значение любого ресурса через `_resource` |
| `ResourcePercentAtLeast` / `ResourcePercentBelow` | Проверяет процент любого ресурса через `_resource` (threshold 0–100) |
| `StatAtLeast` / `StatBelow` | Проверяет значение любого стата через `_stat` |
| `UpgradePointsAtLeast` | Проверяет количество свободных upgrade points |
| `UpgradeLevelAtLeast` | Проверяет сколько раз выбранный стат был улучшен |
| `XpAtLeast` | Проверяет текущее значение XP |

## Опция Invert

При включённой `_invert` результат проверки инвертируется.

## Использование

1. Добавьте `RpgConditionAdapter` на объект.
2. Выберите `Evaluation Mode`.
3. Заполните `Threshold`, `Level Threshold`, `Buff Id`, `Status Id`, `Attack Id`, `_resource` или `_stat`.
4. Используйте в `NeoCondition` как источник типа `IConditionEvaluator` или вызывайте `EvaluateCurrent()` из кода.
