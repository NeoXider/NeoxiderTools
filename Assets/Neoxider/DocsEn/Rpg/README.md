# RPG

**What it is:** a full RPG runtime module for persistent player profiles, local and networked combat actors, melee/ranged/aoe attacks, target selectors, attack presets for AI/skills/spells, evade, buffs, statuses, built-in input, and no-code integration. Scripts live in `Scripts/Rpg/`.

**Contents:**
- [RpgCharacter](./RpgCharacter.md) — universal character component for players, NPCs, mobs, pets, and destructibles.
- [RpgCharacterTemplate](./RpgCharacterTemplate.md) — SO template for resources, stats, effects, and progression.
- [RpgProgressionDefinition](./RpgProgressionDefinition.md) — level growth mode: all-stats, manual upgrades, or hybrid.
- [RpgAttackController](./RpgAttackController.md) — unified melee/ranged/aoe attack entry point.
- [RpgAttackDefinition](./RpgAttackDefinition.md) — ScriptableObject attack payload.
- [RpgAttackPreset](./RpgAttackPreset.md) — AI/skills/spells preset with targeting rules.
- [RpgProjectile](./RpgProjectile.md) — projectile runtime for ranged attacks.
- [RpgTargetSelector](./RpgTargetSelector.md) — reusable target selector for AI/ability logic.
- [RpgEvadeController](./RpgEvadeController.md) — evade and invulnerability.
- [RpgNoCodeAction](./RpgNoCodeAction.md) — UnityEvent bridge for no-code flows.
- [RpgConditionAdapter](./RpgConditionAdapter.md) — RPG checks for `NeoCondition`.
- [RpgResourceBinding](./RpgResourceBinding.md) / [RpgStatBinding](./RpgStatBinding.md) — reactive resource/stat binding for UI and NoCode.

**Navigation:** [← DocsEn](../README.md)

---

## How to use

1. Create `BuffDefinition`, `StatusEffectDefinition`, and `RpgAttackDefinition` assets from the `Neoxider/RPG` menu.
2. Use `RpgCharacter` for the player, enemies, NPCs, pets, and scene-local actors.
3. Enable persistence on `RpgCharacter` when runtime mutations should be saved.
4. Configure resources with presets (`HP`, `Mana`, `Stamina`, `Shield`) or custom IDs (`DarkMana`, `Rage`, etc.).
5. Use `RpgAttackController` for direct, area, and projectile attacks. Primary attack uses left mouse button by default.
6. Use `RpgEvadeController` for dodge/i-frames and `RpgProjectile` for ranged payload delivery.
7. Built-in input on attack/evade can be disabled for NPC and AI actors.
8. Use `RpgNoCodeAction` and `RpgConditionAdapter` for inspector-driven flows.

## Module scope

- `RpgCharacter` manages resources, stats, level, XP, upgrade points, buffs, status effects, regen, profile persistence, and Mirror sync.
- `BuffDefinition` defines temporary buffs with duration and stat modifiers.
- `StatusEffectDefinition` defines status effects (poison, slow, DoT).
- `RpgProfileData` is the serializable profile payload stored via `SaveProvider`.

## Persistence

- Profile is stored through `SaveProvider`, not the scene-local `SaveManager`.
- Default save key is configurable on `RpgCharacter`.

## Integration with Progression

- Level in `RpgCharacter` can be driven directly with `SetLevel`, `AddLevel`, `AddXp`, or manual stat upgrades.
- To sync with another progression system, call `SetLevel(ProgressionManager.Instance.CurrentLevel)` when progression changes.
