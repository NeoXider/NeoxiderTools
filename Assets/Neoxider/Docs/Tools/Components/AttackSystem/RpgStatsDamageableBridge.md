# RpgStatsDamageableBridge

**Что это:** мост из `Scripts/Tools/Components/AttackSystem/RpgStatsDamageableBridge.cs`, реализующий `IDamageable` и `IHealable` с перенаправлением в `RpgCharacter`.

**Навигация:** [← AttackSystem](./README.md) · [RPG](../../Rpg/README.md)

## Назначение

Позволяет legacy-компонентам, работающим с `IDamageable`/`IHealable` (например `AdvancedAttackCollider`), наносить урон и лечить через новый RPG-слой.

## Использование

1. Добавьте `RpgStatsDamageableBridge` на объект с `RpgCharacter` или его дочерний объект.
2. При необходимости назначьте `_character` вручную.
3. Legacy-компоненты будут вызывать `TakeDamage`/`Heal`, а мост передаст вызов в `RpgCharacter.Damage` / `RpgCharacter.Heal`.

## Поля

| Поле | Назначение |
|------|------------|
| `_character` | Явная ссылка на `RpgCharacter`; если пусто, ищется в родителях |
| `_damageMultiplier` | Множитель урона перед передачей в `RpgCharacter` |
| `_healMultiplier` | Множитель лечения перед передачей в `RpgCharacter` |
