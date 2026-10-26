# Native implementation review 11 — Input / NoCode corrections

Status: **COMPLETE — NI4-01/02 and I5-02 source-closed; one bounded Input correction remains; execution UNEXECUTED**. Independent source-only review, 2026-10-24, same native `gpt-6.1-sol` / `max` session. Only this report was written. No product/other-report edits, compiler/tests/Unity/checkers, children, services or commits.

## Source-final / process gate

Read both complete latest receipts first. Input declares source-final including NI4-01/02, no outstanding dependency; NoCode declares source-final including I5-02. Intake SHA256: Input `f5b13ff256d2ed153572d0e5cabd8a2782eb0da2df2584ff64168bda77979e07`; NoCode `08b3540699f83711225bde38dfc6963b636d4a890ded3ba942ed4ca14102ca08`.

The shared implementation-state file still labels these owners source-running; that historical label is not accepted as current process evidence. Read-only tasklist snapshots at intake and completion found all recorded initial/recovery PIDs absent: Input 26008/33712; NoCode 104424/71960. Root was told this distinction. Both receipts and all three critical corrected runtime files retained the same hashes at completion; no moving assigned source scope was accepted. These are observations of the recorded processes and reviewed files, not exhaustive proof that no unrelated writer exists. Root's source-final handoff plus the receipts establishes the scoped review gate; global `sourceFreeze` remains false and `executionAllowed` false. No execution permission is inferred.

## Dispositions and exact correction scope

| Original owner | Current correction scope read | Disposition |
| --- | --- | --- |
| Neo.Tools.Input / SAME `neo-audit-1023-b07-02` | Complete 624-line MouseInputManager, complete 483-line MouseEffect, complete retained 451-line behavior fixture and new 596-line dispatch partial; complete two changed public guides; complete latest receipt/request/NI4 and current baseline-relative source/doc deltas | Named NI4-01/P1 publisher and NI4-02/P2 disabled-subscriber defects are source-closed. Immediate unsubscribe reference release and warmed snapshot reuse are implemented without changing public event signatures. I11-01/P2 below remains on Release observer fault. |
| Neo.NoCode / SAME `neo-audit-1023-b04-06` | Complete current 407-line NoCodeFormattedText, NoCodeBindText, all three complete audit fixtures (follow-up 463, binding 419, occurrence 185 lines), four complete owned guides, latest full receipt/request/NI5 and relevant runtime/doc deltas | I5-02/P2 source-closed: rendering and semantic notification baseline are separated. Earlier occurrence/fallback runtime hashes remain identical to I5. No further confirmed defect in this bounded correction. |

No compiler/discovery/test, GC control, real device/frame scheduling or performance pass is implied. The root-confirmed I11-01 handoff retains synchronous exception propagation and old-tail version suppression; only this gesture's incomplete Release ownership must remain cancelable. No public subscriber-isolation framework is requested.

## I11-01 — P2: a Release observer fault discards unfinished visual ownership before later cancellation

**Original owner/task:** SAME Neo.Tools.Input / `neo-audit-1023-b07-02`. **Current source:** `Assets/Neoxider/Scripts/Tools/Input/MouseInputManager.cs`, `ProcessMouseState` lines 159–197, especially `_releasePending = _pressed` / `ClearPressedState` at 164–165 and unconditional current-version pending clear in `finally` at 191–196; `CancelPressedState` lines 207–222. Consumer: `MouseEffect.OnRelease` lines 358–382 and `OnCanceled` lines 353–355.

**Concrete trigger and state trace, source-confirmed / UNEXECUTED:**

1. Register a throwing ordinary `OnRelease` observer before enabling/subscribing a MouseEffect. Use ordinary enabled Press/Release, a real valid press, and the effect's normal follower/trail. The successful Press leaves manager `_pressed = true` and the effect following.
2. Release over the target. The manager computes `clicked`, sets `_releasePending = true`, then clears `_pressed` and `_pressedObj` before observers. The earlier Release observer throws before the later captured MouseEffect callback executes. Propagation correctly stops that batch and prevents ReleaseIn/Click.
3. The outer `finally` still has the same version, so line 195 sets `_releasePending = false` despite unfinished Release notification/visual cleanup. The effect never received Release and remains following.
4. Remove the faulty observer, then disable **only the manager**, leaving the effect enabled. `OnDisable → CancelPressedState` now sees `_pressed == false` and `_releasePending == false`, emits no OnCanceled and cannot stop the outstanding effect. The new pending-release mechanism has lost the resource ownership it was introduced to retain. Subsequent effect Update can continue following; manager-only disable does not deliver the effect's own focus/disable hooks.

**Expected:** preserve the documented synchronous exception and stop the failed publication, while retaining that current gesture's unfinished visual ownership for later lifecycle cancellation. Normal successful Release clears pending ownership. A replacement Press owns its new state and cannot be erased by any old Release finally/tail. This does not require continuing past a faulty subscriber or synthesizing Release/Click.

**Minimal correction accepted by root:** consume this old Release's pending ownership only after successful completion of the relevant Release phases; do not erase it merely because a throwing old call unwinds. Preserve the existing version guard so a nested replacement's state remains authoritative. Later disable/camera/lifecycle cancellation, after the faulty observer is removed, must publish one OnCanceled and stop the current visual ownership. Keep the exception visible to the original caller. No general exception isolation, public event replacement or resource/session framework is needed.

**Required meaningful acceptance, UNEXECUTED:** earlier throwing Release observer + later subscribed effect; real Press establishes active follower/trail/start; release throws the same exception, leaves later Release/In/Click counters at zero, and removing the fault followed by manager-only disable produces one cancellation/stop and hides visuals. Repeat cancellation must not duplicate cleanup. Preserve a successful normal Release positive control with one stop/no cancellation and retain existing replacement/reentry cases asserting the replacement hit/position, next Hold/Release/Click and exact start/stop counts. The current exception regression at dispatch partial lines 457–478 exercises **Press**, where `_pressed` remains set; it cannot prove this Release unwind case.

This is a bounded remaining ownership defect, not a request to change the deliberate subscriber exception propagation contract. Root accepted this source finding during review; the SAME Input actor must implement and receipt it. No correction was made by this reviewer.

## Input source closure: event admission, snapshot ownership and compatibility

### NI4-01 publisher/version fencing

`ProcessMouseState` rejects inactive/destroyed managers and missing-camera work, ignores samples with no eligible phase, and versions each accepted operation. `Dispatch` / `DispatchIn` check the version and current native enabled state before capture, **between each subscriber**, after the batch and before the next event/phase. Earlier OnPress → manager disable therefore cancels before a later effect can start. Disable → re-enable inside the callback cannot revive the old dispatch because the cancellation changed its version.

Press/Hold/Release/Click receive a captured value payload; a newer accepted sample can update the polling state and supersede the old operation without the old one later clearing it. Cancellation clears press and pending state before notification. Its phase permits cleanup while the manager is disabled/destroying, but still rejects an obsolete version. Repeated cancellation with no new owned press inside that same Cancel batch returns without advancing its version, so it does not accidentally skip later cleanup subscribers. A new Press started from an earlier OnCanceled/OnRelease callback gets a new version and survives the obsolete old tail. The finally blocks restore their enclosing dispatch phase; the pending-state finally is version-guarded but has the separate exception flaw I11-01.

For uninterrupted samples, existing independent mode switches and ordinary-then-in ordering remain. Press disabled still records the press for enabled Hold/Click. Release without observed Press remains an intentional ordinary/In event with no Click or cancellation. Click still requires the same non-null hit GameObject. Picking remains closest 3D first, then nearest 2D only when no 3D hit; masks, fallback depth and per-phase native query behavior were not silently changed. No unconditional same-frame physics cache was introduced.

### Warm reuse and immediate unsubscribe release

All nine public events retain their names/delegate types. Their explicit accessors use the private `SubscriberSnapshot<T>` at lines 514–558. Effective Add/Remove changes commit the current combined delegate and immediately set `_snapshot = null` under a per-event lock; null no-ops and removing an absent registration do not force a rebuild. Delegate.Combine/Remove retain order, duplicates and last-match removal semantics. No old combined-delegate field is retained solely to compare on the next event.

Capture builds GetInvocationList only when that event's snapshot is absent. An unchanged event reuses its immutable Delegate array; there is no GetInvocationList in the per-subscriber or per-Hold loop. An active dispatch keeps its **local** array even if a callback unsubscribes/adds another handler; accessors do not clear/mutate that array. Once it returns, the publisher's idle cache does not keep removed targets. Retaining the local array until active invocation completes is required snapshot ownership, not an idle unsubscribe leak.

Each ordinary and `in` event captures at its own start, not together before ordinary notification. An In handler added during ordinary Press can join that sample's separately captured PressIn; an ordinary handler removed during its already-running batch still receives that batch unless dispatch itself was superseded. Locks cover subscription/cache ownership only; callback invocation is outside the lock. This is a private implementation helper, not a new public framework or a promise that Unity input/physics dispatch is safe from arbitrary worker threads.

### NI4-02 effect entry and retained earlier guards

OnHold lines 434–437 and OnClick lines 421–424 now reject destroyed/disabled/noninteractable effects at entry. Thus an ordinary captured delegate removed by an earlier listener's effect disable cannot instantiate a new effect afterward. Previously spawned instances retain their existing lifetime. OnPress/OnRelease have matching native lifetime admission; first-Hold and repeat timestamps remain committed before spawn callbacks. Existing generation checks still fence continuation after follower activation/onSpawn and preserve a newer independent gesture. No generated instances are destroyed as an implicit cancellation side effect.

These source closures concern the named admission/continuation defects. They do not turn ordinary C# subscriber exceptions into isolated notifications, nor promise delivery of every cleanup observer after an exception; I11-01 specifically preserves a later supported cancellation opportunity without changing propagation.

## NoCode I5-02 source closure: render destination versus semantic baseline

`NoCodeFormattedText.ReconcileDirtyConfiguration` lines 122–133 no longer erases `_lastText`; it invalidates binding resolution, marks private nonserialized `_forceRender` and reconciles subscriptions. OnValidate still only clamps interval and marks the volatile dirty flag. Reconciliation still occurs before the LateUpdate polling gate, preserving the earlier ordinary/unresolved Reactive fallback fix and avoiding source getters from validation itself.

`RefreshFromSource` computes the formatted string and compares it with `_lastText` at line 365. An unchanged result is skipped only when neither a new target nor a pending render needs writing. It commits `_lastText` and consumes `_forceRender` **before** external target/event callbacks, then passes the captured `textChanged` decision to ApplyText. A new/changed target or equivalent format/interval edit can therefore receive the current string without an extra OnTextChanged. A different source/format output emits once; repeated identical refreshes remain quiet. No force-render state is cleared after an external callback returns and potentially queues new work.

OnEnable deliberately retains `_lastText = null`, providing one initial publication for the new enable period. Once does not gain periodic getter reads or redraw: its dirty pending render waits for explicit refresh/re-enable. Poll In Late Update=false still suppresses periodic output, while explicit refresh is allowed. Existing target priority, invariant string.Format, missing binding → zero and literal invalid format behavior are unchanged. This is a render/notification distinction, not an event/session extraction or a change to the existing public/serialized API.

ComponentFloatBinding and NoCodeFloatBindingBehaviour hashes match I5's fully read source exactly. Their occurrence/default0/explicit invalid rejection, matching-type component order, explicit composition invalidation and final post-ApplyFloat subscription/fallback behavior are not reopened or silently modified by I5-02. NoCodeBindText remains byte-identical to the original request fingerprint. Full current relevant docs and complete current occurrence fixture were nevertheless read for continuity. The Editor drawer/Condition occurrence consumer remains another owner's scope; no pending consumer is approved by this report.

## Authored acceptance matrix and positive controls — all UNEXECUTED

### Input

Read every body/helper of both current behavior partials. The new dispatch partial declares **32 cases** by source; it preserves the existing main fixture's earlier cases and first implementation increment. Source enumeration is not discovered/passed tests.

| Authored case family | Meaningful observed state / positive control | Evidence boundary |
| --- | --- | --- |
| Earlier Press disables manager, including immediate re-enable (2 cases, lines 13–70) | No late effect start, ordinary/In/Hold/Release/Click; one cancellation, hidden visuals; later valid press+release gives exact normal counts | Internal real ProcessMouseState and native enabled hooks, not real mouse-device sampling/frame cadence |
| Earlier Cancel/Release starts replacement (2 cases, lines 72–142) | Two starts, zero old-tail stops, exact new collider/screen/world/follower position; later replacement Hold/Release/ordinary+In Click and one stop | Actual ray hits/objects; no copied recognizer or merely private-version assertion |
| Repeated cancel / disable during unfinished ordinary or In Release (3 cases, lines 144–220) | Cleanup exactly once, no stale click; later valid gesture succeeds | Normal disable path covered; throwing Release path is missing I11-01 |
| Replacement within each of PressIn/HoldIn/ReleaseIn/ClickIn (4 cases, lines 222–279) | Old target absent from obsolete late In callbacks; new target/polling/follower state; preserved legitimate prior Click and exact stop totals | Covers event-local snapshot timing/old-tail suppression through actual dispatch |
| Five mode combinations + release without press (6 cases, lines 281–323) | Exact ordinary/In transcript, hit/screen/world payload, independent switches; Release/In without Click/cancel | Positive compatibility controls retained |
| Subscription mutation (line 326) / duplicate and null operations (line 481) | Active A,B batch survives B removal, later A,C; separately captured In includes new I2; duplicate removal removes one and null operations reuse array | Not a mutable shared-array approximation |
| Captured Hold/Click disables only effect (2 cases, lines 353–408) | No second spawn while disabled, owned prior instance survives, re-enable valid gesture gives exact two instances/start-stop totals/new position | Currently enabled/lifetime admission is tested; no claim of every arbitrary lifecycle/callback combination |
| Warm ordinary/In Hold (line 411) | Same cached array identities across 32 later native-backed samples; exact 33 callback counts and real target | Demonstrates intended cache reuse if run, not total-allocation/GC or frame-time benchmark |
| Immediate release of unsubscribed target (9 event-specific cases, lines 436–454, helper 549–594) | Real warm callbacks for each event; accessor immediately nulls cache; plain managed probe must collect without another publication; NoInlining helper avoids keeping its strong reference in the test's live frame | GC controls UNEXECUTED; engine/runtime GC behavior must actually be observed, not inferred from the private null alone |
| Subscriber exception (line 457) | Press fault propagates, stops late ordinary/In, later cancellation and valid input recover | Does not cover Release fault/pending ownership; retain and extend rather than weakening this control |

The retained main fixture still contains manager disable/camera/focus/pause/destruction cancellation, no synthetic Release/Click/spawn, ordinary release, onSpawn disable, onStart disable, nested replacement and real follower OnEnable-hook interruption controls. Its helpers create owned objects, use real colliders/rays and native enabled state, restore auto-create and reset polling. It requires the explicit isolated/exclusive Play scene/singleton prerequisite and does not claim to preserve an arbitrary existing live manager/gesture. Spawned children are owned by the effect and removed with its hierarchy; pre-existing external objects are not destroyed.

The backend-selection/UI fixtures, visual-hook source, backend/mouse/key/swipe runtime, other three Input guides and Input asmdef hashes match NI4. Their prior source disposition is carried forward by byte identity; this is not a claim of re-running them. Simulated initial backend rejection, fake adapter touch controls, absent-mouse skips and optional reflection allocation limits remain as stated in NI4. Root's UnityEngine.UI reference is present. Existing Input friend declarations include Neo.Tests.Play and Neo.Editor.Tests, so the internal ProcessMouseState seam is admitted by current source references; compilation remains unexecuted.

### NoCode

Read all **52 authored current cases** across the 21-case follow-up, 20-case binding and 11-case occurrence fixtures. The follow-up retains all prior source/getter/polling/Once/Slider assertions and adds six declared I5-02 cases; changed old assertions now also count notifications.

| Authored case family | Actual public/output controls | Evidence boundary |
| --- | --- | --- |
| Interval validation, ordinary Reactive/Poll (4 cases) | Initial 0.25, unchanged interval refresh emits zero; later 0.75 displays and emits exactly one formatted event; repeated refresh stays one | Both wrappers exercised; ordinary BindText has no new formatted event API |
| Getter deferral / Once / disabled periodic polling / reactive-to-plain / mode-to-Once | Getter count unchanged during validation; appropriate manual/due refresh; old reactive listener removed; later changed value updates exactly once | Reflection invokes actual OnValidate/LateUpdate and sets deadline due; not native real-time scheduling |
| Target rebind Reactive/Poll (2 added cases, lines 234–263) | New target gets unchanged 0.25 with zero events, old target stays 0.25; later 0.75 reaches new target only and emits one | A quiet no-write implementation fails the new target assertion |
| Equivalent/different format Reactive/Poll (2 added cases, lines 265–296) | Equivalent output stays silent; different literal+format output emits once; repeated refresh silent; changed source emits a second event | Distinguishes semantic output from raw configuration changes |
| Once target rebind (1 added case, lines 298–330) | Due LateUpdate does not read/repaint; manual same-string render writes new target with zero events; changed source/manual refresh emits once | Preserves nonpolling contract, rather than fixing duplicates by introducing a new poll |
| Enable publication (1 added case, lines 332–350) | Explicit enable hook gives one initial event, repeated refresh quiet, later changed value gives second | Reflection lifecycle seam, explicitly not native disable/re-enable evidence |
| Late/destroyed target current binding fixture (lines 320–357) | New/recreated TMP receives unchanged text with zero duplicate event; later reactive 0.75 gives one event | Retains output and notification positive controls |
| Existing source/lifetime/occurrence cases | Listener transfer/release, disabled captured notification rejection, component 0/1/2 and invalid selectors, JsonUtility legacy default/roundtrip, real Slider rewire then normalized 0.75 | Actual C# binding/UI objects; native component order/serialization/IL2CPP still needs execution |

Fixtures own/destroy only their created objects in reverse, with null/destroyed guards. SerializedObject configuration is an authoring seam; assertions observe text, getter/listener counts and Slider output. No new unsupported NonParallelizable attribute was added. Real cadence with timeScale0, native parent deactivation/re-enable, authored component selection in the Editor and stripping remain unexecuted; field reflection is not a replacement for those checks.

## Documentation, ownership and aggregate work

Read the complete changed Input manager/effect guides and all four NoCode guides. Public event order/snapshot timing, immediate idle reference release, disable/replacement ownership and render-versus-notification descriptions match the named corrections. The pending-release text is too broad for the current fault unwind until I11-01 is repaired. No guide claims subscriber fault isolation. The NoCode enable-period baseline and Once pending redraw are documented explicitly.

All follow-up changes stay within the original two modules' runtime, owned docs and prefixed tests/metadata. Input's private event helper retains the existing public nine events; NoCode adds only a private nonserialized pending flag for this correction. No new service/controller/registration, serialized migration, scene/prefab/sample/settings/package change is needed. Root owns shared asmdef/integration changes; this reviewer read references but changed none and does not approve their entire unrelated dependency closure. Text/Extensions/Core/Editor/Condition consumers are separate scopes.

Aggregate work remains explicit: stable event subscriptions avoid fresh invocation arrays, while each event phase can still build physics data; per-event capture locks, subscribers, optional backend reflection/boxing, effect spawning/following and UI work retain costs. No across-phase ray cache was introduced because callbacks can change the physical context. NoCode still reads values through its existing reflected binding and formats strings at its unscaled refresh gate; duplicate source slots, changed binding reconciliation and force redraw can perform work. No global watchdog/new per-frame scene search, hard total allocation bound or measured performance claim is accepted here.

## Final acceptance / handoff

Return **I11-01 only** to SAME Input owner for the bounded incomplete-release correction, focused fault/normal/replacement source controls, docs if necessary and an updated exact source-final receipt. Then re-review that changed snapshot. NI4-01/02 and I5-02 need no further runtime redesign from this review. Source closure is limited to the reviewed artifacts, not a compile/run approval.

After all writers finish, root must obtain a frozen-snapshot compile/discovery and nonempty test results on installed Unity 6.3/6.6, including the Input partial fixture and all NoCode cases. Record actual warm snapshot/weak-reference results; do not call private array identity an allocation benchmark. Native mouse Legacy/Both/New-only, focus/pause/device/camera loss, runtime lifecycle, optional backend stripping, UI hit parity and real unscaled scheduling need their original operators' acceptance. The synchronous state seams and explicit hooks are useful authored regressions, not evidence that all those native routes ran.

Actual reviewer operations were source/doc/receipt/diff/reference reads, limited recorded-PID process enumeration and SHA256 bookkeeping. Only this report was written. No Unity/MCP, compiler/build/discovery/test/checker, installation/service, process management, source change, commit/push or child agent was performed. All product execution remains **UNEXECUTED**.

## Reviewed fingerprints (SHA256)

Receipts and critical runtime sources were read again at completion and matched intake. Table paths are repository-relative; prior identical artifacts listed afterward inherit only their existing NI4/I5 source coverage, never execution results.

| Artifact | SHA256 |
| --- | --- |
| `Docs/Audits/2026-10-23/Neo.Tools.Input.implementation.md` | `f5b13ff256d2ed153572d0e5cabd8a2782eb0da2df2584ff64168bda77979e07` |
| `Docs/Audits/2026-10-23/Neo.NoCode.implementation.md` | `08b3540699f83711225bde38dfc6963b636d4a890ded3ba942ed4ca14102ca08` |
| `Docs/Audits/2026-10-23/Neo.Tools.Input.implementation-request.md` | `7c956378945e6e833945ecaf17f60a45585e2c1e81fc3f6240c858ca52c37fbd` |
| `Docs/Audits/2026-10-23/Neo.NoCode.implementation-request.md` | `c25adfd2629a480bf235143eb6eac0cb8803b3c5ded002b5404e55e80c6e85d3` |
| `Docs/Audits/2026-10-23/NativeImplementationReview-4.md` | `7bda3e36088846193d68220b40f679463fe0253134e8f4b77a6f2761518ac2cc` |
| `Docs/Audits/2026-10-23/NativeImplementationReview-5.md` | `e9c1e44db94457eafb7286ac4df20b81613ae7edd54a44d1499ba925c8f07e01` |
| `Assets/Neoxider/Scripts/Tools/Input/MouseInputManager.cs` | `0c3878e6ba6bd98387e4fe9f1f45d79a5491db266a955d37dfc2e5abe2b5b84e` |
| `Assets/Neoxider/Scripts/Tools/Input/MouseEffect.cs` | `13ef2a08f1f06cbd4b0997033a2a77c7635663e03f51d061632c7d67e68296a9` |
| `Assets/Neoxider/Scripts/Tools/Input/Neo.Tools.Input.InternalsVisibleTo.cs` | `d31fb6e5e90e5d86e9d350a6c13f166440ac1ebd7f3ccd9a91a57ceddfa35af7` |
| `Assets/Neoxider/Tests/Play/Core/Audit20261023NeoToolsInputBehaviorTests.cs` | `0c13bc41fc6c1045efc4f1210ea355abdbc7319c50ec0b869df3ad4d1673b595` |
| `Assets/Neoxider/Tests/Play/Core/Audit20261023NeoToolsInputBehaviorTests.Dispatch.cs` | `4c6ee73a7d37c66f2655d12da021f41cb9fc773b761f0ba4d24a6ed3fc57ad02` |
| `Assets/Neoxider/Tests/Play/Core/Audit20261023NeoToolsInputBehaviorTests.Dispatch.cs.meta` | `36f22bcce6e2f2f1628c2860e35d4886fb8d0a736046369ca354e58e2b67305b` |
| `Assets/Neoxider/Docs/Tools/Input/MouseInputManager.md` | `7e6631eafd7e435860fe6b12803a54234ad170f8c9f098315ead037192673299` |
| `Assets/Neoxider/Docs/Tools/Input/MouseEffect.md` | `d4296da704269304797d0582563d0f211f0ec0415e23543f61fb998f9902e307` |
| `Assets/Neoxider/Scripts/NoCode/NoCodeFormattedText.cs` | `855f9b10d6d248cda9ef0e4f79052e07e42934ff490d4f1841b41e0ed21e2530` |
| `Assets/Neoxider/Scripts/NoCode/ComponentFloatBinding.cs` | `079a9b301378e1e89e48ab072761e9f7bfb34a5eb68daf592a0e339085e8648f` |
| `Assets/Neoxider/Scripts/NoCode/NoCodeFloatBindingBehaviour.cs` | `4acf75fd3b115c037c63ad919e09f45d2da5809371a405b88b7cd929dd17b8ad` |
| `Assets/Neoxider/Scripts/NoCode/NoCodeBindText.cs` | `85652c9eac823cd2ae8ce5a1ef7824e649b26870c0b6d69fbf2c928b3b33deb5` |
| `Assets/Neoxider/Tests/Edit/NoCode/Audit20261023NeoNoCodeFollowupTests.cs` | `382b745402cab58d32413a1fc8a6309e13868db7ee2c7bef38b4ff68b642aea0` |
| `Assets/Neoxider/Tests/Edit/NoCode/Audit20261023NeoNoCodeBindingTests.cs` | `c36fd0db6aaace3be1731641ab7bf850a73c1981f2500084113f40017de66c0f` |
| `Assets/Neoxider/Tests/Edit/NoCode/Audit20261023NeoNoCodeOccurrenceTests.cs` | `3908e178c4823ebc95b825328b2f49c5cf9ded53108ddc5dab0236f76a237b9d` |
| `Assets/Neoxider/Docs/NoCode/README.md` | `c3910cd4abb4dc1aedcd069d847125de72c1bfba7535a719749490db713e09df` |
| `Assets/Neoxider/Docs/NoCode/GettingStarted.md` | `ad1ff70a30b43af495838e87ab359e896646ae2341a34bc1367499b8eb7baa95` |
| `Assets/Neoxider/Docs/NoCode/ComponentFloatBinding.md` | `9f26d20086733e8d90a5e0854687a1d7a397d174348e2f24ceb576faf27b75f8` |
| `Assets/Neoxider/Docs/NoCode/NoCodeFloatBindingBehaviour.md` | `9900556a6cdec11f49bcad86c584e3c84661327daa262baef44b993234cd48dd` |
| `Assets/Neoxider/Scripts/Tools/Input/Neo.Tools.Input.asmdef` | `97237be5ebaff3727392d6dff620f6341b319e1ba9290a710a56e43caad24021` |
| `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` | `1856ba6d704ec0852b65cf22f63fdc0427818c49259f6f2032488b02745d62ad` |
| `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` | `204a5ba6382792131c48cae74f38e27f5431909c0c0866c3b2905be0f2c4b4a5` |

SHA comparisons against NI4 additionally confirmed unchanged InputBackendCompat/UIInputCompat and their metadata, KeyInputCompat, MouseInputCompat, SwipeController, backend/UI regression files and their metadata, visual hook/metadata, README, KeyInputCompat guide and SwipeController guide. These exact earlier hashes remain in NI4. Ordinary git diff does not include new untracked additions; the new dispatch partial/meta and all current regression bodies were read directly. No package-wide GUID/encoding validator or C# syntax/type checker was run.
