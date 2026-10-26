# Neo.Rpg.ConditionBridge: approved implementation request

Original actor: `neo-audit-1023-b01-07`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### RPG-BRIDGE-FIXTURE-ACTIVITY (P1; approved-source-correction)

Ready-controller fixture keeps root inactive against new parent activity guards. Configure input off then activate for readiness, retain disabled-controller failures rather than weakening guard.

## Relevant full native sections

Source: NativeReview-C.md

### C-03 / P1 — ConditionBridge's active-controller regressions create inactive controllers

- Current location: `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgConditionBridgeTests.cs:26`, `Evaluate_ControllerMode_UsesAssignedControllerWithoutCharacter`, lines 128–150. Cross-owner contracts: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgEvadeController.cs:56` and `RpgAttackController.cs:502`.
- Trigger: all four parameterized active-controller cases run after SetUp deactivates `_root`; child controller objects stay inactive in hierarchy. Expected precondition is a ready active assigned controller without a character. Actual source result: `CanEvade` and `CanUseAttack` reject `isActiveAndEnabled == false`; the explicit readiness assertions at 137/150 fail before evaluating the adapter. The owner's plan already records this; it remains unfixed in the current fixture.
- Smallest repair: disable built-in input, finish configuration, activate the fixture root in this test before asserting readiness; add disabled-controller false/inverted-true cases. Preserve parent RPG activity gates. Owner: **Neo.Rpg.ConditionBridge / `neo-audit-1023-b01-07`**, after final `Neo.Rpg` controller contracts from `neo-audit-1023-b05-03`.
- The historical missing EditMode bridge reference is already present in current `Neo.Editor.Tests.asmdef`; do not schedule that central repair again or infer a compiler pass from JSON alone.
Source: NativeReview-C.md

### Neo.Rpg.ConditionBridge — reviewed

Original owner/task: `neo-audit-1023-b01-07`. Full audit/plan, changed adapter body/hunks, all new test source and the full new canonical topic read. Local-result reentrancy isolation is correct; missing-character evaluations now share inversion/event dispatch and controller branches precede character absence. Serialized fields, MovedFrom and API remain intact. The fixture owns/destroys its hierarchy and temporary assets, disposes SerializedObjects, and wires events by reflection while asserting public outputs; C-03 prevents four cases from reaching their intended contract. Parent navigation is checked with the RPG owner's documentation later.

Aggregate work: evaluation is explicit-call work with local/parent lookup only, no scene-wide search, Physics query or Update loop. Repeated consumers may repeat hierarchy/property reads, but caching here would change current-state lookup semantics without demonstrated need. The typed event-free query/status proposal is a reasonable optional legacy integration seam; it must preserve zero-fallback/inversion and coordinate the parent's typed attack lookup first. Do not parse failure strings or move scene references into presets.

Read fingerprints:

| Full artifact/source path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.ConditionBridge.md` | `06b3452eb4190d5e4aaea27e6debc1c51a8f12d08ca7ab9cbcf44f37c82e074f` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.ConditionBridge.plan.md` | `f487ea68709d1fe6bd9716e14f32ccbbb8e28f766edabc663fd55a40218c2ba8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Bridge/Condition/RpgConditionAdapter.cs` | `1043b3d483fbbfc12d46553b6a9b5e585920efc19aa8ff8148468a07240e2bcf` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgConditionBridgeTests.cs` | `390f4eda1f7b51f0bd0110b930e4d0c9256aa33c512172ba501f6905813261a2` |

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Rpg.ConditionBridge.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.
