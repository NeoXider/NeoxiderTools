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
- [Condition bridge](./ConditionBridge.md) — optional `Neo.Rpg.ConditionBridge` assembly and RPG checks for `NeoCondition`.
- [Combat compatibility](./Combat/README.md) — optional `Neo.Rpg.Combat` assembly and legacy AttackSystem migration.
- [Network integration](./Network.md) — optional `Neo.Rpg.Network` assembly, authority and snapshots.
- [RpgResourceBinding](./RpgResourceBinding.md) / [RpgStatBinding](./RpgStatBinding.md) — reactive resource/stat binding for UI and NoCode; text and progress output should use generic `Neo.NoCode`.
- [RpgStatsDamageableBridge](../Tools/Components/AttackSystem/RpgStatsDamageableBridge.md) — legacy
  `AttackSystem` bridge (`IDamageable/IHealable` -> `RpgCharacter`), sourced from
  `Scripts/Rpg/Combat/` in the optional `Neo.Rpg.Combat` compatibility assembly.

**Navigation:** [← Docs](../README.md)

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

- `Neo.Rpg` is the base local/offline assembly. Its complete asmdef dependency closure contains no Mirror,
  `Neo.Network`, or other network implementation; only the transport-neutral `Neo.Network.Contracts` leaf
  is allowed for adapter contracts.
- `RpgNoCodeAction` and `RpgNoCodeActionType` remain in `Neo.Rpg` so existing compiled code and serialized
  UnityEvent type names retain their original assembly identity.
- `Neo.Rpg.ConditionBridge` contains only the optional `RpgConditionAdapter` component and owns the
  dependency on `Neo.Condition`. Code in a custom asmdef that uses this adapter must reference
  `Neo.Rpg.ConditionBridge` in addition to `Neo.Rpg`.
- `RpgCharacter` is the scene/API facade for stats, level, XP, upgrade points, effects, persistence, and
  replication hooks. Its resource dictionary, mutations, reactive queries, and regen clocks are owned by
  the plain-C# `RpgCharacterResourceService`.
- `RpgCharacterNetworkAdapter` adds optional Mirror commands and snapshot replication without coupling
  `Neo.Rpg` to the `Neo.Network` runtime implementation; the local assembly uses only
  `Neo.Network.Contracts`. The adapter itself lives in the separate `Neo.Rpg.Network` assembly.
- `BuffDefinition` defines temporary buffs with duration and stat modifiers.
- `StatusEffectDefinition` defines status effects (poison, slow, DoT).
- `RpgCharacterProfileData` is the serializable profile payload stored via `SaveProvider`; the plain-C#
  `RpgCharacterProfileService` validates and serializes the same payload for persistence or adapters.
- `RpgCharacterResourceService` can be tested or hosted without a GameObject; `RpgCharacter` preserves the
  existing UnityEvent-friendly methods and serialized resource definitions as its scene wrapper.

## Persistence

- Profile is stored through `SaveProvider`, not the scene-local `SaveManager`.
- Default save key is configurable on `RpgCharacter`.
- `CaptureProfile()` / `ApplyProfile(data)` transfer state without accessing storage.
- `RpgCharacterProfileService.Serialize` clones its input; buff/status stacks survive serialization.
  Null collections and null rows in profile payloads are sanitized without changing the serializer's source.
- Profile **v2** keeps saved effective resource Max authoritative for that restore call (last duplicate ID wins).
  Max is restored before Current and percentage regen. `BaseMax` remains the template base; an ordinary later
  derived refresh recomputes from that base. There is no v3 schema or durable base-max override.
- Initialization/rebuild/profile application defer transport notifications until state is coherent.
  `RuntimeRebuilt` still fires before optional auto-load; `OnProfileLoadedEvent` marks a completed storage load.

## C# resource runtime

Use `Neo.Rpg.Runtime` without a scene component when a simulation only needs resource pools:

```csharp
RpgCharacterResourceService resources = new RpgCharacterResourceService();
resources.Build(new[]
{
    new RpgResourceDefinition
    {
        id = new RpgStatId(RpgStatPreset.Mana),
        startMax = 100f,
        startCurrent = 20f,
        restoreToFull = false,
        regen = new RpgRegenDefinition
        {
            enabled = true, mode = RpgRegenMode.FlatPerTick, value = 5f, tickInterval = 0.5f
        }
    }
});
resources.Spend("Mana", 10f);
resources.TickRegen(0.5f, false); // Mana is now 15.
```

Use `RefreshDerived(maxResolver, regenResolver, initial)` to resolve stats and active modifiers.
Both continuous and tick modes use the resolved rate. `RegenFlat` and upgrade `AddRegenFlat` values
are units per second; `RegenPercent = 50` adds 50%. Resource percentage regen uses percentage points
too (`value = 5` means 5% of Max). Tick modes preserve their configured interval.

Tick regen and status damage deliver at most **1024 logical ticks per entry per public call**. Extra whole
intervals are discarded; fractional phase is retained, including a small prior phase with a huge finite
delta. Nonfinite delta/interval/amount cannot enter delivery loops. Nested calls to the same tick service
return immediately. Resource gates and the existing pause-crossing frame skip remain in effect; status
expiry uses UTC and retains its damage-before-expiry order. This is a per-entry bound, not a frame budget
across every character. See [runtime contracts](InternalTypes.md).

For scene authoring, configure the same definitions on `RpgCharacter` or its template, then connect
`RpgNoCodeAction`, `RpgResourceBinding`, and `RpgStatBinding` to UnityEvents. Bindings reconnect after
`ApplyTemplate` / `RebuildRuntime`; C# consumers caching reactive states can use `RuntimeRebuilt`.

## Integration with Progression

- Level in `RpgCharacter` can be driven directly with `SetLevel`, `AddLevel`, `AddXp`, or manual stat upgrades.
- To sync with another progression system, call `SetLevel(ProgressionManager.Instance.CurrentLevel)` when progression changes.
- `ApplyLevelGrowth()` explicitly commits deferred growth at the current level. Manual/None/deferred
  modes retain their previously applied growth level across unrelated refreshes. See
  [growth and v2 restore policy](RpgProgressionDefinition.md).

По-русски: RPG остаётся набором C# runtime-контрактов с компонентными обёртками. Рост можно применить
явно через `ApplyLevelGrowth()`. Лимит тиков действует на одну запись за вызов; сохранение v2 временно
восстанавливает эффективный Max, не меняя базовое значение шаблона.
