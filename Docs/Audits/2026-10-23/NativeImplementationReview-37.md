# Native implementation review37 — snapshot2 compiler corrections

Status: **COMPLETE — assigned bounded corrections source-closed; no confirmed residual. Current corrected source remains UNCOMPILED / UNEXECUTED.** SAME independent B, gpt-6.1-sol/max. Only this report written. No product/config/other-report edits, Unity/compiler/tests/checkers, source patches, new actors, services/process control or commits.

Root admitted StateMachine.Editor first, then native C's completed Abilities fixture, then exact original Editor b03-03 / Progression b04-07 / Rpg.Network b05-06 DONE0/source-final. Only immutable snapshot2 and admitted final files were read; moving scopes were held until root's explicit admissions. Closing liveness is empty; sourceFreeze=false/executionAllowed=false. Root's direct shared Edit reference is complete. Full latest receipts read: StateEditor89, Abilities164, Editor103, Progression120, Rpg.Network175 lines. Their historical module work is not freshly re-audited here.

## Diagnostic baseline and bounded disposition

Immutable baseline manifest **`9142923cb57ef2a4e6047e3bc1627174ab5a15e9ef2f6823c08ca6a7052611ef`**, source `Local/Audits/assembly-2026-10-23/unity/snapshots/final-20261023T235218Z/source`. Personally read both complete actual error JSONs from run **f84c9007d0494cd8a421d84d70d931a0**: Unity6.6 has46 records/370 lines, SHA `b1c0d49009a08f9208b499b2a3770fa9681eedd5b66acb994c37b701dc869939`; Unity6.3 has44/354, SHA `caaedb21d9b130a8e185e6f23c9d219cef91ba3f6d932e1aa653b900c3eae160`. These are real previous compile failures, not evidence the corrected files compile.

| Assigned scope | Current source correction and preserved oracle |
|---|---|
| Abilities fixture199/260 | Exactly two invalid one-argument constructor arrangements become existing parameterless AbilitySystem plus its existing public World setter; the same named/fresh FakeWorldAdapter and PendingProjectileTimeout1 remain. Read both complete changed test bodies and constructor53–64/World71–75. Assignment precedes units/casts/ticks. All health/cooldown/duration/event/pending-hit assertions and their order remain, including overflow HP50/duration2 rejection followed by HP54/duration1 recovery. Production AbilitySystem is byte-identical to baseline; no stub/constructor overload. |
| Editor Collection75–78 | Four independent Has.Member assertions still require both actual local and nested collider in both scene array/list. Exact local/child array/equivalence oracles and preceding attribute qualification remain; no string conversion or member dropped. |
| Editor SceneSaver81/104 and Lobby71/84/87 | Capture and compare actual Scene values instead of obsolete implicit SceneHandle→int. Active scene and sentinel object's scene still compare exact identity, not name. All source/backup bytes, paths, dirty/loaded/content/scene/preview/selection/cancellation and callback/prefab checks are retained; no version branch/GetRawData/int cast. |
| ProgressionManager226 | Root-approved additive read-only `UnityEvent OnProfileLoaded => _onProfileLoaded`. Existing serialized field55/default and sole invocation752 are unchanged. Removing the getter/XML block reconstructs the entire old production file. Existing load/apply/refresh/commit/retry/event order and schema therefore do not change; no second event or dummy dispatch. |
| Progression fixture1073–1077 / two guides | Adds SameAs against the actual serialized event to the existing loaded/fresh two-case initialization control. Existing one/zero load-event/read counts, one binding, no initialization save, later7→8/0→1 save and all prior failure/reentry call sites remain. Changed README/API paragraphs accurately give AddListener/RemoveListener on that same Inspector event and distinguish load-enabled from deliberately fresh setup. No new case/framework. |
| Rpg.Network fixture12 | Only `using Neo.Core.Resources;` added inside MIRROR region. Existing canonical RpgResourceId namespace/constants and existing Neo.Core test reference resolve all six diagnosed sites. No magic strings/new type/wrapper/assertion/payload change; non-MIRROR branch retained. |
| StateMachine.Editor fixture133 | CollectionAssert.DoesNotContain still excludes the actual NonSerializableAction Type from actual Type[]. Adjacent positive LogStateAction inclusion and empty action-list assertion remain, as do construction-failure/recovery/Undo/serialization controls. No production change. |
| Root shared Edit asmdef28 | Only direct Mirror.Components reference added. Actual inheritance read: NeoLobbyManager→NetworkRoomManager (Mirror.Components)→NetworkManager→MonoBehaviour. RoomScene/GameplayScene/roomPlayerPrefab and inherited offlineScene/onlineScene/playerPrefab exist. FindOwned<T> where T:Component and all member assertions remain; no weakened constraint/base stub or invented property. This addresses missing-base visibility and its cascading diagnostics at source level; actual on/off graph acceptance is pending. |

**Byte inverse evidence:** reversing only the explicitly reviewed edits reproduces snapshot2 bytes exactly for all nine C#/asmdef files below, including entire fixtures. Thus no old assertions, case attributes, cleanup, ordering, serialized fields or unrelated implementation body changed in those files. Both Progression guide deltas were separately read and are additive API/event wording only. Baseline/current Abilities runtime and fixture metadata also match exactly. No broad runtime reassessment or test-dropping repair was performed.

## Current read hashes

First paths relative to `Assets/Neoxider/`. Each C#/asmdef row has the exact inverse-match described above; guide rows are reviewed deltas.

| Current file | SHA256 |
|---|---|
| Tests/Edit/Abilities/Audit20261023AbilitiesReceiptsAndTimeTests.cs | `8a3e89fed06cc391c96f91c683837b24c83900ad2cb8d6b4edf186ad62c69903` |
| Tests/Edit/Editor/Audit20261023NeoEditorCollectionTests.cs | `2ec8a645b9f42a99cd3f306b2e0a00f54ae3124567e98cb7dc2950eb9e4fa195` |
| Tests/Edit/Editor/Audit20261023NeoEditorSceneSaverTests.cs | `848701424cc41ab38ff683f371eac1aec3d283712d3bf55d4aa7b531e132b35f` |
| Tests/Edit/Editor/Audit20261023NeoEditorLobbyTests.cs | `a6b8ac952d1507ec6b3121d6ce8b228d6f7aa2de8c2be87f6b428937e8934b7b` |
| Scripts/Progression/Components/ProgressionManager.cs | `ec99cadac84f51bb117afd6ad51488fd7dca2aa08501e40781aecb6b5628528c` |
| Tests/Edit/Progression/Audit20261023NeoProgressionRegressionTests.cs | `d3486908d34764488dd7ab040b0061a32c4beb473a77add791bfc896c39b99ca` |
| Docs/Progression/README.md | `08e4ce5d63293de373c0a14fbafc31ed7024dfbb56882d68f162e2db8f5d3fa6` |
| Docs/Progression/ProgressionManager.md | `ad5affee04b545f59f759fe9cacd5bfc66c33a282a5fb4dd23b01872e02eb256` |
| Tests/Play/Network/Audit20261023NeoRpgNetworkRegressionTests.cs | `db24665a0a5399dab1fb7b75ee80980bb92809547e58a2fccd4dc397373617fe` |
| Tests/Edit/StateMachine/Audit20261023NeoStateMachineEditorTests.cs | `a6a050b8364825c269b1b82d1be5780042adb0dc5535bc6d3b350086f95c42ed` |
| Tests/Edit/Neo.Editor.Tests.asmdef | `e38c42b789ce5901fb4b3554552ff57822087353018bb4c9af5f3b1744b4a7b2` |

Full receipts relative to `Docs/Audits/2026-10-23/`:

| Receipt | SHA256 |
|---|---|
| Neo.Abilities.implementation.md | `542e9bdf6ab93b71bcd1e073f3f0dde2b5aa0ca0e22a69b99f29f867798e95c8` |
| Neo.Editor.implementation.md | `545725735de1b175c21b50fc25496220178bf41297a2c08f2947404f8b1f1b91` |
| Neo.Progression.implementation.md | `a791d983e4b71d39061ec3eea30931629d1c5ff9d968fafa1c87d68e21065252` |
| Neo.Rpg.Network.implementation.md | `fbe8f3d67bfd2eafdfea2b251fa28934d3fd45a1951265045fb8569e83afb718` |
| Neo.StateMachine.Editor.implementation.md | `b3155839a3f740c45a3831f240ca6d41e0517e62317c9f5d68218673e9a6718c` |

No confirmed residual requiring another source correction. Root may refreeze after completing its remaining gate work; only the sole authorized Unity operator's next actual compilation can close compiler evidence, followed by discovery/execution of retained meaningful cases. No current syntax/compiler/test pass is inferred from this bounded source closure, old diagnostics, or earlier .NET results.
