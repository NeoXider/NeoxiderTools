# RpgStatsManager

`RpgStatsManager` was removed in v8.4.0.

Use [RpgCharacter](./RpgCharacter.md). It replaces the old singleton profile manager and is now the single source of truth for HP/Mana/Stamina/custom resources, stats, level, XP, upgrade points, buffs, statuses, persistence, and network synchronization.

## Migration

1. Add `RpgCharacter` to the player object.
2. Configure resources/stats through `RpgCharacterTemplate` or the local component fields.
3. Enable persistence and set the save key when save/load is needed.
4. Replace `RpgStatsManager.Instance` calls with an explicit `RpgCharacter` reference.
5. Use `RpgNoCodeAction` and `RpgConditionAdapter` for inspector-driven flows.
