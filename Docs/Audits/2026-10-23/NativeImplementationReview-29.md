# Native implementation review 29 — public entry and migration documentation

Status: **COMPLETE — assigned public documentation source-approved; all execution UNEXECUTED**. Independent source-only QA, same native `gpt-6.1-sol` / `max` session. Only this report was written; NI21/22/25/27 remain final and untouched. No product/config/other-report edits, validators/HTTP/compiler/tests/Unity, children/services/process control or commits.

Root declared the six README entry documents plus root-owned migration/changelog stable during this read. README receipt52 lines is DOCS SOURCE-FINAL / INTEGRATED VERIFICATION UNEXECUTED, SHA256 `39bad10b99ae1e74404642ea0cf586e9c9cc8c0dcd0ef25882158a085bf65811`. Complete receipt52/request27/integration97/implementation-contract65 lines were read. Closing fingerprints at `2026-10-23T22:13:30Z` match the initial six entries, migration, changelog and four coordination artifacts. Remaining Parallax/Other/Network harness writers and their guides/runtime are outside approval; no result is promised for them.

## Disposition and complete public read coverage

**No additional confirmed bounded documentation defect, typo or required correction found.** Root does not need a doc change from NI29. This approves the inspected public wording as source documentation, not execution, publication, exhaustive link health or runtime implementation.

| Complete current document read | Scope/result |
|---|---|
| Repository `README.md`,211 lines | Version/preparation/install, prerequisites, C#/scene/NoCode routes, Condition event wording, module map, three samples, current test-status qualification and historical HTML/UITK slice. |
| `Assets/Neoxider/README.md`,181 lines | Package-boundary links, current version/preparation, runtime and scene starts, required external/bundled dependencies, module/sample maps and test isolation. |
| Package `Docs/README.md`,124 lines | Canonical entry/index, RU/EN routes, module namespaces, reuse map, testing/isolation, migration and sample navigation. |
| Package `Docs/GettingStarted.md`,213 lines | Entire literal resource/audio snippets and scene authoring steps, install paths/prerequisites, sample identities/import paths and RU orientation. |
| Package `Docs/PackageCompatibility.md`,73 lines | Manifest/local versions, unchanged Unity floor, both stable-ID compiler branches, dependency matrix, isolated host and explicit UNVERIFIED scopes. |
| Package `Docs/Samples.md`,67 lines | All three manifest identities, mutually alternative source roots/imported copy, Audio/Survivor/UI Mesh Rig examples, manual-vs-automated intent, hidden YAML-vs-visible native coverage. |
| Package `Docs/Migration11.md`,73 lines | Complete current accepted major-contract notes and all linked module destinations. |
| `Assets/Neoxider/CHANGELOG.md`, current11 section1–157 | Actual file verified; full current11/preparation section, major contracts/audit integration and retained existing development notes read. Remaining1600 historical lines were not freshly audited. Whole-file SHA below identifies the read snapshot. |

## Metadata, navigation and evidence boundaries

The complete52-line package manifest declares `com.neoxider.tools`, displayName NeoxiderTools, version11.0.0, Unity6000.0/0f1, and precisely these samples: **Demo Scenes → Samples~/Demo; NeoxiderPages → Samples~/NeoxiderPages; Character Movement Fundamentals → Samples~/CharacterMovementFundamentals**. Six entry tables/examples agree. Source enumeration found the named Audio, Survivor and UI Mesh Rig scenes at their documented release paths. Documents distinguish hidden release Samples~, alternative developer Samples, and imported Assets/Samples/NeoxiderTools/11.0.0/<sample name>; they do not require all roots simultaneously or overwrite imported10.x copies. Branch demo is preparation, not an instruction to rename source folders.

Package/local dependency values agree with package.json and Packages/manifest.json: AI Navigation1.1.7/local2.0.11, Input System1.14.2/local1.19.0, uGUI package1.0.0/local2.0.0; ProjectVersion is6000.3.14f1. Required external DOTween/UniTask, bundled CMF and optional integrations are distinguished. Source-declared6000.0 minimum is not silently raised by the major bump or certified across the whole family. Both compiler branches and native/player/device scopes remain UNVERIFIED. Samples27 is a required no-Mirror source policy, not executed no-Mirror import evidence: the current optional Demo asmdef still names Mirror and actual missing/present dependency behavior remains in the compatibility matrix. No new compile-failure inference is made from a named reference alone.

Version badges refer to metadata; preparation notices and Migration11 explicitly deny a published tag. Default-branch Git URLs are identified as such, with a local checkout route for current preparation. No invented#11.0.0/#demo release URL or fresh pass appears. Package-only relative targets stay inside the payload; repository-only hero/CLI/reports use explicit repository URLs. The inspected C# runtime/first-audio/local-path heading anchors match their current headings. Canonical module entries, migration, Testing/PlayMode and RU routes are present in the six-page navigation. The selected migration/testing/ownership/bridge/contract target files were manually enumerated. No exhaustive link/anchor resolver or HTTP probe was run; remote availability remains unverified.

Historical HTML/UITK239/239 and7/7 results are explicitly dated2026-10-14 and expressly excluded from current11 verification. The existing development notes in changelog retain their earlier scoped evidence/backport context under the preparation header. Authored regressions, broad 1,500+ inventory language, workflow badge and source review are not called fresh discovery/pass evidence. Migration31 explicitly defers1/100/1000/dense-hit results. Test guides are canonical destinations, with static-reset/provider/scene/PlayerPrefs isolation and broad-suite Mirror dependencies distinguished from single-module runtime requirements. UPM testables exposes tests without claiming every host/layout/capability works.

## Literal onboarding APIs

`GettingStarted:74–106` uses actual `Neo.Core.Resources.ResourcePoolModel`/ResourcePoolEntry, ordinary C# types with no UnityEngine imports. Current AddPool36–57 registers the100/100 entry, -1 caps are unlimited, TrySpend82–108/ValidateSpend111–151 checks full cost/cap and stores100−25, GetCurrent21–24 returns75. The example uses explicit types and valid out-string syntax; it introduces no mandatory preflight/scene/singleton. The expected75 is source arithmetic, **not execution or literal-snippet compilation**. Tick/OnResourceChanged names and scene HealthComponent route exist.

`GettingStarted:153–184` uses actual `Neo.Audio.AM.Play(string)`669–703 and a serialized assigned AM reference. AM has the named AddComponentMenu, Sounds/Sound Entries surface; AudioEntry.Id/AudioClip[] Clips exist. The NoCode path uses real PlayAudio serialized soundId/playOnAwake and public AudioPlay54–97, invoked by Start42–48. Default clip array does not supersede the named ID; the manager/assignment instructions cover dependencies. Named/index/singleton alternatives are real APIs. Playback, scene setup, inspector labels/import and literal compiler acceptance remain unexecuted.

## Major migration parity

The complete IMPLEMENTATION-CONTRACT and prior root-accepted native reviews are the behavioral baseline. Targeted current API/control-flow reads supplement it; this is not fresh whole-runtime approval of every linked module.

| Migration lines | Contract parity/source evidence |
|---|---|
| Money9–11 | Current Money44–52 defaults the single authority field to ServerOnly; ingress711–748 rejects local-only/unknown/refund/nonfinite/invalid authority before budget. None/OwnerOnly retain their documented trusted/identity meaning. Direct server/offline paths and lack of acknowledgement are explicitly preserved/disclosed. No reward-earned guarantee is invented. |
| Counter/Selector13–15 | Retains accepted NI19/25 remote relative-op/operand and finite raw-candidate admission before budget/Int conversion; absolute Set/direct legacy policy distinguished. Selector RpcSyncState295–303 carries active+excluded snapshots; receiver393–401 applies exclusions before final active selection/publication. SelectorModel109–124 rejects unrepresentable logical bounds; checked IndexWithOffset801 throws on overflow. Matching peer versions are required for actual changed payloads. |
| Shop19–21 | Current TryCommitPurchase1097–1123 persists within compensated mutation; observers separately report faults after commit. Bundle continuation/best-effort grant/no arbitrary-callback or multikey disk transaction match approved contract. Exceptions are no longer presented as purchase cancellation. |
| Inventory/Box23–25 | Committed quantity/price ownership and original-slot identity match approved receipts. Actual InventoryDropper PendingReturnCount110, RetryPendingReturns138–172 and receipt-before-publication recovery support the explicit live dropper/inventory requirement, nonpersistent sidecar and inspect-current-pending advice after observer fault. No durable sidecar/disk transaction promise. |
| Interaction29–31 | Exact physical contexts/complete raw hits/per-target policy/unique LOS, existing obstacleLayers meaning and no new scene wiring match authoritative design. Explicit simulation invalidation/fresh-query responsibility remains; performance/count results pending, not mock proof. No reapproval of later writing scopes. |
| State/pause35–39 | Current StateMachine60–75 has additive Func<float> clock constructor;172–181 addressed SetCurrentStateEnterTime(float). Shared-predicate standalone setter vs machine anchor is accurate. Bounded dispatch/exact GM baseline and stale-generation suppression match accepted contracts; synchronous listeners are not called atomic. SetText exact-final/approximate-interpolation and near-plane migration remain explicit. CameraRotationController77/99–125 retains pixel×sensitivity×axisSpeed×scaled deltaTime; no proposed frame-independent option is claimed. |
| Cards43 | Matches source-closed NI27 admission-before-animation/accepted reset survival, generation-aware rejection and concrete cancellation vs quiet internal supersession; optional ordered interface/custom fallback retained. |
| Core/Abilities47 | Current cap-aware ValidateSpend/receipt semantics preserve actual stored delta before observers; positive no-effective-debit rejection and 100,000,000−5 storing8 caveat match approved precision contract. No exact-currency guarantee or removed global observer delivery. |
| Clock/RPG49–57 | CooldownReward's overridden GetUtcNow uses NeoTrustedTime; base TimerObject defaults DateTime.UtcNow. Current RpgTickUtility cap1024/remainder reduction plus both consumers implement bounded per-entry/call catch-up; docs correctly avoid frame-time guarantee and preserve UTC expiry despite invalid simulation delta. Effective-Max v2 restore vs later BaseMax refresh, applied-growth anchor behavior/no v3, one-shot stack grant ordering/no restore replay and unsupported AttackSpeedPercent match accepted contracts. ApplyLevelGrowth1217–1229, enum value23 and adapter282–284 are actual APIs/wire path. Dense projectile capacity is described as completeness, not a new gameplay cap or cross-dimension order. |
| Save61 | CreateProvider53–61 throws on false TryCreateProvider70–105 before File provider creation; lazy SaveProvider149–179 calls factory before committing initialization. Assigned component25–42 validates before SetProvider and has no edit-asset auto-retry. Corrective retry advice and preserved active provider are accurate; no plaintext fallback claimed. |
| Progression63 | Current EnsureInitialized245–257 rejects unfinished reentry; initialization finally clears retry guard; ApplyProfile751–771 commits readiness before observers. LoadProfile660–663 and same-key reward-watermark754–766 match retry/commit/same-session wording and Core-before-Progression ordering. |
| Leaderboard65 / ownership67 | Actual readonly legacy id plus serialized FormerlySerializedAs persistent key, explicit binding and LeaderboardLegacyJson.TryDeserialize18–71 exist. Helper accepts only the stated raw legacy aggregate, preserves keyless rows without guessing and rejects duplicate player-key matches. Docs separate authored token metadata from explicit raw JSON path and require real prefab/aggregate roundtrips. Generated-resource/coroutine ownership delegates to canonical guide rather than promising observation of raw native cancellation. |

## Remaining verification and hashes

No bounded doc correction requested. Root still owns any future public changes. After every writer finishes, central operators must run actual link/anchor/HTTP, UTF-8/package/meta checks, literal snippet compilation, imported scene/menu authoring, optional dependency/Unity version matrix, genuine native query counts/audio/UI/lifecycle and remote transport acceptance. Source wording remains UNVERIFIED until those receipts exist. This pass ran none of those validators/executions and does not bless current Parallax/Other/harness corrections.

Whole-file SHA256 below records read identities; source rows identify selected API reads, not full-runtime rereviews.

| Public document / artifact | SHA256 |
|---|---|
| README.md | `4c233dcaf6498fb089688fca89126940edd6775a3be272d86d1a1eae143545f9` |
| Assets/Neoxider/README.md | `46405f47324567d68a99635dcf79895744e1f5b5481d93d3bd583f22ba573663` |
| Assets/Neoxider/Docs/README.md | `4345e4948e7689f91909f8dc1423b62a896a6f222352482afe464a113de9ae7a` |
| Assets/Neoxider/Docs/GettingStarted.md | `a0c5443e3fbba059bab097c0bc71bec6c4082a83a9c91fd28dc2d09f5d390008` |
| Assets/Neoxider/Docs/PackageCompatibility.md | `53779d79633f3f54c4900a36386dc802918a84f0d9d12934d5d4236ccdba7a4e` |
| Assets/Neoxider/Docs/Samples.md | `2fd1c0ccaf3173960a679e10df4c15a13f60ade49fcffa796b70881b3bb39bc8` |
| Assets/Neoxider/Docs/Migration11.md | `fa80b103b6152fa9d088c60ec53ea1e341a10700bdaedb898e8c1ed3be7138cd` |
| Assets/Neoxider/CHANGELOG.md | `ae75282c945df92e2eac90ea445ff379212ec50238deb0779d8741febfef4b29` |
| Docs/Audits/2026-10-23/README-review.implementation.md | `39bad10b99ae1e74404642ea0cf586e9c9cc8c0dcd0ef25882158a085bf65811` |
| Docs/Audits/2026-10-23/README-review.integration.md | `5306ac9805865752016b8fa6032223efdf9c3d452c824771e3a52e7ead87ada9` |
| Docs/Audits/2026-10-23/README-review.implementation-request.md | `2b58243a1b344de5d7ef32021900ad249ed365a0c530ccb74c309159fec6632f` |
| Docs/Audits/2026-10-23/IMPLEMENTATION-CONTRACT.md | `d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab` |
| Assets/Neoxider/package.json | `ef704423999fffb8e70b42475cdbff47bf88f3edeec66aacf7ffcb19546c5576` |
| ProjectSettings/ProjectVersion.txt | `4c9a38500db22864fe56e4a6da3720df8f82bff1c6a47c4235389294867f39a1` |
| Packages/manifest.json | `470a73e9fb0a59fb3a24c83882a66fb94e69c34239a07fbb8f4b8f39687fab53` |

Remaining paths relative to Assets/Neoxider/.

| API/reference source | SHA256 |
|---|---|
| Scripts/Core/Resources/Domain/ResourcePoolModel.cs | `7fe3ac2cebdada0a6b212370cab520f88456a14b9997688c3ec1c5520667213f` |
| Scripts/Core/Resources/Domain/ResourcePoolEntry.cs | `4513c7b7a568c88a5a7e064daf73a51423172a24795757b4ba1480d4fdd19d02` |
| Scripts/Audio/AudioSimple/AM.cs | `6f39f3bb90404bba1e3d27baa9fd82ecbb107ec3ac02331d8a386bc8d83a1f3b` |
| Scripts/Audio/AudioSimple/PlayAudio.cs | `a525111ed8e695e8da21f615ba6404d0ba2d29c207ee810e1ea5689307c28beb` |
| Scripts/StateMachine/StateMachine.cs | `26211741759424ad2e06943b0f918198823f15259feae48360e1a3909f84d50c` |
| Scripts/Shop/Money.cs | `ca141e9f3a54082f66130a6b6ce9877e1f91af136c2886ae630627585bacff0d` |
| Scripts/Save/Settings/SaveProviderSettings.cs | `311d30d4e7f0373070388094ca376eff3294af09f969beb81b940c062bbfca19` |
| Scripts/Save/Settings/SaveProviderSettingsComponent.cs | `eec64d98c5e2697de5b2e9b624fa8ee05a8fcb4b1be9c4d94786fc91a36b62ca` |
| Scripts/Save/SaveProvider.cs | `6195d7abe3096d9126f235b27aaec08c564e5b6851d8fd6a31621fc1ad449cb1` |
| Scripts/Tools/FakeLeaderboard/Leaderboard.cs | `8c3a896137e7bacf5f153fc3619c744265e90b35bf7eb0cedeed9878d4d44269` |
| Scripts/Tools/FakeLeaderboard/LeaderboardLegacyJson.cs | `46e91eedbf43612dca286ad6ab7d0f390819d297a24cfa5c54fc9d7b1eb2be86` |
| Scripts/Tools/Inventory/Runtime/InventoryDropper.cs | `ceb4738d4806899340d72e5cc2e336757d48a47b1afef67276b1ebe7233d5f5c` |
| Scripts/Rpg/Runtime/RpgCharacterNetworkCommand.cs | `a3241ff1a7095b693e1ed810ae5f7af88cc45a48981e9877ca092232036fdd00` |
| Scripts/RpgNetwork/RpgCharacterNetworkAdapter.cs | `41d917bed933ae330e35bec822df5e103b60786667284c75ea12753479c660cb` |
| Scripts/Progression/Components/ProgressionManager.cs | `3388c56b1e7ff148a9bd2aac1043ff223c83639bec962dc4459d182bb03d84ef` |
| Scripts/Tools/Move/CameraRotationController.cs | `206f8e363dcc5026429cdc67bef798018fa0c137703249baaf07a85fe0a82f71` |
| Scripts/Tools/View/Selector.cs | `8117a7719e9f3808a7c48e0afc1f2bfeeed4336c106957d0d133b618a6fc3d15` |
| Scripts/Tools/View/SelectorModel.cs | `45928c9c1ab01181de097ce5ca72d3614eabf6015d80c7a051a6fd87619d116e` |
| Samples~/Demo/Scripts/Neo.Rpg.Demo.asmdef | `fc0afa8c21281f107f4850dfee848edbb1614884da4bfbe5b8fa341f4438bf3f` |
| Scripts/Condition/NeoCondition.cs | `c8fca0ceac06f37e2a861d1f38b3250920d6d30889ece26614a0a18e9f0333f8` |
| Scripts/Shop/Shop.cs | `29c932ce37396bf49dfb95c229a4e20d152f0bf33c62e738cc51ec07e3ab8d31` |
| Scripts/Rpg/Runtime/RpgTickUtility.cs | `88a47df4f1ce82afc7acf7e1d3b9ff2373a49695049096ad50b63109a59a1dca` |
| Scripts/Rpg/Runtime/RpgCharacterResourceService.cs | `3a71ffc8f6528288e5aa9d90fe5a84a375096921e4cef59e5e09b9233936c5f5` |
| Scripts/Rpg/Runtime/RpgEffectShelf.cs | `1c81202e74067d647129ffd79c4323cf04f665b97997a6b4f39256e5280bf41c` |
| Scripts/Rpg/Components/RpgCharacter.cs | `9dfadfbaac83b7797da38ff0064979648ef3214b1fad98a7018a1e733bfd637c` |
| Scripts/Bonus/TimeReward/CooldownReward.cs | `0ff8635f120bfb92ab7f3262f8c802160485a8c0e53c9aa53dea33d28025e32a` |
| Scripts/Tools/Time/TimerObject.cs | `2131a3f3d8fc214490bd06b12b47e52c5bb3b60bd74257c3041301eab8840489` |
