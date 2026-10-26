# Neo.Abilities: corrections after coordinator review

Status: bounded source correction complete; execution and automated checks deferred until the coordinator's global source freeze.

The original [audit](./Neo.Abilities.md), [plan](./Neo.Abilities.plan.md) and [earlier independent review](./Neo.Abilities-review.md) remain phase snapshots. The coordinator personally reviewed the audit and plan and requested the corrections below in the same original owner session. The earlier review pass applies to its recorded fingerprints, not this later delta.

## Scope and ownership

This round changes only:

- `Assets/Neoxider/Scripts/Abilities/Domain/AbilitySystem.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Events/AbilityEventBus.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Casting/AbilitySlot.cs` (API documentation)
- `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesRuntimeTests.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesSceneTests.cs`
- `Assets/Neoxider/Docs/Abilities/README.md`
- `Assets/Neoxider/Docs/Abilities/AbilityLibrary.md`
- `Assets/Neoxider/Docs/Abilities/AbilityDefinition.md`
- `Assets/Neoxider/Docs/Abilities/AbilityAutoCaster.md`
- `Assets/Neoxider/Docs/Abilities/AbilityNoCodeAction.md`
- This correction report.

No changes to Core, asmdefs, package/settings, sample layout, original audit/plan, or the earlier shield implementation. No typed delivery, catalog compiler or batch/transaction API was implemented. Existing unique test metadata is retained; no new package files require metadata in this round.

## AC-01 / P2: catalog replacement mixed incompatible granted and cast data

**Evidence/reproduction.** `GrantAbility` stores an `AbilitySlot` with its original `Blueprint` reference. Slot readiness, charge restoration and `AbilityAutoCaster` selection already read that reference. `Cast` instead looked up the current catalog blueprint. Grant a Unit-targeted, 10-mana, 10-damage, 6-second ability, then register another object under the same case-insensitive id with different damage/cost/cooldown/charge mode: subsequent casts mixed the replacement's rules with the original slot state. Replacing Unit targeting with NoTarget also made the auto-caster's original target request disagree with execution.

**Correction.** `Cast` preserves the existing UnknownCaster → UnknownAbility → NotGranted validation order, then uses `slot.Blueprint` throughout targeting, costs, effects, delivery, cooldown and charge decisions. Registration replaces the catalog for future newly created slots. Repeated grant returns the existing slot unchanged; explicit revoke/regrant creates a fresh slot bound to the replacement, level 1, ready cooldown and full initial charges.

The automatic selector and cooldown source already used granted slots and needed no source changes. New scene regression covers original Unit targeting after replacement and the new NoTarget behavior after revoke/regrant. Pending projectile casts retain the granted blueprint reference already stored in the existing pending-cast object.

**Public regressions.** `CatalogReplacement_ExistingGrantKeepsItsCostsEffectsCooldownAndCharges` covers classic→charged and charged→classic catalog replacements, same-slot identity, retained level, actual resource payment/damage, effective timing, charges and failure reasons. `CatalogReplacement_FutureGrantAndExplicitRegrantUseTheReplacement` covers new-unit grants and explicit regrant/reset. `CatalogReplacement_ProjectileUsesTheGrantedDeliveryAndImpact` covers deferred original delivery/archetype and damage. `AutoCast_CatalogReplacementKeepsGrantedTargetingUntilRegrant` covers automatic request/target selection and cache refresh.

**Compatibility boundary.** This is reference retention, not a deep immutable snapshot. Blueprints and effect lists remain mutable shared objects; changing the held object can still affect later execution. Configure and treat them as read-only. Existing late lookup of a durable modifier's named specials by `SourceAbilityId` remains outside this cast/grant correction; no new promise of modifier-data snapshots is made. Public signatures and serialized fields are unchanged.

## AC-02 / P2: clearing subscriptions disabled internal modifier reactions

**Evidence/reproduction.** The system installed `HandleModifierReactions` using the same `SubscribeAny` list exposed to consumers. `Events.Clear()` removed it. A defender with a thorns modifier stopped reflecting damage after clearing subscriptions, although the modifier remained active.

**Correction.** Preserve the public standalone `AbilityEventBus()` constructor and add an internal constructor accepting the system-owned handler. `Clear` removes specific and any user subscriptions, without replacing/removing the internal route. Publication remains specific handlers → internal reactions → any handlers. The any-handler snapshot is captured before internal reactions, matching the former first-any-handler position of the reaction route. A nested reaction clearing subscriptions therefore cannot discard an already captured outer observer; future publications see the cleared user lists.

**Public regressions.** `EventsClear_RemovesUserSubscriptionsAndKeepsModifierReactions` observes thorns damage and removal of old user subscribers. `EventsClear_PreservesSpecificReactionAnyHandlerOrder` observes exact relative order through damage receipts. `NestedReactionClearingUsers_KeepsCurrentAnySnapshotAndFutureReactions` covers nested Clear, the current outer snapshot and subsequent reflection. `StandaloneEventBus_ClearRemovesUsersAndKeepsCurrentAnySnapshot` preserves standalone behavior.

No exception-isolation or whole-transaction guarantee is added. User-handler exceptions still propagate and can stop later publication phases. The earlier shield break ordering and internal breaking-state behavior were not edited.

## AC-03 / P2: capped resource costs were accepted after underpayment

**Evidence/reproduction.** With mana=100, `MaxDecreaseAmount=1` and cost=10, the original ability path prechecked only balance, called clipped `Decrease`, ignored its returned amount and accepted the cast after paying 1. A cap failure in a later resource also needed to be detected before an earlier resource was paid. Recomputing costs during the payment loop allowed the first resource's callback to alter later resource ids, amounts or `mana_cost_mul` after validation.

**Cross-assembly integration.** The original Neo.Core owner added concrete `ResourcePoolModel.CanSpend(string, float, out string)`, sharing exact preflight with `TrySpend`. This worker inspected the new read-only API and reused it; Core was not edited. No provider-interface change or dependency addition was needed because Neo.Abilities already references Neo.Core.

**Correction.** Capture authored resource ids and effective amounts once into a cast-local `AbilityCost[]` before callbacks. Reject nonfinite values as in the previous QA correction. Preflight every positive entry with CanSpend, retaining per-authored-operation cap semantics, and independently check cumulative case-insensitive resource balances. Then pay captured entries with exact TrySpend. A callback making a later entry unpayable returns `NotEnoughResources` before charges, cooldown, cast receipt or effects; it cannot turn clipped payment into success.

Repeated entries are not merged into one capped payment: two costs of 6 each can use a per-operation cap of 10 when the balance covers 12. For zero costs the captured storage is `Array.Empty<AbilityCost>()`; nonempty lists allocate a cast-local array. No allocation/performance measurement is claimed.

**Explicit limit.** CanSpend is preflight, not reservation. If an earlier successful payment callback invalidates a later entry, earlier completed payments remain spent; the exact failing operation does not partially spend. Arbitrary callback mutation, exceptions, full multi-resource rollback and atomic publication remain outside this correction. The general batch API remains a proposal.

**Public regressions.** `CappedResourceCost_RejectsBeforeAnyPaymentOrCastState` covers cap=1/cost=10 both alone and after another resource, asserting no payments/events/charge/cooldown/effects. `RepeatedResourceCosts_ApplyTheDecreaseCapToEachAuthoredEntry` covers the non-merged cap semantics. `PaymentCallbacks_UseCapturedResourceIdsAndEffectiveAmounts` covers multiplier mutation and definition/id mutation. `PaymentCallback_LateCapChangeRejectsCastWithoutClippingTheNextPayment` covers a cap invalidated after preflight, rejection and the documented earlier-payment limit.

## Verification boundary and source handoff

Authored this round: 12 public regression methods / 15 cases. Source inventory across the two existing unique audit files is now 39 methods / 52 cases, including all earlier regressions. These are authored counts, not execution results.

The coordinator's latest stage barrier requires all source writers to finish before tests/checks. Accordingly, no Unity/MCP, compilation, test process, build, new automated static-check run, dependency install, commit or child agent was started for this later delta. Source/API bodies were read while implementing; prior green audit checks do not verify the changed files at this handoff.

After the global freeze the coordinator should:

1. Review this delta with the Core CanSpend integration, including the captured-cost and exact-payment boundaries.
2. Compile supported Unity 6.0–6.4 and 6.5+ branches, including the current Unity 6.6 environment; preserve centralized compatibility helpers. No `NonParallelizable` attributes were added.
3. Execute a nonempty Abilities EditMode selection containing all 52 audit cases plus existing cast, charge, damage, modifier, reaction, event and projectile tests; record actual selected/pass/fail counts.
4. Execute focused real PlayMode checks for auto-cast replacement/regrant and the earlier lifecycle/level-source/shield scenarios. The manual EditMode scene rig is not lifecycle evidence.
5. Run scoped whitespace/encoding/metadata/link checks and the package mojibake test after source completion.

This worker stops source work at this bounded handoff. The original audit/plan remain historical phase snapshots; this document records which requested plan defects and cap correction are now implemented but still unverified by execution.
