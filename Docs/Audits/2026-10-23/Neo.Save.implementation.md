# Neo.Save — implementation receipt

Status: **SOURCE-FINAL FOR BOUNDED FIRST-CHARACTER REGRESSION DELIVERY** (2026-10-26); target `demo` / `11.0.0`; original actor `neo-audit-1023-b05-07`. Two new first-character root-array cases are **UNEXECUTED**; the prior four canonical/middle-character cases have the narrowly scoped p6 PASS evidence recorded below. No source dependency remains; the new native verdict is pending. Runtime, public docs/API and the original ten scene callers remain unchanged in this continuation.

## Published additive API

Assigned action: **SAVE-ENCRYPTION-CONFIG (P1)**.

```csharp
public bool TryCreateProvider(out ISaveProvider provider, out string error);
```

Declared on `Neo.Save.SaveProviderSettings`. The method validates required file encryption before constructing a file provider. Invalid configuration returns `false`, a null provider, and a descriptive error containing no key/IV material. Successful construction returns `true` and a null error. Existing `CreateProvider()` remains and throws `InvalidOperationException` for invalid enabled encryption. No consumer assembly needs a new dependency or signature migration; callers requiring recoverable validation can opt into the new method.

## Completed action and exact files

**SAVE-ENCRYPTION-CONFIG — implemented in source.**

| File | Final change |
| --- | --- |
| `Assets/Neoxider/Scripts/Save/Settings/SaveProviderSettings.cs` | Added the result-bearing factory, reusing `FileSaveEncryptionConfig.TryCreate`; rejected invalid required encryption before `FileSaveProvider` construction; legacy factory throws the returned descriptive configuration error. Explicit local types. |
| `Assets/Neoxider/Scripts/Save/SaveProvider.cs` | Creates settings/provider candidates locally; publishes `_settings`, `_provider`, event forwarding, lifecycle hooks and finally `_isInitialized` only after construction succeeds. A failed lazy attempt remains retryable. Retained the earlier failure-safe slot delegation. |
| `Assets/Neoxider/Scripts/Save/Settings/SaveProviderSettingsComponent.cs` | Invalid assigned settings return before `SetProvider`, preserving the active provider and its event forwarding. Error uses existing debug-gated logging. Corrected the unset-settings tooltip/log to describe normal current/Resources/default selection. |
| `Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveConfigurationTests.cs` | New focused regression source: five methods / ten parameter-expanded cases, detailed below. |
| `Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveConfigurationTests.cs.meta` | Added source metadata, GUID `9cf475ca81924dfdafac284ea602073b`. |
| `Assets/Neoxider/Docs/Save/Settings/SaveProviderSettings.md` | Asset setup, serialized configuration, validation/failure/retry contract, typed C# activation example, format and result limitations, Russian summary. |
| `Assets/Neoxider/Docs/Save/SaveProviderSettingsComponent.md` | Scene bootstrap order, retention on failure, gated diagnostics, explicit retry, correct module backlink, Russian summary. |
| `Assets/Neoxider/Docs/Save/SaveProvider.md` | Lazy failure/retry behavior, scene fallback selection, deliberate 11.0 error behavior, settings backlinks. |
| `Assets/Neoxider/Docs/Save/README.md` | Canonical entry includes rejection/retry behavior and both settings-guide links. |
| `Docs/Audits/2026-10-23/Neo.Save.implementation.md` | Early API publication followed by this source-final/dependency receipt. |

Earlier coherent FileSaveProvider/SaveManager fixes, their prefixed regression sources and public guides remain in place. Original audit and plan remain historical artifacts. No legacy/shared fixture, asmdef, consumer assembly, shared tracker/configuration, package metadata or sample asset was edited by this implementation.

## Authored observable acceptance — UNEXECUTED

All cases are in `Audit20261023NeoSaveConfigurationTests.cs`:

1. **Four invalid configurations:** invalid key size, invalid IV size, key-only and IV-only. Both factories reject; the Try result has no provider and a descriptive error containing neither supplied secret; legacy factory throws that error. Existing encrypted primary and backup remain byte-identical. A `.tmp` marker also remains identical: `FileSaveProvider.Load()` would delete it, so this checks rejection before provider construction/file access. After correction, the profile restores `coins=20`, saves `30` encrypted, and rotates the previous primary bytes into backup.
2. **Three compatible File configurations:** encryption disabled with invalid unused strings, encryption enabled with both strings empty, and valid custom encryption. Disk round trips preserve `coins=7`; expected plaintext or decryptable encrypted payload is asserted.
3. **PlayerPrefs:** invalid unused File-encryption fields do not reject PlayerPrefs or open a profile file. No PlayerPrefs values are written.
4. **Scene adapter failure and retry:** directly dispatches the real private `Awake` callback on an inactive owned object with assigned settings. Invalid configuration emits the expected debug-gated error, preserves the active provider/pending values/event forwarding and all three target files. Re-dispatch after correction loads `20`; successful replacement detaches old-provider events and forwards new-provider changes once, then persists `30`. This is callback dispatch evidence to be executed, not native Awake-order coverage.
5. **Actual lazy Resources path:** temporarily creates a GUID-owned `Assets/Audit20261023NeoSaveConfig_<guid>/Resources/SaveProviderSettings.asset`. Two consecutive public `CurrentProvider` reads must both throw while invalid, leaving primary/backup/staging bytes unchanged. Correcting the loaded settings makes the next public access restore `20`, retain the same initialized instance on further access, and persist `30` with the expected backup. Private reflection only prepares uninitialized facade state while retaining public listeners/lifecycle-hook ownership; creation/retry use the public facade.

Isolation: the fixture requires the coordinator's exclusive isolated validation host, with no pre-existing Resources asset at that key. It uses a GUID temporary filesystem root and its own temporary asset folder; restores the prior provider, settings reference and logging flag; removes its own listener/object/assets/files. It does not clear global listeners or SaveManager registries. No unavailable NUnit serialization attribute is introduced. Temporary Resource assets exist only when the future fixture executes; none were created during this source phase.

## Compatibility, reuse and dependencies

Preserved serialized field names/defaults, enum values/fallback, `ISaveProvider`, legacy factory/facade signatures, valid custom encryption, empty-pair built-in defaults, disabled encryption, PlayerPrefs selection and AES/Base64/JSON formats. Reused the existing encryption validator, provider constructors, event-transfer logic and debug-gated diagnostics; no framework, new assembly or runtime scene scan.

Intentional 11.0 migration for root's release guide: invalid **enabled File encryption** now makes `CreateProvider()` and first lazy facade access throw `InvalidOperationException` instead of opening plaintext storage. `TryCreateProvider` is the recoverable path. Invalid assigned scene settings retain the current provider; a component runs once in `Awake`, so editing its asset does not itself trigger a runtime retry. A caller may create/activate again explicitly. Provider replacement still does not implicitly flush old pending values or refresh scene/GlobalSave caches.

**Cross-owner dependency: none pending.** Existing `Neo.Settings`, `Neo.Shop`, `Neo.Progression` and `Neo.Quest` consumers keep their signatures; owners can adopt the additive factory without a new reference. Root owns migration/changelog integration and the later coordinated validation. `Neo.Editor.Tests` owns shared/legacy fixtures and test assembly configuration; this change uses its existing references only.

The result/session/transaction design in the original plan remains **proposed, unimplemented**. This bounded correction validates pair presence/byte lengths; a correctly sized but wrong key, corrupted storage recovery/reset policy, write durability, GlobalSave cache generations and arbitrary provider rollback are not solved by this factory.

## Actual inspection and remaining evidence debt

Performed source reads, reference enumeration, branch/status bookkeeping (`demo`), and manual review of the scoped runtime/documentation diff and regression source/control flow. The retained earlier changes were preserved. No compiler, NUnit/Unity runner, Unity/MCP, product checker/validator, service/install, commit/push or child agent was launched. Historical audit checks are not passes for this source snapshot.

**All new acceptance is UNEXECUTED.** After every writer finishes, root must compile/discover/run the three owned prefixed Save fixtures (historical 22 cases plus these ten), the existing Save suite and integrated consumers on the frozen Unity 6.3/6.6 hosts. Check temporary Resources asset import/cleanup, exact static-state restoration and unexpected logs. Native scene/Awake/sceneLoaded ordering, domain-reload-disabled sessions, JsonUtility malformed containers, filesystem `File.Replace` fallback, device interruption and IL2CPP reflection/encryption remain integration/platform evidence debt. Package UTF-8/docs-link/metadata/GUID checks are also deferred to the coordinated validation phase.

## Source3 follow-up: required root items ARRAY admission — source-final

Assigned by the appended **Snapshot3 native JSON required array admission** section of this actor's implementation request. This is an additional bounded correction to the earlier structural-recovery fix; the encryption correction and prior coherent edits remain intact.

### Native evidence and diagnosis

Root reports two actual failures in `StructurallyDamagedMainFile_RestoresCompleteBackup`: `{}` and `{"items":null}` returned `-1` instead of the required backup value `10`. Evidence supplied by root: `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/actual22-full-edit-graphics.xml`, SHA prefix `7c6d24e1...`, historical assertion line 99. This worker did not rerun or independently rehash that artifact.

Current `SaveData.items` already has **no initializer**, and `Save()` explicitly constructs the list when writing. Removing its initializer did not establish wire presence: native `JsonUtility.FromJson<SaveData>` materialized an empty list for missing/null collection input. Consequently, `saveData?.items != null` admitted an incomplete primary as an intentional empty save and bypassed the complete backup. The historical source-only assumption was insufficient; native failure evidence establishes the remaining defect.

### Exact source changes

Only these four files were changed in this follow-up:

| File | Correction |
| --- | --- |
| `Assets/Neoxider/Scripts/Save/Providers/FileSaveProvider.cs` | Added a private structural admission step before `JsonUtility.FromJson`: require an object with one root `items` array, reject missing/null/non-array/duplicate root members and incomplete root/container boundaries. Walk root member tokens, balance nested containers iteratively, consume quoted strings/escapes and decode Unicode escapes in names. Nested keys and text never satisfy the root requirement. Typed values and DTO mapping still use the existing JsonUtility path; existing per-record validation, whole-primary rejection, backup selection and recovery commit behavior remain. Both plaintext and decrypted JSON reach the same admission step. |
| `Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveFileProviderTests.cs` | Retained all original negative controls and the exact `10` expectation, plus the unchanged `ValidEmptyProfile_DoesNotRestoreOlderBackup` control. Expanded damaged-primary cases and added a focused plain/encrypted missing/null/explicit-empty matrix; details below. No MonoBehaviour/helper class, shared reference or fixture move. |
| `Assets/Neoxider/Docs/Save/FileSaveProvider.md` | Documents required root array admission, missing/null DTO normalization, deliberate empty profiles, duplicate ambiguity, plain/encrypted parity and unchanged wire format. |
| `Docs/Audits/2026-10-23/Neo.Save.implementation.md` | Preserves the earlier receipt and records the native failure, bounded correction, authored acceptance and remaining dependency boundary. |

**Public/additive API changes: none in this follow-up.** Existing `items`, `key`, `value`, `type` wire names, serialized settings, constructor and public provider APIs are unchanged. Unknown root metadata remains allowed. Duplicate root `items` members are deliberately rejected to avoid serializer-dependent first/last-member interpretation; the existing writer emits one member. No optional UI assembly, JSON package, asmdef or general-purpose JSON framework was added. Reused `JsonUtility` for typed deserialization and the existing encryption/backup pipeline; no suitable independent parser exists in Save's current mandatory dependencies. The admission step is private to FileSaveProvider and uses token boundaries rather than substring/regex searches for a key.

### Authored acceptance after the native failures — UNEXECUTED

- Expanded `StructurallyDamagedMainFile_RestoresCompleteBackup` from five to **19** input cases. The original `{}`, `items:null`, null record/container and missing type cases are retained. Added wrong token types, nested-object/array lookalikes, a quoted lookalike, case mismatch, duplicate root members, truncated/trailing root input and a valid record followed by an invalid record. Every case still requires backup `coins=10`; new assertions reject leaked `main-only` records and verify primary/backup bytes are unchanged by loading.
- Added **12** `RootArrayAdmission_DistinguishesMissingNullAndExplicitEmpty_InPlainAndEncryptedFiles` cases: missing, null and explicit empty root collections, valid empty arrays alongside misleading nested/text content, and an escaped root member name, each in plaintext and encrypted storage. Missing/null must restore `10`; explicit empty must remain empty. Every load leaves primary/backup bytes unchanged. A subsequent valid save must round-trip `30`; after backup recovery that first commit must retain the original good backup bytes.
- The existing explicit-empty profile, ordinary disk round trips, empty keys, first-save-after-recovery backup preservation and failed-slot controls remain present and unweakened. This adds **26 parameter-expanded cases** to the owned FileProvider fixture; none was executed by this worker after the correction.

Actual work in this follow-up: reread the complete current implementation contract/request, current DTO/admission/recovery source, owned helper-free FileProvider fixture/guide and existing receipt; enumerated existing JSON facilities/dependencies; manually traced token/backup control flow and reviewed the scoped source/doc diff. No compiler, Unity/MCP, test runner, product checker, installation/service, child agent, commit or push was launched. The earlier source-phase acceptance paragraph above describes its then-pending state; the source3 failure report here is later native evidence, and **all post-correction execution remains pending**.

### Ownership and handoff

This required-array correction is **source-final, with no pending implementation dependency**. Root must native-review it, freeze only after all writers finish, and rerun the original failing cases plus the expanded helper-free FileProvider fixture on the final supported hosts. No pass is inferred from the authored cases or manual inspection.

Primary `b03-04` retains runtime test-helper relocation, new Fixtures/shared references and caller/scene reservations recorded in `runtime-fixture-relocation-reservations.json`. No Save Mono helper, reserved `SaveManagerTests` scene/caller fixture, shared asmdef, tracker or vendor314 migration target was changed. The separate additive-scene admission follow-up is **deferred until that primary release**, per root's scope instruction; it does not block this FileSaveProvider handoff. Original audit/plan remain historical and unchanged.

## Released caller: SAVE-POST-RELOCATION-SCENE-CALLER — source-final

Root subsequently released this original caller after primary `b03-04` declared its 54 helper relocations source-final. This closes the source dependency described in the preceding historical section. Helper files under `Tests/Fixtures`, their aliases/type/GUID mapping, shared asmdefs/references and extraction remain primary-exclusive and were not edited or recreated here.

### Actual source3 evidence read before patching

Read the actual artifact `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml` (the filename has no `actual` prefix). Its action index records SHA256 `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`; this worker did not independently rehash it. The Save scene case at XML line 19642 reports `System.InvalidOperationException: Cannot create a new scene additively with an untitled scene unsaved`, with the frozen caller at line 201. The four incoming/resident assertions were not reached. This is native scene-arrangement failure evidence, not proof of a runtime restore defect or a successful rerun.

Read the current caller and its extracted `TestSaveable`/`SceneCallbackHost` bodies, current SaveManager discovery/callback/cache contracts and singleton ownership code, the primary reservation mapping, and the existing Editor-owned `Audit20261023NeoEditorTestsPreviewSceneScope` plus its scene-asset creation pattern. The moved aliases remain intact.

### Exact changes and preserved oracles

Only `Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveManagerTests.cs` and this receipt changed in this released-caller follow-up. **No production/API/friend/serialized-settings or FileSaveProvider change.** No new shared helper, reference, metadata, scene asset, package layout or protected DocsEn file was created/changed during this source phase.

- Reuse the existing Editor-owned preview scope to save **only an owned preview scene as a copy** into a GUID-owned temporary asset folder, then open that saved asset additively. Both resident and incoming contexts have explicit owned paths; there is no additive `NewScene`, foreign-scene save/close or global scene reset. Every fixture-created GameObject starts in the owned resident scene, so arranging the other tests no longer dirties a borrowed active scene either.
- Capture borrowed scene order/handles, loaded/dirty flags, root-object references, active scene and preview-scene count before arrangement. Capture the existing manager reference, `IsLoad`, registration entries with their exact component/FieldInfo-list references, provider and logging state. Restore registry state rather than leaving the earlier blanket cache clear behind. Reflection for this capture/restore is confined to the caller; it does not add a production seam or invoke lifecycle to conceal a precondition.
- Record each owned path before native `OpenScene`, and handle setup exceptions with immediate teardown. Cleanup attempts object destruction, closing only exact owned scene paths, active-scene restoration, owned asset/folder removal, registry/IsLoad restoration, provider/log restoration and temporary-file deletion even if an earlier operation fails. Cleanup failures are aggregated and fail the test; they are not whitelisted. Final assertions require the original borrowed scene/root/dirty/active/manager/registry/provider state, including component/list reference identity.
- Retain the same substantive scene-filter callback assertions: incoming value **88**, incoming loaded count **1**, resident value **20**, resident loaded count **0**. The incoming object is saved/registered before dispatch, and the callback still runs the production discovery/filter/load path. Add exactly **two owned registration entries** before and after, distinct/stable identity keys and same-instance ownership, identical provider/payload, and **zero key-change/save/load provider events during dispatch**. All existing duplicate, inheritance, malformed-data and callback-count cases remain present with their prior expected values.

The registration count is scoped to the two owned instances so unrelated borrowed registrations do not become a new fixture precondition. Full original registration counts and entry identities are separately restored/asserted during cleanup. No expected-error acceptance, Ignore, ExecuteAlways, manual Update or weakened restore/count oracle was added.

### Coverage boundary and deferred closure

The existing case now has an explicit NUnit Description/comment identifying it as **EditMode dispatch of the production scene-filter callback**. Its private callback invocation is an intentional filtering regression after public registration; it is **not native sceneLoaded emission, native OnEnable registration timing, or Play lifecycle acceptance**, and is not represented as such. Other public SaveManager contract cases still use the public C# API with the extracted runtime fixture types.

**Native closure remains deferred:** root must review/freeze after every writer finishes and run the corrected caller on the supported hosts, including an untitled/dirty borrowed baseline and cleanup-state assertions. Real OnEnable-to-sceneLoaded ordering still needs a supported top-level Play transition and actual native scene loading; this callback-dispatch case cannot substitute for that acceptance. Setup/partial-failure cleanup and borrowed manager/scene preservation are authored assertions/control flow, not measured post-change outcomes.

Actual work here was source/artifact reading, scoped source diff inspection and manual control-flow review. No Unity/MCP, compiler, test runner, product checker/validator, service, install, child agent, commit or push was launched. **The proposed caller correction and its additional assertions are UNEXECUTED.** The existing Editor preview scope is consumed without changes; no Editor-owner API or primary helper move is pending. Original audit/plan, all historical receipts and the independently reviewed FileSaveProvider correction remain preserved.

## P5 E11: persistent scene admission for all ten cases — source-final

This later native evidence supersedes the preceding preview-save arrangement; the earlier source-final entry was not a native pass. Read the complete current implementation contract/request and `Local/Audits/assembly-2026-10-23/p5-source-correction-common.md`. The p5 source identity is `956e2aa29ea85e7dea90df498a70392083af4a067058cbcbe24891105966efd1`, under `unity/snapshots/final-20261025T151934Z/source`.

### Both actual failures retained

Read all ten SaveManager case messages in `Local/Audits/assembly-2026-10-23/unity/final/p5/u63/Receipts/20-full-edit-graphics.xml`, including the aggregate's complete setup/cleanup stack. The fixture reports **10 total / 0 passed / 10 failed**. Its test bodies were not admitted:

1. `OpenOwnedScene` line **375** invokes `EditorSceneManager.SaveScene` on a preview scene. Native throws `ArgumentException: EditorSceneManager.SetActive failed; you can not save a preview scene`. Saving a preview as a copy was an invalid arrangement; the previous source assumption is disproved.
2. Partial-SetUp cleanup then fails at line **125**, asserting `SetActiveScene(_previousActiveScene)` returned true when the actual result was **false**. `SetUp` line 86 correctly retained both failures in `Save fixture setup and cleanup failed`; neither failure is suppressed or reclassified as a pass here. The XML does not establish a general cause for that false return.

No product Save/Load defect is inferred from these SetUp failures. The existing scene test's XML Description also explicitly identifies private callback dispatch rather than native sceneLoaded/OnEnable timing; this boundary remains unchanged.

### Shared API coordination and exact owned patch

Before editing the caller, read primary's published `Local/Audits/assembly-2026-10-23/p5-neo-editor-tests-scene-reservations.json` and `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsSceneAssets.cs`. That contract lists **Neo.Save E11** as a consumer, excludes this caller from primary's edits, and publishes:

```csharp
internal static void CreateEmptySceneAsset(string ownedAssetPath);
```

The containing type is `Neo.Editor.Tests.Audit20261023NeoEditorTestsSceneAssets`. It copies the primary-owned persistent empty-scene seed to a fresh caller-owned path and requires an imported SceneAsset, without saving/opening/closing a scene or modifying borrowed context. The authored API, seed and metadata files were present at inspection; their native import acceptance remains pending. No guessed alternative preview-save API is used.

Changed files in this p5 continuation:

- `Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveManagerTests.cs`: `OpenOwnedScene` calls the published `CreateEmptySceneAsset(path)`, retains the native SceneAsset assertion and path recording before additive OpenScene, and removes the preview scope/SaveScene call. A private `ActivateScene(Scene)` requires a valid loaded target, requests a switch only if the active identity differs, still requires true for an attempted switch, and always asserts the exact resulting active scene. SetUp, object creation and cleanup use the same rule. If cleanup is already in the captured original scene, it verifies that state without an unnecessary native switch request; a failed required switch still fails the test.
- `Docs/Audits/2026-10-23/Neo.Save.implementation.md`: preserves both native failures and historical source receipts, records this bounded replacement and its shared/native dependency.

Final owned caller SHA256 (actual `certutil -hashfile` bookkeeping): **`afd41cff2da911fecce1dd3a3327f27fb57c89f471508c012491ef925238c8f5`**.

No shared helper/seed/asmdef/reference/GUID, another owner's caller, product runtime/API/settings, FileSaveProvider, sample layout, protected DocsEn metadata or vendor314 migration target was edited. Primary alone owns its reserved seed/API/test files.

### Unchanged strict controls and remaining acceptance

Reviewed the source diff directly against the frozen p5 caller: only scene creation/activation arrangement changes; **all ten case bodies, identities, variants and expected values are retained**. This includes duplicate-registration rejection/owner preservation, unchanged stored payload, private/base **37/12** round trips in both save modes, malformed-record last-entry **29** with other value **5** and loaded count **1**, repaired value **44** with count **1**, and already-registered incoming **88/1** versus resident **20/0**. Two owned registration entries, stable distinct identity keys, unchanged provider/payload and zero provider events during the private callback remain exact.

Exact provider, manager, registry component and FieldInfo-list references are still captured/restored/asserted. Borrowed scene order/handles, open/loaded/dirty flags, root-object references, active identity and preview count remain strict. Cleanup still attempts every owned resource and state restoration after partial SetUp failure, aggregates actual cleanup failures, and never saves/closes a foreign scene or resets global scene setup. No new Ignore, expected-error whitelist, relaxed count, fake native lifecycle claim or skipped case was added.

**Source-final for Save; shared/native dependency:** root must include/finalize primary b03-04's published SceneAssets API and its persistent seed/metadata under the reservation contract before the next freeze. The reservation was `authoring-reserved` at coordination; Save does not claim primary's source-final or native-valid result. No additional Save patch is required if that published API remains unchanged.

After every writer finishes and root freezes again, rerun all ten E11 cases on 6.3/6.6, including strict borrowed unsaved/dirty/active-state and partial-failure cleanup observations. The scene case remains **private callback scope only**; real native OnEnable-to-sceneLoaded timing is still separate deferred acceptance. The scene seed import, legal additive opening, required active-scene transitions, cleanup and Save/Load bodies are **UNEXECUTED after this patch**.

Actual work: existing native artifact/source/reservation reads, manual frozen-source diff review and SHA256 bookkeeping only. No Unity/MCP, compiler, test runner, product checker, service/install, process control, other agent, commit or push was launched. Historical audit/plan and all prior failure artifacts remain unchanged.

## P5 nonempty escaped-root regression delivery — SOURCE-FINAL

**SOURCE-FINAL FOR BOUNDED REGRESSION DELIVERY; new decode acceptance UNEXECUTED.** This continuation delivers diagnostic regression source only. Save-specific escaped nonempty decoding/silent loss remains **UNPROVEN**; no runtime fix or native outcome is inferred.

Read the complete current implementation contract/request and `Local/Audits/assembly-2026-10-23/p5-save-escaped-nonempty-assessment.md`. Actual assessment SHA256: `b7ee70eacb279037443ba153635689cfcaed351c5f774844efdbadf583864285`. Its distinction is retained: prior escaped **empty-array** success cannot establish nonempty binding, and the FakeLeaderboard rejection does not establish Save's decoding stage or outcome.

Only these two files changed in this delivery:

- `Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveFileProviderTests.cs`: one small method beside `RootArrayAdmission`, `NonemptyRootArray_PreservesEscapedAndCanonicalPrimaryOverBackup(bool escapedRoot, bool encrypt)`, with **four** cases: `(false,false)`, `(true,false)`, `(false,true)`, `(true,true)`.
- `Docs/Audits/2026-10-23/Neo.Save.implementation.md`: this receipt/status update; historical entries remain intact.

The cases use independent literal JSON inputs, not a product serializer or private DTO mirror. Canonical root `items` and literal escaped root `it\u0065ms` carry identical nonempty entries: **coins20 + primary-marker=alive**. The distinct canonical backup contains **coins10 + backup-marker=kept**. The C# escaped literal retains a backslash-u in the on-disk JSON; it is not compiler-decoded first. Existing encryption/configuration APIs encode each literal independently for encrypted variants, and the existing GUID-owned PersistenceRoot isolates each case.

Each case enters through the **public FileSaveProvider constructor and its real load path**, pins primary/backup bytes before construction and asserts both unchanged afterward, then strictly requires **20**, **alive**, and **no backup-marker key**. Assertion messages include all three observed values: **10/kept** identifies fallback, while **-1 with both markers absent** identifies accepted-empty. Neither outcome satisfies primary decoding. There is no expected error, log whitelist, default relaxation, Save call or global provider mutation in the new cases.

Existing genuine malformed-primary recovery **10** positives remain separate and unchanged in `StructurallyDamagedMainFile_RestoresCompleteBackup` and `RootArrayAdmission_DistinguishesMissingNullAndExplicitEmpty_InPlainAndEncryptedFiles`. Explicit empty-profile, backup preservation, failed-slot and all original controls remain intact. No new harness, helper assembly, parser/framework, runtime/doc/API change or scene-caller edit was made.

Actual SHA256 bookkeeping before/after:

| Path | Before | After |
| --- | --- | --- |
| `Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveFileProviderTests.cs` | `75ad18fd157a2e1f85fff3aca9871a22d08633a6616d288987aa8a81d3058ed1` | `023afb8fedf69b96ae0d0fb0daaf90e68f503da3ab406de3f51c1202b2753d66` |
| `Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveManagerTests.cs` | `afd41cff2da911fecce1dd3a3327f27fb57c89f471508c012491ef925238c8f5` | same |
| `Assets/Neoxider/Scripts/Save/Providers/FileSaveProvider.cs` | `616655b5e29ce3851a6df68e3c17382f9b1177f32d163a987a52c6572cc4afdf` | same |

Actual work was source/assessment reading, manual review of the added literal/oracle code, and SHA256 bookkeeping. **No tests, Unity/MCP, compiler, product checker, installation/service, process control, child/other agent or git command was run.** No passing decode, backup recovery or unchanged-byte runtime result is claimed. Root will review the tiny source delta, freeze once all writers finish, and have the operator run the exact four new cases on both supported versions alongside diagnostics. **No dependency except that native verdict; stop after this delivery.** Any runtime response follows the measured Save-specific outcome and a separate authorization.

## P6 bounded first-character regression delivery (2026-10-26)

Read the implementation contract, exact updated request and `Local/Audits/assembly-2026-10-23/p6-save-firstchar-followup.json`. Root's recorded gate state says all 12 p6 native jobs are terminal; `sourceFreeze` and `executionAllowed` remain false. This continuation performs source delivery only.

Independently read the existing operator artifact `Local/Audits/assembly-2026-10-23/unity/final/p6/u63/Receipts/11-focused-diagnostic.xml`, SHA256 `519b3a6dbbb511b796f5f07306c999a207ce1e0983e1abac5ec060d9d8098fa4`. Its selected four `NonemptyRootArray_PreservesEscapedAndCanonicalPrimaryOverBackup` cases all PASS: canonical `items` and middle-character `it\u0065ms`, each plain/encrypted. That proves only those inputs on this recorded Unity 6.3 run. It does not establish first-character binding, all Unicode escapes or a Save defect. The prior section's UNEXECUTED statements describe its earlier delivery, before this operator evidence.

Added only `Neo.Editor.Tests.Edit.Audit20261023NeoSaveFileProviderTests.FirstCharacterEscapedRootArray_PreservesPrimaryOverBackup(False)` and `(True)` in the existing FileProvider fixture. Independent literal JSON supplies first-character `\u0069tems` with primary **coins=20 / primary-marker=alive**, against canonical backup **coins=10 / backup-marker=kept**. Both cases use current PersistenceRoot/encryption facilities and the public constructor/load path; pin both file byte arrays before/after; require **20**, **alive**, and absence of the backup key. Diagnostics retain the distinction between **10/kept fallback** and **-1/both absent accepted-empty**. No expected error or relaxed oracle is added.

The original four method identities/arguments/oracles, malformed-primary recovery, explicit-empty admission and all other controls are untouched. No runtime/parser/API/docs, scene caller, helper, meta, shared configuration or other owner's file changes belong to this continuation.

Exact SHA256 source bookkeeping:

| Path | Before | After |
| --- | --- | --- |
| `Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveFileProviderTests.cs` | `023afb8fedf69b96ae0d0fb0daaf90e68f503da3ab406de3f51c1202b2753d66` | `c068162bc8e2f324403bf23f15274adecfbe4a1b319bb70091ca1dc995a73356` |
| `Assets/Neoxider/Scripts/Save/Providers/FileSaveProvider.cs` | `616655b5e29ce3851a6df68e3c17382f9b1177f32d163a987a52c6572cc4afdf` | same |
| `Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveManagerTests.cs` | `afd41cff2da911fecce1dd3a3327f27fb57c89f471508c012491ef925238c8f5` | same |

Actual validation here is source/XML reading, manual literal/oracle review and SHA256 bookkeeping only. **The two first-character native outcomes remain UNPROVEN / UNEXECUTED.** No tests, Unity/MCP, compiler, checker, process control, install/service, child agent or git command was run. **SOURCE-FINAL; no source dependency remains.** Root/native peer review, a new freeze and operator execution of these exact two cases on both SDK versions are pending; a measured failure would require a separately authorized production response by this same author. Stop after this delivery.
