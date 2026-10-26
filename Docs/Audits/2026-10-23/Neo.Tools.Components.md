# Neo.Tools.Components: audit

Status: source audit and scoped fixes complete; compilation and runtime verification pending centrally.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/Components/Neo.Tools.Components.asmdef`.
- Scope: **13/13 C# files reviewed completely**, 2,638 baseline lines. Eight source files changed;
  five required no source changes. No nested assembly, shared configuration, existing test, scene,
  prefab, sample layout, or other worker's source was edited. No commits, installs, Unity/MCP,
  builds, test runners, services, or child agents were started.

Проверены все исходники сборки. Ниже отдельно отмечены исправления, написанные регрессии,
фактически выполненные статические проверки и проверки Unity, которые ещё не запускались.

## Scope and review receipt

All paths below are relative to `Assets/Neoxider/Scripts/Tools/Components/`. The exact manifest
inventory was checked against the current directory; it contains no nested assembly sources.

| C# file | Complete review coverage and result |
|---|---|
| `AnimatorParameterDriver.cs` | All trigger/bool/float/int overloads, serialized names, local Animator fallback, null/empty-name handling, Reset and UnityEvent usage. No demonstrated source defect; no runtime log added. |
| `CameraAspectRatioScaler.cs` | All projection formulas, reference capture, viewport mutation, ExecuteAlways lifecycle, preview opt-out, render targets and runtime cache. Fixed target/output and edit-mode lifecycle defects. Shared size/FOV limits and reference-capture behavior remain compatible. |
| `Counter.cs` | All arithmetic and send overloads, rounding, repeat loops, reactive/typed notifications, save/load, registry lifecycle, Mirror Commands/RPCs and initial snapshots. Fixed arithmetic, lifecycle, initial replication and duplicate host notifications. |
| `Interface/InterfaceAttack.cs` | All four independent global contracts and actual signatures. No code change; corrected docs that described a nonexistent `InterfaceAttack` type and parameterless `Attack()`. |
| `Loot.cs` | Null/empty prefab arrays, null entries, chance/count helpers, count inclusivity, horizontal placement, Instantiate ownership and destruction. Automatic destruction drops now require Play Mode; runtime scene teardown policy remains an application decision. |
| `ModifyCounterByKey.cs` | Money precedence, empty keys, registry fallback, local ownership preference, all five operations and divide guards. No source change; documented registry lifetime and direct typed API alternative. |
| `RandomRange.cs` | Integer/float bounds, reactive output, both generated events, Mirror generation/initial state and host guard. Fixed reversed float bounds and per-sender throttling. Documented float-backed integer domain and Float-mode truncation. |
| `ScoreManager.cs` | Initialization, serialized backing fields, five reactive outputs, arithmetic, best persistence/reset, both label paths, threshold order/null handling, target/progress/stars. Fixed derived-output and label inconsistencies. |
| `TextScore.cs` | Explicit/fallback source, late initialization, disable/re-enable, legacy Best flag, subscription ownership and inherited SetText behavior. Reused disposable reactive subscriptions and tracked coroutine handles. Cross-assembly base lifecycle issue is recorded below. |
| `TypewriterEffect.cs` | Every pause/configuration API, tag scanner, progress/audio, both cancellation sources, all callbacks, restart/complete/reset and exception cleanup. Fixed callback re-entry and completion lifecycle. Grapheme/full-TMP parsing remains an explicit proposal. |
| `TypewriterEffectComponent.cs` | Target fallback, authored text capture, startup/re-enable combinations, all public controls, reactive progress, cancellation ownership and retained-effect callbacks. Fixed startup, completion ordering and destruction forwarding. |
| `UnityLifecycleEvents.cs` | Every lifecycle/application relay, null-safe events, optional per-frame emission and delta payloads. No demonstrated source defect; per-frame flags remain opt-in. |
| `UpdateChilds.cs` | Complete child-count callback and nullable UnityEvent path. No demonstrated source defect; no extra update loop or scene search introduced. |

Dependency/context inspection was limited to relevant `Neo.Reactive`, `Neo.Save`, `Neo.Network`,
`Singleton`, coroutine/random extensions, `SetText`, current test asmdefs, existing component/typewriter
tests, and Mirror serialization/network test support. Reuse references read: neoxider-tools skill and
its tools/extensions/network catalogs; unity-game-agent instructions. Existing `Subscribe`,
`CoroutineHandle.Stop`, random helpers and network test support were reused. No parallel runtime
framework or replacement NoCode layer was introduced.

All serialized fields, public overloads and legacy global interfaces are preserved. No runtime log
was added; existing diagnostic calls remain gated through `NeoDiagnostics`. No direct instance-ID API,
UnityEditor import, or new version-specific Unity conditional was introduced.

## Findings and completed fixes

These are **source/control-flow demonstrations with authored regressions**, not claims that Unity
executed the reproductions. Source method names identify the affected locations in the owned inventory.

### Counter arithmetic and registration

- **C01 — Medium:** `SetValue` used `Mathf.Approximately` as the mutation equality guard. At a value of
  1,000,000, adding a representable unit can be discarded by the relative tolerance. It now uses exact
  stored-float equality, matching the reactive property's equality contract. Regression:
  `Counter_AddOneAtOneMillion_UpdatesValueAndNotifiesOnce`, including unchanged-value suppression.
- **C02 — Medium:** `Subtract(int)` negated its argument before conversion to float. `int.MinValue`
  overflows when negated as an int, so subtraction moves in the wrong direction. Conversion now
  precedes negation. The Float-mode regression asserts the observable positive magnitude 2,147,483,648.
- **C03 — Medium:** `OnDisable` looked up the current save key rather than the enabled registration's
  key. Editing that key left a disabled/destroyed counter in the original bucket; empty buckets also
  accumulated. The enabled key is captured, removed on disable, and empty buckets are deleted.
  Regressions assert old-key removal and preservation of another counter sharing the key.

### Counter replication and command throttling

- **C04 — High:** `RpcSetValue` skipped only `isServerOnly`. A host is both server and client, so it
  applied the server value twice, duplicating typed and repeat events even when reactive equality hid
  the duplicate. The RPC now skips every server instance, matching `RandomRange`'s existing guard.
  A new host regression asserts one value event and three repeats for `Set(3)`.
- **C05 — High:** `_syncValue` started at zero and was updated only by mutation commands. A counter
  configured with a nonzero initial value, or subsequently loaded through `Load`, gave new clients a
  stale initial snapshot. `OnStartServer` now captures the current value; server save loads update the
  snapshot and broadcast it. Two regressions exercise Mirror's real initial-state serialization and
  deserialization into a separate receiver and assert configured and loaded values respectively.
  These are snapshot round trips, **not independent remote-client sessions**.
- **C06 — Medium:** `Counter.CmdSetValue` and `RandomRange.CmdGenerate` received a sender but called
  the instance-wide throttle. Two independent clients acting within one interval could suppress each
  other. Both now reuse `NeoNetworkComponent.RateLimitCheck(sender)`. The protected overload exists in
  the current shared workspace; central integration must verify its dependency and separate-client
  behavior. No new component-level multi-connection test was executed or claimed.

### Score consistency

- **S01 — Medium:** changing `TargetScoreValue` updated progress but not stars. With score 50 and
  target changing from 100 to 200, progress became 0.25 while reactive stars stayed at two. The setter
  now recomputes stars; the regression asserts progress, stored stars and the reported star count.
- **S02 — Medium:** `Init` never published the serialized target into `TargetScore`. The numeric target
  could be 240 while reactive consumers read zero. Initialization now publishes it; regression asserts
  both public paths.
- **S03 — Medium:** `ResetBestScore` changed persistence/reactive state but did not refresh configured
  labels. It now refreshes labels after deleting the key; the regression checks text, reactive zero,
  and absence of the unique save key.
- **S04 — Medium:** a null public `starScores` array caused score mutation to throw during derived
  star evaluation. A missing threshold array now awards zero; regression checks score and star outputs.

### Typewriter cancellation, callbacks and wrapper lifecycle

- **T01 — High:** the existing generation guard protected only final cleanup. A text/start/progress
  callback could stop or replace a run while the old run still emitted a character, progress, or final
  completion, especially when the delay rounded to zero. Cancellation is checked after callback
  boundaries and before completion. Startup callbacks now execute inside the cleanup scope, fixing
  leaked typing state when `OnStart` throws. Natural completion releases its source before notifying
  handlers, so `IsTyping` is false during completion. Forced completion returns a captured text value,
  even when a handler starts another run. Six synchronous core regressions cover those distinct cases.
- **T02 — Medium:** `_hasStarted` was set only when Auto Start was enabled, making Play On Enable
  ineffective when Auto Start was off. Playback also happened in `Awake`, contrary to its Start setting,
  and Awake cleared authored text even for manual playback. Awake now captures text without clearing
  it; Start handles the first automatic run once, and subsequent enables replay when configured.
  Regressions cover manual authored text and Auto Start off / Play On Enable on.
- **T03 — High:** wrapper `Complete` fired completion before revealing the label and cancelled its
  token afterward. A completion handler starting the next line saw partial text and then had its new
  line cancelled. The wrapper detaches/cancels the old token, updates the label, then notifies through
  the core. The regression asserts visible full text during completion and a still-active replacement.
  `Clear` also resets reactive progress; its regression checks both state layers and the empty label.
- **T04 — Medium:** anonymous forwarding delegates on the public `Effect` retained the wrapper after
  destruction when another consumer retained the effect. Named forwarders are removed in OnDestroy.
  A regression plays an externally retained effect after wrapper teardown and asserts no forwarding.

### Views, random ranges, camera and loot

- **V01 — Medium:** `TextScore` discarded its pending wait handle and removed listeners using the
  *current* display mode, rather than the acquired subscription. A late source could bind a disabled
  view, or changing the mode could leave the previous source subscribed. The wait is stopped on disable;
  a disposable subscription owns the exact channel; initialization refuses disabled views. Two Edit
  regressions check subscription release/rebinding. A Play regression checks that a disabled late-bound
  view retains its numeric output, then follows the source after re-enable.
- **R01 — Medium:** reversed Float bounds always returned `_min`, while Int bounds were normalized.
  Float generation now normalizes both endpoints. A seeded regression checks interval output,
  nonconstant sampling and all 32 generation notifications; random global state is restored.
- **K01 — Medium:** camera scaling used screen dimensions for render textures and relied on viewport
  rectangles to implement FitBoth. [Unity's Camera.targetTexture reference](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera-targetTexture.html)
  documents that render textures ignore viewport rectangles. The scaler now uses target dimensions;
  FitBoth preserves content through projection expansion when targeting a texture. Regression asserts
  the real camera size and full rect for a portrait render target. Rendering itself is not verified.
- **K02 — Medium:** the runtime cache omitted target texture dimensions and projection mode, leaving
  stale size/FOV until another cached setting changed. Both are now cached. Two regressions change
  target dimensions and orthographic/perspective mode without forcing refresh and inspect the camera.
- **K03 — Medium:** Update In Editor off did not protect `Start` or viewport cleanup in `OnDisable`.
  Both now honor that opt-out in Edit Mode. Two regressions assert authored size/viewport preservation.
  Explicit Apply Camera Scale remains an intentional authoring action.
- **L01 — Medium:** the destruction drop path had no Play Mode gate and could instantiate objects
  during editor cleanup. Automatic destruction drops now require `Application.isPlaying`; the Edit
  regression invokes the lifecycle callback and counts actual clone objects. Explicit `DropLoot()`
  remains available for deliberate authoring. Runtime scene-unload/quit policy is unchanged.

## New regression files

All files use the manifest prefix and have unique `.meta` GUIDs. Existing tests were read-only.

| File | Authored cases | Observable assertions |
|---|---:|---|
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsComponentsNumericTests.cs` | 9 | Numeric results/events, registry entries, target/stars, saved-key deletion and label text |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsComponentsSceneTests.cs` | 8 | Actual camera projection/rect, spawned-object count, reactive listener ownership |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsComponentsTypewriterTests.cs` | 11 | Text, character/completion sequences, progress, idle state, replacement playback and detached forwarding |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoToolsComponentsBindingTests.cs` | 1 | Disabled view stays unchanged; re-enabled view follows 42 then 84 |
| `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoToolsComponentsNetworkTests.cs` | 3 | Host event multiplicity and actual Mirror initial-state serialization round trips |

**32 cases authored; zero executed in this worker.** The three network cases require `MIRROR` in the
test assembly. Existing `Neo.Editor.Tests` already references UniTask and Components; existing Play
references cover the new fixtures. No asmdef/reference change was made.

Reflection in Edit tests is limited to serialized fixture configuration and Unity lifecycle entry
points, which Edit Mode does not reliably drive for ordinary MonoBehaviours. Assertions inspect
public outputs and ownership state. Core typewriter tests call its API directly with zero-duration
delays; the replacement-run wrapper test deliberately suspends and stops its run in cleanup. Play
tests use bounded waits, unique save keys, restore global settings/transport, and destroy owned objects.

## Documentation

Updated only the owned canonical Components entry and relevant existing guides:

- `Assets/Neoxider/Docs/Tools/Components/README.md`: runtime/scene entry points, reactive lifetime,
  links to score/range/key routing and existing camera/child guides; render-target and preview behavior.
- `Counter.md`, `ScoreManager.md`, `TextScore.md`, `RandomRange.md`, `TypewriterEffect.md`, `Loot.md`:
  replaced placeholder summaries with real C# contracts, examples, scene/UnityEvent workflows, lifetime,
  persistence/output behavior and relevant limits.
- `ModifyCounterByKey.md`: direct typed API alternative and live-key registration semantics.
- `Interface/README.md` and `Interface/InterfaceAttack.md`: corrected actual global interfaces/signatures.

English style retained. No `AttackSystem/` topic, root/package README, shared guide, DocsEn file or
existing package GUID was changed. Camera's sibling canonical guide is outside this ownership;
coordinator action to synchronize it is below.

## Substantial improvement proposal

### P1 — Deterministic typewriter session with replaceable tokenization (proposed, not implemented)

**Problem.** `TypewriterEffect` combines timing, cancellation ownership, text parsing, Unity audio and
presentation callbacks. Timing tests require UniTask/Unity loop plumbing. Its scanner treats every
angle-bracket sequence as a tag and counts UTF-16 code units: combining characters, emoji and TMP inline
sprites have no reliable visible-character contract. The completed fixes protect current callback
ownership but do not solve these representation and testing limitations.

**Runtime architecture/public API.** Introduce a small plain C# `TypewriterSession` owning immutable
tokens, cursor, elapsed delay and a `TypewriterPlaybackState` enum. Proposed operations:

```csharp
void Start(string text, TypewriterSettings settings, ITypewriterTokenizer tokenizer);
bool Advance(float deltaSeconds);
TypewriterFrame Complete();
void Stop();
TypewriterFrame Current { get; }
event Action<TypewriterFrame> Changed;
```

`TypewriterFrame` contains visible text/count, total count, progress and playback state. A plain-text
tokenizer uses grapheme boundaries; an optional TMP tokenizer preserves markup and counts inline
glyphs explicitly. Timing accepts caller delta time and does not own an AudioSource, scene reference,
coroutine or cancellation source. The existing TypewriterEffect remains the UniTask driver and event
compatibility adapter; the MonoBehaviour remains a label/audio authoring adapter. This is one focused
playback model with replaceable tokenization, not a manager spanning unrelated systems.

**Compatibility/migration.** Keep current serialized fields, constructors, public events,
`PlayAsync/Stop/Complete/Reset`, wrapper methods and UnityEvents. First introduce the session behind the
adapter with legacy tokenization as the default. Offer grapheme/TMP behavior explicitly before any
major-version default change. Define empty-text/tag-only progress and callback ordering in the new
frame contract; adapt legacy event behavior rather than silently changing it. Existing scenes require
no rewiring; programmer consumers can use the pure session without a scene component.

**Acceptance scenarios.** Ordinary C# tests compare 30/60/120 Hz advancement, punctuation pauses and
large-step catch-up; verify emoji/combining characters and nested tags/inline sprites; prove Stop and
replacement runs emit no stale frames; test forced/natural completion and re-entry consistently;
verify pause/unscaled-time through supplied delta; run current public regressions unchanged through
the compatibility adapter. Unity smoke tests cover label/audio and enable/disable ownership only.

**Effort/risk.** Roughly 3–5 focused developer days plus TMP/Unity verification, depending on supported
TMP tags. Medium compatibility risk around callback order and punctuation timing; higher parser risk
if full TMP grammar is promised. Stage tokenizer support explicitly and keep the legacy adapter path.

### P2 — Server-side counter operations and explicit numeric policy (proposed, not implemented)

**Problem/evidence.** Counter delta/factor operations compute a new absolute value from local client
state and send `CmdSetValue`. Two clients starting from ten can both send eleven for Add(1), losing an
increment. `requiresAuthority = false` also has no per-counter permission policy. Float storage cannot
represent all integers; `RandomRange` rounds float bounds into Int32 and computes `maxI + 1`, making
extreme endpoints unsuitable for a full-Int32 contract. These are separate from the fixed host and
initial-snapshot defects; no atomicity, authorization or full-range numeric guarantee is claimed here.

**Proposed design/API.** Add small pure `IntCounterState` and `FloatCounterState` models with typed
`TryApply(CounterOperation operation, operand, out CounterChange change)` and immutable snapshots.
Choose overflow/nonfinite/rounding policy explicitly. A network adapter sends operations, evaluates
them against server state, applies an optional `CanModify(sender, operation)` policy and publishes one
snapshot revision. Provide an integer-bound range API with an injected random source, independent of
the float-backed scene component. Persistence and UnityEvent adapters stay optional consumers.

**Compatibility/migration.** Preserve the legacy float-backed component fields, current absolute Set,
existing save keys and events. Add exact models and operation dispatch as an opt-in API/backend; retain
legacy permissions by default until the consuming game selects a stricter policy. Introduce tagged
save data before migrating exact integer values. Do not reinterpret already-saved floats as exact
integers or silently change overflow behavior.

**Acceptance scenarios.** Two real remote clients each add one to ten and converge to twelve; verify
multiply/add ordering, denied sender behavior, per-sender throttling, reconnect/late join, Int32 endpoint
sampling and exact values beyond 2^24 in the new integer model. Preserve old scene/save behavior when
the opt-in backend is disabled. This needs genuine multi-client integration, not a host-only assertion.

**Effort/risk.** Approximately 4–7 developer days including numeric/property tests and network
integration. High migration risk if the legacy backend is replaced by default; moderate risk when
introduced as independent models plus an explicit adapter. Coordinate transport/policy contracts with
the Neo.Network owner rather than expanding Counter into a general network manager.

## Verification and remaining risks

### Checks actually performed

- Read-only `git rev-parse --short=8 HEAD`: `ca198556`, matching the contracted baseline.
- Full inventory inspection and a read-only Python manifest/directory comparison: **13/13 exact files**.
- Strict UTF-8 decoding and all **21 project mojibake markers** from ModulePrinciplesTests checked
  against **34 owned source/docs/test/meta/report files** on final verification; no replacement
  characters or markers found.
- Baseline/current textual compatibility checks: **165 existing public/protected declaration lines**
  and **63 serialized field names** preserved. This is a source-level consistency check, not ABI or
  serialization execution proof.
- Static checks across **18 C# files**: no implicit `var` declarations, direct `GetInstanceID` calls,
  runtime UnityEditor imports, unbalanced delimiters or unbalanced conditional directives.
- All **37 relative links** in the **10 modified owned guides** resolve. Five new GUIDs each occur
  exactly once in package metadata. New test metadata is UTF-8 and has no trailing whitespace.
- Scoped `git diff --check -- <exact owned source/docs/tests/meta/report paths>`: final resumed-phase
  run **passed with exit code 0**. Untracked regression files, metadata and report also passed direct
  trailing-whitespace checks. Git's LF/CRLF conversion notices are not errors and were not used as a
  reason to rewrite source text.

The static checks were short Python/shell inspections, not Unity, compilation, NUnit, or a build/test
runner. **Compile/import, Unity console, Edit Mode, Play Mode, render output and both Unity-version
branches remain unverified.** No pass is inferred from the authored tests.

### Central verification and cross-boundary actions

1. Compile the final shared workspace, including `MIRROR` and its fallback, against supported Unity
   6.0–6.4 and 6.5+ configurations. Check discovery of the three conditional network cases in
   `Neo.Tests.Play`; symbol/asmdef changes are coordinator-owned. Run the 28 new Edit cases and four
   Play cases plus relevant existing component/typewriter/network coverage.
2. `Counter` and `RandomRange` now consume the sender-aware protected rate-limit overload in
   `Neo.Network`. Verify the current base implementation and independent-client throttle tests at
   integration. Host and snapshot fixtures do not prove remote transport, atomic updates or permissions.
3. **Neo.Tools.Text owner:** current `SetText` now implements private `OnEnable`/`OnDisable`, but
   TextScore declares its own callbacks. Unity's derived callback hides the base callback, so the base
   pause/resume policy is not composed for TextScore. Coordinate protected virtual hooks plus derived
   base calls, or expose a narrow animation-lifecycle API. Current base `OnDestroy`/SetLink cleanup was
   observed in the concurrent worker's source; do not report its earlier missing cleanup as still open.
   Add a central regression for disabling an already-animating TextScore with animation-on-enable set.
4. Synchronize `Assets/Neoxider/Docs/Tools/CameraAspectRatioScaler.md` with the render-target and editor
   opt-out changes. This sibling guide is outside the manifest's editable docs root; this worker only
   updated the owned Components entry. Verify actual orthographic/perspective render output and limits.
5. Remaining existing limits are documented/proposed rather than disguised as verified fixes: full
   Int32/large-integer precision, nonfinite input policy, server-side atomic counter operations and
   permissions, grapheme/TMP tokenization, and gameplay-vs-scene-teardown loot policy. Runtime loot
   cleanup should use explicit death events when destruction on unload must not create drops.

No shared configuration, package/asmdef or other worker's file was changed to satisfy these actions.
