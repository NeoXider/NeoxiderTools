# Neo.Tests.Play: audit

Status: source audit and scoped static validation complete; Unity compilation/execution await the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef`.
- Scope: all 28 assigned C# files reviewed completely; 22 changed, six unchanged. Nested assemblies excluded.
- Added: three prefixed regression files, 17 cases, one canonical guide and four unique asset metadata files.
- Read completely: AGENTS.md, WORKER-CONTRACT.md, exact manifest entry and original report.

## Scope

- `Assets/Neoxider/Tests/Play/AmTeardownPlayModeTests.cs`
- `Assets/Neoxider/Tests/Play/Animations/TransformAnimatorCombinedPlayModeTests.cs`
- `Assets/Neoxider/Tests/Play/Bonus/BonusPlayModeTests.cs`
- `Assets/Neoxider/Tests/Play/Core/InputPlayModeTests.cs`
- `Assets/Neoxider/Tests/Play/Dialogue/DialoguePlayModeTests.cs`
- `Assets/Neoxider/Tests/Play/Network/InteractiveObjectNetworkTests.cs`
- `Assets/Neoxider/Tests/Play/Network/MoneyNetworkTests.cs`
- `Assets/Neoxider/Tests/Play/Network/MultSceneNetworkTests.cs`
- `Assets/Neoxider/Tests/Play/Network/NeoNetworkBootstrapTests.cs`
- `Assets/Neoxider/Tests/Play/Network/NeoNetworkManagerScenePlayerTests.cs`
- `Assets/Neoxider/Tests/Play/Network/NeoNetworkManagerSessionTests.cs`
- `Assets/Neoxider/Tests/Play/Network/NetworkActionRelayTests.cs`
- `Assets/Neoxider/Tests/Play/Network/NetworkContextActionRelayTests.cs`
- `Assets/Neoxider/Tests/Play/Network/NetworkEventDispatcherTests.cs`
- `Assets/Neoxider/Tests/Play/Network/NetworkOwnerFilterTests.cs`
- `Assets/Neoxider/Tests/Play/Network/NetworkSingletonTests.cs`
- `Assets/Neoxider/Tests/Play/Network/NetworkTestHelper.cs`
- `Assets/Neoxider/Tests/Play/Network/NetworkToolsTests.cs`
- `Assets/Neoxider/Tests/Play/Network/PhysicsEventsNetworkTests.cs`
- `Assets/Neoxider/Tests/Play/Network/RpgCharacterNetworkAdapterTests.cs`
- `Assets/Neoxider/Tests/Play/RPG/InventoryPlayModeTests.cs`
- `Assets/Neoxider/Tests/Play/RPG/RpgCombatPlayModeTests.cs`
- `Assets/Neoxider/Tests/Play/RPG/RpgCombatTargetingPlayModeTests.cs`
- `Assets/Neoxider/Tests/Play/SampleRuntimeSceneSmokeTests.cs`
- `Assets/Neoxider/Tests/Play/ShopPurchasePlayModeTests.cs`
- `Assets/Neoxider/Tests/Play/Tools/InteractiveObjectPlayTests.cs`
- `Assets/Neoxider/Tests/Play/UI/AnimationFlyPlayModeTests.cs`
- `Assets/Neoxider/Tests/Play/UI/UIMeshRigPlayModeTests.cs`

## Findings and fixes

Review covered setup/teardown (including failed assertions), lifecycle timing, native ownership,
global state, null/boundary inputs, serialization/reflection seams, logging, dependencies, version
guards and what assertions actually prove. Evidence below is source control/data flow; no Unity
reproduction or test execution was performed.

### Complete review ledger

All filenames below refer to the full paths in Scope.

| File | Result |
| --- | --- |
| AmTeardownPlayModeTests | Track hosts/clips through interrupted tests; preserve teardown-only log capture. F7. |
| TransformAnimatorCombinedPlayModeTests | Reviewed paused pose, channel contribution, evaluator seam and finally cleanup; no demonstrated defect. |
| BonusPlayModeTests | Isolate cooldown saves, own roots/assets, restore time scale and bound scaled-clock loops. F1/F3/F7. |
| InputPlayModeTests | Reviewed creation/cleanup; creation-only assertion leaves device/chord behavior uncovered. |
| DialoguePlayModeTests | Reviewed Start timing, indices, end events, disable/enable and optional events; no demonstrated defect. |
| InteractiveObjectNetworkTests | Bound host startup, destroy original player template; reviewed authority and exact event counts. F3/F7. |
| MoneyNetworkTests | Isolate persistence before initialization, remove persistent key deletion, restore provider after cleanup. F1/F3/F7. |
| MultSceneNetworkTests | Bound both scene loads and host/player waits; reviewed skips, context actions and searches. F3. |
| NeoNetworkBootstrapTests | Fail timed-out waits; reviewed roles, ports, global settings, ownership and expected logs. F3. |
| NeoNetworkManagerScenePlayerTests | Bound four startup waits; reviewed cloning, sceneId, independent counters and pickup scope. F3. |
| NeoNetworkManagerSessionTests | Fail timed-out waits; reviewed handshake modes, event ordering, restart handlers and payload equality. F3. |
| NetworkActionRelayTests | Bound startup/release template; reviewed scope, payload, name lookup and invalid index behavior. F3/F7. |
| NetworkContextActionRelayTests | Bound host and relay-spawn waits; reviewed local filtering and clone isolation. F3. |
| NetworkEventDispatcherTests | Bound startup/release template; reviewed once-only host events and offline fallback. F3/F7. |
| NetworkOwnerFilterTests | Bound startup/release template; reviewed server/everyone outcomes and event polarity. F3/F7. |
| NetworkSingletonTests | Bound startup/release template; reviewed singleton lifecycle, authority, SyncVar state and dummy transport limits. F3/F7. |
| NetworkTestHelper | Retain transport/assetId setup; add validated assertion-producing real-time wait and host wrapper. F3. |
| NetworkToolsTests | Bound startup/release template; reviewed Counter, Selector, condition, random and reactive outputs. F3/F7. |
| PhysicsEventsNetworkTests | Bound startup/release template; reviewed server/local routing and explicit callback seam. F3/F7. |
| RpgCharacterNetworkAdapterTests | Reviewed existing bounded startup, damage count, limiter, snapshot IDs and cleanup; no additional demonstrated defect. |
| InventoryPlayModeTests | Remove global save wipes/private singleton reset; isolate provider and restore exact instance. F1. |
| RpgCombatPlayModeTests | Own generated templates/projectile clones; real-time bounds; reviewed direct/projectile HP results. F2/F3/F7. |
| RpgCombatTargetingPlayModeTests | Assert actual HP deductions, bound observed attacks, clean up after failure. F6/F7. |
| SampleRuntimeSceneSmokeTests | Prefer visible sample roots, bound scene loading; reviewed reflection and gameplay/UI assertions. F3/F4. |
| ShopPurchasePlayModeTests | Correct provider lifetime/float culture; reviewed typed/legacy APIs, prices, bundles, grants and UI controls. F1/F5. |
| InteractiveObjectPlayTests | Scope object ownership, preserve borrowed EventSystems, remove legacy input module, select fixture camera. F2. |
| AnimationFlyPlayModeTests | Reviewed coordinates, rewards, pooling, timing, deadline and tracked cleanup; no demonstrated defect. |
| UIMeshRigPlayModeTests | Reviewed native assets, masks, raycasts, Animator seam, time scale and reverse cleanup; no demonstrated defect. |

### Completed corrections

**F1 — P1, destructive persistence/global restoration.** Inventory SetUp/TearDown called
`SaveProvider.DeleteAll()` on the configured provider. Money tests performed persisted wallet changes
and deleted real `Money`/`MoneyAllMoney` PlayerPrefs keys. Bonus cooldown tests used ambient storage.
Shop teardown installed a new PlayerPrefs provider before destroying components. These paths could
erase unrelated profiles, modify existing balances, route destruction callbacks into real storage,
and replace a caller's configured provider. All four fixtures now install the existing in-memory test
provider before component creation and restore the exact previous provider in finally after cleanup.
Removed persistent key deletion and Inventory's unconditional private singleton reset: the verified
base OnDestroy already clears its own instance. Regression cases preserve an ambient sentinel and
prove that Shop OnDisable writes stay isolated while the original provider is restored by identity.

**F2 — P1, destruction/activation of unrelated scene objects.** Interactive SetUp destroyed all
EventSystems and teardown destroyed all active GameObjects. RPG combat activated/destroyed every
discovered projectile. These operations could damage another scene's objects and input infrastructure.
Interactive now tracks its 22 allocation sites, temporarily suspends/restores borrowed EventSystems
and current selection, and selects its own camera. It invokes pointer handlers directly, so the
unnecessary StandaloneInputModule and its legacy-input dependency were removed. RPG activation/cleanup
recognizes only clones of its uniquely named template. Regressions retain active/inactive sentinel
roots, the original EventSystem and an unrelated inactive projectile, while releasing owned resources.

**F3 — P2, hangs and silent timeout success.** Sixteen host/player waits, two relay-spawn waits and
scene waits lacked a deadline. Bootstrap/session WaitUntil helpers silently completed on expiry;
for example a manual-handshake assertion about an absent player could still pass without readiness.
Three Bonus and two RPG loops relied on scaled time to expire. NetworkTestHelper now validates inputs,
uses `Time.realtimeSinceStartupAsDouble` and fails with operation/server/client/player diagnostics.
All 16 startup sites reuse its host wrapper; prior helpers delegate to the assertion-producing wait.
Scene loads have ten-second real-time deadlines; other affected bounds use real/unscaled time.
New cases cover immediate readiness, readiness transition, named timeout failure and expiry while paused.

**F4 — P2, imported sample coverage skipped.** GetActiveSampleDemoRoot returned Samples~ before checking
imported roots. A normal release package plus imported Demo Scenes therefore still triggered the
hidden-script skip. Discovery now stores hidden roots as fallback and continues looking for visible
development/imported samples. An internal filesystem seam permits five regressions: coexistence,
development priority, hidden-only fallback, legacy imported names and missing roots. No sample asset,
layout or scene was changed.

**F5 — P2, locale-dependent test save values.** Shop's MemorySaveProvider wrote invariant float text
but parsed using current culture. Fractional values could return defaults or change interpretation in
comma-decimal locales. Parsing is now invariant too. Exact round trips under fr-FR and ru-RU restore
the caller's culture afterwards. The existing provider is reused internally within this test assembly.

**F6 — P2, HP assertion that could never fail.** The targeting fixture compared CurrentHp to a
StartingDamage getter that returned the same current HP. It now captures initial HP and checks exact
first/second deductions through SetTargetReceiver and real Updates. Disabling from each observed
attack callback prevents extra-frame hits before explicitly enabling the second observation. This
strengthens the existing regression without changing runtime combat code.

**F7 — P2, native/test resource leaks.** Audio-created clips and RPG character templates were not
released. Bonus/targeting resources survived failed assertions. Eight network fixtures stored the
original player template only in a setup local; host shutdown owns spawned players, not that original.
Fixtures now track/release those objects and assets, including interrupted setup. Regressions observe
destruction of three created audio clips and two character templates, preserve unrelated assets, and
run a host fixture through teardown to verify manager/original-template destruction.

No runtime package API, serialized gameplay field, scene, prefab, shared configuration or another
worker's source/docs/tests was changed. Read-only dependency inspection established SaveProvider,
singleton, camera, network-template adoption and RPG contracts used by these fixes.

## Documentation

The allocated canonical guide did not exist at intake. Added
[Play Mode testing](../../../Assets/Neoxider/Docs/PlayModeTesting.md), covering typed API assertions,
scene/UnityEvent workflows, persistence/resource ownership, Mirror prerequisites, sample layouts,
timeouts and the limits of host/callback/visual coverage. Coordinator should link it from the shared
documentation index; the index is excluded from this worker's ownership.

New regression files in Assets/Neoxider/Tests/Play (each has unique GUID metadata):

- Audit20261023NeoTestsPlayFixtureIsolationTests.cs: seven cases.
- Audit20261023NeoTestsPlayNetworkWaitTests.cs: five cases.
- Audit20261023NeoTestsPlaySampleRootTests.cs: five cases.

New guide metadata also has a unique GUID. All three new C# files were reviewed in addition to the
28-file inventory. The 17 cases are added, not executed.

## Substantial improvement proposal

**Problem:** the assembly repeats transport/player setup, provider selection, global setting changes,
singleton cleanup and frame polling. This audit found several independent omissions. It also directly
references Mirror and Neo.Rpg.Network, so selecting an offline test does not establish compatibility
without optional networking dependencies. Creation-only input and direct physics callback tests can
hide gaps behind a broad green PlayMode result.

**Proposed architecture/public C# surface, not implemented:** introduce a small opt-in test-support
assembly gated by UNITY_INCLUDE_TESTS. PlayModeTestScope exposes Own<T>(T), UseSaveProvider(ISaveProvider),
UseTimeScale(float), WaitFor(Func<bool>, TimeSpan, string) and coroutine Cleanup(). It registers explicit
objects only, destroys owned roots/components before native assets, then restores borrowed providers
and settings in finally. A synchronous disposal fallback restores state if coroutine cleanup is
interrupted. Borrowed EventSystems/cameras are optional explicit adapters rather than global purges.

MirrorHostTestScope remains an optional adapter owning its manager, transport, original player template
and handler registrations. It provides bounded shutdown plus failure cleanup, and refuses to take over
an unrelated running session. Offline fixtures reference only common support; Mirror fixtures live in
their own opt-in test assembly. A sample resolver accepts an explicit imported version/root instead
of selecting an arbitrary installed version.

Expose coverage categories for offline APIs, host loopback, real transport, scene samples and device
input. Input needs a backend-specific driver and observable chord events; physics needs actual collider
motion and event counts. A separate real peer must exercise late join, restart, owner rejection and
duplicate delivery before reporting remote synchronization coverage.

**Compatibility/migration:** preserve gameplay APIs, serialized assets and existing NUnit names.
Adopt the support scope in small fixture groups first. Split assembly files only as a coordinated
change because references and consumer test filters need review. Keep development/release/imported
sample layouts and explicit missing-sample skips. Shared asmdef/package/settings changes are outside
this audit and have not been made.

**Acceptance:** partial setup or assertion failure releases manager/template/native assets; destruction
callback saves remain isolated; paused time cannot stall shutdown; repeated cleanup preserves borrowed
objects; two consecutive runs restore identical provider/settings/owned-object counts; imported samples
work beside hidden release samples; offline tests compile without Mirror; real-transport and host-only
results are separate. Compile/check Unity 6.0–6.4 and 6.5+ centrally before claiming compatibility.

**Effort/risk:** approximately three to five engineering days for support contracts, staged fixture
migration, assembly separation and central lifecycle validation; real-peer/device drivers are a separate
follow-up. Risk is medium because deferred destruction, singleton ownership, cleanup order and Mirror
static handlers can affect unrelated fixtures. Retain before/after run artifacts and migrate by domain.

## Verification and remaining risks

Executed checks:

- All 28 manifest C# files read completely; final scoped source diff reviewed; 22 existing files changed.
- Scoped git diff --check over owned paths: exit 0.
- Lexical delimiter and #if/#endif balance: 31 C# files, no issues. This is not C# compilation.
- Strict UTF-8 decoding/replacement-character scan: 37 owned files, no issues.
- Added-line/new-file whitespace and added C# var scan: no issues. An initial whole-file whitespace
  scan flagged two inherited comment endings in InteractiveObjectPlayTests; baseline ca198556 contains
  the same two endings, so unrelated comments were preserved.
- No remaining unbounded host-loop pattern, SaveProvider.DeleteAll or PlayerPrefs.DeleteKey in the
  assigned inventory/new regressions.
- Each of four new metadata GUIDs occurs exactly once under Assets.
- Assigned asmdef diff is empty; changes require no additional assembly reference.

Not executed: Unity import/editor, C# compiler, NUnit/PlayMode runners, builds, live console, screenshots
or remote-client/device checks. The user/worker contract reserves Unity and runners for the coordinator.
No execution pass is claimed for the new cases.

Residual limits: several existing animation/cooldown/target-finding assertions still depend on frame
timing and need central execution on a slow worker. Scene smoke replaces the active scene; network
scene cleanup assumes an isolated test environment. Dummy transport cannot validate remote peers.
Private-field/assetId reflection depends on installed versions. Multiple imported sample versions are
not ranked semantically. These require the proposal/coordinated follow-up rather than an unreviewed
cross-assembly change.

Coordinator actions: link the new guide from the shared index; compile and execute affected PlayMode
fixtures, including all three new prefixed files, and inspect actual failures/logs. Run scene smoke with
visible development/imported samples. Verify both Unity compatibility ranges and decide separately on
optional-network test assembly separation. No confirmed runtime-module defect needs another owner to
change runtime source. No commits, installations, runners or child agents were used.

Русское резюме: полностью проверены 28 файлов. Исправлены разрушительная очистка данных и чужих
объектов, зависания ожиданий, пропуск импортированных samples, проверка HP без реального сравнения
и утечки ресурсов. Здесь выполнена статическая проверка; запуск Unity и тестов остаётся координатору.
