# NpcCombatPreset

**What it is:** a `ScriptableObject` (`Scripts/NPC/Combat/NpcCombatPreset.cs`, namespace `Neo.NPC.Combat`) holding
the data for `NpcRpgCombatBrain`: an `RpgAttackPreset` plus the distances and behaviour flags that shape chase /
hold / attack decisions.

**How to use:**
1. Create it via `Assets -> Create -> Neoxider/NPC/Npc Combat Preset` and assign an `RpgAttackPreset`.
2. Reference it from `NpcRpgCombatBrain` (Inspector or `brain.Preset = preset`).

---

## Setup

- Create via `Assets -> Create -> Neoxider/NPC/Npc Combat Preset`.
- Assign an `RpgAttackPreset` and tune the distances, then reference the preset from an `NpcRpgCombatBrain`.

## Key Fields (Inspector)

| Field | Default | Description |
|-------|---------|-------------|
| `_id` | (asset name) | Stable id; falls back to the asset name when blank. |
| `_displayName` | `NPC Combat Preset` | Human-readable name. |
| `_attackPreset` | none | `RpgAttackPreset` executed by the attack controller. |
| `_preferredAttackDistance` | `2` | Distance at/under which the NPC attacks instead of chasing. |
| `_loseTargetDistance` | `15` | Distance past which the target is dropped. |
| `_runWhileChasing` | `true` | Use run speed while chasing. |
| `_stopMovementInsideAttackRange` | `true` | Stop the agent when in attack range. |
| `_faceTargetBeforeAttack` | `true` | Rotate to face the target before attacking. |
| `_autoRestoreNavigationMode` | `true` | Restore the NPC's previous nav mode when the target is lost. |

## Public API (read-only)

`Id`, `DisplayName`, `AttackPreset`, `PreferredAttackDistance`, `LoseTargetDistance`, `RunWhileChasing`, `StopMovementInsideAttackRange`, `FaceTargetBeforeAttack`, `AutoRestoreNavigationMode`. Distance getters clamp to a minimum of `0.1`.

## See Also

- [Module Root](../README.md)
- [NpcRpgCombatBrain](./NpcRpgCombatBrain.md)
- [NpcCombatScenarios](./NpcCombatScenarios.md)
