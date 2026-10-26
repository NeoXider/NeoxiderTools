# Native cross-review C

Status: complete independent source review of all 25 assigned original workers. All 50 full audit/plan artifacts, actual changed source hunks, new regression sources and factual documentation claims were read. No runtime, compiler, test, validator or performance pass is claimed. Source corrections remain with original owners after the global review barrier.

Baseline: `ca198556`; branch `demo`; current evidence version 10.19.0; target 11.0.0. The source/implementation barrier is respected: only this report is written. Existing compiler diagnostics and prior audit check receipts are historical leads, not evidence for the current tree. Abilities runtime/corrections authored earlier in this session are reviewed independently by group A and are excluded from this group's disposition.

## Read contracts and framing

Full files read; SHA-256 at read time:

| Full artifact path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/AGENTS.md` | `19e4e16eb1be1463ad4937584e19db783864a2009317ac5fd448eed631ac240f` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NATIVE-REVIEW-CONTRACT.md` | `c1bb559c242ee21a44542abacc013cba2f7955d64c154350a4f336cbd11f1edd` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeReview-C.assignment.json` | `58f12eea3a5c6391bca1daa79b3542fbbc303270dc4e1bcfe2c2bcef17aca50a` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/INTERACTION-QUERY-DESIGN.md` | `c2d83ac281407ff36f897c50a1dbfef5d289447d8d64bde06d5dc2df9e5b90d1` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/V11-RELEASE-PLAN.md` | `c95eefb8722c37e197ed00a47165818a811762b06a02821f4a47dede0a90fd34` |

Interaction acceptance applied to this group's aggregate-work review: share only identical raw physics query contexts/epochs; preserve distinct masks, scenes, trigger/contact policies, geometry, cameras and 2D/3D. Keep target-specific tags/eligibility/blocker evaluation separate; preserve unique LOS queries; require complete buffers and explicit lifecycle/invalidation. Per-component ray geometry caching alone is not cross-object query reuse. No new interaction behavior or broad all-layers shortcut is approved by this report.

The assignment was read completely. Its source lists exclude nested assembly ownership; broad test-prefix matches (for example `Neo.Rpg` and `Neo.UI`) include child-owner fixtures, which are attributed to their exact original owner below. No assigned correction artifact exists under an exact `<assembly>.correction*.md` name at initial enumeration; corrections embedded in audits/plans are reviewed with their full artifact.

## Coverage ledger

Completion markers refer to full audit/plan, actual source hunks, new regression sources and factual documentation review; execution is still pending globally.

1. Neo.Rpg.ConditionBridge — `neo-audit-1023-b01-07` — reviewed; completed runtime fix appears sound; open fixture defect C-03 confirmed.
2. Neo.Rpg.Combat — `neo-audit-1023-b05-04` — reviewed; see per-module disposition.
3. Neo.Rpg — `neo-audit-1023-b05-03` — reviewed; see per-module disposition.
4. Neo.Rpg.Network — `neo-audit-1023-b05-06` — reviewed; see per-module disposition.
5. Neo.Rpg.Demo — `neo-audit-1023-b05-05` — reviewed; C-34/C-35 confirmed.
6. Neo.HtmlToUitk.Editor — `neo-audit-1023-b03-07` — reviewed; see per-module disposition.
7. Neo.HtmlToUitk.Runtime — `neo-audit-1023-b03-08` — reviewed; see per-module disposition.
8. Neo.HtmlToUitk.Tests.Editor — `neo-audit-1023-b04-01` — reviewed; see per-module disposition.
9. Neo.UI.Editor — `neo-audit-1023-b08-08` — reviewed; see per-module disposition.
10. Neo.UI — `neo-audit-1023-b08-07` — reviewed; see per-module disposition.
11. Neo.StateMachine — `neo-audit-1023-b06-02` — reviewed; see per-module disposition.
12. Neo.StateMachine.Editor — `neo-audit-1023-b06-03` — reviewed; see per-module disposition.
13. Neo.Audio.Editor — `neo-audit-1023-b01-08` — reviewed; see per-module disposition.
14. Neo.Audio — `neo-audit-1023-b02-03` — reviewed; completed fade/selection/UI corrections appear coherent; open C-04–C-07.
15. Neo.Level — `neo-audit-1023-b04-02` — reviewed; confirmed remaining findings recorded below.
16. Neo.NPC — `neo-audit-1023-b04-04` — reviewed; C-26 confirmed.
17. Neo.Tools.Spawner — `neo-audit-1023-b08-03` — reviewed; C-23–C-25 and joint Draw boundary.
18. Neo.Bonus — `neo-audit-1023-b02-04` — reviewed; clock mismatch C-08 and exception/prize-cost boundary C-09 confirmed.
19. Neo.Tools.Draw — `neo-audit-1023-b06-08` — reviewed; see per-module disposition.
20. Neo.Save — `neo-audit-1023-b05-07` — reviewed; see per-module disposition.
21. Neo.NoCode — `neo-audit-1023-b04-06` — reviewed; confirmed remaining findings recorded below.
22. Neo.Tools.Text — `neo-audit-1023-b08-04` — reviewed; confirmed remaining findings recorded below.
23. Neo.Tools.Physics — `neo-audit-1023-b08-01` — reviewed; callback-lifetime finding C-01 and documentation clarification C-02.
24. Neo.Haptics — `neo-audit-1023-b01-01` — reviewed, no confirmed correction regression; backend/payload risks unverified.
25. Neo.Merge — `neo-audit-1023-b04-03` — reviewed, scoped correction appears sound; proposal remains optional.

## Findings

Findings are source-traced, never claimed executed reproductions. Root confirmation and any repair belong to the original owners after all reviews.

### C-01 / P2 — ImpulseZone dereferences its destroyed emitter after an enter callback

- Current location: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/ImpulseZone.cs`, `ApplyImpulse`, lines 214–222; `CalculateDirection`, line 245.
- Trigger: a valid target enters a zone; its `OnObjectEntered` listener immediately destroys the zone component/GameObject (for example `DestroyImmediate(zone.gameObject)` in an EditMode/public-event driver). The target collider and Rigidbody remain alive.
- Expected: the cancelled/destroyed zone returns without force or a later applied event. Actual source path: the guard at 217 checks only the target objects, passes, then `CalculateDirection` accesses the destroyed component's `transform.position`, causing a missing-reference failure. The new target-destruction fixture does not cover emitter destruction. This is not a claim about deferred `Destroy` timing.
- Smallest repair: add an emitter Unity-null/lifetime check after the callback, before any emitter-backed access; retain the in-progress `finally`. Add a public event regression observing zero force/applied notifications after emitter destruction. Do not silently change whether explicit manual calls on a merely disabled live component are supported.
- Original owner/task: **Neo.Tools.Physics / `neo-audit-1023-b08-01`**. No cross-assembly change is required. Related callback destruction in the other two components needs a source/driver check rather than a blanket rewrite.

### C-02 / P3 — Physics entry overstates center-of-mass direction for the native explosion mode

- Current location: `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Physics/README.md`, paragraph beginning “Each explosion or magnetic query”; source `ExplosiveForce.Explode` at `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/ExplosiveForce.cs:220`; detailed `ExplosiveForce.md` Force Mode row.
- The entry says radial direction and custom falloff use center of mass. The native `AddExplosionForce` branch delegates direction/application to Unity; the detailed page correctly distinguishes its closest-surface direction from AddForce's center-of-mass direction. Expected documentation agrees between entry and detailed guide.
- Smallest repair: qualify that COM direction applies to AddForce/magnetic evaluation, while custom falloff uses COM in both modes. Keep runtime behavior/settings and the numerical regression unchanged. Owner: **Neo.Tools.Physics / `neo-audit-1023-b08-01`**.

### C-03 / P1 — ConditionBridge's active-controller regressions create inactive controllers

- Current location: `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgConditionBridgeTests.cs:26`, `Evaluate_ControllerMode_UsesAssignedControllerWithoutCharacter`, lines 128–150. Cross-owner contracts: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgEvadeController.cs:56` and `RpgAttackController.cs:502`.
- Trigger: all four parameterized active-controller cases run after SetUp deactivates `_root`; child controller objects stay inactive in hierarchy. Expected precondition is a ready active assigned controller without a character. Actual source result: `CanEvade` and `CanUseAttack` reject `isActiveAndEnabled == false`; the explicit readiness assertions at 137/150 fail before evaluating the adapter. The owner's plan already records this; it remains unfixed in the current fixture.
- Smallest repair: disable built-in input, finish configuration, activate the fixture root in this test before asserting readiness; add disabled-controller false/inverted-true cases. Preserve parent RPG activity gates. Owner: **Neo.Rpg.ConditionBridge / `neo-audit-1023-b01-07`**, after final `Neo.Rpg` controller contracts from `neo-audit-1023-b05-03`.
- The historical missing EditMode bridge reference is already present in current `Neo.Editor.Tests.asmdef`; do not schedule that central repair again or infer a compiler pass from JSON alone.

### C-04 / P2 — AudioControl cannot recover if Start had no settings

Current source: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/View/AudioControl.cs:96–156`, `OnEnable`/`Start`. Trigger: a built-in control starts while AMSettings is absent (singleton auto-create defaults false), then settings is created and the control re-enabled. Start returns before `_started = true` and input setup; OnEnable always returns while `_started` is false. UI remains disabled/unbound. Smallest repair: one-time UI initialization independent of successful binding, idempotent bind/retry on enable or explicit initialization, restoring only interactability the component itself changed. Add real late-settings/re-enable/input-cardinality regression. Owner: **Neo.Audio / `neo-audit-1023-b02-03`**. This is already confirmed pending work in its plan, not an unfinished claim about the separate lazy-source fix.

### C-05 / P2 — AudioControl replaces its settings reference without moving its subscription

Current source: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/View/AudioControl.cs:159–230`, `TryResolveSettings`, `SubscribeMuteEventsForToggle`, `UnsubscribeMuteEventsForToggle`, OnEnable/OnDestroy. Trigger: bind toggle to settings A, destroy A, create B and reopen control. OnEnable resolves B and silently refreshes once, but only Start subscribes; future B mute changes do not update it. Unsubscribe uses the current reference, so an independently retained old reactive event can keep the old delegate. Smallest repair: retain/dispose the actual subscribed owner/handle, bind on enable, unbind on disable/destroy, avoid duplicate UI listeners. Assert A/B notifications and silent one-input/one-callback behavior. Owner: **Neo.Audio / `neo-audit-1023-b02-03`**; reuse Neo.Reactive subscription handles, no playback-session extraction prerequisite.

### C-06 / P2 — AMSettings permanently abandons source binding when AM arrives later

Current source: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/AMSettings.cs:115–123`, `Start`; source-dependent setters at 202 onward. Trigger: AMSettings Start runs with no AM, then AM is created. The one-shot Start returns, no retry binds `efx`/`music`, setters continue returning early. Smallest repair: idempotent explicit `Initialize(AM)` plus enable-time fallback; apply loaded startup values once per successful binding, without replaying persistence or resetting active playback on every enable. Observe late binding and normalized/mute state on new sources. Owner: **Neo.Audio / `neo-audit-1023-b02-03`**. Current singleton implementation confirms CreateInstance defaults false; the completed cold-*sources* fix is not a late-*manager* fix.

### C-07 / P2 — reentrant OnStopped during standalone Initialize leaves the old source running

Current source: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/RandomMusicController.cs:51–63` (`Initialize`), `Start:64–89`, `Stop:96–118`. Trigger: play A; attach a one-shot `OnStopped` handler calling `controller.Start()`; call `Initialize(B, tracksB)`. Stop cancels old state/stops A, publishes OnStopped, and the handler synchronously starts A again with a new token before Initialize assigns B. The new loop/state then shares the overwritten B fields while A keeps playing outside the reported current source. Expected, matching the existing reinitialize test contract: Initialize finishes stopped and rebound to B; a later explicit Start plays only B. Smallest repair: bounded rebind/generation guard or equivalent ownership reconciliation around Stop notification, explicitly define reentrant Start during rebind; no session/framework extraction is needed. Add a two-source real-audio/public-state regression with a one-shot subscriber, owned-controller Stop and source cleanup. Owner: **Neo.Audio / `neo-audit-1023-b02-03`**. This is a source trace; no new audio execution occurred.

## Per-module review

### Neo.Audio — reviewed

Original owner/task: `neo-audit-1023-b02-03`. Full audit/plan, all six runtime changed-file diffs, affected selection/fade/transport/settings/control bodies, both complete new fixtures and all eight documentation diffs read. Per-voice independent gain/envelope state, failed clip selection before commit, outgoing mute fan-out, sparse fallback and enum migration are locally coherent. Public/serialized changes are private runtime state only. The tests meaningfully observe gain products, clip identity, resource ownership, mute/input events and elapsed behavior; Play cleanup stops controllers, restores listener pause, destroys owned objects/clips and yields for destruction. Hardware/audio-enabled assumptions are explicit; these are unexecuted here. Current tests do not cover C-04–C-07.

Aggregate work: AM has fixed two outgoing voices, request/fade-driven small allocations and one watchdog/session loop; no per-target Physics query. AudioFocusMute performs a scene lookup on explicit Install, not each frame, and still requires one listener-volume owner. AudioControl does no per-frame scene search; binding fixes should retain that property. Singleton lookup caches misses/instances. Do not replace explicit retry with a FindObjects scan per control per Update. The proposed shared session/observable channel state is optional and substantially larger than required binding fixes; preserve clamping order, unscaled fade policy and source ownership if later adopted. Repeating the same AudioClip at different entry indices and observer-mutated payload/state need explicit contracts before changing selection weighting or replaying state; not counted as confirmed new defects here. The StartMusicClip XML paragraph saying it takes no volume argument is stale after its private signature change and should be corrected in the owner's next documentation pass.

Read fingerprints:

| Full artifact/source path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Audio.md` | `56e817afbda42acc006b88fbc0bc368a745a363aced81a3166c95de93827f7d0` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Audio.plan.md` | `d80cf88da14dabc1381aeb5267f09b339da11429a3e4afe25557df6bb6142612` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/AMSettings.cs` | `b32ed4e37e0f5e04963d2adf7c6ce7abd925cd72bc7d48607b1f76f22c2389d6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/AudioSimple/AM.cs` | `f17cdcfd99bc074d672e3a70200328544ff35822066ce3a6daa44214bce5586e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/AudioSimple/AudioEntry.cs` | `c36c741558f11d70d0088ddfae592834c17b627e30e609402d6e6cb155c2c8d9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/RandomMusicController.cs` | `6df673738b27e3f7a25c374408ca07237983b7f7663c06f3f90bab3ad958c418` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/SettingMixer.cs` | `ec4ca224ce277eae3680baa3ce5684b84349f06fc1bf6ae237cdcc66aa76bd74` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/View/AudioControl.cs` | `c28959f45a5cdfef434115836c6fdf2e90fe454144314b84f6a03ca9c61db35b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Audio/Audit20261023NeoAudioRegressionTests.cs` | `81472623e514c164bbd34eb37d289d05104480b8f5b36525f7815ca33e9b48ab` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Audit20261023NeoAudioLifecycleTests.cs` | `bd58c79fec82d5cbd64c545a250a666f8a5e0a404ab4c1bc02db8f5acb3b48d1` |

### Neo.Rpg.ConditionBridge — reviewed

Original owner/task: `neo-audit-1023-b01-07`. Full audit/plan, changed adapter body/hunks, all new test source and the full new canonical topic read. Local-result reentrancy isolation is correct; missing-character evaluations now share inversion/event dispatch and controller branches precede character absence. Serialized fields, MovedFrom and API remain intact. The fixture owns/destroys its hierarchy and temporary assets, disposes SerializedObjects, and wires events by reflection while asserting public outputs; C-03 prevents four cases from reaching their intended contract. Parent navigation is checked with the RPG owner's documentation later.

Aggregate work: evaluation is explicit-call work with local/parent lookup only, no scene-wide search, Physics query or Update loop. Repeated consumers may repeat hierarchy/property reads, but caching here would change current-state lookup semantics without demonstrated need. The typed event-free query/status proposal is a reasonable optional legacy integration seam; it must preserve zero-fallback/inversion and coordinate the parent's typed attack lookup first. Do not parse failure strings or move scene references into presets.

Read fingerprints:

| Full artifact/source path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.ConditionBridge.md` | `06b3452eb4190d5e4aaea27e6debc1c51a8f12d08ca7ab9cbcf44f37c82e074f` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.ConditionBridge.plan.md` | `f487ea68709d1fe6bd9716e14f32ccbbb8e28f766edabc663fd55a40218c2ba8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Bridge/Condition/RpgConditionAdapter.cs` | `1043b3d483fbbfc12d46553b6a9b5e585920efc19aa8ff8148468a07240e2bcf` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgConditionBridgeTests.cs` | `390f4eda1f7b51f0bd0110b930e4d0c9256aa33c512172ba501f6905813261a2` |

### Neo.Tools.Physics — reviewed

Original owner/task: `neo-audit-1023-b08-01`. Full audit/plan, all changed source hunks and all three current component bodies, complete 20-case regression source and four guides read. The focused deduplication, custom attenuation, deadline maintenance, body identity, reentry reservation and magnetic cleanup/cache changes are coherent by source inspection. Fixture observations use accumulated impulse/force and membership/events; owned remote hierarchy is destroyed and no global physics simulation setting is changed. Real scheduling/contacts and native forces remain unexecuted; manual lifecycle messages are not an engine proof. C-01 remains outside the existing callback fixture; C-02 is a documentation inconsistency.

Aggregate work: each active magnet independently queries at line 375, scans its own full history and applies field-specific force/membership. Distinct centers/radii/masks are genuinely different queries; do not remove per-field forces or identity semantics. Co-located fields with identical raw query context could share discovery, but this is a profiling/optional-backend opportunity, not proof that all fields should share one result. The continuous-cadence proposal correctly keeps legacy tuning by default and separates discovery from integration; it does not yet address saturated buffers or same-context cross-component discovery reuse. Require explicit incomplete-query handling and query-count/velocity evidence before advertising completeness/performance. Reused buffers must not be called persistent immutable snapshots. Impulse history scan is per zone/body; explosion allocation/query is explicit-call work.

Read fingerprints:

| Full artifact/source path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Physics.md` | `bee7c69e320df04e855d2e1dc4a0df942994102969fc32c25ad488e3d3ba9f83` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Physics.plan.md` | `38e20b27f984d6ce9b0035d8e557150fd5e8781ce08bd328c11b58f24cd9fba8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/ExplosiveForce.cs` | `04397b5006978e809b5287235bd592a8c0ee9e69ad307ed71f5ffab760315bf8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/ImpulseZone.cs` | `9953be72a50ab10e7b8540166801630e22c1dc25caf7ae99946c905685ecfed1` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/MagneticField.cs` | `ea7cb40ec3dac120f89a35cca968f0b373d2f8845f625feca1f5226f29833577` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsPhysicsRegressionTests.cs` | `4a3970efc828245c84314112d9c0e6361b517edf0fc9f27b2fa877583e93ea70` |

### Neo.Haptics — reviewed

Original owner/task: `neo-audit-1023-b01-01`. Full audit/plan read; full changed source, fixture and canonical guide inspected. Per-type dictionary throttling, enum bounds, empty-pattern and finite guards occur before gate/event mutation; no public enum/signature or serialized data change. Native mappings are unchanged. Fixture asserts public event sequences, detaches its listener, restores provider/master/interval and clears its own throttle state. Root's removal of unsupported NonParallelizable attributes and reference integration supersedes the original artifact's historical pending-reference/nonparallel statements.

Disposition: completed scoped fixes appear correct by source review. No Update/physics/search loop; history is bounded to nine validated types and native waveform allocation is request-driven. No aggregate cross-object physics duplication. Device/backend compilation and real dispatch remain unverified. In particular, the guide's claim about missing named assembly references is not established by these event tests; retain a compiler matrix as evidence debt rather than asserting a current compiler pass. Pattern arrays are read again after `Played`; synchronous observer mutation can affect native payload construction. The service proposal explicitly copying paired segments before observation addresses that ownership risk, but this is not declared an executed native reproduction or a mandatory service extraction. Reject unnecessary scene/NoCode wrapper or extra dependency work without a consumer need.

Read fingerprints:

| Full artifact/source path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Haptics.md` | `355cff26fd2ca9a6ba90e3302e03f94fa69beee2248d743176b1741ee58dd4c2` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Haptics.plan.md` | `944e6496e6405b15e2c35fbc3bb9ac0af74ce019e095b4203af5643cfc9ac1f4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Haptics/Haptics.cs` | `11443a1ba5417dcf57ae6fa9868b851eb36465e0fe94bb063c4a958e7baaa682` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Audit20261023NeoHapticsTests.cs` | `0d2ea828fc6270c4184e01bf804942a37afb2afc69100c54ebb1fc7dc5f139d5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Haptics/Haptics.md` | `db2aebd235effdd764d1c568bdb48eb3bb3a7786dd19634b67ceabb77c3d40f9` |

### Neo.Merge — reviewed

Original owner/task: `neo-audit-1023-b04-03`. Full audit/plan, both changed files/hunks, resolver context, the new fixture, canonical README and pure asmdef inspected. Null selector rejection precedes result staging; commit remains after all seeds/cascades, so the four public regressions meaningfully check zero setter calls and unchanged source state after late failures. Fixtures use local dictionaries and no Unity/static resources. External result destinations and value-type zero are not accidentally rejected by `is null`.

Disposition: completed fix appears correct; no new confirmed defect in this delta. Resolver work/allocations are per explicit call: BFS/list/queue/hash sets and repeated seed/cascade evaluation are not hidden per-object Update work. Infinite graph/callback mutation/setter rollback are documented caller limits. The additive structurally immutable plan/read-set proposal is coherent only with its stated shallow-value/topology and caller-serialization limits; it remains a proposal, with GridSystem/Dice adapter ownership separate. Do not introduce a Unity dependency, speculative transaction framework or scene wrapper for this fix. Compile, four new cases, seven existing Merge cases and integration parity remain unexecuted.

Read fingerprints:

| Full artifact/source path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Merge.md` | `d5a48dfcbbee994bbd6d26540cdc2e6a2a825111f9022ee1355052efc80713af` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Merge.plan.md` | `77369fec4005fee5ddd4c7bdab18f1e4aabffc24b9fe623fb8c390bebb6d9c76` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Merge/MergeRequest.cs` | `a0bdc7127b22f3c0645ae8db2502dc08fd1955e57471582012542e2e852379bd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Merge/MergeResolver.cs` | `7a305eb80367f67760b74705107b6c2e5095cda9fcb835dc1019ea0099d65b8a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Merge/Audit20261023NeoMergeNullResultTests.cs` | `278e74e80346fdb92f6202af89f624bda0521d29220d26cdb35109924081e7a4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Merge/README.md` | `b9d761ce8de109c0fc05f9ef782f4f80caf313823580eb51d19cc6098c772abe` |

### C-08 / P1 — CooldownReward reads trusted time but its inherited timer persists device time

Current source: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/TimeReward/CooldownReward.cs:229–243` (`GetClaimableCount`), take/save at 277; `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs:397–529` (`SaveState`/`LoadState`, device UTC sites 423, 443, 448, 483, 529). Reproduction: trusted UTC is ten minutes ahead of device UTC, cooldown 60 seconds, accumulation enabled/cap one; a take writes device-now+60 through inherited SaveState, then a sequential second take compares this deadline with trusted now and is already eligible. The recursion guard has completed and cannot prevent repeated economic grants. Restore/start countdowns also cross clock domains. Expected: consume and restore use the same clock as eligibility.

Smallest repair: Time owner `neo-audit-1023-b08-05` adds a protected virtual UTC-now hook defaulting to device UTC and routes all five real-time save/load calculations through it; Bonus owner `neo-audit-1023-b02-04` overrides with NeoTrustedTime. Preserve plain TimerObject defaults and existing keys. Add ahead-clock sequential-claim/deadline/restore regressions plus the unchanged base-timer clock contract. The broad reward schedule/profile framework remains optional; it is not a prerequisite for this fix.

### C-09 / P2 — Collection callback exceptions can grant another Box prize without another threshold

Current source: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/Collection/Box.cs:133–149` (`TakePrize`), `Collection.cs:194–203` (`TryAddItem`) and `GetPrize:160–168`. Two locked assigned items, progress exactly one positive threshold; attach a one-shot throwing OnItemAdded listener. GetPrize commits/persists item zero, then the exception exits Box before its progress subtraction. Its finally releases the guard; after catching/removing the listener a second take unlocks item one for the same threshold. Expected: an already committed grant consumes its price even when notification throws; no-prize still leaves progress unchanged. This is distinct from arbitrary callback rollback.

Smallest repair: a bounded grant/notification boundary or equivalent reservation that commits Box progress with the actual unlock before collection external notifications, preserving the no-prize path and documenting event order. Reuse existing collection selection/unlock logic, retain public APIs, and test item identity, persisted progress, exception propagation/guard release, and the second take. Owner **Neo.Bonus / `neo-audit-1023-b02-04`**. Existing reentry-only test does not cover exceptions; the root entry's consumption statement is true for timer rewards but should not imply this path is safe yet.

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

### C-10 / P2 — TimeToText commits a mode that cannot display its current value

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Text/TimeToText.cs:148–155` (`DisplayMode`) writes `_displayMode` then redraws; `Set:245` rejects the value against the new mode. Compact/dynamic `Set(3000000000f)` then `DisplayMode = Clock` leaves Clock state with old Compact text/time. Expected rejected transition retains mode/time/text and publishes no time event; after Set(90), transition succeeds as 01:30. Validate proposed mode before commit, preserving existing formatter and rejecting rather than clamping accepted time. Owner **Neo.Tools.Text / `neo-audit-1023-b08-04`**; already openly pending D1 in plan, independently confirmed. Shared duration-core API is optional.

### C-11 / P2 — LevelButton's one-based label still overflows

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Level/LevelButton.cs:95`, `SetVisual`: `(level + 1).ToString()`. `SetVisual(1, int.MaxValue)` with assigned TMP displays a negative value instead of 2147483648. Smallest repair `(long)level + 1`, keep level/event payload Int32 and existing optional-label semantics; exact-label/state/event regression. Owner **Neo.Level / `neo-audit-1023-b04-02`**. Existing boundary tests only cover manager counters, not this label.

### C-12 / P2 — integer level display drops valid digits through float

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Text/SetText.cs:253–261` and `Level/TextLevel.cs:97–106`. Current wider addition fixes overflow but `(float)((long)value + indexOffset)` loses unit precision; level 16777216 plus offset one cannot finish as 16777217. Expected exact final integer text with existing number options/affixes; interpolation may remain float if final commit retains exact typed value. Reuse `SetBigInteger`/existing ToPrettyString, no second formatter or deep new service. Text owner **`neo-audit-1023-b08-04`** establishes additive exact final integer path (and explicit animation/currentNum approximation); Level owner **`neo-audit-1023-b04-02`** integrates it. Keep mutable formatting updates exact too. Document any animation contract decision and test grouped/plain output plus both signed/offset limits. This is an existing documented limitation requiring a deliberate 11.0 contract correction, not a claim that the earlier widening patch failed at its stated purpose.

### C-13 / P2 — TextLevel hides the new SetText tween lifecycle messages

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Level/TextLevel.cs:32–51` declares private OnEnable/OnDisable; base `Tools/Text/SetText.cs:29–43` also declares private messages and owns tween pause/resume. There is no base invocation/overridable hook. Derived-label dispatch therefore cannot reliably honor the base's advertised enabled-state lifecycle; component-only disable during an active tween can keep writing while disabled, and a deferred tween can remain paused on re-enable. Source message collision is confirmed; exact Unity dispatch/output is a required engine regression, not an executed result. Minimum repair: protected virtual base messages/hooks and matching TextLevel overrides calling base, preserving listener/wait cleanup and ordering. Owners **Text `neo-audit-1023-b08-04` → Level `neo-audit-1023-b04-02`**. Test active and queued tween on the derived type; plain SetText tests do not cover it.

### C-14 / P2 — Inspector validation disables NoCode plain-source fallback polling

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NoCode/NoCodeFloatBindingBehaviour.cs:41–45` and `NoCodeFormattedText.cs:70–75` reset `_useReactivePollFallback`, but LateUpdate decides whether to refresh before reconciling it. With Reactive mode bound to an ordinary float, edit Poll Interval during Play, then change the float: UI no longer polls until explicit refresh/re-enable. Expected next allowed poll reflects the new value; Once remains once. Minimum main-thread dirty/reconcile before shouldPoll, no component searches/getters during OnValidate. Owner **Neo.NoCode / `neo-audit-1023-b04-06`**; plan F1 accurately records it. Test both wrappers by output rather than private flags.

### C-15 / P2 — callback rewire retains obsolete reactive status and loses fallback

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NoCode/NoCodeFloatBindingBehaviour.cs:181–201`, `RefreshFromSource`: `reactive |= TrySubscribeReactive()` after ApplyFloat keeps earlier true even when a Slider callback changes binding from reactive member to ordinary float, removing the subscription. No fallback is enabled; later plain changes stay invisible. Expected final binding's actual subscription status decides fallback. Use the current result after ApplyFloat (plus existing enabled/mode gates), test old ListenerCount zero and later normalized Slider output 0.75. Owner **Neo.NoCode / `neo-audit-1023-b04-06`**; plan F2 independently confirmed. No session extraction required.

### Neo.Tools.Text — reviewed

Full audit/plan, both complete changed source diffs, both complete new fixtures and all three factual guide diffs read; related formatter boundaries/SetBigInteger examined. Immediate replacement/cancellation, retained formatting, event baseline before reentry and finite/display range guards are coherent. C-10 remains; C-12/C-13 require cross-owner coordination. Thirty-one cases assert exact text, retained numbers, event histories and real enter/exit-Play lifecycle. Teardown kills owned tween, destroys hierarchy and exits Play; defaultUpdateType is restored in finally. ManualUpdate is global: execution must use an isolated test context, not assume unrelated tweens are harmless. Format-only redraw can notify text by design, but time events deduplicate. No Update/scene scan/Physics; per-number tween and numeric-format delegate allocations follow requests. Pure duration Try API is optional reuse of Extensions rather than a parallel formatter.

### Neo.Level — reviewed

Full audit/plan, all five changed source diffs, affected binding/request/progress bodies, both complete new fixtures and all six guide diffs read. Frontier/wrap rules, sparse-map stable indices, public SetLevel, optional views, failed-provider counter rollback and per-request async identity are coherent within documented boundaries. Completion callback after request cleanup allows a new request; native cancellation still only releases activation. Cross-controller queue, disk rollback and arbitrary external StopAllCoroutines are explicitly outside promises. C-11–C-13 remain; fixes do not require progression-model or load-operation redesign. Thirty-one prepared cases observe saved keys, selections/events, actual click dispatch and deferred label output. Lifecycle fixture resets the static loader to default rather than preserving an injected prior loader; run isolated/restore exact prior in any future fixture update. Native 0.9/manual cleanup/terminal events still unexecuted; zero-duration label tests do not prove active-tween inheritance.

Aggregate Level work: one pending coroutine per controller, enabled label wait only when unresolved, one hierarchy collection on validation (replacing redundant descendant scans), per-progress button iteration. No identical per-object camera ray to share. Optional operation seam would improve deterministic pending-operation coverage; it is not prerequisite for confirmed label/lifecycle fixes. Core XP progression is a different domain, so reusing it blindly for map selection would be unnecessary scope.

### Neo.NoCode — reviewed

Full audit/plan, all four changed source diffs, subscription/polling/target reconciliation bodies, complete twenty-case fixture and all four factual guide diffs read. Host cache identity, destroyed-root explicit clearing, overloaded-indexer rejection, disabled snapshot notifications and late text recovery are coherent. Existing public/serialized descriptors retained. C-14/C-15 remain openly recorded. Fixture owns/destroys local hierarchies and asserts actual TMP output/listener counts; SerializedObject/OnValidate drives authoring configuration, not sole outcome assertions. Actual LateUpdate/IL2CPP/TMP behavior remains unexecuted.

Aggregate work: reflection metadata is cached, values are read each refresh; poll cadence defaults 0.16 unscaled, scene-name fallback throttled by existing resolver. Duplicate formatted slots currently force more reconciliation/rebuild work because match walks occurrences while registration deduplicates; this is a measured-work candidate, not a demonstrated wrong-output bug. Reuse unique underlying source identities/handles within a session if implementing optional typed adapters. Do not add global per-frame scene scans or pretend float preserves large integer currency. Existing Neo.Reactive subscription handles suffice; a new reactive framework is unnecessary.

Full read fingerprints for Neo.Tools.Text:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Text.md` | `bdbb0b481c595df7263a434087dd99b4475e44f680a3e8d51b9e5e2f2bee7280` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Text.plan.md` | `98b50a241ea2d654d349a7cac5949520abd2223b57eb16b2425ab976fbf561d6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Text/SetText.cs` | `4fdc99bce57556d745c80c88dd2b8e38c96f3e84480927402940f31d276ae51f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Text/TimeToText.cs` | `aaa54f5b448a36fe641fbb3cb1c7027c7249539d01df01fa87072125a64cc2b8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoToolsTextSetTextTests.cs` | `572b1b8c95c4238300eeb2870feba8559bd4ec347f37fb8092183b32811cfc25` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoToolsTextTimeToTextTests.cs` | `bceb9b2829c59b40afc0939cbc74bc701d14e126a5ea5a853f1a67d6c2366297` |

Full read fingerprints for Neo.Level:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Level.md` | `63d18bb0b9792d652c5a5ada1610b791178bb8310a6e326140d5c28f52067816` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Level.plan.md` | `12d8236491fa74c08684da28a8a8681bd35e268609db2a06c4471a242b76541f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Level/LevelButton.cs` | `813fa098f2c54249a023e350215fe42ce89a3e1c10fbc8d77ed91947b12d8a17` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Level/LevelManager.cs` | `7fc63bc89138c80f1079a458d6c89181a08aafb8267c9295fbf3e9be930c7ed9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Level/Map.cs` | `1be669416b0649772e8b4fe6721c168c7d386025668ab5beb90ef6614701dbaa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Level/SceneFlowController.cs` | `1a3a837c215dc2b7f74417c86866206b0efdd0da6870b03781bc0a7503875218` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Level/TextLevel.cs` | `e8065edec2b4eb47dc7d7e6567b859a000e55a08949adb76699df42752f3d79f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelLifecycleTests.cs` | `6485beabff7f34ba99df354ade11f6ba6096245ba318c2f2d416fe2ed337fd96` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelProgressTests.cs` | `fb2b19da011955c7c6b10badf9424d262b60f3535380b631b64a39cbc0455adc` |

Full read fingerprints for Neo.NoCode:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.NoCode.md` | `498aec807925a9b0a0af8a4125c4085172cdd39dcfbf3d214f50457455c8b023` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.NoCode.plan.md` | `c029f700d2d55fa8a117f0e1a3b2fee4f8a54336c4fac42b251540af503e1b32` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NoCode/ComponentFloatBinding.cs` | `bb431ab10d852b66337afd111690e4ea0022effbb10c85ad56b40c632b1e5df3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NoCode/NoCodeBindText.cs` | `85652c9eac823cd2ae8ce5a1ef7824e649b26870c0b6d69fbf2c928b3b33deb5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NoCode/NoCodeFloatBindingBehaviour.cs` | `188879bfa122489d223492bb5e7607123893756859a32def66473711b1ec5294` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NoCode/NoCodeFormattedText.cs` | `b7dc14e974ff94a492d785d74df01a27edc75762c4df03fa8c32513e77d7a481` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/NoCode/Audit20261023NeoNoCodeBindingTests.cs` | `4c7f719a8d9d6b162f0b0c1ae6d052ad7b2e61a241296e6ede46472f2c26adc4` |


### C-16 / P1 — reentrant state change from OnExit recurses on the same current state

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StateMachine.cs:128–156` (`ChangeState`): A.OnExit calls machine.ChangeState(C) while A is still CurrentState at 148, so A.OnExit repeats without a bound and can overflow the stack. Expected one exit per committed departure and coherent ordered events. A safe regression requests once and counts duplicate exits; do not intentionally crash the runner. Minimum bounded reentrant command dispatcher for ChangeState/Stop, immutable payload locals and finally guard release, retaining synchronous normal calls and normal Exit(A)/exited(A)/Enter(B)/entered(B)/Changed(A,B) order. Nested requests drain after current dispatch with a documented cap/gated diagnostic, no unbounded recursion or arbitrary rollback promise. Owner **Neo.StateMachine / `neo-audit-1023-b06-02`**. This is a confirmed defect, not optional architecture despite audit proposal B labeling.

### C-17 / P2 — shared duration predicates reset another machine's wait

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/NoCode/StateMachineData.cs:159–165` registers shared transitions; `StateMachine.cs:170–174` notifies predicates with entry time; `StatePredicate.cs:395–424` stores one timestamp. A enters at t0, B enters same configured transition/predicate at t4; A evaluated at t5 with duration five now waits until t9. Same defect occurs in shared And/Or/Not children. Expected independent per-machine elapsed time for reusable authoring data.

Smallest correction: machine-owned scoped entry/evaluation-time context used by built-in duration in the **default** runtime and wrapper paths, preserving legacy virtual Evaluate(IState) dispatch, inversion once and nested/finally scopes. No graph cloning, global custom-state isolation claim or timer service prerequisite. New options/frame API as legacy-default opt-in does not close the defect. Explicit compatibility decision is necessary: existing `StateDurationPredicateTests.SetEnterTimeInThePast_SatisfiesTheThreshold` intentionally edits the globally shared predicate after machine entry. Keep standalone SetEnterTime behavior; provide a clearly named legacy escape or an explicit addressed machine-anchor API/migration for this machine-global edit, and test both contracts. Do not silently change custom override dispatch or claim deep immutable assets. Owners **runtime `neo-audit-1023-b06-02`**, editor `neo-audit-1023-b06-03` only if authoring exposes a compatibility option. Acceptance: two default machines/fake clock, shared composites, re-entry, legacy standalone/manual mode, nested exception cleanup.

### C-18 / P2 — state lifecycle callbacks can publish null/stale payloads or revive stale transitions

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StateMachine.cs:152–156`: B.OnEnter disables its owner, wrapper OnDisable calls Stop, CurrentState becomes null, then outer OnStateEntered receives null and OnStateChanged receives (A,null), losing B's coherent lifecycle. `Stop:397–409` also reads mutable PreviousState after callbacks. Separate repro `EvaluateTransitions:337–355`: predicate/listener changes A→B→same cached A; reference guard passes and stale A→C applies. `CanTransitionTo/TryChangeState:184–222` also evaluates callbacks without generation reconciliation.

Expected newest explicit choice retained, events name the state actually entered/exited, one terminal stop and no stale transition across a departure/re-entry. Reuse C-16 dispatcher plus monotonic state generation and captured event arguments; check generation after callback-driven condition/evaluation paths, including addressed Try paths. Test enter-disable ordered B then stop, A→B→A stale-cancel, repeated Stop and bounded cyclic requests. Owner **Neo.StateMachine / `neo-audit-1023-b06-02`**. Shared global log gate is a separate P3 composition limitation, not prerequisite.

### Neo.StateMachine — reviewed

Full audit/plan, all seven changed runtime diffs, core lifecycle/evaluation/anchoring/composite bodies, all four complete new test/helper sources, existing duration tests and all eight factual documentation diffs read. SM-01–09 local fixes are coherent; source-index mutation remains unregister/register by contract. Current 32 cases assert selected states, events/config validity, source contexts and actual action effects; cleanup owns all assets/objects. EditMode SendMessage simulates callback entry and is explicitly not real Play lifecycle proof. C-16–C-18 remain mandatory bounded repairs. Correcting default shared-duration behavior must precede optional generalized execution factories; keep standard callbacks/standalone custom dispatch rather than mandatory asset cloning.

Aggregate: per-wrapper Update/Fixed/Late and enabled auto-evaluation, priority-view scan/sort on changed priorities, per-callback context lambdas; no built-in duplicate physics query. Shared mutable custom actions and global StateMachineLog.Enabled remain composition risks/contract decisions. Avoid speculative caching that suppresses side-effecting custom conditions; profile counts/allocations without invented frame rates.

### Neo.StateMachine.Editor — reviewed

Full audit/plan, all five actual editor diffs, complete fifteen-case fixture, new Editor.md and runtime integration read. Serialized predicate children, context overrides, Undo before mutation, temporary handle cleanup, ctor-before-insert, membership-before-edit and explicit multi-select limitation are coherent. Fixture reflection deliberately discovers assembly/types without direct editor reference; asserts evaluation/serialized values and Undo, destroys owned windows/editors/assets and clears their Undo history. Real IMGUI removal balance, save/reload and TypeCache remain central execution/visual debt. No confirmed additional editor-source defect found. Canonical README still needs link to Editor.md (document discovery debt). Persistent transition IDs/repair are optional additive authoring work, not prerequisite for safe refusal after reconstruction; runtime must not generate IDs/dirty assets. Per-repaint TypeCache menu creation/one SerializedObject per predicate is bounded by open editor windows, not player per-object query work.

Full read fingerprints for Neo.StateMachine:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.StateMachine.md` | `349e3d2e85d599ad09ce2ce127e3176bd58f1f6d8ac91a0f21ea361b0d7b92fd` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.StateMachine.plan.md` | `fac158f8270eba8243ea9a968067b9943f51faeeab725557f1c61ba2d4239ba8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/ConditionEntryPredicate.cs` | `ee2cc23dd5acf5741e0a7573fc5c824316e6df736da4b82ae19f626f22d99080` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/NoCode/StateMachineData.cs` | `19352e4a3e27dacaf978d3c87ad5883bbd805df25145a4cdeabc76ec5ad3d2c6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StateMachine.cs` | `c734efc5a07dc89aa7fe897e7f7c5256332d335c4e1e04019a57275bc1f09454` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviour.cs` | `1cb0f135d9f1c7753e492ff874ebd56ea2179972d470b70d9c41be92465ecf1f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviourBase.cs` | `e36f98caa0f70092aa4ccd04638abd30cbdab0492f8d54c55a2bc35d5c2f3225` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StatePredicate.cs` | `af9199b54ac340ce237172905fb4aead08891fe8edee262a649bd92bf85c0f7c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StateTransition.cs` | `50f59e64e3f921313de868d0e85a2031c624f0bac908509365a363bf4d858c36` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineBehaviourTests.cs` | `9bde60aa08dd9b94d2ded654702c5274844aed993da7cee33cca3d82ba7903cc` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineDerivedState.cs` | `2ac0c04648ac4f8c6c570974ebd50d2786ef278904910224e24166ceb3fad1a0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineGenericHost.cs` | `b2ab4f7f8349ef54bb14825b0d164d45119f88b05df6712a8ae6f1e7bc4c77d8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineRuntimeTests.cs` | `655f2fee8f5c904365b24b53c0008ff7d68d6a815dfac53a91c7705cd537633b` |

Full read fingerprints for Neo.StateMachine.Editor:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.StateMachine.Editor.md` | `61fb6229d07fad81461c7679fc770b105247c8669cd33ff2dcd004e8ebc16064` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.StateMachine.Editor.plan.md` | `530342f1edcc33a84f477fabd549ee19b4f6a2761f1c541964c8f5bf4e5573ec` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/StateDataEditor.cs` | `c06cc83966d875151fbca10f4cc47f60ddcf6d7e5d78522272f7e91e5188a7b6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/StateMachineBehaviourBaseEditor.cs` | `502656b4c794463bf673abe91e74c02d03108e6f78599fbef685b7c9cea9b719` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/StateMachineEditor.cs` | `af4d44160335c3a72fc9e26570088adc9e2b07cf0a9a8aecf0f22875c9e22d6e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/StateMachineEditorRegistrar.cs` | `c209156d8d5f9908f824f57fe992ed0e09d5b33adeadb837745b0268a5c11933` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/TransitionEditorWindow.cs` | `5b35f6dd83bc7e5d76f38171e2581ce397f34e20485a297fe249c2e04c13f77f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineEditorTests.cs` | `1c1f1cd0f819391366b97005754e3e4eee299899a7d0ecb574ba6f45f32752fe` |


### C-19 / P1 — runtime mesh layout replacement can delete its renderer or a nested foreign rig

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/MeshRig/UIMeshRigLayoutPreset.cs:230` (`ApplyToOwner`) invokes `RemoveExistingPoints:333–354` before creation. An owned point on the rig root causes whole renderer/root deletion; an owned child point containing an inactive nested IUIMeshRigOwner destroys the nested foreign rig, although its own points are skipped. Expected reject protected removal shape before any mutation. Minimum full preflight of all removed branches for root/nested owners using existing resolver, including inactive descendants, then ordinary replacement unchanged. Existing array-return API can report an explicit invalid-operation failure before destruction; new planner/Try API is optional. Add Edit and real Play/deferred-destroy assertions of both hierarchies, keys/mode/count and normal replacement. Owner **Neo.UI / `neo-audit-1023-b08-07`**; editor owner **`neo-audit-1023-b08-08`** already fixed and reported its own path. No extra authoring framework required.

### C-20 / P1–P2 — PausePage and GM have conflicting snapshot ownership

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/PausePage.cs:54–88`, `OnEnable/OnDisable`; current `Tools/Managers/GM.cs:190–233`. Default page opens at scale1, writes0, GM.Pause then saves0; close page restores1, GM.Resume restores0. Expected a single ordinary pause returns original nonzero time and resume observers see it. Minimum capture/order reconciliation with the actual final GM adapter, preserving its corrected event-before/after state contracts. P2 same source unconditionally calls current GM.I.Resume even if that GM was already paused or a different instance replaced it: retain actual acquired GM and whether this page performed its transition; release only its own pause.

Separate confirmed overlapping-pages case: A/B capture1/0; closing A first resumes prematurely, closing B last restores0. Reordering local calls cannot fix arbitrary owners. Use existing shared pause owner/root as immediate documented policy or a scoped common ownership registry; a full cross-module lease/cursor platform is optional, not prerequisite for single-page/foreign-GM fixes. Owner **Neo.UI / `neo-audit-1023-b08-07`**, coordinate **Neo.Tools.Managers / `neo-audit-1023-b07-06`**, preserve serialized defaults and GM public methods. Test baselines1/0.35, existing GM pause, replacement manager and both closure orders when claiming overlap support. No runtime execution here.

### Neo.Audio.Editor — reviewed

Full audit/plan, all three source diffs, complete twelve-case fixture and new Editor.md read. Trim cleanup is only on new slot append and preserves existing indexed trims/replacement; raw IDs remain distinct from popup labels; negative manager lookup caching now actually throttles all drawers. Tests inspect real SerializedObject entries, runtime clip/trim output, options and cache expiry; restore both statics and owned manager in finally, own/destroy clips/host. Actual IMGUI drops/Undo/prefab interaction remains unexecuted. No additional confirmed local source defect. Canonical runtime entry still needs Editor.md link. One global editor lookup per TTL is not per-field search anymore, but repeated list materialization and context ambiguity remain optional contextual-catalog work; reflect actual global AM playback routing rather than imply scene-specific runtime IDs. No player Update/Physics work introduced. Plan correctly retracts direct use of inaccessible NeoComponentHealth.StableId and needs no object-id workaround.

### Neo.UI.Editor — reviewed

Full audit/plan, both changed source diffs, all twenty-one cases and new guide read. Root/nested-owner preflight before Undo/deletion, source-parent duplication, owner-scoped key allocation, final mode Undo record and Selectable prefab recording are coherent. Fixtures assert hierarchy/rest/pose/keys, actual Undo and prefab target property modifications, restore Selection and delete unique temporary prefab assets. Redo, saved-scene/prefab reconstruction and isolated Undo grouping remain unverified; tests observe overrides, not a completed disk round trip. No additional confirmed editor regression. Runtime C-19 is distinct and still open; docs correctly avoid promising Inspector guards on runtime calls. Optional immutable authoring plans/stable diagnostics are not prerequisite. Canonical UIMeshRig entry needs Editor.md link. Search/allocations occur only explicit authoring/preview/repaint, not shared player camera rays.

### Neo.UI — reviewed

Full audit/plan, all nine changed runtime source diffs, affected lifecycle/pool/request/page/category/point bodies, all five complete fixtures and all ten factual guide diffs read. Optional-page/static ownership, CanvasGroup/ToggleGroup gates, category list alias/listener ownership, shake depth, explicit Canvas camera, aspect-space point placement and direct point reparent invalidation are locally coherent. Public/serialized names preserved; KeepAlive ownership transfer is explicit. C-19/C-20 remain required bounded repairs. New tests observe actual states, identities, coordinates, events and pooled representations; hierarchy/assets cleaned, waits bounded. UI statics and singleton assumptions need isolated runtime execution; cached-scene-reference JSON setup and native atlas/mask/panel/raycast paths are unexecuted.

Aggregate work: active flight tween/spawn work and allocations scale with accepted visuals; pool lookup includes space and visuals return/destroy under owner. UIMeshRigElement geometry build creates point-profile/curve data, and geometry builder allocates output arrays; cache immutable templates/reuse caller-owned buffers only with output parity and measured allocation counts. Distinct deformed targets/geometry cannot be replaced with one target result. Actual UI Toolkit/uGUI picking is separate from the shared camera physics contexts in INTERACTION-QUERY-DESIGN. Whole-container owner-cache changes remain a specific hierarchy-policy risk, not claimed solved by point-only tests. Tracked cancellation/status and immutable delayed request plans are valuable optional API work; current destruction cleanup does not claim disable cancellation/terminal status.

Full read fingerprints for Neo.Audio.Editor:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Audio.Editor.md` | `4b8bfc7c9c2ac04be0f2273ab9b1c76c03a6b1c2dd4a8499e698d212de23531b` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Audio.Editor.plan.md` | `3c929e0bac96350190889281ef28e07ffe9bb2e25a8c6ac0b25b16f779577811` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/Editor/AMEditor.cs` | `0e9b745a4211e622946b1f61a2d1e5a0376b219521d0fb140efa331de7d69e68` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/Editor/AudioEntryDrawer.cs` | `75ce6e7578ffaded1251d3b3b8cf0143bc538a30f345d7447f3fe518f8b613f1` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/Editor/AudioIdDrawer.cs` | `216d306898626c5bfe381a927463b0d6ca400cb3df3aac2f63729af6fbb59354` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Audio/Audit20261023NeoAudioEditorTests.cs` | `376fee4913c2cf3067150274953411832542a6e207fffc3fcae383ab294c89c0` |

Full read fingerprints for Neo.UI.Editor:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.UI.Editor.md` | `c7824349f80f216b2c9a47ff8a2611c38cdfb9f4abd27285eb706adc8ca819c4` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.UI.Editor.plan.md` | `1b5a7bc2c7f5a4379b1ab8afdd9e04e81855c66e6dff6bfb5648cf15cdd4f933` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/Editor/UIMeshRigGraphicEditor.cs` | `7dbb76c825d69f7ffaa92aace0fd2456a28e29bdbcc596aef4d3aa9c344850f6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/Editor/UIMeshRigMenu.cs` | `51c6645df2921e290027c81540e79bd1c515e72888407741b49975831fe30646` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIEditorAuthoringTests.cs` | `96970c15199cf31661df91698d3ee8c5042f2100d2cc3fa65c740bce1ca9e087` |

Full read fingerprints for Neo.UI:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.UI.md` | `4426be8ef1faf99ec7ce7b2759471e02cc57ea752bc622a02f83b50793dbe93c` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.UI.plan.md` | `9fcc3e88d5e7f525f26c012b59f6821e15873750e7466eaf36af99e63a5fd699` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/Animation/ButtonShake.cs` | `f26b1ebc5a7a20dc5179eb9e949a9ee2df5d30cf0f60c6efb36d9eda26e49705` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/AnimationFly.cs` | `00c9f1500adf522760faf87ec6aba32bd28c767ebb6f6fbfedd0f0fa69625b0b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/CategoryBar/CategoryBar.cs` | `709d4178c8b9970b49c59ea65b1283bc8e4b4a76b9d3f376d18e7049e019bb43` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/MeshRig/UIMeshRigLayoutPreset.cs` | `eeacda823c27dbe816ccac37842f7ba3545256e6b8c803fcf57e2bc5c2b3ddc5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/MeshRig/UIMeshRigPoint.cs` | `b0bd28e898ef17fb29f4537d32978aeda9bd8e0a7dd08717906a93b05a06d945` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/Simple/ButtonChangePage.cs` | `b8ee2115342d9c67533fe7e3a9d71c65b68d07d962f5b9785f8f651468b56205` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/Simple/UI.cs` | `52707c7c78f41a8ee6c804b420f47b4ff49b49783591f5779078365fb49bdbfe` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/View/VariantView.cs` | `850841fbcbd25317350c6e3cc30a7d40320103f529d2e04287d0016baec19f64` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/View/VisualToggle.cs` | `f61b0fbebe9921d8d120775201bd38f6082c47e4d8c09f4b1b34b2f97bfb2111` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/UI/Audit20261023NeoUIAnimationFlyTests.cs` | `9383a693f217b1a48bcecc90f8fc3fd521921cac67f1004522f183e614d438d5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/UI/Audit20261023NeoUILifecycleTests.cs` | `b5adad1ff7257405b28ba0bc122671975e37653aa2114955d8f06f57eef8fe39` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUICategoryBarTests.cs` | `5c833a3aab199444ac5c2572da8f4761c89c073e7cca74a281293d19ab101d34` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIMeshRigTests.cs` | `6e15676ff91e4e36b532ce18a1436cfb6113a79537e93182b5bbf6fd6200e8aa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIViewTests.cs` | `baf25db691488b9c0685d31265dda2aa747af948a6d3124e9f0b4e03b6d7dddd` |


### C-21 / P1 — invalid save-encryption configuration silently opens plaintext storage

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/Settings/SaveProviderSettings.cs:63–75`, `CreateProvider`: enable encryption with invalid key/IV length or incomplete pair; TryCreate fails, only a warning is logged, then plaintext FileSaveProvider opens the same path. Existing ciphertext cannot be read; subsequent save can replace it as plaintext. Expected invalid required encryption rejects before provider construction/I/O and leaves current provider/files intact. Minimum additive TryCreateProvider plus descriptive failure in legacy CreateProvider; publish facade initialized/provider only after successful construction, preserving retry (`SaveProvider.cs:163` currently sets initialized before construction), and retain previous provider on scene-settings failure. Owners **Neo.Save / `neo-audit-1023-b05-07`**. Test unchanged primary/backup bytes and corrected-settings retry. Already plan P1, independently confirmed. Result/session redesign is optional, not prerequisite.

### C-22 / P2 — Drawer reset callbacks can erase a newly started stroke

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs:286–301`, `ResetPoints`: Distance.Value=0 notifies before raw/rendered geometry is cleared. With a nonzero completed/active distance, a one-shot listener on zero calls BeginLine(Vector3.up); the external reset resumes and clears that new stroke and overwrites PointCount. Expected geometry and both metrics committed before notifications, then version check preserves replacement. Minimum existing SetValueWithoutNotify/ForceNotify and stroke-generation checks across Reset and Begin startup notifications, keeping metric order explicit; test actual first rendered point/count/distance rather than private generation. Owner **Neo.Tools.Draw / `neo-audit-1023-b06-08`**. Current Append guards do not protect Reset; plan independently records it.

### Neo.Save — reviewed

Full audit/plan, all three runtime diffs, settings/facade initialization boundary, both complete new fixtures and all six guide diffs read. Failed dirty flush retention, structural backup recovery, preserve-good-backup first commit, empty-key retention, duplicate-instance ownership and inherited fields/malformed records are coherent. C-21 remains required. The new 22 cases use GUID temp roots, disk round trips/actual save failure injection, field/event results, and an owned additive scene closed in finally. Provider/logging restored and registration caches cleared; singleton/external registered scene state requires isolated test execution. Historical fake-interface/asmdef issues already centrally fixed are not current findings. JsonUtility missing/null containers, fallback File.Replace platform behavior, bootstrap ordering and actual sceneLoaded are unexecuted, not assumed passes.

Aggregate: SaveManager scene load scans MonoBehaviours and reflected fields at discovery/registration, not per-render Update; bulk writes normalize/serialize whole component container and file saves snapshot all keys. Keep explicit checkpoint batching and no hidden scene scan per key. Transaction/result sessions are a useful optional composition API but must not infer durability from void legacy providers or promise arbitrary participant rollback. GlobalSave stale cache/slot generation remains documented orchestration limitation, separate from confirmed configuration data-loss path.

### Neo.Tools.Draw — reviewed

Full audit/plan (including both proposals), entire changed Drawer diff, affected reset/start/geometry/material/pool/deletion/projection bodies, complete twenty-three-case fixture and both guide diffs read. Material records track actual allocations, actual owner pool replaces mutable usePooling provenance, collider conversion/reuse and expiry generation guard are coherent within documented ownership. C-22 remains. Test fixture asserts native object/material/renderer/collider states and actual timed reuse; owns/destroys objects, clears created pools, and explicitly enters/exits Play. It resets PoolManager statics instead of preserving existing ones and uses a tagged camera; run isolated. It destroys only Drawer component in lifecycle case, so full hierarchy/scene teardown remains unverified. Do not equate that case with unload safety.

Aggregate: held input can project the same pointer ray twice per frame (mouse-down plus held branch) but uses analytic Plane.Raycast rather than Physics world query; cache identical projection locally if measuring need. Raw input and smoothing are unbounded before maxPoints reduction; 1000 points/10 doubling passes gives ~1,024,000 intermediates, a structural estimate, no measured FPS. Live follow copies renderer points/list/collider arrays each update. Optional bounded StrokeBuilder/reusable buffers directly address those sites while preserving unrestricted output by default. Automatic Drawer source pool remains registered after owner source destruction; confirmed retained pool/clone/closure references in Spawner require coordinated release API/policy, not an unsafe global Clear. External cross-Drawer returns still lack material ownership transfer; generation guards currently protect expiry only. Treat advertised interop expansion and lease API separately from required local Reset fix.

Full read fingerprints for Neo.Save:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Save.md` | `5c2a584a939db2fe9a102149548dbcaf75cdecd5f67279e259dbf1d802e5da13` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Save.plan.md` | `aefad16b73f7e133b8450eb0b6f8c71bb5ee7f3b89fd66b158bb3b75f4cb80cf` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/Providers/FileSaveProvider.cs` | `a677c26b4ec381ff7f68be318801754d3d36b6bd4b0c40a6622d89193f8e05ba` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/SaveManager.cs` | `0f892ab9c35b1d91e75dac5334f8c630691d422571b9d903b335cd81be18fc64` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/SaveProvider.cs` | `8dd4d03a753a09a8b86d9730763cc1c490217469accff2e4d424a375a29afb72` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveFileProviderTests.cs` | `94859d2e42de107807cd94fac62a23c6a3a3f38401af8b1f78c3b34e754dd568` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveManagerTests.cs` | `9d2ebafe0496dd2da10df6279a7091ca95fd4edff4e8a2854f49e7d40a140c35` |

Full read fingerprints for Neo.Tools.Draw:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Draw.md` | `27f58231f591c6370d30af4dd93c02b5996f4fb3da739c2ada148c1d33c685a7` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Draw.plan.md` | `b6570d3f0023c1ec134deb323bd1340cd3fbf613e1d061396f8fcbf3426f5982` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs` | `3f25cdefd8a55ca33c5e2c9bba2f2c4b4f44dafd5f89e00f49984a8bb008b456` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsDrawTests.cs` | `b0f8467402a5c0d133c078f9b33633a5ca0b21da88329b9ef7a0536ce26b06bf` |


### C-23 / P2 — automatic Drawer pool registrations outlive their unique destroyed source

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs:409–430` creates unique automatic source; `OnDestroy:190–202` destroys it. `Tools/Spawner/PoolManager.cs:35,69–78` retains the source-keyed NeoObjectPool, whose `_prefab`/component cache/ObjectPool retain source/instances; Clear has no registration retirement. Repeat create/draw/return/destroy one hundred automatic-source Drawers: registrations/inactive clones remain until manager dies; the retained pool may later try Instantiate on dead source. Expected disposing this unique owner releases registration/retained resources without disturbing assigned/shared prefabs.

Bounded joint repair, not full lease framework: Spawner owner **`neo-audit-1023-b08-03`** provides exact-source retirement that removes registration, clears inactive objects, prevents further creation, and destroys any outstanding valid rental on late return. Draw owner **`neo-audit-1023-b06-08`** returns its current rentals then retires **only** its automatically created unique source, never assigned prefab pools. Preserve other pools/storage: current storage lookup is by prefab.name and can share a root; retirement must not assume root exclusivity. Test counts/resources return to baseline across repeated owner disposal, outstanding external rental late return, and a second assigned/shared pool survives. Reference-counted shared registrations/general policy API remain optional.

### C-24 / P2 — old Drawer cleanup can change a reissued renderer and its material

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs:680–741` (`DestroyLineMaterial`/`Delete`), OnDestroy and `_lineMaterials`. A creates pooled line/material, external caller returns it, B rents same renderer and installs its own material, then A.Delete/OnDestroy runs. Only delayed expiry captures generation; Delete returns B's active renderer, and A's material record can be a borrowed/restored material in B. Expected every old-owner operation leaves current rental activity/material/geometry intact.

Minimum capture `(OwnerPool, SpawnGeneration)` for every pooled acquired renderer and check before all instance mutations, not only timer. Add a one-shot generation-specific release/eviction cleanup on existing metadata/common pool release, **outside cached IPoolable discovery**, so a material owner attached after creation can restore/free its exact clone before reuse. Preserve established IPoolable/network callback context/order and cleanup once. This can be a small typed hook, no general registration/lease registry required. Tests: external return/reissue with two Drawers, old Delete and full OnDestroy, capacity eviction and no retained/destroyed borrowed material. Owners **Spawner `neo-audit-1023-b08-03` → Draw `neo-audit-1023-b06-08`**. Existing generation expiry test is useful but insufficient. Full arbitrary pool-callback rollback is not requested.

### C-25 / P1 — callback restart occurs before Spawner owns its coroutine handle

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/Spawner.cs:232` (`StartSpawn`), `SpawnObjects:266–314`, `SpawnObject:457`. StartCoroutine advances synchronously to first yield before assignment to `_spawnRoutine`. A one-shot OnObjectSpawned listener calls StopSpawn then StartSpawn: Stop sees no current returned handle yet; inner loop B starts, outer A resumes while shared isSpawning is true, then its returned handle overwrites B. Both produce; future Stop can cancel only A. Expected only latest production session continues, independent delayed expiry preserved.

Smallest per-production generation allocated before coroutine dispatch, validated after external callbacks/yields and before returned-handle publication/final isSpawning write. Superseded handle must not overwrite/stop current B; public manually-started enumerator ownership remains explicit. New real Play regression restarts from first spawn/wave callback and observes only two initial spawns then latest cadence, final Stop and no resurrection. Owner **Neo.Tools.Spawner / `neo-audit-1023-b08-03`**. Current direct StopThenRestart test starts outside callback and does not cover the synchronous first-step window.

### Neo.Tools.Spawner — reviewed

Full audit/plan, all eight changed source diffs, full NeoObjectPool/PoolManager and relevant producer/despawner/fallback bodies, all four complete new regression/support sources, and all ten documentation diffs read. Foreign/double return, destroyed inactive retrieval, max-retention normalization, duplicate manager prewarm guard, Despawner per-generation reentry and Spawner snapshot Clear/count/expiry are coherent within documented lifecycle-callback restrictions. C-25 remains; C-23/C-24 are bounded joint work required for Draw composition, even though original Spawner plan calls registration retirement out of feature scope.

Fixtures observe activity/owner/count/geometry, preserve Random.state/timeScale/fallback policy, clear owned pools/spawners and destroy objects; host tests stop host and clear spawners. Global ClearFallbackPools/NetworkClient.ClearSpawners and singleton assumptions require isolated native execution; symbol-gated zero-discovery is not a pass. Callback fixtures cover reentrant spawn on release, but not first synchronous production dispatch. Mirror active-registry unspawn source matches intended before-storage order; weaving/host/remote custom handler behavior is unexecuted. Pool lifecycle callbacks remain forbidden from throwing/returning their own transitioning instance; do not widen these guarantees accidentally.

Aggregate: configured per-producer coroutines/finite retry collider sampling and pool component-cache discovery, no shared camera ray. Broad IPoolable discovery occurs once per created instance; cached set intentionally excludes later-added hooks, hence C-24 cannot rely on adding a late PoolableBehaviour. Source-key pools/retention and inactive storage are actual retained resources; this is not an invented FPS issue. Single-load fallback created during incoming Awake then cleared by sceneLoaded remains a concrete integration scenario to execute before claiming scene safety; true ordering/provider context must be observed. Capsule test covers containment rather than uniform density; preserve per-axis/scaled collider semantics. Optional PoolLease/pose-first/warmup/active-limit/network-assembly extraction are independent proposals.

Full read fingerprints for Neo.Tools.Spawner:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Spawner.md` | `a717e6015f25c74776db060424860a5ee84dc9f4a4c6c01823a82aabe5b717d9` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Spawner.plan.md` | `9acd54eaba67db1217b610fa33ba959fd0ec625c8c6707bc43e96b173ce35329` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/Despawner.cs` | `f639b78e394e68e0273c9fccae347695376af627ebc51a06cbf3575f8e6a5ddd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/NeoObjectPool.cs` | `30da991d202d8d75732963e1f4c918f0ddf8e41d25e43870cfbb361cbdcb57d2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/PoolExtensions.cs` | `ee1845f4df7fa9554762728bc6a23f1648240898bed3e1cef05c6eacf92b820b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/PoolManager.cs` | `ebaf2fed22f60a56438c028a08f633b20a11b8f5050e60ca6c41a485df65c1c6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/PooledObjectInfo.cs` | `7120f1b50d71ca39aa6bd20fdfb67065b4e461e17db585f50e83d070e54205f6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/SimpleSpawner.cs` | `837cb18b281e139cc86dbe52171ff4ffad2e80f9860fbc59a48a865e56cb92f3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/SpawnUtility.cs` | `7fbf7baf53401b9c9386cb87e7f39d0f35527023a849a0dcf98b74f84ad8fbaa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/Spawner.cs` | `a6e0fdfe80e3856a3da3a6e0e9ca02fcdb0f3a33928e5711dbc4d8fd3b53d2c6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Network/Audit20261023NeoToolsSpawnerNetworkTests.cs` | `2b9fb58d5fbb0faa4fe6a386175bb3536c1cde13d2d61a45d3613bb373f09cdd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsSpawnerPlayTests.cs` | `0af6c7682182c79b8b8b9f6bd2a812a092e5dd9d394efa92abe112a2b9bbee3b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsSpawnerPoolProbe.cs` | `f069948a6367c7f04acc0400bdf080cf5f778d89e2f452843eaef6e548df738b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsSpawnerTests.cs` | `56849da7c6eb2be557d98697e3991986601dd2462e7196ae3c6b694f6be73bcc` |

### C-26 / P2 — initial NPC mode event can disagree with the behaviour actually entered

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NPC/NpcNavigation.cs:164–165`, `Awake`, notifies through SetModeInternal before initialized=true. Listener calls SetMode(Patrol); pre-initialized branch at 302–305 only assigns mode, so currentBehaviour remains FollowTarget. OnEnable:175 enters FollowTarget while public Mode says Patrol. Expected latest deferred mode is selected before the single initial Enter. Minimum post-notification reconciliation without an intermediate Enter/duplicate startup event; preserve existing defaults/serialized fields. Add real Awake callback-mode-change test with one patrol-start/path and no follow entry. Owner **Neo.NPC / `neo-audit-1023-b04-04`**. Plan confirms it; independent source trace agrees. Full planner/snapshot proposals are not prerequisite.

### Neo.NPC — reviewed

Full audit/plan, all seven changed source diffs, relevant full initialization/follow/patrol/combined/restoration/target bodies, resolver, both complete new fixtures and all seven factual doc diffs read. Pre-Awake deferral, off-mesh native gates, persistent core Stop, dirty first path, partial-arrival rejection, acceptance propagation, one Combined state, snapshot-before-hold and destroyed-reference repair are locally coherent. C-26 remains. Current mode snapshot preserves only documented target/mode/activity; does not promise custom speed/patrol cursor or competing-owner arbitration.

Twenty-seven prepared cases assert actual native paths/preconditions/events/Animator values and context state. Fixture builds/removes only owned transient NavMesh data and restores Random; objects/controllers owned, teardown separates GameObjects and resources. Manual Edit callbacks are not proof of actual lifecycle. Existing project NavMesh surfaces/agent type settings, immediate remainingDistance/velocity timing and transient Animator resources require isolated central execution; no nonparallel attribute or native scheduling pass is assumed. Partial-path tests require a demonstrated native partial path rather than skipping. No source-only claim of native success.

Aggregate: brain decision interval and retry finder are already throttled; empty acquisition duplicate was removed. Follow/Combined misses can still make up to 61 NavMesh.SamplePosition calls per attempt, then retry each Combined tick; no measured FPS stated. Typed planner/backoff is optional larger design; a small opt-in retry interval can bound work while preserving legacy frequency. Identical raw sample contexts (desired position, radius, agent/filter/area, NavMesh epoch) may share sampling only with explicit invalidation; actual path/connectivity starts and agent SetDestination acceptance are unique and cannot share one chosen route/arrival. Mask/tag/blocker distinctions of physics targeting are inspected with RPG below. Keep strict destination/path containment and uniform sampler policy separate; current docs accurately disclaim them. Full navigation takeover token/snapshot is optional, not mandatory migration to a new combat framework.

Full read fingerprints for Neo.NPC:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.NPC.md` | `eb7790c1b02b62aec90b753bd4550885a3e9aa9162d0f091d311838e8dd181b5` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.NPC.plan.md` | `c1a563cd468ced09fb71fdc328b4290a15db5298f6a6eb4885264dd3e9498394` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NPC/Combat/NpcRpgCombatBrain.cs` | `c4770527c802ecc4a773dbc7059fe86def72f5d93a5b62283fb4438133dc3ea8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NPC/Navigation/NpcAnimationCore.cs` | `074a2cc76ccdf2a7bf54dace95bee8e6aa06a8dc4979580a82afe96f4cf3f1fa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NPC/Navigation/NpcFollowTargetCore.cs` | `44b4a8563939c8ea5eebd46fd8ef6caa4bbeb3ac62b17c77aa6bff850c1a6869` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NPC/Navigation/NpcPatrolCore.cs` | `230b996e6822c64763dc70fda0d0b204040fbd7284b4956cd735f26c787b3a52` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NPC/NpcNavigation.Behaviours.cs` | `d515e3dc339e124b810b6b15f3e1a4c80bca44993f6854531869319cfa02e916` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NPC/NpcNavigation.cs` | `be50da623816bb79cb975ff50cc714d3c69bb60e9f3c693c647f064a9331cb78` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NPC/NpcTargetFinder.cs` | `eec324f98dc790a0799e16b7c2bb1a5ad0ff46321c16b6b417141ba642ced2a4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/NPC/Audit20261023NeoNPCComponentTests.cs` | `8d3889a7e237b0726c5d7b3086441aefaba85b87f042d7a7ba4c977362eff855` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/NPC/Audit20261023NeoNPCNavigationTests.cs` | `2530695e09fb9899e9424359e4c7bd80d7693352542d9c6ea3ca28dc04585f25` |

### C-27 / P1 — resource/stat callbacks can rebuild live dictionaries during their traversal

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterResourceService.cs:171–177` RestoreAll, `209–261` TickRegen, `278–304` RefreshDerived; facade `Components/RpgCharacter.cs:1221–1252` RefreshAllDerived/RefreshStat. Build A/B, subscribe first resource reactive/ResourceChanged callback (or derived resolver) to Build C=7. Setters/resolvers call external code, then the old pass continues/stale-notifies or next MoveNext throws after new entries. Stat reactive/OnStatChanged can similarly RebuildRuntime inside facade's stat foreach. Expected replacement C remains7, no stale old resource mutation/notification or invalid enumeration.

Minimum generation on Build/Clear/rebuild and checks immediately after **each** external resolver/reactive setter/notification before subsequent writes/events/MoveNext; capture current entry identity when needed. Existing EffectShelf mutation-version approach is reusable, no full transaction service prerequisite. Cover RestoreAll, continuous/tick regen, max/regen resolvers and facade stat refresh with observed C/state/event identities. Owner **Neo.Rpg / `neo-audit-1023-b05-03`**. Calling a dictionary snapshot alone would still mutate obsolete runtime objects, so it is insufficient.

### C-28 / P1 — regeneration/status loops can fail to terminate for invalid or huge steps

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterResourceService.cs:243–255` TickRegen and `Runtime/RpgEffectShelf.cs:274–290` Tick. A not-full tick resource or live damaging status, finite positive interval, delta +Infinity: accumulator stays Infinity after subtraction and loop never ends. Large **finite** float delta (e.g. float.MaxValue with interval0.5) also does not decrease when subtracting that interval due to float precision, so IsFinite alone is insufficient. Expected public tick returns without poisoning state or unbounded notifications; ordinary cadence and pause-crossing behavior preserved.

Minimum finite guards for delta/interval/rate/accumulator arithmetic plus subtraction-progress or explicit bounded-drain policy; sanitize/reject unsupported catch-up with documented residual semantics. Do not run known infinite repro on unfixed code. Add public tests for NaN, infinities, overflow/nonprogress finite step, then ordinary fractional/multiple ticks, callback removal and pause timing. Owner **Neo.Rpg / `neo-audit-1023-b05-03`**. This is a bounded correctness fix, not prerequisite for a simulation-clock/state framework.

### C-29 / P2 — unrelated derived refresh applies disabled/manual/deferred level growth

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs:1009–1040` SetLevel gates auto-growth, but `RefreshStat:1249` always passes live `_level`; `Runtime/RpgStatResolver.cs:25–29` always evaluates affectedByLevel. Wisdom base10/+2 per level, ManualUpgradePoints or None (or autoApplyGrowthOnLevelUp=false), SetLevel(3) leaves10; apply empty buff calls RefreshAllDerived and changes it to14. Expected no growth until permitted/explicitly applied, and unrelated buffs cannot change policy. Minimum applied-growth state/level passed to existing resolver, explicit apply command if needed, correct upward/downward automatic paths; no model extraction required. Owner **Neo.Rpg / `neo-audit-1023-b05-03`**. Test all modes, deferred explicit apply, repeated refresh and profile policy; keep upgrade points separate.

### C-30 / P2 completeness/ordering risk — projectile raw hit buffers silently saturate

Current source `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgProjectile.cs:91–128`, HandleHitsBetween: 16 (or configured `_maxHitsPerCast`) 3D/2D hits are consumed directly with no saturation handling, and 3D hits are not distance-ordered. A segment crossing more colliders than capacity can omit an eligible receiver, especially when returned entries are self/duplicate/otherwise rejected hurtboxes; ProjectileMaxHits then does not describe all reachable impacts. Exact native membership/order requires central deterministic scene evidence; the silent incomplete-query structure is confirmed, not a measured failure/FPS claim.

Smallest next action: original owner **Neo.Rpg / `neo-audit-1023-b05-03`** adds dense-3D/2D public impact regression with ignored/source hurtboxes and near/far receivers; establish `_maxHitsPerCast`'s documented work-budget versus raw-capacity meaning before repair. Complete raw context via grow/requery or full fallback when saturated, then deterministic distance/tie and existing delivery/ProjectileMaxHits policy. Preserve actual configured gameplay limits rather than silently disabling them; expose explicit incomplete/budget outcome if intentionally bounded. Do not cache selected receiver globally. Direct/area allocating casts have complete arrays but their hit-order policy is a separate contract; changing target ordering/3D→2D precedence needs explicit acceptance.

### C-31 / P2 — RPG bindings still deliver automatic UI output after disable/rebind

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgResourceBinding.cs:117–130,161–174` and `RpgStatBinding.cs:107–110`. Neo.Reactive snapshots all live subscribers; removal during delivery does not cancel that snapshot. Earlier listener disables binding, OnDisable unsubscribes, then snapshot calls unguarded handler and emits UI output while disabled, contradicting both guides. Initial resource OnCurrent listener may disable/rebind; outer SubscribeIfReady still emits old Max/Percent afterward. Expected automatic output only for current enabled binding/session.

Minimum enabled/subscription-generation checks in automatic typed handlers and after initial output callbacks; prevent obsolete initial continuation after source/ID/rebuild change. Retain exact old-object unsubscription introduced by audit. Regression uses an earlier reactive listener plus event/text counts, and initial OnCurrent rebind/disable observing no stale subsequent fields. Owner **Neo.Rpg / `neo-audit-1023-b05-03`**. No general binding-session API is needed; NoCode's existing snapshot guards are a local reuse example.

### C-32 / P1 — active-server snapshot capture recursively reinitializes character state

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs:243–269` EnsureInitialized refreshes before ready; RefreshStat:1252/NotifyResource call PushSnapshotIfServer; `RpgNetwork/RpgCharacterNetworkAdapter.cs:65–71` synchronously CaptureProfile; character `CaptureProfile:1399` calls EnsureInitialized again. Active server, enabled/resolved adapter (Damage(0) resolves it), ApplyTemplate/RebuildRuntime with stat/resource reproduces recursive Build/refresh/capture before initialized, risking stack exhaustion/dictionary replacement. Expected one completed rebuild and valid final snapshot.

Minimum core initialization/rebuild publication guard acquired **before** collection clearing, finally released, readiness only after consistent state, one final network notification after required loading/derived resolution. Adapter-only recursion guard still permits the first nested rebuild and is insufficient. RuntimeRebuilt currently precedes LoadProfile, so do not relabel it as complete-profile commit without documenting order. Owner **Neo.Rpg / `neo-audit-1023-b05-03`** first; **Neo.Rpg.Network / `neo-audit-1023-b05-06`** adds active-server final-payload regression after core repair. Do not run stack-overflow repro on unfixed tree. Snapshot coalescing/protocol envelope are separate optional work.

### C-33 / P3 — Combat guide still describes the repaired shared timer as broken

`D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/Combat/README.md`, Health Integration limitation paragraph. Current `Tools/Time/Timer.cs:274–306` creates cancellation before start notification, validates cancellation, and delivers OnTimerEnd for completed looping cycles. Original Combat audit's R1/R2 are historical and its plan correctly recognizes source repairs. Update public wording to source fixed/runtime integration pending, then actual results after central execution; do not claim auto-heal works based on source alone. Owner **Neo.Rpg.Combat / `neo-audit-1023-b05-04`**; link canonical topic from parent/legacy entries and retain existing URLs. Folder metadata status is coordinator-owned; historical absence is not reasserted here.

### Neo.Rpg — reviewed

Full audit/plan, all nineteen actual changed source diffs, relevant complete resource/targeting and facade initialization/growth/profile/binding/projectile bodies, all three complete new fixtures and eleven guide diffs read. Profile stack/null cloning, upgrade rollback, tick-unit/derived regen, cast cancellation, exact evade lock owner, projectile preflight, legacy stack/slow factors, generic revival and invalid upgrade rejection are locally coherent. C-27–C-32 remain; C-30 is confirmed structural incompleteness requiring native/contract resolution, not claimed deterministic result. Forty-two prepared cases assert actual rates/stacks/HP/locks/events/targets and stale-object detach; objects/assets owned, no new shared config. Play fixture uses tagged Player and world position20000; isolate external actors/camera/NavMesh state, native timing and provider boundaries remain unexecuted.

Contract decisions separated from mandatory fixes: live SetMaxResource writes derived Max while resolver starts from BaseMax, and v2 CaptureProfile records final Max. Explicit160 can refresh/load back100; treating saved final Max as base would double-count modifiers. Additive base-max setters/versioned presence field are sensible optional v3 design, but legacy setter semantics/v2 migration need root decision. Likewise AddResourceCurrentFlat, AttackSpeedPercent and legacy HpRegenPerSecond lack complete consumers; one-shot grant versus refresh/load and next-versus-current cooldown need product contracts before implementing. Do not advertise these as implemented, or make full transactional service prerequisite for guards/growth/init fixes. Effect UTC versus simulation time is a documented legacy contract; opt-in simulation clock is separate.

Aggregate: character derived refresh iterates every stat/resource and publishes each change; adapter currently clones/sanitizes/serializes the full profile for every NotifyStateChanged, plus wrapper calls can add another capture. Cost scales with notification count × full profile size; actual allocations/bytes should be measured, no FPS invented. Target selection allocates complete 3D/2D overlap arrays/list/receiver set per request and deduplicates receivers after querying. Identical raw context/epoch can share complete overlaps, while selection mode/dead/action status remain per caller; distinct origins/ranges/masks/scenes/trigger policies stay separate. Unique projectile segments and target LOS cannot be merged. Preserve mask versus receiver/tag/blocker decisions; target-layer filtering already excludes unrelated geometry by contract, do not rebrand it as a wall/LOS blocker query. Aura queries use authored transformed center/absolute scale and per-call receiver dedup; coincident identical contexts may share discovery while each aura's damage remains additive.

### Neo.Rpg.Network — reviewed

Full audit/plan, both complete changed diffs/current adapter bodies, complete nine-case fixture, new Network.md and core boundary read. Current opt-out ingress guard, late client flag refresh, source-first/parent identity fallback and lazy wrapper character lookup are coherent. C-32 is required cross-owner repair; adapter-only recursion workaround rejected. Tests exercise actual woven serialization/registered delegate boundaries, restore Transport and stop client/server, destroy owned actors/templates. They deliberately initialize core before installing adapter, so do not cover rebuild. Dummy transport/lifecycle replay proves neither packets nor ownership/reconnect; weaving/discovery/real remote client remain central debt. Full JSON publication per notification is confirmed source cost; optional dirty revision/interval publisher must retain complete initial/late-join state and zero-interval compatibility. Legacy client arbitrary command opt-in/rate/authority remains explicitly retained; no new acknowledgment/validation/protocol guarantee inferred.

### Neo.Rpg.Combat — reviewed

Full audit/plan including dependency correction, all five source diffs, affected health/attack/evade/hitbox bodies, all three complete new fixtures, canonical README and current Timer dependency path read. Integer sign/overflow/caps, serialized HP synchronization, once-per-damage-transition death, attack generation across first synchronous dispatch, managed collider cancellation/filter gates and evade next-cycle/deferred cooldown are coherent. Twenty-five cases observe real HP/events/public timing states and collider windows; Enter/ExitPlayMode cleanup owns actor, callback filter tests deliberately use SendMessage and do not prove engine contact/knockback. Current shared Timer repair exists; C-33 public wording pending. Auto-heal and cooldown-start reset/disable integration must execute, not retain old expected-fail label or assume pass.

Aggregate: auto-heal uses one owned timer only while configured/enabled, attack/evade work request-driven, physics callbacks are unique contacts rather than redundant shared camera rays. Per-collider repeat suppression remains explicitly different from per-receiver AoE dedup. Optional typed bridge injection reuses existing IRpgCombatReceiver/RpgDamageInfo without mandatory new framework; bool forwarding result must not promise immediate remote HP change. Native force/effect lifetime and arbitrary finite/timing public-field policy remain integration risks; do not silently migrate compatibility API to Abilities.

Full read fingerprints for Neo.Rpg:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.md` | `a3d955102c6a156c6e27b45b53d97cd781df21d842416e214f5e42c66662f9d8` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.plan.md` | `eb4e8deffac22ae999d41e5f97e9530d972ef879065ae748ee538fff601a8f74` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgAttackController.cs` | `b826bf64fbf932aad55dae867370112a7391d0a2dff17d62a38380f8982a4c7e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgAutoAttackController.cs` | `5e00b458997218033b1f96ad89f9d418e4a6d9e8ef630057131888b52e58a28b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs` | `dd2e3bdcd5289c72b126c4101394541f0087a02cd01bd9e3266847efe1275daf` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgEvadeController.cs` | `40cb0db451fbcfb99a7fa7e7d243e743a2ced3797a9f7265971f0000ae36d2a8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgResourceBinding.cs` | `75fc19a3b9674c43edae39c8450a9fb61dea4e3023d79e1dffa0fd11b7084949` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgStatBinding.cs` | `8137d4cbae50cad293a463ddb22f6cf4c87f3917a652b58ff9b40d3494c0b87a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgTargetSelector.cs` | `eba488d7f180eacc3f2bc727fea58eacdda058bc60398b05c28b6db9572cc25f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/Weapons/AuraWeapon.cs` | `601a690adf1cfec3ec0b89b308a9f2d5694c5db3348b2195a6d04c4aacc4a7b2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/Weapons/RpgContactDamage.cs` | `b5296f7e362e2b70f9ff6b357dcc119f2dc59382f2f8d04b9b4769b7a56c6fa8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Data/RpgAttackDefinition.cs` | `04c5d7bc0dd43e2cc66e411aa0efb19d39b386ddc7876fbdf572a79efc8f866c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Data/RpgRegenMode.cs` | `6ffec2d1bd944e49093c10131d1c1841d5cc8994dc8561fc851729fcbacda3a2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Data/RpgStatUpgradeRule.cs` | `486e48dd7878302ce44cf15135e2bb0d2d11b799dd7b2ee00f80e6f49c00e36e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterProfileData.cs` | `504467f86274c0e869c355eb6a3ea13d1fd903fe36360175ac521dfbaa60ec67` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterResourceService.cs` | `57562958371244a698f7293300de28c9f0e3b174aa36532c50d422d8c19a73d9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgEffectShelf.cs` | `b41e90d340c012ea332e7b929f0b7ec592619f1be732409f44af8d866b4f1506` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgResourceRuntime.cs` | `cac5b5a11e816ac472a8e28a8a9eff6e774605da5c2deb0e88e683f127d0192b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgStatResolver.cs` | `cabb70bb7aaf8224fe8a8822d631f47caaf637e148be9bed30af54da2f749f68` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgStatRuntime.cs` | `b9c26034a4de67d5c7d9a7ee9340eb2b72410cbf2f28a285b5fd40d1e57371a6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgTargetingUtility.cs` | `ff6920420fbc30126b5586dc7a476865efc144047790b2652c9ce61fe660a721` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgRuntimeTests.cs` | `6fc8d21bd5c24cf887cf0ca21e947341274c7a41be6fce68cfcfe1564d6f051e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgCharacterTests.cs` | `5e1e4caf8d9732de5949bfef169e73ffcbf85cfacc20e283a509cc673720543e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/RPG/Audit20261023NeoRpgLifecycleTests.cs` | `894f99a3b9b04ffec4b3153870980c95313589140c095d93522453f7102e6b3f` |

Full read fingerprints for Neo.Rpg.Network:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.Network.md` | `6d8c075297f367b37078848bb33b97944f3faee5d3e81bf1a1d7a592e67d470d` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.Network.plan.md` | `894fa7cf147a094e52352f00a7545c4d5e0cd2544959f4e4e6955ef5e3069c3f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/RpgNetwork/RpgAttackNetworkAdapter.cs` | `a9e143727c687e901abefaea8f175d76957049baa30da1884652d65057de9163` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/RpgNetwork/RpgCharacterNetworkAdapter.cs` | `7c8a4fddbd1e2c65e52a4dc85ee515e0deaed7fcfd6bab6b30440269891110d0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Network/Audit20261023NeoRpgNetworkRegressionTests.cs` | `9a7c03a73dc090f8614779cb724def54c00860feba5ea3ed8cdb14acf86dd166` |

Full read fingerprints for Neo.Rpg.Combat:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.Combat.md` | `78e773b6b360129be577080de92df7176ce1a3eb1097d8d085085c8ce48075bc` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.Combat.plan.md` | `39e729176e0ed0f7b815801a767ca0132744c73328a1c65b8bf100e96702cee3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Combat/AdvancedAttackCollider.cs` | `c8144fc7d3c1ae1e39e703b8d75e1a400a3e84708e3bf683a60b376936042cee` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Combat/AttackExecution.cs` | `dc84d073974d5c717afd20b94656546a794762283745a3cbe133f7b34fddf60c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Combat/Evade.cs` | `06725944900d4f491b72600dfb9697b77466a4ba8005ac6f25ef853311da4ba2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Combat/Health.cs` | `ecf91483c1a9c0ae380f55742c2093c2911ccefd3120a1c20e6c60a06763162e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Combat/RpgStatsDamageableBridge.cs` | `c1ac4bc054004b5ca770a9569c66b1491449e918c9e9f6ca10b90ad68ecef42f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgCombatHealthTests.cs` | `c62d2da23f3d2c2e73007896f6bbb494aa24ea4679e337d4c41088713bcf0de1` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgCombatHitFilterTests.cs` | `e50692bce96f95f9e4d150ba2cc8b3aa51899ae8abb574a814eeb67f693b797c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgCombatLifecycleTests.cs` | `1bc499a25ad78359e2f13d1a13c7e858513ab28a6d9838271d51269fe8c4e5e4` |


Coordinator policy recorded during review (C-20): root explicitly requires the standard PausePage path to share minimal first/last ownership without optional scene wiring: time/cursor baseline retained until last page, exact acquired GM/own Pause only, GM Resume before final restore, prepaused/replaced GM untouched. Active cursor requests override closing-page ForceLockedHidden; final policy deterministic. Full PauseCoordinator/Level/Move integration remains optional. This refines the earlier report's immediate single-root mitigation into mandatory bounded default repair.

### C-34 / P2 — demo shell/controllers leak their generated UI and native assets

`D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/Shell/NeoDemoShell.cs:54–81` builds unparented Canvas; `GetGradient:191–219` creates Texture2D/Sprite every call and overwrites static reference; Context:226 has no lifetime API. AudioDemoController BuildClips:156 makes six clips, OnDestroy:147 only unhooks events; LevelDemoController creates curve:86, OnDestroy:69 only unhooks. Create then remove only the demo controller repeatedly: old UI/callback targets/native assets survive their owner (some audio still referenced by persistent AM). Expected generated session resources release while unrelated shared camera/manager/audio/assets survive.

Minimum additive owned Context.Dispose/owner overload and controller OnDestroy disposal, plus exact created clips/curve consumer teardown before object destruction. Stop only demo-owned playback, including outgoing voices if necessary via narrow Audio-owned release, never global Stop as ownership inference. No generic resource/session framework prerequisite. Test twenty create/dispose cycles after shared-cache warmup, deferred-destroy counts, component removal, two concurrent demos and double Dispose. Owner **Neo.Rpg.Demo / `neo-audit-1023-b05-05`**, Audio **`neo-audit-1023-b02-03`** only if existing playback API cannot release exact owned voices; Quest registry detach contract is separate before quest-definition disposal. Current plan correctly treats native ownership as required despite calling broader session work proposal.

### C-35 / P1–P2 — Survivor teardown deletes pools by name and leaves their registrations

`D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorGame.cs:84–107`: tracks `template.name + "_Pool"`, destroys manager child root by name. PoolManager:80–90 can share that root for different same-name prefab identities and `_pools` still retains both entries. Dispose one Survivor with same-named unrelated prefab: its storage can be deleted; repeated scenes retain dead template keys/pools. Expected retire exact owned registrations after returning own rentals, preserve other prefab pools.

Reuse bounded retirement from C-23, store actual template identities/registration ownership, do not infer ownership from names or reflect into dictionaries. Shared root destruction must be reference-safe; outstanding rental late return must follow retired policy. Owners **Spawner `neo-audit-1023-b08-03` → Demo `neo-audit-1023-b05-05`**, joint acceptance with Draw. Test distinct same-named templates, active rental teardown, repeated recreation and unaffected external pool. General shared lease framework remains optional.

### Neo.Rpg.Demo — reviewed

Full audit/plan, all eleven sample source diffs, affected view rebind/resource/controller/camera bodies and actual shell/Audio/Level/Survivor teardown, both complete new fixtures and canonical Examples README read. XP threshold widening/clamping, preservation of authored Dice assets, generated fallback with visible value, sample-root-relative prefab lookup, view subscription cleanup and generated sprite cache, configured Condition amounts, exact death-listener removal and optional health-bar resource cleanup are coherent. C-34/C-35 remain. Preserve release Samples~; fixes must also work from developer and imported root without moving package content.

Seventeen prepared cases use optional assembly discovery/reflection at public boundary and real state/renderers/events; absent sample/import and unsupported new-input-only Dice case explicitly skip. A skip is not acceptance; central run must import sample and count executed cases. Play fixture restores disabled existing cameras and destroys owned hierarchy; Condition test avoids best-score persistence but globally selected singleton/provider and native TMP still require isolation. External canvas/root, drag cancellation, Dice fallback disposal and player-without-editor-loader scenarios remain unexecuted debt. No assertion of compile, sample import, screenshots or input support made here.

Aggregate: sample health bars throttle scene-target scans but project/update visible targets each frame; unique per-target projection/LOS should remain. Views execute picking only on their configured active board/input, not a demonstrated N-per-target identical physics ray; shared raw query design applies if multiple identical contexts are active. Shell clips/textures are startup allocations and leaks, not invented frame-rate evidence. Data/session disposal should reuse existing services rather than migrate example combat/modules. Direct Mirror reference/new-input-only support are packaging/input contract risks for root's isolated import matrix; do not silently remove references/change project settings.

Full read fingerprints for Neo.Rpg.Demo:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.Demo.md` | `c92d9de45dbb8ea0561094225c858f0d9ae48500d719557078554986250680a1` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.Demo.plan.md` | `331be2a6e5ec439f32299838191c01f3f5480c9a1a324e1702ee31a587ad0190` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/Condition/ConditionDemoSetup.cs` | `9e650f4784f4881a7eed2128fd2b51c2b5f3b83b07eae5451acdc65914a4026d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/Condition/ConditionDemoUI.cs` | `14693bcc63678764b84ac6bf93db609d45772ff5aa9f7839ab31a4d941b55231` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/Dice/DiceDieView.cs` | `7c9f3984e00a425613d1361de2760985979539320dabbd4f87610037bab34a35` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/Dice/DiceMergeDemoController.cs` | `fe5b58a277d00c51400f26653d4e0c2038fd0e48b2318b7fd3e33502c0bd41d3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/Dice/DiceMergeDemoSetup.cs` | `f9753f5496852833dc1cc7afd92d22cba8b660e4f8c3862a7a5729b14549ce25` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/Dice/DiceMergeDemoView.cs` | `475ae0aee57507c22ea9fc39782ab689be06a258c695f7284328ab7b43541c25` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/GridSystemMatch3BoardView.cs` | `ac2d66a84e776ee5497599e37fa89ee777af6ca7c455eb72c7e2416b7bc53e07` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/GridSystemTicTacToeBoardView.cs` | `ad3bb6c1180fe4bff96285409ffef74a1f02bd2daf8d2563cad7d6d73edded5d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/RpgCombatDemoController.cs` | `436f4135f47147bf9b075e741912d22de48d4064f0a359b4700bcd440f4ae6c7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/RpgWorldHealthBars.cs` | `1f74bdf80797361409274c075bcb79e07f0990c7c48a1e1dd2ff4a9607fbb49e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorConfig.cs` | `2acdac259d3a33306feea6f5d78e2fcb01c983d9158c09a1956a620600578368` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Audit20261023NeoRpgDemoLifecycleTests.cs` | `841d86fdce059d68563d39e15af5566413c8e1576266f54de12e0cc219ebea51` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoRpgDemoTests.cs` | `a677ef63970d30544b5d93ae4eccf71abf01ebf2b12d10fab06e34ffa4103871` |

### C-36 / P2 — replacement animation callback can create two owners for the same key

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Animation/UitkAnimator.cs:220–231` Start, Finish:343–360. Start fade A; one-shot Cancelled handler calls PlayManual(fade, same element, Off); external PlayManual replaces A. Finish removes A and callback starts C, then outer replacement checks only disposed and appends B. ActiveCount becomes two for one (element, animation ID), with concurrent style writers and restoration snapshots. Expected latest reentrant same-key request owns the run; unrelated element/ID starts remain unaffected.

Minimum key-scoped request generation/replacement guard before callback dispatch and before committing run, not a rescan/cancel loop that can recurse forever. Add real-panel one-shot cancellation-restart test observing one active run, correct eventual/restored styles and stopping latest run once. Existing cancellation cases cover StopAll/Dispose, not restart. Owner **Neo.HtmlToUitk.Runtime / `neo-audit-1023-b03-08`**. Strict metadata compiler/general execution framework is not prerequisite.

### C-37 / P1 — opening/reloading a job discards dirty edits without a decision

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.cs:369–452`, LoadJson:427. Edit A, Open B/Reload/New, return A: LoadJson unconditionally replaces _doc and clears dirty/hasUnsavedChanges after parsing, with no Save/Discard/Cancel guard. Expected user choice before replacement; Cancel/failing save preserves document/path/dirty. Minimum common replacement guard and result-bearing internal save with candidate load before commit; New must not write its file before cancel decision. Existing queued run source-identity fix stays. Owner **Neo.HtmlToUitk.Editor / `neo-audit-1023-b03-07`**; current plan confirms. UI interactions unexecuted.

### C-38 / P2 — CLI timeout overflows after process creation and error cleanup does not own termination

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Bridge/CliProcessBridge.cs:104–127` (timeout at 110), Run. Public TimeoutSeconds=int.MaxValue; int multiplication by1000 wraps negative before WaitForExit, causing argument failure after Start. Dispose closes Process handle but catch has no guaranteed termination of its running child; TryKill also swallows failures. Expected bounded validated wait and known cleanup outcome. Minimum long/clamped timeout calculation before launch; finally-safe bounded termination/drain on post-start failures, report inability to terminate. Source arithmetic/cleanup gap confirmed; actual PID lifetime requires controlled native probe after freeze. Owner **Neo.HtmlToUitk.Editor / `neo-audit-1023-b03-07`**. No session abstraction required for this fix.

### C-39 / P2 — stale diagnostics file wins over the current run's stdout

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/ReportReader.cs:12–17,49–57`, FromRun/TryReadFile. Prior PASS diagnostics timestamp within2s, current ValidateOnly/refused conversion writes FAIL JSON stdout but no output report. Reader accepts prior file first and loses current rules/messages/counts. Converter source `D:/Git/NeoxiderTools/Tools/HtmlToUitk/src/HtmlToUitk.Convert/Jobs/JobRunner.cs:103` returns before report write, `Jobs/JobEntry.cs:124` emits current JSON. Expected current parsed stdout preferred, ValidateOnly never inherits old output report; file fallback needs explicit provenance. Keep legitimate WriteIfChanged/unchanged successful outputs valid; timestamp equality alone is not identity. Owner **Neo.HtmlToUitk.Editor / `neo-audit-1023-b03-07`**. Public regression old PASS/current FAIL and old FAIL/current PASS plus identical repeated success; new CLI run-ID protocol optional, not prerequisite.

### C-40 / P2 — changed CSS/USS/partials roots and moved-from/USS imports are not watched

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.cs:302–319` MakeLines/Edit changes dependency lists without StartWatching (HTML entry edits at568 do restart); roots only assembled at734–752. Existing Watch, add CSS outside old directory, edit it: no filesystem subscription exists. `Editor/Import/GeneratedAssetPostprocessor.cs:79–108` collects imported/deleted/moved but omits movedFrom and .uss. Expected one relevant conversion for actual updated dependencies, old path removal and USS pass-through, no generated/unrelated loop. Minimum rebuild roots after dependency edits and collect/dedup both move paths/.uss before filtering through existing normalized output/dependency policy. Broader unified index is optional. Owner **Neo.HtmlToUitk.Editor / `neo-audit-1023-b03-07`**. Callback/source omission confirmed; native event cadence and feedback acceptance unexecuted.

### Neo.HtmlToUitk.Runtime — reviewed

Full audit/plan, all seven changed source diffs, animation finish/start/sample and router dispatch/focus/blur bodies, all four complete new fixtures and full factual canonical HtmlToUitk doc diff read. Exact finite JSON output/strict grammar, selector invalid-form rejection, generation-before/after external router dispatch, modal focus separation, fractional terminal timeline reuse/captured bases, snapshot cancellation and blur owned-style/RT finally cleanup are coherent. C-36 remains. Public/serialized names and centralized PanelCompatibility/BackdropBlur gates preserved; strict parser rejection intentionally changes malformed data admission only.

Fifty-nine prepared cases use pure public values, fake backend visible/focus/event outcomes, real EditorWindow scheduler and UIDocument/GPU ownership. Own windows/RTs/textures/docs disposed, GPU Null explicitly skipped; no skipped case counts as acceptance. Native filter/camera-render error restoration, true focusedElement/detach/disabled ConfigureRoot and both incompatible native branches remain unexecuted. History/source-check receipts predate these edits and prove no current pass. Blur EveryFrame allocates level/temporary chain lists and performs down/up blits per configured owner; scheduled animation updates are per distinct run, selector collection per play. Share immutable prepared tracks/parsed selectors only after mutation contract; concurrent roots keep separate style/run state. Separate blur owners cannot share one output blindly when camera/texture/size/settings differ. No redundant physics camera ray introduced. Strict frozen metadata compiler is optional additive design; use existing parser/timeline/selector rather than a second stack.

### Neo.HtmlToUitk.Editor — reviewed

Full audit/plan, all six changed diffs and affected runner/window/job/watcher bodies, current process/report/postprocessor and actual CLI provenance boundary, all three complete fixtures and new editor guide read. Canonical paths, submission-time deep copy/flags/list snapshot, submitted completion context, fail-before-write JSON, object-root rejection and old/new rename/disposal gates are coherent. C-37–C-40 remain required; no execution-session/DI framework prerequisite. Fifteen prepared cases observe real temporary/permanent bytes, captured bridge requests/options and directory handles/OS rename; bridge override restored, worker gate synchronized by events rather than race sleeps, watcher disposed before temp deletion. Blocking waits exist inside future tests only; no process/CLI/service ran here. Central native watcher/window/import verification still required.

Aggregate: filesystem events queue into one editor-update/debounce owner; source snapshots allocate once per submission and one global gate serializes conversion. Output normalization avoids generated feedback but dependency-root reconstruction still incomplete. Closing window removes polling while process can continue; explicit retained completion/cancel owner is optional broader session policy after mandatory save/timeout/report/watch fixes. Huge timeout source overflow is correctness, not speculative performance. Filesystem lexical path case/symlink policy remains documented limitation; do not alter global package/player behavior to fix editor tooling.

### Neo.HtmlToUitk.Tests.Editor — reviewed

Full audit/plan, all four changed existing-test diffs, all four complete new regression sources, shared partial backend/panel/teardown helpers and new testing guide read. Temp cleanup/exact BridgeOverride restoration, extractable malformed JSON branch and explicit middle-stack pop policy materially improve tests. Fourteen added cases assert valid completion/focus before negative stale checks, preserved public state/files, authored responsive classes and actual trigger cardinality. Shared helpers are reused via partial declarations rather than copied implementations; no test oracle reimplements router. No additional confirmed owned fixture defect from this delta. Current runtime generation guards protect F5 by source; native execution unverified. Historical NonParallelizable/type-check receipts are not current compiler facts; root already removed unsupported attributes. Run process-global bridge/scheduler/EditorWindow fixtures in isolated central sequence and preserve exact global state; do not infer scheduling guarantees from old prose.

Optional named backend conformance helper is proportionate test-support reuse, not a reason to introduce production reentry policy/API. Keep some actual panels/Play component lifecycle/GPU cases; fake backends do not establish pixels/browser parity. Canonical guide needs reciprocal editor/testing links. Testing guide's source-repository audit link should not become a required package navigation target after UPM import. New runtime/editor features stay separate from this test owner; no new shared config justified here.

Full read fingerprints for Neo.HtmlToUitk.Runtime:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.HtmlToUitk.Runtime.md` | `1eff02ab02924e688b6a1effd83afb542470049d398e77725460390477490537` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.HtmlToUitk.Runtime.plan.md` | `7b1c42f74d2ee153e0d7ffa14dd23c579cb6760a0111a28156c1a450de44eda5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Animation/UitkAnimator.cs` | `846b2049c22e9b62581896b431610775b6517ad90ebd82c9e94b656fa7802583` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Effects/BackdropBlur.cs` | `a11d6cfa9c0fed707c77f29dfb1b6ee7b844812873c68ed910891e2b6a924b0a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Effects/BackdropBlurPass.cs` | `a8df05d85311dc59b134721d9db920735d88d1d1e5c05dc05da503bbcea43458` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Animation/Easing.cs` | `d42582bc991b30bc4c71aedc59ce51124fb16bf7758e23597c2ed60da0cd6471` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Json/MiniJson.cs` | `02273082975346ad1fe6ab909af98087431afdf55875bc814e2f73fb6fd80bcc` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Selectors/SimpleSelector.cs` | `505388784e2820cda6c34f964005d3e20cddc1ad25666bf7c76ee3f9d8f01a07` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Navigation/UitkRouter.cs` | `afb5f6f9e27a37490174d12882f5aa9f205d9b6fd7856631ef988cf4173ce717` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkRuntimeAnimationTests.cs` | `ed2e748c64bce742962c1ce45bee4f42a21235318ad8cdb65c10183f5a287aac` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkRuntimeBlurTests.cs` | `3704df7a6b556e0b807cf61beb31a1ab4e617c01ccf872cbced5d83b82c4c8f2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkRuntimeRouterTests.cs` | `f6f818ed75ece006fd74128a3cc3c149a93f54b0bab9f11a5d14e1135e210971` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/PureLogic/Audit20261023NeoHtmlToUitkRuntimeJsonSelectorTests.cs` | `fc7f9c108a4734c4a56e0f6be48ee9a7174b4d638e5b08c669dcb7726ea93452` |

Full read fingerprints for Neo.HtmlToUitk.Editor:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.HtmlToUitk.Editor.md` | `14a4fdf91708a752b644c80781b4bbb3e6f55008b683deca2f4e02b54c1d8abc` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.HtmlToUitk.Editor.plan.md` | `9cd87d9af01d3dec43ac4d109ee87bc806e5f6850947259a96496d9d2b2553be` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Bridge/HtmlToUitkRunner.cs` | `3e94e33f4e4cfa6f653074bf49fd541235c753e2aece7ab1907b514a2ce955ff` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Bridge/HtmlToUitkSettings.cs` | `9a66e561185b431cc81a26a1e670c704a61857b860e974a80122f9ba73ba916c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Jobs/HtmlToUitkJob.cs` | `4e5d8062dd681d27f7742920a7b9425d9deed04479b5a7781e8c1277b1fe8b8b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/JobDocument.cs` | `895c24558cae52b86d74eb61877566556f334c704a2b6a7d5a9739a08953aca9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Watch/HtmlToUitkWatcher.cs` | `89a7ea4fc141d4cc811d8f2a73c32a83bdcb58f69a6315476a496129ae5e3244` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.cs` | `89a824a4e54c8fac51a9d5b65371b561e1588e5f4e4cef2bea9674b6fe490a57` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkEditorPipelineTests.cs` | `a84f8bd507c37315aae6de5f00133519420c8da4d5850c7545e21842a3a2b04c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkEditorWatcherTests.cs` | `263594df15c3a6d6751a7df13bc98eb65f443a9902db355def812c40b8ad56b4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/PureLogic/Audit20261023NeoHtmlToUitkEditorDocumentTests.cs` | `4aec7bfbee4896a531bfbb82bc44a5576e1a7a267cb22a2c8ccef9fdb8dd526f` |

Full read fingerprints for Neo.HtmlToUitk.Tests.Editor:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.HtmlToUitk.Tests.Editor.md` | `e3b11aaee81d852dc0bdc15de9b77912d1244f118b362a615acc20d928e79dec` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.HtmlToUitk.Tests.Editor.plan.md` | `6b96500c9b3938e98bbe4e068571dcdd7e871a7ebc5c0f97fe7e13f3af1af880` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/RunnerEditModeTests.cs` | `041ff8db0d934528977cb03463c5fda713b2be53198d50594e786b503d69d64a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/RuntimeOwnershipTests.cs` | `64c27df91689e867a4919dce69a8f6e990f4df58d6fd2e977899de5fa6fc6d58` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/PureLogic/EditorLogicTests.cs` | `c2a5e606be0c658a353a97399d6a07d9ebfb528a87bb3792cfd250e81b27811e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/PureLogic/NavigationTests.cs` | `5ed7890739a444108fe526810c1c69ad0254d86cc53e82afc7628f4431b197ba` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkTestsEditorRunnerRegressionTests.cs` | `13028466abdac4fc23d095f3abfd943890666b8231a7e7732c7a09ac41425aad` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkTestsEditorRuntimeRegressionTests.cs` | `ca739a88537ba986d4e905a673f031c189027138533c6455d0280d059c79fa43` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/PureLogic/Audit20261023NeoHtmlToUitkTestsEditorNavigationRegressionTests.cs` | `90018236f7bb2c4e066d589716c361f5a20abd0eba0fa1c44fb85a6c9f490047` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/PureLogic/Audit20261023NeoHtmlToUitkTestsEditorReportReaderRegressionTests.cs` | `d1e434ce8bd0bf2c9ed505c2e1ec3a93462f406b6df20dea382babbff9fc2155` |


## Bounded policy recommendations for coordinator decisions

**RPG ticks, C-28:** recommend maximum **1024 delivered ticks per entry per public call**, dropping excess **whole-tick** backlog and retaining only the fractional remainder in `[0, interval)`. This intentionally corrects pathological catch-up; ordinary due-tick counts at/below the budget and existing pause-boundary behavior remain. Keeping astronomical float backlog would cause 1024 callbacks every future update without representable progress. The budget is a documented internal constant initially, not a new Inspector tuning framework.

Validate finite delta/normalized interval/rate/accumulator/converted amount before mutation. Use double intermediate sum, quotient/remainder and amount; clamp quotient before integer conversion. Before first external callback discard excess whole ticks and store only bounded scheduled work plus fraction; consume each scheduled tick before publishing it. After callback stop on removed/rebuilt generation and never overwrite replacement clocks. A small per-service/shelf Tick reentry guard or equivalent dispatch token prevents a nested Tick from consuming the same pending work twice. Preserve valid zero-delta Shelf expiry processing; reject nonfinite simulation steps without damage/regen receipts. Reset a pre-poisoned accumulator under an explicitly documented recovery policy rather than propagating Infinity.

Acceptance in both resource/status APIs: NaN/±Infinity step cannot deliver invalid amount or enter a loop; float.MaxValue returns with at most1024 callbacks, finite state, residual below interval; next normal interval delivers exactly one tick; normal 0.25+0.75 at0.5 delivers two total; arithmetic amount overflow rejects before callback; rebuild/removal aborts old work; spend/damage pause crossing stays frame-quantized. These are required future regression specifications, not executed checks. Root selects/finalizes the policy before authors implement it.

**Quest demo resources:** root proposes additive `bool UnregisterQuest(QuestConfig expected)` that detaches only the same registered instance and never deletes saved state; this is a narrow prerequisite for disposing demo-owned quest definitions. Neo.Quest owner implements/validates it, Demo integrates afterward. Do not use global registry clear or unregister-by-ID to delete another registration.

**Demo audio resources:** first reuse current exact playback/clip/entry ownership. If outgoing crossfade voices cannot be released through existing API, Audio owner provides a narrow clip/entry-owned cleanup; disposing one demo must preserve another client's current/outgoing playback. Generic audio/session/resource frameworks remain optional.

## Final disposition and deferred verification

Coverage complete: **25/25 original module pairs**, full artifact paths/hashes above; no exact assigned correction artifacts existed at initial or final enumeration. Forty numbered observations include confirmed functional/lifecycle/fixture/doc defects, explicitly separated structural/native risks and optional proposals. C-30 deliberately does not claim an executed near/far/saturation failure; C-13 records confirmed callback collision with native dispatch regression still needed. All actual execution outcomes remain unverified.

Highest priority before source freeze: timer/clock economic correctness (C-08), recursion/never-return loops (C-16/C-28/C-32), callback production overlap (C-25), data/config loss (C-21/C-37), protected hierarchy/pool ownership (C-19/C-23/C-24/C-35), and the ConditionBridge readiness fixture (C-03). Other confirmed bounded fixes should go to the exact original owners listed, without making large optional API frameworks prerequisites. Preserve prior Abilities/shield fixes, all serialized/public fields, explicit C# types, and same-worker/native fix policy. Group A reviews this agent's earlier Abilities work separately.

Actual actions in this cross-review: source/document reads, baseline diff reads, path/test/correction enumeration, SHA-256 fingerprints, and writing **only this report**. No source/public guide/test/config edit, Unity/MCP, compiler, build, test runner, package validator, install, service, child agent or commit was performed. Existing source checks/old compiler diagnostics are quoted only as history. Root owns final execution after **all** approved source writers finish: supported Unity branches/conditional network weaving, focused regressions with real preconditions and discovery counts, lifecycle/native UI/physics/OS watcher/process cases, and aggregate query/allocation observations.

Shared-query review conclusion: implement shared **complete raw** contexts per input/physics epoch where geometry/settings truly match; preserve masks/tags/blocker distinctions and per-object unique LOS, effect/force application, selected target and eligibility. Do not claim fewer per-object query calls from only cached ray construction, ignore saturated buffers, change trigger/layer settings for cache hits, or invent measured FPS. Group C identified actual allocation/query sites per module above; central acceptance must record query counts and output parity before performance claims.

## Factual documentation fingerprints

These changed/new guides were read as full diffs or complete new files and checked against the current API/source during the corresponding module review. Full audit/plan and changed source/new regression fingerprints remain in each module's table. UTF-8 report codepoints were inspected after console displayed an em dash incorrectly; file contained U+2014 and no replacement codepoint, so no public document rewrite was made.

| Full guide path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Audio/AM.md` | `14f3ceeb0ae55d7bced0e703444e14b10c111ed4b90d67e8e20e0d591363aaa7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Audio/AMSettings.md` | `23a28981c518d7017db7d3c328251cef469199bd34569369fae8cc956501125f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Audio/AudioControl.md` | `a396cd6eef189503e2c8caf876074049df5ee8e5eb6edf69bb510c1309468348` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Audio/Editor.md` | `93dcbf0d479e88b037ce9a484bf8eddc7e81f09b1a6d7e2645c2871eb2b4c1de` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Audio/PlayAudio.md` | `53cbc3884e950287f5b478440d6034b6e820a972244b02562ea3ee04f56341ba` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Audio/README.md` | `1773f1deafb178638553165292bf8cfe0a8acd5bbd41dc450d8e78684af97d41` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Audio/RandomMusicController.md` | `9a92925285c301424cfe50d2ef1429e521efb36849e130649880855eded520fa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Audio/SettingMixer.md` | `2527a4fe01a308e6560010e8982d3c1ae4ae5b31040139557a01940413b24d2b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Audio/View/AudioControl.md` | `0c2218a99a5d392a1689d203db7ccad7742b95836aa916a758994dccdfeb9c52` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Bonus/Collection/Box.md` | `0586dafd7fe1e1478759b504481e9301f7e3b71808b608dfc55cde1f484440ec` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Bonus/Collection/CollectionVisualManager.md` | `b044585455a1d2fab1afb9c05e31e421f6e7384255df57b715dcf5fa62dc37a5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Bonus/Collection/ItemCollectionInfo.md` | `5ccaa12560dd904c61b6f49514f0b99c200f81e4480f89f1f139b15d63bbf3d9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Bonus/Collection/README.md` | `32c3a45d90c8a693cb2ef4469db4b8c756dc33d6ce5bf7a962bbe06a262fe70c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Bonus/LineRoulett.md` | `69e82c0f3cbc94b8c395abc86f212ad606ce280e0781b0eb0849ba79bf21d1cf` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Bonus/README.md` | `b6a4b029bf361a08e8c14b7a8011257d972c632d385c19dce0200a5e3fb86439` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Bonus/Slot/Data/SpriteMultiplayerData.md` | `19a15b2236b38ecb351662ac91227c7e9b87758dffc516c6b13b4f10a7622808` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Bonus/Slot/README.md` | `c6a89563f120ad8e38ab0583c3f4fca1298b28311baa523daf308168524db074` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Bonus/Slot/SlotEconomyDefinition.md` | `862ea3ae195b16a6578cd4d58accc691afd73c1d691d112fdefe946190cf207c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Bonus/Slot/SpinController.md` | `ab0ebe69496d1e4a6990eb467e2b946eb2a4d278e947b49fff7e86ca88b6b426` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Bonus/TimeReward/CooldownReward.md` | `64b9c2e168cc13fbee8adc5909f54da4f308644fa56a21decf53194cbca5c631` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Bonus/TimeReward/README.md` | `b115782fb8d4902ac301ac8bc6ea8e26cabf14e80da1ca80722a2413439fcc78` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Bonus/TimeReward/TimeReward.md` | `74707e14bc6c96bdb53b659e0f923664822633c217d69b58e3e15bd1db05017f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Bonus/WheelFortune/WheelFortune.md` | `1f227ac2767944b2fe41fe5bdbf91e97d4f6c9ebb2c5c697f740749fb1ad8de0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Examples/README.md` | `9a2d7cc4b5f94ab718230bd13c63702dac506d4296425d5eddc6199a74ba38a9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Haptics/Haptics.md` | `db2aebd235effdd764d1c568bdb48eb3bb3a7786dd19634b67ceabb77c3d40f9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Level/LevelButton.md` | `1abfac608a49c953b35e528f14f49f7f1114e143e84237217c9ec664eee033e6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Level/LevelManager.md` | `b541706bc1e93abf80a80c944bdc1e12ae7534b78deb93af1aa731fcbfa3c610` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Level/Map.md` | `fe7cb10293be44f7573cb9c81fa22ae60cc47a0cebdc0ef36221bb6334ecaac6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Level/README.md` | `6e44bc49ea30e3f20ade5cbb0df07637318cec5d03403565511a4198ced8012a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Level/SceneFlowController.md` | `0d688e8e88b6d5c87b28a5f43d747ac982ddc40715cc4ea911fdc7dc5a207433` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Level/TextLevel.md` | `2cd9dc235abbc08b477aa942096c5a357480b9a28a4f7682e5d521af31061852` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Merge/README.md` | `b9d761ce8de109c0fc05f9ef782f4f80caf313823580eb51d19cc6098c772abe` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/NPC/Combat/NpcCombatScenarios.md` | `7e265984fb08e1ff73a0e04f0f1f57e8021d68b31a4785fd28fb5a39c01324d1` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/NPC/Combat/NpcRpgCombatBrain.md` | `53a0e43b899ba982d062f12b0daf220a6608c41a9e2625c21676a771eaca6057` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/NPC/InternalTypes.md` | `e76178ad90cdbc74b571e21d4ff57d4ed51a160f9e26c3c6341954e586698031` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/NPC/Navigation/NPCNavigation.md` | `4821bbfa96185b09752cb45a1a0db8ad5408083486abe9027c5589854b92ad32` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/NPC/NpcAnimatorDriver.md` | `9cd80f02c0f5ce781fc227a05649258ee496ce9a490d8e40fd28d4a9fdad1904` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/NPC/NpcTargetFinder.md` | `7d56cad47e2d3e51a5c829c9310cb2e07b76d1f9b9ec4bcfb7eba39028e6be75` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/NPC/README.md` | `3f8eb9858bab62f34edd9afd2287fb4c965eaa60c16cf6ee279746e4da110de9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/NoCode/ComponentFloatBinding.md` | `a42c1e8394b6a6229e7db4dc0dbb0c8b10bfadb0645d7193adc31cfec8ee11ef` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/NoCode/GettingStarted.md` | `c2df736630c91d7f96bbe794e9b999cccb48d88e63af45fe1228d7badf348380` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/NoCode/NoCodeFloatBindingBehaviour.md` | `677db21407174020b2cf75387868ae221d50de14287d539868eb114e295dacf3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/NoCode/README.md` | `1e81bde38eea4168be1eebddfd98980e85798ce52d10c5a8846bf1e329cabfb2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/Combat/README.md` | `d6791a3d394cbc786f1ba2819e7f07c8298fdb4ff227cbd2b43f719abf87256d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/Components/Weapons/AuraWeapon.md` | `9ce08e33a1cca395072a80fe95de9ffd2c8e7c8d3d3dad7cc4dd91362032034a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/ConditionBridge.md` | `f808c718e87a79f60c084642a21c96e7927ed2d273838b81c5598b32fef6279d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/InternalTypes.md` | `5788b2775fe6a6829fd9ea60062f9cefee0ae0e2b78aa853779ea9262651311c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/Network.md` | `da46c2bb29741c7ecd35734b73f2fb2b69402247af0ad7b3bf24d15c29df6159` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/README.md` | `a47cd616b7629bf9b3f78b91882e6d26357894667fb9565fa22e84959a73efc6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgAttackController.md` | `678f0313ae007e2aca534a0a835d1831417e12cf6e5dcd741d56a2f3d6db02cc` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgAutoAttackController.md` | `6470ee21d8fdda0d1c29ad7ac57e63a03c548acd18011ef2c14d9c24141be422` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgCharacter.md` | `19be6d32c11b10ea151de0ec6d4dc087f3794f3406bdaedb6faef1f86d902a27` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgContactDamage.md` | `6742224124f02cf7283c98d8d6aef284bbed4e6c9b4906a20fafbcba93e6a73c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgEvadeController.md` | `0d5d20e34fce368f1725ee37ca362392852fc9eba1e47c2f618a19938182e21c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgResourceBinding.md` | `ddcaec7059e58cca070a28c0f1df366792a038376cd2531b9bf49001b181ebb0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgStatBinding.md` | `b2e217f796c738512648fc86ae9526b5d5ace59c5dcef6d153e19eeeedc26825` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgTargetSelector.md` | `4462140f169e63f4fdec7b706ee68a6c6cf1fa08844ff7e6a833b32257fa0c97` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Save/FileSaveProvider.md` | `a06f80ae530f618b995d31ff679fa6170961db0fa1d86799798a50726d8ecbd2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Save/README.md` | `4cddc311ed1bc088ed320d0db4e864f4e77d2f4c8b9ed843361a713695518586` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Save/SaveField.md` | `f592466a645274b294ac93e1e9014fc17e160789c223b3a83e4117443dd92ed2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Save/SaveManager.md` | `37cc4f295a047d6aa15fc4f3a1ce0ace5bb86eea11a1eeac8650ff16403b3e9b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Save/SaveProvider.md` | `08637769232fc614ceeaf59d36498d16864cd64de322e8578b23af814a9339bc` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Save/SaveableBehaviour.md` | `99a1ab41fb908223f044ee0212a76bbd6c80846e636bb07138bc1b97746728f5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/StateMachine/ConditionEntryPredicate.md` | `0ecad81d558710a907feac3c9b5113fa2a1fc3cf3cbc1a9c9d5a3c6b8b49a643` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/StateMachine/Editor.md` | `28caea31a42431da3a5a0e27bfc915f8f2da4cbee0cdb375ce625dbd7adfd87b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/StateMachine/NoCode/StateMachineData.md` | `ee9bf22b6acaa3137a68e873285fc501f2d9be2a68f70b705a03ea3ceb6e4ab0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/StateMachine/NoCode_StateMachine_Usage.md` | `5118355e96cb8a46c0934a089e03c1d30e26ea46953919355037f16b8d7123c0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/StateMachine/README.md` | `abda5e8a0b3899c35a2e5cb887a665c0bdd8812bee950a8ee1b529a8a87a8997` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/StateMachine/StateMachine.md` | `09a4dfb45d84a508b313ae4fd82c495a9ecdc28d7a4ae668089cea4ffbeaba42` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/StateMachine/StateMachineBehaviour.md` | `111696f313d340258bb8371e502519c373c4b5ad1f4640b7b59b85d2cd50fd2a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/StateMachine/StateMachineBehaviourBase.md` | `97277ae19fd8763d4ec1aab8f88f13d3fec506e2b0b157e522f7f4eec67ba90d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/StateMachine/StateTransition.md` | `f04dfd936bb7f8a3fe1edde47d15ca1a891e6a57feb9c8b911aaa917c61ea5fb` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Draw/Drawer.md` | `5c948ec7c0977ffb442cc795a376bced9123ed9099c07d035fe97e826f76e071` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Draw/README.md` | `4183b50f3b00ed91545b8a11968353c3bb6c4fa268bd4a950fbd93b814b2c30e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Physics/ExplosiveForce.md` | `c9e46ec76c3d5d249a219f87ea110c62ff96d3e1981f22c26970ab07da816abe` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Physics/ImpulseZone.md` | `a7754f6389bc9b328ddcc48b4c301985dde650a922bf034cb372e73dfc24e4f4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Physics/MagneticField.md` | `be12d2560af3b9a2546a7c28cf97cbbcdc7355dcfeccbfa673dd9a2ba7b0be0f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Physics/README.md` | `146978b6bf6b75a6516cf4a083cb270b72a3264335589547b7dfa5d1973e7d57` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/Despawner.md` | `48d4813ede96846375bdc9aff7a3e277755ab3fae2c358b6fab2364f91392c33` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/IPoolable.md` | `012d681fcf2d49b5f518e7c5cbc28584d9013d24eeb8b45e3f8f07cb85be088f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/NeoObjectPool.md` | `5a08fc0d114828538b273150ede5587b176dbebd7b72a90267383a176b81e60a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/PoolManager.md` | `9c52a7722b9eb274bd15e1fc9057d8edca0822419e303749ce0f5450897dfb06` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/PoolableBehaviour.md` | `3cbbc31109aad51567799e50fa7f776485f8d9f912b000ea8c91ea690f96a154` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/PooledObjectInfo.md` | `d451e6b0276990168553f3f1400df2cc9fd04ce34f81b94ad5d651c8faf4edd9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/README.md` | `486aec0a33e98c8b6c8b4418e8f2cd11a9776203703df263fe849d91c6e3b0b2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/SimpleSpawner.md` | `1ae7fd503cbd3a8487e023deafc3c66a854bae488ee5f76e4477567a9092d652` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/SpawnUtility.md` | `77c6c72e322daccfbe57979876cb04751469967b770afc173a3dc660bc05d635` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/Spawner.md` | `aed371a3861db73d71ec20057014d3e0ee36acaae6fecb8f8c53d7e0757344c3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Text/README.md` | `f0a6ab9fda6f65acb05da29779e1aef1bd4d3da7457c60147baa2dba610fff82` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Text/SetText.md` | `f642ea29b1f47f2a58f8518abc8e33679296bc80191b614d33c6690aeb1b9ae5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Text/TimeToText.md` | `8df7bf6e3d0848877369f7c82f5a18070e4105c552be27000a6f17963e676df8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/UI/AnimationFly.md` | `e2d3457dda19cc307aab00186315b564abeb85f32b78c6ad5a40f2ca237fcb5b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/UI/ButtonChangePage.md` | `de30c8a9c2cd110ed79aea39c06a916e7c96cfc1fd269ce0ffe2f805821422fe` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/UI/ButtonShake.md` | `e11843a805d546ec9784e475eb99ae5cc196f6fa051ac19b5f295306e417b6e7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/UI/CategoryBar.md` | `fe83f71772bb0775b6805164a981faa29c280cf5aaad61f9e296ec093aacd800` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/UI/Editor.md` | `8a26d0672b42fd2b5e4fc48b367a97854999dfeb89bf680324ef303f8e58c233` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/UI/HtmlToUitk.md` | `e13c49c63e3b16eb1e6b4d79a88ff98518f8f69969684a0cc406663522cb4a1a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/UI/HtmlToUitkEditor.md` | `d5dcb2047b5f6629a4591d3f0ea6ed8e1f0469290b48b731efa3dd233dffbc7b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/UI/HtmlToUitkTesting.md` | `1597db83a583712401403206c47e4083aebf7f84128ec573d4e4470d328d1976` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/UI/PausePage.md` | `8b19fe3b15311f9382746b8cac1edf85fbdb4ff9055f9e391cdd35f3a1a7566d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/UI/README.md` | `ccbd9f053860c4c3cd85100af24855cf61ad056002f743015449ec322f358f4c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/UI/UI.md` | `cbea3570f3aa56ac7f578129dc29e28e21b600ff7a01302ff3daccabbfa67514` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/UI/UIMeshRig.md` | `cc8fefb650fa3461954e06797ed82c05d8fb622509c879ae8eedfd872e548cdc` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/UI/VariantView.md` | `8919aa4d04ff25ece443a45f3547eedacdbdc3e870e04cf016d4451ee1210fd7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/UI/VisualToggle.md` | `7cab005d5119a255e7beae16f639eedc4e273f4e6814960af97951a3eca9413d` |
