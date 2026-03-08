# RPG

**What it is:** an RPG stats module for HP, level, buffs, and status effects. Scripts live in `Scripts/Rpg/`.

**Contents:**
- [RpgStatsManager](./RpgStatsManager.md) — main runtime entry point and profile coordinator.
- [RpgNoCodeAction](./RpgNoCodeAction.md) — UnityEvent bridge for no-code flows.
- [RpgConditionAdapter](./RpgConditionAdapter.md) — RPG checks for `NeoCondition` and other evaluator-based systems.

**Navigation:** [← DocsEn](../README.md)

---

## How to use

1. Create `BuffDefinition` and `StatusEffectDefinition` assets from the `Neoxider/RPG` menu.
2. Add `RpgStatsManager` to a scene object and assign the definitions.
3. Configure `Save Key` when the project needs a custom or separate profile.
4. Bind UI to `HpState`, `HpPercentState`, `LevelState`, or the manager UnityEvents.
5. Trigger `TakeDamage`, `Heal`, `TryApplyBuff`, `TryApplyStatus` from code or via `RpgNoCodeAction`.
6. Use `RpgConditionAdapter` for no-code gating rules.

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
