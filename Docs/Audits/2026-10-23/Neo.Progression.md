# Neo.Progression: audit

Status: source audit and scoped fixes complete; static validation passed. Unity compilation and EditMode execution remain assigned to the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Requested model: `gpt-6.1-sol`, reasoning `max` (manifest).
- Assembly: `Assets/Neoxider/Scripts/Progression/Neo.Progression.asmdef`.
- Scope: all 13 manifest C# files reviewed completely; 2,148 lines after fixes. No nested assembly exists under this source root.
- Changes: 6 owned source files, all 9 existing owned public guides, one new regression fixture plus its unique `.meta`, and this report.
- Execution: file-only local implementation and self-review. The user prohibited Unity/MCP, builds/test runners, installs, shared configuration changes, commits, and child agents; no independent QA or runtime verification was performed.

Кратко: исправлены потеря perk points при переполнении, повторные premium-награды при росте уровня, повторная выдача при назначении того же провайдера, устаревшее состояние после замены провайдера, null-списки профиля и ошибки инициализации/событий мостов. Добавлены точные регрессии; запуск в Unity остаётся за координатором.

## Scope

Paths below are relative to `Assets/Neoxider/Scripts/Progression/`. Line counts refer to the reviewed final source, including comments and whitespace.

| File | Lines | Review coverage/result |
|------|-------|------------------------|
| `Bridge/ProgressionConditionAdapter.cs` | 79 | Explicit/context/singleton resolution, all modes, inversion, destroyed/null references, first-use initialization. Fixed F7/F8. |
| `Bridge/ProgressionConditionEvaluationMode.cs` | 14 | All five switch cases and serialized enum ordering checked; unchanged. |
| `Bridge/ProgressionNoCodeAction.cs` | 182 | All seven actions, clamped accessors, manager fallback, result/success/failure event routing. Fixed F6/F10. |
| `Bridge/ProgressionNoCodeActionType.cs` | 16 | Serialized values match supported switch cases; no reordering or new values. |
| `Components/ProgressionManager.cs` | 876 | Initialization, provider binding/removal, XP/level mutations, reward reentry, perk costs/prerequisites/conditions, defaults, persistence/reset, snapshots, notifications, and warning gating. Fixed F1-F4/F6. |
| `Data/LevelCurveDefinition.cs` | 193 | Authoring sort, null entries, level lookup, legacy XP boundaries, ordering/duplicate validation. Fixed F11. Helpers still assume valid ordered definitions. |
| `Data/PerkTreeDefinition.cs` | 258 | Definition fields, exact ordinal lookup, sorting, duplicate/missing prerequisites, cross-tree unlock validation, cycle traversal, default-reward diagnostics. No source mutation. |
| `Data/ProgressionReward.cs` | 76 | All reward amounts, asset/scene target fields, premium flag, supported dispatcher dependencies. Scene targets inside reusable assets require the architectural follow-up below; fields preserved. |
| `Data/ProgressionRewardType.cs` | 15 | All six serialized values match dispatcher branches; unchanged. |
| `Data/UnlockTreeDefinition.cs` | 206 | Definition fields, exact ordinal lookup, sorting, null/duplicate/missing references, cycle traversal, default-reward diagnostics. No source mutation. |
| `Runtime/ProgressionEvents.cs` | 13 | Serializable string event wrapper and all event consumers; unchanged. |
| `Runtime/ProgressionProfileData.cs` | 99 | Version 2 payload, nonnegative point setter, list sanitation, null collections, independent cloning. Fixed F5. |
| `Runtime/ProgressionRewardDispatcher.cs` | 121 | All reward kinds, premium selection, null entries/manager, existing service fallbacks, missing dependency diagnostics. Fixed F9. |

Read-only context: `Neo.Progression.asmdef`, `Neo.Editor.Tests.asmdef`, existing Progression tests, Core `LevelComponent`/relevant `LevelModel` mutations, `Singleton<T>`, `ConditionEntry`, `SaveProvider`, and `ISaveProvider`. No dependent assembly, existing test, shared configuration, scene, prefab, sample, or package manifest was changed by this worker. Concurrent changes to `ProgressionManagerTests.cs` belong to another worker and were left intact.

Reuse decision: consulted the NeoxiderTools module/game-system/idiom/extension references and local source. Reused the current Core level component, existing profile and reward dispatcher, `NeoDiagnostics`, SaveProvider, reactive properties, and UnityEvents. No second progression framework or utility service was introduced. Saturation uses existing `System.Math` inline; the extension catalog supplies no saturating integer addition helper. `TryAddXp` follows the manager's existing `TrySetLevel` failure-reason pattern.

## Findings and fixes

The pre-fix outcomes below follow directly from the reviewed source and exact inputs; they are not claimed as executed Unity reproductions. The new EditMode tests encode these observable outcomes for central execution.

| ID / severity | Trigger and observable pre-fix consequence | Completed fix / final source |
|---------------|-------------------------------------------|------------------------------|
| F1 / High | Grant 10 points at balance `int.MaxValue - 1`, directly or through `GrantedPerkPoints`. Integer addition wraps negative and the profile setter clamps the balance to **0**, destroying existing points. | Widen addition to `long`, cap at `int.MaxValue`, then assign. `Components/ProgressionManager.cs:335`, `:793`. |
| F2 / High | At level 2, activate premium with a premium XP reward that reaches level 3. Normal level dispatch grants level 3 rewards, then the old replay loop reads the increased live level and grants its premium rewards again. The regression's expected 12 points became 19. A save callback advancing the level similarly produced 17 instead of 12. | Capture the achieved level before setting premium or invoking save callbacks; replay only that fixed boundary. A `long` loop counter also avoids signed counter wrap at `int.MaxValue`. `Components/ProgressionManager.cs:584`. |
| F3 / High | Reward levels 2/3 for 5 points, lower to level 1, assign the same `LevelProvider`, then restore level 3. The setter reset the watermark and added another listener, enabling another 5 points. | Same-reference assignments return without resetting the watermark or adding listeners. Reference equality still permits clearing a destroyed Unity provider. `Components/ProgressionManager.cs:75`. |
| F4 / Medium | Replace a provider after publishing 50 XP with another already at 100 XP, or clear it. Live getters changed, while `XpState`, `LevelState`, and `XpToNextLevelState` retained the previous values. | Refresh published state after actual provider changes; retain old-listener removal and incoming-level baseline behavior. Covers replacement, null, and destroyed-provider cleanup. `Components/ProgressionManager.cs:98`. |
| F5 / Medium | A serialized/migrated profile contains a null identifier list. `Sanitize()` left it null; manager import dereferenced `Count`, and `Clone()` constructed a list from null. | Repair both lists during sanitation; clone null lists into independent empty lists. Preserve valid identifiers, entitlement, balance, and version. `Runtime/ProgressionProfileData.cs:59`, `:71`. |
| F6 / Medium | Execute NoCode `AddXp` without a level provider, or with zero XP. The manager did nothing, while the action invoked `OnSuccess` and reported XP added. | New backward-compatible `TryAddXp(int, out string)` returns a reason; the void API delegates to it, and NoCode forwards success/failure correctly. `Components/ProgressionManager.cs:275`; `Bridge/ProgressionNoCodeAction.cs:103`. |
| F7 / Medium | An adapter targets an explicit, uninitialized manager whose persisted profile has 7 points. Numeric mode read the default 0 without loading it, unlike identifier-based modes. | Ensure manager initialization before all query modes. `Bridge/ProgressionConditionAdapter.cs:41`. |
| F8 / Low | No manager, `Invert = true`: `Evaluate()` returned true but `LastResult` remained false. | Store the actual inverted result on the missing-manager branch. `Bridge/ProgressionConditionAdapter.cs:37`. |
| F9 / Medium | Call public `DispatchRewards` as the first operation on an explicit manager with persisted premium entitlement. Filtering saw the uninitialized false flag and silently skipped its premium reward. | Initialize the manager before entitlement filtering. `Runtime/ProgressionRewardDispatcher.cs:25`. |
| F10 / Low | Execute an unsupported serialized action enum value. No success, failure, or unified result event was emitted, leaving an Inspector flow without a result. | Add an explicit failure branch. `Bridge/ProgressionNoCodeAction.cs:152`. |
| F11 / Medium | Legacy `GetXpToNextLevel` receives negative XP with entries at 0/100 XP. `-1` returned 1 instead of 100, and `int.MinValue` overflowed the subtraction to a negative result. | Normalize negative total XP to zero before threshold selection/subtraction. `Data/LevelCurveDefinition.cs:129`. |

Compatibility: all existing public methods remain; `TryAddXp` is additive. All serialized field sets, profile version/save key, enum ordering, and existing Unity asset metadata remain intact. Intentional behavior changes are documented: saturated point grants, correct NoCode failures, fixed replay boundaries, and immediate provider-swap notifications. No new logs were added; current diagnostics remain gated and the missing-provider warning remains once per manager.

## Regression coverage

New file: `Assets/Neoxider/Tests/Edit/Progression/Audit20261023NeoProgressionRegressionTests.cs` (16 test methods, 22 parameterized scenarios), plus `.cs.meta` GUID `958b28e72e0646a5b93757331f778ca6`.

| Covered contract | Concrete assertions |
|------------------|---------------------|
| Direct and level point overflow | Exact maximum balance, reactive value, UnityEvent payload, and snapshot value. |
| Premium XP reentry and save-callback reentry | Exact XP/level/point totals; new-level premium/free/base rewards occur once; repeated activation does not add rewards. |
| Same provider reassignment after downgrade | Exact original 5 points remain after reaching the previously claimed level again. |
| Replacement/null/destroyed provider | Exact cached XP/level/remaining XP and point totals; old-provider changes cannot award rewards. |
| Null profile lists and independent clone | Repaired collections, preserved valid IDs/balance/premium, and mutations of the clone do not alter the original. |
| Negative legacy XP | Exact next-level distance of 100 at `-1` and `int.MinValue`, with level 1 preserved. |
| Condition and reward-dispatch first use | Persisted numeric state and entitlement are actually loaded before querying/filtering. |
| Missing-manager inversion | Both return value and `LastResult` agree for each inversion setting. |
| NoCode failure/success/unsupported action | Exact success/failure/result routing, real XP/profile output, and absence of save operations for rejected commands. |
| Typed XP rejection | Nonpositive requests return a reason and preserve the existing 20 XP and published state. |

The fixture uses a private in-memory `ISaveProvider`, inactive owned objects, a linear Core XP curve, and exact existing serialized field names for authoring configuration. It restores the prior SaveProvider and singleton state in teardown and is nonparallel. Reflection configures fixtures and isolates static fallback; assertions observe public state/events rather than private implementation. Existing test asmdef references already cover every dependency; no reference changes are requested.

## Documentation

Updated existing English entry points and guides only, retaining their metadata:

- `Assets/Neoxider/Docs/Progression/README.md`: Core XP curve versus Progression reward track, separate save ownership, optional trees/provider-free use, typed failure path, links, and missing Unity imports in examples.
- `ProgressionManager.md`: actual ownership, public contracts, provider swap behavior, saturated points, premium replay boundary, profile switching/reset behavior, and direct-provider notification limits.
- `ProgressionNoCodeAction.md`: correct XP failure events, typed C# equivalent, unsupported actions, unified result routing.
- `ProgressionConditionAdapter.md`: removed duplicated content; documented manager resolution/initialization and consistent inverted result.
- `Data/LevelCurveDefinition.md`: valid ordering assumptions, normalized negative XP, reward ownership and premium replay behavior.
- `Data/PerkTreeDefinition.md` and `Data/UnlockTreeDefinition.md`: replaced placeholder component instructions with actual asset menus, fields, validation APIs, C# usage, scene/NoCode equivalents, and default reward rules.
- `InternalTypes.md`: corrected nonexistent enum values/type names and incorrect profile contents; documented clone/sanitation, low-level reward dispatch, entitlement filtering, and service fallback contracts.
- `Scenarios.md`: corrected minimal Core XP/provider requirements and provider-free tree setups.

## Substantial improvement proposal

### Proposal: reusable ProgressionSession with compiled definitions and explicit reward delivery

**Not implemented.** This is architectural follow-up, separate from the focused fixes above.

**Problem and source evidence:** `ProgressionManager` currently combines rules, profile mutation, persistence, singleton lifetime, Unity notifications, provider events, and cross-module reward delivery in 876 lines. Reusing the rule engine for two independent profiles, deterministic C# simulation, or a server requires scene components. `ProgressionReward` stores concrete scene service references in data that is embedded in ScriptableObject trees/tracks. `ActivatePremium` saves entitlement before external rewards; unlock/purchase state is recorded even when dispatch only warns about a missing target. The level watermark is not serialized. `DispatchRewardsForLevel` and premium replay also enumerate every integer level and repeatedly scan track entries, which is costly for sparse/high-level catalogs.

**Proposed design/API:**

1. Build an immutable `ProgressionCatalog` from the existing authoring assets through a Unity adapter. Compile ordinal ID maps and sorted reward-level indexes once; reject duplicate/empty IDs, invalid costs/levels, and cycles with structured diagnostics. This reuses current definitions and validation concepts instead of creating another authoring system.
2. Introduce a plain C# `ProgressionSession` owning profile state and operations such as `CanUnlockNode`, `TryUnlockNode`, `TryBuyPerk`, `ApplyLevel`, `ActivatePremium`, and `GetSnapshot`. Return a typed `ProgressionOperationResult` containing a failure code, resulting state changes, and reward commands. Resolve external requirements through supplied callbacks/adapters around the existing condition APIs; do not embed GameObjects in the session or introduce another condition engine.
3. Keep `ProgressionManager` as the backward-compatible scene adapter for Core level events, SaveProvider, UnityEvents/reactive properties, and optional singleton convenience. Allow normal C# callers to create multiple sessions without managers. Queue reward-induced state transitions so reentrant XP rewards cannot create a deep recursive call chain.
4. Add an explicit `IProgressionRewardSink.TryApply(ProgressionRewardCommand, out ProgressionRewardFailure)` seam. Commands carry stable reward/resource/claim identifiers; scene adapters resolve Money, Collection, and Quest dependencies with serialized scene references. This moves target resolution out of reusable ScriptableObjects and makes missing-service behavior observable.
5. As a second phase, persist a versioned claim/pending-delivery ledger and require idempotent sink handling by claim ID. Use indexed reward-level ranges rather than iterating every integer. This permits retries and prevents a reload/downgrade or partially delivered premium replay from silently losing/regranting rewards. Cross-module atomicity requires the sink/Save coordinator contract; a single JSON write alone cannot provide it.

**Compatibility/migration:** retain existing serialized names, enums, public manager methods, and definition asset GUIDs. Phase one can adapt Version 2 profiles unchanged and preserve the current baseline rule for the provider's incoming level. Existing direct target fields remain readable through a legacy adapter; new catalogs use asset/resource identifiers and scene bindings. Introduce an explicit Version 3 migration only with the ledger phase; migrate existing entitlement/points/IDs and mark prior levels as historical baseline rather than granting them again. Provide a validation/migration preview; no automatic sample/prefab rewrite or forced scene reference removal.

**Acceptance scenarios:**

- Two sessions share one catalog but keep independent points, unlocks, requirements, and premium claims; no Unity scene is needed for those tests.
- Replay and reward-produced XP produce exactly the same results regardless of callback order, save callbacks, or splitting an XP grant into several operations.
- A sparse catalog with reward levels 2 and 1,000,000 processes only relevant entries; runtime work scales with entries reached rather than the numeric level gap.
- Version 2 profile fixtures migrate without duplicate rewards or changes to points, identifiers, entitlement, or existing manager consumers.
- Missing reward services return a typed failure; a persisted pending command can be retried once the scene service exists, with claim IDs preventing duplication across reloads.
- C# and NoCode entry paths return identical failure reasons/results, and existing UnityEvents/reactive properties reflect the final committed state.

**Effort/risk:** estimated 4-6 developer days for catalog/session extraction and compatibility coverage, plus 3-5 days for ledger/sink migration, assuming current Core/Save APIs remain available. Risk is moderate for extraction and high for durable reward delivery because callback ordering, old profile semantics, external economy idempotency, and migration policy must be agreed and integration-tested. Coordinate with Core, Save, Condition, Quest, Shop/Bonus, and Editor owners before implementing cross-assembly contracts. Do not fold this redesign into the present bug-fix batch.

## Verification and remaining risks

Actual checks executed:

| Check | Actual result |
|-------|---------------|
| Manifest inventory compared with current `.cs` files under the assembly root | Exact match: 13/13, all fully reviewed; no nested asmdef. |
| Python static C# delimiter checks after stripping comments/strings; explicit-type scan | Passed for all 13 owned files plus the new fixture (14 files). This is lexical consistency, not C# compilation. |
| Serialized field-name set comparison against `git show HEAD:<owned file>` | Passed for all 13 files; source diff also manually reviewed. |
| Strict UTF-8 reads, replacement/control marker scan, final-newline/whitespace checks | Passed for the owned source, 9 guides, new fixture, and test metadata. An initial check found an existing whitespace-only line in the edited manager guide; it was cleaned and the check rerun successfully. |
| Owned Markdown relative-link resolution | Passed: 23 links across 9 guides. |
| New test names/case inventory and existing asmdef dependency inspection | 16 methods / 22 scenarios; all required test references already present. |
| `rg --crlf -l '^guid: 958b28e72e0646a5b93757331f778ca6$' Assets -g '*.meta'` | Exit 0, exactly the new test metadata file found. |
| `git diff --check -- <six owned changed C# files> Assets/Neoxider/Docs/Progression` | Exit 0, no output. |
| `git diff --no-index --check -- NUL <new regression fixture>` | No whitespace diagnostics; exit 1 denotes the expected new-file difference. The independent Python whitespace check also passed. |

Final handoff checks also passed for the report, fixture, and metadata: strict UTF-8 and whitespace inspection, scoped `git diff --check` exit 0 over all owned paths, and three new-file `git diff --no-index --check` comparisons with no diagnostics (expected difference exits). The Russian summary was verified from UTF-8 content. No Unity compilation, EditMode/PlayMode test runner, build, install, commit, or shared configuration mutation was executed. Static checks do not establish that the 22 Unity scenarios pass.

Remaining limitations and coordinator actions:

- Centrally compile `Neo.Progression` against the concurrently audited Core/Managers/Save dependencies, then run `Neo.Progression.Tests.Audit20261023NeoProgressionRegressionTests` (22 cases) and the existing Progression suite. No asmdef changes are required by this worker.
- Validate scene wiring and reactive UI behavior in Unity, especially provider replacement and NoCode XP failures. This worker intentionally did not access the Editor.
- Direct Core provider mutations without a new rewarded level, including XP-only changes, level decreases, reset, and loading, are not fully mirrored into the manager's cached reactive values; documented use is manager mutations or direct binding to Core reactive state. Complete notification synchronization belongs with the adapter/session follow-up.
- Progression and Core profiles save independently. Reward services can fail after unlock/purchase state or entitlement is persisted; there is no durable claim ledger or automatic retry/rollback. The proposal describes the required cross-boundary work rather than claiming it fixed.
- Definition validation is advisory and legacy XP helpers assume ordered, valid authored assets. Target references embedded in reusable assets and synchronous sparse/high-level reward iteration remain architectural limitations; no serialized schema change was made for them.
- No Unity 6.0-6.5 branch was compiled. The changes add no version-dependent Unity object/ID API and preserve current assembly boundaries, but compatibility must be established centrally.
