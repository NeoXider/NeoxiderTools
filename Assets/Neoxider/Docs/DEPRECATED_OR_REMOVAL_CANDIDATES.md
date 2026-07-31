# Deprecated Components and Removal Candidates

**What it is:** This file is maintained for planning removals and end-of-support. Components and types marked in code with the `[Obsolete]` attribute are listed here along with their replacements.

**How to use:** see the sections below.

---


This file is maintained for planning removals and end-of-support. Components and types marked in code with the `[Obsolete]` attribute are listed here along with their replacements.

**Target removal version: 10.0** — all types in the table below will be removed in the first major 10.x release; until then they are kept for backward compatibility.

## Table: old → new / status

| Old script / component | New / replacement | Status | Note |
|---------------------------|----------------|--------|------------|
| TimeReward | CooldownReward | Obsolete | See Bonus/TimeReward. |
| AiNavigation | Neo.NPC.NpcNavigation | Obsolete | See Tools/Other. |
| HandLayoutType (enum) | CardLayoutType | Obsolete | In Cards/Config/HandLayoutType.cs; use the CardLayoutType enum. |
| HandComponent.LegacyLayoutType (property) | HandComponent.LayoutType (CardLayoutType) | Obsolete | Only the LegacyLayoutType property is obsolete; HandComponent itself is current. |
| Health | Neo.Rpg.Components.RpgCharacter | Obsolete | Persistent/local RPG actor via `RpgCharacter`. |
| AttackExecution | Neo.Rpg.RpgAttackController + RpgAttackDefinition | Obsolete | Universal melee/ranged/aoe attack system. |
| Evade | Neo.Rpg.RpgEvadeController | Obsolete | Evasion, cooldown, and invulnerability locks. |
| AdvancedAttackCollider | Neo.Rpg.RpgAttackController + Neo.Rpg.RpgProjectile | Obsolete | For legacy `IDamageable`, `RpgStatsDamageableBridge` serves as the bridge. |

## Planned removal

Candidates for complete removal from the codebase are decided release by release. At the moment, removal of the types listed above is not scheduled; they are kept for backward compatibility.
