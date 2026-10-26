# Neo.Network.Contracts: approved implementation request

Original actor: `neo-audit-1023-b01-03`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

No additional confirmed runtime defect was assigned after independent review. Preserve your coherent earlier patch. Finish canonical docs/navigation and factual API/scene/NoCode contracts in owned scope, keep at least one substantial improvement described as a proposal, and write an honest source-final receipt. Do not manufacture a runtime rewrite or duplicate tests.

## Relevant full native sections

Source: NativeReview-A.md

### A-10 — P1: invalid authority enum values fail open

Owner: `Neo.Network`, original task `neo-audit-1023-b04-05`; Contracts owner `neo-audit-1023-b01-03` supplies declarations/docs. Current source: `Assets/Neoxider/Scripts/Network/Core/NeoNetworkState.cs:160–179`, `IsAuthorized`; SHA256 `072340a0e1a526336e46e94a62dae5ab05ecc11a4ebd80e95636a14dd3ea50e4`.

Trigger: a configured/cast/deserialized mode is -1, 3 or int.MaxValue. Expected: unknown policies reject every caller category. Actual: `default: return true` admits the call, including an unrelated remote sender. No remotely controllable policy/exploit is alleged; the reusable authority gate itself demonstrably accepts invalid configuration.

Smallest repair: fail closed for unknown modes before any server/host allowance; the current implementation switches before named-policy allowances, so a rejecting default is sufficient if that structure is retained. Preserve exact named-policy and `sender == NetworkServer.localConnection` behavior. Cover remote owner/nonowner, direct server, host, null target/missing identity and invalid values. The optional pure policy extraction is not required. Link `Contracts.md` from the canonical Network README and document the deliberate invalid-input change.
Source: NativeReview-A.md

### Neo.Network.Contracts — source review complete; cross-owner fail-closed repair required

Original owner: `neo-audit-1023-b01-03`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Network.Contracts.md` — `51cd74dd0d92c28a57cbd6b0f1f659f43f736ebfd90c49f03ca03b8756ffa0b1`.
- `Docs/Audits/2026-10-23/Neo.Network.Contracts.plan.md` — `06ddd7d220f76c5bb1b587735f8c3af7aef9be6328fd90b69fd82ed5332b91d8`.
- `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkContractsTests.cs` — `f2d543f0478a6d053d0546ecd898b2d4108e7cf14f1ec86d09a47578f8b1e45c`.

Both source diffs and complete new regression/Contracts guide were read with the actual NeoNetworkState consumer. Changes are XML/documentation only; enum values and marker ABI are retained. Serialization tests use historical integer fixtures and an independent integer DTO, rather than merely restating enum declarations. They own no persistent/global resources. No Update/search/allocation work exists in these declarations.

**Completed docs fixes appear correct. Confirmed cross-owner defect:** A-10, still present in Network. Reject waiting for an optional pure-policy extraction before failing closed. Preserve named host behavior and keep Contracts free of a reverse implementation dependency. The Network README backlink is still missing. Runtime/editor scene reactivation and the optional-dependency matrix remain **needs evidence**; a Mirror-free test source does not make the shared test assembly Mirror-free.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Network.Contracts.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Oct25 terminal reconciliation of historical failed wrapper

ROOT bounded terminal reconciliation on2026-10-25: latest original CLI metadata remains error/exit1 after historical Windows memory/fork interruption. Your existing source-final receipt has already undergone earlier root/native review, but cannot certify successful terminal completion of that failed step. SAME original session6.1sol/max, inspect exact current owned sources/receipt and original approved request, retain all work. Do NOT restart the audit or expand runtime scope. Verify source-only whether all assigned approved work is present and cross-owner API dependencies are now available; explicitly distinguish historical native failures from UNEXECUTED current source. Finish only actual outstanding owned correction or precise unresolved dependency, update own receipt with current source paths/hashes and honest completion status, then end normally. No product tests/compiler/Unity/checkers/new agents/install/process control/commit/push. Runtime Fixtures extraction is completed; helper files/shared references remain primary-exclusive. Avoid heavy PowerShell under current memory pressure, prefer small cmd/Python reads. Foreign Unity and the protected DocsEn.meta/release Samples~ are untouched.
