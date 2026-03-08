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

## События

- `OnSuccess` — при успешном выполнении.
- `OnFailed` — при ошибке (при `ApplyBuff` / `ApplyStatus`).
- `OnResultMessage` — сообщение о результате (успех или ошибка).

## Использование

1. Добавьте `RpgNoCodeAction` на объект.
2. Выберите `Action Type`.
3. Заполните `Amount`, `Level`, `Buff Id`, `Status Id` или `Attack Id` в зависимости от типа.
4. Привяжите `Execute()` к `Button.onClick` или другому UnityEvent.
