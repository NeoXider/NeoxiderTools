# RpgNoCodeAction

**Что это:** `MonoBehaviour`-bridge из `Scripts/Rpg/Bridge/RpgNoCodeAction.cs` для вызова RPG-действий без кода через UnityEvent.

**Навигация:** [← К RPG](./README.md)

---

## Типы действий

| Тип | Описание |
|-----|----------|
| `TakeDamage` | Наносит урон (значение `_amount`) |
| `Heal` | Лечит (значение `_amount`) |
| `SetMaxHp` | Устанавливает максимальное HP |
| `SetLevel` | Устанавливает уровень |
| `ApplyBuff` | Применяет бафф по `_buffId` |
| `ApplyStatus` | Применяет статус по `_statusId` |
| `RemoveBuff` | Снимает бафф |
| `RemoveStatus` | Снимает статус |
| `UseAttackById` | Запускает атаку через `RpgAttackController` по `_attackId` |
| `UsePrimaryAttack` | Запускает первую атаку из `RpgAttackController` |
| `UsePresetById` | Запускает preset через `RpgAttackController` по `_presetId` |
| `UsePrimaryPreset` | Запускает первый preset из `RpgAttackController` |
| `StartEvade` | Запускает `RpgEvadeController` |
| `ResetProfile` | Сбрасывает профиль |
| `SaveProfile` | Сохраняет профиль |
| `LoadProfile` | Загружает профиль |
| `SetMaxResource` / `AddMaxResource` | Меняет максимум любого ресурса через `_resource` (`HP`, `Mana`, `Stamina`, `Shield` или custom ID) |
| `SpendResource` / `RefillResource` / `RestoreResource` | Списывает, восполняет или полностью восстанавливает выбранный ресурс |
| `RestoreAllResources` | Восстанавливает все ресурсы персонажа до максимума |
| `AddStatBase` / `SetStatBase` | Меняет базовое значение любого стата через `_stat` |
| `AddLevel` / `AddXp` / `AddUpgradePoints` | Управляет уровнем, опытом и очками улучшений |
| `UpgradeStat` | Тратит upgrade points на выбранный стат, если правило улучшения разрешает |
| `ApplyInlineBuff` | Применяет inline buff по индексу `_inlineBuffIndex` |
| `ClearAllBuffs` / `ClearAllStatuses` | Очищает все активные баффы или статусы |
| `LockInvulnerable` / `UnlockInvulnerable` / `SetInvulnerable` | Управляет неуязвимостью персонажа |

## События

- `OnSuccess` — при успешном выполнении.
- `OnFailed` — при ошибке (при `ApplyBuff` / `ApplyStatus`).
- `OnResultMessage` — сообщение о результате (успех или ошибка).

## Использование

1. Добавьте `RpgNoCodeAction` на объект.
2. Выберите `Action Type`.
3. Заполните `Amount`, `Level`, `Buff Id`, `Status Id`, `Attack Id`, `_resource` или `_stat` в зависимости от типа.
4. Привяжите `Execute()` к `Button.onClick` или другому UnityEvent.
