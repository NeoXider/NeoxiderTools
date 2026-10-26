# Neo.Tools.View — implementation receipt

Status: **SOURCE-FINAL Root continuation; corrected full-order native acceptance PENDING.** Original author history and failed execution evidence remain below; Root completed the bounded fixture correction after subagent usage limits.
`gpt-6.1-sol`, reasoning `max`; branch `demo`, target `11.0.0`, original audit base `ca198556`.
All four assigned bounded corrections, `VIEW-NATIVE-COLOR-AND-URP-LOOKUP` and the queued p5 Image
target lifetime correction plus the NI77 post-rect handle check are implemented in source. The P6 continuation
corrects only the dirty fixture's deferred OnKill observation. Historical snapshot3/p5/P6 results are separated
below from new unexecuted acceptance. No dependency API or shared assembly reference is pending.

Read the complete implementation contract/request, original audit/plan, current manifest entry, relevant
owned source, published Network APIs, and the complete `Tests-Play-Network.implementation.md` receipt
identified by root as `3a47f47c`. Existing coherent audit changes and both original regression sources
were preserved. Historical audit/plan and shared trackers were not rewritten.

## Approved actions completed in source

| Action | Exact implementation and observable acceptance |
|---|---|
| **VIEW-DEFERRED-INPUT / A-29** | `SelectorModel.SetExcludedIndices` and `SetUsedIndicesForUnique` materialize a replacement HashSet before clearing the destination. Direct alias remains a no-op; null clears only the requested set. Deferred filtered inputs retain their values; an enumeration exception leaves that destination unchanged. No whole-Configure/iterator-side-effect transaction is promised. |
| **VIEW-NETWORK-SNAPSHOT-EVENTS / A-30** | `Selector` stages received index, mode, exclusions and complete active set before publishing Selector-level events, persistence and final count. RPC and late-join paths share the same received-state application. Intermediate selection/count publication is suppressed; local selection/object/save/count order is retained. Individual Unity activation/SelectorItem callbacks remain per-item. |
| **VIEW-PEER-RATE-BUDGET / A-31** | Every Selector Command now rejects queued local-only and unauthorized/unknown-policy calls before sender-aware `RateLimitCheck(sender)`. Selection/random ingress additionally rejects absent/unrepresentable bounds, out-of-range selection indices and disabled random before budget. Accepted remote callers use the published per-connection unscaled-double limiter. Known policy/null/exact host behavior remains delegated to NeoNetworkState; direct public server/host/offline paths are retained. |
| **VIEW-INTEGER-WRAP / A-32** | Bound calculation, wrapping, stepping, effective sums and inclusive enumeration use long intermediates. Count/offset configurations with logical bounds outside Int32 explicitly reject. Singleton pools at int.MaxValue terminate through Inspector, included/random/unique and step paths. Configure checks bounds before changing configuration; component Count/IndexOffset validate before assignment. Raw IndexWithOffset is checked, rather than silently wrapping. |
| **p5 Image target lifetime** | ImageFillAmountAnimator's captured getter/setter admit only the live, still-assigned Image and the exact current handle. Invalid callbacks retire only their captured non-recyclable tween. A newly resolved Image resets target-specific width/readback capture; owner disable and mode restoration do not apply the old target's state to a replacement. Six owned native regression sources retain positive progress, exact ownership/kill counts, fresh writes and strict scoped target-warning assertions. |
| **NI77 captured handle after rect callback** | The animated SlicedWidth setter passes its captured handle into ApplyWidth. After rect.anchorMax returns, visibility writes require the same live assigned Image and the same current handle. An immediate zero from the public dirty callback keeps its hidden state. Immediate/disabled snap paths retain their existing behavior. One additional native regression source exercises this exact boundary. |

### Concrete harness dependency resolved

The unchanged three-process harness's denied B → immediately valid owner A scenario now reaches the
same admission/mutation core as the production Commands. B cannot consume either A's budget or its own
budget while denied. A's accepted additive random result preserves the full active `{0,2,3}` and excluded
sets; a repeated accepted caller still encounters the ordinary limit. A local-only queued call similarly
cannot consume its caller's budget before the component is reenabled. No sleep or limiter reset was added
to mask the failure. The production interval remains the base default; only the owned regression subclass
overrides it to 1000 seconds so expiration cannot conceal missing guards.

This resolves the **source dependency**, not remote-delivery evidence. The separate-peer harness and all
Network-owned files remain untouched. Its build/Weaver/transport acceptance still belongs to the frozen
source verification operator after every writer finishes.

## Exact files and API surface

Current bounded runtime changes:

- `Assets/Neoxider/Scripts/Tools/View/Selector.cs`.
- `Assets/Neoxider/Scripts/Tools/View/SelectorModel.cs`.
- `Assets/Neoxider/Scripts/Tools/View/LightAnimator.cs` (snapshot3 continuation: optional assembly lookup).
- `Assets/Neoxider/Scripts/Tools/View/ImageFillAmountAnimator.cs` (p5 continuation: captured target/handle lifetime).

Public method signatures and serialized field names/defaults are preserved. One internal static
`SelectorModel.GetBounds(int, int, bool)` is reused by the component to validate prospective configuration
without mutation. Under `MIRROR`, two additive **protected** server-adapter methods share the actual Command
core with typed derived adapters/tests:

```csharp
protected bool TryApplyNetworkSelectionRequest(int newIndex, bool fillMode,
    bool deactivateNonSelected, NetworkConnectionToClient sender);
protected bool TryApplyNetworkRandomRequest(bool deactivateOthers,
    NetworkConnectionToClient sender);
```

They require an active server and return local admission only. They introduce no client acknowledgement,
optimistic rollback, new authority enum, mandatory component or test-only public gameplay method.

Earlier owned fixes remain in `BillboardUniversal.cs`, `LightAnimator.cs`, `MeshEmission.cs`, `StarView.cs`
and the two selector files. Other inventory source is unchanged. No runtime file, assembly or broad
SelectorCommand/snapshot/session framework was added. The substantial model-as-authority proposal remains
explicitly unimplemented in the historical individual plan.

Owned canonical guides updated this phase:

- `Assets/Neoxider/Docs/Tools/View/README.md`.
- `Assets/Neoxider/Docs/Tools/View/Selector.md`.
- `Assets/Neoxider/Docs/Tools/View/SelectorModel.md`.
- `Assets/Neoxider/Docs/Tools/View/LightAnimator.md` (split URP runtime compatibility and stripping limit).
- `Assets/Neoxider/Docs/Tools/View/ImageFillAmountAnimator.md` (target loss/replacement lifecycle).

The other five previously updated component guides remain intact. C# and existing Inspector/UnityEvent
workflows are documented without additional NoCode wiring.

## Deliberate 11.0 compatibility details

1. Impossible logical Int32 bounds now throw `ArgumentOutOfRangeException`; a raw int IndexWithOffset
   sum outside Int32 throws `OverflowException`. Valid ordinary configurations keep their selection rules.
   `GetEffectiveIndex` clamps the widened raw sum. UniqueRemainingCount saturates at int.MaxValue when its
   logical candidate count cannot fit that int readback. Enumeration remains proportional to pool size;
   no zero-GC or per-frame performance guarantee is established.
2. Invalid incoming logical selection indices now reject before mutation/budget. Normal public Set still
   wraps/clamps before dispatch. Exclusion entries keep their existing integer-set/CSV semantics, including
   entries outside current bounds and null/empty bulk clearing; the parser is not replaced with a new policy.
3. The private 11.0 selection RPC now includes exclusions alongside the active indices. A received callback
   does not depend on a later dirty SyncVar packet for the matching exclusion set. All peers must use the
   matching package version. Existing Command signatures, SyncVar names, public APIs, save keys and assets
   are retained; root should carry this wire-version note into its release migration guidance.
4. Selector-level received-state observers/persistence see the complete state. This does not make Unity
   OnEnable or each SelectorItem callback atomic, and denied optimistic client state is not rolled back by
   a newly invented acknowledgement path.
5. Optional Light2D discovery now tries `Unity.RenderPipelines.Universal.2D.Runtime` followed by the
   existing `Unity.RenderPipelines.Universal.Runtime` identity. No required URP assembly reference or
   public/serialized API change is introduced. Reflected accessors and managed stripping still need
   actual player acceptance; package presence alone does not prove rendered output or IL2CPP preservation.
6. Image target loss/reassignment stops the captured tween on its next native tween update without
   completing it. Later input resolves the current valid Image and captures its own authored width.
   Existing serialized names/defaults, duration/easing/time-scale policy, FillAmount/SlicedWidth meanings,
   inversion, disabled input/snap and valid-target mode restoration remain. No public API was added.

No scene/prefab/sample layout rewrite, package version/configuration/asmdef change or DocsEn change was made.

## Authored regression source — latest correction UNEXECUTED

New owned files:

| File | Cases and assertions |
|---|---|
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsViewSelectorInputTests.cs` | **16 cases**: filtered self-derived exclusions/history, throwing enumeration with unchanged previous sets and next random output, null clearing, exact Int32 wrap, widened effective readback/raw overflow, extreme singleton included/random/unique/step termination, invalid Configure preserving prior configuration, and component Inspector/item/configuration agreement. |
| `Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsViewNetworkTests.cs` | **13 cases**: denied B → owner A additive payload, B subsequently permitted without consumed budget, same-peer cooldown, independent A/B budgets, local-only/invalid/disabled-random/unknown-policy → immediately valid controls, trusted null server callers, complete snapshot state inside all Selector callbacks for GameObjects and SelectorItems, and retained local fill event order. |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsViewImageFillTests.cs` | **7 UnityTest methods; latest dirty-case timing UNEXECUTED**: the six preserved target-lifecycle scenarios plus NI77 public dirty-callback immediate zero; real Play Mode/Normal Update progress, exact owned retirement, eventual exact OnKill after native cleanup, unrelated progress, fresh target/handle, disabled snap and replacement-anchor restoration, scoped warning failures, and three-native-frame zero/hidden stability after callback replacement. |

New files have unique generated `.meta` assets:

- Input tests: `08d980b999694645a8e234b8de5b3a65`.
- Network tests: `6332bf352e3a4aa5beaf720ff7d38c58`.
- Image lifetime tests: `f381f08c024948a787c38c1a44e9e3e5`.

Network regressions use the actual shared limiter and spawned server-side Selector state with typed
protected request cores. Their peer connection objects and initial-state replay are **local unit coverage**,
not remote transport/Weaver acceptance. No private Command body is invoked. They reuse the existing
DummyTransport and Mirror public server lifecycle; own their objects/transport, restore transport/RNG,
require an isolated exclusive runner and add no unavailable NUnit parallelization annotation.

Original sources: `Audit20261023NeoToolsViewSelectorTests.cs` (10 cases, preserved SHA-256
`fe8eadd9c11654ac7e1a3ed6401df18b29fdaee7850a07285667a5d64c4dfecd`) and
`Audit20261023NeoToolsViewLifecycleTests.cs` (11 cases, historical snapshot3 SHA-256
`10d5fbde3917a8a62a0469d266071ad43fd0695c735572742891be1dcbf39d4a`).
The latter now contains the scoped native baseline and URP lookup corrections described below.
Combined owned regression source contains **57 authored cases**, with the 13 new network cases conditional
on MIRROR and existing URP/HDRP cases explicitly optional. No authored count is a passed/discovered count.

## Snapshot3 continuation — VIEW-NATIVE-COLOR-AND-URP-LOOKUP

Read the complete updated contract/request and current manifest entry, the reservation map, exact owned
XML failures/reasons/stacks, relevant triage entries, current runtime/fixture and installed vendor APIs.
The Lifecycle fixture is absent from the 51 reserved files and contains no embedded MonoBehaviour helper.
No fixture relocation, shared asmdef, harness or Network-owner file was changed.

### Actual prior native evidence, before this correction

Root's graphical Unity 6.3 snapshot3 XML contains **26 owned Edit passed** and **24 owned Play cases:
21 passed, 1 failed, 2 skipped**. These results belong to that frozen snapshot, not the corrected source.

- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml`:
  SHA-256 `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`.
- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml`:
  SHA-256 `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a`.
- `MeshEmission_DestroyComponent_RestoresSurvivingRendererAndReleasesOwnedClone` failed at fixture
  line231, `emission.CurrentColor == authored`. Expected and actual both print
  `RGBA(0.200, 0.400, 0.600, 1.000)`; the stack enters the native UnityTest enumerator. The XML does
  **not** reveal full channel values or establish an incorrect runtime restoration.
- `Light2D_WhenUrpIsAvailable_AnimatesAndRestoresRealComponent` was ignored as URP unavailable.
  Actual installed snapshot3 URP is **17.3.0**, with both main and 2D runtime DLLs present. In
  `Library/PackageCache/com.unity.render-pipelines.universal@3b809f23691d`,
  `Runtime/2D/Unity.RenderPipelines.Universal.2D.Runtime.asmdef` owns `Runtime/2D/Light2D.cs`:
  `UnityEngine.Rendering.Universal.Light2D`, public writable `Color color` and `float intensity`
  (lines261/266). The inspected vendor source exactly matches the snapshot3 copy. The old lookup
  searched the main assembly only, confirming the runtime and fixture lookup defect.
- The other skip is independently `Shader unavailable in this compatibility project: HDRP/Lit`.
  HDRP acceptance remains unavailable; fixing URP discovery does not resolve that capability.

### Source correction and observable acceptance

`LightAnimator.InitializeLight` and the existing Light2D fixture now use the two bounded assembly
identities in compatibility order. Existing instance-bound delegates, actual AddComponent/property
access, yielded native Updates, intensity2 animation and intensity3/green restoration assertions remain.
No skip, mandatory URP dependency, manual Update or mock/native-evidence substitution was added.
On a new URP17.3 native run this case must execute rather than repeat the false unavailable skip.

The MeshEmission fixture captures original and cloned **native GetColor baselines before reset**.
Authoring/copy/reset material channels use an explicit absolute **1e-6** tolerance for this normalized
0..1 sample: one millionth of full scale, about 8.4 single-precision epsilons at1 and 1000 times finer
than the XML's three-decimal display unit. This is a declared precision budget, **not a measured error
from the old XML**. Each comparison logs all four actual/expected channels with `R` precision and
checks each numeric channel separately. A magenta clone mutation/negative assertion ensures reset
must actually restore its captured color. Published CurrentColor equals the captured clone baseline
with **zero tolerance**; the surviving original equals its captured native baseline with **zero tolerance**.
Exact original material identity, nonidentity of the owned clone, clone destruction after a native frame
and original survival remain asserted. `MeshEmission.cs` received no change in this continuation.

At the snapshot3 correction handoff, corrected source had **not been compiled or executed**. Its requested native run had to record the
full numeric baseline/error, preserve these strict ownership oracles, run real Light2D animation/reset,
and keep the HDRP-unavailable result separate. A material error exceeding the declared bound still fails;
that handoff made no new pass or rendered/stripped-player capability claim. The later p5 results below
record subsequent root evidence, while the new Image correction remains unexecuted.

Snapshot3 correction source SHA-256:

- `LightAnimator.cs`: `186724c9b3950199fd8fa008271308d0b20c20c9bee419c87f66d7d8dc1dcc3e`.
- `Audit20261023NeoToolsViewLifecycleTests.cs`: `5da3bea0b7b6ca8b194efe7854a5fb08c7c3e53db6ddc7a5a86c5383b88ec8e6`.
- `LightAnimator.md`: `42b07c6ca311636e1ee370575d5a95858dc8c84413eaabba89991c3bdd203a7a`.

## p5 continuation — actual destroyed Image target warning

Read the complete updated implementation contract/request, `p5-source-correction-common.md` and NI71,
the actual p5 XML case output and terminal log21757–21809, current ImageFillAmountAnimator and its
canonical guide, all 17 legacy ImageFill cases, installed UGUI Image setter and DOTween public XML APIs.
No new audit, unrelated TMP/Text change or legacy fixture adaptation was performed.

### Actual prior evidence and provenance

Frozen p5 identity: `956e2aa29ea85e7dea90df498a70392083af4a067058cbcbe24891105966efd1`,
`unity/snapshots/final-20261025T151934Z/source`; baseline ImageFillAmountAnimator.cs SHA-256
`2a30c30eb829d7d752ed747994cdb098bd49619d29e6929499798df05c2354a7`.

Both p5 SDKs report the previous owned matrix as **26 Edit passed; Play23 passed / 1 ignored**.
Thus the preceding Light2D/native-color corrections have later passing component-level evidence;
the remaining ignore is explicitly unavailable `HDRP/Lit`, not a new mandatory dependency or a rendered
HDRP/IL2CPP capability pass. Both also report **17/17 legacy ImageFill cases passed**. These results
belong to the earlier frozen source and do not validate the current Image lifetime correction.

Actual XML pins under `Local/Audits/assembly-2026-10-23/unity/final/p5/`:

| Receipt | SHA-256 |
|---|---|
| `u63/Receipts/20-full-edit-graphics.xml` | `2fc329874dff049e845eebd01635deb1f445359fd951fe82536c79cb2bb7debd` |
| `u63/Receipts/21-full-play-graphics.xml` | `7ae7f99a5f648cf31a86a445bdd59761bfc3390d4bcf9d4d3e1371a6e86d681c` |
| `u66/Receipts/20-full-edit-graphics.xml` | `adeb027f9f14e1c34c8973767f9a6d3420c7c188f809ea26283ebda32d31a74b` |
| `u66/Receipts/21-full-play-graphics.xml` | `73d18c2935644dd92c8fecd6cd1415c5d8f9443b9f0fd3a7afadf2ce9974cc69` |

In u63, the passing legacy `SlicedWidth_Disable_DuringTween_AppliesTargetWidth` XML output contains
two detailed warnings: a destroyed `UnityEngine.UI.Image` accessed by the **FillAmount** captured
setter, `ImageFillAmountAnimator.SetValue:164` → `Image.fillAmount:502` → `Graphic.SetVerticesDirty`
→ native `Behaviour.isActiveAndEnabled`. The terminal stack proceeds through DOTween FloatPlugin /
TweenerCore / Normal Update. This establishes the unguarded captured Image access; it does not establish
which earlier fixture created every orphan or attribute later repeated aggregate summaries to TMP/Text.
`u63/Receipts/20-full-edit-graphics.log` SHA-256:
`c8c40752d7c19dfbb083cf77f1c947d3b8afd89aad58f2bc952fec596e5a1b34`.
Safe Mode logs warnings, so the old passing case alone did not prove absence of this defect.

### Bounded source and regression delivery

The getter and setter now check Unity lifetime, owner enabled state, captured Image identity and exact
current Tween identity **before accessing Image properties**. A rejected callback detaches the owner
field only if it still owns that handle, then kills only that captured active handle. Detachment precedes
Kill callbacks; a stale callback cannot clear/kill a later owned tween. `.SetRecyclable(false)` remains.
No global Kill, manual update, Safe Mode/log/settings mutation, catch-and-ignore or warning suppression
was introduced.

One private nonserialized bound-image reference scopes existing anchor/current/target caches.
Replacement resolution retires the previous tween and resets those captures. SlicedWidth writes use
the admitted Image directly, with another lifetime/identity check before visibility writes following a
rect change. Disable snap and mode restoration apply only to the matching bound target. All public APIs,
serialized fields/defaults and source/doc metadata remain; no scene/sample/vendor or shared tracker edit.

The new **helper-free owned Edit fixture** has six native scenarios: FillAmount/SlicedWidth each with
Image-component-only destruction while its owner remains active, separate external target GameObject
destruction, and reassignment of a still-live Image. Every scenario first observes real unfinished
Image/rect progress plus a second advancing Image/owner/tween. It then requires native frame advancement,
one captured OnKill, zero forced completions, cleared owner handle, live owner and uninterrupted unrelated
progress. Live reassignment additionally requires both old Image and untouched replacement to remain
unchanged until new input. Fresh writes require a distinct active handle and real replacement progress;
disabled snap, bool mapping, no disabled tween, both mode meanings and the replacement's own authored
anchors/offsets/visibility remain asserted.

Each test subscribes to Application.logMessageReceived only after EnterPlayMode, records destroyed-target
or DOTween target/Safe Mode warnings, and asserts an empty capture through owned cleanup. There is no
expected-error whitelist. Each tears down only its own handles/GameObjects, unsubscribes and exits Play
Mode. Ten-second test-owned, linear/unscaled tweens ensure an unfinished native precondition; waits retain
an explicit five-second real-time deadline and require Time.frameCount advancement. No lifecycle callback,
ManualUpdate or private delivery body is invoked.

The existing Play asmdef's explicit DLL list omits DOTween.dll; the existing Edit asmdef already admits
DOTween.dll and UnityEngine.UI. Therefore these new prefixed fixtures use real EnterPlayMode/ExitPlayMode
under Edit, with **no shared asmdef change or pending new reference**. No existing fixture, Mono helper,
fixture assembly, metadata GUID or reservation was moved/extracted. Prior View regression sources remain.

Historical p5 correction pins, before NI77:

| Exact path | SHA-256 |
|---|---|
| `Assets/Neoxider/Scripts/Tools/View/ImageFillAmountAnimator.cs` | `4ffc38a2cdcf0647b199304f431b58a9921f23ec71ff10f7d2ec232fd1ca42e2` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsViewImageFillTests.cs` | `6ad26da358abfe66861255d3751c1fdfa5deca2a07322cb4b95ec976d35bf480` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsViewImageFillTests.cs.meta` | `1bb2133ef0ad5b24ed7bcf872f9a4efc88ba8a95bc8fccfaa9fc1bf6540c88a9` |
| `Assets/Neoxider/Docs/Tools/View/ImageFillAmountAnimator.md` | `7ffadd085bede9dad4a17c0b07534ff2445d471e702c6d3f0e5f8ae0c0d80883` |

Runtime/doc `.meta` files exactly match the frozen p5 source, respectively
`979fe641ebc38e553465397ce0b0dd0f02fbc6d50cc9c70ab05d8b5281526c9b` and
`ed90fa64340c5d1a4345d47bbef9a7964782fda2b97d1cd79873dc5c0dda819e`.

**New native acceptance remains pending.** After all writers stop and root opens a new freeze, compile
both SDKs and run the seven exact native cases (six p5 plus NI77 below), then retain all 17 old ImageFill controls and the previous
View matrix. Observe owned retirement/kill/completion counts, native progress before/after replacement,
zero captured target warnings and no disturbance to the unrelated owner/tween. Root's same native peer
review and focused operator own execution. No execution/process control/install/commit/push/child actor
was started here; source reads, scoped diffs and UTF-8/hash bookkeeping are the actual validation performed.

## NI77 continuation — later immediate write owns visibility

Read the complete updated contract/request, current runtime and full owned fixture, and the actual
p5/u63 UGUI `Graphic.cs` identified in the request. The supplied Root/C NI77 finding is a **source P2**,
not an executed native reproduction. The prior handoff receipt was `10589c0014d6c41995b1e8500eb7d6ba5e2ade2309412886f985dceaecdda748`;
its runtime/fixture pins were `4ffc38a2...` / `6ad26da3...`, recorded in full above.

Vendor path: `Local/Audits/assembly-2026-10-23/unity/final/p5/u63/Library/PackageCache/`
`com.unity.ugui@8ccc29d23a79/Runtime/UGUI/UI/Core/Graphic.cs`. Source342–352 calls SetVerticesDirty
from OnRectTransformDimensionsChange; source293–308 synchronously invokes m_OnDirtyVertsCallback.
Public RegisterDirtyVerticesCallback1072–1075 only subscribes; Unregister1081–1084 removes that listener.
Thus the old ApplyWidth rect setter could enter SAME-owner SetValueImmediate(0), retire the old handle
and write zero/hidden, then re-enable that same Image from the old positive setter tail. Image identity
alone did not prove continued ownership. No native result is inferred from this source chain.

The bounded delta passes the captured Tween into private ApplyWidth and repeats Image lifetime/identity
**and exact current-handle identity** after rect.anchorMax returns, before changing Image.enabled.
The immediate/disabled-snap caller passes no tween and retains its prior path. No public/serialized API,
default, mode-restoration behavior, epoch/framework, global tween operation, settings or log policy changed.
The canonical ImageFill guide documents precedence of this immediate dirty-callback write.

Added exactly one case to the same fixture:
`Neo.Editor.Tests.Audit20261023NeoToolsViewImageFillTests.SlicedWidth_DirtyCallbackImmediateZero_WinsOverRetiredTweenVisibility`.
It enters real Play Mode and proves unfinished positive native Image/rect progress for both the owned
SlicedWidth tween and an unrelated control before registering a one-shot **public** dirty callback.
The callback unregisters itself before calling SAME-owner SetValueImmediate(0), records actual invocation
with the current active owned handle, and captures immediate inactive/null-handle, exact authored-left
anchor, DisplayedValue0 and hidden state. After the callback returns, assertions require exactly one
old OnKill, zero forced completions, inactive old handle, null owner handle, active owner, unchanged
zero/hidden output through at least three later native frames, uninterrupted unrelated handle/progress,
and a distinct fresh tween with real positive output/visibility. Scoped warning assertions remain strict.
The dirty listener is also removed in finally on failure; existing owned teardown remains responsible
for its handles/GameObjects/log subscription and ExitPlayMode. No private callback delivery or ManualUpdate.

All six previous cases/bodies and the 17 unowned legacy oracles were retained; no shared assembly/helper,
other owner source or metadata file was changed. Fixture GUID remains `f381f08c024948a787c38c1a44e9e3e5`.
No dependency API is pending. Source/vendor review, read-only diff and UTF-8/hash bookkeeping are actual
validation; **new compilation and all seven native cases remain pending** until all writers stop and
Root opens a new freeze. Root and the same C reviewer own independent recheck before focused execution.
Prior View/URP evidence and the optional HDRP-unavailable result remain separate and unchanged.

Historical NI77/NI79 source pins, frozen P6 input:

| Exact path | SHA-256 |
|---|---|
| `Assets/Neoxider/Scripts/Tools/View/ImageFillAmountAnimator.cs` | `526a758dd2898e218b728089e46d2f301a82f30361525ef60b3f7e46b8628ea8` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsViewImageFillTests.cs` | `0434169a5424e64fbe3e36e804ddc69bdb0c771ce9b280ffd83c8eaba5f944df` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsViewImageFillTests.cs.meta` | `1bb2133ef0ad5b24ed7bcf872f9a4efc88ba8a95bc8fccfaa9fc1bf6540c88a9` |
| `Assets/Neoxider/Docs/Tools/View/ImageFillAmountAnimator.md` | `c5a93a83fd982920382e2b0e62f024cf4d81f76a898dfef3354c00f61db2ee87` |

## Dependencies, actual work and remaining evidence debt

Consumed ready APIs, without editing their owners: `NeoNetworkComponent.RateLimitCheck(sender)` with
unscaled-double time, `NeoNetworkState.IsAuthorized` with unknown-policy rejection, Mirror connection/spawn
APIs, existing collection primitives and existing test transport. No missing additive API blocks this owner.
No new test assembly reference or folder metadata is required for these existing test directories.

Work performed here consists of source/docs/regression-source edits, complete contract/receipt reads,
source/API/reference enumeration, scoped Git status/diff review and file/GUID/hash bookkeeping through
small cmd.exe/Python reads. No Unity, compiler, tests, product checker/validator, installer, service,
commit/push or child actor was executed by this owner. Reading root's existing XML is historical
evidence review, not new execution. No historical verification result was relabeled for this source.

Root/operator acceptance after the writer barrier: compile supported Unity 6.3/6.6 plus MIRROR on/off,
discover/run the nonempty owned Edit/Play fixtures and existing selector/fill/billboard regressions,
then rebuild and run the unchanged dedicated-server/two-client matrix. Observe exact server and receiver
active/excluded arrays, denial without budget loss, accepted event payload/counts, late join and same-peer
cooldown. Require fresh matching builds because of the RPC payload addition. URP/HDRP rendering/material
lifetime and IL2CPP Light2D stripping remain separate capability evidence. Global package/docs/UTF-8/GUID
checks and the user's required thorough verification precede any root release push.


## Root integration amendment — NI79 native callback admission

Root made a two-line fixture-only strengthening after the original author stopped: capture the current anchor immediately before arming and require the public callback to observe an anchor strictly greater than that captured value. This proves a later positive animated rect change; an unrelated dirty notification with the already-positive old anchor cannot satisfy admission. No actual spurious event is claimed. Runtime and all seven cases/oracles remain; the new fixture pin in the table above supersedes original-author delivery 6f2348890c06181c8c040fe586d7acd0c297c002270c96cb4314ccffd55fb34a. Native execution remains UNEXECUTED; independent NI79 reviews this exact amendment before the new freeze.

## P6 timing delivery — deferred native OnKill observation

Read the complete updated contract/request, actual focused XML for both SDKs, current dirty method and
Root's `Local/Audits/assembly-2026-10-23/p6-view-native-kill-assessment.md`. The pre-edit fixture matched
the Root-integrated frozen pin `0434169a5424e64fbe3e36e804ddc69bdb0c771ce9b280ffd83c8eaba5f944df`;
the pre-edit receipt including the NI79 amendment was `7651486fa3d733763fe8ec97144375bb1cb270db7e8ddc39da29ac9e28545eba`.
The NI79 amendment text and its strict `anchorMax.x > armedAnchor` admission are preserved.

Actual P6 `11-focused-diagnostic.xml` results are **53 = 50 passed / 3 failed in each SDK**.
Within View, all **six ordinary lifecycle cases passed**, all **17 legacy ImageFill cases passed**,
and the dirty-callback case failed at frozen fixture199: expected killCount1, actual0. Both full stacks
point to that exact assertion. Earlier assertions180–198 passed: callback exactly1, newly advanced armed
anchor, immediate retired inactivity/null owner handle, exact zero anchor/display, hidden Image and active
owner. This is evidence of a genuine callback and protected stale-write boundary, not missing Canvas setup.
The remaining stability/fresh-positive assertions were not reached in this case.

| Actual focused receipt | SHA-256 |
|---|---|
| `Local/Audits/assembly-2026-10-23/unity/final/p6/u63/Receipts/11-focused-diagnostic.xml` | `519b3a6dbbb511b796f5f07306c999a207ce1e0983e1abac5ec060d9d8098fa4` |
| `Local/Audits/assembly-2026-10-23/unity/final/p6/u66/Receipts/11-focused-diagnostic.xml` | `f2a3ddb908b80e9d6223bfadfc51a562858f176c1124bab1abc932b9c23f5098` |

The assessment's read-only IL evidence for shipped DOTween DLL `8e90fc43...` explains the timing:
Kill06000085 inside an SDK update sets active=false and returns without Despawn/OnKill. A later native
Update marks that inactive tween for Despawn060002fa, which delivers OnKill. Immediate inactivity is
therefore distinct from synchronous event delivery. This is a confirmed **fixture timing prerequisite**;
no new production bug or runtime correction is claimed, and no IL reader/SDK code was executed here.

Only the dirty method now admits a genuinely later Time.frameCount plus eventual exact OnKill1 before
starting its full three-native-frame stability window. While cleanup is pending, it continues asserting
immediate hard inactivity/null handle, exact zero anchor/display/hidden state, completion0, callback1,
active owner, the unrelated active owned tween and zero captured target warnings. Cleanup and the full
stability window share the same original five-second deadline, without resetting/extending it between
phases. Exact killCount1 remains required before/during stability and after the distinct fresh positive
control. Existing unrelated progress/identity, visibility, cleanup/finally and warning oracles remain.
No count deduplication/relaxation, skip, Canvas, manual update, private call or global flush was added.

Only this existing fixture and this receipt changed. Runtime, canonical guide, metadata, all six other
case bodies/helpers and the 17 legacy oracles were left intact. UTF-8 source outside the dirty method
has identical before/after SHA-256 `623b6f74f4363eeb7c24a73fb5ca6e5d7807d3729511725b7cacad6c9a20e0f7`.

Current source pins after the actual fixture edit; this fixture pin supersedes frozen `0434169a...`:

| Exact path | SHA-256 |
|---|---|
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsViewImageFillTests.cs` | `efdcc3f192d100dc3249b77ca7eb4b2af187f1921974e1de880d2e221e937420` |
| `Assets/Neoxider/Scripts/Tools/View/ImageFillAmountAnimator.cs` — unchanged | `526a758dd2898e218b728089e46d2f301a82f30361525ef60b3f7e46b8628ea8` |
| `Assets/Neoxider/Docs/Tools/View/ImageFillAmountAnimator.md` — unchanged | `c5a93a83fd982920382e2b0e62f024cf4d81f76a898dfef3354c00f61db2ee87` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsViewImageFillTests.cs.meta` — unchanged | `1bb2133ef0ad5b24ed7bcf872f9a4efc88ba8a95bc8fccfaa9fc1bf6540c88a9` |

**SOURCE-FINAL; new timing delivery native-pending.** No all-seven-pass claim is made. Root/same C peer
review precedes a new all-writers-stopped freeze and both-SDK execution of the seven cases plus the 17
legacy controls. Existing actual XML parsing, source review and UTF-8/hash bookkeeping are the validation
performed here. No Unity, compiler, tests, checkers, process control, installers, children or Git operation
was run in this continuation. Prior View/HDRP optional evidence and Root integration history remain.

## Root P8 full-order continuation

Root continuation after original CLI/native reviewer usage limit. Corrected only common six-case post-retirement observation: require a genuinely later native frame and eventual exact OnKill1, retaining original deadline, all inactive/ownership/completion/warning/unrelated states and later fresh/mode assertions. No runtime/guide/meta change. All P8 raw full-order failures remain; new native acceptance pending.

Current fixture: Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsViewImageFillTests.cs; SHA256 78531557b6e8441e22ffd4cca9d52aa93488a0872a042b31f282934344b587bb. Root personally reviewed the complete frozen-to-current delta. Independent new model review was unavailable because the selected subagent usage limit was exhausted; no independent approval or native pass is claimed. Root owns new frozen compilation/targeted/full-order verification.
