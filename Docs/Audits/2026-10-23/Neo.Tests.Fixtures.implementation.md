# Neo.Tests.Fixtures — source-final mapping receipt

Same original primary owner `neo-audit-1023-b03-04`, demo / 11.0.0. Source-only runtime boundary and local primary-fixture corrections are implemented; native acceptance remains UNEXECUTED. This receipt supplements the earlier Neo.Editor.Tests implementation inventory. All 54 component bodies plus the contracts source were reviewed. No Unity/compiler/test/checker/installer/service/commit/push/child was launched.

The main assembly contains 46 helpers + one contract source (47 C# files); the Network companion contains eight helpers. Seven standalone CS/meta pairs retain original bytes/GUIDs; 47 embedded helpers were extracted from 28 fixtures. Assertions remain in Neo.Editor.Tests, with aliases for renamed private types. Four marker interfaces and ResourcePath are now runtime test contracts. Two InventorySystemTests payload sites use typeof(TestWalletState).Name. No hardcoded old helper reflection literal was found in reserved consumers; existing typeof(...).AssemblyQualifiedName calls obtain the new identity.

All source paths below are relative to Assets/Neoxider/Tests. Every old assembly was Neo.Editor.Tests. Existing namespaces are retained; exact original/new reflection names, namespace/type, assembly and GUID are recorded in the owner-published Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json, alongside every reserved consumer and runtime contract extraction.

Runtime contract source Fixtures/Audit20261023FixtureContracts.cs contains the collision-safe First/SecondRequirement and two property-contract marker interfaces plus ResourcePath = Audit20261023NeoEditorCollectionFixtures. Fixture aliases and the attribute resource path preserve the runtime assignment contracts.

Edit/Neo.Editor.Tests.asmdef adds Neo.Tests.Fixtures and Neo.Tests.Fixtures.Network in one direction. Main references only actual UI/Neo runtime bases/contracts. Network explicitly references Neo.Network/Mirror, guards six Mirror-specific files and retains two fallback singleton helpers. Both new asmdefs have no Editor parent/platform restriction, UNITY_INCLUDE_TESTS, autoReferenced false and no NUnit/TestRunner/Editor reference. Neither points to a fixture assembly, and no production reference/friend changed. Play has no consumer; its asmdef is unchanged. Mirror absence still needs real profile compilation.

Other primary edits: SampleAssetValidationTests.cs guards its Mirror using and typed network scan only. Absent Mirror is an explicitly inconclusive named capability; the present scan still requires >0 behaviours; generic checks remain. AuditFixesTypewriterTests.cs yields ExitPlayMode from the top-level two cleanup-control UnityTests and UnityTearDown. The nested drain cleans/observes all owned operations and returns the first original fault; native exit precedes fault/timeout assertions or rethrow. No manual iterator exhaustion/private runtime bypass occurs. New RuntimeFixtureBoundaryTests requires actual phase/raycaster/quest attachment and getter/callback controls.

Public entry: Assets/Neoxider/Docs/Testing.md. New analysis/plan: Neo.Tests.Fixtures.md and Neo.Tests.Fixtures.plan.md. Primary completion: Neo.Editor.Tests.implementation.md. New source/folder/asmdef metadata was created without replacing moved metadata. Main Samples~ layout, historical primary audit/plan, native evidence, vendor assets, DocsEn.meta and 314 migration targets remain untouched.

Actual evidence read: source3 graphical Edit XML failures and helper triage, true-absence SampleAssetValidationTests.cs(5,7) CS0246 Mirror, installed UTF EnumeratorHelper/BeforeAfterTestCommandBase nested-mode guard, actual public/protected runtime declarations. Current review is source/reference enumeration and GUID bookkeeping only. No new native result is claimed.

After all writers and a new root freeze: compile both Unity versions/present+true-absent profiles; require live attachment, real phase/raycast/getter and original negative/nonempty controls, reflection/binding and save/inventory identities, cleanup/static state, top-level mode exit before fault assertions. Rerun the existing behavioral slices and new smoke, then report actual outcomes. No production internal API blocks this source work; unrelated module defects remain with their owners.

Exact helper mapping:

| Original file :: helper | New file | Target assembly | GUID |
| --- | --- | --- | --- |
| Edit/Core/Audit20261023NeoToolsManagersBootstrapTests.cs :: Audit20261023NeoToolsManagersBootstrapProbe | Fixtures/Managers/Audit20261023NeoToolsManagersBootstrapProbe.cs | Neo.Tests.Fixtures | 8c81888804e84bd78295c2b97b92a36a |
| Edit/Core/Audit20261023NeoToolsManagersBootstrapTests.cs :: Audit20261023NeoToolsManagersInitProbe | Fixtures/Managers/Audit20261023NeoToolsManagersInitProbe.cs | Neo.Tests.Fixtures | cbf22b9dbd1742d6af3957f06a5b6b17 |
| Edit/Core/Audit20261023NeoToolsManagersGameStateTests.cs :: Audit20261023NeoToolsManagersGameManagerProbe | Fixtures/Managers/Audit20261023NeoToolsManagersGameManagerProbe.cs | Neo.Tests.Fixtures | 0bed55c4512e4f908f12de45c41bf297 |
| Edit/Core/Audit20261023NeoToolsManagersGameStateTests.cs :: Audit20261023NeoToolsManagersEventHubProbe | Fixtures/Managers/Audit20261023NeoToolsManagersEventHubProbe.cs | Neo.Tests.Fixtures | 8c91f53ae64a41e48626086dfd5e8ec6 |
| Edit/Core/AuditFixesStaticsTests.cs :: AuditFixesSingletonRoot | Fixtures/Managers/AuditFixesSingletonRoot.cs | Neo.Tests.Fixtures | 0df75eb918504bf3ac883dd77004d83e |
| Edit/Core/AuditFixesStaticsTests.cs :: AuditFixesSingletonLeaf | Fixtures/Managers/AuditFixesSingletonLeaf.cs | Neo.Tests.Fixtures | 1fb1ce37d16544cf80ec6432f906a835 |
| Edit/Core/BootstrapTests.cs :: TestInitializable | Fixtures/Managers/Audit20261023FixtureBootstrapTestInitializable.cs | Neo.Tests.Fixtures | 402a9bcf4d8347aeb66d0f8bb5078bb5 |
| Edit/Editor/Audit20261023NeoEditorBindingInspectorTests.cs :: NumericProbe | Fixtures/Inspector/Audit20261023FixtureNeoEditorBindingInspectorNumericProbe.cs | Neo.Tests.Fixtures | f7486b02d56244bc9c28bb089343077a |
| Edit/Editor/Audit20261023NeoEditorBindingInspectorTests.cs :: BindingProbe | Fixtures/Inspector/Audit20261023FixtureNeoEditorBindingInspectorBindingProbe.cs | Neo.Tests.Fixtures | 15ea036b29b046b1b68846e6c4609456 |
| Edit/Editor/Audit20261023NeoEditorCollectionTests.cs :: PartialCandidate | Fixtures/Inspector/Audit20261023FixtureNeoEditorCollectionPartialCandidate.cs | Neo.Tests.Fixtures | 421453b68b814873a4e7c52416ac2b47 |
| Edit/Editor/Audit20261023NeoEditorCollectionTests.cs :: SecondCandidate | Fixtures/Inspector/Audit20261023FixtureNeoEditorCollectionSecondCandidate.cs | Neo.Tests.Fixtures | a145334d349f40d88934b1ccde0c04a8 |
| Edit/Editor/Audit20261023NeoEditorCollectionTests.cs :: CompleteCandidate | Fixtures/Inspector/Audit20261023FixtureNeoEditorCollectionCompleteCandidate.cs | Neo.Tests.Fixtures | e931613a45d840859630b27c6550bbe1 |
| Edit/Editor/Audit20261023NeoEditorCollectionTests.cs :: ComponentFindProbe | Fixtures/Inspector/Audit20261023FixtureNeoEditorCollectionComponentFindProbe.cs | Neo.Tests.Fixtures | 0785027ddb1d4b46a5fb15491c4f27fb |
| Edit/Editor/Audit20261023NeoEditorCollectionTests.cs :: GameObjectFindProbe | Fixtures/Inspector/Audit20261023FixtureNeoEditorCollectionGameObjectFindProbe.cs | Neo.Tests.Fixtures | 29db7b21cb134d06833926b9b8ce388c |
| Edit/Editor/Audit20261023NeoEditorCollectionTests.cs :: CollectionProbe | Fixtures/Inspector/Audit20261023FixtureNeoEditorCollectionCollectionProbe.cs | Neo.Tests.Fixtures | 69798ed9b572432384aab14de4686522 |
| Edit/Extensions/Audit20261023CoroutineUnityPhaseProbe.cs :: Audit20261023CoroutineUnityPhaseProbe | Fixtures/Extensions/Audit20261023CoroutineUnityPhaseProbe.cs | Neo.Tests.Fixtures | 20dee4576f374a14b4a2bbeaa91dc301 |
| Edit/Extensions/Audit20261023CoroutineUnityRaycaster.cs :: Audit20261023CoroutineUnityRaycaster | Fixtures/Extensions/Audit20261023CoroutineUnityRaycaster.cs | Neo.Tests.Fixtures | f3ecca1d50d940d4b3e8c49bef29a591 |
| Edit/Network/Audit20261023NeoNetworkSceneTests.cs :: Audit20261023NeoNetworkSingleton | Fixtures/Network/Audit20261023NeoNetworkSingleton.cs | Neo.Tests.Fixtures.Network | 50e6a1c8713c4716b5c91f3cb44dfcdb |
| Edit/Network/Audit20261023NeoNetworkSceneTests.cs :: Audit20261023NeoNetworkDerivedSingleton | Fixtures/Network/Audit20261023NeoNetworkDerivedSingleton.cs | Neo.Tests.Fixtures.Network | b6b2b7934c004231a6743b297564a0e2 |
| Edit/Network/Audit20261023NeoNetworkSceneTests.cs :: Audit20261023NeoNetworkMethodReceiver | Fixtures/Signals/Audit20261023NeoNetworkMethodReceiver.cs | Neo.Tests.Fixtures | 3df2ab0ed3c545978918c70fb7deaa27 |
| Edit/Network/NeoNetworkManagerHardeningTests.cs :: ManagerProbe | Fixtures/Network/Audit20261023FixtureNeoNetworkManagerHardeningManagerProbe.cs | Neo.Tests.Fixtures.Network | 81cfdbfafa904f37aafe0e6f56890736 |
| Edit/Network/NeoNetworkManagerHardeningTests.cs :: UnsupportedTransport | Fixtures/Network/Audit20261023FixtureNeoNetworkManagerHardeningUnsupportedTransport.cs | Neo.Tests.Fixtures.Network | 806d1413f104464687d664fdf5d68483 |
| Edit/Network/NetworkContextActionIngressTests.cs :: ManagerProbe | Fixtures/Network/Audit20261023FixtureNetworkContextActionIngressManagerProbe.cs | Neo.Tests.Fixtures.Network | a04eb336950d405687a9d2e872c0ad24 |
| Edit/Network/NetworkContextActionIngressTests.cs :: CooldownRelay | Fixtures/Network/Audit20261023FixtureNetworkContextActionIngressCooldownRelay.cs | Neo.Tests.Fixtures.Network | 10ac7edcf732491cad53fd1dd0456537 |
| Edit/Network/NetworkRateLimitTests.cs :: RateLimitProbe | Fixtures/Network/Audit20261023FixtureNetworkRateLimitRateLimitProbe.cs | Neo.Tests.Fixtures.Network | 54e10f3fa5fd4f2d88f8d11499c97c9f |
| Edit/Network/NetworkSessionHelpersTests.cs :: RetrySingleton | Fixtures/Network/Audit20261023FixtureNetworkSessionHelpersRetrySingleton.cs | Neo.Tests.Fixtures.Network | 9543cbfc94bb4efcb2a612767b889a57 |
| Edit/NoCode/Audit20261023NeoNoCodeBindingTests.cs :: Audit20261023NeoNoCodeSource | Fixtures/NoCode/Audit20261023NeoNoCodeSource.cs | Neo.Tests.Fixtures | f8bade8006164bf5ac0dad4ed77f1e94 |
| Edit/NoCode/Audit20261023NeoNoCodeFollowupTests.cs :: Audit20261023NeoNoCodeFollowupSource | Fixtures/NoCode/Audit20261023NeoNoCodeFollowupSource.cs | Neo.Tests.Fixtures | c9a37399f8f94ebd94c237cbb500a610 |
| Edit/NoCode/NoCodeBindEditModeTests.cs :: NoCodeBindEditModeFloatSource | Fixtures/NoCode/NoCodeBindEditModeFloatSource.cs | Neo.Tests.Fixtures | 4e2d6f7cd0cb46898407e612a2709612 |
| Edit/PropertyAttribute/Audit20261023NeoPropertyAttributeRegressionTests.cs :: ContractComponent | Fixtures/PropertyAttribute/Audit20261023FixtureNeoPropertyAttributeRegressionContractComponent.cs | Neo.Tests.Fixtures | ae28fa7714fb4436904e674d54522b03 |
| Edit/PropertyAttribute/PropertyAttributeEditModeTests.cs :: TestContractComponent | Fixtures/PropertyAttribute/Audit20261023FixturePropertyAttributeEditModeTestContractComponent.cs | Neo.Tests.Fixtures | 5a6ad16a814842a99b93e1e608f0b14f |
| Edit/Quest/Audit20261023NeoQuestConditionProbe.cs :: Audit20261023NeoQuestConditionProbe | Fixtures/Quest/Audit20261023NeoQuestConditionProbe.cs | Neo.Tests.Fixtures | a415c9e0597a4e22ba32cf18e62ed0fe |
| Edit/Reactive/ReactivePropertyLifetimeTests.cs :: DestroyableSubscriber | Fixtures/Reactive/Audit20261023FixtureReactivePropertyLifetimeDestroyableSubscriber.cs | Neo.Tests.Fixtures | 1183e1ed4f684b75a423612e548b4fe0 |
| Edit/RPG/Audit20261023NeoRpgImplementationCharacterTests.cs :: Audit20261023NeoRpgSnapshotProbe | Fixtures/RPG/Audit20261023NeoRpgSnapshotProbe.cs | Neo.Tests.Fixtures | a74ea8a7cbe142c9bfa6e391ecf4a664 |
| Edit/RPG/RpgCombatEdgeTests.cs :: TestCombatReceiver | Fixtures/RPG/Audit20261023FixtureRpgCombatEdgeTestCombatReceiver.cs | Neo.Tests.Fixtures | b2fb461be3774fbf8cbad083420d5a72 |
| Edit/Save/Audit20261023NeoSaveManagerTests.cs :: TestSaveable | Fixtures/Save/Audit20261023FixtureNeoSaveManagerTestSaveable.cs | Neo.Tests.Fixtures | e51cce4c37fc44f190f133f004aaa29f |
| Edit/Save/Audit20261023NeoSaveManagerTests.cs :: BaseSaveable | Fixtures/Save/Audit20261023FixtureNeoSaveManagerBaseSaveable.cs | Neo.Tests.Fixtures | 5df767bc68d241e9996306f2c46c5ca5 |
| Edit/Save/Audit20261023NeoSaveManagerTests.cs :: DerivedSaveable | Fixtures/Save/Audit20261023FixtureNeoSaveManagerDerivedSaveable.cs | Neo.Tests.Fixtures | e608e7eec0644b2586244f03b3414337 |
| Edit/Save/Audit20261023NeoSaveManagerTests.cs :: SceneCallbackHost | Fixtures/Save/Audit20261023FixtureNeoSaveManagerSceneCallbackHost.cs | Neo.Tests.Fixtures | 5157788109c74974b25c427a4cf77cb1 |
| Edit/Save/SaveIdentityUtilityTests.cs :: StableSaveable | Fixtures/Save/Audit20261023FixtureSaveIdentityUtilityStableSaveable.cs | Neo.Tests.Fixtures | 9cb78512287243128310618544ef6a64 |
| Edit/Save/SaveIdentityUtilityTests.cs :: CustomIdentitySaveable | Fixtures/Save/Audit20261023FixtureSaveIdentityUtilityCustomIdentitySaveable.cs | Neo.Tests.Fixtures | 765e34997d64456aa390dcf392b2b34c |
| Edit/Save/SaveManagerReadModifyWriteTests.cs :: TestSaveable | Fixtures/Save/Audit20261023FixtureSaveManagerReadModifyWriteTestSaveable.cs | Neo.Tests.Fixtures | ae52393b3cec4855a377e488ff1b8655 |
| Edit/Save/SaveManagerReadModifyWriteTests.cs :: MixedSaveable | Fixtures/Save/Audit20261023FixtureSaveManagerReadModifyWriteMixedSaveable.cs | Neo.Tests.Fixtures | 21ca3025801446de90aed234b3403f91 |
| Edit/Save/ShopManagerTests.cs :: PendingAuthorityMoneySpend | Fixtures/Save/Audit20261023FixtureShopManagerPendingAuthorityMoneySpend.cs | Neo.Tests.Fixtures | 179963ff885c4015bf9aca839f29b83a |
| Edit/Shop/ShopResolverTests.cs :: ShopResolverContext | Fixtures/Shop/ShopResolverContext.cs | Neo.Tests.Fixtures | dbfb07eb967e47eca4f0ba441c5dea1c |
| Edit/Shop/ShopResolverTests.cs :: ShopResolverProbe | Fixtures/Shop/ShopResolverProbe.cs | Neo.Tests.Fixtures | 8fb5f9e0a269430983b60c17880ec596 |
| Edit/Shop/ShopVariantsPanelTests.cs :: TestVariantView | Fixtures/Shop/Audit20261023FixtureShopVariantsPanelTestVariantView.cs | Neo.Tests.Fixtures | d27ddec480764356b4bace01dcfd56b6 |
| Edit/StateMachine/Audit20261023NeoStateMachineGenericHost.cs :: Audit20261023NeoStateMachineGenericHost | Fixtures/StateMachine/Audit20261023NeoStateMachineGenericHost.cs | Neo.Tests.Fixtures | 4fb6bbcf9f864573988388a8790ae242 |
| Edit/Tools/Audit20261023NeoConditionAmbiguousIndexerProbe.cs :: Audit20261023NeoConditionAmbiguousIndexerProbe | Fixtures/Tools/Audit20261023NeoConditionAmbiguousIndexerProbe.cs | Neo.Tests.Fixtures | d630847f589b493ea87e923cda8a19d4 |
| Edit/Tools/Audit20261023NeoConditionValueProbe.cs :: Audit20261023NeoConditionValueProbe | Fixtures/Tools/Audit20261023NeoConditionValueProbe.cs | Neo.Tests.Fixtures | 73ff714ffd9b442fa7151dbc8ad38b10 |
| Edit/Tools/Audit20261023NeoToolsTimeTimerProbe.cs :: Audit20261023NeoToolsTimeTimerProbe | Fixtures/Tools/Audit20261023NeoToolsTimeTimerProbe.cs | Neo.Tests.Fixtures | 0ff34b1aa4f84266a21471afafe212e2 |
| Edit/Tools/NeoConditionTests.cs :: TestComponent | Fixtures/Tools/Audit20261023FixtureNeoConditionTestComponent.cs | Neo.Tests.Fixtures | 493c6e7b8d314d57861c54416a8e34c4 |
| Edit/Tools/Inventory/Audit20261023NeoToolsInventoryRuntimeTests.cs :: RestoreFailureState | Fixtures/Tools/Audit20261023FixtureNeoToolsInventoryRuntimeRestoreFailureState.cs | Neo.Tests.Fixtures | 16b795605e554c60ba6ed49ef4d27111 |
| Edit/Tools/Inventory/InventorySystemTests.cs :: TestWalletState | Fixtures/Tools/Audit20261023FixtureInventorySystemTestWalletState.cs | Neo.Tests.Fixtures | 64396382c6944707baa172a5eeb2474e |

Exact edited existing source/asmdef files:

- Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoToolsManagersBootstrapTests.cs
- Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoToolsManagersGameStateTests.cs
- Assets/Neoxider/Tests/Edit/Core/AuditFixesStaticsTests.cs
- Assets/Neoxider/Tests/Edit/Core/BootstrapTests.cs
- Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorBindingInspectorTests.cs
- Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorCollectionTests.cs
- Assets/Neoxider/Tests/Edit/Editor/SampleAssetValidationTests.cs
- Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef
- Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkSceneTests.cs
- Assets/Neoxider/Tests/Edit/Network/NeoNetworkManagerHardeningTests.cs
- Assets/Neoxider/Tests/Edit/Network/NetworkContextActionIngressTests.cs
- Assets/Neoxider/Tests/Edit/Network/NetworkRateLimitTests.cs
- Assets/Neoxider/Tests/Edit/Network/NetworkSessionHelpersTests.cs
- Assets/Neoxider/Tests/Edit/NoCode/Audit20261023NeoNoCodeBindingTests.cs
- Assets/Neoxider/Tests/Edit/NoCode/Audit20261023NeoNoCodeFollowupTests.cs
- Assets/Neoxider/Tests/Edit/NoCode/NoCodeBindEditModeTests.cs
- Assets/Neoxider/Tests/Edit/PropertyAttribute/Audit20261023NeoPropertyAttributeRegressionTests.cs
- Assets/Neoxider/Tests/Edit/PropertyAttribute/PropertyAttributeEditModeTests.cs
- Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationCharacterTests.cs
- Assets/Neoxider/Tests/Edit/RPG/RpgCombatEdgeTests.cs
- Assets/Neoxider/Tests/Edit/Reactive/ReactivePropertyLifetimeTests.cs
- Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveManagerTests.cs
- Assets/Neoxider/Tests/Edit/Save/SaveIdentityUtilityTests.cs
- Assets/Neoxider/Tests/Edit/Save/SaveManagerReadModifyWriteTests.cs
- Assets/Neoxider/Tests/Edit/Save/ShopManagerTests.cs
- Assets/Neoxider/Tests/Edit/Shop/ShopResolverTests.cs
- Assets/Neoxider/Tests/Edit/Shop/ShopVariantsPanelTests.cs
- Assets/Neoxider/Tests/Edit/Tools/AuditFixesTypewriterTests.cs
- Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryRuntimeTests.cs
- Assets/Neoxider/Tests/Edit/Tools/Inventory/InventorySystemTests.cs
- Assets/Neoxider/Tests/Edit/Tools/NeoConditionTests.cs
