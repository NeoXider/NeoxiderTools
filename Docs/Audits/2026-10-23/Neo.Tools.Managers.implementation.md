# Neo.Tools.Managers — implementation receipt

Target: `demo` / `11.0.0`; original actor `neo-audit-1023-b07-06`.

Status: **SOURCE-FINAL — current bounded NeoxiderPages prefab correction complete**.
No source dependency remains for this request. Native admission of these new asset bytes remains
unexecuted until the next root freeze. Earlier runtime source completion is retained below as history.

## 2026-10-25 — root-approved NeoxiderPages prefab correction

**SOURCE-FINAL for the current bounded asset request; fresh native admission is UNEXECUTED.**
Only the nine release prefabs listed below and this receipt were written in this dispatch.
The earlier GM/runtime source closure below is historical and did not certify these repaired asset bytes.

### Native evidence and exact removals

Read the updated implementation contract/request, root's Page Load lines 201–295, and the retained
ba515 diagnostic findings for both natural Unity 6.3 imports. Input evidence:

- `Local/Audits/assembly-2026-10-23/unity/maintenance/ba515785480c4d2c9c791b2b91cc0e82/diagnostic-results.json`
  SHA-256 `ae1e045c7d54a8eb7d0d12487661f73b3e88d58f3c8a42dc8967ebda40b124c1`.
- Frozen source manifest: `2d12ae058b989ce064e86a8d7db8223ecb65241d5dcd19babfb2fd7082adfc70`.
- Raw logs: `Local/Audits/assembly-2026-10-23/unity/m/f169d9f1/c/Receipts/Editor.log` and
  `Local/Audits/assembly-2026-10-23/unity/m/f169d9f1/l/Receipts/Editor.log`.

Each cohort records the same 12 nonexistent component local IDs in seven example prefabs. Source
enumeration found no duplicate document IDs or repeated resolved component entries before repair;
Unity's generic duplicate/missing-component messages do not justify removing any other valid component.

All paths in the following two tables are relative to
`Assets/Neoxider/Samples~/NeoxiderPages/Prefabs/Page/`:

| Changed prefab | Deleted dangling m_Component IDs | Deliberately removed inert Pro document + entry |
| --- | --- | --- |
| `_Page base.prefab` | — | `3037656400551465100` |
| `Example/End Page.prefab` | `5043587056089648212`, `3968793066292654786` | `383798610104139837` |
| `Example/Game Page.prefab` | `6298829328643137154` | — |
| `Example/Lose Page.prefab` | `4452082432887727440`, `4394070271191383668` | `1154498771708353932` |
| `Example/Page Load.prefab` | `7970048788368920216`, `5822085909133773776` | `8051529374577195505` |
| `Example/Pause Page.prefab` | `4400833269697777927`, `4683553654222530915` | `5721949178245906421` |
| `Example/Privacy Page.prefab` | `8211126209374537711` | — |
| `Example/Shop Page.prefab` | — | `4944389172882614602` |
| `Example/Win Page.prefab` | `7631292229471538435`, `4495803230795778436` | `3469362002819826030` |

Deleted 12 demonstrably undefined component entries. The seven separately identified Pro records were
valid documents and are removed only under the explicit inert-Pro decision below, together with their
own component entry. No other valid script/component was removed or replaced.

### Pro dependency decision — proven inert for the authored prefabs

Inspected local vendor API/XML/source and raw PE/CLI type metadata/IL without loading or executing the
vendor assembly. `Assets/Plugins/Demigiant/DOTweenPro/DOTweenPro.dll.meta` identifies GUID
`aa0b1eebb5db27a419fa4564bbe5c9c5`. The DLL SHA-256 is
`db5225f6db87969a03b48a56e2dc3828b38384adca44da0f89248a2bb483b056`.
Type metadata plus the serialized MonoScript type-ID hash maps fileID `40306178` exactly to
`DG.Tweening.DOTweenVisualManager`, not DOTweenAnimation or an unknown missing script.

The metadata/IL establishes this behavior:

- Awake caches only `GetComponent<DG.Tweening.Core.ABSAnimationComponent>()` on its own GameObject.
- All seven records configure `preset = 0` (Custom), `onEnableBehaviour = 2` (Restart),
  `onDisableBehaviour = 0` (None). Restart calls that cached component's DORestart only if non-null;
  None performs no disable effect. The restart-from-spawn Update path is not enabled by this preset.
- Every host has only RectTransform, CanvasRenderer, the ordinary Unity UI Image
  (script GUID `fe87c0e1cc204ed48ad3b37840f39efc`), this manager, and in some cases the undefined
  entries listed above. No ABSAnimationComponent driver exists there. DOTweenPath's metadata base is
  ABSAnimationComponent; no DOTweenPath or DOTweenAnimation driver is serialized in these prefabs.
- Reference enumeration across serialized text `.prefab`, `.unity` and `.asset` files under `Assets`
  found only each manager's own component entry and definition, with no other target reference.
  The Shop Page Variant overrides no removed component; its source prefab remains intact.

Thus these seven manager records have no active animation/timing/event behavior to translate. Removing
them removes the accidental optional-Pro dependency while retaining the actually configured page
behavior. No replacement tween framework/component is needed. The existing canonical NeoxiderPages
README already promises operation without DOTween/Pro and documents sample re-import; that contract
has not changed, so README/PM guides were read but not edited. Vendor DLLs/source/metas were untouched.

### Hashes and preservation

| Changed prefab | SHA-256 immediately before this repair | SHA-256 repaired source |
| --- | --- | --- |
| `_Page base.prefab` | `a6c73ae33d8a7e26ff74e8fc5a62cd75ddeb096acfd57da37508cbdede69af3a` | `515df0c42c26842994db3ae8c4d9ade98b8635860d2d1eb43c9a728a38191ac7` |
| `Example/End Page.prefab` | `49cedb3561b7cca5e95b503e6896ea0cdf30007fc5989ae8d4baf1ec7bded825` | `b44d43a81bae8ae854279971f0bced815c5da2e4d8cc76a3157c1ef4e20318fa` |
| `Example/Game Page.prefab` | `4d6f60141876d96ca977fbf212602a05fd80ab89118559e28967491b9da0782d` | `944d9b64c7e9b3b72ceadb4e5d19f79806b3aa3a2c0a2d1f8463375e3c940387` |
| `Example/Lose Page.prefab` | `f3dd5f90b260c7fb942cc4d2884ea254358af8d2eccd633aef17809fa87a198a` | `21012d28cbe3d35fef9bb208695662fc5966de9337f3b09d5978229da469f6bc` |
| `Example/Page Load.prefab` | `2401b489afb73abdd00e68f7b3c0a04665b01f44f4c4c2774c525e158f64420e` | `fd69616f6172eb817ddc99958cd3be3528457621f947f08852b8b2655e5cb897` |
| `Example/Pause Page.prefab` | `a0f030f020313732d53d501d1abc7e94f64e8a21e9a2f9edc1c88fd9a20c3b6e` | `1ef611cdd370aa1b246d3d8739d09473697384dbd70a58c44129914e3bb4be13` |
| `Example/Privacy Page.prefab` | `fce5782a1e00cf091afa74c2ef49aef9ba566dcbc9397596d8701e3191383aff` | `7c041adf414ffc193248a5204b1fd62304d2dca1e5b99857f8870f8c56658959` |
| `Example/Shop Page.prefab` | `635506d30cef7f0f32b268d7458ae7b0fa7bbbebf9327f1fc1d185bdadd56b02` | `0368024b5b521b7eb8b162987773ecefc65479dabddd4de21fc550b1432188ca` |
| `Example/Win Page.prefab` | `b1ee86c380963e755b2c5def33f694fa6d9c9fb53bf951e700e876be3d262439` | `7b585dc2b252914f091d87f965803cf18ac6c3f907eda787a42980c06dd0367c` |

The byte patch is deletion-only: 124 lines / 4,029 bytes across nine prefabs. It neither reserializes
assets nor changes serializedVersion, local-ID numbering, GUIDs, line endings or BOM. Before writing,
retained document IDs/order and every retained document body were compared with the expected source
minus only the listed component-entry lines. All valid retained MonoBehaviour/public-field/event data,
GameObjects, hierarchy, RectTransforms, images, sprites, materials and other wiring retain their bytes.

All 21 release NeoxiderPages prefabs were enumerated, including base/variant/PM and non-page prefabs.
The final source has 558 unique per-file YAML documents and 355 explicit local m_Component references:
zero undefined references, duplicate component entries, duplicate per-file document IDs or Pro-DLL GUID
occurrences. Newline normalization was used only for reading/enumerating CRLF text, never for writing.
The other 12 prefabs and all 110 sample `.meta` files are byte-identical to their pre-dispatch hashes.

No imported `Assets/Samples` copy, ThirdParty/vendor file, runtime API, other owner's source, shared
reference/configuration/tracker, package version, main sample layout or protected `DocsEn.meta` was edited.
The only non-prefab write is this owned receipt. The previous GM implementation and regression source
were not replayed or modified.

### Evidence boundary and required new native acceptance

Work actually performed: small source/reference reads, raw vendor metadata/IL inspection, guarded byte
edits, explicit source enumeration/hash bookkeeping and focused diff reading. No Unity, compiler, tests,
product checker/validator, install, service/process control, child actor, commit or push was launched.
No new test source was written because the approved correction scope permits only these prefabs and
this receipt. Source enumeration is not a native PrefabImporter or runtime admission result.

Root must create a NEW freeze, then repeat full native UPM three-sample import and load every
NeoxiderPages prefab, including its variants, in isolated supported Unity hosts. Include the no-Pro
capability lane without altering the main project's vendors, confirm no broken PPtr/duplicate/missing
component warnings, and exercise page loading/navigation/events/rendering. Previously imported
`Assets/Samples` copies remain unchanged and need re-import; old source3/ba515 observations cannot
certify this corrected source. No source dependency is outstanding; native acceptance remains pending.

## Earlier GM/runtime implementation — historical source closure

The following records the prior completed runtime dispatch, retained as context. Its original source
closure and unexecuted acceptance descriptions are not evidence for the new prefab bytes above.

### Approved action completed

**MANAGERS-STALE-STATE-BROADCAST**: GM now uses one instance transition generation to abandon remaining
old gameplay broadcasts and composite-flow steps after a synchronous callback performs another effective
transition. In particular, `OnStateChange(Pause)` -> `Resume()` publishes the nested Resume without an
obsolete outer Pause. Returning to the same final state through intervening transitions does not revive
the old generation.

The same fence applies to Preparing, StartGame/restart, StopGame, Menu and terminal outcomes. A transition
from a terminal `OnStopGame` suppresses the old Win/Lose/End callback. Menu/StartGame stop after a resumed
observer takes over, and Menu does not overwrite a transition from its StopGame observer. Existing virtual
Resume/StopGame hooks that leave state unchanged remain supported; those hooks are not required to create
a synthetic transition. Lazy EM resolution is fenced before and after its initialization because OnAwake
can itself change GM state.

Nested repetition of the current State payload from inside a state notification is suppressed. The latest
dispatched-version marker is a snapshot of the common generation, not another incrementing counter.
The notification flag is restored in `finally`; observer failure retains the committed state/version and
propagates normally. State payloads and gated debug output use the captured announced value.

### Exact API handoff — Neo.UI dependency supplied

`Neo.Tools.GM` exposes this additive, public, read-only API:

```csharp
public ulong TransitionVersion => _transitionVersion;
```

This is the exact `public ulong TransitionVersion { get; }` contract requested by Neo.UI. The single
backing counter advances with `unchecked(previous + 1UL)` exactly once before each effective State
transition, before `_state` changes and before callbacks. A normal Pause from a non-Pause state therefore
advances exactly once; reentrant effective transitions advance further. Same-state assignments and
guarded no-op Pause/Resume calls do not advance it. No UI-specific counter, public setter, serialization
field or reset/rewind on the same object is introduced. Equality comparison follows unchecked ulong wrap.

Read the full current `Neo.UI.implementation.md`, `PausePage.cs`, and its reentrant pause PlayMode source
as context. All three existing `TransitionVersion` consumer reads in AcquireManager/OwnsManagerPause
can now bind without changing Neo.UI code. PausePage can keep its captured exact manager/time baseline,
expected next version and current-instance release check. The dependency direction remains
`Neo.UI -> Neo.Tools.Managers`; no reverse reference or asmdef change is needed.

Neo.UI's independently written dependency receipt predates this property. Its required API is now
published, but integrated compilation and page acceptance have not been executed. No Neo.UI source,
receipt, shared manifest or review tracker was edited by this actor.

### Exact files changed in that earlier runtime implementation phase

- Runtime: `Assets/Neoxider/Scripts/Tools/Managers/GM.cs`.
- Regression source: `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoToolsManagersGameStateTests.cs`.
- Public docs: `Assets/Neoxider/Docs/Tools/Managers/GM.md`, `EM.md`, `README.md`.
- This completion receipt: `Docs/Audits/2026-10-23/Neo.Tools.Managers.implementation.md`.

The existing regression file and its metadata were reused; no new package asset or GUID was needed.
Earlier coherent Bootstrap/singleton/pause fixes, the other owned regression files and their metadata
were preserved. Original `Neo.Tools.Managers.md` and `Neo.Tools.Managers.plan.md` remain historical and
unchanged. No shared configuration, package version, scene, prefab or sample layout was modified.

### Compatibility and documented 11.0 behavior

All existing public method/property signatures, serialized names/defaults, enum values and event fields
remain. Explicit same-state assignments outside a state callback keep their legacy notification without
advancing the version. Existing guarded method no-ops remain no-ops. Exact captured time restoration,
including zero, still precedes applicable notifications. Ordinary synchronous event order is retained.

Intentional 11.0 change: a superseded GM flow no longer publishes its remaining obsolete gameplay events
or overwrites the nested observer's newer transition. Repeating the current payload from inside its state
callback no longer recursively republishes that generation. An already-entered UnityEvent invocation is
not interrupted: subsequent listeners can observe newer state while receiving its captured original
argument. Direct EM helper calls remain explicit broadcasts without GM state/version validation.

The three owned guides now explain these distinctions, exact generation arithmetic, read-only C# pause
ownership, synchronous/exception behavior and scene/UnityEvent setup. They link the existing PausePage
guide for overlapping-page ownership. No general pause/lease platform, state transaction queue, editor
adapter or speculative keyed-registry/IL2CPP rewrite was introduced. The substantial initialization-queue
improvement in the original plan remains a proposal, not implemented work.

### Authored acceptance — UNEXECUTED at that runtime handoff

Extended the existing GM fixture by 14 test methods / 21 parameter-expanded source cases and strengthened
the original PauseStateListener case with a full event/version trace. The GM fixture now declares 24 test
methods / 34 source cases; together with the preserved 6 Bootstrap and 3 singleton cases there are 43
authored cases. These are source declaration counts, not executed discovery or pass counts.

- Normal Pause publishes the exact next version inside both state and pause callbacks for baselines
  0, 0.35 and 1. Repeated Pause/Resume guards add no transition; the exact baseline returns.
- State(Pause) -> Resume yields `state:Pause, state:Game, resume`, with version +2 and no stale Pause.
  State(Game) -> Pause during Resume yields the inverse trace without stale Resume.
- Resume/re-pause inside Pause's callback ends at version +3 with one new-generation Pause event;
  a same-state repeat after the nested transition adds no payload.
- Bounded same-payload reentry, ordinary same-state notification, and a throwing state observer exercise
  deduplication, no rewind and `finally` guard release through public state/events.
- Each terminal outcome redirected by OnStopGame omits the old outcome; a Win-state restart omits both
  old Stop and Win. Menu/Start/restart composites preserve the newer observer transition.
- Custom virtual no-op Resume/Stop hooks remain composable without fabricated version increments.
- A newly resolved inactive EM changing GM state from OnAwake does not receive an old Pause broadcast.
- An ordinary command sequence preserves the complete existing state/gameplay event trace.

Assertions inspect actual public state, transition versions, callback arguments/order and time scale.
The fixture retains exact event-hub/object cleanup and restores its original global time value. It relies
on the isolated/exclusive runner contract. No unavailable `[NonParallelizable]` annotation was added.

### Work performed and remaining evidence debt at the runtime handoff

Read the full implementation contract/request, original audit/plan, exact manifest entry, current owned
GM/EM/test/docs, the complete UI receipt and relevant consumer source. Used small cmd.exe source reads,
reference enumeration, focused diff reading, and scoped source/doc edits. Read back the final regression
source and reviewed runtime/document changes. No completed earlier patch was replayed.

No Unity, compiler, tests, product validator/checker, install, service, commit, push or child actor was
launched. In particular, the earlier audit's static-check receipts were not rerun or relabelled as passes
on this source. Diff/UTF-8/meta/package validators also remain behind the global execution barrier.

After every writer finishes, root must freeze source and run the focused Managers fixtures plus the
independent Neo.UI PausePage lifecycle suite on the installed Unity 6.3 and 6.6 hosts. Preserve nonempty
Edit/Play discovery, exact baseline/version/event assertions, replacement/prepaused manager cases and
both overlapping-page closure orders. Other Unity branches, native lifecycle/cursor delivery, keyed
reload-disabled recovery, reflection/IL2CPP behavior and package/text checks remain evidence debt.
Arbitrary continuously alternating callbacks and independent raw time writers are not a general
transaction/ownership guarantee. Final root push remains conditional on later thorough verification.
