# RpgProjectile

**What it is:** a lightweight projectile runtime used by ranged RPG attacks.

**Navigation:** [← RPG](./README.md)

---

## What it does

- Moves forward using speed from `RpgAttackDefinition`
- Checks hits between frames with physics casts
- Applies the same attack payload on impact
- Can pierce multiple targets up to `ProjectileMaxHits`
