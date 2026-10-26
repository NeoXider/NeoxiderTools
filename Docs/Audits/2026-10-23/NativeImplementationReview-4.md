# Native implementation review 4 — Coroutine/UI and Input

Status: scoped Coroutine/UI source approval with native evidence debt; Input needs bounded same-owner corrections NI4-01/02 below. Follow-up NI3-01 and Core NI2-01 are closed **at source level only** against the new receipts. Compilation, discovery, tests, native lifecycle/device behavior and performance acceptance are **UNEXECUTED**. The global execution gate remains closed until EVERY writer finishes and root declares the integrated freeze.

Review date: 2026-10-24 local. Branch/target: `demo` / `11.0.0`; contract base `ca198556`. This is a working-tree source review, not a final frozen-snapshot acceptance receipt. Only this review document is written. No product source/test/public documentation, original receipts/plans, configuration, metadata or validation isolate is changed; no Unity/MCP, compiler, test/validator/checker, installer/service, process management or child agent is used. Source reads/diffs, reference enumeration and artifact hashing are the actual evidence.

## Read coverage and ownership

Read the complete current implementation contract, both complete assigned implementation requests and receipts. Read every implementation-phase runtime hunk, complete new regression bodies/helpers, lifecycle fixture changes and all five Input public-documentation diffs; read the complete lifecycle/ownership guide. The earlier complete native-A review of the original audit fixes remains historical source context, not execution evidence. New phase work is distinguished from the original fixes visible in the larger base diff.

| Owner | Current implementation scope read | Disposition |
| --- | --- | --- |
| `Extensions-Coroutines-Unity`, SAME `neo-audit-1023-extra-02` | Both changed runtime files; all four new C# files and their metadata; changed lifecycle partial fixture; complete owned lifecycle/UI guide. Re-read the startup, native wrapper, advancement, iterator registry/disposal and tracker/reset call graph. | Additive UI API and the two narrow finalization guards are coherent. All 37 authored cases remain unexecuted; native-finalization branch observation remains explicitly conditional. |
| `Neo.Tools.Input`, SAME `neo-audit-1023-b07-02` | New backend/UI adapters; key/mouse compatibility readers; shared touch selection; manager cancellation and full event dispatch; effect lifecycle/follow/spawn/reentry paths; three new source fixtures/helper and ten added behavior cases; five owned guide diffs. | Backend selection and UI composition are coherent. Local effect continuation guards close the named onSpawn/activation continuation, but cancellation/replacement admission is incomplete at publisher boundaries. NI4-01/02 remain. |
| `Neo.Abilities`, SAME `/root/audit_abilities` | Strengthened existing overflow test body and updated implementation receipt; hash comparison of every artifact recorded in NI3. | NI3-01 source-closed; six runtime files and all other previously approved artifacts are unchanged. |
| `Neo.Core`, SAME `neo-audit-1023-b02-08` | Complete new receipt; current Decrease/CanSpend/TrySpend/validation paths; nine new precision cases and helper state; owned README changes. Earlier NI2 XP/registration review is retained. | NI2-01 source-closed; floating-point representation and input-cap compatibility are explicit. |

Nearest-assembly boundaries are respected. Primary Extensions owns canonical navigation, Inventory owns serialized preview consumers and Move owns ScreenPositioner migration. This review does not reopen their unfinished source scopes. Root's current `Neo.Tools.Input.asmdef` already contains `UnityEngine.UI`, required by public `RaycastResult` signatures. The Input receipt's remaining-reference request is historical/stale integration information; the reference is present, and no duplicate request is made. Root owns that disjoint configuration change.

## NI4-01 — P1: old publisher batches can restart a canceled follow or stop its replacement

Original owner: SAME `Neo.Tools.Input` / `neo-audit-1023-b07-02`. Current sources: `Assets/Neoxider/Scripts/Tools/Input/MouseInputManager.cs:88–135` (`ProcessMouseState`), `:138–151` (`ClearPressedState`/`CancelPressedState`), event fields `:282–294`; `Assets/Neoxider/Scripts/Tools/Input/MouseEffect.cs:279–335` (`OnPress`), `:353–382` (`OnCanceled`/`OnRelease`). Source-confirmed through synchronous callback tracing; no executed reproduction is claimed. Root has accepted the bounded obsolete-dispatch correction.

Trigger A: register an ordinary manager OnPress listener BEFORE enabling/subscribing MouseEffect. That listener disables the manager. ProcessMouseState has already set `_pressed=true`; disabling invokes CancelPressedState, clears the pending press and broadcasts OnCanceled. The already captured OnPress multicast invocation then continues to the later effect subscriber. Its admission checks only the effect's own enabled/interactable state, so it creates a NEW effect generation, activates visuals and emits onStartFollow even though the manager is disabled with no active press. No future matching release is guaranteed. This recreates the original stuck-follow ownership defect under an ordinary callback ordering.

Trigger B: while canceling a started gesture, an earlier OnCanceled listener starts a replacement Press. The later captured effect OnCanceled still calls StopFollowing and stops that NEW gesture. Trigger C: an earlier OnRelease listener starts a replacement Press; the later effect OnRelease reads the replacement's current `_followGeneration` and stops it. The old publisher can also continue into its `in`/Click/later phases after callbacks have replaced or canceled state. An effect-local generation created only when its callback begins cannot identify an obsolete manager event.

Expected: canceled/superseded input work cannot start or mutate later visual ownership; a new independent gesture survives old Cancel/Release tails. Preserve normal successful ordinary-then-`in` notification order, independent event switches, current subscription/exception semantics, and no synthetic Release/Click on cancellation. The existing release-without-press behavior remains an intentional documented path; do not silently forbid it.

Smallest repair: add manager gesture/dispatch generation admission and fence obsolete callbacks/phases, including BETWEEN subscribers where interruption occurs; pair any effect admission needed to bind its work to the initiating gesture. A single check after the entire multicast call is too late for Trigger A. Do not introduce a general event framework. If dispatch is manually iterated, avoid GetInvocationList/array allocation on every Hold/frame: cache snapshots on subscription changes or use an equivalent bounded approach. Preserve the current snapshot moment of each ordinary and `in` event rather than moving subscription changes to a different phase.

Root's bounded allocation-safe recommendation retains the public events: a private per-event cache keeps the last multicast Delegate reference and its invocation array. Combined-delegate identity changes on effective subscription changes, so the array is rebuilt only then. Each dispatch holds its local immutable array and iterates typed handlers with the current gesture/version guard between callbacks and before the next event/phase. This is an implementation option for the SAME owner, not a requirement to replace public event APIs.

Required authored controls for the SAME actor: earlier OnPress subscriber disables manager before the late captured effect callback; cancellation before that callback produces no orphan start/visuals and a later valid press still works. Earlier OnCanceled/OnRelease subscriber starts a replacement gesture; old tails cannot stop/clear it or emit stale follow work. Include state/event counts and current hit/position ownership, positive normal Press/Hold/Release/Click and ordinary/`in` ordering controls, repeated cancellation and subscription mutation/reentry. Existing new tests cancel only AFTER a whole Press or interrupt inside the effect's own callback; they do not cover this publisher-listener ordering.

## NI4-02 — P2: captured Hold/Click subscribers still spawn after the effect is disabled

Original owner: SAME Input actor. Current `Assets/Neoxider/Scripts/Tools/Input/MouseEffect.cs:419–430` (`OnClick`) and `:432–464` (`OnHold`), with unsubscribe/cleanup at `:185–198` (`OnDisable`). Source-confirmed, unexecuted.

Trigger: subscribe a manager OnHold or OnClick handler before MouseEffect, then have it disable the effect. OnDisable unsubscribes and clears its state, but normal multicast invocation retains the previously captured effect callback. Unlike OnPress and OnRelease, these two callbacks check only `interactable`, so a disabled effect can instantiate the Hold/Click prefab and emit onSpawn after cleanup. Publisher gesture fences alone do not fix this when the manager's gesture is still current and only the subscriber was disabled.

Expected: the disabled effect admits no new spawn work; existing spawned instances retain their documented lifetime. Smallest repair: consistently validate current native enabled/lifetime admission at these subscriber entries, preserving interactable and mode semantics; retain the existing pre-callback timestamp/first-Hold commit and replacement-gesture protection. Add the two ordering regressions plus a subsequent re-enable/valid gesture positive control. No recognizer redesign or pool dependency is needed.

## Coroutine/UI source closure and capability debt

The coordinate overload at `UIUtils.cs:43–60` validates a non-null caller list, clears it before missing-EventSystem return, captures the current EventSystem, forwards exact screen pixels through real RaycastAll and returns the list count. The allocating overload creates an independent list. Both avoid input-backend reads; the legacy cursor wrappers retain their existing legacy read. New UI tests observe deterministic EventSystem dispatcher output/coordinates, stale result replacement, independent ownership, missing-system clearing and the null argument. They do not prove real device input.

`CoroutineHandle.ReleaseRoutine:183–201` now keeps the registry intact and completes/defer-cleans if native wrapper release arrives during Advance. `Advance:149–181` resets its advancing flag in finally and drains completed work after MoveNext/Current returns. `RunTrackedCoroutine:465–475` completes the root's tracked tree before releasing that root, so nested iterators are disposed child-to-parent even if native finalization supplies the root first. Registry removal before Dispose, terminal state before callbacks and later release no-ops retain exactly-once cleanup. No new scheduler or native-liveness inference was added.

The partial lifecycle fixture captures/restores timeScale/fixedDeltaTime and prior global hosts. Teardown cancels a snapshot and immediately stops newly tracked cleanup work, avoiding mutation of its active enumeration; only owned/new hosts/objects are removed. Tests drive real native yield instructions, lifecycle hooks and iterator cleanup through the existing API. The two test MonoBehaviours have matching filenames. Existing synchronous contracts and new UI tests account for 17 authored cases; existing 8 plus new 12 lifecycle cases account for 20, total 37. These are source counts, not discovered/passed tests.

**E1 — new native branches are not automatically proved by a green supported-stop test.** `Audit20261023CoroutineUnityLifecycleTests.Acceptance.cs:233–267` issues raw StopCoroutine/StopAllCoroutines, then calls handle.Stop. `NativeAdvanceScenario:269–294` likewise calls raw StopCoroutine and then tracked Stop while inside MoveNext/Current. If a Unity path does not finalize the native wrapper at the raw stop, the existing tracked completion/Advance-finally route can satisfy every assertion without reaching either new native-finalization guard. Hierarchy deactivation also invokes the package tracker, so completion alone cannot attribute the route. These are meaningful supported raw-stop→tracked-stop/cleanup tests, not vacuous tests, but they must not be reported as unconditional native branch coverage.

After the gate, record actual raw-stop observation BEFORE the tracked fallback on each installed host/path, using real cancellation and an explicit capability/route receipt; do not synthesize private handle state. If wrapper finalization is not observable, disclose that route as unobserved and retain supported tracked-stop results separately. An optional small same-owner source instrumentation/control may be useful if root needs direct branch proof; no product defect or mandatory scheduler refactor is inferred here. The receipt honestly preserves raw-stop observability as a limit.

**E2 — other native evidence remains open.** The EndOfFrame case explicitly Ignores batch mode; it requires a render-capable execution and cannot be called covered by batch success. Phase probe assertions must actually run for custom-wait/fixed/update/late ordering. Mini-thumbnail availability, generated Sprite deferred destruction versus borrowed texture lifetime, native cancellation/destruction/reentry order, scaled/unscaled pause and domain-reload on/off remain unexecuted. List/proxy/cancellation-registration allocations and helper cleanup costs are unmeasured; no zero-allocation or platform parity claim is made.

## Input completed actions and aggregate work

Common InputBackendCompat interprets the compile-time backend symbols centrally, preserves legacy-first Both behavior, skips known-disabled legacy on New-only, remembers the first rejected legacy read and resets on subsystem registration or explicit deliberate reconfiguration. Key/mouse/touch share the selection, so upper Move/Debug consumers do not introduce another mapper. Cached rejection is a backend choice, not device-presence state; the optional adapter queries current devices. No mandatory Input System package/reference was added. Three new tests distinguish simulated invalidation, explicit reset and New-only zero-selection-exception reads; simulated initial invalidation is explicitly not a native rejection reproduction.

UIInputCompat uses MouseInputCompat above the new Extensions coordinate API, clears unavailable-input results and exposes caller-owned lists. Dependency direction is retained; there is no shared static result buffer. The real GraphicRaycaster parity test has an explicit available-mouse requirement; missing capability can skip and must be reported. The no-EventSystem case requires the isolated fixture scene. Both APIs still allocate per-query PointerEventData; convenience List/IsPointerOverUI calls allocate a result list.

The local effect guards correctly fence follower activation, onSpawn continuation and nested replacement inside those callbacks. onStart→disable commits following first and therefore pairs start/stop. First-Hold flags/timestamps commit before onSpawn and do not overwrite new state on return. These source closures do not discharge NI4-01/02. All 15 newly authored Input cases (10 behavior, 3 backend, 2 UI) remain unexecuted, alongside the historical 25.

Aggregate cost is reduced by shared selection rather than a per-consumer exception path. Optional reflection/string/boxing work still exists per read. Manager still builds event physics separately for Press/Hold/Release with existing nearest-3D-first/2D-fallback semantics; callbacks can mutate collider/settings between phases, so no unconditional same-frame query sharing is implied. Each active effect polls its own following position/camera at scaled intervals; Instantiate/Destroy spawning and EventSystem raycast work are unchanged. NI4 remediation must not add one delegate-array allocation per Hold. Real Legacy/New-only/Both configuration, no-Input-System fallback, hotplug/current-device behavior, primary-touch native lifetime, native focus/pause/camera recovery, domain/scene reload and IL2CPP reflection stripping remain open execution debt.

## NI3-01 recheck — source-closed, runtime unchanged

Read the updated Abilities receipt and existing `Audit20261023AbilitiesReceiptsAndTimeTests.cs:258–295` case. After an EMPTY system reaches float.MaxValue, it creates HP50/Max100 with HealthRegen4, a fresh active Duration2/TickInterval0/no-effects modifier and the successful pending empty-impact projectile. It asserts these valid preconditions; the rejected overflowing Tick preserves HP50/duration2 and the pending-hit control. The subsequent ordinary Tick1 must produce HP54/duration1. A clock poisoned to Infinity before rejection would leave those values unchanged, so the positive control now distinguishes the mutation identified by NI3 without a private clock probe or a false absolute cooldown assumption.

The absolute clock is float and float.MaxValue+1 rounds to float.MaxValue; the recovery assertions intentionally use relative delta-driven resource/modifier work. Subsequent pending expiry pruning is not a required retention control. All six runtime source hashes, four guide hashes, Target fixture and both fixture metadata hashes match NI3; only the expected receipt and existing receipts/time fixture changed. NI3-01 is closed at source level; 28 authored cases and the 1/100/1000 workload receipts remain UNEXECUTED.

## NI2-01 recheck — source-closed with explicit float compatibility

The new full Core receipt is available. Current `ResourcePoolModel.Decrease:154–177` stores its clamped subtraction, then captures previous minus stored new Current BEFORE Changed/Depleted callbacks. It returns that receipt even when a listener mutates the balance. Positive nominal1 at current100,000,000 returns0 and retains Decrease's prior Changed notification; that low-level API was not silently given spend/no-event semantics.

`CanSpend:77–80` and `TrySpend:82–109` share `ValidateSpend:111–151`. Existing nonfinite/nonpositive/id/balance/decrease-limit precedence remains; only after those checks a forced-float candidate must be lower than Current. A positive cost that rounds away rejects with the same reason before any state, event or timer write. Ordinary effective fractional subtraction is accepted without requiring delta==requested amount.

The nine new cases in six methods cover rounded-zero Decrease receipt with notification, over-round/unlimited/input-cap receipts, callback-independent actual delta, shared zero-effective preflight/payment with both timer phases/event controls, original cap failure precedence and effective fractional/large-float successful spends. Source tracing of later scheduled recovery confirms the timer positive controls are nontrivial. Runtime hash matches the new Core receipt. The 5-file bounded follow-up remains disjoint from consumers and earlier XP/registration changes.

Representation remains intentional: 100,000,000−5 stores 99,999,992, actual delta 8; a request 20 with MaxDecreaseAmount 5 clamps the INPUT to 5 and can still store delta 8. This preserves the existing input-limit semantics, not a new absolute representable-delta cap. The guide explicitly discloses rounding; no double/int backend or broad transaction framework is required. Decrease/Increase consumers should retain the corrected return-contract usage. All 78 Core cases and dependent wrapper/Abilities integration remain UNEXECUTED against the same eventual frozen snapshot.

## Artifact identity

SHA256 values below identify the actual source/read snapshot. They are read bookkeeping, not validator/compiler/test results. Complete full-artifact reads are listed for requests/receipts; source/doc rows identify their reviewed complete bodies or phase hunks as described above. Prior unchanged Abilities artifacts retain their complete NI3 read coverage and were compared bytewise by SHA256.

| Read artifact | SHA256 |
| --- | --- |
| `Docs/Audits/2026-10-23/IMPLEMENTATION-CONTRACT.md` | `d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab` |
| `Docs/Audits/2026-10-23/Extensions-Coroutines-Unity.implementation-request.md` | `d998f7c646dad1ca1760e2814cb10107b59c7fb56fcd6589b6484ec4188272ea` |
| `Docs/Audits/2026-10-23/Extensions-Coroutines-Unity.implementation.md` | `42135487c95e35ecf7ba2d98b1f981200ae86ad19441b10f8d652539b4fff179` |
| `Docs/Audits/2026-10-23/Neo.Tools.Input.implementation-request.md` | `032dfb9c2494126f084675e299964450d08bdf273ca121ad66478ada71849d31` |
| `Docs/Audits/2026-10-23/Neo.Tools.Input.implementation.md` | `82171ab61003213fc61827d3aba2819bbcac668cb0aa0e095d43095dfc2d0086` |
| `Assets/Neoxider/Scripts/Extensions/CoroutineExtensions.cs` | `ecbeb1a46e6b2ed198a5c4ba62004be46baa1ee9f860cb790c892d08856eaf41` |
| `Assets/Neoxider/Scripts/Extensions/UIUtils.cs` | `d8a3bba82096f06568214a1deb6fa2600dbfc81ca6ec4db653caeda01bf86c4a` |
| `Assets/Neoxider/Docs/Extensions/LifecycleAndOwnership.md` | `8a163578f415ce71b3abd5dc798ad33002dd745164197a01aadb6d6f86231fb6` |
| `Assets/Neoxider/Scripts/Tools/Input/InputBackendCompat.cs` | `7239385234ae3ee189d3339e4cd6f10701ae6a657dbe7bcc0c70c08e51b09eb8` |
| `Assets/Neoxider/Scripts/Tools/Input/InputBackendCompat.cs.meta` | `302eefb5d97f9b6ba8ac5ea64cfc8af2c67d79eceae5ad7c37c137cdd9bc3c80` |
| `Assets/Neoxider/Scripts/Tools/Input/UIInputCompat.cs` | `bede118ba3b2d3f5e2b7d710f32255201509f4c73ecc3b88b39f39fc014b1ff5` |
| `Assets/Neoxider/Scripts/Tools/Input/UIInputCompat.cs.meta` | `ccca01c09458e90f4181a47989e3a81490818ed14eb5b3ba1d319b2e88bf77b7` |
| `Assets/Neoxider/Scripts/Tools/Input/KeyInputCompat.cs` | `44bfa760fabc48f8cbef61b842003e40d74f527c5a90639b5344674ff03726e3` |
| `Assets/Neoxider/Scripts/Tools/Input/MouseInputCompat.cs` | `fd092762f90fb7c7bac51544d899f3f19f16bfebac1841edcaacb6025f70ad14` |
| `Assets/Neoxider/Scripts/Tools/Input/SwipeController.cs` | `ba120993059a03b462169e00e6af644ccfbe2b60b40c8634326208d45f8b36a5` |
| `Assets/Neoxider/Scripts/Tools/Input/MouseInputManager.cs` | `0cfe2b421f33355cce0a2b80ebe232c810de4850b6624c296ac11ac4b265cc9f` |
| `Assets/Neoxider/Scripts/Tools/Input/MouseEffect.cs` | `7118b93b7f1ca0b4d2d3f9d6187a81d1d4b462731b569b091b04bf2416ca7354` |
| `Assets/Neoxider/Scripts/Tools/Input/Neo.Tools.Input.asmdef` | `97237be5ebaff3727392d6dff620f6341b319e1ba9290a710a56e43caad24021` |
| `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityTests.cs` | `16cec2059e6322a134f5b15dc395997b21b55b70700e55cee5c11796e79cc57d` |
| `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityLifecycleTests.cs` | `9e36a0596fc43740526011d8d249c9bf7502e4576fa9ba88ca2f25080f215c8d` |
| `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityLifecycleTests.Acceptance.cs` | `36d95ce2fc870430077ae676bf4853b1013894e7de003bfbf142e8ba5b54bda4` |
| `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityLifecycleTests.Acceptance.cs.meta` | `f413b9aeb638f8f069da8f3b2f03abc4bc8d14b0f000948fad7629cdfb5a2173` |
| `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityUIUtilsTests.cs` | `2e4a7ba18002c0bea328dd6aa2b71a200b106df1f3869ccca8a0a72fb0fd5a18` |
| `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityUIUtilsTests.cs.meta` | `61bb0a5869c91ab7662ff82bc4f251a02954518a3ab77d51eebc8cbba07ac729` |
| `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityPhaseProbe.cs` | `a0f43d04c198a0a5b5a7b220795401ae0ac1e0aa0c3d245f2f6cf81c2bfed18d` |
| `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityPhaseProbe.cs.meta` | `754cf1d0049045402b91a9ff902b0a9fc7ed1a46aeacff389e066b76b837b124` |
| `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityRaycaster.cs` | `fc9dfe25f72a176e551a3e2114d466b035efd4172acefdbf53d183ffa3616ede` |
| `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityRaycaster.cs.meta` | `3aadd4dc2472e9646c60cb1e6c5b00d14bec73721d2652d48c0a15dcc62fe71a` |
| `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoToolsInputBackendTests.cs` | `7cafb853b74328567bfeb6a02bae8e8f8747ddbf3bf8323f8fcd898532cfa03d` |
| `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoToolsInputBackendTests.cs.meta` | `a742cf2dd45f1103f535f51cebc11ffd63437c921955ea1d3d5fbe6f01ba5719` |
| `Assets/Neoxider/Tests/Play/Core/Audit20261023NeoToolsInputBehaviorTests.cs` | `7b822a5cdd2e24d37de87687fa5b9979f34f5d951d980d86d90b37a86fbfe527` |
| `Assets/Neoxider/Tests/Play/Core/Audit20261023NeoToolsInputVisualHook.cs` | `2adcaace179edce251d88272dc1304906e7fd221ebcf9cd731f38d5c2f757be7` |
| `Assets/Neoxider/Tests/Play/Core/Audit20261023NeoToolsInputVisualHook.cs.meta` | `050a89346cfb77e955de8863f71e563195ded37f8b6559db6aae30a4426cf1cc` |
| `Assets/Neoxider/Tests/Play/Core/Audit20261023NeoToolsInputUITests.cs` | `e8993fff9577a11f3fe73d136cb81881f0836142ed8f387f9ce31e5d84403d6f` |
| `Assets/Neoxider/Tests/Play/Core/Audit20261023NeoToolsInputUITests.cs.meta` | `6bca737e6d8b555ee2b0cfab9d8c9052ed95a4d5eb46cd6d198ff81a877db17d` |
| `Assets/Neoxider/Docs/Tools/Input/README.md` | `f4e5cf5098d996e3f40e4ab18caefc8029652c8b5134b9fa0c402b4aac63038e` |
| `Assets/Neoxider/Docs/Tools/Input/KeyInputCompat.md` | `883c9336c8066d717d059b2c2ba3b20b2ecdd73e17e527be71f902a7b29ba0c0` |
| `Assets/Neoxider/Docs/Tools/Input/MouseInputManager.md` | `9486628e1071c6f99419083a791788054293216e49e6f6f7d4cdaac5e57a22a9` |
| `Assets/Neoxider/Docs/Tools/Input/MouseEffect.md` | `79711ee2af59340ae51ca63cc3e4ca59b146a0e57b8a611279c8645272dd27a7` |
| `Assets/Neoxider/Docs/Tools/Input/SwipeController.md` | `e8196ed11765e52bfdaaebf1809e0cf3a22da8aa08d4cd38a0bf0c4c315970cc` |
| `Docs/Audits/2026-10-23/Neo.Abilities.implementation.md` | `a0d78e39436e1f5d7902ff83ec0e5988041bf5198583b8a761487bfdddcd1b30` |
| `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesReceiptsAndTimeTests.cs` | `1dcc936a3465ec685cffbbf7136748f97e0b0181401c05687e40a15f4ecbd5a6` |
| `Docs/Audits/2026-10-23/Neo.Core.implementation.md` | `22d03515a1cd8aaca6c48d6ba5da5a3df0438ca270d716e9f6def9f16fb7869f` |
| `Assets/Neoxider/Scripts/Core/Resources/Domain/ResourcePoolModel.cs` | `7fe3ac2cebdada0a6b212370cab520f88456a14b9997688c3ec1c5520667213f` |
| `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourceLifecycleTests.cs` | `4c3de2dbddfb55b9ae81034a116248f30238a8f4d1f7bd7dcfb9b7d253b5328b` |
| `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreCanSpendTests.cs` | `c3556248951e597d63b831c8d3461cb5f583f21b74401075f67a4e7670c68e76` |
| `Assets/Neoxider/Docs/Core/README.md` | `3826c2f5f89426cd6eb82ca5f9e6f7b435f514236193fba13acdfa09ace5bead` |

## Handoff

Return NI4-01/02 to the SAME original Input actor for bounded publisher/admission controls and regression/receipt updates; root owns the approval/integration sequence. Coroutine/UI source can proceed to later acceptance with E1/E2 capability distinctions preserved. NI3-01 and Core NI2-01 need no additional runtime repair from this review. This does not amend old reports into current passes. Every compiler/discovery/test/device/native/profile/package check remains deferred until all source writers finish and root resumes the original verification operator with a frozen source identity.
