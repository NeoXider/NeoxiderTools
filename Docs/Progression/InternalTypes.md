# Progression runtime types

**What it is:** the public supporting types of the Progression module (`Scripts/Progression/`) that complement the [ProgressionManager](ProgressionManager.md) API.

**How to use:** reference them when configuring bridges, reading profile snapshots, or dispatching rewards from your own systems.

---

| Type | Contract |
|------|----------|
| `ProgressionConditionEvaluationMode` | `HasUnlockedNode`, `HasPurchasedPerk`, `LevelAtLeast`, `XpAtLeast`, `PerkPointsAtLeast`, `HasPremium`. |
| `ProgressionNoCodeActionType` | `AddXp`, `GrantPerkPoints`, `UnlockNode`, `BuyPerk`, `ResetProgression`, `SaveProfile`, `LoadProfile`, `ActivatePremium`. |
| `ProgressionRewardType` | `None`, `Xp`, `PerkPoints`, `Money`, `UnlockCollectionItem`, `AcceptQuest`. |
| `ProgressionReward` | Serialized reward data: integer/monetary amount, optional explicit targets, collection item, quest, note, and `IsPremium`. |
| `ProgressionStringEvent` | Serializable `UnityEvent<string>` used for identifiers and action result messages. |
| `ProgressionProfileData` | Version 2 payload: available perk points, premium ownership, unlocked node IDs, and purchased perk IDs. XP/level are stored separately by Core. |
| `ProgressionRewardDispatcher` | Applies reward descriptors through the manager or existing Money/Collection/Quest APIs. |

Enum values are serialized; new members are appended only.

## Profiles

`Sanitize()` clamps negative points, repairs null identifier lists, and removes empty/whitespace identifiers. `Clone()` copies both lists; null source lists become empty lists in the clone. Manager snapshots are independent of live profile state. Sanitization preserves unknown nonempty identifiers, allowing content to return in a later definition version.

## Rewards

`DispatchRewards(IReadOnlyList<ProgressionReward>, ProgressionManager, bool premiumOnly = false)` initializes the manager before checking entitlement. It skips null entries; normal dispatch delivers free rewards plus premium rewards when `HasPremium` is true. With `premiumOnly = true`, it selects only premium entries for an explicit replay. The indexed overload is used for definition reward lists (`Rewards` properties are `IReadOnlyList`) and allocates nothing; the `IEnumerable<ProgressionReward>` overload remains for other sequences and takes the same indexed path when the sequence is a list.

`DispatchReward(ProgressionReward, ProgressionManager)` is the low-level application API: it does not filter entitlement or track claims. Use `DispatchRewards` for premium-aware filtering, and the manager's level/unlock/purchase methods for normal claim ownership. Missing required reward services/assets produce warnings gated by `NeoDiagnostics`; they do not roll back an unlock or purchase.

```csharp
// Grant a reward list from your own system (for example a daily chest).
ProgressionRewardDispatcher.DispatchRewards(chestRewards, ProgressionManager.Instance);
```

Explicit scene targets belong to scene workflows. A reward stored in a reusable definition asset must leave `MoneyTarget`, `CollectionTarget`, and `QuestManagerTarget` empty; dispatch then falls back to the corresponding service singleton. Collection item and quest configuration assets are reusable references.

[Progression](README.md)
