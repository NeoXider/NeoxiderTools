# Neo.Condition: audit

Status: full source review complete; focused fixes, regression sources, and public documentation prepared. Unity compilation and test execution remain centrally pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Condition/Neo.Condition.asmdef`.
- Scope: all 6 manifest C# files reviewed in full (1,774 original lines); nested assemblies excluded. No nested assembly is present in this source directory.

## Scope

- `Assets/Neoxider/Scripts/Condition/BindingSourceGameObjectResolver.cs`
- `Assets/Neoxider/Scripts/Condition/ConditionEntry.cs`
- `Assets/Neoxider/Scripts/Condition/ConditionValueSource.cs`
- `Assets/Neoxider/Scripts/Condition/IConditionEvaluator.cs`
- `Assets/Neoxider/Scripts/Condition/NeoCondition.cs`
- `Assets/Neoxider/Scripts/Condition/ReflectionCache.cs`

### Review coverage

| File | Review performed |
|---|---|
| `BindingSourceGameObjectResolver.cs` (110 original lines) | All source-precedence paths, destroyed Unity-object sentinels, successful/missing name caches, warning suppression, unscaled retry timing, invalidation, and reuse by NoCode bindings. No functional edit required. |
| `ConditionEntry.cs` (749) | Every serialized field/public setter; constant/other-object evaluation; source binding; read/conversion exceptions; inversion; all comparison operators; supported method signatures; diagnostic formatting. Fixed generic-method selection and destroyed other-source rebinding. |
| `ConditionValueSource.cs` (351) | Both source branches, fallback-context changes, component/GameObject cache lifetimes, destroyed/replaced targets, reflection and argument allocation, retry/logging flags, member selection, and invalidation. Fixed context retention and method argument conversion. |
| `IConditionEvaluator.cs` (18) | Complete public contract and callers' context expectations. No signature change. |
| `NeoCondition.cs` (479) | Full startup/enable/disable/interval/every-frame lifecycle; safe entry evaluation; AND/OR short-circuiting and null entries; last-result/event behavior; all Mirror command/RPC/late-join paths. Fixed command ingress and client revalidation routing. |
| `ReflectionCache.cs` (67) | All three caches, null-hit behavior, locking, method resolution, caller flags, and metadata ownership. Fixed incomplete cache keys. |

Read-only integration context: existing `NeoConditionTests` and `NeoConditionThrottleTests`, both relevant asmdefs, `NeoDiagnostics`, `NeoNetworkComponent`, `ConditionEntryPredicate`, Condition editor/member-picker sections, and local Mirror weaver naming/body-generation code. No dependency, existing test, or editor source was edited.

## Findings and fixes

The reproductions below are code-level evidence and new regression scenarios, not claims that Unity reproduced or passed them during this worker run.

| ID / severity | Trigger and observable consequence before the fix | Source and completed fix |
|---|---|---|
| CND-01 / P1 | Reuse one entry for context A (`Value = 3`) and B (`Value = 8`), comparing to 3. After A succeeds, B incorrectly reads A's cached member/target and also succeeds. Passing null can retain A as well. Affects Component, GameObject, and Other Object branches, including shared State Machine conditions. | `ConditionValueSource.EnsureCache`, `EnsureCacheComponent`, `EnsureCacheGameObject`: resolve the current target before considering a binding reusable; reuse only when the target matches. Component bindings now retain their owning GameObject for this check. |
| CND-02 / P2 | Query a private member with Public flags, then NonPublic flags: the cached miss hides the existing member. Reverse the order: a Public-only lookup receives a private member. Properties, fields, and methods are all affected. | `ReflectionCache.GetProperty/GetField/GetMethod`: include `BindingFlags` in every cache key. Metadata hits/misses and existing public signatures remain unchanged. |
| CND-03 / P2 | Configure the supported `short` or `byte` method argument using the Inspector's integer value. The reader passes a boxed Int32 directly to reflection; narrowing is rejected, so a valid condition returns false without invoking its method. | `ConditionValueSource.ReadValue` and method-cache construction: cache the actual parameter type and convert with `Convert.ChangeType`/invariant culture. Out-of-range values fail through the existing evaluation error path before method invocation. The reused argument array and per-evaluation argument refresh remain intact. |
| CND-04 / P2 | A method such as `int GenericOnly<T>(int value)` passes the argument/return filters, but cannot be invoked without generic type arguments. Runtime lookup exposes an unusable method, and may select it ahead of an invokable overload. | `ConditionEntry.FindMethodWithOneArgument`: exclude methods with `ContainsGenericParameters`. No serialized method-name/signature migration. |
| CND-05 / P2 | An explicitly assigned Other Source Object is destroyed before startup binding. Unity's overloaded null comparison treats it as unset, so `BindOtherToSourceIfNull` replaces it with the left source. An unavailable threshold can become a successful self-comparison. | `ConditionEntry.BindOtherToSourceIfNull`: use managed-reference null identity when deciding whether the field was unset, preserving the resolver's existing destroyed-reference policy. |
| CND-06 / P1 | With Mirror, `CmdClientResult` is registered with `requiresAuthority = false`, but its server body never checks Authority. A client-provided result can update server state/fire events even under ServerRevalidate. Both command bodies also previously ignored `isNetworked`, and used a component-wide cooldown for unrelated senders. | `NeoCondition.CmdClientResult`: require networking enabled and TrustClient mode before accepting input. `CmdRequestCheck` also requires networking enabled. Both reuse the existing `RateLimitCheck(sender)` primitive; rejected configuration does not consume its budget. |
| CND-07 / P2 | A pure client calls `Check()` repeatedly in ServerRevalidate mode with Only On Change enabled. Its local boolean stays constant while server state changes. The local deduplication return occurs before the request, so the server never re-evaluates after the first local result. | `NeoCondition.Check`: dispatch ServerRevalidate requests before local evaluation/change suppression. The server still deduplicates its authoritative result; TrustClient/local behavior retains the existing event logic. |

Four owned runtime files changed. Serialized field names/types, enum values, public declarations, menus, and scene/prefab data were preserved. No shared configuration, assembly definition, sample layout, or asset wiring changed.

### Regression sources

New files, both with unique MonoImporter `.meta` GUIDs:

- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionRegressionTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionValueProbe.cs`

The fixture contains **25 NUnit cases across 13 test methods**:

- Six cache cases check Public/NonPublic lookup isolation after hits/misses, and read/invoke the actual returned member to observe 41/42.
- Three context cases verify Component/GameObject switching, missing context, explicit source precedence, and independent Other Object rebinding.
- Five valid short/byte cases verify comparison output, actual argument received, and call count, including both parameter boundaries.
- Four overflow cases verify false results and zero method calls; argument refresh and Other Object method invocation each have a separate case.
- One case verifies the public method lookup rejects an open generic definition.
- One serialized-source case verifies equality before destruction and failure after destruction/startup binding, preventing silent self-comparison.
- Three command-ingress cases verify rejected input cannot update `LastResult` or emit `OnResult`. They invoke Mirror's received `UserCode_...` body rather than its outgoing wrapper, and require no listening transport. These cases explicitly skip when the woven body is absent; a skip is not a pass.

No existing tests were edited. `Neo.Editor.Tests.asmdef` already references Neo.Condition, the editor/Unity test framework, and required dependencies; no reference addition is requested. The revalidation-routing change and accepted multi-peer rate-limit behavior require the coordinator's network integration scenario below.

## Documentation

Updated the existing English public entry points:

- `Condition/README.md`: C# entry example and context reuse, component workflow, real API names (`ResetState`, `InvalidateAllCaches`), and event/LastResult semantics.
- `Condition/NeoCondition.md`: supported numeric method signatures/conversion/range behavior, non-generic runtime lookup, overload limitation, source precedence/search retries/destruction, cache invalidation, inversion on unavailable values, and network authority/revalidation behavior. Corrected both result-event descriptions to reflect Only On Change.
- `Condition/Condition_Reuse.md`: context behavior, current State Machine ContextSlot/scene Context Overrides model, explicit C# types in the loop example, accurate inspector integration boundaries, and cache/failure behavior. Removed an incomplete introductory fragment.
- `Condition/InternalTypes.md`: current reader/cache/resolver responsibilities and public versus internal surfaces.

The roadmap was left as historical material. No duplicate guide or DocsEn tree was added. All local links in these four edited documents were checked.

## Substantial improvement proposal (not implemented)

### Typed evaluation core with explicit unavailable results and separate binding sessions

**Problem.** `ConditionEntry` combines authoring data, scene references, mutable binding caches, member selection, conversion, and boolean comparison. C# callers cannot configure method arguments or most Other Object settings through public typed setters. Reused data switches a single mutable binding session between owners. Missing data and a legitimate failed comparison both become false, then inversion turns either into true. Name + ArgumentKind cannot identify overloads such as `Read(int)`/`Read(long)`, and component type name cannot distinguish two components of the same type. Runtime/editor discovery and preview logic have already diverged.

**Architecture / proposed additive API.** Introduce a small plain-C# comparison layer rather than another manager:

```csharp
public interface IConditionValueSource<TContext, TValue>
{
    bool TryRead(TContext context, out TValue value);
}

public enum ConditionEvaluationStatus
{
    Passed,
    Failed,
    Unavailable,
    InvalidConfiguration
}
```

Add a `ConditionResult` carrying status and the comparison result, plus a `ConditionComparison<TContext, TValue>` accepting typed left/right sources and an explicit comparer/operator. A Unity adapter resolves GameObjects/components; an optional reflection adapter prepares member/parameter metadata once per evaluation session. Cache sessions belong to the consuming owner, rather than the serializable definition. Shared definitions can then remain free of scene-object references; scene wrappers provide bindings/context slots. Use typed providers or cached reflection for IL2CPP/AOT; do not require runtime code generation.

Expose an additive `ConditionEntry.TryEvaluate(GameObject context, out bool result)` (or a status-returning equivalent), typed source/method configuration factories, and a prepared binding handle. Retain `Evaluate` as the existing compatibility path. A versioned member descriptor can include declaring type, exact parameter type, and an optional component binding identifier, while the existing name/kind/type fields remain readable as the legacy fallback. Editor discovery/preview should consume the same descriptor/validation rules.

**Compatibility / migration.** Keep all current serialized fields and public boolean APIs. Existing entries preserve inversion, source precedence, and event behavior. The new status API treats unavailable/invalid results explicitly; failure policy is opt-in rather than changing old scenes. Migrate exact signatures only through deliberate editor authoring, preserving legacy fallback for existing assets. Keep ScriptableObject data free of scene references by providing bindings on scene components. Any new assembly or dependency split must be planned and performed centrally, outside this worker patch.

**Acceptance scenarios.** A pure C# provider/comparison works without a scene component; one definition is evaluated for two owners with independent prepared sessions; missing data stays Unavailable under inversion in the new API; legacy Evaluate retains its documented result; exact overloads and duplicate component bindings remain stable after reload; numeric bounds, argument changes, destroyed/replaced objects, and both source branches retain coverage; editor preview and runtime return the same value for all supported signatures; IL2CPP stripping/AOT and Unity 6.0/6.5 compilation are verified without a reflection-only scene dependency.

**Effort / risk.** Approximately 4–6 developer days for the additive core, Unity adapter, contract tests, and docs, then 2–3 days for coordinated inspector/member-descriptor migration and player/AOT checks. Main risks are serialized legacy ambiguity, consumers relying on inversion of unavailable sources, delegate lifetime retention, and metadata stripping. Ship the additive core first; retain the old path and make authoring migration explicit.

## Verification and remaining risks

### Checks actually executed

- Read `AGENTS.md`, the full worker contract, the exact Neo.Condition manifest entry, and the dedicated initial report; reviewed every owned C# file.
- A scoped Python read-only consistency check confirmed the exact six-file inventory and matched serialized field names/types and public declaration lines against `ca198556` for all six files.
- Strict UTF-8 decoding succeeded for all owned source, edited public docs, and new test/metadata files; no replacement or NUL characters were found. Original source BOMs were preserved.
- Added C# lines/new test files contain no `var`; both new importer GUIDs occur exactly once under Assets; new-file whitespace checks passed.
- All local Markdown links in the four edited public docs resolve.
- Scoped `git diff --check -- <owned source/docs/tests/report>` exited 0. New/untracked test and metadata whitespace was checked separately, because normal git diff excludes untracked content.
- Confirmed the command-body test naming/access against the local Mirror weaver's `SubstituteMethod` and `GenerateMethodName` implementations. This is static compatibility evidence, not a woven-assembly execution.

**Not executed by this worker:** Unity import/compilation, Edit Mode/Play Mode tests, Mirror weaving/transport scenarios, player builds, installs, or test/build runners, as explicitly reserved for the coordinator. The 25 cases are authored, not passed. No commit was created.

### Coordinator actions / cross-boundary findings

1. Run the new Edit Mode fixture, existing NeoCondition suites, and affected State Machine/NoCode binding suites. Compile both Mirror-enabled and disabled configurations and the required Unity versions; no branch is declared compiled here.
2. In a real Mirror client/server scenario with Only On Change enabled, keep the client's local value constant, change server state, and call Check again: the server must re-evaluate and publish the change. Verify TrustClient acceptance, ServerRevalidate rejection, disabled-network rejection, two independent senders, host single-event behavior, and late join. The three ingress regressions cover rejection bodies only.
3. **Neo.Editor owner:** `Assets/Neoxider/Editor/Condition/NeoConditionEditor.cs:GetCurrentValueFromComponent` still directly invokes methods with boxed integer arguments and does not reject open generic methods. `Assets/Neoxider/Editor/Binding/ComponentBindingInspectorShared.cs:BuildConditionMemberLists` also lists generic definitions. Align preview/picker with runtime conversion and generic filtering; these editor-owned sources were not modified.
4. Exact overload and duplicate-component identity remain legacy representation limitations. `DrawComponentDropdown` selects by type name, and runtime picks the first matching component; stabilizing these bindings requires coordinated authoring/runtime work, covered by the proposal rather than a unilateral serialized-data change.
5. Direct binding-time reflection failures (for example ambiguous property metadata), unavailable-value inversion, reflection stripping/AOT, and cached metadata lifetime deserve the proposed explicit-status/descriptor work. Existing read/conversion exception handling and gated diagnostics remain; no full platform-safety claim is made.

Кратко: полностью проверены шесть файлов; исправлены повторное использование контекста, ключи кэша, вызовы методов и сетевые проверки полномочий. Регрессии и документация подготовлены. Unity-проверки не запускались по контракту; результаты компиляции и тестов должен приложить координатор.
