# Extensions-Numeric-Time-Random: implementation receipt

Target: `demo` / `11.0.0`. Original actor/session: `neo-audit-1023-extra-01`, requested profile `gpt-6.1-sol`, `max`.

Status: **SOURCE-FINAL**, 2026-10-24. Assigned owned documentation/source-retention work is complete; existing corrected APIs are available to dependents. No incoming API dependency remains. Compilation, tests and product validation remain behind the global source-writer barrier; this receipt does not declare that global barrier open.

## Published API and ownership

No additive API or new numeric/RNG kernel is assigned by this implementation request. Existing signatures remain available in `Neo.Extensions`:

```csharp
public static int GetAccumulatedClaimCount(this DateTime lastClaimUtc, float cooldownSeconds, DateTime nowUtc);
public static int CapToMaxPerTake(int accumulated, int maxPerTake);
public static DateTime AdvanceLastClaimTime(this DateTime lastClaimUtc, int claimsGiven, float cooldownSeconds);
public static int GetRandomWeightedIndex(this IList<float> weights);
public static string FormatTime(this float timeSeconds, TimeFormat format = TimeFormat.Seconds, string separator = ":");
public static string FormatTime(this float timeSeconds, TimeFormat format, string separator, bool trimLeadingZeros);
```

These are existing public methods, not declarations of new members. `GetAccumulatedClaimCount` and `AdvanceLastClaimTime` belong to `CooldownRewardExtensions`; `CapToMaxPerTake` is a regular static method of that class. Weighted index selection belongs to `RandomExtensions`; float clock formatting belongs to `PrimitiveExtensions`.

Bonus can delegate timestamp advancement immediately: `lastClaimUtc.AdvanceLastClaimTime(claimsGiven, cooldownSeconds)`. This normalizes UTC, preserves count multiplication in double, truncates sub-tick fractions and saturates at UTC DateTime.MaxValue. No new assembly reference is necessary for the existing Neo.Bonus → Neo.Extensions dependency.

The eight numeric/time/random runtime files and existing three `Audit20261023Numeric*.cs` fixtures remain this actor's scope. Canonical Extensions README/doc integration belongs to the main Neo.Extensions actor. No reverse dependency on Neo.Tools.Random is introduced.

## Approved action accounting

- The exact implementation request assigns **no additional confirmed runtime defect or numeric review-action ID**. It requires retention of the coherent earlier patch and completion of factual public guidance/source-final bookkeeping.
- Historical audit findings **N01–N15** remain implemented in the five previously corrected runtime files. This phase does not claim those fixes as newly written and does not reopen the completed audit or plan.
- **A-07** belongs to Neo.Bonus. On the final source read, TimeReward's count and advancement methods both delegate to the corrected Extensions helpers. The consumer source dependency is therefore integrated by its owner, not claimed as a change by this actor; consumer execution evidence remains pending.
- The proposed immutable WeightedDistribution remains a proposal in the original audit/plan. No new distribution type, sampler framework or duplicate regression fixture is assigned or added.

## Source-final file ledger

**Changed during this approved implementation phase:**

1. `Assets/Neoxider/Docs/Extensions/NumericEdgeCases.md`: finished public consumer guidance and related-API navigation. It explains value-option copying; preserving the typed integer instead of converting through float; the existing decimal/double routing; passing the application's trusted clock; delegating both reward calculations; reusing ChanceManager's existing GetId/RandomProvider; C# and Inspector UnityEvent use of TimeToText; and the component's input rejection/Clock/Compact behavior versus raw FormatTime sanitization. Links point to existing canonical API guides. Existing `.meta` is retained.
2. `Docs/Audits/2026-10-23/Extensions-Numeric-Time-Random.implementation.md`: this completion/API/dependency receipt.

**Previously corrected runtime retained, without another implementation-phase source patch:**

| File under `Assets/Neoxider/Scripts/Extensions/` | Retained behavior |
| --- | --- |
| `NumberFormatExtensions.cs` | N01–N06: small-double significands, signed directed rounding, safe decimal decomposition, BigInteger sticky digits, grouped trimming/separators and consistent nonfinite affixes. |
| `PrimitiveExtensions.cs` | N10/N12–N14: bounded float clock conversion, finite normalization arithmetic, decimal-place scaling and precise/bounded snapping. |
| `RandomExtensions.cs` | N07–N09: finite double weight totals, invalid probabilities/inclusive endpoint and full-int randomization. |
| `TimeSpanExtensions.cs` | N11: MinValue-safe display and exact long whole-unit totals. |
| `CooldownRewardExtensions.cs` | N15: finite cooldown validation, precise count multiplication, tick-based UTC advancement and DateTime saturation. |
| `RandomShapeExtensions.cs` | Audited shape sampling, unchanged. |
| `TimeParsingExtensions.cs` | Existing parsing/range/finite guards, unchanged. |
| `DateTimeExtensions.cs` | Existing UTC/round-trip semantics, unchanged. |

All eight runtime hashes match the snapshot read before the Windows interruption. Existing coherent code, public signatures and serialized fields were preserved. No assembly, scene, prefab, sample-layout, save-key or version-metadata change was made by this implementation phase.

## Regression source retained — UNEXECUTED

Files under `Assets/Neoxider/Tests/Edit/Extensions/` retain the exact SHA-256 hashes recorded by NativeReview-A:

| File | Authored cases | SHA-256 |
| --- | ---: | --- |
| `Audit20261023NumericFormatTests.cs` | 34 | `38dced82a1fce33de7848fb153f2a16e3fa0ce91974212d64ed8593491ff37cc` |
| `Audit20261023NumericRandomAndPrimitiveTests.cs` | 20 | `e95356e79f3cce97397a58d6114f9265cb99d881a181f076dcc3e6f929b08df1` |
| `Audit20261023NumericTimeTests.cs` | 28 | `0e09b06fe7ee20d0ab05731200b62c9cfdc17a49d8c564237c55fdfa5236a9dd` |

These 28 test methods / 82 cases are the meaningful regression SOURCE authored in the original audit phase; no duplicate tests were added merely to create an implementation-phase diff. They observe numeric/text/date results and selected buckets. The RNG fixture snapshots/restores random state and the changed diagnostics flag. None of these fixtures contains `[NonParallelizable]`; no unavailable NUnit attribute was introduced. Actual execution must follow the coordinator's isolated/exclusive runner contract.

Acceptance to execute after all writers finish:

- Signed ceiling/floor of −1251 with one idle decimal produces −1.2K/−1.3K across numeric overloads; BigInteger 12501 and 12500 round differently at the exact midpoint boundary.
- Tiny/subnormal scientific doubles preserve significands, including `4.94e-324`; decimal boundaries format without multiplication overflow; trim/separator and affix cases produce their named outputs.
- Two float.MaxValue weights select both positive buckets, never the trailing zero bucket; nonfinite distributions return the documented failure value. Extreme int randomization reaches both sign halves and preserves fixture cleanup.
- Float clocks produce zero for NaN/negative values and `24855:03:14:07` at positive overflow; every TimeFormat works at both TimeSpan extremes.
- Full finite float-range normalization preserves midpoint/endpoints, and snapping/decimal scaling retain the expected finite values.
- UTC advancement preserves 16777217 one-second claims, retains the authored whole-tick fraction and saturates to UTC DateTime.MaxValue. Bonus verifies persisted timestamps and bounded public reward/event behavior through its own fixtures.

No authored test above has been executed by this actor in this implementation phase.

## Deliberate compatibility contracts retained

Grouped TrimTrailingZeros=true removes optional fractional padding; false keeps it. Nonfinite double affixes match the float/core API. Decimal/scientific rounding respects the signed value. Float clocks sanitize NaN/negative input and saturate positive overflow at int.MaxValue seconds; TimeSpan provides the longer-duration path. Existing centisecond float formats and real-millisecond TimeSpan formats remain distinct.

Date advancement uses whole ticks rather than profile-dependent AddSeconds millisecond rounding, normalizes UTC and saturates. Existing round-trip saved timestamps remain readable. A sub-tick duration cannot advance DateTime. Snapping saturates an unrepresentable result, so the boundary need not itself be a step multiple. Wider weighted arithmetic can change choices near a cumulative boundary, and the corrected int.MinValue range uses two random draws. No cross-Unity-version seeded sequence guarantee is added.

## Cross-owner integration and remaining evidence

- **No pending additive API dependency.** The exact existing helper signatures above are ready; no new immutable distribution requirement was implemented. The substantial improvement remains an explicit proposal in the historical plan, with ChanceManager reuse acknowledged.
- **Neo.Bonus / A-07:** current `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs` delegates GetAccumulatedClaimCount at line 257 and AdvanceLastClaimTime at line 277 to Extensions. That owner's source integration is present; its trusted-clock/persistence/event acceptance remains the Bonus owner's and coordinator's work. This actor did not edit the consumer or its receipt.
- **Main Neo.Extensions navigation:** current `Assets/Neoxider/Docs/Extensions/README.md` links NumericEdgeCases in the reference list, formatting section and Russian navigation. The canonical backlink is integrated by its owner; this actor did not edit the README.
- **Neo.Tools.Text:** current TimeToText validation distinguishes Clock/Compact and rejects unsupported input. The owned guide describes the observed consumer contract. Display-mode/event/lifecycle verification remains with that owner and the central operator; this actor did not change TimeToText.
- **Coordinator after global source freeze:** execute the three retained fixtures and relevant existing Extensions/consumer fixtures on the installed Unity 6.3 and 6.6 hosts, including the real lower/upper Unity compatibility branches. Mono E16/subnormals, extreme native Random.Range and seeded-boundary behavior still require runtime evidence. Numerical evidence near the decimal-routing threshold is a targeted follow-up before any further rounding behavior change, not a claim of a new confirmed defect.
- **Product/package checks:** UTF-8/meta/GUID/docs/package validation and consumer PlayMode acceptance remain centrally pending. No new asset or sample root was introduced; the target remains 11.0.0 with the existing release layout.

## Actual work and historical integrity

This phase used source/document reads, reference enumeration, diffs/status and artifact hash bookkeeping only. The historical audit and plan still match the native-review hashes `a13c5ca5385d89b11b5553e444922d988126005980011f42ddf4880138d2fa1c` and `a06da07e4f60d4d11db150ff4432793f232b78dd06f7551e9a0187bb5aaaa989`. Their old verification statements were not rewritten as new evidence.

No Unity/MCP, compiler, test runner, product checker, installer, service, commit/push or child agent was launched. Shared manifest/review-actions/contracts/trackers and other actors' receipts remain untouched. Earlier audit static checks and native source review are context, not proof of the current integrated product. Source-final here means this actor has finished the approved owned work and is ready for coordinated frozen-source verification.
