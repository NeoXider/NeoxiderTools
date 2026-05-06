# RpgStatsDamageableBridge

**Что это:** мост из `Scripts/Tools/Components/AttackSystem/RpgStatsDamageableBridge.cs`, реализующий `IDamageable` и `IHealable` с перенаправлением в `RpgStatsManager`.

**Навигация:** [← AttackSystem](./README.md) · [RPG](../Rpg/README.md)

---

## Назначение

Позволяет компонентам, работающим с `IDamageable`/`IHealable` (например `AdvancedAttackCollider`), наносить урон и лечить через `RpgStatsManager` вместо устаревшего `Health`.

## Использование

1. Добавьте `RpgStatsDamageableBridge` на объект игрока.
2. Убедитесь, что в сцене есть `RpgStatsManager` (или назначьте его в поле `Manager`).
3. Компоненты, ищущие `IDamageable`, будут вызывать `TakeDamage`/`Heal` через этот мост.

## Поля

| Поле | Назначение |
|------|------------|
| `_manager` | Ссылка на RpgStatsManager (если пусто — используется Instance) |
| `_damageMultiplier` | Множитель урона перед передачей в RpgStatsManager |
| `_healMultiplier` | Множитель лечения перед передачей в RpgStatsManager |

## Рекомендация

Для новых проектов используйте `RpgStatsManager` и `RpgNoCodeAction` напрямую. Этот мост нужен для совместимости с `AdvancedAttackCollider` и другими потребителями `IDamageable`.


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `1f` | 1f. |
| `DamageMultiplier` | Damage Multiplier. |
| `HealMultiplier` | Heal Multiplier. |
| `_combatant` | Combatant. |