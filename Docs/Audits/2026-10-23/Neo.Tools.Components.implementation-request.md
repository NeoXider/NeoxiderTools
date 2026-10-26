# Neo.Tools.Components: approved implementation request

Original actor: `neo-audit-1023-b06-05`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### RPG-COMBAT-NAVIGATION (P2; approved-source-correction)

Link Rpg/Combat/README from canonical RPG and old Tools/Components/AttackSystem entry; create missing Combat.meta once with unique stable GUID.

### COMPONENTS-CAMERA-DOC (P3; approved-source-correction)

Sync sibling Tools/CameraAspectRatioScaler.md with render texture dimensions/projection, cache and editor preview opt-out.

### TEXT-SCORE-LIFECYCLE (P2; approved-source-correction)

TextScore derived callbacks hide updated private SetText callbacks; compose protected hooks/base calls and preserve reactive subscription ownership.

### COUNTER-REMOTE-OPERATIONS (P1; approved-source-correction)

Remote Add/multiply sends locally computed absolute value; concurrent clients lose increments. Assess minimal server operation dispatch preserving float-backed API; exact numeric backend optional.

### COUNTER-STALE-PUBLICATION (P1; approved-source-correction)

Set1 callback Set2 then outer1 publishes stale snapshot/RPC. Revision guard after callbacks preserves final2 across server/client/initial-state; retain typed event contract.

## Relevant full native sections

Source: NativeReview-A.md

### Neo.Tools.Components — source review complete; Counter and inherited lifecycle repairs required

Original owner: `neo-audit-1023-b06-05`. Full artifact reads:

- `Docs/Audits/2026-10-23/Neo.Tools.Components.md` — `4236da8ad3f0cdc4ab5afaa059e235fa732d5353959f6954320e0882ceaf79dd`.
- `Docs/Audits/2026-10-23/Neo.Tools.Components.plan.md` — `e451996bb23e982bdffa4fac7f1c431e86752e47143744695bbebf1fafa90df4`.

All eight changed source diffs, complete five new regression sources (Numeric, Scene, Typewriter, Binding, Network), changed ten guide diffs and relevant SetText/counter/async cleanup/camera paths were read. Test SHA256 values appear in the coverage receipt below. **Completed fixes appear locally correct** for arithmetic/registration, server snapshot startup/load and host RPC suppression, sender throttling, derived score/labels, callback generation checks/cleanup, wrapper startup/complete ordering, disposable score subscription, reversed float bounds, render-target projection/cache/editor gating and Play-only destruction loot. Tests inspect real object/state/events/serialized wire output. RNG is restored, unique save keys cleaned, target textures/objects destroyed and host transport restored. Host/snapshot round trips remain narrower than independent remote-client evidence; the late-binding Play test explicitly requires no preexisting score source. Synchronous typewriter tests cover zero-rounded delays, not awaited native update timing.

**A-26, P1, confirmed:** `Counter.SetValue:402–408` resumes after ApplyValueLocally callbacks; `CmdSetValue:442–444` also sends after callbacks. Set(1)'s Value listener Set(2) completes/broadcasts2, then the outer server path writes/sends obsolete1 (direct Set also rewinds `_syncValue`). Expected final local/remote/new-client snapshots agree on2. Minimal repair: revision/generation checks after callback boundaries before continuing stale replication/derived publication, in both direct server and Command paths. Define typed/repeat callback continuation while retaining ordinary event ordering; do not claim a global transaction. Add nested Value/typed handler cases and actual receiver/initial-snapshot state. Root independently accepted this P1.

**A-27, P1, confirmed:** `Counter.ApplyDelta:357–365`, `ApplyFactor:368–376`, `SetValue:394–397` calculate and send an absolute value from each remote client's stale local balance. Two clients both Add(1) to10 each submit11, so the server loses one increment. Expected sequential server operations produce12. Minimal repair: send existing CounterModifyOperation+operand, evaluate against current server state, retain per-peer limiter and public rounding/divide guards; absolute Set remains last-writer-wins. Shared-counter authorization is a separate explicit application policy, not an automatically established vulnerability. Root approved this bounded correction; exact-int models/save migration are optional future work.

**A-28, P1, confirmed integration defect:** `TextScore.OnEnable:36`, `OnDisable:44` hide the new private `SetText.OnEnable:29`, `OnDisable:37`; the inherited `_onEnableAnim` pause/resume contract is not composed. Disabling an animating TextScore can let its tween advance while disabled. Minimal cross-owner repair: original Text owner provides protected virtual lifecycle hooks, then Components overrides and calls base while preserving subscription cleanup. Retain existing base OnDestroy/SetLink cleanup. Add native disable-while-animating and reenable-to-current-score regression; source confirms hidden callbacks, native tween timing remains later evidence. This is already correctly called out by the author's plan and root.

Aggregate work: typewriter repeatedly materializes growing text strings (O(text length²) total copied characters), does per-character async/audio work and stores pause maps. Counter's optional repeat loop is synchronous O(rounded value), explicitly documented for bounded gameplay values; do not enable it for arbitrary balances by default. Camera's per-frame cache checks are O(1), projection recalculation only when applicable inputs differ. Late TextScore fallback can repeatedly search missing singleton while enabled; use explicit source for many labels. Public Counter.Registry is shared mutable lookup, now cleaned by captured-key lifecycle but not protected from external mutation. No universal pooling/math/typewriter framework is required for these fixes. Grapheme/TMP tokens, pure session and exact-numeric backend are **future proposals**. Rendered camera output, runtime loot teardown policy, native delays and real remote operations remain **needs evidence**.

Components regression read receipt:

- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsComponentsNumericTests.cs` — `d83b0aee6efffae618b3cccd30abc61a485429dc0eaa4849e4fe5e3cd4584860`.
- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsComponentsSceneTests.cs` — `61d0c24abaf77fe1a9022161a5dd4da7c2d214f021affd54dd6874e2d70e205f`.
- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsComponentsTypewriterTests.cs` — `01717ce350cdafc9e248c0d0eb661546a5190b55e27caa556634038df61a5751`.
- `Assets/Neoxider/Tests/Play/Audit20261023NeoToolsComponentsBindingTests.cs` — `a0228bb769c1a6d724ccd3cdb93f89773235e9a554af77f38c9ce00a92e47863`.
- `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoToolsComponentsNetworkTests.cs` — `dd7f0120c4ad736ebfe1c20b018abb01150696ea6ec409b6050d296b5d799336`.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Tools.Components.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Root accepted I19 remote Counter finite admission

ROOT personally FULL-read all128lines NativeImplementationReview-19.md andCounter225-530, accepts I19-01/P2. SAME Components owner guard ONLY remote Command ingress before senderbudget: CmdSetValue finiteabsoluteinput (ALLOWfiniteSet torepair priorNaNcurrent); CmdApplyOperation localonly+knownAdd/Multiply thenfiniteoperand+RAWserverfloatcandidate BEFORE Mathf.RoundToInt canhideInf. RejectNaN/±Inf andfiniteoperand rawoverflow beforestate/reactive/typed/repeat/save/SyncVar/RPC/budget. No clamp exists; no newclamp/integerbackend/authority/framework or globaldirectAPIpolicy. Preservecooperative requiresAuthority=false, normalInt rounding/division, directpublic server/host/offline legacy behavior. Meaningful actualwoven/registeredreceive tests allbadabsolute/relative/overflow+Int rawguard, unchangedallobservables/outgoingbytes/senderwindow, immediatevalid SAMEpeer positivecontrol, finiteabsolute repair, ordinaryfinite/int/perpeer controls. Keepprevious46cases/14phasecases. Root ALREADY added Neo.Tools.Text sharedPlayreference (andGridSystemseparately); no configedit/wait. Counterguide+receipt update, noexecution untilglobalfreeze. Extra-originalnetworkactor is extending genuine3processharness for these badinputs; do NOTeditits source or weakenbodytests.

## Snapshot3 actual COUNTER-NATIVE-WIRE-AND-UNITY-LIVENESS

ROOT has closed source3 execution after every owned native job stopped. SAME original actor only, gpt-6.1-sol/max. No Unity/compiler/checkers/tests until ALL source writers finish and root opens a NEW freeze. Root personally read all98 graphical Edit failure-message groups and all43 Play groups; Local/full-play-failure-triage.md (70 lines) and critical-failure-triage.md (35 lines) are independently reviewed. Actual XML paths are Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (7c6d24e1...) and21-full-play-graphics.xml (2b152164...). Read your exact cases/messages/stacks and actual runtime/vendor APIs before editing. CENTRAL RESERVATION: primary Neo.Editor.Tests b03-04 alone owns ALL Editor-only native helper moves/extractions, new Neo.Tests.Fixtures, shared test asmdefs, and its owned legacy test adaptations. Do NOT move/extract Mono helpers, edit shared asmdefs, or race any file in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. If the map is not yet present, work only the explicitly disjoint runtime/Play/new helper-free fixtures below; coordinate any caller containing embedded helpers before changing it. Preserve every substantive positive/negative/count/payload/ownership oracle and all public/serialized APIs/settings. Do not make production changes to hide a fixture precondition, add ExecuteAlways, blanket Ignore/error whitelists, manual Update/mocks as native evidence, reflection into private UserCode as receive proof, or weaker comparisons without actual native-baseline/numeric evidence. No other owners/vendor assets/314 migration targets, no children/commits/push; release Samples~ and protected DocsEn.meta untouched. Update own honest implementation receipt and canonical docs only if behavior/contract actually changes; report unresolved native capabilities explicitly. Stop all source writes when done. Own Play ComponentsNetworkTests first operation case Deliver676 leaves3 bytes because enum int fixed4 vs actual woven priorityVarInt. Correct exact fixture serializer (all manual fields), retain Remaining0 and cross-command shared-per-peer budget/security/state/event/save/RPC positives. 55 later SetUp39 failures compare destroyed Unity NetworkManager wrapper with NUnit CLR Is.Null. Use genuine Unity liveness singleton==null while preserving idle-server/client and LIVE foreign-manager rejection. No global reset/delete/takeover. This is DISTINCT from legacy62 LIVE-manager cascade (legacy owner handles it). Compose any owned Mirror graphs inactive before a single real Awake if actual identity preconditions need it. Only own disjoint Play fixture/receipt; no Counter production numeric/network policy changes.
