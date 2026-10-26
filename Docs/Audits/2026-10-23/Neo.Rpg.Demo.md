# Neo.Rpg.Demo: audit

Status: full source review completed; focused fixes and regression sources added. Unity compilation and test execution are centrally pending.

Optional demo assembly, 49 C# files. Preserve Samples~ layout.

Reviewed `AGENTS.md`, the complete worker contract, the exact `Neo.Rpg.Demo` manifest entry and this report before editing. All 49 files below were read through, including editor-only setup branches. Only owned source, the canonical Examples README, this report and new uniquely prefixed tests/metadata were changed. No nested assembly, shared config, scene, prefab, existing test, package layout or Git history was changed.

Assumption: maintain the optional sample's existing legacy RPG, NoCode and legacy-input workflows; a module migration is a proposal, not part of these compatibility fixes.

## Scope

- `Assets/Neoxider/Samples~/Demo/Scripts/AdventureBlockMarker.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/AttributeTest.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Condition/ConditionDemoSetup.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Condition/ConditionDemoUI.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Condition/HealthTextDisplay.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/Dice/DiceDieView.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/Dice/DiceMergeDemoController.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/Dice/DiceMergeDemoSetup.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/Dice/DiceMergeDemoView.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/GridSystemMatch3BoardView.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/GridSystemMatch3DemoSetup.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/GridSystemMatch3DemoUI.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/GridSystemTicTacToeBoardView.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/GridSystemTicTacToeDemoSetup.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/GridSystemTicTacToeDemoUI.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/ModuleDemoSceneInfo.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Network/TestStart.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/ProgressionDemoUI.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/QuestDemoView.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/RpgCharacterQuickDemo.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/RpgCombatDemoController.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/RpgDemoUI.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/RpgWorldHealthBars.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/SampleDiagnostics.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Shell/AnimatorsDemoController.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Shell/AudioDemoController.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Shell/LevelDemoController.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Shell/NeoDemoShell.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Shell/NetworkDemoController.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Shell/ParallaxDemoController.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Shell/QuestDemoController.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Shell/SaveDemoController.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Shell/SettingsDemoController.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Shell/ShopDemoBootstrap.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Shell/StateMachineDemoController.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorArt.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorConfig.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorEnemy.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorEnemyType.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorGame.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorHud.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorPlayerController.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorUI.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorUpgrade.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorUpgradeKind.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorXpOrb.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/SwipeTextAudio.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/UI/AnimationFlyDemoController.cs`
- `Assets/Neoxider/Samples~/Demo/Scripts/VampireSurvivorDemoController.cs`

## Findings and fixes

The triggers below are source-traced reproductions, not claims of an executed Unity session.

### F1 — High: zero/overflowed Survivor XP thresholds can hang the game

- Source: `Survivor/SurvivorConfig.cs::XpForLevel`; consumer `Survivor/SurvivorGame.cs::AddXp`.
- Trigger: author `BaseXp = 0`, `XpPerLevel = 0`, then collect XP. The old threshold remains zero; `while (_xp >= needed)` subtracts zero and never finishes. Large positive fields can also wrap the old integer multiplication to a negative threshold. `level - 1` overflows for `int.MinValue`.
- Fix: calculate the level offset and multiplication in `long`, clamp the offset to zero, and clamp the returned threshold to `[1, int.MaxValue]`. Valid ordinary curves keep their values and serialized fields stay intact.
- Regression: six Edit Mode cases cover ordinary levels, zero/negative costs, multiplication overflow and `int.MinValue`.

### F2 — High: Dice runtime repair erases authored art and can create an invisible game

- Sources: `GridSystem/Dice/DiceMergeDemoSetup.cs::RepairRuntimeScene`, `LoadDiceSprites`, `LoadCellPrefab`, `LoadDiePrefab`; `DiceMergeDemoView.cs::Configure`, `CreateDieView`.
- Trigger: the shipped scene has serialized `_cellPrefab`, `_diePrefab` and dice sprites. A player build returns null from editor-only loaders. The old `Configure` overwrote those references and accepted an all-null sprite array; `CreateDieView` then returned null. In an imported sample the hardcoded `Assets/Neoxider/Samples/Demo/...` prefab paths also miss the actual location.
- Fix: null art arguments retain authored references; an empty sprite load returns null; editor prefab lookup uses the setup script's own sample root; a missing die prefab uses the existing `DiceDieView` with generated fallback art, a numeric value and a defined scale. An additive four-argument `Initialize` overload requests that value label while preserving the original three-argument API. No sample folder or scene was moved or rewritten.
- Regression: two Edit Mode cases inspect actual tray and placed-die renderers, both with an authored prefab/sprite/scale and with no art supplied. Fallback dice show their value as text. Both place a real `DicePiece` through the controller/board API.

### F3 — Medium: live grid views lose or retain the wrong subscriptions

- Sources: `GridSystemMatch3BoardView.cs::Configure/OnEnable/OnDisable`; `GridSystem/Dice/DiceMergeDemoView.cs::Configure/OnEnable/OnDisable`.
- Trigger: runtime setup adds Match3 view to an active object, so `OnEnable` runs before `Configure` assigns services. The old `Configure` never subscribes. Later rectangular/diamond regeneration changes the field while the view remains stale. Dice did subscribe during configuration but never disconnected previous services; old generators could rebuild the new view. A piece changed while Dice view was disabled was not refreshed on enable.
- Fix: detach before replacing dependencies, subscribe only when active, rebuild the live Match3 view, refresh Dice cells/tray on enable. Disabling Dice also cancels its drag preview and restores tray visibility.
- Regression: Edit Mode generation/rebinding tests for each view verify geometry and prove old generators cannot replace new cell roots; Play Mode test changes a single piece to a pair while the view is disabled and observes two tray renderers after enable. Drag cancellation remains a central interactive check.

### F4 — Medium: grid rebuilds leave cells and leak generated sprites

- Sources: `GridSystemTicTacToeBoardView.cs::ClearAll/CreateSolidSprite`; `GridSystemMatch3BoardView.cs::CreateSolidSprite`; `DiceMergeDemoView.cs::CreateSolidSprite`.
- Trigger: TicTacToe Edit Mode rebuild destroys children during a forward Transform enumeration, skipping shifted children. Both Match3 and TicTacToe allocate a native sprite per cell per rebuild without releasing it. Dice caches its fallback sprite but previously never released it.
- Fix: remove TicTacToe children backwards; cache one sprite per grid view; destroy owned generated sprites on view destruction while preserving `Texture2D.whiteTexture` and authored assets.
- Regression: parameterized Edit Mode tests rebuild both grids repeatedly, assert exact cell/renderer counts, sprite identity reuse, disposal of the owned sprite and survival of Unity's shared white texture. Dice sprite disposal is statically checked and needs central runtime confirmation.

### F5 — Medium: Condition setup amounts change button labels but not actions

- Sources: `Condition/ConditionDemoSetup.cs::_damagePerHit/_healPerClick/_pointsPerClick`; `ConditionDemoUI.cs::DealDamage/HealPlayer/AddScore`.
- Trigger: set setup damage to 7, healing to 3 and points to 11. Labels use those values; the old actions still use 25/10/25.
- Fix: additive serialized UI amount fields preserve the old defaults; public `ConfigureAmounts(int, int, int)` clamps negative arguments; existing setup copies its amounts and marks the UI dirty so values persist when the user saves. Existing serialized names and persistent button callbacks are retained.
- Regression: public actions produce HP 100 → 93 → 96 and score 11; subsequent negative amounts produce no change. The test sets its temporary score record to `int.MaxValue` so it does not save a new best score.

### F6 — Medium: destroyed combat controllers keep handling enemy death

- Source: `RpgCombatDemoController.cs::RegisterDeathListeners/OnDestroy`.
- Trigger: controller adds anonymous enemy-death listeners, then only removes its player listener on destruction. Enemies that outlive it still invoke its captured hide/count action and retain the controller. Duplicate enemy entries also added duplicate listeners.
- Fix: retain one `UnityAction` per enemy, skip duplicates, remove each exact listener on destruction and clear the map.
- Regression: Play Mode test first proves a live controller hides an enemy on the public death event; after destroying the component, the same event leaves that enemy active.

### F7 — Medium: health bars override their assigned camera and outlive their component

- Source: `RpgWorldHealthBars.cs::Awake/ResolveCamera` and view lifecycle.
- Trigger: assign an alternate world camera and leave a different MainCamera in the scene. The old `Awake` overwrites the explicit reference. Disabling the component leaves its last bars visible; destroying just the component leaves its generated canvas/views behind.
- Fix: resolve a camera only when missing; hide pooled views and reset public visible count on disable; destroy only generated canvas/root/views on destruction. Authored external canvases and roots are retained.
- Regression: Play Mode test places the MainCamera far away and the assigned camera over the target, observes one visible bar, disables/enables the component, and checks generated-canvas disposal after component destruction. External-canvas preservation is statically reviewed and remains a central scenario.

### F8 — Medium: an unwired Dice controller throws during Start/Restart

- Source: `GridSystem/Dice/DiceMergeDemoController.cs::RestartDemo`.
- Trigger: add the controller with either service absent. Start calls Restart, then dereferences `_diceBoard.ClearBoard()`.
- Fix: report the unavailable state through existing outputs (`GameOver = true`, no current piece, status text and state event), allowing later configuration and restart.
- Regression: Edit Mode test observes `GameOver` and `CurrentPiece` after the public restart action.

### Review coverage notes

Every file in Scope was reviewed. In addition to the changed paths above:

| Inventory area / files | Reviewed contracts and result |
| --- | --- |
| `AdventureBlockMarker`, `AttributeTest`, `SampleDiagnostics`, `SwipeTextAudio` | Marker has no runtime state. Attribute sample exposes explicit diagnostic buttons; normal sample logging is editor-gated. Swipe view assumes its assigned TMP label. No replacement utility or log pipeline added. |
| `ConditionDemoSetup`, `ConditionDemoUI`, `HealthTextDisplay` | Setup serialization/persistent events, reset/panel actions, Health reactive subscription pairing. Amount bug fixed; existing setup authoring tools retained for compatibility. |
| `DiceDieView`, all three Dice Merge scripts plus setup | Large-value labels, sprite sorting, drag promotion, board mutations, pool progression, orientation search, service wiring, prefab loading and generated resources. Fixes F2/F3/F4/F8. |
| All six Match3/TicTacToe setup/UI/view scripts | Shape masks, blockers, occupied/disabled cells, path endpoints, swap validation, event lifecycle and generated views. Fixes F3/F4; scene-only UI actions still require their documented references. |
| `ModuleDemoSceneInfo`, `NeoDemoShell` | Overlay enable/disable, hint composition, bounded log, UI callbacks, procedural textures, camera/EventSystem fallback and input compilation branches. Wider lifetime ownership is proposed below. |
| `Network/TestStart`, `Shell/NetworkDemoController` | MIRROR guards, ready-client command path, dedicated-server/offline handling and network status UI. No transport, settings or asmdef changes made. |
| `ProgressionDemoUI`, `Shell/LevelDemoController` | XP progress snapshots, LevelComponent initialization order, progression bridge/provider rebind, event cleanup, map/save actions. Runtime curve/session lifetime is covered by proposal P1. |
| `QuestDemoView`, `Shell/QuestDemoController` | Resource/default quest discovery, flow gating, bounded logs, objective counters, event/button cleanup, rewards and restart/reset. Default runtime quest definitions need an ownership/unregister policy before general component-only teardown. |
| `RpgCharacterQuickDemo`, `RpgCombatDemoController`, `RpgDemoUI`, `RpgWorldHealthBars` | Resource templates/IDs, stat upgrades, NoCode forwarding, health-bar pooling/projection, combat event cleanup, restore loops and public buttons. Fixes F6/F7. |
| `Shell/AnimatorsDemoController`, `AudioDemoController`, `ParallaxDemoController` | Existing animator/audio/parallax APIs, data initialization, UI listeners and engine/manager side effects. Generated clip/texture and camera ownership are addressed in proposal P1. |
| `Shell/SaveDemoController`, `SettingsDemoController`, `ShopDemoBootstrap` | Existing persistence calls and keys, settings-event cleanup, wallet/bootstrap/purchase UI. Persistence is intentional in these examples; no save-provider replacement or global reset performed. |
| `Shell/StateMachineDemoController` | Typed state transitions, per-state movement/reset behavior, NoCode state authoring and event cleanup. Runtime-created state definitions/closures require the session ownership design in P1. |
| All eleven `Survivor/*` files | Config/data assets, Abilities registration/auto-cast, pooling and respawn, health ramp, contact damage, paused movement/orb collection, upgrade limits, HUD callbacks, restart/game-over and static art. F1 fixed; pool teardown requires the cross-boundary action below. |
| `UI/AnimationFlyDemoController`, `VampireSurvivorDemoController` | Coordinate spaces, completion/reward timing, procedural assets, death/spawn subscriptions, control restoration and run reset. Existing legacy UI/input contracts remain; lifetime/performance improvements are proposed, not silently migrated. |

Reuse receipt: consulted `neoxider-tools` references for tools, pooling, object cleanup/extensions and Abilities; verified local `FieldGenerator`, `DiceBoardService`, `Health`, `ScoreManager`, `RpgAttackController`, `StateMachineBehaviourBase`, `PoolManager` and `NeoObjectPool` signatures where relevant. Reused existing `DiceDieView` and all rule services. No existing sample-root resolver was found in the owned assembly; the small editor lookup derives its own script root. `Neo.Extensions.SafeDestroy` would require an additional assembly reference; shared asmdef edits are forbidden, so existing Unity destruction APIs remain local.

## Documentation

Expanded the existing canonical `Assets/Neoxider/Docs/Examples/README.md` rather than adding a second entry point. It now documents release/developer/imported sample paths, optional assembly prerequisites, a module/API/workflow map, `ConfigureAmounts`, Dice art fallback/configuration, live view rebinding, Survivor XP guards and the exact regression fixture names. Existing AnomalyGame entry and English style are retained.

## Substantial improvement proposal

### P1 — Explicit ownership for a reusable demo session (proposed; not implemented)

**Problem and evidence.** Controllers build a scene session but ownership is split across scene roots, static art and persistent managers. `NeoDemoShell.Build` creates an unparented canvas and allocates a new texture/sprite on every call (`GetGradient` overwrites the static reference). Destroying a controller does not retire that shell. `AudioDemoController.BuildClips` creates six native clips but `OnDestroy` only unhooks events; its singleton manager can keep demo music alive. Level/quest/state-machine examples create runtime ScriptableObjects with inconsistent teardown. `SurvivorGame.DestroyPoolStorage` deletes named child roots, while the persistent pool manager retains entries. These are concrete lifecycle limitations when composing, reopening or removing demos without unloading the whole application; the focused view fixes above do not implement a general session boundary.

**Architecture / public API.** Extend the existing `NeoDemoShell.Context` with `IDisposable` and an optional owning Transform; keep the old `Build(title, accent)` signature and add `Build(title, accent, owner)`. A context owns its canvas and generated gradient pair and records only resources it created. Add typed resource registration (`Own(UnityEngine.Object)` or explicit clip/config ownership methods) and retained listener cleanup actions; disposal is idempotent. Controllers dispose their context and runtime assets in `OnDestroy`, with an explicit disable/re-enable policy. Keep gameplay rules in their current modules. Keep scene bindings on components; shared ScriptableObject definitions use a runtime context rather than captured renderer callbacks. For singleton resources, request a scoped pool handle from the Spawner owner rather than searching/deleting by name, and use existing Audio APIs to stop only music owned by this demo.

**Compatibility / migration.** Preserve namespaces, serialized component fields, MonoScript GUIDs and existing `Build` callers. Migrate one shell controller at a time using the new overload. Do not destroy externally supplied canvases, cameras, clips, managers or configuration assets. Save/Settings/Shop keep their documented persistence behavior. Quest assets may be disposed only after their registry ownership is detached; coordinate a typed unregister contract with the Quest owner. Pool release is a separate module change, not reflection into private dictionaries.

**Acceptance scenarios.** (1) Open/close or create/destroy each module demo 20 times: generated object/clip/sprite counts return to baseline. (2) Destroy only the component and confirm callbacks/UI/music from its session stop. (3) Two demos share a camera/AM/PoolManager: disposing one leaves the other running. (4) Disable/re-enable refreshes UI without duplicate listeners. (5) Restart Survivor and reload its scene repeatedly with no stale pool entries; shared pools remain usable. (6) Exercise imported and developer roots, both Unity version branches centrally, and legacy/new-input sample policies without changing package settings.

**Effort / risk.** Approximately 3–5 developer days plus central Play Mode/performance verification. Medium risk: singleton/registry lifetimes and callbacks during disposal need clear ownership; pool handles and quest unregister need other assembly owners. Start with shell canvas/gradient disposal and one audio controller, then migrate remaining controllers.

### Cross-boundary coordinator actions

- **Spawner API needed:** `SurvivorGame.DestroyPoolStorage` removes `prefab.name + "_Pool"` GameObjects, but `PoolManager._pools` keeps each `NeoObjectPool` and its template. `PoolManager` exposes Get/Release for instances, no per-template pool removal. Repeated Survivor sessions retain destroyed template keys/pools; equal prefab names also share a storage-root name. Coordinate a scoped `ReleasePool(prefab)`/pool-handle API and migrate Survivor to it. Do not erase a shared root by name as a replacement for registry cleanup. Dependencies were read only.
- **Sample packaging:** `Neo.Rpg.Demo.asmdef` directly references Mirror while network sources have `#if MIRROR` fallbacks. The no-Mirror import/compile claim needs coordinator review; no dependency/config edits made.
- **Input policy:** Grid/Survivor and several EventSystem setup paths use legacy input unconditionally. New-input-only playability is not claimed. A migration must use a common input boundary and the appropriate assembly/package references, coordinated centrally.

## Verification and remaining risks

Actual checks run with read-only Git/`rg` and an inline Python static validator:

| Check | Actual result |
| --- | --- |
| Inventory against nearest asmdef boundaries | Exactly 49 owned C# files; no extra or missing files; no nested asmdef roots in this inventory. |
| Complete source review | All 49 files, 12,725 lines after the focused fixes, including editor-only branches. |
| Strict UTF-8 decoding, no NUL or replacement character | Passed for all 55 scoped source/docs/test/meta files. |
| Brace counts and preprocessor nesting | Passed for 49 owned sources plus two new regression sources. These are lexical checks, not compilation. |
| Existing declared fields and public method signatures | Compared the eleven changed sources to `git show HEAD:<path>`; no removed fields or changed existing public method signatures. |
| Added C# typing and Unity-ID compatibility scan | No new `var`, direct `GetInstanceID()` or `objectReferenceInstanceIDValue` calls. |
| Canonical README links | Both local link targets exist. |
| Test metadata | Two new GUIDs; each occurs in exactly one `.meta` under Assets. |
| New untracked files | Full-file whitespace and final-newline checks passed for both test sources, both metadata files and this report; no pending placeholders remain. |
| `git diff --check -- <all owned source/docs/tests/meta/report paths>` | Passed, exit 0. |

No Unity process, MCP, compiler, build or test runner was started. The 14 Edit Mode cases and three Play Mode cases are regression source additions, not executed passes. Tests discover `Neo.Rpg.Demo` by name and explicitly skip if samples are not compiled; they use public actions and observable state, with reflection only to cross the optional sample boundary and serialized reference wiring matching Inspector authoring. Existing test asmdefs already provide the referenced runtime dependencies; no new asmdef reference was added. The Dice Play Mode case also skips a new-input-only player configuration because that existing sample requires legacy input.

Central verification still required: import the sample, compile Unity 6.0–6.4 and 6.5+ targets, run both new fixtures and existing sample smoke tests, exercise real drag/restart and imported prefab loading, and confirm external-canvas preservation. No new obsolete-as-error stable-ID calls or Unity-version branches were introduced. Native resource/session limitations described in P1 and the Spawner dependency action remain open.

Русское резюме: проверены все 49 исходников; исправлены подтверждённые ошибки XP, Dice, подписок, параметров Condition, камеры и жизненного цикла views. Компиляция и Unity-тесты не запускались по контракту; архитектурное владение demo-сессиями и освобождение пулов вынесены в отдельное предложение/действия координатору.
