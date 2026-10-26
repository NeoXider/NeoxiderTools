# Neo.Network.Contracts — implementation receipt

Status: **source-final; terminal reconciliation completed on 2026-10-25**. Owned corrections are present; the former cross-owner A-10 runtime/backlink dependencies are now available in source. Target: `demo`, `11.0.0`. Original actor: `neo-audit-1023-b01-03`; same audit/plan/implementation session, `gpt-6.1-sol`, `max`. The branch was reread as `demo`; the previously recorded implementation gate was `globalReviewApproved: true`. Current compile/test/product acceptance remains **UNEXECUTED by this worker**.

## 2026-10-25 terminal reconciliation

Root reports that the original CLI metadata retained `error` / exit 1 following historical Windows memory/fork interruption. The earlier source-final receipt and native/root reviews did not prove successful terminal completion of that interrupted step. This reconciliation does not relabel that failure or claim that historical native failures passed against today's source.

The same original session reread the implementation contract/request, this receipt, both current owned declarations, the Contracts guide, the complete existing regression source, its current nearest test asmdef, and the two relevant cross-owner boundaries. All four owned source/doc/test fingerprints still match the earlier receipt. No outstanding owned runtime, documentation or regression-source correction was found; only this receipt is changed on this resume. No patch was repeated and no runtime scope was expanded.

The completed primary fixture extraction is retained: the existing Contracts fixture still lives at `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkContractsTests.cs`, uses its own two serialized DTOs, and consumes no extracted helper API. Its current `Neo.Editor.Tests` configuration includes `Neo.Tests.Fixtures` and `Neo.Tests.Fixtures.Network` references. Shared helpers/references remain primary-exclusive and were not edited. Current source inspection and artifact hashes are not compiler, discovery, native-lifecycle or product-validation evidence.

## Public API / dependency publication

**Additive public APIs: none.** Dependents continue using assembly `Neo.Network.Contracts`, namespace `Neo.Network`, `INeoOptionalNetworked.IsNetworked { get; }` and `NetworkAuthorityMode.None = 0`, `OwnerOnly = 1`, `ServerOnly = 2`. No new assembly reference, symbol, component or initialization is required. The optional `NetworkAuthorityPolicy` extraction remains an unimplemented proposal in the historical plan.

## Cross-owner handoff

`A-10`: the `Neo.Network` owner (`neo-audit-1023-b04-05`) supplied rejection of unknown policies in `Assets/Neoxider/Scripts/Network/Core/NeoNetworkState.cs`. The earlier receipt observed `default: return true`; the 2026-10-25 source now contains `default: return false` at lines 178–179. The switch still precedes named-policy allowances, and the three named cases retain their truth tables and exact `sender == NetworkServer.localConnection` comparisons. This is current source evidence, not an executed authorization test. The existing public `NeoNetworkState.IsAuthorized(GameObject, NetworkConnectionToClient, NetworkAuthorityMode)` API is available; Contracts adds no replacement evaluator.

The parent owner's `Assets/Neoxider/Docs/Network/README.md` now links `[Network contracts](./Contracts.md)` from its Guides table at line 34 and includes a further Contracts link at line 76. Contracts links back to that canonical module page. The previously missing backlink is resolved in source. Neither cross-owner file was edited by this worker.

## Completed actions and exact files

| Action | Completed owned work |
|--------|----------------------|
| `NC-01`, `NC-02` (historical audit fixes) | Preserved the earlier `INeoOptionalNetworked.cs` XML-reference correction and factual hierarchy/configuration description. The implementation request assigned no additional executable defect to Contracts. |
| `A-10` — Contracts declaration/documentation portion | Updated `Assets/Neoxider/Scripts/NetworkContracts/NetworkAuthorityMode.cs` remarks: only named policies are valid; casts/deserialization do not validate them; consumers must reject unnamed values before applying server/host/owner allowances. Executable enum declarations are unchanged. |
| `A-10` — owned public documentation | Updated `Assets/Neoxider/Docs/Network/Contracts.md` with the explicit 11.0.0 invalid-policy migration, all-caller rejection, historical permissive behavior, preserved named-policy semantics and explicit repair of unsupported configuration. Clarified that the enum's zero default does not dictate every component's initialized policy. Existing C# / Inspector / offline scene workflows and navigation links remain. |
| Regression source preservation | Retained `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkContractsTests.cs` and its seven useful serialization cases unchanged. No duplicate enum/fixture tests or cross-owner authority tests were introduced. |

This phase changed the enum's XML remarks, the owned Contracts guide, and this receipt. It did not change the interface implementation surface, existing prefixed tests or metadata, historical audit/plan, shared configuration, runtime consumers, parent README, other receipts, scenes, prefabs, samples or package version files. No new `.meta` is needed for the audit receipt outside `Assets`.

## Compatibility and deliberate migration

The read-only interface member, public namespace/assembly, enum member names, underlying integers, and consumer serialized fields are preserved. `default(NetworkAuthorityMode)` still equals `None`; individual components may specify a different default. No asset rewrite or `Samples~` layout change is involved.

The approved behavioral change belongs to `Neo.Network`: invalid integers such as `-1`, `3`, and `int.MaxValue` must reject every caller, including direct server and exact local-host connection. Previously accepted invalid data must be corrected to an intentionally selected named policy. It must not silently become `None`. All three named-policy truth tables and the exact `sender == NetworkServer.localConnection` exception remain the runtime owner's compatibility obligations. The current rejecting default agrees with the Contracts guide's 11.0 contract; observable acceptance still requires later integrated execution.

## Authored acceptance source — UNEXECUTED

The retained Contracts fixture contains:

- `ExistingSettings_RestoreOriginalAuthoritySelection`: historical JSON values `0`, `1`, `2` restore `None`, `OwnerOnly`, `ServerOnly` respectively (three cases).
- `SavingSettings_PreservesCompatibleAuthorityInteger`: those three named values serialize to the same historical integers, observed through an independent integer DTO (three cases).
- `SettingsWithoutAuthorityField_SelectDefaultPolicy`: the fixture's uninitialized field restores `None` (one case; this does not assert defaults for independently initialized component fields).

The `Neo.Network` owner supplies A-10 authority regressions for invalid policies across remote owner/nonowner, trusted null/exact-local senders, null target and missing identity; the optional pure evaluator is not a prerequisite. Runtime/editor scene-reactivation integration, including an inactive parent and active-session exclusions, remains central acceptance work rather than a Contracts runtime rewrite.

No authored case was executed by this worker, including during the 2026-10-25 reconciliation. Current acceptance remains UNEXECUTED here. Wait for every source writer to finish before frozen-snapshot compilation, nonempty Edit/Play discovery and relevant acceptance on installed Unity 6.3/6.6. The shared test assembly explicitly references Mirror: these test sources' lack of Mirror APIs does not prove a Mirror-free test assembly. MIRROR-on/off dependency evidence and XML-documentation compiler evidence are still required where applicable. Historical audit checks, root/native source reviews and historical native failures are separate evidence; none is a current integrated pass.

## Actual permitted work and source fingerprints

Performed source/document reads, branch/approval inspection, reference enumeration, scoped Git diff/whitespace inspection and SHA256 artifact bookkeeping only. The earlier scoped tracked-source `git diff --check` returned exit 0; it was not an execution receipt. The 2026-10-25 resume used small cmd/Python reads and SHA256 bookkeeping, preserving the four owned source/doc/test files byte-for-byte. No product checker, service, installation, compiler, runner, Unity/MCP, child agent, process control, commit or push was used. Shared trackers, historical audit/plan, primary-exclusive fixture helpers/references, foreign Unity, `DocsEn.meta` and release `Samples~` were untouched.

Current SHA256 snapshots reread on 2026-10-25 (receipt content itself excluded):

| File | SHA256 |
|------|--------|
| `Scripts/NetworkContracts/INeoOptionalNetworked.cs` | `70f4d91f655631933c87d5d4c313fa4fd272bd6aedaa528ac9ff490bbd8e6487` |
| `Scripts/NetworkContracts/NetworkAuthorityMode.cs` | `638e6cf3319a32a8d31ce8d1f0e1b09cbeb6032c01d6debfa6d42f79170383b6` |
| `Docs/Network/Contracts.md` | `40e002c095431e376e0f040923fa0e55ee6a39869262a6ec6becf8f6471f8d9f` |
| `Tests/Edit/Network/Audit20261023NeoNetworkContractsTests.cs` | `f2d543f0478a6d053d0546ecd898b2d4108e7cf14f1ec86d09a47578f8b1e45c` |
| `Scripts/NetworkContracts/Neo.Network.Contracts.asmdef` | `22eaab13315133c3d182912f8a5216a8c81df81f9a8acf813d80d43969a9bdf4` |
| `Scripts/Network/Core/NeoNetworkState.cs` (cross-owner, read only) | `1e86b88bb98d9b6a1cffe0fb026e2905f7fe412ed0d3638fb8cf5a17e20156c6` |
| `Docs/Network/README.md` (cross-owner, read only) | `b1bbf0f3b4216f8d8c73bc85afa0af1106639f76846de3be8c80df6d4653d743` |

Paths in the fingerprint table are relative to `Assets/Neoxider/`. Historical artifacts remain at the review's exact hashes: audit `51cd74dd0d92c28a57cbd6b0f1f659f43f736ebfd90c49f03ca03b8756ffa0b1`; plan `06ddd7d220f76c5bb1b587735f8c3af7aef9be6328fd90b69fd82ed5332b91d8`.

## Remaining dependencies

**No unresolved source/API dependency for this owner.** The previously pending A-10 rejecting fallback and canonical README backlink are present in the current source. No additional Contracts implementation or duplicate regression is required. Root retains the later frozen-snapshot compile/test/scene/optional-dependency evidence debt and terminal-metadata reconciliation. This source-only completion does not certify the historical failed CLI step or claim current test execution.
