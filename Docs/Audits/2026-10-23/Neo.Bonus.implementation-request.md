# Neo.Bonus: approved implementation request

Original actor: `neo-audit-1023-b02-04`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### BONUS-CLOCK-BOUNDARY (P1; approved-source-correction)

Bonus plan: TimerObject saves device UTC but CooldownReward checks trusted UTC; clock offset permits repeated sequential claims.

Root decision: Time routes all5 realtime read/write sites through protected virtual GetUtcNow default device UTC; Bonus override trusted UTC, preserve keys and unrelated Timer defaults.

### BONUS-ADVANCE-PRECISION (P2; approved-source-correction)

TimeReward private AdvanceLastClaimTime still multiplies claims*cooldown in float; delegate corrected existing extension.

### BONUS-BOX-COMMITTED-PRICE (P2; approved-source-correction)

C-09: Box GetPrize persists unlock then listener throws before progress decrement; commit actual grant and threshold before external notifications, no-prize preserves progress.

## Relevant full native sections

Source: NativeReview-A.md

### A-07 — P2: reward consumer retains the old float timestamp advancement

Owner: `Neo.Bonus`, original task `neo-audit-1023-b02-04` (cross-group consumer); Extensions supplies the already-correct helper. Current source: `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs:277–284`, private `AdvanceLastClaimTime`, called by TakeReward at `:323`; read SHA256 `8c74e4920b6a77f930588b4288d8a423f69ec335b0f2b651b8cfeeeef6186c5e`.

Trigger: 16,777,217 claims with a one-second cooldown. Expected timestamp advancement is 16,777,217 seconds. Actual source first multiplies int count by float cooldown, losing one second before AddSeconds. Large date advances can still escape the extension's new saturation path. The accumulated-count helper has **already** been delegated; do not repeat that completed correction.

Smallest repair: delegate only timestamp advancement to the existing `lastClaimUtc.AdvanceLastClaimTime(claimsGiven, cooldownSeconds)` and add a bounded public TakeReward/persisted-timestamp regression. Do not invoke millions of events merely to test large-count precision; the helper's large-count fixture already supplies that arithmetic case. Preserve save keys, event semantics and reward accounting. This is an independently confirmed source residual already identified by the Extensions plans; the Bonus audit itself is outside this review assignment.
Source: NativeReview-C.md

### C-08 / P1 — CooldownReward reads trusted time but its inherited timer persists device time

Current source: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/TimeReward/CooldownReward.cs:229–243` (`GetClaimableCount`), take/save at 277; `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs:397–529` (`SaveState`/`LoadState`, device UTC sites 423, 443, 448, 483, 529). Reproduction: trusted UTC is ten minutes ahead of device UTC, cooldown 60 seconds, accumulation enabled/cap one; a take writes device-now+60 through inherited SaveState, then a sequential second take compares this deadline with trusted now and is already eligible. The recursion guard has completed and cannot prevent repeated economic grants. Restore/start countdowns also cross clock domains. Expected: consume and restore use the same clock as eligibility.

Smallest repair: Time owner `neo-audit-1023-b08-05` adds a protected virtual UTC-now hook defaulting to device UTC and routes all five real-time save/load calculations through it; Bonus owner `neo-audit-1023-b02-04` overrides with NeoTrustedTime. Preserve plain TimerObject defaults and existing keys. Add ahead-clock sequential-claim/deadline/restore regressions plus the unchanged base-timer clock contract. The broad reward schedule/profile framework remains optional; it is not a prerequisite for this fix.
Source: NativeReview-C.md

### C-09 / P2 — Collection callback exceptions can grant another Box prize without another threshold

Current source: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/Collection/Box.cs:133–149` (`TakePrize`), `Collection.cs:194–203` (`TryAddItem`) and `GetPrize:160–168`. Two locked assigned items, progress exactly one positive threshold; attach a one-shot throwing OnItemAdded listener. GetPrize commits/persists item zero, then the exception exits Box before its progress subtraction. Its finally releases the guard; after catching/removing the listener a second take unlocks item one for the same threshold. Expected: an already committed grant consumes its price even when notification throws; no-prize still leaves progress unchanged. This is distinct from arbitrary callback rollback.

Smallest repair: a bounded grant/notification boundary or equivalent reservation that commits Box progress with the actual unlock before collection external notifications, preserving the no-prize path and documenting event order. Reuse existing collection selection/unlock logic, retain public APIs, and test item identity, persisted progress, exception propagation/guard release, and the second take. Owner **Neo.Bonus / `neo-audit-1023-b02-04`**. Existing reentry-only test does not cover exceptions; the root entry's consumption statement is true for timer rewards but should not imply this path is safe yet.
Source: NativeReview-C.md

### Neo.Bonus — reviewed

Full audit/plan, all sixteen changed C# hunks, relevant claim/start/restore, collection grant, reel-start/settle/cancel and complete roulette/wheel lifecycle bodies, all four new regression fixtures and all fourteen changed documentation pages read. Finite weighted sums in double, missing-cell non-wins, saturated slot prices/payouts, empty-catalog guards, reentry locks and cancelled coroutine ownership are locally coherent. Public/serialized names and index-based save keys are retained. The current plan correctly calls the trusted-clock seam mandatory before optional reward-core extraction; C-08 independently confirms it. C-09 remains after recursion fixes.

Fixtures use owned scene objects/assets, a saved/restored provider/trusted clock and GUID temporary save roots; Play fixture restores timeScale and destroys its hierarchy before provider restoration/deletion. Meaningful assertions observe balances, payment cardinality, plans/queued outcomes, callbacks, countdown startup and motion cancellation. Plain EditMode AddComponent/lifecycle-dependent reward initialization remains an execution check, not an inferred pass from source. No Unity or tests run here.

Aggregate work: roulette scans its image strip for wrapping/final selection only while spinning, wheel updates only active spin state, each active reel owns motion and settlement; these distinct geometries/results are not interchangeable physics contexts. Slots allocate matrices/column plans per accepted spin, not one global camera ray per target per frame. Collection view singleton retry and timer display cadence should stay bounded; proposed pure rules/clock service can reduce scene coupling but is not needed to repair C-08/C-09. No invented throughput/FPS claims.

Full artifact/source/test read fingerprints:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Bonus.md` | `32b89fdd46ef97e59ad7c2364246c7331abbb40e3a3e2d2daf73cc78907ae73f` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Bonus.plan.md` | `51a2195120f115759bcd099d4c55347c35f205174412fc7b13795f09033753bb` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/Collection/Box.cs` | `516de15ccd0160bb6d233b7fd752d65c20d73e65aee1ac7bf1ea0910550509e1` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/Collection/Collection.cs` | `7dc29a279300f0a2d10148f3db5f1b5979c6b037168f53585c82d16326fb7654` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/Collection/CollectionVisualManager.cs` | `a89f806ab4c179ae7ddb447bebe2dd315b0075001ef32c189f7066707cc805f2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/Collection/ItemCollection.cs` | `31e76fcb09b38254a7415e6e2c9562fbbcfcc0d29a2b48ee0699878e17ca2419` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/Collection/ItemCollectionInfo.cs` | `6bba6d2ef3054ce07b941f459391a73cf04be5d02344a1cd2d447debdea2d796` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/LineRoulett.cs` | `f42c588b2beb3a83a78b87b99e7f0b024c52b7cd179264400fde28e9dd8c50fc` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/Slot/CheckSpin.cs` | `d775bd681e48f8545119a814b7da1afdf4b3a3b5c3a6c7eadb031c4051e4a7d5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/Slot/Data/SpriteMultiplayerData.cs` | `9a569ce157baf5b670871ac257df10b0d0c041d3f60705c2c0272b187265af35` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/Slot/Row.cs` | `99c41762199dc86da0e980474ebe880e89466fe7499e070b2ddfc3e81f2ac410` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/Slot/SlotEconomyDefinition.cs` | `82e3d6632db05143a30661a3aff4c779dc8cf0082a6f5bb33e112383318d334a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/Slot/SlotElement.cs` | `9a261080fe77026457baebf7660ec46e0e577d48d0b3bba48337f71827759f6d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/Slot/SlotSymbolWeightOverrides.cs` | `0203056520df26a9a0dab40827ae94b7eb315bee8ab860a3fc7a1da428e8621c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/Slot/SpinController.cs` | `43d841496abbf3ab15a334c4da1e439fa63e3961493b64f4786370b9e2403123` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/TimeReward/CooldownReward.cs` | `1300f6302ecaabb776f0b6321f29fb8937566209565a51bb8fea5e1d7b67f2dd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs` | `8c74e4920b6a77f930588b4288d8a423f69ec335b0f2b651b8cfeeeef6186c5e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/WheelFortune/WheelFortune.cs` | `e89466eba925d8bf2bc135571db18873f888d1e52eb697912116b7081269ca09` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoBonusCollectionTests.cs` | `915d94225a7832059970ad0f4269ee9c6c60fcd0835241618d4c9d0a95537e57` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoBonusRewardTests.cs` | `07987b5d10fc6b948d86698ff8c59d8b61d33a69503bd62317d3bafbf22df021` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoBonusSlotTests.cs` | `26296620e2e2ac55c49a8e9517a361e59b902198f14f79e05a931c8cc4626c80` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Bonus/Audit20261023NeoBonusLifecycleTests.cs` | `ff529830979ede6323a0f5148c9bea246c89c87b54facb7f2eaaff997c867560` |

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Bonus.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Final NI7/NI8 integration handoff

ROOT cross-owner integration confirmation before sourcefinal: TimerObject now has protected virtual DateTime GetUtcNow(). CooldownReward MUST override it to return NeoTrustedTime.UtcNow, so eligibility and RealTime persistence share the trusted clock. Current no-override path remains concrete planned consumer debt. Add/retain real sequential claim/deadline/save/restore controls using the trusted hook; do not weaken deterministic clock fixtures to device UTC. Read NI8 and current primary Editor.Tests receipt for context, modify only your owned Bonus files/tests/docs. No execution.

## Root accepted NI16 Box mirror and zero effective price

ROOT personally FULL-read all117lines NativeImplementationReview-16.md. Editor scopedsourceapproved and your trustedUTCoverride closesNI10-01 source; SAME Bonus correct NI16-01/P2 progresssetter. Private monotonicwrite revision before externalSaveProvider.SetFloat, suppress obsolete outer Reactive/Visual publication after nestedwrite; preserve actualthresholdcost, independentnestedcredit, claimguard/manualAPI/eventorder. Actualpublic progress-key OnKeyChanged one-shot Take at20/threshold10 adds5 -> raw/saved/reactive15 and collectionobserver15; ordinary10, subsequent independentcommand and ABA/samevalue control ifneeded, handler removedfinally. ALSO root FULL-read196lineBox.cs and independent NI16reviewer confirmed additional positivezero-effective cost hole (read RootBoxPrecisionFinding.md): finitecurrent1e8 threshold1 CheckProgress admitsgrant while floatstoreddebit0. Add bounded shared positiveeffective candidate admission BEFORE Collection grant/readiness asappropriate, preserve zero-threshold legacybehavior, ordinary fractional/threshold5 stored8 (no exactcost==delta/newnumericbackend/journal). Meaningful usablecollection rejection(no grant/events/save/mirror mutation) +ordinary/rounded/zero positivecontrols. Fix BOTHinone resume, retain all current40authoredcases and oldacceptedpostcommit contracts. Source/regression/docs/receipt only, no execution/children/sharedconfig/foreignsource. Globalfreeze stillrequired.
