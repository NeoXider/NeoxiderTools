# Neo.Tools.InteractableObject.Core: implementation receipt

Status: **source-final** for the approved bounded request. Target: `demo` / `11.0.0`.
Original actor/session: `neo-audit-1023-b07-04`; continued after the Windows resource interruption.
No pending additive API dependency blocks this assembly's source completion.

## Approved actions and completed work

Read `IMPLEMENTATION-CONTRACT.md`, this assembly's implementation request, its original complete
audit/plan, and the authoritative `INTERACTION-QUERY-DESIGN.md`. Re-read all four current owned
C# files, the leaf asmdef, existing prefixed regression source, and current owned guide. Read the
parent camera call sites and current parent/shared-query guides as integration context.

The request assigns no additional confirmed runtime defect. The approved audit actions **CORE-01**
and **CORE-02** remain implemented; their existing coherent patches were preserved, not repeated:

- `InteractionQueryMath.IsWithinRange` promotes coordinates before subtraction, then compares
  squared distance/range in double. Finite float overflow/underflow no longer makes distant
  targets pass simply because both squared intermediates became infinity or zero.
- `TryGetNearestHit` and `TrySelectTarget` retain positive-infinity initial sentinels so a finite
  `float.MaxValue` distance can win. Negative/non-finite hit distances remain excluded; first-entry
  ties, inclusive equal-distance blockers, and the copied target output on blocked failure remain.

Completed the specific **NativeReview-A / Neo.Tools.InteractableObject.Core documentation follow-up**:
the guide now separates a custom adapter's controllable camera reference from `InteractiveObject`'s
private cache. It explicitly states that the concrete component has no public camera setter/reset
and that `InvalidateCachedColliders()` does not replace that camera. It links the provider workflow.

Integrated owned navigation to the parent's complete shared-query guide and explained the boundary:
Core consumes target-classified candidate values; shared raw physics buffers/epochs are owned by the
scene assembly. Classification is target-local and borrowed data is consumed before external
callbacks/fresh queries. No mandatory controller, registry, or new scene setup was introduced.

## Exact files and API scope

| File | Disposition |
|------|-------------|
| `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/InteractionQueryMath.cs` | Earlier approved runtime/XML fixes preserved; no new runtime edit in this continuation. |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/IInteractiveTarget.cs` | Unchanged; no new required members. |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/InteractionRayHit.cs` | Unchanged immutable DTO. |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/InteractionCameraResolver.cs` | Unchanged camera policy. |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/Neo.Tools.InteractableObject.Core.asmdef` | Read only; package reference list remains empty. |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectCoreQueryMathTests.cs` | Existing five methods / 18 parameterized cases retained without duplication or edits. |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectCoreQueryMathTests.cs.meta` | Existing metadata retained. |
| `Assets/Neoxider/Docs/Tools/InteractableObject/Core/README.md` | Updated camera API facts, scene guidance, and shared-query navigation during this continuation. |
| `Assets/Neoxider/Docs/Tools/InteractableObject/Core/README.md.meta` | Existing metadata retained. |
| `Docs/Audits/2026-10-23/Neo.Tools.InteractableObject.Core.implementation.md` | This new source-final receipt. |

Additive public APIs introduced here: **none**. No serialized field, prefab, scene, sample layout,
asmdef, package version, shared tracker, or historical audit/plan was changed. The deliberate behavior
corrections are only the two approved numerical cases above; near-boundary range results now use
double intermediates. Disabled/live cached-camera retention remains existing policy.

The substantial indexed `InteractionSelectionResult` proposal remains in the individual plan as
**unimplemented future work**. It is not a prerequisite for shared physics-query reuse. No result type,
enum, duplicate query backend, optional camera-reset API, or additional regression fixture was added.

## Authored acceptance sources — UNEXECUTED

The retained `Audit20261023NeoToolsInteractableObjectCoreQueryMathTests` fixture asserts observable
public Boolean and hit outputs rather than internal source shape:

| Cases | Expected observable behavior |
|-------|------------------------------|
| Eight axis/range cases | Reject out-of-range overflow, subtraction-overflow, underflow, and subnormal inputs; preserve finite inside/inclusive-boundary acceptance. |
| Two diagonal cases | Three-axis targets beyond the configured range reject when float squares would overflow or underflow. |
| Two nearest-hit cases | Select `float.MaxValue` with correct point/target/blocker flags, ignoring invalid distances and the stale entry beyond `count`. |
| Four target-selection cases | Accept maximum-finite targets with clear-path checks on/off and no closer blocker; preserve the first target tie and allow an equal-distance blocker. |
| Two closer-blocker cases | Reject only when clear-path checks are required, retaining the maximum-finite candidate in the output on rejection. |

No `[NonParallelizable]` attribute was introduced; these tests own no runtime scene/shared-state
resources. Existing `InteractionQueryMathTests` remains another owner's legacy fixture.

## Cross-owner integration and remaining evidence debt

The parent owner has already added the direct Core link in
`Assets/Neoxider/Docs/Tools/InteractableObject/README.md`; the coordinator-owned
`Assets/Neoxider/Docs/Tools/InteractableObject/Core.meta` is now present. Both were inspected, not edited.
The Core guide now links back to the parent, concrete component, ray provider, and shared-query topic.

`InteractionPhysicsQueries`, `InteractionRayProvider`, and automatic `InteractiveObject` integration
belong to `Neo.Tools.InteractableObject` / `neo-audit-1023-b07-03`. Their complete-buffer, callback,
epoch, fresh-manual-query, and aggregate-query-count acceptance belongs to that owner and the central
verification operator. Core neither claims those features as its implementation nor blocks them
with the deferred indexed-result proposal. No shared asmdef reference change is requested.

Actual work performed in this continuation: small `cmd.exe` source reads, `rg` call-site/inventory
enumeration, an ordinary scoped `git diff` confirming preserved math edits, and the owned doc/receipt
edits. No Unity, compiler, tests, product validators/checkers, installation, services, commits, push,
or child actors were run. Historical phase-1 static/numerical receipts were left untouched and are
not presented as execution evidence for the current integrated source.

After **all** writers finish, the coordinator's existing operator must compile/discover/run the
named fixtures on installed Unity 6.3 and 6.6 with the required MIRROR-on/off integration evidence.
Record other advertised Unity versions without actual receipts as unverified. Verify ordinary-scale
inclusive boundaries, parent 2D/3D input/range/event behavior, and camera lifetime/fallback contracts;
no extreme-coordinate physics support is claimed. Current compile, runtime, target-device performance,
and integrated package/docs/UTF-8/meta/GUID checks remain **unexecuted by this actor** under the barrier.
