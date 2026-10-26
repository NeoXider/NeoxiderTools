# Gameplay Edit Mode tests

Gameplay tests live under `Assets/Neoxider/Tests/Edit` in `Neo.Editor.Tests`. The assembly targets the Unity Editor and requires `UNITY_INCLUDE_TESTS`; it is excluded from player builds. Use the Unity Test Runner's **EditMode** tab and filter by fixture or namespace. A successful static review does not establish compilation or a passing Unity run.

This is the gameplay contract-testing entry point. See [utility tests](./Utilities.md) for utility-specific coverage and [network Play Mode tests](./NetworkPlayMode.md) for real message-exchange evidence. Return to the [documentation index](../README.md) for module navigation.

## Public contracts to exercise

| Area | Runtime API | Observable result |
| --- | --- | --- |
| Abilities | `AbilitySystem.Cast`, `ExecuteEffects`, `Tick`, `Modifiers` | Health/resources, rejection reason, cooldown/charges, modifier lifetime and event receipts |
| Grid games | `FieldGenerator.GenerateField`, `GridPlacementService.Place`, `SlidingMergeResolver.Slide`, `DiceBoard.Place` | Content, occupancy, blockers/masks, changed positions and exact notification count |
| Cards and merge | `DeckModel`, `HandModel`, `MergeResolver.Resolve` | Order and duplicate multiplicity, capacity, conservation, dry-run immutability and cascade bounds |
| Progression and quests | `LevelModel`, `ProgressionManager`, `QuestManager` | Thresholds, exact rewards/objective progress, restored state and idempotent events |
| Shop and resources | `Shop.Buy`, `Money`, `ResourcePoolModel`, RPG service APIs | Actual spent/refunded amounts, ownership, caps and persistence |
| State and animation | `StateMachine<IState>`, `TransformAnimationEvaluator.Evaluate` | Lifecycle order, transition decisions and evaluated pose |

Prefer plain C# models and explicit `Tick(deltaTime)` inputs when the behavior permits. For a component facade, create only its required GameObjects/assets and configure dependencies before initialization. `FieldGenerator` can generate an inactive field through its public API, which avoids acquiring its scene singleton.

## Assertions that detect regressions

- Assert exact state and receipts after a successful operation. Check that an event fired before checking its payload; a callback containing assertions can otherwise pass without running.
- Assert state and receipt counts remain unchanged after rejection. For a multi-cell footprint, make a later entry fail and inspect every touched cell, including occupancy.
- Exercise both outcomes of a probability decision with controlled inputs. Same-seed replay alone also passes if the probabilistic node always runs or never runs.
- For batches, inspect the complete committed state from the first observer callback. Verify notification suppression separately without weakening state assertions.
- Derive expected values from hand-calculated examples or an independent contract. Calling a method and reproducing its arithmetic in the assertion does not test the complete purchase or payout workflow.

`Audit20261023TestGameplayProbabilityTests` supplies controlled rolls below, at and above the existing `IRandomSource` chance threshold. It checks health, receipt counts and source/target identity, the final damage amount, and total reported damage against the known health removed in a scenario without shields, healing or reactions. `Audit20261023TestGameplayPlacementTests` checks seven rejection paths, both notification modes and a dice merge that must actually publish consistent cell changes and one logical board change. These two fixtures define 14 cases; authored cases are not execution evidence.

## Shop purchase acceptance in 11.0

Test the failure boundary that the purchase API actually promises. Profile mutation and profile persistence belong to the compensated purchase: fail the profile-store write to verify restoration of ownership and refund of the debit. Throwing from `OnPurchasedId` is not a persistence-failure injection.

After commit, visual refresh, `OnShopChanged` and purchase notifications observe an already committed purchase. A failing observer must be reported separately and must not refund the price, remove committed ownership or reverse an accepted inventory grant. For a bundle, verify that later eligible notifications still run after an earlier observer fails. Check wallet, profile and inventory quantities together, and verify that a nested `Buy`/`BuyBundle` request is rejected before another debit.

This is the intentional 11.0 event-contract change: legacy rollback fixtures that injected failure in a purchase observer must inject a real profile-store failure instead. Separate tests cover observer faults after commit. Existing inspector event connections still invoke the same public methods; granting through arbitrary UnityEvent listeners remains best effort and does not supply general transaction rollback.

## Isolation and cleanup

Capture `SaveProvider.CurrentProvider` before installing an in-memory provider and restore that exact instance in `finally`/teardown, after destroying test components that might save during destruction. Do not clear the developer's provider or restore an assumed default provider. `Money` persistence settings do not disable `Shop` profile writes, so isolate the provider for shop tests too.

Own each created GameObject, ScriptableObject, sprite, texture and tween from creation until cleanup. Keep cleanup reachable after an assertion or setup failure. If a configuration helper creates a replacement asset, destroy or retain the previous asset for teardown. Restore any modified scene loader, singleton, diagnostics configuration, culture and `UnityEngine.Random.state`.

Serialize fixtures that mutate process-wide state. A dedicated temporary save key prevents collisions but does not justify deleting a pre-existing value with that key. Test doubles that write invariant numeric strings must parse them with `CultureInfo.InvariantCulture` as well.

Reward tests should supply fixed time through the existing `INeoClock`, `TrustedClock` and `NeoTrustedTime` seams instead of waiting or preparing timestamps from the real wall clock. Scope both the provider and clock, including high-water restoration state, so the next fixture cannot inherit test time or persisted marks.

## Scene and NoCode coverage

Pair a model test with a narrow component test when scene integration adds behavior. For example, configure an `AbilityNoCodeAction`, execute it, and verify the same unit health/slot change as the typed runtime call together with its success/failure UnityEvent. Private lifecycle invocation can test a handler in isolation; it does not prove Unity schedules that handler, advances a coroutine, or applies a tween across frames.

Use Play Mode for real enable/disable scheduling, coroutine cancellation, projectile flight, UI interaction, scene loading and visual animation. A coroutine test that skips because Edit Mode did not create a handle is not evidence of cancellation. Keep unavailable-environment skips explicit and distinguish them from regressions.

Imported sample scenes may live in `Assets/Samples/NeoxiderTools/<version>/<sample name>`. Package authoring can use `Assets/Neoxider/Samples`, while release packaging uses `Assets/Neoxider/Samples~`; gameplay contract tests should not require any one sample layout.

## Verification handoff

Run the relevant Edit Mode fixtures centrally after concurrent source edits finish, then investigate failures before broadening the run. Include an alternate decimal culture for persistence doubles and supported Unity versions when validating compatibility. Unity 6.0–6.4 and 6.5+ require their own compile results; static checks cannot establish that both API branches compile.

For the two supplemental fixtures, the filters are `Neo.Editor.Tests.Abilities.Audit20261023TestGameplayProbabilityTests` and `Neo.Editor.Tests.GridSystem.Audit20261023TestGameplayPlacementTests`. Keep their core acceptance separate from coroutine cancellation, card tween activity and scene/NoCode lifecycle scheduling, which require their own Play Mode observations. Version, platform and optional-dependency gaps remain explicit until executed on the relevant validation host.

Related runtime documentation: [Abilities](../Abilities/README.md), [GridSystem](../GridSystem/README.md), [Cards](../Cards/README.md), [Merge](../Merge/README.md), [Shop](../Shop/README.md), [StateMachine](../StateMachine/README.md).
