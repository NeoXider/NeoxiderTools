# Neo.Progression — implementation receipt

Status: **source-final** after the 2026-10-25 recovery and Snapshot3 **PROGRESSION-NATIVE-SUBSCRIPTION-CLEANUP / E79** correction, retaining A-21/A-22, NI20-02/P1 and the Snapshot2 accessor. Branch: `demo`; target: `11.0.0`; original actor/session retained. Root reported the previous S3 actor attempt ended with **exit 3**; that attempt and the older source-final receipt were not completion evidence. The current fixture correction is **UNCOMPILED / UNEXECUTED**; actual older compiler/test results are distinguished below. No source dependency is pending.

The original audit and plan remain historical evidence and were not rewritten. Their earlier coherent fixes remain in the working tree. The optional ProgressionSession/catalog/reward-sink/ledger redesign remains a proposal, not an implemented feature.

## Approved actions completed

### PROGRESSION-RELOAD-WATERMARK / native A-21

`ProgressionManager` now tracks the active loaded/reset profile key separately from the selected `SaveKey`. `LoadProfileInternal` captures the actual key being read. Applying the same active key preserves `max(previous rewarded level, incoming Core level)`. Loading another key establishes a new incoming baseline; explicit `ResetProgression()` deliberately starts a fresh baseline.

Assigning `SaveKey` or saving to another key does not itself load a new profile or erase active reward history. This corrects the 5 → downgrade/reload/recover → 10 exploit while retaining normal fresh-reset and different-profile behavior. The session watermark is not persisted and no durable claim identity was added.

### PROGRESSION-PROVIDER-MIRRORS / native A-22

The manager now owns three existing `ReactivePropertyInt.Subscribe` handles for provider XP, level, and remaining XP. Replacement, clearing, failed binding, and manager destruction release those handles and the exact reward listener. Binding generations reject callbacks captured before a provider swap.

The mirror path compares complete snapshots and calls only runtime refresh: it never saves the Progression profile, dispatches rewards, or changes reward eligibility. It therefore observes direct Core XP/load/reset/downlevel operations without polling or a Core source change. Normal rewards remain on the existing `OnLevelUp` path.

Nested manager-owned XP/level mutations, reward dispatch, premium activation, reset, and persistence stage runtime notifications with balanced `try/finally` scopes. Requested refreshes update cached mirrors silently; the outer scope publishes the settled snapshot. Publication generations stop trailing older payloads after notification reentry. This does not add a transaction framework or roll back external side effects.

### NI20-02 / P1 — failed precommit initialization and explicit retry

Read the complete 238-line `NativeImplementationReview-20.md`, the prior complete 84-line own receipt, implementation contract/current request, and actual `EnsureInitialized` / `Init` / `ApplyProfile` / `BindLevelProvider` / `UnbindLevelProvider` bodies. Retained the completed A-21/A-22 changes and earlier audit fixes.

The manager now distinguishes an unfinished attempt from completed initialization. Both its readiness and `base.Init()` commit after profile/default sets, active identity, and the incoming reward watermark have been applied, before external state/profile-loaded notifications. A dependency initialization, partial subscription, or profile-read exception leaves initialization retryable. The attempt clears its in-progress guard in `finally` and releases its binding only while that exact binding generation remains current. Binding setup uses the same generation check before publication and on failure; an older attempt cannot erase a newer reentrant binding.

During unfinished setup, a reentrant public ensure/profile command throws `InvalidOperationException` before changing/saving defaults. Provider replacement remains available during setup; its mirror refresh is silent and provider callbacks cannot publish/reward an uncommitted profile. Once the profile commits, normal reentrant commands are allowed. An external state or `OnProfileLoaded` observer exception still propagates, but does not reset readiness, reload the profile, rewind the committed reward watermark, replay rewards, or discard newer subscriptions. No arbitrary rollback, transaction/session framework, persisted ledger, or new public API was introduced.

### Snapshot2 — approved `OnProfileLoaded` C# event accessor

Read the actual archived `compiler-errors.json` receipts under `Local/Audits/assembly-2026-10-23/unity/final/f84c9007d0494cd8a421d84d70d931a0/{u6000_3_14f1,u6000_6_0f1}/mirror-on-dev/Receipts/`. Both report six owned CS1061 diagnostics: the previous fixture uses `manager.OnProfileLoaded` at lines 735, 966, 974, 1006, 1009 and 1076, while the manager exposed only its private serialized `_onProfileLoaded`. This was an actual compiler failure in the prior source, not an inferred risk or a passing result.

Root explicitly approved the additive read-only `public UnityEvent OnProfileLoaded => _onProfileLoaded`. Added exactly that getter alongside existing event accessors. It returns the existing Inspector event object; the serialized field/default, metadata/GUIDs, invocation site/order, profile/readiness/binding generations, retry and postcommit observer behavior are unchanged. No second event, test stub, callback rerouting or other production behavior/schema/configuration change was added.

All six existing callback/failed-init call sites and their behavioral oracles remain. The existing two-case ordinary initialization test now also asserts reference identity between the accessor and the serialized event before initialization; the existing load-event counts still exercise actual notification behavior. This adds no standalone mirror-only fixture or extra case. Canonical README/manager docs describe C# `AddListener`/`RemoveListener` and Inspector bindings observing the same notification.

### Snapshot3 — PROGRESSION-NATIVE-SUBSCRIPTION-CLEANUP / E79

Read the exact assigned XML cases and `full-edit-failure-action-index.json` E79. The archived S3 Unity 6.3 graphical Edit run contains **50 owned cases: 49 passed, 1 failed**. The failure is `ProviderSubscriptions_AreReleasedOnReplacementAndDestroyMessage`, old line 623: expected listener count 0, observed 1, with output **`SendMessage OnDestroy has no receiver!`**. The fixture creates an inactive, never-Awake manager, so this message arrangement did not establish native destruction. Initial subscription/replacement and the XP-50 positive controls precede that failure. The assigned Play XML contains **zero Neo.Progression cases** and supplies no native lifecycle evidence for this owner.

On recovery the fixture still had SHA-256 `d3486908d34764488dd7ab040b0061a32c4beb473a77add791bfc896c39b99ca` and the old message call; there was no partial S3 correction to repeat. Checked the primary's reservation map before every edit: primary `neo-audit-1023-b03-04` is source-final, reports no remaining source dependency, and reserves no Progression caller. No Fixtures helper/shared-reference file was touched or extracted.

Used the explicitly permitted **pure public contract**: renamed the existing case to `ProviderSubscriptions_AreReleasedOnReplacementClearAndRebind` and cleared `LevelProvider` through its existing public setter. Retained the initial three listener baselines, same-reference assignment, replacement, old-provider silence, exact XP-50 payload, and all three unsubscribe baselines. Added positive counts for the incoming provider, clear snapshots 0/1/0, detached-provider silence, and repeated same-provider rebind/ensure with exactly one subscription per channel. The exact XP payload sequence is **50, 0, 60, 70, 0**: clear publishes its documented zero snapshot, detached XP changes publish nothing, rebind publishes the provider's current 60, its next +10 publishes 70 once, and final public clear releases the owned binding again. No assertion was replaced by a tolerance/ignore/error whitelist. All failed-init/load-event/reward oracles remain; inventory stays **39 methods / 50 cases**.

This S3 correction changes only the owned fixture and this receipt. Runtime, public/serialized API, defaults, metadata/GUIDs, `OnProfileLoaded` instance/invocation/order and canonical public docs are unchanged; the docs already describe public provider clearing. No production unsubscribe workaround, ExecuteAlways, private lifecycle invocation, scene/provider reset or helper extraction was added. Real active/Awake native disable/destruction and its subscription cleanup remain explicit post-freeze debt; the revised Edit case is not presented as native destruction coverage.

Assigned artifact identities read (bookkeeping, not new execution):

| Artifact | SHA-256 |
|----------|---------|
| `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml` | `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2` |
| `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml` | `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a` |
| `Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json` | `df467fde346b0a1f7eca09a82b972f2e7532b7274758981969400628b1770e97` |
| `Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json` | `14b7a974c8a8a861a666b65d43971f42e4f97b21c01734daacd466f1e88fc3a7` |

## Exact files changed in this implementation phase

- `Assets/Neoxider/Scripts/Progression/Components/ProgressionManager.cs` — both actions plus NI20-02: active-key/binding/notification state, commit readiness, bounded reentry, exact attempt cleanup and retry; Snapshot2 adds the existing event's public `OnProfileLoaded` getter.
- `Assets/Neoxider/Tests/Edit/Progression/Audit20261023NeoProgressionRegressionTests.cs` — A-21/A-22 added 14 methods / 15 scenarios; NI20-02 adds 9 methods / 13 scenarios. Earlier 30 methods / 37 cases remain; total fixture now 39 methods / 50 authored cases. The owned memory provider gains one-shot read failure/count/callback controls. Snapshot2 retains the callback oracles and adds event-instance identity to an existing test. S3 corrects the single E79 arrangement to explicit public clear/rebind with the original ownership/payload checks and stronger subscription preconditions.
- `Assets/Neoxider/Docs/Progression/README.md` — canonical entry: provider mirrors, NoCode wiring, same-key reload/reset/switch semantics, shared C#/Inspector load notification, link to the detailed manager contract.
- `Assets/Neoxider/Docs/Progression/ProgressionManager.md` — C# and scene behavior, shared load-event API/subscription contract, disposal, coalesced notifications, profile identity, Core-first restoration order, and retained delivery limits.
- `Assets/Neoxider/Docs/Progression/Data/LevelCurveDefinition.md` — session reward-baseline rules and distinction from mirror-only updates.
- This completion receipt.

No new package source/asset was needed; existing metadata is retained. No legacy fixture, other assembly, shared asmdef/configuration/tracker, package version/changelog, scene, prefab, sample layout, historical audit or plan was edited in this phase.

## API, reuse and deliberate compatibility changes

A-21/A-22 and NI20-02 required no new public API. The separately approved Snapshot2 correction adds the read-only `UnityEvent OnProfileLoaded` accessor for the existing serialized event. Existing manager signatures, serialized fields, enum values, definition GUIDs, `ProgressionV2.Profile` default key, and Version 2 payload remain. The earlier additive `TryAddXp(int, out string)` remains available.

Reuse: existing Core `LevelComponent` getters/reactive channels and `OnLevelUp`, `Neo.Reactive.Subscribe`/`IDisposable`, existing SaveProvider/profile serialization, and existing UnityEvents. No new component or Inspector wiring is mandatory. Public docs keep their canonical backlinks to existing data and bridge guides.

Intentional 11.0 behavior changes for root migration/changelog integration:

1. Reloading the same active profile no longer lowers its session reward watermark; explicit reset and actual key loads remain distinct.
2. Direct provider changes now publish manager mirror updates. Duplicate unchanged provider notifications are suppressed.
3. Nested manager commands publish a settled runtime batch rather than duplicate intermediate batches; subscribers must not depend on the previous duplicate callback count. Reactive properties and events still publish a complete snapshot, including unchanged values.
4. A direct Core level-up can publish the provider change and the later reward change as separate snapshots. Mirror delivery does not replace reward dispatch.
5. Failed precommit initialization can be explicitly retried after dependency correction. Reentrant profile use before commit now throws instead of using defaults; postcommit observer errors retain completed state. Setup callbacks must defer profile commands until commit.
6. C# consumers can subscribe to the same profile-loaded UnityEvent already available in the Inspector through `OnProfileLoaded`; existing notification dispatch and scene bindings retain their behavior.

Existing reward-save and command-save calls are retained. For example, an auto-saved manager XP command crossing one rewarded level still has its existing two saves; the new mirror subscriptions add none. Profile/core saves and external rewards remain non-atomic/best-effort. Restore Core first, then load Progression to establish the incoming profile baseline. Provider replacement keeps the existing incoming-level baseline policy.

## Authored acceptance — current correction UNEXECUTED

The corrected fixture has not been compiled or executed. The older S3 XML results above apply to the prior source and are not a pass claim for this revision. No NUnit serial attribute was introduced; the shared-state fixture requires root's isolated/exclusive runner and restores its exact prior save-provider/singleton state.

| Source scenario | Required observable result |
|-----------------|----------------------------|
| Same-key downgrade/reload/recovery, repeated three times | Exactly 5 points and Version 2, never 10/15/20. |
| Explicit reset after reload | Points clear, Core returns to level 1, fresh level 3 grants exactly 5 points again. |
| Different key seeded with 10 points | Incoming level 1 establishes an independent baseline; reaching 3 yields 15; original key remains at 5. |
| Change selected key without loading, then return/reload | Original active history still prevents another grant. |
| Same-key reload after Core restores a higher level | Incoming level 3 becomes the baseline; later downgrade/recovery retains the saved 4 points. |
| External XP +25 and repeated unchanged channel notifications | Mirrors 25/1/75, one XP snapshot, zero points and zero saves even with manager AutoSave enabled. |
| Manager XP command reaches rewarded level 2 | One settled XP/level snapshot with 2 points; existing two saves, no new mirror save. |
| External downlevel and external reset (two cases) | Mirrors 0/1/100, retained 5 points, one XP snapshot and zero saves. |
| External Core load | Mirrors 200/3/100, no rewards for restored levels and no Progression save. |
| Replacement/same-reference assignment/public clear/rebind | All three exact listener counts return to baseline on clear and to baseline+1 on rebind; detached providers are silent; exact XP payloads are 50, 0, 60, 70, 0. |
| Swap during reactive or level callbacks | Old captured callbacks cannot overwrite mirrors or award rewards after the swap. |
| Notification reentry | Final XP 35 / remaining 65; the old 25 payload does not trail the newer notification. |
| Observer exception during command publication | Exception remains observable; a subsequent provider change still publishes 35 rather than remaining stuck in a batch. |

NI20-02 adds these **13** cases to the previous **37**, without replacing the earlier controls:

| New source scenario | Required observable result |
|---------------------|----------------------------|
| One-shot Progression `GetString` failure, no Core / assigned Core / corrected Save-provider replacement (three cases) | First ensure throws without a save or loaded event; retry reads saved 7, repeat ensure adds no binding/read/event, and AutoSave of +1 stores Version-2 points 8. Detached Save provider stays unchanged. |
| Earlier Core `GetString` failure during binding | No Progression subscriptions/read/save survive; corrected public retry restores Core XP 25, remaining 75 and Progression points 7, binds once, then saves 8. |
| Partial binding failure after XP subscription but before level subscription | Exactly the three preexisting external subscriptions remain; external level listener still runs. Corrected retry binds once, loads 7, and the next eligible level grants 3 once with one save. |
| Reentry from the initial read | Nested ensure, +1 and save throw without recursive reads/default writes; the outer load completes at 7 and later +1 stores 8. |
| Read failure after a newer provider binding | Old channels return to zero, newer channels remain at one, precommit provider updates publish/save nothing, retry loads 7 and retains one current binding; old source stays detached. |
| State observer / profile-loaded observer fails after committed level 3 rewards and downgrade (two cases) | Reentry is allowed; points reach 12 with the existing three saves. Later ensures perform no read/rebind; level recovery retains 12 and adds only the command save. |
| Profile-loaded observer swaps provider and then throws | Committed 7 and the newer single binding survive; only the new provider publishes, and +1 saves 8. |
| A failing provider's initialization observer creates a newer binding | The older bind failure cannot remove the newer subscriptions or overwrite its XP 60; later provider +10 publishes 70 with zero saves. |
| Ordinary loaded / deliberately fresh initialization (two cases) | Accessor returns the serialized event instance; repeated ensure is idempotent, one binding, expected loaded event/read count, no initialization save; +1 stores 8 / 1 respectively. |

The cleanup regression now uses the public provider-clear API. The prior destruction-message arrangement actually failed and is not retained as lifecycle evidence. Native active/Awake destruction remains required after the writer barrier. Earlier scenarios and their behavioral oracles remain; old test results are not reused as a pass for this revision.

## Actual work/evidence and integration dependencies

Allowed operations performed: small `cmd.exe`/Python source reads, current source/API and fixture inspection, source/guide diffs read manually, reference/file enumeration, branch read, and artifact symbol/hash bookkeeping. NI20-02 was traced manually through failed reads, partial binding, explicit retry, profile commit, observer reentry and newer binding ownership against the current Core initialization/getter/load bodies and retained Reactive contracts. Source enumeration identifies 39 test methods / 50 authored cases; this is not NUnit discovery or execution. No Unity/MCP, compiler, tests, product validators/checkers, installer, service, commit/push, or child actor was launched. No test/compile/pass result is claimed for this source.

Snapshot2 follow-up work read both existing compiler-error receipts and the getter/serialized-field/invocation/callback-test bodies, then manually reviewed the getter, identity assertion and documentation additions. The later root-owned S3 run produced the older results recorded above; earlier failed receipts remain unchanged. This S3 recovery read the assigned artifacts, reservation ownership and current case, changed only the fixture/receipt, manually reviewed the public clear/rebind expectations, and recorded current hashes. Current small cmd/Python reads completed successfully; no fresh compiler/import/discovery/test/checker process was launched. Root's execution gate remains closed until all writers finish and a new freeze is declared.

Current source artifact SHA-256 (receipt bookkeeping, not verification):

| File | SHA-256 |
|------|---------|
| `ProgressionManager.cs` | `ec99cadac84f51bb117afd6ad51488fd7dca2aa08501e40781aecb6b5628528c` |
| `Audit20261023NeoProgressionRegressionTests.cs` | `4d93c1959de8ec9f17975f06febef6fb83a7ba8cdb610be978a3f2c285665736` |
| `README.md` | `08e4ce5d63293de373c0a14fbafc31ed7024dfbb56882d68f162e2db8f5d3fa6` |
| `ProgressionManager.md` | `ad5affee04b545f59f759fe9cacd5bfc66c33a282a5fb4dd23b01872e02eb256` |

No owner API is pending: the inspected `Neo.Core.Level.Components` and `Neo.Reactive` APIs already provide all consumed members. Tests also use the existing public Core `LevelProfileData` DTO; no asmdef change is requested. Save's deliberate throwing configuration/read boundary is preserved; corrected dependency selection uses the existing `SaveProvider.SetProvider` API. The new tests use owned in-memory providers, not Save's Resources fixture, so real Resources encryption/retry remains integration debt. Other assemblies' ongoing audits remain independent.

After **all** source writers finish, root must compile the next frozen revision with the integrated Core/Reactive/Save/Managers dependencies on Unity 6.3 and 6.6, discover/execute the 50-case fixture including the renamed E79 case and existing Progression suite, and obtain real active/Awake native disable/destruction/subscription evidence through supported top-level PlayMode admission. Dependency correction, postcommit observer exceptions, reentrant provider replacement, shared Inspector/C# load-event delivery and scene/UI/NoCode integration remain acceptance targets. Final package, UTF-8, metadata/GUID, MIRROR dependency/discovery and version-branch checks remain with root. No earlier static or runtime receipt establishes these results for the corrected fixture. No reserved source work remains deferred; only the named native/execution debt remains. Source writes stop with this handoff.
