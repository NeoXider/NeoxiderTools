# Neo.Tools.InteractableObject.Core: audit

Status: source audit complete; focused fixes and regressions authored; central Unity validation pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/Neo.Tools.InteractableObject.Core.asmdef`.
- Scope: 4 C# files; nested assemblies excluded.

## Scope

All four manifest C# files were read in full. The inventory was compared with the actual Core
directory and matched exactly. No nested assembly or other worker's implementation was edited.

| Owned file | Review and outcome |
|------------|--------------------|
| `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/IInteractiveTarget.cs` | Reviewed the complete typed contract and its plain-C# implementation path. No resource ownership, serialization, background work, or event subscriptions. Lifetime, eligibility, and dispatch remain responsibilities of implementations and callers. No demonstrated defect. |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/InteractionCameraResolver.cs` | Reviewed cached/main/scene fallback order, destroyed-object null semantics, no-camera result, and `allowSceneFallback = false`. Live cached cameras intentionally remain preferred, including disabled cameras; main-camera changes do not invalidate an existing caller cache. No global state or owned scene objects. No demonstrated defect against the existing policy. |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/InteractionQueryMath.cs` | Reviewed every range, padding, nearest-hit, and target-selection branch. Checked null/empty arrays, negative/oversized counts, populated-prefix ownership, negative/non-finite distances, zero, maximum finite distance, equal-distance ordering, non-blocking hits, and blocked output semantics. Two numerical boundary defects fixed below. |
| `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/InteractionRayHit.cs` | Reviewed the entire immutable value type, constructor/property mapping, default value, flags, and lack of collider/payload ownership. The data carrier intentionally preserves supplied values; selection filters invalid distances. No demonstrated defect. |

Read-only integration context: `InteractiveObject.IsInRange`, the 2D/3D hit-buffer adapters,
camera caching, `IsInteractable`, `InteractionRayProvider.FindOnMainCamera`, existing
`InteractionQueryMathTests`, and the parent module/component docs. These confirm that the core
is reused by both automatic scene queries and custom typed input sources. Manual interaction
dispatch still belongs to the scene assembly; the interface does not imply geometric checks.

Assembly/dependency review: the core asmdef has no package assembly references and only requires
UnityEngine. The existing `Neo.Editor.Tests` asmdef already references this core and NUnit, so no
reference/config changes are required. No `GetInstanceID`, serialized instance-ID APIs, runtime
logging, ScriptableObject scene references, or owned Unity resources exist in this inventory.
`FindFirstObjectByType` remains the existing Unity 6-compatible fallback; no new version branch
was introduced. Compilation on the supported Unity versions is still a central check.

Reuse check: consulted the installed `neoxider-tools` skill's interaction/camera catalog in
`references/tools.md` and distance/collection entries in `references/extensions.md`. Kept the
existing leaf APIs and loops; added no helper, component, coroutine, dependency, or duplicate
interaction system. Applied the Unity workflow skill in file-only mode under the worker contract.

## Findings and fixes

### CORE-01: finite range inputs lose their boundary when float intermediates lose scale

- **Severity:** low; deterministic public-API correctness issue at extreme coordinate scales.
- **Source:** `InteractionQueryMath.IsWithinRange`, originally the float subtraction/squared-magnitude
  and float maximum-distance square immediately after the unlimited-distance shortcut.
- **Trigger:** finite distances whose squares overflow or underflow float, or finite endpoints
  whose subtraction overflows float. All inputs can be finite and the configured range positive.
- **Observable consequence:** an out-of-range target is accepted because both squared values
  become infinity or zero. Custom coordinate systems are affected; this is not a claim that
  Unity physics supports these world scales.
- **Fix:** promote each coordinate before subtraction, then square/sum and compare in double.
  This covers finite float differences and their squares without allocating or changing the
  public signature. Non-positive unlimited ranges and the inclusive boundary remain supported.
  No blanket NaN/infinity input policy was added.

Executed Python IEEE-754 arithmetic probes, rounding the old expression's operations to float32
with `struct.pack`/`struct.unpack`. These reproduce the numerical defect independently of Unity;
they are **not execution of the C# implementation or NUnit tests**.

| Source X | Target X | Maximum | Old squared values | Old result | Expected result |
|----------|----------|---------|--------------------|------------|-----------------|
| `0` | `3e20f` | `2e20f` | infinity / infinity | true | false |
| `-3e38f` | `3e38f` | `3e38f` | infinity / infinity after subtraction overflow | true | false |
| `0` | `1e-23f` | `5e-24f` | zero / zero | true | false |

Regressions: eight parameterized axis/range cases cover overflow, subtraction overflow,
underflow, subnormal coordinates, inside range, and the inclusive boundary. Two diagonal cases
cover all three axes at overflowing/underflowing float-square scales. Assertions observe the
public Boolean result with known inside/outside geometry.

### CORE-02: maximum finite hit distance collides with the initial sentinel

- **Severity:** low; deterministic boundary-value rejection in reusable hit queries.
- **Source:** `InteractionQueryMath.TryGetNearestHit` and `TrySelectTarget`, originally their
  `float.MaxValue` initial nearest-distance sentinels and strict improvement comparisons.
- **Trigger:** the sole or nearest valid candidate has `Distance == float.MaxValue`.
- **Observable consequence:** nearest-hit returns false/default; target-selection also returns
  false/default even with no blocker or with obstacle checks disabled. The value is finite and
  non-negative, so it should pass the methods' own validity filter.
- **Fix:** use positive infinity as the initial distance for nearest hits, targets, and blockers.
  The existing validity filter still rejects actual infinite distances. First-candidate tie
  retention and the inclusive equal-distance blocker rule are preserved.

The executed arithmetic probe confirmed both old admission comparisons reject `float.MaxValue`.
Regressions add two nearest-hit cases, four target-selection cases, and two closer-blocker cases.
They assert success/rejection, selected distance, point/identity marker, and relevant flags. Cases
include invalid-distance candidates, stale buffer entries outside `count`, equal-distance target
ties, an equal-distance blocker, and retained blocked-target output.

All regressions are in the new file
`Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectCoreQueryMathTests.cs`
(five methods, 18 parameterized cases). The existing shared test file was left untouched.

### Reviewed behavior retained

- Negative/non-positive counts and null/empty buffers produce no result; counts exceeding array
  length are clamped. Buffers remain caller-owned, unmodified, and unsorted.
- Negative, NaN, and infinite hit distances are ignored. Hit points are opaque returned data.
- A hit marked as a target does not block itself; non-blocking foreign hits do not occlude it.
- A rejected but present target remains in the `out` parameter. Existing tests rely on this;
  replacing it with default would be an incompatible behavior change.
- Camera eligibility is a policy, not an inferred bug. Disabled cached cameras and sticky caches
  are now documented; changing them would require an explicit API/policy decision.
- `GetObstacleCheckDistance` retains its negative-padding/zero clamp behavior. Callers should
  supply finite configuration; no unsupported assumption about NaN/infinity was converted into a fix.

## Documentation

Added the narrow, manifest-owned English entry point
`Assets/Neoxider/Docs/Tools/InteractableObject/Core/README.md`. The assigned Core docs directory
did not exist, so there was no existing owned guide to update. It documents:

- namespace/leaf assembly dependencies and typed `IInteractiveTarget` dispatch;
- query boundary, invalid-distance, count-prefix, tie, and blocked-output contracts;
- an allocation-free caller-buffer C# adapter example with range/path checks;
- camera fallback, sticky/disabled caches, main-thread use, and object lifetime;
- the scene/NoCode workflow through the separate `InteractiveObject` wrapper and its manual
  dispatch semantics.

Updated owned XML method summaries. Added unique `.meta` files for the new test and doc.
No folder metadata was created: the coordinator owns that step.

Кратко: исправлены численные граничные ошибки проверки расстояния и выбора попадания.
Публичные сигнатуры и сериализованные данные сохранены; Unity и тесты не запускались.

## Substantial improvement proposal

**Proposed, not implemented: return selection identity and an explicit decision from the pure core.**

**Problem.** `InteractionRayHit` contains geometry and flags but no identity. A custom source
that keeps parallel arrays of colliders, entities, or `IInteractiveTarget` receivers receives
only a copied hit from selection. Two hits can have identical distance, point, and flags, so
matching the returned value cannot reliably identify the selected original entry. The Boolean
result also mixes a missing target and a blocked target. Callers must reconstruct information
the selection loop already had or duplicate selection logic, which can drift from the shared
tie/obstacle rules.

**Architecture and public API.** Add an immutable result type and an additive method in the
same dependency-free leaf:

```csharp
public enum InteractionSelectionStatus
{
    NoTarget,
    Clear,
    Blocked
}

// Proposed properties on a readonly InteractionSelectionResult struct:
// InteractionSelectionStatus Status { get; }
// int TargetIndex { get; }       // -1 when no target exists
// int BlockingHitIndex { get; }  // -1 unless a closer blocker rejects the target

public static InteractionSelectionResult SelectTarget(
    InteractionRayHit[] hits, int count, bool requireClearPath);
```

Keep reduction over the existing caller-owned populated prefix. Record source indices during
that pass; preserve first-entry ties, finite/non-negative filtering, and inclusive blocker
distance. Return a value type without allocation, scene search, collider inspection, target
references, or a network dependency. The source indexes its own parallel receiver array and
can give precise blocked-target feedback without geometry matching. Document that indices are
valid for the supplied buffer snapshot; consumers must not queue them after reusing the buffer.

**Compatibility and migration.** Keep `InteractionRayHit`, `IInteractiveTarget`, and all current
signatures unchanged. Existing `TrySelectTarget` can delegate to the same internal reduction,
preserving its blocked-target output. Introduce the richer API additively; migrate scene
adapters only with their owning worker in a separate change. No serialized field, prefab, or
ScriptableObject migration is needed. Avoid adding public generics or scene-object payloads
merely to carry source identity.

**Acceptance scenarios.**

1. Two distinct receivers with identical geometry retain and report the first target's source index.
2. A closer blocker yields `Blocked` plus correct target/blocker indices; an equal-distance
   blocker yields `Clear`.
3. Null/empty/invalid prefixes yield `NoTarget` with both indices `-1`; oversized counts clamp.
4. Invalid-distance entries and stale entries outside `count` never produce indices.
5. Disabling clear-path requirements yields `Clear` for a valid target despite a closer blocker.
6. Legacy methods return the same Boolean and copied hit as before for all valid inputs,
   including blocked output, zero, `float.MaxValue`, and equal-distance ties.
7. A custom input adapter dispatches to the selected parallel receiver with no allocation or
   reference to the scene/network assembly.

**Effort/risk.** Approximately 1-2 developer days for the small pure-core result/reduction API,
focused regression coverage, and docs; scene-adapter migration is a separate owner task. Low
serialization risk and moderate contract risk: legacy failure outputs and index snapshot
lifetime must be explicit. Verify allocations and legacy parity centrally before adopting it.

## Verification and remaining risks

Executed checks:

- Exact manifest inventory equals all four C# files under the assigned Core directory.
- Source review covers every owned C# file and the relevant read-only integration sites above.
- Python float32/double arithmetic probes reproduced both defects, including the three range
  counterexamples in the table. This is numerical evidence, not compiled C# execution.
- Static scan found no `var` declarations or forbidden instance-ID APIs in owned source.
- New regression filename matches `Audit20261023NeoToolsInteractableObjectCore`; the existing
  edit-mode asmdef already references the core. Five methods contain 18 parameterized cases.
- Scoped `git diff --check -- <owned changed files>` passed. New untracked files additionally
  passed strict UTF-8/whitespace checks, since ordinary `git diff` does not inspect them.
- Strict decoding, final-newline/trailing-whitespace checks, and replacement/control-character
  scans passed across nine scoped source/test/docs/metadata files. Simple delimiter and explicit-type
  scans passed for the owned/new C# files. This is static consistency, not C# parsing/compilation.
- All relative links in the new core README resolve. Package-wide GUID lookup returned exactly
  the expected new `.meta` file for each of `2c9912dcb65b497d8620f4156d95ff3a` and
  `fae2c090a0354ae89e962d29715accb3`; no collision was found.

Central checks still required: compile/import on the supported Unity versions and run existing
`InteractionQueryMathTests` plus
`Audit20261023NeoToolsInteractableObjectCoreQueryMathTests`. No Unity, MCP, build, test runner,
install, shared-config edit, commit, or child agent was used. No C# compiler/parser was run;
optional Python tree-sitter modules were unavailable, and no installation was attempted.

Remaining risk: using double intermediates makes near-boundary range decisions more precise and
may change results that previously depended on float rounding. Target-platform performance of
the small arithmetic change has not been measured. The fixes do not promise that parent Unity
physics/direction calculations support extreme world coordinates; that assembly was not edited.
Camera fallback behavior requires central Unity verification; no camera logic changed here.

Coordinator actions: link `Core/README.md` from the parent module entry (outside this worker's
docs ownership); create folder metadata for the new Core docs directory; run central compilation
and the named edit-mode fixtures. No assembly-reference change is requested.
