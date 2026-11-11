# Deterministic mode

**What it is:** how to run `Neo.Abilities` reproducibly: for replays, server-authoritative simulation or lockstep. The float domain is seeded and ordered, and `DeterministicAbilityProfile` adds fixed ticks and Q8.8 rounding. For integer-only simulation that is bit-exact across platforms, use the [Fixed core](./FixedCore.md).

**How to use:**
1. Choose the float profile or the fixed core ([table below](#choosing-a-mode)).
2. For the float profile, call `DeterministicAbilityProfile.Apply(system.Rules)` once after creating the system.
3. Drive the simulation with `system.StepTicks(n)` or `Advance(hostDelta)`, with the same inputs on every peer.
4. Compare `AbilityStateHasher.Compute(system)` between peers every N ticks.
5. On a mismatch, dump `system.Trace` around that tick on both sides.

- Namespace: `Neo.Abilities`
- Sources: `Scripts/Abilities/Domain/Rules/DeterministicAbilityProfile.cs`, `Scripts/Abilities/Domain/AbilitySystem.Hashing.cs`
- Related: [Fixed core](./FixedCore.md), [Rules and extension points](./Rules.md)

---

## Choosing a mode

| | Float profile | Fixed core |
|--|--|--|
| Setup | `DeterministicAbilityProfile.Apply(rules)` on an `AbilitySystem` | a `FixedAbilitySystem` with converted content |
| Numbers | float arithmetic, results rounded to Q8.8 | Q-format raw ints, integer arithmetic |
| Time | fixed float steps | whole ticks |
| Bit-exact across platforms | no | yes |
| Scene and NoCode components | yes | no; the host drives the system |
| Use for | server-authoritative play where only the server simulates; replays on the same build | lockstep across platforms; a "no floats in the domain" policy |

The fixed core is additive: `Neo.Abilities` is unchanged, and a shared conformance suite keeps both cores in step ([known differences](./FixedCore.md#conformance-with-the-float-core)). A future major version may merge both into one generic domain.

### Why rounding floats is not enough

IEEE single-precision `+ − × ÷ sqrt` are deterministic for the same operation order, and Mono and CoreCLR on x64 and ARM64 evaluate float expressions in single precision. Two risks remain between a server and IL2CPP clients: the C++ compiler may contract `a * b + c` into a fused multiply-add on ARM64, and different compilers may reorder operations or keep wider intermediates. Rounding to Q8.8 after each step hides most 1-ulp differences, but a value that lies on a rounding boundary still flips.

## The float profile

`DeterministicAbilityProfile.Apply(AbilityRules rules, int ticksPerSecond = 30, int fractionBits = 8, int maxStepsPerCall = 8)` sets three rules and leaves the others, such as formulas and stacking, as they are:

| Rule | Set to | Effect |
|------|--------|--------|
| `Time` | `FixedStepTimeSource(ticksPerSecond, maxStepsPerCall)` | Every step has the same length. `StepTicks(n)` and `TickCount` work in ticks; `FixedStepTimeSource.ToTicks` converts authored seconds. |
| `Numeric` | `FixedPointNumeric.Q8`, or `new FixedPointNumeric(fractionBits)` | Computed amounts and property values are rounded to the grid before they are stored or published. |
| `Random` | `XorShiftRandomFactory.Instance` | Integer xorshift32, identical on every platform. |

`DeterministicAbilityProfile.IsApplied(rules)` is true when the time source is fixed-step and the numeric backend is fixed-point. Each rule is described in [Rules](./Rules.md).

## Seeds and order

- **Cast seeds.** Every cast carries a seed: `CastRequest.Seed` when non-zero, otherwise one derived from the cast id and the caster. Chance rolls (`EffectNodeData.Chance`) and combat randomness use `XorShiftRandom` (`IRandomSource`). The domain never reads `UnityEngine.Random` or `Time`.
- **Reseeding.** `XorShiftRandom.Reseed(seed)` restarts an instance exactly like `new XorShiftRandom(seed)`, so pooled contexts and your own reusable generators need no new object per cast.
- **Derived seeds.** Each modifier tick and reaction rolls with its own seed: the instance seed for the first roll, then the instance seed mixed with the tick or reaction count. A 50% tick node therefore fires on about half the ticks, and the same sequence of ticks and events rolls the same way. Projectile hits and zone pulses also get derived seeds.
- **Order.** Unlimited area selectors keep the adapter's order. Capped areas and chain bounces sort by distance, then by unit id. Reactions run by priority, then application, then list order; zones in spawn order.

To reproduce a run, repeat the seeds and also the initial content, the unit and cast creation order, the tick-step sequence and the world adapter's answers (positions, radius queries, projectile hits) in the same order and on the same tick. The eight-tick catch-up cap per modifier means one large step can differ from many small ones. This is a replay-friendly runtime seam, not a complete cross-platform lockstep or snapshot protocol.

## State hashing

`AbilityStateHasher.Compute(system)` is an FNV-1a hash that folds, in a fixed order:

- the hash version, `TickCount` and the domain clock;
- every unit sorted by id: id, team, alive, level, health and mana current and max, and every modifier in list order (blueprint id, caster, stacks, remaining time, ability level, consumed shield, reaction counters and charges);
- every granted slot sorted by ability id: level, charges, cooldown;
- zones in spawn order: id, centre, radius, remaining time, pulse count;
- pending projectile cast ids, sorted.

Floats are hashed by their raw IEEE bits, or by their Q-format raw value when `FixedPointNumeric` is active. Strings are hashed upper-cased, like the domain's comparer, so the hash does not depend on culture.

Not hashed: event subscriptions, the trace, the rules objects, presentation state and the derived property cache. A host that keeps more authoritative state hashes it next to this value.

## Authoring for the profile

- Turning the profile on changes results by rounding. Record replays again and re-tune content that relied on fractional amounts.
- With `FixedStepTimeSource`, author durations as multiples of the tick; `ToTicks` reports the rounding. A 0.1 s interval at 30 ticks per second becomes 3 ticks.
- Replays recorded with an older package version: see [Migration to 12](../Migration12.md).

## По-русски

Детерминированный запуск нужен для повторов, серверной авторитетной игры и lockstep. Профиль
`DeterministicAbilityProfile.Apply(system.Rules)` включает фиксированные тики, округление к сетке Q8.8 и целочисленный
генератор xorshift32; хэш `AbilityStateHasher.Compute` сравнивается между участниками каждые N тиков. Этого хватает для
серверной игры и повторов на одной сборке, но не для бит-в-бит lockstep между платформами: float-арифметика в IL2CPP на
ARM может сливать умножение и сложение. Для политики «в домене нет float» используйте целочисленное ядро
[Fixed core](./FixedCore.md).
