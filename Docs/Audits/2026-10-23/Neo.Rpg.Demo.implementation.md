# Neo.Rpg.Demo implementation receipt

Status: **source-final after E28 native-frame barriers and acquired-Play cleanup correction; native verification UNEXECUTED**.
Initial receipt: 2026-10-24; latest correction: 2026-10-25. Original auditor/session: `neo-audit-1023-b05-05`.
Branch observed: `demo`; authorized integration target: `11.0.0`.

Implemented the bounded actions in [the implementation request](Neo.Rpg.Demo.implementation-request.md)
under [IMPLEMENTATION-CONTRACT.md](IMPLEMENTATION-CONTRACT.md). The completed
[audit](Neo.Rpg.Demo.md), its 49-file review inventory, and [plan](Neo.Rpg.Demo.plan.md) remain historical
evidence. Audit fixes F1–F8 and their regression sources were preserved, not reapplied or represented as
new implementation work.

## Action disposition and exact owned files

Runtime paths below are relative to `Assets/Neoxider/Samples~/Demo/Scripts/`.

| Action | Priority / disposition | Implementation and files |
| --- | --- | --- |
| `DEMO-OWNED-SHELL-ASSETS` | P2 / source complete | `Shell/NeoDemoShell.cs`: retain `Build(string, Color)`; add `Build(string, Color, Transform)`, `Context : IDisposable`, `IsDisposed`, `SetVisible(bool)`, and idempotent `Dispose()`. Track the exact Canvas GameObject and generated gradient Sprite/Texture2D privately, before subsequent initialization can fail. Failed construction disposes partial resources. Disposal hides UI, detaches generated row callbacks, clears references, and releases the exact native assets, including when a backdrop was replaced with borrowed art. All generated button/slider/toggle callbacks also guard disposed/hidden contexts. |
| Shell consumers | Part of preceding action / source complete | Migrate all ten callers to the owner overload and enable/disable/dispose lifecycle: `ProgressionDemoUI.cs`; `Shell/AnimatorsDemoController.cs`, `AudioDemoController.cs`, `LevelDemoController.cs`, `NetworkDemoController.cs`, `ParallaxDemoController.cs`, `QuestDemoController.cs`, `SaveDemoController.cs`, `SettingsDemoController.cs`, `StateMachineDemoController.cs`. Save preserves `SaveableBehaviour` base registration/autosave through overrides. Existing service-event subscriptions are detached. Remove only the generated animation disc, parallax layer roots and Progression bridge when their controllers are removed. Network removal disposes its UI without stopping a borrowed network session. |
| `DEMO-OWNED-AUDIO-CONFIG` | P2 / source complete | `Shell/AudioDemoController.cs`: replace shared one-shot SFX calls with fully 2D `AM.PlayAt(clip, Vector3.zero, 1f, 0f)` voices. Unsubscribe, snapshot/clear the six owned clip fields, call `AM.ReleaseClips(ownedClips)`, then destroy only those clips and dispose the shell in `finally`. This includes an outgoing music crossfade; borrowed tracks/manager remain owned by their original caller. `Shell/LevelDemoController.cs`: deactivate/detach the generated LevelComponent/NoCode host before destroying its runtime curve. `Shell/StateMachineDemoController.cs`: stop both owned machines, clear their caches, detach/destroy the NoCode consumer, then destroy its StateMachineData and two StateData definitions. Recolor through the scene event handler instead of storing a renderer-capturing closure in ScriptableObjects. Guard Attack.OnExit when its dot has already been destroyed. |
| `DEMO-NULLABLE-LEVEL-CLEANUP` | Source3 confirmed runtime hazard / source complete, new execution pending | `Shell/LevelDemoController.cs`: capture the two optional borrowed `UnityEvent<int>` instances at setup, add/remove only this controller's listeners through null-safe calls, and detach from those exact instances even if the manager's public fields are cleared/replaced. Nested `finally` blocks separate listener removal and consumer detachment from destruction of the owned host, curve and shell. No borrowed event is instantiated/replaced and no LevelManager source/state is changed. |
| E28 remaining board-view sprite lifetime | Source3 failure / fixture correction complete, native acceptance pending | `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoRpgDemoTests.cs`: replace the two parameterized synchronous ownership cases with two top-level Play-transition UnityTests. Establish native OnEnable, observe two genuine frame-counter advances at each destruction boundary with a realtime deadline, retain all ownership/count oracles and unrelated-resource controls. Reject preexisting Play, exit only an acquired session, and retain/rethrow the first cleanup/barrier fault after the top-level exit. Both board views and NI39 nullable-Level runtime/Play source remain unchanged. |
| `QUEST-REGISTRATION-OWNERSHIP` | P2 / source complete | `Shell/QuestDemoController.cs` and `QuestDemoView.cs`: track only runtime-created QuestConfig instances as soon as created. Unbind consumers/events and call `QuestManager.UnregisterQuest(expected)` before destroying them. The shell's owned NoCode bridge is disabled/detached/destroyed. Resource/flow configs, replacement registrations sharing an ID, manager and saved states are preserved. No teardown reset/save deletion was added. |
| `DEMO-EXACT-POOL-RETIREMENT` | P1 / source complete | `Survivor/SurvivorGame.cs`: replace pool-root-name tracking with exact generated source references. Return tracked enemies/orbs and retire every owned source through `SpawnUtility.RetirePool(source)` in `finally`. Do not filter out Unity-destroyed source wrappers. Remove name-based shared-root deletion entirely. Outstanding rentals retain the pool owner's valid late-return path; unrelated sources with identical names remain usable. This action does not introduce a general Survivor world/session disposal framework. |
| `EDITOR-LOBBY-PERSISTENCE` | P1 / cross-owner source complete | Owned and implemented by **Neo.Editor**, not duplicated here. See [Neo.Editor receipt](Neo.Editor.implementation.md), action of the same name, and `Assets/Neoxider/Editor/Network/LobbyDemoSceneCreator.cs`. The current creator supplies real room/game prefab assets, distinct typed manager slots and persistent room/game UI listeners, consuming the Network presenter API. Editor save/reopen, cancellation and actual peer workflow remain central acceptance work. |

Owned public documentation updated:
`Assets/Neoxider/Docs/Examples/README.md`. It documents sample layouts/imports, shell lifecycle and
backward compatibility, ownership of native/audio/config/pool resources, programmer and scene paths,
Russian lifecycle guidance, nullable LevelManager events and the regression entry points.

## Consumed dependencies and compatibility

No missing source dependency currently blocks these owned changes.

| Owner / source receipt | Public contract consumed | Ownership rule |
| --- | --- | --- |
| [Neo.Tools.Spawner](Neo.Tools.Spawner.implementation.md) | `SpawnUtility.RetirePool(GameObject source) : bool` | Exact-source unified manager/fallback retirement; no singleton creation, shared-root deletion or pool-internals reflection. Already-destroyed Unity source wrappers are supported. Valid late returns destroy retired rentals. |
| [Neo.Quest](Neo.Quest.implementation.md) | `QuestManager.UnregisterQuest(QuestConfig expected) : bool` | Same-instance removal only; does not remove saved progress or another config with the same ID. |
| [Neo.Audio](Neo.Audio.implementation.md) | `AM.ReleaseClips(params AudioClip[])`; `AM.PlayAt(AudioClip, Vector3, float, float, ...)` | Selective current/outgoing/pooled voice detachment before caller-owned clip destruction. This is not cancellation of overlapping shared `PlayOneShot` calls; Demo now uses releasable 2D voices. Release notifications can throw after Audio has settled cleanup, so Demo's clip/shell disposal uses `finally`. |
| [Neo.Editor](Neo.Editor.implementation.md), [Neo.Network](Neo.Network.implementation.md) | Persistent lobby generation and presenter methods/events | Cross-owner work was inspected read-only; no Editor/Network file was edited. |

Existing serialized fields, public signatures, prefab/scene assets, IDs and asmdefs were retained. New
shell APIs are additive. The two-argument Build keeps its original root placement and now has explicit
caller disposal guidance. Shared cameras, EventSystems, managers and SurvivorArt caches are not disposed
by a shell context. A small internal destruction helper selects deferred Play Mode destruction versus
immediate Edit Mode destruction; using Neo.Extensions.SafeDestroy here would require an otherwise
unneeded shared asmdef dependency change. No version-specific stable-ID branch or instance-ID call was
introduced. Sample root layout and package/shared configuration were not changed.

## Authored acceptance sources — current revision pending execution

New owned fixture and metadata:

- `Assets/Neoxider/Tests/Play/Audit20261023NeoRpgDemoSessionTests.cs`
- `Assets/Neoxider/Tests/Play/Audit20261023NeoRpgDemoSessionTests.cs.meta`
  (`8137da6cbe0d4cb592f57a10bbf68e77`).

The fixture now contains ten UnityTest methods. Source3 results for its original nine methods are
recorded below; the added nullable-event control has not been executed. The original nine bodies,
including every `LevelController_ComponentRemoval` resource oracle, were retained unchanged in this resume.

| Case | Observable acceptance |
| --- | --- |
| `Shell_TwentyDisposals_ReturnNativeCountsAndSilenceCallbacks` | After warmup, 20 cycles return generated Sprite/Texture counts to baseline; each live callback runs once, hidden/disposed callbacks are silent, double disposal is safe, borrowed services/art survive. |
| `Shell_DisposingOneFrame_PreservesOtherFrameAndBorrowedArt` | Legacy two-argument Build remains available; disposal releases its original gradient after art replacement, preserves the borrowed replacement and a second independently interactive frame. |
| `NetworkController_TwentyComponentRemovals_ReuseOnEnableAndReleaseShell` | Real controller Start, 20 component-only deletions, disable/re-enable with the same frame/gradient, and final owner-hierarchy destruction all release native resources without removing borrowed camera/EventSystem. No network session is started. |
| `AudioController_DestroyDuringCrossfade_ReleasesSixClipsAndKeepsBorrowedTrack` | Actual demo SFX uses a clip-addressable voice; destroying the controller during a real outgoing-pad crossfade releases all six generated clips and leaves unrelated incoming music playing. Requires functioning Unity audio. |
| `LevelController_ComponentRemoval_ReleasesCurveAndOwnConsumers` | Public XP button adds 10 XP; component deletion removes the runtime curve and its consumer host while retaining the borrowed LevelManager. |
| `LevelController_PartialBorrowedEvents_PreservesObserversAndReleasesResources` | Two complementary controls leave one public manager event null and the other populated. Public XP/UI event-log, exact observer count/payload and unchanged event identity assertions remain positive controls. Clear the event fields before component removal; the owned consumer, curve, Canvas, gradient Sprite/Texture are destroyed, external observers and borrowed manager/map/state survive, and null fields remain null. No Mono helper or shared fixture change is needed. **UNEXECUTED.** |
| `QuestController_Destroy_PreservesReplacementAndSavedProgress` | Three generated definitions are destroyed; own registrations disappear, another instance under the same ID remains, saved JSON is unchanged and progress survives public Save/Load. |
| `QuestView_Destroy_ReleasesOnlyGeneratedFallbackDefinitions` | Legacy fallback definitions are unregistered/destroyed while an unrelated registered config and manager survive. |
| `StateMachineController_ComponentRemoval_StopsConsumerAndReleasesDefinitions` | Public Alert transition works; deletion leaves the captured machine stopped, destroys its three definitions/generated scene views and preserves shared art. |
| `SurvivorRemoval_RetiresExactSources_PreservesSameNamePoolAndLateReturn` | Real Survivor-created source and unrelated same-name source share storage; removing the game retires only its registration/parked clones, prevents recreation and safely destroys a late rental on return. External pool/root remain usable. |

Tests discover optional sample/dependency types at the assembly boundary, rather than adding a test
asmdef reference to hidden Samples~. Native objects and public behavior are the assertions; no private
pool-state counters are inspected. Reuse the existing in-memory SaveProvider fixture; restore provider,
random state, singleton auto-create flags, listener pause and borrowed camera settings after cleanup.
Service-isolation cases explicitly skip when an ambient singleton would be affected. The Survivor case
also skips new-input-only configurations because that sample currently requires legacy input. No
`NonParallelizable` attribute was introduced.

Preserved: `Audit20261023NeoRpgDemoTests` (audit Edit regressions) and
`Audit20261023NeoRpgDemoLifecycleTests` (audit Play regressions); their source3 results are below.
The current source needs a new freeze and rerun. Import Demo Scenes or use the active
developer Samples root for meaningful optional-assembly coverage; an absent-sample skip is not a pass
for Demo. Central verification must also exercise the real imported scenes and borrowed/external UI
roots, save/reopen lobby generation, network peers, native teardown timing and supported Unity versions.

## Actual verification and remaining limits

Performed **read-only source/diff review**, including all 13 runtime files changed in this implementation,
the ten migrated Build call sites, the original nine acceptance methods, current dependency signatures and
source receipts. Targeted Git status shows the owned runtime/docs edits and new fixture/meta; branch is
`demo`. The audit's previous static results apply only to that earlier phase.

**This source actor has run no Unity, compiler, build/test runner, static product checker/validator,
install, service, commit or push.** The coordinator's first frozen compilation failure and later source3
execution are recorded below. The nullable-event correction, its new acceptance control and the E28
lifecycle fixture correction have not been compiled/executed by this actor; no newer corrected native
outcome has been supplied in this resume. Prior snapshot results do not validate this revision.
No child agents, shared trackers/configuration, runtime source outside the manifest owner,
historical audit or plan were changed. This source-final receipt is a handoff for the coordinator's later
complete verification; it is not a release approval.

### First frozen compilation failure and bounded correction

The coordinator reports identical first frozen compilation failures on **Unity 6000.6.0f1 and
6000.3.14f1**: `QuestDemoView.cs(111,17): CS0103`, `NeoDemoShell` was not found in the current context.
The existing 6000.6 diagnostic artifact was read directly:
`Local/Audits/assembly-2026-10-23/unity/final/ff4e1aaceb2e4320a3f882f61c262b15/u6000_6_0f1/mirror-on-dev/Receipts/compiler-errors.json`.
The 6000.3 result is the coordinator's supplied evidence; this actor did not rerun either compiler.

Cause: `QuestDemoView` is in `Neo.Demo.Quest`, while the internal cleanup helper belongs to
`Neo.Samples.NeoDemoShell` in the same assembly. Corrected only that call in
`Assets/Neoxider/Samples~/Demo/Scripts/QuestDemoView.cs` to
`global::Neo.Samples.NeoDemoShell.DestroyOwnedObject(quest)`.
The helper remains internal; exact-instance quest unregistration still precedes destruction, and
object ownership, public APIs and all nine regression bodies are unchanged. No shared configuration,
metadata, sample layout or other owner's source was edited during this correction.

At that namespace-only handoff, both diagnostic jobs were stopped and root's execution gate was closed,
per the coordinator; this actor reviewed that correction as source only. Later source3 execution is
recorded separately below. The initial failure remains historical failure evidence.

### Source3 native evidence and DEMO-NULLABLE-LEVEL-CLEANUP

Read the owned test cases, exact failure messages/stacks and outputs directly from these existing
Unity 6000.3.14f1 graphical artifacts (no job was started):

| Artifact | SHA-256 | Owned fixture results in that snapshot |
| --- | --- | --- |
| `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml` | `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a` | 11 passed, 1 failed: all three lifecycle cases and eight session cases passed; the Level component-removal session case failed. |
| `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml` | `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2` | 12 passed, 2 failed: the Match3 (8 renderers) and TicTacToe (4 renderers) `GridRebuild_ReplacesEveryCellAndOwnsOneSprite` controls failed their sprite-destruction oracle at fixture line 125. |

The Play case records `NullReferenceException` at `LevelDemoController.BuildLevelManager:135`,
called by `Start:67`, followed by another at `OnDestroy:82` during teardown. Direct inspection of
`Assets/Neoxider/Scripts/Level/LevelManager.cs` confirms that `OnChangeLevel` and `OnChangeMaxLevel`
are nullable public fields; the manager itself uses null-safe invocation. Demo's unconditional
AddListener failed during startup, and its unconditional RemoveListener then aborted owned cleanup.
This is a real runtime hazard with a newly constructed/partially configured borrowed manager, not a
reason to initialize its events in the fixture or suppress exception logs.

Bounded correction in `Assets/Neoxider/Samples~/Demo/Scripts/Shell/LevelDemoController.cs`:

- Cache the exact optional event objects and use `?.AddListener` / `?.RemoveListener` for only the
  controller's own handlers. Do not overwrite borrowed events or clear external listeners. If resolving
  the existing service returns null, display an unavailable map section and retain the primary XP UI.
- Release generated resources in `finally` even if setup was partial or listener/consumer detachment
  fails. Deactivate/detach the generated consumer before native destruction, preserving cleanup order.
- Add the helper-free Play control described above in
  `Assets/Neoxider/Tests/Play/Audit20261023NeoRpgDemoSessionTests.cs`; preserve all original nine bodies.
  Update only the owned canonical Examples README for the nullable-event behavior.

The reservation map was read; these runtime/Play paths are disjoint from the reserved Editor/helper
moves. No Editor demo helper, prefab/migration target, shared test asmdef, fixture extraction, metadata,
other owner's code or sample layout was edited. At the NI39 handoff, the two Editor sprite-lifetime
failures remained unresolved for the coordinator's primary/native plan. Root subsequently assigned
the bounded helper-free fixture followup to this same actor; its correction is recorded below.

Source3 execution is closed, per the coordinator. This resume performed source/diff review only and
authored one additional test method with two partial-event controls. **A new freeze after all writers
finish is required before compilation/tests on either Unity version. No corrected native outcome is
claimed.** Other imported-scene, external-root, input/device and peer acceptance remains scoped to the
coordinator's complete verification rather than inferred from these fixture counts.

### E28 residual board-view sprite ownership: native lifecycle fixture correction

Read the E28 owner grouping in
`Local/Audits/assembly-2026-10-23/full-edit-failure-triage.md`, the matching full-name/message entries
in `edit-triage-groups-raw.json`, and both exact failure elements in the source3 Edit XML above.
Both old cases failed at `Audit20261023NeoRpgDemoTests.cs:125` with
`Destroying the view must release its generated sprite. Expected: True; But was: False`:

- `GridRebuild_ReplacesEveryCellAndOwnsOneSprite("Neo.Demo.GridSystem.GridSystemMatch3BoardView",8)`
- `GridRebuild_ReplacesEveryCellAndOwnsOneSprite("Neo.Demo.GridSystem.GridSystemTicTacToeBoardView",4)`

Read both complete board-view sources. Each creates/caches its own Sprite over the borrowed
`Texture2D.whiteTexture`; each releases only that Sprite in OnDestroy, using deferred Destroy while
playing and DestroyImmediate otherwise. Neither view has ExecuteAlways. The previous test allocated
the view in Edit Mode, manually called public Rebuild and assumed SetActive(true) had established
native Awake/OnEnable/OnDestroy eligibility. That prerequisite was not demonstrated. The XML proves
the old fixture observed a surviving Sprite; it does **not** establish an admitted runtime OnDestroy
ownership leak. No production lifecycle change is justified from that evidence alone.

Only the released, helper-free owned Edit fixture and this receipt changed in this resume:

- Introduce `GridRebuild_ReplacesEveryCellAndOwnsOneSprite_Match3` and
  `GridRebuild_ReplacesEveryCellAndOwnsOneSprite_TicTacToe` as separately discovered UnityTests.
  Each yields **EnterPlayMode at the top test level**, then arranges actual native components.
  Native OnEnable must create exactly four cell roots before the first explicit public Rebuild;
  this is a positive lifecycle-admission oracle rather than a private callback invocation.
- Preserve exact 8/4 SpriteRenderer counts, a single shared generated Sprite across every cell,
  three explicit rebuilds, complete replacement of old cell roots, and the original generated-Sprite
  destruction assertion. The initial transition candidate used two null yields at destruction
  boundaries; root subsequently identified that those Editor polls did not establish native frame
  advancement. The frame-counter correction below replaces that unsupported timing assumption.
  Owner GameObject and final cell roots must also die.
- A second independent view must retain four roots, its exact 8/4 renderers and a distinct live
  generated Sprite. An external renderer/art Sprite, the borrowed generator and Unity's exact shared
  white texture must survive deletion of the tested owner. The generated-asset oracle was retained,
  not converted into a null-tolerant or skipped assertion.
- Use the installed Unity Test Framework's supported UnityTearDown/ExitPlayMode instructions;
  session acquisition, failure retention and the destruction barrier were strengthened in the
  followup below. Non-transition tests retain immediate Edit Mode cleanup. The other twelve Edit case bodies
  are unchanged. The fixture still represents fourteen cases; the two old parameterized names map to
  the two explicit UnityTest names above.

The installed `com.unity.test-framework@aebcb98460c6` EnterPlayMode, ExitPlayMode,
UnityTestAttribute and UnityTearDownAttribute sources were inspected read-only, including the supported
UnityTearDown exit pattern. FieldGenerator's actual Awake path was inspected before arranging the
active 2×2 generator. The reservation map has no entry reserving this Demo fixture; no Mono helper,
new fixture assembly, shared reference, metadata/GUID, prefab/migration target, sample layout or other
owner's file changed. No ExecuteAlways, new Ignore/error whitelist or fake lifecycle call was added.
No public runtime/serialized contract changed, so canonical public docs were left unchanged.

**Source-final, UNEXECUTED correction:** read-only artifact/source review is the actual validation in
this resume. Main product execution remains closed; no Unity/compiler/test/checker was launched.
The source3 failures remain historical failures. Actual compilation, discovery of both replacement
cases, native lifecycle admission, exact counts and resource destruction/survival must be verified in
root's next frozen native run. If that admitted run still shows a generated Sprite surviving owner
destruction, it is remaining runtime evidence to investigate, not a result this fixture assumes away.
NI39 nullable-Level runtime cleanup and its ten-method Play fixture were preserved unchanged.

### E28 root frame-barrier review and acquired-session cleanup

Read the complete [RootDemoFrameBarrierReview.md](RootDemoFrameBarrierReview.md), the updated request
and contract. Root's reviewed receipt baseline was
`88043db84e7670c120c8ea75d0767d0c2e17b90d36ad47cf6f4633c45b83e3c1`.
This is another **source-precondition correction**, with no new native outcome.

Only the owned helper-free Edit fixture and this receipt changed:

- `WaitForNativeFrames` observes **two separate positive changes in actual `Time.frameCount`**.
  Null yields merely poll that observation; they are never counted as frames. A monotonic
  `Stopwatch` caps each barrier at ten realtime seconds. Leaving Play or a counter reset fails
  explicitly. Every rebuild, owner deletion and deferred fixture cleanup uses this barrier.
- Both named UnityTests reject existing/pending Play and an outstanding owned session before
  their top-level EnterPlayMode. `_ownsPlaySession` is set only after that instruction successfully
  returns. A failed precondition does not acquire or exit someone else's Play session.
- Teardown attempts destruction of every object in the private owned list, preserving its first
  genuine failure with `ExceptionDispatchInfo`. It advances only the managed frame observer to
  catch barrier faults while still reaching the **direct top-level ExitPlayMode**. The exit is
  conditional on this fixture's acquisition flag and a still-active/pending session; the flag is
  released once the Editor has left Play. The retained first fault is rethrown with its original
  exception/stack after the exit attempt, so a later cleanup/timeout fault cannot replace it.
  Native mode instructions are not nested in the frame observer or another helper.
- All fourteen logical cases and both explicit Match3/TicTacToe test names remain. Native OnEnable,
  all three rebuilds, exact 8/4 renderers, the same owned Sprite, old-cell and owner destruction,
  second-owner/art/white-texture/generator survival assertions are unchanged. The other twelve
  case bodies and NI39 Level runtime/Play source were not edited.

Actual validation here was source/exception-flow review and read-only fingerprint bookkeeping.
No Unity, compiler, test/checker, process control, install, agent, commit or push was invoked.
No production API/settings, native Mono helper, shared reference, metadata/GUID, sample layout,
vendor/migration asset, other owner's file or canonical public documentation changed.

Current SHA-256 fingerprints (file bytes, not execution evidence):

| File | SHA-256 |
| --- | --- |
| `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoRpgDemoTests.cs` | `57d639a86be30870e1de158b4f882b2086c00be12c5e855ea4e5edfd18cd1285` |
| `Assets/Neoxider/Samples~/Demo/Scripts/Shell/LevelDemoController.cs` | `48958c8733da65e5b01e8e8fb1879cba3addabda5fde8bbe6c63a35002b20c11` |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoRpgDemoSessionTests.cs` | `08ee414b64aa98cb6db60cc5441755f126fff2ad185483ac99c1a3abe86810a5` |
| `Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/GridSystemMatch3BoardView.cs` | `ac2d66a84e776ee5497599e37fa89ee777af6ca7c455eb72c7e2416b7bc53e07` |
| `Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/GridSystemTicTacToeBoardView.cs` | `ad3bb6c1180fe4bff96285409ffef74a1f02bd2daf8d2563cad7d6d73edded5d` |

The product gate remains closed. **All corrected native frame/cell/resource outcomes and compilation
remain UNEXECUTED**, pending root review and a new frozen run after every source writer finishes.
No timeout, absent frame progress or cleanup fault is converted into a pass. Source writes stop at
this handoff.

The plan's larger generic session/resource framework, runtime-authoring factories and broader sample
architecture remain proposals. This implementation deliberately uses the existing Context and the
owners' exact resource APIs; those larger designs are not presented as completed features.
