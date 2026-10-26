# Native implementation review 35 — first-snapshot corrections

Reviewer: original Abilities/native C owner, same session; source-only. Repository base: `D:/Git/NeoxiderTools`. No product, test, metadata, configuration, authorization or state edits were made by this review. No Unity, compiler, test, validator, process probe or maintenance operation was executed.

## Scope and admission

Reviewed the five complete corrective receipts, their exact bounded source/test deltas against frozen snapshot 1, the 13 central metadata repairs, and the single Play assembly reference. Root supplied successful empty exact-CLI liveness and DONE/exit-0 completion for the five original owners at admission. These are orchestration facts, not compilation evidence. Root subsequently resumed the primary owner for the independently confirmed finding below; this report pins that owner's **pre-continuation** state and does not follow its moving files. The later completion notice requires separate NI36 closure.

Snapshot manifest: `Local/Audits/assembly-2026-10-23/unity/snapshots/final-20261023T230131Z/manifest.json`, SHA-256 `0e433bf9848fe86066661c9209ebc955ed75a285e73d1e0a21f458a925e93260`. Comparison source: its `source/` directory. Existing full native reviews remain the unchanged-source context; this is not another whole-module audit.

Full receipts read at admission:

| Receipt under `Docs/Audits/2026-10-23/` | Lines / bytes | SHA-256 |
| --- | --- | --- |
| `Neo.Editor.implementation.md` | 87 / 13107 | `f9ba9a553e97d58510a46ca1ef20434955373e9612a025a26b8c20f9ce2324a8` |
| `Neo.Level.implementation.md` | 83 / 11017 | `4eb2151747f55f06d57cf18c3d145fc27da9bfaa05757172ae44e51135ed5970` |
| `Neo.HtmlToUitk.Editor.implementation.md` | 127 / 18132 | `6100101d21700c749d2965e47e126772c2275514d0511d31ee4a28c58dbf5fab` |
| `Neo.Editor.Tests.implementation.md` — pinned before continuation | 136 / 28449 | `4f7ae241ecdc1a5d61c5c6b50480de0321aa8d418eb78f23dfada54700e26cd0` |
| `Neo.Rpg.Demo.implementation.md` | 124 / 14034 | `fcf55ccef3f0f50011db3190d24f8048b5e70f0abe0c3c9669edcfff147f225c` |

The four nonmoving receipts and their reviewed product deltas retained these hashes on the closing read. The primary receipt/helper hashes below intentionally belong to the admitted earlier state.

## NI35-01 — confirmed remaining PackageInfo ambiguity (P1, compile blocker)

Original owner: primary `Neo.Editor.Tests`, `neo-audit-1023-b03-04`. The initial fix adds a correct file-local `PackageInfo = UnityEditor.PackageManager.PackageInfo` alias only in `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsPaths.cs:5`. C# aliases do not apply to another source file.

At the pinned state, `Audit20261023NeoEditorTestsValidationHost.cs:10-11` still imports both `UnityEditor` and `UnityEditor.PackageManager`, then uses bare `PackageInfo` at line 119. Its `Audit20261023NeoEditorTestsValidationHost.Discovery.cs:6,8` imports the same two namespaces and uses bare `PackageInfo` as a type and static qualifier at line 331. Both Unity types exist: the actual first-snapshot compiler already reported CS0104 for the equivalent Paths declaration. Ordinary C# name resolution therefore leaves these method-body names ambiguous after the first error is removed. No new native compilation was run to discover this residual.

Expected: all three helpers resolve their intended package-manager type without changing discovery behavior. Actual pinned result: two helpers remain ambiguous. Smallest correction: the same SDK alias or explicit qualification in **both** files; retain Discovery's package-manager import for `DependencyInfo`. Root independently confirmed the headers and usages and returned exactly this correction to the same owner. Later owner completion is not certified in this pre-continuation report.

Pinned source SHA-256: Paths `e20adc92fdc27f99f8e5968dfc85d34671c43c5b836c48e0363c3a39587bf9e6`; main host `1c8e5d91c76f6c88b9d06ae589eb651a8bd089d214c766f18d77c84e117dc207`; Discovery `3f02ffce3dd206e04f8c2143554e88d9b9bdcfd7be3bdc1b26deedcd7796062c`. Source closure of the primary correction is withheld pending the separate two-alias check and eventual native compilation.

## Other bounded corrections — source-supported

| Scope / exact current path | Reviewed change and retained contract | SHA-256 |
| --- | --- | --- |
| Demo: `Assets/Neoxider/Samples~/Demo/Scripts/QuestDemoView.cs` | Only line 111 qualifies `global::Neo.Samples.NeoDemoShell.DestroyOwnedObject(quest)`. This matches the actual helper namespace and its internal same-assembly accessibility under `Neo.Rpg.Demo`; exact-instance quest unregister and existing teardown ordering remain intact. No new dependency or runtime behavior. | `1aa17e2ce131f664015058980b7b9b3041ec185121bb96275c987681d121a8b1` |
| Editor: `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorCollectionTests.cs` | Only lines 27 and 33 qualify the actual `global::Neo.FindInScene` attribute. Its definition is `Neo.FindInSceneAttribute : PropertyAttribute`. Other stacked constraints, probes, public operations, assertions and fixture cleanup are unchanged. | `7d9789b49f1d6fd5bc7a4f02d3c8bbda3620e0853f15ce939426d0573a98d6d5` |
| Html: `Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkEditorWindowReplacementTests.cs` | Removes six illegal true-setter writes and the cleanup false-setter write. Seed now invokes the real private `MarkDirty` edit command after the asserted real replacement, then asserts the native unsaved flag. Cleanup calls public `DiscardChanges`. Existing save/discard/cancel/failure and own/foreign persisted-byte oracles remain. | `7f6f4061d70dad7757b4739aee806436b51aed5f4bbe4855ef084da0ba00aead` |
| Html: `Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkEditorWatchDependenciesTests.cs` | Replaces the remaining illegal cleanup setter with public `DiscardChanges`; asserts that the real CSS field edit set the native unsaved flag. Pending-task settlement, bridge/job-path restoration and owned-file cleanup remain. | `d145517f65dbfd1fceacc4bcaf3898ece83b16f41b4037cd741e6cbbf2f462ca` |
| Level: `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelBoundaryTests.cs.meta` | Only the unreferenced new malformed 33-character GUID becomes the accepted 32-character `7193ba98667d41d9b7db388c452b613e`. All importer settings remain. Actual boundary fixture C# is byte-identical to snapshot 1 (`c151cd4dd27cb6cb14f40c239b66fb676f45e78c5e0b7b520bba0354364b6221`). | `52242b7d2ca84157bc10c10d25adc01b3abf5684db2c9ab36317fcc2f6f495db` |
| Root integration: `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` | Exactly one direct `Neo.Extensions` reference is added. The unchanged Input UI fixture really imports `Neo.Extensions` and calls public `UIUtils.GetUIElementsAtPosition`; the actual class and assembly have that namespace/name. Existing assembly settings/references remain. | `04a8b1e3678d9391a5fd539c4756d0704fccb2a55e5fd338224359d5bc1863be` |

Both Html changed fixtures and the complete delta bodies were read, rather than inferring coverage from case counts. The Html receipt's 49 authored Editor cases remain a source inventory, not an execution result; existing bodies in both changed fixtures were retained. Production `Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.cs` is byte-identical to snapshot 1, SHA-256 `020ac2d612d14f5e093c9b8b7115f7facd847e0b84891eafb476a37ae51745ac`. Its actual `MarkDirty` sets both dirty states, and it has no `DiscardChanges` override. The installed offline 6.3 and 6.6 EditorWindow API documentation describes public base `DiscardChanges` resetting the native unsaved flag; no protected setter access or invented wrapper was introduced. These changes preserve existing public APIs and test oracles.

## Central metadata — exact accepted bytes confirmed

Read the accepted map and successful application receipt, and independently read all 14 current metadata files as bytes. All 14 equal their accepted `recommendedContentUtf8` bytes and recommended SHA-256. For each of the 13 pre-existing assets, additionally compared current bytes with the exact canonical slice of frozen snapshot 1 and read `git show HEAD:<path>`: the first existing GUID is retained. Twelve repairs retain the first complete metadata block byte-for-byte; the sample repair removes only its eight-byte malformed prefix (`d18fe29597e29490`, Unicode U+044F/U+2557/U+2510), retaining its existing GUID, timestamp and line endings. No importer schema/version/settings regeneration occurred.

Map: `Local/Audits/assembly-2026-10-23/metadata-correction-map.json`, SHA-256 `9ffd73313af1fe653fe5580d150bed8295219db91aebc1eef9f11b2ba9afd46e`. Successful application receipt: `legacy-metadata-applied.json` in the same directory, SHA-256 `c24252873b937578ec47e6c7fd8b6a2db82f21697de86259731a4b02f37eafbb`. Every one of its 13 after-hashes matches the current bytes and the accepted map. The Level entry is the separately owned fourteenth file, not a central application.

| Existing path below `Assets/Neoxider/` | Retained existing GUID | Exact current SHA-256 |
| --- | --- | --- |
| `Docs/Progression/ProgressionConditionAdapter.md.meta` | `dbca67c1cdb84fcab69685644117fbcd` | `080b12fc4f2a41b2bd9a703c2625f89a4d4240f5c3705ba2da42d71e37306800` |
| `Docs/Progression/ProgressionManager.md.meta` | `a95a985d96ba4d8c8df7f7a45b29cb2e` | `214c1bcad49926f0b1641758aa4bbbc1f7dfa05402ac7dad493a60fdaad9db79` |
| `Docs/Progression/ProgressionNoCodeAction.md.meta` | `a912e89cc3834c519c6e1283b0a63de0` | `4c55162fb92c62179132518fdbc86a66cebc1dd00466ad2f14d8cf099dfe5c28` |
| `Docs/Quest/QuestBridge.md.meta` | `a2ee4881b2134517a2c0ea0e821ef213` | `da7077d09e8207e561c682222aa60d5ee032fb0e9c178d49068c6812fc2bbf02` |
| `Docs/Quest/QuestState.md.meta` | `e1e55ab9d93442d88447fbdef5f13209` | `dcd66f1c1febffe68bd1c33410cd288c8f05d0b644e6817a98b5921f50d5ad2f` |
| `Docs/Save/SaveableBehaviour.md.meta` | `7952a34215344d74b51aa462c6d2af49` | `1a4a9170a4b91ec2a6c9bcbca40df8e1354f2183808209eeea70328736ac7c11` |
| `Docs/Save/SaveManager.md.meta` | `40dbf7db2a28460cb3ad61f7d2286f7c` | `3eb8ab8c47ba786f9c170df826ceaa0fa5bc40c756d07012388f4e4a64201ae8` |
| `Docs/Shop/ButtonPrice.md.meta` | `767759141520420bb8bc09eff5cd784f` | `d606d1b2be0875cf3a61b314086ac0601cb2c80e0fa1d0a6cdf5de62292cd339` |
| `Docs/UI/VisualToggle.md.meta` | `474b4c61416e4927a1cbf637b91ed020` | `2caf0038cdbc3fe902eeb354f37ac7c50eb602761b10c20b77adc6c126d0d02b` |
| `Samples~/NeoxiderPages/Editor/Tools/AutoSpriteAssignerEditor.cs.meta` | `5bc05dbb930e42bf8d111334614deb40` | `d885c7316f27ed6a7eb3838592a5e5f46e6b24c84eb0ba3ee37c29d009925770` |
| `Scripts/Save/ISaveIdentityProvider.cs.meta` | `22e3f628eb2d4bbd90f9de2896825d0d` | `56bc846165b53ae6efa15043910ffd4f8ca89455ae90342a21f102ee467babe8` |
| `Scripts/Save/SaveIdentityUtility.cs.meta` | `30a6c1d5b8994bb7a9538b5b72b4ed54` | `8650bf4ea369479c7582f667f717f2432f2347b8b6da97cbe877c184f4a8e51d` |
| `Scripts/Tools/Input/OptionalInputSystemAdapter.cs.meta` | `0f7b7b37a0c24f228f2414dbcb0d6228` | `0495948bc1f62bd54e87c2f22c4747d57a059b3038bbc8dbfd752aebec7666cd` |

Read the current 50-line central application helper without executing/importing it. It preflights the complete exact 13-path set, before-hashes and proposed bytes before any write; the null `retainFirstBytes` branch is explicitly separated. Root's initial failed preflight is a reported zero-write historical attempt, not a failure reproduced here. Current successful bytes are independently established above. Prior accepted-map reference-search limits remain applicable; this review does not assert safety for unknown external projects. The added `Docs/Migration11.md:73-75` paragraph truthfully describes retained identities and explicitly calls out potential external use of a discarded second GUID.

## Disposition and verification debt

Demo qualification, Editor attribute qualification, both Html fixture corrections, Level metadata correction, central metadata repairs and the Play direct reference are **source-supported within this bounded scope**. NI35-01 is a confirmed source blocker at the pinned primary state; its later owner continuation requires NI36. No additional bounded defect was found in the other reviewed deltas.

The first frozen 6.3/6.6 compilation's 15 C# diagnostics and package check's metadata failures are actual historical failures, not present-source results. The complete actual 6.6 compiler-error record was read; this did not turn any corrective source into a compiled pass. All current-source compilation, native import, discovery and runtime tests remain unexecuted here. Root's six Demigiant `PluginImporter` v1 metadata files are a separate pending native-migration scope; they are unchanged by these repairs and are **not approved as Unity 6.6 compatible** by this report. This review grants no freeze, execution or maintenance authorization.
