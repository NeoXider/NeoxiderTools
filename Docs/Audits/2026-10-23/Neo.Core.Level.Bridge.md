# Neo.Core.Level.Bridge: audit

Status: source audit and scoped fixes complete; static checks passed. Unity compilation and test execution remain assigned to the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Core/Level/Bridge/Neo.Core.Level.Bridge.asmdef`.
- Scope: 4 C# files; nested assemblies excluded.

## Scope and complete inventory review

Read the repository `AGENTS.md`, all of `WORKER-CONTRACT.md`, the exact `Neo.Core.Level.Bridge` manifest entry, and the original report. Ownership is the four files below, `Assets/Neoxider/Docs/Core/Level/Bridge`, this report, and new regressions with prefix `Audit20261023NeoCoreLevelBridge`. There are no nested assemblies or additional C# files inside this assembly's directory.

| Owned C# file, relative to `Assets/Neoxider/Scripts/Core/Level/Bridge/` | Review outcome |
|---|---|
| `LevelConditionAdapter.cs` | Reviewed completely: explicit/context resolution, Unity destroyed-object checks, null context, all three inclusive comparisons, threshold and inversion, invalid mode fallback, `EvaluateCurrent`, cached result. Fixed inconsistent missing-provider result. |
| `LevelConditionEvaluationMode.cs` | Reviewed completely: all three modes match dispatch; serialized numeric ordering unchanged. No defect found. |
| `LevelNoCodeAction.cs` | Reviewed completely: explicit/sibling resolution, late providers, both actions, public setters and clamping, missing/unknown actions, success/change events, downward changes, callback mutation and reentrancy. Fixed event result attribution. |
| `LevelNoCodeActionType.cs` | Reviewed completely: both actions match dispatch; serialized numeric ordering unchanged. No defect found. |

Read-only context: `ILevelProvider`, `LevelComponent`, `LevelModel`, `LevelCurveDefinition`, `IConditionEvaluator`, existing level and ability bridge test patterns, both owned public docs, and the runtime/test asmdefs. The older audit was a lead only; the current source independently demonstrated the cached-result issue. Existing level tests already cover simple unchanged-XP and upward-SetLevel events; the new fixture covers callback interactions and adapter behavior.

Reuse: checked the neoxider-tools module/game-system references and relevant helper catalogs. Reused `ILevelProvider`, `LevelComponent`, `LevelCurveDefinition.SetLinear`, and the established `SerializedObject` test setup pattern. No new runtime helper, manager, service, or dependency was introduced.

### Lifecycle, boundaries, and compatibility

- Both bridges resolve locally on each explicit API invocation. They own no coroutine, subscription, static cache, disposable resource, or save state; there is no Update loop or lifecycle cleanup to add. Missing providers do not log or allocate persistent state.
- Explicit `LevelComponent` references take precedence; destroyed references fall back using Unity's object-null semantics before conversion to `ILevelProvider`. Actions fall back to their own GameObject; conditions use the supplied context, with `EvaluateCurrent()` selecting their own GameObject. There is no parent/child/global scene search.
- The action's public setters clamp negative XP to zero and target levels below one to one; the provider owns XP arithmetic, curve behavior, and caps. Zero-XP and unchanged-level invocations retain success events. Unknown actions remain no-ops; unknown condition modes remain false before inversion. A zero XP-to-next value still satisfies a nonnegative `XpToNextLevelAtMost` threshold.
- Preserved every serialized field name/type, all public declarations, enum ordering, UnityEvent names, and event ordering. `OnLevelUp` intentionally still includes downward SetLevel changes. No scene/prefab/sample migration is required.
- Runtime dependencies remain `Neo.Core`, `Neo.Core.Level.Components`, `Neo.Extensions`, `Neo.Condition`, `Neo.Reactive`, and `Neo.PropertyAttribute`. No asmdef edits were needed. The existing editor test assembly already references all types used by the new fixture.
- No editor API entered runtime code, and no obsolete Unity identity APIs or version branches were introduced. Compilation on either Unity version family has not been performed by this worker.
- No owned ScriptableObject stores scene references. Both component bridges remain optional scene adapters over the provider contract. Their restriction to concrete `LevelComponent` resolution is an existing API limitation, addressed in documentation and the proposal below.

## Findings and fixes

### P2 — Missing-provider inversion disagreed with LastResult (fixed)

Source: `Assets/Neoxider/Scripts/Core/Level/Bridge/LevelConditionAdapter.cs:28`.

Trigger: no explicit/context provider, or a previously assigned provider has been destroyed, with `_invert = true`.

Source-proven behavior before the fix: the missing-provider branch assigned `LastResult = false` but returned `_invert`, hence `Evaluate(null)` returned true while `LastResult` reported false. Consumers reading the cached property disagreed with callers using the return value, including after provider removal.

Fix: assign the inverted missing-provider result to `LastResult` and return that same value. The existing false-before-inversion policy remains unchanged, including inverted success when a provider is absent.

Regression evidence authored: `Condition_MissingProvider_LastResultMatchesReturnedValue` and `Condition_DestroyedProvider_ReplacesPreviousResult`, each with inversion off/on. Assertions compare public return values and cached output, including a transition from a present provider to a destroyed one. The inverted cases expose the original branch directly; these tests have not been executed here.

### P2 — Success listeners could suppress, replace, or invent the action's level event (fixed)

Source: `Assets/Neoxider/Scripts/Core/Level/Bridge/LevelNoCodeAction.cs:97`.

Trigger: an `OnSuccess` UnityEvent listener changes the provider, directly or by synchronously executing another action.

Source-proven examples before the fix:

- From level 1, SetLevel(4), then an OnSuccess listener sets level 7: the bridge emitted 7 instead of the action's resulting 4.
- The same action followed by a listener restoring level 1 emitted no level-change event at all.
- A SetLevel(1) or non-leveling AddXp followed by a listener setting level 4 invented a level-change event for an action that had not changed the level.
- An outer SetLevel(2) whose success listener runs a nested SetLevel(3) reported 3 twice, losing the outer result.

Fix: capture `provider.Level` immediately after the supported provider operation, then invoke OnSuccess and compare/report the captured level. This preserves success-before-change event order and reports each synchronous invocation's own result.

Regression evidence authored: four callback reset/overwrite cases spanning both actions; two cases excluding spurious events after a success listener changes an otherwise unchanged level; and a nested invocation asserting the exact output sequence `success:2, success:3, level:3, level:2`. Assertions observe provider state and real UnityEvent payloads/order. These tests have not been executed here.

### Documentation defect — Claimed arbitrary ILevelProvider support (corrected)

The action's class summary and public guide claimed it could target any `ILevelProvider`. Both resolver paths actually accept `LevelComponent` only, as does the public `LevelProvider` property. Corrected the claim without expanding or breaking the runtime API. An interface-based alternative is proposed below.

### Cross-boundary observation — XP overflow handled concurrently by Neo.Core

The dependency snapshot initially read used `LevelModel.AddXp` with `TotalXp += amount`. In unchecked arithmetic, starting at 1 XP and adding `int.MaxValue` would wrap TotalXp negative, potentially corrupting saved progress. This belongs to `Neo.Core`, not this assembly; this worker made no dependency edit.

A later targeted inspection during final validation found the concurrent owner had already changed the method to clamp the addition with `Math.Min(amount, int.MaxValue - TotalXp)` and report the accepted amount. The original overflow finding is therefore not an outstanding bridge defect in the current shared working tree. Coordinator action: validate the Neo.Core owner's saturation regression centrally; do not attribute that implementation to this worker.

## Documentation

Updated the two existing English canonical pages under the owned Bridge directory; no redundant entry point or shared navigation was added.

- `LevelConditionAdapter.md`: complete Inspector setup, all inclusive comparisons, threshold and XP-to-next semantics, C# `IConditionEvaluator` calls, explicit/context precedence, destroyed/missing providers, inversion, and the actual `LastResult` contract. Clarifies which configuration remains Inspector-only.
- `LevelNoCodeAction.md`: both actions and public configuration APIs, C# and UnityEvent workflows, local resolution, clamping/caps, zero/missing/unknown cases, downward changes, callback-safe captured payloads, and synchronous nested event ordering. Removes unsupported generic-provider claims.
- Both pages cross-link each other and the existing component/module docs. All six local documentation links were resolved against the filesystem.

## Substantial improvement proposal

### Typed provider injection and reusable condition rules (proposed, not implemented)

**Problem.** The core already exposes `ILevelProvider`, but both bridges are coupled to `LevelComponent`. A custom level provider or pure C# model cannot be injected. The condition adapter additionally exposes no public configuration setters, forcing C# callers either to bypass it or to rely on Inspector serialization. This limits reuse in battle passes, AI gating, headless simulations, and tests with independent providers.

**Proposed design/public API.** Add a plain immutable `LevelConditionRule` with constructor `(LevelConditionEvaluationMode mode, int threshold, bool invert = false)` and `bool Evaluate(ILevelProvider provider)`. It owns only comparison policy; the MonoBehaviour owns provider resolution and LastResult. Initially keep the rule and existing enum in the Bridge assembly to avoid changing enum assembly identities or introducing a Core-to-Bridge dependency cycle.

Add `Initialize(ILevelProvider provider)` to both component bridges, plus typed `Mode`, `Threshold`, and `Invert` accessors on `LevelConditionAdapter`. Provider precedence should be documented as injected override, serialized LevelComponent, then existing local/context fallback. `Initialize(null)` clears only the injected override. Setting the existing action `LevelProvider` property should clear the runtime override so legacy runtime configuration remains authoritative. A destroyed injected Unity object must fall back even though an interface reference is not CLR-null; ordinary C# providers must remain valid without any Unity object requirement.

**Compatibility/migration.** Retain every existing serialized field, concrete property, enum value/location, UnityEvent, and parameterless call. Existing scenes need no migration and retain their current resolution path unless code explicitly injects a provider. Preserve missing-provider inversion and captured event-result semantics. Refactor comparison dispatch into the rule only after parity cases pass. Do not move types between assemblies as part of this change.

**Acceptance scenarios.** A fake plain C# ILevelProvider supports all three comparisons and both actions without a GameObject provider; negative thresholds are handled by a documented constructor policy; runtime configuration equals Inspector configuration; a destroyed injected MonoBehaviour falls back correctly; clearing injection restores serialized/context selection; old scenes produce unchanged events; callback mutation and nested invocation retain the regression outputs in this audit. Rule tests should be pure state/output tests, with only resolution parity requiring EditMode components.

**Effort/risk.** Approximately 1–2 engineering days plus central Unity version validation. Main risks are provider precedence, Unity interface references after destruction, and accidentally changing missing-provider policy during extraction. Keep implementation additive and split pure rule behavior from scene resolution tests. No install, new framework, or shared settings change is necessary.

## Verification and remaining risks

### Authored regressions

New file: `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelBridgeTests.cs`, with unique GUID `c8d88d3c243b4da1b9ff6ea9e7d30b60` in its new `.meta`. It contains 10 test methods and 22 expanded NUnit cases. In addition to the defect regressions above, it covers all condition modes at inclusive/pass-fail boundaries with both inversion states, explicit/context precedence after destruction, late sibling provider creation, XP/state output, downward/unchanged SetLevel behavior, and unsupported actions.

Fixture providers use an explicit 100-XP linear curve. Hosts start inactive so configuration precedes Awake; save loading, autosave, and save keys are disabled before public initialization. Teardown destroys all created GameObjects and the curve. `SerializedObject` is used only for existing private Inspector configuration; behavioral assertions use public state and events, not source text or reflected internals.

### Checks actually run

- Read all four inventory C# files completely and confirmed directory C# inventory equals the manifest (4/4).
- Ran a read-only Python static consistency check: compared all existing public declarations and serialized field names/types against `git show HEAD:<path>`; no changes. Both enum files are unchanged.
- Lexically checked balanced braces/brackets/parentheses and explicit types across four runtime files plus the new fixture. No runtime UnityEditor import, obsolete identity call, or new version conditional was found. This is a static check, not compilation.
- Strict UTF-8 decoding, no replacement characters, and final-newline checks passed for four runtime files, two docs, the new fixture, and its metadata. Final report validation is included in the final whitespace pass.
- Resolved all six local links in the two updated guides. Confirmed required test assembly references and NUnit precompiled reference already exist; no shared asmdef edit is required.
- Verified the new meta GUID occurs exactly once in package asset metadata. Counted 10 authored test methods / 22 cases. Checked the untracked regression file for trailing whitespace (zero lines).
- Ran `git diff --check --` scoped to the four inventory files, both owned guides, new fixture/meta, and this report; exit code 0. Untracked files are additionally covered by the explicit UTF-8/whitespace checks, since git diff does not include their contents.
- Reviewed the scoped source/docs diff and the new fixture. No commits, installs, shared configuration edits, Unity/MCP, builds, test runners, services, scene/sample changes, or child agents were used.

### Remaining validation and coordinator handoff

Compile/import: **not run**, per worker contract. EditMode/PlayMode/builds: **not run**, per worker contract. These 22 test cases are authored and statically reviewed, not claimed passing. Run `Neo.Core.Tests.Audit20261023NeoCoreLevelBridgeTests` in the existing editor test assembly and validate both supported Unity API branches centrally. No independent QA agent was used because child agents were expressly prohibited.

Callback exceptions retain existing propagation/short-circuit behavior. The captured result is sampled after the provider method returns; callbacks inside the provider's own implementation remain the provider owner's responsibility. Shared dependencies changed concurrently, so coordinator integration/compilation must use the assembled working tree. No unresolved owned-source defect was identified after these focused fixes.

Кратко: полностью проверены четыре файла, исправлены расхождение LastResult с результатом инверсии и искажение событий уровня обработчиками OnSuccess. Добавлены содержательные регрессии и обновлены оба руководства. Выполнены только статические проверки; запуск Unity и тестов остаётся у координатора. Предложение по внедрению ILevelProvider не реализовано.
