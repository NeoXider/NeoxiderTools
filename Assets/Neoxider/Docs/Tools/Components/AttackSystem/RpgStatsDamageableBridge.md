# RpgStatsDamageableBridge

**Что это:** совместимый мост из legacy `AttackSystem` в новый RPG combat layer. Файл: `Scripts/Tools/Components/AttackSystem/RpgStatsDamageableBridge.cs`.

**Навигация:** [← AttackSystem](./README.md) · [RPG](../../../Rpg/README.md)

## Назначение

Компонент реализует старые интерфейсы `IDamageable` и `IHealable`, но фактический урон и лечение пересылает в `RpgCharacter`.

Используйте его только для старых сцен, префабов и компонентов, которые ещё завязаны на `IDamageable/IHealable`, например `AdvancedAttackCollider`. В новых RPG-сценариях лучше вызывать `RpgCharacter`, `IRpgCombatReceiver`, `RpgAttackController` или `RpgNoCodeAction` напрямую.

## Как подключить

1. Добавьте `RpgCharacter` на актёра.
2. Добавьте `RpgStatsDamageableBridge` на этот же объект или на дочерний hitbox.
3. Если bridge находится не под нужным `RpgCharacter`, назначьте поле `_character` вручную.
4. Старый компонент будет вызывать `TakeDamage(int)` / `Heal(int)`, а bridge передаст вызов в `RpgCharacter.Damage(float)` / `RpgCharacter.Heal(float)`.

## Поля

| Поле | Назначение |
|------|------------|
| `_character` | Явная ссылка на `RpgCharacter`; если пусто, bridge ищет его в родителях. |
| `_damageMultiplier` | Множитель урона перед передачей в `RpgCharacter`. |
| `_healMultiplier` | Множитель лечения перед передачей в `RpgCharacter`. |

## Поведение

- `TakeDamage(int amount)` игнорирует `amount <= 0`.
- `Heal(int amount)` игнорирует `amount <= 0`.
- `DamageMultiplier` и `HealMultiplier` публично обрезают отрицательные значения до `0`.
- Bridge не добавляет сетевую авторизацию сам по себе. Если объект сетевой, итоговое применение всё равно должно проходить через правила `RpgCharacter` / `NeoNetworkComponent`.
