# Neo.StateMachine.Editor: approved implementation request

Original actor: `neo-audit-1023-b06-03`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

No additional confirmed runtime defect was assigned after independent review. Preserve your coherent earlier patch. Finish canonical docs/navigation and factual API/scene/NoCode contracts in owned scope, keep at least one substantial improvement described as a proposal, and write an honest source-final receipt. Do not manufacture a runtime rewrite or duplicate tests.

## Relevant full native sections

Source: NativeReview-C.md

### C-17 / P2 — shared duration predicates reset another machine's wait

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/NoCode/StateMachineData.cs:159–165` registers shared transitions; `StateMachine.cs:170–174` notifies predicates with entry time; `StatePredicate.cs:395–424` stores one timestamp. A enters at t0, B enters same configured transition/predicate at t4; A evaluated at t5 with duration five now waits until t9. Same defect occurs in shared And/Or/Not children. Expected independent per-machine elapsed time for reusable authoring data.

Smallest correction: machine-owned scoped entry/evaluation-time context used by built-in duration in the **default** runtime and wrapper paths, preserving legacy virtual Evaluate(IState) dispatch, inversion once and nested/finally scopes. No graph cloning, global custom-state isolation claim or timer service prerequisite. New options/frame API as legacy-default opt-in does not close the defect. Explicit compatibility decision is necessary: existing `StateDurationPredicateTests.SetEnterTimeInThePast_SatisfiesTheThreshold` intentionally edits the globally shared predicate after machine entry. Keep standalone SetEnterTime behavior; provide a clearly named legacy escape or an explicit addressed machine-anchor API/migration for this machine-global edit, and test both contracts. Do not silently change custom override dispatch or claim deep immutable assets. Owners **runtime `neo-audit-1023-b06-02`**, editor `neo-audit-1023-b06-03` only if authoring exposes a compatibility option. Acceptance: two default machines/fake clock, shared composites, re-entry, legacy standalone/manual mode, nested exception cleanup.
Source: NativeReview-C.md

### Neo.StateMachine.Editor — reviewed

Full audit/plan, all five actual editor diffs, complete fifteen-case fixture, new Editor.md and runtime integration read. Serialized predicate children, context overrides, Undo before mutation, temporary handle cleanup, ctor-before-insert, membership-before-edit and explicit multi-select limitation are coherent. Fixture reflection deliberately discovers assembly/types without direct editor reference; asserts evaluation/serialized values and Undo, destroys owned windows/editors/assets and clears their Undo history. Real IMGUI removal balance, save/reload and TypeCache remain central execution/visual debt. No confirmed additional editor-source defect found. Canonical README still needs link to Editor.md (document discovery debt). Persistent transition IDs/repair are optional additive authoring work, not prerequisite for safe refusal after reconstruction; runtime must not generate IDs/dirty assets. Per-repaint TypeCache menu creation/one SerializedObject per predicate is bounded by open editor windows, not player per-object query work.

Full read fingerprints for Neo.StateMachine:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.StateMachine.md` | `349e3d2e85d599ad09ce2ce127e3176bd58f1f6d8ac91a0f21ea361b0d7b92fd` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.StateMachine.plan.md` | `fac158f8270eba8243ea9a968067b9943f51faeeab725557f1c61ba2d4239ba8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/ConditionEntryPredicate.cs` | `ee2cc23dd5acf5741e0a7573fc5c824316e6df736da4b82ae19f626f22d99080` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/NoCode/StateMachineData.cs` | `19352e4a3e27dacaf978d3c87ad5883bbd805df25145a4cdeabc76ec5ad3d2c6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StateMachine.cs` | `c734efc5a07dc89aa7fe897e7f7c5256332d335c4e1e04019a57275bc1f09454` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviour.cs` | `1cb0f135d9f1c7753e492ff874ebd56ea2179972d470b70d9c41be92465ecf1f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviourBase.cs` | `e36f98caa0f70092aa4ccd04638abd30cbdab0492f8d54c55a2bc35d5c2f3225` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StatePredicate.cs` | `af9199b54ac340ce237172905fb4aead08891fe8edee262a649bd92bf85c0f7c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StateTransition.cs` | `50f59e64e3f921313de868d0e85a2031c624f0bac908509365a363bf4d858c36` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineBehaviourTests.cs` | `9bde60aa08dd9b94d2ded654702c5274844aed993da7cee33cca3d82ba7903cc` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineDerivedState.cs` | `2ac0c04648ac4f8c6c570974ebd50d2786ef278904910224e24166ceb3fad1a0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineGenericHost.cs` | `b2ab4f7f8349ef54bb14825b0d164d45119f88b05df6712a8ae6f1e7bc4c77d8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineRuntimeTests.cs` | `655f2fee8f5c904365b24b53c0008ff7d68d6a815dfac53a91c7705cd537633b` |

Full read fingerprints for Neo.StateMachine.Editor:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.StateMachine.Editor.md` | `61fb6229d07fad81461c7679fc770b105247c8669cd33ff2dcd004e8ebc16064` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.StateMachine.Editor.plan.md` | `530342f1edcc33a84f477fabd549ee19b4f6a2761f1c541964c8f5bf4e5573ec` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/StateDataEditor.cs` | `c06cc83966d875151fbca10f4cc47f60ddcf6d7e5d78522272f7e91e5188a7b6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/StateMachineBehaviourBaseEditor.cs` | `502656b4c794463bf673abe91e74c02d03108e6f78599fbef685b7c9cea9b719` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/StateMachineEditor.cs` | `af4d44160335c3a72fc9e26570088adc9e2b07cf0a9a8aecf0f22875c9e22d6e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/StateMachineEditorRegistrar.cs` | `c209156d8d5f9908f824f57fe992ed0e09d5b33adeadb837745b0268a5c11933` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/TransitionEditorWindow.cs` | `5b35f6dd83bc7e5d76f38171e2581ce397f34e20485a297fe249c2e04c13f77f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineEditorTests.cs` | `1c1f1cd0f819391366b97005754e3e4eee299899a7d0ecb574ba6f45f32752fe` |

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.StateMachine.Editor.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Snapshot2 actual Type collection negative constraint error

Bothactualsource2 compiles report owned StateMachineEditorTests133 CS1503 System.Type->string inDoes.Not.Contain(typeofNonSerializableAction). Rootread133/134: actionTypes isType[], useactualinstalledNUnit COLLECTIONnegativeassertion (e.gCollectionAssert.DoesNotContain orsupportedHas.No.Member), retainactualnonserializabletypeexclusion ANDpositiveLogStateAction inclusion, allserialization/Undo/AddActionrules unchanged. No productionAPIoverload/stub/behaviorchange,newframework/config/otherfiles. SAMEoriginalactor boundedfixture+ownreceipt only; actual46/44errors JSON under Local/Audits/assembly-2026-10-23/unity/final/f84c9007d0494cd8a421d84d70d931a0/.../Receipts/. AlljobsSTOPPED/rootgateclosed, noUnity/compiler/tests/checkers beforeallwritersfinish/newfreeze.
