# RpgProjectile

**What it is:** a lightweight projectile runtime (`MonoBehaviour`, `Scripts/Rpg/Components/RpgProjectile.cs`, namespace `Neo.Rpg`) used by ranged RPG attacks.

**How to use:**
1. Add the component (menu `Neoxider/RPG/RpgProjectile`) to a projectile prefab; hits are found with physics
   casts between frames, so the prefab needs no collider of its own.
2. Assign the prefab as `ProjectilePrefab` of an [RpgAttackDefinition](RpgAttackDefinition.md) with the
   `Projectile` delivery; the attack controller instantiates it and calls `Initialize(...)`.

**Navigation:** [← RPG](./README.md)

---

## What it does

- Moves forward using speed from `RpgAttackDefinition`
- Checks hits between frames with physics casts
- Applies the same attack payload on impact
- Can pierce multiple targets up to `ProjectileMaxHits`
- Deduplicates repeated hits by `IRpgCombatReceiver`, not just by `GameObject`, so several colliders on one character do not consume several hit slots.
- Repeated `Initialize(...)` calls reset lifetime, remaining hits, and hit-deduplication state, so the component is safe to reuse in pool-friendly projectile flows.

## Complete hit queries

`_maxHitsPerCast` is the initial raw hit-buffer capacity of the 3D sphere cast. A saturated query grows and
requeries; after 4096 reusable slots, saturation falls back to a complete allocating query. The 2D circle
cast uses Unity's list overload (layer mask plus the global **Queries Hit Triggers** setting, like the former
`CircleCastNonAlloc`), which grows on its own. Source colliders, objects without receivers, and multiple
hurtboxes cannot hide a valid target merely by filling that initial buffer. Buffers retain their grown
capacity for later segments and drop their collider references when the projectile is disabled.

Hits within each dimension are sorted by distance, then by the collider's engine identity for ties (no
string formatting, stable within a session).
The 3D pass still precedes the 2D pass; this is not a combined cross-dimensional distance ordering.
`RpgAttackDefinition.ProjectileMaxHits` remains the limit on successful delivered hits. Callbacks that
disable/reinitialize a projectile stop its former hit pass and movement continuation.

По-русски: размер буфера не ограничивает поиск целей. Лимит реально нанесённых попаданий задаётся
`ProjectileMaxHits`; повторные коллайдеры одного получателя не расходуют дополнительные попадания.
