# Deterministic mode (design note)

**What it is:** the plan and the current state of running `Neo.Abilities` inside a deterministic, server-authoritative or lockstep simulation, such as VampireDota's: Q8.8 fixed-point raw integers (1.0 = 256), integer ticks, no floats and no `UnityEngine` in the domain. It covers the numeric abstraction, tick-based time, the seeded random source, state hashing for desync checks and migration notes.

**How to use today:** apply `DeterministicAbilityProfile` to a system (fixed ticks, Q8.8 rounding, the seeded generator) and compare `AbilityStateHasher.Compute(system)` between peers. That makes float simulations replayable and desync-checkable on one runtime; it does not make the domain integer-only. The integer core is a proposal awaiting a decision (below).

- Namespace: `Neo.Abilities`
- Related: [Rules and extension points](./Rules.md)

## Requirements of a deterministic host

1. The same inputs (content, unit creation order, casts with seeds, tick sequence, world adapter answers) produce bit-identical state on every peer and platform.
2. Integer time: durations, cooldowns and intervals are whole ticks; no accumulation of float seconds.
3. Integer numbers: amounts, pools and properties are Q-format raw integers with defined rounding.
4. A seeded, portable random source owned by the simulation.
5. A cheap state hash to detect desyncs, and replays that reproduce a recorded match.
6. An engine-free domain assembly (`noEngineReferences`), compilable outside Unity.

## What 12.1 provides (float domain)

| Requirement | Mechanism | Status |
|-------------|-----------|--------|
| Ordered, seeded execution | Per-cast seeds (`CastRequest.Seed`), derived seeds per tick, reaction, projectile hit and zone pulse; deterministic ordering rules (adapter query order, nearest-N by distance then unit id, reaction priority / application / list order, zone spawn order) | done |
| Replaceable random | `AbilityRules.Random` (`IRandomSourceFactory`, `IReseedableRandomSource`); default `XorShiftRandom` (integer xorshift32, identical on every platform) | done |
| Integer-style time | `FixedStepTimeSource` + `AbilitySystem.StepTicks(n)` / `TickCount`: every step has the same length; `FixedStepTimeSource.ToTicks` converts authored seconds | done (time is still a float step) |
| Fixed-point values | `AbilityRules.Numeric = FixedPointNumeric.Q8`: computed amounts and property values are rounded to the 1/256 grid before they are stored or published | done (rounding, not integer arithmetic) |
| Desync check | `AbilityStateHasher.Compute(system)`: FNV-1a over the sorted simulation state | done |
| One switch | `DeterministicAbilityProfile.Apply(rules, ticksPerSecond)` | done |
| Engine-free integer domain | — | **open, see options** |

### Why rounding floats is not enough

IEEE single-precision `+ − × ÷ sqrt` are deterministic for the same operation order, and Mono / CoreCLR on x64 and ARM64 evaluate float expressions in single precision. Two risks remain across a server and IL2CPP clients: the C++ compiler may contract `a * b + c` into a fused multiply-add on ARM64, and different compilers may reorder or keep wider intermediates. Rounding to Q8.8 after each step hides most 1-ulp differences, but a value lying on a rounding boundary still flips. So the float profile is suitable for server-authoritative play where only the server simulates and clients predict or replay on the same build, and for single-runtime replays. It is not bit-exact lockstep across platforms, and it does not meet a policy of "no floats in the domain".

## Options for the integer core

| | A. Float profile only | B. Parallel integer core | C. Generic numeric core |
|--|--|--|--|
| Shape | 12.1 as above | New engine-free assembly `Neo.Abilities.Fixed` (no references, `noEngineReferences`) with Q-format raw ints (`FixedValue` int, configurable fraction bits), integer ticks, integer vectors and the same concepts: units, pools, base properties, Add / Mul / Max layering, modifiers with stack policies, triggers, damage hooks, ops, zones, events, rules interfaces typed on raw ints. `Neo.Abilities` stays as it is and can reference the core to convert `AbilityDefinition` / `ModifierDefinition` assets into integer blueprints at registration | One engine-free generic domain (`AbilitySystem<TNumber, TOps>` with a struct numeric-ops strategy); the float `Neo.Abilities` becomes a specialization / facade |
| Meets VampireDota policy | no | yes | yes |
| Breaking for float users | no | no (additive) | yes: types move to another assembly, `ModifierInstance`, `EffectContext`, `AbilityEventArgs`, `IEffectOperation` and rule signatures change; asmdef references must be updated |
| Version | 12.1 | 12.2.0 | 13.0.0 |
| Cost | none | a second implementation of the domain (about 5–6 k lines with tests) and two code paths to keep in sync; content written once (assets) and converted | the largest refactor; one code path afterwards; every existing test and sample touched |
| Risk | lockstep across platforms not guaranteed | drift between the two cores (mitigated by a shared conformance test suite that runs the same scenarios on both and compares results within the Q8.8 grid) | regressions for current users, release coordination |

**Recommendation:** B for the VampireDota migration timeline — it does not break anyone and lets the game keep its Q8.8 / tick model — with a shared conformance suite from day one, and C as a 13.0 consolidation once both cores are proven. The choice changes the public surface of the package, so it waits for root and the owner; nothing of B or C is implemented yet.

## Design of the integer core (option B, if chosen)

- **Numbers:** `FixedValue` = `int` raw with `FixedMath` helpers (saturating add, `MulDiv` with 64-bit intermediates, round half away from zero, the same as VampireDota's `DeterministicMath`). Fraction bits configurable per system (default 8).
- **Time:** every duration, cooldown, tick interval, zone interval and lifetime is an `int` tick count; the system advances by `Step()` / `Step(n)`; catch-up caps stay as today (8 per call).
- **Space:** `FixedVec3` raw ints; the world adapter interface is the same shape typed on `FixedVec3`; radius tests compare squared raw distances in 64-bit.
- **Random:** `IRandomSourceFactory` returns integer draws (`NextRaw(oneRaw)` in `[0, one)`, `NextInt`); chance and crit compare raw values.
- **Rules:** the same twelve extension points, typed on raw ints: VampireDota plugs its `CombatResolver` (armor `amount·100/(100+armor)`, resistance, crit with structure immunity flags) as `IDamageFormula`, its `StatBlock` rational multipliers as `IPropertyAggregator`, its stacking (`DenyNew`, `RefreshDuration`, `StackUpTo3`) as `IModifierStackingRule`.
- **State hash:** the same field order as `AbilityStateHasher`, over raw ints only.
- **Content:** blueprints with raw-int fields; `FixedContentConverter` (in `Neo.Abilities`) converts float assets with explicit rounding and reports every value that changes by more than half a raw unit.

## State hashing

`AbilityStateHasher.Compute(system)` folds, in a fixed order: the hash version, `TickCount`, the domain clock, then every unit sorted by id (id, team, alive, level, health and mana current / max, every modifier in list order: blueprint id, caster, stacks, remaining time, ability level, consumed shield, reaction counters and charges), every granted slot sorted by ability id (level, charges, cooldown), zones in spawn order (id, centre, radius, remaining time, pulse count) and pending projectile cast ids sorted. Floats are hashed by their raw IEEE bits, or by their Q-format raw value when `FixedPointNumeric` is active. Strings are hashed upper-cased (the comparer of the domain), so the hash does not depend on culture. Compare hashes every N ticks; on mismatch, dump `system.Trace` around the tick on both sides.

Not hashed: event subscriptions, the trace, the rules objects, presentation state, base properties set outside units (the property cache is derived). A host that keeps additional authoritative state hashes it next to this value.

## Migration notes

- Replays recorded before 12.1 keep their outcomes: the default rules reproduce the classic formulas and the seed derivation did not change.
- Turning on `DeterministicAbilityProfile` changes results by rounding: re-record replays and re-tune content that relied on fractional amounts.
- With `FixedStepTimeSource`, author durations as multiples of the tick (`ToTicks` reports the rounding); a 0.1 s interval at 30 ticks per second becomes 3 ticks.
- Hosts must report adapter answers (positions, radius queries, projectile hits) deterministically: same order, same tick.

## По-русски

Записка о детерминированном режиме для сетевых и lockstep-игр (как VampireDota: Q8.8, целые тики, без float и без UnityEngine в домене). В 12.1 уже есть: сиды на каст, тик, реакцию, попадание и импульс зоны; заменяемый генератор (`AbilityRules.Random`, по умолчанию целочисленный xorshift32); фиксированные тики (`FixedStepTimeSource`, `StepTicks`); округление значений к сетке Q8.8 (`FixedPointNumeric.Q8`); хэш состояния `AbilityStateHasher` для поиска рассинхронизации и профиль `DeterministicAbilityProfile`, включающий всё это одним вызовом. Этого хватает для серверной авторитетной игры и повторов на одной сборке, но не для бит-в-бит lockstep между платформами: float-арифметика в IL2CPP на ARM может сливать умножение и сложение. Для политики «в домене нет float» нужен целочисленный домен. Варианты: B — отдельная сборка `Neo.Abilities.Fixed` без UnityEngine, на целых Q-числах и тиках, с теми же точками расширения (без поломок, 12.2.0, но вторая реализация и общий набор проверок соответствия); C — один обобщённый домен (ломающее изменение, 13.0.0). Рекомендация — B сейчас и C позже; решение за root и владельцем, B и C пока не реализованы.
