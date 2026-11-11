# Triggers, damage hooks, zones and templates

**What it is:** the reactive layer of `Neo.Abilities`: damage hooks that change a hit in flight, modifier triggers, payload ops, zones for auras and formations, ready templates, a content validator and a trace that explains why something fired. Everything is data on `ModifierBlueprint` and `EffectNodeData`, so it works the same from assets, from code and over the network.

**How to use:**
1. Add reactions to the **Event Reactions** of a [ModifierDefinition](./ModifierDefinition.md) and effect nodes to an [AbilityDefinition](./AbilityDefinition.md).
2. Or start from a template: **+ Template** in the [Ability Designer](./AbilityDesignerWindow.md), or `AbilityTemplates` in code.
3. Check the content with `AbilityContentValidator`; the Designer's status bar shows its findings.
4. Enable the trace to see why a reaction fired.
5. For a guided tour, open `Assets/NeoxiderDemos/Demo/Scenes/Abilities/AbilitiesTriggersDemo.unity` (Demo download).

- Sources: `Assets/Neoxider/Scripts/Abilities/Domain`: `Combat/DamagePipeline.cs`, `AbilitySystem.Triggers.cs`, `AbilitySystem.Zones.cs`, `Effects/Ops/*`, `Templates/AbilityTemplates.cs`, `Validation/AbilityContentValidator.cs`, `Diagnostics/AbilityTrace.cs`
- Replaceable formulas and systems: [Rules and extension points](./Rules.md)

---

## Demo scene

`AbilitiesTriggersDemo.unity` is a six-lesson tutorial on a 2D arena with a Hero, a Golem with thorns and an Imp.

| Lesson | Button | What happens | Built from |
|--------|--------|--------------|------------|
| 1 | **Strike golem (20)** | The golem loses 20 minus armor; the hero gets 30% of it back. | `Thorns` (`before_take_damage` + `reflect`) |
| 2 | **Empower** | The next three hits deal ×1.5 (an orange glow shows the charges). | `AttackEnhancer` (`before_deal_damage` + `modify_damage`, charges) |
| 3 | **Mark golem** | The next three hits on the golem deal +15 before the multiplier (yellow diamond). | `Mark` (`before_take_damage`, `RemoveWhenSpent`) |
| 4 | **Venom dart** | Each dart adds a venom stack; ticks deal 4 per stack (green glow, stack count on the label). | `StackingPoison` (`Stack` policy, `AmountPerStack`) |
| 5 | **Place formation** | A ring under the golem pulses 12 damage and a slow on enemies inside for 6 s. | `FormationZone` (`zone` op, `AbilityZoneView`) |
| 6 | **Golem smashes hero (70)** | The third smash drops the hero below 40%: a 120 shield bubble appears, with a 10 s internal cooldown. | `AutoDefenseShield` (`HealthBelow` trigger) |

The bottom-left panel shows `AbilitySystem.Trace.Explain` for the latest reaction, the log lists the receipts and **Reset demo** restores everything. The assets are in `Scenes/Abilities/Assets/Triggers`; the wiring is saved in the scene. `AbilitiesTriggersDemoGuide` holds the button handlers and readouts.

## Damage pipeline with hooks

Every damage application runs through the system's `DamagePipeline` (`system.Damage`): effect ops, basic attacks, projectiles, DoT ticks, reflection and direct `DamageService.ApplyDamage` calls.

1. Guards: dead target, no health pool, `invulnerable`, `magic_immune` for magical damage.
2. **`before_deal_damage`** on the attacker (Target = attacker, Source = victim, Amount = raw).
3. **`before_take_damage`** on the victim (Target = victim, Source = attacker).
4. `AbilityRules.DamageFormula`: `(raw + FlatAdd) × Multiplier` → evasion → outgoing multiplier → crit → mitigation → incoming multiplier.
5. The hook's **Absorb** (a flat block), then shield modifiers (`shield_hp`, skipped by pure damage), then health and death.
6. Receipts: `take_damage`, `deal_damage`, `attack_received` and `attack_landed` (direct hits only), `critical_hit`, `kill`.
7. Lifesteal, then **reflection**: `ReflectFraction` × mitigated damage goes back to the attacker as `ReflectDamageType` (pure by default). It is flagged `Reflected` and never reflected again.

While a before hook runs, `system.ActiveDamage` (a `DamageContext`) can be changed:

| Member | Effect |
|--------|--------|
| `Multiplier` | Scales `(raw + FlatAdd)` before the formula; 1 = unchanged. |
| `FlatAdd` | Bonus added to the raw amount; mitigation still applies. |
| `Absorb` | Flat amount blocked after mitigation, counted in `Absorbed`. |
| `ReflectFraction`, `ReflectDamageType` | Share of the mitigated damage sent back to the attacker. |
| `Cancel()` | Nothing is dealt and no receipts are published; `DamageResult.Cancelled` is true. |
| `DamageType` | Hooks may convert the type; immunities are checked again. |
| `Source`, `Target`, `RawAmount`, `Flags`, `AbilityId`, `CastId`, `Random` | Read-only facts of the hit. |

Code-first hook:

```csharp
AbilitySystem system = AbilitySystemBehaviour.I.System;
system.Events.Subscribe(AbilityEvents.BeforeTakeDamage, e =>
{
    DamageContext damage = system.ActiveDamage;      // valid only inside this handler
    if (damage.HasFlag(DamageFlags.Hit) && e.Target == boss.UnitId)
    {
        damage.Multiplier *= 0.5f;                     // the boss halves direct hits
    }
});
```

Data-driven hook: a modifier reaction on `before_deal_damage` or `before_take_damage` with one of these ops.

| Op | Node fields | Effect |
|----|-------------|--------|
| `modify_damage` | `CustomParam` = `multiply` (default), `add`, `absorb` or `cancel`; `Amount` (leveled, per stack) | Changes the hit in flight. Does nothing outside a before hook. |
| `reflect` | `Amount` = fraction (0.3 = 30%), `DamageType` | Reflects a share of the mitigated damage to the attacker. |

**Ordering.** Event-id handlers run first, in subscription order. Then the owner's modifier reactions run by **Priority** (higher first), then by modifier application order, then by list order.

**Depth caps.** Reactions stop at `EffectContext.MaxDepth` (3) nested executions, damage at `DamagePipeline.MaxDepth` (8) nested applications, casts started from effects at `AbilitySystem.MaxCastDepth` (4).

**Optional receipts.** `before_deal_damage`, `before_take_damage`, `attack_landed` and `attack_received` are produced only when something consumes them: a reacting modifier of that unit, a handler of that exact event id, or the enabled trace. Global (`SubscribeAny`) and Target observers receive them when they are published but do not request them, so scene unit wrappers cost nothing extra per hit.

**`DamageFlags`** for `DamageService.ApplyDamage(system, ..., flags)` and `system.Damage.Apply`: `Hit` (a direct hit that publishes the attack events; the built-in `damage`, `execute` and `chain` ops set it for casts and projectile hits), `Reflected`, `NoHooks`, `NoCrit`, `NoEvasion`, `NoLifesteal`, `NoReflect`.

### Combat properties

The pipeline reads these unit properties on every application. All default to 0, so they do nothing until a template or modifier contributes them.

| Property | Read from | Meaning |
|----------|-----------|---------|
| `crit_chance` | attacker | Chance `[0..1]` of a critical hit. One roll per application, before mitigation. Fires `critical_hit`. |
| `crit_multiplier` | attacker | Damage multiplier on a crit (2 = double), at least 1. |
| `evasion_chance` | victim | Chance `[0..1]` to evade **physical** damage completely, rolled before mitigation. Fires `evaded`. |
| `lifesteal_percent` | attacker | `0..100`: percent of dealt health damage returned to the attacker as healing (fires `heal_received`). Skipped for self-damage and dead attackers. |

Crit and evasion roll on the cast's deterministic generator. Effect ops, ticks and reactions supply one; a direct `DamageService.ApplyDamage` call without an `IRandomSource` never crits or evades.

Damage and healing receipts report the **accepted change** of the resource, after pool caps. A synchronous resource callback may change the balance again: removing 10 HP from 50 and healing 20 inside that callback leaves 60 HP, while `HealthDamage` and `take_damage` still report 10. Lifesteal, `heal` and `AbilityUnitBehaviour.ApplyHeal` follow the same rule; death and later healing check the live state. Read the resource pool for the final balance.

## Triggers

`ModifierEventReaction` keeps its basic fields (`EventId`, `Effects`, `TargetEventSource`) and adds optional trigger settings. With the defaults it is a plain event reaction.

| Field | Meaning |
|-------|---------|
| **Trigger** | `Event` (every matching event), `EveryNth`, `HealthBelow`, `StateEnter`, `StateExit`, `Applied`, `Removed`. |
| **Every N** | `EveryNth`: fire on every Nth matching event. The counter lives on the modifier instance. |
| **Threshold** | `HealthBelow`: a fraction of max health (0.3 = 30%). |
| **State Id** | `StateEnter` / `StateExit`: the watched boolean state. |
| **Tag Filter** | Only events whose `Tag` matches (animation moments, state events). |
| **Ability Filter** | Only events of this ability id, e.g. a basic attack. |
| **Hits Only** | Damage hooks fire only for direct hits, not for DoT ticks, reactions or reflection. |
| **Max Triggers**, **Remove When Spent** | Charges per instance; a spent instance can remove itself, like a mark consumed by the next N hits. |
| **Cooldown** | Internal cooldown in seconds between fires of this reaction on one instance. |
| **Priority** | Order among reactions to the same event on the same unit. |

How the triggers behave:

- **`attack_landed` / `attack_received`:** a direct hit of a cast or projectile landed, also when shields absorbed all of it. DoT ticks, reactions and reflection publish `deal_damage` and `take_damage` but not these.
- **`HealthBelow`** is edge-triggered on `take_damage`: it fires when one hit takes health from at least the threshold to below it. Healing back above the threshold re-arms it. Pool changes outside the damage pipeline are not seen.
- **`EveryNth`** counts every matching event per instance (`ModifierInstance.GetReactionCounter`).
- **`StateEnter` / `StateExit`** fire on changes of the owner's state, from modifier states and `AbilityUnit.SetPermanentState`, after the instance was applied. They also publish `state_entered` / `state_exited` with Tag = the state id.
- **`Applied` / `Removed`** run when this instance is created (not on refresh or stack) and when it is removed for any reason. The event source is the modifier caster. Plain `Event` reactions to `modifier_applied` / `modifier_removed` are never dispatched, because they would recurse; use these triggers instead.
- **Animation moments:** the presentation layer calls `system.RaiseAnimationEvent(unit, "hit_frame")`, `AbilityUnitBehaviour.RaiseAnimationEvent("hit_frame")` (from an Animation Event or a Timeline signal) or `AbilityAnimationEventRelay.Raise` on a child Animator. Reactions with `EventId = animation_event` and `TagFilter = hit_frame` fire.

For UI, `ModifierInstance` exposes `GetReactionFireCount(i)`, `GetReactionCounter(i)` and `GetRemainingCharges(i)`.

## Payload ops, cone selector and zones

A **payload ability** is an ability used as a reusable bundle of impact effects. It does not need to be granted.

| Op | Node fields | Effect |
|----|-------------|--------|
| `projectile` | `AbilityId` (payload); `ArchetypeId`, else the payload's archetype; `Amount` = speed, else the payload's speed | Launches one projectile per resolved target, or one at the target point. See below. |
| `area_apply` | an area or `ConeFromCaster` selector; `ModifierId` and/or `AbilityId` | Applies the modifier to every resolved unit and runs the payload once with all of them as primary targets. |
| `zone` | `Radius`, `Duration`, `Interval`, `TeamFilter`, `ModifierId`, `AbilityId`, `ArchetypeId`, `CustomParam` | Spawns a domain zone. See [Zones](#zones). |
| `cast_ability` | `AbilityId`, a granted ability of the caster | Casts through the normal pipeline (cooldown, costs, range): unit abilities at each target, point and direction abilities toward each target, no-target abilities once. For auto-cast passives and "on hit, cast X". |

**`projectile`:** the projectile is spawned from the caster through `IAbilityWorldAdapter.RequestSpawn`. The host reports the hit with `NotifyProjectileHit(castId, ...)`, and the payload's impact effects run there as a direct hit.

**`ConeFromCaster`** selects units within `Radius` of the caster and within `Angle` degrees around the aim (the target point, else the first primary target), team-filtered; `MaxTargets` keeps the nearest N.

### Zones

API: `system.SpawnZone(in AbilityZoneSpec)`, `RemoveZone`, `RemoveZonesOf`, `GetZones`, `TryGetZone`, `ZoneCount`.

- **Placement.** Selector `Caster` makes an aura that follows the caster; `CustomParam = fixed` leaves it in place. Other selectors make a formation at the target point or the first target; `CustomParam = follow` makes it follow that target.
- **Pulses.** A zone pulses on spawn and then every `Interval` seconds. Each pulse applies `ModifierId` to every unit inside (team filter relative to the caster's team) and runs the payload's impact effects with those units as primary targets and the zone centre as the target point.
- **Lifetime.** A zone ends after `Duration` (0 = until removed) or when its anchor unit dies. It publishes `zone_spawned` and `zone_expired`, with CastId = the zone id.
- **Visual.** With an `ArchetypeId` the zone asks the world adapter to spawn a prefab (`SpawnRequest.ZoneId`, `Duration`, `Magnitude` = radius, `TargetUnit` = the followed anchor). An [AbilityZoneView](./AbilitySystemBehaviour.md#zone-visuals) on it scales to the radius, follows the anchor and returns to the pool when the zone ends.
- **Auras** keep a short `Refresh` buff refreshed, so the buff fades a moment after a unit leaves.

`EffectNodeData.AmountPerStack` scales tick and reaction amounts per extra stack: `amount += AmountPerStack × (stacks − 1)`, e.g. a poison dealing 4 per stack. Stack policies and **Restart Ticks On Refresh** are described in [ModifierDefinition](./ModifierDefinition.md#stack-policies).

## Templates

`AbilityTemplates` builds ready content from the public data model. `AbilityTemplates.Catalog` lists them; each returns an `AbilityTemplateBundle` (abilities, modifiers, abilities to grant, innate modifiers) with `RegisterInto(system)` and `SetUp(system, unit)`.

| Template | What you get |
|----------|--------------|
| Auto-cast technique | Technique `spirit_bolt` and passive `spirit_bolt_autocast` that casts it at the nearest enemy every interval (`cast_ability` from a tick). |
| Attack enhancer | Buff `empower` and cast `empower_cast`: the next N direct hits deal ×multiplier, then it ends. |
| Auto-defense shield | Passive `guardian_barrier_passive`: a hit that drops health below the threshold grants the shield `guardian_barrier`, with an internal cooldown. |
| Mark (next N hits) | Ability `hunter_mark` applies `hunter_mark_mark`: the next N hits on the target deal +bonus, then the mark is consumed. |
| Stacking poison | Poison `venom` (`Stack`, `AmountPerStack`), dart `venom_dart` and innate coating `venom_coating` (`attack_landed` applies a stack). |
| Thorns (reflect) | Passive `thorns` that reflects a share of each hit taken. |
| Aura | Ability `war_banner` that spawns a zone around the caster, keeping `war_banner_buff` on allies. |
| Formation zone | Ability `thunder_formation` that places a zone pulsing damage and a slow (`thunder_formation_pulse` payload) on enemies. |

```csharp
AbilityTemplates.StackingPoison("venom", tickDamage: 4f, maxStacks: 5).SetUp(system, hero.UnitId);
AbilityTemplates.Thorns("thorns", fraction: 0.3f).SetUp(system, golem.UnitId);
```

In the Ability Designer, **+ Template** writes the bundle as assets into a folder you choose: one `AbilityDefinition` or `ModifierDefinition` per blueprint, plus an `AbilityLibrary`. Passives such as thorns go into a [UnitTemplate](./UnitTemplate.md)'s **Innate Modifiers**.

## Validator

`AbilityContentValidator.Validate(abilities, modifiers, ops)` returns `AbilityContentIssue` findings (Error or Warning). The Ability Designer status bar shows them next to its asset checks. It reports:

- unknown op ids; missing or unknown payload abilities, cast abilities and modifiers;
- `HealthBelow` not on `take_damage`, or with a threshold outside (0, 1]; state triggers without a state;
- negative charges or cooldowns, `RemoveWhenSpent` without charges, `HitsOnly` outside damage hooks, tag filters on events without tags;
- `modify_damage` or `reflect` outside before hooks, unknown `modify_damage` modes, a multiply by 0;
- `take_damage` reactions that damage their own owner;
- zones without a radius or payload, cones without an angle, `cast_ability` cycles.

## Trace: why did this fire

`system.Trace` is a ring buffer: 256 entries by default, disabled by default. `AbilitySystemBehaviour` has **Trace Enabled** and **Trace Capacity**. Each `AbilityTraceEntry` records one step with the sequence number of its cause: casts and rejected casts, effect ops, published events, fired reactions with their trigger, skipped triggers (charges spent, internal cooldown, Nth counter), modifier ticks, zone spawns, pulses and expiries, projectile hits and cancellations.

```csharp
system.Trace.Enabled = true;
// ... play ...
if (system.Trace.TryFindLatest(AbilityTraceKind.Reaction, "hunter_mark_mark", out AbilityTraceEntry entry))
{
    Debug.Log(system.Trace.Explain(entry.Sequence));
    // #57 reaction 'hunter_mark_mark'[0] on Unit(2): event
    //   because #56 event before_take_damage on Unit(2) from Unit(1) amount=10
    //   because #55 effect damage x1 by Unit(1)
    //   because #53 cast 'strike' by Unit(1)
}
```

`Explain(sequence, maxDepth, unitName)` takes a `Func<UnitId, string>` for readable names ("cast 'strike' by Hero"); `AbilityTraceEntry.ToString(unitName)` formats one entry the same way. Recording does not allocate; `Explain`, `Dump` and `ToString` build text on demand. For a shield bar, `DamageService.RemainingShield(instance)` returns the shield HP a modifier can still absorb.

## Performance

Hooks, triggers, zones and the trace keep the domain hot paths allocation-free once warm. Optional receipts are skipped when nothing consumes them, and counters answer "does anything react to this hook" without scanning. Reference numbers on the development machine (Unity 6000.3, editor Mono, median per system tick, no allocations):

| Workload | Median per tick |
|----------|-----------------|
| 1024 units, one damage-over-time each | 2537 µs |
| 256 units × 1 modifier | 744 µs |
| 1024 units × 4 modifiers (one `take_damage` reaction) | 5293 µs |
| 1024 units × 8 modifiers (three `take_damage` reactions) | 7018 µs |

Units whose modifiers react to `take_damage` pay for the trigger bookkeeping (charges, cooldowns, counters, ordering); units without reactions do not.

## По-русски

Весь урон проходит через `DamagePipeline`. Перед расчётом публикуются хуки `before_deal_damage` (на атакующем) и
`before_take_damage` (на цели); пока они идут, `system.ActiveDamage` можно менять: множитель, прибавка, поглощение,
отражение, отмена. Из данных это делают реакции модификаторов с операциями `modify_damage` и `reflect`. Глубина
ограничена: реакции — 3, урон — 8, касты из эффектов — 4.

Триггеры реакций: каждое событие, каждое N-е, порог здоровья (`HealthBelow`), вход и выход из состояния, наложение и
снятие, моменты анимации. Есть фильтры, заряды, внутренний кулдаун и приоритет. Операции `projectile`, `area_apply`,
`zone` и `cast_ability` запускают снаряд, область, зону-ауру или формацию и каст другой способности. Готовые шаблоны —
`AbilityTemplates` и кнопка **+ Template** в Ability Designer; `AbilityContentValidator` проверяет данные, а
`system.Trace.Explain` объясняет, почему сработала реакция. Демо — `AbilitiesTriggersDemo.unity`, шесть уроков.
