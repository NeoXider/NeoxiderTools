# Neo.Editor — implementation receipt (11.0.0 / demo)

Actor: `neo-audit-1023-b03-03`, same original session. Current status (2026-10-26, P7 correction): **SOURCE-FINAL for the owned Edit fixture callback prerequisite; no pending source/API dependency**. Both retained P7 changed-family runs report **133 passed / 1 failed**; acceptance of this corrected fixture is PENDING. Branch `demo`, target `11.0.0`. The final section records the actual failure, bounded callback-state scope and current hashes. Earlier audit/plan, interrupted S3 exit **1**, earlier 21 failures, source-final snapshots and native failures remain historical evidence. Root's two idempotent production activation guards and receipt amendment are preserved. This actor launched no compilation, Unity, tests or product validators.

## Additive API available now

`Assets/Neoxider/Editor/PropertyAttribute/CustomEditorBase.cs`:

```csharp
internal bool HasRepaintLease(double now);
```

Read-only addressed inspection of this Inspector's animation/live/status leases using the production expiry predicate. It does not tick any client, expire another lease, change membership, advance shared repaint timestamps or alter update subscriptions. Existing `InternalsVisibleTo("Neo.Editor.Tests")` exposes it to primary fixtures. `Neo.Editor.Tests` can inspect its own future expiry while retaining a live foreign-client sentinel instead of calling `TickRepaintClients(now + 60)`.

Additional public Editor helpers in `Assets/Neoxider/Editor/Binding/ComponentBindingInspectorShared.cs`:

```csharp
public static Component FindComponentByTypeName(GameObject obj, string typeName, int occurrence);
public static int IndexOfComponent(IReadOnlyList<string> fullTypeNames, string storedTypeName, int occurrence);
public static int ComponentOccurrenceAt(IReadOnlyList<string> fullTypeNames, int index);
```

Occurrences count matching types only. Negative/missing requests return null/-1. The existing two-argument resolver and `IndexOfFullName` remain available. Runtime fields consumed from their owners are exactly Condition `_componentOccurrence` / `_otherComponentOccurrence` and NoCode `_componentOccurrence`; runtime public properties are `ComponentOccurrence` / `OtherComponentOccurrence`, default zero.

## Completed owned corrections

Paths in this section are relative to `Assets/Neoxider/`.

| Approved action | Files and resulting behavior |
|---|---|
| `EDITOR-COMPONENT-INSTANCE` | `Editor/Binding/ComponentBindingInspectorShared.cs`, `Editor/Condition/NeoConditionEditor.cs`, `Editor/NoCode/ComponentFloatBindingDrawer.cs`: duplicate component rows select and preview the exact occurrence on both Condition sides and in float bindings/reactive hints. Existing flat UI index fields are retained; only an explicit popup selection writes the new occurrence. Missing/negative stored selections no longer auto-snap to the first component. New/reset entries use occurrence zero. |
| `EDITOR-INTERFACE-CANDIDATES` | `Editor/PropertyAttribute/ComponentDrawer.cs`: scene candidates are filtered through every existing `RequireInterfaceDrawer.IsReferenceValid` constraint before assignment; original search scope/order and explicit references remain intact. Assignment records Undo. No-match does not dirty the field and warns once per object/field until success, using weak object ownership. No shared candidate cache/framework introduced. |
| `EDITOR-BACKUP-IDENTITY` | `Editor/GUI/SceneSaverGUI.cs`: `SaveSceneClone()` uses `Assets/Scenes/AutoSaves/<source-GUID>/<name>_AutoSave.unity`. Internal `GetBackupPath(string)` and `SaveSceneBackup(Scene, out string)` provide the actual writer to addressed fixtures without mutating shared scheduler/settings. `saveAsCopy`, public API and scheduler handling remain intact. |
| `EDITOR-LOBBY-PERSISTENCE` | `Editor/Network/LobbyDemoSceneCreator.cs`: save/continue and destination cancellation precede scene replacement and asset creation. A unique adjacent asset folder receives real room/game prefab assets plus `LobbyGame.unity`. Assigns typed `roomPlayerPrefab`, distinct `playerPrefab`, lobby Room/offline/online paths and GameplayScene. Buttons/address/discovery/status/count text use persistent listener targets. Scene-canvas presenters survive the workflow through fresh scene views and the runtime singleton fallback, without cross-scene serialized manager references. Generated gameplay includes a persistent Leave control. KCP is explicitly assigned if available. Private temporary prefab sources and the owned gameplay construction scene are released in finally. |
| `EDITOR-TEST-GLOBAL-SAVE` | `Tests/Edit/Editor/Audit20261023NeoEditorCollectionTests.cs`: removed the unnecessary global `AssetDatabase.SaveAssets()`; CreateAsset writes only owned fixture assets. No replacement global save. |
| `EDITOR-TEST-REMAINING-ISOLATION` — owned portion | `Editor/PropertyAttribute/CustomEditorBase.cs`: published the addressed read-only lease seam above and reused its expiry predicate in the production ticker. Added a live foreign-client sentinel regression in the owned prefixed fixture. Existing legacy fixture integration remains with its primary owner; see handoff. |

Seven owned production files changed in this implementation phase. Earlier coherent audit fixes were preserved. No runtime, sample-owner source/assets, shared configuration, asmdef, manifest, contract or another owner's receipt was edited.

## Regression source authored, not executed

All paths are under `Assets/Neoxider/Tests/Edit/Editor/` and retain the `Audit20261023NeoEditor` prefix.

| File | New acceptance cases in this phase |
|---|---|
| `Audit20261023NeoEditorCollectionTests.cs` | 3: skip a partial interface match in favor of a complete Component; GameObject constraints satisfied by separate components; repeated no-match neither assigns/dirties nor repeats the diagnostic. Explicit references remain untouched. Global SaveAssets removed from the existing resource case. |
| `Audit20261023NeoEditorBindingInspectorTests.cs` | 4: interleaved component types map to the second instance and its actual preview value; negative/missing requests fail (two cases); EditorJson roundtrip preserves occurrence and independent legacy indices, reads the second runtime float binding, and rejects its removal. |
| `Audit20261023NeoEditorInspectorTests.cs` | 1: inspecting an owned future-expired lease preserves a live foreign client and shared membership. No global ticker advancement. |
| `Audit20261023NeoEditorSceneSaverTests.cs` (new) | 1: save two GUID-owned same-basename scenes, inspect distinct saved backup contents, update one backup, preserve the other's bytes, source bytes/paths/dirty states and active scene. Uses the production writer, without shared scheduler/EditorPrefs changes. |
| `Audit20261023NeoEditorLobbyTests.cs` (new, MIRROR) | 3: save-prompt cancellation and destination cancellation preserve unsaved scene handles/content and create no assets; saved/reopened lobby retains prefab assets, all four callbacks, address/status/count wiring, gameplay scene and its Leave callback. Does not start a server/client. |

There are 12 added source cases, giving 40 cases across the seven owned fixture files when MIRROR is enabled; this is source enumeration, not test discovery or a pass claim. The new scene/lobby files have `.meta` GUIDs `9e53cd7d0c9b4121b6ed3762e0b528f9` and `c35f4eb6213c40a2b722bed5613cacc1`. Central metadata validation remains pending. Fixtures use isolated owned asset directories/additive scenes, restore active scene/selection, and never call global SaveAssets.

## Public documentation and compatibility

Updated exactly these owned public pages:

- `Assets/Neoxider/Docs/Editor/README.md`: occurrence C#/Inspector contracts, legacy defaults and links to backup/lobby workflows.
- `Assets/Neoxider/Docs/Editor/ComponentDrawer.md`: all-interface candidate filtering, explicit references, Undo and bounded diagnostics.
- `Assets/Neoxider/Docs/Editor/SceneSaver.md`: GUID backup layout, recovery and retained older copies.
- `Assets/Neoxider/Docs/Editor/EditorWindows.md`: persistent lobby/prefab/gameplay authoring, runtime presenter calls, Build Profiles/Transport setup and sample-layout boundaries.

English guides include Russian 11.0 behavior summaries. No existing public/serialized member was removed or renamed. Deliberate changes: GUID directories replace basename-only destinations while all old backups remain; invalid component occurrences require explicit repair; newly generated lobby content includes an adjacent asset directory and gameplay scene. Existing scenes/prefabs are not automatically migrated. Developer `Samples`, release `Samples~`, and imported sample roots remain unchanged. Large maintenance/update lifecycle frameworks stay historical proposals.

## Exact cross-owner handoff

- **Resolved source dependencies:** Condition and NoCode occurrence fields/properties are present. Neo.Network's source-final presenter provides `Manager`, `SetAddress`, Host/Join/Ready/Leave and `OnStatusChanged` / `OnPlayerCountTextChanged`; this creator consumes that exact API. Existing `Neo.Tools.Text` reference supplies `SetText.Set(string)`; no assembly change needed. Runtime/sample ownership was not crossed.
- **Primary `Neo.Editor.Tests`, actor `neo-audit-1023-b03-04`:** complete the legacy `EDITOR-TEST-REMAINING-ISOLATION` integration. B-18 maps to `Tests/Edit/Shop/ShopPurchaseRollbackTests.cs`, `ShopAffordabilityTests.cs`, `ShopVariantsPanelTests.cs`; use the existing exact-provider SaveScope and destroy components before restoring it, retaining substantive assertions. B-19 covers `Tests/Edit/Extensions/ExtensionsTests.cs` and `Tests/Edit/Tools/TimerTests.cs` fixed keys/cleanup. B-20 covers `Tests/Edit/PropertyAttribute/NeoInspectorAnimationTests.cs`: consume `HasRepaintLease(double)` instead of advancing all clients into the future. These existing files were not edited here.
- **Neo.UI.Editor / primary tests:** addressed preview stop seam and legacy `Tests/Edit/UI/UIMeshRigEditorTests.cs` adaptation remain their ownership. No global preview reset was added here.

No unlanded API blocks Neo.Editor-owned source. The legacy fixture integration above remains a precise cross-owner dependency for the global action, not a claim that those fixtures are already isolated.

## Actual review and execution debt

Performed source reads, bounded reference enumeration and manual diff review only, including current Condition/NoCode fields, Network presenter's final handoff, Mirror prefab/scene contracts and the existing Neo.Tools.Text assembly reference. Corrected a duplicated save found during diff review and kept the lobby presenter on the scene canvas so it does not persist with a stale UI through room/game transitions.

**NOT RUN by this actor:** compiler, Unity/MCP, NUnit/Edit/Play, network peers, package/product validators, UTF-8/meta/GUID checkers or installs. No service, child agent, commit or push. Earlier audit checks are historical and do not validate this source. Later coordinator compile evidence is recorded below.

After EVERY writer finishes, root's verification operator must compile/discover and run the nonempty prefixed cases in both supported installed Unity branches and MIRROR on/off. Graphical inspector/GPU, modal save cancellation and missing-script Undo/failure checks remain centrally pending. Lobby needs two independent peers, Build Profiles containing both generated scenes, runtime prefab identity/room-to-game replacement, Join/Ready/Leave and return-to-lobby verification. Matching the selected EventSystem input module to the validation project's input configuration and optional transport availability is part of that product setup. Central source/text/meta/package checks and review of all skips remain mandatory. No authored acceptance is reported as passed.

## Frozen compile correction — 2026-10-24

Coordinator reports identical first frozen compile failures on Unity `6000.6.0f1` and `6000.3.14f1`. Read the supplied `Local/Audits/assembly-2026-10-23/unity/final/ff4e1aaceb2e4320a3f882f61c262b15/u6000_6_0f1/mirror-on-dev/Receipts/compiler-errors.json`: four owned CS0400 diagnostics refer to `Audit20261023NeoEditorCollectionTests.cs` lines 27/33, where the compiler attempts both `FindInSceneAttribute` and its abbreviated name in the global namespace.

Bounded source repair: changed exactly those two annotations from `global::FindInScene` to `global::Neo.FindInScene`, matching the actual `namespace Neo` declaration in `Scripts/PropertyAttribute/InjectAttribute/FindInSceneAttribute.cs`. Both probe fields, all stacked interface constraints and all lookup/assertion oracles are preserved. Inspected the other fixture attribute declarations: `GetComponentsAttribute`, `FindAllInSceneAttribute` and `LoadAllFromResourcesAttribute` are actually global and their qualifications remain unchanged.

Only the owned collection fixture and this receipt changed for this repair. SOURCE-FINAL again; no API or source dependency blocks the correction. Actual work here was source/declaration/error-artifact reading and the two-line patch. No compiler, Unity, tests or checkers were started. The execution gate is closed pending every source writer finishing and a new freeze; both-version compilation and test acceptance remain unverified after this correction. Other owners' diagnostics in the supplied JSON remain with their owners.

## Source2 compile corrections — 2026-10-24

Read owned diagnostics in both `Local/Audits/assembly-2026-10-23/unity/final/f84c9007d0494cd8a421d84d70d931a0/u6000_6_0f1/mirror-on-dev/Receipts/compiler-errors.json` and the corresponding `u6000_3_14f1` artifact. Both contain collection CS1503 errors at lines 75/76; Unity 6.6 additionally reports the two implicit `SceneHandle`-to-int CS0619 errors in the owned lobby/backup fixtures. This is actual coordinator compile-failure evidence, not a successful compile or execution of tests.

Bounded changes, all under `Assets/Neoxider/Tests/Edit/Editor/`:

- `Audit20261023NeoEditorCollectionTests.cs`: replaced the two `.And.Contain(nested)` chains with four independent NUnit `Has.Member` collection assertions. Both local and nested colliders remain required in both the scene array and list. Existing exact local/child collection oracles, preservation checks, interface constraints and the preceding `Neo.FindInScene` fix are unchanged.
- `Audit20261023NeoEditorLobbyTests.cs`: snapshots the actual sentinel `Scene` value and compares the active scene and sentinel object's scene directly with that value. All cancellation, scene/preview counts, dirty/loaded/content/selection and persistent callback/prefab oracles remain intact.
- `Audit20261023NeoEditorSceneSaverTests.cs`: snapshots the active `Scene` value and checks its equality after backups. Source paths/bytes, distinct backup contents, repeated-save isolation, dirty state, owned-scene cleanup and foreign active-scene restoration remain intact.

Source enumeration covered every handle snapshot/comparison in both fixtures: two integer snapshots and three handle comparisons were replaced consistently. No raw handle conversion, `GetRawData`, version conditional, name-only scene comparison, altered generic constraint or dummy base class was introduced. No Editor production/runtime source, public documentation, shared asmdef or another owner's file changed in this repair; only the three fixtures and this receipt were edited.

**Root-owned integration now present in source:** the post-patch reference read found direct `Mirror.Components` in `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` (line 28); this actor did not edit that file. The source2 artifacts report the prior CS0012 for `NetworkRoomManager` alongside cascading CS0311/CS1061. Read the real `NetworkRoomManager : NetworkManager` declarations: RoomScene, GameplayScene and roomPlayerPrefab exist, and NetworkManager supplies offlineScene, onlineScene and playerPrefab. Their assertions and `FindOwned<T>(Scene) where T : Component` remain unchanged. No unlanded source/API dependency blocks the owned repair; the effect of the root reference change still requires a new frozen compile. This actor did not weaken those checks or edit shared references.

SOURCE-FINAL for this bounded repair. Work performed: source/error-artifact reads, declaration/reference enumeration and source patches only. No Unity, compiler, tests or checkers were launched. Execution gate stays closed until all writers finish and root creates a new freeze; successful compilation on both versions and all test acceptance are still pending.

## Recovered S3 correction E12–E15 — 2026-10-25

**Current source work is finished; native verification is pending a new global freeze.** Root explicitly reported the prior S3 source attempt's nonzero exit (`1`). That interrupted attempt left partial scene-fixture edits but no S3 completion section. This recovery retained those edits, completed the owned callers and updated this receipt; the old source-final paragraphs above do not establish completion or passing tests for S3.

### Actual failure evidence read

Read the exact assigned cases in `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml` and their entries in `Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json`:

| Action | Actual S3 result | Current bounded source correction |
|---|---|---|
| `E12` | One failed `SerializedOccurrences_RoundTripAndReadTheSecondInstanceWithoutReinterpretingLegacyIndex`: `TryReadFloat` returned false at the old line 143. | Keep the primary-extracted, attachable runtime `BindingProbe` alias. Require the original attached source to resolve and read the second collider **before** serialization; require restored source root/type/member, successful resolution and the same exact `1f` afterward. First collider remains false. Both occurrences, independent legacy indices `91`/`92`, the read result and rejection of the removed second occurrence are retained. No runtime binding defect is inferred from the old invalid fixture precondition. |
| `E13` | All three Lobby cases failed in SetUp, old line 36: native additive `NewScene` refused the borrowed unsaved untitled scene. | Seed a saved owned sentinel asset through the existing preview scope, synchronously import it, assert its SceneAsset, open additively and assert active-scene selection before creating dirty unsaved work. Keep its context in a separate GUID-owned sibling folder so both cancellation cases still require an entirely empty destination. Both cancellation paths and every saved/reopened prefab/presenter/gameplay oracle remain. |
| `E14` | All five MissingScripts cases failed in SetUp, old line 29, with the same refusal; TearDown also dereferenced an unconstructed scanner at old line 40. | Use the saved owned preview baseline for both protected work and the duplicate-named scene asset. Require successful save/import/active selection, retain dirty unsaved work, null-guard the scanner cleanup and clean partial SetUp. Keep all five cases, exact duplicate/slash addressing, repair/result counts, closed-asset persistence and the loaded-scene no-implicit-save assertions. |
| `E15` | The one SceneSaver case failed in `CreateSource`, old line 111, with the same additive `NewScene` refusal. | Seed/import/open each owned `A/Level.unity` and `B/Level.unity` before adding their source roots. Keep both same-basename sources, GUID-distinct backup paths, exact backup root names/content, repeat-backup isolation, source bytes/paths/dirty state and exact active-scene equality. |

The assigned groups total **10 actual failed Edit cases** (1 + 3 + 5 + 1). Read `21-full-play-graphics.xml`; it contains no cases from these assigned fixtures, so it supplies no Play acceptance for them.

Evidence SHA-256 (file-byte bookkeeping only):

| Artifact | SHA-256 |
|---|---|
| `.../s3/u63/Receipts/22-full-edit-graphics.xml` | `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2` |
| `.../s3/u63/Receipts/21-full-play-graphics.xml` | `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a` |
| `Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json` | `df467fde346b0a1f7eca09a82b972f2e7532b7274758981969400628b1770e97` |
| `Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json` | `14b7a974c8a8a861a666b65d43971f42e4f97b21c01734daacd466f1e88fc3a7` |

### Ownership, scene preservation and current files

Checked the reservation map before each mutation. Its primary actor is `neo-audit-1023-b03-04`, status `source-final`, remaining source dependency null. The map still lists BindingInspector among original callers, but root's explicit Oct25 recovery instruction releases those callers after completed extraction. Applied that later authorization only to owned caller semantics. No helper extraction/move, Fixtures helper, assembly reference, shared scene scope, other owner's file, shared tracker or configuration was edited.

Reused the existing primary-owned `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsPreviewSceneScope.cs` unchanged (SHA-256 `5d201b58807f061d2d6076fb49550a891b41c12edae7147a19acd72b585b68cb`). The released BindingInspector caller uses the unchanged primary helper `Assets/Neoxider/Tests/Fixtures/Inspector/Audit20261023FixtureNeoEditorBindingInspectorBindingProbe.cs` (SHA-256 `c8431097eafb973bee63b94d2043d7b782535747fc7202e6c3530d3014dd54e9`).

All three scene fixtures capture the exact borrowed Scene values in order, each loaded/dirty state, active Scene and preview count before side effects. SetUp uses a disposable owned preview to save only a new GUID-owned asset, then imports/opens that asset; it never saves or closes the borrowed untitled scene. Protected work is created after the owned saved scene becomes active and remains unsaved/dirty. TearDown closes/deletes only owned resources, restores the borrowed active scene (and Lobby selection), and asserts exact original scene order/identity, loaded/dirty states, scene count and preview count. Partial SetUp invokes the managed fixture cleanup before rethrowing. This is NUnit resource cleanup, not manual MonoBehaviour lifecycle execution or native lifecycle evidence.

Current S3 source-final files, all under `Assets/Neoxider/Tests/Edit/Editor/`; this recovery additionally changes only this receipt:

| File | SHA-256 | Existing `.meta` SHA-256 (not edited) |
|---|---|---|
| `Audit20261023NeoEditorBindingInspectorTests.cs` | `4dfc8e2deea816d7694d73f7c4eb7a87412d6adced1638b43bf5a35d70cdd03b` | `40971fda354db2826d75e5d718f78f8678f8639f98203d9b682757b91ec73856` |
| `Audit20261023NeoEditorLobbyTests.cs` | `048983d64773438d184d45df98e22522577a95868ea6ab623beb3c92281f7e1b` | `98ec969a6930b821bd7d464584c93f84839d3c1db6d843444a78f8341b991663` |
| `Audit20261023NeoEditorMissingScriptsTests.cs` | `fd7043540064ffb94f0e69bb2c01eec52a22fc8daa42ce626adcb49a6a8523c9` | `2fc2b738f933a453a0d0c92723ed073e15f0533e42abab925626a97f08d4196e` |
| `Audit20261023NeoEditorSceneSaverTests.cs` | `9172bc58553a69a11ea835edfff57896e94ffc0444d54a4e5b3da8aad3720c4c` | `69a33f6590c56b9f46fc2fbbae2261e6ff54903204a84f9e3373b281b8fee30f` |

No additive production API, serialized/public contract, GUID, setting or product behavior changes in this S3 correction. Accordingly, the four owned public pages updated earlier remain current and are not rewritten for fixture-only changes. Historical audit/plan and optional service/framework proposals are untouched.

### Actual source review and remaining native evidence

Completed source reads, reference/declaration reads, the bounded caller edits, manual source-diff review against the frozen S3 scene fixtures, and the SHA-256 bookkeeping above. The diff retains the original three Lobby, five MissingScripts and one SceneSaver test bodies/oracles; BindingInspector retains its original assertions and adds positive prerequisites/diagnostics. No case was ignored, removed, weakened with tolerances/default changes or made to pass through expected-error whitelists. No global editor state reset was introduced.

**No remaining unlanded source/API dependency blocks this owned correction.** Shared preview scope and runtime binding helpers are present; their files/shared refs remain exclusively primary-owned. Source writes stop with this receipt.

**UNEXECUTED on the corrected source:** both-version compilation and all ten E12–E15 native cases; preview save-as-copy/import/open viability with a borrowed dirty untitled scene; failure-path cleanup; actual EditorJson serialization and pre/post binding reads; no-prompt cancellation; duplicate/slash repair persistence; backup identity/content/dirty/byte isolation. The positive prerequisites above are authored assertions, not observed passes. In particular, S3 never reached the Lobby SUT: the next frozen native run must verify its additive lobby/gameplay scene creation while the borrowed untitled scene stays open. Any new refusal there requires its own exact native evidence and bounded review; this recovery does not save/close foreign work or assert an unobserved production defect. Mirror-on/off discovery, both Unity branches, graphical acceptance and the previously recorded two-peer lobby/product evidence remain with root after every source writer finishes and a new freeze.

No Unity/MCP, compiler, test runner, product checker/validator, install, service, child agent, commit or push was launched in this recovery. The execution gate remains closed. The earlier interrupted S3 source attempt and original native failures remain failures in the record; this is source-final completion with explicit verification debt, not a test-pass receipt.

## P5 correction E02 and scene handoff dependency E03–E05 — 2026-10-25

Historical dependency checkpoint, resolved by the released continuation below.

Read the current implementation contract/request and `Local/Audits/assembly-2026-10-23/p5-source-correction-common.md`. Read the ten assigned native cases, messages and stack variants in BOTH `unity/final/p5/u63/Receipts/20-full-edit-graphics.xml` and `u66/Receipts/20-full-edit-graphics.xml`, under `Local/Audits/assembly-2026-10-23/`. Their SHA-256 values are respectively `2fc329874dff049e845eebd01635deb1f445359fd951fe82536c79cb2bb7debd` and `adeb027f9f14e1c34c8973767f9a6d3420c7c188f809ea26283ebda32d31a74b`. The frozen source identity is `956e2aa29ea85e7dea90df498a70392083af4a067058cbcbe24891105966efd1` at `unity/snapshots/final-20261025T151934Z/source`.

Actual P5 evidence: E02 reaches the successful original second-component read, then the restored binding's SourceRoot is null at line 116 in both engines. E03's three Lobby cases, E04's five MissingScripts cases and E05's SceneSaver case all reject `SaveScene` on a preview scene. These are actual failures; the earlier proposed preview-save baseline did not establish a legal native arrangement.

### E02 completed source correction

Inspected the actual `[SerializeField] private GameObject _sourceRoot` in `Scripts/NoCode/ComponentFloatBinding.cs`, its SourceRoot setter, the primary runtime BindingProbe's public inline Binding/Entry fields and the attached caller. The wrong serialization API explains the scene reference loss: Unity documents that [EditorJsonUtility.ToJson](https://docs.unity3d.com/jp/current/ScriptReference/EditorJsonUtility.ToJson.html) retains asset references but excludes scene object references, including same-scene references. [JsonUtility.ToJson](https://docs.unity3d.com/ja/current/ScriptReference/JsonUtility.ToJson.html) supports MonoBehaviours and serializes object references for restoration within the same engine session. This test keeps both native objects alive within one session, so it now uses `JsonUtility.ToJson(probe)` / `JsonUtility.FromJsonOverwrite(json, restored)`.

Changed only `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorBindingInspectorTests.cs` plus this receipt. Before serialization, the test additionally requires the probe's actual GameObject, the SerializedObject field `Binding._sourceRoot` and the public SourceRoot to equal the original root. It retains successful pre/post second-instance reads, first-isTrigger false / second-isTrigger true, exact restored root/type/member, occurrence 1 on both Condition sides, legacy indices 91 and 92, and missing-second rejection. It never reassigns restored SourceRoot or uses host fallback to disguise a lost reference. No common helper, runtime API/schema or source-root resolution policy changed. The existing case identity is retained.

Manually reviewed the source diff against the actual P5 frozen caller. Current SHA-256: `ee9a23fd4973539381acae7dd6657a16bca37dfd87ba603a532bbd03f6978968`. Same-session roundtrip execution on the corrected source remains UNEXECUTED; no cross-session JSON persistence claim is made.

### E03–E05 precise pending dependency

Primary `Neo.Editor.Tests` / `neo-audit-1023-b03-04` owns the scene seeds, shared helper and its regressions. Read `Local/Audits/assembly-2026-10-23/p5-neo-editor-tests-scene-reservations.json`; the observed status is **`authoring-reserved`**, executionAllowed false. Its observed SHA-256 is `16396370735c839759f1e90d36df43027b07762349aa2ba5394802caac61c3e4`. Published intended API:

```csharp
internal static class Audit20261023NeoEditorTestsSceneAssets
{
    internal static void CreateEmptySceneAsset(string ownedAssetPath);
    internal static void CreateMissingScriptSceneAsset(string ownedAssetPath);
    internal static UnityEditor.SceneAsset EmptySceneAsset { get; }
}
```

Await primary's completed handoff for `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsSceneAssets.cs` and its owned `SceneFixtures` assets before changing the scene callers. The helper source has appeared, but its reservation is still authoring and the shared regression source was not yet present at this checkpoint. Do not treat draft source presence as a completed handoff or native acceptance. The helper's declared contract is to copy/import a seed into a previously absent GUID-owned path without saving/opening/closing borrowed scenes; the caller owns its copied scene and asset.

Pending owned callers: `Audit20261023NeoEditorLobbyTests.cs` (sentinel baseline), `Audit20261023NeoEditorMissingScriptsTests.cs` (protected work and duplicate/slash missing-script arrangement), and `Audit20261023NeoEditorSceneSaverTests.cs` (A/B Level baselines), all under `Assets/Neoxider/Tests/Edit/Editor/`. This P5 recovery has not edited those callers before handoff. Their current SHA-256 values remain `048983d64773438d184d45df98e22522577a95868ea6ab623beb3c92281f7e1b`, `fd7043540064ffb94f0e69bb2c01eec52a22fc8daa42ce626adcb49a6a8523c9` and `9172bc58553a69a11ea835edfff57896e94ffc0444d54a4e5b3da8aad3720c4c`, respectively. They retain the known failing preview-save arrangement until the primary API is released; **E03–E05 are not complete**.

On handoff, resume the same b03-03 actor for those callers, retaining all nine case identities, no-prompt cancellation before replacement, typed persistent prefabs/presenter callbacks, duplicate/slash target identity and repair persistence, distinct same-basename backup content/bytes/dirty state, exact borrowed scene identities/order/active/loaded/dirty preservation and partial-SetUp cleanup. No borrowed scene may be saved/closed to bypass the native restriction. The primary shared seed/API and its missing-script contract must be consumed rather than racing a replacement helper or inventing another unsupported preview-save path.

No production/public/serialized contract changed; canonical public docs need no fixture-only rewrite. Historical audit/plan and earlier coherent edits remain. Actual work here: native artifact/source/API-documentation reads, the bounded binding caller patch, source diff review and hash bookkeeping. No Unity/compiler/tests/checkers/install/service/process control/other agent/commit/push execution. Writes stop at this dependency checkpoint; E02's corrected native acceptance and E03–E05 completion/acceptance remain pending. The global execution gate stays closed.

## P5 released continuation E03–E05 — source-final

Root explicitly released these callers after reading primary's completed handoff. Re-read the complete implementation contract/request and the final reservation/helper. `Local/Audits/assembly-2026-10-23/p5-neo-editor-tests-scene-reservations.json` is now `source-final`, executionAllowed false, SHA-256 `c045b7d6b1675025729e0c541a5436ba4699ae09acc464f5cc4940e1e07ded7c`. The consumed `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsSceneAssets.cs` has the published SHA-256 `affb046be1c60b7715c011ce16578ac34f5be6bdcd619bf86b8a708778096317`. Checked its ownership map before mutations. No primary helper, seed, regression, Save caller, shared reference/configuration/tracker or other owner's source was edited.

Changed only the following three owned callers and this receipt. All caller paths below are under `Assets/Neoxider/Tests/Edit/Editor/`:

| Action / retained cases | File / current SHA-256 | Completed source behavior |
|---|---|---|
| `E03` / 3 | `Audit20261023NeoEditorLobbyTests.cs` — `53bd6f88d3f9a1fa565a66917e0d40bfa0c581f1dffa4b93b5da81f63ebfeab9` | `CreateEmptySceneAsset` copies the sentinel baseline into its separate owned context folder. Open it additively, require a regular scene at the exact path with no roots, activate it, then create dirty unsaved sentinel work. Both no-prompt cancellation branches still require no destination assets; saved/reopened typed prefab, presenter/address/status/count/button and gameplay Leave oracles are unchanged. |
| `E04` / 5 | `Audit20261023NeoEditorMissingScriptsTests.cs` — `ac29c2f181963d976f7a25e5e22732fd6562690d232826f690701a00b05ae3e2` | Use `CreateEmptySceneAsset` for protected dirty work and `CreateMissingScriptSceneAsset` for every missing-script source. Require the seed's actual native missing count before arranging targets. For duplicate roots, detach one broken child from each of two owned seed copies and move the second into the first scene. Keep a healthy root first and two broken roots next, all named `Same/Name`; require counts 0/1/1, save the owned regular scene and require two missing scripts on readback. For prefab/search cases, use an owned copied seed's broken child plus a new healthy first child, both `Same/Name`, and native `PrefabUtility.SaveAsPrefabAsset`; require save success and exact native child counts 0/1 before invoking the scanner. All five test identities, repair/result counts, duplicate/slash addressing, loaded/closed persistence and search/cancel assertions remain. |
| `E05` / 1 | `Audit20261023NeoEditorSceneSaverTests.cs` — `4a19a4cf221d9012c4973eb477a85b0e62d391314e1c0ecd29eff61903ea51a4` | `CreateEmptySceneAsset` copies each A/B `Level.unity` baseline. Require a regular scene at the exact path with empty roots, register ownership immediately after opening, populate and save only that owned scene. Distinct source-GUID backup paths, exact backup root names/content, repeated-backup isolation, both source paths/bytes/dirty states and active Scene equality remain unchanged. |

Removed the caller's synthetic `InjectMissingScripts` writer, YAML regex/file edits and GlobalObjectId-based injection. No protected `m_Component` array is modified. Missing-script state comes from the primary seed and native object operations; no new shared fixture helper or runtime component was extracted. The local `RequireMissingSeedRoot` method only asserts the copied scene's one healthy root/one genuinely missing child precondition.

All three callers now snapshot borrowed Scene values in order, paths, loaded/dirty flags and exact root object arrays, plus the active Scene and preview count. Cleanup retains the original partial-SetUp catch/rethrow path, null-safe scanner cleanup, exact owned scene/folder release and active-scene restoration; Lobby also restores selection. Reset only stale owned handles before SetUp. Closing the second missing-script source uses nested finally cleanup so the first owned source is also closed if the second close faults. No borrowed scene is saved/closed/reloaded, and no global scene/editor state reset is used. Added path/root equality oracles supplement the prior identity/order/count/active/loaded/dirty assertions.

The primary seeds remain unchanged: EmptyScene SHA-256 `9cc78fe5c446d55f1cef047133203f31270cf6d1638bb72addeb6aeae5a175a5`; MissingScriptScene SHA-256 `227e8da8844a52d1204898f97f36f84fb0cc1d6ff53d3cc417d19e846879bc13`. Both are under the primary-owned `SceneFixtures/` directory. Caller `.meta` files were not edited; current SHA-256 values are Lobby `98ec969a6930b821bd7d464584c93f84839d3c1db6d843444a78f8341b991663`, MissingScripts `2fc2b738f933a453a0d0c92723ed073e15f0533e42abab925626a97f08d4196e`, SceneSaver `69a33f6590c56b9f46fc2fbbae2261e6ff54903204a84f9e3373b281b8fee30f`. E02's completed BindingInspector caller is untouched at `ee9a23fd4973539381acae7dd6657a16bca37dfd87ba603a532bbd03f6978968`.

Actual validation in this continuation was source/reference reading, manual diff review against the P5 frozen callers and SHA-256 bookkeeping. The diff leaves every original test body and its substantive oracle intact: **all nine E03–E05 case identities remain**. No public/serialized/runtime contract changed, so the existing canonical Editor guides remain current without another fixture-only rewrite. Historical audit/plan and proposals remain untouched.

**No remaining source/API dependency. SOURCE-FINAL; writes stop.** Root/native review and a new global freeze must precede execution. None of the corrected cases has been run here. Required native evidence still includes copied seed import/open with the borrowed unsaved scene intact, exact missing-component retention across moves/regular scene saves and prefab `SaveAsPrefabAsset`, duplicate/slash repair persistence, loaded-scene no-implicit-save behavior, partial-failure cleanup, Lobby generator additive creation/persistent callbacks and backup content/byte/dirty preservation on both engines. In particular, prefab save success and retained missing count are explicit preconditions, not inferred passes; a refusal must be reported as fixture/native evidence without hiding it or declaring an untested scanner defect. [Unity's SaveAsPrefabAsset contract](https://docs.unity3d.com/jp/current/ScriptReference/PrefabUtility.SaveAsPrefabAsset.html) supplies the public root/children save API, not proof that this seed conversion has passed on either engine.

No Unity/MCP, compiler, test runner, checker/validator, install, service, process-control operation, child/other agent, git command, commit or push was launched. The global source/test execution gate remains closed. Earlier native failures and the prior dependency checkpoint remain historical evidence, not success claims for the corrected snapshot.

## P6 native-confirmed prefab prerequisite and Lobby creation correction — 2026-10-26

**SOURCE-FINAL; stop writes for root/native review and a new global freeze.** Read the complete current contract/request and actual retained XML cases in both SDK hosts. The previous P5 source-final snapshot is not native completion evidence for these paths.

### Actual prior evidence

All artifacts below are under `Local/Audits/assembly-2026-10-23/unity/final/p6/`:

| Actual retained artifact | SHA-256 |
|---|---|
| `u63/Receipts/11-focused-diagnostic.xml` | `519b3a6dbbb511b796f5f07306c999a207ce1e0983e1abac5ec060d9d8098fa4` |
| `u66/Receipts/11-focused-diagnostic.xml` | `f2a3ddb908b80e9d6223bfadfc51a562858f176c1124bab1abc932b9c23f5098` |
| `u63/Receipts/20-corrected-families-edit.xml` | `7ab8b3bbb4a4cee8a460b601fed07370dc9d2c17293f3c9ec06f834c50ba31e5` |
| `u66/Receipts/20-corrected-families-edit.xml` | `e40644c808b54070bd57f6020976894adafe814378fc5ed14bd89e9da01f69e1` |

Both focused runs fail the two owned prefab/search cases at frozen `CreatePrefabWithDuplicateChildNames:285`. Read both actual outputs: Unity refuses saving a prefab containing a missing script, naming the broken `Same/Name` object. This confirms the NI76 fixture-arrangement gap; the scanner was not reached. Both runs pass the three ordinary MissingScene controls and all three primary scene-helper controls. Those passing source paths and assertions are unchanged here.

Both additional Edit XMLs pass the two Lobby cancellation cases and fail saved/reopen at frozen `LobbyDemoSceneCreator:64`, with the native untitled-unsaved additive NewScene refusal. Source review also found the second NewScene(Additive) in gameplay creation, before the new lobby had been saved. That second unsafe creation was not independently reached by the failing run; both calls are replaced in this correction.

### NI76: persistent prefab prerequisite

Added the explicitly authorized own-prefixed fixture `Tests/Edit/Editor/SceneFixtures/Audit20261023NeoEditorMissingScriptsPrefab.prefab` and its own `.meta` (GUID `42cabb436c0342fcbe9a2fe50338e2f1`). It describes a healthy root, two children both named `Same/Name`, a healthy first child and one missing MonoBehaviour slot on the second child. **Authored YAML alone does not establish native validity.**

Only `CreatePrefabWithDuplicateChildNames` changes in the MissingScripts caller. Resolve that seed by GUID, require a loadable source, copy/import it into the existing GUID-owned test folder, and require an actual persistent prefab GameObject at the copied path. Before scanning, assert a nonempty copied GUID different from the seed, prefab-asset identity, root missing count zero, exactly two same-named children and native child counts **0/1**, plus unchanged selection. There is no missing-root SaveAsPrefabAsset call, protected component-array mutation, regex/asset rewrite or expected-error whitelist. The seed remains outside the scanner's addressed target; cleanup deletes only the copied owned input through the existing fixture teardown.

Both original case identities and bodies remain: repair must persist zero missing scripts while retaining both duplicate/slash-named children; search must retain exact restart counts and cancel/stop at zero. The other three scene cases, their seed arrangement, borrowed scene/active/dirty/path/order/root assertions and partial-SetUp cleanup are untouched. No primary helper/API/seed/reservation was edited.

### EDITOR-LOBBY-PERSISTENCE: actual production correction

Added package-owned `Editor/Network/LobbyDemoSceneTemplate.unity` and `.meta`, GUID `6ba1121cb747442992b1c286f8a7d3fa`. Production resolves this asset by its own GUID, with no Tests, Samples or preview-save dependency. Copy/import the template into a previously absent lobby destination before opening it as a regular scene. Gameplay similarly copies into the fresh generated asset folder before additive opening. Both scenes have persistent paths before population; the creator adds a camera/listener and directional light, then the existing content. No NewScene call remains.

The public menu entry and original three-argument internal method/default Single behavior are preserved. Save/continue and destination cancellation still precede creation/replacement. Single opens the new persistent asset with the authorized Single replacement; Additive keeps borrowed scenes open. The creator now explicitly refuses any existing destination file, metadata, directory or imported asset before mutation; the menu asks for an unused `.unity` path inside Assets. This safe fresh-destination requirement is documented in the canonical English/Russian EditorWindows guide. Filesystem preflight uses the actual Unity project root from Application.dataPath.

Failure cleanup holds the exact created Scene values, with GUID-scoped path fallback for an interrupted open. It releases owned scenes, deletes the newly copied lobby and generated folder while their GUIDs match, and restores the prior active scene/selection while those objects remain valid. Template import failure also releases its own copied asset. Single replacement retains its authorized discard semantics; cleanup does not recreate discarded unsaved work. No unrelated scene is implicitly saved, closed or reloaded to satisfy an additive precondition.

The original typed room/game prefabs, Room/offline/online/gameplay paths, four persistent lobby controls, address/discovery/status/count callbacks, gameplay Leave callback and final manager selection remain in the preserved population code. No Network/runtime/public serialized API changed.

Published internal regression seam, preserving the existing overload:

```csharp
internal static bool TryCreateLobbyDemoScene(
    Func<bool> saveCurrentScenes, Func<string> chooseScenePath,
    NewSceneMode sceneMode, Action<Scene> beforePopulation);
```

The callback is an addressed fault-injection point after opening/activating the owned persistent lobby. Two new source smoke cases require existing-destination rejection without changing bytes/GUID/context, and an exact injected population failure after real asset/scene creation followed by owned-only cleanup and active/selection/source-byte restoration. The original three Lobby cases remain; saved/reopen also now checks sentinel source bytes and exact root identities. This is authoring resource-ownership coverage, not manual runtime lifecycle or networking evidence.

### Current exact files and hashes

Paths in this table are relative to `Assets/Neoxider/`; these eight files plus this receipt are the complete P6 mutation scope:

| File | SHA-256 |
|---|---|
| `Editor/Network/LobbyDemoSceneCreator.cs` | `3d0bdc75230f739c76afa84c955c30778d50ce208059d08e7131fc6924b0e92e` |
| `Editor/Network/LobbyDemoSceneTemplate.unity` | `9cc78fe5c446d55f1cef047133203f31270cf6d1638bb72addeb6aeae5a175a5` |
| `Editor/Network/LobbyDemoSceneTemplate.unity.meta` | `89ffde806cb5db18e14378d28ec51a1d144601e0fc8b8a0d83aac512f9f5b763` |
| `Tests/Edit/Editor/Audit20261023NeoEditorMissingScriptsTests.cs` | `45bcb56df2203bfed131a224f1e924494a9838ea3742611589e73bb64c52c483` |
| `Tests/Edit/Editor/Audit20261023NeoEditorLobbyTests.cs` | `a289b4202eaddf434a67543074292380173ceeaa0e38d2086916be45529d8f14` |
| `Tests/Edit/Editor/SceneFixtures/Audit20261023NeoEditorMissingScriptsPrefab.prefab` | `455ebbd90997329cdfdcc834db1c2b092e4cf7d1ea0156b176acf60cc9c73fb0` |
| `Tests/Edit/Editor/SceneFixtures/Audit20261023NeoEditorMissingScriptsPrefab.prefab.meta` | `46e44613f225d1ed9c8d3cca168b8090f63bf172a866e446f6f8449d8ce751af` |
| `Docs/Editor/EditorWindows.md` | `6aea84231216c2f4dc5d05cca4de27447965173300f7880709bdd45337291595` |

Existing source/asset GUIDs and `.meta` files were preserved. BindingInspector remains `ee9a23fd4973539381acae7dd6657a16bca37dfd87ba603a532bbd03f6978968`; SceneSaver remains `4a19a4cf221d9012c4973eb477a85b0e62d391314e1c0ecd29eff61903ea51a4`. Shared configuration, assembly references, reservations, contracts, trackers, changelog/migration and other owners' files are unchanged by this actor. Historical audit/plan and optional substantial proposals remain unchanged.

### Actual validation and pending acceptance

Performed current contract/request reads, actual both-SDK failure/passed-control artifact reads, source/reference reads, manual diff review against the P6 retained sources and file-byte SHA-256 bookkeeping. The MissingScripts diff is limited to the two prefab prerequisites' shared private method; the three passing scene controls are unchanged. All five MissingScripts case identities and original three Lobby cases/oracles remain. New smoke source and added positive assertions have **not** executed.

**No source/API dependency remains. Native import and acceptance are PENDING after root/native peer review, all writers finishing and a new freeze.** Both SDKs must import the new persistent prefab and prove the actual 0/1 native prerequisite, then run all five MissingScripts cases, including repaired-copy persisted zero and exact search/cancel counts. Both must compile/import the Editor template and exercise additive Lobby saved/reopen plus the two cancellation and two new failure/overwrite cases, retaining all borrowed scene/source-byte/dirty/root/order/selection checks. Default Single plus generated gameplay also requires the selected later manual smoke in the isolated validation host after explicit save/continue, followed by the previously recorded independent-peer product evidence. No new seed or template is called native-valid from YAML/hash/source inspection.

No Unity/MCP, compiler, tests, product checker, installer, service, process control, child/other agent, git command, commit or push was launched. The source/test execution gate remains globally closed. Prior native passes describe the P6 frozen controls only; prior native failures remain failures. This receipt reports completed source corrections and pending acceptance.


## Root integration amendment — idempotent scene activation

After the original author stopped, Root strengthened the two activation guards: only call SetActiveScene when OpenScene has not already selected that exact owned Scene. Existing valid active state is accepted without relying on the return value of an idempotent switch; a required rejected switch still throws. This applies to both lobby and gameplay creation and matches the previously reviewed Save fixture activation rule. Only these two conditions changed; the current creator pin above supersedes original-author delivery d6e299bcbff11784a1f3f9c08f72cdaabb2d8c70b6bcaa94acdd680f6798605f. NI80 independently reviews this amendment before the new freeze. Native acceptance remains pending.

## P7 Edit callback prerequisite correction — 2026-10-26

**SOURCE-FINAL; writes stop.** Completed the released `EDITOR-LOBBY-PERSISTENCE` / E03 fixture correction only. Read the complete current implementation contract/request, both retained P7 failure cases and relevant current owned source. The optional `p7-lobby-callback-context-assessment.md` was not present at review time. Checked both primary reservation maps before edits; no reserved/shared helper was changed.

### Actual frozen evidence

Artifacts are under `Local/Audits/assembly-2026-10-23/unity/final/p7/`:

| Retained XML | SHA-256 | Actual result |
|---|---|---|
| `u63/Receipts/10-changed-first.xml` | `6a4090bf64b0c84f85a8201117144ea63174ced3578f93a1a7cbe4c6a6470d57` | 134 total, 133 passed, 1 failed |
| `u66/Receipts/10-changed-first.xml` | `74524147ac80a07ca64f666a20bc3b8815688ca9fb373625b884893e990243d6` | 134 total, 133 passed, 1 failed |

The sole failure on both engines is `SavedAndReopenedLobby_RetainsTypedPrefabReferencesAndPresenterCallbacks`, frozen fixture line 276: expected address `10.0.0.5`, actual `localhost` after the public input event invocation. Creation, save/reopen, typed prefab references and all four button target/method assertions preceding that invocation passed. Count/status invocations and later gameplay assertions were not reached; no pass is inferred for them.

The other 133 actual passes comprise Lobby cancellation x2, existing-destination rejection and population-failure cleanup (4), MissingScripts (5), View (7), Save (6), FakeLeaderboard serialization (60), SetText (33) and TimeToText (18). Those results qualify the retained P7 snapshot only. Earlier 21 failures and subsequent P6 evidence remain historical records; this source correction does not turn any recorded failure into a pass.

### Completed bounded source change

Only `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorLobbyTests.cs` and this receipt changed in this continuation. The generator's persistent listeners retain their RuntimeOnly production policy. The ordinary Edit test cannot use those callbacks as though Application were playing.

Before any temporary policy change, the fixture now requires exactly one saved listener for each of the three owned loaded string events, its actual RuntimeOnly state, and its exact target/method: input address to the reopened presenter / `SetAddress`; player-count text to the independently located `ConnectionPanel/PlayerCountText` SetText / `Set`; status to `ConnectionPanel/StatusText` SetText / `Set`. Missing targets, wrong methods or different saved states fail the precondition rather than being replaced or repaired by the fixture.

The existing public UnityEvent invocations run inside a try/finally scope that temporarily sets only these three listeners to EditorAndRuntime using public `SetPersistentListenerState`. Exact payload/output assertions remain `10.0.0.5`, `Players: 7` and `Restored status`. Finally restores each captured original state without saving. Afterward, the fixture asserts restored states, listener counts and exact targets/methods. This is **Edit serialized wiring and adapter coverage**, not genuine Play delivery of RuntimeOnly listeners.

Manual diff review against the retained P7 fixture shows only the Events using directive and this callback block changed. All five Lobby identities, five MissingScripts identities, main/gameplay typed references, all four buttons, gameplay Leave callback, borrowed scene identity/order/open/active/dirty/root/source-byte/selection cleanup, and destination/fault oracles remain. Production, runtime, templates, guides, missing-prefab assets, all metadata and primary helper/framework files were not edited. Root's two idempotent activation guards and its amendment above remain unchanged. No public or serialized API or authoring contract changed.

### Current pins and remaining acceptance

| File | SHA-256 |
|---|---|
| Lobby fixture, retained P7 source before correction | `a289b4202eaddf434a67543074292380173ceeaa0e38d2086916be45529d8f14` |
| `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorLobbyTests.cs`, corrected source | `d5eac55b0282e9680353082038479a603f223c6ced8b8a9fa2d7cfb97b6bbf84` |
| Its unchanged `.cs.meta` | `98ec969a6930b821bd7d464584c93f84839d3c1db6d843444a78f8341b991663` |
| Unchanged `Assets/Neoxider/Editor/Network/LobbyDemoSceneCreator.cs`, including Root guards | `3d0bdc75230f739c76afa84c955c30778d50ce208059d08e7131fc6924b0e92e` |
| Unchanged `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorMissingScriptsTests.cs` | `45bcb56df2203bfed131a224f1e924494a9838ea3742611589e73bb64c52c483` |

Actual validation here consists of retained XML reads, source/API reads, manual source diff review and file-byte SHA-256 bookkeeping. **No corrected test was executed. No source/API dependency remains; native acceptance is PENDING.** Root/native-B review and a fresh global freeze must precede all five Lobby cases on both SDKs; only then may root broaden execution to the full suites. Genuine RuntimeOnly Play delivery, default-Single/gameplay manual smoke and the previously recorded networking/product evidence remain separate native debt.

No Unity/MCP, compiler, tests, product checker, process control, install, service, children, git, commit or push was launched. Source/test execution remains globally closed. Stop writes for review and new frozen evidence.
