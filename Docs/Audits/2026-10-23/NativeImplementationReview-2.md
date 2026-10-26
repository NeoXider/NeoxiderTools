# Native implementation review 2

Status: review complete for the two requested SOURCE-FINAL scopes, Neo.Core and Neo.Condition, on `demo` / `11.0.0`. Condition has scoped source approval. Core's assigned actions are present, but source approval requires the bounded precision correction NI2-01 accepted by root during this review. All executable acceptance is **UNEXECUTED**.

Only this report is written. No product/source/test/doc/configuration, original request/receipt/plan, isolate or user asset is modified. No Unity/MCP, compiler, test runner, validator/checker, installer, service, process management or child agent is used. Execution stays closed until EVERY writer finishes and root publishes the global freeze. The same original authors own any correction.

## Read scope and evidence

Read both complete current implementation requests/receipts, all actual changed source hunks in the seven Core files and four Condition files, current owned documentation changes, and complete regression sources listed below. ResourcePoolModel, LevelModel and ConditionValueSource were additionally read in full. Core's nested Data/components/bridges, Abilities, NoCode, Editor authoring and StateMachine are integration dependencies, not reviewed unfinished owner code in this pass.

Original actors: Core `neo-audit-1023-b02-08`, Condition `neo-audit-1023-b02-07`. The plain model/helper changes preserve nearest-assembly ownership; ComponentOccurrence does not appropriate the Editor's old flat selection index. No new mandatory framework, test shim, backend or assembly dependency is introduced. Historical native/audit/compile results are leads, not current execution acceptance.

## NI2-01 — P2: Decrease's receipt and a zero-effective positive spend disagree with stored float state

Owner: SAME Core actor `neo-audit-1023-b02-08`. Current `Assets/Neoxider/Scripts/Core/Resources/Domain/ResourcePoolModel.cs:155–170`, `Decrease`; `:82–108`, `TrySpend`; shared `ValidateSpend` from `:111` and `CanSpend:77–79`.

Trigger: a valid uncapped pool (`Max=0`, Current=100000000f, unlimited decrease cap) receives amount1. Under binary32, subtracting1 leaves stored Current at100000000. Current Decrease nevertheless returns the input-clamped1; CanSpend and TrySpend both return true, and TrySpend emits Changed without a stored debit. Expected Decrease reports its own actually stored delta, captured before callbacks; a positive spend with no effective stored debit is rejected by both query and payment without events/timer changes. This is source/float-representation reasoning, not an executed arithmetic probe or Unity result. The current Core guide promises actual operation deltas/full positive payment, and downstream ability damage now relies on that return.

Smallest approved correction: compute/store the resulting float value, capture `before - after` BEFORE external callbacks, and return that capture from Decrease. In shared spend validation reject a positive request whose proposed stored subtraction does not lower Current; retain finite/nonpositive/id/balance/input-cap precedence, no-op behavior and read-only query semantics. Do not require exact equality between amount and a float before-after difference: ordinary fractional costs must continue working. Decrease's existing Changed notification policy for zero-effective positive requests is distinct from the new spend rejection; retain established behavior unless root deliberately changes it.

Representational over-round case: Current100000000f minus input5 may store99999992f, an actual delta8. Returning8 is the honest own-operation receipt. The existing cap is applied to the INPUT request (`ClampDecrease`) before float storage; a cap5 can admit input5 even though storage quantization removes8. The bounded repair must disclose this distinction and preserve input-cap admission, rather than silently imposing a new actual-delta cap or rejecting every inexact fractional operation. An exact double/int backend or a broader cap-policy redesign is not a prerequisite.

Required meaningful controls, authored by the same Core owner: unit request at a coarse-ULP balance returns zero actual Decrease; CanSpend/TrySpend reject zero-effective positive debit with unchanged state/events/timer remainders; over-round input5 reports actual8 even if a Changed listener subsequently mutates balance; an ordinary fractional positive spend remains accepted; existing cap-4/cost-10 and failure precedence remain. Root explicitly confirmed this bounded correction during review and will send it to the SAME Core actor after this formal report/slot availability. No test runs now.

## Neo.Core — assigned actions closed by source; NI2-01 remains

### Public receipt/event and registration closure

- `CORE-XP-REENTRANT-PAYLOAD`: AddXp stages the recomputed `resultingLevel` before OnXpGained, then compares/reports that capture. Arguments of each XP invocation are captured for that invocation, and XP-before-level ordering is retained. The three new tests prove nested `(2,3)` then outer `(1,2)`, reset preserving the outer event and reset not inventing a transition for a 50-XP addition. Tests assert ordered event lists and final live state, not just counts.
- `CORE-POOL-REGISTRATION-GENERATION`: every valid AddPool creates a fresh opaque token in a case-insensitive map; Remove retires it. Tick snapshots id/entry/token, checks before regen and again before heal, and clears its retained snapshot/reentry guard in finally. This covers same-instance replacement, remove/readd and a not-yet-run snapshotted entry. New paired tests choose the other entry at callback time, avoiding reliance on dictionary order, and prove both timer remainders start afresh on the next tick with exact balances/counts.
- `CORE-DEPLETION-REENTRY`: Decrease/TrySpend capture their own positive-to-zero transition before Changed, then notify Depleted. Callback revival/nested spending therefore cannot substitute the outer operation's depletion. The retained five lifecycle cases assert ordered Changed/Depleted sequences, actual own results and guard/snapshot recovery after a throwing Tick listener. This does not promise that every later listener/event executes after an escaping observer exception.
- `CORE-CANSPEND` / Core portion of `ABIL-CAPPED-PAYMENT`: concrete CanSpend and TrySpend share read-only validation, preserving interfaces, failure precedence and complete input-cap admission. The retained23 cases distinguish preflight from payment, assert no query events/balance/timer mutation and verify immediate query/payment parity for small representable balances. They do not cover NI2-01's coarse-ULP input. Ability captured costs/payment are a separate final owner review, not closed by this API receipt.
- `LEVEL-DATA-DOC`: the owned canonical guide now links `Level/Data/CurveEvaluation.md` and explains floored fractional endpoints without rewriting assets. Core README explains public C#/component/NoCode entry points, operation-local events, registration generations, uncapped pools and their mutable caller-owned entry contract.

### Regression quality, cost and compatibility

All five complete Core fixtures were read: Level, Resource, Profile, ResourceLifecycle and CanSpend, totaling69 authored cases according to the receipt. New cases are nonvacuous, compare exact public state/events/timer carry and include positive recovery. Pure models require no scene/global/save cleanup. No unsupported NonParallelizable attribute was introduced. Existing profile tests observe repaired data/deep-copy isolation; evaluator tests use hand-calculated thresholds and Int32 limits rather than mirror the evaluator.

All seven current changed-source files were covered: LevelCurveEvaluator, LevelCurveType, LevelFormulaType, LevelModel, ILevelCurveDefinition, ResourcePoolModel and ResourceProfileData. Numeric normalization/saturation/inverse search and profile/schema behavior remain coherent by source inspection, subject to NI2-01. Enum values, serialized DTO fields, public/provider signatures are retained; CanSpend is the declared additive concrete API and creates no reservation or callback transaction. Tokens allocate per registration, not per tick; Tick is O(pools), uses a retained tuple buffer and aggregates elapsed cycles. Level inversion repeatedly evaluates curves, so expensive custom/Data scans remain a disclosed cost, not a new global scheduler requirement.

**UNEXECUTED:** both Unity compilers, all69 cases/discovery, the additional precision cases, final Data/Level/Health wrappers, ability payment/receipt integration, native import and package/text/meta checks. No Core action is accepted through mocks or static output alone. NI2-01 must be corrected by the original owner before renewed scoped source approval.

## Neo.Condition — scoped source approval

### ComponentOccurrence and legacy compatibility

`EDITOR-COMPONENT-INSTANCE` is source-closed: `_componentOccurrence` and `_otherComponentOccurrence` are additive integer fields default0 with public ComponentOccurrence/OtherComponentOccurrence. Old ComponentIndex/OtherComponentIndex fields retain their old values/UI meaning. Setters invalidate the correct source; cache reuse additionally checks occurrence, so direct serialized changes rebind. Matching counts only components satisfying the existing full-name/short-name type rule; unrelated components do not consume an index. Negative/missing occurrences return unavailable without first-match fallback.

Selected-component destruction rebuilds the cache and honors the requested occurrence. When an earlier component is removed but the cached selected one survives, topology/order changes require explicit invalidation, exactly as the receipt/guide disclose. This is a stable binding lifetime, not an assertion of automatic topology monitoring. Unavailable remains false before legacy Invert; no new fail-closed/status API is silently introduced. GameObject mode remains separate from component occurrence selection.

### Indexer, cache and documentation closure

`CONDITION-INDEXER-BINDING` is source-closed: property lookup catches specifically AmbiguousMatchException before an exception can escape the EnsureCache/read boundary; accepted properties require CanRead and zero index parameters. Both component and GameObject paths have the guard, retaining ordinary field fallback and the public ReflectionCache contract. Failure clears validity/member state and subsequent retargeting can recover. The two probe types expose actual single/overloaded Item indexers with getter counters; no native reflection invocation is claimed to have run.

`CONDITION-DOC-PARITY` is closed in owned guides: events are the bool pair plus exactly one OnTrue/OnFalse branch; only StateMachine Owner has state-object fallback, while missing Override fails. Context slots, matching-type occurrences and old flat UI indices are distinct. New documentation gives direct C# and Inspector/NoCode paths, zero defaults, invalidation, legacy data and indexer behavior. It does not claim Editor's still separately owned occurrence UI/preview migration is already verified.

The existing context/flags/numeric conversion/generic filtering/network ingress changes remain preserved. Their conditional woven-body tests still prove only received-method rejection when actually executed; they are not real remote admission evidence. A-33 preview selection belongs to Neo.Editor, and NoCode has its own additive occurrence implementation; neither unassigned in-progress source is read/approved here.

### Exact regression controls and debt

Read complete22-case binding fixture, both complete current probe sources and complete retained25-case original regression fixture. The controls are meaningful: probes have Values3/8 and an intervening BoxCollider; invalid selection would otherwise read a passing first match; selected-destruction deliberately makes remaining occurrence0 pass while requesting missing1; topology cases explicitly invalidate; serialized0→1→-1→0 observes warm-cache rebind. Legacy JSON omits new fields, preserves nonzero old UI indices and still selects occurrence0. Indexer cases assert false repeatedly, zero getter reads and positive field-retarget recovery on BOTH source branches. All owned GameObjects are inactive and destroyed in teardown. No new framework/shared asmdef/unsupported attribute was added.

No additional confirmed Condition defect was found in this scope. Native authored-asset import of absent fields/default0 is still stronger evidence than JsonUtility alone. **UNEXECUTED:** all47 cases/discovery, real metadata/indexer behavior, both Unity compilers, final actual Editor selection/preview, StateMachine/NoCode integration, native lifetime and real-peer/MIRROR-on/off/stripping evidence. Typed result/session/descriptor frameworks remain proposals.

## Source identities

Read/hashing checkpoint: 2026-10-23 18:38:13 UTC. Paths below are relative to `D:/Git/NeoxiderTools/`. Hashes identify the reviewed source, not an executed checker result.

| Artifact | SHA256 |
|---|---|
| `Docs/Audits/2026-10-23/Neo.Core.implementation-request.md` | `87bfbf2e3331ec6768a1598ada3f499f80e0cf40203585c257a22cc8771961f8` |
| `Docs/Audits/2026-10-23/Neo.Core.implementation.md` | `f7443a994303fbc98ed9677329f967298a3949f32a1d2a1a3adc8f4329a26c39` |
| `Docs/Audits/2026-10-23/Neo.Condition.implementation-request.md` | `885d4cdac5b4813cdab913f5aecf618f6544459e8d148fa4bdf4bdff3bfba624` |
| `Docs/Audits/2026-10-23/Neo.Condition.implementation.md` | `6cf4aa021fe1c3eb11cf1392279d388d0e583de7cd7da845eef24eda05cd90e6` |
| `Assets/Neoxider/Scripts/Core/Level/Domain/LevelModel.cs` | `b421d876e131a0f89fd3c90e5148724bbd765089818040b309f7aa161ec71d70` |
| `Assets/Neoxider/Scripts/Core/Resources/Domain/ResourcePoolModel.cs` | `9d26c0d655e40ecf47483512be88ed47b6f8934d371a4069cae071a5246641ec` |
| `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreLevelTests.cs` | `4ba088aad9002327c82054e6e7784b74f99b62b0302234284bebd9803003c751` |
| `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourceTests.cs` | `ca4fbab6b37838a5ca06abad7b2dc82116cac9d7fb59bc4f9f19a2be79928568` |
| `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourceLifecycleTests.cs` | `3789cfb7fa64fa25280b8ce270db42a4734beaece54ccc1d26b0d764687e3ce3` |
| `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreCanSpendTests.cs` | `23cddb1a0503b1359018ae086bdbcb933026f6efac724480353e10019dc1d53c` |
| `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreProfileTests.cs` | `9d7b14ff0ffe6e257e547dd115c3377f00ad2ed80b8d84e9509f285cb9c53227` |
| `Assets/Neoxider/Docs/Core/README.md` | `077a4deeda2aa013a1237b736ea905f9d4343c8f498a76c76676c625e49b7015` |
| `Assets/Neoxider/Docs/Core/LevelCurveDefinition.md` | `69c5daec11356f349820e412fb0cb4726d8ab8b02b2d7b196001939f8faea6b1` |
| `Assets/Neoxider/Scripts/Condition/ConditionEntry.cs` | `8f4a37146b6ec9384dbfb715563b5e0a0cabe9c0cd456f39a9eed945e3cd3570` |
| `Assets/Neoxider/Scripts/Condition/ConditionValueSource.cs` | `1a9f8bef10ff398a845d989a7fad6e7653e904b13b8d503e3fbdeb52534e3a7c` |
| `Assets/Neoxider/Scripts/Condition/NeoCondition.cs` | `c8fca0ceac06f37e2a861d1f38b3250920d6d30889ece26614a0a18e9f0333f8` |
| `Assets/Neoxider/Scripts/Condition/ReflectionCache.cs` | `1f7d7a0052eab83cd6b18536fd7cb9932bd0c422bd5af8152c629d0c01bd9721` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionBindingTests.cs` | `db048239cd712cf83281ed423becff88cb460f47a1e7324a179d10e919025b2c` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionAmbiguousIndexerProbe.cs` | `257893e54e1bb0bb4c2f9d050c8b8c959bc502cbdb090294f0b434fbfb423aad` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionValueProbe.cs` | `c90ff3287de2bf07e8e43396c9dcb5a77b49124629f03c216dbfe269fc7a4044` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionRegressionTests.cs` | `1d6aacf5b1da198a573b7ff7d0ab2016d9b661b2e80ffe09cba5c623cbaacb36` |
| `Assets/Neoxider/Docs/Condition/README.md` | `85f91d985b6a7852c60cd7bea8a4c97f7aacaa8af3abc6608fe8951e78dbea6e` |
| `Assets/Neoxider/Docs/Condition/Condition_Reuse.md` | `ebed3955a5a201f294acd87aab2827b8b598625dab1548f7f21088a7c9d80c27` |
| `Assets/Neoxider/Docs/Condition/NeoCondition.md` | `03fa281dc01226936d5b5a6670fd42c4e201d46b3b37782d9ed1211f01d56165` |
| `Assets/Neoxider/Docs/Condition/InternalTypes.md` | `ad08f77927cd6c64ba4e93dc4839b9b05ec4255fd7234d341750f4b1f1d13874` |

## Handoff

Return NI2-01 to the SAME Core author through root; root already accepted its bounded repair and quantization disclosure. Preserve completed XP/registration/depletion/cap-query fixes and their regressions. Condition can keep scoped source approval while separately owned Editor/NoCode/StateMachine/peer integration remains open. Do not start a dependent Abilities final review in this report or run tests; root will request that as its next separate source review. Global source-freeze/verification authority remains with root.
