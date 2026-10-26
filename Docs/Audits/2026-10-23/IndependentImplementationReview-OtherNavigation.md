# Independent implementation review: Other navigation

Status: **COMPLETE — one confirmed P2 source residual; scoped source approval withheld.**

Reviewer: SOURCE_REVIEW_ID_neo-verify-1024-other, an additional independent source reviewer. Original implementation owner: **neo-audit-1023-b07-08**. Target: demo / 11.0.0. Review date: 2026-10-24 Asia/Yekaterinburg; closing gate snapshot: 2026-10-23T21:50:02.574214+00:00.

Scope: the latest OTHER-NAV-DUPLICATE-SCHEDULER / OTHER-NAV-CALLBACK-GENERATION / OTHER-NAV-NOPATH implementation, its complete navigation regression fixture, and its two changed guides. This is source QA, with no release, compilation, native behavior, or test pass claim.

## Admission and current writer gate

Before admitting the implementation bodies, I read the actual root AGENTS.md, the complete implementation contract/request, the complete source-final receipt, and the current cli-liveness.json. Ancestor instruction discovery found no additional AGENTS.md on the assigned paths.

- The implementation receipt explicitly says source-final at line 3. Its four source-final artifact hashes match the raw current files.
- The exact original owner named by the request and manifest is neo-audit-1023-b07-08. No entry or string for that owner occurs in the current liveness snapshot. The snapshot lists only neo-verify-1024-other, neo-verify-1024-dialogue, and neo-verify-1024-parallax, each marked independent-source-reviewer. The Other reviewer entry is PID 48608 with session SOURCE_REVIEW_ID_neo-verify-1024-other. The original owner's recorded launch/recovery PIDs, 25588 and 43492, are also absent from that snapshot.
- The implementation-state entry still says source-running for Other; the manifest also retains an earlier approved-awaiting-launch value. These registry values disagree with the later source-final handoff. They do not establish an active writer. I did not modify them, query operating-system processes, or treat receipt/hash agreement as implementation approval.
- The gate remains globalReviewApproved=true, sourceFreeze=false, executionAllowed=false. Scoped owner absence permits this independent read; it does not establish global source freeze or authorize execution. Admission and closing raw source/receipt/liveness hashes are unchanged.

Root should return the bounded residual below to the **same original owner**, then recheck the changed source/fixture/receipt after that owner is again source-final and absent. This reviewer makes no implementation edit.

## Confirmed residual IOTHER-01 / P2: automatic refresh can starve every status observation

Exact source: Assets/Neoxider/Scripts/Tools/Other/AiNavigation.cs, Update at lines 355–358, UpdatePathfinding at 904–911, UpdatePath at 917–922, TrySubmitPath at 929–945, and CheckPathStatus at 976–979.

Public trigger: configure the valid Inspector Path Update Interval to 0.1 seconds, leave AutoUpdatePath enabled, use FollowTarget with TriggerDistance=0, and call SetTarget for a stationary reachable target on a ready agent. Sustain frames whose scaled Time.time increment exceeds that interval, for example 0.15 seconds per frame. Consider the ordinary accepted-submission path, with the adapter enabled/unpaused and each automatic native SetDestination returning true. The same interaction applies to an active Combined follow.

The source proof is independent of the native path result:

1. Update calls UpdatePathfinding before CheckPathStatus.
2. With the interval due each frame, UpdatePathfinding submits a refresh each frame, including for a stationary target.
3. Every successful TrySubmitPath writes requestSubmittedFrame = Time.frameCount, even for an unchanged destination and already resolved previous request.
4. CheckPathStatus then returns because requestSubmittedFrame equals that same frame. The next due frame repeats all four steps.

| Frame | Accepted automatic refresh | Subsequent status observation |
|---|---|---|
| n, interval due | Stores submission frame n | Returns at line 977 |
| n+1, interval due | Stores submission frame n+1 | Returns at line 977 |
| Every later due frame | Replaces the frame stamp again | Never reaches native status classification |

Observed-by-source consequence: throughout this accepted-refresh trace, no arrival, blockage, or path-status transition can be processed by CheckPathStatus. In particular, the adapter cannot publish an otherwise eligible arrival or update its reached/blocked state while this cadence continues. This is more than the intended one-frame deferral. It is not an assertion that Unity produced any particular missing/complete/partial path; the rejection occurs before those states are read, even if the native agent is ready and no longer pending. No callbacks or private-state fabrication are needed to cause the scheduling interaction.

Smallest bounded correction: allow the previous accepted request's settled state to be observed before that frame's optional automatic resubmission. A local Update ordering change, or a narrowly equivalent refresh deferral, can address this. Preserve initial-submission and pending-frame protection, accepted destination capture, generation checks after callbacks, scaled interval reservation before notifications, and the single automatic scheduler. Do not remove all submission-frame guards or introduce a navigation framework.

Meaningful acceptance for the original owner to author and the later operator to execute:

- A public SetTarget/native-agent scenario with an interval due on every observed frame. Record submission frame/time values and assert that the intended cadence actually occurred, so a test cannot pass by running ordinary faster frames. Use a bounded realtime deadline and restore any public timing controls it changes. Establish actual native readiness and an eligible settled arrival using native state and movement; assert one arrival and the public reached state while automatic refresh remains enabled. Do not use reflection, manual Update invocation, or mocked native flags.
- Ordinary control: retain the current single-initial-submission and minimum automatic spacing assertions, plus ordinary native arrival and moving-target arrival. A stationary already-arrived target must not repeatedly emit arrival merely because it refreshes.
- Negative controls: a fresh submission frame or genuinely pending request must still avoid premature arrival/blockage; idle/Stop/disable must remain silent; callback Stop/replacement must still win. Preserve the actual-path-before-ResetPath positive control and one blocked episode/recovery checks.

The current cadence test at fixture lines 78–123 checks submissions with a 0.2-second interval but does not establish an interval-due-every-frame trace or assert status/arrival in that trace. The principal missing-path tests explicitly disable automatic refresh. Their authored success criteria therefore do not close this interaction.

## Other scoped source conclusions

The duplicate immediate submission itself is corrected: SetTarget has one TrySubmitPath call and one accepted onPathUpdated publication at runtime lines 487–500. No PathUpdateRoutine or path-coroutine handle remains. UpdatePathfinding is the sole periodic follow scheduler; it reserves the interval before UpdatePath, and successful submissions also timestamp before callbacks. The AutoUpdatePath setter at 245–260 ignores unchanged assignments and performs one refresh on a false-to-true transition when eligible. The frame/status interaction above is the remaining integration defect, not a request to restore the removed coroutine.

Startup at 307–345 captures initialTarget, then preserves an accepted pre-Start request or a paused command. SetTarget and SetDestination finish their target/public/native movement mutations before onPathUpdated and do not resume stale mutation after it; SetDestination's final true acknowledges acceptance even when an observer stops/replaces it. That policy is documented and is not reopened here.

Accepted request identity/destination/frame tracking is private. Stop cancels active observation; disable increments the generation; Resume observes a retained native route/pending calculation or submits the retained target. CheckPathStatus excludes inactive/off-mesh/paused/stopped/idle/pending/submission-frame states, classifies missing accepted routes separately, and uses the captured destination. Its no-path proximity branch treats an already reached destination as arrival; partial endpoints remain blockage. These conclusions describe the branches, not observed native outcomes.

CheckPathStatus captures its request version and destination before arrival/blocked callbacks and rejects stale trailing status at 1017–1019. Combined start/stop-following continuations use the analogous version/active/paused guards at 1094–1099 and 1125–1130. Stop, accepted replacement, disable, Resume, and ReturnToPatrol invalidate old continuations. A Combined chase does not resubmit an already active sufficiently identical destination. No additional confirmed callback-generation residual was found within this assignment.

Both guides explain public C# and Inspector use, the single scheduler, captured destination events, observer Stop/replacement, native acceptance versus resolution, and independent NavMeshAgent lifetime. The Other README supplies the canonical backlink. I found no separate confirmed guide defect requiring a policy change; source acceptance remains withheld for IOTHER-01.

## Exact source read coverage

All line references below describe the raw current snapshot recorded in the hash table.

| File | Personally read coverage |
|---|---|
| AGENTS.md | Entire file, lines 1–121 |
| Docs/Audits/2026-10-23/IMPLEMENTATION-CONTRACT.md | Entire file, lines 1–65 |
| Docs/Audits/2026-10-23/Neo.Tools.Other.implementation-request.md | Entire file, lines 1–48, including the embedded full Other native-review contract |
| Docs/Audits/2026-10-23/Neo.Tools.Other.implementation.md | Entire file, lines 1–68 |
| Docs/Audits/2026-10-23/Neo.Tools.Other.md | Entire historical original audit, lines 1–103 |
| Docs/Audits/2026-10-23/Neo.Tools.Other.plan.md | Entire historical original plan, lines 1–67 |
| Docs/Audits/2026-10-23/NativeReview-A.md | Relevant complete Other section, lines 521–538; not a full read of other modules in that report |
| Assets/Neoxider/Scripts/Tools/Other/AiNavigation.cs | Entire runtime, lines 1–1434, including all public setters/APIs, lifecycle, scheduler, status, Combined, patrol continuations and cancellation |
| Assets/Neoxider/Tests/Edit/Tools/Other/Audit20261023NeoToolsOtherNavigationTests.cs | Entire fixture, lines 1–693, all 19 cases, all helpers, setup, teardown and positive/negative oracles |
| Assets/Neoxider/Docs/Tools/Other/AiNavigation.md | Entire guide, lines 1–83 |
| Assets/Neoxider/Docs/Tools/Other/README.md | Entire guide, lines 1–16 |
| Local/Audits/assembly-2026-10-23/cli-liveness.json | Entire current snapshot, admission and close |
| Docs/Audits/2026-10-23/manifest.json | Exact original Other owner entry context; ownership lookup, not a review of all manifest modules |
| Docs/Audits/2026-10-23/implementation-state.json | Decoded gates and exact Other owner/action entry; other owners are outside this review |

Also read every current AiNavigation working-tree diff hunk against HEAD (755 diff lines, in bounded slices) and both guide diffs. The navigation fixture is untracked in this working tree; its entire addition was reviewed as a full file. The aggregate runtime diff includes the earlier coherent audit corrections; those bodies were read for interactions without expanding the assignment into a new broad navigation design. UTF-8 text was read explicitly. No apparent console encoding issue was used to rewrite an input.

### All 19 current regression cases and their oracles

These are source observations only; every case is **UNEXECUTED**.

| Case and declaration line | Oracle read |
|---|---|
| AllNullLoopingRoute_DoesNotStartOrRecurse, 61 | Invalid route stays unstarted, not moving, with no started event |
| FollowTarget_SubmitsOnce_AndUsesOneAutomaticInterval, 78 | One initial event; minimum spacing; automatic off/on; Stop silence |
| FollowTarget_DisableEnable_ResumesOnlyTheUpdateScheduler, 126 | Disabled silence; exactly one resumed refresh; retained target |
| SetTarget_PathUpdatedListenerStop_LeavesAgentAndPublicStateStopped, 152 | Public stopped/cleared/reached state and native isStopped; no later submission/arrival |
| PathUpdatedReplacementThenStop_PreservesTheLatestCommand, 181 | Nested replacement then Stop; accepted replacement destination retained; no stale restart |
| AcceptedDestination_NativeResetPath_ReportsOneBlockedDestinationAndRecovers, 216 | Actual native path prerequisite; ResetPath clears path on ready nonpending agent; captured blocked destination, one invalid episode, fresh-request native arrival/recovery |
| BlockedListenerReplacement_SuppressesTheOldInvalidStatus, 283 | Actual path before ResetPath; callback replacement accepted; old invalid status suppressed; new destination/movement/public state |
| IdleAndStoppedNativeResetPath_DoNotReportBlocked, 328 | Idle Resume/ResetPath and accepted-then-stopped ResetPath emit no false blockage |
| AlreadyAtAcceptedDestination_DoesNotReportNoPathBlocked, 356 | Accepted current native position arrives once without blockage |
| CombinedMode_StartFollowingListenerStop_CancelsTheTransition, 381 | Real Combined transition callback runs; callback Stop leaves public/native stopped state |
| MovementRequests_AfterStop_ResumeAgent_AndDisabledAgentRejectsWarp, 408 | Positional/target commands restart stopped native agent; target preserved on mode change; disabled readiness/warp queries |
| PositionDestination_EmitsPathAndArrival_AndReportsReachedState, 441 | One accepted path event, one captured positional arrival, public reached state and Stop reset |
| PartialPositionPath_ReportsBlockedWithoutSuccessfulArrival, 477 | Disconnected native islands produce a partial endpoint; blocked once without arrival |
| MovingFollowTarget_ReopensArrivalState_AndEmitsAnotherArrival, 507 | Native first arrival, moved target, second arrival, actual final position |
| StopPatrol_DuringWait_CancelsDelayedMovement, 537 | Shared real-wait cancellation oracle; patrol cleared |
| Stop_DuringWait_RemainsPausedUntilANewRequest, 543 | Shared real-wait cancellation oracle; retained patrol resumes to next point |
| Disable_DuringWait_CancelsDelayedMovement, 549 | Shared real-wait cancellation oracle; no disabled delayed movement |
| ClearingWaypoints_DuringWait_StopsTheRoute, 555 | Shared real-wait cancellation oracle; route cleared/stopped |
| RouteWithNullWaypoints_SkipsToAValidWaypoint, 561 | Bounded valid-index selection, public movement and native destination |

Helpers and cleanup were read in full: VerifyPatrolCancellation at 578–620 requires a real waypoint event before interruption; CreateNavigation at 622–641 authors an inactive object and then activates it; WaitForNativePath at 643–655 requires on-mesh, nonpending, actual hasPath and remaining distance outside stopping range; BuildTestNavMesh at 665–690 builds and owns its data/instance, with disconnected islands when requested. SerializedObject wrappers at 83–87 and 586–590 use disposal scopes ending before waits.

TearDown at 31–58 restores captured timeScale, destroys only recorded temporary objects, removes only the owned NavMeshDataInstance, destroys its NavMeshData, and exits Play Mode when entered. No global RemoveAllNavMeshData call or unsupported NonParallelizable attribute is present. Coroutine observation loops have explicit deadlines; the first cadence loop uses scaled time with CreateNavigation setting timeScale=1, while native readiness/arrival/cancellation loops use realtime deadlines. Fixed waits are bounded. Their correctness in the actual runner remains unexecuted, including the required exclusive/shared-state runner contract.

### Native evidence debt, distinct from IOTHER-01

The ResetPath regression has a meaningful positive control: it first waits for a real native path outside stopping distance, then directly calls the native ResetPath API and asserts the ready/nonpending/no-path state. It checks a single blocked/invalid episode and ordinary arrival after a fresh accepted destination. Idle/explicit-Stop and already-at-destination controls protect against indiscriminate no-path classification. The replacement case checks that old invalid status cannot follow the callback's new accepted destination. These are useful authored oracles, not proof that the engine has produced those states.

Actual ResetPath behavior, complete/partial/no-path resolution, timing, event transcripts, repeated frames, native movement, Play Mode lifecycle and cleanup must still be observed by the coordinator's Unity operator after all source writers finish and execution is authorized. In particular, the already-at-destination test asserts the outcome but does not itself prove that the native agent chose the hasPath=false branch; record actual flags in the later native evidence. This is an evidence limitation, not a second demonstrated source bug.

Compile/discover/run the current fixtures and existing OtherToolsBehaviorTests on the approved Unity 6.3/6.6 hosts only after the gate opens. Other supported but unavailable versions remain unverified. Historical static/native reports do not pass this changed source.

## Ownership exclusions and actions performed

No fresh approval is offered for CameraShake.cs or its eight-case fixture, RevertAmount.cs, either SpineController branch/vendor integration, Neo.NPC, any other module, shared test internals/asmdefs, scenes/prefabs, sample layout, package configuration, trackers, or release/demo integration. Existing accepted documented navigation policies and optional procedural shake proposals are preserved. Historical original reports/plans/receipts were not rewritten.

Actual work was limited to source/instruction/receipt/reference reads, scoped read-only Git status/diffs, manual control-flow/oracle reasoning, and raw SHA-256 bookkeeping. One Python read command failed from shell quoting and was corrected; it did not load, compile, parse, validate, or run C# or product code. This report is the sole intended filesystem write. No process/service control, installation, commit/push, child agent, or message to another service was used.

| Execution category | Result |
|---|---|
| Unity Editor/MCP, import, Edit/Play Mode, demo/native acceptance | UNEXECUTED — prohibited in this phase |
| C# compiler, dotnet, syntax/product/package/UTF-8 validators | UNEXECUTED — prohibited in this phase |
| Test discovery, all 19 navigation cases, existing/other fixtures | UNEXECUTED — prohibited in this phase |
| Performance/runtime/visual evidence and supported-version compilation | UNEXECUTED — no source-only substitution |

## Raw source and receipt hashes

SHA-256 hashes below cover raw bytes, not normalized text. The four implementation files and the original-owner receipt were hashed before body review and rehashed at close; all are stable and match the handoff's artifact values. Contract/request/history/gate hashes are recorded as identity bookkeeping only. NativeReview-A and manifest hashes identify files from which only the explicitly scoped sections above were reviewed.

| Path | Raw SHA-256 |
|---|---|
| Assets/Neoxider/Scripts/Tools/Other/AiNavigation.cs | 93cbe119ee66f234de1518064d96c979a76cf583f27a64af1def5e3b25b46701 |
| Assets/Neoxider/Tests/Edit/Tools/Other/Audit20261023NeoToolsOtherNavigationTests.cs | 1b9694baf20206181689fe7090159135cd3f9c802919a6dca8b616a25fffd887 |
| Assets/Neoxider/Docs/Tools/Other/AiNavigation.md | 37ddb35a43428b67fe7996cd3fa5f643164089665cfe7b9a3d387404f32a6f24 |
| Assets/Neoxider/Docs/Tools/Other/README.md | 74c537ca786143096538349346b5955e493398bb7f319dc6da7dfb363522c8af |
| Docs/Audits/2026-10-23/Neo.Tools.Other.implementation.md | bb3322d11f9a4bd86126391e461c65821617cbe208e40c83568fd23b35ffb118 |
| AGENTS.md | 3a0c5c7a70719cba5d8d9c73b2ca162d7fb0ce96efd60ef926edbebc82bd9384 |
| Docs/Audits/2026-10-23/IMPLEMENTATION-CONTRACT.md | d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab |
| Docs/Audits/2026-10-23/Neo.Tools.Other.implementation-request.md | 4cb1fc5718308797b7966f95cb3d342af9c9878aaeadd94c47f5cbb26fbc8a44 |
| Docs/Audits/2026-10-23/Neo.Tools.Other.md | 05c699013341da234c0f944567c7e47f691beed7955b02d9b412dc673d68c3be |
| Docs/Audits/2026-10-23/Neo.Tools.Other.plan.md | ae64f90ae0ddcc9cdeead8233c57f86e96e0dd5b9551f4cee410867de912eac9 |
| Docs/Audits/2026-10-23/NativeReview-A.md | 564fefce32e51ae170cdf2712069331265989b50aee59beda1666852c2872ef6 |
| Docs/Audits/2026-10-23/manifest.json | b0976599ce7d259c46efbda407819edfd1703eabf9de37f6f2fd03a176fb0539 |
| Docs/Audits/2026-10-23/implementation-state.json | 36f349bbe37843f05ab32a3d0a06a1308fcef34a1a8551a9b7b12c6516e92fde |
| Local/Audits/assembly-2026-10-23/cli-liveness.json | c955a2a09fe66810a83ce86cfa784fa65a66700ae2b33e9a2956a58be00b1918 |

Final disposition: **source QA COMPLETE; IOTHER-01 requires the same original owner's bounded correction/recheck. Native acceptance remains UNEXECUTED. Current writer gate: original owner absent in the supplied current liveness snapshot; global sourceFreeze=false and executionAllowed=false.**
