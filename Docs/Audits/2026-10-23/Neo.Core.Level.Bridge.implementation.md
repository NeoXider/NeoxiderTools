# Neo.Core.Level.Bridge — implementation receipt

Status: **SOURCE-FINAL**. Branch `demo`; target `11.0.0`; original actor `neo-audit-1023-b03-01`, `gpt-6.1-sol`, reasoning `max`. Read `IMPLEMENTATION-CONTRACT.md`, the exact implementation request, historical audit/plan, current owned runtime, regression source, and public guides. The shared state records `globalReviewApproved: true`. Following the Windows interruption, reread the current contract/request using cmd.exe/Python and continued the same partial changes. No source/API dependency is blocking this owner.

## Published API and dependencies

**Additive APIs: none. No dependent owner needs to wait for a new Bridge API.** Preserve the reviewed runtime patch and its existing public contracts:

- `LevelConditionAdapter.LastResult`, `Evaluate(GameObject context)`, `EvaluateCurrent()`.
- `LevelNoCodeAction.LevelProvider` (`LevelComponent`), `ActionType`, `XpAmount`, `TargetLevel`, `OnSuccess`, `OnLevelUp`, `SetXpAmount(int)`, `SetTargetLevel(int)`, `Execute()`.
- Both existing enum identities/order and all serialized fields remain unchanged.

`Initialize(ILevelProvider)`, typed condition configuration, and `LevelConditionRule` remain the substantial improvement proposal in the historical plan. They are not implemented APIs for 11.0.0 by this worker.

## Assigned actions

The exact implementation request assigns **no additional confirmed runtime action ID** after native review. Its bounded work is to preserve the accepted missing-provider/LastResult and captured-level event corrections, finish canonical owned documentation/navigation, and provide this source-final receipt. No new runtime rewrite or duplicate regression source is required.

Completed that assigned work. The accepted runtime and meaningful regression source remain intact; this implementation phase adds the canonical documentation entry and completes factual navigation/authoring guidance. It does not repeat the runtime patch or manufacture another fix.

## Exact owned files

Retained accepted runtime corrections:

- `Assets/Neoxider/Scripts/Core/Level/Bridge/LevelConditionAdapter.cs`: missing-provider evaluation stores and returns the same post-inversion result.
- `Assets/Neoxider/Scripts/Core/Level/Bridge/LevelNoCodeAction.cs`: capture the resulting level before success observers, then report that captured result in the original success-before-level-event order.
- `Assets/Neoxider/Scripts/Core/Level/Bridge/LevelConditionEvaluationMode.cs` and `LevelNoCodeActionType.cs`: preserved unchanged, including serialized enum ordering.

Retained regression source:

- `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelBridgeTests.cs` and its existing `.meta`: 10 methods / 22 NUnit cases authored by this worker during the audit; reviewed again, not duplicated or executed. Persistence is disabled before provider initialization and owned hosts/curve are destroyed in teardown. No unavailable NUnit attribute was introduced.

Completed owned public documentation:

- **New** `Assets/Neoxider/Docs/Core/Level/Bridge/README.md`: canonical entry mapping direct C# operations, condition evaluation, UnityEvent actions, provider configuration, and the relevant assembly references to existing guides.
- **New** `Assets/Neoxider/Docs/Core/Level/Bridge/README.md.meta`: TextScriptImporter metadata; assigned GUID `971f00cebf62458d946905c994ba0f0e`.
- `Assets/Neoxider/Docs/Core/Level/Bridge/LevelConditionAdapter.md`: backlink to the canonical entry; makes explicit evaluation/cached LastResult behavior precise. Documents the actual Inspector route through NeoCondition's ConditionEntry rows and `LevelComponent.LevelStateValue`, `XpStateValue`, and `XpToNextLevelStateValue`, rather than implying NeoCondition accepts an arbitrary evaluator component as a condition row.
- `Assets/Neoxider/Docs/Core/Level/Bridge/LevelNoCodeAction.md`: backlink to the canonical entry; explicit invocation contract and the NeoCondition OnTrue-to-Execute workflow. Retains the concrete provider restriction, setter clamping, no-op success, downward changes, captured payloads, nested ordering, and exception behavior.

Receipt: `Docs/Audits/2026-10-23/Neo.Core.Level.Bridge.implementation.md` (this file).

The original audit and plan remain historical artifacts, unchanged at the hashes recorded in the native review. No shared manifest, review action, contract, configuration, parent documentation, or other owner's receipt was edited.

## Compatibility and migration

No additive API, serialized field/type change, enum relocation, assembly reference, scene/prefab rewrite, sample-layout change, or package-version edit is required. Existing explicit/sibling/context resolution remains local and per call. Missing providers still evaluate false before inversion; inverted absence still passes. The adapter does not poll when LastResult is read.

The deliberate corrective behavior is event attribution: when an OnSuccess listener changes the provider, OnLevelUp reports the completed operation's captured level, which can differ from the provider's later state. Success remains first; downward SetLevel changes still produce this historically named event. Callback exceptions retain propagation/short-circuit behavior. Changes inside the provider call itself remain the provider's responsibility.

The public docs use existing ILevelProvider, IConditionEvaluator, ConditionEntry, LevelComponent and UnityEvent contracts. The optional typed-injection/rule proposal remains in the plan; its API must not be assumed available.

## Authored acceptance source — UNEXECUTED

The retained fixture asserts observable state and events:

1. Missing/destroyed providers with inversion off/on: Evaluate and LastResult agree; a previous provider result is replaced after destruction.
2. At 225 XP on an explicit 100-XP linear curve: level 3 and 75 XP remaining produce the expected inclusive/pass-fail results for all three modes, with both inversion states.
3. Explicit level 5 takes precedence over context level 2 at threshold 4; destruction restores context fallback. An action executed before a sibling exists raises no events, then adding a sibling permits 250 XP, level 3, and one successful level notification.
4. AddXp and SetLevel preserve their own captured result when success observers reset or overwrite the level; an unchanged operation does not invent a level event from a success observer's later mutation.
5. A nested SetLevel produces exactly `success:2, success:3, level:3, level:2`. Downward SetLevel still reports its target, unchanged SetLevel succeeds without a level event, and an unknown action changes no provider state or events.

These are authored assertions, not execution results. No additional duplicate test was added in this phase.

## Dependencies and coordinator handoff

**Pending additive API: none. Source writer complete; no need to resume this owner for another module's API.** Existing Core/component/curve semantics are external dependencies, not copied into the Bridge.

Parent navigation integration belongs to `Neo.Core`: in `Assets/Neoxider/Docs/Core/README.md`, add a canonical link to `./Level/Bridge/README.md`. Its current direct links to both component guides remain valid and those guides now lead to the canonical entry, so this does not block the owned source-final state. This worker did not cross that ownership boundary.

After every writer finishes, the coordinator runs `Neo.Core.Tests.Audit20261023NeoCoreLevelBridgeTests` and the relevant existing level tests on the frozen integrated tree. Existing test references were sufficient at source review; any shared asmdef correction belongs to root coordination. Core XP saturation/initialization regressions belong to their owners.

## Actual work and remaining evidence debt

During this phase only source/document reads, reference enumeration, scoped diffs, metadata generation, and SHA-256 artifact bookkeeping were performed. The documentation members were located in current NeoCondition/ConditionEntry/LevelComponent source. Runtime, regression and historical audit/plan hashes were read again after interruption and remain unchanged from the partial implementation checkpoint.

| Artifact | SHA-256 |
|---|---|
| `LevelConditionAdapter.cs` | `f43110cb256bbd91d4925ad7fae8c69bba44e91d6123e5a07146871141ee1555` |
| `LevelConditionEvaluationMode.cs` | `01756d52f85ee9ce6dba04bd97d9d1c11926f9098ccc2c9102fcd29937bf0543` |
| `LevelNoCodeAction.cs` | `5db1707d3da917547efe731cd618160be44bba43a8ce23ce4401252099ccd762` |
| `LevelNoCodeActionType.cs` | `602de4c0f31935062142b8c5bdcc71820cd84160ade99f29f4db7d861f23e715` |
| `Audit20261023NeoCoreLevelBridgeTests.cs` | `41dd291ed58ddbd371e7c30fb286ff9383ae033bf5397f613f85d42a78a47219` |

**UNEXECUTED:** compilation/import, EditMode/PlayMode tests, native callbacks/deferred destruction fallback, supported-version execution, and package/docs/UTF-8/meta/GUID/product validation. The existing fixture exercises immediate destruction in EditMode source; actual Unity lifecycle and event wiring still need central acceptance. Root schedules frozen-snapshot checks on installed Unity 6.3 and 6.6, representing the 6.0–6.4 and 6.5+ API families. Unsupported branches remain explicitly unverified.

No historical static-check claim is promoted to current integrated verification. No compiler, Unity/MCP, test runner, product checker, service, install, commit/push, or child agent was launched. The execution barrier remains in force until every source writer finishes.
