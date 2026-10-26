# RPG Module — Internal Types

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
| `RpgTargetQuery` | Target search query. |
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
| `RpgCombatMath` | Damage calculation utility. |
| `RpgTargetingUtility` | Internal receiver-based physics target search, used by selectors/presets. |
| `RpgCharacterResourceService` | Plain C# resource dictionary, mutations, reactive queries, and regeneration clocks. |
| `RpgResourceRuntime` / `RpgStatRuntime` | Current runtime values and reactive objects; initial values obey definition limits. |
| `RpgStatResolver` | Resolves stat values, resource maxima, and per-second regen with active modifiers. |
| `RpgCharacterProfileService` | Serializes a sanitized clone and deserializes universal profiles. |
| `RpgEffectShelf` | Effect libraries, stacking, UTC expiry, status damage ticks, modifier projection, and snapshots. |

`RpgStatResolver.ResolveRegen(resource, stats, modifiers, upgradeRules)` also includes invested
`AddRegenFlat` bonuses. The original three-argument overload remains available.

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
