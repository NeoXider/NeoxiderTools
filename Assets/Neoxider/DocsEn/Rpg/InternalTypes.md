# RPG Module — Internal Types

## Enums
| Type | Description |
|------|-------------|
| `BuffStatType` | Buff stat type (Health, Attack, Defense, etc.). |
| `RpgAttackDeliveryType` | Damage delivery method (Melee, Projectile, Aura). |
| `RpgHitMode` | Hit mode (Single, AOE, Piercing). |
| `RpgInputTriggerType` | Input trigger type (Press, Hold, Release). |
| `RpgMouseButton` | Mouse button (Left, Right, Middle). |
| `RpgTargetSelectionMode` | Target selection mode (Nearest, Manual, Auto). |
| `RpgConditionEvaluationMode` | Condition evaluation mode (All, Any). |
| `RpgNoCodeActionType` | No-Code action type for RPG. |

## Data / Config
| Type | Description |
|------|-------------|
| `AuraWeapon` | Aura weapon config (radius, damage, ticks). |
| `BuffStatModifier` | Buff stat modifier (type, value, duration). |
| `RpgAttackEffectRefs` | Attack effect references (VFX, SFX). |
| `RpgButtonBinding` | Button-to-RPG action binding. |
| `RpgStatGrowthDefinition` | Stat growth definition (formula, coefficients). |
| `RpgTargetQuery` | Target search query. |
| `RpgProfileData` | RPG character profile data. |

## Events
| Type | Description |
|------|-------------|
| `RpgAttackEvent` | Attack UnityEvent. |
| `RpgGameObjectEvent` | UnityEvent<GameObject> for RPG. |
| `RpgStringEvent` | UnityEvent<string> for RPG. |

## Helpers / Runtime
| Type | Description |
|------|-------------|
| `RpgProgressionHelper` | Helper for RPG + Progression integration. |
| `IRpgCombatReceiver` | Damage receiver interface. |
| `RpgCombatMath` | Damage calculation utility. |
| `RpgTargetingUtility` | Target search utility. |

## See Also
- ← [Rpg](README.md)
