# RpgAttackPreset

**What it is:** a `ScriptableObject` preset (`Scripts/Rpg/Data/RpgAttackPreset.cs`, namespace `Neo.Rpg`) that combines an `RpgAttackDefinition` with targeting rules for AI, skills, and spells.

**How to use:**
1. Create an asset via **Create > Neoxider > RPG > Rpg Attack Preset** and assign its attack definition.
2. Set the targeting rules (required target, selector use, aiming, target query).
3. Add it to the `Presets` list of an [RpgAttackController](RpgAttackController.md) and call `TryUsePreset(...)`.

**Navigation:** [← RPG](./README.md)

---

## Stores

- `RpgAttackDefinition`
- `Require Target`
- `Use Selector Component When Available`
- `Aim At Target`
- `RpgTargetQuery`
