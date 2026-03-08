# RpgStatsManager

**Что это:** persistent `MonoBehaviour`-менеджер профиля игрока. Отвечает за HP, уровень, баффы, статус-эффекты, invulnerability locks и сохранение через `SaveProvider`.

**Как использовать:**
1. Добавьте `RpgStatsManager` на объект сцены.
2. Назначьте `BuffDefinition` и `StatusEffectDefinition` в массивы.
3. При необходимости задайте свой `Save Key`.
4. Если нужно автосохранение при runtime-изменениях, включите `Auto Save`. По умолчанию он выключен.
5. Если игрок должен атаковать или уклоняться, комбинируйте его с `RpgAttackController` и `RpgEvadeController`.
6. Вызывайте `TakeDamage`, `Heal`, `TryApplyBuff`, `TryApplyStatus`, `ResetProfile`, `LoadProfile`, `SaveProfile`.
7. Для UI используйте `HpState`, `HpPercentState`, `LevelState` или UnityEvent.

**Навигация:** [← К RPG](./README.md)

---

## Основные поля

| Поле | Тип | Назначение |
|------|-----|------------|
| `_buffDefinitions` | `BuffDefinition[]` | Определения баффов |
| `_statusDefinitions` | `StatusEffectDefinition[]` | Определения статус-эффектов |
| `_saveKey` | `string` | Ключ сохранения профиля |
| `_autoSave` | `bool` | Автозапись json-профиля после изменений; по умолчанию выключено |
| `_hpRegenPerSecond` | `float` | Регенерация HP в секунду |
| `_regenInterval` | `float` | Интервал проверки регена и тиков статусов |

## Reactive state

| Поле | Тип | Назначение |
|------|-----|------------|
| `HpState` | `ReactivePropertyFloat` | Текущее HP |
| `HpPercentState` | `ReactivePropertyFloat` | HP в процентах (0..1) |
| `LevelState` | `ReactivePropertyInt` | Текущий уровень |

## Основные методы

| Метод | Что делает |
|------|------------|
| `TakeDamage(float amount)` | Наносит урон, возвращает фактический урон |
| `Heal(float amount)` | Восстанавливает HP, возвращает фактическое лечение |
| `SetMaxHp(float maxHp, bool clampCurrent)` | Устанавливает максимальное HP |
| `SetLevel(int level)` | Устанавливает уровень |
| `GetOutgoingDamageMultiplier()` | Возвращает итоговый множитель исходящего урона |
| `GetMovementSpeedMultiplier()` | Возвращает текущий speed multiplier |
| `AddInvulnerabilityLock()` / `RemoveInvulnerabilityLock()` | Управляет временной неуязвимостью |
| `TryApplyBuff(string buffId, out string failReason)` | Применяет бафф |
| `TryApplyStatus(string statusId, out string failReason)` | Применяет статус-эффект |
| `RemoveBuff(string buffId)` | Снимает бафф |
| `RemoveStatus(string statusId)` | Снимает статус |
| `HasBuff(string buffId)` | Проверяет наличие баффа |
| `HasStatus(string statusId)` | Проверяет наличие статуса |
| `ResetProfile()` | Сбрасывает профиль |
| `LoadProfile()` / `SaveProfile()` | Загружает или сохраняет профиль |

## События

- `OnDamaged` — при получении урона (float = фактический урон).
- `OnHealed` — при лечении (float = фактическое лечение).
- `OnDeath` — при HP = 0.
- `OnBuffApplied` / `OnBuffExpired` — при применении/истечении баффа.
- `OnStatusApplied` / `OnStatusExpired` — при применении/истечении статуса.

## Пример кода

```csharp
using Neo.Rpg;
using UnityEngine;

public class EnemyDamageExample : MonoBehaviour
{
    [SerializeField] private float damageAmount = 25f;

    public void DealDamage()
    {
        if (RpgStatsManager.Instance != null)
        {
            RpgStatsManager.Instance.TakeDamage(damageAmount);
        }
    }
}
```
