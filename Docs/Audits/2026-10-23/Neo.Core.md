# Neo.Core: audit

Status: source audit complete; fixes and regressions authored. Central compilation and EditMode execution remain pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Core/Neo.Core.asmdef`.
- Scope: 16 C# files; nested assemblies excluded.

## Scope and complete review inventory

Read `AGENTS.md`, the complete worker contract, the exact manifest entry, and this report before changes.
All 16 manifest C# files were read in full. Paths in this table are relative to `Assets/Neoxider/Scripts/Core/`.

| File | Review and disposition |
| --- | --- |
| `Level/Domain/LevelCurveEvaluator.cs` | Reviewed every legacy/formula/custom branch, inverse thresholds, rounding, caps, null entries, finite inputs, integer limits. Fixed numerical and next-level defects below. |
| `Level/Domain/LevelCurveMode.cs` | Reviewed serialized enum values and authoring meanings; unchanged. |
| `Level/Domain/LevelCurveType.cs` | Reviewed enum compatibility; corrected threshold XML descriptions without changing values. |
| `Level/Domain/LevelFormulaType.cs` | Reviewed all six formula contracts; corrected `(level - 1)` XML descriptions, preserving values. |
| `Level/Domain/LevelModel.cs` | Reviewed initial state, XP/direct modes, restoration, caps, definitions, level inversion, and event data. Fixed initial/mode state, overflow, and search bounds. |
| `Level/Interfaces/ILevelCurveDefinition.cs` | Reviewed Unity-free dependency boundary; documented the nondecreasing evaluation contract needed by inverse search. Signatures unchanged. |
| `Level/Interfaces/ILevelCurveEntry.cs` | Reviewed read-only table contract, repeated levels, null entries, and custom thresholds; unchanged. |
| `Level/Interfaces/ILevelProvider.cs` | Reviewed programmer/NoCode provider contract against model and wrapper usage; unchanged. |
| `Level/LevelProfileData.cs` | Reviewed serialized names/version, nonnegative XP/cap, minimum level, cap sanitation, and independent clone. No demonstrated defect requiring change. |
| `Resources/Domain/ResourcePoolEntry.cs` | Reviewed every option, zero versus unlimited limits, mutable alias ownership, regen/heal settings. No fields changed; alias risk addressed by proposal. |
| `Resources/Domain/ResourcePoolModel.cs` | Reviewed every mutation/query, case-insensitive ids, exact spending, finite values, depletion/heal gates, timer accumulation, callback mutation/reentrancy. Fixed defects below. |
| `Resources/Interfaces/IResourcePoolProvider.cs` | Reviewed exact spending and actual-delta contracts; corrected model behavior without changing signatures. |
| `Resources/ResourceEntryData.cs` | Reviewed serialized defaults, null-id fallback, max setter, and profile sanitation boundary. Unchanged. |
| `Resources/ResourceProfileData.cs` | Reviewed null collection/entries, invalid ids, finite/bounded state, uncapped resources, and deep copies. Fixed sanitation and cloning. |
| `Resources/RpgResourceId.cs` | Reviewed constant identifiers and case-insensitive lookup compatibility; unchanged. |
| `Resources/UnityEventFloatFloat.cs` | Reviewed serializable UnityEvent specialization and parameter types; unchanged. |

Nearest-asmdef validation confirms all inventory files belong to `Neo.Core`. Nested Level Data,
Components, Bridge, and Resources Components assemblies and their documentation topics were not edited.
Read relevant portions of `LevelComponent`, `LevelCurveDefinition`, and `HealthComponent` as dependency
context, plus existing `ResourcePoolModelTests`, level curve regression coverage, the shared test asmdef,
and the package text validation contract. No MonoBehaviour/editor/NoCode implementation is owned here.

Reuse check: read the `neoxider-tools` skill and matching Core/level/numeric references. `Neo.Core` has
no package assembly dependencies; the existing models were adapted with BCL arithmetic and collections.
No replacement system, Unity lookup, external library, shared service, or new package dependency was added.

## Findings and completed fixes

Evidence below is source tracing and independent arithmetic, not a claim of executed Unity behavior.
The new public-behavior tests encode each reproduction for the central runner.

### F1 — High: integer XP overflow and incomplete inverse search

Source: `Level/Domain/LevelModel.cs`, `AddXp` and `FindMinimumXpForLevel`.

- Trigger: total XP `int.MaxValue - 5`, then `AddXp(10)`. Unchecked addition previously produced negative
  XP and dropped the default linear level to 1. Now XP saturates, the event reports 5 actually stored,
  and subsequent additions at the ceiling produce no XP event.
- Trigger: linear XP step 1,000,000,000; request level 3 from a fresh model. The old doubling search
  stopped at 1,073,741,824, called level 3 unreachable, and selected level 2 / 1,000,000,000 XP.
  Now the search reaches the full integer domain and selects level 3 / 2,000,000,000 XP. Custom tables
  whose highest thresholds exceed half the integer domain also resolve correctly.
- Regression: `AddXp_AtIntegerLimit_ReportsOnlyStoredXpWithoutWrapping`, `SetLevel_SearchesTheEntireXpRange`,
  and `SetLevel_UnreachableCustomLevel_FindsHighestThresholdAboveHalfIntegerRange`.

### F2 — High: evaluator overflow, precision loss, and inconsistent next-level thresholds

Source: `Level/Domain/LevelCurveEvaluator.cs`, both evaluation families, required-XP calculation, and both next-level methods.

- Linear evaluation at `int.MaxValue` XP with a 1-XP step previously wrapped `1 + quotient` and resolved
  level 1, even with a configured cap. Power formulas could overflow the same conversion path. Levels
  now saturate, caps still apply, and the next-level query cannot overflow `currentLevel + 1`.
- Quadratic level 46,341 with base 1 requires exactly 2,147,395,600 XP. Former float intermediates rounded
  the threshold to 2,147,395,584 and could resolve the wrong level. Division, multiplication, and offset
  subtraction now use double intermediates. Inverse quadratic/exponential/power estimates verify adjacent
  actual thresholds to avoid rounding across a level boundary.
- At `int.MaxValue - 10` XP with the default quadratic formula, next XP is 838,863. The former implementation
  narrowed the absolute next threshold (2,148,322,500) to `int` before subtraction, outside the integer
  range. Now subtraction precedes conversion and out-of-range remaining gaps saturate.
- Legacy quadratic/exponential next-level queries used raw parameters while evaluation clamped them.
  With base zero, they reported zero XP to the next level instead of 1. Both APIs now share parameter
  normalization. Non-finite formula inputs use documented minima (offset uses zero); negative total XP
  is treated as zero.
- Custom tables previously counted the next future row even when it repeated the already reached level.
  For `(2,100), (2,200), (3,400)` at 100 XP, the old answer was 100 despite no level-up at 200. It is now 300.
- Regression: integer-limit linear/power cases, large exact quadratic threshold and offset cases,
  `XpToNextLevel_SubtractsBeforeConvertingLargeThresholdToInteger`, minimum-parameter cases, and
  `CustomNextLevel_SkipsEntriesThatCannotIncreaseTheLevel` in the new level test file.

### F3 — Medium: incoherent initial progress and mode switching

Source: `Level/Domain/LevelModel.cs`, initial `XpToNextLevel` and `SetUseXp`.

A fresh model previously exposed zero next XP despite having a default 100-XP linear curve. Disabling XP
left a stale next-level counter; reenabling it after setting a direct level retained the direct level
instead of resolving stored XP. Initial progress now matches the default curve, disabling clears the
counter, and enabling immediately recomputes. State restoration/configuration event semantics were preserved.
Regressions: `NewModel_ExposesDefaultLinearProgress`, `SetUseXp_UpdatesProgressWhenSwitchingModes`.

### F4 — High: successful spending after a partial payment

Source: `Resources/Domain/ResourcePoolModel.cs`, `TrySpend`.

With current Mana 20, decrease limit 4, and cost 10, the method previously returned true after deducting
only 4. Consumers could grant a skill while underpaying. A positive spend now either deducts the full
cost or fails without mutation/events; the configured decrease limit is honored. Finite nonpositive
costs retain their existing successful no-op behavior. Regressions cover limits 0 and 4, case-insensitive
lookup, unchanged failure state/events, and a successful cost within the limit.

### F5 — Medium: long-frame regen/heal losses and timer corruption

Source: `Resources/Domain/ResourcePoolModel.cs`, `Tick`, `ClampIncrease`, `ApplyIncrease`.

- Current 10, regen 10/sec at 1-sec intervals, heal 2 every second, increase limit 3: `Tick(3.5)` formerly
  reached 16, while three 1-second ticks plus 0.5 reached 25. Catch-up multiplied the amount before
  applying one per-operation cap. Now each elapsed cycle receives its configured cap before aggregation,
  so both paths reach 25 and the next 0.5 reaches 30. Event delivery stays aggregated rather than looping
  over missed cycles.
- A finite interval of 1e-9 and a 3-second delta produces more than `int.MaxValue` cycles; an integer cast
  previously corrupted cycle count/remainder. Double cycle counts and modulo remainder preserve the
  expected 3 units of regen without an unbounded loop.
- Negative/non-finite delta times previously consumed or poisoned future timer progress. They are ignored.
- Regressions: `Tick_LongFrame_AppliesIncreaseLimitPerScheduledCycle`,
  `Tick_MoreThanIntegerMaxCycles_StillAppliesElapsedRegen`, and parameterized invalid-delta cases.

### F6 — Medium: stale pool state and repeated ticking from synchronous callbacks

Source: `Resources/Domain/ResourcePoolModel.cs`, `Tick` snapshot and reentrancy handling.

A regen listener can remove HP and add a different entry with the same id. The old tick then used the
removed entry's heal settings on the replacement (7 becomes 9 in the regression instead of staying 7).
Nested `Tick` calls also reused the active buffer and advanced the same timer twice. The snapshot now
stores entry identities and checks them before each stage; removed/replaced entries stop processing,
and added/replaced pools begin on the next tick. Recursive calls are ignored, with `finally` releasing
the guard and snapshot references even if a listener throws. Regressions assert replacement state and
one tick's exact scheduled total under a recursive callback.

### F7 — Medium: healing returns include unrelated listener mutations

Source: `Resources/Domain/ResourcePoolModel.cs`, `Increase` / `ApplyIncrease`.

Starting at 50, healing 20, then spending 5 in `OnResourceChanged` formerly returned 15 as the heal amount.
The operation now captures its own delta before notifying listeners: returned heal is 20, final pool is
65. Regression: `Increase_ReturnsItsOwnDeltaWhenAListenerSpendsResource`.

### F8 — Medium: invalid public inputs poison state or throw unexpectedly

Source: `Resources/Domain/ResourcePoolModel.cs`, queries, mutations, and `AddPool`.

Null ids reached `Dictionary` APIs although registration already rejected them. NaN spending bypassed
all comparisons, returned success, and changed current to NaN. Other non-finite operations could poison
uncapped state. Negative initial current could make `Decrease` return a negative damage amount.
Missing/null queries now follow unknown-resource results; invalid mutations are ignored/rejected;
registration normalizes finite current/max bounds. Regressions assert meaningful return values,
failure reasons, preserved state, initial bounds, and event counts for null/empty/non-finite cases.

### F9 — Medium: uncapped resource state is erased during max changes and persistence

Source: `Resources/Domain/ResourcePoolModel.cs`, `SetMax`; `Resources/ResourceProfileData.cs`, `Sanitize`.

Existing increase/set-current logic explicitly treats nonpositive Max as uncapped, but `SetMax(0)`
previously erased current and profile sanitation clamped any saved uncapped value to zero. Both now
clamp only against a positive maximum. Sanitation also repairs non-finite saved values. `Restore` still
assigns Max (zero on an uncapped pool), preserving its explicit fill-to-maximum behavior. Regressions
verify a live max change and the profile-to-model restoration path.

### F10 — Medium: null entries prevent a public resource profile clone

Source: `Resources/ResourceProfileData.cs`, `Clone`.

The publicly mutable Entries list can contain nulls, and deserialization can supply a null list.
Cloning previously dereferenced either. Clones now skip null entries, return an empty independent
profile for a null list, and retain valid entries without changing the source. The regression inserts
a null through the public list and proves the valid entry remains deeply independent. The repaired
method also replaces the inventory's existing `var` declaration with an explicit type.

## Documentation and compatibility

- Updated `Assets/Neoxider/Docs/Core/README.md` as the canonical Core entry point: plain C# examples,
  optional assembly boundaries, model mode/state/event behavior, inverse search and integer limits,
  exact costs versus partial damage/healing, timer/callback policy, uncapped/finite resource rules,
  DTO sanitation/cloning, and links to scene/NoCode workflows. Removed the self-referential notes section
  and corrected the level/XP entry link.
- Updated `Assets/Neoxider/Docs/Core/LevelCurveDefinition.md` with shared evaluator normalization,
  thresholds/integer limits, a runtime API link, and an explicitly typed example.
- Updated owned formula enum and curve-interface XML contracts. Serialized fields, enum values, class
  names, public signatures, and DTO layouts remain unchanged. No migration assets/configuration needed.
- Intended behavior changes: full-cost failures under decrease limits, XP saturation and actual-added
  event values, immediate XP mode synchronization, consistent uncapped sanitation, and per-cycle timer
  caps. Callers depending on the defective partial-cost or time-loss behavior will observe corrected values.

Кратко: проверены все 16 исходников; исправлены переполнение XP, поиск больших порогов, ошибки списания,
таймеров и сохранения ресурсов. Документация описывает C# API и сценовые обёртки. Unity не запускалась.

## Substantial improvement proposal — model-owned resource state and explicit change records

**Not implemented in this audit.**

Problem: `ResourcePoolEntry` combines mutable current state, shared configuration, timer settings,
and wrapper metadata. `AddPool` retains the caller's reference. External writes can bypass every
invariant/event gate or inject NaN after registration; sharing one entry across two models also shares
current state. Synchronous event listeners can mutate state while an earlier operation's notification
is still being dispatched. Local guards fix the demonstrated timer bugs but do not define ownership
or a consistent event transaction contract for a reusable library.

Proposed architecture/API:

1. Add a Unity-free immutable `ResourcePoolSettings` value (max, per-operation limits, regen/heal policy)
   and keep current/timers in private model-owned state. Authoring components/SOs produce settings;
   they retain no scene-object references in shared data assets.
2. Add an opt-in `TryAddPool(string id, ResourcePoolSettings settings, float initialCurrent)` overload,
   `TryUpdateSettings`, and `TryGetSnapshot(string id, out ResourcePoolSnapshot snapshot)`. Snapshots
   expose read-only current/max and do not leak mutable model storage.
3. Add typed `ResourceSpendResult` / failure reasons and an immutable `ResourceChange` record containing
   id, previous/current/max, actual delta, and cause (spend, damage, heal, regen, restore, settings/load).
   Keep `TrySpend(..., out string)` as a compatibility adapter. Validate, commit once, capture the change,
   then dispatch records in order; callback-generated records queue behind the current dispatch.
4. Preserve the legacy `AddPool(string, ResourcePoolEntry)` path and its alias behavior during a transition.
   New code opts into owned state explicitly. Component owners can migrate their internal model setup
   without renaming serialized inspector fields; legacy methods/events remain adapters. Any final removal
   of mutable alias semantics requires a major version, not a silent copy in this patch.

Acceptance scenarios:

- Mutating a settings source or a legacy entry after using the new overload cannot alter owned current
  state; two models using the same settings remain independent.
- Invalid finite/bounds inputs receive a typed rejection and never produce NaN snapshots; zero maximum
  retains the documented uncapped semantics.
- Failed spending changes no state and emits no committed change; successful spending records exactly
  one complete cost. Existing string-based consumers observe the same result.
- Listeners that spend, remove, replace, or register a pool observe stable before/after records in order;
  old timers never affect replacements and recursive notifications do not corrupt iteration.
- Long-frame regen/heal and direct operations keep current regression outputs. Existing scene/NoCode
  data, saves, and public wrapper calls continue working without prefab migration.

Effort/risk: approximately 4–6 developer days including focused edit-mode coverage and wrapper adapters;
medium compatibility risk around mutable aliases and event ordering. Coordinate Resources Components
and RPG consumers before migrating wrappers. No dependency or assembly split is required for the new
domain contracts. Separately, a future wide-XP API would address progression beyond Int32; the current
audit deliberately preserves integer public contracts.

## Regression artifacts and actual validation

New files under `Assets/Neoxider/Tests/Edit/Core/`, each with its own unique GUID `.meta`:

- `Audit20261023NeoCoreLevelTests.cs`: level state, integer search/saturation, thresholds, custom rows.
- `Audit20261023NeoCoreResourceTests.cs`: exact costs, invalid ids/numbers, caps, callback results,
  catch-up, large cycle counts, replacement/reentrant ticking.
- `Audit20261023NeoCoreProfileTests.cs`: uncapped restoration, invalid data, independent cloning.

Exactly **27 methods / 34 NUnit cases** authored in these three files. All assert state/output/events,
not source text or merely absence of exceptions. Existing shared `Neo.Editor.Tests.asmdef` already
references `Neo.Core`; no reference edit is needed. Tests have **not been executed**.

Executed checks:

- Static nearest-asmdef/inventory check: all 16 files resolve to the assigned assembly.
- Baseline/current serialized-field declaration comparison for all 16 files: unchanged.
- Baseline/current public method signature comparison for all 16 files: unchanged.
- Strict UTF-8, replacement-character and the existing package test's 21 mojibake-marker checks:
  passed for owned sources, new tests, and changed public docs. No encoding repairs were inferred from
  console display.
- Static explicit-type and balanced-delimiter scan: passed for all 16 owned C# files and the three new
  regression files. This is a lexical check, not a C# compiler.
- Changed public Markdown link resolution and new test metadata GUID uniqueness against package
  metadata: passed. New test/meta files also checked for trailing whitespace.
- Independent arithmetic probes (Python, not execution of C#): exact quadratic threshold
  2,147,395,600 versus former float product 2,147,395,584; next quadratic threshold 2,148,322,500 and gap
  838,863; former search upper bound 1,073,741,824 versus required 2,000,000,000.
- Scoped `git diff --check -- <inventory, owned docs, own report, three tests and metadata>`: exit 0.
  Git emits an existing LF-to-CRLF normalization advisory for `LevelModel.cs`; no whitespace error.
- Manual source/diff review: no public signature or enum value change, raw runtime logs, UnityEditor
  imports, scene searches, or object identity/version-switch APIs added. Domain code remains Unity-free.

## Remaining risks and coordinator actions

- Compile/import, Unity console, EditMode/PlayMode execution, and builds were intentionally not run under
  the worker contract. Central validation must compile the package and run the 34 new cases plus existing
  `ResourcePoolModelTests` / level curve tests. Unity 6.0–6.4 and 6.5+ compatibility remains unverified by
  compilation here; owned files contain no version-specific Object identity calls.
- **Cross-boundary reconciliation:** initial dependency inspection found saved state applied under an
  old cap in `LevelComponent.Load`, and raw/unaccepted max values forwarded by `HealthComponent.SetMax`.
  Final source inspection shows concurrent owners have addressed both: `RestoreModelState` applies the
  incoming cap before state, and `OnChangeMax` receives `_model.GetMax(resourceId)`. These are other
  workers' changes, not fixes claimed by this audit. Central integration tests should cover those paths;
  no additional out-of-scope source edit is requested here.
- `ResourcePoolEntry` remains externally mutable and models remain single-threaded. Mutating retained
  entries directly can bypass invariants/events; changing that compatibility contract belongs to the
  proposal. Event exceptions still propagate synchronously; only internal tick cleanup is guaranteed.
- Custom `ILevelCurveDefinition` implementations must obey the documented monotonic/cap contract.
  Int32 XP saturation prevents progression to thresholds beyond that storage domain.
- No commits, installs, child agents, Unity/MCP, runners, shared configuration, nested source/docs,
  scenes, prefabs, or sample layout changes were made.
