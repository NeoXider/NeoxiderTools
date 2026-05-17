# RpgCombatant

`RpgCombatant` удалён в v8.4.0.

Используйте [RpgCharacter](./RpgCharacter.md). Он заменяет прежний `RpgCombatant` и покрывает те же сценарии для NPC, врагов, питомцев и разрушаемых объектов, но с универсальными ресурсами, статами, баффами, статусами, NoCode API и Mirror-синхронизацией.

## Миграция

1. Добавьте `RpgCharacter` на тот же объект.
2. Перенесите HP/level/regen/buffs/statuses в template или локальные поля `RpgCharacter`.
3. В ссылках UI, attack, condition и no-code компонентов выберите новый `RpgCharacter`.
4. Для урона используйте `Damage`, `DamageType`, `Heal`, `ApplyBuffById`, `ApplyStatusById`.
