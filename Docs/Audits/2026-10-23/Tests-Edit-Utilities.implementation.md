# Tests-Edit-Utilities — implementation receipt

Status: **source-final for the assigned scope; acceptance UNEXECUTED**.

Date: 2026-10-24. Same original supplemental session, `neo-audit-1023-extra-04`. Requested integration branch/target: `demo` / `11.0.0`.

## Approved actions and completed work

Authority: `IMPLEMENTATION-CONTRACT.md` and `Tests-Edit-Utilities.implementation-request.md`, including the complete supplied NativeReview-B section for Tests-Edit-Utilities. The request assigns no additional confirmed runtime correction or numeric action ID. B-19–B-23 legacy-fixture work remains with the primary `Neo.Editor.Tests` owner; it is not reassigned or claimed complete by this receipt.

1. Preserved the three coherent, previously authored prefixed regression sources and their metadata. Current source no longer contains the unsupported `NonParallelizable` attribute removed centrally; this continuation did not repeat that patch or add any fixture-serialization annotation.
2. Finished `Assets/Neoxider/Docs/Testing/Utilities.md`:
   - Removed the stale recommendation to serialize a fixture with that annotation. The guide now states the actual sequential/exclusive runner requirement, captured cleanup, and dedicated company/product/effective preference and persistence namespace.
   - Added navigation to the canonical `../Testing.md`, `../PlayModeTesting.md`, and sibling `Gameplay.md` / `NetworkPlayMode.md` guides.
   - Retained the explicit C# extension example and component/scene/NoCode contract table. Kept synchronous public API checks separate from lifecycle, rendering, device and async integration evidence.
   - Updated the imported sample example to `Assets/Samples/NeoxiderTools/11.0.0/Demo Scenes/...`, preserving development, hidden release and installed-package layout guidance.
   - Explicitly identified common ownership/capability/resolver support as a **proposal**, coordinated by the primary test owner and reusing the existing small save/preview scopes. No competing generic scope was implemented.
3. Wrote this completion receipt. Original audit and plan remain historical evidence; they were not rewritten.

## Exact retained source artifacts

All test paths below are under `Assets/Neoxider/Tests/Edit/`:

| File | Authored methods / source cases | Observable acceptance, UNEXECUTED |
| --- | --- | --- |
| `Extensions/Audit20261023TestUtilitiesEnumerableTests.cs` | 8 / 11 | Actual `IEnumerable` extension dispatch; ordered callback delivery; iterator disposal and original exception on callback failure; null handling; extreme wrapped/safe indices; copying versus in-place shuffle identity, source order and duplicate multiplicity. |
| `Extensions/Audit20261023TestUtilitiesSerializationTests.cs` | 8 / 16 | Fractional UTC ticks/kind and offset normalization; culture-independent float preferences; actual escaped legacy JSON; rejected-write preservation and empty-write deletion; duration separator and segment boundaries. |
| `Tools/Audit20261023TestUtilitiesSceneContractsTests.cs` | 6 / 16 | Complete TMP percentage/currency strings and event payload/count; invalid duration preserves the prior time/text/events; repeated custom-separator value notifies once; an actual right/backward turn and absolute 30-degree yaw limit. |

Total: **22 methods / 43 authored cases**, not a Unity discovery or pass count. The associated `.cs.meta` files retain GUIDs `f5e4e66edf6744039b72133c07c30e29`, `c8069c6ecd5845128e92a3e2f3f0cdcb`, and `ec9adb0dc8a64f609f5defec20d703f6`, respectively. `Utilities.md.meta` retains `4e44fa62febb4ca1adb6c7a0752ba5c3`.

Files edited in this continuation: **Utilities.md and this receipt only**. No runtime/editor source, legacy fixture, shared asmdef, tracker, audit, plan, package metadata, scene, prefab or existing GUID was changed by this continuation.

## API and compatibility

No additive production API, serialized migration or new scene wiring is introduced. The regressions keep using the existing `Neo.Extensions` APIs, `PlayerPrefsUtils`, `SetText.SetPercentage` / `SetCurrency`, `TimeToText.Set` / `TrySetFromString`, and `UniversalRotator.RotateTo` paths. Current text-source signatures/events were inspected as context. The test assembly already references the required APIs; no reference change is requested.

Tests preserve culture and RNG in `finally`, use GUID preference keys with selective cleanup, and retain an inactive, explicitly owned scene root. These cleanup rules do not make execution parallel-safe: shared-state execution remains the coordinator's exclusive-host responsibility.

## Dependencies and deliberately unimplemented proposals

- **No pending additive API blocks this assigned scope.** The finite-weight overflow correction and numeric regressions are already owned by the numeric Extensions actor; no duplicate patch or acceptance fixture was added.
- `Testing.md` currently links to this utility guide, and `Docs/Testing.meta` now exists. These central changes were observed, not recreated. Root retains ownership of global navigation/folder metadata integration.
- Existing legacy fixture repairs, positive reflection/throttle controls and shared package/sample resolver work stay with the primary test owner. The larger ownership/capability/resolver design remains a proposal in the historical individual plan and is labelled accordingly in the public guide.
- Aggregate interaction-query acceptance belongs to the runtime/query owners and primary integration test coordination. No extra aggregate fixture was assigned to this actor, so no uncoordinated physics harness or duplicate shared internals were introduced.

## Work evidence and remaining verification debt

This phase used small `cmd.exe` / Python file reads, scoped Git source-diff inspection, and reference/file enumeration. The implementation request, contract, retained fixtures, relevant historical context, current text API declarations, public guide and metadata were inspected. Historical audit checks remain historical; they are not current integrated evidence.

`git branch --show-current` returned `demo`; scoped `git diff --check` returned 0. The separate no-index checks of the two untracked Markdown files returned 1 with only Git's LF-to-CRLF conversion notices. These are recorded as source-diff observations, not product-validator passes; no line-ending policy was changed.

No Unity, compiler, test runner, product checker/validator, install, service, child agent, commit or push was launched. Shared trackers were not changed. This receipt does not declare a global source freeze.

After every writer finishes, the coordinator must compile and discover the frozen sources on the installed Unity 6.3 and 6.6 hosts, run these 43 cases under the exclusive validation contract, and record actual pass/fail/skip results. Repeated/reordered fixture runs must preserve preference, culture and RNG baselines and release owned objects. Real lifecycle/UI/audio/physics and imported-sample/MIRROR-on/off acceptance remain separate central evidence. Unsupported version branches and unavailable capabilities must remain explicit debt; none is claimed passed here.
