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

## Опция Invert

При включённой `_invert` результат проверки инвертируется.

## Использование

1. Добавьте `RpgConditionAdapter` на объект.
2. Выберите `Evaluation Mode`.
3. Заполните `Threshold`, `Level Threshold`, `Buff Id`, `Status Id` или `Attack Id`.
4. Используйте в `NeoCondition` как источник типа `IConditionEvaluator` или вызывайте `EvaluateCurrent()` из кода.
