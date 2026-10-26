# Neo.Condition — implementation receipt

Status: **SOURCE-FINAL after P5/E10 fixture correction; corrected-source native acceptance UNEXECUTED**. Target: `demo` / `11.0.0`. Same original auditor/session; earlier coherent audit fixes were retained. No source dependency blocks this owner's completion.

The original implementation handoff below is retained as history. Its UNEXECUTED labels describe the pre-freeze source delivery; the actual subsequent P5 failure and the newly authored correction are recorded in the appended E10 section.

## Approved actions delivered

### EDITOR-COMPONENT-INSTANCE

`Assets/Neoxider/Scripts/Condition/ConditionEntry.cs` now provides:

```csharp
public int ComponentOccurrence { get; set; }
public int OtherComponentOccurrence { get; set; }
```

The corresponding serialized fields are exactly `_componentOccurrence` and `_otherComponentOccurrence`, both integer default `0`. Each public setter invalidates its own source's member binding. The existing `_componentIndex`, public `ComponentIndex`, and `_otherComponentIndex`/internal `OtherComponentIndex` remain intact; no old flat UI index is reinterpreted or migrated.

`Assets/Neoxider/Scripts/Condition/ConditionValueSource.cs` counts only components matching the configured type's existing full-name/short-name rules, in `GetComponents<Component>()` order. Occurrence `0` selects the first match, `1` the second. Unrelated component types do not consume an occurrence. Negative and missing selections are unavailable and do not fall back to the first match.

The binding cache records the selected occurrence, so a direct serialized occurrence change is detected on the next evaluation, independently of using the setter. Cached bindings still require explicit invalidation after component topology/order changes when the cached selected component survives; the guide explains that existing binding-lifetime rule. Destruction of the selected component triggers the existing cache rebuild and respects the requested occurrence rather than selecting the remaining first match.

Unavailable sources continue to produce false **before the existing Invert flag**. This action does not introduce a new fail-closed/status API or silently change unavailable-source inversion.

### CONDITION-INDEXER-BINDING

Confirmed the exact source path: both left/right `EnsureCache` calls occur outside the read-value catch in `ConditionEntry.EvaluateInternal`; component property resolution previously called `ReflectionCache.GetProperty` without handling ambiguous metadata and accepted readable indexers. A public component exposing both `this[int]` and `this[string]` supplies the concrete same-name `Item` reproduction source.

`ConditionValueSource.cs` now catches `AmbiguousMatchException` at property lookup and accepts a property only when readable with zero index parameters. Applied consistently to component and GameObject binding paths. Ordinary field fallback remains available. No broad exception swallowing or change to the public `ReflectionCache` lookup contract was introduced.

Single/overloaded-indexer regression sources exercise direct public `ConditionEntry.Evaluate`, assert false for unavailable binding, zero getter calls, and successful subsequent evaluation after retargeting to an ordinary field. Native execution remains required: the source correction and authored reproduction do not constitute a demonstrated Unity pass.

### CONDITION-DOC-PARITY

Updated owned existing public documents:

- `Assets/Neoxider/Docs/Condition/README.md`: events are the bool pair plus exactly one OnTrue/OnFalse branch; added a backlink to the component-occurrence topic.
- `Assets/Neoxider/Docs/Condition/Condition_Reuse.md`: only State Machine Owner may fall back to the current state's GameObject; a missing Override does not substitute Owner. Distinguished context slots from component occurrences.
- `Assets/Neoxider/Docs/Condition/NeoCondition.md`: C# example selecting a second AudioSource, scene/NoCode occurrence setup, defaults, invalid selections, legacy-index compatibility, cache invalidation, and indexer/ambiguity rejection.

The historical audit/plan remain unchanged. The typed status/session/member-descriptor core remains a future proposal, not an implemented or mandatory dependency.

## Regression sources

- **New:** `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionBindingTests.cs` and `.meta`.
- **New:** `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionAmbiguousIndexerProbe.cs` and `.meta`.
- **Extended:** `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionValueProbe.cs` with a single indexer and observable getter counter.

The new fixture authors **22 cases**, all UNEXECUTED:

| Cases | Observable acceptance |
|---|---|
| 2 legacy JSON cases | Missing occurrence fields read as zero; actual evaluation reads the first match despite nonzero legacy UI indices; the old right-side index survives JSON serialization. |
| 2 public selection cases | A BoxCollider between two matching probes does not affect occurrence counting; changing the public selector changes the comparison and changing back restores it. |
| 2 serialized selection cases | Warm caches follow serialized `0 → 1 → -1 → 0` selection without manual invalidation, including the Other Object branch. |
| 8 invalid selection cases | Negative/minimum and missing/maximum integer occurrences return false without a first-match fallback; valid selection subsequently recovers. |
| 2 selected-destruction cases | Removing the selected second component makes occurrence 1 unavailable even when the remaining first component's value would pass; explicitly selecting occurrence 0 succeeds. |
| 2 topology/invalidation cases | Removing an earlier match and invalidating bindings re-counts occurrences without silently selecting the remaining first component. |
| 2 single-indexer cases | No indexed getter calls; false result on repeated evaluation; ordinary field retargeting recovers. |
| 2 ambiguous-indexer cases | Both source branches reject same-name Item overloads and recover with zero indexed getter calls. |

Together with the unchanged original 25-case regression fixture, this owner's authored EditMode sources contain 47 cases. This is a source count, not test discovery or execution evidence. The original three woven-body ingress rejection cases still do not prove remote admission.

New importer GUIDs generated for the two new files: `dc85b4f20aed4f04b6faaa1d20924dd2` (binding fixture), `d630847f589b493ea87e923cda8a19d4` (ambiguous probe). New folder metadata is unnecessary; no asmdef reference change was made.

## Cross-owner handoff

- **Neo.Editor:** the exact Condition API/serialized fields above are available now. Expose matching-type occurrences on each side; retain old flat component-list bookkeeping independently. Do not convert `_componentIndex`/`_otherComponentIndex` into occurrences. Serialized occurrence edits are already observed by the runtime cache.
- **Neo.NoCode:** its separate `ComponentOccurrence` action remains with its owner. Condition implementation has no dependency on that change. Root may integrate Editor authoring after both owners' APIs land.
- **A-33 preview generic-overload parity:** remains the Neo.Editor owner's bounded work. No editor-owned sources were changed. Existing editor parameter conversion/picker fixes were not reapplied.
- **Neo.StateMachine:** this phase updates only Condition-owned documentation to its Owner/Override contract, without editing predicate sources.

## Work performed and evidence boundary

During this source phase, performed read-only contract/request/source/reference reads and scoped source diff review; applied the two runtime edits, three public-document edits, prefixed regression-source changes, generated importer metadata, and this receipt. Reused the existing resolver/reflection/invalidation and gated-diagnostic paths; no new manager, framework, package dependency, Unity-version gate, asset rewrite, or sample-layout change.

No Unity, compiler, test process, product validator/checker, installer, service, child actor, git commit or push was started. No shared tracker, historical audit/plan, package metadata, scene/prefab, or another owner's source was edited. Earlier static-audit results are historical and are not reused as verification of these additions.

After every writer finishes, coordinator acceptance must include native execution of the occurrence/indexer cases on the frozen snapshot; legacy authored-asset import/default-zero behavior; actual Editor occurrence selection and previews; existing context/numeric/network regressions; installed Unity 6.3/6.6 compilation and MIRROR-on/off discovery; package/docs/UTF-8/meta/GUID checks; and separately owned real-peer Mirror evidence. AOT/stripping and any unsupported/unavailable environment remain explicitly unverified until their own receipts exist.

## P5 E10 — serialized positive-control correction, 2026-10-25

**Source-final for the queued E10 action; native rerun pending.** Read the reopened implementation contract/request and `Local/Audits/assembly-2026-10-23/p5-source-correction-common.md`, the actual frozen fixture, current evaluation/binding source, and both P5 XML cases.

### Historical native failure actually inspected

Case identity retained:

`Neo.Editor.Tests.Edit.Audit20261023NeoConditionRegressionTests.BindOtherToSourceIfNull_PreservesDestroyedExplicitSource`

- `Local/Audits/assembly-2026-10-23/unity/final/p5/u63/Receipts/20-full-edit-graphics.xml`: case 5510, Failed, `Expected: True / But was: False`, stack at the initial positive-control assertion on fixture line 213, before destruction (2026-10-25 15:59:58Z).
- `Local/Audits/assembly-2026-10-23/unity/final/p5/u66/Receipts/20-full-edit-graphics.xml`: the same case/stage/message/line, Failed (2026-10-25 16:26:41Z).
- Frozen source: `Local/Audits/assembly-2026-10-23/unity/snapshots/final-20261025T151934Z/source`, aggregate identity `956e2aa29ea85e7dea90df498a70392083af4a067058cbcbe24891105966efd1`. The working fixture before this correction matched its frozen SHA-256 exactly: `1d6aacf5b1da198a573b7ff7d0ab2016d9b661b2e80ffe09cba5c623cbaacb36`.

This failure does not establish a destroyed-source runtime defect: execution had not reached destruction or BindOtherToSourceIfNull. The previous arrangement mixed partial JsonUtility configuration of a standalone entry with a native inline SerializedObject update of only the other-object reference. It did not inspect the applied comparison/bindings. The XML records no field-level state, so the exact previously mismatched field is not claimed as observed.

### Bounded correction and preserved oracles

Changed only these owned paths in E10:

1. `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionRegressionTests.cs`: configure both explicit component sources, type/member names, occurrence zero, integer equality, OtherObject threshold mode, and non-inverted/non-method/non-search settings through the owner's SerializedObject in one native setup. Apply, update/read back the right binding, reacquire the managed entry, and assert the live values plus public left-binding/comparison settings before evaluation.
2. `Assets/Neoxider/Docs/Condition/Condition_Reuse.md`: document complete native serialized editor setup/reacquisition and a live-source comparison control before interpreting missing/destroyed-source outcomes.
3. This implementation receipt: preserve original handoff and append actual prior failure, correction, identities, and remaining debt.

The original initial `Evaluate == true` assertion and final destroyed-source `Evaluate == false` assertion remain. Added causal controls change the explicit other value from 3 to 4 (false) and restore equality (true), so a constant-threshold or unrelated-source setup cannot silently satisfy the positive control. After DestroyImmediate, assert the captured reference is Unity-null but not managed-null, the host remains alive, and the entry is unavailable before binding. Then call BindOtherToSourceIfNull, retain the final false assertion, and independently assert the surviving host is still a valid value source. Rebinding the destroyed explicit source to that host would therefore violate the final oracle.

No runtime code was changed for E10. In particular, `ConditionEntry.BindOtherToSourceIfNull` retains the managed-reference guard and destroyed-source semantics. Current and frozen `Assets/Neoxider/Scripts/Condition/ConditionEntry.cs` SHA-256 both remain `8f4a37146b6ec9384dbfb715563b5e0a0cabe9c0cd456f39a9eed945e3cd3570`.

### Corrected artifact hashes and acceptance debt

| Current artifact | SHA-256 |
|---|---|
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionRegressionTests.cs` | `f72a7516b0cf1acf573dc277e4c0bb4c29de46043134509c35d54f7bbfe8c961` |
| `Assets/Neoxider/Docs/Condition/Condition_Reuse.md` | `2144141cba615a491f6c6ee769cea343aa98810fb024971c436f4bdfeb7f2756` |

Actual E10 work: source/artifact reads, scoped diff review against the frozen fixture, SHA-256 bookkeeping, and the changes above. The existing test identity/case inventory, cleanup, positive/final results, native runtime contract, and all other regression cases are preserved. No new Ignore, unavailable NUnit attribute, error whitelist, tolerance change, metadata/asset rewrite, or shared tracker change.

**No execution in this reopened source phase.** A reviewed correction is not a native pass. After all writers finish and root freezes a new snapshot, rerun the exact E10 identity on Unity 6.3 and 6.6 and retain all new configuration/value controls, the positive/negative/restored-positive sequence, and the destroyed-reference final false outcome. The historical P5 failures remain evidence; no pass is claimed for the corrected fixture.
