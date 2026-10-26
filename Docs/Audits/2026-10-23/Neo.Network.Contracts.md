# Neo.Network.Contracts: audit

Status: full source review complete; scoped documentation fixes and compatibility regressions added. Static verification is recorded below; Unity compilation and test execution remain centrally pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/NetworkContracts/Neo.Network.Contracts.asmdef`.
- Scope: 2 C# files; nested assemblies excluded.

## Scope

Reviewed both manifest-owned C# files completely (2/2). No nested assembly exists under this source root. Read dependencies/tests only as context; no other worker's source, tests, docs, or shared configuration were edited.

| File | Review and result |
|------|-------------------|
| `Assets/Neoxider/Scripts/NetworkContracts/INeoOptionalNetworked.cs` | Getter-only public contract; configuration rather than connection state; explicit implementations and hierarchy aggregation checked against runtime/editor consumers. No implementation, allocations, scene references, lifecycle hooks, resource ownership, or logging. Fixed the unresolved external XML reference and incomplete reactivation description. |
| `Assets/Neoxider/Scripts/NetworkContracts/NetworkAuthorityMode.cs` | Every enum member and explicit integer reviewed: `None = 0`, `OwnerOnly = 1`, `ServerOnly = 2`. Checked meanings against the authorization consumer, host exception, defaults, serialization compatibility, and unnamed integer inputs. No runtime logic defect in the enum. Documented persisted numeric stability and consumer responsibility. |

Assembly definition inspected read-only: `Neo.Network.Contracts`, namespace `Neo.Network`, no explicit references/define constraints/version defines, all platforms. Its declarations use neither UnityEngine nor Mirror APIs and contain no conditional version branches or Unity object-ID calls. The existing `noEngineReferences: false` setting was left unchanged. There are no serialized fields or public implementation methods to migrate in the owned runtime source.

Context inspected: `NeoNetworkComponent`'s explicit property, `NeoMirrorSceneReactivator` (including all identity eligibility/hierarchy guards), `NeoMirrorScenePostProcess`, `NeoNetworkState.IsAuthorized`, implementation sites in Money/physics controllers, existing network test coverage and test assembly references. The installed `neoxider-tools` skill's `references/network.md` was read; no runtime helper or replacement system was needed.

## Findings and fixes

### NC-01 — Low: unresolved XML reference across the assembly boundary (fixed)

- **Trigger:** generating XML documentation for the contracts assembly. `INeoOptionalNetworked.cs:13` used `see cref="NeoMirrorSceneReactivator"`, but that type is in `Neo.Network`, which references Contracts; Contracts has no reference back to the implementation. The implementation type is additionally absent without Mirror.
- **Observable consequence:** the documentation compiler cannot resolve that symbol in this assembly (a CS1574-class documentation diagnostic when XML documentation is enabled); this was established from the assembly graph/source, not from an executed compiler.
- **Fix:** refer to the optional implementation by plain XML code text. No dependency, signature, binary, or runtime behavior change.
- **Regression strategy:** static XML-reference consistency check; a source-text NUnit test would not exercise public behavior and was deliberately avoided.

### NC-02 — Low: property documentation overpromised offline activation (fixed)

- **Trigger:** an identity hierarchy contains one implementation reporting `false` and another reporting `true`, or networking is active. The old property summary said returning `false` tells the reactivator to keep the GameObject alive.
- **Observable consequence:** a developer could reasonably expect activation from one false getter, while `NeoMirrorSceneReactivator.ShouldReactivate` (`Core/NeoMirrorSceneReactivator.cs:194-210`) and the editor post-processor (`Editor/Network/NeoMirrorScenePostProcess.cs:89-106`) require at least one implementation and reject any hierarchy containing a true implementation. The runtime also skips active sessions.
- **Fix:** XML docs now distinguish configuration from session state and explain the all-implementations condition. The owned public guide documents the full relevant workflow and limitations.
- **Regression strategy:** no activation implementation was changed; adding synthetic interface getters would test the fixture itself. Central runtime/editor owners retain responsibility for activation integration tests.

### NC-03 — Medium, cross-boundary: unnamed authority modes are accepted (not changed)

- **Trigger:** `NeoNetworkState.IsAuthorized(obj, remoteSender, (NetworkAuthorityMode)3)` or any other unnamed integer from code/configuration.
- **Observable consequence:** `Core/NeoNetworkState.cs:178-179` reaches `default: return true`, granting the same manual authority result as `None`, even for an unowned remote sender. C# enums permit unnamed values. This is a source-proven input case; a remotely controllable policy or exploit has not been demonstrated.
- **Owner/action:** coordinator / `Neo.Network` worker should review rejection of invalid policies and add consumer-level regressions. The behavior lies outside the two owned declarations; no cross-boundary source edit or intentionally failing test was added here. See the concrete proposal below.

No executable runtime bug was found inside the owned interface/enum, so no speculative runtime change was made. Public names, getter shape, enum ordering/integers, serialized consumers, and asset metadata were preserved.

### New compatibility regressions

`Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkContractsTests.cs` adds seven NUnit cases:

- Three historical JSON integer fixtures restore the original named policy.
- Three selected policies serialize to compatible integers, verified through an independent integer DTO rather than a same-enum round trip.
- A settings object with no policy field restores `None`.

These protect persisted public contract behavior against accidental renumbering, rather than only asserting the enum declaration or using reflection/source-text assertions. No Mirror symbol is required by the test source. The existing `Neo.Editor.Tests` assembly already references Contracts, NUnit and UnityEngine; no asmdef change is required. The shared test assembly still references Mirror, so this file alone does not prove that the entire project test suite runs without Mirror.

## Documentation

Created the manifest-assigned narrow page `Assets/Neoxider/Docs/Network/Contracts.md` (it did not previously exist), with a unique `.meta` GUID. It explains both always-available types, custom asmdef references, typed C# configuration, explicit interface access, configuration/session distinction, actual runtime/editor scene recovery conditions, Inspector policy selection, host/owner/server semantics, and numeric serialization compatibility. Existing Network entry/guide pages were read as context and linked, not edited outside ownership.

Coordinator action: link `Contracts.md` from the Network module entry point. No redundant module README, DocsEn tree, folder metadata, or package/root guide was created. Existing source `.meta` files were untouched.

## Substantial improvement proposal

### Extract a transport-independent, fail-closed authority evaluator (proposed, not implemented)

**Problem.** Policy declaration is reusable but its only shared evaluation depends on Mirror/GameObject state and accepts every unnamed value. Consumers can drift in their owner/host interpretation, and isolated policy tests currently require the networking implementation.

**Architecture / public API.** Add a small pure C# `NetworkAuthorityPolicy` alongside the existing enum, with `bool IsKnown(NetworkAuthorityMode mode)` and `bool IsAuthorized(NetworkAuthorityMode mode, bool isDirectServerCall, bool isLocalHostSender, bool senderOwnsObject)`. Reject unknown modes before evaluating any caller exception. For known modes, preserve the existing truth table: `None` allows any caller; `OwnerOnly` allows direct server, local host or actual owner; `ServerOnly` allows direct server or local host. No scene search, mutable state, logging, transport dependency, or service manager is needed. `NeoNetworkState.IsAuthorized(GameObject, NetworkConnectionToClient, NetworkAuthorityMode)` remains the Mirror adapter: it derives the three trusted facts and delegates to this evaluator.

**Compatibility / migration.** Keep the enum's names/integers and existing adapter signature. Existing scenes, prefabs, typed callers and valid-policy host behavior remain compatible. Invalid integers intentionally change from permissive to rejected; document this behavior change and surface invalid configuration during editor validation in the consumer module. Do not add a Contracts-to-Network reference or serialize scene references in ScriptableObjects. Implement only after agreement between the two assembly owners/coordinator; this audit does not expand runtime API opportunistically.

**Acceptance scenarios.** Test the pure evaluator with: remote non-owner versus owner for `OwnerOnly`; direct server and local-host calls for all named policies (including a remotely owned target); remote owner rejected by `ServerOnly`; remote non-owner allowed by `None`; `-1`, `3` and `int.MaxValue` rejected for every caller category, including direct server/host. Adapter regressions must verify null targets, missing identities, real ownership and local connections. Run the evaluator without Mirror and verify existing JSON fixtures keep selecting the same policies. Leave local/offline action handling in each consumer's existing flow.

**Effort / risk.** Estimated half to one developer day for the evaluator, adapter delegation, meaningful unit/adapter regressions and docs, plus centralized Unity verification with/without Mirror. Primary risks are a deliberate compatibility change for previously invalid data and accidentally changing local-host exceptions. A truth-table comparison for all named policies and preserved serialized fixtures bounds those risks.

## Verification and remaining risks

Checks actually run (read-only Python, Git and ripgrep; no C# execution):

| Check | Actual result |
|-------|---------------|
| Inventory / assembly boundary | Pass: exactly the two manifest C# files; only the owning asmdef under this root. |
| Declaration comparison with `ca198556` | Pass: both sources match the baseline after removing XML comments and blank lines; executable declarations/API values unchanged. |
| XML documentation parsing | Pass: both XML-comment documents parse; the external `cref` was replaced with code text. Compiler diagnostic verification remains pending. |
| UTF-8 / whitespace | Pass: all seven scoped source/doc/test/meta/report files decode strictly, contain no U+FFFD or trailing whitespace, and end with a newline. |
| Public guide relative links | Pass: all six links resolve to existing files. |
| New metadata GUID search | Pass: both new GUIDs occur exactly once under `Assets`, in their assigned `.meta` files. |
| Static NUnit inventory / references | Pass: six parameterized cases plus one default-policy case; test source has no Mirror dependency/guard or `var`; existing asmdef references Contracts and NUnit. |
| `git diff --check -- <all seven scoped files>` | Pass, exit 0. |
| New/untracked file diff whitespace | Pass: `git diff --no-index --check` against the empty input for all five files produced no whitespace diagnostics (exit 1 denotes added content; local LF/CRLF notices only). |
| Scoped diff review | Pass: the two existing source diffs contain only the documented XML changes; status contains the expected scoped additions. |

Unity compilation/tests are centrally scheduled by the coordinator; none were launched by this worker. The seven NUnit cases are authored, not executed. No independent QA or without-Mirror project build was performed; static evidence does not substitute for those checks. No build/test runner, Unity/MCP, installation, shared service, commit, git index mutation, scene/prefab change, or child agent was used.

Remaining coordinator actions: compile the assembly and run `Audit20261023NeoNetworkContractsTests`; exercise XML-documentation generation if used by packaging; link the new guide from the Network entry; route NC-03 to the `Neo.Network` owner. Existing shared test configuration requires Mirror despite this new test source not using it. The proposed evaluator remains unimplemented.

Самостоятельно проверены оба исходника сборки. Исправлены документационные дефекты; runtime API и числовые значения enum сохранены. Проверка компиляции и семь новых NUnit-кейсов остаются за координатором; за пределами владения отмечен разрешающий fallback для неизвестной политики.
