# RpgProjectile

**What it is:** a lightweight projectile runtime used by ranged RPG attacks.

**Navigation:** [← RPG](./README.md)

---

## What it does

- Moves forward using speed from `RpgAttackDefinition`
- Checks hits between frames with physics casts
- Applies the same attack payload on impact
- Can pierce multiple targets up to `ProjectileMaxHits`
- Deduplicates repeated hits by `IRpgCombatReceiver`, not just by `GameObject`, so several colliders on one character do not consume several hit slots.
- Repeated `Initialize(...)` calls reset lifetime, remaining hits, and hit-deduplication state, so the component is safe to reuse in pool-friendly projectile flows.
