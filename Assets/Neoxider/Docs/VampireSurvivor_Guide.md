# Building a Vampire Survivors 3D Guide

This guide describes the architecture and assembly steps of a full-fledged Vampire Survivors clone using only the built-in components of the NeoxiderTools library.

## Contents
- [Purpose](#purpose)
- [Step 1: Player and Movement](#step-1-player-and-movement)
- [Step 2: Parallel Abilities (Weapons)](#step-2-parallel-abilities-weapons)
- [Step 3: Enemy Waves](#step-3-enemy-waves)
- [Step 4: Professional NPC HUD (Floating HP)](#step-4-professional-npc-hud-floating-hp)
- [Step 5: Experience System and Level Up](#step-5-experience-system-and-level-up)
- [See Also](#see-also)

---

## Purpose
The goal of this guide is to show how to use the NeoxiderTools component-based approach to build a highly dynamic RPG game with automatic firing, character progression, and a professional interface without writing complex code.

---

## Step 1: Player and Movement

1. **Create the player object (capsule/model).**
2. Add movement components (for example, `TargetTransformFollow` or the standard `CharacterController`).
3. Add `RpgCharacter` (for resources, stats, and handling incoming damage).
4. **Stats setup**: In `RpgCharacter` or `RpgCharacterTemplate`, set base values and progression rules for stat growth per level.
5. **Health UI**: Display the player's health via `RpgResourceBinding` + `SetProgress` in the Canvas. For an `HP / MaxHP` text, use `NoCodeFormattedText`.

---

## Step 2: Parallel Abilities (Weapons)

The key feature is parallel automatic firing. The player simply attaches different weapon components as child objects.

### Example A: Garlic Aura (`AuraWeapon`)
- **Component**: Attach `AuraWeapon` (inherits from `MeleeWeapon`).
- **Damage**: Automatically multiplied by level and buff bonuses from `RpgCharacter`. If the player has +885% damage, the Aura will hit 9.85 times harder than the base value.
- **Cycle**: Attach a `TimerObject`. Configure it to trigger every 1.0 sec. In `OnTick`, wire a UnityEvent -> `AuraWeapon.Attack()`.
- **Trigger**: Add a `SphereCollider` (IsTrigger = true) around the player to detect targets.

### Example B: Throwing Knives (`RpgProjectileLauncher`)
- **Component**: Attach `RpgProjectileLauncher`.
- **Projectile**: Select the projectile prefab in the inspector.
- **Search**: Set the search mask and radius for automatic targeting of the nearest enemies.
- **Cycle**: Use a `TimerObject` to periodically call `Fire()`.

---

## Step 3: Enemy Waves

1. **Spawner**: Create an empty object and attach `Spawner` (from `Neo.Tools`).
2. **Wave setup**: Change `Spawn Mode` to `Waves`. Configure `Base Wave Count` and delays.
3. **Enemy prefab**: Must contain:
   - `NPC_Navigation` — for chasing the target.
   - `RpgCharacter` — for health (tag `Enemy`).
   - `RpgContactDamage` — for dealing damage on contact with the player.
   - `DemoNpcUI` — for automatic HP bar creation.

---

## Step 4: Professional NPC HUD (Floating HP)

For enemies to look polished, the health indicators must be stable.

| Field/Setting | Recommended Value | Description |
|----------------|------------------------|----------|
| **DemoNpcUI** | (component) | Automatically creates a world-space Canvas on spawn. |
| **HP Text** (`NoCodeFormattedText`) | sources: `HpValue`, `MaxHpValue` | Shows current and max health (e.g. `50 / 50`). |
| **Billboard Mode** | `AwayFromCamera` | The most stable mode for UI in 3D. |
| **Ignore Y** | `True` | Prevents UI tilting when viewed from above. |

---

## Step 5: Experience System and Level Up

1. **Progression**: Add a `ProgressionManager` to the scene.
2. **Gaining XP**: In the enemies' `RpgCharacter`, configure XP granting via a death/drop/no-code event.
3. **Level up**: Subscribe to the `ProgressionManager.OnLevelUp` event.
4. **Pause and choice**:
   - On level up: `Time.timeScale = 0`.
   - Show the card selection UI. Add a new weapon object (Step 2) or a buff to `RpgCharacter`.
   - Restore `Time.timeScale = 1`.

---

## See Also
- [RPG Module Index](./Rpg/README.md)
- [Tools Module Index](./Tools/README.md)
- [Progression System](./Progression/README.md)
- ← [Back to the main table of contents](../README.md)
