# Neo.Condition: approved implementation request

Original actor: `neo-audit-1023-b02-07`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### EDITOR-COMPONENT-INSTANCE (P2; approved-source-correction)

Editor C1: serialized ComponentIndex exists but runtime chooses first matching type

Root decision: Condition: additive ComponentOccurrence and OtherComponentOccurrence; NoCode: ComponentOccurrence; all default0, count only matching type in stable GetComponents order. Editor exposes those exact fields after owner implementation. Preserve existing ComponentIndex/OtherComponentIndex UI fields; no reinterpretation. Missing/negative occurrence rejects explicitly, no fallback.

### CONDITION-DOC-PARITY (P2; approved-source-correction)

README promises all four events but actual bool pair plus one branch; Condition_Reuse incorrectly describes missing Override fallback.

### CONDITION-INDEXER-BINDING (P2; bounded-source-investigation-and-native-acceptance-required)

EnsureCache executes outside read catch; ambiguous Item overloads/indexed properties can fail binding. Confirm exact current source and reject indexers/ambiguity without poisoning subsequent valid binding.

## Relevant full native sections

Source: NativeReview-A.md

### Neo.Condition — source review complete; precise editor/doc integration follow-up

Original owner: `neo-audit-1023-b02-07`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Condition.md` — `75babe074bd3e2a3c81b8001647171f523559f8f6a2edb2cb8d8f52287f1277c`.
- `Docs/Audits/2026-10-23/Neo.Condition.plan.md` — `b4bbcd6011ae9416cf89ae341f51d21aa4908ab7e3007ccf99eb927dad5c04b3`.
- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionRegressionTests.cs` — `1d6aacf5b1da198a573b7ff7d0ab2016d9b661b2e80ffe09cba5c623cbaacb36`.
- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionValueProbe.cs` — `ccc0c14d8a1c6c2892c8ade9ae150022610cebc9af16f0c51357f99f17057937`.

All four changed source diffs, complete fixtures/probe, four changed guide diffs and reflection resolution/evaluation, lifecycle, network and current Editor/StateMachine integration paths were read. **Completed runtime fixes appear sound:** current-context rebinding, flags-sensitive hit/miss caches, actual numeric parameter conversion, non-invokable generic rejection, destroyed explicit right-source identity, mode/network command ingress and repeated ServerRevalidate routing. Tests read returned members, actual method arguments/counts and public result/event state; woven-body rejection tests intentionally do not prove outer admission/transport, and absent weaving skips remain unverified.

**A-33, P2, confirmed narrow cross-owner parity residual:** `Assets/Neoxider/Editor/Condition/NeoConditionEditor.cs:GetCurrentValueFromComponent:1052–1073` selects the first same-name/argument-kind method without excluding open generic candidates, unlike runtime `ConditionEntry.FindMethodWithOneArgument:584`. A generic overload encountered before an invokable same-kind overload faults preview and returns null instead of displaying the runtime-selected value. Smallest repair by original Neo.Editor owner: use existing ReflectionCache/runtime method-selection rules (or the same complete filters) before conversion/invocation. **Do not repeat stale plan requests:** parameter conversion is already present at1068, and picker `ComponentBindingInspectorShared:335` already skips IsGenericMethod and indexers. These existing corrections were personally inspected. Current generic-overload preview parity still needs a focused regression, not a new descriptor schema.

Required bounded documentation correction by original Condition owner: README:43 says all four event types fire, while InvokeEvents publishes OnResult, OnInvertedResult and exactly one OnTrue/OnFalse branch. Condition_Reuse's StateMachine table should say only Owner falls back to the current state; missing Override never substitutes Owner (`ConditionEntryPredicate:74–84`). These are confirmed source/doc discrepancies, separate from runtime behavior. Preserve current unavailable-source inversion semantics; the typed status/session/descriptor core is **future work**.

Aggregate work: stable bindings reuse metadata but reflection reads/conversion still box; switching one shared entry among many contexts repeatedly allocates GetComponents arrays/rebinds its single mutable cache. Missing-member lookups can repeat component enumeration on every check despite warning suppression; name-search retries alone are realtime-throttled. EveryFrame checks throttle individually to ~60Hz scaled gameplay time, not a global evaluation cache; interval loops remain per component. Shared metadata caches retain configured keys/types, whereas entry bindings retain their last resolved target. A prepared per-owner session is a future optimization, not mandatory new scene wiring. Binding-time ambiguous property/indexer resolution outside read catches is a **needs evidence** lead already identified in the plan; the docs' broad failure wording is not a proof that all metadata resolution is contained. Native lifecycle, Mirror accepted ingress and AOT/stripping remain **needs evidence**.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Condition.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## P5 native destroyed explicit source positive baseline

Read Local/Audits/assembly-2026-10-23/p5-source-correction-common.md. SAME b02-07, E10. BindOtherToSourceIfNull_PreservesDestroyedExplicitSource fails INITIAL Evaluate true at213 before destruction. Establish genuine comparable serialized source/value/threshold baseline under actual native fixture setup, then retain destroyed explicit source must NOT rebound to host and all validity/final result assertions. Do not change runtime destroyed-source semantics merely to satisfy a false baseline, do not remove positive control. Own fixture/runtime only if actual causal bug established, canonical docs/receipt. No execution.
