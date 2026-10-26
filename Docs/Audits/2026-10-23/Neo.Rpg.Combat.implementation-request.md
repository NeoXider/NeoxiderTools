# Neo.Rpg.Combat: approved implementation request

Original actor: `neo-audit-1023-b05-04`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### RPG-COMBAT-DOC-STATUS (P2; approved-source-correction)

Combat README still claims Timer looping completion broken after Time owner repaired it; distinguish source correction from executed integration and add cooldown-start cancellation regression.

## Relevant full native sections

Source: NativeReview-C.md

### C-33 / P3 — Combat guide still describes the repaired shared timer as broken

`D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/Combat/README.md`, Health Integration limitation paragraph. Current `Tools/Time/Timer.cs:274–306` creates cancellation before start notification, validates cancellation, and delivers OnTimerEnd for completed looping cycles. Original Combat audit's R1/R2 are historical and its plan correctly recognizes source repairs. Update public wording to source fixed/runtime integration pending, then actual results after central execution; do not claim auto-heal works based on source alone. Owner **Neo.Rpg.Combat / `neo-audit-1023-b05-04`**; link canonical topic from parent/legacy entries and retain existing URLs. Folder metadata status is coordinator-owned; historical absence is not reasserted here.
Source: NativeReview-C.md

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

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Rpg.Combat.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Snapshot3 actual RPGCOMBAT-NATIVE-LIFETIME

SAME original source actor, gpt-6.1-sol/max. Root closed execution after all owned Unity jobs stopped. NO compiler/Unity/checkers/tests until ALL writers finish and a new global freeze. Root personally read all98 graphical Edit failure messages/counts and all43 Play groups, independent full-edit-failure-triage.md and full-play-failure-triage.md. Read exact assigned cases in Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (SHA7c6d24e1...) and21-full-play-graphics.xml (SHA2b152164...) plus Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json. PRIMARY b03-04 exclusively owns ALL helper moves/extractions, Fixtures assembly/shared refs and files in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. Check map before every edit. Do not race/extract helpers or change reserved fixture callers; work only disjoint source now, explicitly report deferred reserved work for root to resume after release. Preserve public/serialized API/settings/GUIDs and EVERY substantive positive/negative/count/ownership/payload oracle. Establish a real positive precondition before inferring runtime defect. Native mode failures cannot be hidden with ExecuteAlways, manual Update/private lifecycle as native evidence, blanket Ignore, expected-error whitelist, arbitrary tolerance/default change, reflection UserCode bypass or lost cases. Use supported TOP-LEVEL UnitySetUp/UnityTearDown/UnityTest Enter/ExitPlayMode for true lifecycle, or existing public API for explicit pure contract tests; nested ExitPlayMode is unsupported. No user/foreign scenes/providers/sessions may be reset/deleted; only owned test resources. No other owners/shared asmdefs/vendor314 migration targets, no children/commits/push. Update own canonical docs if actual contract changes and own honest implementation receipt; no test pass inferred from source. Stop writes when done; name unresolved native evidence. Five new Combat HitFilter/Lifecycle failures: illegal Edit SendMessage trigger; Evade completion0; ManagedHitbox disabled pending-end remains true. Establish real top-level admitted native lifecycle and interval/time controls first, then bounded actual runtime fix only if demonstrated. Preserve real positive damage/filter/owner/target, restart-window/cooldown-before-reentry and stale completion suppression. Reserved legacy RpgCombatEdgeTests/helper extraction must remain primary-owned; new Lifecycle/HitFilter cases unreserved.

## Explicit Oct25 recovery of failed S3 attempt

ROOT explicit incomplete-run recovery on2026-10-25: your latest S3 attempt ended nonzero (exit=3), so an older source-final receipt is NOT completion evidence. SAME original session/model6.1sol/max, retain every partial edit. Current CLI jobs succeed again; resume and FINISH the already approved exact scope below. Do not redo initial audit or declare finished from old receipt. Update own receipt with actual S3 failure/correction/current paths and hashes, unresolved native debt, then stop. If API/resource failure repeats, leave exact failed status and no false completion. No tests/Unity/compiler/checkers/new agents/commit/push while writers work. Primary helper extraction is completed; your original caller files are released for your semantics, but Fixtures helper files/shared refs remain primary-exclusive. Original approved bounded scope follows: SAME original source actor, gpt-6.1-sol/max. Root closed execution after all owned Unity jobs stopped. NO compiler/Unity/checkers/tests until ALL writers finish and a new global freeze. Root personally read all98 graphical Edit failure messages/counts and all43 Play groups, independent full-edit-failure-triage.md and full-play-failure-triage.md. Read exact assigned cases in Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (SHA7c6d24e1...) and21-full-play-graphics.xml (SHA2b152164...) plus Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json. PRIMARY b03-04 exclusively owns ALL helper moves/extractions, Fixtures assembly/shared refs and files in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. Check map before every edit. Do not race/extract helpers or change reserved fixture callers; work only disjoint source now, explicitly report deferred reserved work for root to resume after release. Preserve public/serialized API/settings/GUIDs and EVERY substantive positive/negative/count/ownership/payload oracle. Establish a real positive precondition before inferring runtime defect. Native mode failures cannot be hidden with ExecuteAlways, manual Update/private lifecycle as native evidence, blanket Ignore, expected-error whitelist, arbitrary tolerance/default change, reflection UserCode bypass or lost cases. Use supported TOP-LEVEL UnitySetUp/UnityTearDown/UnityTest Enter/ExitPlayMode for true lifecycle, or existing public API for explicit pure contract tests; nested ExitPlayMode is unsupported. No user/foreign scenes/providers/sessions may be reset/deleted; only owned test resources. No other owners/shared asmdefs/vendor314 migration targets, no children/commits/push. Update own canonical docs if actual contract changes and own honest implementation receipt; no test pass inferred from source. Stop writes when done; name unresolved native evidence. Five new Combat HitFilter/Lifecycle failures: illegal Edit SendMessage trigger; Evade completion0; ManagedHitbox disabled pending-end remains true. Establish real top-level admitted native lifecycle and interval/time controls first, then bounded actual runtime fix only if demonstrated. Preserve real positive damage/filter/owner/target, restart-window/cooldown-before-reentry and stale completion suppression. Reserved legacy RpgCombatEdgeTests/helper extraction must remain primary-owned; new Lifecycle/HitFilter cases unreserved.
