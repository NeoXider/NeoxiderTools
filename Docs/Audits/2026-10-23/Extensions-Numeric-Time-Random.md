# Extensions-Numeric-Time-Random: additional deep audit

Status: source audit and supplemental fixes complete; central compilation and Unity test execution pending.

Model `gpt-6.1-sol`, reasoning `max`.

## Assignment

Extensions main owner finished source edits. Own only listed numeric/time/random files and NEW tests Audit20261023Numeric*.cs; public docs only Assets/Neoxider/Docs/Extensions/NumericEdgeCases.md. Deep leads to verify: tiny doubles cast decimal => zero; negative directed rounding in idle/scientific; decimal rounding multiply overflow; nonfinite/overflowing weights; FormatTime huge/nonfinite float casts; TimeSpan.MinValue.Duration/int totals; grouped TrimTrailingZeros ignored. Do not duplicate already-fixed exponent trim/NaN duration/int Wrap fixes; audit current version.

## Files

- `Assets/Neoxider/Scripts/Extensions/NumberFormatExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/PrimitiveExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/RandomExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/RandomShapeExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/TimeParsingExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/TimeSpanExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/DateTimeExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/CooldownRewardExtensions.cs`

## Findings, fixes and regression checks

### Scope and review evidence

Read the root AGENTS.md, WORKER-CONTRACT.md, this exact supplemental manifest entry and this report. The supplemental entry takes precedence over the main Extensions partition. Reviewed every line of all eight assigned C# files (2,254 lines at intake), existing Extensions tests, relevant enum definitions, the existing module entry/docs and the reward consumer's copied calculations. Applied the neoxider-tools Extensions reference and the unity-game-agent file-only workflow. User restrictions override skill defaults for delegation, shared state and Unity verification.

Existing main-owner edits were already present at intake and were preserved. In particular, this supplement does not claim the exponent-trimming, duration NaN/overflow validation, accumulated-count saturation, explicit-type cleanup, Wrap or PingPong fixes as new work.

| Owned source | Review and disposition |
| --- | --- |
| `NumberFormatExtensions.cs` | All public numeric overloads, options/defaults, sign handling, decimal/double boundaries, suffix promotion, arbitrary-size integer rounding, separators, affixes and culture. Fixed findings N01–N06. Serializable fields and public signatures preserved. |
| `PrimitiveExtensions.cs` | Boolean conversions, float decimal rounding, every clock format and both separator paths, normalization/remapping, float/int snapping, integer wrap/ping-pong and approximate comparisons. Fixed N10, N12–N14; preserved main-owner integer wrap/ping-pong fixes. |
| `RandomExtensions.cs` | Collection validation, copy/in-place shuffle ownership, deferred selection results, selector evaluation/allocation, weighted validation/cumulative arithmetic, probability endpoints, enum cache/reset, value/range overloads. Fixed N07–N09. Diagnostics remain gated. |
| `RandomShapeExtensions.cs` | Bounds/interior/surface overloads, circle area distribution, sphere sampling, collider bounds and null/lifetime preconditions. No demonstrated defect against the current contract; unchanged. Surface faces are chosen equally, not by area, and collider AABBs are not the collider volume; documented. |
| `TimeParsingExtensions.cs` | Single/multi-token parsing, 2–4 component ranges, finite float bounds, invariant/current-culture paths and false/zero failure contract. Main-owner fixes are already present; unchanged. Numeric precision below float range remains an input-representation limit. |
| `TimeSpanExtensions.cs` | All compact variants, all TimeFormat branches, maxParts, negative/zero handling, clock/day mode and extremes. Fixed N11 without changing normal field widths or the legacy millisecond component width. |
| `DateTimeExtensions.cs` | UTC/Local/Unspecified semantics, invariant round-trip persistence, legacy parsing fallbacks, elapsed/remaining sign and the full DateTime range. No additional demonstrated defect; unchanged. The current-culture fallback is intentional legacy compatibility. |
| `CooldownRewardExtensions.cs` | Finite duration/count bounds, negative elapsed time, caps, UTC normalization, multiplication precision, timestamp overflow and fractional precision across .NET profiles. Fixed advancement N15; preserved the main-owner accumulated-count hardening. |

These are static helpers, without owned scene objects or instance lifecycle/resource handles. Random selection uses Unity global state and its supported thread; the enum cache resets at SubsystemRegistration. Temporary shuffle copies, selector arrays, strings and BigInteger digit buffers remain ordinary managed allocations. No scene search, scene-reference ScriptableObjects, direct Unity identity API or new runtime log stream was introduced. All new C# declarations use explicit types.

### Demonstrated owned defects and completed fixes

Evidence below is source-level reproduction and the named executable regression scenario. Tests were authored, not executed by this worker. P1 denotes an exception or material selection corruption on valid finite input; P2 denotes incorrect results, invalid-input handling or presentation.

| ID / severity | Trigger, source and observable pre-fix consequence | Completed fix / regression evidence |
| --- | --- | --- |
| N01 / P2 | `NumberFormatExtensions.FormatNumber(double)` unconditionally cast to decimal: scientific `1.23e-30` became `0`; subnormal doubles lost their significand. A tiny positive value with ceiling also became zero instead of the smallest displayed unit. | Retain the double path below the decimal precision needed for a 12-place significand plus guard digit. Scientific fallback reads a 17-significant-digit runtime mantissa/exponent, including subnormals. `ScientificTinyDouble_PreservesSignificand`: `1.23e-030`, `-1.23e-100`, `4.94e-324`; directed plain tests expect `0.001`/`-0.001`. |
| N02 / P2 | Idle/scientific decimal and BigInteger paths rounded a positive magnitude before restoring the negative sign. `-1251`, one decimal, ceiling produced `-1.3K` instead of `-1.2K`; floor reversed similarly. The large-double scientific fallback ignored custom rounding entirely. | Reverse ceiling/floor only while rounding a negative number's magnitude; use the requested rounding on the signed scientific double mantissa. Cross-type tests cover int/long/float/double/decimal/BigInteger and convenience overloads; large-double cases cover directed rounding and exponent promotion. |
| N03 / P1 | `Round(decimal)` used `value * 10^decimals` for directed modes. Formatting `decimal.MaxValue`, even at one decimal place, threw OverflowException despite a representable unchanged result. | Split the integral and fractional parts and scale only the fraction. Apply the same overflow/precision-safe decomposition to directed double fallback rounding. `DecimalLimits_DirectedRoundingDoesNotOverflow` covers both decimal limits and all three directed modes; large double plain values retain their magnitude. |
| N04 / P2 | BigInteger idle/scientific methods kept one rounding digit but discarded the remaining tail. Scientific `12501` with one decimal and ToEven became `1.2e4`, incorrectly treating a value above midpoint as the exact midpoint. Ceiling of `10001` similarly lost the nonzero remainder. | Retain a sticky digit whenever the discarded tail contains any nonzero digit. Tests distinguish `12501` from `12500`, `1250001` from `1250000`, and signed directed cases. |
| N05 / P2 | `FormatGrouped(decimal)` returned ApplySeparators directly and ignored `TrimTrailingZeros`: 1234.5 at four places stayed `1,234.5000`. Trimming after applying overlapping user separators can additionally mistake a group for a fraction. | Normalize invariant fractional text before inserting custom separators. Tests cover trim true/false and overlapping/multi-character separators; 12000 with `.` grouping remains `12.000`. Existing main-owner exponent/multi-character trim implementation remains intact. |
| N06 / P2 | `ToPrettyString(double)` returned early for NaN/infinity, bypassing affixes; the float and core entry points applied them. | Delegate every double value to the core formatter. Tests require `[NaN]`, `[Infinity]`, `[-Infinity]` across float/double/core entry points. |
| N07 / P1 | Weighted selection accumulated in float. Two finite `float.MaxValue` weights overflowed the total to infinity and collapsed selection onto the last positive bucket. NaN/infinite entries passed validation and yielded a seemingly valid result instead of rejecting the distribution. | Accumulate and walk in double, reject nonfinite entries and preserve the existing last-positive endpoint fallback. Fixed-seed regression observes both large-weight buckets and excludes a zero bucket; invalid-weight tests exercise index, parallel-list and selector overloads. |
| N08 / P2 | `Chance(float.NaN)` bypassed range validation and always returned false. `Chance(1f)` could return false at Unity Random.value's inclusive 1.0 endpoint. | Reject NaN and explicitly accept probability one after consuming the normal random sample. NaN has an exception-contract regression. The exact inclusive endpoint correction is source-reviewed; forcing Unity RNG to that endpoint was not attempted. |
| N09 / P2 | `RandomizeBetween(int.MinValue)` negated int.MinValue, overflowing back to itself, then sampled an equal-endpoint range; every result was int.MinValue. | Sample a negative half-range and choose its representable sign half, covering the full int range without negation overflow. A fixed-seed test observes both sign halves. Ordinary range overload behavior remains unchanged. |
| N10 / P2 | Float `FormatTime` directly cast NaN, infinity or values >= 2147483648 to int. The out-of-range cast then fed negative/invalid totals and digit writers; output depended on runtime conversion behavior. | Sanitize in double before conversion: negative/NaN -> zero, positive overflow/infinity -> exactly int.MaxValue seconds. Tests exercise seconds, fractional, days and multi-character separator paths, including `24855:03:14:07`. |
| N11 / P1 | All TimeSpan formatters called Duration(), which throws for TimeSpan.MinValue. The enum compact formatter also narrowed total seconds/minutes/milliseconds to int; valid large durations overflowed. Floating total milliseconds could round across a whole-unit boundary before truncation. | Use an absolute display value safe for MinValue (MaxValue differs by one tick and has identical displayed components), long totals and exact integer tick division. Tests cover every TimeFormat at both extremes and all three entry points, including `922337203685477ms` and `-256204778:48:05`. |
| N12 / P2 | Normalization and denormalization performed `max-min` in float. The finite range [-float.MaxValue,+float.MaxValue] overflowed: zero normalized to 0 rather than 0.5, and midpoint denormalization became infinity. | Compute range arithmetic in double while retaining validation/clamping. A public-API regression covers midpoint, endpoints, NormalizeToRange and Remap. |
| N13 / P2 | Float RoundToDecimal scaled in float. `float.MaxValue` at one place became infinity, and a huge decimal-place count produced infinity/infinity -> NaN for ordinary finite input. | Keep the usual Unity path, fall back to double when scaling overflows, and return the original float beyond 45 places. Tests cover places 1, 39, 100 and subnormal input at 46 places. |
| N14 / P2 | Int Snap converted through float: `16777217.Snap(1)` lost one unit; near-boundary multiplication could overflow. Float Snap could overflow its quotient/product even when the rounded result was representable, e.g. `1f.Snap(float.Epsilon)`. | Int snapping uses long quotient/remainder and even midpoint handling; unrepresentable results saturate. Float snapping preserves the ordinary Unity path and uses a finite double fallback with saturation. Tests cover exact large integers, signed ties, int bounds and tiny/large float steps. |
| N15 / P1 | AdvanceLastClaimTime multiplied int count in float, so 16777217 one-second claims advanced only 16777216 seconds. NaN/infinity reached AddSeconds and threw; valid large advances exceeded DateTime range and threw. Fractional behavior also depended on the .NET profile's AddSeconds precision. | Reject invalid cooldowns, normalize UTC once, multiply in double, truncate only sub-tick fractions, add ticks and saturate to UTC MaxValue. Tests cover all invalid cooldown categories, exact large count, 152 sub-millisecond ticks and overflow saturation. |

### Regression assets

New files under `Assets/Neoxider/Tests/Edit/Extensions/`, with unique GUID metadata:

- `Audit20261023NumericFormatTests.cs`: 11 test methods, 34 parameterized/plain cases.
- `Audit20261023NumericRandomAndPrimitiveTests.cs`: 9 test methods, 20 cases; snapshots/restores Unity random state and the diagnostics error flag.
- `Audit20261023NumericTimeTests.cs`: 8 test methods, 28 cases.

Total: 28 test methods / 82 cases authored. Assertions observe outputs, selected buckets, exact dates or documented invalid-input exceptions; no reflection/source-text assertions or no-throw-only tests. Existing tests/asmdefs were not edited. `Neo.Editor.Tests.asmdef` already references Neo.Extensions; no extra assembly references are requested.

## Documentation

Added the exclusively owned public topic [NumericEdgeCases.md](../../../Assets/Neoxider/Docs/Extensions/NumericEdgeCases.md), with metadata. It documents typed C# examples, all corrected boundary contracts, rounding and separator semantics, float clock saturation and legacy centiseconds, TimeSpan extremes, UTC persistence/reward advancement, random-state/thread requirements, zero/invalid weights, shape-sampling limitations, and scene/NoCode integration. It includes a concise Russian explanation and links back to the canonical module entry.

No existing module docs, root/package entry points or another worker's docs were edited. Coordinator action: link this narrow supplemental topic from `Assets/Neoxider/Docs/Extensions/README.md` in its Numbers and time section.

## Substantial improvement proposal

### Immutable weighted distribution with explicit sampling (proposed, not implemented)

**Problem:** Every weighted roll validates/sums/walks the same distribution in O(n); the selector overload allocates an array on every call. Selection is coupled to Unity's global random state, complicating replay, independent test inputs, isolated simulations and reusable economy/loot systems. An explicit unit sample would also permit an exact deterministic test of the inclusive 1.0 endpoint without probing Unity RNG.

**Architecture/API:** Add a small pure-C# immutable `WeightedDistribution` under Neo.Extensions. Proposed surface: `TryCreate(IReadOnlyList<float> weights, out WeightedDistribution distribution)` and `SelectIndex(double unitSample)`. Construction validates finite non-negative input once and stores double cumulative positive-weight buckets plus original indices. Binary search selects in O(log n); zeros never form a selectable bucket. Define unitSample [0,1], with 1 mapping to the last positive bucket; reject nonfinite/out-of-range samples explicitly. A thin existing-extension adapter supplies Unity Random.value. A separate optional overload can accept an injected random delegate where repeated sampling needs it; no global manager, scene object or mandatory interface is required.

**Compatibility/migration:** Keep existing extension signatures, validation defaults and fallback behavior. Introduce the table as an opt-in reusable API; callers with changing weights rebuild explicitly. Existing one-off extension behavior can remain as-is initially, avoiding surprise allocations or changed seeded selection sequences. ScriptableObjects may store weight definitions, with the runtime snapshot built from data, never scene references. Existing scene wrappers call the same C# table.

**Acceptance scenarios:** (1) negative/NaN/infinite/all-zero construction fails without a usable table; (2) two float.MaxValue weights remain individually selectable; (3) leading/interior/trailing zeros are never selected, including exact samples 0 and 1; (4) identical table/sample sequences replay identically without changing Unity random state; (5) mutation of the original list cannot change an existing snapshot; (6) repeated rolls allocate nothing after construction; (7) a 10,000-entry repeated-draw workload avoids repeated validation and linear scans; (8) imported Unity consumers can use the typed API without scene/NoCode dependencies.

**Effort/risk:** Approximately 2–3 developer days including focused unit tests, adapter, docs and a centrally run Unity smoke check. Main risks are seeded compatibility at cumulative boundaries, floating precision when weights differ by many orders of magnitude, ownership/mutation expectations and overpromising cross-runtime RNG determinism. Specify the sample contract and snapshot semantics first; deterministic sampling does not imply that Unity's global RNG is portable across engine versions.

## Verification and risks

### Actual checks

- Full source and public workflow review of all eight owned files, including every helper overload and enum format branch.
- Read-only review of the existing Extensions regressions and Neo.Editor.Tests references; test scenarios and expected integer/tick results checked against the source arithmetic.
- `git diff --check -- <all owned source/doc/test/report paths>`: exit 0 after source/test/doc edits. New untracked files also receive explicit whitespace/UTF-8 checks because git diff does not inspect their content before addition.
- Strict UTF-8 reads of all eight sources, all three new tests and the new doc succeeded; no replacement characters or implicit `var` declarations were found in owned C#.
- Intake SHA-256 snapshots confirmed RandomShapeExtensions.cs, TimeParsingExtensions.cs and DateTimeExtensions.cs remain byte-for-byte unchanged.
- Static finalization inspected 17 scoped files (eight sources, three tests, one public doc, this report and four new metadata files): UTF-8, replacement-character, whitespace, final-newline and explicit-type checks all passed. Public static method declarations in all eight sources match HEAD; serialized fields were unchanged in the reviewed diff. Each of the four new GUIDs occurs exactly once among package metadata.
- No Unity/MCP, compiler, build, test runner, installs, shared config changes, child agents, commits or git mutations were invoked. Review/selfcheck was local, without independent QA, as required by the assignment.

### Deliberate contract corrections and remaining limits

- Float clocks now explicitly saturate at int.MaxValue seconds. Full float-range durations are not added to that API; TimeSpan is the supported longer-duration display path. Fractional precision of float remains limited.
- Correcting Grouped TrimTrailingZeros changes padded display text when the flag is true; set it false for fixed fractional widths. Nonfinite double affixes now match the float/core API.
- UTC advancement now retains whole ticks rather than AddSeconds' legacy millisecond rounding. Existing ISO saved timestamps remain valid; subsequent advances of sub-millisecond cooldowns may differ. Durations below one tick cannot advance DateTime and callers should choose representable cooldowns.
- Snapped values beyond numeric bounds saturate; the boundary itself need not be an exact step multiple.
- Weight arithmetic now avoids float-total overflow, but a finite RNG has finite resolution; extremely tiny probabilities can remain practically unreachable. This is not an arbitrary-precision sampler.
- Wider weight totals can change rare seeded choices near a bucket boundary; the int.MinValue random-range correction uses two draws for that formerly broken argument. Chance's valid endpoints still consume one draw.
- Random methods still depend on Unity's supported thread/global state; no replay guarantee or thread-safe enum cache was added. Empty enum types remain outside GetRandomEnumValue's meaningful-value contract. Shape surface area weighting and collider-volume rejection sampling were not added because the current methods promise bounds sampling, not those distributions.
- Compilation/import, NUnit execution (82 new cases plus existing Extensions coverage), runtime RNG behavior and Unity 6.0–6.4 / 6.5+ compatibility remain **unverified and reserved for the coordinator**. No Unity-version-specific API was introduced. The global RNG's exact inclusive-1 endpoint branch is source-reviewed, without a forced-endpoint runtime test.

### Cross-boundary coordinator actions

1. **P1 — Neo.Bonus / TimeReward:** `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs`, private GetAccumulatedClaimCount (line 256 at inspection) and AdvanceLastClaimTime (line 282), duplicate the old algorithms. Count casts are unsaturated, cooldowns are not checked for finiteness, and advancement still multiplies in float then calls AddSeconds. The owned extension fixes do not reach these copied implementations. Have the Bonus owner delegate to the typed Extensions APIs or apply equivalent guards/precision handling, with consumer regressions. No Bonus source/test/doc was edited.
2. Link NumericEdgeCases.md from the Extensions entry point, then run the new fixtures and existing Extensions tests centrally. In particular verify the double fallback's E16 formatting and the extreme half-range RNG calls on Unity's actual runtime; static review cannot certify those runtime paths.
3. Review the documented new clock/date/snap saturation policies and whole-tick cooldown behavior when integrating UI and reward consumers. This worker did not change those consumers.

По-русски: все восемь файлов проверены; исправления ограничены пятью закреплёнными исходниками. Добавлены 82 регрессионных сценария и отдельная документация. Unity и тесты не запускались по контракту. Дубликаты расчётов наград в Bonus требуют отдельной правки владельцем модуля.
