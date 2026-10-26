# Native implementation review 20 — Save, Progression and GridSystem

Status: **SOURCE REVIEW COMPLETE; two root-accepted bounded residuals remain**: NI20-02/P1 failed precommit Progression initialization, and NI20-01/P2 nested first Grid occupancy publication. Save C21 and the other assigned corrections are supported in source. No compiler/import/discovery/test/native/performance result is claimed.

Reviewer: SAME `/root/audit_abilities`, native `gpt-6.1-sol` / `max`, repository `D:/Git/NeoxiderTools`, target `demo` / `11.0.0`. Only this Markdown is written. No product/public-doc/test/other-report/config edit, Unity/MCP, compiler/build/test/checker/validator, installer, service/process management, child, commit or push. The global source/execution barrier remains closed.

## Admission, coverage and identity gate

Read all three complete latest SOURCE-FINAL receipts first, then all three complete approved requests. Initial receipt/request identities were captured at **2026-10-23T20:50:32.090021+00:00**. Independently read the exact CLI metadata for Save `neo-audit-1023-b05-07`, Progression `neo-audit-1023-b04-07` and Grid `neo-audit-1023-b03-05`: all `state=done`, `exit=0`. Root's recovery dispatch also skipped them as finished. The shared implementation-state tracker still had old source-running rows; this report uses the final receipts/exact metadata, not that stale generic label. No process was queried, managed or launched for validation.

Scoped inventory from the assignment artifacts is **20 Save + 13 Progression + 30 Grid C# files**, excluding nested assembly ownership. Main raw identity capture at **2026-10-23T20:53:53.870326+00:00** covers **105 unique paths**: those inventories, module asmdefs, phase receipts/requests/docs/tests/metadata, original artifacts/prior native reports and exact owner metadata. The post-read gate at **2026-10-23T21:06:22.681232+00:00** matched **105/105**, plus **7/7** then-current provider/shared-definition contexts. Final handoff at **2026-10-23T21:18:55.287936+00:00** again matched all **105/105** scoped identities and six unchanged contexts. Only root's shared Play definition changed for its separately announced Move integration; that definition was fully reread and its final hash replaces the context row below. No scoped module/receipt/owner metadata changed. A later source/owner-state change requires renewed scoped review.

Coverage is deliberately bounded to the final approved phases:

- **Save:** complete current SaveProviderSettings, SaveProviderSettingsComponent and SaveProvider; complete existing encryption validator/config; entire new configuration fixture; all four changed guides; FileSaveProvider constructor/load-path context. Prior NativeC C21/disposition/fingerprints were read. Six recorded prior artifacts exactly match: original audit/plan, FileSaveProvider, SaveManager and both older Save fixtures; only the fingerprinted facade changes and was reread fully.
- **Progression:** complete current 1059-line ProgressionManager, complete 831-line/37-scenario fixture, full manager diff, all three changed guides and relevant Core/Reactive context. Prior NativeA A21/A22/disposition was read. Its old 13-file aggregate changes, as expected; it cannot identify which individual files were unchanged. No false per-file equality claim is inferred from that aggregate.
- **Grid:** full current placement service, merge request/resolver and object-spawner bodies; all generator phase hunks plus relevant generation/footprint/bounds/shape/Gizmo bodies; all five changed source diffs; both complete new fixtures/metadata and four changed guides. Read prior NativeB B05–B07/disposition and relevant existing disabled-seed/generated-shape controls. Dice/Generic Merge are provider context, not newly approved nested implementations.

Original audit/plan identities match their prior native artifact fingerprints. Earlier full NativeA/B/C source coverage is retained where applicable; this is not a full unrelated module re-audit. Raw inventory hashes below include files not reread beyond the coverage just stated and establish identity, not a claim to re-inspect every unchanged body. No optional framework/whole-module redesign is required or credited.

## Accepted remaining corrections

## Progression/Save integration residual — NI20-02 / P1 (root accepted)

Owner: SAME Progression worker `neo-audit-1023-b04-07`. Root independently read the exact Init/ApplyProfile/Ensure/Bind-Unbind bodies and accepted this P1 precommit initialization/data-loss residual during NI20. Current `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Components/ProgressionManager.cs:679` sets `_progressionInitialized = true` before `BindLevelProvider` (line686) and profile reading (LoadProfileInternal / SaveProvider.GetString, line701), without resetting readiness when dependency initialization/read throws. Save's approved invalid-enabled-Resources-encryption path now intentionally throws at exactly this boundary; Core EnsureInitialized itself already clears its own readiness on a failed attempt.

Concrete public retry reproduction: a manager with profile loading enabled and saved points7; its first profile read throws (real invalid Resources encryption, or a one-shot failing ISaveProvider.GetString control). The first EnsureInitialized throws. Correct the dependency and call EnsureInitialized again: it returns because the manager flag stayed true, leaving the saved profile unapplied at0; AddPerkPoints(1) with normal AutoSave then saves1 instead of first loading7 and saving8. With an assigned Core provider, a failure before binding can also leave mirror/reward subscriptions absent, while a profile-read failure after binding leaves handles owned by an unsuccessful attempt. This is an explicit public corrected-dependency retry, not an automatic scene-cache-refresh or arbitrary input-policy demand.

Expected bounded repair: readiness represents successful initialization, with an in-progress guard retained for reentry; actual unfinished setup/read attempts clean only their own subscriptions and permit corrected dependency retry without duplicate listeners. Distinguish failure before profile commit from an external observer exception after commit: do not reset/replay committed initialization/rewards/watermark or erase a newer reentrant binding. Preserve profile-key/reset rules and normal binding counts. A small attempted-initialization boundary is sufficient; no session/transaction framework or arbitrary reward rollback is required. Add genuine fail-on-read/corrected-retry controls, assigned-provider cleanup/rebind, completed observer-error and newer-binding controls, plus normal positives. This is source-traced and unexecuted; no runtime source is edited here.

## Confirmed Grid residual — NI20-01 / P2

Owner: SAME Grid worker `neo-audit-1023-b03-05`. Root confirmed and accepted this bounded B07 follow-up during NI20; SAME owner resumes after root reads the full report. Current `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldObjectSpawner.cs:125`, `SpawnAt`, requires no other registered blocking object after `OnObjectSpawned` before publishing occupancy. Registration happens before that callback. The original count-of-blockers test had the same hole: this is an uncovered existing residual in the touched lifecycle contract, not a claim that the new guard introduced it.

Concrete source reproduction: an initially free current cell; one-shot OnObjectSpawned listener calls `SpawnAt` for a second blocking object in the same cell. Nested B sees registered A and skips SetOccupied; outer A sees registered B and also skips. Two blocking objects remain in GetObjectsInCell/GetAllObjects, while FieldCell.IsOccupied remains false. IsCellOccupied reports true only via its registry fallback, and normal RequireUnoccupied placement can incorrectly accept the cell. No external metadata writes, regeneration, exotic input or transaction framework is involved. Documentation explicitly permits multiple objects per cell and reserves occupancy ownership to this spawner.

Expected: settled current-cell occupancy true with one first-occupancy transition; removing either first must keep it occupied until the last blocker. Smallest repair: distinguish registered in-flight blockers from already published occupancy when deciding the first write, retaining exact registration/current-generation checks after callbacks and stale-event suppression. Keep ordinary first/last blocker order and all regeneration/removal controls. The seven authored lifecycle cases cover sequential blockers but omit nested first publication. Add a one-shot nested SpawnAt regression with real current-cell flag, placement rejection, registry/event counts and both removal orders. This is source-confirmed, not executed; no source was changed by the reviewer.

## Save — C21 encryption preflight supported

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/Settings/SaveProviderSettings.cs` adds the narrow public `bool TryCreateProvider(out ISaveProvider provider, out string error)`. It sets null outputs, selects the File path, validates **enabled** encryption with the existing FileSaveEncryptionConfig.TryCreate, and returns false before constructing FileSaveProvider on an incomplete/invalid pair. Error text describes configuration and contains no key/IV values. Existing CreateProvider delegates and throws InvalidOperationException on the same failure. PlayerPrefs, disabled encryption, built-in empty-pair defaults and valid custom encryption retain their paths; unknown-provider fallback remains existing compatibility behavior.

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/SaveProvider.cs`, Initialize, constructs settings/provider candidates locally before publishing settings/provider, forwarding and lifecycle hooks, then initialized readiness. A failed lazy config attempt therefore remains retryable. It does not open a plaintext File provider first. SettingsComponent validates before SetProvider and returns on failure, preserving active-provider pending values/event forwarding. An unset component consumes the existing normal CurrentProvider selection rather than secretly forcing a new default.

The previous FileSaveProvider/SaveManager fixes retain exact NativeC identities; their durability/recovery semantics are not silently expanded by the new factory. Successful File construction loads existing storage; a correctly sized wrong key, later corruption, file replacement durability, arbitrary provider/observer failure and GlobalSave cache generations remain existing limits. TryCreateProvider is encryption-config validation, not an all-exception or write-success result API.

**Ten authored new cases / five methods, all read and UNEXECUTED**, in `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveConfigurationTests.cs`:

- Four invalid pair/size cases reject both factories, null the result and exclude supplied secrets from errors. Existing encrypted primary, backup and staging bytes stay identical. The staging marker would be removed by provider Load, so it discriminates pre-construction rejection. Correction then loads20, saves30 encrypted and rotates expected backup bytes.
- Three compatible configurations prove disabled invalid strings are unused, empty enabled defaults work and valid custom encryption round-trips. Actual AES decrypt/plain JSON controls are separate from merely checking a prefix.
- PlayerPrefs ignores unused file-encryption fields without writing preferences or creating a file.
- The owned inactive scene adapter's real Awake method is dispatched deterministically: failure preserves provider, pending values and event transfer; correction detaches the old source, forwards the new source once and persists the encrypted result. This is callback logic source, not native Awake ordering evidence.
- The actual GUID-owned Resources asset path tests two public CurrentProvider failures and corrected public lazy retry, with byte conservation and stable successful provider identity. Reflection only arranges unfinished facade state while preserving listeners/hooks.

Fixture teardown retains the old provider until owned object cleanup, restores exact provider/settings/logging, removes only its listener/asset folder and GUID temporary files, and never clears global event lists/SaveManager registration. It requires the exclusive isolated host with no competing Resources asset. Actual import, cleanup failure, native Awake/sceneLoaded/focus/domain reload, filesystem fallback and IL2CPP encryption remain unexecuted. The new metadata was read individually; global GUID/package validation was not run.

All four guides match this fail-closed/retry contract, deliberate 11.0 exception behavior, typed recoverable activation, once-only scene initialization and non-atomic provider replacement limits. C# and Inspector workflows remain available without a new interface/scene service. **No additional Save source defect found in this phase.** NI20-02 is the Progression consumer's unfinished-initialization response to this deliberate throwing boundary, not a reason to restore plaintext fallback.

## Progression — A21/A22 supported except unfinished initialization

**A21 / active profile identity:** LoadProfileInternal captures the actual selected key before reading. ApplyProfile compares it to the last loaded/reset active key and preserves max(previous rewarded level, incoming Core level) only for that same active key. Different loaded keys establish an incoming baseline; explicit ResetProgression starts fresh. Assigning SaveKey or saving under another key does not itself change active history. V2 JSON/public signatures and Core-first account restoration remain intact. No durable reward claim identity or cross-provider transaction is claimed.

**A22 / mirror ownership:** BindLevelProvider detaches the exact previous level listener and owns three IDisposable subscriptions. Replacement, null clearing, failed subscription setup and manager destruction release those handles. Binding versions reject old captured tails after a swap. Existing Reactive.Subscribe defaults to non-immediate, so sequential handle acquisition does not run an unexpected immediate observer between assignments. Core publishes complete XP/level/remaining values before channel callbacks and restores load/reset state without level reward replay; direct XP/load/decrease/reset can therefore refresh one complete manager mirror snapshot without new Progression saves or rewards.

**Notification/reward/persistence boundaries:** requested runtime refreshes update all mirrors silently while a manager-owned command/reward/premium/reset/persistence scope is nested. Balanced try/finally depth publishes the settled outer snapshot. Publication version/liveness checks after each channel/UnityEvent stop older trailing payloads after reentry. Reward eligibility remains on OnLevelUp, not the mirror path. Existing reward-save and command-save calls remain, including two saves for an autosaved command crossing a rewarded level. Premium replay still captures the initial achieved boundary, and ordinary recursive XP rewards reread the watermark. These source guarantees do not add arbitrary external reward rollback or atomic Core+Progression saves.

The **14 added methods / 15 scenarios** were read in full, along with earlier positive controls; the fixture now has **30 methods / 37 authored scenarios**, all UNEXECUTED:

- Same-key downgrade/reload/recover repeats retain5 points; explicit reset permits fresh5; another loaded key seeded10 reaches15 while original storage remains5; changing selected key without loading does not discard active history; Core-first restoration baselines an incoming higher level.
- Direct Core +25 / repeated unchanged notifications yields one complete25/1/75 mirror snapshot, zero rewards and zero saves. Direct downlevel/reset/load refresh without claiming restored levels. The rewarded manager command publishes one settled snapshot with2 points and preserves exactly the existing two saves.
- Public reactive ListenerCount controls verify same-reference assignment, replacement and deterministic destruction-message cleanup return channels to their baselines. Swaps from earlier real reactive/level listeners reject old captured callbacks.
- Notification reentry ends at35/65 without a trailing25 event; an observable publication exception does not leave later changes permanently batched.

Owned inactive objects/ScriptableObjects, singleton/search/create state and exact Save provider are cleaned/restored. The destruction-message case is explicitly not native destruction. Changed guides accurately describe active-key rules, Core-first restore, disposable mirrors, event coalescing and best-effort/durable-ledger limits. **NI20-02 remains a concrete P1 source hole:** completed initialization and an in-progress precommit attempt still share the same flag, so a read/bind failure can permanently skip required profile initialization. Its fix must preserve the successful/postcommit observer and reentry behavior above.

## Grid — arithmetic, shape/rules and lifecycle review

**Footprint coordinates:** `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldGenerator.cs:831`, TryAddCoordinates, widens x/y/z before addition, rejects outside Int32 and converts only afterward. Both generator validation and GridPlacementService use it before board lookup. All entries resolve before writes; duplicate/bounds/enabled/walkable/occupancy/predicate checks remain. Invalid sums cannot wrap onto origin; valid negative offsets and outside anchors whose resolved cells fit remain allowed. Request/predicate/entry mutation is explicitly outside this bounded placement transaction.

**Merge values and rule boundaries:** GridMergeRequest.SaturatingAdd uses long intermediates and signed Int32 saturation; factory Increment and resolver null-calculator fallback share it. Custom delegates, including Dice's existing long/capped calculation, are not replaced. Generic Merge stages its value overrides, applies them only for Mutate, and Grid publishes cell events after resolution. Saturation still clears the non-result group, and ordinary cascades/minimum-group guards remain the existing provider policy.

Whole-board merge scanning preserves RequireEnabled/RequireWalkable opt-ins and caller-owned occupancy; explicit seeds still pass CanUseCell. Generated bounds, shape flags and manual placement semantics are distinct. Manual FieldObjectSpawner.SpawnAt deliberately validates cell existence/template, leaving shape/walkability/previous occupancy policy to its caller; do not invent a new disabled-cell refusal for that API. The existing disabled merge/preview and generated walkable-spawn controls were read. No blanket arbitrary vector/size/custom-callback input policy, gameplay-core replacement or automatic external occupancy ownership is inferred.

**B07 lifecycle:** registration/removal and current-cell identity checks prevent old generation objects freeing replacement cells. Metadata removal precedes object-removed notification; original instances remain detached/tracked after regeneration until removed. Post-field-state callbacks are rechecked before publishing stale occupied/freed events. The seven new bodies cover detached removal, regeneration from spawn/remove/state callbacks, removal inside spawn, and ordinary sequential blocking/decorative objects with actual deferred destruction. They do not cover valid nested first occupancy publication; NI20-01 remains.

**Gizmo style:** the changed generator creates one GUIStyle per draw invocation only when cell/coordinate drawing needs it, outside the xyz loops, preserving alignment/font/color/text. This removes the demonstrated per-coordinate style construction without claiming no other label/string allocation or measured timing/FPS/native-memory improvement. Both UNITY_EDITOR and player preprocessor paths retain the appropriate variable uses.

**46 new authored Grid cases, all read and UNEXECUTED:**

- Boundary fixture: **11 methods / 39 cases**. Both public placement paths reject Min+Min on all axes and a later overflow after an earlier valid entry with no writes/results/events. Negative-offset and extreme-cancelled-anchor positives still place correctly. Dice goes through those public paths with rejected overflow/no board events and a valid placement control.
- Factory/default increment cases cover both integer endpoints/extreme signed steps and ordinary results, assert final board/group/notification snapshots, preserve preview/no-write/no-event behavior, clear saturated groups and finish an ordinary two-group cascade.
- Lifecycle fixture: **7 methods / 7 Play cases**. They assert current replacement cell identity/occupancy, registry counts, event absence/order and Unity object-null semantics after yielded destruction. Owned roots/templates are destroyed and cleanup yields. No reflection/source-text no-op oracle or unsupported NUnit serial attribute is introduced.

All four changed guides match widened placement, signed/custom merge policy, stable-request limitations, generation-bound metadata lifetime and typed/scene entry points. The spawner's documented occupancy-maintenance promise is incomplete for NI20-01. The final Grid receipt's shared Play reference was initially absent, then root added literal Neo.GridSystem (and separate Tools.Text/Move integration); current source now contains it. **That named source dependency is closed**, not a compilation result or an unresolved request. No asmdef was edited by this reviewer.

## Integration and required next acceptance

Return NI20-02 to SAME Progression owner `neo-audit-1023-b04-07` and NI20-01 to SAME Grid owner `neo-audit-1023-b03-05`, then review their exact source-final corrections. Root explicitly accepted both bounded scopes after independently reading the relevant source. Save needs no additional source action from NI20. Keep shared provider/definition integration; do not introduce optional transaction/session/catalog/lease frameworks.

After every source writer finishes and root opens execution: compile/import/discover on the intended Unity6.3/6.6/MIRROR/Odin configurations; run the frozen three Save fixtures (old22 + new10), Progression37, Grid39+7 plus relevant earlier Grid/Merge/Dice/Match3 and integrated consumer suites. Also run the later initialization/nested-spawn corrections and their controls. **71 added scenarios across these phases is only a source inventory**, not discovery/pass evidence.

Mandatory native/integration debt includes Resources import/retry/cleanup, exact static/provider/subscription restoration, scene/Awake/sceneLoaded/domain-reload-disabled lifecycle, real object destruction/occupancy/callback ordering, original persistence/write/recovery behavior, profile observer exceptions and newer reentrant bindings, UI/NoCode composition, actual Gizmo appearance/allocations and filesystem/crypto/platform behavior. Save's void providers do not certify write durability; profile/reward saves remain non-atomic. Old compiler diagnostics/static receipts do not certify this snapshot. No executable checks or whitespace/package/text/GUID validators were run during NI20.

## Current source-manifest identities

For comparison with NativeA, digest formula is SHA256 of sorted assignment paths encoded as UTF8(path)+NUL+ASCII(lowercase raw file SHA256)+LF. These inventory identities do not claim full unrelated re-audit. NativeA's prior Progression digest was `13190a68cb70a90f7948b1ce018b5680902923b4cf1258b6474ed52f2fe1f838`; the current changed digest is expected and cannot prove individual non-manager files unchanged. NativeB did not supply a corresponding individual Grid runtime fingerprint ledger; no invented equality is claimed.

| Module | Owned C# files | Current manifest SHA-256 |
| --- | ---: | --- |
| Neo.Progression | 13 | `934665e04c8510e3a11ce9695bb8c93a956e04909ee8c09baf60dc3588e07ae9` |
| Neo.GridSystem | 30 | `fa5e1a0b5deafa8bb8e6171c0cdac2775696581add340cfe507d947c100ff804` |
| Neo.Save | 20 | `33365e658eaf087de455e229af8fa65655148f5cf11c01dc855712b2728b62de` |

## Frozen main identity ledger

The **105** entries matched initial-to-post-read gate. Read coverage is stated above: full final receipts/requests, phase bodies/hunks/tests/docs, exact prior matches and provider context. Inventory-only rows are identity bookkeeping, not an assertion that every old body was reaudited. Raw hashes retain BOM/newlines. Any later scoped edit invalidates the affected disposition.

| Full artifact path | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Save.implementation.md` | 9223 | `8807178f02fb9320da34227f571641b7e97d2a497a83bb1c9b073ffe3a73ff92` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Progression.implementation.md` | 9491 | `46b5110fbdfb128fb680371ba409d28b596af38c235f6c0e3a78c5d20a6e11f6` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.GridSystem.implementation.md` | 10131 | `b2993c23d5ee22b6c6ba37beddc5911e285310ad5339e075bd74b29c874f6e6c` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Save.implementation-request.md` | 3828 | `d134df41989b3bc2d9cf1a1deeae2c750d801face064e69ec5a6023d578c0047` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Progression.implementation-request.md` | 5375 | `27821a8425d474d4da958fe83331fa41e08f7d307c7f3bff2d0359ccb35865b9` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.GridSystem.implementation-request.md` | 5941 | `cf0279caa88f972e8895e5977d3dfacc7ea9868d28d7d5e5ff8467a7381f6b70` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Bridge/ProgressionConditionAdapter.cs` | 2944 | `231100a60563f1f1b50f2dec4edd946a65af099725d5e3d9acfce0e461744e35` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Bridge/ProgressionConditionEvaluationMode.cs` | 324 | `376160364fa2fea6bba5b0ce6b4c8769baef84a0bd1fcd2bf8536e6c82748891` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Bridge/ProgressionNoCodeAction.cs` | 6761 | `c3c1f494d61749b0c06ba004025edb35544e00aca28fe4068cffc80676a67407` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Bridge/ProgressionNoCodeActionType.cs` | 338 | `1cadf60200acacebc289bcbaada8b0626cf0654247bd1870b8bd6a8c21de7741` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Components/ProgressionManager.cs` | 36437 | `4bb7d33d866d672c0aa18117b6c5c98ef53922a60e04c64db37cadc3bb59639f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Data/LevelCurveDefinition.cs` | 6578 | `fc9e450fbba5dd6b67415985dd5e0d89a672366b5b0545789b402ed66e5e4d00` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Data/PerkTreeDefinition.cs` | 9616 | `68d5b6b41775c8b39afc2780150a65616004607543d7199755c2c621e695e26d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Data/ProgressionReward.cs` | 2822 | `b9a241d1afa991bbbe5465be531ca726f1ad2cdefd4ecadd65b0b979f5d45b46` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Data/ProgressionRewardType.cs` | 340 | `99cfb41bbb14697a8293b130a7ce28bc46c41c9eda83699486f231abf8dc32d5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Data/UnlockTreeDefinition.cs` | 7467 | `effabdee846152ff0711628c62904c34a1a9ed1f8e06f529e7a804a3d7a9269d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Runtime/ProgressionEvents.cs` | 324 | `261babc4564ba658c38cad5a296c267f984f69f62430526d81dadd4c35af8b4f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Runtime/ProgressionProfileData.cs` | 3224 | `48c80ae1d1e8d636e525b8c423e4631add57f64869eff858adc06e803bb3c3c2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Runtime/ProgressionRewardDispatcher.cs` | 4328 | `42df92155f4a5689dcb80b3bc5a152cc9d112f48c2061f140de28e2a46c0f893` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Neo.Progression.asmdef` | 879 | `b96d110e1cdac4a124f5db2a2546eb3ce77b637d4eafbbbfb3a7f50dba04bce1` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Progression.md` | 22461 | `5e3c2163f2e9981d99c72308f00543cb9ca1d8f98cb57152a5da5e22e373acf4` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Progression.plan.md` | 14765 | `07a9545bdafb7369078bfd64577a50d5198af851ef39a2e5fe5c7fdbf3d6f58a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldCell.cs` | 2979 | `20576842a7f3de0613477c5c5cb8e8f9bf1c6178b0e29a0dabe4e2a2da125ee9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldDebugDrawer.cs` | 4452 | `76424ee1aa8729df7f93b981733af3d3d1f53d00454fe02aeea87ebc6b1c858b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldGenerator.cs` | 40927 | `af277d868af63771321ae96db12a209aaed1c9ac09635a01801eb00b91c4d609` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldGeneratorConfig.cs` | 3159 | `3a564b4729b86b7db594164981adbfba8e2635934eda80b286827d2853b328b5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldGizmoSettings.cs` | 892 | `26478280d0ee80f75df04672e0e7e48603a6adbd1d2e458ceb7b56bfc5c4f958` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldObjectSpawner.cs` | 9190 | `f7d75ac74fba740f6cf2a2d0a203556a8a406afb3270283fc333f4226c62aeb3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldSpawner.cs` | 2850 | `d147065cc03856dbbfa96dea6093a5759612e431cb882279382cef832a34a161` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridCellMarker.cs` | 1168 | `5a813c73e08d666af840a8f4ed04fdaaf9a8895d64ecee88594c79b86e60c3ae` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridGameBuilder.cs` | 2474 | `05e2e2b5f340f8decdfbc83aa6139a4b98f7cbdc2e0d273d9585d02548df273d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridPathfinder.cs` | 5851 | `e3e057a7a1051fe4196509e80c37df3d0c34c8e76ae75cf24b6399721dcd4259` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridPlacementEntry.cs` | 531 | `9612bb16880c734c1f39593d21d074829885da9b3cc612d7e5fb3d4ce9688e7e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridPlacementResult.cs` | 407 | `6f099a51c66e37825e77867be5b6d720d72bfd310ab5fe595ed46569376a98db` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridPlacementService.cs` | 8113 | `3a9366505499dc32aed169523f2f084c4336ec2758bf7febd95e9c5ca96df957` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridShapeMask.cs` | 2275 | `ce9c1cf6f2625a617d2533bf25c58470d46a445f174480d92dc0a383a129ddae` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridSlotAllocator.cs` | 7735 | `b43dc21c71a7c167eca82162b388b77651b45e267cc7d111f75573abdf6a1fe2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Match3/Match3BoardService.cs` | 24425 | `c107c61b82c0869a1f2e1848afd035e7a2d6a72f9ed983451108738aabf1b370` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Match3/Match3MatchFinder.cs` | 3015 | `5e9a7083e3a98f1bfa5b246e172f0f6481a658cfc72cd369bd615cceb562908f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Match3/Match3TileState.cs` | 306 | `a7aa9c151fdd4f68cb30711120dff665bf733910d92f3efa3c2849ead8642f58` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Merge/GridMergeGroupResult.cs` | 557 | `5809bfdb1b747e556a98b7eba3235dba3b0f9c2c932ea754e1173445314c71d5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Merge/GridMergeRequest.cs` | 3753 | `965772b96dc8a3278269b877cc640cf88183a74a9a156cfc6c50501e10107fb6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Merge/GridMergeResolver.cs` | 5699 | `8aa75b9e497279aa6d2520227d78bb9449fa5dd4c9456a38d61099dd8896481f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Merge/GridMergeResult.cs` | 730 | `bbb3b9f0d54d80e1ac1b3adfc5717028e65de089d0bb8133d7992f126ec3b304` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/MovementRule.cs` | 4514 | `9f6ef2432b1c2a51ae97ce541268dc21c97c72b346a9a3f10186a80b031897d8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/SlidingMerge/SlidingMergeBoardService.cs` | 4491 | `62ebc849562bfc5a3479548ca2e7002515c372036250dfaa469a63d28061d6d8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/SlidingMerge/SlidingMergeDirection.cs` | 1116 | `ff593bbaf7197472d6e3f03a8a2de722e7eb20e4d8e058723df6efe40c69d45c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/SlidingMerge/SlidingMergeResolver.cs` | 9681 | `08cd07ae8f6ba186dd1f000bc0e6a10101c7e8d0e850cbb23ba66544edaa92e1` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/SlidingMerge/SlidingMergeResult.cs` | 606 | `acc1f0662e54acfb38f75697fc0e732e78a1b20bb3fe415c2a53048da6417fe4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/TicTacToe/TicTacToeBoardService.cs` | 6053 | `e531e9f0b9ffdf9f93ecd8ca350436d62f0c964c265f069f4bbaf97a5cc22043` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/TicTacToe/TicTacToeCellState.cs` | 253 | `e4862c510f714eb63c854faadd0703c7c93dae7288f1f00d4fb957e80aee0c6f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/TicTacToe/TicTacToeWinChecker.cs` | 4496 | `e18de91124f8f05aceafb973b94d9f834f3f03a693d14b4e0d82325962978884` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Neo.GridSystem.asmdef` | 141 | `b7999cbbc27ba7e3d2153b6d0fc8ae01182fde8bd91313c3f675ee0a8754b313` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.GridSystem.md` | 27509 | `4835bb77089394439008f322359b1bc8ccc6284a5ec85968e8665bdc612a580b` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.GridSystem.plan.md` | 8817 | `c6b3241cce1fcb4977f441da450a2efde8b7ed218b5e666165d9e80b7f592bcd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/Example/PlayerData.cs` | 1085 | `d38ab38b81d5f6587747ba32f20a7dc077f5054ac02a5b55f68e293f63bf462d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/GlobalSave/GlobalData.cs` | 106 | `aaab74b05aa662593aace0f5511547f897832da744b48114b20cb068e80ae4b8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/GlobalSave/GlobalSave.cs` | 2237 | `6351345dcdb6e877fbfbdc60999684bd6889773067bac8fca400707f02fdcce4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/ISaveIdentityProvider.cs` | 376 | `edb05b95297287f18fb4b4a096377139f3fa1e2ef8cbb952d0d7d28f7457ab4f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/ISaveableComponent.cs` | 111 | `bfa289f1eacfd8a439239122becc5551bd0397cb19a482eb343e258e382c5e6b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/Neo.Save.InternalsVisibleTo.cs` | 94 | `6d794d9e032d0204a83184919d0021522e4bf35eef9aa48cf891481c127ee147` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/Providers/FileSaveProvider.cs` | 17848 | `a677c26b4ec381ff7f68be318801754d3d36b6bd4b0c40a6622d89193f8e05ba` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/Providers/ISaveProvider.cs` | 4059 | `dc614ab88969fd45948d93fbcc81f14f9631b8b8654ef550f4832f7839e07bcd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/Providers/PlayerPrefsSaveProvider.cs` | 4749 | `77fbe36aedc7e9232b46489d187708146e1b08650e22e7dd9d74b3e4b78316e3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/Providers/SaveProviderType.cs` | 392 | `b599f6e36f4b1e23611ee7f8e50c6169fe0f5278a6951296c41dfbcda00df603` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/SaveField.cs` | 562 | `1ec7d8e4ac554c2ec66994b4c0f26dd99b453412ccee1bbf613c73e7557d3589` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/SaveFileEncryption.cs` | 7606 | `2de9a171f01e4bb684919f425167993212e35dce7a702a44a00b2cf8cb1618aa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/SaveIdentityUtility.cs` | 3857 | `591bde48c42758759854503b77a4015c42f875e7e30016814be76e992d26ba8d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/SaveManager.cs` | 30017 | `0f892ab9c35b1d91e75dac5334f8c630691d422571b9d903b335cd81be18fc64` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/SaveManagerSubsystemRegistration.cs` | 705 | `0563d3239f8d3e3d79406a68aad3c88faf2afe7f2ff18f48d9b4018d7242897c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/SaveProvider.cs` | 12081 | `6195d7abe3096d9126f235b27aaec08c564e5b6851d8fd6a31621fc1ad449cb1` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/SaveProviderExtensions.cs` | 4048 | `2c444ed74d0416fed88064c58a963bf9efc2da9ca06090ee34400e811d30e86c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/SaveableBehaviour.cs` | 925 | `bc894d3c03d07617e05dd8a8164e5fbdcaa6c4e9a2c1ce861675bdb9b65d7bba` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/Settings/SaveProviderSettings.cs` | 4671 | `311d30d4e7f0373070388094ca376eff3294af09f969beb81b940c062bbfca19` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/Settings/SaveProviderSettingsComponent.cs` | 1588 | `eec64d98c5e2697de5b2e9b624fa8ee05a8fcb4b1be9c4d94786fc91a36b62ca` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/Neo.Save.asmdef` | 480 | `74c981c810c29703aebc5ca09a0bec0064958c1b6bfe9e3e812b6f86b48cbbfb` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Save.md` | 22268 | `5c2a584a939db2fe9a102149548dbcaf75cdecd5f67279e259dbf1d802e5da13` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Save.plan.md` | 8864 | `aefad16b73f7e133b8450eb0b6f8c71bb5ee7f3b89fd66b158bb3b75f4cb80cf` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeReview-A.md` | 116157 | `564fefce32e51ae170cdf2712069331265989b50aee59beda1666852c2872ef6` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeReview-B.md` | 82796 | `3d81b201d8fc56e890c557ecf0c1848c4d216be087c79c907712a84dd45b111a` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeReview-C.md` | 152380 | `118bffb374e28cc2fc75f525e3292ac38e000d996b6c42b3d71a89edbb9cb075` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveConfigurationTests.cs` | 15119 | `ba36432610b61661af954f228a2c3f706b89f6e1e4d4574e5113e929f7f3b44b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveConfigurationTests.cs.meta` | 60 | `e60ce811ea4e7ffe3c82999319c5ac99559521cef32de89f8230d925524d3d02` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveFileProviderTests.cs` | 6661 | `94859d2e42de107807cd94fac62a23c6a3a3f38401af8b1f78c3b34e754dd568` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveManagerTests.cs` | 10896 | `9d2ebafe0496dd2da10df6279a7091ca95fd4edff4e8a2854f49e7d40a140c35` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Save/Settings/SaveProviderSettings.md` | 3715 | `37a2c82d6cb8b6bbc6dac866653eee2ca292295dfbadca6218386332437d6c48` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Save/SaveProviderSettingsComponent.md` | 2199 | `a295d4e669b7364ad1d550e806fe94076d6852158f461904a493c5f37e83f052` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Save/SaveProvider.md` | 3384 | `c8c298f4317c0b20d49746f78bfceca8cda116e4fcfdf323f5f9600991b7092e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Save/README.md` | 3666 | `ef6f9ccb4a90cd6fb3944cab5dba68c7e341bebb4171bd007b4584812638a074` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Progression/Audit20261023NeoProgressionRegressionTests.cs` | 37203 | `a0c843f0b333cd01b4b1c3b868b3f35e5f3a63b1fe0882ec5da8bab0acab5772` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Progression/README.md` | 7141 | `819e709be8310ba5cd869c4be45049ddbc8da1bf764b70dc4a97c2bb814ef147` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Progression/ProgressionManager.md` | 8154 | `7bcc08a8ba0f3b5dc53f4ef5a22ff4a0981bb8cc98d2ac2f8e0a8c2ebfc10b0d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Progression/Data/LevelCurveDefinition.md` | 3007 | `d4b0ade2c04d63dc31c1db744e1c1ce607385f9e2ead84e690d1f7694b7ec680` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/GridSystem/Audit20261023NeoGridSystemBoundaryTests.cs` | 14430 | `487495c8db0c549941f7c140108a5d48348f6ca8fdddfe43e20c87b53fcb7262` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/GridSystem/Audit20261023NeoGridSystemBoundaryTests.cs.meta` | 60 | `a5ab82f460a44aa9f0bb97275fa3c0e599da93c5c569e0f96d8eac3db7ad5bc6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Audit20261023NeoGridSystemLifecycleTests.cs` | 11302 | `a765e4600ddcedcab196eba6ea294cd0573cd42d24847fe552334016d8f6b409` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Audit20261023NeoGridSystemLifecycleTests.cs.meta` | 60 | `e93904316d90b5421aaa8fca8fb6e342d0f8950517af54c10dd74ab1fde63aa7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/GridSystem/Audit20261023NeoGridSystemRegressionTests.cs` | 23693 | `9e3a5d3c7a69f98e44eef0e1e4a1b5d0729e629bf3bcb6910847b0e8186d69a5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/GridSystem/README.md` | 5853 | `8e38b2273bfec7cca061ab8671cb6f0fdc51a3813a099ee38fba5e2ca4774769` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/GridSystem/FieldGenerator.md` | 5942 | `7418a0be81312747ed69f5ca0d28868a0767d21f55e190f6773460e0c5fb1840` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/GridSystem/GridPlacementService.md` | 2871 | `22193ca47695512dd0649b24b1d04385258873d24c5d590df20453c15b10b14c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/GridSystem/FieldObjectSpawner.md` | 4051 | `36efae710b27cc9827f2ee70413d1f451881d4ce5c141397c16068d4a5147b04` |
| `D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/agents/neo-audit-1023-b05-07.meta` | 167 | `b42eb4726f5d37ff573526be7a94e8b01fd66da944a3c126409dc75bf5c588c9` |
| `D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/agents/neo-audit-1023-b04-07.meta` | 234 | `a67f5cb80e6ee68bc96f789f063ce4289a139f8564d700c9855236f35714d397` |
| `D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/agents/neo-audit-1023-b03-05.meta` | 234 | `bd9e5b549f48c03538b80249ef266c67063180754d18e4c4608c25c4f7fda764` |

## Additional provider/shared-definition context

Initial checkpoint **2026-10-23T21:05:33.872292+00:00**, unchanged at the post-read gate; table below uses final handoff identities from **2026-10-23T21:18:55.287936+00:00**. Six entries remain identical; shared Play was fully reread after root added its separate Move reference. These are partial dependency bodies/reference checks as described above; LevelProfileData and the shared Play definition were read fully. Hashes do not grant full independent runtime approval or compilation. The Play hash identifies root's corrected definition, not the initially missing-reference revision.

| Full context path | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Core/Level/Components/LevelComponent.cs` | 10802 | `616dd0235e0c7f2de3b4a074739dd135fb6227b164910fbb52066aa93fa31cab` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Core/Level/LevelProfileData.cs` | 1718 | `cff00c207faafa5d9167cc87a0866d3571f193790a25fd820a9ffc6a6c722179` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Reactive/ReactiveProperty.cs` | 21394 | `14df81d1e13d76b658e20e8560dfa14376ce882c716a827da80f34e03256b2a4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Dice/DiceBoard.cs` | 6740 | `a2a9a6646f5f5aa906f48614be96fb19b379eb234cd9e9dd2db30c9ba6ecfede` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Merge/MergeResolver.cs` | 8891 | `7a305eb80367f67760b74705107b6c2e5095cda9fcb835dc1019ea0099d65b8a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` | 1505 | `2518b0df939933514e55de55be5313226107a2382fe89d31b5361565aea50fe4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` | 2098 | `204a5ba6382792131c48cae74f38e27f5431909c0c0866c3b2905be0f2c4b4a5` |
