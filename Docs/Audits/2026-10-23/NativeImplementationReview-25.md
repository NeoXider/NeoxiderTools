# Native implementation review 25 — Counter / Move corrections

Status: **COMPLETE — I19-01 and I15-01/02 source-closed; no additional confirmed correction. All execution UNEXECUTED.** Same independent native `gpt-6.1-sol` / `max` session. NI22 was completed before this assignment and remains untouched. Only this new report was written; no product/config/other-report edits, Unity/compiler/test/checker execution, services/process management, children or commits.

## Intake

Full latest source-final receipts read: Components `50e70de25cee43fa1b10137ac2476c7102fa00ce505330edb3a0c296510ba632` (170 lines), Move `41acb99e94a2b0c4c506081aef6f5582342edd406150a567eadeddb775ed89f3` (100 lines). Current `Local/Audits/assembly-2026-10-23/cli-liveness.json`, timestamp `2026-10-23T21:25:34Z`, SHA256 `1a4914b8ab69e9018ccdaa2951519995a1a31f5b208e42e2e27905e6b77f9071`, contained neither exact Components nor Move actor. Recorded original/recovery PIDs Components 98876/54036 and Move 36420/38068 were absent in the actual read-only snapshot at `2026-10-23T21:26:28Z`; both scopes were admitted.

Closing liveness file timestamp `2026-10-23T21:28:00Z`, SHA256 `3f9face8ad5bbd35b8ed902abfbf955b72d78aba1d0b5dd77df33efa7257b82e`, again contained neither owner. Actual recorded-PID enumeration finished at `2026-10-23T21:31:18Z` with all four absent. Reviewed receipts, central runtime, fixture and guide identities stayed stable. This is an owner/source gate, not a global source freeze. Writing Neo.Tools.Physics emitter work and Cards I21 are outside scope and were not read/approved as final. Move's unchanged PlayerController3DPhysics is the already-owned Move dependency, not that separate Physics assembly.

## Disposition / exact coverage

| Original owner/task | Bounded correction | Disposition |
|---|---|---|
| Components / SAME `neo-audit-1023-b06-05` | I19-01 remote finite payload/candidate admission | **Source-closed.** Checks precede budget/Int normalization; all requested denial/repair/ordinary controls are authored. |
| Move / SAME `neo-audit-1023-b07-07` | I15-01 surviving cursor after internal look restoration | **Source-closed.** Silent current-top write follows gate restoration, retaining exact gates/assigned owner and ordinary notifications. |
| Move / same actor | I15-02 CameraDrag case isolation | **Source-closed.** Only two initial SetUp resets; exact previous body/teardown is retained. |

Read full current Counter (589 lines) and CursorLockController (1053), complete Network fixture (718), Cursor Edit/Play fixtures (372/267) and CameraDrag (189), both changed guides, both full latest receipts, updated approved correction sections and NI19/I15 baseline findings/identity context. Every new parameter-expanded case/helper/cleanup path was read. Player gate/cursor setters, the shared memory provider and actual shared Play references were dependency context. No lower Extensions self-approval, other report rewrite or unrelated runtime re-audit is implied.

## Counter I19-01 closure

Current `Counter.cs:474–481` rejects local-only, NaN and either infinity at absolute Command ingress before the sender limiter. It does not inspect/reject the old balance, so finite absolute Set can repair NaN/infinite state. `CmdApplyOperation:485–513` rejects local-only and unknown operations, then nonfinite operands; it computes the **raw server float** Add/Multiply candidate at 498–500 and rejects nonfinite results before `RateLimitCheck(sender)` at 501. Only accepted candidates reach the existing arithmetic/Int rounding/mutation path. The default limiter path introduces no external notification between candidate validation and that arithmetic; arbitrary side effects from custom server configuration overrides are not a new promised transaction contract.

These changes remain entirely inside the received Command bodies. Public direct server/host/offline overloads, cooperative `requiresAuthority=false`, opcode values/signatures, division checks, normal Int rounding and prior mutation-revision publication fences are unchanged. There is no new clamp, storage backend, permission policy or public API. Reverse substitution of **only** the finite-ingress additions in the current source bytes reproduces the exact NI19 Counter SHA256 `88a947d160f242049555b73ef9fcd50043bb469b41806838c0798bd6999348d9`; this is read-only source identity evidence, not execution. The Counter guide accurately limits the new contract to received Commands and documents finite repair/no budget consumption/unchanged direct behavior.

All **47 new expanded Network cases** were read: six absolute nonfinite denials; twelve relative operand denials; eight positive/negative finite-input raw overflows across both modes; twelve corrupt-current relative denials followed by repair; six ordinary Add/Multiply/absolute finite and Int-rounding controls; one cross-command/per-peer budget control; two direct host/offline compatibility controls. The last two deliberately use public direct calls; the receive controls use actual registered woven Command delegates.

Every shared denial helper captures actual initial serialized state, delivers the registered receiver with its payload/sender, and requires unchanged stored value, separate RPC receiver, exact snapshot bytes including SyncVar, empty reactive/typed/repeat observations, persisted sentinel 77, **zero save writes**, zero matching RPC payloads/bytes and no sender window. It then accepts finite Set(3) from the SAME sender with no yield, asserting exact value/events/repeats/save writes/RPC sequence, decoded late-join state and established budget. The corrupt-current cases exercise finite absolute repair in both modes rather than merely rejecting invalid arithmetic. Raw-overflow Int cases are authored to expose validation placed after normalization; they do not depend on which integer a particular engine conversion returns for infinity.

Ordinary literal fractional results and rounding/repeat counts are explicit. The successful Add→immediate same-peer Set denial preserves the established budget/snapshot/RPC bytes, while two other senders succeed in order. Direct public controls preserve legacy NaN/overflow in Float mode, including save/notifications and host replication, with repeats disabled to avoid an unrelated giant loop. A bounded throwing repeat observer fails a broken implementation above 32 deliveries rather than allowing its invalid large repeat loop to hang the runner.

The probe uses the existing internal memory provider in the SAME test assembly. Its actual SetFloat writes storage and emits OnKeyChanged on every call, even an identical write, so zero-save assertions detect equal writes. It installs a GUID-owned key/sentinel before counting, removes its static key observer, disables counter saves, deletes only that key and restores the exact previous provider in finally, including constructor failure. The enclosing fixture refuses a foreign Mirror session/manager, captures outgoing serialized RPC messages and invokes the actual RPC receiver on a distinct object, then stops only its owned host, removes diagnostics, destroys owned objects and restores transport. These are source-authored cleanup guarantees; isolation has not been executed.

Prior 46 authored Components cases remain: unchanged Numeric/Binding and original Scene/Typewriter identities are retained, and all nine old Network bodies/assertions remain in the full current read. Current inventory is **93 authored cases**, including 56 conditional Mirror cases; this is not runner discovery. Handler invocation, real initial serialization and distinct receiver state are meaningful receive-body evidence, **not independent remote transport proof**. The supplemental dedicated-server/two-client owner retains that acceptance; NI25 does not review or approve its pending harness source.

## Move I15-01 closure

Current `CursorLockController.cs:623–657` factors the existing mode-specific Cursor writes into private `WriteCursorState(bool)`; ordinary ApplyCursorState still checks the global switch, synchronizes players and sends its normal lock/unlock notification. The factored helper itself performs only the requested Cursor writes.

At `ReleasePlayerGate:893–906`, the last owner is removed and its captured gate baseline restored through the unchanged player setter. When restoring a true look baseline under active global cursor management, the current surviving top controller silently writes its requested cursor state. That happens after `PlayerController3DPhysics.SetLookEnabled(true)` may auto-lock through a disabled assigned external owner. It does not call ApplyCursorState, SyncPlayersToCursor, ResolveDrivenPlayers or BindPlayer: no duplicate event, new gate acquisition/baseline or stolen assigned binding. No surviving owner means no silent write, preserving ordinary standalone SetLookEnabled behavior. Restoring an initially false look gate does not invent an enable or cursor request.

ReleaseControl removes the departing owner from the stack before restoring gates; when a TOP owner closes, the existing lower-controller ApplyCursorState activation/event remains. Native OnDisable/OnDestroy, explicit DisableController, repeated cleanup, unregister/hide and destroyed-player accounting retain their original shared-gate paths. Public signatures, serialized cursor modes/defaults and separate look/movement snapshots remain. PlayerController3DPhysics itself is byte-identical to NI15; default CursorControlEnabled remains true, rather than being disabled to hide the defect. The updated guide accurately separates silent internal repair from ordinary lower activation and standalone auto-lock.

All **21 added Edit cases** are meaningful: 16 combinations of both owner directions, both closure orders and all four initial look/movement values create a NEW actual-default-enabled player, assign external look-only A and independent movement-only B, and require visible/unlocked cursor while any owner survives. They inspect exact public gates, ownership, assigned reference, repeated cleanup and precise unlock counts. Four single-owner baseline cases and one unbound public SetLookEnabled control preserve ordinary behavior. The first 13 gate-only cases still disable cursor control for their original accounting purpose; the new helper explicitly asserts the separate new player's true default, so these controls do not inherit that blind spot.

The complete new two-method Play fixture authors **40 internal native scenarios**: 32 mixed cases across directions, release order, actual `enabled=false` versus deferred Object.Destroy and four baselines, plus eight single-owner controls. It configures an owned camera pivot, disables unrelated sensitivity/pause dependencies, leaves cursor control true and yields native activation/destruction. It requires the survivor's cursor/gates, retained exact external reference even across native destruction, no duplicate unlock notification, repeated cleanup, final standalone policy and restored first baselines. It never sends private lifecycle callbacks. Each scenario retires its own hierarchy; teardown waits for owned destruction before restoring captured cursor/global values in finally. No native result is inferred until execution.

## Move I15-02 and retained scope

`Audit20261023NeoToolsMoveCameraDragTests.SetUp:19–20` resets `_starts` / `_ends` before creating the controller or listeners. Reversing **only those two added reset lines** reproduces exact NI15 fixture hash `ca4ff623a2590caddd04f06b485019400026a0e2dff6f179fa182ee2560775e2`: all 13 prior bodies, exact quaternion/count assertions and teardown are otherwise byte-identical. No runtime drag rewrite or replacement fixture was introduced. Whole-fixture execution remains necessary to demonstrate case isolation; selected fresh-process methods do not supply that result.

Other Move runtime CameraRotation/ScreenPositioner/PlayerController3DPhysics, Ground/Physics2D/Regression/ScreenDepth fixtures and the other four guides match NI15 hashes. Numeric, Binding, TextScore, camera source and unchanged Components guides match NI19; original Scene/Typewriter fixtures match the approved fingerprints. These identities preserve earlier source dispositions, not new native results. Current Move inventory is **95 Edit + two Play methods = 97 authored cases/methods** and 40 internal native scenarios, not a discovered/pass count.

Root's actual `Neo.Tests.Play.asmdef` contains both `Neo.Tools.Move` and `Neo.Tools.Text`, plus Components/Network/Mirror and the existing test support. The typed Play dependency is source-resolved, not a blocker or reason to change runtime. This reviewer edited no shared configuration. The new Play `.meta` is present with the receipt's GUID; GUID/import/package acceptance is still unexecuted.

## Compatibility / cost / remaining evidence

The Counter guard adds constant-time validation on received numeric mutations and reuses the existing limiter; no framework or per-frame search was introduced. The cursor repair performs one existing-stack lookup and mode-specific writes only on last-look release, without another player search/synchronization pass or new steady Update work. Existing configured stack/player work and source cost bounds are not an FPS/allocation measurement. Arbitrary external gate setters do not redefine first snapshots; the documented general base-intent API remains a proposal.

**No additional confirmed residual in the bounded NI25 scope.** I19-01/I15-01/I15-02 close at source level; no same-owner correction is requested by this report. All compiler/import, MIRROR-on/off/nonempty discovery, these cases together with retained suites, independent remote clients, actual default-enabled cursor/focus/native destruction, whole drag-fixture lifetime, input versions/backends, package/docs/metadata/GUID/UTF-8 and performance acceptance remain **UNEXECUTED**. No mock/private budget field, source count, identity comparison or historical pass is promoted to native/remote proof. Global execution waits for every source writer and root's freeze; no execution was requested/launched here.

## Source-read identity ledger

First table paths are relative to `Docs/Audits/2026-10-23/`; earlier reports remain unchanged historical baselines.

| Artifact | SHA256 |
|---|---|
| NativeImplementationReview-19.md | `a07ea96d16315607eadbe30e9c90d9c70fff12b871b58ef4313f2a1ca72ddaa1` |
| NativeImplementationReview-15.md | `87b3a2da107568e2f2d76007bb814cb3bb0f1e2d7e1ec4344c7770d384ee1748` |
| Neo.Tools.Components.implementation.md | `50e70de25cee43fa1b10137ac2476c7102fa00ce505330edb3a0c296510ba632` |
| Neo.Tools.Components.implementation-request.md | `d1712dd91d12898cc01fb8f8141ac06ef8a07cbe03700ecac0e027ac670f2b3a` |
| Neo.Tools.Move.implementation.md | `41acb99e94a2b0c4c506081aef6f5582342edd406150a567eadeddb775ed89f3` |
| Neo.Tools.Move.implementation-request.md | `46b0b92ad9a343c5380110cab4b8d4bf8215b8c59a4d8b119b080ad16f530fdc` |

Current changed paths below are relative to `Assets/Neoxider/`; bodies/guides are read in full. Metadata/asmdef rows are identity/visibility context, not execution.

| Current file | SHA256 |
|---|---|
| Scripts/Tools/Components/Counter.cs | `280faa4b24fb9babd4d51deefa67893d45c2052e41324d44c25ee1e648a7857f` |
| Tests/Play/Network/Audit20261023NeoToolsComponentsNetworkTests.cs | `657aac6b2658246c79067841576dbe319cf2ea651ee6c9a48fb154c20743f0d2` |
| Docs/Tools/Components/Counter.md | `801b31c2c5edb7a74bfcd309952192c03922aac6454813a26f41a00f80f5713c` |
| Scripts/Tools/Move/CursorLockController.cs | `320901b862670b24ec7884e4f944f49e46b8a9f673eeed2ca9f32aa2994f4b76` |
| Tests/Edit/Tools/Move/Audit20261023NeoToolsMoveCursorOwnershipTests.cs | `276879db286b48c614bc10bcfa16b8169c3da4d317df16d924cca22b8f5d03f6` |
| Tests/Play/Audit20261023NeoToolsMoveCursorOwnershipPlayTests.cs | `553a9397fa722dcbda72f6d7df859186aa9ed15a4227db3e7786ffbe4eaf2e9e` |
| Tests/Play/Audit20261023NeoToolsMoveCursorOwnershipPlayTests.cs.meta | `d6a623381d0ccb84f6f20327673f82d7a96d33365a553a5b1e67b6aac79f5a41` |
| Tests/Edit/Tools/Move/Audit20261023NeoToolsMoveCameraDragTests.cs | `22dc1869e3dd80d366fb908d6cfcbf11ead6d7198cc568cd9f412ffafc12cbb1` |
| Docs/Tools/Move/CursorLockController.md | `3a8d534109d3f84b4e91d819a506e0e9afc1887d02d6b48db6f192452fee2d18` |
| Tests/Play/Neo.Tests.Play.asmdef (root-owned context) | `2518b0df939933514e55de55be5313226107a2382fe89d31b5361565aea50fe4` |

Unchanged identity rows are retained with their exact hashes in NI19/15. This review rechecked those identities rather than silently approving unrelated new changes; all earlier Input/View/Managers/UI and lower Extensions scopes keep their separate dispositions.
