# RPG

**What it is:** a full RPG runtime module for persistent player profiles, local combat actors, melee/ranged/aoe attacks, evade, buffs, statuses, built-in input, and no-code integration. Scripts live in `Scripts/Rpg/`.

**Contents:**
- [RpgStatsManager](./RpgStatsManager.md) — persistent player profile and save/load coordinator.
- [RpgCombatant](./RpgCombatant.md) — local combat actor for enemies and NPCs.
- [RpgAttackController](./RpgAttackController.md) — unified melee/ranged/aoe attack entry point.
- [RpgAttackDefinition](./RpgAttackDefinition.md) — ScriptableObject attack payload.
- [RpgProjectile](./RpgProjectile.md) — projectile runtime for ranged attacks.
- [RpgEvadeController](./RpgEvadeController.md) — evade and invulnerability.
- [RpgNoCodeAction](./RpgNoCodeAction.md) — UnityEvent bridge for no-code flows.
- [RpgConditionAdapter](./RpgConditionAdapter.md) — RPG checks for `NeoCondition`.

**Navigation:** [← DocsEn](../README.md)

---

## How to use

1. Create `BuffDefinition`, `StatusEffectDefinition`, and `RpgAttackDefinition` assets from the `Neoxider/RPG` menu.
2. Use `RpgStatsManager` for the player when persistence is required.
3. Enable `Auto Save` on `RpgStatsManager` when runtime mutations should be written automatically. It is off by default.
4. Use `RpgCombatant` for enemies, NPCs, and scene-local actors.
5. Use `RpgAttackController` for direct, area, and projectile attacks. Primary attack uses left mouse button by default.
6. Use `RpgEvadeController` for dodge/i-frames and `RpgProjectile` for ranged payload delivery.
7. Built-in input on attack/evade can be disabled for NPC and AI actors.
8. Use `RpgNoCodeAction` and `RpgConditionAdapter` for inspector-driven flows.

## Module scope

- `RpgStatsManager` manages HP, level, buffs, status effects, regen, and profile persistence.
- `BuffDefinition` defines temporary buffs with duration and stat modifiers.
- `StatusEffectDefinition` defines status effects (poison, slow, DoT).
- `RpgProfileData` is the serializable profile payload stored via `SaveProvider`.

## Persistence

- Profile is stored through `SaveProvider`, not the scene-local `SaveManager`.
- Default save key is `RpgV1.Profile`, configurable in `RpgStatsManager`.

## Integration with Progression

- Level in `RpgStatsManager` is stored separately from `ProgressionManager`.
- To sync, call `SetLevel(ProgressionManager.Instance.CurrentLevel)` when progression changes.
