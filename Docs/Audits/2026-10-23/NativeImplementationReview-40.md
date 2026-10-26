# Native implementation review 40 — completed source3 editor/Save deltas

Independent native C review, same session; **source-only**. Scope is the stopped, completed `Neo.Abilities.Editor`, `Neo.UI.Editor` and `Neo.Save` corrections. Root supplied exact stopped-writer observations; all three current receipts were confirmed source-final before admission and reconfirmed after restart at the same hashes. No owner was deferred for an active writer or unfinished receipt.

Baseline: `Local/Audits/assembly-2026-10-23/unity/snapshots/final-20261024T002340Z/source`, manifest SHA-256 `2d12ae058b989ce064e86a8d7db8223ecb65241d5dcd19babfb2fd7082adfc70`. Repository: `D:/Git/NeoxiderTools`. Read all three complete receipts, every bounded source3 diff and every changed/new regression body, plus the relevant renderer/serialization/pose/load/recovery methods. Unchanged earlier coverage is retained by byte comparison; this is not a whole-module re-audit.

Only this report was written. No Unity, compiler, tests, parser/checker, product/config edit, process operation or child agent was used. Moving primary fixture extraction, shared references/reserved files and this reviewer's Local asset-maintenance helper were not reviewed or changed. A separate sealed-input metadata job grants no product-test permission here.

## Current pinned artifacts

| Artifact relative to repository | Lines / bytes | SHA-256 |
| --- | --- | --- |
| `Docs/Audits/2026-10-23/Neo.Abilities.Editor.implementation.md` | 111 / 17202 | `8b58d432935b46baf97e009d4e99bfdb388200fee32de5bd99249fd9f0c68069` |
| `Docs/Audits/2026-10-23/Neo.UI.Editor.implementation.md` | 187 / 13351 | `ee3cf92300f5d962f3e10e9d5bb922747c34c1e254be19cc11d09f243b58ff4f` |
| `Docs/Audits/2026-10-23/Neo.Save.implementation.md` | 97 / 16181 | `c4bc3fd4168eb9d95dad7c0e8b1a23225fe5f25d417463b2720f97685afcc5e1` |
| `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023NeoAbilitiesEditorTests.cs` | 592 / 28535 | `50cff18a4695c1fe5986fbacf67dd2adf20dc43432bb150c3bd56c378a796f36` |
| `Assets/Neoxider/Scripts/UI/Editor/UIMeshRigGraphicEditor.cs` | 1574 / 66824 | `2bfd5f4e18c3688247dd616327f51d7767c4d49e873220837690b98b2aa0ff15` |
| `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIEditorAuthoringTests.cs` | 428 / 22428 | `13b2c9d0f59a79b29adbdeda37a5cc8ecce7ca50cf00e531dd6aa0fd79e9caaa` |
| `Assets/Neoxider/Scripts/Save/Providers/FileSaveProvider.cs` | 738 / 24833 | `616655b5e29ce3851a6df68e3c17382f9b1177f32d163a987a52c6572cc4afdf` |
| `Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveFileProviderTests.cs` | 256 / 10923 | `75ad18fd157a2e1f85fff3aca9871a22d08633a6616d288987aa8a81d3058ed1` |
| `Assets/Neoxider/Docs/Save/FileSaveProvider.md` | 86 / 5560 | `3797b1c4cac8784f5018e490562882972432474d6954bc25bb6fd383714a48b4` |

## Neo.Abilities.Editor — source-supported

The source3 delta changes only its owned fixture/receipt. Existing production `AbilityDesignerWindow.SelectAsset` constructs the SerializedObject and `RebuildCenter` calls Update before drawing. `ModifierDefinition._blueprint` is an inline SerializeField. The corrected tests therefore establish their deliberately managed nulls **after** ordinary binding and render before another serialization boundary; they do not claim a null inline asset value persists through Inspector serialization.

The null-tick case still requires exactly one `3 healing` Label. It now additionally checks heal styling, absence of a reaction card, two retained tick slots, a still-null first slot and the same valid second node, plus the unchanged null reaction. The missing-blueprint case still requires the exact diagnostic and validation issue; it now scopes the real Error HelpBox to the newly created board, checks no tick cards/library warning and verifies rendering did not repair the null.

The private fixture helper removes only the previous retained `nad-mod-board`, invokes the existing production renderer and requires one different board identity. `RefreshValidationAndLibrary` reads managed definitions and rebuilds library/status without SerializedObject.Update. Production `BuildModifierBoard` already appends the real diagnostic and skips actual null nodes/reactions. No fake renderer or broad root query can satisfy these corrected assertions from a stale board.

The added ordinary OpenWindow/serialized-refresh positive case requires both nonnull cards in order, including `0 magical damage` and `3 healing`, with the authored zero node still present. This prevents hiding a materialized/default-looking node as a false null fix. The original14 cases and27 mixed-case cases remain; current source inventory is42 cases/16 methods. Window/owned asset/Undo cleanup is retained. Native binding/focus/scheduled-panel behavior remains unexecuted on this correction, and the receipt properly avoids claiming the exact old materialization point was instrumented.

Disposition: **ABILITIES-EDITOR-MODIFIER-BOARD source correction supported; no bounded residual identified.** No production behavior, public API, serialized contract or friend access changed.

## Neo.UI.Editor — source-supported

The only production hunk is `UIMeshRigEditorUtility.DuplicatePoint`: capture the source's **current local position, rotation and scale** before cloning; instantiate using its world position/rotation and exact original parent; register creation/complete Transform Undo; then apply those captured local TRS values after creation bookkeeping. The remaining owner notification, cloned descendant-key allocation, dirty/prefab recording and root/ownership rejection paths remain. No rest capture, manual component reconstruction or source/parent transform mutation was added.

This preserves current deformation independently from serialized rest data. Existing `UIMeshRigPoint.CaptureRestPose` records nested local rest TRS; `ResetPose` restores those values for the nested branch. The three owner NotifyPointChanged implementations mark caches/geometry dirty rather than recapturing/restoring pose. Applying current TRS after creation therefore does not intentionally redefine REST. Existing native Instantiate retains component/reference cloning semantics.

All three adapter cases now require a genuine pre-duplication Pose-mode/local-TRS/world-position precondition under a rotated, nonuniform parent. They compare source and clone independently with the authored baseline, preserve the parent and serialized rest getters, then require local/world Reset Pose restoration and Undo removal of only the duplicate. Original relative-position/rest/count assertions remain. The positional0.0001 and rotation0.001 bounds were not relaxed. Three new adapter cases verify real Button.targetGraphic remapping to the cloned child Image, preserved external Navigation selection, and Undo preserving the source/external object and point count.

The21 original cases remain and three reference/Undo cases are added:24 authored cases/10 methods. New objects are owned by existing root/asset cleanup; no new Mono helper or shared fixture dependency was introduced. This is a concrete pose-restoration correction, not native Undo/Instantiate success evidence; saved-prefab round trips and future callback timing still require the actual runner.

Disposition: **UIEDITOR-NATIVE-NESTED-POSE source correction supported; no bounded residual identified.** Existing public signatures, serialized fields, parent selection and rejection mechanics remain intact.

## Neo.Save — source-supported

The added private root admission is called before real JsonUtility typed deserialization. It walks root member tokens, accepts one exact decoded `items` name only when its value starts an array, rejects missing/null/non-array/duplicate roots, and requires complete root consumption. Nested containers are balanced iteratively, quoted strings/escapes are consumed, and Unicode escapes in names are decoded. Nested/text lookalikes cannot satisfy the root requirement. This is not an unsafe regex/substring-presence test; JsonUtility still owns typed/value syntax and existing per-record validation. No general-purpose parser or public DTO/API change was introduced.

Both plaintext and decrypted payloads reach the same admission function. A valid explicit `items:[]` remains a successfully loaded empty primary and never consults the older backup. Missing/null/invalid primary returns failure to the existing complete-backup selection. Record copying uses a local candidate dictionary; a later invalid record cannot publish the earlier `main-only` record. Load selects a completed dictionary and does not rewrite primary/backup; the existing recovered-backup flag still preserves the good backup on the first subsequent commit. The wire fields `items/key/value/type`, AES/Base64 path, empty-key behavior and commit/event ordering remain unchanged.

The original five damaged-primary cases are retained and expanded to19, preserving exact backup value10. Added controls cover non-array roots, nested/quoted/case-mismatched lookalikes, duplicates, truncated/trailing input and a good record followed by an invalid one. They also assert no partial record leakage and byte-identical primary/backup after loading. The12-case plain/encrypted matrix distinguishes missing/null from explicit empty, permits misleading nested/text metadata alongside the real root array, checks an escaped member name and later save/reload30, and verifies first-recovery-commit backup preservation. Existing ordinary/empty profile, empty-key, failed-slot and recovery controls remain. No error whitelist or relaxed value oracle was added; regex remains only in expected-log matching.

The canonical FileSaveProvider guide accurately documents explicit-root-array versus JsonUtility's missing/null normalization, deliberate empty saves, duplicate ambiguity, plain/encrypted parity and unchanged encoding. New encryption controls consume the existing public SaveFileEncryption/FileSaveEncryptionConfig API; no production friend expansion is required.

Disposition: **required-root-items-array correction source-supported; no bounded residual identified.** The separately reserved Save scene-admission follow-up remains deferred outside NI40 and is not certified by this disposition.

## Preserved identities and verification boundary

All six changed code/test/doc file metas match frozen source3 byte-for-byte: Abilities fixture `7ca65221c6e7633e0b8d3f002f52d153d07ccea1f24eee50a9331e898ac4fa79`; UI editor source `1dc16bcc3eea010c8069ddc3b2ab5b79535340f940bcb65306157c65983bfb7c`; UI fixture `49db4f00ec9b76079c29df3c40cf03f7c9ecd5258d7c4ebca7663b5c34d0dde3`; Save source `b3e2d3ebfed59b08e582a9a4578a2904cf8845e754531e9c354b3544bd6a189e`; Save fixture `9ba7ad940243f4e9955f124ee70b63e8fcfa378fdddb0de0f177a9dee8aeac1d`; Save guide `7fdae12b03411fb8e53704e05e32584958d1990050b5e939ccc558a20df91eff`.

Unchanged production/context hashes: AbilityDesignerWindow `24a66bbc533e3437a649c0deff73c9d0cc8e0cb5b8a2c3034387bf9a4f88df29`; AbilityDesignerUI `07bc4131a23bdd9f5b4b399922a7445623aafd4b1b448226b18f370a84ed4c52`; Abilities Editor guide `4c46dfbb459b5c500819e3b7a9edeb42b06b671b8459eb3dce975d04ff28588b`; UI Editor guide `3ab3fe1349686a8f23902a3f202e39423140bd09ac3b91025ce028a6720f64f8`; existing UI Editor AssemblyInfo/friend declaration `6d794d9e032d0204a83184919d0021522e4bf35eef9aa48cf891481c127ee147`.

All three assigned bounded corrections are **approved at source level only**. No additional confirmed defect or acceptance weakening was found, and closing hashes remained stable. The receipts' old graphical failures/pass counts remain historical; no corrected-source compiler, JsonUtility, UI-tree, transform, Undo, encrypted-disk or test pass is claimed. Root must include the42-case Abilities,24-case UI and expanded FileProvider suites in a later frozen native run after all source writers finish. Existing primary/shared/reserved and broader platform/integration debt is not closed by this report.
