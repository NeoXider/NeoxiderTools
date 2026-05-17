# RpgCombatant

`RpgCombatant` was removed in v8.4.0.

Use [RpgCharacter](./RpgCharacter.md). It replaces the old scene-local combat actor and supports the same NPC, enemy, pet, and destructible scenarios with universal resources, stats, buffs, statuses, NoCode API, and Mirror synchronization.

## Migration

1. Add `RpgCharacter` to the same object.
2. Move HP/level/regen/buffs/statuses into a template or the local `RpgCharacter` fields.
3. Reassign UI, attack, condition, and no-code references to the new `RpgCharacter`.
4. Use `Damage`, `DamageType`, `Heal`, `ApplyBuffById`, and `ApplyStatusById` for gameplay calls.
