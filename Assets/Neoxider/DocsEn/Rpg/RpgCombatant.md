# RpgCombatant

**What it is:** a scene-local combat receiver for enemies, NPCs, destructibles, and any non-persistent actor.

**Navigation:** [← RPG](./README.md)

---

## Use it when

- You need HP/level/buffs/statuses on a scene object without `SaveProvider`.
- You need a target for `RpgAttackController`.
- You need an actor that can be damaged, healed, buffed, stunned, or made temporarily invulnerable.

## API Integration

`RpgCombatant` now accepts an `RpgDamageInfo` structure instead of a raw float, passing through crucial context for advanced combat systems.

### Basic Damage Flow

```csharp
RpgCombatant target = GetComponent<RpgCombatant>();

// Create damage context
var damageInfo = new RpgDamageInfo(
    amount: 50f, 
    source: this.gameObject, 
    damageType: "Fire"
);

// Apply damage with elemental resistances properly calculated
float actualDamageTaken = target.TakeDamage(damageInfo);
```

### Elemental Resistances

To create an elemental resistance buff:
1. Create a `BuffDefinition` asset structure.
2. Add a `BuffStatModifier` and set the stat type to `SpecificDefensePercent`.
3. Set the `SpecificDamageType` to `Fire` (or your chosen damage string). 
4. The `RpgCombatMath` pipeline will automatically extract the `damageType` from `RpgDamageInfo` and reduce incoming damage by `SpecificDefensePercent`.
