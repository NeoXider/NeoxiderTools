# Neo.StateMachine.Editor — implementation receipt

Branch/target: `demo` / `11.0.0`. Original actor/session: `neo-audit-1023-b06-03`.

Status: **source-final**, including the bounded 2026-10-24 snapshot2 NUnit assertion correction recorded below. Earlier implementation/navigation notes retain their original inspection context. No Unity/compiler/test/product-validation execution was performed by this actor; renewed execution requires the coordinator's new global source freeze.

## API publication for dependents

**No additive editor API is introduced in this approved scope.** Existing signatures remain:

- `public static void TransitionEditorWindow.ShowWindow(StateTransition transition, StateMachineData data)`.
- `public static bool StateMachineEditorRegistrar.IsInitialized { get; private set; }`.

The five reviewed editor source files and their serialized/public contracts are retained. The optional `Id`, repair utility and `TryShowWindowById` design remain proposals in the historical plan; dependents must not compile against those proposed APIs.

## Assigned actions and boundaries

- **`Neo.StateMachine.Editor — reviewed` — completed in owned scope**: no additional numbered editor-source defect was assigned. Preserved the earlier seven bounded corrections and completed owned `Editor.md` contracts/navigation without rewriting runtime logic or duplicating tests.
- **C-17**: assigned to `Neo.StateMachine` (`neo-audit-1023-b06-02`). Editor work is conditional on that actor exposing a serialized authoring compatibility option. The current component source has no such option. This worker will not invent one or claim C-17 is fixed.
- The module entry backlink in `Assets/Neoxider/Docs/StateMachine/README.md` belongs to the parent `Neo.StateMachine` actor under the implementation contract; it is not edited here.

## Exact files and completed work

The following five files retain the coherent earlier patch, with SHA-256 matching the full-read native review fingerprints in the implementation request:

| File under `Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/` | Retained correction |
|---|---|
| `StateDataEditor.cs` | Serializable type filtering, callback property-path reacquisition, construction before insertion and error handling |
| `StateMachineBehaviourBaseEditor.cs` | Scene context array authoring and explicit C# locals |
| `StateMachineEditor.cs` | All-target validation, pending-edit application and explicit single-owner condition editing |
| `StateMachineEditorRegistrar.cs` | Unconditional temporary editor/asset cleanup in `finally` |
| `TransitionEditorWindow.cs` | Concrete serialized predicate fields, scoped serialization handles, Undo, balanced deferred removal and stale/destroyed/closed target refusal |

Finalized in this implementation pass:

- `Assets/Neoxider/Docs/StateMachine/Editor.md`: documented both type menus' constructor/serialization contracts, typed action/predicate extension points, serialized fields versus runtime delegates, single-asset condition editing, owner-membership/destruction behavior, and the factual distinction between `IsInitialized` and configuration validation. Added navigation to the state/machine asset, scene wrapper, condition and C# extension guides. Runtime timing contracts link to their parent's runtime documentation rather than inventing an unpublished compatibility API.
- `Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineEditorTests.cs`: strengthened the existing constructor-failure case. It now explicitly applies pending serialized edits after the failure, checks the original count/content, then successfully appends one valid independent action and checks both values. This exercises the delayed-apply corruption path; it adds no duplicate fixture or extra test case.
- `Docs/Audits/2026-10-23/Neo.StateMachine.Editor.implementation.md`: this source-final/dependency receipt.

Existing `.meta` files are preserved; no new package file, assembly reference, version branch, serialized field, scene/sample asset or shared configuration was introduced. Runtime/player APIs are unchanged. There is no new editor migration: reconstructed inline transitions still require reopening their window, as documented. Persistent IDs/repair remain an optional proposal, not an implemented capability.

The original audit and plan are untouched. Their current SHA-256 values still match the request's historical fingerprints:

- Audit: `61fb6229d07fad81461c7679fc770b105247c8669cd33ff2dcd004e8ebc16064`.
- Plan: `530342f1edcc33a84f477fabd549ee19b4f6a2761f1c541964c8f5bf4e5573ec`.

## Authored acceptance — UNEXECUTED

The existing fixture still contains **15** ordinary/parameterized cases: three independent action-list insertions/Undo; constructor failure and subsequent recovery; unsupported action type filtering; predicate addition/serialization/Undo/Redo; removal of the selected null entry/Undo; header value Undo with endpoint preservation; built-in/custom predicate field evaluation and JSON persistence; reorder targeting; stale replacement refusal; destroyed owner refusal; late command refusal after window destruction; and multi-asset validation.

None of these cases was executed here. They assert configuration/evaluation/Undo output rather than registration alone. They do not constitute real IMGUI, native asset save/reload or Play Mode context evidence.

## Actual work evidence and deferred validation

- Read the complete implementation contract/request, historical audit/plan, exact manifest entry and relevant current editor/runtime source/docs. Read-only branch inspection returned `demo`.
- Read-only `Get-FileHash` matched all five preserved C# sources and both historical artifacts to the native review fingerprints. Regression attributes were enumerated with `rg`; the fixture remains 15 cases.
- Scoped `git diff --check` returned exit code 0 for the owned path set. This is a diff check, not a product checker or compilation; it does not inspect untracked guide/test/receipt contents as staged diffs. Those files were edited/read as UTF-8, and their final hashes are recorded below for the coordinator's frozen-snapshot checks.
- No compiler, Unity/MCP, test runner, product checker, service, install, child agent, commit or push was launched. Historical audit checks were not relabeled as new execution results.

Previous implementation artifact SHA-256 (historical bookkeeping; the fixture hash is superseded by the 2026-10-24 correction below):

| File | SHA-256 |
|---|---|
| `Assets/Neoxider/Docs/StateMachine/Editor.md` | `614112b238a12b3d6674f0c4ffe14c74583775f79a610db4827dda9933c7e70d` |
| `Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineEditorTests.cs` | `3746957b75b5455e81a9a02355674368439529c96c54265a5808f3bf030fd313` |

## Remaining integration dependency — original implementation snapshot

These observations describe the earlier implementation pass and were not re-audited during the bounded snapshot2 fixture correction.

**Exact owner/action:** `Neo.StateMachine` / `neo-audit-1023-b06-02` must add `[Editor](Editor.md)` to `Assets/Neoxider/Docs/StateMachine/README.md`. The current entry does not link that topic; this worker follows the contract assigning parent-entry backlinks to the parent owner.

**Conditional source dependency:** that same runtime actor owns C-17 and must publish its addressed-anchor/compatibility contract. At this receipt's final source inspection, no runtime implementation receipt or new serialized timing option was present. If the final `StateMachineBehaviourBase` gains such an option, root should resume this same editor actor to expose the exact field in `StateMachineBehaviourBaseEditor.cs` and document its real default. A runtime addressed C# anchor API alone requires no new inspector setting. This worker consumes no pending additive API and leaves no speculative source reference that would block compilation.

All 15 authored editor regression cases remain **UNEXECUTED**. Actual IMGUI removal, TypeCache, asset save/reload, runtime-owned duration isolation, scene context behavior and installed Unity 6.3/6.6 compilation await the global source freeze and central verification operator. Other supported-version branches remain unverified until compiled by their appropriate Unity version.

## 2026-10-24 — snapshot2 NUnit Type collection assertion correction

**Assigned action:** `Snapshot2 actual Type collection negative constraint error` in this actor's implementation request. Root reported CS1503 at fixture line 133 in both actual source2 compilation receipts (aggregate 46/44 errors) under `Local/Audits/assembly-2026-10-23/unity/final/f84c9007d0494cd8a421d84d70d931a0/`. That is supplied central evidence; this actor did not rerun either compilation or inspect unrelated errors.

**Completed source change:** only line 133 of `Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineEditorTests.cs` changed from `Assert.That(actionTypes, Does.Not.Contain(typeof(NonSerializableAction)));` to `CollectionAssert.DoesNotContain(actionTypes, typeof(NonSerializableAction));`. The actual collection remains `Type[]`, and exclusion still checks the concrete `System.Type`. The positive `Assert.That(actionTypes, Does.Contain(typeof(LogStateAction)));` at line 134 and the empty action-list assertion remain intact. Constructor failure/recovery, serialization, Undo and every production API/rule remain unchanged. No test case, overload, stub, framework, configuration, public documentation or metadata was added or altered in this follow-up.

**Read-only reference evidence:** the installed `Library/PackageCache/com.unity.ext.nunit@d8c07649098d/net40/unity-custom/nunit.framework.dll` contains the `DoesNotContain` member name. Existing package fixtures already use `CollectionAssert.DoesNotContain` (`Assets/Neoxider/Tests/Edit/Editor/CreateMenuObjectLegacyTests.cs:61–62`). Source inspection confirms the negative collection assertion and positive type-inclusion assertion are adjacent and preserved. These reads are not a compilation or test pass.

**Current fixture SHA-256:** `a6a050b8364825c269b1b82d1be5780042adb0dc5535bc6d3b350086f95c42ed`, recorded with read-only `certutil -hashfile`. The original audit/plan and partial coherent implementation edits were preserved. The only files written in this resume are the owned fixture and this receipt.

**Acceptance remains UNEXECUTED:** on the next frozen snapshot, both installed Unity versions must compile this assertion without CS1503, then the existing test must exclude `NonSerializableAction`, include `LogStateAction`, and leave the action list empty after rejecting the unsupported type. The fixture still has 15 cases. No Unity, compiler, test runner, product checker, installer, service, commit/push or child actor was launched. No unavailable NUnit annotation was introduced.

**Dependency for this bounded correction:** no source/API dependency remains. Root owns reopening the verification gate after all writers finish; fresh compilation/test results are required before claiming resolution in executed evidence. Earlier parent/C-17 integration notes above are historical, outside this two-file follow-up.
