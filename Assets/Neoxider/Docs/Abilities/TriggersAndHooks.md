# Triggers, damage hooks, zones and templates

**What it is:** the reactive layer of `Neo.Abilities` added in 12.1: damage pre-hooks with a mutable damage context, modifier triggers (hit landed, health threshold, every Nth event, state enter / exit, animation moments, applied / removed, charges, internal cooldowns, priorities), payload ops (projectile, area apply, zone, cast ability), domain zones for auras and formations, ready templates, a content validator and a runtime trace that answers "why did this fire". Everything is data on `ModifierBlueprint` / `EffectNodeData`, so it works from assets, from code and over the network the same way.

**How to use:** author reactions on a [ModifierDefinition](./ModifierDefinition.md) (**Event Reactions** list) and effect nodes on an [AbilityDefinition](./AbilityDefinition.md); or start from a template in the [Ability Designer](./AbilityDesignerWindow.md) (**+ Template**) or from code (`AbilityTemplates`). Open the demo `Scenes/Abilities/AbilitiesTriggersDemo.unity` (Demo Scenes sample) for a guided tour.

- Namespace: `Neo.Abilities`
- Sources: `Assets/Neoxider/Scripts/Abilities/Domain` (`Combat/DamagePipeline.cs`, `AbilitySystem.Triggers.cs`, `AbilitySystem.Zones.cs`, `Effects/Ops/*`, `Templates/AbilityTemplates.cs`, `Validation/AbilityContentValidator.cs`, `Diagnostics/AbilityTrace.cs`)
- Replaceable formulas and systems: [Rules and extension points](./Rules.md)

## Demo scene

`Scenes/Abilities/AbilitiesTriggersDemo.unity` (Demo Scenes sample; developer layout `Assets/Neoxider/Samples/Demo`, release source `Assets/Neoxider/Samples~/Demo`, imported copies under `Assets/Samples/NeoxiderTools/<version>/Demo Scenes`) is a six-lesson tutorial on a 2D arena: a Hero, a Golem with thorns and an Imp.

| Lesson | Button | What happens | Built from |
|--------|--------|--------------|------------|
| 1 | **Strike golem (20)** | The golem loses 20 minus armor, the hero loses 30% of it back. | `Thorns` template (`before_take_damage` + `reflect`) |
| 2 | **Empower** | The next three hits deal ×1.5 (the orange glow shows the charges). | `AttackEnhancer` (`before_deal_damage` + `modify_damage`, charges) |
| 3 | **Mark golem** | The next three hits on the golem deal +15 before the multiplier (yellow diamond). | `Mark` (`before_take_damage`, `RemoveWhenSpent`) |
| 4 | **Venom dart** | Each dart adds a venom stack; ticks deal 4 per stack (green glow, stack count on the label). | `StackingPoison` (Stack policy, `AmountPerStack`) |
| 5 | **Place formation** | A ring under the golem pulses 12 damage and a slow on enemies inside for 6 s. | `FormationZone` (`zone` op, `AbilityZoneView`) |
| 6 | **Golem smashes hero (70)** | The third smash drops the hero below 40%: a 120 shield bubble appears (10 s internal cooldown). | `AutoDefenseShield` (`HealthBelow` trigger) |

The bottom-left panel shows `AbilitySystem.Trace.Explain` for the latest reaction, the log lists the receipts and **Reset demo** restores everything. The scene is authored by `Editor/Builders/Abilities/AbilitiesTriggersDemoBuilder.cs` (assets in `Scenes/Abilities/Assets/Triggers`, wiring saved in the scene, readable without Play Mode); `AbilitiesTriggersDemoGuide` holds the button handlers and readouts. The PlayMode test `AbilitiesTriggersDemoPlayTests` presses every lesson and checks the domain state.

## Damage pipeline with hooks

Every damage application (effect ops, basic attacks, projectiles, DoT ticks, reflection, direct `DamageService.ApplyDamage`) runs through the system's `DamagePipeline` (`system.Damage`):

1. Guards: dead target, no health pool, `invulnerable`, `magic_immune` for magical damage.
2. **`before_deal_damage`** on the attacker (Target = attacker, Source = victim, Amount = raw).
3. **`before_take_damage`** on the victim (Target = victim, Source = attacker).
4. `AbilityRules.DamageFormula`: `(raw + FlatAdd) × Multiplier` → evasion → outgoing multiplier → crit → mitigation → incoming multiplier.
5. Hook **Absorb** (flat block), then shield modifiers (`shield_hp`, skipped by pure damage), then health, death.
6. Receipts: `take_damage`, `deal_damage`, `attack_received` and `attack_landed` (direct hits only), `critical_hit`, `kill`.
7. Lifesteal, then **reflection**: `ReflectFraction` × mitigated damage goes back to the attacker as `ReflectDamageType` (pure by default), flagged `Reflected` so it is never reflected again.

While a before hook runs, `system.ActiveDamage` (a `DamageContext`) can be adjusted:

| Member | Effect |
|--------|--------|
| `Multiplier` | Scales `(raw + FlatAdd)` before the formula (1 = unchanged). |
| `FlatAdd` | Bonus added to the raw amount; mitigation still applies. |
| `Absorb` | Flat amount blocked after mitigation, counted in `Absorbed`. |
| `ReflectFraction`, `ReflectDamageType` | Share of the mitigated damage sent back to the attacker. |
| `Cancel()` | Nothing is dealt, no receipts; `DamageResult.Cancelled` is true. |
| `DamageType` | Hooks may convert the type (immunities are re-checked). |
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

Data-driven hook: a modifier reaction on `before_deal_damage` / `before_take_damage` with the ops below.

| Op | Node fields | Effect |
|----|-------------|--------|
| `modify_damage` | `CustomParam` = `multiply` (default) / `add` / `absorb` / `cancel`, `Amount` (leveled, per stack) | Adjusts the hit in flight. Outside a before hook it does nothing. |
| `reflect` | `Amount` = fraction (0.3 = 30%), `DamageType` | Reflects a share of the mitigated damage to the attacker. |

**Ordering.** Event-id handlers run first in subscription order; then modifier reactions of the owner by **Priority** (higher first), then modifier application order, then list order. **Caps:** reactions stop at `EffectContext.MaxDepth` (3) nested executions, damage at `DamagePipeline.MaxDepth` (8) nested applications, casts started from effects at `AbilitySystem.MaxCastDepth` (4); reflected damage is never reflected again.

**Optional receipts.** `before_deal_damage`, `before_take_damage`, `attack_landed` and `attack_received` are only produced when someone consumes them: a reacting modifier of that unit, a handler subscribed to that exact event id, or the enabled trace. Global (`SubscribeAny`) and Target observers receive them whenever they are published but do not request them, so scene unit wrappers cost nothing extra per hit.

`DamageFlags` on `DamageService.ApplyDamage(system, ..., flags)` / `system.Damage.Apply`: `Hit` (a direct hit: publishes the attack events; the built-in damage, execute and chain ops set it for casts and projectile hits), `Reflected`, `NoHooks`, `NoCrit`, `NoEvasion`, `NoLifesteal`, `NoReflect`.

## Triggers

`ModifierEventReaction` keeps its classic fields (`EventId`, `Effects`, `TargetEventSource`) and gains optional trigger settings; all defaults reproduce the old behaviour.

| Field | Meaning |
|-------|---------|
| **Trigger** | `Event` (every matching event), `EveryNth`, `HealthBelow`, `StateEnter`, `StateExit`, `Applied`, `Removed`. |
| **Every N** | `EveryNth`: fire on every Nth matching event; the counter lives on the modifier instance. |
| **Threshold** | `HealthBelow`: fraction of max health (0.3 = 30%). |
| **State Id** | `StateEnter` / `StateExit`: the watched boolean state. |
| **Tag Filter** | Only events whose `Tag` matches (animation moments, state events). |
| **Ability Filter** | Only events of this ability id (e.g. a basic attack, "on use skill X"). |
| **Hits Only** | Damage hooks fire only for direct hits, not DoT ticks, reactions or reflection. |
| **Max Triggers**, **Remove When Spent** | Charges per instance; a spent instance can remove itself (a mark consumed by the next N hits). |
| **Cooldown** | Internal cooldown in seconds between fires of this reaction on one instance. |
| **Priority** | Ordering among reactions to the same event on the same unit. |

Trigger semantics:

- **attack_landed / attack_received** — a direct hit of a cast or projectile landed (also when shields absorbed all of it). DoT ticks, reactions and reflection publish `deal_damage` / `take_damage` but not these. Filter by ability with **Ability Filter**.
- **HealthBelow** is edge-triggered on `take_damage`: it fires when one hit takes health from at least the threshold to below it. Healing back above re-arms it. Changes of the pool outside the damage pipeline are not seen.
- **EveryNth** counts every matching event per instance (`ModifierInstance.GetReactionCounter`).
- **StateEnter / StateExit** fire on transitions of the owner's state (modifier states and `AbilityUnit.SetPermanentState`) after the instance was applied; they also publish `state_entered` / `state_exited` (Tag = state id).
- **Applied / Removed** run when this instance is created (not on refresh or stack) and when it is removed for any reason. The event source is the modifier caster.
- **Animation moments**: the presentation layer calls `system.RaiseAnimationEvent(unit, "hit_frame")`, `AbilityUnitBehaviour.RaiseAnimationEvent("hit_frame")` (Animation Event / Timeline signal) or `AbilityAnimationEventRelay.Raise` on a child Animator. Reactions with `EventId = animation_event` and `TagFilter = hit_frame` fire.

Classic `Event` reactions to `modifier_applied` / `modifier_removed` are still never dispatched (they would recurse); use the `Applied` / `Removed` triggers.

`ModifierInstance` exposes `GetReactionFireCount(i)`, `GetReactionCounter(i)` and `GetRemainingCharges(i)` for UI.

## Payload ops, cone selector and zones

A **payload ability** is an ability used as a reusable bundle of impact effects: it does not need to be granted.

| Op | Node fields | Effect |
|----|-------------|--------|
| `projectile` | `AbilityId` (payload), `ArchetypeId` (else the payload's projectile archetype), `Amount` = speed (else the payload's speed) | One projectile per resolved target (or one at the target point) from the caster through `IAbilityWorldAdapter.RequestSpawn`; the host reports the hit with `NotifyProjectileHit(castId, ...)` and the payload's impact effects run there (a direct hit). |
| `area_apply` | selector (area or `ConeFromCaster`), `ModifierId` and/or `AbilityId` | Applies the modifier to every resolved unit and runs the payload once with all of them as primary targets ("apply modifier to an area"). |
| `zone` | `Radius`, `Duration`, `Interval`, `TeamFilter`, `ModifierId`, `AbilityId`, `ArchetypeId`, `CustomParam` | Spawns a domain zone. Selector `Caster` ⇒ an aura following the caster (`CustomParam = fixed` leaves it in place); other selectors ⇒ a formation at the target point or the first target (`CustomParam = follow` follows that target). |
| `cast_ability` | `AbilityId` (a granted ability of the caster) | Casts through the normal pipeline (cooldown, costs, range): unit abilities at each target, point / direction abilities toward each target, no-target abilities once. Auto-cast passives and "on hit, cast X". |

**ConeFromCaster** selector: units within `Radius` of the caster and within `Angle` degrees around the aim (target point, else first primary target), team-filtered; `MaxTargets` keeps the nearest N.

**Zones** (`system.SpawnZone(in AbilityZoneSpec)`, `RemoveZone`, `RemoveZonesOf`, `GetZones`, `TryGetZone`, `ZoneCount`): a zone pulses on spawn and then every `Interval` seconds while it lives; each pulse applies `ModifierId` to every unit inside (team filter relative to the caster's team) and runs the payload's impact effects with those units as primary targets and the zone centre as the target point. A zone ends after `Duration` (0 = until removed) or when its anchor unit dies. It publishes `zone_spawned` / `zone_expired` (CastId = zone id) and, with an `ArchetypeId`, asks the world adapter to spawn a visual (`SpawnRequest.ZoneId`, `Duration`, `Magnitude` = radius, `TargetUnit` = followed anchor). [AbilityZoneView](./AbilitySystemBehaviour.md#zone-visuals) on that prefab scales it to the radius, follows the anchor and returns it to the pool when the zone ends. Auras keep a short `Refresh` buff refreshed: the buff fades a moment after a unit leaves.

`EffectNodeData.AmountPerStack` scales tick and reaction amounts per extra stack (`amount += AmountPerStack × (stacks − 1)`), e.g. a poison dealing 4 per stack.

## Stack policies

`Independent`, `Refresh`, `Stack` as before, plus `Unique` (re-application while active does nothing), `Prolong` (adds the full duration to the remaining time) and `IndependentCapped` (independent instances up to Max Stacks; one more replaces the oldest). The decision is a replaceable rule, see [Rules](./Rules.md#modifier-stacking).

`ModifierBlueprint.RestartTicksOnRefresh` (Restart Ticks On Refresh): a re-application that refreshes, stacks or prolongs restarts the tick phase, so the next tick comes a full interval after it; by default the phase continues. The fixed core has the same flag (`FixedModifierBlueprint.RestartTicksOnRefresh`).

## Templates

`AbilityTemplates` builds ready content from the public data model; `AbilityTemplates.Catalog` lists them and each returns an `AbilityTemplateBundle` (abilities, modifiers, abilities to grant, innate modifiers) with `RegisterInto(system)` and `SetUp(system, unit)`.

| Template | What you get |
|----------|--------------|
| Auto-cast technique | Technique `spirit_bolt` and passive `spirit_bolt_autocast` casting it at the nearest enemy every interval (`cast_ability` from a tick). |
| Attack enhancer | Buff `empower` (+ cast `empower_cast`): the next N direct hits deal ×multiplier, then it ends (`before_deal_damage` + `modify_damage`, charges). |
| Auto-defense shield | Passive `guardian_barrier_passive`: a hit dropping health below the threshold grants shield `guardian_barrier`, with an internal cooldown (`HealthBelow`). |
| Mark (next N hits) | Ability `hunter_mark` applies `hunter_mark_mark`: the next N hits on the target deal +bonus, then the mark is consumed. |
| Stacking poison | Poison `venom` (Stack, `AmountPerStack`), dart `venom_dart` and innate coating `venom_coating` (`attack_landed` applies a stack). |
| Thorns (reflect) | Passive `thorns` reflecting a share of each hit taken. |
| Aura | Ability `war_banner` spawning a zone around the caster that keeps `war_banner_buff` on allies. |
| Formation zone | Ability `thunder_formation` placing a zone that pulses damage and slow (`thunder_formation_pulse` payload) on enemies. |

```csharp
AbilityTemplates.StackingPoison("venom", tickDamage: 4f, maxStacks: 5).SetUp(system, hero.UnitId);
AbilityTemplates.Thorns("thorns", fraction: 0.3f).SetUp(system, golem.UnitId);
```

In the Ability Designer, **+ Template** writes the bundle as assets (one `AbilityDefinition` / `ModifierDefinition` per blueprint plus an `AbilityLibrary`) into a folder you choose.

## Validator

`AbilityContentValidator.Validate(abilities, modifiers, ops)` returns `AbilityContentIssue` findings (Error / Warning): unknown op ids, missing or unknown payload / cast abilities and modifiers, `HealthBelow` not on `take_damage` or with a threshold outside (0, 1], state triggers without a state, negative charges or cooldowns, `RemoveWhenSpent` without charges, `HitsOnly` outside damage hooks, tag filters on events without tags, `modify_damage` / `reflect` outside before hooks, unknown `modify_damage` modes, a multiply by 0, take_damage reactions damaging their own owner, zones without radius or payload, cones without angle, `cast_ability` cycles. The Ability Designer status bar shows them next to its asset checks.

## Trace: why did this fire

`system.Trace` is a ring buffer (default 256 entries, disabled by default; `AbilitySystemBehaviour` has **Trace Enabled** / **Trace Capacity**). Each `AbilityTraceEntry` records casts, rejected casts, effect ops, published events, fired reactions with their trigger, skipped triggers (charges spent, internal cooldown, Nth counter), modifier ticks, zone spawns / pulses / expiries, projectile hits and cancellations, each with the sequence of its cause.

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

`Explain(sequence, maxDepth, unitName)` takes a `Func<UnitId, string>` for readable names ("cast 'strike' by Hero"); `AbilityTraceEntry.ToString(unitName)` formats a single entry the same way. `DamageService.RemainingShield(instance)` returns the shield HP a modifier can still absorb (capacity minus consumed), e.g. for a shield bar.

Recording is allocation-free (references to existing ids, no formatting); `Explain`, `Dump` and `ToString` build text on demand.

## Performance

Hooks, triggers, zones and the trace keep the domain hot paths allocation-free once warm (`AbilityTriggerAllocationTests`, plus the existing `AbilityHotPathAllocationTests`). Optional receipts are skipped when nothing consumes them; per-owner and engine-wide counters answer "does anything react to this hook" without scanning. `AbilityTickCostBenchmarks` (category `Performance`) measures a system tick at N units × M modifiers. Reference numbers on the development machine (Unity 6000.3, editor Mono, median per tick, 0 allocations in every row):

| Workload | 12.0.0 | 12.1 |
|----------|--------|------|
| 1024 units, one damage-over-time each | 2605 µs | 2537 µs |
| 256 units × 1 modifier | 735 µs | 744 µs |
| 1024 units × 4 modifiers (one take_damage reaction) | 4180 µs | 5293 µs |
| 1024 units × 8 modifiers (three take_damage reactions) | 6262 µs | 7018 µs |

Units whose modifiers react to `take_damage` pay for the trigger bookkeeping (charges, cooldowns, counters, ordering); units without reactions do not.

## По-русски

Слой реакций Neo.Abilities (12.1). Весь урон проходит через `DamagePipeline` (`system.Damage`). Перед расчётом публикуются хуки `before_deal_damage` (на атакующем) и `before_take_damage` (на цели); пока они идут, `system.ActiveDamage` можно менять: множитель, плоская прибавка, поглощение, доля отражения, отмена. Из данных это делают реакции модификаторов с операциями `modify_damage` (режимы multiply / add / absorb / cancel) и `reflect`. Порядок детерминирован: сначала подписчики события, затем реакции модификаторов по приоритету, порядку наложения и порядку в списке. Глубина ограничена: реакции — 3 уровня, урон — 8, касты из эффектов — 4; отражённый урон повторно не отражается. Необязательные события (хуки и `attack_landed` / `attack_received`) публикуются, только если на них подписан обработчик именно этого события, реагирует модификатор юнита или включён журнал.

Триггеры реакций: каждое событие, каждое N-е (`EveryNth`, счётчик на экземпляре), порог здоровья (`HealthBelow`, срабатывает на переходе через порог при получении урона и заново после лечения выше порога), вход и выход из состояния, наложение и снятие модификатора, моменты анимации (`RaiseAnimationEvent`, событие `animation_event` с фильтром тега). Есть фильтр способности, «только прямые попадания», заряды с удалением модификатора после последнего, внутренний кулдаун и приоритет. `attack_landed` — прямое попадание каста или снаряда (тики DoT, реакции и отражение не считаются).

Новые операции: `projectile` (снаряд с полезной нагрузкой — эффектами попадания другой способности), `area_apply` (модификатор и нагрузка по области или конусу), `zone` (зона: аура за кастером или формация в точке, импульс при появлении и далее с интервалом, время жизни, визуал через `AbilityZoneView`), `cast_ability` (каст выданной способности через обычный конвейер). Селектор `ConeFromCaster`, `AmountPerStack` для урона за стак, политики стаков `Unique`, `Prolong`, `IndependentCapped`. Готовые шаблоны (`AbilityTemplates` и кнопка **+ Template** в Ability Designer): автокаст техники, усилитель атак, автощит, метка на N ударов, стакающийся яд, шипы, аура, зона-формация. `AbilityContentValidator` проверяет данные, а журнал `system.Trace` объясняет, почему сработала реакция (`Explain`). Горячие пути не выделяют память; цифры производительности — в таблице выше.

Демо-сцена `Scenes/Abilities/AbilitiesTriggersDemo.unity` — шесть уроков на 2D-арене (герой, голем с шипами, бес): удар и отражение, усилитель атак, метка на три удара, стакающийся яд, зона-формация и автощит ниже 40% HP. Панель внизу слева показывает `Trace.Explain` последней реакции, кнопка **Reset demo** возвращает исходное состояние. Сцену собирает `AbilitiesTriggersDemoBuilder`, вся связка сохранена в сцене и видна без Play Mode.

Помощники: **Innate Modifiers** в `UnitTemplate` (пассивки вроде шипов и автощита накладываются при регистрации, кастер — сам юнит), **Plane** у `AbilityZoneView` (`GroundXZ` для 3D-пола, `ScreenXY` для 2D-спрайта), `DamageService.RemainingShield(instance)` для полоски щита и `Trace.Explain(sequence, maxDepth, unitName)` с именами юнитов вместо `Unit(id)`.

Флаг `RestartTicksOnRefresh` у модификатора: обновление, новый стак или продление перезапускает фазу тиков — следующий тик через полный интервал после повторного наложения; по умолчанию фаза продолжается. Тот же флаг есть в целочисленном ядре.
