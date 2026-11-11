# RPG Module — Internal Types

**What it is:** the supporting public types of the RPG module (`Scripts/Rpg/`): enums, data/config types, events,
runtime services and the clock and callback contracts.

**How to use:** look a type up here when a component page references it; behaviour lives on the linked pages.

---

## Enums
| Type | Description |
|------|-------------|
| `BuffStatType` | Legacy combat aliases and universal stat/resource/regen modifier kinds. |
| `RpgAttackDeliveryType` | `Direct`, `Area`, `Projectile`. |
| `RpgHitMode` | `Damage` or `Heal`. |
| `RpgInputTriggerType` | `MouseButton` or `KeyCode`, read through input compatibility helpers. |
| `RpgMouseButton` | Mouse button (Left, Right, Middle). |
| `RpgTargetSelectionMode` | Distance, health, level, or random selection among receivers. |
| `RpgConditionEvaluationMode` | Character/resource/stat/controller predicates for the optional condition bridge. |
| `RpgNoCodeActionType` | No-Code action type for RPG. |

## Data / Config
| Type | Description |
|------|-------------|
| `AuraWeapon` | Scene weapon component driven by a timer or `ApplyAuraDamage()`. |
| `BuffStatModifier` | Modifier kind, amount, stat/resource ID, and optional damage type. |
| `InlineBuffEntry` | Inspector-authored buff data without a separate asset; same stacking lifecycle as an SO buff. |
| `RpgAttackEffectRefs` | Target buff IDs, target status IDs, and self buff IDs applied on a hit. |
| `RpgButtonBinding` | Button-to-RPG action binding. |
| `RpgStatGrowthDefinition` | Stat growth definition (formula, coefficients). |
| `RpgTargetQuery` | Target search query; every property has a setter for C# configuration. |
| `RpgCharacterProfileData` | Universal RPG character profile data. |
| `ActiveBuffEntry` / `ActiveStatusEntry` | Active buffs and statuses for runtime/save/network. |
| `RpgResourceDefinition` / `RpgStatDefinition` | Resource pool and single-value stat authoring data. |
| `RpgRegenDefinition` | Continuous/tick regen, stat scaling, alive/full gates, and pause windows. |
| `RpgStatUpgradeRule` / `RpgResourceModifier` | Upgrade cost/cap, stat increment, and derived resource max/regen effects. |

## Events
| Type | Description |
|------|-------------|
| `RpgAttackEvent` | Attack UnityEvent. |
| `RpgGameObjectEvent` | UnityEvent<GameObject> for RPG. |
| `RpgStringEvent` | UnityEvent<string> for RPG. |

## Helpers / Runtime
| Type | Description |
|------|-------------|
| `IRpgCombatReceiver` | Damage receiver interface. |
| `IRpgDamageModifier` | Code hook into `RpgCharacter`'s incoming damage pipeline (shields, armor, caps). |
| `RpgCombatMath` | Legacy multiplier utility over `ActiveBuffEntry` sequences (allocates; kept for compatibility). |
| `RpgModifierMath` | Allocation-free outgoing/incoming/movement/attack-speed multipliers over projected modifiers. |
| `RpgTargetingUtility` | Internal receiver-based physics target search, used by selectors/presets; allocation-free when warm. |
| `RpgPhysicsQueryBuffers` | Internal growable 3D/2D query buffers used by attacks, targeting, auras and projectiles. |
| `RpgCharacterResourceService` | Plain C# resource dictionary, mutations, reactive queries, and regeneration clocks. |
| `RpgCharacterStatService` | Plain C# stat runtimes, upgrade rules, invested points, stat/resource resolvers, save rows. |
| `RpgCharacterProgressionService` | Plain C# level, XP, upgrade points and applied-growth anchor with overflow-safe arithmetic (`ClampLevel` saturates a `long` level into the valid range). |
| `RpgResourceRuntime` / `RpgStatRuntime` | Current runtime values and reactive objects; initial values obey definition limits, and a NaN write keeps the previous value. |
| `RpgStatResolver` | Resolves stat values, resource maxima, and per-second regen with active modifiers. |
| `RpgCharacterProfileService` | Serializes a sanitized clone and deserializes universal profiles. |
| `RpgCharacterNetworkCommand` | Transport-neutral mutation request; `IsKnownType(type)` tells a named command from a raw wire value. |
| `RpgEffectShelf` | Effect libraries, stacking, UTC expiry, status damage ticks, modifier projection, and snapshots; `GetStatusMovementMultiplier()` and `HasBlockingStatus()` answer the status queries without allocating. |

`RpgStatResolver.ResolveRegen(resource, stats, modifiers, upgradeRules)` also includes invested
`AddRegenFlat` bonuses. The original three-argument overload remains available.

The services compose without a scene:

```csharp
RpgCharacterStatService stats = new();
stats.Build(template.stats);
stats.BuildUpgradeRules(template.progression);

RpgCharacterProgressionService progression = new() { Definition = template.progression };
progression.SetLevel(5);
progression.AddUpgradePoints(progression.GetUpgradePointsForLevelChange(1, 5));
if (stats.CanUpgrade("Vitality", progression.UpgradePoints) &&
    stats.TryGetUpgradeRule("Vitality", out RpgStatUpgradeRule rule) &&
    progression.TrySpendUpgradePoints(rule.costPerUpgrade))
{
    stats.AddInvestment("Vitality");
}

float vitality = stats.ResolveStat(stats.Stats["Vitality"], progression.AppliedGrowthLevel, null);
```

`RpgEffectShelf.Tick` permits callbacks to clear/remove/apply effects. If a callback changes the shelf,
the current pass stops before visiting stale entries; the next tick processes remaining effects.
Effect expiry uses persisted UTC timestamps, while tick damage is accumulated from the supplied delta time.

## Clock and callback contracts (11.0)

Both tick clocks deliver at most **1024 logical ticks per entry per public invocation**. They discard
excess whole intervals and retain the fractional phase. Double arithmetic reduces a huge delta separately
from the previous remainder, so a small prior phase survives. Public resource phase remains a float.
Nonfinite inputs and unrepresentable delivery amounts produce no tick-damage/regen delivery and do not
poison subsequent finite calls. Independent UTC expiry still runs and may invoke expiry callbacks.
Legacy nonpositive interval fallbacks remain in place. Status clocks preserve finite
negative delayed phase; resource regen ignores nonpositive deltas. Negative phase beyond the representable
float range saturates at `-float.MaxValue`.

Pending counts and remainders are consumed before callbacks. Nested ticks on the same service/shelf return
immediately; `finally` releases the guard when observers throw. Resource alive/full/enabled/mode/pause gates
remain authoritative; a frame crossing the end of a regen pause is still skipped. UTC buff expiry precedes
statuses; a status due to expire retains damage-before-expiry ordering. The bound is per entry, not a
global frame/character performance guarantee.

`Build`, `Clear`, facade rebuild and profile application invalidate in-flight resource work. Resolvers,
reactive setters and resource/stat notifications are followed by validity checks before later writes,
events or dictionary iteration. A callback replacing the runtime with pool `C = 7` leaves that replacement
untouched by the old operation. Public resource signatures and resolver overloads remain intact.

The profile schema remains v2. `RpgResourceSaveEntry.Max` is effective Max for one restore call; last
duplicate known ID wins. `BaseMax` remains the definition base for later ordinary refreshes.
See [RpgCharacter](RpgCharacter.md) for restore and publication ordering.

По-русски: часы ограничены 1024 тиками на запись за вызов, остаточная фаза сохраняется. Перестроение
из callback останавливает старый проход; новые словари и значения не изменяются его продолжением.

## See Also
- ← [Rpg](README.md)
