# Neo.Core: approved implementation request

Original actor: `neo-audit-1023-b02-08`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### ABIL-CAPPED-PAYMENT (P2; source-implemented-unexecuted)

Cost10 against pool with MaxDecreaseAmount1 pays1 yet casts successfully; require cap-aware preflight and captured payment amounts without claiming arbitrary callback transaction atomicity

### CORE-DEPLETION-REENTRY (P1; source-implemented-unexecuted)

Core plan D1: outer subtraction attributes nested transition twice; capture own depletion before callbacks

### CORE-CANSPEND (P2; source-implemented-unexecuted)

Need read-only exact cap-aware preflight for consumer costs; additive concrete API, no interface/batch redesign

### LEVEL-DATA-DOC (P3; approved-source-correction)

Link Core/Level/Data/CurveEvaluation.md from canonical Core/LevelCurveDefinition.md; retain fractional endpoint fix migration note and create missing new-folder metadata centrally.

### CORE-XP-REENTRANT-PAYLOAD (P2; approved-source-correction)

A-04: capture own recomputed level before XP callback; nested AddXp/reset must not replace outer operation transition.

### CORE-POOL-REGISTRATION-GENERATION (P2; approved-source-correction)

A-05: remove/readd SAME ResourcePoolEntry instance bypasses reference guard; tick snapshot includes registration generation before every stage.

## Relevant full native sections

Source: NativeReview-A.md

### A-04 — P2: nested XP callbacks replace the outer level-transition payload

Owner: `Neo.Core`, original task `neo-audit-1023-b02-08`. Current source: `Assets/Neoxider/Scripts/Core/Level/Domain/LevelModel.cs:124–130`, `AddXp`; read SHA256 `1e21c447a0686157bb78a9fe8050fbb985117a5b5d8448b924ca802c35ee5f0a`.

Trigger: on the default 100-XP curve, an `OnXpGained` listener performs one nested `AddXp(100)`, and the outer caller also adds 100 from zero. Expected operation transitions are nested `(2,3)` and outer `(1,2)` if the existing XP-before-level callback order is retained. Actual source flow emits nested `(2,3)` then outer `(1,3)`, because the outer code rereads the live `CurrentLevel` after listeners. A listener that resets the level can instead suppress or invent the outer level event. Consumers can grant incorrect level rewards or receive a payload for a different operation.

Smallest repair: capture the operation's resulting level immediately after recomputation, before `OnXpGained`, and compare/report that captured value; retain ordinary event ordering. Add public event-sequence regressions for nested AddXp and a listener resetting state. The existing level fixture observes arithmetic/events without mutation from its XP callback, so it does not cover this case. Source proof only; no test ran.
Source: NativeReview-A.md

### A-05 — P2: re-registering the same resource entry bypasses tick identity protection

Owner: `Neo.Core`, original task `neo-audit-1023-b02-08`. Current source: `Assets/Neoxider/Scripts/Core/Resources/Domain/ResourcePoolModel.cs:272` and `:288`, `Tick`; registration resets timers in `AddPool` but both tick guards compare only `ReferenceEquals`. Read source SHA256 `c831ad29e6c30cd0eee29bcf67f9d43a631239bba16022701f3abd2149c66857`.

Trigger: a pool has regen and discrete healing. Its first regen Changed listener removes the id, sets the retained caller-owned `ResourcePoolEntry.Current` to 7, and registers **the same entry instance** under that id. Expected: the new registration starts on the next Tick, as the public guide promises; current remains 7 for this tick. Actual: the reference check at line 288 passes, the reset heal timer receives the old tick's entire delta, and a 2-point/1-second heal yields 9 immediately. Re-registering another snapshotted pool with its same instance has the equivalent pre-loop issue.

Smallest repair: include a registration generation/token in the tick snapshot and validate it before each stage, incrementing/replacing the token on AddPool/remove; retain the legacy mutable-entry API and timer resets. Add same-instance remove/readd and same-instance replacement regressions. Existing coverage replaces an entry with a **different** object, so it misses this supported caller-owned alias case. This is a source-proven gap in the new tick protection, not a request to implement the broader owned-state proposal.
Source: NativeReview-A.md

### Neo.Core — source review complete; two further corrections required

Original owner: `neo-audit-1023-b02-08`. Full artifact reads:

- `Docs/Audits/2026-10-23/Neo.Core.md` — `6d197e36308cc46878ac2de1e6a27d167ad5c93c164703ba4b4290e7239a27d7`.
- `Docs/Audits/2026-10-23/Neo.Core.plan.md` — `cfe234b552b01ebc197e83f76cf1e2db348327f90e105170a30d6815789b8e5e`.
- `Docs/Audits/2026-10-23/Neo.Core.correction.md` — `e673aac6ed8673b7b6ed263f007a5115d4a6a9fc76c5a8c26398c314aabcf8c5`.

Every changed source hunk in the seven assigned files was inspected, including all evaluator families, LevelModel saturation/inversion and ResourcePoolModel validation/timer/notification branches. Current source hashes for the two reentry findings are recorded above. The current canonical Core README and changed LevelCurveDefinition guide were read with the wrapper call sites as context.

Complete new regression reads:

- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreLevelTests.cs` — `bde596362aa0bf73e4b15a0f0e632b255a79128206da1ae677464deb344a6fe4`.
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourceTests.cs` — `cfb4528253a726e09b1d646fc6bc1274d395631627d7036945a9d0779f1a4c2f`.
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreProfileTests.cs` — `9d7b14ff0ffe6e257e547dd115c3377f00ad2ed80b8d84e9509f285cb9c53227`.
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourceLifecycleTests.cs` — `3789cfb7fa64fa25280b8ce270db42a4734beaece54ccc1d26b0d764687e3ce3`.
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreCanSpendTests.cs` — `23cddb1a0503b1359018ae086bdbcb933026f6efac724480353e10019dc1d53c`.

Disposition: XP saturation/full-range inverse search, promoted numeric thresholds, exact capped-spend validation, actual-delta capture, invalid operation inputs, uncapped restoration and deep cloning are **completed fixes that appear correct by source inspection**. The later operation-local depletion correction is present and its captured transition survives nested mutations. `CanSpend` is a small additive concrete-model query sharing validation with TrySpend; it changes no state, events or timers and does not reserve resources. It leaves third-party provider interfaces intact.

The new pure tests protect meaningful balances, operation deltas, timer remainder, event sequences and immutable cloned data; no Unity/global state needs cleanup. Exception cleanup in Tick is explicitly covered. **Confirmed remaining defects:** A-04 and A-05. Their missing cases are distinct from the already-fixed duplicate depletion and different-instance replacement cases. Runtime numerical/native compilation evidence remains pending.

Aggregate cost: Tick snapshots and scans all model pools on every explicit update using one retained list, with no cycle-count loop; large elapsed intervals are aggregated. Level SetLevel uses bounded doubling/binary search, repeatedly calling the curve evaluator, so expensive custom/AnimationCurve definitions can multiply work. Mutable caller entries remain an existing alias contract; the broad settings/snapshot/change-record API is a **future proposal**, not required for the two minimal reentry repairs. New scheduling or thread-safety claims are not approved by source inspection.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Core.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Root-confirmed final source precision correction

ROOT CONFIRMED NI2 SOURCE LEAD: ResourcePoolModel.Decrease returns ClampDecrease amount instead of actual own stored binary32 delta. At Current100000000, Decrease1 returns1 without changing Current. Capture previous-minus-final Current AFTER clamp and BEFORE callbacks as return; retain callback semantics. In shared ValidateSpend reject positive amount when the candidate stored Current cannot decrease (including zero-effective precision), without state/event/timer changes; keep finite nonpositive no-op and existing failure precedence, do NOT require exact float equality for ordinary fractional costs. Add public zero-effective, actual representational delta and callback-mutated receipt regressions. No new double/int resource backend, no execution now. Update owned docs/receipt truthfully.
