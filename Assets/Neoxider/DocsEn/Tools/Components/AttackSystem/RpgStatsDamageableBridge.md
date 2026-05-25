# RpgStatsDamageableBridge

**What it is:** a compatibility bridge from the legacy `AttackSystem` into the current RPG combat layer. File: `Scripts/Tools/Components/AttackSystem/RpgStatsDamageableBridge.cs`.

**Navigation:** [← AttackSystem](./README.md) · [RPG](../../../Rpg/README.md)

## Purpose

The component implements the old `IDamageable` and `IHealable` interfaces, then forwards actual damage and healing into `RpgCharacter`.

Use it for old scenes, prefabs, and components that still depend on `IDamageable/IHealable`, such as `AdvancedAttackCollider`. For new RPG flows, call `RpgCharacter`, `IRpgCombatReceiver`, `RpgAttackController`, or `RpgNoCodeAction` directly.

## Setup

1. Add `RpgCharacter` to the actor.
2. Add `RpgStatsDamageableBridge` to the same object or to a child hitbox.
3. If the bridge is not under the target `RpgCharacter`, assign `_character` explicitly.
4. Legacy code calls `TakeDamage(int)` / `Heal(int)`, and the bridge forwards to `RpgCharacter.Damage(float)` / `RpgCharacter.Heal(float)`.

## Fields

| Field | Description |
|-------|-------------|
| `_character` | Explicit `RpgCharacter` reference; when empty, the bridge searches parents. |
| `_damageMultiplier` | Damage multiplier before forwarding to `RpgCharacter`. |
| `_healMultiplier` | Heal multiplier before forwarding to `RpgCharacter`. |

## Behavior

- `TakeDamage(int amount)` ignores `amount <= 0`.
- `Heal(int amount)` ignores `amount <= 0`.
- `DamageMultiplier` and `HealMultiplier` clamp negative values to `0`.
- The bridge does not add network authority by itself. Networked objects must still follow `RpgCharacter` / `NeoNetworkComponent` mutation rules.
